using System.Data;
using System.Security.Claims;
using System.Text.RegularExpressions;
using BUS;
using DAL;
using Dapper;
using ET;
using Microsoft.Data.SqlClient;

internal static class LegacyPayrollChecks
{
    internal static async Task Run(string connection)
    {
        await using var db = new SqlConnection(connection); await db.OpenAsync();
        await db.ExecuteAsync("""
            CREATE TABLE dbo.CHINH_SACH_LUONG(MaCSL nvarchar(50) PRIMARY KEY,MaChucVu nvarchar(50),TenChinhSach nvarchar(100),PhienBan int,
              NgayHieuLuc date,NgayHetHieuLuc date,LuongCoBan decimal(18,2),SoCaDinhMuc int,DonGiaCa decimal(18,2),CoSoHoaHong nvarchar(200),
              TyLeHoaHong decimal(9,4),PhuCapCoDinh decimal(18,2),TrangThai nvarchar(50),MaNVDuyet nvarchar(50),NgayDuyet datetime2,
              TyLeBHXH decimal(9,4),TyLeBHYT decimal(9,4),TyLeBHTN decimal(9,4),HeSoTangCaThuong decimal(9,4),HeSoTangCaChuNhat decimal(9,4),HeSoTangCaNgayLe decimal(9,4));
            CREATE TABLE dbo.BANG_LUONG(MaBL nvarchar(50) PRIMARY KEY,MaNV nvarchar(50),MaCSL nvarchar(50),TrangThai nvarchar(50),TuNgay date,DenNgay date,TongLuong decimal(18,2));
            CREATE TABLE dbo.CA_LAM(MaCa nvarchar(50) PRIMARY KEY,GioBatDau time,GioKetThuc time);
            CREATE TABLE dbo.LICH_LAM_VIEC(MaLLV nvarchar(50) PRIMARY KEY,MaNV nvarchar(50),MaCa nvarchar(50),NgayLam date,TrangThai nvarchar(50),GioVao datetime2,GioRa datetime2,TrangThaiHienDien nvarchar(50));
            CREATE TABLE dbo.CHAM_CONG(MaCC nvarchar(50) PRIMARY KEY,MaLLV nvarchar(50),TrangThai nvarchar(50),CongDuocTinh decimal(4,2),SoPhutDiTre int,MaNVDoiSoat nvarchar(50),NgayDoiSoat datetime2,GhiChu nvarchar(500));
            CREATE TABLE dbo.DON_XIN_NGHI(MaDonNghi nvarchar(50) PRIMARY KEY,MaNV nvarchar(50),TrangThaiDuyet nvarchar(50),TuNgay date,DenNgay date);
            CREATE TABLE dbo.NHAT_KY_DUYET_CHAM_CONG(Id int IDENTITY PRIMARY KEY,MaLLV nvarchar(50),MaNV nvarchar(50),NguoiThucHien nvarchar(50),NgayThucHien datetime2,
              TrangThaiCu nvarchar(50),TrangThaiMoi nvarchar(50),GioVaoCu datetime2,GioRaCu datetime2,GioVaoMoi datetime2,GioRaMoi datetime2,CongCu decimal(4,2),CongMoi decimal(4,2),LyDo nvarchar(500));
            INSERT dbo.BANG_LUONG VALUES(N'BL_CHECK',N'NV1',NULL,N'Nháp','20260101','20260131',0);
            INSERT dbo.CA_LAM VALUES(N'CA_CHECK','00:00','23:59:59.9999999');
            INSERT dbo.LICH_LAM_VIEC VALUES(N'LLV_CHECK',N'NV1',N'CA_CHECK',CONVERT(date,SYSDATETIME()),N'Đã xác nhận',NULL,NULL,N'Vắng');
            """);
        // Lightweight procedures exercise the real DAL dispatch/transaction/context, not payroll formulas.
        foreach (string sql in new[]
        {
            "CREATE PROCEDURE dbo.RHM_TinhLuongKy @TuNgay date,@DenNgay date,@MaNVTinh nvarchar(50) AS UPDATE dbo.BANG_LUONG SET TongLuong=TongLuong+1 WHERE TuNgay=@TuNgay AND DenNgay=@DenNgay;",
            "CREATE PROCEDURE dbo.RHM_ChotLuongNhanVien @MaBL nvarchar(50),@MaNVChot nvarchar(50),@CongDaXem decimal(7,2),@TongDaXem decimal(18,2),@HoaHongDaXem decimal(18,2) AS UPDATE dbo.BANG_LUONG SET TrangThai=N'Đã chốt' WHERE MaBL=@MaBL;",
            "CREATE PROCEDURE dbo.RHM_DuyetChinhSachLuong @MaCSL nvarchar(50),@MaNVDuyet nvarchar(50) AS UPDATE dbo.CHINH_SACH_LUONG SET TrangThai=N'Đã duyệt',MaNVDuyet=@MaNVDuyet,NgayDuyet=SYSDATETIME() WHERE MaCSL=@MaCSL;"
        }) await db.ExecuteAsync(sql);

        // Install the actual audit-trigger generator from the production SQL script against this isolated schema.
        string file = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Database/QLBenhVienRangHamMat.sql"));
        string source = await File.ReadAllTextAsync(file);
        int start = source.IndexOf("-- Audit các phân hệ không đi qua editor chung.", StringComparison.Ordinal);
        int end = source.IndexOf("-- END SPRINT 3 THANH TAN", start, StringComparison.Ordinal);
        if (start < 0 || end < start) throw new InvalidOperationException("Cannot locate production audit-trigger installer.");
        await db.ExecuteAsync("EXEC sys.sp_set_session_context @key=N'RHM.AuditSchema.Completed',@value=1");
        foreach (string batch in Regex.Split(source[start..end], @"^\s*GO\s*\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            if (!string.IsNullOrWhiteSpace(batch)) await db.ExecuteAsync(batch);

        ClaimsPrincipal Principal(Guid? session = null)
        {
            List<Claim> claims = [new(ClaimTypes.NameIdentifier,"TK1"),new(DinhDanhNghiepVu.MaNhanVienClaim,"NV1")];
            if (session.HasValue) claims.Add(new("RHM.MaPhien",session.Value.ToString("D")));
            return new(new ClaimsIdentity(claims,DinhDanhNghiepVu.KieuXacThuc));
        }
        var actor = new NguCanhNghiepVu { MaTK="TK1",MaNV="NV1" };
        Thread.CurrentPrincipal=Principal();
        Guid old = new BUS_NHATKY().BatDauPhien(actor);
        Thread.CurrentPrincipal=Principal(session:old);
        var policy = new ET_CHINHSACHLUONG { MaChucVu="CV1",TenChinhSach="Session fixture",NgayHieuLuc=new(2026,1,1),LuongCoBan=1000 };
        var policies = new DAL_CHINHSACHLUONG();
        var attendance = new DAL_CHAMCONG();
        var salary = new DAL_BANGLUONG();
        var operations = new (string Name,Action Write)[]
        {
            ("luuChinhSach",()=>policies.luuChinhSach("NV1",policy)),
            ("chamCong",()=>attendance.chamCong("LLV_CHECK","TK1")),
            ("tinhLuongTheoKy",()=>salary.tinhLuongTheoKy(new(2026,1,1),new(2026,1,31),"NV1")),
            ("chotLuongNhanVien",()=>salary.chotLuongNhanVien("BL_CHECK","NV1",0,1,0)),
            ("duyetChinhSach",()=>policies.duyetChinhSach(policy.MaCSL,"NV1"))
        };
        // No UI or watchdog executes here: use the stale real principal immediately after the replacement commits.
        Thread.CurrentPrincipal=Principal(); Guid latest=new BUS_NHATKY().BatDauPhien(actor);
        Thread.CurrentPrincipal=Principal(session:old);
        int before=await db.ExecuteScalarAsync<int>("SELECT (SELECT COUNT(*) FROM dbo.CHINH_SACH_LUONG)+(SELECT COUNT(*) FROM dbo.CHAM_CONG)+CONVERT(int,SUM(TongLuong)) FROM dbo.BANG_LUONG");
        foreach (var operation in operations)
        {
            bool denied=false;
            try { operation.Write(); }
            catch(UnauthorizedAccessException e) when(e.Message==ThongBaoPhien.DangNhapMayKhac) { denied=true; }
            if(!denied)throw new InvalidOperationException("Revoked legacy write was accepted: "+operation.Name);
            Console.WriteLine("PASS revoked principal cannot execute legacy "+operation.Name+" before a watchdog runs");
        }
        int after=await db.ExecuteScalarAsync<int>("SELECT (SELECT COUNT(*) FROM dbo.CHINH_SACH_LUONG)+(SELECT COUNT(*) FROM dbo.CHAM_CONG)+CONVERT(int,SUM(TongLuong)) FROM dbo.BANG_LUONG");
        if(before!=after)throw new InvalidOperationException("A revoked payroll/attendance/policy call mutated business data.");
        Thread.CurrentPrincipal=Principal(session:latest);
        foreach(var operation in operations)operation.Write();
        foreach(string table in new[]{"CHINH_SACH_LUONG","CHAM_CONG","BANG_LUONG"})
            if(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE MaPhien=@latest AND LoaiDoiTuong=@table",new{latest,table})==0)
                throw new InvalidOperationException("The production audit trigger did not receive the real session: "+table);
        Console.WriteLine("PASS all five legacy writes succeed under the current session and production SQL triggers audit policy, attendance and salary");
        Thread.CurrentPrincipal=Principal();
        salary.tinhLuongTheoKy(new(2026,1,1),new(2026,1,31),"NV1");
        Console.WriteLine("PASS legacy callers without a session claim retain their internal compatibility");

        await db.ExecuteAsync("""
            ALTER PROCEDURE dbo.RHM_TinhLuongKy @TuNgay date,@DenNgay date,@MaNVTinh nvarchar(50) AS
            BEGIN
              UPDATE dbo.BANG_LUONG SET TongLuong=TongLuong+1 WHERE TuNgay=@TuNgay AND DenNgay=@DenNgay;
              WAITFOR DELAY '00:00:01.200';
            END
            """);
        decimal totalBefore=await db.ExecuteScalarAsync<decimal>("SELECT TongLuong FROM dbo.BANG_LUONG WHERE MaBL=N'BL_CHECK'");
        var inFlight=Task.Run(() =>
        {
            Thread.CurrentPrincipal=Principal(session:latest);
            salary.tinhLuongTheoKy(new(2026,1,1),new(2026,1,31),"NV1");
        });
        bool entered=false;
        for(int poll=0;poll<100&&!entered;poll++)
        {
            entered=await db.ExecuteScalarAsync<decimal>("SELECT TongLuong FROM dbo.BANG_LUONG WITH(READUNCOMMITTED) WHERE MaBL=N'BL_CHECK'")>totalBefore;
            if(!entered)await Task.Delay(10);
        }
        if(!entered)throw new InvalidOperationException("The in-flight payroll command did not enter its transaction.");
        var replacement=Task.Run(() =>
        {
            Thread.CurrentPrincipal=Principal();
            return new BUS_NHATKY().BatDauPhien(actor);
        });
        await Task.Delay(100);
        if(replacement.IsCompleted||inFlight.IsCompleted)throw new InvalidOperationException("The session guard did not hold through the in-flight payroll transaction.");
        await inFlight;
        await replacement;
        Console.WriteLine("PASS replacement login waits until the guarded payroll transaction commits");

        // Also exercise database enforcement directly, independently of the DAL guard.
        await db.ExecuteAsync("EXEC sys.sp_set_session_context @key=N'RHM.MaPhien',@value=@old",new{old=old.ToString("D")});
        try
        {
            foreach(string table in new[]{"CHINH_SACH_LUONG","CHAM_CONG","BANG_LUONG"})
            {
                bool denied=false;
                try { await db.ExecuteAsync($"UPDATE dbo.{table} SET TrangThai=TrangThai"); }
                catch(SqlException e) when(e.Number==53022) { denied=true; }
                if(!denied)throw new InvalidOperationException("Production SQL trigger accepted revoked context: "+table);
            }
        }
        finally { await db.ExecuteAsync("EXEC sys.sp_set_session_context @key=N'RHM.MaPhien',@value=NULL"); }
        Console.WriteLine("PASS production SQL triggers reject revoked session context on policy, attendance and salary writes");
    }
}
