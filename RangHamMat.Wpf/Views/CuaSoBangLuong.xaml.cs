// PHẦN AI ĐÃ THỰC HIỆN: AI đã bổ sung chú thích tiếng Việt cho CuaSoBangLuong.xaml.cs: Màn hình biểu mẫu bảng lương: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình. File hiện chỉ khởi tạo giao diện, chưa có thao tác lưu SQL riêng. Giải thích được đặt cạnh khai báo/hàm tương ứng.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows.Controls;

namespace RangHamMat.Views;

/// <summary>
/// Màn hình biểu mẫu bảng lương: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình. File hiện chỉ khởi tạo giao diện, chưa có thao tác lưu SQL riêng.
/// </summary>
public partial class CuaSoBangLuong : UserControl
{
    /// <summary>
    /// Khởi tạo biểu mẫu bảng lương từ XAML. Chưa có xử lý đọc/lưu SQL riêng trong constructor.
    /// </summary>
    public CuaSoBangLuong() => InitializeComponent();
}
