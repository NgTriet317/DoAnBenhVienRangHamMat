using BUS;
using ET;
using System;
using System.Collections.Generic;
using System.Linq;
using RangHamMat.Controls;
using System.Windows;
using System.Windows.Controls;

namespace RangHamMat.Views
{
    public partial class LichKham : UserControl, IPatientScoped
    {
        BUS_LICHKHAM lichkham = new BUS_LICHKHAM();
        List<ET_LICHKHAM_HIENTHI> danhSach = new List<ET_LICHKHAM_HIENTHI>();
        public string? PatientId { get; private set; }

        public void SetPatient(string? maBenhNhan)
        {
            PatientId = maBenhNhan;
            if (DuLieuGiaoDien.PhamViLamSang && maBenhNhan is not null)
            {
                DuLieuGiaoDien.BaoDamNguCanh(new() { MaBN = maBenhNhan });
                TaiDuLieu();
            }
            else ApDungBoLoc();
            HienFormThem();
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
            AddAppointmentButton.IsEnabled = !DuLieuGiaoDien.PhamViLamSang;
            NapBoLoc();
            TaiDuLieu();
            HienFormThem();
            HoTroGiaoDien.Observe(this, TaiDuLieu);
        }

        public void TaiDuLieu()
        {
            try
            {
                string maDangChon = "";
                if (lstLichKham.SelectedItem is ET_LICHKHAM_HIENTHI lichDangChon)
                    maDangChon = lichDangChon.MaLich;
                if (DuLieuGiaoDien.PhamViLamSang)
                {
                    danhSach = DuLieuGiaoDien.Rows("LICH_KHAM").Select(row =>
                    {
                        var item = UiRecord.ChuyenDoi<ET_LICHKHAM_HIENTHI>(DuLieuGiaoDien.Values(row));
                        item.TenBenhNhan = DuLieuGiaoDien.Lookup("MaBN", item.MaBN);
                        item.TenBacSi = DuLieuGiaoDien.Lookup("MaBS", item.MaBS);
                        item.TenPhong = DuLieuGiaoDien.Lookup("MaPhong", item.MaPhong);
                        item.TenDichVu = DuLieuGiaoDien.Lookup("MaDV", item.MaDV);
                        item.TenCa = DuLieuGiaoDien.Lookup("MaCa", item.MaCa);
                        return item;
                    }).ToList();
                }
                else danhSach = lichkham.layLichKhamHienThi().ToList();
                ApDungBoLoc();
                if (!string.IsNullOrWhiteSpace(maDangChon)) ChonLichTrongDanhSach(maDangChon);
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
            List<ET_LICHKHAM_HIENTHI> ketQua = new List<ET_LICHKHAM_HIENTHI>();
            foreach (ET_LICHKHAM_HIENTHI lich in danhSach)
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
                if (trangThai != "Tất cả" && lich.TinhTrangDieuPhoi != trangThai) continue;
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
                foreach (ET_LICHKHAM_HIENTHI lich in ketQua)
                {
                    if (lich.TinhTrangDieuPhoi == "Đã xác nhận") daXacNhan++;
                    if (lich.TinhTrangDieuPhoi == "Nháp" || lich.TinhTrangDieuPhoi == "Chờ xác nhận" ||
                        lich.TinhTrangDieuPhoi == "Cần phân công lại" || lich.TinhTrangDieuPhoi == "Cần bổ sung dự bị") choXuLy++;
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

        // Không dùng popup. Cùng một panel cho thêm, xem chi tiết và chỉnh sửa.
        private void HienFormThem()
        {
            if (pnlBieuMau == null) return;
            if (DuLieuGiaoDien.PhamViLamSang)
            {
                pnlBieuMau.Content = null;
                return;
            }
            ThemLichKham form = new ThemLichKham(PatientId);
            form.HienTrongPanel(false);
            form.LuuThanhCong += Form_LuuThanhCong;
            form.YeuCauDong += Form_YeuCauDong;
            pnlBieuMau.Content = form;
        }

        private void HienChiTiet(ET_LICHKHAM_HIENTHI lichChon)
        {
            if (DuLieuGiaoDien.PhamViLamSang)
            {
                var record = DuLieuGiaoDien.Find("LICH_KHAM", lichChon.MaLich);
                if (record is not null) HoTroGiaoDien.Workspace(this)?.ShowHoSoDetails("LICH_KHAM", record);
                return;
            }
            try
            {
                // Luôn lấy dữ liệu mới nhất trước khi hiển thị / sửa.
                ET_LICHKHAM? lich = lichkham.layLichKhamTheoMa(lichChon.MaLich);
                if (lich == null)
                {
                    MessageBox.Show("Lịch khám không còn tồn tại. Vui lòng tải lại danh sách.");
                    TaiDuLieu();
                    return;
                }
                ThemLichKham form = new ThemLichKham(lich);
                form.HienTrongPanel(true, lichChon.CanPhanCongLai);
                form.LuuThanhCong += Form_LuuThanhCong;
                form.YeuCauDong += Form_YeuCauDong;
                pnlBieuMau.Content = form;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được chi tiết lịch khám: " + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChonLichTrongDanhSach(string maLich)
        {
            foreach (object item in lstLichKham.Items)
            {
                ET_LICHKHAM_HIENTHI lich = item as ET_LICHKHAM_HIENTHI;
                if (lich == null) continue;
                if (lich.MaLich == maLich)
                {
                    lstLichKham.SelectedItem = item;
                    lstLichKham.ScrollIntoView(item);
                    return;
                }
            }
        }

        private void Form_LuuThanhCong(object? sender, EventArgs e)
        {
            ThemLichKham form = sender as ThemLichKham;
            string maVuaLuu = "";
            if (form != null) maVuaLuu = form.MaLichVuaLuu;
            DuLieuGiaoDien.RefreshFromDatabase();
            TaiDuLieu();
            if (!string.IsNullOrWhiteSpace(maVuaLuu))
            {
                ChonLichTrongDanhSach(maVuaLuu);
                if (lstLichKham.SelectedItem == null)
                {
                    // Lịch vừa lưu không nằm trong bộ lọc hiện tại.
                    HienFormThem();
                }
            }
            else HienFormThem();
        }

        private void Form_YeuCauDong(object? sender, EventArgs e)
        {
            lstLichKham.SelectedItem = null;
            HienFormThem();
        }

        private void btnThemLichKham_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (DuLieuGiaoDien.PhamViLamSang) return;
            lstLichKham.SelectedItem = null;
            HienFormThem();
        }

        // Chọn một dòng là chi tiết được điền ngay bên phải, không phải nhấp đúp.
        private void lstLichKham_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ET_LICHKHAM_HIENTHI lich = lstLichKham.SelectedItem as ET_LICHKHAM_HIENTHI;
            if (lich != null) HienChiTiet(lich);
        }
    }
}
