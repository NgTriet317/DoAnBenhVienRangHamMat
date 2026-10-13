// PHẦN AI ĐÃ THỰC HIỆN: AI đã giải thích nạp nguồn ca làm, đổi chế độ bảng/lịch và cập nhật ScheduleView khai báo trong XAML.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
namespace RangHamMat.Views;
/// <summary>
/// Màn hình lịch ca làm việc: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class CaLamViec : UserControl
{
    /// <summary>
    /// Khởi tạo lịch ca làm việc từ XAML. Chưa có xử lý đọc/lưu SQL riêng trong constructor.
    /// </summary>
    public CaLamViec() => InitializeComponent();
    /// <summary>
    /// Chuyển giữa bảng dữ liệu và lịch tuần ca làm theo Tag RadioButton; gọi Schedule.Refresh khi mở chế độ lịch.
    /// </summary>
    private void ViewMode_Changed(object sender, RoutedEventArgs e)
    {
        if (TableView is null || Schedule is null) return;
        var calendar = (sender as RadioButton)?.Tag as string == "Schedule";
        TableView.Visibility = calendar ? Visibility.Collapsed : Visibility.Visible;
        Schedule.Visibility = calendar ? Visibility.Visible : Visibility.Collapsed;
        if (calendar) Schedule.Refresh();
    }
}
