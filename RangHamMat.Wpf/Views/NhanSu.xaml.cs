using BUS;
using ET;
using System.Windows;
using System.Windows.Input;

namespace RangHamMat.Views
{
    public partial class NhanSu
    {        
        public NhanSu()
        {
            InitializeComponent();

            dgvNhanVien.ItemsSource = new BUS_NHANVIEN().layToanBoNhanVien();
            dgvChucVu.ItemsSource = new BUS_CHUCVU().layToanBoChucVu();
            cboChuyenKhoa.ItemsSource = new BUS_CHUYENKHOA().layToanBoChuyenKhoa();
            cboChucVu.ItemsSource = new BUS_CHUCVU().layToanBoChucVu();
        }

        private void dgvNhanVien_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvNhanVien.SelectedItem is not ET_NHANVIEN nhanVien)
                return;

            var frm = new ChiTietNhanSu(nhanVien)
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };
            frm.ShowDialog();
        }

        private void btnThemChucVu_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BUS_CHUCVU busChucVu = new BUS_CHUCVU();

            if (busChucVu.themChucVu(new ET_CHUCVU(txtMaChucVu.Text, txtTenChucVu.Text, txtMoTaChucVu.Text)))
            {
                MessageBox.Show("Thêm chức vụ thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Thêm chức vụ thất bại!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            dgvChucVu.ItemsSource = new BUS_CHUCVU().layToanBoChucVu();
        }
        public void clearTextBoxes()
        {
            txtMaChucVu.Text = string.Empty;
            txtTenChucVu.Text = string.Empty;
            txtMoTaChucVu.Text = string.Empty;
        }

        private void btnLamMoiChucVu_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            clearTextBoxes();
            dgvChucVu.ItemsSource = new BUS_CHUCVU().layToanBoChucVu();
        }

        private void btnThemNhanVien_Click(object sender, RoutedEventArgs e)
        {
            if (cboChucVu.SelectedValue is not string maChucVu || dpNgayVaoLam.SelectedDate is not DateTime ngayVaoLam)
            {
                MessageBox.Show("Chọn chức vụ và ngày vào làm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                BUS_NHANVIEN busNhanVien = new BUS_NHANVIEN();
                if (busNhanVien.themNhanVien(new ET_NHANVIEN(
                    string.Empty,
                    txtHoTen.Text,
                    cboGioiTinh.Text,
                    dpNgaySinh.SelectedDate,
                    txtSDT.Text,
                    txtDiaChi.Text,
                    maChucVu,
                    cboChuyenKhoa.SelectedValue?.ToString(),
                    txtBangCap.Text,
                    ngayVaoLam,
                    cboTrangThai.Text)))
                {
                    MessageBox.Show("Thêm nhân viên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    dgvNhanVien.ItemsSource = busNhanVien.layToanBoNhanVien();
                }
                else
                {
                    MessageBox.Show("Thêm nhân viên thất bại!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể thêm nhân viên: " + ex.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
