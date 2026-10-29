using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RangHamMat;
using RangHamMat.Controls;
using RangHamMat.Views;

internal static class UiAuditChecks
{
    private static readonly string Folder = Path.GetFullPath("XemTruoc/ra-soat-giao-dien");
    private static readonly List<string> Failures = new();
    public static int Run()
    {
        Directory.CreateDirectory(Folder);
        DuLieuGiaoDien.RefreshFromDatabase();
        var cases = new (string Id, string Title, Func<UserControl> Create, string? List)[] {
            ("bao-cao", "Báo cáo và thống kê", () => new BaoCao(), null),
            ("bao-tri", "Lịch sử bảo trì", () => new BaoTri(), "Grid_BAO_TRI_THIET_BI"),
            ("nha-cung-cap", "Nhà cung cấp", () => new NhaCungCap(), "Grid_NHA_CUNG_CAP"),
            ("benh-nhan", "Bệnh nhân", () => new BenhNhan(), "Grid_BENH_NHAN"),
            ("kham-dieu-tri", "Khám điều trị", () => new KhamDieuTri(), "VisitList"),
            ("dich-vu", "Dịch vụ thực hiện", () => new DichVuThucHien(), "Grid_DICH_VU_THUC_HIEN"),
            ("hoa-don", "Hóa đơn và thanh toán", () => new HoaDon(), "InvoiceList"),
            ("don-thuoc", "Đơn thuốc", () => new DonThuoc(), "PrescriptionList")
        };
        foreach (var item in cases)
        {
            var page = item.Create();
            var host = new Window { Width = 1366, Height = 768, ShowInTaskbar = false,
                Content = new AppWorkspace(new MainWindow(), item.Title, "Admin", page) };
            try
            {
                host.Show(); Drain(host);
                if (item.List != null && page.FindName(item.List) is System.Windows.Controls.Primitives.Selector list && list.Items.Count > 0)
                    list.SelectedIndex = 0;
                Drain(host);
                // Prefer a record with detail rows; fixture databases may also contain empty drafts.
                if (page is HoaDon or DonThuoc && page.FindName(item.List!) is Selector detailsList)
                {
                    for (var i = 0; i < detailsList.Items.Count; i++)
                    {
                        detailsList.SelectedIndex = i; Drain(host);
                        if (Visuals<DataGrid>(page).Any(g => g.Items.Count > 0)) break;
                    }
                }
                foreach (var size in new[] { (1366,768), (1024,768), (1920,1080) })
                {
                    host.Width = size.Item1; host.Height = size.Item2; Drain(host);
                    Capture(host, $"{item.Id}-{size.Item1}");
                    if (page.FindName("SearchBox") is TextBox search && search.ActualHeight > 44)
                        Failures.Add($"{item.Id}: single-line search is too tall ({search.ActualHeight:0}px).");
                    if (page is DichVuThucHien && page.FindName("Grid_DICH_VU_THUC_HIEN") is DataGrid services && services.ActualHeight < 130)
                        Failures.Add($"{item.Id}: selection hides the list ({services.ActualHeight:0}px).");
                    if (page is DichVuThucHien && page.FindName("ConfirmButton") is Button action && action.IsVisible)
                    {
                        var position = action.TransformToAncestor(page).Transform(new Point(0, action.ActualHeight));
                        if (position.Y > page.ActualHeight + 1) Failures.Add("Service actions fall outside the page.");
                    }
                    CheckTables(page, item.Id);
                    Console.WriteLine($"RENDER {item.Id} {size.Item1}: " + string.Join("; ", Visuals<DataGrid>(page).Where(g => g.IsVisible).Select(g => $"{g.Name}={g.ActualWidth:0}x{g.ActualHeight:0} ({g.Items.Count} rows)")));
                }
                host.Width = 1366; host.Height = 768; Drain(host);
                // Only select tabs and scroll: never invoke a business operation or save data.
                foreach (var tabs in Visuals<TabControl>(page).Where(t => t.IsVisible).ToArray())
                {
                    for (var i = 1; i < tabs.Items.Count; i++)
                    {
                        if (tabs.Items[i] is not TabItem { IsVisible: true, IsEnabled: true }) continue;
                        tabs.SelectedIndex = i; Drain(host);
                        CheckTables(page, item.Id + " tab " + i);
                        Capture(host, $"{item.Id}-tab-{i}");
                    }
                    tabs.SelectedIndex = 0; Drain(host);
                }
                if (page.FindName("InvoiceContent") is ScrollViewer scroll)
                { scroll.ScrollToBottom(); Drain(host); Capture(host, "hoa-don-thanh-toan"); }
                var searchName = page is BaoTri ? "txtTimKiem" : page is NhaCungCap ? "txtTimKiem" : "SearchBox";
                if (page.FindName(searchName) is TextBox filter)
                {
                    filter.Text = "__ui_audit_no_match__"; Drain(host);
                    Capture(host, item.Id + "-khong-co-ket-qua");
                    filter.Clear(); Drain(host);
                }
            }
            finally { host.Close(); }
        }
        foreach (var (form, table) in new (UserControl, string)[] { (new CuaSoBenhNhan(), "BENH_NHAN"), (new CuaSoKhamDieuTri(), "LICH_SU_KHAM_BENH"), (new CuaSoKhamDieuTri(), "TINH_TRANG_BENH"), (new CuaSoKhamDieuTri(), "KE_HOACH_DIEU_TRI"), (new CuaSoKhamDieuTri(), "CHI_TIET_KE_HOACH"), (new CuaSoKhamDieuTri(), "DICH_VU_THUC_HIEN"), (new CuaSoDonThuoc(), "DON_THUOC"), (new CuaSoHoaDon(), "HOA_DON"), (new CuaSoBaoTri(), "BAO_TRI_THIET_BI"), (new CuaSoNhaCungCap(), "NHA_CUNG_CAP") })
        {
            var existing = table is "KE_HOACH_DIEU_TRI" or "DICH_VU_THUC_HIEN" ? DuLieuGiaoDien.Rows(table).FirstOrDefault() : null;
            BieuMauHoSo.Configure(form, table, existing);
            var host = new Window { Width = 1024, Height = 768, ShowInTaskbar = false,
                Content = new AppWorkspace(new MainWindow(), "Kiểm tra biểu mẫu", "Admin", form) };
            try { host.Show(); Drain(host); CheckTables(form, table); Capture(host, "form-" + form.GetType().Name + "-" + table); }
            finally { host.Close(); }
        }
        if (Failures.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, Failures.Distinct()));
        Console.WriteLine("PASS: eight modules at three sizes, detail tabs, search empty states and ten form variants; table alignment and service list visibility; read-only SQL.");
        return 0;
    }
    private static void Drain(Window host)
    { host.UpdateLayout(); Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); host.UpdateLayout(); }
    private static void Capture(Window host, string name)
    {
        var content = (FrameworkElement)host.Content;
        var bitmap = new RenderTargetBitmap((int)content.ActualWidth, (int)content.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(Folder, name + ".png")); encoder.Save(stream);
    }
    private static void CheckTables(FrameworkElement page, string id)
    {
        foreach (var grid in Visuals<DataGrid>(page).Where(g => g.IsVisible && g.HeadersVisibility.HasFlag(DataGridHeadersVisibility.Column)))
        {
            if (grid.Items.Count > 0)
            {
                grid.ScrollIntoView(grid.Items[0], grid.Columns.Last()); grid.UpdateLayout();
                if (grid.ItemContainerGenerator.ContainerFromIndex(0) is DataGridRow row)
                    foreach (var button in Visuals<Button>(row).Where(b => b.IsVisible))
                    {
                        var bottom = button.TransformToAncestor(row).Transform(new Point(0, button.ActualHeight)).Y;
                        if (bottom > row.ActualHeight + 1.5) Failures.Add($"{id}: row action '{button.Content}' wraps outside its row.");
                    }
                grid.ScrollIntoView(grid.Items[0], grid.Columns.First()); grid.UpdateLayout();
                Visuals<ScrollViewer>(grid).FirstOrDefault()?.ScrollToLeftEnd(); grid.UpdateLayout();
            }
            foreach (var header in Visuals<DataGridColumnHeader>(grid).Where(h => h.Column is DataGridTextColumn))
            {
                var column = header.Column;
                if (column.ActualWidth < 40)
                    Failures.Add($"{id}: column '{column.Header}' compressed to {column.ActualWidth:0}px.");
                if (grid.Items.Count == 0 || column.GetCellContent(grid.Items[0]) is not TextBlock content) continue;
                var caption = Visuals<TextBlock>(header).FirstOrDefault(t => t.Text == Convert.ToString(column.Header));
                if (caption == null || string.IsNullOrEmpty(content.Text)) continue;
                var right = content.HorizontalAlignment == HorizontalAlignment.Right;
                var titleRect = right ? caption.ContentEnd.GetCharacterRect(LogicalDirection.Backward) : caption.ContentStart.GetCharacterRect(LogicalDirection.Forward);
                var valueRect = right ? content.ContentEnd.GetCharacterRect(LogicalDirection.Backward) : content.ContentStart.GetCharacterRect(LogicalDirection.Forward);
                var titleX = caption.TransformToAncestor(grid).Transform(new Point(right ? titleRect.Right : titleRect.Left, 0)).X;
                var valueX = content.TransformToAncestor(grid).Transform(new Point(right ? valueRect.Right : valueRect.Left, 0)).X;
                if (Math.Abs(titleX - valueX) > 1.5)
                    Failures.Add($"{id}: header/data '{column.Header}' misaligned ({titleX:0.0}/{valueX:0.0}).");
            }
        }
    }
    private static IEnumerable<T> Visuals<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        { var child = VisualTreeHelper.GetChild(root,i); if (child is T value) yield return value;
          foreach (var nested in Visuals<T>(child)) yield return nested; }
    }
}
