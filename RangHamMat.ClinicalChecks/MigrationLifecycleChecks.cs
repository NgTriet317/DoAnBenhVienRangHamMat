using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RangHamMat.ClinicalChecks;

/// <summary>DDL chỉ chạy khi người vận hành chọn rõ mode và database thử. Không restore/drop database.</summary>
internal static class MigrationLifecycleChecks
{
    private static Dictionary<string, string[]> legacyColumns = new();
    private static string Root => Directory.GetCurrentDirectory();
    private static string Script(string name) => File.ReadAllText(Path.Combine(Root, "Database", "Migrations", name));
    private static string Upgrade => Script("20261024_02_ClinicalWorkflow.sql");
    private static string Rollback => Script("20261024_04_Rollback_ClinicalWorkflow.sql");
    private static string Verify => Script("20261024_03_Verify_ClinicalWorkflow.sql");

    internal static int Run(bool rollbackRoundtrip)
    {
        using var db = MigrationChecks.Open();
        if (db.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0") != 46)
            throw new InvalidOperationException("Lifecycle yêu cầu bản restore baseline 46 bảng chưa nâng cấp.");
        legacyColumns = db.Query<(string TableName, string ColumnName)>("SELECT t.name,c.name FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id WHERE t.is_ms_shipped=0 ORDER BY t.name,c.column_id")
            .GroupBy(v => v.TableName).ToDictionary(g => g.Key, g => g.Select(v => v.ColumnName).ToArray());
        var history = LegacyData(db);
        var objects = LegacyObjects(db);
        var schema = Schema(db);
        Execute(db, Upgrade);
        Require(history == LegacyData(db), "Nâng cấp giữ nguyên toàn bộ cột/dòng của 46 bảng cũ");
        Require(objects == LegacyObjects(db), "Trigger và view baseline giữ nguyên định nghĩa");
        var upgraded = Schema(db);
        Execute(db, Upgrade);
        Require(upgraded == Schema(db), "Chạy nâng cấp lần hai không đổi schema/nhân bản đối tượng");
        Execute(db, Verify);
        var failed = MigrationChecks.CheckContract(db);
        if (failed != 0) return failed;
        if (rollbackRoundtrip)
        {
            Execute(db, Rollback);
            Require(schema == Schema(db), "Rollback trước dữ liệu mới khôi phục schema baseline");
            Require(history == LegacyData(db) && objects == LegacyObjects(db), "Rollback giữ dữ liệu/trigger/view cũ");
            Execute(db, Upgrade);
            Require(upgraded == Schema(db), "Có thể nâng cấp lại sau rollback");
            BlockedRollback(f => f.Execute("UPDATE dbo.LICH_SU_KHAM_BENH SET LyDoKhongKeThuoc=N'FIXTURE không dùng thật' WHERE MaLS=@id",new{id=f.Id("LSA")}),"cột mới");
            BlockedRollback(f =>
            {
                var user=f.User("X");
                new BUS.BUS_DMTINHTRANGBENH(f.Transaction).LuuNhap(new ET.ET_DMTINHTRANGBENH { TenTinhTrang="FIXTURE không dùng thật",TrangThai="Đang áp dụng" },user);
            },"bảng mới");
            Require(upgraded == Schema(db), "Rollback bị chặn giữ đủ schema mới");
            Require(history == LegacyData(db), "Fixture kiểm tra rollback không để lại dữ liệu");
        }
        Console.WriteLine("PASS: Migration lifecycle hoàn tất trên " + db.Database);
        return 0;
    }

    internal static void Execute(SqlConnection db, string sql,System.Data.IDbTransaction? transaction=null)
    {
        // Các artifact của dự án chỉ dùng GO riêng dòng; không hỗ trợ GO trong chuỗi nhiều dòng.
        foreach (var batch in Regex.Split(sql, @"^\s*GO\s*(?:--[^\r\n]*)?$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            if (!string.IsNullOrWhiteSpace(batch)) db.Execute(batch, transaction:transaction,commandTimeout: 120);
    }

    private static void BlockedRollback(Action<Fixture> setup,string target)
    {
        using var fixture=new Fixture();
        setup(fixture);
        try
        {
            Execute(fixture.Connection, Rollback,fixture.Transaction);
            throw new InvalidOperationException("Rollback phải dừng khi có dữ liệu cột mới.");
        }
        catch (SqlException ex) when (ex.Number == 52104)
        {
            Console.WriteLine("PASS: Rollback từ chối dữ liệu "+target+" bằng lỗi 52104");
        }
    }

    private static string LegacyData(SqlConnection db)
    {
        var combined = new StringBuilder();
        foreach (var (name, names) in legacyColumns)
        {
            var columns = names.Select(n => "[" + n + "]").ToArray();
            // Chỉ lấy các cột baseline: cột thêm/default mới không làm nhiễu hash lịch sử.
            var json = string.Concat(db.Query<string>($"SELECT {string.Join(',', columns)} FROM dbo.[{name}] ORDER BY {columns[0]} FOR JSON PATH,INCLUDE_NULL_VALUES"));
            combined.Append(name).Append(Hash(json));
        }
        return Hash(combined.ToString());
    }

    private static string LegacyObjects(SqlConnection db) => Hash(string.Concat(db.Query<string>("""
        SELECT o.type,o.name,m.definition FROM sys.objects o
        JOIN sys.sql_modules m ON m.object_id=o.object_id
        WHERE o.type IN('V','TR') AND o.name NOT LIKE N'TR[_]CW[_]%' ORDER BY o.type,o.name FOR JSON PATH,INCLUDE_NULL_VALUES
        """)));

    private static string Schema(SqlConnection db)
    {
        var parts = new[]
        {
            "SELECT t.name AS tbl,c.name AS col,ty.name AS typ,c.max_length,c.precision,c.scale,c.is_nullable,c.is_computed,dc.definition AS def FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id WHERE t.is_ms_shipped=0 ORDER BY t.name,c.column_id FOR JSON PATH,INCLUDE_NULL_VALUES",
            "SELECT o.type,o.name,m.definition FROM sys.objects o LEFT JOIN sys.sql_modules m ON m.object_id=o.object_id WHERE o.is_ms_shipped=0 AND o.type IN('U','V','TR','F','C','PK','UQ','D') ORDER BY o.type,o.name FOR JSON PATH,INCLUDE_NULL_VALUES",
            "SELECT t.name AS tbl,i.name,i.is_unique,i.is_disabled,i.filter_definition,c.name AS col,ic.key_ordinal,ic.is_included_column FROM sys.tables t JOIN sys.indexes i ON i.object_id=t.object_id JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id ORDER BY t.name,i.name,ic.index_column_id FOR JSON PATH,INCLUDE_NULL_VALUES"
        };
        return Hash(string.Join('|', parts.Select(sql => string.Concat(db.Query<string>(sql)))));
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static void Require(bool ok, string name)
    {
        if (!ok) throw new InvalidOperationException(name);
        Console.WriteLine("PASS: " + name);
    }
}
