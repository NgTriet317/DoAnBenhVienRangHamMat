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
    public partial class NhanSu : UserControl
    {
        private BUS_NHANVIEN busNhanVien = new();
        private BUS_CHUCVU busChucVu = new();
        private BUS_CHUYENKHOA busChuyenKhoa = new();

        private List<ET_NHANVIEN> danhSachNhanVien = new();

        public NhanSu()
        {
            InitializeComponent();

            LoadDuLieuNhanVien();
            LoadDuLieuChucVu();
            LoadDuLieuChuyenKhoa();
            DangKySuKienXoaBaoDo();
        }

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

        private void LoadDuLieuChucVu()
        {
            var chucVuList = busChucVu.layToanBoChucVu().ToList();
            dgvChucVu.ItemsSource = chucVuList;
            cboChucVu.ItemsSource = chucVuList;
        }

        private void LoadDuLieuChuyenKhoa()
        {
            cboChuyenKhoa.ItemsSource = busChuyenKhoa.layToanBoChuyenKhoa();
        }

        private void XoaThongBaoLoi(object? sender, EventArgs e)
        {
            if (sender is Control control)
            {
                XoaBaoDo(control);
            }
        }

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

        private void XoaToanBoBaoDoNhanVien()
        {
            XoaBaoDo(txtHoTen);
            XoaBaoDo(txtSDT);
            XoaBaoDo(dpNgaySinh);
            XoaBaoDo(dpNgayVaoLam);
            XoaBaoDo(cboChucVu);
        }

        private void XoaToanBoBaoDoChucVu()
        {
            XoaBaoDo(txtMaChucVu);
            XoaBaoDo(txtTenChucVu);
        }

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

        private void txtTimNhanVien_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApDungBoLocNhanVien();
        }

        private void cboLocTrangThaiNhanVien_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApDungBoLocNhanVien();
        }

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

        public void clearTextBoxesChucVu()
        {
            txtMaChucVu.Text = string.Empty;
            txtTenChucVu.Text = string.Empty;
            txtMoTaChucVu.Text = string.Empty;
            XoaToanBoBaoDoChucVu();
        }

        private void btnLamMoiChucVu_Click(object sender, RoutedEventArgs e)
        {
            clearTextBoxesChucVu();
            LoadDuLieuChucVu();
        }

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
