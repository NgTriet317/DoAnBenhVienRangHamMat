using BUS;
using ET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RangHamMat.Views;

/// <summary>
/// Phần xử lý chứng chỉ nhân viên của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class ChungChiNhanVien : UserControl
{
    /// <summary>
    /// Đối tượng nghiệp vụ chứng chỉ nhân viên được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private BUS_CHUNGCHINHANVIEN busChungChi = new();
    /// <summary>
    /// Đối tượng nghiệp vụ nhân viên được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private BUS_NHANVIEN busNhanVien = new();

    /// <summary>
    /// Danh sách chứng chỉ dùng trong chứng chỉ nhân viên được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private List<ET_CHUNGCHINHANVIEN> danhSachChungChi = new();
    /// <summary>
    /// Danh sách nhân viên dùng trong chứng chỉ nhân viên được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private List<ET_NHANVIEN> danhSachNhanVien = new();
    /// <summary>
    /// Nhân viên đang chọn được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private ET_NHANVIEN? nhanVienDangChon;

    /// <summary>
    /// Khởi tạo chứng chỉ nhân viên. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    public ChungChiNhanVien()
    {
        InitializeComponent();

        LoadDuLieu();
    }

    /// <summary>
    /// Nạp dữ liệu từ các lớp nghiệp vụ, chuẩn bị danh sách lựa chọn và đồng bộ vùng hiển thị của màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>LoadDanhSachNhanVien</c>, <c>LoadDanhSachChungChi</c>.</remarks>
    private void LoadDuLieu()
    {
        LoadDanhSachNhanVien();
        LoadDanhSachChungChi();
    }

    /// <summary>
    /// Nạp nhân viên cho trường lựa chọn hoặc danh sách hồ sơ trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>busNhanVien.layToanBoNhanVien</c>, <c>LocDanhSachNhanVien</c>.</remarks>
    private void LoadDanhSachNhanVien()
    {

        danhSachNhanVien = busNhanVien.layToanBoNhanVien().ToList();
        LocDanhSachNhanVien();

    }

    /// <summary>
    /// Lọc danh sách nhân viên theo từ khóa để chọn hồ sơ cần xem chứng chỉ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>.Trim</c>, <c>danhSachNhanVien.Where</c>, <c>nv.MaNV.ToLower</c>, <c>nv.HoTen.ToLower</c>.</remarks>
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

    /// <summary>
    /// Đọc các chứng chỉ theo nhân viên hoặc phạm vi hiện tại rồi cập nhật danh sách hiển thị.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>busChungChi.layToanBoChungChiNhanVien</c>, <c>CapNhatThongKe</c>, <c>ApDungBoLocChungChi</c>, <c>MessageBox.Show</c>.</remarks>
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

    /// <summary>
    /// Tính lại các chỉ số tổng hợp từ danh sách đang được màn hình quản lý.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>danhSachChungChi.Count</c>, <c>DateTime.Today.AddDays</c>, <c>tong.ToString</c>, <c>sapHetHan.ToString</c>.</remarks>
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

    /// <summary>
    /// Lọc chứng chỉ theo nội dung tìm kiếm và trạng thái đang chọn.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>.Trim</c>, <c>.ToString</c>, <c>danhSachChungChi.Where</c>, <c>cc.SoChungChi.ToLower</c>.</remarks>
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

    /// <summary>
    /// Tiếp nhận thay đổi ô nhập tìm nhân viên và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>LocDanhSachNhanVien</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void txtTimNhanVien_TextChanged(object sender, TextChangedEventArgs e)
    {
        LocDanhSachNhanVien();
    }

    /// <summary>
    /// Phản ánh lựa chọn danh sách nhân viên vào dữ liệu và phần hiển thị liên quan.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ApDungBoLocChungChi</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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

    /// <summary>
    /// Xử lý thao tác nút xem tất ca khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ApDungBoLocChungChi</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnXemTatCa_Click(object sender, RoutedEventArgs e)
    {
        lstNhanVien.SelectedIndex = -1;
        nhanVienDangChon = null;
        lblNhanVienChonTen.Text = "Tất cả nhân viên";
        lblNhanVienChonChiTiet.Text = "Hiển thị toàn bộ chứng chỉ";
        ApDungBoLocChungChi();
    }

    /// <summary>
    /// Xử lý thao tác nút thêm chứng chỉ khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>frmThem.ShowDialog</c>, <c>LoadDanhSachChungChi</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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

    /// <summary>
    /// Mở nội dung hoặc thao tác chi tiết của bảng chứng chỉ nhân viên khi người dùng bấm đúp.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>frmChiTiet.ShowDialog</c>, <c>LoadDanhSachChungChi</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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

    /// <summary>
    /// Tiếp nhận thay đổi ô nhập lọc chứng chỉ và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ApDungBoLocChungChi</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void txtLocChungChi_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApDungBoLocChungChi();
    }

    /// <summary>
    /// Phản ánh lựa chọn hộp chọn lọc trạng thái vào dữ liệu và phần hiển thị liên quan.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ApDungBoLocChungChi</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void cboLocTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApDungBoLocChungChi();
    }
}
