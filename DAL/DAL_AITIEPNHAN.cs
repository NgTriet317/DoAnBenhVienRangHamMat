using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Dapper;
using ET;

namespace DAL;

public class DAL_AITIEPNHAN : dbconnect
{
    public List<ET_AITIEPNHAN> layDanhSachChoDuyet()
    {
        // Chỉ truy vấn hệ thống bản khai truyền thống (SQL 07), không phụ thuộc
        // AI_SERVICE_INTAKE_SESSION của chế độ AI tự tạo phiếu đã ngưng sử dụng.
        return ThucHien((db, tx) =>
        {
            bool coLoaiKham = db.ExecuteScalar<int>("""
                SELECT CASE WHEN COL_LENGTH(N'dbo.AI_TIEP_NHAN_NHAP',N'LoaiKham')
                    IS NOT NULL THEN 1 ELSE 0 END
                """, transaction: tx) == 1;
            string truongLoaiKham = coLoaiKham ? "n.LoaiKham"
                : "CONVERT(nvarchar(20),NULL) AS LoaiKham";
            string sql = $"""
                SELECT p.MaPhien,p.TrangThai,p.NgayTao,p.MaPhieu,ph.SoThuTu,
                       n.HoTen,n.NgaySinh,n.SoDienThoai,n.DaTungKham,
                       n.LyDoKham,n.TrieuChung,n.CanHoTroGap,{truongLoaiKham}
                FROM dbo.AI_PHIEN_TIEP_NHAN p
                LEFT JOIN dbo.AI_TIEP_NHAN_NHAP n ON n.MaPhien=p.MaPhien
                LEFT JOIN dbo.PHIEU_KHAM_BENH ph ON ph.MaPhieu=p.MaPhieu
                WHERE p.TrangThai IN (N'ChoNhanVien',N'CanHoTro',N'DaDuyet')
                ORDER BY CASE WHEN p.TrangThai=N'CanHoTro' THEN 0 ELSE 1 END, p.NgayTao DESC
                """;
            return db.Query<ET_AITIEPNHAN>(sql, transaction: tx).ToList();
        });
    }

    public string luuBanKhai(ET_AI_BAN_KHAI m)
    {
        return ThucHien((db, tx) =>
        {
            if (db.ExecuteScalar<int>("""
                SELECT CASE WHEN OBJECT_ID(N'dbo.AI_PHIEN_TIEP_NHAN',N'U') IS NOT NULL
                     AND OBJECT_ID(N'dbo.AI_TIEP_NHAN_NHAP',N'U') IS NOT NULL
                     AND COL_LENGTH(N'dbo.AI_TIEP_NHAN_NHAP',N'LoaiKham') IS NOT NULL
                     THEN 1 ELSE 0 END
                """, transaction: tx) != 1)
                throw new InvalidOperationException("Chưa cài bảng bản khai AI hoặc SQL/17_AI_BanKhai_NhanVienDuyet.sql.");

            string? existing = db.QuerySingleOrDefault<string>("""
                SELECT TrangThai FROM dbo.AI_PHIEN_TIEP_NHAN WITH(UPDLOCK,HOLDLOCK)
                WHERE MaPhien=@MaPhien
                """, new {m.MaPhien}, tx);
            if (existing is not null)
            {
                if (existing is "ChoNhanVien" or "CanHoTro" or "DaDuyet")
                    return "AI-" + m.MaPhien.ToString("N")[..8].ToUpperInvariant();
                throw new InvalidOperationException("Phiên AI này đã đóng hoặc không thể gửi lại.");
            }

            string trangThai = m.CanHoTroGap ? "CanHoTro" : "ChoNhanVien";
            byte[] tokenHash = SHA256.HashData(RandomNumberGenerator.GetBytes(32));
            db.Execute("""
                INSERT dbo.AI_PHIEN_TIEP_NHAN
                    (MaPhien,MaKiosk,TokenHash,Buoc,TrangThai,DongY,NgayTao,NgayCapNhat,HetHan)
                VALUES (@MaPhien,N'WPF-AI',@TokenHash,N'XacNhan',@TrangThai,1,
                        SYSUTCDATETIME(),SYSUTCDATETIME(),DATEADD(minute,30,SYSUTCDATETIME()))
                """, new {m.MaPhien,tokenHash,TrangThai=trangThai}, tx);
            db.Execute("""
                INSERT dbo.AI_TIEP_NHAN_NHAP
                    (MaPhien,HoTen,NgaySinh,SoDienThoai,DaTungKham,LyDoKham,
                     TrieuChung,CanHoTroGap,LoaiKham,NgayCapNhat)
                VALUES (@MaPhien,@HoTen,@NgaySinh,@SoDienThoai,@DaTungKham,@LyDoKham,
                        @TrieuChung,@CanHoTroGap,@LoaiKham,SYSUTCDATETIME())
                """, new {m.MaPhien, HoTen = string.IsNullOrWhiteSpace(m.HoTen) ? null : m.HoTen.Trim(),
                            m.NgaySinh, SoDienThoai = string.IsNullOrWhiteSpace(m.SoDienThoai) ? null : m.SoDienThoai.Trim(),
                            m.DaTungKham,LyDoKham = string.IsNullOrWhiteSpace(m.LyDoKham) ? null : m.LyDoKham.Trim(),
                            m.TrieuChung,m.CanHoTroGap,m.LoaiKham}, tx);
            db.Execute("""
                INSERT dbo.AI_SU_KIEN(MaPhien,LoaiSuKien)
                VALUES(@MaPhien,@LoaiSuKien)
                """, new {m.MaPhien,LoaiSuKien=m.CanHoTroGap?"CanHoTro":"GuiNhanVien"}, tx);
            return "AI-" + m.MaPhien.ToString("N")[..8].ToUpperInvariant();
        });
    }

    public void xacNhanPhieu(Guid maPhien, string maPhieu, string maNV)
    {
        ThucHien((db, tx) =>
        {
            db.Execute("dbo.AI_XacNhanPhien",
                new { MaPhien = maPhien, MaPhieu = maPhieu, MaNV = maNV },
                transaction: tx, commandType: System.Data.CommandType.StoredProcedure);
            return true;
        });
    }
}
