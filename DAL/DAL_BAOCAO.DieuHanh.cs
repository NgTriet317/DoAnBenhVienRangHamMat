using Dapper;
using ET;

namespace DAL;

/// <summary>Tổng hợp điều hành theo các nguồn độc lập để không nhân số bệnh nhân, lịch hoặc tiền.</summary>
public sealed partial class DAL_BAOCAO
{
    private const string SqlTongQuanDieuHanh = LuotKham + """
        SELECT
          (SELECT COUNT(DISTINCT MaBN) FROM Luot) AS SoBenhNhan,
          (SELECT COUNT(*) FROM Luot) AS TongLuotKham,
          (SELECT COUNT(*) FROM Luot WHERE CoMa=0) AS LuotChuaPhanLoai,
          (SELECT COUNT(*) FROM dbo.LICH_SU_KHAM_BENH WHERE TrangThai=N'Nháp' AND ThoiGianKham>=@TuNgay AND ThoiGianKham<@DenExclusive) AS LuotKhamNhap,
          (SELECT COUNT(*) FROM dbo.LICH_KHAM WHERE NgayGioKham>=@TuNgay AND NgayGioKham<@DenExclusive) AS TongLichHen,
          (SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH WHERE ThoiGianTiepNhan>=@TuNgay AND ThoiGianTiepNhan<@DenExclusive) AS SoPhieuTiepNhan,
          (SELECT COALESCE(SUM(SoTien),0) FROM dbo.THANH_TOAN WHERE TrangThai=N'Đã xác nhận' AND LoaiGiaoDich=N'Thu' AND NgayThanhToan>=@TuNgay AND NgayThanhToan<@DenExclusive) AS TienThu,
          (SELECT COALESCE(SUM(SoTien),0) FROM dbo.THANH_TOAN WHERE TrangThai=N'Đã xác nhận' AND LoaiGiaoDich=N'Hoàn' AND NgayThanhToan>=@TuNgay AND NgayThanhToan<@DenExclusive) AS TienHoan,
          (SELECT COUNT(*) FROM dbo.THANH_TOAN WHERE TrangThai=N'Đã xác nhận' AND NgayThanhToan>=@TuNgay AND NgayThanhToan<@DenExclusive) AS SoGiaoDich;
        """;

    private const string SqlTrangThaiLichDieuHanh = """
        SELECT TrangThai,COUNT(*) AS SoLuong FROM dbo.LICH_KHAM
        WHERE NgayGioKham>=@TuNgay AND NgayGioKham<@DenExclusive
        GROUP BY TrangThai ORDER BY COUNT(*) DESC,TrangThai;
        """;

    private const string SqlDichVuDieuHanh = """
        SELECT TOP (5) dv.MaDV,dv.TenDV,COUNT(DISTINCT th.MaLS) AS SoLuotKham
        FROM dbo.DICH_VU_THUC_HIEN th
        JOIN dbo.DICH_VU dv ON dv.MaDV=th.MaDV
        JOIN dbo.LICH_SU_KHAM_BENH ls ON ls.MaLS=th.MaLS
        WHERE th.TrangThai=N'Đã xác nhận' AND ls.TrangThai=N'Đã xác nhận'
          AND th.ThoiGianThucHien>=@TuNgay AND th.ThoiGianThucHien<@DenExclusive
        GROUP BY dv.MaDV,dv.TenDV ORDER BY COUNT(DISTINCT th.MaLS) DESC,dv.MaDV;
        """;

    /// <summary>Đọc tổng quan và các kỳ đối chiếu trên cùng kết nối/giao dịch sau khi xác thực phiên.</summary>
    public GoiTongQuanDieuHanh DocGoiTongQuan(KySoSanh[] cacKy,KySoSanh kyHienTai)
    {
        ArgumentNullException.ThrowIfNull(cacKy);
        if(cacKy.Length is <1 or >50||!cacKy.Contains(kyHienTai))throw new ArgumentException("Gói tổng quan phải chứa kỳ cần xem và tối đa 50 kỳ.");
        foreach(var ky in cacKy)
        {
            KiemTraDinhDang("00",ky.TuNgay,ky.DenExclusive.AddDays(-1),30);
            if(ky.TuNgay.TimeOfDay!=TimeSpan.Zero||ky.DenExclusive.TimeOfDay!=TimeSpan.Zero)throw new ArgumentException("Kỳ tổng quan phải dùng ngày tròn.");
        }
        KiemTraPrincipal();
        return ThucHien((db,tx)=>
        {
            XacThucQuyen(db,"00",tx);
            object Args(KySoSanh ky)=>new{ky.TuNgay,ky.DenExclusive,MaCK=(string?)null,MaDV=(string?)null};
            var periods=cacKy.Distinct().ToDictionary(ky=>ky,ky=>db.QuerySingle<ThongKeDieuHanh>(SqlTongQuanDieuHanh,Args(ky),tx));
            var statuses=db.Query<DongTrangThaiDieuHanh>(SqlTrangThaiLichDieuHanh,Args(kyHienTai),tx).ToArray();
            var services=db.Query<DongDichVuDieuHanh>(SqlDichVuDieuHanh,Args(kyHienTai),tx).ToArray();
            return new GoiTongQuanDieuHanh(periods,kyHienTai,statuses,services,DateTime.Now);
        });
    }
}
