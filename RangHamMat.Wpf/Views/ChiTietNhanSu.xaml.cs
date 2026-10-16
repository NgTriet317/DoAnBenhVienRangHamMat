using BUS;
using ET;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RangHamMat.Views;

public partial class ChiTietNhanSu : Window
{
    private ET_NHANVIEN nhanVien;
    private BUS_NHANVIEN busNhanVien = new();
    public bool DaThayDoi { get; private set; }

    public ChiTietNhanSu(ET_NHANVIEN ttNhanVien)
    {
        InitializeComponent();
        nhanVien = ttNhanVien;
        DataContext = nhanVien;

        LoadDanhSachChucVuVaChuyenKhoa();
        GanDuLieuBanDau();
        DangKySuKienXoaBaoDo();
    }

    private void LoadDanhSachChucVuVaChuyenKhoa()
    {
        cboChucVu.ItemsSource = new BUS_CHUCVU().layToanBoChucVu();
        cboChuyenKhoa.ItemsSource = new BUS_CHUYENKHOA().layToanBoChuyenKhoa();
    }

    private void GanDuLieuBanDau()
    {
        txtMaNV.Text = nhanVien.MaNV;
        txtHoTen.Text = nhanVien.HoTen;
        txtSDT.Text = nhanVien.SDT;
        txtDiaChi.Text = nhanVien.DiaChi;
        txtBangCap.Text = nhanVien.BangCap;

        dpNgaySinh.SelectedDate = nhanVien.NgaySinh == default ? DateTime.Today.AddYears(-25) : nhanVien.NgaySinh;
        dpNgayVaoLam.SelectedDate = nhanVien.NgayVaoLam == default ? DateTime.Today : nhanVien.NgayVaoLam;

        // so sánh gioi tinh vói gioitinh trong db để gán
        foreach (ComboBoxItem item in cboGioiTinh.Items)
        {
            if (string.Equals(item.Content?.ToString(), nhanVien.GioiTinh, StringComparison.OrdinalIgnoreCase))
            {
                cboGioiTinh.SelectedItem = item;
                break;
            }
        }//ko có thì mặc định nam
        if (cboGioiTinh.SelectedItem == null)
        {
            cboGioiTinh.SelectedIndex = 0;
        }

        // Chức vụ và chuyên khoa
        cboChucVu.SelectedValue = nhanVien.MaChucVu;
        cboChuyenKhoa.SelectedValue = nhanVien.MaCK;

        //// so trạng thái vs trạng thái trong db
        //foreach (ComboBoxItem item in cboTrangThai.Items)
        //{
        //    if (string.Equals(item.Content?.ToString(), nhanVien.TrangThai, StringComparison.OrdinalIgnoreCase))
        //    {
        //        cboTrangThai.SelectedItem = item;
        //        break;
        //    }
        //}//ko có thì mặc định đang làm việc
        //if (cboTrangThai.SelectedItem == null)
        //{
        //    cboTrangThai.SelectedIndex = 0;
        //}

        CapNhatHeader();
    }

    private void Control_ThayDoiDuLieu(object? sender, EventArgs e)
    {
        if (sender is Control control)
        {
            XoaBaoDo(control);
        }
    }

    private void DangKySuKienXoaBaoDo()
    {
        txtHoTen.TextChanged += Control_ThayDoiDuLieu;
        txtSDT.TextChanged += Control_ThayDoiDuLieu;
        dpNgaySinh.SelectedDateChanged += Control_ThayDoiDuLieu;
        dpNgayVaoLam.SelectedDateChanged += Control_ThayDoiDuLieu;
        cboChucVu.SelectionChanged += Control_ThayDoiDuLieu;
    }

    private void CapNhatHeader()
    {
        lblHeaderHoTen.Text = nhanVien.HoTen;
        lblHeaderTrangThai.Text = nhanVien.TrangThai;
    }

    private void DatBaoDo(Control control)
    {
        control.BorderBrush = Brushes.Red;
        control.BorderThickness = new Thickness(1.5);
    }

    private void XoaBaoDo(Control control)
    {
        control.ClearValue(BorderBrushProperty);
        control.ClearValue(BorderThicknessProperty);
    }

    private void XoaToanBoBaoDo()
    {
        XoaBaoDo(txtHoTen);
        XoaBaoDo(txtSDT);
        XoaBaoDo(dpNgaySinh);
        XoaBaoDo(dpNgayVaoLam);
        XoaBaoDo(cboChucVu);
    }

    private bool KiemTraDuLieuDauVao(out string thongBaoLoi)
    {
        thongBaoLoi = string.Empty;
        bool hopLe = true;
        XoaToanBoBaoDo();

        // Họ tên không đc để trống
        if (string.IsNullOrWhiteSpace(txtHoTen.Text))
        {
            DatBaoDo(txtHoTen);
            thongBaoLoi = "Họ và tên không được để trống.";
            hopLe = false;
        }
        // họ tên không đc chứa số và kí tự đặc biệt
        else if (!txtHoTen.Text.Trim().All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
        {
            DatBaoDo(txtHoTen);
            thongBaoLoi = "Họ và tên không được chứa số và kí tự đặc biệt.";
            hopLe = false;
        }

        // Số điện thoại (10 chữ số, bắt đầu bằng 0)
        string sdt = txtSDT.Text;
        if (string.IsNullOrWhiteSpace(sdt))
        {
            DatBaoDo(txtSDT);
            if (string.IsNullOrEmpty(thongBaoLoi))
                thongBaoLoi = "Số điện thoại không được để trống.";
            hopLe = false;
        }
        else if (sdt.Length != 10 || !sdt.StartsWith("0") || !long.TryParse(sdt, out _))
        {
            DatBaoDo(txtSDT);
            thongBaoLoi = "Số điện thoại không hợp lệ (phải gồm 10 chữ số và bắt đầu bằng số 0).";
            hopLe = false;
        }

        // Kiểm tra ngày sinh (ít nhất 18 tuổi, không trong tương lai)
        if (!dpNgaySinh.SelectedDate.HasValue)
        {
            DatBaoDo(dpNgaySinh);
            if (string.IsNullOrEmpty(thongBaoLoi))
                thongBaoLoi = "Vui lòng chọn ngày sinh.";
            hopLe = false;
        }
        else
        {
            DateTime ngaySinh = dpNgaySinh.SelectedDate.Value;
            if (ngaySinh > DateTime.Today)
            {
                DatBaoDo(dpNgaySinh);
                if (string.IsNullOrEmpty(thongBaoLoi))
                    thongBaoLoi = "Ngày sinh không được lớn hơn ngày hiện tại.";
                hopLe = false;
            }
            else if (DateTime.Today.Year - ngaySinh.Year < 18 ||
                     (DateTime.Today.Year - ngaySinh.Year == 18 && DateTime.Today < ngaySinh.AddYears(18)))
            {
                DatBaoDo(dpNgaySinh);
                if (string.IsNullOrEmpty(thongBaoLoi))
                    thongBaoLoi = "Nhân viên phải từ đủ 18 tuổi trở lên.";
                hopLe = false;
            }
        }

        // Kiểm tra chức vụ
        if (cboChucVu.SelectedValue == null)
        {
            DatBaoDo(cboChucVu);
            if (string.IsNullOrEmpty(thongBaoLoi))
                thongBaoLoi = "Vui lòng chọn chức vụ cho nhân viên.";
            hopLe = false;
        }

        // Kiểm tra ngày vào làm
        if (!dpNgayVaoLam.SelectedDate.HasValue)
        {
            DatBaoDo(dpNgayVaoLam);
            if (string.IsNullOrEmpty(thongBaoLoi))
                thongBaoLoi = "Vui lòng chọn ngày vào làm.";
            hopLe = false;
        }
        else if (dpNgaySinh.SelectedDate.HasValue && dpNgayVaoLam.SelectedDate.Value <= dpNgaySinh.SelectedDate.Value)
        {
            DatBaoDo(dpNgayVaoLam);
            if (string.IsNullOrEmpty(thongBaoLoi))
                thongBaoLoi = "Ngày vào làm phải sau ngày sinh.";
            hopLe = false;
        }

        return hopLe;
    }

    private void btnChinhSua_Click(object sender, RoutedEventArgs e)
    {
        if (!KiemTraDuLieuDauVao(out string thongBaoLoi))
        {
            MessageBox.Show(thongBaoLoi, "Kiểm tra dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        nhanVien.HoTen = txtHoTen.Text.Trim();
        nhanVien.GioiTinh = cboGioiTinh.Text;
        nhanVien.NgaySinh = dpNgaySinh.SelectedDate!.Value;
        nhanVien.SDT = txtSDT.Text.Trim();
        nhanVien.DiaChi = txtDiaChi.Text.Trim();
        nhanVien.MaChucVu = cboChucVu.SelectedValue?.ToString() ?? "";
        nhanVien.MaCK = cboChuyenKhoa.SelectedValue?.ToString() ?? "";
        nhanVien.BangCap = txtBangCap.Text.Trim();
        nhanVien.NgayVaoLam = dpNgayVaoLam.SelectedDate!.Value;
        //nhanVien.TrangThai = cboTrangThai.Text;

        if (busNhanVien.capNhatNhanVien(nhanVien))
        {
            DaThayDoi = true;
            CapNhatHeader();
            MessageBox.Show("Cập nhật thông tin nhân viên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("Cập nhật thông tin nhân viên thất bại. Vui lòng kiểm tra lại!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnKhoiPhuc_Click(object sender, RoutedEventArgs e)
    {
        if (!string.Equals(nhanVien.TrangThai, "Đã nghỉ", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Chỉ có thể khôi phục nhân viên đang ở trạng thái 'Đã nghỉ'.",
                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Bạn có muốn khôi phục nhân sự '{nhanVien.HoTen}' về trạng thái 'Đang làm việc'?",
            "Xác nhận khôi phục", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        if (busNhanVien.khoiPhucNhanVien(nhanVien.MaNV))
        {
            nhanVien.TrangThai = "Đang làm việc";
            DaThayDoi = true;
            CapNhatHeader();
            MessageBox.Show("Khôi phục nhân sự thành công!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("Không thể khôi phục nhân sự. Vui lòng kiểm tra kết nối hoặc trạng thái hiện tại trong cơ sở dữ liệu.",
                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void btnNghiViec_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn chuyển trạng thái nhân sự '{nhanVien.HoTen}' sang 'Đã nghỉ'?",
            "Xác nhận nghỉ việc",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;
        else
        {
            busNhanVien.nghiViecNhanVien(nhanVien.MaNV);
            MessageBox.Show("Thao tác thành công");
            DaThayDoi = true; CapNhatHeader();
            
        }



        //if (busNhanVien.nghiViecNhanVien(nhanVien.MaNV))
        //{
        //    nhanVien.TrangThai = "Đã nghỉ";
        //    foreach (ComboBoxItem item in cboTrangThai.Items)
        //    {
        //        if (string.Equals(item.Content?.ToString(), "Đã nghỉ", StringComparison.OrdinalIgnoreCase))
        //        {
        //            cboTrangThai.SelectedItem = item;
        //            break;
        //        }
        //    }
        //    DaThayDoi = true;
        //    CapNhatHeader();
        //    MessageBox.Show("Đã chuyển trạng thái nhân sự sang 'Đã nghỉ' thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        //}
        //else
        //{
        //    MessageBox.Show("Thao tác nghỉ việc thất bại. Vui lòng thử lại!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
        //}
    }

    private void btnDong_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
