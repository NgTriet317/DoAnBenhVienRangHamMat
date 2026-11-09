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
    /// <summary>
    /// Phần xử lý lịch khám của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public partial class LichKham : UserControl, IPatientScoped
    {
        /// <summary>
        /// Đối tượng nghiệp vụ lịch khám được màn hình dùng để đọc/ghi dữ liệu.
        /// </summary>
        BUS_LICHKHAM lichkham = new BUS_LICHKHAM();
        /// <summary>
        /// Danh sách  dùng trong lịch khám được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        List<ET_LICHKHAM_HIENTHI> danhSach = new List<ET_LICHKHAM_HIENTHI>();
        /// <summary>
        /// Mã bệnh nhân. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? PatientId { get; private set; }

        /// <summary>
        /// Áp dụng bệnh nhân được chỉ định làm phạm vi dữ liệu của màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>DuLieuGiaoDien.BaoDamNguCanh</c>, <c>TaiDuLieu</c>, <c>ApDungBoLoc</c>, <c>HienFormThem</c>.</remarks>
        /// <param name="maBenhNhan">Mã bệnh nhân xác định đối tượng cần xử lý.</param>
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

        // CV002 là mã bác sĩ chuẩn ở các phân hệ lâm sàng.
        // Giữ nhận diện theo tên chức vụ cũ để không làm mất các vị trí bác sĩ tự tạo.
        internal static List<ET_NHANVIEN> LayBacSi()
        {
            var maChucVuBacSi = new BUS_CHUCVU().layToanBoChucVu()
                .Where(cv => string.Equals(cv.MaChucVu?.Trim(), "CV002", StringComparison.OrdinalIgnoreCase) ||
                    (cv.TenChucVu?.Contains("Bác sĩ", StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(cv => cv.MaChucVu).ToList();
            return new BUS_NHANVIEN().layToanBoNhanVien()
                .Where(nv => maChucVuBacSi.Contains(nv.MaChucVu)).ToList();
        }

        /// <summary>
        /// Khởi tạo lịch khám. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
        /// </summary>
        public LichKham()
        {
            InitializeComponent();
            AddAppointmentButton.IsEnabled = !DuLieuGiaoDien.PhamViLamSang;
            NapBoLoc();
            TaiDuLieu();
            HienFormThem();
            HoTroGiaoDien.Observe(this, TaiDuLieu);
        }

        /// <summary>
        /// Đọc lại dữ liệu theo phạm vi hiện tại, áp dụng bộ lọc và cập nhật danh sách/chi tiết.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>NapDanhSachBacSi</c>, <c>form.NapLaiDanhSachBacSi</c>, <c>DuLieuGiaoDien.Rows</c>, <c>UiRecord.ChuyenDoi&lt;ET_LICHKHAM_HIENTHI&gt;</c>.</remarks>
        public void TaiDuLieu()
        {
            try
            {
                // Nhân sự vừa đổi chức vụ phải có trong cả bộ lọc và biểu mẫu lập lịch.
                NapDanhSachBacSi();
                if (pnlBieuMau?.Content is ThemLichKham form)
                    form.NapLaiDanhSachBacSi();
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

        /// <summary>
        /// Nạp các danh sách dùng cho bộ lọc và đặt lựa chọn ban đầu.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>NapDanhSachBacSi</c>.</remarks>
        private void NapBoLoc()
        {
            NapDanhSachBacSi();
            cboPhong.ItemsSource = new BUS_PHONG().layToanBoPhong().ToList();
            cboBacSi.SelectedIndex = -1;
            cboPhong.SelectedIndex = -1;
        }

        /// <summary>
        /// Nạp lại danh sách bác sĩ và khôi phục mã đang chọn để giữ bộ lọc hiện tại.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>.ToString</c>, <c>LayBacSi</c>.</remarks>
        private void NapDanhSachBacSi()
        {
            // Giữ bộ lọc bác sĩ hiện tại khi làm mới, không xóa lựa chọn người dùng.
            string maDangChon = cboBacSi.SelectedValue?.ToString() ?? "";
            cboBacSi.ItemsSource = LayBacSi();
            if (maDangChon.Length > 0) cboBacSi.SelectedValue = maDangChon;
        }

        /// <summary>
        /// Áp dụng các tiêu chí đang chọn để cập nhật danh sách và trạng thái rỗng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>txtTimKiem.Text.Trim</c>, <c>cboBacSi.SelectedValue.ToString</c>, <c>cboPhong.SelectedValue.ToString</c>, <c>lich.MaLich.Contains</c>.</remarks>
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

        /// <summary>
        /// Tiếp nhận thay đổi bộ lọc và cập nhật trạng thái/dữ liệu phụ thuộc.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ApDungBoLoc</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void BoLoc_Changed(object sender, RoutedEventArgs e)
        {
            ApDungBoLoc();
        }

        /// <summary>
        /// Tiếp nhận thay đổi ngày lọc và cập nhật trạng thái/dữ liệu phụ thuộc.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ApDungBoLoc</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void NgayLoc_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApDungBoLoc();
        }

        /// <summary>
        /// Xử lý thao tác nút lọc khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ApDungBoLoc</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnLoc_Click(object sender, RoutedEventArgs e)
        {
            ApDungBoLoc();
        }

        /// <summary>
        /// Xử lý thao tác nút đặt lại khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>txtTimKiem.Clear</c>, <c>ApDungBoLoc</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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

        /// <summary>
        /// Đưa dữ liệu mục đang chọn vào vùng chi tiết, đồng thời cập nhật các nút thao tác liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>DuLieuGiaoDien.Find</c>, <c>HoTroGiaoDien.Workspace</c>, <c>.ShowHoSoDetails</c>, <c>lichkham.layLichKhamTheoMa</c>.</remarks>
        /// <param name="lichChon">Giá trị lịch chọn dùng khi thực hiện thao tác; kiểu ET_LICHKHAM_HIENTHI.</param>
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

        /// <summary>
        /// Chọn lịch khám theo mã để hiển thị lại đúng bản ghi sau khi tải danh sách.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>lstLichKham.ScrollIntoView</c>.</remarks>
        /// <param name="maLich">Mã lịch xác định đối tượng cần xử lý.</param>
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

        /// <summary>
        /// Tiếp nhận thông báo biểu mẫu đã lưu, tải lại dữ liệu và chọn lịch vừa xử lý.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>DuLieuGiaoDien.RefreshFromDatabase</c>, <c>TaiDuLieu</c>, <c>ChonLichTrongDanhSach</c>, <c>HienFormThem</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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

        /// <summary>
        /// Tiếp nhận yêu cầu đóng biểu mẫu để quay về trạng thái xem lịch khám.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>HienFormThem</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void Form_YeuCauDong(object? sender, EventArgs e)
        {
            lstLichKham.SelectedItem = null;
            HienFormThem();
        }

        /// <summary>
        /// Xử lý thao tác nút thêm lịch khám khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>HienFormThem</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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
