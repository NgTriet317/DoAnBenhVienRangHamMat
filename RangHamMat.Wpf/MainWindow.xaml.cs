using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Input;
using RangHamMat.Views;
using RangHamMat.Controls;

namespace RangHamMat;

public partial class MainWindow : Window
{
    private static readonly HashSet<string> AllPages = new(StringComparer.Ordinal)
    {
        "TongQuan", "BenhNhan", "KhamDieuTri", "DichVuThucHien", "DonThuoc", "NhanSu", "ChungChiNhanVien", "CaLamViec",
        "ThietBi", "BaoTri", "VatTu", "DanhMucHangHoa", "NhaCungCap", "NhapXuatVatTu", "NhapKho", "XuatKho", "LichKham",
        "DichVu", "HoaDon", "BangLuong", "ChinhSachLuong", "TaiKhoan", "BaoCao", "PhieuHuongDan",
        "NhatKy"
    };

    private static readonly Dictionary<string, HashSet<string>> RolePermissions = new(StringComparer.Ordinal)
    {
        ["Quản trị hệ thống"] = AllPages,
        ["Ban quản lý / Kế toán"] = new(StringComparer.Ordinal)
        {
            "TongQuan", "BenhNhan", "LichKham", "NhanSu", "ChungChiNhanVien", "CaLamViec", "VatTu", "DanhMucHangHoa", "NhapXuatVatTu",
            "NhapKho", "XuatKho", "ThietBi", "BaoTri", "NhaCungCap", "DichVu", "DichVuThucHien", "HoaDon", "BangLuong", "ChinhSachLuong", "BaoCao"
        },
        ["Người hướng dẫn bệnh nhân"] = new(StringComparer.Ordinal)
        {
            "TongQuan", "BenhNhan", "LichKham", "CaLamViec", "HoaDon", "DichVu", "PhieuHuongDan"
        },
        ["Bác sĩ"] = new(StringComparer.Ordinal)
        {
            "TongQuan", "BenhNhan", "LichKham", "CaLamViec", "KhamDieuTri", "DichVuThucHien", "DonThuoc", "DichVu",
            "PhieuHuongDan"
        },
        ["Nhân viên kho / thiết bị"] = new(StringComparer.Ordinal)
        {
            "TongQuan", "VatTu", "DanhMucHangHoa", "NhapXuatVatTu", "NhapKho", "XuatKho", "ThietBi", "BaoTri", "NhaCungCap"
        }
    };


    public MainWindow()
    {
        InitializeComponent();
        RolePicker.SelectedIndex = 0;
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && AppHost.Content is AppWorkspace workspace && workspace.ReturnToPage())
                e.Handled = true;
        };
    }

    private string CurrentRole => (RolePicker.SelectedItem as ComboBoxItem)?.Content as string ?? "Quản trị hệ thống";

    private void App_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
            OpenApp(tag);
    }

    internal void OpenApp(string tag)
    {
        if (!RolePermissions[CurrentRole].Contains(tag))
        {
            MessageBox.Show("Vai trò đang xem không có ứng dụng này. Bạn có thể đổi vai trò tại màn hình chính.",
                "Xem trước vai trò", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
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
            "CaLamViec" => new CaLamViec(),
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
            _ => null
        };
        if (page is null) return;

        var tile = AppGrid.Children.OfType<Button>().First(button => (string)button.Tag == tag);
        AppHost.Content = new AppWorkspace(this, AutomationProperties.GetName(tile), CurrentRole, page);
        HomeSurface.Visibility = Visibility.Collapsed;
        AppHost.Visibility = Visibility.Visible;
    }

    internal void OpenModuleWindow(string tag, AppWorkspace workspace, string? table = null, CreationContext? context = null)
    {
        if (!RolePermissions[CurrentRole].Contains(tag)) return;
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

    internal void ShowHome()
    {
        AppHost.Content = null;
        AppHost.Visibility = Visibility.Collapsed;
        HomeSurface.Visibility = Visibility.Visible;
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        // Keep restored windows fullscreen; OS app switching/minimizing stays available.
        if (WindowState == WindowState.Normal)
            WindowState = WindowState.Maximized;
    }

    private string selectedCategory = "Tất cả";

    private void RolePicker_SelectionChanged(object sender, SelectionChangedEventArgs e) => FilterApps();
    private void AppSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterApps();

    private void Category_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { CommandParameter: string category }) return;
        selectedCategory = category;
        foreach (var button in CategoryFilters.Children.OfType<Button>())
            button.Style = (Style)FindResource((string)button.CommandParameter == category ? "Primary" : "Quiet");
        FilterApps();
    }

    private void FilterApps()
    {
        // TextChanged can fire while InitializeComponent is still constructing Home.
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
