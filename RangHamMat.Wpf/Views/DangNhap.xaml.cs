// AI bổ sung kiểm tra nhập liệu: chặn thiếu tên/mật khẩu ở giao diện mẫu; chưa triển khai xác thực SQL.
// PHẦN AI ĐÃ THỰC HIỆN: AI đã bổ sung giải thích sự kiện đăng nhập, DialogResult và điều kiện đóng cửa sổ; ghi rõ phạm vi kiểm tra đăng nhập đang có trong code.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;

namespace RangHamMat.Views;

/// <summary>
/// Màn hình hộp thoại đăng nhập: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class DangNhap : Window
{
    /// <summary>
    /// Khởi tạo hộp thoại đăng nhập từ XAML. Chưa có xử lý đọc/lưu SQL riêng trong constructor.
    /// </summary>
    public DangNhap() => InitializeComponent();

    /// <summary>
    /// Đặt DialogResult=true để App mở cửa sổ chính. Code hiện tại chưa kiểm tra tên đăng nhập/mật khẩu với bảng TAI_KHOAN.
    /// </summary>
    private void DangNhap_Click(object sender, RoutedEventArgs e)
    {
        if(!TryValidateInputs(out _)) return;
        DialogResult = true;
    }
    /// <summary>AI tạo: chặn tên đăng nhập trống/quá dài và mật khẩu trống, hiện lỗi và giữ các ô nhập; bước này chưa xác thực SQL.</summary>
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
        FormError.Text=error; return error.Length==0;
    }

    /// <summary>
    /// Đặt DialogResult=false để đóng hộp thoại; App nhận kết quả này và kết thúc ứng dụng.
    /// </summary>
    private void Thoat_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

}
