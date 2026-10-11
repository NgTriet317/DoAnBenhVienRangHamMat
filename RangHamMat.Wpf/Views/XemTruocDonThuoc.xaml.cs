using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RangHamMat.Controls;
namespace RangHamMat.Views;

public partial class XemTruocDonThuoc : Window
{
    public XemTruocDonThuoc(RxCard card) { InitializeComponent(); DataContext = card; }
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
