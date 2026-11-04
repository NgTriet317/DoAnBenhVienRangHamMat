using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;

namespace RangHamMat.Views
{
    /// <summary>
    /// Phần xử lý hóa đơn nhà cung cấp dòng của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public class HoaDonNccDong
    {
        /// <summary>
        /// Dữ liệu nghiệp vụ gốc liên kết với dòng hiển thị.
        /// </summary>
        public ET_HOADONNHAP DuLieu { get; set; } = new ET_HOADONNHAP();
        /// <summary>
        /// Giá trị Số hóa đơn nhà cung cấp; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string SoHoaDonNCC { get { return DuLieu.SoHoaDonNCC; } }
        /// <summary>
        /// Tên nhà cung cấp.
        /// </summary>
        public string TenNCC { get; set; } = "";
        /// <summary>
        /// Tên nhân viên.
        /// </summary>
        public string TenNhanVien { get; set; } = "";
        /// <summary>
        /// Giá trị Trạng thái; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string TrangThai { get { return DuLieu.TrangThai; } }
        /// <summary>
        /// Giá trị Tổng tiền hiển thị; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string TongTienHienThi { get { return DuLieu.TongTien.ToString("N0") + " đ"; } }
        /// <summary>
        /// Giá trị Ngày hiển thị; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string NgayHienThi { get { return DuLieu.NgayHoaDon.ToString("dd/MM/yyyy"); } }
        /// <summary>
        /// Số phiếu nhập.
        /// </summary>
        public int SoPhieuNhap { get; set; }
        /// <summary>
        /// Danh sách phiếu dùng trong hóa đơn nhà cung cấp dòng.
        /// </summary>
        public string DanhSachPhieu { get; set; } = "";
    }

    /// <summary>
    /// Phần xử lý chọn nhà cung cấp của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public class ChonNhaCungCap
    {
        /// <summary>
        /// Mã.
        /// </summary>
        public string Ma { get; set; } = "";
        /// <summary>
        /// Tên.
        /// </summary>
        public string Ten { get; set; } = "";
    }

    /// <summary>
    /// Phần xử lý hóa đơn nhà cung cấp của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public partial class HoaDonNhaCungCap : UserControl
    {
        /// <summary>
        /// Đối tượng nghiệp vụ hóa đơn nhập được màn hình dùng để đọc/ghi dữ liệu.
        /// </summary>
        private readonly BUS_HOADONNHAP bus = new BUS_HOADONNHAP();
        /// <summary>
        /// Danh sách  dùng trong hóa đơn nhà cung cấp được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly List<HoaDonNccDong> danhSach = new List<HoaDonNccDong>();
        /// <summary>
        /// Tên nhà cung cấp được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, string> tenNcc = new Dictionary<string, string>();
        /// <summary>
        /// Tên nhân viên được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, string> tenNhanVien = new Dictionary<string, string>();
        /// <summary>
        /// Định dạng được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly CultureInfo dinhDang = CultureInfo.GetCultureInfo("vi-VN");
        /// <summary>
        /// Đang chọn được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private HoaDonNccDong? dangChon;
        /// <summary>
        /// Cờ Đang tải; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool dangTai;
        /// <summary>
        /// Cờ Đã tải; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool daTai;
        /// <summary>
        /// Cờ Đang tạo mới; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool dangTaoMoi = true;

        /// <summary>
        /// Khởi tạo hóa đơn nhà cung cấp. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
        /// </summary>
        public HoaDonNhaCungCap()
        {
            InitializeComponent();
            dpNgayHoaDon.SelectedDate = DateTime.Today;
            Loaded += HoaDonNhaCungCap_Loaded;
        }

        /// <summary>
        /// Nạp dữ liệu khởi đầu khi hóa đơn nhà cung cấp được tải lên giao diện.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>TaiDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void HoaDonNhaCungCap_Loaded(object sender, RoutedEventArgs e)
        {
            if (!daTai) TaiDuLieu("");
        }

        /// <summary>
        /// Tạo bản dữ liệu riêng để biểu mẫu chỉnh sửa mà không tác động trực tiếp đối tượng nguồn.
        /// </summary>
        /// <param name="cu">Giá trị cũ dùng khi thực hiện thao tác; kiểu ET_HOADONNHAP.</param>
        /// <returns>Kết quả kiểu ET_HOADONNHAP dùng cho bước xử lý tiếp theo.</returns>
        private static ET_HOADONNHAP SaoChep(ET_HOADONNHAP cu)
        {
            ET_HOADONNHAP moi = new ET_HOADONNHAP();
            moi.MaHDN = cu.MaHDN;
            moi.SoHoaDonNCC = cu.SoHoaDonNCC;
            moi.MaNCC = cu.MaNCC;
            moi.MaNV = cu.MaNV;
            moi.NgayHoaDon = cu.NgayHoaDon;
            moi.TongTien = cu.TongTien;
            moi.TrangThai = cu.TrangThai;
            moi.GhiChu = cu.GhiChu;
            return moi;
        }

        /// <summary>
        /// Tra tên theo mã trong danh mục và dùng giá trị dự phòng khi không tìm thấy.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ds.ContainsKey</c>.</remarks>
        /// <param name="ds">Giá trị ds dùng khi thực hiện thao tác; kiểu Dictionary&lt;string, string&gt;.</param>
        /// <param name="ma">Mã định danh của bản ghi cần xử lý.</param>
        /// <returns>Chuỗi kết quả của thao tác tên theo mã.</returns>
        private static string TenTheoMa(Dictionary<string, string> ds, string ma)
        {
            if (ds.ContainsKey(ma)) return ds[ma];
            return "Chưa có tên";
        }

        /// <summary>
        /// Đọc lại dữ liệu theo phạm vi hiện tại, áp dụng bộ lọc và cập nhật danh sách/chi tiết.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>tenNcc.Clear</c>, <c>tenNhanVien.Clear</c>, <c>danhSach.Clear</c>, <c>cacNcc.Add</c>.</remarks>
        /// <param name="maCanChon">Mã cần chọn xác định đối tượng cần xử lý.</param>
        private void TaiDuLieu(string maCanChon)
        {
            dangTai = true;
            try
            {
                tenNcc.Clear();
                tenNhanVien.Clear();
                danhSach.Clear();
                List<ChonNhaCungCap> cacNcc = new List<ChonNhaCungCap>();
                cacNcc.Add(new ChonNhaCungCap { Ma = "", Ten = "Tất cả nhà cung cấp" });
                List<ChonNhaCungCap> nccTrongForm = new List<ChonNhaCungCap>();
                foreach (ET_NHACUNGCAP n in new BUS_NHACUNGCAP().layToanBoNhaCungCap())
                {
                    tenNcc[n.MaNCC] = n.TenNCC;
                    cacNcc.Add(new ChonNhaCungCap { Ma = n.MaNCC, Ten = n.TenNCC });
                    nccTrongForm.Add(new ChonNhaCungCap { Ma = n.MaNCC, Ten = n.TenNCC });
                }
                foreach (ET_NHANVIEN nv in new BUS_NHANVIEN().layToanBoNhanVien())
                    tenNhanVien[nv.MaNV] = nv.HoTen;

                Dictionary<string, HoaDonNccDong> theoMa = new Dictionary<string, HoaDonNccDong>();
                foreach (ET_HOADONNHAP h in bus.layToanBoHoaDonNhap())
                {
                    HoaDonNccDong d = new HoaDonNccDong();
                    d.DuLieu = h;
                    d.TenNCC = TenTheoMa(tenNcc, h.MaNCC);
                    d.TenNhanVien = TenTheoMa(tenNhanVien, h.MaNV);
                    danhSach.Add(d);
                    theoMa[h.MaHDN] = d;
                }
                foreach (ET_PHIEUNHAP phieu in new BUS_PHIEUNHAP().layToanBoPhieuNhap())
                {
                    if (!theoMa.ContainsKey(phieu.MaHDN)) continue;
                    HoaDonNccDong d = theoMa[phieu.MaHDN];
                    d.SoPhieuNhap++;
                    if (d.DanhSachPhieu.Length > 0) d.DanhSachPhieu += ", ";
                    d.DanhSachPhieu += phieu.MaPN;
                }
                danhSach.Sort(SoSanhHoaDon);

                string locCu = "";
                if (cboLocNCC.SelectedValue is string maLoc) locCu = maLoc;
                cboLocNCC.ItemsSource = cacNcc;
                cboLocNCC.SelectedValue = locCu;
                if (cboLocNCC.SelectedIndex < 0) cboLocNCC.SelectedIndex = 0;
                cboNhaCungCap.ItemsSource = nccTrongForm;

                lblTong.Text = danhSach.Count.ToString();
                int cho = 0;
                int xacNhan = 0;
                foreach (HoaDonNccDong d in danhSach)
                {
                    if (d.TrangThai == "Nháp") cho++;
                    if (d.TrangThai == "Đã xác nhận") xacNhan++;
                }
                lblCho.Text = cho.ToString();
                lblDaXacNhan.Text = xacNhan.ToString();
                daTai = true;
                dangTai = false;
                ApDungBoLoc(maCanChon);
                if (maCanChon.Length == 0 && dangTaoMoi) TaoFormMoi();
            }
            catch (Exception ex)
            {
                dangTai = false;
                MessageBox.Show("Không tải được hóa đơn nhà cung cấp: " + ex.Message,
                    "Hóa đơn nhà cung cấp", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// So sánh hai hóa đơn để xác định thứ tự hiển thị của danh sách.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>b.DuLieu.NgayHoaDon.CompareTo</c>.</remarks>
        /// <param name="a">Giá trị a dùng khi thực hiện thao tác; kiểu HoaDonNccDong.</param>
        /// <param name="b">Giá trị b dùng khi thực hiện thao tác; kiểu HoaDonNccDong.</param>
        /// <returns>Giá trị số được đọc, đếm hoặc tính toán trong hàm.</returns>
        private static int SoSanhHoaDon(HoaDonNccDong a, HoaDonNccDong b)
        {
            return b.DuLieu.NgayHoaDon.CompareTo(a.DuLieu.NgayHoaDon);
        }

        /// <summary>
        /// Áp dụng các tiêu chí đang chọn để cập nhật danh sách và trạng thái rỗng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>txtTim.Text.Trim</c>, <c>item.Content.ToString</c>, <c>noiDung.IndexOf</c>, <c>ketQua.Add</c>.</remarks>
        /// <param name="maCanChon">Mã cần chọn xác định đối tượng cần xử lý.</param>
        private void ApDungBoLoc(string maCanChon = "")
        {
            if (dangTai || !daTai) return;
            string tim = txtTim.Text.Trim();
            string maNcc = "";
            if (cboLocNCC.SelectedValue is string ma) maNcc = ma;
            string trangThai = "Tất cả";
            if (cboLocTrangThai.SelectedItem is ComboBoxItem item && item.Content != null)
                trangThai = item.Content.ToString();
            if (maCanChon.Length == 0 && dangChon != null) maCanChon = dangChon.DuLieu.MaHDN;

            List<HoaDonNccDong> ketQua = new List<HoaDonNccDong>();
            foreach (HoaDonNccDong d in danhSach)
            {
                if (maNcc.Length > 0 && d.DuLieu.MaNCC != maNcc) continue;
                if (trangThai != "Tất cả" && d.TrangThai != trangThai) continue;
                string noiDung = d.SoHoaDonNCC + " " + d.DuLieu.MaHDN;
                if (tim.Length > 0 && noiDung.IndexOf(tim, StringComparison.OrdinalIgnoreCase) < 0) continue;
                ketQua.Add(d);
            }
            dangTai = true;
            lstHoaDon.ItemsSource = ketQua;
            lblSoKetQua.Text = ketQua.Count + " / " + danhSach.Count + " hóa đơn";
            HoaDonNccDong? chon = null;
            foreach (HoaDonNccDong d in ketQua)
            {
                if (d.DuLieu.MaHDN == maCanChon) { chon = d; break; }
            }
            if (chon == null && ketQua.Count > 0 && !dangTaoMoi) chon = ketQua[0];
            lstHoaDon.SelectedItem = chon;
            dangTai = false;
            if (chon != null) DienHoaDon(chon);
            else if (!dangTaoMoi) TaoFormMoi();
        }

        /// <summary>
        /// Điền thông tin hóa đơn nhà cung cấp và đặt lại mốc đối chiếu dữ liệu đang sửa.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>h.TongTien.ToString</c>, <c>CapNhatQuyen</c>.</remarks>
        /// <param name="d">Giá trị d dùng khi thực hiện thao tác; kiểu HoaDonNccDong.</param>
        private void DienHoaDon(HoaDonNccDong d)
        {
            dangChon = d;
            dangTaoMoi = false;
            ET_HOADONNHAP h = d.DuLieu;
            lblCheDo.Text = "Hóa đơn " + h.SoHoaDonNCC;
            lblTrangThai.Text = h.TrangThai;
            txtSoHoaDon.Text = h.SoHoaDonNCC;
            dpNgayHoaDon.SelectedDate = h.NgayHoaDon.Date;
            cboNhaCungCap.SelectedValue = h.MaNCC;
            txtTongTien.Text = h.TongTien.ToString("0.##", dinhDang);
            txtNguoiLap.Text = d.TenNhanVien;
            txtGhiChu.Text = h.GhiChu;
            if (d.SoPhieuNhap == 0) lblPhieuLienKet.Text = "Chưa có phiếu nhập tham chiếu";
            else lblPhieuLienKet.Text = d.SoPhieuNhap + " phiếu: " + d.DanhSachPhieu;
            CapNhatQuyen();
        }

        /// <summary>
        /// Xóa lựa chọn cũ và chuẩn bị biểu mẫu tạo chứng từ mới.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>TenTheoMa</c>, <c>CapNhatQuyen</c>.</remarks>
        private void TaoFormMoi()
        {
            dangChon = null;
            dangTaoMoi = true;
            dangTai = true;
            lstHoaDon.SelectedItem = null;
            dangTai = false;
            lblCheDo.Text = "Thêm hóa đơn mới";
            lblTrangThai.Text = "Nháp";
            txtSoHoaDon.Text = "";
            txtTongTien.Text = "";
            txtGhiChu.Text = "";
            dpNgayHoaDon.SelectedDate = DateTime.Today;
            cboNhaCungCap.SelectedIndex = -1;
            txtNguoiLap.Text = TenTheoMa(tenNhanVien, PhienDangNhap.MaNV);
            lblPhieuLienKet.Text = "Sẽ liên kết khi tạo phiếu nhập từ hóa đơn này.";
            CapNhatQuyen();
        }

        /// <summary>
        /// Bật hoặc tắt các thao tác theo quyền người dùng và trạng thái chứng từ.
        /// </summary>
        private void CapNhatQuyen()
        {
            bool suaDuoc = dangTaoMoi;
            if (dangChon != null && dangChon.TrangThai == "Nháp" && dangChon.SoPhieuNhap == 0)
                suaDuoc = true;
            txtSoHoaDon.IsReadOnly = !suaDuoc;
            txtTongTien.IsReadOnly = !suaDuoc;
            txtGhiChu.IsReadOnly = !suaDuoc;
            dpNgayHoaDon.IsEnabled = suaDuoc;
            cboNhaCungCap.IsEnabled = suaDuoc;
            btnLuu.IsEnabled = suaDuoc;
            btnXacNhan.IsEnabled = dangChon != null && dangChon.TrangThai == "Nháp";
            btnHuy.IsEnabled = dangChon != null && dangChon.TrangThai == "Nháp" && dangChon.SoPhieuNhap == 0;
        }

        /// <summary>
        /// Đọc các trường biểu mẫu thành đối tượng chứng từ đầu vào trước khi kiểm tra và lưu.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>txtSoHoaDon.Text.Trim</c>, <c>MessageBox.Show</c>, <c>decimal.TryParse</c>, <c>txtTongTien.Text.Trim</c>.</remarks>
        /// <param name="h">Giá trị h dùng khi thực hiện thao tác; kiểu ET_HOADONNHAP.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        private bool LayDuLieuNhap(out ET_HOADONNHAP h)
        {
            h = new ET_HOADONNHAP();
            string maNcc = "";
            if (cboNhaCungCap.SelectedValue is string ma) maNcc = ma;
            string soHoaDon = txtSoHoaDon.Text.Trim();
            if (soHoaDon.Length == 0 || maNcc.Length == 0 || !dpNgayHoaDon.SelectedDate.HasValue)
            {
                MessageBox.Show("Nhập số hóa đơn, nhà cung cấp và ngày hóa đơn.");
                return false;
            }
            if (dpNgayHoaDon.SelectedDate.Value.Date > DateTime.Today)
            {
                MessageBox.Show("Ngày hóa đơn không được ở tương lai.");
                return false;
            }
            if (PhienDangNhap.MaNV.Length == 0 && dangTaoMoi)
            {
                MessageBox.Show("Không xác định được nhân viên đăng nhập.");
                return false;
            }
            decimal tongTien;
            if (!decimal.TryParse(txtTongTien.Text.Trim(), NumberStyles.Number, dinhDang, out tongTien) || tongTien < 0)
            {
                MessageBox.Show("Tổng tiền không hợp lệ. Vui lòng nhập số tiền bằng VNĐ.");
                return false;
            }
            h.MaNCC = maNcc;
            h.SoHoaDonNCC = soHoaDon;
            h.NgayHoaDon = dpNgayHoaDon.SelectedDate.Value.Date;
            h.TongTien = tongTien;
            h.GhiChu = txtGhiChu.Text.Trim();
            h.TrangThai = "Nháp";
            h.MaNV = PhienDangNhap.MaNV;
            if (!dangTaoMoi && dangChon != null)
            {
                h.MaHDN = dangChon.DuLieu.MaHDN;
                h.MaNV = dangChon.DuLieu.MaNV;
            }
            return true;
        }

        /// <summary>
        /// So sánh nội dung biểu mẫu với bản dữ liệu đã chấp nhận để phát hiện thay đổi chưa lưu.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>decimal.TryParse</c>, <c>txtTongTien.Text.Trim</c>, <c>txtSoHoaDon.Text.Trim</c>, <c>txtGhiChu.Text.Trim</c>.</remarks>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        private bool CoThayDoiChuaLuu()
        {
            if (dangChon == null) return false;
            ET_HOADONNHAP h = dangChon.DuLieu;
            string maNcc = "";
            if (cboNhaCungCap.SelectedValue is string ma) maNcc = ma;
            decimal tien;
            if (!decimal.TryParse(txtTongTien.Text.Trim(), NumberStyles.Number, dinhDang, out tien)) return true;
            if (txtSoHoaDon.Text.Trim() != h.SoHoaDonNCC) return true;
            if (maNcc != h.MaNCC) return true;
            if (!dpNgayHoaDon.SelectedDate.HasValue || dpNgayHoaDon.SelectedDate.Value.Date != h.NgayHoaDon.Date) return true;
            if (tien != h.TongTien) return true;
            string ghiChu = "";
            if (h.GhiChu != null) ghiChu = h.GhiChu;
            if (txtGhiChu.Text.Trim() != ghiChu.Trim()) return true;
            return false;
        }

        /// <summary>
        /// Xử lý thao tác lưu nháp khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>LayDuLieuNhap</c>, <c>bus.themHoaDonNhap</c>, <c>bus.suaHoaDonNhap</c>, <c>MessageBox.Show</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void LuuNhap_Click(object sender, RoutedEventArgs e)
        {
            ET_HOADONNHAP h;
            if (!LayDuLieuNhap(out h)) return;
            if (!dangTaoMoi && dangChon != null && dangChon.TrangThai != "Nháp") return;
            try
            {
                if (dangTaoMoi) bus.themHoaDonNhap(h);
                else if (dangChon != null) bus.suaHoaDonNhap(h, dangChon.DuLieu);
                MessageBox.Show("Đã lưu hóa đơn ở trạng thái Nháp.", "Hóa đơn nhà cung cấp", MessageBoxButton.OK, MessageBoxImage.Information);
                TaiDuLieu(h.MaHDN);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không lưu được hóa đơn: " + ex.Message, "Hóa đơn nhà cung cấp", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Xử lý thao tác xác nhận khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>CoThayDoiChuaLuu</c>, <c>MessageBox.Show</c>, <c>SaoChep</c>, <c>bus.suaHoaDonNhap</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void XacNhan_Click(object sender, RoutedEventArgs e)
        {
            if (dangChon == null || dangChon.TrangThai != "Nháp") return;
            if (CoThayDoiChuaLuu())
            {
                MessageBox.Show("Thông tin đang khác với bản đã lưu. Hãy Lưu nháp trước khi xác nhận.");
                return;
            }
            if (dangChon.DuLieu.TongTien <= 0)
            {
                MessageBox.Show("Hóa đơn phải có tổng tiền lớn hơn 0 trước khi xác nhận.");
                return;
            }
            if (MessageBox.Show("Xác nhận hóa đơn này? Sau khi xác nhận sẽ không thể chỉnh sửa.",
                "Xác nhận hóa đơn", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                ET_HOADONNHAP h = SaoChep(dangChon.DuLieu);
                h.TrangThai = "Đã xác nhận";
                bus.suaHoaDonNhap(h, dangChon.DuLieu, true);
                TaiDuLieu(h.MaHDN);
                MessageBox.Show("Hóa đơn đã xác nhận và có thể dùng cho phiếu nhập kho.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không xác nhận được hóa đơn: " + ex.Message);
            }
        }

        /// <summary>
        /// Xử lý thao tác hủy khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>SaoChep</c>, <c>bus.suaHoaDonNhap</c>, <c>TaiDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void Huy_Click(object sender, RoutedEventArgs e)
        {
            if (dangChon == null || dangChon.TrangThai != "Nháp" || dangChon.SoPhieuNhap > 0) return;
            if (MessageBox.Show("Hủy hóa đơn nháp này? Hóa đơn hủy được giữ lại để tra cứu.",
                "Hủy hóa đơn", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try
            {
                ET_HOADONNHAP h = SaoChep(dangChon.DuLieu);
                h.TrangThai = "Hủy";
                bus.suaHoaDonNhap(h, dangChon.DuLieu, true);
                TaiDuLieu(h.MaHDN);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không hủy được hóa đơn: " + ex.Message);
            }
        }

        /// <summary>
        /// Xử lý thao tác tạo mới khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>TaoFormMoi</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void TaoMoi_Click(object sender, RoutedEventArgs e)
        {
            TaoFormMoi();
        }

        /// <summary>
        /// Xử lý thao tác làm mới khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>TaiDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void LamMoi_Click(object sender, RoutedEventArgs e)
        {
            string ma = "";
            if (dangChon != null) ma = dangChon.DuLieu.MaHDN;
            TaiDuLieu(ma);
        }

        /// <summary>
        /// Tiếp nhận thay đổi bộ lọc và cập nhật trạng thái/dữ liệu phụ thuộc.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ApDungBoLoc</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void BoLoc_Changed(object sender, RoutedEventArgs e)
        {
            if (dangTai || !daTai) return;
            ApDungBoLoc();
        }

        /// <summary>
        /// Phản ánh lựa chọn hóa đơn vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>DienHoaDon</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void HoaDon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dangTai) return;
            if (lstHoaDon.SelectedItem is HoaDonNccDong dong) DienHoaDon(dong);
        }
    }
}
