using BUS;
using ET;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RangHamMat.Views
{
    public class CaHienThi
    {
        public string MaCa { get; set; } = "";
        public string TenCa { get; set; } = "";
        public TimeSpan GioBatDau { get; set; }
        public TimeSpan GioKetThuc { get; set; }
    }

    public partial class ThemLichKham : UserControl
    {
        BUS_LICHKHAM busLichKham = new BUS_LICHKHAM();
        ET_LICHKHAM lichCu;
        bool dangSua;
        string? maBenhNhanKhoa;
        public bool DaLuu { get; private set; }

        public ThemLichKham() : this((string?)null) { }

        public ThemLichKham(string? maBenhNhan)
        {
            InitializeComponent();
            dangSua = false;
            maBenhNhanKhoa = maBenhNhan;
            cboTrangThai.Items.RemoveAt(2);
            NapDanhSachChon();
            DatLaiDuLieu();
        }

        public ThemLichKham(ET_LICHKHAM lich)
        {
            InitializeComponent();
            lichCu = lich;
            dangSua = true;
            NapDanhSachChon();
            txtTieuDeForm.Text = "Chỉnh sửa lịch khám";
            txtMaLich.IsReadOnly = true;
            pnlLyDoThayDoi.Visibility = Visibility.Visible;
            btnLamMoi.Content = "Khôi phục";
            DatLaiDuLieu();
        }

        // Mở cửa sổ riêng, không thay nội dung workspace và không toàn màn hình.
        public static bool HienCuaSo(ET_LICHKHAM? lich, string? maBenhNhan = null)
        {
            ThemLichKham form;
            if (lich == null)
                form = new ThemLichKham(maBenhNhan);
            else
                form = new ThemLichKham(lich);

            Window cuaSo = new Window();
            cuaSo.Title = "Điều phối lịch khám";
            cuaSo.MaxWidth = Math.Max(660, SystemParameters.WorkArea.Width - 50);
            cuaSo.MaxHeight = Math.Max(490, SystemParameters.WorkArea.Height - 50);
            cuaSo.Width = Math.Min(1040, cuaSo.MaxWidth);
            cuaSo.Height = Math.Min(750, cuaSo.MaxHeight);
            cuaSo.MinWidth = Math.Min(800, cuaSo.MaxWidth);
            cuaSo.MinHeight = Math.Min(530, cuaSo.MaxHeight);
            cuaSo.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            cuaSo.ResizeMode = ResizeMode.CanResize;
            cuaSo.ShowInTaskbar = false;
            cuaSo.Content = form;
            if (Application.Current != null)
            {
                if (Application.Current.MainWindow != null)
                    cuaSo.Owner = Application.Current.MainWindow;
            }
            cuaSo.ShowDialog();
            return form.DaLuu;
        }

        private void NapDanhSachChon()
        {
            cboBenhNhan.ItemsSource = new BUS_BENHNHAN().layToanBoBenhNhan().ToList();
            cboBacSi.ItemsSource = LichKham.LayBacSi();
            cboPhong.ItemsSource = new BUS_PHONG().layToanBoPhong().ToList();
            var dsCa = new System.Collections.Generic.List<CaHienThi>();
            foreach (ET_CALAM ca in new BUS_CALAM().layToanBoCaLam())
            {
                CaHienThi item = new CaHienThi();
                item.MaCa = ca.MaCa;
                item.TenCa = ca.GhiChu;
                if (string.IsNullOrWhiteSpace(item.TenCa))
                    item.TenCa = ca.GioBatDau.ToString(@"hh\:mm") + " – " + ca.GioKetThuc.ToString(@"hh\:mm");
                item.GioBatDau = ca.GioBatDau;
                item.GioKetThuc = ca.GioKetThuc;
                dsCa.Add(item);
            }
            cboCa.ItemsSource = dsCa;
            cboDichVu.ItemsSource = new BUS_DICHVU().layToanBoDichVu().ToList();
        }

        // Tự điền giờ khám từ giờ bắt đầu của ca vừa chọn.
        // Người dùng vẫn có thể điều chỉnh giờ trong khoảng thời gian của ca.
        private void cboCa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtGioKham == null) return;

            CaHienThi ca = cboCa.SelectedItem as CaHienThi;
            if (ca == null)
            {
                txtGioKham.Clear();
                return;
            }

            txtGioKham.Text = ca.GioBatDau.ToString(@"hh\:mm");
        }

        private void DatLaiDuLieu()
        {
            XoaBaoLoi();
            cboBenhNhan.SelectedIndex = -1;
            cboDichVu.SelectedIndex = -1;
            cboBacSi.SelectedIndex = -1;
            cboPhong.SelectedIndex = -1;
            cboCa.SelectedIndex = -1;
            txtLyDoKham.Clear();
            txtGhiChu.Clear();
            txtLyDoThayDoi.Clear();
            txtGioKham.Clear();
            txtThoiLuong.Text = "30";
            dpNgayKham.SelectedDate = DateTime.Today.AddDays(1);
            cboUuTien.SelectedIndex = 0;
            cboTrangThai.SelectedIndex = 0;

            if (!dangSua)
            {
                txtMaLich.Text = "Tự sinh khi lưu";
                txtMaLich.IsReadOnly = true;
                if (!string.IsNullOrWhiteSpace(maBenhNhanKhoa))
                {
                    cboBenhNhan.SelectedValue = maBenhNhanKhoa;
                    cboBenhNhan.IsEnabled = false;
                }
                return;
            }

            txtMaLich.Text = lichCu.MaLich;
            cboBenhNhan.SelectedValue = lichCu.MaBN;
            cboBacSi.SelectedValue = lichCu.MaBS;
            cboPhong.SelectedValue = lichCu.MaPhong;
            cboCa.SelectedValue = lichCu.MaCa;
            cboDichVu.SelectedValue = lichCu.MaDV;
            dpNgayKham.SelectedDate = lichCu.NgayGioKham.Date;
            txtGioKham.Text = lichCu.NgayGioKham.ToString("HH:mm");
            txtThoiLuong.Text = lichCu.ThoiLuongDuKien.ToString();
            txtLyDoKham.Text = lichCu.LyDoKham;
            txtGhiChu.Text = lichCu.GhiChu;
            txtLyDoThayDoi.Text = "";
            ChonGiaTriCombo(cboUuTien, lichCu.UuTien);
            ChonGiaTriCombo(cboTrangThai, lichCu.TrangThai);
        }

        private void ChonGiaTriCombo(ComboBox combo, string giaTri)
        {
            foreach (object item in combo.Items)
            {
                ComboBoxItem dong = item as ComboBoxItem;
                if (dong == null) continue;
                if (Convert.ToString(dong.Content) == giaTri)
                {
                    combo.SelectedItem = dong;
                    return;
                }
            }
        }

        private string GiaTriChon(ComboBox combo)
        {
            if (combo.SelectedValue == null) return "";
            return combo.SelectedValue.ToString();
        }

        private string NoiDungChon(ComboBox combo)
        {
            ComboBoxItem item = combo.SelectedItem as ComboBoxItem;
            if (item == null) return "";
            return Convert.ToString(item.Content);
        }

        private void BaoLoi(Control dieuKhien, string thongBao)
        {
            dieuKhien.BorderBrush = Brushes.IndianRed;
            dieuKhien.BorderThickness = new Thickness(2);
            dieuKhien.Focus();
            MessageBox.Show(thongBao, "Dữ liệu chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void XoaBaoLoi()
        {
            Control[] danhSach = { txtMaLich, cboBenhNhan, cboBacSi, cboPhong, cboCa,
                cboDichVu, dpNgayKham, txtGioKham, txtThoiLuong, cboUuTien,
                cboTrangThai, txtLyDoKham, txtLyDoThayDoi, txtGhiChu };
            foreach (Control control in danhSach)
            {
                control.ClearValue(Control.BorderBrushProperty);
                control.ClearValue(Control.BorderThicknessProperty);
            }
        }

        private bool KiemTraDuLieu(out ET_LICHKHAM lichMoi)
        {
            lichMoi = new ET_LICHKHAM();
            XoaBaoLoi();
            if (dangSua && string.IsNullOrWhiteSpace(txtMaLich.Text))
            {
                BaoLoi(txtMaLich, "Mã lịch khám không hợp lệ.");
                return false;
            }
            if (cboBenhNhan.SelectedValue == null)
            {
                BaoLoi(cboBenhNhan, "Vui lòng chọn bệnh nhân.");
                return false;
            }
            if (cboBacSi.SelectedValue == null)
            {
                BaoLoi(cboBacSi, "Vui lòng chọn bác sĩ.");
                return false;
            }
            if (cboPhong.SelectedValue == null)
            {
                BaoLoi(cboPhong, "Vui lòng chọn phòng khám.");
                return false;
            }
            if (cboCa.SelectedValue == null)
            {
                BaoLoi(cboCa, "Vui lòng chọn ca khám.");
                return false;
            }
            if (!dpNgayKham.SelectedDate.HasValue)
            {
                BaoLoi(dpNgayKham, "Vui lòng chọn ngày khám.");
                return false;
            }
            DateTime gioNhap;
            if (!DateTime.TryParseExact(txtGioKham.Text.Trim(), "HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out gioNhap))
            {
                BaoLoi(txtGioKham, "Giờ khám phải theo định dạng HH:mm (ví dụ 08:30).");
                return false;
            }
            int thoiLuong;
            if (!int.TryParse(txtThoiLuong.Text.Trim(), out thoiLuong))
            {
                BaoLoi(txtThoiLuong, "Thời lượng khám phải là số nguyên (phút).");
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtLyDoKham.Text))
            {
                BaoLoi(txtLyDoKham, "Lý do khám không được để trống.");
                return false;
            }
            if (dangSua && string.IsNullOrWhiteSpace(txtLyDoThayDoi.Text))
            {
                BaoLoi(txtLyDoThayDoi, "Vui lòng nhập lý do chỉnh sửa lịch khám.");
                return false;
            }

            lichMoi.MaLich = dangSua ? lichCu.MaLich : "";
            lichMoi.MaBN = GiaTriChon(cboBenhNhan);
            lichMoi.MaBS = GiaTriChon(cboBacSi);
            lichMoi.MaPhong = GiaTriChon(cboPhong);
            lichMoi.MaCa = GiaTriChon(cboCa);
            if (cboDichVu.SelectedValue == null)
                lichMoi.MaDV = null;
            else
                lichMoi.MaDV = GiaTriChon(cboDichVu);
            lichMoi.NgayGioKham = dpNgayKham.SelectedDate.Value.Date.Add(gioNhap.TimeOfDay);
            lichMoi.ThoiLuongDuKien = thoiLuong;
            lichMoi.UuTien = NoiDungChon(cboUuTien);
            lichMoi.TrangThai = NoiDungChon(cboTrangThai);
            lichMoi.LyDoKham = txtLyDoKham.Text.Trim();
            lichMoi.GhiChu = txtGhiChu.Text.Trim();
            lichMoi.LyDoThayDoi = txtLyDoThayDoi.Text.Trim();
            lichMoi.NgayTao = DateTime.Now;
            lichMoi.NgayCapNhat = dangSua ? DateTime.Now : null;
            if (dangSua) lichMoi.NgayTao = lichCu.NgayTao;
            
            if (thoiLuong < 5 || thoiLuong > 480)
            {
                BaoLoi(txtThoiLuong, "Thời lượng từ 5 đến 480 phút.");
                return false;
            }
            CaHienThi? caChon = cboCa.SelectedItem as CaHienThi;
            if (caChon == null || gioNhap.TimeOfDay < caChon.GioBatDau ||
                gioNhap.TimeOfDay.Add(TimeSpan.FromMinutes(thoiLuong)) > caChon.GioKetThuc)
            {
                BaoLoi(txtGioKham, "Giờ khám và thời lượng phải nằm trong khung giờ của ca.");
                return false;
            }
            // Báo sớm tại giao diện; DAL vẫn kiểm tra lại khi lưu để tránh dữ liệu thay đổi.
            BUS_DON_XIN_NGHI busNghi = new BUS_DON_XIN_NGHI();
            if (busNghi.daDuocDuyetNghi(lichMoi.MaBS, lichMoi.NgayGioKham.Date))
            {
                BaoLoi(cboBacSi, "Bác sĩ này đã được duyệt nghỉ trong ngày khám.");
                return false;
            }
            // Kiểm tra xung đột với lịch chưa hủy/hoàn tất đang có.
            foreach (ET_LICHKHAM lich in busLichKham.layToanBoLichKham())
            {
                if (lich.MaLich == lichMoi.MaLich ||
                    lich.TrangThai is "Hủy" or "Đã hủy" or "Đã đổi" or "Hoàn thành" or "Đã khám") continue;
                if (lich.NgayGioKham < lichMoi.NgayGioKham.AddMinutes(thoiLuong) &&
                    lich.NgayGioKham.AddMinutes(lich.ThoiLuongDuKien) > lichMoi.NgayGioKham &&
                    (lich.MaBS == lichMoi.MaBS || lich.MaPhong == lichMoi.MaPhong || lich.MaBN == lichMoi.MaBN))
                {
                    MessageBox.Show("Bệnh nhân, bác sĩ hoặc phòng đã có lịch trùng giờ.");
                    return false;
                }
            }
            return true;
        }

        private void btnLuuLich_Click(object sender, RoutedEventArgs e)
        {
            ET_LICHKHAM lichMoi;
            try
            {
                if (!KiemTraDuLieu(out lichMoi)) return;
                bool thanhCong;
                if (dangSua)
                    thanhCong = busLichKham.suaLichKham(lichMoi, lichCu);
                else
                    thanhCong = busLichKham.themLichKham(lichMoi);
                if (!thanhCong)
                {
                    MessageBox.Show("Không lưu được lịch khám. Có thể mã đã tồn tại, lịch bị trùng hoặc trạng thái đã thay đổi.",
                        "Không thể lưu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                DaLuu = true;
                MessageBox.Show("Lưu lịch khám thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                Window cuaSo = Window.GetWindow(this);
                if (cuaSo != null)
                {
                    // Báo về màn hình lịch khám để tải lại danh sách sau khi lưu.
                    cuaSo.DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể lưu lịch khám: " + ex.Message,
                    "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            DatLaiDuLieu();
        }

        private void btnDong_Click(object sender, RoutedEventArgs e)
        {
            Window cuaSo = Window.GetWindow(this);
            if (cuaSo != null) cuaSo.Close();
        }

        private void EditorTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
