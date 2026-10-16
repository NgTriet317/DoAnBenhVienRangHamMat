using BUS;
using ET;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RangHamMat.Views;

public partial class ChiTietChungChi : Window
{
    private readonly ET_CHUNGCHINHANVIEN _chungChi;
    private readonly BUS_CHUNGCHINHANVIEN _busChungChi = new();
    private readonly BUS_NHANVIEN _busNhanVien = new();
    public bool DaThayDoi { get; private set; }

    public ChiTietChungChi(ET_CHUNGCHINHANVIEN chungChi)
    {
        InitializeComponent();
        _chungChi = chungChi;

        LoadDanhSachNhanVien();
        GanDuLieuBanDau();
        DangKySuKienXoaBaoDo();
    }

    private void LoadDanhSachNhanVien()
    {
        var nvList = _busNhanVien.layToanBoNhanVien()?.ToList();
        cboNhanVien.ItemsSource = nvList;
    }

    private void GanDuLieuBanDau()
    {
        txtMaCC.Text = _chungChi.MaCC;
        cboNhanVien.SelectedValue = _chungChi.MaNV;
        txtSoChungChi.Text = _chungChi.SoChungChi;
        txtTenChungChi.Text = _chungChi.TenChungChi;
        txtNoiCap.Text = _chungChi.NoiCap;
        txtGhiChu.Text = _chungChi.GhiChu;

        dpNgayCap.SelectedDate = _chungChi.NgayCap == default ? DateTime.Today : _chungChi.NgayCap;
        dpHanChungChi.SelectedDate = _chungChi.HanChungChi;

        foreach (ComboBoxItem item in cboTrangThai.Items)
        {
            if (string.Equals(item.Content?.ToString(), _chungChi.TrangThai, StringComparison.OrdinalIgnoreCase))
            {
                cboTrangThai.SelectedItem = item;
                break;
            }
        }
        if (cboTrangThai.SelectedItem == null)
            cboTrangThai.SelectedIndex = 0;

        CapNhatHeader();
    }

    private void CapNhatHeader()
    {
        lblHeaderTenCC.Text = _chungChi.TenChungChi;
        lblHeaderTrangThai.Text = _chungChi.TrangThai;
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

    private void btnLuu_Click(object sender, RoutedEventArgs e)
    {
        if (!KiemTraDuLieuDauVao(out string thongBaoLoi))
        {
            MessageBox.Show(thongBaoLoi, "Kiểm tra dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _chungChi.MaNV = cboNhanVien.SelectedValue?.ToString() ?? "";
        _chungChi.SoChungChi = txtSoChungChi.Text.Trim();
        _chungChi.TenChungChi = txtTenChungChi.Text.Trim();
        _chungChi.NoiCap = txtNoiCap.Text.Trim();
        _chungChi.NgayCap = dpNgayCap.SelectedDate!.Value;
        _chungChi.HanChungChi = dpHanChungChi.SelectedDate;
        _chungChi.TrangThai = (cboTrangThai.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? cboTrangThai.Text;
        _chungChi.GhiChu = txtGhiChu.Text.Trim();

        try
        {
            if (_busChungChi.suaChungChiNhanVien(_chungChi))
            {
                DaThayDoi = true;
                CapNhatHeader();
                MessageBox.Show("Cập nhật chứng chỉ thành công!");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi cập nhật chứng chỉ: " + ex.Message);
        }
    }

    private void btnDong_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
