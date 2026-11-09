using BUS;
using ET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RangHamMat.Views
{
    /// <summary>
    /// Phần xử lý nhân sự của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public partial class NhanSu : UserControl
    {
        /// <summary>
        /// Đối tượng nghiệp vụ nhân viên được màn hình dùng để đọc/ghi dữ liệu.
        /// </summary>
        private BUS_NHANVIEN busNhanVien = new();
        /// <summary>
        /// Đối tượng nghiệp vụ chức vụ được màn hình dùng để đọc/ghi dữ liệu.
        /// </summary>
        private BUS_CHUCVU busChucVu = new();
        /// <summary>
        /// Đối tượng nghiệp vụ chuyên khoa được màn hình dùng để đọc/ghi dữ liệu.
        /// </summary>
        private BUS_CHUYENKHOA busChuyenKhoa = new();

        /// <summary>
        /// Danh sách nhân viên dùng trong nhân sự được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private List<ET_NHANVIEN> danhSachNhanVien = new();

        /// <summary>
        /// Khởi tạo nhân sự. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
        /// </summary>
        public NhanSu()
        {
            InitializeComponent();

            LoadDuLieuNhanVien();
            LoadDuLieuChucVu();
            LoadDuLieuChuyenKhoa();
            DangKySuKienXoaBaoDo();
            IsVisibleChanged += NhanSu_IsVisibleChanged;
        }

        // Tải lại để phản ánh ngày nghỉ có hiệu lực khi quay về màn hình Nhân sự.
        private void NhanSu_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is true) LoadDuLieuNhanVien();
        }

        /// <summary>
        /// Đọc lại nhân viên và áp dụng bộ lọc để phản ánh dữ liệu nhân sự hiện tại.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>busNhanVien.layToanBoNhanVien</c>, <c>duLieu.ToList</c>, <c>ApDungBoLocNhanVien</c>, <c>MessageBox.Show</c>.</remarks>
        private void LoadDuLieuNhanVien()
        {
            try
            {
                IEnumerable<ET_NHANVIEN> duLieu = busNhanVien.layToanBoNhanVien();
                if (duLieu == null)
                    danhSachNhanVien = new List<ET_NHANVIEN>();
                else
                    danhSachNhanVien = duLieu.ToList();

                ApDungBoLocNhanVien();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải danh sách nhân viên: " + ex.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Nạp danh mục chức vụ cho các trường chọn và danh sách chức vụ.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>busChucVu.layToanBoChucVu</c>.</remarks>
        private void LoadDuLieuChucVu()
        {
            var chucVuList = busChucVu.layToanBoChucVu().ToList();
            dgvChucVu.ItemsSource = chucVuList;
            cboChucVu.ItemsSource = chucVuList;
        }

        /// <summary>
        /// Nạp danh mục chuyên khoa để chọn và hiển thị đúng tên trên hồ sơ nhân viên.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>busChuyenKhoa.layToanBoChuyenKhoa</c>.</remarks>
        private void LoadDuLieuChuyenKhoa()
        {
            cboChuyenKhoa.ItemsSource = busChuyenKhoa.layToanBoChuyenKhoa();
        }

        /// <summary>
        /// Xóa nội dung thông báo lỗi đang hiển thị trên biểu mẫu.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>XoaBaoDo</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void XoaThongBaoLoi(object? sender, EventArgs e)
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
            txtHoTen.TextChanged += XoaThongBaoLoi;
            txtSDT.TextChanged += XoaThongBaoLoi;
            dpNgaySinh.SelectedDateChanged += XoaThongBaoLoi;
            dpNgayVaoLam.SelectedDateChanged += XoaThongBaoLoi;
            cboChucVu.SelectionChanged += XoaThongBaoLoi;

            txtMaChucVu.TextChanged += XoaThongBaoLoi;
            txtTenChucVu.TextChanged += XoaThongBaoLoi;
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
        /// Gỡ các dấu lỗi của biểu mẫu nhân viên trước lần nhập hoặc kiểm tra mới.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>XoaBaoDo</c>.</remarks>
        private void XoaToanBoBaoDoNhanVien()
        {
            XoaBaoDo(txtHoTen);
            XoaBaoDo(txtSDT);
            XoaBaoDo(dpNgaySinh);
            XoaBaoDo(dpNgayVaoLam);
            XoaBaoDo(cboChucVu);
        }

        /// <summary>
        /// Gỡ các dấu lỗi của biểu mẫu chức vụ trước lần nhập hoặc kiểm tra mới.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>XoaBaoDo</c>.</remarks>
        private void XoaToanBoBaoDoChucVu()
        {
            XoaBaoDo(txtMaChucVu);
            XoaBaoDo(txtTenChucVu);
        }

        /// <summary>
        /// Lọc danh sách nhân viên theo tên/mã và trạng thái nhân sự.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>txtTimNhanVien.Text.Trim</c>, <c>mucDaChon.Content.ToString</c>, <c>nhanVien.MaNV.IndexOf</c>, <c>nhanVien.HoTen.IndexOf</c>.</remarks>
        private void ApDungBoLocNhanVien()
        {
            if (dgvNhanVien == null || danhSachNhanVien == null)
                return;

            string tuKhoa = "";
            if (txtTimNhanVien != null)
                tuKhoa = txtTimNhanVien.Text.Trim();

            string trangThaiLoc = "Tất cả";
            if (cboLocTrangThaiNhanVien != null)
            {
                ComboBoxItem mucDaChon = cboLocTrangThaiNhanVien.SelectedItem as ComboBoxItem;
                if (mucDaChon != null && mucDaChon.Content != null)
                    trangThaiLoc = mucDaChon.Content.ToString();
            }

            List<ET_NHANVIEN> ketQua = new List<ET_NHANVIEN>();
            foreach (ET_NHANVIEN nhanVien in danhSachNhanVien)
            {
                bool khopTuKhoa = string.IsNullOrWhiteSpace(tuKhoa);
                if (!khopTuKhoa)
                {
                    if (nhanVien.MaNV != null && nhanVien.MaNV.IndexOf(tuKhoa, StringComparison.OrdinalIgnoreCase) >= 0)
                        khopTuKhoa = true;
                    if (nhanVien.HoTen != null && nhanVien.HoTen.IndexOf(tuKhoa, StringComparison.OrdinalIgnoreCase) >= 0)
                        khopTuKhoa = true;
                    if (nhanVien.SDT != null && nhanVien.SDT.Contains(tuKhoa))
                        khopTuKhoa = true;
                }

                bool khopTrangThai = trangThaiLoc == "Tất cả";
                if (!khopTrangThai && nhanVien.TrangThai != null)
                    khopTrangThai = string.Equals(nhanVien.TrangThai.Trim(), trangThaiLoc, StringComparison.OrdinalIgnoreCase);

                if (khopTuKhoa && khopTrangThai)
                    ketQua.Add(nhanVien);
            }

            dgvNhanVien.ItemsSource = ketQua;
        }

        /// <summary>
        /// Tiếp nhận thay đổi ô nhập tìm nhân viên và cập nhật trạng thái/dữ liệu phụ thuộc.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ApDungBoLocNhanVien</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void txtTimNhanVien_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApDungBoLocNhanVien();
        }

        /// <summary>
        /// Phản ánh lựa chọn hộp chọn lọc trạng thái nhân viên vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ApDungBoLocNhanVien</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void cboLocTrangThaiNhanVien_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApDungBoLocNhanVien();
        }

        /// <summary>
        /// Mở nội dung hoặc thao tác chi tiết của bảng nhân viên khi người dùng bấm đúp.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>frm.ShowDialog</c>, <c>LoadDuLieuNhanVien</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void dgvNhanVien_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvNhanVien.SelectedItem is not ET_NHANVIEN nhanVien)
                return;

            var frm = new ChiTietNhanSu(nhanVien)
            {
                Owner = Application.Current?.MainWindow
            };

            frm.ShowDialog();

            // Nếu người dùng đã sửa thông tin hoặc cho nhân sự nghỉ việc, tải lại danh sách
            if (frm.DaThayDoi)
            {
                LoadDuLieuNhanVien();
            }
        }


        /// <summary>
        /// Đề xuất mã nhập theo quy tắc đang áp dụng của biểu mẫu; mã lưu cuối cùng vẫn do nghiệp vụ xử lý.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>chucVu.Split</c>, <c>catchuoi[0].Substring</c>, <c>catchuoi[1].Substring</c>.</remarks>
        /// <param name="sdt">Giá trị số điện thoại dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="chucVu">Giá trị chức vụ dùng khi thực hiện thao tác; kiểu string.</param>
        /// <returns>Chuỗi kết quả của thao tác tạo mã tự động.</returns>
        public string taoMaTuDong(string sdt, string chucVu)
        {
            string[] catchuoi = chucVu.Split(' ');
            string ma = catchuoi[0].Substring(0, 1).ToUpper() + catchuoi[1].Substring(0, 1).ToUpper() + sdt;
            return ma;
        }

        //kiem tra du lieu nhan vien
        private bool KiemTraDuLieuThemNhanVien(out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            bool hopLe = true;
            XoaToanBoBaoDoNhanVien();

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


            // Ngày sinh
            if (!dpNgaySinh.SelectedDate.HasValue) //nếu ko có chọn ngày
            {
                DatBaoDo(dpNgaySinh);
                if (string.IsNullOrEmpty(thongBaoLoi))
                    thongBaoLoi = "Vui lòng chọn ngày sinh.";
                hopLe = false;
            }
            else //nếu có chọn ngày
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

            // Chức vụ
            if (cboChucVu.SelectedValue == null)
            {
                DatBaoDo(cboChucVu);
                if (string.IsNullOrEmpty(thongBaoLoi))
                    thongBaoLoi = "Vui lòng chọn chức vụ cho nhân viên.";
                hopLe = false;
            }

            // Ngày vào làm
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
        /// Xử lý thao tác nút thêm nhân viên khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>KiemTraDuLieuThemNhanVien</c>, <c>MessageBox.Show</c>, <c>taoMaTuDong</c>, <c>txtSDT.Text.Trim</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnThemNhanVien_Click(object sender, RoutedEventArgs e)
        {
            if (!KiemTraDuLieuThemNhanVien(out string thongBaoLoi))
            {
                MessageBox.Show(thongBaoLoi, "Kiểm tra dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string tenChucVu = "";
                if (cboChucVu.SelectedItem is ET_CHUCVU cv)
                    tenChucVu = cv.TenChucVu;
                else
                    tenChucVu = cboChucVu.Text;

                string maNV = taoMaTuDong(txtSDT.Text.Trim(), tenChucVu);
                string gioiTinh = cboGioiTinh.Text;
                string trangThai = cboTrangThai.Text;
                string maChucVu = cboChucVu.SelectedValue.ToString() ?? "";
                string maCK = cboChuyenKhoa.SelectedValue.ToString() ?? "";

                var nhanVienMoi = new ET_NHANVIEN(
                    maNV,
                    txtHoTen.Text.Trim(),
                    gioiTinh,
                    dpNgaySinh.SelectedDate!.Value,
                    txtSDT.Text.Trim(),
                    txtDiaChi.Text.Trim(),
                    maChucVu,
                    string.IsNullOrEmpty(maCK) ? null! : maCK,
                    txtBangCap.Text.Trim(),
                    dpNgayVaoLam.SelectedDate.Value,
                    trangThai
                );

                if (busNhanVien.themNhanVien(nhanVienMoi))
                {
                    MessageBox.Show("Thêm nhân viên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    btnLamMoiNhanVien_Click(sender, e);
                    LoadDuLieuNhanVien();
                }
                else
                {
                    MessageBox.Show("Thêm nhân viên thất bại. Có thể mã nhân viên đã tồn tại hoặc dữ liệu không hợp lệ!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi thêm nhân viên: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Xử lý thao tác nút làm mới nhân viên khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>XoaToanBoBaoDoNhanVien</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnLamMoiNhanVien_Click(object sender, RoutedEventArgs e)
        {
            txtHoTen.Text = string.Empty;
            txtSDT.Text = string.Empty;
            txtDiaChi.Text = string.Empty;
            txtBangCap.Text = string.Empty;
            cboGioiTinh.SelectedIndex = 0;
            cboTrangThai.SelectedIndex = 0;
            cboChucVu.SelectedIndex = -1;
            cboChuyenKhoa.SelectedIndex = -1;
            dpNgaySinh.SelectedDate = null;
            dpNgayVaoLam.SelectedDate = DateTime.Today;

            XoaToanBoBaoDoNhanVien();
        }

        /// <summary>
        /// Xử lý thao tác nút thêm chức vụ khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>XoaToanBoBaoDoChucVu</c>, <c>DatBaoDo</c>, <c>MessageBox.Show</c>, <c>txtMaChucVu.Text.Trim</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnThemChucVu_Click(object sender, RoutedEventArgs e)
        {
            XoaToanBoBaoDoChucVu();

            if (string.IsNullOrWhiteSpace(txtMaChucVu.Text))
            {
                DatBaoDo(txtMaChucVu);
                MessageBox.Show("Mã chức vụ không được để trống.", "Kiểm tra dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtTenChucVu.Text))
            {
                DatBaoDo(txtTenChucVu);
                MessageBox.Show("Tên chức vụ không được để trống.", "Kiểm tra dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var chucVuMoi = new ET_CHUCVU(txtMaChucVu.Text.Trim(), txtTenChucVu.Text.Trim(), txtMoTaChucVu.Text.Trim());

            if (busChucVu.themChucVu(chucVuMoi))
            {
                MessageBox.Show("Thêm chức vụ thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                clearTextBoxesChucVu();
                LoadDuLieuChucVu();
            }
            else
            {
                MessageBox.Show("Thêm chức vụ thất bại!.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Xóa các ô nhập của biểu mẫu chức vụ để bắt đầu nhập lại.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>XoaToanBoBaoDoChucVu</c>.</remarks>
        public void clearTextBoxesChucVu()
        {
            txtMaChucVu.Text = string.Empty;
            txtTenChucVu.Text = string.Empty;
            txtMoTaChucVu.Text = string.Empty;
            XoaToanBoBaoDoChucVu();
        }

        /// <summary>
        /// Xử lý thao tác nút làm mới chức vụ khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>clearTextBoxesChucVu</c>, <c>LoadDuLieuChucVu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnLamMoiChucVu_Click(object sender, RoutedEventArgs e)
        {
            clearTextBoxesChucVu();
            LoadDuLieuChucVu();
        }

        /// <summary>
        /// Tiếp nhận thay đổi ô nhập họ tên và cập nhật trạng thái/dữ liệu phụ thuộc.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>DatBaoDo</c>, <c>txtHoTen.Text.Trim</c>, <c>char.IsLetter</c>, <c>char.IsWhiteSpace</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void txtHoTen_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Họ tên không đc để trống
            if (string.IsNullOrWhiteSpace(txtHoTen.Text))
            {
                DatBaoDo(txtHoTen);
                return;
            }
            // họ tên không đc chứa số và kí tự đặc biệt
            else if (!txtHoTen.Text.Trim().All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                DatBaoDo(txtHoTen);
                return;
            }
        }
    }
}
