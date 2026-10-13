// PHẦN AI ĐÃ THỰC HIỆN: AI đã chú thích mục đích form và các handler hiện có, gồm những thao tác chưa có xử lý lưu dữ liệu.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows.Controls;

namespace RangHamMat.Views;

/// <summary>
/// Màn hình phiếu hướng dẫn bệnh nhân: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class PhieuHuongDan : UserControl
{
    /// <summary>
    /// Khởi tạo phiếu hướng dẫn bệnh nhân từ XAML. Chưa có xử lý đọc/lưu SQL riêng trong constructor.
    /// </summary>
    public PhieuHuongDan() => InitializeComponent();

    /// <summary>
    /// Handler hiện chưa có xử lý; thay lựa chọn không tự đọc/lưu phiếu hướng dẫn tại SQL.
    /// </summary>
    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }
}
