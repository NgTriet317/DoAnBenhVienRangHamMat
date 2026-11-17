using System.Security.Claims;
using System.Text.RegularExpressions;
using BUS;
using Dapper;
using ET;
using Microsoft.Data.SqlClient;
using RangHamMat;
using RangHamMat.Models;
using RangHamMat.Repositories;

if(args.Length!=2 || !Regex.IsMatch(args[1],"^RHM_Hospital_Test_[A-Za-z0-9_]+$"))
    throw new ArgumentException("Only an isolated RHM_Hospital_Test_ database is allowed.");
var cs=new SqlConnectionStringBuilder { DataSource=args[0],InitialCatalog=args[1],IntegratedSecurity=true,
    TrustServerCertificate=true,Encrypt=SqlConnectionEncryptOption.Optional }.ConnectionString;
Environment.SetEnvironmentVariable("RHM_CONNECTION_STRING",cs);
await using var db=new SqlConnection(cs); await db.OpenAsync();
await db.ExecuteAsync("""
    SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET ANSI_WARNINGS ON; SET ANSI_PADDING ON;
    SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF;
    EXEC sys.sp_set_session_context @key=N'RHM.MaPhien',@value=NULL;
    IF EXISTS(SELECT 1 FROM dbo.NHAN_VIEN WHERE MaNV=N'MERGE_AI_DOCTOR')
        THROW 55990,N'Use a fresh AI test database.',1;
    INSERT dbo.NHAN_VIEN(MaNV,HoTen,MaChucVu,MaCK,NgayVaoLam,TrangThai)
    VALUES(N'MERGE_AI_DOCTOR',N'FIXTURE bác sĩ AI',N'CV002',N'CK001','20200101',N'Đang làm việc');
    INSERT dbo.TAI_KHOAN(MaTK,TenDangNhap,MatKhauHash,MaVaiTro,MaNV,TrangThai)
    VALUES(N'MERGE_AI_ACCOUNT',N'merge_ai_doctor',N'fixture',N'VTRO002',N'MERGE_AI_DOCTOR',N'Đang hoạt động');
    INSERT dbo.PHONG(MaPhong,MaKhoa,TenPhong,LoaiPhong,TinhTrang,TrangThai)
    VALUES(N'AI_RETRY_A',N'KHOA001',N'FIXTURE phòng AI A',N'Phòng khám chuyên khoa',N'Đang sử dụng',N'Đang hoạt động');
    DECLARE @start datetime2(0)=DATEADD(hour,-2,SYSDATETIME()),@end datetime2(0)=DATEADD(hour,2,SYSDATETIME());
    INSERT dbo.CA_LAM(MaCa,GioBatDau,GioKetThuc,TrangThai)
    VALUES(N'MERGE_AI_CA',CONVERT(time(0),@start),CONVERT(time(0),@end),N'Đang hoạt động');
    INSERT dbo.LICH_LAM_VIEC(MaLLV,MaNV,MaCa,NgayLam,MaPhong,TrangThai,TrangThaiHienDien)
    VALUES(N'MERGE_AI_SHIFT',N'MERGE_AI_DOCTOR',N'MERGE_AI_CA',CONVERT(date,@start),N'AI_RETRY_A',N'Đã xác nhận',N'Có mặt');
    """);
ClaimsPrincipal Principal(Guid? session=null)
{
    var claims=new List<Claim> { new(ClaimTypes.NameIdentifier,"TK001"),new(DinhDanhNghiepVu.MaNhanVienClaim,"NV001") };
    if(session.HasValue) claims.Add(new("RHM.MaPhien",session.Value.ToString("D")));
    return new(new ClaimsIdentity(claims,DinhDanhNghiepVu.KieuXacThuc));
}
void Check(bool valid,string message)
{ if(!valid)throw new InvalidOperationException(message); Console.WriteLine("PASS "+message); }
Thread.CurrentPrincipal=Principal();
Guid session=new BUS_NHATKY().BatDauPhien(new(){MaTK="TK001",MaNV="NV001"});
Thread.CurrentPrincipal=Principal(session);
PhienDangNhap.MaNV="NV001";
var repo=new SqlHospitalRepository();
var request=new SlipRequest("Nguyễn Văn Kiểm Thử","09"+Random.Shared.Next(10000000,99999999),
    DepartmentGroup.KHAM_TONG_QUAT,false,"Khám tổng quát",Guid.NewGuid(),new(2000,1,2),true);
var first=await repo.CreateGuidanceSlipAsync(request,CancellationToken.None);
Check(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@Code AND MaPhong=@AssignedRoomCode AND SoThuTu=@QueueNumber",first)==1,
    "First receipt and displayed room match SQL");
// No current doctor remains eligible, but an already committed receipt must still be retrievable.
await db.ExecuteAsync("UPDATE dbo.LICH_LAM_VIEC SET TrangThai=N'Hủy' WHERE NgayLam BETWEEN DATEADD(day,-1,CONVERT(date,SYSDATETIME())) AND CONVERT(date,SYSDATETIME())");
var retry=await repo.CreateGuidanceSlipAsync(request,CancellationToken.None);
Check(retry.Code==first.Code && retry.QueueNumber==first.QueueNumber && retry.AssignedRoomCode==first.AssignedRoomCode,
    "Retry returns the persisted receipt and room without requiring another doctor shift");
Check(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.AI_KET_QUA_TAO_PHIEU WHERE MaPhien=@SessionId",request)==1,
    "Retry creates no duplicate receipt");
bool changedDenied=false;
try { await repo.CreateGuidanceSlipAsync(request with { PatientName="Trần Thị Khác" },CancellationToken.None); }
catch(InvalidOperationException) { changedDenied=true; }
Check(changedDenied,"Retry with changed patient information is rejected");
Thread.CurrentPrincipal=Principal();
new BUS_NHATKY().BatDauPhien(new(){MaTK="TK001",MaNV="NV001"});
Thread.CurrentPrincipal=Principal(session);
bool revokedDenied=false;
try { await repo.CreateGuidanceSlipAsync(request,CancellationToken.None); }
catch(UnauthorizedAccessException) { revokedDenied=true; }
Check(revokedDenied,"Revoked session cannot retrieve or create an AI receipt");
Console.WriteLine("RESULT: 5 passed; 0 failed");
