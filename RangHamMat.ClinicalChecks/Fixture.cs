using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Security.Principal;
using BUS;
using ET;

namespace RangHamMat.ClinicalChecks;

internal sealed class Fixture : IDisposable
{
    public SqlConnection Connection { get; }
    public IDbTransaction Transaction { get; }
    public string Prefix { get; } = "CW" + Guid.NewGuid().ToString("N");
    private readonly IPrincipal? originalPrincipal = Thread.CurrentPrincipal;
    public string Id(string suffix) => Prefix + suffix;

    // Chỉ fixture giả lập ranh giới đăng nhập đã xác thực; endpoint không được tin định danh trong DTO.
    public void ActAs(string suffix)
    {
        PhienDangNhap.MaNV = Id(suffix);
        PhienDangNhap.KhoiTaoDinhDanhNghiepVu(Id("TK" + suffix));
    }

    public NguCanhNghiepVu User(string suffix)
    {
        ActAs(suffix);
        return new BUS_QUYENNGHIEPVU(Transaction).LayNguCanh(Id(suffix));
    }

    public Fixture()
    {
        var configured = Environment.GetEnvironmentVariable("RHM_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("ClinicalChecks yêu cầu RHM_CONNECTION_STRING trỏ bản sao thử nghiệm.");
        var builder = new SqlConnectionStringBuilder(configured);
        if (!builder.InitialCatalog.StartsWith("RHM_ClinicalWorkflow_Test_", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ClinicalChecks chỉ ghi fixture vào database có tên RHM_ClinicalWorkflow_Test_*.");
        Connection = new SqlConnection(configured);
        try
        {
            Connection.Open();
            Transaction = Connection.BeginTransaction(IsolationLevel.Serializable);
            Connection.Execute(Script, new { prefix = Prefix }, Transaction);
        }
        catch
        {
            Connection.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        try { if (Transaction.Connection is not null) Transaction.Rollback(); }
        finally { Transaction.Dispose(); Connection.Dispose(); Thread.CurrentPrincipal = originalPrincipal; }
    }

    public void Execute(string sql, object? values = null) => Connection.Execute(sql, values, Transaction);
    public T Scalar<T>(string sql, object? values = null) => Connection.ExecuteScalar<T>(sql, values, Transaction)!;

    private const string Script = """
        DECLARE @room nvarchar(50)=(SELECT TOP(1) MaPhong FROM dbo.PHONG ORDER BY MaPhong);
        DECLARE @specialty nvarchar(50)=(SELECT TOP(1) MaCK FROM dbo.CHUYEN_KHOA ORDER BY MaCK);
        DECLARE @userRole nvarchar(50)=(SELECT MaVaiTro FROM dbo.VAI_TRO WHERE TenVaiTro=N'User');
        DECLARE @adminRole nvarchar(50)=(SELECT MaVaiTro FROM dbo.VAI_TRO WHERE TenVaiTro=N'Admin');
        DECLARE @drug nvarchar(50)=(SELECT TOP(1) MaThuoc FROM dbo.THUOC ORDER BY MaThuoc);
        IF @room IS NULL OR @specialty IS NULL OR @userRole IS NULL OR @adminRole IS NULL OR @drug IS NULL
            THROW 51000,N'Bản sao baseline thiếu danh mục cần cho fixture quyền.',1;
        DECLARE @now datetime2(0)=DATEADD(minute,-10,SYSDATETIME());
        INSERT dbo.NHAN_VIEN(MaNV,HoTen,MaChucVu,MaCK,NgayVaoLam,TrangThai)
        SELECT @prefix+suffix,N'FIXTURE QUYỀN - ROLLBACK',job,@specialty,'2020-01-01',N'Đang làm việc'
        FROM (VALUES ('A',N'CV002'),('B',N'CV002'),('R',N'CV004'),('N',N'CV003'),
                     ('C',N'CV007'),('P',N'CV005'),('X',N'CV001')) jobs(suffix,job);
        INSERT dbo.TAI_KHOAN(MaTK,TenDangNhap,MatKhauHash,MaVaiTro,MaNV,TrangThai)
        SELECT @prefix+'TK'+suffix,@prefix+suffix,N'FIXTURE-NOT-A-LOGIN',
            CASE WHEN suffix='X' THEN @adminRole ELSE @userRole END,@prefix+suffix,N'Đang hoạt động'
        FROM (VALUES ('A'),('B'),('R'),('N'),('C'),('P'),('X')) users(suffix);
        INSERT dbo.BENH_NHAN(MaBN,HoTenBN,TrangThai,NhomMau,TienSuBenh,DiUng)
        VALUES (@prefix+'BN1',N'FIXTURE CÙNG TÊN',N'Đang theo dõi',N'A+',N'FIXTURE RIÊNG',N'FIXTURE DỊ ỨNG'),
               (@prefix+'BN2',N'FIXTURE CÙNG TÊN',N'Đang theo dõi',N'B+',N'FIXTURE RIÊNG',N'FIXTURE DỊ ỨNG');
        INSERT dbo.HO_SO_BENH_AN(MaHS,MaBN,NgayLap,LyDoKhamBanDau,TrangThai)
        VALUES (@prefix+'HS1',@prefix+'BN1',DATEADD(day,-1,@now),N'FIXTURE',N'Đang theo dõi'),
               (@prefix+'HS2',@prefix+'BN2',DATEADD(day,-1,@now),N'FIXTURE',N'Đang theo dõi');
        INSERT dbo.PHIEU_KHAM_BENH(MaPhieu,MaHS,MaPhong,MaBS,MaNVTiepNhan,SoThuTu,ThoiGianTiepNhan,LyDoKham,TrangThai)
        SELECT @prefix+'PH'+suffix,@prefix+history,@room,@prefix+doctor,@prefix+'R',1,@now,N'FIXTURE',N'Chờ khám'
        FROM (VALUES ('A','HS1','A'),('B','HS1','B'),('H','HS1','B'),('U','HS2','B')) visits(suffix,history,doctor);
        INSERT dbo.LICH_SU_KHAM_BENH(MaLS,MaPhieu,MaBS,MaPhong,ThoiGianKham,TrieuChung,ChanDoan,KeHoachXuLy,TrangThai,NgayXacNhan)
        SELECT @prefix+'LS'+suffix,@prefix+'PH'+suffix,@prefix+doctor,@room,DATEADD(minute,1,@now),
            N'FIXTURE TRIỆU CHỨNG',N'FIXTURE CHẨN ĐOÁN',N'FIXTURE XỬ TRÍ',
            CASE WHEN suffix IN ('H','U') THEN N'Đã xác nhận' ELSE N'Nháp' END,
            CASE WHEN suffix IN ('H','U') THEN DATEADD(minute,2,@now) ELSE NULL END
        FROM (VALUES ('A','A'),('B','B'),('H','B'),('U','B')) visits(suffix,doctor);
        INSERT dbo.DON_THUOC(MaDT,MaLS,MaBS,NgayKe,TrangThai,GhiChu)
        SELECT @prefix+'DT'+suffix,@prefix+'LS'+suffix,@prefix+doctor,DATEADD(minute,3,@now),N'Nháp',N'FIXTURE NỘI DUNG RIÊNG'
        FROM (VALUES ('A','A'),('B','B'),('H','B'),('U','B')) orders(suffix,doctor);
        INSERT dbo.CHI_TIET_DON_THUOC(MaCTDT,MaDT,MaThuoc,LieuDung,SoLuong,SoNgayDung,HuongDan)
        SELECT @prefix+'CT'+suffix,@prefix+'DT'+suffix,@drug,N'FIXTURE KHÔNG DÙNG THẬT',1,1,N'FIXTURE KHÔNG DÙNG THẬT'
        FROM (VALUES ('A'),('B'),('H'),('U')) lines(suffix);
        UPDATE dbo.DON_THUOC SET TrangThai=N'Đã xác nhận'
        WHERE MaDT IN (@prefix+'DTH',@prefix+'DTU');
        """;
}
