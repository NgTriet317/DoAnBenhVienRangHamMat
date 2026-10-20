// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ giao diện trang chủ, lọc phân hệ theo vai trò/từ khóa và luồng mở phân hệ; đã chú thích các sự kiện và dữ liệu dùng để điều hướng.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Input;
using RangHamMat.Views;
using RangHamMat.Controls;

namespace RangHamMat;

/// <summary>
/// Cửa sổ chính quản lý các ô ứng dụng, bộ lọc vai trò/nhóm/từ khóa và chuyển phân hệ cùng ngữ cảnh hồ sơ.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly HashSet<string> AllPages = new(StringComparer.Ordinal)
    {
        "TongQuan", "BenhNhan", "KhamDieuTri", "DichVuThucHien", "DonThuoc", "NhanSu", "ChungChiNhanVien", "CaLamViec",
        "ThietBi", "BaoTri", "VatTu", "DanhMucHangHoa", "NhaCungCap", "NhapXuatVatTu", "NhapKho", "XuatKho", "LichKham",
        "DichVu", "HoaDon", "BangLuong", "ChinhSachLuong", "TaiKhoan", "BaoCao", "PhieuHuongDan",
        "NhatKy", "XinNghiViec"
    };

    private static readonly Dictionary<string, HashSet<string>> RolePermissions = new(StringComparer.Ordinal)
    {
        ["Admin"] = AllPages,
        ["User"] = new(StringComparer.Ordinal) { "TongQuan", "CaLamViec", "XinNghiViec" }
    };

    /// <summary>
    /// Khởi tạo MainWindow từ XAML.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        RolePicker.Items.Clear();
        RolePicker.Items.Add(new ComboBoxItem { Content = PhienDangNhap.IsAdmin ? "Admin" : "User" });
        RolePicker.SelectedIndex = 0;
        RolePicker.IsEnabled = false;
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && AppHost.Content is AppWorkspace workspace && workspace.ReturnToPage())
                e.Handled = true;
        };
    }

    /// <summary>
    /// Vai trò đã xác thực tại đăng nhập, không cho đổi bằng bộ chọn trên trang chủ.
    /// </summary>
    private string CurrentRole => PhienDangNhap.IsAdmin ? "Admin" : "User";

    /// <summary>
    /// Tra danh sách phân hệ được mở theo tài khoản đã đăng nhập.
    /// </summary>
    internal bool CanOpen(string module) => RolePermissions[CurrentRole].Contains(module);

    /// <summary>
    /// Lấy tên phân hệ từ Tag của nút ứng dụng rồi chuyển cho OpenApp để kiểm tra quyền và mở màn hình.
    /// </summary>
    private void App_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
            OpenApp(tag);
    }

    // Mở khu vực làm đơn xin nghỉ phép / nghỉ việc từ trang chủ.
    private void XinNghiViec_Click(object sender, RoutedEventArgs e)
    {
        OpenApp("XinNghiViec");
    }

    /// <summary>
    /// Kiểm tra vai trò, nạp dữ liệu SQL cho các phân hệ hồ sơ/tài chính, tạo UserControl tương ứng và đặt vào AppWorkspace; thông báo nếu không kết nối được.
    /// </summary>
    internal void OpenApp(string tag)
    {
        if (!RolePermissions[CurrentRole].Contains(tag))
        {
            MessageBox.Show("Tài khoản hiện tại không có quyền mở phân hệ này.",
                "Quyền truy cập", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (tag is "BenhNhan" or "LichKham" or "KhamDieuTri" or "DichVuThucHien" or "DonThuoc" or "HoaDon" or "BaoTri" or "NhaCungCap")
        {
            try {DuLieuGiaoDien.EnsureLoaded();}
            catch(InvalidOperationException ex) {MessageBox.Show(ex.Message,"Kết nối database",MessageBoxButton.OK,MessageBoxImage.Error);return;}
        }
        UserControl? page = tag switch
        {
            "TongQuan" => new TongQuan(),
            "BenhNhan" => new BenhNhan(),
            "LichKham" => new LichKham(),
            "KhamDieuTri" => new KhamDieuTri(),
            "DichVuThucHien" => new DichVuThucHien(),
            "DonThuoc" => new DonThuoc(),
            "PhieuHuongDan" => new PhieuHuongDan(),
            "NhanSu" => new NhanSu(),
            "ChungChiNhanVien" => new ChungChiNhanVien(),
            "CaLamViec" => new CaLamViec(PhienDangNhap.MaNV, PhienDangNhap.IsAdmin),
            "ThietBi" => new ThietBi(),
            "BaoTri" => new BaoTri(),
            "VatTu" => new VatTu(),
            "DanhMucHangHoa" => new DanhMucHangHoa(),
            "NhaCungCap" => new NhaCungCap(),
            "NhapXuatVatTu" => new NhapXuatVatTu(),
            "NhapKho" => new NhapKho(),
            "XuatKho" => new XuatKho(),
            "HoaDon" => new HoaDon(),
            "BangLuong" => new BangLuong(),
            "ChinhSachLuong" => new ChinhSachLuong(),
            "BaoCao" => new BaoCao(),
            "DichVu" => new DichVu(),
            "TaiKhoan" => new TaiKhoan(),
            "NhatKy" => new NhatKy(),
            "XinNghiViec" => new XinNghiViec(),
            _ => null
        };
        if (page is null) return;

        string tenTrang = "Đơn xin nghỉ phép / nghỉ việc";
        if (tag != "XinNghiViec")
        {
            var tile = AppGrid.Children.OfType<Button>().First(button => (string)button.Tag == tag);
            tenTrang = AutomationProperties.GetName(tile);
        }
        AppHost.Content = new AppWorkspace(this, tenTrang, CurrentRole, page);
        HomeSurface.Visibility = Visibility.Collapsed;
        AppHost.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Lấy AppWorkspace hiện đang nằm trong AppHost để chuyển ngữ cảnh hoặc mở biểu mẫu sau khi điều hướng.
    /// </summary>
    internal AppWorkspace? AppHostWorkspace() => AppHost.Content as AppWorkspace;

    /// <summary>
    /// Kiểm tra quyền mở phân hệ rồi truyền đồng thời mã bệnh nhân, lần khám và đơn thuốc sang màn hình đích.
    /// </summary>
    internal void OpenHoSoApp(string module, string? patientId, string? visitId = null, string? prescriptionId = null)
    {
        if (!CanOpen(module)) { MessageBox.Show("Vai trò hiện tại không được mở chức năng này.", "Quyền truy cập"); return; }
        OpenApp(module);
        if (AppHost.Content is AppWorkspace workspace) workspace.FocusHoSo(patientId, visitId, prescriptionId);
    }

    /// <summary>
    /// Chọn biểu mẫu theo tên phân hệ và giao cho workspace hiển thị, kèm tên bảng và ngữ cảnh cha nếu tạo dữ liệu con.
    /// </summary>
    internal void OpenModuleWindow(string tag, AppWorkspace workspace, string? table = null, CreationContext? context = null)
    {
        if (!RolePermissions[CurrentRole].Contains(tag)) return;
        if (tag == "CaLamViec" && !PhienDangNhap.IsAdmin) return;
        UserControl? form = tag switch
        {
            "BenhNhan" => new CuaSoBenhNhan(),
            "LichKham" => new CuaSoLichKham(),
            "KhamDieuTri" => new CuaSoKhamDieuTri(),
            "DonThuoc" => new CuaSoDonThuoc(),
            // Chi tiết nhân sự cần một ET_NHANVIEN cụ thể và là Window,
            // không phải form UserControl tạo mới qua luồng chung này.
            "CaLamViec" => new CuaSoCaLamViec(),
            "ThietBi" => new CuaSoThietBi(),
            "BaoTri" => new CuaSoBaoTri(),
            "VatTu" => new CuaSoVatTu(),
            "NhaCungCap" => new CuaSoNhaCungCap(),
            "NhapXuatVatTu" => new CuaSoNhapXuatVatTu(),
            "HoaDon" => new CuaSoHoaDon(),
            "BangLuong" => new CuaSoBangLuong(),
            "DichVu" => new CuaSoDichVu(),
            "TaiKhoan" => new CuaSoTaiKhoan(),
            "PhieuHuongDan" => new CuaSoPhieuHuongDan(),
            _ => null
        };
        if (form is null) return;

        workspace.ShowForm(form, table, context);
    }

    /// <summary>
    /// Xóa nội dung AppHost, đóng vùng ứng dụng đang mở và hiện lại danh sách các phân hệ ở trang chủ.
    /// </summary>
    internal void ShowHome()
    {
        AppHost.Content = null;
        AppHost.Visibility = Visibility.Collapsed;
        HomeSurface.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Đóng cửa sổ chính; ứng dụng sẽ kết thúc theo ShutdownMode đã đặt lúc khởi động.
    /// </summary>
    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// Đưa cửa sổ về trạng thái phóng to khi nó trở lại Normal; vẫn cho phép thu nhỏ và chuyển ứng dụng của Windows.
    /// </summary>
    private void Window_StateChanged(object? sender, EventArgs e)
    {
        // Giữ cửa sổ phóng to khi khôi phục; Windows vẫn cho thu nhỏ và chuyển ứng dụng.
        if (WindowState == WindowState.Normal)
            WindowState = WindowState.Maximized;
    }

    private string selectedCategory = "Tất cả";

    /// <summary>
    /// Lọc lại các ô ứng dụng khi người dùng đổi vai trò xem giao diện.
    /// </summary>
    private void RolePicker_SelectionChanged(object sender, SelectionChangedEventArgs e) => FilterApps();
    /// <summary>
    /// Lọc danh sách ứng dụng theo nội dung tìm kiếm, đồng thời giữ bộ lọc nhóm và quyền của vai trò.
    /// </summary>
    private void AppSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterApps();

    /// <summary>
    /// Ghi nhận nhóm chức năng vừa chọn, đổi style nút nhóm và lọc lại các ứng dụng phù hợp.
    /// </summary>
    private void Category_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { CommandParameter: string category }) return;
        selectedCategory = category;
        foreach (var button in CategoryFilters.Children.OfType<Button>())
            button.Style = (Style)FindResource((string)button.CommandParameter == category ? "Primary" : "Quiet");
        FilterApps();
    }

    /// <summary>
    /// Kết hợp quyền vai trò, nhóm chức năng và tên ứng dụng; ẩn các ô không khớp, cập nhật số lượng và thông báo danh sách trống.
    /// </summary>
    private void FilterApps()
    {
        // TextChanged có thể chạy khi InitializeComponent chưa dựng xong các control trang chủ.
        if (AppGrid is null || AppCount is null || NoApps is null) return;
        var allowed = RolePermissions[CurrentRole];
        var query = AppSearch?.Text.Trim() ?? "";
        var count = 0;
        foreach (var tile in AppGrid.Children.OfType<Button>())
        {
            var visible = allowed.Contains((string)tile.Tag)
                && (selectedCategory == "Tất cả" || (string)tile.CommandParameter == selectedCategory)
                && AutomationProperties.GetName(tile).Contains(query, StringComparison.CurrentCultureIgnoreCase);
            tile.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (visible) count++;
        }
        AppCount.Text = $"{count} ứng dụng đang hiển thị";
        NoApps.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
