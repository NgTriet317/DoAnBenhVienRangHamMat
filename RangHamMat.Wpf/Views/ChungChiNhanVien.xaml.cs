using BUS;
using ET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RangHamMat.Views;

public partial class ChungChiNhanVien : UserControl
{
    private BUS_CHUNGCHINHANVIEN busChungChi = new();
    private BUS_NHANVIEN busNhanVien = new();

    private List<ET_CHUNGCHINHANVIEN> danhSachChungChi = new();
    private List<ET_NHANVIEN> danhSachNhanVien = new();
    private ET_NHANVIEN? nhanVienDangChon;

    public ChungChiNhanVien()
    {
        InitializeComponent();

        LoadDuLieu();
    }

    private void LoadDuLieu()
    {
        LoadDanhSachNhanVien();
        LoadDanhSachChungChi();
    }

    private void LoadDanhSachNhanVien()
    {

        danhSachNhanVien = busNhanVien.layToanBoNhanVien().ToList();
        LocDanhSachNhanVien();

    }

    private void LocDanhSachNhanVien()
    {
        if (lstNhanVien == null || danhSachNhanVien == null) return;

        string tuKhoa = txtTimNhanVien?.Text?.Trim().ToLower() ?? "";
        var ketQua = danhSachNhanVien.Where(nv =>
            string.IsNullOrWhiteSpace(tuKhoa) ||
            (nv.MaNV != null && nv.MaNV.ToLower().Contains(tuKhoa)) ||
            (nv.HoTen != null && nv.HoTen.ToLower().Contains(tuKhoa))
        ).ToList();

        lstNhanVien.ItemsSource = ketQua;
    }

    private void LoadDanhSachChungChi()
    {
        try
        {
            danhSachChungChi = busChungChi.layToanBoChungChiNhanVien().ToList();
            CapNhatThongKe();
            ApDungBoLocChungChi();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi tải danh sách chứng chỉ: " + ex.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CapNhatThongKe()
    {
        if (lblTongChungChi == null || lblSapHetHan == null || lblConHieuLuc == null || danhSachChungChi == null) return;

        int tong = danhSachChungChi.Count;
        int sapHetHan = danhSachChungChi.Count(cc =>
            string.Equals(cc.TrangThai, "Sắp hết hạn", StringComparison.OrdinalIgnoreCase) ||
            (cc.HanChungChi.HasValue && cc.HanChungChi.Value >= DateTime.Today && cc.HanChungChi.Value <= DateTime.Today.AddDays(60))
        );
        int conHieuLuc = danhSachChungChi.Count(cc =>
            string.Equals(cc.TrangThai, "Còn hiệu lực", StringComparison.OrdinalIgnoreCase)
        );

        lblTongChungChi.Text = tong.ToString();
        lblSapHetHan.Text = sapHetHan.ToString();
        lblConHieuLuc.Text = conHieuLuc.ToString();
    }

    private void ApDungBoLocChungChi()
    {
        if (dgvChungChiNhanVien == null || danhSachChungChi == null) return;

        string locSoCC = txtLocSoCC?.Text?.Trim().ToLower() ?? "";
        string locTenCC = txtLocTenCC?.Text?.Trim().ToLower() ?? "";
        string locTrangThai = (cboLocTrangThai?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tất cả";

        var ketQua = danhSachChungChi.Where(cc =>
        {
            // Lọc theo nhân viên được chọn ở cột trái (nếu có)
            if (nhanVienDangChon != null && !string.Equals(cc.MaNV, nhanVienDangChon.MaNV, StringComparison.OrdinalIgnoreCase))
                return false;

            // Lọc theo số chứng chỉ
            if (!string.IsNullOrWhiteSpace(locSoCC) && (cc.SoChungChi == null || !cc.SoChungChi.ToLower().Contains(locSoCC)))
                return false;

            // Lọc theo tên chứng chỉ
            if (!string.IsNullOrWhiteSpace(locTenCC) && (cc.TenChungChi == null || !cc.TenChungChi.ToLower().Contains(locTenCC)))
                return false;

            // Lọc theo trạng thái
            if (locTrangThai != "Tất cả" && !string.Equals(cc.TrangThai, locTrangThai, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }).ToList();

        dgvChungChiNhanVien.ItemsSource = ketQua;
    }

    private void txtTimNhanVien_TextChanged(object sender, TextChangedEventArgs e)
    {
        LocDanhSachNhanVien();
    }

    private void lstNhanVien_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (lstNhanVien.SelectedItem is ET_NHANVIEN nv)
        {
            nhanVienDangChon = nv;
            lblNhanVienChonTen.Text = nv.HoTen;
            lblNhanVienChonChiTiet.Text = $"Mã NV: {nv.MaNV} | SĐT: {nv.SDT}";
        }
        else
        {
            nhanVienDangChon = null;
            lblNhanVienChonTen.Text = "Tất cả nhân viên";
            lblNhanVienChonChiTiet.Text = "Hiển thị toàn bộ chứng chỉ";
        }

        ApDungBoLocChungChi();
    }

    private void btnXemTatCa_Click(object sender, RoutedEventArgs e)
    {
        lstNhanVien.SelectedIndex = -1;
        nhanVienDangChon = null;
        lblNhanVienChonTen.Text = "Tất cả nhân viên";
        lblNhanVienChonChiTiet.Text = "Hiển thị toàn bộ chứng chỉ";
        ApDungBoLocChungChi();
    }

    private void btnThemChungChi_Click(object sender, RoutedEventArgs e)
    {
        var frmThem = new CuaSoThemChungChi(nhanVienDangChon?.MaNV)
        {
            Owner = Application.Current?.MainWindow
        };

        frmThem.ShowDialog();

        if (frmThem.DaThemThanhCong)
        {
            LoadDanhSachChungChi();
        }
    }

    private void dgvChungChiNhanVien_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (dgvChungChiNhanVien.SelectedItem is not ET_CHUNGCHINHANVIEN chungChi)
            return;

        var frmChiTiet = new ChiTietChungChi(chungChi)
        {
            Owner = Application.Current?.MainWindow
        };

        frmChiTiet.ShowDialog();

        if (frmChiTiet.DaThayDoi)
        {
            LoadDanhSachChungChi();
        }
    }

    private void txtLocChungChi_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApDungBoLocChungChi();
    }

    private void cboLocTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApDungBoLocChungChi();
    }
}
