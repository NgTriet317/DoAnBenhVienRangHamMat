using Dapper;
using ET;

namespace DAL;

public sealed partial class DAL_BAOCAO
{
    private ET_DONGBAOCAO[] DocChiTietTaiChinh(string ma,DateTime tu,DateTime den)
    {
        using var db=MoKetNoi();
        if(!XacThucQuyen(db,ma).HasFlag(QuyenBaoCao.XemTaiChinhChiTiet))
            throw new UnauthorizedAccessException("Không có quyền xem chứng từ tài chính.");
        var sql=ma=="09"?"""
            SELECT TOP(200) NgayThanhToan AS Ngay,MaTT AS Ma,PhuongThuc AS Ten,LoaiGiaoDich AS PhanLoai,N'VNĐ' AS DonVi,
              NgayThanhToan AS NgayChiTiet,SoTien AS ChiTietMot,
              CASE WHEN LoaiGiaoDich=N'Thu' THEN SoTien ELSE -SoTien END AS ChiTietHai,
              TrangThai,N'Hóa đơn: '+MaHD AS GhiChu
            FROM dbo.THANH_TOAN WHERE TrangThai=N'Đã xác nhận' AND NgayThanhToan>=@TuNgay AND NgayThanhToan<@DenExclusive
            ORDER BY NgayThanhToan,MaTT
            """:"""
            SELECT TOP(200) NgayLap AS Ngay,MaHD AS Ma,N'Hóa đơn bệnh viện' AS Ten,N'Phải thu' AS PhanLoai,N'VNĐ' AS DonVi,
              NgayLap AS NgayChiTiet,SoTienPhaiThu AS ChiTietMot,TrangThai,N'Giá trị phải thu; không phải tiền thực thu' AS GhiChu
            FROM dbo.HOA_DON WHERE NgayLap>=@TuNgay AND NgayLap<@DenExclusive ORDER BY NgayLap,MaHD
            """;
        return db.Query<ET_DONGBAOCAO>(sql,new{TuNgay=tu.Date,DenExclusive=den.Date.AddDays(1)}).ToArray();
    }
}
