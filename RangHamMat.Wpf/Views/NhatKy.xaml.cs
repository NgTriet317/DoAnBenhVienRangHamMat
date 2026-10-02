using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RangHamMat.Controls;
namespace RangHamMat.Views;
public partial class NhatKy : UserControl
{
    private static readonly HashSet<string> Allowed = new() { "Đăng nhập", "Đăng xuất", "Phân quyền", "Thay đổi thông tin" };
    public NhatKy() => InitializeComponent();
    public void SetEntries(IEnumerable entries)
    {
        AuditGrid.ItemsSource = new ListCollectionView(entries.Cast<object>().ToList());
        ApplyFilter();
    }
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void ApplyFilter()
    {
        if (AuditGrid?.ItemsSource is not ListCollectionView view) return;
        var selected = (ActivityFilter.SelectedItem as ComboBoxItem)?.Content as string;
        view.Filter = row => Allowed.Contains(UiRecord.Text(row, "HoatDong"))
            && (selected == "Tất cả hoạt động" || selected == UiRecord.Text(row, "HoatDong"));
    }
}
