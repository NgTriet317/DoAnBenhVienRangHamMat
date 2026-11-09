using BUS;
using ET;
using RangHamMat.Controls.ClinicalWorkflow;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RangHamMat.Views;

/// <summary>
/// Phần xử lý chi tiết nhân sự của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class ChiTietNhanSu : Window
{
    /// <summary>
    /// Nhân viên được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private ET_NHANVIEN nhanVien;
    /// <summary>
    /// Đối tượng nghiệp vụ nhân viên được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private BUS_NHANVIEN busNhanVien = new();
    /// <summary>
    /// Đã thay đổi.
    /// </summary>
    public bool DaThayDoi { get; private set; }

    /// <summary>
    /// Khởi tạo chi tiết nhân sự. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    /// <param name="ttNhanVien">Giá trị thanh toán nhân viên dùng khi thực hiện thao tác; kiểu ET_NHANVIEN.</param>
    public ChiTietNhanSu(ET_NHANVIEN ttNhanVien)
    {
        InitializeComponent();
        nhanVien = ttNhanVien;
        DataContext = nhanVien;

        LoadDanhSachChucVuVaChuyenKhoa();
        GanDuLieuBanDau();
        DangKySuKienXoaBaoDo();
    }

    /// <summary>
    /// Nạp hai danh mục chức vụ/chuyên khoa cho biểu mẫu chi tiết nhân sự.
    /// </summary>
    private void LoadDanhSachChucVuVaChuyenKhoa()
    {
        cboChucVu.ItemsSource = new BUS_CHUCVU().layToanBoChucVu();
        cboChuyenKhoa.ItemsSource = new BUS_CHUYENKHOA().layToanBoChuyenKhoa();
    }

    /// <summary>
    /// Điền bản dữ liệu gốc vào các trường nhập trước khi cho phép chỉnh sửa.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DateTime.Today.AddYears</c>, <c>.ToString</c>, <c>CapNhatHeader</c>.</remarks>
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
        txtHoTen.TextChanged += Control_ThayDoiDuLieu;
        txtSDT.TextChanged += Control_ThayDoiDuLieu;
        dpNgaySinh.SelectedDateChanged += Control_ThayDoiDuLieu;
        dpNgayVaoLam.SelectedDateChanged += Control_ThayDoiDuLieu;
        cboChucVu.SelectionChanged += Control_ThayDoiDuLieu;
    }

    /// <summary>
    /// Cập nhật tiêu đề và các thông tin nhận diện theo nội dung hồ sơ đang hiển thị.
    /// </summary>
    private void CapNhatHeader()
    {
        lblHeaderHoTen.Text = nhanVien.HoTen;
        lblHeaderTrangThai.Text = nhanVien.TrangThai;
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
        XoaBaoDo(txtHoTen);
        XoaBaoDo(txtSDT);
        XoaBaoDo(dpNgaySinh);
        XoaBaoDo(dpNgayVaoLam);
        XoaBaoDo(cboChucVu);
    }

    /// <summary>
    /// Kiểm tra các trường bắt buộc và định dạng; đánh dấu vị trí lỗi để người dùng sửa trước khi lưu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XoaToanBoBaoDo</c>, <c>DatBaoDo</c>, <c>txtHoTen.Text.Trim</c>, <c>char.IsLetter</c>.</remarks>
    /// <param name="thongBaoLoi">Giá trị thông báo lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
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

    /// <summary>
    /// Xử lý thao tác nút chính sửa khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHienThaoTac</c>, <c>KiemTraDuLieuDauVao</c>, <c>MessageBox.Show</c>, <c>txtHoTen.Text.Trim</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnChinhSua_Click(object sender, RoutedEventArgs e)
    {
        ThucHienThaoTac(() =>
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

        });
    }

    /// <summary>
    /// Xử lý thao tác nút khôi phục khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHienThaoTac</c>, <c>MessageBox.Show</c>, <c>busNhanVien.khoiPhucNhanVien</c>, <c>CapNhatHeader</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnKhoiPhuc_Click(object sender, RoutedEventArgs e)
    {
        ThucHienThaoTac(() =>
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

        });
    }

    /// <summary>
    /// Xử lý thao tác nút nghỉ việc khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHienThaoTac</c>, <c>MessageBox.Show</c>, <c>busNhanVien.nghiViecNhanVien</c>, <c>CapNhatHeader</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnNghiViec_Click(object sender, RoutedEventArgs e)
    {
        ThucHienThaoTac(() =>
        {
            var confirm=MessageBox.Show($"Chuyển nhân sự '{nhanVien.HoTen}' sang 'Đã nghỉ'?","Xác nhận nghỉ việc",MessageBoxButton.YesNo,MessageBoxImage.Question);
            if(confirm!=MessageBoxResult.Yes)return;
            if(!busNhanVien.nghiViecNhanVien(nhanVien.MaNV))
            {
                MessageBox.Show("Chưa cập nhật được trạng thái. Hãy tải lại để đối chiếu.","Thông báo",MessageBoxButton.OK,MessageBoxImage.Warning);
                return;
            }
            nhanVien.TrangThai="Đã nghỉ";
            DaThayDoi=true;CapNhatHeader();
            MessageBox.Show("Đã chuyển trạng thái nghỉ việc.","Thông báo",MessageBoxButton.OK,MessageBoxImage.Information);
        });
    }

    /// <summary>
    /// Thực thi thao tác nhân sự được truyền vào và cập nhật thông báo/dữ liệu theo kết quả.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>action</c>, <c>MessageBox.Show</c>, <c>ClinicalFormInput.Message</c>.</remarks>
    /// <param name="action">Giá trị action dùng khi thực hiện thao tác; kiểu Action.</param>
    private void ThucHienThaoTac(Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is InvalidOperationException or System.Data.Common.DbException)
        {
            MessageBox.Show(ClinicalFormInput.Message(ex), "Chưa lưu thay đổi", MessageBoxButton.OK, MessageBoxImage.Warning);
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
