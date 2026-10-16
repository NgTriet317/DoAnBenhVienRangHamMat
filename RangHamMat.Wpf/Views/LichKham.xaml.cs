using BUS;
using ET;
using System;
using System.Collections.Generic;
using System.Linq;
using RangHamMat.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RangHamMat.Views
{
    public partial class LichKham : UserControl, IPatientScoped
    {
        BUS_LICHKHAM lichkham = new BUS_LICHKHAM();
        List<ET_LICHKHAM> danhSach = new List<ET_LICHKHAM>();
        public string? PatientId { get; private set; }

        public void SetPatient(string? maBenhNhan)
        {
            PatientId = maBenhNhan;
            ApDungBoLoc();
        }

        // Chỉ lấy nhân viên có chức vụ bác sĩ.
        internal static List<ET_NHANVIEN> LayBacSi()
        {
            var chucVuBacSi = new BUS_CHUCVU().layToanBoChucVu()
                .Where(cv => cv.TenChucVu.Contains("Bác sĩ", StringComparison.OrdinalIgnoreCase))
                .Select(cv => cv.MaChucVu).ToList();
            return new BUS_NHANVIEN().layToanBoNhanVien()
                .Where(nv => chucVuBacSi.Contains(nv.MaChucVu)).ToList();
        }

        public LichKham()
        {
            InitializeComponent();
            TaiDuLieu();
            NapBoLoc();
            HoTroGiaoDien.Observe(this, TaiDuLieu);
        }

        public void TaiDuLieu()
        {
            try
            {
                danhSach = lichkham.layToanBoLichKham().ToList();
                ApDungBoLoc();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được lịch khám: " + ex.Message);
            }
        }

        private void NapBoLoc()
        {
            cboBacSi.ItemsSource = LayBacSi();
            cboPhong.ItemsSource = new BUS_PHONG().layToanBoPhong().ToList();
            cboBacSi.SelectedIndex = -1;
            cboPhong.SelectedIndex = -1;
        }

        private void ApDungBoLoc()
        {
            if (lstLichKham == null || cboTrangThai == null || txtTimKiem == null) return;

            string timKiem = txtTimKiem.Text.Trim();
            string trangThai = "Tất cả";

            //lọc theo trạng thái
            ComboBoxItem trangThaiDangChon = cboTrangThai.SelectedItem as ComboBoxItem;
            if (trangThaiDangChon != null) trangThai = Convert.ToString(trangThaiDangChon.Content);

            //lọc theo bác sĩ
            string maBacSi = "";
            if (cboBacSi != null && cboBacSi.SelectedValue != null)
                maBacSi = cboBacSi.SelectedValue.ToString();

            //lọc theo phòng khám
            string maPhong = "";
            if (cboPhong != null && cboPhong.SelectedValue != null)
                maPhong = cboPhong.SelectedValue.ToString();

            //thực thi lọc ds theo những lựa chọn trên
            List<ET_LICHKHAM> ketQua = new List<ET_LICHKHAM>();
            foreach (ET_LICHKHAM lich in danhSach)
            {
                if (!string.IsNullOrEmpty(PatientId) && lich.MaBN != PatientId) continue;

                //tìm theo mã lịch (keyword)
                bool dungTuKhoa = true;
                if (!string.IsNullOrWhiteSpace(timKiem))
                {
                    dungTuKhoa = false;
                    if (lich.MaLich != null && lich.MaLich.Contains(timKiem, StringComparison.OrdinalIgnoreCase))
                        dungTuKhoa = true;
                }
                if (!dungTuKhoa) continue;

                //lọc theo cbo
                if (trangThai != "Tất cả" && lich.TrangThai != trangThai) continue;
                if (maBacSi.Length > 0 && lich.MaBS != maBacSi) continue;
                if (maPhong.Length > 0 && lich.MaPhong != maPhong) continue;

                //lọc theo ngày
                if (dpNgayKham != null && dpNgayKham.SelectedDate.HasValue)
                {
                    if (lich.NgayGioKham.Date != dpNgayKham.SelectedDate.Value.Date) continue;
                }
                ketQua.Add(lich);
            }
            lstLichKham.ItemsSource = ketQua;            

            //lọc theo tình trạng
            if (txtDaXacNhan != null && txtChoXuLy != null && txtDaHuy != null)
            {
                int daXacNhan = 0;
                int choXuLy = 0;
                int daHuy = 0;
                foreach (ET_LICHKHAM lich in ketQua)
                {
                    if (lich.TrangThai == "Đã xác nhận") daXacNhan++;
                    if (lich.TrangThai == "Nháp" || lich.TrangThai == "Chờ xác nhận") choXuLy++;
                    if (lich.TrangThai == "Đã hủy" || lich.TrangThai == "Hủy") daHuy++;
                }
                txtDaXacNhan.Text = daXacNhan.ToString();
                txtChoXuLy.Text = choXuLy.ToString();
                txtDaHuy.Text = daHuy.ToString();
            }
        }

        private void BoLoc_Changed(object sender, RoutedEventArgs e)
        {
            ApDungBoLoc();
        }

        private void NgayLoc_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApDungBoLoc();
        }

        private void btnLoc_Click(object sender, RoutedEventArgs e)
        {
            ApDungBoLoc();
        }

        private void btnDatLai_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem.Clear();
            cboTrangThai.SelectedIndex = 0;
            cboBacSi.SelectedIndex = -1;
            cboPhong.SelectedIndex = -1;
            dpNgayKham.SelectedDate = null;
            ApDungBoLoc();
        }

        private void btnThemLichKham_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            try
            {
                if (ThemLichKham.HienCuaSo(null, PatientId))
                {
                    DuLieuGiaoDien.RefreshFromDatabase();
                    TaiDuLieu();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không mở được form thêm lịch khám: " + ex.Message);
            }
        }

        private void lstLichKham_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject viTriNhan = e.OriginalSource as DependencyObject;
            if (viTriNhan == null) return;


            ListBoxItem dong = ItemsControl.ContainerFromElement(lstLichKham, viTriNhan) as ListBoxItem;
            if (dong == null) return;
            ET_LICHKHAM lichDuocChon = dong.DataContext as ET_LICHKHAM;
            if (lichDuocChon == null) return;

            // Lấy bản mới nhất trước khi xem để tránh dữ liệu cũ.
            try
            {
                ET_LICHKHAM? lich = lichkham.layLichKhamTheoMa(lichDuocChon.MaLich);
                if (lich == null) return;
                ChiTietLichKham chiTiet = new ChiTietLichKham(lich);
                Window cuaSoCha = Window.GetWindow(this);
                if (cuaSoCha != null) chiTiet.Owner = cuaSoCha;
                bool daLuu = chiTiet.ShowDialog() == true;
                if (daLuu)
                {
                    DuLieuGiaoDien.RefreshFromDatabase();
                    TaiDuLieu();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không mở được chi tiết lịch khám: " + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true;
        }

        private void BoLoc_Changed(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
