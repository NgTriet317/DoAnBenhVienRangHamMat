using BUS;
using ET;
using RangHamMat.Controls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace RangHamMat.Views
{
    /// <summary>
    /// Phần xử lý danh mục hàng hóa của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public partial class DanhMucHangHoa : UserControl
    {
        /// <summary>
        /// Đối tượng nghiệp vụ hàng hóa được màn hình dùng để đọc/ghi dữ liệu.
        /// </summary>
        private readonly BUS_HANGHOA busHangHoa = new BUS_HANGHOA();
        /// <summary>
        /// Đối tượng nghiệp vụ loại hàng hóa được màn hình dùng để đọc/ghi dữ liệu.
        /// </summary>
        private readonly BUS_LOAIHANGHOA busLoaiHangHoa = new BUS_LOAIHANGHOA();

        /// <summary>
        /// Danh sách hàng hóa dùng trong danh mục hàng hóa được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private DataTable danhSachHangHoa = new DataTable();
        /// <summary>
        /// Danh sách loại dùng trong danh mục hàng hóa được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private List<ET_LOAIHANGHOA> danhSachLoai = new List<ET_LOAIHANGHOA>();
        /// <summary>
        /// Hàng hóa đang chọn được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private ET_HANGHOA? hangHoaDangChon;
        /// <summary>
        /// Bản dữ liệu gốc được giữ để đối chiếu trước khi cập nhật hoặc khôi phục nội dung đang sửa.
        /// </summary>
        private ET_HANGHOA? banGoc;
        /// <summary>
        /// Lọc tìm kiểm được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string locTimKiem = "";
        /// <summary>
        /// Lọc loại được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string locLoai = "";
        /// <summary>
        /// Lọc trạng thái được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string locTrangThai = "";
        /// <summary>
        /// Cờ Đang thêm; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool dangThem;
        /// <summary>
        /// Cờ Đang sửa; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool dangSua;
        /// <summary>
        /// Cờ Đã tải dữ liệu; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool daTaiDuLieu;

        /// <summary>
        /// Khởi tạo danh mục hàng hóa. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
        /// </summary>
        public DanhMucHangHoa()
        {
            InitializeComponent();
            Loaded += (sender, e) => { if (!daTaiDuLieu) LoadDuLieu(); };
        }

        // Phân nhóm từ tên/mã loại trong danh mục hiện tại.
        private string NhomCuaMaLoai(string maLoai)
        {
            foreach (ET_LOAIHANGHOA loai in danhSachLoai)
            {
                if (loai.MaLoaiHH != maLoai) continue;
                string ten = (loai.TenLoaiHH ?? "").Trim();
                if (ten.StartsWith("Thuốc", StringComparison.CurrentCultureIgnoreCase)) return "Thuoc";
                if (ten.StartsWith("Thiết bị", StringComparison.CurrentCultureIgnoreCase)) return "ThietBi";
                if (ten.StartsWith("Vật tư", StringComparison.CurrentCultureIgnoreCase)) return "VatTu";
                if (maLoai == "LHH001" || maLoai == "LHH002" || maLoai == "LHH003") return "Thuoc";
                if (maLoai == "LHH008" || maLoai == "LHH009" || maLoai == "LHH010") return "ThietBi";
                if (maLoai == "LHH004" || maLoai == "LHH005" || maLoai == "LHH006" || maLoai == "LHH007") return "VatTu";
            }
            return "";
        }

        /// <summary>
        /// Đọc nhóm hàng của lựa chọn hiện tại để áp dụng danh mục và quy tắc tương ứng.
        /// </summary>
        /// <returns>Chuỗi kết quả của thao tác lấy nhóm đang chọn.</returns>
        private string LayNhomDangChon()
        {
            return (tabNhomHangHoa.SelectedItem as TabItem)?.Tag as string ?? "Thuoc";
        }

        /// <summary>
        /// Nạp danh mục tương ứng tab đang chọn và đặt lại các lựa chọn phụ thuộc.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>NhomCuaMaLoai</c>, <c>LayNhomDangChon</c>, <c>loaiLoc.Add</c>, <c>loaiNhap.Add</c>.</remarks>
        private void CapNhatDanhMucTheoTab()
        {
            bool daTai = daTaiDuLieu;
            daTaiDuLieu = false;
            var loaiLoc = new List<LuaChonDanhMuc> { new LuaChonDanhMuc("", "Tất cả loại hàng") };
            var loaiNhap = new List<LuaChonDanhMuc>();
            foreach (ET_LOAIHANGHOA loai in danhSachLoai)
            {
                if (NhomCuaMaLoai(loai.MaLoaiHH) == LayNhomDangChon())
                {
                    loaiLoc.Add(new LuaChonDanhMuc(loai.MaLoaiHH, loai.TenLoaiHH));
                    loaiNhap.Add(new LuaChonDanhMuc(loai.MaLoaiHH, loai.TenLoaiHH));
                }
            }
            cboLoaiLoc.ItemsSource = loaiLoc;
            cboLoaiLoc.SelectedIndex = 0;
            cboLoaiHangHoa.ItemsSource = loaiNhap;
            cboLoaiHangHoa.SelectedIndex = -1;
            locLoai = "";
            daTaiDuLieu = daTai;
        }

        /// <summary>
        /// Cập nhật thống kê theo nhóm danh mục của tab hiện tại.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>LayNhomDangChon</c>, <c>Array.Empty&lt;DataRow&gt;</c>, <c>danhSachHangHoa.Select</c>, <c>hangTheoTab.Length.ToString</c>.</remarks>
        private void CapNhatThongKeTheoTab()
        {
            string nhom = LayNhomDangChon();
            DataRow[] hangTheoTab = danhSachHangHoa.Columns.Count == 0
                ? Array.Empty<DataRow>() : danhSachHangHoa.Select($"NhomHang = '{nhom}'");
            lblTongMatHang.Text = hangTheoTab.Length.ToString();
            lblTongLoai.Text = danhSachLoai.Count(x => NhomCuaMaLoai(x.MaLoaiHH) == nhom).ToString();
            lblLienKetThietBi.Text = hangTheoTab.Count(x => x.Field<bool>("DangHoatDong")).ToString();
            pnlThongTinTon.Visibility = nhom == "ThietBi" ? Visibility.Collapsed : Visibility.Visible;
            pnlThongTinThuoc.Visibility = nhom == "Thuoc" ? Visibility.Visible : Visibility.Collapsed;

            if (nhom == "ThietBi")
            {
                lblTenTab.Text = "Danh sách thiết bị";
                lblGhiChuTab.Text = "Số serial, phòng sử dụng và bảo trì được quản lý tại phân hệ Thiết bị.";
                btnThem.Content = "＋  Thêm thiết bị";
            }
            else if (nhom == "VatTu")
            {
                lblTenTab.Text = "Danh sách vật tư";
                lblGhiChuTab.Text = "Mức tồn tối thiểu được lưu tại đây; số lượng thực tế ở Tồn kho.";
                btnThem.Content = "＋  Thêm vật tư";
            }
            else
            {
                lblTenTab.Text = "Danh sách thuốc";
                lblGhiChuTab.Text = "Hàm lượng, giá bán, mức tồn tối thiểu được lưu tại đây; lô và hạn dùng ở Tồn kho.";
                btnThem.Content = "＋  Thêm thuốc";
            }
        }

        /// <summary>
        /// Nạp dữ liệu từ các lớp nghiệp vụ, chuẩn bị danh sách lựa chọn và đồng bộ vùng hiển thị của màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>busHangHoa.layDanhMucChiTiet</c>, <c>busLoaiHangHoa.layToanBoLoaiHangHoa</c>, <c>CapNhatDanhMucTheoTab</c>, <c>hang[&quot;TrangThai&quot;].ToString</c>.</remarks>
        /// <param name="maCanChon">Mã cần chọn xác định đối tượng cần xử lý.</param>
        private void LoadDuLieu(string? maCanChon = null)
        {
            try
            {
                daTaiDuLieu = false;
                danhSachHangHoa = busHangHoa.layDanhMucChiTiet();
                danhSachHangHoa.CaseSensitive = false;
                danhSachLoai = busLoaiHangHoa.layToanBoLoaiHangHoa().ToList();
                CapNhatDanhMucTheoTab();

                var trangThai = new List<string> { "Đang hoạt động", "Ngừng hoạt động" };
                var donViTinh = new List<string> { "Cái", "Bộ", "Viên", "Chai", "Gói", "Hộp" };
                foreach (DataRow hang in danhSachHangHoa.Rows)
                {
                    string tinhTrang = hang["TrangThai"].ToString() ?? "";
                    string donVi = hang["DonViTinh"].ToString() ?? "";
                    if (tinhTrang != "" && !trangThai.Contains(tinhTrang)) trangThai.Add(tinhTrang);
                    if (donVi != "" && !donViTinh.Contains(donVi)) donViTinh.Add(donVi);
                }
                cboTrangThai.ItemsSource = trangThai;
                cboTrangThaiLoc.ItemsSource = new[] { "Tất cả trạng thái" }.Concat(trangThai).ToList();
                cboTrangThaiLoc.SelectedIndex = 0;
                locTrangThai = "";
                cboDonViTinh.ItemsSource = donViTinh;
                CapNhatThongKeTheoTab();

                // Giữ tìm kiếm khi tải lại; các điều kiện vẫn được xử lý bởi sự kiện của control.
                string dieuKien = $"NhomHang = '{LayNhomDangChon()}'";
                if (locTimKiem != "") dieuKien += " AND " + locTimKiem;
                if (locLoai != "") dieuKien += " AND " + locLoai;
                if (locTrangThai != "") dieuKien += " AND " + locTrangThai;
                danhSachHangHoa.DefaultView.RowFilter = dieuKien;
                danhSachHangHoa.DefaultView.Sort = "TenHH ASC";
                Grid_HANG_HOA.ItemsSource = danhSachHangHoa.DefaultView;
                daTaiDuLieu = true;
                lblDangHien.Text = danhSachHangHoa.DefaultView.Count + " mặt hàng";
                lblTrong.Visibility = danhSachHangHoa.DefaultView.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                Grid_HANG_HOA.SelectedItem = null;
                foreach (DataRowView hang in danhSachHangHoa.DefaultView)
                {
                    if (hang["MaHH"].ToString() == maCanChon)
                    {
                        Grid_HANG_HOA.SelectedItem = hang;
                        break;
                    }
                }
                if (Grid_HANG_HOA.SelectedItem == null && danhSachHangHoa.DefaultView.Count > 0)
                    Grid_HANG_HOA.SelectedIndex = 0;
                if (Grid_HANG_HOA.SelectedItem == null) HienTrangThaiTrong();
                lblThongBao.Text = "";
            }
            catch (Exception ex)
            {
                daTaiDuLieu = false;
                Grid_HANG_HOA.ItemsSource = null;
                lblThongBao.Text = "Không thể tải danh mục: " + ex.Message;
                MessageBox.Show(lblThongBao.Text, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Phản ánh lựa chọn nhóm hàng hóa vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ReferenceEquals</c>, <c>CapNhatDanhMucTheoTab</c>, <c>CapNhatThongKeTheoTab</c>, <c>LayNhomDangChon</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void NhomHangHoa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!daTaiDuLieu || !ReferenceEquals(e.Source, sender) || dangThem || dangSua) return;
            CapNhatDanhMucTheoTab();
            CapNhatThongKeTheoTab();
            if (danhSachHangHoa.Columns.Count == 0) return;
            string dieuKien = $"NhomHang = '{LayNhomDangChon()}'";
            if (locTimKiem != "") dieuKien += " AND " + locTimKiem;
            if (locLoai != "") dieuKien += " AND " + locLoai;
            if (locTrangThai != "") dieuKien += " AND " + locTrangThai;
            danhSachHangHoa.DefaultView.RowFilter = dieuKien;
            lblDangHien.Text = danhSachHangHoa.DefaultView.Count + " mặt hàng";
            lblTrong.Visibility = danhSachHangHoa.DefaultView.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (danhSachHangHoa.DefaultView.Count > 0) Grid_HANG_HOA.SelectedIndex = 0;
            else HienTrangThaiTrong();
        }

        /// <summary>
        /// Tiếp nhận thay đổi ô nhập tìm kiểm và cập nhật trạng thái/dữ liệu phụ thuộc.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>Regex.Replace</c>, <c>noiDung.Trim</c>, <c>LayNhomDangChon</c>.</remarks>
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
            locTimKiem = timKiem == "" ? "" :
                $"(MaHH LIKE '%{timKiem}%' OR TenHH LIKE '%{timKiem}%' OR TenLoai LIKE '%{timKiem}%')";
            if (!daTaiDuLieu || danhSachHangHoa.Columns.Count == 0) return;
            string dieuKien = $"NhomHang = '{LayNhomDangChon()}'";
            if (locTimKiem != "") dieuKien += " AND " + locTimKiem;
            if (locLoai != "") dieuKien += " AND " + locLoai;
            if (locTrangThai != "") dieuKien += " AND " + locTrangThai;
            danhSachHangHoa.DefaultView.RowFilter = dieuKien;
            lblDangHien.Text = danhSachHangHoa.DefaultView.Count + " mặt hàng";
            lblTrong.Visibility = danhSachHangHoa.DefaultView.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (Grid_HANG_HOA.SelectedItem == null && danhSachHangHoa.DefaultView.Count > 0) Grid_HANG_HOA.SelectedIndex = 0;
        }

        /// <summary>
        /// Phản ánh lựa chọn hộp chọn loại lọc vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>LayNhomDangChon</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void cboLoaiLoc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string maLoai = cboLoaiLoc.SelectedValue as string ?? "";
            locLoai = maLoai == "" ? "" : $"MaLoaiHH = '{maLoai}'";
            if (!daTaiDuLieu || danhSachHangHoa.Columns.Count == 0) return;
            string dieuKien = $"NhomHang = '{LayNhomDangChon()}'";
            if (locTimKiem != "") dieuKien += " AND " + locTimKiem;
            if (locLoai != "") dieuKien += " AND " + locLoai;
            if (locTrangThai != "") dieuKien += " AND " + locTrangThai;
            danhSachHangHoa.DefaultView.RowFilter = dieuKien;
            lblDangHien.Text = danhSachHangHoa.DefaultView.Count + " mặt hàng";
            lblTrong.Visibility = danhSachHangHoa.DefaultView.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (Grid_HANG_HOA.SelectedItem == null && danhSachHangHoa.DefaultView.Count > 0) Grid_HANG_HOA.SelectedIndex = 0;
        }

        /// <summary>
        /// Phản ánh lựa chọn hộp chọn trạng thái lọc vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>trangThai.Replace</c>, <c>LayNhomDangChon</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void cboTrangThaiLoc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string trangThai = cboTrangThaiLoc.SelectedItem as string ?? "";
            locTrangThai = trangThai == "" || trangThai == "Tất cả trạng thái" ? "" :
                $"TrangThai = '{trangThai.Replace("'", "''")}'";
            if (!daTaiDuLieu || danhSachHangHoa.Columns.Count == 0) return;
            string dieuKien = $"NhomHang = '{LayNhomDangChon()}'";
            if (locTimKiem != "") dieuKien += " AND " + locTimKiem;
            if (locLoai != "") dieuKien += " AND " + locLoai;
            if (locTrangThai != "") dieuKien += " AND " + locTrangThai;
            danhSachHangHoa.DefaultView.RowFilter = dieuKien;
            lblDangHien.Text = danhSachHangHoa.DefaultView.Count + " mặt hàng";
            lblTrong.Visibility = danhSachHangHoa.DefaultView.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (Grid_HANG_HOA.SelectedItem == null && danhSachHangHoa.DefaultView.Count > 0) Grid_HANG_HOA.SelectedIndex = 0;
        }

        /// <summary>
        /// Xử lý thao tác xóa lọc khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>txtTimKiem.Clear</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void XoaLoc_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem.Clear();
            cboLoaiLoc.SelectedIndex = 0;
            cboTrangThaiLoc.SelectedIndex = 0;
        }

        /// <summary>
        /// Phản ánh lựa chọn danh sách vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>HienChiTiet</c>, <c>HienTrangThaiTrong</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void DanhSach_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!daTaiDuLieu || dangThem || dangSua) return;
            if (Grid_HANG_HOA.SelectedItem is DataRowView hang) HienChiTiet(hang);
            else HienTrangThaiTrong();
        }

        /// <summary>
        /// Đưa dữ liệu mục đang chọn vào vùng chi tiết, đồng thời cập nhật các nút thao tác liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>hang[&quot;MaHH&quot;].ToString</c>, <c>hang[&quot;TenHH&quot;].ToString</c>, <c>hang[&quot;MaLoaiHH&quot;].ToString</c>, <c>hang[&quot;DonViTinh&quot;].ToString</c>.</remarks>
        /// <param name="hang">Giá trị hàng dùng khi thực hiện thao tác; kiểu DataRowView.</param>
        private void HienChiTiet(DataRowView hang)
        {
            hangHoaDangChon = new ET_HANGHOA(
                hang["MaHH"].ToString() ?? "", hang["TenHH"].ToString() ?? "",
                hang["MaLoaiHH"].ToString() ?? "", hang["DonViTinh"].ToString() ?? "",
                hang["MoTa"].ToString() ?? "", hang["TrangThai"].ToString() ?? "");
            banGoc = new ET_HANGHOA(hangHoaDangChon.MaHH, hangHoaDangChon.TenHH,
                hangHoaDangChon.MaLoaiHH, hangHoaDangChon.DonViTinh,
                hangHoaDangChon.MoTa, hangHoaDangChon.TrangThai);
            dangThem = false;
            dangSua = false;
            lblCheDo.Text = "CHI TIẾT HÀNG HÓA";
            lblTenDangChon.Text = hangHoaDangChon.TenHH;
            lblMaDangChon.Text = "Mã mặt hàng: " + hangHoaDangChon.MaHH;
            txtMaHH.Text = hangHoaDangChon.MaHH;
            txtTenHH.Text = hangHoaDangChon.TenHH;
            cboLoaiHangHoa.SelectedValue = hangHoaDangChon.MaLoaiHH;
            cboDonViTinh.Text = hangHoaDangChon.DonViTinh;
            cboTrangThai.Text = hangHoaDangChon.TrangThai;
            txtMoTa.Text = hangHoaDangChon.MoTa;
            txtMucTon.Text = Convert.ToDecimal(hang["MucTonToiThieu"]).ToString("0.###", CultureInfo.CurrentCulture);
            txtHamLuong.Text = hang["HamLuong"] == DBNull.Value ? "" : hang["HamLuong"].ToString() ?? "";
            txtGiaBan.Text = Convert.ToDecimal(hang["DonGiaBan"]).ToString("0.##", CultureInfo.CurrentCulture);
            lblThongBao.Text = "";
            DoiCheDo(false);
        }

        /// <summary>
        /// Đặt vùng chi tiết về trạng thái chưa có bản ghi được chọn.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>XoaForm</c>, <c>DoiCheDo</c>.</remarks>
        private void HienTrangThaiTrong()
        {
            hangHoaDangChon = null;
            banGoc = null;
            dangThem = false;
            dangSua = false;
            lblCheDo.Text = "CHI TIẾT HÀNG HÓA";
            lblTenDangChon.Text = "Chưa chọn mặt hàng";
            lblMaDangChon.Text = "Chọn một mặt hàng bên trái hoặc thêm mới.";
            XoaForm();
            DoiCheDo(false);
        }

        /// <summary>
        /// Xóa nội dung biểu mẫu và đặt lại các lựa chọn hoặc trạng thái đang chỉnh.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>txtTenHH.Clear</c>, <c>txtMoTa.Clear</c>, <c>txtHamLuong.Clear</c>.</remarks>
        private void XoaForm()
        {
            txtMaHH.Text = "Tự sinh khi lưu";
            txtTenHH.Clear();
            cboLoaiHangHoa.SelectedIndex = -1;
            cboDonViTinh.Text = "";
            cboTrangThai.Text = "Đang hoạt động";
            txtMoTa.Clear();
            txtMucTon.Text = "0";
            txtHamLuong.Clear();
            txtGiaBan.Text = "0";
            lblThongBao.Text = "";
        }

        /// <summary>
        /// Chuyển chế độ xem/thêm/sửa và cập nhật các trường, nút theo chế độ được chọn.
        /// </summary>
        /// <param name="choPhepNhap">Giá trị chờ phép nhập dùng khi thực hiện thao tác; kiểu bool.</param>
        private void DoiCheDo(bool choPhepNhap)
        {
            Grid_HANG_HOA.IsEnabled = !choPhepNhap;
            txtTimKiem.IsEnabled = !choPhepNhap;
            cboLoaiLoc.IsEnabled = !choPhepNhap;
            cboTrangThaiLoc.IsEnabled = !choPhepNhap;
            btnThem.IsEnabled = !choPhepNhap;
            btnTaiLai.IsEnabled = !choPhepNhap;
            btnXoaLoc.IsEnabled = !choPhepNhap;
            tabNhomHangHoa.IsEnabled = !choPhepNhap;
            txtTenHH.IsReadOnly = !choPhepNhap;
            cboLoaiHangHoa.IsEnabled = choPhepNhap;
            cboDonViTinh.IsEnabled = choPhepNhap;
            cboTrangThai.IsEnabled = choPhepNhap;
            txtMoTa.IsReadOnly = !choPhepNhap;
            txtMucTon.IsReadOnly = !choPhepNhap;
            txtHamLuong.IsReadOnly = !choPhepNhap;
            txtGiaBan.IsReadOnly = !choPhepNhap;
            btnSua.Visibility = Visibility.Collapsed;
            btnXoa.Visibility = Visibility.Collapsed;
            btnLuu.Visibility = choPhepNhap ? Visibility.Visible : Visibility.Collapsed;
            btnHuy.Visibility = choPhepNhap ? Visibility.Visible : Visibility.Collapsed;
            if (!choPhepNhap && hangHoaDangChon != null)
            {
                btnSua.Visibility = Visibility.Visible;
                btnXoa.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Xử lý thao tác thêm mới khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>XoaForm</c>, <c>DoiCheDo</c>, <c>txtTenHH.Focus</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void ThemMoi_Click(object sender, RoutedEventArgs e)
        {
            hangHoaDangChon = null;
            banGoc = null;
            dangThem = true;
            dangSua = false;
            Grid_HANG_HOA.SelectedItem = null;
            XoaForm();
            lblCheDo.Text = "THÊM HÀNG HÓA";
            lblTenDangChon.Text = "Mặt hàng mới";
            lblMaDangChon.Text = "Mã hàng hóa sẽ tự sinh khi lưu";
            DoiCheDo(true);
            txtTenHH.Focus();
        }

        /// <summary>
        /// Xử lý thao tác sửa khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>DoiCheDo</c>, <c>txtTenHH.Focus</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void Sua_Click(object sender, RoutedEventArgs e)
        {
            if (hangHoaDangChon == null) return;
            dangSua = true;
            dangThem = false;
            lblCheDo.Text = "CHỈNH SỬA HÀNG HÓA";
            DoiCheDo(true);
            txtTenHH.Focus();
        }

        /// <summary>
        /// Xử lý thao tác hủy khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>LoadDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void Huy_Click(object sender, RoutedEventArgs e)
        {
            dangThem = false;
            dangSua = false;
            string maCanChon = banGoc?.MaHH ?? "";
            LoadDuLieu(maCanChon);
        }

        /// <summary>
        /// Xử lý thao tác lưu khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>txtTenHH.Text.Trim</c>, <c>cboDonViTinh.Text.Trim</c>, <c>cboTrangThai.Text.Trim</c>, <c>txtMoTa.Text.Trim</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void Luu_Click(object sender, RoutedEventArgs e)
        {
            if (!dangThem && !dangSua) return;
            string ten = txtTenHH.Text.Trim();
            string maLoai = cboLoaiHangHoa.SelectedValue as string ?? "";
            string donVi = cboDonViTinh.Text.Trim();
            string trangThai = cboTrangThai.Text.Trim();
            string moTa = txtMoTa.Text.Trim();
            string nhom = LayNhomDangChon();
            lblThongBao.Text = "";

            if (ten.Length == 0 || ten.Length > 255)
            {
                lblThongBao.Text = "Tên hàng hóa phải có từ 1 đến 255 ký tự.";
                txtTenHH.Focus();
                return;
            }
            if (maLoai == "" || NhomCuaMaLoai(maLoai) != nhom)
            {
                lblThongBao.Text = "Vui lòng chọn loại hàng hóa thuộc tab hiện tại.";
                cboLoaiHangHoa.Focus();
                return;
            }
            if (donVi.Length == 0 || donVi.Length > 255)
            {
                lblThongBao.Text = "Đơn vị tính phải có từ 1 đến 255 ký tự.";
                cboDonViTinh.Focus();
                return;
            }
            if (trangThai.Length == 0 || trangThai.Length > 50)
            {
                lblThongBao.Text = "Trạng thái phải có từ 1 đến 50 ký tự.";
                cboTrangThai.Focus();
                return;
            }

            decimal mucTon = 0;
            decimal giaBan = 0;
            string hamLuong = txtHamLuong.Text.Trim();
            if (nhom != "ThietBi" && (!decimal.TryParse(txtMucTon.Text.Trim(), NumberStyles.Number,
                CultureInfo.CurrentCulture, out mucTon) || mucTon < 0 || decimal.Round(mucTon, 3) != mucTon))
            {
                lblThongBao.Text = "Mức tồn tối thiểu phải là số không âm (tối đa 3 chữ số thập phân).";
                txtMucTon.Focus();
                return;
            }
            if (nhom == "Thuoc")
            {
                if (hamLuong.Length == 0 || hamLuong.Length > 255)
                {
                    lblThongBao.Text = "Vui lòng nhập hàm lượng thuốc (tối đa 255 ký tự).";
                    txtHamLuong.Focus();
                    return;
                }
                if (!decimal.TryParse(txtGiaBan.Text.Trim(), NumberStyles.Number,
                    CultureInfo.CurrentCulture, out giaBan) || giaBan < 0 || decimal.Round(giaBan, 2) != giaBan)
                {
                    lblThongBao.Text = "Giá bán phải là số không âm (tối đa 2 chữ số thập phân).";
                    txtGiaBan.Focus();
                    return;
                }
            }
            var duLieu = new ET_HANGHOA(dangThem ? "" : hangHoaDangChon!.MaHH,
                ten, maLoai, donVi, moTa, trangThai);
            try
            {
                busHangHoa.luuDanhMucChiTiet(duLieu, banGoc, nhom, mucTon, hamLuong, giaBan, dangThem);
                string maDaLuu = duLieu.MaHH;
                dangThem = false;
                dangSua = false;
                LoadDuLieu(maDaLuu);
            }
            catch (Exception ex)
            {
                lblThongBao.Text = "Không thể lưu hàng hóa: " + ex.Message;
                MessageBox.Show(lblThongBao.Text, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Xử lý thao tác xóa khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>busHangHoa.xoaDanhMucChiTiet</c>, <c>LoadDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void Xoa_Click(object sender, RoutedEventArgs e)
        {
            if (hangHoaDangChon == null || dangThem || dangSua) return;
            var xacNhan = MessageBox.Show(
                "Xóa mặt hàng '" + hangHoaDangChon.TenHH + "'?\n"
                + "Thông tin thuốc/vật tư liên quan sẽ được xóa cùng danh mục. "
                + "Nếu đã có phiếu hoặc lô tham chiếu, SQL sẽ không cho xóa; nên dùng Ngừng hoạt động.",
                "Xác nhận xóa hàng hóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (xacNhan != MessageBoxResult.Yes) return;
            try
            {
                busHangHoa.xoaDanhMucChiTiet(hangHoaDangChon.MaHH, banGoc);
                hangHoaDangChon = null;
                banGoc = null;
                LoadDuLieu();
            }
            catch (Exception ex)
            {
                lblThongBao.Text = "Không thể xóa hàng hóa: " + ex.Message;
                MessageBox.Show(lblThongBao.Text, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Xử lý thao tác tải lại khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>LoadDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void TaiLai_Click(object sender, RoutedEventArgs e)
        {
            if (dangThem || dangSua) return;
            LoadDuLieu(hangHoaDangChon?.MaHH);
        }
    }
}
