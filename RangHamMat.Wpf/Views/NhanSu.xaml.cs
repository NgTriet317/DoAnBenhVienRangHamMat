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
        public string taoMaTuDong(string sdt, string chucVu)
        {
            string[] catchuoi = chucVu.Split(' ');
            string ma = catchuoi[0].Substring(0, 1).ToUpper() + catchuoi[1].Substring(0, 1).ToUpper() + sdt;
            return ma;
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
            BUS_NHANVIEN busNhanVien = new BUS_NHANVIEN();

            if(busNhanVien.themNhanVien(new ET_NHANVIEN(
                taoMaTuDong(txtSDT.Text, cboChucVu.Text),
                txtHoTen.Text,
                cboGioiTinh.Text,
                dpNgaySinh.SelectedDate.Value,
                txtSDT.Text,
                txtDiaChi.Text,
                cboChucVu.SelectedValue.ToString(),
                cboChuyenKhoa.SelectedValue.ToString(),
                txtBangCap.Text,
                dpNgayVaoLam.SelectedDate.Value,
                cboTrangThai.Text
                )))
            {
                MessageBox.Show("Thêm nhân viên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Thêm nhân viên thất bại!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
