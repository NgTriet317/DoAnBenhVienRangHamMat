using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using BUS;

namespace RangHamMat.Views
{
    /// <summary>
    /// Phần xử lý bảng lương của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public partial class BangLuong : UserControl
    {
        /// <summary>
        /// Đối tượng nghiệp vụ bảng lương được màn hình dùng để đọc/ghi dữ liệu.
        /// </summary>
        private readonly BUS_BANGLUONG busBangLuong = new BUS_BANGLUONG();
        /// <summary>
        /// Danh sách lương dùng trong bảng lương được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private DataTable danhSachLuong = new DataTable();
        /// <summary>
        /// Danh sách đối soát dùng trong bảng lương được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private DataTable danhSachDoiSoat = new DataTable();
        /// <summary>
        /// Kỳ đã tính được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private DateTime? kyDaTinh;
        /// <summary>
        /// Ngày máy chủ được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private DateTime ngayMayChu;
        /// <summary>
        /// Cờ Đã cài tính lương; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool daCaiTinhLuong;
        /// <summary>
        /// Lọc tìm kiểm được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string locTimKiem = "";
        /// <summary>
        /// Lọc chức vụ được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string locChucVu = "";
        /// <summary>
        /// Lọc trạng thái được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string locTrangThai = "";
        /// <summary>
        /// Cờ Đang tải; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool dangTai;
        /// <summary>
        /// Cờ Đã tải; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool daTai;

        /// <summary>
        /// Khởi tạo bảng lương. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
        /// </summary>
        public BangLuong()
        {
            InitializeComponent();
            cboThang.SelectedIndex = DateTime.Now.Month - 1;
            txtNam.Text = DateTime.Now.Year.ToString();
            Loaded += (sender, e) => { if (!daTai) hienThiDuLieu(); };
        }

        // Đọc dữ liệu đã lưu, hiển thị trạng thái đối soát theo từng nhân viên.
        public void hienThiDuLieu()
        {
            if (dangTai) return;
            if (cboThang.SelectedIndex < 0 || !int.TryParse(txtNam.Text.Trim(), out int nam)
                || nam < 2000 || nam > 2100)
            {
                MessageBox.Show("Vui lòng chọn tháng và năm hợp lệ.","Thông báo",
                    MessageBoxButton.OK,MessageBoxImage.Warning);
                return;
            }

            if (!PhienDangNhap.IsAdmin)
            {
                gridBangLuong.ItemsSource = null;
                lblTrangThaiKy.Text = "●  KHÔNG CÓ QUYỀN";
                lblThongBao.Text = "Chỉ tài khoản quản lý/kế toán được phép xem bảng lương.";
                btnTinhLuong.IsEnabled = false;
                xoaChiTiet();
                return;
            }

            dangTai = true;
            try
            {
                string maBLDangChon = (gridBangLuong.SelectedItem as DataRowView)?["MaBL"].ToString() ?? "";
                bool laV9 = busBangLuong.suDungSchemaV9();
                daCaiTinhLuong = laV9 && busBangLuong.daCaiTinhLuong();
                ngayMayChu = busBangLuong.layNgayMayChu();
                btnTinhLuong.IsEnabled = daCaiTinhLuong;
                DateTime tuNgay = new DateTime(nam,cboThang.SelectedIndex+1,1);
                DateTime denNgay = tuNgay.AddMonths(1).AddDays(-1);
                danhSachLuong = busBangLuong.layBangLuongTheoKy(tuNgay,denNgay);
                int choDoiSoat = busBangLuong.demCongChoDoiSoat(tuNgay,denNgay);
                danhSachDoiSoat = laV9 ? busBangLuong.layDoiSoatTheoKy(tuNgay,denNgay) : new DataTable();

                lblTongNhanVien.Text = danhSachLuong.Rows.Count.ToString("N0");
                lblQuyLuong.Text = danhSachLuong.AsEnumerable().Sum(h => h.Field<decimal>("TongLuong")).ToString("N0")+" đ";
                lblHoaHong.Text = danhSachLuong.AsEnumerable().Sum(h => h.Field<decimal>("TienHoaHong")).ToString("N0")+" đ";
                lblChoDoiSoat.Text = choDoiSoat < 0 ? "Chưa có" : choDoiSoat.ToString("N0");
                lblThongTinKy.Text = $"{tuNgay:MM/yyyy}  •  {danhSachLuong.Rows.Count} NHÂN VIÊN";
                lblTrangThaiKy.Text = !laV9 ? "●  DATABASE CŨ" : choDoiSoat > 0 ? "●  CHỜ ĐỐI SOÁT" : "●  ĐÃ TẢI SỐ LIỆU";

                cboChucVu.Items.Clear();
                cboChucVu.Items.Add(new ComboBoxItem { Content = "Tất cả chức vụ" });
                foreach (string tenChucVu in danhSachLuong.AsEnumerable()
                    .Select(h => h.Field<string>("TenChucVu") ?? "")
                    .Where(t => t != "").Distinct().OrderBy(t => t))
                    cboChucVu.Items.Add(new ComboBoxItem { Content = tenChucVu });
                cboChucVu.SelectedIndex = 0;
                locChucVu = "";
                cboTrangThai.SelectedIndex = 0;
                locTrangThai = "";

                danhSachLuong.DefaultView.Sort = "HoTen ASC";
                gridBangLuong.ItemsSource = danhSachLuong.DefaultView;
                locTimKiem = txtTimKiem.Text.Trim()=="" ? "" :
                    $"(MaNV LIKE '%{txtTimKiem.Text.Trim()}%' OR HoTen LIKE '%{txtTimKiem.Text.Trim()}%')";
                locDuLieu();
                lblGridRong.Visibility = danhSachLuong.Rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                gridBangLuong.SelectedItem = null;
                foreach (DataRowView hang in danhSachLuong.DefaultView)
                {
                    if (hang["MaBL"].ToString() == maBLDangChon)
                    {
                        gridBangLuong.SelectedItem = hang;
                        break;
                    }
                }
                if (gridBangLuong.SelectedItem == null && danhSachLuong.DefaultView.Count > 0)
                    gridBangLuong.SelectedIndex = 0;
                if (gridBangLuong.SelectedItem == null) xoaChiTiet();
                capNhatDoiSoat();

                lblThongBao.Text = !laV9
                    ? "Đang xem bảng lương của database cũ (có thể là kỳ 01–10/10). Đây là công thức cũ, CHƯA ĐƯỢC dùng để chi trả. Cần nâng cấp database để tính lương mới."
                    : danhSachLuong.Rows.Count == 0
                    ? "Không tìm thấy bảng lương của kỳ này trong SQL. Chọn kỳ có dữ liệu hoặc kiểm tra việc nạp SQL V9."
                    : !daCaiTinhLuong
                    ? "Chưa cài SQL tính/chốt lương. Chạy toàn bộ Database/QLBenhVienRangHamMat.sql trên máy chủ đang kết nối."
                    : danhSachLuong.AsEnumerable().Any(h => h.Field<int>("PhienBanTinhLuong") == 8
                        && (h.Field<DateTime>("TuNgay") != tuNgay || h.Field<DateTime>("DenNgay") != denNgay))
                    ? "Có phiếu cũ chưa đủ tháng hoặc chồng kỳ; tiền được giữ nguyên. Cần đối chiếu phiếu này trước khi lập/tính cả tháng để tránh chi trả trùng."
                    : danhSachLuong.AsEnumerable().Any(h => h.Field<int>("PhienBanTinhLuong") == 8)
                    ? "Có phiếu từ công thức cũ; số tiền trước đồng bộ được giữ nguyên. Tính lương sẽ cập nhật các phiếu Nháp theo V9; phiếu đã chốt được giữ nguyên."
                    : kyDaTinh==tuNgay
                    ? "Đã tính lại phiếu Nháp từ nguồn SQL. Chọn nhân viên, kiểm tra đối soát và chốt khi đủ điều kiện."
                    : "Chọn Tính lương để cập nhật phiếu Nháp từ công, phép và chứng từ; chỉ chốt sau khi kiểm tra đầy đủ.";
                daTai = true;
            }
            catch (Exception ex)
            {
                daTai = false;
                danhSachLuong = new DataTable();
                danhSachDoiSoat = new DataTable();
                kyDaTinh = null;
                btnTinhLuong.IsEnabled = false;
                gridBangLuong.ItemsSource = null;
                lblTongNhanVien.Text = "—";
                lblQuyLuong.Text = "—";
                lblHoaHong.Text = "—";
                lblChoDoiSoat.Text = "—";
                lblGridRong.Visibility = Visibility.Visible;
                lblGridRong.Text = "Không tải được dữ liệu bảng lương.";
                lblThongBao.Text = "Không tải được dữ liệu từ SQL. Cần database SQL V9 (có BANG_LUONG.CongThucTe và CHAM_CONG).";
                xoaChiTiet();
                MessageBox.Show("Không thể tải bảng lương. Kiểm tra kết nối và phiên bản SQL: " + ex.Message,
                    "Thông báo",MessageBoxButton.OK,MessageBoxImage.Error);
            }
            finally { dangTai = false; }
        }

        /// <summary>
        /// Lọc dữ liệu đã nạp theo từ khóa và các lựa chọn kỳ/trạng thái của màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>xoaChiTiet</c>.</remarks>
        private void locDuLieu()
        {
            if (danhSachLuong.Columns.Count == 0) return;
            string dieuKien = locTimKiem;
            if (locChucVu != "") dieuKien += (dieuKien == "" ? "" : " AND ")+locChucVu;
            if (locTrangThai != "") dieuKien += (dieuKien == "" ? "" : " AND ")+locTrangThai;
            danhSachLuong.DefaultView.RowFilter = dieuKien;
            lblGridRong.Text = danhSachLuong.Rows.Count == 0
                ? "Kỳ lương chưa có dữ liệu trong SQL." : "Không có nhân viên phù hợp với bộ lọc.";
            lblGridRong.Visibility = danhSachLuong.DefaultView.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (danhSachLuong.DefaultView.Count > 0 && gridBangLuong.SelectedItem == null)
                gridBangLuong.SelectedIndex = 0;
            if (danhSachLuong.DefaultView.Count == 0) xoaChiTiet();
        }

        /// <summary>
        /// Tiếp nhận thay đổi ô nhập tìm kiểm và cập nhật trạng thái/dữ liệu phụ thuộc.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>Regex.Replace</c>, <c>noiDung.Trim</c>, <c>locDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            string noiDung = Regex.Replace(txtTimKiem.Text, @"[^\p{L}\p{M}\p{N} ]", "");
            if (txtTimKiem.Text != noiDung)
            {
                int viTri = txtTimKiem.SelectionStart;
                txtTimKiem.Text = noiDung;
                txtTimKiem.SelectionStart = Math.Min(viTri, noiDung.Length);
                return;
            }
            string timKiem = noiDung.Trim();
            locTimKiem = timKiem == "" ? "" : $"(MaNV LIKE '%{timKiem}%' OR HoTen LIKE '%{timKiem}%')";
            if (daTai && !dangTai) locDuLieu();
        }

        /// <summary>
        /// Phản ánh lựa chọn hộp chọn chức vụ vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>.ToString</c>, <c>chucVu.Replace</c>, <c>locDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void cboChucVu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string chucVu = (cboChucVu.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            locChucVu = chucVu == "" || chucVu == "Tất cả chức vụ" ? "" : $"TenChucVu = '{chucVu.Replace("'", "''")}'";
            if (daTai && !dangTai) locDuLieu();
        }

        /// <summary>
        /// Phản ánh lựa chọn hộp chọn trạng thái vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>.ToString</c>, <c>locDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void cboTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string trangThai = (cboTrangThai.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            locTrangThai = trangThai == "" || trangThai == "Tất cả" ? "" : $"TrangThai = '{trangThai}'";
            if (daTai && !dangTai) locDuLieu();
        }

        /// <summary>
        /// Phản ánh lựa chọn hộp chọn tháng vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>hienThiDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void cboThang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (daTai && !dangTai) { kyDaTinh = null; hienThiDuLieu(); }
        }

        /// <summary>
        /// Tiếp nhận thay đổi ô nhập năm và cập nhật trạng thái/dữ liệu phụ thuộc.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>capNhatDoiSoat</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void txtNam_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            kyDaTinh = null;
            capNhatDoiSoat();
        }

        /// <summary>
        /// Phản ánh lựa chọn bảng hiển thị bảng lương vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>xoaChiTiet</c>, <c>hang[&quot;HoTen&quot;].ToString</c>, <c>hang[&quot;TrangThai&quot;].ToString</c>, <c>capNhatDoiSoat</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void gridBangLuong_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridBangLuong.SelectedItem is not DataRowView hang) { xoaChiTiet(); return; }
            lblNhanVienChiTiet.Text = hang["HoTen"].ToString() + "  •  " + hang["MaNV"];
            lblMaPhieuChiTiet.Text = "Mã phiếu: " + hang["MaBL"] + "  •  " + hang["TenChucVu"];
            lblKyChiTiet.Text = Convert.ToDateTime(hang["TuNgay"]).ToString("dd/MM/yyyy") + " – "
                + Convert.ToDateTime(hang["DenNgay"]).ToString("dd/MM/yyyy")
                + (Convert.ToInt32(hang["PhienBanTinhLuong"]) == 8 ? " • Công thức cũ" : "");
            lblCongChiTiet.Text = Convert.ToDecimal(hang["CongThucTe"]).ToString("N2") + " / "
                + Convert.ToDecimal(hang["CongChuan"]).ToString("N2");
            lblTrangThaiPhieu.Text = hang["TrangThai"].ToString() ?? "—";
            lblLuongCong.Text = Convert.ToDecimal(hang["LuongTheoCa"]).ToString("N0")+" đ";
            lblHoaHongCaNhan.Text = Convert.ToDecimal(hang["TienHoaHong"]).ToString("N0")+" đ";
            lblPhuCapTangCa.Text = (Convert.ToDecimal(hang["TienPhuCap"])+Convert.ToDecimal(hang["TienTangCa"])).ToString("N0")+" đ";
            lblBaoHiem.Text = Convert.ToDecimal(hang["TienBaoHiem"]).ToString("N0")+" đ";
            lblKhauTru.Text = Convert.ToDecimal(hang["TienKhauTruKhac"]).ToString("N0")+" đ";
            lblThucNhanChiTiet.Text = Convert.ToDecimal(hang["TongLuong"]).ToString("N0")+" đ";
            capNhatDoiSoat();
        }

        /// <summary>
        /// Xóa nội dung chi tiết và lựa chọn đang xem trên màn hình.
        /// </summary>
        private void xoaChiTiet()
        {
            lblNhanVienChiTiet.Text = "Chọn nhân viên trong sổ lương";
            lblMaPhieuChiTiet.Text = "—";
            lblKyChiTiet.Text = "—";
            lblCongChiTiet.Text = "—";
            lblTrangThaiPhieu.Text = "—";
            lblLuongCong.Text = "—";
            lblHoaHongCaNhan.Text = "—";
            lblPhuCapTangCa.Text = "—";
            lblBaoHiem.Text = "—";
            lblKhauTru.Text = "—";
            lblThucNhanChiTiet.Text = "—";
            lblDoiSoatChiTiet.Text = "Chọn nhân viên để kiểm tra điều kiện chốt.";
            btnChotLuong.IsEnabled = false;
            btnChotLuong.Content = "Chọn phiếu cần chốt";
        }

        /// <summary>
        /// Xử lý thao tác nút mở chấm công khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>Window.GetWindow</c>, <c>main.OpenApp</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnMoChamCong_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow main) main.OpenApp("ChamCong");
        }

        /// <summary>
        /// Xử lý thao tác nút tải lại khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>hienThiDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnTaiLai_Click(object sender, RoutedEventArgs e)
        {
            // Tải lại cùng kỳ không xóa trạng thái đã tính của kỳ này.
            // Khi đổi tháng/năm, sự kiện chọn kỳ vẫn yêu cầu tính lại.
            hienThiDuLieu();
        }

        /// <summary>
        /// Xử lý thao tác nút tính lương khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>int.TryParse</c>, <c>txtNam.Text.Trim</c>, <c>tuNgay.AddMonths</c>, <c>busBangLuong.tinhLuongTheoKy</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnTinhLuong_Click(object sender, RoutedEventArgs e)
        {
            if (!PhienDangNhap.IsAdmin || !daCaiTinhLuong) return;
            if (cboThang.SelectedIndex<0 || !int.TryParse(txtNam.Text.Trim(), out int nam)
                || nam<2000 || nam>2100) return;
            DateTime tuNgay = new DateTime(nam,cboThang.SelectedIndex+1,1);
            DateTime denNgay = tuNgay.AddMonths(1).AddDays(-1);
            try
            {
                btnTinhLuong.IsEnabled = false;
                kyDaTinh = null;
                busBangLuong.tinhLuongTheoKy(tuNgay,denNgay,PhienDangNhap.MaNV);
                kyDaTinh = tuNgay;
                hienThiDuLieu();
                MessageBox.Show("Đã tính lại các phiếu Nháp trong kỳ. Hãy chọn từng nhân viên để kiểm tra trước khi chốt.",
                    "Tính lương",MessageBoxButton.OK,MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                kyDaTinh = null;
                MessageBox.Show("Không thể tính lương: " + ex.Message,
                    "Thông báo",MessageBoxButton.OK,MessageBoxImage.Error);
            }
            finally { btnTinhLuong.IsEnabled = daCaiTinhLuong; capNhatDoiSoat(); }
        }

        // Mỗi nhân viên được chốt riêng, không làm thay đổi phiếu người khác.
        private void btnChotLuong_Click(object sender, RoutedEventArgs e)
        {
            if (!btnChotLuong.IsEnabled || !PhienDangNhap.IsAdmin ||
                gridBangLuong.SelectedItem is not DataRowView hang) return;
            string maBL = hang["MaBL"].ToString() ?? "";
            string hoTen = hang["HoTen"].ToString() ?? "";
            decimal cong = Convert.ToDecimal(hang["CongThucTe"]);
            decimal tong = Convert.ToDecimal(hang["TongLuong"]);
            decimal hoaHong = Convert.ToDecimal(hang["TienHoaHong"]);
            if (MessageBox.Show($"Xác nhận CHỐT LƯƠNG cho {hoTen} ({hang["MaNV"]})?\n" +
                    $"Công thực tế: {cong:N2}\nHoa hồng: {hoaHong:N0} đ\nThực nhận: {tong:N0} đ\n\n" +
                    "Phiếu chốt sẽ bị khóa, không được sửa lại.", "Xác nhận chốt lương",
                    MessageBoxButton.YesNo,MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                btnChotLuong.IsEnabled = false;
                busBangLuong.chotLuongNhanVien(maBL,PhienDangNhap.MaNV,cong,tong,hoaHong);
                hienThiDuLieu();
                MessageBox.Show("Đã chốt lương nhân viên " + hoTen + ".",
                    "Thành công",MessageBoxButton.OK,MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                kyDaTinh = null;
                hienThiDuLieu();
                MessageBox.Show("Không thể chốt lương: " + ex.Message +
                    "\nHãy đối soát, tính lại và kiểm tra phiếu trước khi thử chốt.",
                    "Thông báo",MessageBoxButton.OK,MessageBoxImage.Error);
            }
            finally { capNhatDoiSoat(); }
        }

        /// <summary>
        /// Tổng hợp hoặc hiển thị các chênh lệch cần đối soát trước khi tính/chốt lương.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>hang[&quot;TrangThai&quot;].ToString</c>, <c>dong[&quot;MaBL&quot;].ToString</c>, <c>hang[&quot;MaBL&quot;].ToString</c>, <c>int.TryParse</c>.</remarks>
        private void capNhatDoiSoat()
        {
            btnChotLuong.IsEnabled = false;
            btnChotLuong.Content = "Chưa đủ điều kiện chốt";
            if (gridBangLuong.SelectedItem is not DataRowView hang)
            {
                lblDoiSoatChiTiet.Text = "Chọn nhân viên để đối soát.";
                return;
            }
            if (hang["TrangThai"].ToString() == "Đã chốt")
            {
                btnChotLuong.Content = "Đã chốt - phiếu bị khóa";
                lblDoiSoatChiTiet.Text = "Phiếu đã được chốt và không thể sửa.";
                return;
            }
            if (!daCaiTinhLuong)
            {
                lblDoiSoatChiTiet.Text = "Chưa cài SQL tính/chốt lương V9.";
                return;
            }
            DataRow? doiSoat = null;
            foreach (DataRow dong in danhSachDoiSoat.Rows)
                if (dong["MaBL"].ToString()==hang["MaBL"].ToString()) { doiSoat=dong; break; }
            if (doiSoat == null)
            {
                lblDoiSoatChiTiet.Text = "Không có dữ liệu đối soát cho đúng tháng của phiếu lương. " +
                    "Kiểm tra TuNgay/DenNgay trong SQL (phiếu cũ có thể chưa đủ tháng).";
                btnChotLuong.Content = "Không khớp kỳ lương";
                return;
            }
            int caCho = Convert.ToInt32(doiSoat["CongChuaDuyet"]);
            int donCho = Convert.ToInt32(doiSoat["DonNghiChoDuyet"]);
            int congTrung = Convert.ToInt32(doiSoat["CongTrungNgayPhep"]);
            int hoaHongSai = Convert.ToInt32(doiSoat["HoaHongChuaHopLe"]);
            int khoanCongSai = Convert.ToInt32(doiSoat["KhoanCongChuaPhanLoai"]);
            int chungTuCho = Convert.ToInt32(doiSoat["ChungTuChuaDuyet"]);
            bool chinhSachDung = Convert.ToBoolean(doiSoat["ChinhSachHopLe"]);
            DateTime tuNgay = Convert.ToDateTime(hang["TuNgay"]);
            DateTime ngayDuocChot = Convert.ToDateTime(hang["DenNgay"]).Date.AddDays(5);
            bool dungKyDangChon = cboThang.SelectedIndex+1==tuNgay.Month &&
                int.TryParse(txtNam.Text.Trim(),out int nam) && nam==tuNgay.Year;
            bool daTinh = kyDaTinh.HasValue && kyDaTinh.Value==tuNgay && dungKyDangChon;
            decimal cong = Convert.ToDecimal(hang["CongThucTe"]);
            bool congHopLe = cong>=0 && cong<=Convert.ToDecimal(hang["CongChuan"]);
            bool tongHopLe = Convert.ToDecimal(hang["TongLuong"])>=0;
            bool hopLe = caCho==0 && donCho==0 && congTrung==0 && hoaHongSai==0
                && khoanCongSai==0 && chungTuCho==0 && chinhSachDung
                && tongHopLe && congHopLe;
            string[] lyDo = new[]
            {
                caCho>0 ? $"{caCho} ca chưa đối soát của nhân viên này" : "",
                donCho>0 ? $"{donCho} đơn nghỉ chưa duyệt" : "",
                congTrung>0 ? $"{congTrung} ca trùng ngày nghỉ đã duyệt" : "",
                hoaHongSai>0 ? $"{hoaHongSai} khoản hoa hồng chưa hợp lệ" : "",
                khoanCongSai>0 ? $"{khoanCongSai} khoản cộng chưa phân loại" : "",
                chungTuCho>0 ? $"{chungTuCho} chứng từ chưa duyệt" : "",
                !chinhSachDung ? "Chính sách lương không hợp lệ" : "",
                !congHopLe ? "Công thực tế ngoài công chuẩn" : "",
                !tongHopLe ? "Thực nhận âm" : "",
                !daTinh ? "Nhấn \"Tính lương kỳ này\" sau khi chọn kỳ" : "",
                ngayMayChu.Date<ngayDuocChot ? $"SQL Server chưa đến ngày {ngayDuocChot:dd/MM/yyyy}" : ""
            };
            string canhBao = string.Join("; ", lyDo.Where(s => s != ""));
            lblDoiSoatChiTiet.Text = $"Ca chờ duyệt của nhân viên: {caCho}  •  Đơn nghỉ chờ: {donCho}  •  Trùng công/phép: {congTrung}\n" +
                $"Hoa hồng sai nguồn: {hoaHongSai}  •  Khoản cộng chưa phân loại: {khoanCongSai}  •  Chứng từ chờ duyệt: {chungTuCho}\n" +
                $"Chính sách: {(chinhSachDung?"Hợp lệ":"Chưa hợp lệ")}  •  Chốt từ: {ngayDuocChot:dd/MM/yyyy}  •  Ngày SQL Server: {ngayMayChu:dd/MM/yyyy}\n" +
                (canhBao=="" ? "Đã đủ điều kiện đối soát." : "Chưa thể chốt: " + canhBao);
            btnChotLuong.IsEnabled = PhienDangNhap.IsAdmin && hopLe && daTinh && ngayMayChu.Date>=ngayDuocChot;
            btnChotLuong.Content = btnChotLuong.IsEnabled ? "Chốt lương nhân viên này" :
                !daTinh ? "Cần tính lại kỳ lương" : ngayMayChu.Date<ngayDuocChot ?
                "Chưa đến ngày được chốt" : "Cần hoàn tất đối soát";
        }

        /// <summary>
        /// Xử lý thao tác nút xem chi tiết khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>hang[&quot;MaBL&quot;].ToString</c>, <c>hang[&quot;TenChucVu&quot;].ToString</c>, <c>hang[&quot;TrangThai&quot;].ToString</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnXemChiTiet_Click(object sender, RoutedEventArgs e)
        {
            if (gridBangLuong.SelectedItem is not DataRowView hang)
            {
                MessageBox.Show("Vui lòng chọn một nhân viên.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            lblMaBLPhieu.Text = hang["MaBL"].ToString() ?? "—";
            lblTenPhieu.Text = hang["HoTen"] + "  •  " + hang["MaNV"];
            lblChucVuPhieu.Text = hang["TenChucVu"].ToString() ?? "—";
            lblKyPhieu.Text = Convert.ToDateTime(hang["TuNgay"]).ToString("dd/MM/yyyy") + " – "
                + Convert.ToDateTime(hang["DenNgay"]).ToString("dd/MM/yyyy")
                + (Convert.ToInt32(hang["PhienBanTinhLuong"]) == 8 ? " • Công thức cũ" : "");
            lblTrangThaiPhieuDayDu.Text = hang["TrangThai"].ToString() ?? "—";
            lblCongPhieu.Text = Convert.ToDecimal(hang["CongThucTe"]).ToString("N2");
            lblCongChuanPhieu.Text = Convert.ToDecimal(hang["CongChuan"]).ToString("N2");
            lblNgayChotPhieu.Text = hang["NgayChot"] == DBNull.Value
                ? "Chưa chốt" : Convert.ToDateTime(hang["NgayChot"]).ToString("dd/MM/yyyy HH:mm");
            lblLuongCongPhieu.Text = Convert.ToDecimal(hang["LuongTheoCa"]).ToString("N0") + " đ";
            lblHoaHongPhieu.Text = Convert.ToDecimal(hang["TienHoaHong"]).ToString("N0") + " đ";
            lblPhuCapPhieu.Text = Convert.ToDecimal(hang["TienPhuCap"]).ToString("N0") + " đ";
            lblTangCaPhieu.Text = Convert.ToDecimal(hang["TienTangCa"]).ToString("N0") + " đ";
            lblBaoHiemPhieu.Text = Convert.ToDecimal(hang["TienBaoHiem"]).ToString("N0") + " đ";
            lblKhauTruPhieu.Text = Convert.ToDecimal(hang["TienKhauTruKhac"]).ToString("N0") + " đ";
            lblThucNhanPhieu.Text = Convert.ToDecimal(hang["TongLuong"]).ToString("N0") + " đ";
            lblGhiChuPhieu.Text = hang["GhiChu"] == DBNull.Value ? "" : hang["GhiChu"].ToString() ?? "";

            PhieuLuongOverlay.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Xử lý thao tác nút đóng phiếu lương khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnDongPhieuLuong_Click(object sender, RoutedEventArgs e)
        {
            PhieuLuongOverlay.Visibility = Visibility.Collapsed;
        }
    }
}
