using System;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;

namespace RangHamMat.Views;

/// <summary>
/// Phần xử lý xin nghỉ việc của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class XinNghiViec : UserControl
{
    /// <summary>
    /// Cờ Đã gửi phép; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool daGuiPhep;
    /// <summary>
    /// Cờ Đã gửi việc; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool daGuiViec;

    /// <summary>
    /// Khởi tạo xin nghỉ việc. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    public XinNghiViec()
    {
        InitializeComponent();
        string hoTen = "Chưa có thông tin nhân viên";
        if (!string.IsNullOrWhiteSpace(PhienDangNhap.MaNV))
        {
            try
            {
                ET_NHANVIEN nhanVien = new BUS_NHANVIEN().layNhanVienTheoMa(PhienDangNhap.MaNV);
                if (nhanVien != null && !string.IsNullOrWhiteSpace(nhanVien.HoTen))
                    hoTen = nhanVien.HoTen;
            }
            catch (Exception)
            {
                hoTen = "Không tải được họ tên nhân viên";
            }
        }
        txtHoTenNghiPhep.Text = hoTen;
        txtHoTenNghiViec.Text = hoTen;
        CapNhatGioiHanNgay();

        cboLoaiNghi.SelectionChanged += Phep_SelectionChanged;
        dpTuNgay.SelectedDateChanged += Phep_DateChanged;
        dpDenNgay.SelectedDateChanged += Phep_DateChanged;
        txtLyDoNghiPhep.TextChanged += Phep_TextChanged;
        txtBanGiaoNghiPhep.TextChanged += Phep_TextChanged;

        dpNgayNghiViec.SelectedDateChanged += Viec_DateChanged;
        txtLyDoNghiViec.TextChanged += Viec_TextChanged;
        txtBanGiaoNghiViec.TextChanged += Viec_TextChanged;

        CapNhatXemTruocNghiPhep();
        CapNhatXemTruocNghiViec();
    }

    /// <summary>
    /// Cập nhật giới hạn ngày lựa chọn theo điều kiện áp dụng cho loại đơn nghỉ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DateTime.Today.AddDays</c>.</remarks>
    private void CapNhatGioiHanNgay()
    {
        DateTime ngaySomNhat = DateTime.Today.AddDays(7);
        if (dpTuNgay.SelectedDate.HasValue && dpTuNgay.SelectedDate.Value.Date < ngaySomNhat)
            dpTuNgay.SelectedDate = null;
        if (dpDenNgay.SelectedDate.HasValue && dpDenNgay.SelectedDate.Value.Date < ngaySomNhat)
            dpDenNgay.SelectedDate = null;
        if (dpNgayNghiViec.SelectedDate.HasValue && dpNgayNghiViec.SelectedDate.Value.Date < ngaySomNhat)
            dpNgayNghiViec.SelectedDate = null;
        dpTuNgay.DisplayDateStart = ngaySomNhat;
        DateTime gioiHanNgayKetThuc = ngaySomNhat;
        if (dpTuNgay.SelectedDate.HasValue)
            gioiHanNgayKetThuc = dpTuNgay.SelectedDate.Value.Date;
        if (dpDenNgay.SelectedDate.HasValue && dpDenNgay.SelectedDate.Value.Date < gioiHanNgayKetThuc)
            dpDenNgay.SelectedDate = null;
        dpDenNgay.DisplayDateStart = gioiHanNgayKetThuc;
        dpNgayNghiViec.DisplayDateStart = ngaySomNhat;
    }

    /// <summary>
    /// Điền thông tin nhân viên và trạng thái biểu mẫu xin nghỉ lên giao diện.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>noiDung.Trim</c>.</remarks>
    /// <param name="noiDung">Giá trị nội dung dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Chuỗi kết quả của thao tác hiển thị.</returns>
    private static string HienThi(string noiDung)
    {
        if (string.IsNullOrWhiteSpace(noiDung))
            return "(Chưa nhập)";
        return noiDung.Trim();
    }

    /// <summary>
    /// Định dạng ngày dùng cho phần mô tả hoặc xem trước đơn nghỉ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>chonNgay.SelectedDate.Value.ToString</c>.</remarks>
    /// <param name="chonNgay">Giá trị chọn ngày dùng khi thực hiện thao tác; kiểu DatePicker.</param>
    /// <returns>Chuỗi kết quả của thao tác hiển thị ngày.</returns>
    private static string HienThiNgay(DatePicker chonNgay)
    {
        if (!chonNgay.SelectedDate.HasValue)
            return "(Chưa chọn)";
        return chonNgay.SelectedDate.Value.ToString("dd/MM/yyyy");
    }

    /// <summary>
    /// Tổng hợp thông tin nghỉ phép đang nhập thành phần xem trước trước khi gửi đơn.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>item.Content.ToString</c>, <c>HienThi</c>, <c>HienThiNgay</c>.</remarks>
    private void CapNhatXemTruocNghiPhep()
    {
        string loaiNghi = "(Chưa chọn)";
        if (cboLoaiNghi.SelectedIndex > 0)
        {
            ComboBoxItem item = (ComboBoxItem)cboLoaiNghi.SelectedItem;
            loaiNghi = item.Content.ToString();
        }

        txtXemTruocNghiPhep.Text =
            "Kính gửi: Bộ phận nhân sự / Ban quản lý\n\n" +
            "Tôi là nhân viên: " + HienThi(txtHoTenNghiPhep.Text) + "\n\n" +
            "Loại nghỉ: " + loaiNghi + "\n" +
            "Từ ngày: " + HienThiNgay(dpTuNgay) + "\n" +
            "Đến ngày: " + HienThiNgay(dpDenNgay) + "\n\n" +
            "Lý do:\n" + HienThi(txtLyDoNghiPhep.Text) + "\n\n" +
            "Bàn giao công việc:\n" + HienThi(txtBanGiaoNghiPhep.Text) + "\n\n" +
            "Kính mong xem xét và phê duyệt.\n\n" +
            "Người làm đơn: " + HienThi(txtHoTenNghiPhep.Text);
    }

    /// <summary>
    /// Tổng hợp ngày hiệu lực và nội dung nghỉ việc đang nhập để người dùng đối chiếu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>HienThi</c>, <c>HienThiNgay</c>.</remarks>
    private void CapNhatXemTruocNghiViec()
    {
        txtXemTruocNghiViec.Text =
            "Kính gửi: Bộ phận nhân sự / Ban quản lý\n\n" +
            "Tôi là nhân viên: " + HienThi(txtHoTenNghiViec.Text) + "\n\n" +
            "Ngày dự kiến nghỉ việc: " + HienThiNgay(dpNgayNghiViec) + "\n\n" +
            "Lý do:\n" + HienThi(txtLyDoNghiViec.Text) + "\n\n" +
            "Kế hoạch bàn giao:\n" + HienThi(txtBanGiaoNghiViec.Text) + "\n\n" +
            "Kính mong xem xét và phản hồi.\n\n" +
            "Người làm đơn: " + HienThi(txtHoTenNghiViec.Text);
    }

    /// <summary>
    /// Tiếp nhận thay đổi phép và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CapNhatXemTruocNghiPhep</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Phep_TextChanged(object sender, TextChangedEventArgs e)
    {
        CapNhatXemTruocNghiPhep();
    }

    /// <summary>
    /// Tiếp nhận thay đổi phép và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CapNhatGioiHanNgay</c>, <c>CapNhatXemTruocNghiPhep</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Phep_DateChanged(object sender, SelectionChangedEventArgs e)
    {
        CapNhatGioiHanNgay();
        CapNhatXemTruocNghiPhep();
    }

    /// <summary>
    /// Phản ánh lựa chọn phép vào dữ liệu và phần hiển thị liên quan.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CapNhatXemTruocNghiPhep</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Phep_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CapNhatXemTruocNghiPhep();
    }

    /// <summary>
    /// Tiếp nhận thay đổi việc và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CapNhatXemTruocNghiViec</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Viec_TextChanged(object sender, TextChangedEventArgs e)
    {
        CapNhatXemTruocNghiViec();
    }

    /// <summary>
    /// Tiếp nhận thay đổi việc và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CapNhatXemTruocNghiViec</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Viec_DateChanged(object sender, SelectionChangedEventArgs e)
    {
        CapNhatXemTruocNghiViec();
    }

    /// <summary>
    /// Xử lý thao tác gửi nghỉ phép khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CapNhatGioiHanNgay</c>, <c>MessageBox.Show</c>, <c>DateTime.Today.AddDays</c>, <c>txtLyDoNghiPhep.Text.Trim</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void GuiNghiPhep_Click(object sender, RoutedEventArgs e)
    {
        if (daGuiPhep)
            return;
        CapNhatGioiHanNgay();

        if (string.IsNullOrWhiteSpace(PhienDangNhap.MaNV))
        {
            MessageBox.Show("Tài khoản chưa liên kết với nhân viên nên không thể gửi đơn.", "Thiếu thông tin nhân viên");
            return;
        }

        if (cboLoaiNghi.SelectedIndex <= 0 || !dpTuNgay.SelectedDate.HasValue ||
            !dpDenNgay.SelectedDate.HasValue || string.IsNullOrWhiteSpace(txtLyDoNghiPhep.Text))
        {
            MessageBox.Show("Vui lòng nhập đầy đủ các trường có dấu *.", "Thiếu thông tin");
            return;
        }

        DateTime tuNgay = dpTuNgay.SelectedDate.Value.Date;
        DateTime denNgay = dpDenNgay.SelectedDate.Value.Date;
        if (tuNgay < DateTime.Today.AddDays(7))
        {
            MessageBox.Show("Phải xin nghỉ trước ít nhất 7 ngày. Ngày sớm nhất: " +
                DateTime.Today.AddDays(7).ToString("dd/MM/yyyy"), "Ngày không hợp lệ");
            return;
        }
        if (denNgay < tuNgay)
        {
            MessageBox.Show("Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.", "Ngày không hợp lệ");
            return;
        }

        MessageBoxResult dongY = MessageBox.Show("Gửi đơn xin nghỉ phép để chờ quản lý xét duyệt?",
            "Xác nhận gửi đơn", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (dongY != MessageBoxResult.Yes)
            return;

        try
        {
            ComboBoxItem loaiNghi = (ComboBoxItem)cboLoaiNghi.SelectedItem;
            ET_DON_XIN_NGHI don = new ET_DON_XIN_NGHI();
            don.MaNV = PhienDangNhap.MaNV;
            don.TuNgay = tuNgay;
            don.DenNgay = denNgay;
            don.LyDo = txtLyDoNghiPhep.Text.Trim();
            don.GhiChu = "Loại đơn: Nghỉ phép\nLoại nghỉ: " + loaiNghi.Content +
                         "\nNgười tiếp nhận / bàn giao: " + txtBanGiaoNghiPhep.Text.Trim();

            BUS_DON_XIN_NGHI bus = new BUS_DON_XIN_NGHI();
            string maDon = bus.guiDon(don);
            daGuiPhep = true;
            KhoaFormNghiPhep(true);
            txtTrangThaiNghiPhep.Text = "Đã gửi · " + maDon + " · Chờ duyệt";
            MessageBox.Show("Đã gửi đơn xin nghỉ phép. Mã đơn: " + maDon,
                "Gửi đơn thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể gửi đơn: " + ex.Message,
                "Lỗi gửi đơn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Xử lý thao tác gửi nghỉ việc khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CapNhatGioiHanNgay</c>, <c>MessageBox.Show</c>, <c>DateTime.Today.AddDays</c>, <c>txtLyDoNghiViec.Text.Trim</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void GuiNghiViec_Click(object sender, RoutedEventArgs e)
    {
        if (daGuiViec)
            return;
        CapNhatGioiHanNgay();

        if (string.IsNullOrWhiteSpace(PhienDangNhap.MaNV))
        {
            MessageBox.Show("Tài khoản chưa liên kết với nhân viên nên không thể gửi đơn.", "Thiếu thông tin nhân viên");
            return;
        }

        if (!dpNgayNghiViec.SelectedDate.HasValue || string.IsNullOrWhiteSpace(txtLyDoNghiViec.Text))
        {
            MessageBox.Show("Vui lòng nhập đầy đủ các trường có dấu *.", "Thiếu thông tin");
            return;
        }

        DateTime ngayNghi = dpNgayNghiViec.SelectedDate.Value.Date;
        if (ngayNghi < DateTime.Today.AddDays(7))
        {
            MessageBox.Show("Phải xin nghỉ trước ít nhất 7 ngày. Ngày sớm nhất: " +
                DateTime.Today.AddDays(7).ToString("dd/MM/yyyy"), "Ngày không hợp lệ");
            return;
        }

        MessageBoxResult dongY = MessageBox.Show("Gửi đơn xin nghỉ việc để chờ quản lý xét duyệt?",
            "Xác nhận gửi đơn", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (dongY != MessageBoxResult.Yes)
            return;

        try
        {
            ET_DON_XIN_NGHI don = new ET_DON_XIN_NGHI();
            don.MaNV = PhienDangNhap.MaNV;
            don.TuNgay = ngayNghi;
            don.DenNgay = ngayNghi;
            don.LyDo = txtLyDoNghiViec.Text.Trim();
            don.GhiChu = "Loại đơn: Nghỉ việc\nKế hoạch bàn giao: " + txtBanGiaoNghiViec.Text.Trim();

            BUS_DON_XIN_NGHI bus = new BUS_DON_XIN_NGHI();
            string maDon = bus.guiDon(don);
            daGuiViec = true;
            KhoaFormNghiViec(true);
            txtTrangThaiNghiViec.Text = "Đã gửi · " + maDon + " · Chờ duyệt";
            MessageBox.Show("Đã gửi đơn xin nghỉ việc. Mã đơn: " + maDon +
                ". Nhân viên vẫn giữ nguyên trạng thái cho đến khi được xử lý.",
                "Gửi đơn thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể gửi đơn: " + ex.Message,
                "Lỗi gửi đơn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Khóa hoặc mở các trường của biểu mẫu nghỉ phép theo trạng thái gửi đơn.
    /// </summary>
    /// <param name="daGui">Giá trị đã gửi dùng khi thực hiện thao tác; kiểu bool.</param>
    private void KhoaFormNghiPhep(bool daGui)
    {
        cboLoaiNghi.IsEnabled = !daGui;
        dpTuNgay.IsEnabled = !daGui;
        dpDenNgay.IsEnabled = !daGui;
        txtLyDoNghiPhep.IsReadOnly = daGui;
        txtBanGiaoNghiPhep.IsReadOnly = daGui;
        btnGuiNghiPhep.IsEnabled = !daGui;
    }

    /// <summary>
    /// Khóa hoặc mở các trường của biểu mẫu nghỉ việc theo trạng thái gửi đơn.
    /// </summary>
    /// <param name="daGui">Giá trị đã gửi dùng khi thực hiện thao tác; kiểu bool.</param>
    private void KhoaFormNghiViec(bool daGui)
    {
        dpNgayNghiViec.IsEnabled = !daGui;
        txtLyDoNghiViec.IsReadOnly = daGui;
        txtBanGiaoNghiViec.IsReadOnly = daGui;
        btnGuiNghiViec.IsEnabled = !daGui;
    }

    /// <summary>
    /// Xử lý thao tác làm mới nghỉ phép khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>KhoaFormNghiPhep</c>, <c>CapNhatGioiHanNgay</c>, <c>txtLyDoNghiPhep.Clear</c>, <c>txtBanGiaoNghiPhep.Clear</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void LamMoiNghiPhep_Click(object sender, RoutedEventArgs e)
    {
        daGuiPhep = false;
        KhoaFormNghiPhep(false);
        cboLoaiNghi.SelectedIndex = 0;
        dpTuNgay.SelectedDate = null;
        dpDenNgay.SelectedDate = null;
        CapNhatGioiHanNgay();
        txtLyDoNghiPhep.Clear();
        txtBanGiaoNghiPhep.Clear();
        txtTrangThaiNghiPhep.Text = "Bản nháp · Chưa gửi";
        CapNhatXemTruocNghiPhep();
    }

    /// <summary>
    /// Xử lý thao tác làm mới nghỉ việc khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>KhoaFormNghiViec</c>, <c>CapNhatGioiHanNgay</c>, <c>txtLyDoNghiViec.Clear</c>, <c>txtBanGiaoNghiViec.Clear</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void LamMoiNghiViec_Click(object sender, RoutedEventArgs e)
    {
        daGuiViec = false;
        KhoaFormNghiViec(false);
        dpNgayNghiViec.SelectedDate = null;
        CapNhatGioiHanNgay();
        txtLyDoNghiViec.Clear();
        txtBanGiaoNghiViec.Clear();
        txtTrangThaiNghiViec.Text = "Bản nháp · Chưa gửi";
        CapNhatXemTruocNghiViec();
    }
}
