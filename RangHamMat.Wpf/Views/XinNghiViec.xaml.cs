using System;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;

namespace RangHamMat.Views;

public partial class XinNghiViec : UserControl
{
    private bool daGuiPhep;
    private bool daGuiViec;

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

    private static string HienThi(string noiDung)
    {
        if (string.IsNullOrWhiteSpace(noiDung))
            return "(Chưa nhập)";
        return noiDung.Trim();
    }

    private static string HienThiNgay(DatePicker chonNgay)
    {
        if (!chonNgay.SelectedDate.HasValue)
            return "(Chưa chọn)";
        return chonNgay.SelectedDate.Value.ToString("dd/MM/yyyy");
    }

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

    private void Phep_TextChanged(object sender, TextChangedEventArgs e)
    {
        CapNhatXemTruocNghiPhep();
    }

    private void Phep_DateChanged(object sender, SelectionChangedEventArgs e)
    {
        CapNhatGioiHanNgay();
        CapNhatXemTruocNghiPhep();
    }

    private void Phep_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CapNhatXemTruocNghiPhep();
    }

    private void Viec_TextChanged(object sender, TextChangedEventArgs e)
    {
        CapNhatXemTruocNghiViec();
    }

    private void Viec_DateChanged(object sender, SelectionChangedEventArgs e)
    {
        CapNhatXemTruocNghiViec();
    }

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

    private void KhoaFormNghiPhep(bool daGui)
    {
        cboLoaiNghi.IsEnabled = !daGui;
        dpTuNgay.IsEnabled = !daGui;
        dpDenNgay.IsEnabled = !daGui;
        txtLyDoNghiPhep.IsReadOnly = daGui;
        txtBanGiaoNghiPhep.IsReadOnly = daGui;
        btnGuiNghiPhep.IsEnabled = !daGui;
    }

    private void KhoaFormNghiViec(bool daGui)
    {
        dpNgayNghiViec.IsEnabled = !daGui;
        txtLyDoNghiViec.IsReadOnly = daGui;
        txtBanGiaoNghiViec.IsReadOnly = daGui;
        btnGuiNghiViec.IsEnabled = !daGui;
    }

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
