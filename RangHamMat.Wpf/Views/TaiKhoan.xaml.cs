using BUS;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace RangHamMat.Views;

public partial class TaiKhoan : UserControl
{
    private readonly BUS_TAIKHOAN busTaiKhoan = new();
    private readonly BUS_VAITRO busVaiTro = new();
    private readonly BUS_NHANVIEN busNhanVien = new();
    private readonly BUS_PHANQUYENVAITRO busPhanQuyen = new();
    private readonly BUS_QUANLYTAIKHOAN busQuanLyTaiKhoan = new();
    private DataTable danhSachPhanQuyen = new();


    private DataTable danhSachTaiKhoan = new();
    private DataTable danhSachVaiTro = new();
    private bool dangTai;
    private bool dangThemTaiKhoan;
    private bool dangThemVaiTro;

    public TaiKhoan()
    {
        InitializeComponent();
        Loaded += TaiKhoan_Loaded;
    }

    private void TaiKhoan_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= TaiKhoan_Loaded;
        btnThemTaiKhoan.IsEnabled = PhienDangNhap.IsAdmin;
        btnThemVaiTro.IsEnabled = PhienDangNhap.IsAdmin;
        editorTaiKhoan.LuuThanhCong += ma => SauKhiLuu(false, ma);
        editorVaiTro.LuuThanhCong += ma => SauKhiLuu(true, ma);
        editorTaiKhoan.HuyThayDoi += () => NapTaiKhoanDangChon();
        editorVaiTro.HuyThayDoi += () => NapVaiTroDangChon();
        hienThiDuLieu();
    }

    // Đọc dữ liệu thật qua BUS/DAL. Không đưa MatKhauHash vào nguồn DataGrid.
    private void hienThiDuLieu()
    {
        dangTai = true;
        try
        {
            var taiKhoan = busTaiKhoan.layToanBoTaiKhoan().ToList();
            var vaiTro = busVaiTro.layToanBoVaiTro().ToList();
            var nhanVien = busNhanVien.layToanBoNhanVien().ToList();
            var cacKhoa = new BUS_KHOA().layToanBoKhoa().ToList();
            var cacPhong = new BUS_PHONG().layToanBoPhong().ToList();

            danhSachTaiKhoan = new DataTable();
            danhSachTaiKhoan.Columns.Add("MaTK", typeof(string));
            danhSachTaiKhoan.Columns.Add("TenDangNhap", typeof(string));
            danhSachTaiKhoan.Columns.Add("HoTen", typeof(string));
            danhSachTaiKhoan.Columns.Add("TenVaiTro", typeof(string));
            danhSachTaiKhoan.Columns.Add("TrangThai", typeof(string));
            danhSachTaiKhoan.Columns.Add("MaVaiTro", typeof(string));
            danhSachTaiKhoan.Columns.Add("MaNV", typeof(string));
            danhSachTaiKhoan.Columns.Add("PhamVi", typeof(string));

            foreach (var tk in taiKhoan)
            {
                var vt = vaiTro.FirstOrDefault(x => x.MaVaiTro == tk.MaVaiTro);
                var nv = nhanVien.FirstOrDefault(x => x.MaNV == tk.MaNV);
                string phamVi = "Chưa chỉ định";
                if (!string.IsNullOrWhiteSpace(tk.MaPhong))
                {
                    var phong = cacPhong.FirstOrDefault(x => x.MaPhong == tk.MaPhong);
                    var khoa = cacKhoa.FirstOrDefault(x => x.MaKhoa == phong?.MaKhoa);
                    phamVi = "Phòng: " + (phong?.TenPhong ?? tk.MaPhong)
                        + (khoa is null ? "" : " · Khoa: " + khoa.TenKhoa);
                }
                else if (!string.IsNullOrWhiteSpace(tk.MaKhoa))
                {
                    var khoa = cacKhoa.FirstOrDefault(x => x.MaKhoa == tk.MaKhoa);
                    phamVi = "Khoa: " + (khoa?.TenKhoa ?? tk.MaKhoa);
                }
                danhSachTaiKhoan.Rows.Add(
                    tk.MaTK,
                    tk.TenDangNhap,
                    nv?.HoTen ?? tk.MaNV,
                    vt?.TenVaiTro ?? tk.MaVaiTro,
                    tk.TrangThai,
                    tk.MaVaiTro,
                    tk.MaNV,
                    phamVi);
            }

            danhSachVaiTro = new DataTable();
            danhSachVaiTro.Columns.Add("MaVaiTro", typeof(string));
            danhSachVaiTro.Columns.Add("TenVaiTro", typeof(string));
            danhSachVaiTro.Columns.Add("MoTa", typeof(string));
            danhSachVaiTro.Columns.Add("SoTaiKhoan", typeof(int));

            foreach (var vt in vaiTro)
            {
                danhSachVaiTro.Rows.Add(vt.MaVaiTro, vt.TenVaiTro, vt.MoTa,
                    taiKhoan.Count(x => x.MaVaiTro == vt.MaVaiTro));
            }

            Grid_TAI_KHOAN.ItemsSource = danhSachTaiKhoan.DefaultView;
            Grid_VAI_TRO.ItemsSource = danhSachVaiTro.DefaultView;

            cboVaiTro.Items.Clear();
            cboVaiTro.Items.Add(new ComboBoxItem
            {
                Content = "Tất cả vai trò",
                Tag = ""
            });
            foreach (var vt in vaiTro.OrderBy(x => x.TenVaiTro))
            {
                cboVaiTro.Items.Add(new ComboBoxItem
                {
                    Content = vt.TenVaiTro,
                    Tag = vt.MaVaiTro
                });
            }
            cboVaiTro.SelectedIndex = 0;

            lblTongTaiKhoan.Text = taiKhoan.Count.ToString();
            lblDangHoatDong.Text = taiKhoan.Count(x => string.Equals(x.TrangThai,
                "Đang hoạt động", StringComparison.OrdinalIgnoreCase)).ToString();
            lblKhongHoatDong.Text = taiKhoan.Count(x => !string.Equals(x.TrangThai,
                "Đang hoạt động", StringComparison.OrdinalIgnoreCase)).ToString();
            lblTongVaiTro.Text = vaiTro.Count.ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không tải được dữ liệu tài khoản và vai trò.\n" + ex.Message,
                "Tài khoản và vai trò", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            dangTai = false;
        }

        locTaiKhoan();
        locVaiTro();
        if (IsLoaded)
        {
            NapTaiKhoanDangChon();
            NapVaiTroDangChon();
        }
    }

    private void locTaiKhoan()
    {
        if (dangTai || danhSachTaiKhoan.Columns.Count == 0) return;

        // Chỉ dùng ký tự tìm kiếm thông thường; không ghép ký tự đặc biệt vào RowFilter.
        var timKiem = Regex.Replace(txtTimTaiKhoan.Text.Trim(),
            @"[^\p{L}\p{N}\s._-]", "");
        var loc = new List<string>();

        if (timKiem.Length > 0)
            loc.Add($"(MaTK LIKE '%{timKiem}%' OR TenDangNhap LIKE '%{timKiem}%' OR HoTen LIKE '%{timKiem}%')");

        var maVaiTro = (cboVaiTro.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
        if (maVaiTro.Length > 0)
            loc.Add($"MaVaiTro = '{maVaiTro.Replace("'", "''")}'");

        danhSachTaiKhoan.DefaultView.RowFilter = string.Join(" AND ", loc);
        lblKetQuaTaiKhoan.Text = $"{danhSachTaiKhoan.DefaultView.Count} tài khoản";
        if (!dangThemTaiKhoan && Grid_TAI_KHOAN.SelectedItem == null && danhSachTaiKhoan.DefaultView.Count > 0)
            Grid_TAI_KHOAN.SelectedIndex = 0;

    }

    private void locVaiTro()
    {
        if (dangTai || danhSachVaiTro.Columns.Count == 0) return;

        var timKiem = Regex.Replace(txtTimVaiTro.Text.Trim(),
            @"[^\p{L}\p{N}\s._-]", "");
        danhSachVaiTro.DefaultView.RowFilter = timKiem.Length == 0
            ? ""
            : $"MaVaiTro LIKE '%{timKiem}%' OR TenVaiTro LIKE '%{timKiem}%'";

        lblKetQuaVaiTro.Text = $"{danhSachVaiTro.DefaultView.Count} vai trò";
        if (!dangThemVaiTro && Grid_VAI_TRO.SelectedItem == null && danhSachVaiTro.DefaultView.Count > 0)
            Grid_VAI_TRO.SelectedIndex = 0;

    }

    // Mỗi lần đổi vai trò, nạp quyền của chính vai trò đó từ SQL.
    private void hienThiPhanQuyen()
    {
        danhSachPhanQuyen = new DataTable();
        danhSachPhanQuyen.Columns.Add("MaPhanHe", typeof(string));
        danhSachPhanQuyen.Columns.Add("TenPhanHe", typeof(string));
        danhSachPhanQuyen.Columns.Add("DuocTruyCap", typeof(bool));
        danhSachPhanQuyen.Columns.Add("CoTheSua", typeof(bool));
        danhSachPhanQuyen.Columns.Add("NhomQuyen", typeof(string));
        danhSachPhanQuyen.Columns.Add("ThuTu", typeof(int));

        // Năm DataView cùng đọc/ghi một DataTable. Lưu quyền vẫn dùng danhSachPhanQuyen.
        lstQuyenNhanSu.ItemsSource = new DataView(danhSachPhanQuyen,
            "NhomQuyen = 'Nhân sự'", "ThuTu ASC", DataViewRowState.CurrentRows);
        lstQuyenKhamBenh.ItemsSource = new DataView(danhSachPhanQuyen,
            "NhomQuyen = 'Khám chữa bệnh'", "ThuTu ASC", DataViewRowState.CurrentRows);
        lstQuyenKhoThietBi.ItemsSource = new DataView(danhSachPhanQuyen,
            "NhomQuyen = 'Kho và thiết bị'", "ThuTu ASC", DataViewRowState.CurrentRows);
        lstQuyenTaiChinh.ItemsSource = new DataView(danhSachPhanQuyen,
            "NhomQuyen = 'Tài chính'", "ThuTu ASC", DataViewRowState.CurrentRows);
        lstQuyenHeThong.ItemsSource = new DataView(danhSachPhanQuyen,
            "NhomQuyen = 'Hệ thống'", "ThuTu ASC", DataViewRowState.CurrentRows);
        btnLuuPhanQuyen.IsEnabled = false;

        if (Grid_VAI_TRO.SelectedItem is not DataRowView hang)
        {
            lblThongBaoQuyen.Text = "Chọn vai trò để cấu hình quyền truy cập.";
            return;
        }

        try
        {
            string maVaiTro = Convert.ToString(hang["MaVaiTro"]) ?? "";
            string tenVaiTro = Convert.ToString(hang["TenVaiTro"]) ?? "";
            bool laVaiTroQuanTri = tenVaiTro.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                || tenVaiTro.Equals("Quản trị", StringComparison.OrdinalIgnoreCase)
                || tenVaiTro.Equals("Quản trị viên", StringComparison.OrdinalIgnoreCase);
            var quyenDaCap = busPhanQuyen.layQuyenTheoVaiTro(maVaiTro).ToHashSet(StringComparer.Ordinal);

            int thuTu = 0;
            foreach (var phanHe in BUS_PHANQUYENVAITRO.CacPhanHe)
            {
                string nhom = phanHe.Ma switch
                {
                    "NhanSu" or "ChungChiNhanVien" or "CaLamViec" or "ChamCong"
                        or "BangLuong" or "ChinhSachLuong" or "XinNghiViec" => "Nhân sự",
                    "BenhNhan" or "LichKham" or "KhamDieuTri" or "DichVuThucHien"
                        or "DonThuoc" or "PhieuHuongDan" or "DichVu" => "Khám chữa bệnh",
                    "Kho" or "DanhMucHangHoa" or "NhapXuatVatTu" or "ThietBi"
                        or "BaoTri" or "NhaCungCap" => "Kho và thiết bị",
                    "HoaDon" or "BaoCao" => "Tài chính",
                    "TongQuan" or "TaiKhoan" or "NhatKy" => "Hệ thống",
                    _ => "Hệ thống"
                };
                bool batBuoc = laVaiTroQuanTri && phanHe.Ma == "TaiKhoan";
                danhSachPhanQuyen.Rows.Add(phanHe.Ma, phanHe.Ten,
                    batBuoc || quyenDaCap.Contains(phanHe.Ma),
                    PhienDangNhap.IsAdmin && !batBuoc, nhom, thuTu++);
            }

            btnLuuPhanQuyen.IsEnabled = PhienDangNhap.IsAdmin;
            lblThongBaoQuyen.Text = PhienDangNhap.IsAdmin
                ? "Tích chọn rồi nhấn Lưu để cấp quyền cho vai trò này."
                : "Chỉ quản trị viên được phép sửa quyền.";
        }
        catch (Exception ex)
        {
            lblThongBaoQuyen.Text = "Không tải được quyền: " + ex.Message;
        }
    }

    private void btnLuuPhanQuyen_Click(object sender, RoutedEventArgs e)
    {
        if (!PhienDangNhap.IsAdmin || Grid_VAI_TRO.SelectedItem is not DataRowView hang)
            return;
        if (MessageBox.Show("Lưu quyền mở phân hệ cho vai trò đã chọn?",
            "Xác nhận phân quyền", MessageBoxButton.YesNo, MessageBoxImage.Question)
            != MessageBoxResult.Yes) return;

        try
        {
            // Đọc mã tài khoản từ định danh đã xác thực, không dùng mã được nhập trên form.
            var maTaiKhoan = (System.Threading.Thread.CurrentPrincipal as
                System.Security.Claims.ClaimsPrincipal)?
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var maVaiTro = Convert.ToString(hang["MaVaiTro"]) ?? "";
            var duocCap = danhSachPhanQuyen.Rows.Cast<DataRow>()
                .Where(r => r.Field<bool>("DuocTruyCap"))
                .Select(r => r.Field<string>("MaPhanHe")!)
                .ToArray();
            busPhanQuyen.luuQuyenVaiTro(maTaiKhoan, maVaiTro, duocCap);
            lblThongBaoQuyen.Text = "Đã lưu quyền vào database.";
            hienThiPhanQuyen();
            (Window.GetWindow(this) as MainWindow)?.LamMoiDanhSachPhanHe();
            MessageBox.Show("Đã cập nhật quyền của vai trò. Các phiên khác cần đăng nhập lại để nhận quyền mới.",
                "Phân quyền", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không lưu được quyền: " + ex.Message,
                "Phân quyền", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LamMoi_Click(object sender, RoutedEventArgs e) => hienThiDuLieu();

    private void txtTimTaiKhoan_TextChanged(object sender, TextChangedEventArgs e)
        => locTaiKhoan();

    private void cboVaiTro_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => locTaiKhoan();

    private void txtTimVaiTro_TextChanged(object sender, TextChangedEventArgs e)
        => locVaiTro();

    private static string MaNguoiSua()
        => (System.Threading.Thread.CurrentPrincipal as System.Security.Claims.ClaimsPrincipal)?
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    private void Grid_TAI_KHOAN_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dangTai || editorTaiKhoan is null) return;
        if (Grid_TAI_KHOAN.SelectedItem is DataRowView) dangThemTaiKhoan = false;
        NapTaiKhoanDangChon();
    }

    private void Grid_VAI_TRO_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dangTai || editorVaiTro is null) return;
        if (Grid_VAI_TRO.SelectedItem is DataRowView) dangThemVaiTro = false;
        NapVaiTroDangChon();
    }

    private void NapTaiKhoanDangChon()
    {
        try
        {
            if (Grid_TAI_KHOAN.SelectedItem is DataRowView hang)
            {
                editorTaiKhoan.MoTaiKhoan(Convert.ToString(hang["MaTK"]));
                bool dangHoatDong = string.Equals(Convert.ToString(hang["TrangThai"]),
                    "Đang hoạt động", StringComparison.OrdinalIgnoreCase);
                bool daKhoa = string.Equals(Convert.ToString(hang["TrangThai"]),
                    "Đã khóa", StringComparison.OrdinalIgnoreCase);
                btnKhoaTaiKhoan.IsEnabled = PhienDangNhap.IsAdmin && dangHoatDong;
                btnKhoiPhucTaiKhoan.IsEnabled = PhienDangNhap.IsAdmin && daKhoa;
                lblThongBaoTaiKhoan.Text = daKhoa
                    ? "Tài khoản đã khóa: không đăng nhập và không có quyền mở phân hệ. Khôi phục để dùng lại vai trò cũ."
                    : "Sửa ngay trong khung này. Khóa sẽ tạm vô hiệu hóa mọi quyền, không xóa phân quyền vai trò.";
            }
            else
            {
                editorTaiKhoan.MoTaiKhoan();
                btnKhoaTaiKhoan.IsEnabled = false;
                btnKhoiPhucTaiKhoan.IsEnabled = false;
                lblThongBaoTaiKhoan.Text = "Chế độ thêm mới: trạng thái mặc định Đang hoạt động.";
            }
        }
        catch (Exception ex)
        {
            lblThongBaoTaiKhoan.Text = "Không đọc được tài khoản: " + ex.Message;
            btnKhoaTaiKhoan.IsEnabled = false;
            btnKhoiPhucTaiKhoan.IsEnabled = false;
        }
    }

    private void NapVaiTroDangChon()
    {
        try
        {
            if (Grid_VAI_TRO.SelectedItem is DataRowView hang)
                editorVaiTro.MoVaiTro(Convert.ToString(hang["MaVaiTro"]));
            else
                editorVaiTro.MoVaiTro();
            hienThiPhanQuyen();
        }
        catch (Exception ex)
        {
            lblThongBaoQuyen.Text = "Không đọc được vai trò: " + ex.Message;
        }
    }

    private static void ChonDong(DataGrid grid, string cotMa, string ma)
    {
        foreach (var item in grid.Items.OfType<DataRowView>())
        {
            if (!string.Equals(Convert.ToString(item[cotMa]), ma, StringComparison.Ordinal)) continue;
            grid.SelectedItem = item;
            grid.ScrollIntoView(item);
            break;
        }
    }

    private void SauKhiLuu(bool laVaiTro, string ma)
    {
        // Bỏ bộ lọc để nhìn thấy bản ghi vừa tạo, tránh mất lựa chọn khi tìm kiếm.
        dangTai = true;
        try
        {
            if (laVaiTro) txtTimVaiTro.Text = "";
            else
            {
                txtTimTaiKhoan.Text = "";
                cboVaiTro.SelectedIndex = 0;
            }
        }
        finally { dangTai = false; }
        dangThemTaiKhoan = false;
        dangThemVaiTro = false;
        hienThiDuLieu();
        if (laVaiTro)
        {
            ChonDong(Grid_VAI_TRO, "MaVaiTro", ma);
            NapVaiTroDangChon();
        }
        else
        {
            ChonDong(Grid_TAI_KHOAN, "MaTK", ma);
            NapTaiKhoanDangChon();
        }
        (Window.GetWindow(this) as MainWindow)?.LamMoiDanhSachPhanHe();
        MessageBox.Show("Đã lưu " + (laVaiTro ? "vai trò" : "tài khoản") + " thành công.",
            "Tài khoản và vai trò", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnThemTaiKhoan_Click(object sender, RoutedEventArgs e)
    {
        if (!PhienDangNhap.IsAdmin) return;
        dangThemTaiKhoan = true;
        Grid_TAI_KHOAN.SelectedItem = null;
        NapTaiKhoanDangChon();
    }

    private void btnThemVaiTro_Click(object sender, RoutedEventArgs e)
    {
        if (!PhienDangNhap.IsAdmin) return;
        dangThemVaiTro = true;
        Grid_VAI_TRO.SelectedItem = null;
        NapVaiTroDangChon();
    }

    private void DoiTrangThaiTaiKhoan(string trangThaiMoi)
    {
        if (!PhienDangNhap.IsAdmin || Grid_TAI_KHOAN.SelectedItem is not DataRowView hang) return;
        var ma = Convert.ToString(hang["MaTK"]) ?? "";
        var trangThaiCu = Convert.ToString(hang["TrangThai"]) ?? "";
        var noiDung = trangThaiMoi == "Đã khóa"
            ? "Khóa tài khoản này? Tài khoản sẽ không thể đăng nhập và mọi quyền mở phân hệ sẽ bị vô hiệu hóa cho đến khi khôi phục."
            : "Khôi phục tài khoản này? Tài khoản sẽ đăng nhập lại được và sử dụng các quyền vai trò đã cấp trước đó.";
        if (MessageBox.Show(noiDung, "Xác nhận trạng thái tài khoản",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            busQuanLyTaiKhoan.doiTrangThaiTaiKhoan(MaNguoiSua(), ma, trangThaiCu, trangThaiMoi);
            hienThiDuLieu();
            ChonDong(Grid_TAI_KHOAN, "MaTK", ma);
            NapTaiKhoanDangChon();
            (Window.GetWindow(this) as MainWindow)?.LamMoiDanhSachPhanHe();
            MessageBox.Show(trangThaiMoi == "Đã khóa"
                    ? "Đã khóa tài khoản. Phân quyền vai trò vẫn được lưu nguyên để phục hồi."
                    : "Đã khôi phục tài khoản và các quyền thuộc vai trò đã gán.",
                "Quản lý tài khoản", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Không đổi được trạng thái",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnKhoaTaiKhoan_Click(object sender, RoutedEventArgs e)
        => DoiTrangThaiTaiKhoan("Đã khóa");

    private void btnKhoiPhucTaiKhoan_Click(object sender, RoutedEventArgs e)
        => DoiTrangThaiTaiKhoan("Đang hoạt động");
}
