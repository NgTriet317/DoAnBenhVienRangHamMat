using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;

namespace DAL
{
    /// <summary>Lưu/đọc quyền vai trò; toàn bộ thay đổi được thực hiện trong một giao dịch SQL.</summary>
    public class DAL_PHANQUYENVAITRO : dbconnect
    {
        public IEnumerable<string> layQuyenTheoVaiTro(string maVaiTro)
        {
            return ThucHien((db, tx) => db.Query<string>(@"
                SELECT MaPhanHe FROM dbo.PHAN_QUYEN_VAI_TRO
                WHERE MaVaiTro=@MaVaiTro AND DuocTruyCap=1",
                new { MaVaiTro = maVaiTro }, tx).ToArray());
        }

        // Tài khoản phải còn hoạt động; quyền không dựa trên cờ UI hoặc tên vai trò từ client.
        public IEnumerable<string> layQuyenTheoTaiKhoan(string maTK)
        {
            return ThucHien((db, tx) => db.Query<string>(@"
                SELECT DISTINCT q.MaPhanHe
                FROM dbo.TAI_KHOAN AS tk
                JOIN dbo.PHAN_QUYEN_VAI_TRO AS q ON q.MaVaiTro=tk.MaVaiTro
                WHERE tk.MaTK=@MaTK AND tk.TrangThai=N'Đang hoạt động' AND q.DuocTruyCap=1
                UNION
                SELECT N'TaiKhoan'
                FROM dbo.TAI_KHOAN AS tk
                JOIN dbo.VAI_TRO AS vt ON vt.MaVaiTro=tk.MaVaiTro
                WHERE tk.MaTK=@MaTK AND tk.TrangThai=N'Đang hoạt động'
                  AND vt.TenVaiTro IN (N'Admin',N'Quản trị',N'Quản trị viên')",
                new { MaTK = maTK }, tx).ToArray());
        }

        // Đọc trạng thái trực tiếp từ SQL, dùng để phát hiện tài khoản vừa bị khóa ở máy khác.
        public bool taiKhoanConHoatDong(string maTK)
            => ThucHien((db, tx) => db.ExecuteScalar<int>(@"
                SELECT COUNT(1) FROM dbo.TAI_KHOAN
                WHERE MaTK=@MaTK AND TrangThai=N'Đang hoạt động'",
                new { MaTK = maTK }, tx) == 1);

        public bool luuQuyenVaiTro(string maNguoiSua, string maVaiTro,
            IReadOnlyCollection<string> danhMuc, IReadOnlyCollection<string> quyenDuocCap)
        {
            return ThucHien((db, tx) =>
            {
                // Kiểm tra quyền quản trị lại trực tiếp từ dữ liệu SQL, không tin checkbox.
                bool laQuanTri = db.ExecuteScalar<int>(@"
                    SELECT COUNT(1) FROM dbo.TAI_KHOAN tk WITH (UPDLOCK,HOLDLOCK)
                    JOIN dbo.VAI_TRO vt ON vt.MaVaiTro=tk.MaVaiTro
                    WHERE tk.MaTK=@MaTK AND tk.TrangThai=N'Đang hoạt động'
                      AND vt.TenVaiTro IN (N'Admin',N'Quản trị',N'Quản trị viên')",
                    new { MaTK = maNguoiSua }, tx) > 0;
                if (!laQuanTri)
                    throw new InvalidOperationException("Chỉ quản trị viên được thay đổi phân quyền.");

                var tenVaiTro = db.QuerySingleOrDefault<string>(@"
                    SELECT TenVaiTro FROM dbo.VAI_TRO WITH (UPDLOCK,HOLDLOCK)
                    WHERE MaVaiTro=@MaVaiTro",
                    new { MaVaiTro = maVaiTro }, tx);
                if (tenVaiTro == null)
                    throw new InvalidOperationException("Vai trò không còn tồn tại.");

                bool laVaiTroQuanTri = new[] { "Admin", "Quản trị", "Quản trị viên" }
                    .Contains(tenVaiTro.Trim(), StringComparer.OrdinalIgnoreCase);
                var daChon = quyenDuocCap.ToHashSet(StringComparer.Ordinal);
                if (laVaiTroQuanTri) daChon.Add("TaiKhoan"); // không tự khóa đường quản trị

                db.Execute("DELETE FROM dbo.PHAN_QUYEN_VAI_TRO WHERE MaVaiTro=@MaVaiTro",
                    new { MaVaiTro = maVaiTro }, tx);
                foreach (var maPhanHe in danhMuc)
                {
                    db.Execute(@"
                        INSERT INTO dbo.PHAN_QUYEN_VAI_TRO
                        (MaVaiTro,MaPhanHe,DuocTruyCap)
                        VALUES (@MaVaiTro,@MaPhanHe,@DuocTruyCap)",
                        new { MaVaiTro = maVaiTro, MaPhanHe = maPhanHe,
                            DuocTruyCap = daChon.Contains(maPhanHe) }, tx);
                }
                return true;
            });
        }
    }
}
