using System.Data;
using Dapper;

namespace DAL;

/// <summary>
/// Đọc dữ liệu phiếu hướng dẫn từ phiếu tiếp nhận hiện có; không tạo bảng/phiếu khám mới.
/// </summary>
public class DAL_PHIEUHUONGDAN : dbconnect
{
    public DataTable layDanhSachPhieu(string maNV, bool laAdmin)
    {
        if (!laAdmin && string.IsNullOrWhiteSpace(maNV))
            throw new InvalidOperationException("Không xác định được nhân viên đăng nhập.");

        return ThucHien((db, transaction) =>
        {
            const string sql = """
                SELECT p.MaPhieu, p.MaHS, hs.MaBN, bn.HoTenBN,
                       p.MaLich, p.MaPhong, phong.TenPhong, phong.MaKhoa,
                       khoa.TenKhoa, p.MaBS,
                       COALESCE(bacsi.HoTen, N'Chưa phân công') AS TenBacSi,
                       p.MaDV, COALESCE(dv.TenDV, N'Chưa chọn dịch vụ') AS TenDichVu,
                       p.MaNVTiepNhan, nv.HoTen AS TenNhanVienTiepNhan,
                       p.SoThuTu, p.ThoiGianTiepNhan, lich.NgayGioKham,
                       p.TrangThai
                FROM dbo.PHIEU_KHAM_BENH AS p
                INNER JOIN dbo.HO_SO_BENH_AN AS hs ON hs.MaHS = p.MaHS
                INNER JOIN dbo.BENH_NHAN AS bn ON bn.MaBN = hs.MaBN
                INNER JOIN dbo.PHONG AS phong ON phong.MaPhong = p.MaPhong
                INNER JOIN dbo.KHOA AS khoa ON khoa.MaKhoa = phong.MaKhoa
                INNER JOIN dbo.NHAN_VIEN AS nv ON nv.MaNV = p.MaNVTiepNhan
                LEFT JOIN dbo.NHAN_VIEN AS bacsi ON bacsi.MaNV = p.MaBS
                LEFT JOIN dbo.DICH_VU AS dv ON dv.MaDV = p.MaDV
                LEFT JOIN dbo.LICH_KHAM AS lich ON lich.MaLich = p.MaLich
                WHERE @LaAdmin = 1 OR p.MaNVTiepNhan = @MaNV OR p.MaBS = @MaNV
                ORDER BY p.ThoiGianTiepNhan DESC, p.MaPhieu DESC;
                """;
            using var reader = db.ExecuteReader(sql, new { MaNV = maNV, LaAdmin = laAdmin }, transaction);
            var bang = new DataTable();
            bang.Load(reader);
            return bang;
        });
    }
}
