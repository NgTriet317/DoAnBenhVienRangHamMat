using BUS;
using ET;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RangHamMat.Views;

/// <summary>
/// Phần xử lý đơn xin nghỉ quản lý của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class DonXinNghiQuanLy : UserControl
{
    /// <summary>
    /// Đối tượng nghiệp vụ đơn xin nghỉ được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private BUS_DON_XIN_NGHI bus = new BUS_DON_XIN_NGHI();
    /// <summary>
    /// Danh sách  dùng trong đơn xin nghỉ quản lý được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private List<ET_DON_XIN_NGHI_HIENTHI> danhSach = new List<ET_DON_XIN_NGHI_HIENTHI>();
    /// <summary>
    /// Đơn đang xem được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private ET_DON_XIN_NGHI_HIENTHI? donDangXem;
    /// <summary>
    /// Cờ Đã nạp dữ liệu; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool daNapDuLieu;
    /// <summary>
    /// Đối tượng nghiệp vụ phòng được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private BUS_PHONG busPhong = new BUS_PHONG();

    /// <summary>
    /// Thông báo quay lại cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    public event EventHandler? QuayLai;

    /// <summary>
    /// Khởi tạo đơn xin nghỉ quản lý. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
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

    /// <summary>
    /// Đọc lại dữ liệu theo phạm vi hiện tại, áp dụng bộ lọc và cập nhật danh sách/chi tiết.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>bus.layDanhSachDon</c>, <c>cboChuyenKhoaDon.Items.Clear</c>, <c>cboChuyenKhoaDon.Items.Add</c>, <c>cboChuyenKhoaDon.Items.Contains</c>.</remarks>
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

    /// <summary>
    /// Lọc danh sách nguồn theo tiêu chí trên giao diện mà không thay đổi dữ liệu trong SQL.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>txtTenNhanVien.Text.Trim</c>, <c>mucTrangThai.Content.ToString</c>, <c>don.HoTen.IndexOf</c>, <c>ketQua.Add</c>.</remarks>
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

    /// <summary>
    /// Tiếp nhận thay đổi bộ lọc và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>LocDanhSach</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void BoLoc_ThayDoi(object sender, RoutedEventArgs e)
    {
        if (daNapDuLieu) LocDanhSach();
    }

    /// <summary>
    /// Xử lý thao tác tải lại khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TaiDuLieu</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void TaiLai_Click(object sender, RoutedEventArgs e)
    {
        TaiDuLieu();
    }

    /// <summary>
    /// Xử lý thao tác quay lại khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>QuayLai</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void QuayLai_Click(object sender, RoutedEventArgs e)
    {
        if (QuayLai != null) QuayLai(this, EventArgs.Empty);
    }

    /// <summary>
    /// Mở nội dung hoặc thao tác chi tiết của bảng đơn xin nghỉ khi người dùng bấm đúp.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>don.TuNgay.ToString</c>, <c>don.DenNgay.ToString</c>, <c>don.GhiChu.LastIndexOf</c>, <c>don.GhiChu.Substring</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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
        KhuPhanCongThayThe.Visibility = Visibility.Collapsed;
        if (don.TrangThaiDuyet == "Đã duyệt") KhuPhanCongThayThe.Visibility = Visibility.Visible;
        btnPhanCongThayThe.IsEnabled = false;
        btnHuyPhanCongThayThe.IsEnabled = false;
        TaiCaCanThayThe();
        TaiLichKhamCanXacNhan();
        txtNhapLyDoTuChoi.Clear();
        ChiTietOverlay.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Nạp các ca của đơn nghỉ đang chọn để quản lý phân công người thay.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>bus.layCaCanThayThe</c>, <c>donDangXem.TuNgay.ToString</c>, <c>donDangXem.DenNgay.ToString</c>, <c>bus.layNhanVienThayThe</c>.</remarks>
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
                btnPhanCongThayThe.IsEnabled = donDangXem.TrangThaiDuyet == "Đã duyệt" &&
                    string.IsNullOrWhiteSpace(caDau.MaLLVThayThe);
                btnHuyPhanCongThayThe.IsEnabled = donDangXem.TrangThaiDuyet == "Đã duyệt" &&
                    !string.IsNullOrWhiteSpace(caDau.MaLLVThayThe);
            }
        }
        catch (Exception ex)
        {
            dgvCaCanThay.ItemsSource = null;
            txtThongTinCaThay.Text = "Không tải được danh sách ca: " + ex.Message;
        }
    }

    /// <summary>
    /// Tiếp nhận thay đổi chọn ca cần thay và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ChonCaCanThay_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (cboPhongThay == null) return;
        if (dgvCaCanThay.SelectedItem is ET_CA_CAN_THAY_THE ca)
        {
            cboPhongThay.SelectedValue = ca.MaPhong;
            btnPhanCongThayThe.IsEnabled = donDangXem != null &&
                donDangXem.TrangThaiDuyet == "Đã duyệt" &&
                string.IsNullOrWhiteSpace(ca.MaLLVThayThe);
            btnHuyPhanCongThayThe.IsEnabled = donDangXem != null &&
                donDangXem.TrangThaiDuyet == "Đã duyệt" &&
                !string.IsNullOrWhiteSpace(ca.MaLLVThayThe);
        }
    }

    /// <summary>
    /// Xử lý thao tác phân công thay thế khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>cboNhanVienThay.SelectedValue.ToString</c>, <c>cboPhongThay.SelectedValue.ToString</c>, <c>bus.phanCongThayThe</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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
            MessageBox.Show("Đã phân công ca thay thế. Tiếp tục xác nhận riêng từng lịch khám bị ảnh hưởng.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể phân công thay thế: " + ex.Message);
            TaiCaCanThayThe();
            TaiLichKhamCanXacNhan();
        }
    }

    /// <summary>
    /// Xử lý thao tác hủy phân công thay thế khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>bus.huyPhanCongThayThe</c>, <c>TaiCaCanThayThe</c>, <c>TaiLichKhamCanXacNhan</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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

    /// <summary>
    /// Nạp các lịch khám bị ảnh hưởng bởi đơn nghỉ cần đối chiếu và xác nhận.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>bus.layLichKhamCanXacNhan</c>.</remarks>
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
            if (donDangXem.TrangThaiDuyet == "Đã duyệt")
                txtThongTinXacNhanLich.Text = "Có " + lich.Count + " lịch khám bị ảnh hưởng; " +
                    chuaXacNhan + " lịch chưa được xác nhận riêng.";
            else
                txtThongTinXacNhanLich.Text = "Chỉ thực hiện thay ca và chuyển lịch khám cho đơn đã duyệt.";
            if (lich.Count > 0) dgvLichKhamXacNhan.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            txtThongTinXacNhanLich.Text = "Không tải được lịch khám: " + ex.Message +
                ". Nếu chưa có bảng XAC_NHAN_DOI_LICH_KHAM, hãy chạy SQL bổ sung.";
        }
    }

    /// <summary>
    /// Tiếp nhận thay đổi chọn lịch cần xác nhận và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>lich.NgayGioKham.ToString</c>, <c>lich.NgayXacNhan.Value.ToString</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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
                "\nBác sĩ dự bị đã chọn: " + lich.TenBSDuBi +
                "\nNgười thay thế: " + lich.TenBSMoi + " / " + lich.TenPhongMoi;
            if (lich.DaXacNhan && lich.NgayXacNhan.HasValue)
                txtChiTietLichXacNhan.Text += "\nNgười xác nhận: " + lich.TenNguoiXacNhan +
                    " lúc " + lich.NgayXacNhan.Value.ToString("dd/MM/yyyy HH:mm");
            btnXacNhanLichKham.IsEnabled = donDangXem != null &&
                donDangXem.TrangThaiDuyet == "Đã duyệt" && !lich.DaXacNhan &&
                !string.IsNullOrWhiteSpace(lich.MaLLVThayThe);
        }
    }

    /// <summary>
    /// Xử lý thao tác xác nhận lịch khám khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>lich.NgayGioKham.ToString</c>, <c>bus.xacNhanTungLichKham</c>, <c>TaiLichKhamCanXacNhan</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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

    /// <summary>
    /// Xử lý thao tác đóng chi tiết khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void DongChiTiet_Click(object sender, RoutedEventArgs e)
    {
        ChiTietOverlay.Visibility = Visibility.Collapsed;
        donDangXem = null;
    }

    /// <summary>
    /// Xử lý thao tác duyệt đơn khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>LuuXuLyDon</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void DuyetDon_Click(object sender, RoutedEventArgs e)
    {
        if (donDangXem == null) return;
        if (MessageBox.Show("Duyệt đơn cho " + donDangXem.HoTen +
            "? Lịch khám cũ sẽ được giữ nguyên và báo Cần phân công lại cho đến khi quản lý bố trí người thay và xác nhận riêng từng lịch.",
            "Duyệt đơn trước", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        LuuXuLyDon(true, "");
    }

    /// <summary>
    /// Xử lý thao tác không duyệt khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>txtNhapLyDoTuChoi.Focus</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void KhongDuyet_Click(object sender, RoutedEventArgs e)
    {
        NhapLyDoTuChoi.Visibility = Visibility.Visible;
        CacNutXuLy.Visibility = Visibility.Collapsed;
        CacNutTuChoi.Visibility = Visibility.Visible;
        txtNhapLyDoTuChoi.Focus();
    }

    /// <summary>
    /// Xử lý thao tác hủy từ chối khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>txtNhapLyDoTuChoi.Clear</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void HuyTuChoi_Click(object sender, RoutedEventArgs e)
    {
        NhapLyDoTuChoi.Visibility = Visibility.Collapsed;
        CacNutTuChoi.Visibility = Visibility.Collapsed;
        CacNutXuLy.Visibility = Visibility.Visible;
        txtNhapLyDoTuChoi.Clear();
    }

    /// <summary>
    /// Xử lý thao tác xác nhận từ chối khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>txtNhapLyDoTuChoi.Focus</c>, <c>LuuXuLyDon</c>, <c>txtNhapLyDoTuChoi.Text.Trim</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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

    /// <summary>
    /// Gửi kết quả duyệt/từ chối đơn với người xử lý và lý do, rồi làm mới dữ liệu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>bus.xuLyDon</c>, <c>TaiDuLieu</c>.</remarks>
    /// <param name="duyet">Giá trị duyệt dùng khi thực hiện thao tác; kiểu bool.</param>
    /// <param name="lyDo">Giá trị lý do dùng khi thực hiện thao tác; kiểu string.</param>
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
    /// <summary>
    /// Xử lý thao tác thêm đơn khẩn cấp khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>txtLyDoKhanCap.Clear</c>, <c>txtGhiChuKhanCap.Clear</c>, <c>MessageBox.Show</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ThemDonKhanCap_Click(object sender, RoutedEventArgs e)
    {
        if (!PhienDangNhap.IsAdmin) return;
        try
        {
            cboNhanVienKhanCap.ItemsSource = new BUS_NHANVIEN().layToanBoNhanVien();
            cboNhanVienKhanCap.SelectedIndex = -1;
            dpKhanCapTuNgay.DisplayDateStart = DateTime.Today;
            dpKhanCapDenNgay.DisplayDateStart = DateTime.Today;
            dpKhanCapTuNgay.SelectedDate = DateTime.Today;
            dpKhanCapDenNgay.SelectedDate = DateTime.Today;
            txtLyDoKhanCap.Clear();
            txtGhiChuKhanCap.Clear();
            KhanCapOverlay.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tải danh sách nhân viên: " + ex.Message);
        }
    }

    /// <summary>
    /// Tiếp nhận thay đổi khẩn cấp ngày và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void KhanCapNgay_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (dpKhanCapDenNgay == null || dpKhanCapTuNgay.SelectedDate == null) return;
        dpKhanCapDenNgay.DisplayDateStart = dpKhanCapTuNgay.SelectedDate;
        if (dpKhanCapDenNgay.SelectedDate < dpKhanCapTuNgay.SelectedDate)
            dpKhanCapDenNgay.SelectedDate = dpKhanCapTuNgay.SelectedDate;
    }

    /// <summary>
    /// Xử lý thao tác dòng khẩn cấp khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void DongKhanCap_Click(object sender, RoutedEventArgs e)
    {
        KhanCapOverlay.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Xử lý thao tác lưu khẩn cấp khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>cboNhanVienKhanCap.SelectedValue.ToString</c>, <c>txtLyDoKhanCap.Text.Trim</c>, <c>txtGhiChuKhanCap.Text.Trim</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void LuuKhanCap_Click(object sender, RoutedEventArgs e)
    {
        if (!PhienDangNhap.IsAdmin) return;
        if (cboNhanVienKhanCap.SelectedValue == null || !dpKhanCapTuNgay.SelectedDate.HasValue ||
            !dpKhanCapDenNgay.SelectedDate.HasValue || string.IsNullOrWhiteSpace(txtLyDoKhanCap.Text))
        {
            MessageBox.Show("Chọn nhân viên, thời gian và nhập lý do nghỉ khẩn cấp.");
            return;
        }
        if (string.IsNullOrWhiteSpace(PhienDangNhap.MaNV))
        {
            MessageBox.Show("Tài khoản quản lý chưa liên kết nhân viên.");
            return;
        }
        if (MessageBox.Show("Tạo đơn khẩn cấp cho nhân viên đã chọn và DUYỆT NGAY?\n" +
            "Lịch khám bị ảnh hưởng sẽ chờ phân công lại, không tự chuyển bác sĩ.",
            "Xác nhận tạo đơn", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        ET_DON_XIN_NGHI don = new ET_DON_XIN_NGHI();
        don.MaNV = cboNhanVienKhanCap.SelectedValue.ToString()!;
        don.TuNgay = dpKhanCapTuNgay.SelectedDate.Value.Date;
        don.DenNgay = dpKhanCapDenNgay.SelectedDate.Value.Date;
        don.LyDo = txtLyDoKhanCap.Text.Trim();
        don.GhiChu = txtGhiChuKhanCap.Text.Trim();
        try
        {
            string maDon = bus.taoDonKhanCap(don,PhienDangNhap.MaNV);
            KhanCapOverlay.Visibility = Visibility.Collapsed;
            cboTrangThaiDon.SelectedIndex = 0;
            TaiDuLieu();
            MessageBox.Show("Đã tạo và duyệt đơn " + maDon + ". Hãy phân ca thay thế và xác nhận từng lịch khám.",
                "Thành công",MessageBoxButton.OK,MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tạo đơn khẩn cấp: " + ex.Message,
                "Lỗi",MessageBoxButton.OK,MessageBoxImage.Error);
        }
    }

}
