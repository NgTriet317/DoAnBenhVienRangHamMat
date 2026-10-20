using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BUS;
using ET;
using RangHamMat;
using RangHamMat.Controls;
using RangHamMat.Views;

internal static class SupplierChecks
{
    [STAThread]
    private static int Main(string[] args)
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        try
        {
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/RangHamMat.Wpf;component/Themes/Theme.xaml", UriKind.Relative) });
            KiemTraNhapLieu.Register();
            if (args.Contains("--inline-validation"))
            {
                InlineValidationChecks.Run();
                return 0;
            }
            DuLieuGiaoDien.RefreshFromDatabase();
            Run();
            Console.WriteLine("PASS: toàn bộ kiểm tra nhà cung cấp, dữ liệu ghi thử đã rollback.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL: " + ex); return 1; }
        finally { app.Shutdown(); }
    }

    private static void Run()
    {
        var original = new BUS_NHACUNGCAP().layToanBoNhaCungCap().ToArray();
        PhienDangNhap.IsAdmin = true;
        var home = new MainWindow();
        var page = new NhaCungCap();
        var workspace = new AppWorkspace(home, "Nhà cung cấp", "Admin", page);
        var host = new Window { Content = workspace, Width = 1366, Height = 900, ShowInTaskbar = false };
        try
        {
            host.Show(); Drain(host);
            var grid = Find<DataGrid>(page, "Grid_NHA_CUNG_CAP");
            Require(original.Length > 0 && grid.Items.Count == original.Length, "Phải đọc đủ nhà cung cấp từ SQL.");
            var rows = grid.Items.Cast<DongNhaCungCap>().ToArray();
            foreach (var row in rows)
            {
                var source = original.Single(item => item.MaNCC == row.MaNCC);
                Require(row.TenNCC == source.TenNCC && row.Email == source.Email && row.NguoiLienHe == source.NguoiLienHe, "Thông tin liên hệ phải khớp SQL.");
                Require(row.SoThietBi == DuLieuGiaoDien.Rows("THIET_BI").Count(item => UiRecord.Text(item, "MaNCC") == row.MaNCC), "Số thiết bị liên kết.");
                Require(row.SoLoHang == DuLieuGiaoDien.Rows("LO_HANG_HOA").Count(item => UiRecord.Text(item, "MaNCC") == row.MaNCC), "Số lô hàng liên kết.");
                Require(row.SoHoaDonNhap == DuLieuGiaoDien.Rows("HOA_DON_NHAP").Count(item => UiRecord.Text(item, "MaNCC") == row.MaNCC), "Số hóa đơn nhập liên kết.");
            }
            Require(Find<TextBlock>(page, "txtHoatDong").Text == original.Count(item => item.TrangThai == "Đang hoạt động").ToString(), "Thống kê phải dùng số thật.");
            grid.SelectedIndex = 0; Drain(host);
            Require(Find<Grid>(page, "ChiTiet").DataContext == grid.SelectedItem, "Chọn đúng chi tiết.");
            Require(Elements(page).OfType<Button>().First(button => Equals(button.Content, "Xóa")).IsEnabled == rows[0].ChoXoa, "Quyền xóa theo liên kết thật.");
            Capture(workspace, "nha-cung-cap-1366");

            var search = Find<TextBox>(page, "txtTimKiem");
            foreach (var term in new[] { original[0].MaNCC, original[0].Email, original[0].NguoiLienHe, original[0].SDT! })
            {
                search.Text = term.ToUpperInvariant();
                Require(grid.Items.Cast<DongNhaCungCap>().Any(row => row.MaNCC == original[0].MaNCC), "Tìm theo mã/email/người liên hệ/điện thoại, không phân biệt hoa thường.");
            }
            search.Text = "__khong_co_nha_cung_cap__"; Drain(host);
            Require(grid.Items.Count == 0 && Find<StackPanel>(page, "KhongCoDuLieu").Visibility == Visibility.Visible, "Tìm không thấy có trạng thái rỗng.");
            Require(Find<Grid>(page, "ChiTiet").Visibility == Visibility.Collapsed, "Kết quả rỗng không giữ chi tiết cũ.");
            Capture(workspace, "nha-cung-cap-khong-co-ket-qua");
            Click(page, "Bỏ bộ lọc");
            Find<ComboBox>(page, "cboTrangThai").SelectedItem = "Ngừng hoạt động";
            Require(grid.Items.Count == original.Count(item => item.TrangThai == "Ngừng hoạt động"), "Lọc trạng thái.");
            Require(Find<TextBlock>(page, "txtTongSo").Text == original.Length.ToString(), "Lọc không đổi tổng toàn danh mục.");
            Click(page, "Bỏ bộ lọc");
            grid.SelectedIndex = 0;
            host.Width = 1024; host.Height = 820; Drain(host);
            Require(Find<Border>(page, "VungDanhSach").ActualHeight > 300, "Bảng ở 1024 phải đủ cao.");
            Capture(workspace, "nha-cung-cap-1024");
            host.Width = 1366; host.Height = 900; Drain(host);
            Console.WriteLine("PASS: dữ liệu SQL, thông tin liên hệ, số liên kết, thống kê, tìm kiếm, lọc, trạng thái rỗng và bố cục 1366/1024.");

            DuLieuGiaoDien.Transaction(() =>
            {
                Click(page, "Thêm nhà cung cấp"); Drain(host);
                var form = (CuaSoNhaCungCap)Find<ContentControl>(workspace, "ModalContent").Content;
                Require(Find<TextBox>(form, "txtMaNCC").IsReadOnly, "Mã nhà cung cấp chỉ đọc.");
                Click(form, "Lưu nhà cung cấp"); Drain(host);
                Require(Find<Grid>(workspace, "ModalLayer").Visibility == Visibility.Visible && KiemTraNhapLieu.GetHasError(Find<TextBox>(form, "txtTenNCC")) && Find<TextBlock>(form, "FormError").Text.Length == 0, "Thiếu trường bắt buộc phải giữ form và báo lỗi ngay dưới tên nhà cung cấp.");
                Capture(workspace, "bieu-mau-nha-cung-cap-bao-loi");
                Find<TextBox>(form, "txtTenNCC").Text = "  Công ty kiểm tra O'Brien  ";
                Find<TextBox>(form, "txtNguoiLienHe").Text = "Nguyễn Minh O'Brien";
                Find<TextBox>(form, "txtEmail").Text = "contact@example.com";
                Find<TextBox>(form, "txtSDT").Text = "02812345678";
                Find<TextBox>(form, "txtEmail").Focus(); Drain(host);
                Require(Find<TextBlock>(form, "FormError").Text.Length == 0, "Sửa trường sai và rời ô phải xóa thông báo cũ.");
                Capture(workspace, "bieu-mau-nha-cung-cap");
                Click(form, "Lưu nhà cung cấp"); Drain(host);
                var saved = BieuMauHoSo.GetSaved(form);
                Require(saved is not null, "Nút Lưu gọi BUS/DAL: " + Find<TextBlock>(form, "FormError").Text);
                Require(UiRecord.Text(saved!, "TenNCC") == "Công ty kiểm tra O'Brien", "Trim và giữ dấu nháy qua SQL parameters.");
                Require(UiRecord.Text(saved!, "SDT") == "02812345678", "Giữ số 0 đầu số điện thoại.");
                Require(UiRecord.Get(saved, "DiaChi") is null, "Địa chỉ được phép bỏ trống.");
                Require(UiRecord.Text(saved!, "MaNCC").StartsWith("NCC") && UiRecord.Text(saved!, "MaNCC") != "Tự sinh khi lưu", "Mã do DAL tự cấp.");
                Require(DuLieuGiaoDien.Rows("NHA_CUNG_CAP").Count == original.Length + 1, "Thêm phải cập nhật cache.");
                Require(Find<Grid>(workspace, "ModalLayer").Visibility == Visibility.Collapsed, "Lưu thành công đóng form.");

                var edit = new CuaSoNhaCungCap();
                BieuMauHoSo.Configure(edit, "NHA_CUNG_CAP", saved);
                workspace.ShowModal(edit); Drain(host);
                Require(Find<TextBox>(edit, "txtTenNCC").Text == UiRecord.Text(saved!, "TenNCC"), "Sửa phải nạp đúng dữ liệu cũ.");
                Find<TextBox>(edit, "txtEmail").Text = "email_sai";
                Click(edit, "Lưu nhà cung cấp"); Drain(host);
                Require(KiemTraNhapLieu.GetHasError(Find<TextBox>(edit, "txtEmail")) && Find<TextBlock>(edit, "FormError").Text.Length == 0 && Find<Grid>(workspace, "ModalLayer").Visibility == Visibility.Visible, "Email sai phải báo ngay dưới ô email và không được đóng form/lưu.");
                Find<TextBox>(edit, "txtEmail").Text = "updated@example.com";
                Find<TextBox>(edit, "txtSDT").Text = "";
                Find<ComboBox>(edit, "cboTrangThai").SelectedIndex = 1;
                Click(edit, "Lưu nhà cung cấp"); Drain(host);
                saved = BieuMauHoSo.GetSaved(edit);
                Require(saved is not null && UiRecord.Text(saved, "TrangThai") == "Ngừng hoạt động", "Cập nhật và ngừng hoạt động phải lưu SQL.");
                Require(UiRecord.Get(saved, "SDT") is null, "Điện thoại được phép bỏ trống.");

                var old = new CuaSoNhaCungCap();
                BieuMauHoSo.Configure(old, "NHA_CUNG_CAP", saved);
                DuLieuGiaoDien.Upsert("NHA_CUNG_CAP", new Dictionary<string, object?> { ["TrangThai"] = "Đang hoạt động" }, saved);
                Require(!BieuMauHoSo.TrySave(old, out _, out _), "Form cũ không ghi đè thay đổi.");
                Require(UiRecord.Text(saved!, "TrangThai") == "Đang hoạt động", "Có thể hoạt động lại.");
                var row = new DongNhaCungCap(saved!);
                Require(row.ChoXoa && row.DienThoai == "Chưa có số điện thoại", "Đối tác mới chưa liên kết có thể xóa; số trống có nhãn.");
                DuLieuGiaoDien.Delete("NHA_CUNG_CAP", saved!);
                Require(DuLieuGiaoDien.Rows("NHA_CUNG_CAP").Count == original.Length, "Xóa cập nhật cache.");
                return false;
            });
            Console.WriteLine("PASS: nút thêm/lưu/sửa, nhập liệu bắt buộc/email, mã tự cấp, số 0 đầu, nullable, đổi trạng thái, chặn form cũ, xóa đối tác chưa liên kết.");

            CheckDatabaseRules();
            var after = new BUS_NHACUNGCAP().layToanBoNhaCungCap().ToArray();
            Require(after.Length == original.Length && original.All(old => after.Any(row =>
                row.MaNCC == old.MaNCC && row.TenNCC == old.TenNCC && row.Email == old.Email && row.SDT == old.SDT &&
                row.DiaChi == old.DiaChi && row.NguoiLienHe == old.NguoiLienHe && row.TrangThai == old.TrangThai)), "Rollback phải giữ nguyên cả dữ liệu SQL ban đầu.");
            Require(grid.Items.Count == original.Length, "Sau rollback danh sách phải tự cập nhật.");
        }
        finally { host.Close(); home.Close(); }
    }

    private static void CheckDatabaseRules()
    {
        using var tx = new BUS_DONTHUOC().BatDauGiaoDich();
        using var connection = tx.Connection!;
        try
        {
            var bus = new BUS_NHACUNGCAP(tx);
            ET_NHACUNGCAP New() => new()
            {
                TenNCC = "Kiểm tra SQL O'Brien", Email = "test@example.com", NguoiLienHe = "Nhân viên thử", TrangThai = "Đang hoạt động"
            };
            var bad = New(); bad.TenNCC = " "; Reject(() => bus.themNhaCungCap(bad), "Tên trống");
            bad = New(); bad.NguoiLienHe = ""; Reject(() => bus.themNhaCungCap(bad), "Người liên hệ trống");
            bad = New(); bad.Email = ""; Reject(() => bus.themNhaCungCap(bad), "Thiếu email");
            bad = New(); bad.Email = "sai@"; Reject(() => bus.themNhaCungCap(bad), "Email sai");
            bad = New(); bad.SDT = "abc123"; Reject(() => bus.themNhaCungCap(bad), "Điện thoại sai");
            bad = New(); bad.SDT = "123"; Reject(() => bus.themNhaCungCap(bad), "Điện thoại quá ngắn");
            bad = New(); bad.TenNCC = new string('x', 256); Reject(() => bus.themNhaCungCap(bad), "Độ dài vượt SQL");
            bad = New(); bad.TrangThai = "Tùy ý"; Reject(() => bus.themNhaCungCap(bad), "Trạng thái sai");

            var current = New(); current.SDT = "+84 (28) 1234-5678";
            bus.themNhaCungCap(current);
            var original = bus.layNhaCungCapTheoMa(current.MaNCC)!;
            current.TenNCC = "Đã sửa"; bus.suaNhaCungCap(current, original);
            Reject(() => bus.suaNhaCungCap(original, original), "Ghi đè ở DAL");
            Reject(() => bus.xoaNhaCungCap(original.MaNCC, original), "Xóa từ bản gốc cũ");
            current.TrangThai = "Ngừng hoạt động"; bus.suaNhaCungCap(current);
            Require(bus.layNhaCungCapTheoMa(current.MaNCC)!.TrangThai == "Ngừng hoạt động", "Trạng thái phải lưu SQL.");
            bus.xoaNhaCungCap(current.MaNCC);
            Require(bus.layNhaCungCapTheoMa(current.MaNCC) is null, "Xóa thật trong giao dịch.");

            foreach (var table in new[] { "THIET_BI", "LO_HANG_HOA", "HOA_DON_NHAP" })
            {
                var id = DuLieuGiaoDien.Rows(table).Select(row => UiRecord.Text(row, "MaNCC")).First(value => value.Length > 0);
                Reject(() => bus.xoaNhaCungCap(id), "Xóa nhà cung cấp đã có " + table);
                Require(bus.layNhaCungCapTheoMa(id) is not null, "Bị chặn xóa phải giữ nguyên nhà cung cấp.");
            }
            Console.WriteLine("PASS: BUS/DAL kiểm tra tên, email, người liên hệ, điện thoại, độ dài, trạng thái; chặn sửa/xóa đồng thời và xóa nhà cung cấp đã liên kết cả ba bảng.");
        }
        finally { tx.Rollback(); }
    }

    private static T Find<T>(UserControl root, string name) where T : class => (T)root.FindName(name);
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
        var path = System.IO.Path.Combine(Environment.CurrentDirectory, "XemTruoc", "nha-cung-cap");
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
