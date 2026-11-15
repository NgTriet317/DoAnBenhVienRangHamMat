using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BUS;
using Microsoft.Data.SqlClient;

// Every mutation is confined to a newly created, randomly named test database.
var server = args.Length == 0 ? "lpc:." : args[0];
var database = "RHM_Hospital_Test_ChangeTracking_" + Guid.NewGuid().ToString("N");
var builder = new SqlConnectionStringBuilder
{
    DataSource = server, InitialCatalog = "master", IntegratedSecurity = true,
    TrustServerCertificate = true, Encrypt = SqlConnectionEncryptOption.Optional,
    ConnectTimeout = 10, Pooling = false
};
string masterConnection = builder.ConnectionString;
builder.InitialCatalog = database;
string connectionString = builder.ConnectionString;
int passed = 0;

void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

async Task Exec(string sql, string? connection = null)
{
    await using var db = new SqlConnection(connection ?? connectionString);
    await db.OpenAsync();
    await using var cmd = new SqlCommand(sql, db) { CommandTimeout = 30 };
    await cmd.ExecuteNonQueryAsync();
}

async Task<T> Scalar<T>(string sql)
{
    await using var db = new SqlConnection(connectionString);
    await db.OpenAsync();
    await using var cmd = new SqlCommand(sql, db);
    return (T)(await cmd.ExecuteScalarAsync())!;
}

async Task Check(string name, Func<Task> action)
{
    await action();
    passed++;
    Console.WriteLine("PASS " + name);
}

// Reflection lets the initial RED run fail on the missing public contract, before implementation exists.
Type? syncType = typeof(BUS_TIEPNHAN).Assembly.GetType("BUS.BUS_DONGBO");
async Task<Batch> Poll(long? version, string? connection = null, CancellationToken cancellationToken = default)
{
    Assert(syncType != null, "BUS_DONGBO must expose the SQL change tracking contract.");
    var instance = Activator.CreateInstance(syncType!, connection ?? connectionString)!;
    var task = (Task)syncType!.GetMethod("DocThayDoiAsync")!.Invoke(instance, [version, cancellationToken])!;
    await task;
    var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
    object Property(string name) => result.GetType().GetProperty(name)!.GetValue(result)!;
    return new Batch((long)Property("Version"), (string)Property("DatabaseIdentity"),
        (bool)Property("FullRefresh"), (string[])Property("Tables"));
}

async Task ExpectConfigurationError(string expected)
{
    try
    {
        await Poll(null);
    }
    catch (InvalidOperationException e) when (e.Message.Contains(expected, StringComparison.OrdinalIgnoreCase)) { return; }
    throw new InvalidOperationException("Reader accepted missing configuration: " + expected);
}

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
string sqlFile = Path.Combine(root, "Database/QLBenhVienRangHamMat.sql");
async Task Install()
{
    string source = await File.ReadAllTextAsync(sqlFile);
    const string marker = "-- BEGIN RHM CROSS-MACHINE CHANGE TRACKING";
    int start = source.IndexOf(marker, StringComparison.Ordinal);
    Assert(start >= 0, "The main SQL script must include the independently runnable change tracking installer.");
    string section = source[start..];
    foreach (string batch in Regex.Split(section, @"^\s*GO\s*\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        if (!string.IsNullOrWhiteSpace(batch)) await Exec(batch);
}

async Task<string> BusinessFingerprint()
{
    var content = new StringBuilder();
    await using var db = new SqlConnection(connectionString);
    await db.OpenAsync();
    var tables = new List<(string Name, string Keys)>();
    await using (var inventory = new SqlCommand("""
        SELECT QUOTENAME(s.name)+'.'+QUOTENAME(t.name),
        STUFF((SELECT ','+QUOTENAME(c.name) FROM sys.indexes i
          JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
          JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
          WHERE i.object_id=t.object_id AND i.is_primary_key=1 ORDER BY ic.key_ordinal
          FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,1,'')
        FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE t.is_ms_shipped=0 ORDER BY s.name,t.name
        """, db))
    await using (var reader = await inventory.ExecuteReaderAsync())
        while (await reader.ReadAsync()) tables.Add((reader.GetString(0), reader.GetString(1)));
    foreach (var (name, keys) in tables)
    {
        await using var read = new SqlCommand($"SELECT (SELECT * FROM {name} ORDER BY {keys} FOR JSON PATH,INCLUDE_NULL_VALUES)", db);
        content.Append(name).Append('\n').Append(Convert.ToString(await read.ExecuteScalarAsync())).Append('\n');
    }
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content.ToString())));
}

await Check("public SQL change tracking contract exists", async () =>
{
    Assert(syncType?.GetConstructor([typeof(string)]) != null, "BUS_DONGBO(string connectionString) is missing.");
    var method = syncType!.GetMethod("DocThayDoiAsync", [typeof(long?), typeof(CancellationToken)]);
    Assert(method?.ReturnType.FullName?.Contains("ET.DotThayDoiDuLieu") == true,
        "DocThayDoiAsync must return Task<DotThayDoiDuLieu>.");
    await Task.CompletedTask;
});

try
{
    Assert(Regex.IsMatch(database, "^RHM_Hospital_Test_[A-Za-z0-9_]+$"), "Unsafe test database name.");
    await Exec($"CREATE DATABASE [{database}]", masterConnection);
    Console.WriteLine("TEST DATABASE " + database);
    await Exec("CREATE TABLE dbo.Alpha(Id int NOT NULL PRIMARY KEY, Value int NOT NULL); CREATE TABLE dbo.Beta(Id int NOT NULL PRIMARY KEY, Value int NOT NULL); INSERT dbo.Alpha VALUES(1,10); INSERT dbo.Beta VALUES(1,20);");

    await Check("missing snapshot configuration is actionable", () => ExpectConfigurationError("snapshot"));
    await Check("installer adds tracking without changing rows", async () =>
    {
        await Install();
        Assert(await Scalar<int>("SELECT SUM(Value) FROM (SELECT Value FROM dbo.Alpha UNION ALL SELECT Value FROM dbo.Beta) x") == 30, "Installer changed existing rows.");
        var first = await Poll(null);
        Assert(first.FullRefresh && first.Tables.Order().SequenceEqual(new[] { "Alpha", "Beta" }), "Initial poll must request all tracked tables.");
        Assert(!string.IsNullOrWhiteSpace(first.Identity), "Database identity is required.");
    });

    await Check("installer preserves existing retention and data on rerun", async () =>
    {
        await Exec($"ALTER DATABASE [{database}] SET CHANGE_TRACKING (CHANGE_RETENTION = 3 DAYS, AUTO_CLEANUP = OFF)");
        var before = await Poll(null);
        await Install();
        Assert(await Scalar<int>("SELECT retention_period FROM sys.change_tracking_databases WHERE database_id=DB_ID()") == 3, "Installer replaced retention.");
        Assert(!await Scalar<bool>("SELECT CONVERT(bit,is_auto_cleanup_on) FROM sys.change_tracking_databases WHERE database_id=DB_ID()"), "Installer replaced auto cleanup setting.");
        Assert((await Poll(before.Version)).Tables.Length == 0, "Rerun must not write tracked data.");
        Assert(await Scalar<int>("SELECT SUM(Value) FROM (SELECT Value FROM dbo.Alpha UNION ALL SELECT Value FROM dbo.Beta) x") == 30, "Rerun changed business rows.");
    });

    await Check("quiet database returns a stable checkpoint", async () =>
    {
        var before = await Poll(null);
        var after = await Poll(before.Version);
        Assert(!after.FullRefresh && after.Version == before.Version && after.Tables.Length == 0, "Quiet poll falsely reported changes.");
        Assert(before.Identity == after.Identity, "Identity changed without restore.");
    });

    await Check("uncommitted writes and rollback never produce refresh", async () =>
    {
        var before = await Poll(null);
        await using var writer = new SqlConnection(connectionString);
        await writer.OpenAsync();
        await using var tx = (SqlTransaction)await writer.BeginTransactionAsync();
        await using var cmd = new SqlCommand("UPDATE dbo.Alpha SET Value=11 WHERE Id=1", writer, tx);
        await cmd.ExecuteNonQueryAsync();
        var during = await Poll(before.Version);
        Assert(during.Version == before.Version && during.Tables.Length == 0, "An uncommitted write escaped the snapshot.");
        await tx.RollbackAsync();
        Assert((await Poll(before.Version)).Tables.Length == 0, "Rollback generated a false change.");
    });

    await Check("commit during enumeration is deferred without losing its checkpoint", async () =>
    {
        await Exec("UPDATE dbo.Alpha SET Value=Value+1 WHERE Id=1");
        var before = await Poll(null);
        await using var writer = new SqlConnection(connectionString);
        await writer.OpenAsync();
        await using var tx = (SqlTransaction)await writer.BeginTransactionAsync();
        await using (var cmd = new SqlCommand("UPDATE dbo.Beta SET Value=Value+1 WHERE Id=1", writer, tx))
            await cmd.ExecuteNonQueryAsync();
        bool committed = false;
        // DiagnosticListener observes the real command boundary; no production test hook is needed.
        // Commit on connection A after connection B captured its version, before B enumerates changes.
        using var observer = new CommandObserver(command =>
        {
            if (committed || !command.CommandText.Contains("CHANGETABLE(CHANGES", StringComparison.Ordinal)) return;
            tx.Commit();
            committed = true;
        });
        // Make B enumerate changes even on an otherwise quiet database.
        var during = await Poll(before.Version - 1);
        Assert(committed, "The concurrent fixture never overlapped the actual change enumeration.");
        Assert(during.Version == before.Version, "A commit after the snapshot advanced the returned checkpoint.");
        Assert(during.Tables.SequenceEqual(new[] { "Alpha" }), "A change committed after the snapshot leaked into enumeration.");
        var after = await Poll(during.Version);
        Assert(after.Version > during.Version && after.Tables.SequenceEqual(new[] { "Beta" }), "The commit made during enumeration was skipped on the next poll.");
    });

    await Check("quiet polling avoids table roundtrips", async () =>
    {
        var before = await Poll(null);
        using var observer = new CommandObserver(_ => { });
        await Poll(before.Version);
        Assert(observer.Count is > 0 and <= 3, "Quiet polling should validate configuration/metadata/checkpoint without a query for each table; actual=" + observer.Count);
    });

    await Check("changed tables are queried in one SQL batch", async () =>
    {
        var before = await Poll(null);
        await Exec("UPDATE dbo.Alpha SET Value=Value+1 WHERE Id=1; UPDATE dbo.Beta SET Value=Value+1 WHERE Id=1;");
        using var observer = new CommandObserver(_ => { });
        var after = await Poll(before.Version);
        Assert(after.Tables.Order().SequenceEqual(new[] { "Alpha", "Beta" }), "The batched result must include every affected table.");
        Assert(observer.Count is > 0 and <= 4, "Change enumeration should use one SQL batch regardless of table count; actual=" + observer.Count);
    });

    foreach (var (operation, sql) in new[]
    {
        ("insert", "INSERT dbo.Alpha VALUES(2,12)"),
        ("update", "UPDATE dbo.Alpha SET Value=13 WHERE Id=2"),
        ("delete", "DELETE dbo.Alpha WHERE Id=2")
    })
    {
        await Check("committed " + operation + " reports only affected table", async () =>
        {
            var before = await Poll(null);
            await Exec(sql);
            var after = await Poll(before.Version);
            Assert(!after.FullRefresh && after.Version > before.Version && after.Tables.SequenceEqual(new[] { "Alpha" }), operation + " was missed or over-reported.");
        });
    }

    await Check("invalid and expired checkpoints request full refresh", async () =>
    {
        var current = await Poll(null);
        Assert((await Poll(current.Version + 100)).FullRefresh, "Future checkpoint must reset after restore/replace.");
        Assert((await Poll(-1)).FullRefresh, "Negative checkpoint must reset.");
        // Advance the committed generation before resetting CT; the saved version is strictly below min_valid.
        await Exec("UPDATE dbo.Alpha SET Value=Value+1 WHERE Id=1; TRUNCATE TABLE dbo.Beta");
        var expired = await Poll(current.Version);
        Assert(expired.FullRefresh && expired.Tables.Order().SequenceEqual(new[] { "Alpha", "Beta" }), "Checkpoint below minimum valid version must reset all tables.");
    });

    await Check("missing table tracking fails visibly", async () =>
    {
        await Exec("ALTER TABLE dbo.Beta DISABLE CHANGE_TRACKING");
        await ExpectConfigurationError("Beta");
        await Install();
    });

    await Check("untracked table with no primary key fails visibly", async () =>
    {
        await Exec("CREATE TABLE dbo.WithoutKey(Value int)");
        try
        {
            await ExpectConfigurationError("WithoutKey");
            bool rejected = false;
            try { await Install(); }
            catch (SqlException e) when (e.Number == 56021 && e.Message.Contains("WithoutKey", StringComparison.Ordinal)) { rejected = true; }
            Assert(rejected, "Installer must name the table missing a primary key.");
        }
        finally { await Exec("DROP TABLE dbo.WithoutKey"); }
    });

    await Check("missing database tracking fails visibly", async () =>
    {
        await Exec($"ALTER TABLE dbo.Alpha DISABLE CHANGE_TRACKING; ALTER TABLE dbo.Beta DISABLE CHANGE_TRACKING; ALTER DATABASE [{database}] SET CHANGE_TRACKING=OFF");
        await ExpectConfigurationError("Change Tracking");
        await Install();
    });

    await Check("the emitted SQL rejects partially visible metadata", async () =>
    {
        string? configurationSql = null;
        using (var observer = new CommandObserver(command =>
        {
            if (command.CommandText.Contains("snapshot_isolation_state=1", StringComparison.Ordinal)) configurationSql = command.CommandText;
        })) await Poll(null);
        Assert(configurationSql != null, "Reader did not emit its metadata visibility validation.");
        await Exec("CREATE USER SyncLimited WITHOUT LOGIN; GRANT SELECT ON dbo.Alpha TO SyncLimited; GRANT VIEW CHANGE TRACKING ON dbo.Alpha TO SyncLimited;");
        await using var db = new SqlConnection(connectionString);
        await db.OpenAsync();
        await using (var impersonate = new SqlCommand("EXECUTE AS USER='SyncLimited'", db)) await impersonate.ExecuteNonQueryAsync();
        bool denied = false;
        try
        {
            await using var check = new SqlCommand(configurationSql, db);
            await check.ExecuteNonQueryAsync();
        }
        catch (SqlException e) when (e.Number == 56011 && e.Message.Contains("VIEW DEFINITION", StringComparison.Ordinal)) { denied = true; }
        finally
        {
            await using var revert = new SqlCommand("REVERT; DROP USER SyncLimited", db);
            await revert.ExecuteNonQueryAsync();
        }
        Assert(denied, "A SQL user who can only see Alpha must not get a healthy incomplete inventory.");
    });

    await Check("polling never reads the UI principal", async () =>
    {
        var previous = Thread.CurrentPrincipal;
        try
        {
            Thread.CurrentPrincipal = new UnavailableUiPrincipal();
            Assert((await Poll(null)).FullRefresh, "Reader must work with an unavailable UI principal.");
        }
        finally { Thread.CurrentPrincipal = previous; }
    });

    await Check("cancellation stops a poll before opening SQL", async () =>
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        try { await Poll(null, cancellationToken: cts.Token); throw new InvalidOperationException("Cancellation was ignored."); }
        catch (OperationCanceledException) { }
    });

    await Check("replacing the database changes identity even at the same version", async () =>
    {
        var before = await Poll(null);
        await Exec($"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; CREATE DATABASE [{database}];", masterConnection);
        await Exec("CREATE TABLE dbo.Alpha(Id int NOT NULL PRIMARY KEY,Value int NOT NULL); CREATE TABLE dbo.Beta(Id int NOT NULL PRIMARY KEY,Value int NOT NULL); INSERT dbo.Alpha VALUES(1,10)");
        await Install();
        await Exec($"WHILE CHANGE_TRACKING_CURRENT_VERSION()<{before.Version} UPDATE dbo.Alpha SET Value=Value+1 WHERE Id=1;");
        var replacement = await Poll(before.Version);
        Assert(replacement.Version == before.Version, "Replacement fixture should rule out simple backwards-version detection.");
        Assert(replacement.Identity != before.Identity, "Replacing the same named database must invalidate its observed identity.");
    });

    if (args.Contains("--full-schema", StringComparer.Ordinal))
    {
        await Check("main SQL installs 74 tracked tables and rerunning tracking preserves every business value", async () =>
        {
            await Exec($"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; CREATE DATABASE [{database}];", masterConnection);
            // Replace every destination reference before SQL executes; the original business database is never selected.
            string source = (await File.ReadAllTextAsync(sqlFile)).Replace("QLBenhVienRangHamMat", database, StringComparison.OrdinalIgnoreCase);
            Assert(!source.Contains("QLBenhVienRangHamMat", StringComparison.OrdinalIgnoreCase), "Unsafe unreplaced business database reference.");
            await using var db = new SqlConnection(masterConnection);
            await db.OpenAsync();
            int batchNumber = 0;
            foreach (string batch in Regex.Split(source, @"^\s*GO\s*\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                batchNumber++;
                await using var command = new SqlCommand(batch, db) { CommandTimeout = 120 };
                try { await command.ExecuteNonQueryAsync(); }
                catch (SqlException e) { throw new InvalidOperationException($"Full schema SQL batch {batchNumber}: {e.Message}", e); }
            }
            Assert(await Scalar<int>("SELECT COUNT(*) FROM sys.change_tracking_tables") == 74, "Main schema must track all 74 business tables.");
            var all = await Poll(null);
            Assert(all.FullRefresh && all.Tables.Length == 74, "Reader omitted tables from the installed main schema.");
            string before = await BusinessFingerprint();
            await Install();
            string after = await BusinessFingerprint();
            Assert(before == after, "Rerunning the tracking installer changed business values.");
            Assert((await Poll(all.Version)).Tables.Length == 0, "Rerunning tracking emitted business writes.");
            Console.WriteLine("FULL SCHEMA BUSINESS SHA256 " + after);
        });
    }

    Console.WriteLine($"SQL sync checks passed: {passed}.");
}
finally
{
    await Exec($"IF DB_ID(N'{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END", masterConnection);
}

internal sealed record Batch(long Version, string Identity, bool FullRefresh, string[] Tables);

internal sealed class UnavailableUiPrincipal : System.Security.Principal.IPrincipal
{
    public System.Security.Principal.IIdentity Identity => throw new InvalidOperationException("Background polling accessed the UI principal.");
    public bool IsInRole(string role) => throw new InvalidOperationException("Background polling accessed UI roles.");
}

internal sealed class CommandObserver : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private readonly Action<SqlCommand> onCommand;
    private readonly List<IDisposable> subscriptions = new();
    private readonly IDisposable allListeners;
    public int Count { get; private set; }
    public CommandObserver(Action<SqlCommand> onCommand)
    {
        this.onCommand = onCommand;
        allListeners = DiagnosticListener.AllListeners.Subscribe(this);
    }
    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name == "SqlClientDiagnosticListener") subscriptions.Add(listener.Subscribe(this));
    }
    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (!value.Key.EndsWith("WriteCommandBefore", StringComparison.Ordinal)) return;
        if (value.Value?.GetType().GetProperty("Command")?.GetValue(value.Value) is not SqlCommand command) return;
        Count++;
        onCommand(command);
    }
    public void OnCompleted() { }
    public void OnError(Exception error) => throw error;
    public void Dispose()
    {
        allListeners.Dispose();
        foreach (var subscription in subscriptions) subscription.Dispose();
    }
}
