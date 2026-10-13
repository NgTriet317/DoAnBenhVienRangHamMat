// PHẦN AI ĐÃ THỰC HIỆN: AI đã giải thích cách đưa dữ liệu đơn thuốc vào vùng xem trước và luồng in/xuất PDF qua hộp thoại in WPF.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RangHamMat.Controls;
namespace RangHamMat.Views;

/// <summary>
/// Màn hình xem trước/in đơn thuốc: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class XemTruocDonThuoc : Window
{
    /// <summary>
    /// Khởi tạo xem trước/in đơn thuốc từ XAML.
    /// </summary>
    public XemTruocDonThuoc(RxCard card) { InitializeComponent(); DataContext = card; }
    /// <summary>
    /// Mở hộp thoại chọn máy in; sau khi người dùng đồng ý thì in vùng đơn thuốc, có thể chọn Microsoft Print to PDF để xuất tệp.
    /// </summary>
    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;
        var oldTransform = PrintablePage.LayoutTransform;
        try
        {
            PrintablePage.UpdateLayout();
            var scale = Math.Min(dialog.PrintableAreaWidth / PrintablePage.ActualWidth, dialog.PrintableAreaHeight / PrintablePage.ActualHeight);
            PrintablePage.LayoutTransform = new ScaleTransform(Math.Min(1, scale), Math.Min(1, scale));
            PrintablePage.UpdateLayout();
            dialog.PrintVisual(PrintablePage, $"Đơn thuốc {(DataContext as RxCard)?.Code}");
            PrintStatus.Text = "Đã gửi đơn thuốc tới máy in đã chọn.";
        }
        catch (Exception exception) { PrintStatus.Text = "Không thể in đơn thuốc: " + exception.Message; }
        finally { PrintablePage.LayoutTransform = oldTransform; PrintablePage.UpdateLayout(); }
    }
}
