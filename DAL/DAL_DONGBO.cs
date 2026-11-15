using System.Data;
using ET;
using Microsoft.Data.SqlClient;

namespace DAL;

/// <summary>
/// Chỉ đọc metadata và Change Tracking trong snapshot ngắn. Không dùng dbconnect vì lớp đó
/// lấy cấu hình/principal môi trường, còn tác vụ nền phải giữ cấu hình rõ ràng của phiên.
/// </summary>
public sealed class DAL_DONGBO
{
    private readonly string connectionString;

    public DAL_DONGBO(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        // Không tham gia TransactionScope của thao tác UI đang lưu.
        this.connectionString = new SqlConnectionStringBuilder(connectionString) { Enlist = false }.ConnectionString;
    }

    /// <summary>Chỉ các giao dịch đã commit mới tạo kết quả; mọi bước xác minh mốc nằm trong cùng snapshot.</summary>
    public async Task<DotThayDoiDuLieu> DocThayDoiAsync(long? version, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            // Kiểm tra trước khi mở snapshot để cấu hình thiếu có thông báo dùng được ngay.
            await using (var check = new SqlCommand("""
                IF NOT EXISTS(SELECT 1 FROM sys.databases WHERE database_id=DB_ID() AND snapshot_isolation_state=1)
                    THROW 56010,N'Chưa bật ALLOW_SNAPSHOT_ISOLATION (snapshot). Chạy phần cài đồng bộ trong file SQL chính.',1;
                IF ISNULL(HAS_PERMS_BY_NAME(DB_NAME(),N'DATABASE',N'VIEW DEFINITION'),0)<>1
                    THROW 56011,N'Đồng bộ SQL cần quyền VIEW DEFINITION trên database để kiểm đủ bảng theo dõi.',1;
                IF NOT EXISTS(SELECT 1 FROM sys.change_tracking_databases WHERE database_id=DB_ID())
                    THROW 56012,N'Chưa bật Change Tracking trên database. Chạy phần cài đồng bộ trong file SQL chính.',1;
                """, connection) { CommandTimeout = 10 })
                await check.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.Snapshot, cancellationToken).ConfigureAwait(false);
            var tables = new List<Table>();
            // VIEW DEFINITION bảo đảm metadata không âm thầm che bảng mà tài khoản SQL không thấy.
            await using (var metadata = Command("""
                SELECT s.name,t.name,
                    CONVERT(bit,CASE WHEN EXISTS(SELECT 1 FROM sys.indexes i WHERE i.object_id=t.object_id AND i.is_primary_key=1) THEN 1 ELSE 0 END),
                    CONVERT(bit,CASE WHEN ct.object_id IS NOT NULL THEN 1 ELSE 0 END),
                    CHANGE_TRACKING_MIN_VALID_VERSION(t.object_id),
                    HAS_PERMS_BY_NAME(QUOTENAME(s.name)+N'.'+QUOTENAME(t.name),N'OBJECT',N'VIEW CHANGE TRACKING'),
                    HAS_PERMS_BY_NAME(QUOTENAME(s.name)+N'.'+QUOTENAME(t.name),N'OBJECT',N'SELECT')
                FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
                LEFT JOIN sys.change_tracking_tables ct ON ct.object_id=t.object_id
                WHERE t.is_ms_shipped=0 ORDER BY s.name,t.name;
                """, connection, transaction))
            await using (var reader = await metadata.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    string schema = reader.GetString(0), name = reader.GetString(1);
                    if (!reader.GetBoolean(2)) throw Configuration($"Bảng {schema}.{name} thiếu khóa chính (PRIMARY KEY).");
                    if (!reader.GetBoolean(3)) throw Configuration($"Bảng {schema}.{name} chưa bật Change Tracking.");
                    if (reader.IsDBNull(4) || reader.IsDBNull(5) || reader.GetInt32(5) != 1 || reader.IsDBNull(6) || reader.GetInt32(6) != 1)
                        throw Configuration($"Thiếu quyền SELECT hoặc VIEW CHANGE TRACKING trên bảng {schema}.{name}.");
                    tables.Add(new Table(schema, name, reader.GetInt64(4)));
                }
            }
            if (tables.Count == 0) throw Configuration("Không có bảng dữ liệu để theo dõi.");

            long currentVersion;
            string databaseIdentity;
            // Lấy mốc SAU khi xác minh minimum và TRƯỚC khi đọc từng bảng, trong cùng snapshot.
            await using (var checkpoint = Command("""
                SELECT CHANGE_TRACKING_CURRENT_VERSION(),
                    CONCAT(CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')),N'/',DB_NAME(),N'/',
                        CONVERT(nvarchar(36),r.database_guid),N'/',CONVERT(nvarchar(36),r.recovery_fork_guid)),
                    r.database_guid,r.recovery_fork_guid
                FROM sys.database_recovery_status r WHERE r.database_id=DB_ID();
                """, connection, transaction))
            await using (var reader = await checkpoint.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false) || reader.IsDBNull(0) || reader.IsDBNull(2) || reader.IsDBNull(3))
                    throw Configuration("Không đọc được định danh phục hồi database; tài khoản SQL cần thấy sys.database_recovery_status (chủ database hoặc VIEW ANY DATABASE).");
                currentVersion = reader.GetInt64(0);
                databaseIdentity = reader.GetString(1);
            }

            bool fullRefresh = !version.HasValue || version < 0 || version > currentVersion || tables.Any(t => version < t.MinimumVersion);
            var changed = new List<string>();
            if (fullRefresh) changed.AddRange(tables.Select(t => t.NotificationName));
            else if (version != currentVersion)
            {
                // Một roundtrip cho mọi bảng, tránh hàng chục SELECT mỗi nhịp trên từng máy.
                // Tên bảng lấy từ metadata và được quote riêng cho identifier/literal.
                string sql = string.Join(" UNION ALL ", tables.Select(table =>
                    $"SELECT N'{table.NotificationName.Replace("'", "''", StringComparison.Ordinal)}' WHERE EXISTS(SELECT 1 FROM CHANGETABLE(CHANGES {Quote(table.Schema)}.{Quote(table.Name)},@version) c)"));
                await using var query = Command(sql, connection, transaction);
                query.Parameters.Add("@version", SqlDbType.BigInt).Value = version!.Value;
                await using var reader = await query.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) changed.Add(reader.GetString(0));
            }
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new DotThayDoiDuLieu(currentVersion, databaseIdentity, fullRefresh, changed.ToArray());
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Không đọc được đồng bộ SQL: " + ex.Message, ex);
        }
    }

    private static SqlCommand Command(string sql, SqlConnection connection, SqlTransaction transaction)
        => new(sql, connection, transaction) { CommandTimeout = 10 };
    private static string Quote(string identifier) => "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]";
    private static InvalidOperationException Configuration(string message)
        => new("Chưa cấu hình đồng bộ SQL: " + message + " Kiểm tra phần cài đồng bộ trong file SQL chính.");
    private sealed record Table(string Schema, string Name, long MinimumVersion)
    {
        public string NotificationName => Schema == "dbo" ? Name : Schema + "." + Name;
    }
}
