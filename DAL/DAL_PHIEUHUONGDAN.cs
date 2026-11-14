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
            // SQL 15 mới bổ sung NguonTiepNhan và MaPhienAI. Bản DB chưa nâng cấp
            // vẫn phải mở được phiếu A5 của nhân viên, thay vì lỗi Invalid column name.
            // Tên đoạn SQL được chọn từ hằng do ứng dụng định nghĩa, KHÔNG lấy từ người dùng.
            bool coNguon = db.ExecuteScalar<int>(
                "SELECT CASE WHEN COL_LENGTH(N'dbo.PHIEU_KHAM_BENH', N'NguonTiepNhan') IS NOT NULL THEN 1 ELSE 0 END",
                transaction: transaction) == 1;
            bool coPhien = db.ExecuteScalar<int>(
                "SELECT CASE WHEN COL_LENGTH(N'dbo.PHIEU_KHAM_BENH', N'MaPhienAI') IS NOT NULL THEN 1 ELSE 0 END",
                transaction: transaction) == 1;
            string nguon = coNguon ? "p.NguonTiepNhan"
                : "CONVERT(nvarchar(20),N'NhanVien') AS NguonTiepNhan";
            string phien = coPhien ? "p.MaPhienAI"
                : "CONVERT(uniqueidentifier,NULL) AS MaPhienAI";
            string dieuKienNguonAi = coNguon ? " OR p.NguonTiepNhan = N'AI'" : "";
            string sql = $"""
                SELECT p.MaPhieu, p.MaHS, hs.MaBN, bn.HoTenBN,
                       p.MaLich, p.MaPhong, phong.TenPhong, phong.MaKhoa,
                       khoa.TenKhoa, p.MaBS,
                       COALESCE(bacsi.HoTen, N'Chưa phân công') AS TenBacSi,
                       p.MaDV, COALESCE(dv.TenDV, N'Chưa chọn dịch vụ') AS TenDichVu,
                       p.MaNVTiepNhan, COALESCE(nv.HoTen, N'Tiếp nhận AI') AS TenNhanVienTiepNhan,
                       {nguon}, {phien},
                       p.SoThuTu, p.ThoiGianTiepNhan, lich.NgayGioKham,
                       p.TrangThai
                FROM dbo.PHIEU_KHAM_BENH AS p
                INNER JOIN dbo.HO_SO_BENH_AN AS hs ON hs.MaHS = p.MaHS
                INNER JOIN dbo.BENH_NHAN AS bn ON bn.MaBN = hs.MaBN
                INNER JOIN dbo.PHONG AS phong ON phong.MaPhong = p.MaPhong
                INNER JOIN dbo.KHOA AS khoa ON khoa.MaKhoa = phong.MaKhoa
                LEFT JOIN dbo.NHAN_VIEN AS nv ON nv.MaNV = p.MaNVTiepNhan
                LEFT JOIN dbo.NHAN_VIEN AS bacsi ON bacsi.MaNV = p.MaBS
                LEFT JOIN dbo.DICH_VU AS dv ON dv.MaDV = p.MaDV
                LEFT JOIN dbo.LICH_KHAM AS lich ON lich.MaLich = p.MaLich
                WHERE @LaAdmin = 1 OR p.MaNVTiepNhan = @MaNV OR p.MaBS = @MaNV{dieuKienNguonAi}
                ORDER BY p.ThoiGianTiepNhan DESC, p.MaPhieu DESC;
                """;
            using var reader = db.ExecuteReader(sql, new { MaNV = maNV, LaAdmin = laAdmin }, transaction);
            var bang = new DataTable();
            bang.Load(reader);
            return bang;
        });
    }
}
