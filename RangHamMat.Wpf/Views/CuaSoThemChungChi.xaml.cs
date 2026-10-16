using BUS;
using ET;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RangHamMat.Views;

public partial class CuaSoThemChungChi : Window
{
    private readonly BUS_CHUNGCHINHANVIEN _busChungChi = new();
    private readonly BUS_NHANVIEN _busNhanVien = new();
    public bool DaThemThanhCong { get; private set; }

    public CuaSoThemChungChi(string? maNVDefault = null)
    {
        InitializeComponent();

        LoadDanhSachNhanVien(maNVDefault);
        KhoiTaoGiaTriBanDau();
        DangKySuKienXoaBaoDo();
    }

    private void LoadDanhSachNhanVien(string? maNVDefault)
    {
        try
        {
            var nvList = _busNhanVien.layToanBoNhanVien()?.ToList();
            cboNhanVien.ItemsSource = nvList;

            if (!string.IsNullOrEmpty(maNVDefault))
            {
                cboNhanVien.SelectedValue = maNVDefault;
            }
            else if (nvList != null && nvList.Count > 0)
            {
                cboNhanVien.SelectedIndex = 0;
            }
        }
        catch
        {
            // Bỏ qua lỗi kết nối nếu có
        }
    }

    private void KhoiTaoGiaTriBanDau()
    {
        txtMaCC.Text = "Tự động";
        dpNgayCap.SelectedDate = DateTime.Today;
        dpHanChungChi.SelectedDate = null;
        cboTrangThai.SelectedIndex = 0;
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
        txtMaCC.TextChanged += Control_ThayDoiDuLieu;
        cboNhanVien.SelectionChanged += Control_ThayDoiDuLieu;
        txtSoChungChi.TextChanged += Control_ThayDoiDuLieu;
        txtTenChungChi.TextChanged += Control_ThayDoiDuLieu;
        txtNoiCap.TextChanged += Control_ThayDoiDuLieu;
        dpNgayCap.SelectedDateChanged += Control_ThayDoiDuLieu;
        dpHanChungChi.SelectedDateChanged += Control_ThayDoiDuLieu;
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
        XoaBaoDo(txtMaCC);
        XoaBaoDo(cboNhanVien);
        XoaBaoDo(txtSoChungChi);
        XoaBaoDo(txtTenChungChi);
        XoaBaoDo(txtNoiCap);
        XoaBaoDo(dpNgayCap);
        XoaBaoDo(dpHanChungChi);
    }

    private bool KiemTraDuLieuDauVao(out string thongBaoLoi)
    {
        thongBaoLoi = string.Empty;
        bool hopLe = true;
        XoaToanBoBaoDo();

        // Kiểm tra nhân viên
        if (cboNhanVien.SelectedValue == null)
        {
            DatBaoDo(cboNhanVien);
            if (string.IsNullOrEmpty(thongBaoLoi))
                thongBaoLoi = "Vui lòng chọn nhân viên sở hữu chứng chỉ.";
            hopLe = false;
        }

        // Kiểm tra số chứng chỉ
        if (string.IsNullOrWhiteSpace(txtSoChungChi.Text))
        {
            DatBaoDo(txtSoChungChi);
            if (string.IsNullOrEmpty(thongBaoLoi))
                thongBaoLoi = "Số chứng chỉ không được để trống.";
            hopLe = false;
        }

        // Kiểm tra tên chứng chỉ
        if (string.IsNullOrWhiteSpace(txtTenChungChi.Text))
        {
            DatBaoDo(txtTenChungChi);
            if (string.IsNullOrEmpty(thongBaoLoi))
                thongBaoLoi = "Tên chứng chỉ không được để trống.";
            hopLe = false;
        }

        // Kiểm tra nơi cấp
        if (string.IsNullOrWhiteSpace(txtNoiCap.Text))
        {
            DatBaoDo(txtNoiCap);
            if (string.IsNullOrEmpty(thongBaoLoi))
                thongBaoLoi = "Nơi cấp chứng chỉ không được để trống.";
            hopLe = false;
        }

        // Kiểm tra ngày cấp
        if (!dpNgayCap.SelectedDate.HasValue)
        {
            DatBaoDo(dpNgayCap);
            if (string.IsNullOrEmpty(thongBaoLoi))
                thongBaoLoi = "Vui lòng chọn ngày cấp chứng chỉ.";
            hopLe = false;
        }
        else if (dpNgayCap.SelectedDate.Value > DateTime.Today)
        {
            DatBaoDo(dpNgayCap);
            if (string.IsNullOrEmpty(thongBaoLoi))
                thongBaoLoi = "Ngày cấp không được lớn hơn ngày hiện tại.";
            hopLe = false;
        }

        // Kiểm tra hạn chứng chỉ nếu có nhập
        if (dpHanChungChi.SelectedDate.HasValue && dpNgayCap.SelectedDate.HasValue)
        {
            if (dpHanChungChi.SelectedDate.Value < dpNgayCap.SelectedDate.Value)
            {
                DatBaoDo(dpHanChungChi);
                if (string.IsNullOrEmpty(thongBaoLoi))
                    thongBaoLoi = "Hạn chứng chỉ phải lớn hơn hoặc bằng ngày cấp.";
                hopLe = false;
            }
        }

        return hopLe;
    }

    private void btnThem_Click(object sender, RoutedEventArgs e)
    {
        if (!KiemTraDuLieuDauVao(out string thongBaoLoi))
        {
            MessageBox.Show(thongBaoLoi, "Kiểm tra dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string maCC = "";
        string maNV = cboNhanVien.SelectedValue?.ToString() ?? "";
        string soChungChi = txtSoChungChi.Text.Trim();
        string tenChungChi = txtTenChungChi.Text.Trim();
        string noiCap = txtNoiCap.Text.Trim();
        DateTime ngayCap = dpNgayCap.SelectedDate!.Value;
        DateTime? hanChungChi = dpHanChungChi.SelectedDate;
        string trangThai = (cboTrangThai.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Còn hiệu lực";
        string ghiChu = txtGhiChu.Text.Trim();

        var chungChiMoi = new ET_CHUNGCHINHANVIEN(
            maCC, maNV, soChungChi, tenChungChi, ngayCap, noiCap, hanChungChi, trangThai, ghiChu);

        try
        {
            if (_busChungChi.themChungChiNhanVien(chungChiMoi))
            {
                DaThemThanhCong = true;
                MessageBox.Show("Thêm chứng chỉ hành nghề thành công!");
                Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi thêm chứng chỉ: " + ex.Message);
        }
    }

    private void btnLamMoi_Click(object sender, RoutedEventArgs e)
    {
        txtMaCC.Text = "Tự động";
        txtSoChungChi.Text = string.Empty;
        txtTenChungChi.Text = string.Empty;
        txtNoiCap.Text = string.Empty;
        txtGhiChu.Text = string.Empty;
        dpNgayCap.SelectedDate = DateTime.Today;
        dpHanChungChi.SelectedDate = null;
        cboTrangThai.SelectedIndex = 0;

        XoaToanBoBaoDo();
    }

    private void btnDong_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
