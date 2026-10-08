using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RangHamMat.Controls;

namespace RangHamMat;

public partial class AppWorkspace : UserControl
{
    private readonly MainWindow home;
    private readonly UserControl page;
    private readonly Stack<object> history = new();
    private readonly Dictionary<DataGrid, List<DataGridColumn>> extraColumns = new();

    public AppWorkspace(MainWindow home, string title, string role, UserControl page)
    {
        InitializeComponent();
        this.home = home;
        this.page = page;
        AppTitle.Text = title;
        RoleLabel.Text = role;
        Workspace.Content = page;
        AddHandler(Button.ClickEvent, new RoutedEventHandler(Navigate));
        AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(OpenDetails), true);
    }

    internal bool ReturnToPage()
    {
        if (history.Count == 0) return false;
        Workspace.Content = history.Pop();
        return true;
    }

    internal void ShowForm(UserControl form, string? table = null, CreationContext? context = null)
    {
        history.Push(Workspace.Content);
        Workspace.Content = form;
        var tableName = table?.Split('|')[0];
        if (form.FindName("EditorTabs") is TabControl tabs)
        {
            TabItem? selected = null;
            foreach (var tab in tabs.Items.OfType<TabItem>())
            {
                var visible = selected is null && (tableName is null || (string?)tab.Tag == "Table:" + tableName);
                tab.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                if (visible) selected = tab;
            }
            if (selected is not null) tabs.SelectedItem = selected;
            if (context is not null)
                foreach (var field in Elements(form).OfType<ComboBox>())
                    if ((string?)field.Tag == "DB:" + context.Table + "." + context.ParentField)
                    {
                        field.ItemsSource = new[] { context.ParentValue };
                        field.SelectedIndex = 0;
                        field.IsEnabled = false;
                        field.ToolTip = "Liên kết với bản ghi đang xem";
                    }
            if (table?.EndsWith("|Thuoc", StringComparison.Ordinal) == true)
            {
                // The medicine view uses the existing material type relation, never a new SQL field.
                var source = page.FindName("Grid_LOAI_VAT_TU") as DataGrid;
                var type = source?.Items.Cast<object>().FirstOrDefault(x => UiRecord.Text(x, "TenLVT").Trim().Equals("Thuốc", StringComparison.CurrentCultureIgnoreCase));
                foreach (var field in Elements(form).OfType<ComboBox>())
                    if ((string?)field.Tag == "DB:VAT_TU.MaLVT")
                    {
                        field.IsEnabled = false;
                        field.ToolTip = "Loại vật tư: Thuốc";
                        field.ItemsSource = new[] { type is null ? "Thuốc" : UiRecord.Text(type, "MaLVT") + " · Thuốc" };
                        field.SelectedIndex = 0;
                    }
            }
        }
    }

    private static IEnumerable<DependencyObject> Elements(DependencyObject parent)
    {
        yield return parent;
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
            foreach (var element in Elements(child)) yield return element;
    }

    private static readonly (string Parent, string Child, string Key, string Title)[] Relations =
    {
        ("BENH_NHAN", "HO_SO_BENH_AN", "MaBN", "Hồ sơ bệnh án"),
        ("LICH_SU_KHAM_BENH", "TINH_TRANG_BENH", "MaLS", "Tình trạng bệnh"),
        ("KE_HOACH_DIEU_TRI", "CHI_TIET_KE_HOACH", "MaKHDT", "Chi tiết kế hoạch điều trị"),
        ("DON_THUOC", "CHI_TIET_DON_THUOC", "MaDT", "Chi tiết đơn thuốc"),
        ("HOA_DON", "CHI_TIET_HOA_DON", "MaHD", "Chi tiết hóa đơn"),
        ("HOA_DON", "THANH_TOAN", "MaHD", "Các lần thanh toán"),
        ("GIAO_DICH_KHO", "HOA_DON_KHO", "MaGD", "Hóa đơn kho"),
        ("VAT_TU", "LO_VAT_TU", "MaVT", "Các lô hàng / lô thuốc")
    };

    private void OpenDetails(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ClickCount != 2) return;
        var current = e.OriginalSource as DependencyObject;
        while (current is not null && current is not DataGridRow)
            current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
        if (current is not DataGridRow row || row.Item == System.Windows.Data.CollectionView.NewItemPlaceholder
            || ItemsControl.ItemsControlFromItemContainer(row) is not DataGrid source
            || source.Tag is not string tag || !tag.StartsWith("Table:", StringComparison.Ordinal)) return;
        var table = tag[6..];
        // Nhân viên có cửa sổ chi tiết riêng được mở bởi MouseDoubleClick trên DataGrid.
        // Không thay nội dung workspace bằng RecordDetails cho bảng này.
        if (table == "NHAN_VIEN") return;

        var related = new List<RelatedRows>();
        foreach (var relation in Relations.Where(x => x.Parent == table))
        {
            if (page.FindName("Grid_" + relation.Child) is not DataGrid child) continue;
            var key = UiRecord.Text(row.Item, relation.Key);
            // A missing parent key must never expose unrelated records with empty foreign keys.
            var rows = child.Items.Cast<object>();
            if (page.FindName("Grid_" + relation.Child + "_Chi") is DataGrid payments)
                rows = rows.Concat(payments.Items.Cast<object>()).Distinct();
            var items = key.Length == 0 ? Array.Empty<object>() : rows
                .Where(x => x != System.Windows.Data.CollectionView.NewItemPlaceholder && UiRecord.Text(x, relation.Key) == key).ToArray();
            var module = relation.Child switch
            {
                "HO_SO_BENH_AN" => "BenhNhan",
                "TINH_TRANG_BENH" or "CHI_TIET_KE_HOACH" => "KhamDieuTri",
                "CHI_TIET_DON_THUOC" => "DonThuoc",
                "CHI_TIET_HOA_DON" or "THANH_TOAN" => "HoaDon",
                "HOA_DON_KHO" => "NhapXuatVatTu",
                "LO_VAT_TU" => "VatTu",
                _ => ""
            };
            var parentValue = UiRecord.Get(row.Item, relation.Key);
            var creation = key.Length == 0 || parentValue is null ? null : new CreationContext(relation.Child, relation.Key, parentValue);
            related.Add(new RelatedRows(relation.Title, module, child, items, creation));
        }
        e.Handled = true;
        history.Push(Workspace.Content);
        Workspace.Content = new RecordDetails(source, row.Item, related);
    }

    private void Navigate(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Button { Tag: string tag } button) return;

        if (tag == "ToggleColumns" && button.CommandParameter is string table
            && page.FindName("Grid_" + table) is DataGrid grid)
        {
            e.Handled = true;
            if (!extraColumns.TryGetValue(grid, out var columns))
            {
                columns = grid.Columns.Where(column => column.Visibility == Visibility.Collapsed).ToList();
                extraColumns[grid] = columns;
            }
            if (columns.Count == 0) return;
            var show = columns[0].Visibility == Visibility.Collapsed;
            foreach (var column in columns) column.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            button.Content = show ? "Thu gọn cột" : "Hiện đủ cột";
        }
        else if (tag == "VeTrangChu")
        {
            e.Handled = true;
            home.ShowHome();
        }
        else if (tag == "DongCuaSo")
        {
            e.Handled = true;
            ReturnToPage();
        }
        else if (tag.StartsWith("MoTrang:", StringComparison.Ordinal))
        {
            e.Handled = true;
            home.OpenApp(tag["MoTrang:".Length..]);
        }
        else if (tag.StartsWith("MoCuaSo:", StringComparison.Ordinal))
        {
            e.Handled = true;
            var context = button.CommandParameter as CreationContext;
            home.OpenModuleWindow(tag["MoCuaSo:".Length..], this, context?.Table ?? button.CommandParameter as string, context);
        }
    }
}
