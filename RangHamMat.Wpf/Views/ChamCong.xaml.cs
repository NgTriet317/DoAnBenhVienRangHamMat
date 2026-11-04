using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;

namespace RangHamMat.Views
{
    /// <summary>
    /// Phần xử lý chấm công của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public partial class ChamCong : UserControl
    {
        /// <summary>
        /// Đối tượng nghiệp vụ chấm công được màn hình dùng để đọc/ghi dữ liệu.
        /// </summary>
        private readonly BUS_CHAMCONG busChamCong = new BUS_CHAMCONG();
        /// <summary>
        /// Danh sách công dùng trong chấm công được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private DataTable danhSachCong = new DataTable();
        /// <summary>
        /// Lọc tìm kiểm được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string locTimKiem = "";
        /// <summary>
        /// Lọc trạng thái được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string locTrangThai = "";
        /// <summary>
        /// Cờ Đã tải; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool daTai;
        /// <summary>
        /// Cờ Đang tải; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool dangTai;

        /// <summary>
        /// Khởi tạo chấm công. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
        /// </summary>
        public ChamCong()
        {
            InitializeComponent();
            cboThang.SelectedIndex = DateTime.Today.Month - 1;
            txtNam.Text = DateTime.Today.Year.ToString();
            Loaded += (sender, e) => { if (!daTai) hienThiDuLieu(); };
        }

        /// <summary>
        /// Đưa các dòng chấm công đã nạp lên giao diện và đồng bộ thông tin chi tiết.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>int.TryParse</c>, <c>busChamCong.layChamCongTheoThang</c>, <c>danhSachCong.Rows.Count.ToString</c>, <c>danhSachCong.AsEnumerable</c>.</remarks>
        public void hienThiDuLieu()
        {
            if (dangTai) return;
            if (!PhienDangNhap.IsAdmin || string.IsNullOrWhiteSpace(PhienDangNhap.MaNV))
            {
                lblThongBao.Text = "Chỉ tài khoản BQL/Admin đã gắn nhân viên mới được duyệt chấm công.";
                return;
            }
            if (cboThang.SelectedIndex < 0 || !int.TryParse(txtNam.Text, out int nam)
                || nam < 2000 || nam > 2100)
            {
                lblThongBao.Text = "Vui lòng nhập kỳ chấm công hợp lệ.";
                return;
            }
            dangTai = true;
            try
            {
                danhSachCong = busChamCong.layChamCongTheoThang(cboThang.SelectedIndex + 1, nam);
                danhSachCong.DefaultView.Sort = "NgayLam ASC, MaCa ASC, HoTen ASC";
                // Bỏ bảng cũ để DataGrid đo lại đúng chiều rộng cột khi đổi kỳ.
                gridChamCong.SelectedItem = null;
                gridChamCong.ItemsSource = null;
                gridChamCong.ItemsSource = danhSachCong.DefaultView;
                lblTongCa.Text = danhSachCong.Rows.Count.ToString("N0");
                lblChoDuyet.Text = danhSachCong.AsEnumerable().Count(r => r.Field<string>("TrangThai") == "Chờ đối soát").ToString("N0");
                lblDaDuyet.Text = danhSachCong.AsEnumerable().Count(r => r.Field<string>("TrangThai") == "Đã xác nhận").ToString("N0");
                locDuLieu();
                if (danhSachCong.DefaultView.Count > 0) gridChamCong.SelectedIndex = 0;
                else xoaChiTiet();
                daTai = true;
                lblThongBao.Text = "BQL có thể duyệt ca ngắn hơn 8 giờ, mỗi ca hoàn tất tính 0,5 công; không tính lại bảng lương đã lưu.";
            }
            catch (Exception ex)
            {
                daTai = false;
                gridChamCong.ItemsSource = null;
                danhSachCong = new DataTable();
                xoaChiTiet();
                lblThongBao.Text = "Không thể tải dữ liệu chấm công: " + ex.Message;
                MessageBox.Show(lblThongBao.Text, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { dangTai = false; }
        }

        /// <summary>
        /// Lọc dữ liệu đã nạp theo từ khóa và các lựa chọn kỳ/trạng thái của màn hình.
        /// </summary>
        private void locDuLieu()
        {
            if (danhSachCong.Columns.Count == 0) return;
            string dieuKien = locTimKiem;
            if (locTrangThai != "") dieuKien += (dieuKien == "" ? "" : " AND ") + locTrangThai;
            danhSachCong.DefaultView.RowFilter = dieuKien;
            lblSoDong.Text = $"{danhSachCong.DefaultView.Count:N0} ca đang hiển thị";
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
            locTimKiem = noiDung.Trim() == "" ? "" :
                $"(MaNV LIKE '%{noiDung.Trim()}%' OR HoTen LIKE '%{noiDung.Trim()}%')";
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
            locTrangThai = trangThai == "Tất cả trạng thái" || trangThai == "" ? "" : $"TrangThai = '{trangThai}'";
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
            if (daTai && !dangTai) hienThiDuLieu();
        }

        /// <summary>
        /// Tiếp nhận thay đổi ô nhập năm và cập nhật trạng thái/dữ liệu phụ thuộc.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>int.TryParse</c>, <c>hienThiDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void txtNam_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (daTai && !dangTai && txtNam.Text.Length == 4 && int.TryParse(txtNam.Text, out _)) hienThiDuLieu();
        }

        /// <summary>
        /// Xử lý thao tác nút tải lại khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>hienThiDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnTaiLai_Click(object sender, RoutedEventArgs e) => hienThiDuLieu();

        /// <summary>
        /// Phản ánh lựa chọn bảng hiển thị chấm công vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>xoaChiTiet</c>, <c>vao.ToString</c>, <c>ra.ToString</c>, <c>hang[&quot;TrangThai&quot;].ToString</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void gridChamCong_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridChamCong.SelectedItem is not DataRowView hang) { xoaChiTiet(); return; }
            lblNhanVien.Text = hang["HoTen"] + "  ·  " + hang["MaNV"];
            DateTime ngay = Convert.ToDateTime(hang["NgayLam"]);
            lblThongTinCa.Text = $"{ngay:dd/MM/yyyy}  •  {hang["MaCa"]}  •  {hang["TenChucVu"]}  •  {hang["TrangThai"]}";
            lblKhungGio.Text = $"{(TimeSpan)hang["GioBatDau"]:hh\\:mm} — {(TimeSpan)hang["GioKetThuc"]:hh\\:mm}";
            txtGioVao.Text = hang["GioVao"] is DateTime vao ? vao.ToString("HH:mm") : "";
            txtGioRa.Text = hang["GioRa"] is DateTime ra ? ra.ToString("HH:mm") : "";
            txtLyDo.Text = "";
            cboKetQua.SelectedIndex = 0;
            if (hang["GioVao"] == DBNull.Value || hang["GioRa"] == DBNull.Value)
                cboKetQua.SelectedIndex = 1;
            if (hang["TrangThai"].ToString() == "Không hợp lệ") cboKetQua.SelectedIndex = 2;
            lblLichSu.Text = hang["NgayDoiSoat"] is DateTime duyet
                ? $"Đối soát gần nhất: {duyet:dd/MM/yyyy HH:mm} · {hang["MaNVDoiSoat"]}"
                : "Chưa có lần đối soát";
            bool khoa = Convert.ToBoolean(hang["DaChot"]);
            bool nghi = Convert.ToBoolean(hang["CoDonNghi"]);
            string trangThaiLich = hang["TrangThaiLich"].ToString() ?? "";
            bool huy = new[] { "Hủy", "Từ chối", "Nghỉ phép", "Nghỉ việc" }.Contains(trangThaiLich);
            bool quaSom = ngay.Date > DateTime.Today;
            lblCanhBao.Text = khoa ? "Kỳ lương đã chốt: không thể sửa công." :
                nghi ? "Ngày này có đơn nghỉ đã duyệt: không được tính trùng công." :
                huy ? "Lịch này đã bị hủy hoặc nghỉ phép: không được duyệt công." :
                quaSom ? "Không được duyệt ca làm việc trong tương lai." :
                "Ca hoàn tất được 0,5 công, không yêu cầu đủ 8 giờ; đi trễ 16–60 phút vẫn trừ 0,25 công khi tổng hợp lương.";
            bool duocDuyet = !khoa && !nghi && !huy && !quaSom;
            btnDuyetMot.IsEnabled = duocDuyet;
            txtGioVao.IsEnabled = duocDuyet && cboKetQua.SelectedIndex == 0;
            txtGioRa.IsEnabled = duocDuyet && cboKetQua.SelectedIndex == 0;
            cboKetQua.IsEnabled = duocDuyet;
            txtLyDo.IsEnabled = duocDuyet;
            btnDuyetNhieu.IsEnabled = duocDuyet && gridChamCong.SelectedItems.Count > 1;
        }

        /// <summary>
        /// Phản ánh lựa chọn hộp chọn kết quả vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void cboKetQua_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtGioVao == null || btnDuyetMot == null) return;
            bool hoanTat = cboKetQua.SelectedIndex == 0 && btnDuyetMot.IsEnabled;
            txtGioVao.IsEnabled = hoanTat;
            txtGioRa.IsEnabled = hoanTat;
        }

        /// <summary>
        /// Xóa nội dung chi tiết và lựa chọn đang xem trên màn hình.
        /// </summary>
        private void xoaChiTiet()
        {
            lblNhanVien.Text = "Chọn một ca làm việc";
            lblThongTinCa.Text = "—";
            lblKhungGio.Text = "—";
            txtGioVao.Text = txtGioRa.Text = txtLyDo.Text = "";
            lblCanhBao.Text = "";
            lblLichSu.Text = "";
            btnDuyetMot.IsEnabled = false;
            btnDuyetNhieu.IsEnabled = false;
        }

        /// <summary>
        /// Xử lý thao tác nút duyệt một khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>.ToString</c>, <c>TimeSpan.TryParseExact</c>, <c>txtGioVao.Text.Trim</c>, <c>txtGioRa.Text.Trim</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnDuyetMot_Click(object sender, RoutedEventArgs e)
        {
            if (!PhienDangNhap.IsAdmin || gridChamCong.SelectedItem is not DataRowView hang) return;
            try
            {
                string ketQua = (cboKetQua.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                DateTime ngay = Convert.ToDateTime(hang["NgayLam"]);
                DateTime? vao = null, ra = null;
                if (ketQua == "Hoàn tất")
                {
                    if (!TimeSpan.TryParseExact(txtGioVao.Text.Trim(), @"hh\:mm", null, out TimeSpan gioVao) ||
                        !TimeSpan.TryParseExact(txtGioRa.Text.Trim(), @"hh\:mm", null, out TimeSpan gioRa))
                        throw new ArgumentException("Nhập giờ theo định dạng HH:mm, ví dụ 06:00 và 14:00.");
                    vao = ngay.Date + gioVao;
                    ra = ngay.Date + gioRa;
                    if (ra < vao) ra = ra.Value.AddDays(1); // Ca qua đêm.
                }
                var ca = new ET_CHAMCONG
                {
                    MaLLV = hang["MaLLV"].ToString() ?? "",
                    TrangThaiCu = hang["TrangThai"].ToString() ?? "",
                    GioVaoCu = hang.Row.Field<DateTime?>("GioVao"),
                    GioRaCu = hang.Row.Field<DateTime?>("GioRa"),
                    GioVao = vao,
                    GioRa = ra,
                    KetQua = ketQua,
                    LyDo = txtLyDo.Text.Trim()
                };
                if (MessageBox.Show("BQL xác nhận đối soát ca " + ca.MaLLV + "?", "Duyệt chấm công",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                busChamCong.duyetChamCong(new[] { ca }, PhienDangNhap.MaNV);
                hienThiDuLieu();
                MessageBox.Show("Đã lưu kết quả duyệt ca. Dữ liệu đã được ghi nhật ký.", "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể duyệt ca: " + ex.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Xử lý thao tác nút duyệt nhiều khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>gridChamCong.SelectedItems.OfType&lt;DataRowView&gt;</c>, <c>ds.Add</c>, <c>hang[&quot;MaLLV&quot;].ToString</c>, <c>hang[&quot;TrangThai&quot;].ToString</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnDuyetNhieu_Click(object sender, RoutedEventArgs e)
        {
            if (!PhienDangNhap.IsAdmin) return;
            var ds = new List<ET_CHAMCONG>();
            try
            {
                foreach (DataRowView hang in gridChamCong.SelectedItems.OfType<DataRowView>())
                {
                    // Chỉ duyệt nhóm khi đã có giờ vào/ra; không kiểm tra đủ 8 giờ.
                    if (hang["GioVao"] is not DateTime vao || hang["GioRa"] is not DateTime ra)
                        throw new ArgumentException("Ca " + hang["MaLLV"] + " chưa đủ giờ vào/ra; hãy duyệt riêng.");
                    ds.Add(new ET_CHAMCONG
                    {
                        MaLLV = hang["MaLLV"].ToString() ?? "",
                        TrangThaiCu = hang["TrangThai"].ToString() ?? "",
                        GioVaoCu = vao,
                        GioRaCu = ra,
                        GioVao = vao,
                        GioRa = ra,
                        KetQua = "Hoàn tất",
                        LyDo = "BQL đối soát hàng loạt theo giờ vào/ra đã ghi nhận"
                    });
                }
                if (ds.Count < 2) { MessageBox.Show("Chọn từ hai ca trở lên để duyệt hàng loạt."); return; }
                if (MessageBox.Show($"Duyệt {ds.Count} ca? Nếu một ca sai, sẽ không lưu ca nào.", "Xác nhận",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                int soCa = busChamCong.duyetChamCong(ds, PhienDangNhap.MaNV);
                hienThiDuLieu();
                MessageBox.Show($"Đã đối soát {soCa} ca.", "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể duyệt hàng loạt: " + ex.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
