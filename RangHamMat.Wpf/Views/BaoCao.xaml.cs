// PHẦN AI ĐÃ THỰC HIỆN: AI đã giải thích cách màn hình nhận số liệu báo cáo, gán nguồn bảng/biểu đồ và phối hợp ChartView được khai báo trong XAML.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
/// <summary>
/// Màn hình báo cáo: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class BaoCao : UserControl
{
    /// <summary>
    /// Khởi tạo báo cáo từ XAML. Chưa có xử lý đọc/lưu SQL riêng trong constructor.
    /// </summary>
    public BaoCao() => InitializeComponent();
    /// <summary>
    /// Chuyển giữa bảng báo cáo và vùng đồ thị theo Tag của RadioButton; các chế độ không tự tạo thêm dữ liệu.
    /// </summary>
    private void ViewMode_Changed(object sender, RoutedEventArgs e)
    {
        if (TableView is null || ChartsView is null) return;
        var charts = (sender as RadioButton)?.Tag as string == "Charts";
        TableView.Visibility = charts ? Visibility.Collapsed : Visibility.Visible;
        ChartsView.Visibility = charts ? Visibility.Visible : Visibility.Collapsed;
    }
    // Đây là đầu vào hiển thị; nơi gọi cần cung cấp các tổng báo cáo đã được tính và kiểm tra.
    /// <summary>
    /// Nhận các điểm tổng hợp do nơi gọi cung cấp và gán cho đồ thị bệnh viện/kho; không tự tính doanh thu hoặc tự truy vấn SQL.
    /// </summary>
    public void SetChartData(IEnumerable<ChartPoint> hospital, IEnumerable<ChartPoint> warehouse)
    {
        HospitalChart.Points = hospital.ToArray();
        WarehouseChart.Points = warehouse.ToArray();
    }
    // Nguồn báo cáo phải phân loại rõ khoản thu/chi; chuyển động hàng tồn không tự được coi là doanh thu.
    /// <summary>
    /// Nhận hai nguồn đã phân loại thu/chi và gán vào bảng báo cáo kho; không coi phiếu nhập/xuất hàng là doanh thu.
    /// </summary>
    public void SetWarehouseEntries(System.Collections.IEnumerable receipts, System.Collections.IEnumerable payments)
    {
        Grid_HOA_DON_KHO.ItemsSource = receipts;
        Grid_HOA_DON_KHO_Chi.ItemsSource = payments;
    }
}
