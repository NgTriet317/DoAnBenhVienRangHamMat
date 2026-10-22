using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BUS;
using ET;
using RangHamMat;
using RangHamMat.Views;
using RangHamMat.Controls;

internal static class BaoTriChecks
{
    [STAThread]
    private static int Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        try
        {
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/RangHamMat.Wpf;component/Themes/Theme.xaml", UriKind.Relative) });
            KiemTraNhapLieu.Register();
            DuLieuGiaoDien.RefreshFromDatabase();
            Run();
            Console.WriteLine("PASS: tất cả kiểm tra bảo trì; dữ liệu ghi thử đã rollback.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL: " + ex); return 1; }
        finally { app.Shutdown(); }
    }

    public static void Run()
    {
        var initial = new BUS_BAOTRITHIETBI().layToanBoBaoTriThietBi().Count();
        PhienDangNhap.IsAdmin = true;
        var home = new MainWindow();
        var page = new BaoTri();
        var workspace = new AppWorkspace(home, "Lịch sử bảo trì", "Admin", page);
        var host = new Window { Content = workspace, Width = 1366, Height = 900, ShowInTaskbar = false };
        try
        {
            host.Show(); Drain(host);
            var grid = Control<DataGrid>(page, "Grid_BAO_TRI_THIET_BI");
            Require(initial > 0 && grid.Items.Count == initial, "Lịch sử bảo trì phải tải đủ các dòng từ SQL.");
            var rows = grid.Items.Cast<DongBaoTri>().ToArray();
            Require(rows.All(row => row.TenThietBi != row.MaTB && row.NhanVien.Length > 0), "Thiết bị và nhân viên phải có tên từ SQL.");
            grid.SelectedIndex = 0; Drain(host);
            Require(Control<Grid>(page, "ChiTiet").DataContext == grid.SelectedItem, "Chọn dòng phải hiện đúng chi tiết.");
            Capture(workspace, "lich-su-bao-tri-1366");

            var search = Control<TextBox>(page, "txtTimKiem");
            search.Text = rows[0].MaBT;
            Require(grid.Items.Count == 1, "Tìm theo mã bảo trì.");
            search.Text = "__khong_co__";
            Require(grid.Items.Count == 0 && Control<StackPanel>(page, "KhongCoDuLieu").Visibility == Visibility.Visible, "Tìm không thấy phải có trạng thái rỗng.");
            search.Clear();
            Control<DatePicker>(page, "dpTuNgay").SelectedDate = rows[0].NgayBatDau;
            Control<DatePicker>(page, "dpDenNgay").SelectedDate = rows[0].NgayBatDau;
            Require(grid.Items.Count == rows.Count(row => row.NgayBatDau == rows[0].NgayBatDau), "Bộ lọc ngày phải bao gồm cả hai đầu.");
            Control<DatePicker>(page, "dpTuNgay").SelectedDate = rows[0].NgayBatDau.AddDays(1);
            Require(Control<TextBlock>(page, "txtLoi").Text.Length > 0, "Khoảng ngày ngược phải được báo lỗi.");
            Click(page, "Bỏ bộ lọc");
            Control<ComboBox>(page, "cboTrangThai").SelectedItem = "Nháp";
            Require(grid.Items.Count == rows.Count(row => row.Status == "Nháp"), "Lọc theo trạng thái thật.");
            Click(page, "Bỏ bộ lọc");
            host.Width = 1024; host.Height = 820; Drain(host);
            Require(Control<Border>(page, "VungDanhSach").ActualHeight > 250 && Grid.GetColumn(Control<Border>(page, "VungChiTiet")) == 2, "Ở 1024px bảng phải đủ cao và chi tiết nằm bên cạnh.");
            grid.SelectedIndex = 0; Drain(host); Capture(workspace, "lich-su-bao-tri-1024");
            host.Width = 1366; host.Height = 900; Drain(host);
            Console.WriteLine("PASS: SQL, tên liên kết, tìm kiếm, lọc ngày/trạng thái, chi tiết, trạng thái rỗng, bố cục 1366/1024.");

            DuLieuGiaoDien.Transaction(() =>
            {
                Click(page, "Thêm lần bảo trì"); Drain(host);
                var form = (CuaSoBaoTri)Control<ContentControl>(workspace, "ModalContent").Content;
                Control<ComboBox>(form, "cboThietBi").SelectedIndex = 0;
                Control<ComboBox>(form, "cboNhanVien").SelectedIndex = 0;
                Control<TextBox>(form, "txtNoiDung").Text = "Kiểm tra bảo trì O'Brien, dữ liệu rollback.";
                Control<TextBox>(form, "txtChiPhi").Text = "125000,50";
                Capture(workspace, "bieu-mau-bao-tri");
                Control<TextBox>(form, "txtGioKetThuc").Text = "25:99";
                Click(form, "Lưu lần bảo trì"); Drain(host);
                Require(KiemTraNhapLieu.GetHasError(Control<TextBox>(form, "txtGioKetThuc")) && Control<TextBlock>(form, "FormError").Text.Length == 0, "Giờ sai phải báo ngay dưới ô giờ.");
                Control<TextBox>(form, "txtGioKetThuc").Text = "00:00";
                Click(form, "Lưu lần bảo trì"); Drain(host);
                var saved = BieuMauHoSo.GetSaved(form);
                Require(saved is not null, "Nút Lưu phải đi qua workspace/BUS/DAL: " + Control<TextBlock>(form, "FormError").Text);
                Require(Convert.ToDecimal(UiRecord.Get(saved, "ChiPhi")) == 125000.50m, "Chi phí thập phân phải lưu chính xác.");
                Require(UiRecord.Text(saved!, "MaBT").StartsWith("BTTB"), "Mã phải do DAL cấp theo quy ước dự án.");
                Require(Control<Grid>(workspace, "ModalLayer").Visibility == Visibility.Collapsed, "Lưu thành công phải đóng form.");

                var edit = new CuaSoBaoTri();
                BieuMauHoSo.Configure(edit, "BAO_TRI_THIET_BI", saved);
                Control<TextBox>(edit, "txtChiPhi").Text = "135000,25";
                Require(BieuMauHoSo.TrySave(edit, out saved, out var error), "Cập nhật form: " + error);
                var stale = new CuaSoBaoTri();
                BieuMauHoSo.Configure(stale, "BAO_TRI_THIET_BI", saved);
                DuLieuGiaoDien.Upsert("BAO_TRI_THIET_BI", new Dictionary<string, object?> { ["GhiChu"] = "Đổi từ nơi khác" }, saved);
                Require(!BieuMauHoSo.TrySave(stale, out _, out _), "Form cũ phải chặn ghi đè.");
                DuLieuGiaoDien.Delete("BAO_TRI_THIET_BI", saved!);
                Require(DuLieuGiaoDien.Rows("BAO_TRI_THIET_BI").Count == initial, "Xóa nháp phải đồng bộ cache.");
                return false;
            });
            Console.WriteLine("PASS: nút thêm/lưu thật, mã tự sinh, chi phí lẻ, cập nhật, form cũ, xóa nháp và rollback.");

            CheckRules();
            CheckDeadlines(host, workspace, page);
            Require(new BUS_BAOTRITHIETBI().layToanBoBaoTriThietBi().Count() == initial, "Sau kiểm tra SQL phải giữ nguyên số bản ghi.");
        }
        finally { host.Close(); home.Close(); }
    }

    private static void CheckRules()
    {
        using var tx = new BUS_DONTHUOC().BatDauGiaoDich();
        using var connection = tx.Connection!;
        try
        {
            var bus = new BUS_BAOTRITHIETBI(tx);
            ET_BAOTRITHIETBI New() => new()
            {
                MaTB = UiRecord.Text(DuLieuGiaoDien.Rows("THIET_BI").First(), "MaTB"),
                MaNV = UiRecord.Text(DuLieuGiaoDien.Rows("NHAN_VIEN").First(), "MaNV"),
                NgayBatDau = DateTime.Today, NoiDung = "Kiểm tra rollback", ChiPhi = 10.25m, TrangThai = "Nháp"
            };
            var bad = New(); bad.ChiPhi = -1; Reject(() => bus.themBaoTriThietBi(bad), "Chi phí âm");
            bad = New(); bad.NoiDung = " "; Reject(() => bus.themBaoTriThietBi(bad), "Nội dung trống");
            bad = New(); bad.MaNV = ""; Reject(() => bus.themBaoTriThietBi(bad), "Thiếu nhân viên");
            bad = New(); bad.NgayKetThuc = DateTime.Today.AddDays(-1); Reject(() => bus.themBaoTriThietBi(bad), "Kết thúc trước bắt đầu");
            bad = New(); bad.NgayBaoTriTiepTheo = DateTime.Today; Reject(() => bus.themBaoTriThietBi(bad), "Hẹn tiếp theo không hợp lệ");
            bad = New(); bad.TrangThai = "Tùy ý"; Reject(() => bus.themBaoTriThietBi(bad), "Trạng thái sai");
            bad = New(); bad.TrangThai = "Hoàn thành"; Reject(() => bus.themBaoTriThietBi(bad), "Hoàn thành thiếu ngày kết thúc");
            var draft = New(); bus.themBaoTriThietBi(draft);
            var old = bus.layBaoTriThietBiTheoMa(draft.MaBT)!;
            draft.ChiPhi = 20.50m; bus.suaBaoTriThietBi(draft, old);
            old.NoiDung = "Ghi đè từ bản cũ"; Reject(() => bus.suaBaoTriThietBi(old, old), "Sửa đồng thời tại DAL");
            draft.TrangThai = "Đang thực hiện"; bus.suaBaoTriThietBi(draft);
            Reject(() => bus.xoaBaoTriThietBi(draft.MaBT), "Xóa lần đang thực hiện");
            draft.TrangThai = "Hoàn thành"; draft.NgayKetThuc = DateTime.Today.AddHours(10);
            bus.suaBaoTriThietBi(draft);
            Reject(() => bus.suaBaoTriThietBi(draft), "Sửa lần hoàn thành");
            Reject(() => bus.xoaBaoTriThietBi(draft.MaBT), "Xóa lần hoàn thành");
            Console.WriteLine("PASS: ràng buộc nội dung, nhân viên, chi phí, ngày, trạng thái, sửa đồng thời; giữ lịch sử hoàn tất.");
        }
        finally { tx.Rollback(); }
    }

    private static void CheckDeadlines(Window host, AppWorkspace workspace, BaoTri page)
    {
        DuLieuGiaoDien.Transaction(() =>
        {
            var device = UiRecord.Text(DuLieuGiaoDien.Rows("THIET_BI").First(), "MaTB");
            object Save(DateTime start, DateTime due, string status) => DuLieuGiaoDien.Upsert("BAO_TRI_THIET_BI", new Dictionary<string, object?>
            {
                ["MaTB"] = device, ["MaNV"] = UiRecord.Text(DuLieuGiaoDien.Rows("NHAN_VIEN").First(), "MaNV"),
                ["NgayBatDau"] = start, ["NgayBaoTriTiepTheo"] = due,
                ["NoiDung"] = "Kiểm tra hẹn bảo trì", ["ChiPhi"] = 0m, ["TrangThai"] = status
            });
            Save(DateTime.Today.AddDays(-5), DateTime.Today.AddDays(-4), "Nháp");
            var latest = Save(DateTime.Today.AddDays(-2), DateTime.Today.AddDays(30), "Nháp");
            Save(DateTime.Today.AddDays(-1), DateTime.Today, "Hủy");
            var content = Control<ContentControl>(workspace, "Workspace");
            content.Content = null; Drain(host); content.Content = page; Drain(host);
            var grid = Control<DataGrid>(page, "Grid_BAO_TRI_THIET_BI");
            Require(grid.Items.Cast<DongBaoTri>().All(row => row.MaTB != device || !row.DenHan), "Hẹn cũ/hủy không được tính đến hạn khi có lịch mới.");
            DuLieuGiaoDien.Upsert("BAO_TRI_THIET_BI", new Dictionary<string, object?> { ["NgayBaoTriTiepTheo"] = DateTime.Today.AddDays(-1) }, latest);
            content.Content = null; Drain(host); content.Content = page; Drain(host);
            Require(grid.Items.Cast<DongBaoTri>().Count(row => row.MaTB == device && row.DenHan) == 1, "Mỗi thiết bị chỉ tính hạn trên lần mới nhất chưa hủy.");
            Control<CheckBox>(page, "chkDenHan").IsChecked = true;
            Require(grid.Items.Cast<DongBaoTri>().All(row => row.DenHan), "Lọc chỉ thiết bị đến hạn.");
            Click(page, "Bỏ bộ lọc");
            return false;
        });
        Console.WriteLine("PASS: tính hạn theo thiết bị, bỏ lịch cũ/hủy, lọc đến hạn.");
    }

    private static T Control<T>(UserControl root, string name) where T : class => (T)root.FindName(name);
    private static void Click(DependencyObject root, string text)
    {
        var button = Elements(root).OfType<Button>().First(item => Equals(item.Content, text));
        Require(button.IsEnabled, "Nút phải hoạt động: " + text);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
    }
    private static IEnumerable<DependencyObject> Elements(DependencyObject root)
    {
        yield return root;
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach (var element in Elements(child)) yield return element;
    }
    private static void Drain(Window host)
    {
        host.UpdateLayout(); host.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); host.UpdateLayout();
    }
    private static void Capture(FrameworkElement content, string name)
    {
        content.UpdateLayout();
        var path = System.IO.Path.Combine(Environment.CurrentDirectory, "XemTruoc", "bao-tri");
        System.IO.Directory.CreateDirectory(path);
        var bitmap = new RenderTargetBitmap((int)content.ActualWidth, (int)content.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = System.IO.File.Create(System.IO.Path.Combine(path, name + ".png")); encoder.Save(output);
    }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("Không chặn: " + name);
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
