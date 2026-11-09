using BUS;
using ET;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RangHamMat.Views;

/// <summary>
/// Phần xử lý chi tiết chứng chỉ của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class ChiTietChungChi : Window
{
    /// <summary>
    /// Chứng chỉ được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly ET_CHUNGCHINHANVIEN _chungChi;
    /// <summary>
    /// Đối tượng nghiệp vụ chứng chỉ nhân viên được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private readonly BUS_CHUNGCHINHANVIEN _busChungChi = new();
    /// <summary>
    /// Đối tượng nghiệp vụ nhân viên được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private readonly BUS_NHANVIEN _busNhanVien = new();
    /// <summary>
    /// Đã thay đổi.
    /// </summary>
    public bool DaThayDoi { get; private set; }

    /// <summary>
    /// Khởi tạo chi tiết chứng chỉ. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    /// <param name="chungChi">Giá trị chứng chỉ dùng khi thực hiện thao tác; kiểu ET_CHUNGCHINHANVIEN.</param>
    public ChiTietChungChi(ET_CHUNGCHINHANVIEN chungChi)
    {
        InitializeComponent();
        _chungChi = chungChi;

        LoadDanhSachNhanVien();
        GanDuLieuBanDau();
        DangKySuKienXoaBaoDo();
    }

    /// <summary>
    /// Nạp nhân viên cho trường lựa chọn hoặc danh sách hồ sơ trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>_busNhanVien.layToanBoNhanVien</c>, <c>.ToList</c>.</remarks>
    private void LoadDanhSachNhanVien()
    {
        var nvList = _busNhanVien.layToanBoNhanVien()?.ToList();
        cboNhanVien.ItemsSource = nvList;
    }

    /// <summary>
    /// Điền bản dữ liệu gốc vào các trường nhập trước khi cho phép chỉnh sửa.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>.ToString</c>, <c>CapNhatHeader</c>.</remarks>
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

    /// <summary>
    /// Cập nhật tiêu đề và các thông tin nhận diện theo nội dung hồ sơ đang hiển thị.
    /// </summary>
    private void CapNhatHeader()
    {
        lblHeaderTenCC.Text = _chungChi.TenChungChi;
        lblHeaderTrangThai.Text = _chungChi.TrangThai;
    }

    /// <summary>
    /// Tiếp nhận thay đổi trường nhập và xóa cảnh báo liên quan để chuẩn bị kiểm tra lại.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XoaBaoDo</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Control_ThayDoiDuLieu(object? sender, EventArgs e)
    {
        if (sender is Control control)
        {
            XoaBaoDo(control);
        }
    }

    /// <summary>
    /// Đăng ký sự kiện các trường nhập để gỡ đánh dấu lỗi khi người dùng chỉnh lại nội dung.
    /// </summary>
    private void DangKySuKienXoaBaoDo()
    {
        cboNhanVien.SelectionChanged += Control_ThayDoiDuLieu;
        txtSoChungChi.TextChanged += Control_ThayDoiDuLieu;
        txtTenChungChi.TextChanged += Control_ThayDoiDuLieu;
        txtNoiCap.TextChanged += Control_ThayDoiDuLieu;
        dpNgayCap.SelectedDateChanged += Control_ThayDoiDuLieu;
        dpHanChungChi.SelectedDateChanged += Control_ThayDoiDuLieu;
    }

    /// <summary>
    /// Đánh dấu trường nhập không hợp lệ và lưu thông tin cần dùng khi gỡ cảnh báo.
    /// </summary>
    /// <param name="control">Giá trị điều khiển dùng khi thực hiện thao tác; kiểu Control.</param>
    private void DatBaoDo(Control control)
    {
        control.BorderBrush = Brushes.Red;
        control.BorderThickness = new Thickness(1.5);
    }

    /// <summary>
    /// Gỡ đánh dấu lỗi của trường đã được chỉnh sửa hoặc kiểm tra lại.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>control.ClearValue</c>.</remarks>
    /// <param name="control">Giá trị điều khiển dùng khi thực hiện thao tác; kiểu Control.</param>
    private void XoaBaoDo(Control control)
    {
        control.ClearValue(BorderBrushProperty);
        control.ClearValue(BorderThicknessProperty);
    }

    /// <summary>
    /// Xóa các dấu lỗi còn trên biểu mẫu trước khi kiểm tra lại hoặc đặt lại dữ liệu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XoaBaoDo</c>.</remarks>
    private void XoaToanBoBaoDo()
    {
        XoaBaoDo(cboNhanVien);
        XoaBaoDo(txtSoChungChi);
        XoaBaoDo(txtTenChungChi);
        XoaBaoDo(txtNoiCap);
        XoaBaoDo(dpNgayCap);
        XoaBaoDo(dpHanChungChi);
    }

    /// <summary>
    /// Kiểm tra các trường bắt buộc và định dạng; đánh dấu vị trí lỗi để người dùng sửa trước khi lưu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XoaToanBoBaoDo</c>, <c>DatBaoDo</c>.</remarks>
    /// <param name="thongBaoLoi">Giá trị thông báo lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
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

    /// <summary>
    /// Xử lý thao tác nút lưu khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>KiemTraDuLieuDauVao</c>, <c>MessageBox.Show</c>, <c>.ToString</c>, <c>txtSoChungChi.Text.Trim</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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

    /// <summary>
    /// Xử lý thao tác nút dòng khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Close</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnDong_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
