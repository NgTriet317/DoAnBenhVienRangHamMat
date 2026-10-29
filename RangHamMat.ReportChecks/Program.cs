using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RangHamMat;
using RangHamMat.Controls;
using RangHamMat.Views;
using Dapper;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/RangHamMat.Wpf;component/Themes/Theme.xaml", UriKind.Relative) });
            KiemTraNhapLieu.Register();
            PhienDangNhap.IsAdmin = true;
            PhienDangNhap.TenDangNhap = "kiem-tra-bao-cao";
            if (args.Contains("--ui-audit")) return UiAuditChecks.Run();
            CheckRequestedRevision();
            CheckAggregation();
            var page = new BaoCao(_ => new[] { new PhatSinhBaoCao(new DateTime(2026, 10, 19), 4, 2, 1, 0, "Chờ khám") });
            Require(page.FindName("ReportCatalog") is ItemsControl,
                "Báo cáo phải có danh mục chuyên đề thay cho các tab bảng tách rời.");
            Require(page.FindName("ExportButton") is Button { IsEnabled: false },
                "Chưa tải dữ liệu thì không được xuất.");
            foreach (var item in BaoCaoDuLieu.DanhMuc)
            {
                page.ChonBaoCao(item.Ma);
                Require(Find<TextBlock>(page, "ReportTitle").Text == item.Ten, "Đổi chuyên đề phải đổi đúng tiêu đề.");
                Require(Find<DataGrid>(page, "ReportGrid").Items.Count == 0 && !Find<Button>(page, "ExportButton").IsEnabled, "Chuyển chuyên đề phải xóa kết quả cũ và khóa xuất.");
            }
            Require(BaoCaoDuLieu.DanhMuc.Count == 18 && BaoCaoDuLieu.DanhMuc.Select(r => r.Nhom).Distinct().Count() == 4, "Danh mục phải có 18 chuyên đề / 4 nhóm.");
            page.ChonBaoCao("02"); SetDates(page, new(2026, 10, 18), new(2026, 10, 20));
            Complete(page.TaiBaoCaoAsync());
            Require(Find<DataGrid>(page, "ReportGrid").Items.Count == 3 && Find<Button>(page, "ExportButton").IsEnabled, "Tải phải có đủ các ngày, bao gồm ngày không phát sinh.");
            Find<DatePicker>(page, "FromDate").SelectedDate = new DateTime(2026, 10, 21);
            Require(Find<DataGrid>(page, "ReportGrid").Items.Count == 0 && !Find<Button>(page, "ExportButton").IsEnabled, "Đổi ngày phải xóa bảng và khóa xuất ngay.");
            Complete(page.TaiBaoCaoAsync());
            Require(Find<TextBlock>(page, "FilterError").Visibility == Visibility.Visible, "Khoảng ngày ngược phải báo lỗi.");
            Find<TextBox>(page, "ReportSearch").Text = "thu hoan";
            Require(Find<ItemsControl>(page, "ReportCatalog").Items.Cast<NhomBaoCao>().SelectMany(g => g.CacMuc).Single().ChuyenDe.Ma == "09", "Tìm không dấu phải tìm được Thu – hoàn.");
            Find<TextBox>(page, "ReportSearch").Text = "khongcochuyendenay";
            Require(Find<TextBlock>(page, "NoSearchResults").Visibility == Visibility.Visible, "Tìm rỗng phải có hướng dẫn.");
            CheckStaleResponse(); CheckAccess(); CheckErrors();
            if (args.Contains("--sql")) CheckDatabase();
            if (args.Contains("--render")) CheckRender(args.Contains("--sql"));
            Console.WriteLine("PASS: 18 chuyên đề / 4 nhóm; một điểm mỗi ngày/tuần/tháng; tìm không dấu; thu ròng âm; CSV; quyền; lỗi; phản hồi cũ không ghi đè.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL: " + ex); return 1; }
        finally { app.Shutdown(); }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    private static void CheckRequestedRevision()
    {
        var page = new BaoCao(_ => new[] { new PhatSinhBaoCao(new DateTime(2026, 10, 1), 100, 0, 100, 1) });
        page.ChonBaoCao("09"); SetDates(page, new DateTime(2026, 9, 1), new DateTime(2026, 10, 20));
        Complete(page.TaiBaoCaoAsync());
        Require(Find<ChartView>(page, "ReportChart").Points!.Count() == 50,
            "Chế độ ngày phải vẽ đúng 50 ngày riêng biệt, không gộp thành 8 cột.");
        Require(BaoCaoDuLieu.DanhMuc.Count == 18 && BaoCaoDuLieu.DanhMuc.All(r => r.Nhom is not ("Tổng hợp" or "Hệ thống")),
            "Phải bỏ nhóm Tổng hợp và Hệ thống, giữ 18 chuyên đề nghiệp vụ.");
        Require(BaoCaoDuLieu.DanhMuc.All(r => r.DaKetNoi), "Mọi chuyên đề còn lại phải có nguồn database hoạt động.");
        foreach (var mode in new[]{"Ngày", "Tuần", "Tháng"})
        {
            Find<ComboBox>(page, "PeriodMode").SelectedIndex = mode == "Ngày" ? 0 : mode == "Tuần" ? 1 : 2;
            Complete(page.TaiBaoCaoAsync());
            var rows = Find<DataGrid>(page, "ReportGrid").Items.Cast<DongBaoCao>().ToArray();
            var points = Find<ChartView>(page, "ReportChart").Points!.ToArray();
            Require(points.Length == rows.Length && points.Select((p,i) => p.Primary == (double)rows[i].Mot && p.Secondary == (double)rows[i].Hai).All(b => b),
                "Từng điểm phải khớp đúng một kỳ của bảng, không cộng gộp: " + mode);
            Require(points.All(p => !string.IsNullOrWhiteSpace(p.PeriodDescription)), "Tooltip phải có kỳ và khoảng ngày thực tế.");
            Require(Find<TextBlock>(page,"TrendTitle").Text == "Xu hướng theo " + mode.ToLowerInvariant(), "Tiêu đề phải phân biệt ngày/tuần/tháng.");
        }
    }

    private static T Find<T>(FrameworkElement page, string name) where T : class => page.FindName(name) as T ?? throw new InvalidOperationException("Thiếu control " + name);
    private static void SetDates(BaoCao page, DateTime from, DateTime to)
    { Find<DatePicker>(page, "FromDate").SelectedDate = from; Find<DatePicker>(page, "ToDate").SelectedDate = to; }
    private static void Complete(Task task)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!task.IsCompleted && stopwatch.Elapsed < TimeSpan.FromSeconds(30))
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        Require(task.IsCompleted, "Tải báo cáo không hoàn tất trong 30 giây.");
        task.GetAwaiter().GetResult();
    }
    private static void CheckAggregation()
    {
        var from = new DateTime(2026, 10, 18); var to = new DateTime(2026, 10, 20);
        var events = new[]
        {
            new PhatSinhBaoCao(from.AddDays(-1), 999, 0, 999, 1),
            new PhatSinhBaoCao(from, 100, 0, 100, 1, "Thu · Tiền mặt", 100),
            new PhatSinhBaoCao(to.AddHours(23).AddMinutes(59), 0, 150, -150, 1, "Hoàn · Tiền mặt", 150),
            new PhatSinhBaoCao(to.AddDays(1), 999, 0, 999, 1)
        };
        foreach (var mode in new[] { "Ngày", "Tuần", "Tháng" })
        {
            var result = BaoCaoDuLieu.TongHop("09", events, from, to, mode);
            Require(result.Tong.Mot == 100 && result.Tong.Hai == 150 && result.Tong.Ba == -50 && result.Tong.Bon == 2, "Không được lấy thêm ngày ngoài khoảng khi nhóm kỳ.");
            Require(result.CacKy.Sum(r => r.Ba) == -50, "Thu ròng âm phải được giữ nguyên trong bảng.");
            if (mode != "Ngày") Require(result.CacKy.All(r => !r.Ky.Contains("12/10/2026") && !r.Ky.Contains("25/10/2026")), "Nhãn phải chỉ rõ tuần/tháng chỉ chứa một phần khoảng.");
        }
        var report = BaoCaoDuLieu.DanhMuc.Single(r => r.Ma == "09");
        var data = BaoCaoDuLieu.TongHop("09", events, from, to, "Ngày");
        var csv = BaoCaoDuLieu.TaoCsv(report, data, from, to, "Ngày", "=cmd", from);
        Require(csv.Contains("Thu – hoàn bệnh viện") && csv.Contains("-50") && csv.Contains("'=cmd") && csv.Contains("Tiền hoàn xác nhận (VNĐ)"), "CSV phải đúng chuyên đề, giữ số âm và chặn công thức từ chuỗi.");
        var empty = BaoCaoDuLieu.TongHop("09", Array.Empty<PhatSinhBaoCao>(), from, to, "Ngày");
        Require(empty.CacKy.Length == 3 && empty.CacKy.All(r => r.Mot == 0), "Không phát sinh vẫn phải có đủ từng ngày bằng 0.");
        Require(BaoCaoDuLieu.TongHop("09", events, from, to, "Tuần").CacKy.Length == 2, "18–20/10 cắt qua hai tuần phải có đúng hai kỳ.");
        Require(BaoCaoDuLieu.TongHop("09", events, from, to, "Tháng").CacKy.Single().Ky == "Tháng 10/2026", "Kỳ tháng phải có nhãn tháng rõ ràng.");
        var item = new ChiTietBaoCao("HH1", "Hàng thử", "", "hộp", null, null, 2, 0, 10, null, "", "");
        var inventory = BaoCaoDuLieu.TongHop("13", new[]{new PhatSinhBaoCao(from,1,0,10,0,ChiTiet:item),new PhatSinhBaoCao(to,1,0,10,0,ChiTiet:item)}, from,to,"Ngày");
        Require(inventory.Tong.Bon == 1 && inventory.CacKy.Sum(r=>r.Bon) == 2 && inventory.ChiTiet.Single().Mot == 4,
            "Một mặt hàng phát sinh hai ngày phải đếm tổng một mặt hàng; chi tiết cộng cùng mã và đơn vị.");
    }
    private static void CheckStaleResponse()
    {
        using var started = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        var page = new BaoCao(_ => { started.Set(); release.Wait(TimeSpan.FromSeconds(10)); return new[] { new PhatSinhBaoCao(DateTime.Today, 123, 0, 0, 0) }; });
        var task = page.TaiBaoCaoAsync();
        Require(started.Wait(TimeSpan.FromSeconds(5)), "Nguồn chưa được gọi.");
        page.ChonBaoCao("09"); release.Set(); Complete(task);
        Require(Find<DataGrid>(page, "ReportGrid").Items.Count == 0 && !Find<Button>(page, "ExportButton").IsEnabled, "Phản hồi chuyên đề cũ không được ghi vào chuyên đề mới.");
    }
    private static void CheckAccess()
    {
        PhienDangNhap.IsAdmin = false;
        try
        {
            var called = false; var page = new BaoCao(_ => { called = true; return Array.Empty<PhatSinhBaoCao>(); });
            Complete(page.TaiBaoCaoAsync());
            Require(!called && Find<ItemsControl>(page, "ReportCatalog").Items.Count == 0, "Tài khoản không quyền không được gọi nguồn hoặc xem danh mục.");
            var blocked = false; try { BaoCaoDuLieu.Doc("09"); } catch (UnauthorizedAccessException) { blocked = true; }
            Require(blocked, "Nguồn dữ liệu phải kiểm tra quyền trước khi đọc BUS.");
        }
        finally { PhienDangNhap.IsAdmin = true; }
    }
    private static void CheckErrors()
    {
        var page = new BaoCao(_ => throw new InvalidOperationException("Nguồn đang tạm ngừng"));
        Complete(page.TaiBaoCaoAsync());
        Require(Find<TextBlock>(page, "PageStatus").Text.Contains("Nguồn đang tạm ngừng") && !Find<Button>(page, "ExportButton").IsEnabled, "Lỗi phải rõ ràng và không cho xuất dữ liệu cũ.");
        page.ChonBaoCao("14");
        Require(Find<WrapPanel>(page, "SnapshotFields").Visibility == Visibility.Visible && Find<WrapPanel>(page, "DateRangeFields").Visibility == Visibility.Collapsed, "Hạn dùng phải có ngày chốt và ngưỡng cảnh báo riêng.");
        var disconnected = new BaoCao(_ => throw new ReportDbException());
        Complete(disconnected.TaiBaoCaoAsync());
        Require(Find<TextBlock>(disconnected, "PageStatus").Text.Contains("máy chủ dữ liệu") && !Find<Button>(disconnected, "ExportButton").IsEnabled,
            "Lỗi SQL trực tiếp phải thành trạng thái thử lại, không thoát ứng dụng.");
    }
    private static void CheckRender(bool sql)
    {
        using var log = new StringWriter(); using var listener = new TextWriterTraceListener(log);
        PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
        var page = new BaoCao();
        var workspace = new AppWorkspace(new MainWindow(), "Báo cáo và thống kê", "Admin", page);
        var host = new Window { Width = 1366, Height = 768, ShowInTaskbar = false, Content = workspace, Title = "Kiểm tra báo cáo và thống kê" };
        try
        {
            host.Show(); Drain(host); Capture(host, "bao-cao-1366-chua-tai");
            Require(Find<Button>(page, "LoadButton").IsVisible && Find<Button>(page, "ExportButton").IsVisible, "1366×768 phải thấy nút tải và xuất.");
            if (sql)
            {
                SetDates(page, new DateTime(2026, 9, 1), new DateTime(2026, 10, 20));
                foreach (var report in BaoCaoDuLieu.DanhMuc.Where(r => r.DaKetNoi))
                {
                    page.ChonBaoCao(report.Ma); Complete(page.TaiBaoCaoAsync()); Drain(host);
                    Require(Find<Button>(page, "ExportButton").IsEnabled, "Không tải được SQL chuyên đề " + report.Ma + ": " + Find<TextBlock>(page, "PageStatus").Text);
                    var rows = Find<DataGrid>(page, "ReportGrid").Items.Cast<DongBaoCao>().ToArray();
                    var points = Find<ChartView>(page, "ReportChart").Points!.ToArray();
                    if (!report.TheoThoiDiem)
                    {
                        Require(points.Length == rows.Length && points.Select((p,i)=>p.Primary == (double)rows[i].Mot && p.Secondary == (double)rows[i].Hai).All(b=>b),
                            "Mỗi điểm phải khớp một kỳ trong bảng cho chuyên đề " + report.Ma);
                    }
                    else Require(Find<DataGrid>(page,"DetailsGrid").Items.Count > 0, "Hạn dùng phải có danh sách lô thật từ SQL.");
                }
                page.ChonBaoCao("02"); Complete(page.TaiBaoCaoAsync()); Drain(host); Capture(host, "bao-cao-1366-ho-so");
                host.Width = 1920; host.Height = 1080; Drain(host); Capture(host, "bao-cao-1920-ho-so");
                SetDates(page, new DateTime(2026,10,1), new DateTime(2026,10,20));
                page.ChonBaoCao("09"); Complete(page.TaiBaoCaoAsync()); Drain(host); Capture(host, "bao-cao-1920-thu-hoan");
                foreach(var mode in new[]{0,1,2})
                {
                    Find<ComboBox>(page,"PeriodMode").SelectedIndex=mode; Complete(page.TaiBaoCaoAsync()); Drain(host);
                    Capture(host,"bao-cao-theo-" + new[]{"ngay","tuan","thang"}[mode]);
                    if(mode==0)
                    {
                        var scroll=Find<ScrollViewer>(page,"TrendScroll"); scroll.ScrollToRightEnd(); Drain(host);
                        Require(scroll.ScrollableWidth>0 && scroll.HorizontalOffset>0,"Biểu đồ ngày dài phải cuộn tới các ngày cuối được.");
                        Capture(host,"bao-cao-theo-ngay-cuoi-ky"); scroll.ScrollToLeftEnd();
                    }
                }
                Find<ComboBox>(page,"PeriodMode").SelectedIndex=0; Complete(page.TaiBaoCaoAsync());
                host.Width = 1366; host.Height = 768; Drain(host); Capture(host, "bao-cao-1366-thu-hoan");
                Find<ScrollViewer>(page, "ReportScroll").ScrollToBottom(); Drain(host); Capture(host, "bao-cao-1366-bang-doi-chieu");
                CheckColumnAlignment(Find<DataGrid>(page,"ReportGrid"));
                foreach(var ma in new[]{"11","13","14","18"})
                {
                    page.ChonBaoCao(ma); SetDates(page,new DateTime(2026,9,1),new DateTime(2026,10,20));
                    Complete(page.TaiBaoCaoAsync()); Drain(host); Find<Border>(page,"DetailsPanel").BringIntoView(); Drain(host);
                    Capture(host,"bao-cao-chi-tiet-"+ma);
                }
            }
            host.Width = 1024; host.Height = 768; Drain(host);
            Require(page.KpiColumns == 2, "Cửa sổ hẹp phải chuyển KPI sang hai cột.");
            Require(Visuals(Find<ItemsControl>(page, "MetricItems")).OfType<UniformGrid>().Single().Columns == 2, "Binding panel KPI thực tế phải có hai cột.");
            Require(Grid.GetRow(Find<Border>(page, "CompositionPanel")) == 2, "Cửa sổ hẹp phải xếp biểu đồ dọc.");
            Find<ScrollViewer>(page, "ReportScroll").ScrollToTop(); Drain(host); Capture(host, "bao-cao-1024");
            page.ChonBaoCao("14"); Drain(host); Capture(host, "bao-cao-1024-han-dung");
            CheckInvalidTypedDate(host);
            Require(!log.ToString().Contains("Error", StringComparison.OrdinalIgnoreCase), "Binding báo cáo lỗi: " + log);
            Console.WriteLine("PASS: render 1366×768, Full HD khi dùng --sql, 1024×768; binding và bố cục co giãn.");
        }
        finally { host.Close(); PresentationTraceSources.DataBindingSource.Listeners.Remove(listener); }
    }
    private static void Drain(Window host)
    { host.UpdateLayout(); host.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); host.UpdateLayout(); }
    private static void Capture(FrameworkElement control, string name)
    {
        var folder = Path.Combine(Environment.CurrentDirectory, "XemTruoc", "bao-cao-thong-ke"); Directory.CreateDirectory(folder);
        var bitmap = new RenderTargetBitmap((int)control.ActualWidth, (int)control.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        var content = control is Window window ? (FrameworkElement)window.Content : control;
        bitmap = new RenderTargetBitmap((int)content.ActualWidth, (int)content.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(folder, name + ".png")); encoder.Save(stream);
    }
    private static void CheckInvalidTypedDate(Window host)
    {
        var called = 0; var page = new BaoCao(_ => { called++; return Array.Empty<PhatSinhBaoCao>(); });
        host.Content = page; Drain(host);
        var picker = Find<DatePicker>(page, "FromDate");
        picker.Text = "31/02/2026";
        Complete(page.TaiBaoCaoAsync());
        Require(called == 0 && Find<TextBlock>(page, "FilterError").Visibility == Visibility.Visible, "Ngày không tồn tại bị WPF hoàn tác vẫn phải bị chặn, không dùng ngày cũ.");
        picker.SelectedDate = new DateTime(2026, 10, 19); Find<DatePicker>(page, "ToDate").SelectedDate = new DateTime(2026, 10, 20);
        Complete(page.TaiBaoCaoAsync()); Require(called == 1, "Sửa ngày đúng phải tải lại được.");
    }
    private static IEnumerable<DependencyObject> Visuals(DependencyObject root)
    { yield return root; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) foreach (var child in Visuals(VisualTreeHelper.GetChild(root, i))) yield return child; }
    private sealed class ReportDbException : System.Data.Common.DbException { }

    private static void CheckColumnAlignment(DataGrid grid)
    {
        var headers = Visuals(grid).OfType<DataGridColumnHeader>().Where(h=>h.Column is not null).ToArray();
        Require(headers.Length == grid.Columns.Count, "Bảng phải render đủ header để kiểm tra căn cột.");
        foreach(var header in headers)
        {
            var caption=Visuals(header).OfType<TextBlock>().First(t=>t.Text==Convert.ToString(header.Column.Header));
            var content=(TextBlock)header.Column.GetCellContent(grid.Items[0]);
            var numeric=header.Column.DisplayIndex>0;
            var titleRect=numeric ? caption.ContentEnd.GetCharacterRect(System.Windows.Documents.LogicalDirection.Backward) : caption.ContentStart.GetCharacterRect(System.Windows.Documents.LogicalDirection.Forward);
            var valueRect=numeric ? content.ContentEnd.GetCharacterRect(System.Windows.Documents.LogicalDirection.Backward) : content.ContentStart.GetCharacterRect(System.Windows.Documents.LogicalDirection.Forward);
            var titleX=caption.TransformToAncestor(grid).Transform(new Point(numeric ? titleRect.Right : titleRect.Left,0)).X;
            var valueX=content.TransformToAncestor(grid).Transform(new Point(numeric ? valueRect.Right : valueRect.Left,0)).X;
            Require(Math.Abs(titleX-valueX)<=1.5,
                $"Header và dữ liệu cột '{header.Column.Header}' bị lệch: header {titleX:N1}px, dữ liệu {valueX:N1}px.");
        }
    }

    private static void CheckDatabase()
    {
        var from = new DateTime(2026,9,1); var to = new DateTime(2026,10,20);
        var args = new { Tu=from, Den=to.AddDays(1) };
        using var connection = new DAL.dbconnect().MoKetNoi();
        KetQuaBaoCao Load(string ma) => BaoCaoDuLieu.TongHop(ma,BaoCaoDuLieu.Doc(ma,from,to),from,to,"Ngày");
        var salary=Load("11");
        var pay=connection.QuerySingle<(decimal Luong,decimal HoaHong,decimal Tong,int Ca)>("""
            SELECT COALESCE(SUM(LuongTheoCa),0), COALESCE(SUM(TienHoaHong),0), COALESCE(SUM(TongLuong),0),COALESCE(SUM(SoCaTinh),0)
            FROM BANG_LUONG WHERE TrangThai=N'Đã chốt' AND NgayChot>=@Tu AND NgayChot<@Den
            """,args);
        Require(salary.Tong.Mot==pay.Luong && salary.Tong.Hai==pay.HoaHong && salary.Tong.Ba==pay.Tong && salary.Tong.Bon==pay.Ca,
            "Lương/hoa hồng phải trùng SELECT độc lập chỉ tính bảng đã chốt.");
        Require(salary.ChiTiet.Sum(r=>r.Ba)==pay.Tong,"Chi tiết lương phải cộng đúng quỹ lương.");
        var inventory=Load("13");
        var distinct=connection.ExecuteScalar<int>("""
            SELECT COUNT(DISTINCT MaHH) FROM (
              SELECT c.MaHH FROM CHI_TIET_PHIEU_NHAP c JOIN PHIEU_NHAP p ON p.MaPN=c.MaPN
              WHERE p.TrangThai=N'Đã ghi sổ' AND p.NgayNhap>=@Tu AND p.NgayNhap<@Den
              UNION ALL
              SELECT c.MaHH FROM CHI_TIET_PHIEU_XUAT c JOIN PHIEU_XUAT p ON p.MaPX=c.MaPX
              WHERE p.TrangThai=N'Đã ghi sổ' AND p.NgayXuat>=@Tu AND p.NgayXuat<@Den) t
            """,args);
        Require(inventory.Tong.Bon==distinct && inventory.ChiTiet.Select(r=>r.Ma).Distinct().Count()==distinct,
            "Vật tư phải đếm đúng mã hàng khác nhau, không bị nhân bởi nhiều dòng hoặc nhiều ngày.");
        var imported=connection.ExecuteScalar<decimal>("""
            SELECT COALESCE(SUM(c.ThanhTien),0) FROM CHI_TIET_PHIEU_NHAP c JOIN PHIEU_NHAP p ON p.MaPN=c.MaPN
            WHERE p.TrangThai=N'Đã ghi sổ' AND p.NgayNhap>=@Tu AND p.NgayNhap<@Den
            """,args);
        Require(inventory.Tong.Ba==imported && inventory.ChiTiet.Sum(r=>r.Ba)==imported,"Giá trị nhập phải khớp SQL và bảng chi tiết.");
        var leave=Load("18");
        var submitted=connection.ExecuteScalar<int>("SELECT COUNT(*) FROM DON_XIN_NGHI WHERE NgayGui>=@Tu AND NgayGui<@Den",args);
        var substitutes=connection.ExecuteScalar<int>("""
            SELECT COUNT(*) FROM PHAN_CONG_THAY_THE p WHERE p.TrangThai=N'Đã xác nhận' AND EXISTS
              (SELECT 1 FROM DON_XIN_NGHI d WHERE d.MaDonNghi=p.MaDonNghi AND d.NgayGui>=@Tu AND d.NgayGui<@Den)
            """,args);
        Require(leave.Tong.Mot==submitted && leave.ChiTiet.Length==submitted && leave.Tong.Bon==substitutes,
            "Đơn nghỉ và thay thế phải trùng nguồn, không nhân số đơn khi liên kết nhiều ca thay thế.");
        var expiry=connection.ExecuteScalar<DateTime>("SELECT MIN(HanSuDung) FROM LO_HANG_HOA WHERE HanSuDung IS NOT NULL").AddDays(-45).Date;
        var previous=0m;
        foreach(var threshold in new[]{30,60,90})
        {
            var snapshot=BaoCaoDuLieu.TongHop("14",BaoCaoDuLieu.Doc("14",expiry,expiry,threshold),expiry,expiry,"Ngày");
            var expected=connection.ExecuteScalar<int>("""
                SELECT COUNT(*) FROM LO_HANG_HOA WHERE HanSuDung>=@Chot AND HanSuDung<=@CanhBao
                  AND NgayNhap<@DenNhap AND TrangThai NOT IN(N'Hủy',N'Đã hủy')
                """,new{Chot=expiry,CanhBao=expiry.AddDays(threshold),DenNhap=expiry.AddDays(1)});
            Require(snapshot.Tong.Ba==expected && snapshot.Tong.Ba>=previous && snapshot.ChiTiet.Length==snapshot.Tong.Mot,
                "Hạn dùng phải đúng ngày chốt và ngưỡng "+threshold);
            var csv=BaoCaoDuLieu.TaoCsv(BaoCaoDuLieu.DanhMuc.Single(r=>r.Ma=="14"),snapshot,expiry,expiry,"Ngày","kiem-tra",DateTime.Now,threshold);
            Require(csv.Contains(threshold+" ngày") && csv.Contains("Phân loại hạn dùng") && csv.Contains("Bảng chi tiết"),"CSV hạn dùng phải xuất đúng bộ lọc và danh sách lô.");
            previous=snapshot.Tong.Ba;
        }
        var denied=false;
        try{new BUS.BUS_BAOCAO().Doc("11",from,to,30,false);}catch(UnauthorizedAccessException){denied=true;}
        Require(denied,"BUS báo cáo phải chặn tài khoản thiếu quyền trước khi đọc database.");
        Console.WriteLine("PASS SQL: lương chốt, dòng vật tư/đơn vị, hạn dùng 30/60/90 ngày và đơn nghỉ/thay thế đối soát SELECT độc lập; không ghi dữ liệu.");
    }
}
