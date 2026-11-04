// Đăng nhập và xác thực tài khoản từ SQL Server.
// PHẦN AI ĐÃ THỰC HIỆN: AI đã bổ sung giải thích sự kiện đăng nhập, DialogResult và điều kiện đóng cửa sổ; ghi rõ phạm vi kiểm tra đăng nhập đang có trong code.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Security.Cryptography;
using BUS;

namespace RangHamMat.Views;

/// <summary>
/// Màn hình hộp thoại đăng nhập: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class DangNhap : Window
{
    /// <summary>
    /// Khởi tạo hộp thoại đăng nhập từ XAML.
    /// </summary>
    public DangNhap() => InitializeComponent();

    /// <summary>
    /// Chỉ mở cửa sổ chính khi thông tin đăng nhập khớp TAI_KHOAN.
    /// </summary>
    private void DangNhap_Click(object sender, RoutedEventArgs e)
    {
        if (!TryValidateInputs(out _)) return;
        try
        {
            var taiKhoan = new BUS_TAIKHOAN().layToanBoTaiKhoan()
                .FirstOrDefault(x => x.TenDangNhap.Equals(UsernameBox.Text.Trim(), StringComparison.OrdinalIgnoreCase));
            if (taiKhoan == null || !taiKhoan.TrangThai.Equals("Đang hoạt động", StringComparison.OrdinalIgnoreCase)
                || !KiemTraMatKhau(PasswordBox.Password, taiKhoan.MatKhauHash))
            {
                FormError.Text = "Tên đăng nhập, mật khẩu không đúng hoặc tài khoản bị khóa.";
                return;
            }

            var vaiTro = new BUS_VAITRO().layVaiTroTheoMa(taiKhoan.MaVaiTro);
            if (vaiTro == null)
            {
                FormError.Text = "Tài khoản chưa được cấp vai trò hợp lệ.";
                return;
            }
            string tenVaiTro = vaiTro.TenVaiTro.Trim();
            bool admin = tenVaiTro.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                      || tenVaiTro.Equals("Quản trị", StringComparison.OrdinalIgnoreCase);
            if (!admin && !tenVaiTro.Equals("User", StringComparison.OrdinalIgnoreCase))
            {
                FormError.Text = "Vai trò chưa được hỗ trợ.";
                return;
            }
            PhienDangNhap.MaNV = taiKhoan.MaNV;
            PhienDangNhap.TenDangNhap = taiKhoan.TenDangNhap;
            PhienDangNhap.IsAdmin = admin;
            PhienDangNhap.KhoiTaoDinhDanhNghiepVu(taiKhoan.MaTK);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            FormError.Text = "Không thể đăng nhập: " + ex.Message;
        }
    }
    /// <summary>AI tạo: chặn tên đăng nhập trống/quá dài và mật khẩu trống, hiện lỗi và giữ các ô nhập; Sau đó xác thực SQL trong sự kiện đăng nhập.</summary>
    public bool TryValidateInputs(out string error)
    {
        error="";
        RangHamMat.Controls.KiemTraNhapLieu.SetError(UsernameBox,"");
        RangHamMat.Controls.KiemTraNhapLieu.SetError(PasswordBox,"");
        if(string.IsNullOrWhiteSpace(UsernameBox.Text)||UsernameBox.Text.Trim().Length>100)
        {
            error="Nhập tên đăng nhập, tối đa 100 ký tự.";
            RangHamMat.Controls.KiemTraNhapLieu.SetError(UsernameBox,error); UsernameBox.Focus();
        }
        else if(PasswordBox.Password.Length==0)
        {
            error="Vui lòng nhập mật khẩu.";
            RangHamMat.Controls.KiemTraNhapLieu.SetError(PasswordBox,error); PasswordBox.Focus();
        }
        RangHamMat.Controls.KiemTraNhapLieu.ShowError(this,error); return error.Length==0;
    }

    /// <summary>
    /// Đặt DialogResult=false để đóng hộp thoại; App nhận kết quả này và kết thúc ứng dụng.
    /// </summary>
    private void Thoat_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    // Định dạng mật khẩu SQL: pbkdf2_sha256$210000$base64(salt)$base64(hash).
    private static bool KiemTraMatKhau(string matKhau, string hashDaLuu)
    {
        try
        {
            string[] parts = hashDaLuu.Split('$');
            if (parts.Length != 4 || parts[0] != "pbkdf2_sha256") return false;
            int soVong = int.Parse(parts[1]);
            if (soVong < 10000 || soVong > 1000000) return false;
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] mongDoi = Convert.FromBase64String(parts[3]);
            byte[] thucTe = Rfc2898DeriveBytes.Pbkdf2(matKhau, salt, soVong,
                HashAlgorithmName.SHA256, mongDoi.Length);
            return CryptographicOperations.FixedTimeEquals(mongDoi, thucTe);
        }
        catch { return false; }
    }

}
