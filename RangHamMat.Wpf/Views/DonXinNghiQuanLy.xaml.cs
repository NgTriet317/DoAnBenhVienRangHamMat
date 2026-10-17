using BUS;
using ET;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RangHamMat.Views;

public partial class DonXinNghiQuanLy : UserControl
{
    private BUS_DON_XIN_NGHI bus = new BUS_DON_XIN_NGHI();
    private List<ET_DON_XIN_NGHI_HIENTHI> danhSach = new List<ET_DON_XIN_NGHI_HIENTHI>();
    private ET_DON_XIN_NGHI_HIENTHI? donDangXem;
    private bool daNapDuLieu;
    private BUS_PHONG busPhong = new BUS_PHONG();

    public event EventHandler? QuayLai;

    public DonXinNghiQuanLy()
    {
        InitializeComponent();
        cboTrangThaiDon.SelectedIndex = 1; // Mặc định xem các đơn chờ duyệt.
        if (!PhienDangNhap.IsAdmin)
        {
            MessageBox.Show("Chỉ quản lý được xem và duyệt đơn xin nghỉ.", "Không có quyền");
            IsEnabled = false;
            return;
        }
        TaiDuLieu();
    }

    private void TaiDuLieu()
    {
        try
        {
            danhSach = bus.layDanhSachDon();
            string chuyenKhoaDaChon = "Tất cả chuyên khoa";
            if (cboChuyenKhoaDon.SelectedItem is string giaTri)
                chuyenKhoaDaChon = giaTri;

            cboChuyenKhoaDon.Items.Clear();
            cboChuyenKhoaDon.Items.Add("Tất cả chuyên khoa");
            foreach (ET_DON_XIN_NGHI_HIENTHI don in danhSach)
            {
                if (!cboChuyenKhoaDon.Items.Contains(don.TenCK))
                    cboChuyenKhoaDon.Items.Add(don.TenCK);
            }
            cboChuyenKhoaDon.SelectedIndex = 0;
            if (cboChuyenKhoaDon.Items.Contains(chuyenKhoaDaChon))
                cboChuyenKhoaDon.SelectedItem = chuyenKhoaDaChon;
            daNapDuLieu = true;
            LocDanhSach();
        }
        catch (Exception ex)
        {
            daNapDuLieu = false;
            dgvDonXinNghi.ItemsSource = null;
            txtSoDon.Text = "Không thể tải dữ liệu";
            MessageBox.Show("Không thể tải danh sách đơn xin nghỉ: " + ex.Message,
                "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LocDanhSach()
    {
        if (!daNapDuLieu) return;
        string tuKhoa = txtTenNhanVien.Text.Trim();
        string trangThai = "Tất cả trạng thái";
        if (cboTrangThaiDon.SelectedItem is ComboBoxItem mucTrangThai)
            trangThai = mucTrangThai.Content.ToString()!;
        string chuyenKhoa = "Tất cả chuyên khoa";
        if (cboChuyenKhoaDon.SelectedItem is string mucChuyenKhoa)
            chuyenKhoa = mucChuyenKhoa;

        List<ET_DON_XIN_NGHI_HIENTHI> ketQua = new List<ET_DON_XIN_NGHI_HIENTHI>();
        foreach (ET_DON_XIN_NGHI_HIENTHI don in danhSach)
        {
            if (!string.IsNullOrWhiteSpace(tuKhoa) &&
                don.HoTen.IndexOf(tuKhoa, StringComparison.CurrentCultureIgnoreCase) < 0)
                continue;
            if (trangThai != "Tất cả trạng thái" && don.TrangThaiDuyet != trangThai)
                continue;
            if (chuyenKhoa != "Tất cả chuyên khoa" && don.TenCK != chuyenKhoa)
                continue;
            ketQua.Add(don);
        }
        dgvDonXinNghi.ItemsSource = ketQua;
        int choDuyet = 0;
        foreach (ET_DON_XIN_NGHI_HIENTHI don in danhSach)
        {
            if (don.TrangThaiDuyet == "Chờ duyệt") choDuyet++;
        }
        txtSoDon.Text = ketQua.Count + " đơn hiển thị / " + danhSach.Count + " đơn · " + choDuyet + " chờ duyệt";
    }

    private void BoLoc_ThayDoi(object sender, RoutedEventArgs e)
    {
        if (daNapDuLieu) LocDanhSach();
    }

    private void TaiLai_Click(object sender, RoutedEventArgs e)
    {
        TaiDuLieu();
    }

    private void QuayLai_Click(object sender, RoutedEventArgs e)
    {
        if (QuayLai != null) QuayLai(this, EventArgs.Empty);
    }

    private void dgvDonXinNghi_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (dgvDonXinNghi.SelectedItem is not ET_DON_XIN_NGHI_HIENTHI don) return;
        donDangXem = don;
        txtMaDon.Text = "Mã đơn: " + don.MaDonNghi;
        txtHoTen.Text = don.HoTen;
        txtThongTinNhanVien.Text = don.TenCK;
        txtLoaiDon.Text = don.LoaiDon;
        txtTrangThaiChiTiet.Text = don.TrangThaiDuyet;
        txtThoiGian.Text = don.TuNgay.ToString("dd/MM/yyyy") + " – " + don.DenNgay.ToString("dd/MM/yyyy");
        txtLyDo.Text = don.LyDo;
        txtGhiChu.Text = don.GhiChu;
        if (!string.IsNullOrWhiteSpace(don.LyDoKhongDuyet) && !string.IsNullOrWhiteSpace(don.GhiChu))
        {
            int viTri = don.GhiChu.LastIndexOf("Lý do không duyệt: ", StringComparison.OrdinalIgnoreCase);
            if (viTri >= 0) txtGhiChu.Text = don.GhiChu.Substring(0, viTri).Trim();
        }
        txtXuLy.Text = "Gửi ngày " + don.NgayGui.ToString("dd/MM/yyyy HH:mm");
        if (don.NgayDuyet.HasValue)
            txtXuLy.Text += "\nNgười xử lý: " + don.TenNguoiDuyet + " · " +
                don.NgayDuyet.Value.ToString("dd/MM/yyyy HH:mm");

        ThongTinTuChoi.Visibility = Visibility.Collapsed;
        if (!string.IsNullOrWhiteSpace(don.LyDoKhongDuyet))
        {
            txtLyDoTuChoi.Text = don.LyDoKhongDuyet;
            ThongTinTuChoi.Visibility = Visibility.Visible;
        }
        NhapLyDoTuChoi.Visibility = Visibility.Collapsed;
        CacNutTuChoi.Visibility = Visibility.Collapsed;
        CacNutXuLy.Visibility = Visibility.Collapsed;
        if (don.TrangThaiDuyet == "Chờ duyệt")
            CacNutXuLy.Visibility = Visibility.Visible;
        KhuPhanCongThayThe.Visibility = Visibility.Visible;
        btnPhanCongThayThe.IsEnabled = false;
        btnHuyPhanCongThayThe.IsEnabled = false;
        TaiCaCanThayThe();
        TaiLichKhamCanXacNhan();
        txtNhapLyDoTuChoi.Clear();
        ChiTietOverlay.Visibility = Visibility.Visible;
    }

    private void TaiCaCanThayThe()
    {
        if (donDangXem == null) return;
        try
        {
            List<ET_CA_CAN_THAY_THE> ca = bus.layCaCanThayThe(donDangXem.MaDonNghi);
            dgvCaCanThay.ItemsSource = ca;
            int chuaThay = 0;
            foreach (ET_CA_CAN_THAY_THE item in ca)
            {
                if (string.IsNullOrWhiteSpace(item.MaLLVThayThe)) chuaThay++;
            }
            if (ca.Count == 0)
                txtThongTinCaThay.Text = "Không có ca làm đã xác nhận hoặc chờ xác nhận trong thời gian xin nghỉ.";
            else
                txtThongTinCaThay.Text = "Từ " + donDangXem.TuNgay.ToString("dd/MM/yyyy") + " đến " +
                    donDangXem.DenNgay.ToString("dd/MM/yyyy") + ": " + ca.Count +
                    " ca bị ảnh hưởng, " + chuaThay + " ca chưa được bố trí người thay thế.";
            cboNhanVienThay.ItemsSource = bus.layNhanVienThayThe(donDangXem.MaDonNghi);
            cboPhongThay.ItemsSource = busPhong.layToanBoPhong();
            if (ca.Count > 0)
            {
                dgvCaCanThay.SelectedIndex = 0;
                ET_CA_CAN_THAY_THE caDau = ca[0];
                cboPhongThay.SelectedValue = caDau.MaPhong;
                btnPhanCongThayThe.IsEnabled = donDangXem.TrangThaiDuyet == "Chờ duyệt" &&
                    string.IsNullOrWhiteSpace(caDau.MaLLVThayThe);
                btnHuyPhanCongThayThe.IsEnabled = donDangXem.TrangThaiDuyet == "Chờ duyệt" &&
                    !string.IsNullOrWhiteSpace(caDau.MaLLVThayThe);
            }
        }
        catch (Exception ex)
        {
            dgvCaCanThay.ItemsSource = null;
            txtThongTinCaThay.Text = "Không tải được danh sách ca: " + ex.Message;
        }
    }

    private void ChonCaCanThay_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (cboPhongThay == null) return;
        if (dgvCaCanThay.SelectedItem is ET_CA_CAN_THAY_THE ca)
        {
            cboPhongThay.SelectedValue = ca.MaPhong;
            btnPhanCongThayThe.IsEnabled = donDangXem != null &&
                donDangXem.TrangThaiDuyet == "Chờ duyệt" &&
                string.IsNullOrWhiteSpace(ca.MaLLVThayThe);
            btnHuyPhanCongThayThe.IsEnabled = donDangXem != null &&
                donDangXem.TrangThaiDuyet == "Chờ duyệt" &&
                !string.IsNullOrWhiteSpace(ca.MaLLVThayThe);
        }
    }

    private void PhanCongThayThe_Click(object sender, RoutedEventArgs e)
    {
        if (!PhienDangNhap.IsAdmin || donDangXem == null) return;
        if (dgvCaCanThay.SelectedItem is not ET_CA_CAN_THAY_THE ca) return;
        if (cboNhanVienThay.SelectedValue == null || cboPhongThay.SelectedValue == null)
        {
            MessageBox.Show("Chọn nhân viên và phòng thay thế.");
            return;
        }
        string maNV = cboNhanVienThay.SelectedValue.ToString()!;
        string maPhong = cboPhongThay.SelectedValue.ToString()!;
        try
        {
            bus.phanCongThayThe(donDangXem.MaDonNghi, ca.MaLLV, maNV, maPhong);
            TaiCaCanThayThe();
            TaiLichKhamCanXacNhan();
            MessageBox.Show("Đã ghi nhận phương án thay ca. Ca sẽ được xác nhận khi đơn nghỉ được duyệt.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể phân công thay thế: " + ex.Message);
            TaiCaCanThayThe();
            TaiLichKhamCanXacNhan();
        }
    }

    private void HuyPhanCongThayThe_Click(object sender, RoutedEventArgs e)
    {
        if (!PhienDangNhap.IsAdmin || donDangXem == null) return;
        if (dgvCaCanThay.SelectedItem is not ET_CA_CAN_THAY_THE ca) return;
        if (MessageBox.Show("Hủy phương án thay hiện tại để chọn người khác?", "Đổi người thay thế",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            if (!bus.huyPhanCongThayThe(donDangXem.MaDonNghi,ca.MaLLV))
                MessageBox.Show("Phương án đã thay đổi. Hãy tải lại.");
            TaiCaCanThayThe();
            TaiLichKhamCanXacNhan();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể đổi người thay thế: " + ex.Message);
            TaiCaCanThayThe();
            TaiLichKhamCanXacNhan();
        }
    }

    private void TaiLichKhamCanXacNhan()
    {
        if (donDangXem == null) return;
        dgvLichKhamXacNhan.ItemsSource = null;
        btnXacNhanLichKham.IsEnabled = false;
        txtChiTietLichXacNhan.Text = "";
        try
        {
            List<ET_LICH_KHAM_CAN_XAC_NHAN> lich = bus.layLichKhamCanXacNhan(donDangXem.MaDonNghi);
            dgvLichKhamXacNhan.ItemsSource = lich;
            int chuaXacNhan = 0;
            foreach (ET_LICH_KHAM_CAN_XAC_NHAN item in lich)
            {
                if (!item.DaXacNhan) chuaXacNhan++;
            }
            if (donDangXem.TrangThaiDuyet == "Chờ duyệt")
                txtThongTinXacNhanLich.Text = "Có " + lich.Count + " lịch khám bị ảnh hưởng; " +
                    chuaXacNhan + " lịch chưa được xác nhận riêng.";
            else
                txtThongTinXacNhanLich.Text = "Lịch sử " + lich.Count + " xác nhận lịch khám của đơn đã xử lý.";
            if (lich.Count > 0) dgvLichKhamXacNhan.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            txtThongTinXacNhanLich.Text = "Không tải được lịch khám: " + ex.Message +
                ". Nếu chưa có bảng XAC_NHAN_DOI_LICH_KHAM, hãy chạy SQL bổ sung.";
        }
    }

    private void ChonLichCanXacNhan_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (btnXacNhanLichKham == null) return;
        btnXacNhanLichKham.IsEnabled = false;
        txtChiTietLichXacNhan.Text = "Chọn lịch khám cần xác nhận.";
        if (dgvLichKhamXacNhan.SelectedItem is ET_LICH_KHAM_CAN_XAC_NHAN lich)
        {
            txtChiTietLichXacNhan.Text = "Mã lịch: " + lich.MaLich +
                " | Ngày: " + lich.NgayGioKham.ToString("dd/MM/yyyy HH:mm") +
                " | Bác sĩ hiện tại: " + lich.TenBSCu + " / " + lich.TenPhongCu +
                "\nNgười thay thế: " + lich.TenBSMoi + " / " + lich.TenPhongMoi;
            if (lich.DaXacNhan && lich.NgayXacNhan.HasValue)
                txtChiTietLichXacNhan.Text += "\nNgười xác nhận: " + lich.TenNguoiXacNhan +
                    " lúc " + lich.NgayXacNhan.Value.ToString("dd/MM/yyyy HH:mm");
            btnXacNhanLichKham.IsEnabled = donDangXem != null &&
                donDangXem.TrangThaiDuyet == "Chờ duyệt" && !lich.DaXacNhan &&
                !string.IsNullOrWhiteSpace(lich.MaLLVThayThe);
        }
    }

    private void XacNhanLichKham_Click(object sender, RoutedEventArgs e)
    {
        if (!PhienDangNhap.IsAdmin || donDangXem == null) return;
        if (dgvLichKhamXacNhan.SelectedItem is not ET_LICH_KHAM_CAN_XAC_NHAN lich) return;
        if (string.IsNullOrWhiteSpace(PhienDangNhap.MaNV))
        {
            MessageBox.Show("Tài khoản quản lý chưa liên kết nhân viên.");
            return;
        }
        if (MessageBox.Show("Xác nhận chuyển RIÊNG lịch " + lich.MaLich +
            " (" + lich.TenBenhNhan + ", " + lich.NgayGioKham.ToString("dd/MM/yyyy HH:mm") + ")\n" +
            "Từ bác sĩ " + lich.TenBSCu + " / " + lich.TenPhongCu +
            " sang " + lich.TenBSMoi + " / " + lich.TenPhongMoi + "?",
            "Xác nhận từng lịch khám", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            bus.xacNhanTungLichKham(donDangXem.MaDonNghi, lich.MaLich, PhienDangNhap.MaNV);
            TaiLichKhamCanXacNhan();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể xác nhận lịch: " + ex.Message, "Lỗi dữ liệu",
                MessageBoxButton.OK, MessageBoxImage.Error);
            TaiLichKhamCanXacNhan();
        }
    }

    private void DongChiTiet_Click(object sender, RoutedEventArgs e)
    {
        ChiTietOverlay.Visibility = Visibility.Collapsed;
        donDangXem = null;
    }

    private void DuyetDon_Click(object sender, RoutedEventArgs e)
    {
        if (donDangXem == null) return;
        try
        {
            List<ET_LICH_KHAM_CAN_XAC_NHAN> lichCanXacNhan = bus.layLichKhamCanXacNhan(donDangXem.MaDonNghi);
            foreach (ET_LICH_KHAM_CAN_XAC_NHAN lich in lichCanXacNhan)
            {
                if (!lich.DaXacNhan)
                {
                    MessageBox.Show("Lịch khám " + lich.MaLich +
                        " chưa được xác nhận riêng hoặc đã bị thay đổi. Hãy xác nhận từng lịch trước khi duyệt.",
                        "Chưa xác nhận đủ lịch", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không kiểm tra được lịch khám: " + ex.Message,
                "Không thể duyệt", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (MessageBox.Show("Duyệt đơn " + donDangXem.MaDonNghi + " của " + donDangXem.HoTen +
            "? Tất cả lịch khám bị ảnh hưởng đã được xác nhận riêng và sẽ được chuyển theo phương án thay ca.",
            "Xác nhận duyệt", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        LuuXuLyDon(true, "");
    }

    private void KhongDuyet_Click(object sender, RoutedEventArgs e)
    {
        NhapLyDoTuChoi.Visibility = Visibility.Visible;
        CacNutXuLy.Visibility = Visibility.Collapsed;
        CacNutTuChoi.Visibility = Visibility.Visible;
        txtNhapLyDoTuChoi.Focus();
    }

    private void HuyTuChoi_Click(object sender, RoutedEventArgs e)
    {
        NhapLyDoTuChoi.Visibility = Visibility.Collapsed;
        CacNutTuChoi.Visibility = Visibility.Collapsed;
        CacNutXuLy.Visibility = Visibility.Visible;
        txtNhapLyDoTuChoi.Clear();
    }

    private void XacNhanTuChoi_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtNhapLyDoTuChoi.Text))
        {
            MessageBox.Show("Vui lòng nhập lý do không duyệt đơn.", "Thiếu lý do");
            txtNhapLyDoTuChoi.Focus();
            return;
        }
        if (MessageBox.Show("Xác nhận không duyệt đơn này?", "Xác nhận không duyệt",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        LuuXuLyDon(false, txtNhapLyDoTuChoi.Text.Trim());
    }

    private void LuuXuLyDon(bool duyet, string lyDo)
    {
        if (!PhienDangNhap.IsAdmin || donDangXem == null) return;
        if (string.IsNullOrWhiteSpace(PhienDangNhap.MaNV))
        {
            MessageBox.Show("Tài khoản quản lý chưa liên kết nhân viên.", "Thiếu thông tin");
            return;
        }
        if (donDangXem.MaNV == PhienDangNhap.MaNV)
        {
            MessageBox.Show("Không thể tự xét duyệt đơn của chính mình.", "Không hợp lệ");
            return;
        }
        try
        {
            bool thanhCong = bus.xuLyDon(donDangXem.MaDonNghi, PhienDangNhap.MaNV, duyet, lyDo);
            if (!thanhCong)
            {
                MessageBox.Show("Đơn đã được xử lý bởi người khác hoặc không còn chờ duyệt. Hãy tải lại.",
                    "Dữ liệu đã thay đổi", MessageBoxButton.OK, MessageBoxImage.Warning);
                ChiTietOverlay.Visibility = Visibility.Collapsed;
                donDangXem = null;
                TaiDuLieu();
                return;
            }
            ChiTietOverlay.Visibility = Visibility.Collapsed;
            donDangXem = null;
            TaiDuLieu();
            MessageBox.Show("Đã cập nhật trạng thái đơn thành công.", "Hoàn tất",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể xử lý đơn: " + ex.Message, "Lỗi lưu đơn",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
