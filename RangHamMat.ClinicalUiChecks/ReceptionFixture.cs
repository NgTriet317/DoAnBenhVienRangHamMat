using System.Data;
using Dapper;
using DAL;

namespace RangHamMat.ClinicalUiChecks;

/// <summary>Fixture WPF phải nhìn thấy từ kết nối của form; chỉ chạy sau guard database thử ở Main.</summary>
internal sealed class ReceptionFixture : IDisposable
{
    private readonly IDbConnection db=new DAL_DONTHUOC().MoKetNoi();
    private readonly string prefix="CWUI"+Guid.NewGuid().ToString("N");
    public string Patient=>prefix+"BN";
    public string Doctor=>prefix+"A";
    public string Service { get; }
    public ReceptionFixture()
    {
        var info=db.QuerySingle<(string Service,string Specialty,string Room)>("SELECT TOP(1) d.MaDV,d.MaCK,p.MaPhong FROM dbo.DICH_VU d JOIN dbo.CHUYEN_KHOA c ON c.MaCK=d.MaCK JOIN dbo.PHONG p ON p.MaKhoa=c.MaKhoa WHERE d.TrangThai=N'Đang hoạt động' AND p.TrangThai=N'Đang hoạt động' AND p.LoaiPhong LIKE N'Phòng khám%' ORDER BY d.MaDV,p.MaPhong");
        Service=info.Service;
        try
        {
            using var tx=db.BeginTransaction();
            db.Execute("""
                INSERT dbo.NHAN_VIEN(MaNV,HoTen,MaChucVu,MaCK,NgayVaoLam,TrangThai) VALUES(@doctor,N'FIXTURE WPF BÁC SĨ',N'CV002',@specialty,'2020-01-01',N'Đang làm việc');
                INSERT dbo.TAI_KHOAN(MaTK,TenDangNhap,MatKhauHash,MaVaiTro,MaNV,TrangThai) VALUES(@prefix+'TK',@prefix,N'FIXTURE-NOT-A-LOGIN',(SELECT MaVaiTro FROM dbo.VAI_TRO WHERE TenVaiTro=N'User'),@doctor,N'Đang hoạt động');
                INSERT dbo.BENH_NHAN(MaBN,HoTenBN,TrangThai,DiUng,TienSuBenh) VALUES(@patient,N'FIXTURE WPF NGƯỜI BỆNH',N'Đang theo dõi',N'FIXTURE DỊ ỨNG',N'FIXTURE TIỀN SỬ');
                INSERT dbo.CA_LAM(MaCa,GioBatDau,GioKetThuc,TrangThai) VALUES(@prefix+'CA','00:00:00','23:59:59',N'Đang hoạt động');
                INSERT dbo.LICH_LAM_VIEC(MaLLV,MaNV,MaCa,NgayLam,MaPhong,TrangThai,TrangThaiHienDien)
                VALUES(@prefix+'LLV',@doctor,@prefix+'CA',CAST(SYSDATETIME() AS date),@room,N'Đã xác nhận',N'Có mặt');
                """,new{prefix,doctor=Doctor,patient=Patient,specialty=info.Specialty,room=info.Room},tx);
            tx.Commit();
        }
        catch {db.Dispose();throw;}
    }
    public int CountVisits(string receipt)=>db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.LICH_SU_KHAM_BENH WHERE MaPhieu=@receipt",new{receipt});
    public void Dispose()
    {
        try
        {
            using var tx=db.BeginTransaction();
            db.Execute("""
                DELETE n FROM dbo.NHAT_KY_THAO_TAC n WHERE n.LoaiDoiTuong=N'PHIEU_KHAM_BENH' AND n.MaDoiTuong IN(SELECT p.MaPhieu FROM dbo.PHIEU_KHAM_BENH p JOIN dbo.HO_SO_BENH_AN h ON h.MaHS=p.MaHS WHERE h.MaBN=@patient);
                DELETE p FROM dbo.PHIEU_KHAM_BENH p JOIN dbo.HO_SO_BENH_AN h ON h.MaHS=p.MaHS WHERE h.MaBN=@patient;
                DELETE dbo.HO_SO_BENH_AN WHERE MaBN=@patient;
                DELETE dbo.BENH_NHAN WHERE MaBN=@patient;
                DELETE dbo.LICH_LAM_VIEC WHERE MaLLV=@prefix+'LLV';
                DELETE dbo.CA_LAM WHERE MaCa=@prefix+'CA';
                DELETE dbo.TAI_KHOAN WHERE MaTK=@prefix+'TK';
                DELETE dbo.NHAN_VIEN WHERE MaNV=@doctor;
                """,new{prefix,patient=Patient,doctor=Doctor},tx);
            tx.Commit();
        }
        finally {db.Dispose();}
    }
}
