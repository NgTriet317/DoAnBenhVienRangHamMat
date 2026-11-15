using System.Data;
using Dapper;
using ET;

namespace DAL
{
    // Cổng ghi dữ liệu quản trị: kiểm tra quyền từ SQL và lưu bằng DAL hiện có
    // trong một giao dịch. Không dùng IsAdmin từ client làm nguồn xác thực.
    public class DAL_QUANLYTAIKHOAN : dbconnect
    {
        public bool luuTaiKhoan(string maNguoiSua, ET_TAIKHOAN duLieu, ET_TAIKHOAN? banGoc)
        {
            return ThucHien((db, tx) =>
            {
                KiemTraQuanTri(db, tx, maNguoiSua);

                if (db.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM dbo.VAI_TRO WHERE MaVaiTro=@Ma",
                    new { Ma = duLieu.MaVaiTro }, tx) == 0)
                    throw new InvalidOperationException("Vai trò được chọn không còn tồn tại.");

                if (db.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM dbo.NHAN_VIEN WHERE MaNV=@Ma",
                    new { Ma = duLieu.MaNV }, tx) == 0)
                    throw new InvalidOperationException("Nhân viên được chọn không còn tồn tại.");

                if (db.ExecuteScalar<int>(@"
                    SELECT COUNT(1) FROM dbo.TAI_KHOAN WITH(UPDLOCK,HOLDLOCK)
                    WHERE TenDangNhap=@Ten AND MaTK<>@Ma",
                    new { Ten = duLieu.TenDangNhap, Ma = duLieu.MaTK }, tx) > 0)
                    throw new InvalidOperationException("Tên đăng nhập đã được sử dụng.");

                if (db.ExecuteScalar<int>(@"
                    SELECT COUNT(1) FROM dbo.TAI_KHOAN WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaNV=@NhanVien AND MaTK<>@Ma",
                    new { NhanVien = duLieu.MaNV, Ma = duLieu.MaTK }, tx) > 0)
                    throw new InvalidOperationException("Nhân viên đã có tài khoản đăng nhập.");

                if (banGoc is not null)
                {
                    if (!string.Equals(banGoc.MaTK, duLieu.MaTK, StringComparison.Ordinal))
                        throw new InvalidOperationException("Mã tài khoản không được thay đổi.");
                    if (duLieu.MaTK == maNguoiSua && duLieu.TrangThai != "Đang hoạt động")
                        throw new InvalidOperationException("Không thể khóa chính tài khoản đang đăng nhập.");
                    if (duLieu.MaTK == maNguoiSua && duLieu.MaNV != banGoc.MaNV)
                        throw new InvalidOperationException("Không thể đổi nhân viên liên kết với chính tài khoản đang đăng nhập.");

                    var vaiTroCu = db.QuerySingleOrDefault<string>(@"
                        SELECT vt.TenVaiTro
                        FROM dbo.TAI_KHOAN tk WITH(UPDLOCK,HOLDLOCK)
                        JOIN dbo.VAI_TRO vt ON vt.MaVaiTro=tk.MaVaiTro
                        WHERE tk.MaTK=@Ma", new { Ma = duLieu.MaTK }, tx);
                    var vaiTroMoi = db.QuerySingle<string>(
                        "SELECT TenVaiTro FROM dbo.VAI_TRO WHERE MaVaiTro=@Ma",
                        new { Ma = duLieu.MaVaiTro }, tx);

                    if (LaQuanTri(vaiTroCu) && (!LaQuanTri(vaiTroMoi)
                        || duLieu.TrangThai != "Đang hoạt động"))
                    {
                        int conLai = db.ExecuteScalar<int>(@"
                            SELECT COUNT(1) FROM dbo.TAI_KHOAN tk WITH(UPDLOCK,HOLDLOCK)
                            JOIN dbo.VAI_TRO vt ON vt.MaVaiTro=tk.MaVaiTro
                            WHERE tk.MaTK<>@Ma AND tk.TrangThai=N'Đang hoạt động'
                              AND vt.TenVaiTro IN (N'Admin',N'Quản trị',N'Quản trị viên')",
                            new { Ma = duLieu.MaTK }, tx);
                        if (conLai == 0)
                            throw new InvalidOperationException("Không thể vô hiệu hóa quản trị viên hoạt động cuối cùng.");
                    }
                    return new DAL_TAIKHOAN(tx).suaTaiKhoan(duLieu, banGoc);
                }

                return new DAL_TAIKHOAN(tx).themTaiKhoan(duLieu);
            });
        }

        public bool luuVaiTro(string maNguoiSua, ET_VAITRO duLieu, ET_VAITRO? banGoc)
        {
            return ThucHien((db, tx) =>
            {
                KiemTraQuanTri(db, tx, maNguoiSua);

                if (banGoc != null)
                {
                    if (!string.Equals(banGoc.MaVaiTro, duLieu.MaVaiTro, StringComparison.Ordinal))
                        throw new InvalidOperationException("Không được thay đổi mã vai trò.");
                    if (LaQuanTri(banGoc.TenVaiTro)
                        && !string.Equals(banGoc.TenVaiTro.Trim(), duLieu.TenVaiTro.Trim(), StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Không thể đổi tên vai trò quản trị hệ thống.");
                }

                if (db.ExecuteScalar<int>(@"
                    SELECT COUNT(1) FROM dbo.VAI_TRO WITH(UPDLOCK,HOLDLOCK)
                    WHERE LTRIM(RTRIM(TenVaiTro))=@Ten AND MaVaiTro<>@Ma",
                    new { Ten = duLieu.TenVaiTro, Ma = duLieu.MaVaiTro }, tx) > 0)
                    throw new InvalidOperationException("Tên vai trò đã tồn tại.");

                return banGoc == null
                    ? new DAL_VAITRO(tx).themVaiTro(duLieu)
                    : new DAL_VAITRO(tx).suaVaiTro(duLieu, banGoc);
            });
        }

        // Khóa/khôi phục chỉ đổi trạng thái. Giữ nguyên vai trò, mật khẩu và bảng phân quyền.
        public bool doiTrangThaiTaiKhoan(string maNguoiSua, string maTK,
            string trangThaiCu, string trangThaiMoi)
        {
            return ThucHien((db, tx) =>
            {
                KiemTraQuanTri(db, tx, maNguoiSua);
                if (string.IsNullOrWhiteSpace(maTK) || maTK == maNguoiSua)
                    throw new InvalidOperationException("Không được khóa hoặc khôi phục chính tài khoản đang đăng nhập.");
                if (!((trangThaiCu == "Đang hoạt động" && trangThaiMoi == "Đã khóa")
                    || (trangThaiCu == "Đã khóa" && trangThaiMoi == "Đang hoạt động")))
                    throw new InvalidOperationException("Chuyển trạng thái không hợp lệ. Vui lòng tải lại tài khoản.");

                var hienTai = db.QuerySingleOrDefault<ET_TAIKHOAN>(@"
                    SELECT * FROM dbo.TAI_KHOAN WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaTK=@Ma", new { Ma = maTK }, tx);
                if (hienTai == null || hienTai.TrangThai != trangThaiCu)
                    throw new InvalidOperationException("Trạng thái tài khoản đã thay đổi. Vui lòng tải lại.");

                // Bảo vệ quản trị viên hoạt động cuối cùng.
                if (trangThaiMoi == "Đã khóa")
                {
                    var tenVaiTro = db.QuerySingleOrDefault<string>(@"
                        SELECT TenVaiTro FROM dbo.VAI_TRO WHERE MaVaiTro=@Ma",
                        new { Ma = hienTai.MaVaiTro }, tx);
                    if (LaQuanTri(tenVaiTro))
                    {
                        int quanTriConLai = db.ExecuteScalar<int>(@"
                            SELECT COUNT(1) FROM dbo.TAI_KHOAN tk WITH(UPDLOCK,HOLDLOCK)
                            JOIN dbo.VAI_TRO vt ON tk.MaVaiTro=vt.MaVaiTro
                            WHERE tk.MaTK<>@Ma AND tk.TrangThai=N'Đang hoạt động'
                              AND vt.TenVaiTro IN (N'Admin',N'Quản trị',N'Quản trị viên')",
                            new { Ma = maTK }, tx);
                        if (quanTriConLai == 0)
                            throw new InvalidOperationException("Không thể khóa quản trị viên hoạt động cuối cùng.");
                    }
                }

                var daSua = db.Execute(@"
                    UPDATE dbo.TAI_KHOAN SET TrangThai=@TrangThaiMoi
                    WHERE MaTK=@Ma AND TrangThai=@TrangThaiCu",
                    new { Ma = maTK, TrangThaiCu = trangThaiCu, TrangThaiMoi = trangThaiMoi }, tx);
                if (daSua != 1)
                    throw new InvalidOperationException("Không cập nhật được trạng thái. Vui lòng tải lại.");
                return true;
            });
        }

        private static bool LaQuanTri(string? ten)
            => string.Equals(ten?.Trim(), "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ten?.Trim(), "Quản trị", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ten?.Trim(), "Quản trị viên", StringComparison.OrdinalIgnoreCase);

        private static void KiemTraQuanTri(IDbConnection db, IDbTransaction? tx, string maNguoiSua)
        {
            if (string.IsNullOrWhiteSpace(maNguoiSua))
                throw new InvalidOperationException("Phiên đăng nhập không hợp lệ.");

            int coQuyen = db.ExecuteScalar<int>(@"
                SELECT COUNT(1) FROM dbo.TAI_KHOAN tk WITH(UPDLOCK,HOLDLOCK)
                JOIN dbo.VAI_TRO vt ON vt.MaVaiTro=tk.MaVaiTro
                WHERE tk.MaTK=@Ma AND tk.TrangThai=N'Đang hoạt động'
                  AND vt.TenVaiTro IN (N'Admin',N'Quản trị',N'Quản trị viên')",
                new { Ma = maNguoiSua }, tx);
            if (coQuyen == 0)
                throw new InvalidOperationException("Chỉ quản trị viên đang hoạt động mới được thêm hoặc sửa tài khoản và vai trò.");
        }
    }
}
