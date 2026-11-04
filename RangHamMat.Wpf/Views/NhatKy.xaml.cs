// PHẦN AI ĐÃ THỰC HIỆN: AI đã giải thích nguồn dữ liệu nhật ký nhận từ bên ngoài và bộ lọc hiển thị; ghi rõ phần nào đang là dữ liệu giao diện.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RangHamMat.Controls;
namespace RangHamMat.Views;
/// <summary>
/// Màn hình nhật ký hoạt động: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class NhatKy : UserControl
{
    /// <summary>
    /// Quyền cho phép thao tác được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static readonly HashSet<string> Allowed = new() { "Đăng nhập", "Đăng xuất", "Phân quyền", "Thay đổi thông tin" };
    /// <summary>
    /// Khởi tạo nhật ký hoạt động từ XAML. Chưa có xử lý đọc/lưu SQL riêng trong constructor.
    /// </summary>
    public NhatKy() => InitializeComponent();
    /// <summary>
    /// Bọc nguồn nhật ký được truyền vào bằng ListCollectionView, đặt vào bảng và áp dụng bộ lọc hoạt động.
    /// </summary>
    public void SetEntries(IEnumerable entries)
    {
        AuditGrid.ItemsSource = new ListCollectionView(entries.Cast<object>().ToList());
        ApplyFilter();
    }
    /// <summary>
    /// Áp dụng lại bộ lọc nhật ký khi đổi lựa chọn loại hoạt động.
    /// </summary>
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    /// <summary>
    /// Chỉ hiển thị bốn nhóm hoạt động được khai báo và loại người dùng đang chọn; không tự ghi nhật ký đăng nhập hoặc lấy dữ liệu SQL.
    /// </summary>
    private void ApplyFilter()
    {
        if (AuditGrid?.ItemsSource is not ListCollectionView view) return;
        var selected = (ActivityFilter.SelectedItem as ComboBoxItem)?.Content as string;
        view.Filter = row => Allowed.Contains(UiRecord.Text(row, "HoatDong"))
            && (selected == "Tất cả hoạt động" || selected == UiRecord.Text(row, "HoatDong"));
    }
}
