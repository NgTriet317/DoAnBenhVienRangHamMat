using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using BUS;
using DAL;
using Dapper;
using ET;
using Microsoft.Data.SqlClient;

// Every write is restricted to a fresh, randomly named test database.
var database = "RHM_Hospital_Test_SingleSession_" + Guid.NewGuid().ToString("N");
if (!Regex.IsMatch(database, "^RHM_Hospital_Test_[A-Za-z0-9_]+$")) throw new InvalidOperationException();
var cs = new SqlConnectionStringBuilder
{
    DataSource = args.FirstOrDefault() ?? "lpc:.", InitialCatalog = "master", IntegratedSecurity = true,
    TrustServerCertificate = true, Encrypt = SqlConnectionEncryptOption.Optional, Pooling = false, ConnectTimeout = 10
};
string master = cs.ConnectionString;
cs.InitialCatalog = database;
string connection = cs.ConnectionString;
Environment.SetEnvironmentVariable("RHM_CONNECTION_STRING", connection);
const string replaced = "tài khoản đã được đăng nhập vào máy khác";
var actor = new NguCanhNghiepVu { MaTK = "TK1", MaNV = "NV1" };
int passed = 0;

ClaimsPrincipal Principal(string account = "TK1", string employee = "NV1", Guid? session = null)
{
    var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, account), new(DinhDanhNghiepVu.MaNhanVienClaim, employee) };
    if (session is not null) claims.Add(new("RHM.MaPhien", session.Value.ToString("D")));
    return new ClaimsPrincipal(new ClaimsIdentity(claims, DinhDanhNghiepVu.KieuXacThuc));
}
void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
async Task Exec(string sql, object? parameters = null, string? target = null)
{
    await using var db = new SqlConnection(target ?? connection); await db.OpenAsync();
    await db.ExecuteAsync(sql, parameters);
}
async Task<T> Scalar<T>(string sql, object? parameters = null)
{
    await using var db = new SqlConnection(connection); await db.OpenAsync();
    return (await db.ExecuteScalarAsync<T>(sql, parameters))!;
}
async Task Check(string name, Func<Task> action)
{
    await action(); passed++; Console.WriteLine("PASS " + name);
}
void Denied(Action action, string? message = null)
{
    try { action(); } catch (UnauthorizedAccessException e) when (message is null || e.Message == message) { return; }
    throw new InvalidOperationException("An invalid session was accepted, or its rejection message was incorrect.");
}
async Task<string> Status(Guid session, string account = "TK1", CancellationToken cancellationToken = default)
{
    var method = typeof(BUS_NHATKY).GetMethod("KiemTraPhienHienTaiAsync", [typeof(string), typeof(Guid), typeof(string), typeof(CancellationToken)]);
    Require(method is not null, "Missing read-only session watcher API.");
    var task = (Task)method!.Invoke(new BUS_NHATKY(), [connection, session, account, cancellationToken])!;
    await task;
    return task.GetType().GetProperty("Result")!.GetValue(task)!.ToString()!;
}
Guid Restore(Guid session)
{
    var method = typeof(BUS_NHATKY).GetMethod("KhoiPhucPhien", [typeof(Guid), typeof(NguCanhNghiepVu)]);
    Require(method is not null, "Missing atomic remembered-session restore API.");
    try { return (Guid)method!.Invoke(new BUS_NHATKY(), [session, actor])!; }
    catch (TargetInvocationException e) when (e.InnerException is not null)
    { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; }
}

try
{
    await Exec($"CREATE DATABASE [{database}]", target: master);
    await Exec("""
        CREATE TABLE dbo.CHUC_VU(MaChucVu nvarchar(50) PRIMARY KEY);
        CREATE TABLE dbo.VAI_TRO(MaVaiTro nvarchar(50) PRIMARY KEY,TenVaiTro nvarchar(50));
        CREATE TABLE dbo.NHAN_VIEN(MaNV nvarchar(50) PRIMARY KEY,MaChucVu nvarchar(50),MaCK nvarchar(50),TrangThai nvarchar(50));
        CREATE TABLE dbo.TAI_KHOAN(MaTK nvarchar(50) PRIMARY KEY,MaNV nvarchar(50),MaVaiTro nvarchar(50),TrangThai nvarchar(50));
        CREATE TABLE dbo.PHIEN_DANG_NHAP(MaPhien uniqueidentifier PRIMARY KEY,MaTK nvarchar(50) NOT NULL REFERENCES dbo.TAI_KHOAN(MaTK),
          DangNhapUtc datetime2(0) NOT NULL,DangXuatUtc datetime2(0),HoatDongCuoiUtc datetime2(0) NOT NULL,
          TrangThaiPhien nvarchar(50) NOT NULL,LyDoKetThuc nvarchar(255),PhienKeTiep uniqueidentifier NULL);
        CREATE INDEX IX_Rhm_Phien_Account ON dbo.PHIEN_DANG_NHAP(MaTK) INCLUDE(PhienKeTiep,TrangThaiPhien);
        CREATE UNIQUE INDEX UX_Rhm_SingleActiveSession ON dbo.PHIEN_DANG_NHAP(MaTK) WHERE TrangThaiPhien=N'Đang mở';
        CREATE TABLE dbo.NHAT_KY_THAO_TAC(MaNK nvarchar(50) PRIMARY KEY,MaTK nvarchar(50),ThoiGian datetime2,
          ThoiGianUtc datetime2,MaPhien uniqueidentifier,MaSuKien uniqueidentifier,PhanHe nvarchar(100),HanhDong nvarchar(100),
          LoaiDoiTuong nvarchar(100),MaDoiTuong nvarchar(50),KetQua nvarchar(50),DuLieuTruoc nvarchar(max),DuLieuSau nvarchar(max));
        INSERT dbo.CHUC_VU VALUES(N'CV1'); INSERT dbo.VAI_TRO VALUES(N'VT1',N'Admin');
        INSERT dbo.NHAN_VIEN VALUES(N'NV1',N'CV1',NULL,N'Đang làm việc'),(N'NV2',N'CV1',NULL,N'Đang làm việc');
        INSERT dbo.TAI_KHOAN VALUES(N'TK1',N'NV1',N'VT1',N'Đang hoạt động'),(N'TK2',N'NV2',N'VT1',N'Đang hoạt động');
        """);
    Thread.CurrentPrincipal = Principal();
    var service = new BUS_NHATKY();
    Guid first = service.BatDauPhien(actor), second = Guid.Empty;
    await Check("a later login atomically revokes the prior machine with the exact message", async () =>
    {
        second = service.BatDauPhien(actor);
        Require(await Scalar<int>("SELECT COUNT(*) FROM dbo.PHIEN_DANG_NHAP WHERE MaTK=N'TK1' AND TrangThaiPhien=N'Đang mở'") == 1, "Two machines kept active sessions.");
        Require(await Scalar<string>("SELECT LyDoKetThuc FROM dbo.PHIEN_DANG_NHAP WHERE MaPhien=@first", new { first }) == replaced, "The replaced session has the wrong reason.");
        Require(await Scalar<int>("SELECT COUNT(*) FROM dbo.PHIEN_DANG_NHAP WHERE MaPhien=@first AND DangXuatUtc IS NOT NULL", new { first }) == 1, "Replacement must use a SQL end timestamp.");
    });
    await Check("the revoked principal can read its status without heartbeat or normal connection guards", async () =>
    {
        Thread.CurrentPrincipal = Principal(session: first);
        Require(await Status(first) == "DaDangNhapMayKhac", "The watcher did not identify replacement.");
        Require(await Scalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE MaPhien=@first AND HanhDong=N'LOGOUT'", new { first }) == 1, "Replacement did not emit exactly one logout event.");
        Require(await Scalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE HanhDong=N'LOGIN_SUCCESS'") == 2, "Missing login audit.");
    });
    await Check("revoked heartbeat and business access are denied even with an existing transaction", async () =>
    {
        Thread.CurrentPrincipal = Principal(session: first);
        Denied(() => service.Heartbeat(first, actor), replaced);
        await using var db = new SqlConnection(connection); await db.OpenAsync();
        using var tx = db.BeginTransaction();
        Denied(() => new DAL_QUYENNGHIEPVU(tx).LayNguCanh("NV1"), replaced);
        tx.Rollback();
        Thread.CurrentPrincipal = Principal(session: second);
        Denied(() => service.Heartbeat(first, actor));
    });
    await Check("logout from an old machine cannot close the replacement session", async () =>
    {
        Thread.CurrentPrincipal = Principal(session: first);
        try { service.KetThucPhien(first); } catch (UnauthorizedAccessException) { }
        Require(await Scalar<int>("SELECT COUNT(*) FROM dbo.PHIEN_DANG_NHAP WHERE MaPhien=@second AND TrangThaiPhien=N'Đang mở'", new { second }) == 1, "Old logout closed the latest machine.");
        Thread.CurrentPrincipal = Principal(session: second);
        Denied(() => service.KetThucPhien(first));
    });
    await Check("a different account keeps its independent active session", async () =>
    {
        Thread.CurrentPrincipal = Principal("TK2", "NV2");
        Guid other = service.BatDauPhien(new NguCanhNghiepVu { MaTK = "TK2", MaNV = "NV2" });
        Thread.CurrentPrincipal = Principal(); second = service.BatDauPhien(actor);
        Require(await Scalar<int>("SELECT COUNT(*) FROM dbo.PHIEN_DANG_NHAP WHERE MaPhien=@other AND TrangThaiPhien=N'Đang mở'", new { other }) == 1, "Login revoked a different account.");
    });
    await Check("different accounts can log in concurrently without revoking each other", async () =>
    {
        var accounts = new[] { actor, new NguCanhNghiepVu { MaTK = "TK2", MaNV = "NV2" } };
        var winners = await Task.WhenAll(accounts.Select(current => Task.Run(() =>
        {
            Thread.CurrentPrincipal = Principal(current.MaTK, current.MaNV);
            return new BUS_NHATKY().BatDauPhien(current);
        })));
        Require(await Scalar<int>("SELECT COUNT(*) FROM dbo.PHIEN_DANG_NHAP WHERE MaPhien IN @winners AND TrangThaiPhien=N'Đang mở'", new { winners }) == 2, "Independent account login was blocked or revoked.");
    });
    await Check("concurrent fresh logins leave exactly one winner and all loser heartbeats fail", async () =>
    {
        var sessions = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Task.Run(() =>
        { Thread.CurrentPrincipal = Principal(); return new BUS_NHATKY().BatDauPhien(actor); })));
        Require(await Scalar<int>("SELECT COUNT(*) FROM dbo.PHIEN_DANG_NHAP WHERE MaTK=N'TK1' AND TrangThaiPhien=N'Đang mở'") == 1, "Concurrent login created multiple winners.");
        second = await Scalar<Guid>("SELECT MaPhien FROM dbo.PHIEN_DANG_NHAP WHERE MaTK=N'TK1' AND TrangThaiPhien=N'Đang mở'");
        Require(sessions.Contains(second), "The winning session did not belong to the race.");
        foreach (Guid loser in sessions.Where(s => s != second))
        { Thread.CurrentPrincipal = Principal(session: loser); Denied(() => service.Heartbeat(loser, actor), replaced); }
        Thread.CurrentPrincipal = Principal(session: second); service.Heartbeat(second, actor);
    });
    await Check("watcher distinguishes expiry, missing and normal logout without writes", async () =>
    {
        Thread.CurrentPrincipal = Principal(session: second);
        await Exec("UPDATE dbo.PHIEN_DANG_NHAP SET HoatDongCuoiUtc=DATEADD(minute,-6,SYSUTCDATETIME()) WHERE MaPhien=@second", new { second });
        Require(await Status(second) == "HetHan", "Expired heartbeat was not detected.");
        int before = await Scalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC");
        Guid missing = Guid.NewGuid(); Thread.CurrentPrincipal = Principal(session: missing);
        Require(await Status(missing) == "KhongTonTai", "Missing session was not detected.");
        Require(await Scalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC") == before, "Read-only watcher wrote audit events.");
        Require(await Scalar<int>("SELECT COUNT(*) FROM dbo.PHIEN_DANG_NHAP WHERE MaPhien=@second AND HoatDongCuoiUtc<DATEADD(minute,-5,SYSUTCDATETIME())", new { second }) == 1, "Read-only watcher refreshed heartbeat.");
        Thread.CurrentPrincipal = Principal(); second = service.BatDauPhien(actor);
        Thread.CurrentPrincipal = Principal(session: second); service.KetThucPhien(second);
        Require(await Status(second) == "DaDangXuat", "Normal logout was not detected.");
        Require(await Scalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC") > before, "Expected lifecycle audit missing.");
    });
    await Check("remembered normal logout restores atomically and stale tokens never reclaim another machine", async () =>
    {
        Thread.CurrentPrincipal = Principal(); Guid remembered = second;
        second = Restore(remembered);
        Denied(() => Restore(remembered), replaced);
        Thread.CurrentPrincipal = Principal(session: second); service.KetThucPhien(second);
        Thread.CurrentPrincipal = Principal(); Guid closedLatest = second;
        Guid next = service.BatDauPhien(actor);
        Thread.CurrentPrincipal = Principal(session: next); service.KetThucPhien(next);
        Thread.CurrentPrincipal = Principal(); Denied(() => Restore(closedLatest), replaced);
        var race = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            Thread.CurrentPrincipal = Principal();
            try { return Restore(next); } catch (UnauthorizedAccessException) { return Guid.Empty; }
        })));
        Require(race.Count(s => s != Guid.Empty) == 1, "Two restores claimed the same remembered token.");
    });
    await Check("the watcher honors cancellation and refuses another account's token", async () =>
    {
        second = await Scalar<Guid>("SELECT MaPhien FROM dbo.PHIEN_DANG_NHAP WHERE MaTK=N'TK1' AND TrangThaiPhien=N'Đang mở'");
        Thread.CurrentPrincipal = Principal(session: second);
        try { await Status(second, "TK2"); throw new InvalidOperationException("The watcher accepted another account."); }
        catch (UnauthorizedAccessException) { }
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        try { await Status(second, cancellationToken: cancelled.Token); throw new InvalidOperationException("The watcher ignored cancellation."); }
        catch (OperationCanceledException) { }
    });
    await Check("legacy payroll, policy and attendance writes enforce the SQL session boundary", () => LegacyPayrollChecks.Run(connection));
    await Check("missing migration reports the exact script needed to update the schema", async () =>
    {
        await Exec("DROP INDEX IX_Rhm_Phien_Account ON dbo.PHIEN_DANG_NHAP; ALTER TABLE dbo.PHIEN_DANG_NHAP DROP COLUMN PhienKeTiep");
        Thread.CurrentPrincipal = Principal();
        try { service.BatDauPhien(actor); throw new InvalidOperationException("Missing schema was accepted."); }
        catch (InvalidOperationException e) when (e.Message.Contains("Database/QLBenhVienRangHamMat.sql", StringComparison.Ordinal)) { }
    });
    Console.WriteLine($"PASS {passed} SQL session checks");
}
finally
{
    Thread.CurrentPrincipal = null;
    await Exec($"IF DB_ID(N'{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END", target: master);
}
