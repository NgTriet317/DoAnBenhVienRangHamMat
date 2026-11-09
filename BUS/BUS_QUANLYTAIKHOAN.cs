using System.Security.Cryptography;
using System.Text.RegularExpressions;
using DAL;
using ET;

namespace BUS
{
    // Quy tắc thêm/sửa tài khoản và vai trò, kế thừa định dạng mật khẩu của DangNhap.xaml.cs.
    public class BUS_QUANLYTAIKHOAN
    {
        private readonly DAL_QUANLYTAIKHOAN data = new();

        public bool luuTaiKhoan(string maNguoiSua, ET_TAIKHOAN duLieu,
            ET_TAIKHOAN? banGoc, string? matKhauMoi)
        {
            duLieu.TenDangNhap = (duLieu.TenDangNhap ?? "").Trim();
            duLieu.TrangThai = (duLieu.TrangThai ?? "").Trim();

            if (!Regex.IsMatch(duLieu.TenDangNhap, @"^[a-zA-Z0-9._-]{3,100}$"))
                throw new InvalidOperationException("Tên đăng nhập cần 3-100 ký tự (chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang).");
            if (string.IsNullOrWhiteSpace(duLieu.MaVaiTro) || string.IsNullOrWhiteSpace(duLieu.MaNV))
                throw new InvalidOperationException("Vui lòng chọn nhân viên và vai trò.");
            if (duLieu.TrangThai is not ("Đang hoạt động" or "Đã khóa"))
                throw new InvalidOperationException("Trạng thái tài khoản không hợp lệ.");
            if (duLieu.MaKhoa != null && duLieu.MaPhong != null)
                throw new InvalidOperationException("Chỉ được liên kết tài khoản với khoa hoặc phòng, không được cả hai.");

            if (!string.IsNullOrEmpty(matKhauMoi))
            {
                if (matKhauMoi.Length < 8 || matKhauMoi.Length > 128 ||
                    !matKhauMoi.Any(char.IsLetter) || !matKhauMoi.Any(char.IsDigit))
                    throw new InvalidOperationException("Mật khẩu phải có 8-128 ký tự, bao gồm chữ và số.");
                duLieu.MatKhauHash = BamMatKhau(matKhauMoi);
            }
            else if (banGoc != null)
                duLieu.MatKhauHash = banGoc.MatKhauHash;
            else
                throw new InvalidOperationException("Vui lòng đặt mật khẩu cho tài khoản mới.");

            return data.luuTaiKhoan(maNguoiSua, duLieu, banGoc);
        }

        public bool luuVaiTro(string maNguoiSua, ET_VAITRO duLieu, ET_VAITRO? banGoc)
        {
            duLieu.TenVaiTro = (duLieu.TenVaiTro ?? "").Trim();
            duLieu.MoTa = (duLieu.MoTa ?? "").Trim();
            if (duLieu.TenVaiTro.Length == 0 || duLieu.TenVaiTro.Length > 255)
                throw new InvalidOperationException("Tên vai trò phải có từ 1 đến 255 ký tự.");
            return data.luuVaiTro(maNguoiSua, duLieu, banGoc);
        }

        // Trạng thái bị khóa: tài khoản mất mọi quyền; khôi phục sử dụng lại MaVaiTro hiện có.
        public bool doiTrangThaiTaiKhoan(string maNguoiSua, string maTK,
            string trangThaiCu, string trangThaiMoi)
            => data.doiTrangThaiTaiKhoan(maNguoiSua, maTK, trangThaiCu, trangThaiMoi);

        private static string BamMatKhau(string matKhau)
        {
            const int soVong = 210000;
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(matKhau, salt, soVong,
                HashAlgorithmName.SHA256, 32);
            return $"pbkdf2_sha256${soVong}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }
    }
}
