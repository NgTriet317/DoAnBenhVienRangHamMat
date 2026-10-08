using BUS;
using ET;
using System.Windows.Input;

namespace RangHamMat.Views
{
    public partial class NhanSu
    {
        public NhanSu()
        {
            InitializeComponent();

            dgvNhanVien.ItemsSource = new BUS_NHANVIEN().layToanBoNhanVien();
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

    }
}
