using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;

namespace RangHamMat.Views
{
    /// <summary>
    /// Phần xử lý phiếu nhập hiển thị của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public class PhieuNhapHienThi
    {
        /// <summary>
        /// Mã phiếu tiếp nhận khám.
        /// </summary>
        public string MaPhieu { get; set; } = "";
        /// <summary>
        /// Mã kho.
        /// </summary>
        public string MaKho { get; set; } = "";
        /// <summary>
        /// Mã hóa đơn nhập.
        /// </summary>
        public string MaHDN { get; set; } = "";
        /// <summary>
        /// Nội dung ghi chú bổ sung của bản ghi.
        /// </summary>
        public string GhiChu { get; set; } = "";
        /// <summary>
        /// Có thể sửa.
        /// </summary>
        public bool CanSua { get; set; } = true;
        /// <summary>
        /// Nhà cung cấp.
        /// </summary>
        public string NhaCungCap { get; set; } = "";
        /// <summary>
        /// Số hóa đơn.
        /// </summary>
        public string SoHoaDon { get; set; } = "";
        /// <summary>
        /// Tên kho.
        /// </summary>
        public string TenKho { get; set; } = "";
        /// <summary>
        /// Tên nhân viên.
        /// </summary>
        public string TenNhanVien { get; set; } = "";
        /// <summary>
        /// Ngày nhập.
        /// </summary>
        public string NgayNhap { get; set; } = "";
        /// <summary>
        /// Thời gian nhập.
        /// </summary>
        public DateTime ThoiGianNhap { get; set; }
        /// <summary>
        /// Trạng thái.
        /// </summary>
        public string TrangThai { get; set; } = "";
        /// <summary>
        /// Lý do.
        /// </summary>
        public string LyDo { get; set; } = "";
        /// <summary>
        /// Hàng hóa.
        /// </summary>
        public List<HangNhapHienThi> HangHoa { get; set; } = new List<HangNhapHienThi>();
    }

    /// <summary>
    /// Phần xử lý hàng nhập hiển thị của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public class HangNhapHienThi
    {
        /// <summary>
        /// Mã hàng hóa.
        /// </summary>
        public string MaHH { get; set; } = "";
        /// <summary>
        /// Hạn sử dụng của lô hàng. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? HanSuDung { get; set; }
        /// <summary>
        /// Trạng thái lô.
        /// </summary>
        public string TrangThaiLo { get; set; } = "";
        /// <summary>
        /// Tên hàng.
        /// </summary>
        public string TenHang { get; set; } = "";
        /// <summary>
        /// Tên lô.
        /// </summary>
        public string TenLo { get; set; } = "";
        /// <summary>
        /// Đơn vị tính.
        /// </summary>
        public string DonViTinh { get; set; } = "";
        /// <summary>
        /// Số lượng của dòng dữ liệu.
        /// </summary>
        public decimal SoLuong { get; set; }
        /// <summary>
        /// Đơn giá áp dụng cho dòng dữ liệu.
        /// </summary>
        public decimal DonGia { get; set; }
        /// <summary>
        /// Giá trị Số lượng hiển thị; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string SoLuongHienThi { get { return SoLuong.ToString("N3"); } }
        /// <summary>
        /// Giá trị Đơn giá hiển thị; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string DonGiaHienThi { get { return DonGia.ToString("N0") + " đ"; } }
        /// <summary>
        /// Giá trị Thành tiền hiển thị; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string ThanhTienHienThi { get { return (SoLuong * DonGia).ToString("N0") + " đ"; } }
    }

    /// <summary>
    /// Phần xử lý lựa chọn nhập của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public class LuaChonNhap
    {
        /// <summary>
        /// Mã.
        /// </summary>
        public string Ma { get; set; } = "";
        /// <summary>
        /// Tên.
        /// </summary>
        public string Ten { get; set; } = "";
        /// <summary>
        /// Nhóm hàng.
        /// </summary>
        public string NhomHang { get; set; } = "";
    }

    /// <summary>
    /// Phần xử lý dòng nhập tạm của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public class DongNhapTam
    {
        /// <summary>
        /// Mã hàng hóa.
        /// </summary>
        public string MaHH { get; set; } = "";
        /// <summary>
        /// Tên hàng.
        /// </summary>
        public string TenHang { get; set; } = "";
        /// <summary>
        /// Số lô do nghiệp vụ kho ghi nhận.
        /// </summary>
        public string SoLo { get; set; } = "";
        /// <summary>
        /// Hạn sử dụng của lô hàng. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? HanSuDung { get; set; }
        /// <summary>
        /// Số lượng của dòng dữ liệu.
        /// </summary>
        public decimal SoLuong { get; set; }
        /// <summary>
        /// Đơn giá áp dụng cho dòng dữ liệu.
        /// </summary>
        public decimal DonGia { get; set; }
        /// <summary>
        /// Giá trị Hạn sử dụng hiển thị; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string HanSuDungHienThi { get { if (HanSuDung.HasValue) return HanSuDung.Value.ToString("dd/MM/yyyy"); return "—"; } }
        /// <summary>
        /// Giá trị Số lượng hiển thị; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string SoLuongHienThi { get { return SoLuong.ToString("N3"); } }
        /// <summary>
        /// Giá trị Thành tiền hiển thị; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string ThanhTienHienThi { get { return (SoLuong * DonGia).ToString("N0") + " đ"; } }
    }

    /// <summary>
    /// Phần xử lý nhập kho của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public partial class NhapKho : UserControl
    {
        /// <summary>
        /// Cờ Đang tải; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool dangTai;
        /// <summary>
        /// Cờ Đã tải; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool daTai;
        /// <summary>
        /// Danh sách  dùng trong nhập kho được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly List<PhieuNhapHienThi> danhSach = new List<PhieuNhapHienThi>();
        /// <summary>
        /// Dòng nhập mới được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly List<DongNhapTam> dongNhapMoi = new List<DongNhapTam>();
        /// <summary>
        /// Danh sách hàng chọn dùng trong nhập kho được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly List<LuaChonNhap> danhSachHangChon = new List<LuaChonNhap>();
        /// <summary>
        /// Hóa đơn được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, ET_HOADONNHAP> hoaDon = new Dictionary<string, ET_HOADONNHAP>();
        /// <summary>
        /// Nhà cung cấp được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, string> nhaCungCap = new Dictionary<string, string>();
        /// <summary>
        /// Tên hàng được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, string> tenHang = new Dictionary<string, string>();
        /// <summary>
        /// Đơn vị được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, string> donVi = new Dictionary<string, string>();
        /// <summary>
        /// Tên kho được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, string> tenKho = new Dictionary<string, string>();
        /// <summary>
        /// Tên nhân viên được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, string> tenNhanVien = new Dictionary<string, string>();
        /// <summary>
        /// Mã đang xem được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maDangXem = "";
        /// <summary>
        /// Mã đang sửa được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maDangSua = "";
        /// <summary>
        /// Ngày nhập đã tạo được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private DateTime? ngayNhapDaTao;

        /// <summary>
        /// Khởi tạo nhập kho. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
        /// </summary>
        public NhapKho()
        {
            InitializeComponent();
            lblNgayNhapMoi.Text = "Tự động lấy khi lưu phiếu mới";
            txtLyDo.Text = "Nhập hàng từ nhà cung cấp";
            Loaded += NhapKho_Loaded;
        }

        /// <summary>
        /// Nạp dữ liệu khởi đầu khi nhập kho được tải lên giao diện.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>TaiDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void NhapKho_Loaded(object sender, RoutedEventArgs e)
        {
            // Mỗi lần mở lại tab sẽ lấy phiếu mới, kể cả phiếu vừa ghi từ phân hệ khác.
            if (!dangTai) TaiDuLieu();
        }

        /// <summary>
        /// Tra tên trong danh mục dữ liệu đã nạp theo mã được truyền vào.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>bang.ContainsKey</c>.</remarks>
        /// <param name="bang">Giá trị bang dùng khi thực hiện thao tác; kiểu Dictionary&lt;string, string&gt;.</param>
        /// <param name="ma">Mã định danh của bản ghi cần xử lý.</param>
        /// <param name="macDinh">Giá trị mặc định dùng khi thực hiện thao tác; kiểu string.</param>
        /// <returns>Chuỗi kết quả của thao tác lấy tên.</returns>
        private static string LayTen(Dictionary<string, string> bang, string? ma, string macDinh)
        {
            if (ma != null && bang.ContainsKey(ma)) return bang[ma];
            return macDinh;
        }

        /// <summary>
        /// Đọc lại dữ liệu theo phạm vi hiện tại, áp dụng bộ lọc và cập nhật danh sách/chi tiết.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>tenKho.Clear</c>, <c>tenNhanVien.Clear</c>, <c>tenHang.Clear</c>, <c>donVi.Clear</c>.</remarks>
        private void TaiDuLieu()
        {
            dangTai = true;
            try
            {
                tenKho.Clear();
                tenNhanVien.Clear();
                tenHang.Clear();
                donVi.Clear();
                nhaCungCap.Clear();
                hoaDon.Clear();
                Dictionary<string, string> soLo = new Dictionary<string, string>();
                Dictionary<string, ET_LOHANGHOA> thongTinLo = new Dictionary<string, ET_LOHANGHOA>();
                List<LuaChonNhap> khoChon = new List<LuaChonNhap>();
                List<LuaChonNhap> hangChon = new List<LuaChonNhap>();
                List<LuaChonNhap> hoaDonChon = new List<LuaChonNhap>();
                List<LuaChonNhap> khoLoc = new List<LuaChonNhap>();
                khoLoc.Add(new LuaChonNhap { Ma = "", Ten = "Tất cả kho" });

                foreach (ET_KHO k in new BUS_KHO().layToanBoKho())
                {
                    tenKho[k.MaKho] = k.TenKho;
                    khoChon.Add(new LuaChonNhap { Ma = k.MaKho, Ten = k.TenKho });
                    khoLoc.Add(new LuaChonNhap { Ma = k.MaKho, Ten = k.TenKho });
                }
                foreach (ET_NHANVIEN n in new BUS_NHANVIEN().layToanBoNhanVien()) tenNhanVien[n.MaNV] = n.HoTen;
                foreach (ET_NHACUNGCAP n in new BUS_NHACUNGCAP().layToanBoNhaCungCap()) nhaCungCap[n.MaNCC] = n.TenNCC;
                // Danh mục hàng dùng chung ba loại, theo cách phân nhóm ở màn hình Danh mục hàng hóa.
                // Ưu tiên nhóm từ HANG_HOA; vẫn nhận các dòng thuốc/vật tư cũ theo bảng nghiệp vụ.
                Dictionary<string, string> nhomHangTheoMa = new Dictionary<string, string>();
                foreach (ET_VATTU vt in new BUS_VATTU().layToanBoVatTu()) nhomHangTheoMa[vt.MaHH] = "VatTu";
                foreach (ET_THUOC thuoc in new BUS_THUOC().layToanBoThuoc()) nhomHangTheoMa[thuoc.MaHH] = "Thuoc";
                DataTable danhMuc = new BUS_HANGHOA().layDanhMucChiTiet();
                foreach (DataRow hang in danhMuc.Rows)
                {
                    string nhom = hang["NhomHang"].ToString() ?? "";
                    if (nhom != "Thuoc" && nhom != "VatTu" && nhom != "ThietBi") continue;
                    string maHH = hang["MaHH"].ToString() ?? "";
                    if (maHH.Length > 0) nhomHangTheoMa[maHH] = nhom;
                }
                foreach (ET_HANGHOA h in new BUS_HANGHOA().layToanBoHangHoa())
                {
                    tenHang[h.MaHH] = h.TenHH;
                    donVi[h.MaHH] = h.DonViTinh;
                    if (nhomHangTheoMa.TryGetValue(h.MaHH, out string? nhom))
                    {
                        string tenChon = h.TenHH + " · " + h.DonViTinh;
                        hangChon.Add(new LuaChonNhap { Ma = h.MaHH, Ten = tenChon, NhomHang = nhom });
                    }
                }
                foreach (ET_LOHANGHOA l in new BUS_LOHANGHOA().layToanBoLoHangHoa())
                {
                    thongTinLo[l.MaLo] = l;
                    if (!string.IsNullOrWhiteSpace(l.SoLo)) soLo[l.MaLo] = l.SoLo;
                    else soLo[l.MaLo] = l.MaLo;
                }
                foreach (ET_HOADONNHAP h in new BUS_HOADONNHAP().layToanBoHoaDonNhap())
                {
                    hoaDon[h.MaHDN] = h;
                    if (h.TrangThai != "Hủy")
                        hoaDonChon.Add(new LuaChonNhap { Ma = h.MaHDN, Ten = h.SoHoaDonNCC + " · " + LayTen(nhaCungCap, h.MaNCC, "Nhà cung cấp") });
                }

                string maKhoLoc = cboKhoLoc.SelectedValue as string;
                string maHoaDonMoi = cboHoaDon.SelectedValue as string;
                string maKhoMoi = cboKhoMoi.SelectedValue as string;
                string maHangMoi = cboHangMoi.SelectedValue as string;
                cboKhoLoc.ItemsSource = khoLoc;
                cboKhoMoi.ItemsSource = khoChon;
                cboHoaDon.ItemsSource = hoaDonChon;
                danhSachHangChon.Clear();
                danhSachHangChon.AddRange(hangChon);
                LocHangMoi(maHangMoi);
                cboKhoLoc.SelectedValue = maKhoLoc;
                cboKhoMoi.SelectedValue = maKhoMoi;
                cboHoaDon.SelectedValue = maHoaDonMoi;
                if (cboKhoLoc.SelectedIndex < 0) cboKhoLoc.SelectedIndex = 0;
                if (cboKhoMoi.SelectedIndex < 0 && khoChon.Count > 0) cboKhoMoi.SelectedIndex = 0;
                if (cboHoaDon.SelectedIndex < 0 && hoaDonChon.Count > 0) cboHoaDon.SelectedIndex = 0;

                danhSach.Clear();
                Dictionary<string, PhieuNhapHienThi> phieuTheoMa = new Dictionary<string, PhieuNhapHienThi>();
                foreach (ET_PHIEUNHAP p in new BUS_PHIEUNHAP().layToanBoPhieuNhap())
                {
                    PhieuNhapHienThi item = new PhieuNhapHienThi();
                    item.MaPhieu = p.MaPN;
                    item.MaKho = p.MaKho;
                    item.MaHDN = p.MaHDN;
                    item.GhiChu = p.GhiChu;
                    item.TenKho = LayTen(tenKho, p.MaKho, "Chưa xác định kho");
                    item.TenNhanVien = LayTen(tenNhanVien, p.MaNV, "Chưa xác định");
                    item.NgayNhap = p.NgayNhap.ToString("dd/MM/yyyy HH:mm");
                    item.ThoiGianNhap = p.NgayNhap;
                    item.TrangThai = p.TrangThai;
                    item.LyDo = p.LyDo;
                    if (hoaDon.ContainsKey(p.MaHDN))
                    {
                        ET_HOADONNHAP h = hoaDon[p.MaHDN];
                        item.NhaCungCap = LayTen(nhaCungCap, h.MaNCC, "Chưa xác định");
                        item.SoHoaDon = h.SoHoaDonNCC;
                    }
                    danhSach.Add(item);
                    phieuTheoMa[p.MaPN] = item;
                }
                foreach (ET_CHITIETPHIEUNHAP ct in new BUS_CHITIETPHIEUNHAP().layToanBoChiTietPhieuNhap())
                {
                    if (!phieuTheoMa.ContainsKey(ct.MaPN)) continue;
                    HangNhapHienThi dong = new HangNhapHienThi();
                    dong.MaHH = ct.MaHH;
                    if (ct.MaLo != null && thongTinLo.ContainsKey(ct.MaLo))
                    {
                        ET_LOHANGHOA lo = thongTinLo[ct.MaLo];
                        dong.HanSuDung = lo.HanSuDung;
                        dong.TrangThaiLo = lo.TrangThai;
                    }
                    else dong.TrangThaiLo = "Không hợp lệ";
                    if (dong.TrangThaiLo != "Chờ nhập") phieuTheoMa[ct.MaPN].CanSua = false;
                    dong.TenHang = LayTen(tenHang, ct.MaHH, "Chưa có tên hàng");
                    dong.TenLo = LayTen(soLo, ct.MaLo, "Không có lô");
                    dong.DonViTinh = LayTen(donVi, ct.MaHH, "");
                    dong.SoLuong = ct.SoLuong;
                    dong.DonGia = ct.DonGia;
                    phieuTheoMa[ct.MaPN].HangHoa.Add(dong);
                }
                foreach (PhieuNhapHienThi p in danhSach)
                    if (p.HangHoa.Count == 0) p.CanSua = false;
                danhSach.Sort(SoSanhPhieu);
                daTai = true;
                dangTai = false;
                LocPhieu();
                CapNhatNguoiLap();
                HienNhaCungCap();
            }
            catch (Exception ex)
            {
                dangTai = false;
                MessageBox.Show("Không tải được dữ liệu phiếu nhập: " + ex.Message, "Nhập kho", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// So sánh hai phiếu để xác định thứ tự hiển thị của danh sách.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>b.ThoiGianNhap.CompareTo</c>.</remarks>
        /// <param name="a">Giá trị a dùng khi thực hiện thao tác; kiểu PhieuNhapHienThi.</param>
        /// <param name="b">Giá trị b dùng khi thực hiện thao tác; kiểu PhieuNhapHienThi.</param>
        /// <returns>Giá trị số được đọc, đếm hoặc tính toán trong hàm.</returns>
        private static int SoSanhPhieu(PhieuNhapHienThi a, PhieuNhapHienThi b)
        {
            return b.ThoiGianNhap.CompareTo(a.ThoiGianNhap);
        }

        /// <summary>
        /// Hiển thị nhân viên lập phiếu theo ngữ cảnh và dữ liệu đang chọn.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>LayTen</c>.</remarks>
        private void CapNhatNguoiLap()
        {
            string maNV = PhienDangNhap.MaNV;
            lblNguoiLapMoi.Text = LayTen(tenNhanVien, maNV, "Chưa có nhân viên đăng nhập");
        }

        /// <summary>
        /// Đưa thông tin nhà cung cấp của chứng từ đang chọn lên biểu mẫu.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>hoaDon.ContainsKey</c>, <c>LayTen</c>.</remarks>
        private void HienNhaCungCap()
        {
            string maHDN = cboHoaDon.SelectedValue as string;
            if (maHDN != null && hoaDon.ContainsKey(maHDN))
                lblNCCMoi.Text = LayTen(nhaCungCap, hoaDon[maHDN].MaNCC, "Không rõ nhà cung cấp");
            else lblNCCMoi.Text = "Chọn hóa đơn";
        }

        /// <summary>
        /// Lọc danh sách phiếu theo các tiêu chí đang chọn trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>txtTimPhieu.Text.Trim</c>, <c>trangThaiItem.Content.ToString</c>, <c>chuoi.IndexOf</c>, <c>ketQua.Add</c>.</remarks>
        private void LocPhieu()
        {
            if (dangTai || !daTai) return;
            string tim = txtTimPhieu.Text.Trim();
            string kho = cboKhoLoc.SelectedValue as string;
            if (kho == null) kho = "";
            string trangThai = "Tất cả";
            ComboBoxItem trangThaiItem = cboTrangThaiLoc.SelectedItem as ComboBoxItem;
            if (trangThaiItem != null) trangThai = trangThaiItem.Content.ToString();
            List<PhieuNhapHienThi> ketQua = new List<PhieuNhapHienThi>();
            foreach (PhieuNhapHienThi p in danhSach)
            {
                if (kho.Length > 0 && p.MaKho != kho) continue;
                if (trangThai != "Tất cả" && p.TrangThai != trangThai) continue;
                string chuoi = p.MaPhieu + " " + p.TenKho + " " + p.NhaCungCap + " " + p.SoHoaDon;
                if (chuoi.IndexOf(tim, StringComparison.OrdinalIgnoreCase) < 0) continue;
                ketQua.Add(p);
            }
            dangTai = true;
            lstPhieu.ItemsSource = null;
            lstPhieu.ItemsSource = ketQua;
            lblDemPhieu.Text = ketQua.Count.ToString() + " phiếu";
            foreach (PhieuNhapHienThi p in ketQua)
            {
                if (p.MaPhieu == maDangXem)
                {
                    lstPhieu.SelectedItem = p;
                    break;
                }
            }
            dangTai = false;
        }

        /// <summary>
        /// Hiển thị đầu phiếu, các dòng hàng và trạng thái cho phép thao tác của phiếu được chọn.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>phieu.HangHoa.Count.ToString</c>, <c>soLuong.ToString</c>, <c>tongTien.ToString</c>.</remarks>
        /// <param name="phieu">Giá trị phiếu dùng khi thực hiện thao tác; kiểu PhieuNhapHienThi.</param>
        private void HienPhieu(PhieuNhapHienThi phieu)
        {
            if (phieu == null) return;
            maDangXem = phieu.MaPhieu;
            maDangSua = "";
            formTaoMoi.Visibility = Visibility.Collapsed;
            panelXemPhieu.Visibility = Visibility.Visible;
            lblTieuDeCu.Text = "Phiếu " + phieu.MaPhieu;
            lblNCC.Text = phieu.NhaCungCap;
            lblSoHoaDon.Text = phieu.SoHoaDon;
            lblKho.Text = phieu.TenKho;
            lblNhanVien.Text = phieu.TenNhanVien;
            lblNgayNhap.Text = phieu.NgayNhap;
            lblTrangThai.Text = phieu.TrangThai;
            lblLyDo.Text = phieu.LyDo;
            gridHangNhap.ItemsSource = phieu.HangHoa;
            lblSoDong.Text = phieu.HangHoa.Count.ToString() + " dòng hàng";
            decimal soLuong = 0;
            decimal tongTien = 0;
            foreach (HangNhapHienThi dong in phieu.HangHoa)
            {
                soLuong = soLuong + dong.SoLuong;
                tongTien = tongTien + dong.SoLuong * dong.DonGia;
            }
            lblTongSL.Text = "Tổng lượng: " + soLuong.ToString("N3");
            lblTongTien.Text = tongTien.ToString("N0") + " đ";
            if (phieu.TrangThai == "Nháp")
            {
                btnGhiSoCu.Visibility = Visibility.Visible;
                btnGhiSoPhieu.Visibility = Visibility.Visible;
                if (phieu.CanSua) btnSuaNhap.Visibility = Visibility.Visible;
                else btnSuaNhap.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnGhiSoCu.Visibility = Visibility.Collapsed;
                btnGhiSoPhieu.Visibility = Visibility.Collapsed;
                btnSuaNhap.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Khởi tạo dữ liệu và các dòng rỗng để bắt đầu soạn phiếu nhập mới.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dongNhapMoi.Clear</c>, <c>HienDongMoi</c>, <c>HienNhaCungCap</c>, <c>CapNhatNguoiLap</c>.</remarks>
        private void KhoiTaoPhieuMoi()
        {
            maDangXem = "";
            maDangSua = "";
            lblCheDoNhap.Text = "TẠO PHIẾU NHẬP MỚI · MỖI DÒNG MỘT LÔ MỚI";
            dangTai = true;
            lstPhieu.SelectedItem = null;
            dangTai = false;
            formTaoMoi.Visibility = Visibility.Visible;
            panelXemPhieu.Visibility = Visibility.Collapsed;
            btnGhiSoCu.Visibility = Visibility.Collapsed;
            ngayNhapDaTao = null;
            lblNgayNhapMoi.Text = "Tự động lấy khi lưu phiếu mới";
            txtLyDo.Text = "Nhập hàng từ nhà cung cấp";
            txtGhiChu.Text = "";
            txtSoLo.Text = "";
            txtSoLuong.Text = "";
            txtDonGia.Text = "";
            dpHanSuDung.SelectedDate = null;
            dongNhapMoi.Clear();
            HienDongMoi();
            if (cboHoaDon.Items.Count > 0) cboHoaDon.SelectedIndex = 0;
            HienNhaCungCap();
            CapNhatNguoiLap();
        }

        /// <summary>
        /// Lọc danh mục hàng có thể thêm vào phiếu nhập đang soạn.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ketQua.Add</c>.</remarks>
        /// <param name="maDaChon">Mã đã chọn xác định đối tượng cần xử lý.</param>
        private void LocHangMoi(string? maDaChon = null)
        {
            string nhom = (cboLoaiHangMoi.SelectedItem as ComboBoxItem)?.Tag as string ?? "Thuoc";
            List<LuaChonNhap> ketQua = new List<LuaChonNhap>();
            foreach (LuaChonNhap hang in danhSachHangChon)
            {
                if (hang.NhomHang == nhom) ketQua.Add(hang);
            }
            cboHangMoi.ItemsSource = ketQua;
            if (!string.IsNullOrEmpty(maDaChon)) cboHangMoi.SelectedValue = maDaChon;
            if (cboHangMoi.SelectedIndex < 0 && ketQua.Count > 0) cboHangMoi.SelectedIndex = 0;
        }

        /// <summary>
        /// Phản ánh lựa chọn loại hàng mới vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>LocHangMoi</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void LoaiHangMoi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dangTai || !daTai) return;
            LocHangMoi();
        }

        /// <summary>
        /// Đọc giá trị số từ ô nhập theo định dạng được biểu mẫu chấp nhận.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>decimal.TryParse</c>.</remarks>
        /// <param name="text">Giá trị nội dung dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="so">Giá trị số dùng khi thực hiện thao tác; kiểu decimal.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        private static bool DocSo(string text, out decimal so)
        {
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out so)) return true;
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out so);
        }

        /// <summary>
        /// Xử lý thao tác thêm dòng khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>txtSoLo.Text.Trim</c>, <c>DocSo</c>, <c>txtSoLuong.Text.Trim</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void ThemDong_Click(object sender, RoutedEventArgs e)
        {
            string maHH = cboHangMoi.SelectedValue as string;
            if (string.IsNullOrWhiteSpace(maHH))
            {
                MessageBox.Show("Vui lòng chọn vật tư, thuốc hoặc thiết bị.");
                return;
            }
            string soLo = txtSoLo.Text.Trim();
            if (soLo.Length == 0)
            {
                MessageBox.Show("Phải nhập số lô mới cho từng dòng hàng.");
                return;
            }
            decimal soLuong;
            decimal donGia;
            if (!DocSo(txtSoLuong.Text.Trim(), out soLuong) || soLuong <= 0 || decimal.Round(soLuong, 3) != soLuong)
            {
                MessageBox.Show("Số lượng không hợp lệ (tối đa 3 chữ số thập phân).");
                return;
            }
            if (!DocSo(txtDonGia.Text.Trim(), out donGia) || donGia < 0 || decimal.Round(donGia, 2) != donGia)
            {
                MessageBox.Show("Đơn giá không hợp lệ (tối đa 2 chữ số thập phân).");
                return;
            }
            DateTime ngayNhap = ngayNhapDaTao ?? DateTime.Today;
            if (dpHanSuDung.SelectedDate.HasValue && dpHanSuDung.SelectedDate.Value.Date < ngayNhap.Date)
            {
                MessageBox.Show("Hạn sử dụng không được trước ngày nhập.");
                return;
            }
            foreach (DongNhapTam dong in dongNhapMoi)
            {
                if (dong.MaHH == maHH && dong.SoLo.Equals(soLo, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Mặt hàng và số lô này đã được thêm vào phiếu.");
                    return;
                }
            }
            DongNhapTam moi = new DongNhapTam();
            moi.MaHH = maHH;
            moi.TenHang = LayTen(tenHang, maHH, "Chưa rõ hàng hóa");
            moi.SoLo = soLo;
            moi.HanSuDung = dpHanSuDung.SelectedDate;
            moi.SoLuong = soLuong;
            moi.DonGia = donGia;
            dongNhapMoi.Add(moi);
            HienDongMoi();
            txtSoLo.Text = "";
            txtSoLuong.Text = "";
            txtDonGia.Text = "";
            dpHanSuDung.SelectedDate = null;
        }

        /// <summary>
        /// Cập nhật danh sách các dòng nhập mới đang soạn trên biểu mẫu.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>tong.ToString</c>.</remarks>
        private void HienDongMoi()
        {
            gridDongMoi.ItemsSource = null;
            gridDongMoi.ItemsSource = dongNhapMoi;
            decimal tong = 0;
            foreach (DongNhapTam dong in dongNhapMoi) tong = tong + dong.SoLuong * dong.DonGia;
            lblTongTienMoi.Text = "Tổng tiền: " + tong.ToString("N0") + " đ";
        }

        /// <summary>
        /// Xử lý thao tác xóa dòng khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dongNhapMoi.Remove</c>, <c>HienDongMoi</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void XoaDong_Click(object sender, RoutedEventArgs e)
        {
            DongNhapTam dong = gridDongMoi.SelectedItem as DongNhapTam;
            if (dong == null) return;
            dongNhapMoi.Remove(dong);
            HienDongMoi();
        }

        /// <summary>
        /// Kiểm tra và lưu đầu phiếu cùng các dòng hàng; nội dung được ghi theo trạng thái và quyền hiện tại.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>cacDong.Add</c>, <c>dongNhapMoi.Clear</c>, <c>HienDongMoi</c>.</remarks>
        /// <param name="ghiSo">Giá trị ghi sổ dùng khi thực hiện thao tác; kiểu bool.</param>
        private void LuuPhieu(bool ghiSo)
        {
            string maHDN = cboHoaDon.SelectedValue as string;
            string maKho = cboKhoMoi.SelectedValue as string;
            if (string.IsNullOrWhiteSpace(maHDN) || string.IsNullOrWhiteSpace(maKho))
            {
                MessageBox.Show("Chọn hóa đơn và kho nhận.");
                return;
            }
            if (string.IsNullOrWhiteSpace(PhienDangNhap.MaNV))
            {
                MessageBox.Show("Không xác định được tài khoản nhân viên đang đăng nhập.");
                return;
            }
            if (dongNhapMoi.Count == 0)
            {
                MessageBox.Show("Vui lòng thêm ít nhất một mặt hàng có lô mới.");
                return;
            }
            if (ghiSo)
            {
                MessageBoxResult xacNhan = MessageBox.Show(
                    "Ghi sổ sẽ xác nhận phiếu và cộng tồn kho, không thể sửa lại. Bạn đồng ý?",
                    "Ghi sổ phiếu nhập", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (xacNhan != MessageBoxResult.Yes) return;
            }
            List<ET_DONGNHAPMOI> cacDong = new List<ET_DONGNHAPMOI>();
            foreach (DongNhapTam d in dongNhapMoi)
            {
                ET_DONGNHAPMOI dong = new ET_DONGNHAPMOI();
                dong.MaHH = d.MaHH;
                dong.SoLo = d.SoLo;
                dong.HanSuDung = d.HanSuDung;
                dong.SoLuong = d.SoLuong;
                dong.DonGia = d.DonGia;
                cacDong.Add(dong);
            }
            btnLuuNhap.IsEnabled = false;
            btnGhiSoMoi.IsEnabled = false;
            try
            {
                string maPN = "";
                DateTime ngay = maDangSua.Length > 0 && ngayNhapDaTao.HasValue ? ngayNhapDaTao.Value : DateTime.Now;
                if (maDangSua.Length > 0)
                {
                    maPN = maDangSua;
                    new BUS_NHAPKHO().CapNhatPhieu(maPN, maHDN, maKho, PhienDangNhap.MaNV,
                        ngay, txtLyDo.Text, txtGhiChu.Text, cacDong, ghiSo);
                }
                else
                {
                    maPN = new BUS_NHAPKHO().TaoPhieu(maHDN, maKho, PhienDangNhap.MaNV,
                        ngay, txtLyDo.Text, txtGhiChu.Text, cacDong, ghiSo);
                }
                string thaoTac = "lưu nháp";
                if (ghiSo) thaoTac = "ghi sổ";
                MessageBox.Show("Đã " + thaoTac + " phiếu " + maPN + ".",
                    "Nhập kho", MessageBoxButton.OK, MessageBoxImage.Information);
                maDangXem = maPN;
                dongNhapMoi.Clear();
                HienDongMoi();
                TaiDuLieu();
                HienPhieuTheoMa(maPN);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không lưu được phiếu nhập: " + ex.Message,
                    "Nhập kho", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                btnLuuNhap.IsEnabled = true;
                btnGhiSoMoi.IsEnabled = true;
            }
        }

        /// <summary>
        /// Tìm phiếu theo mã vừa xử lý và chọn lại để hiển thị dữ liệu sau khi lưu.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>HienPhieu</c>.</remarks>
        /// <param name="maPN">Mã phiếu nhập xác định đối tượng cần xử lý.</param>
        private void HienPhieuTheoMa(string maPN)
        {
            foreach (PhieuNhapHienThi p in danhSach)
            {
                if (p.MaPhieu == maPN)
                {
                    HienPhieu(p);
                    foreach (object item in lstPhieu.Items)
                    {
                        PhieuNhapHienThi hienThi = item as PhieuNhapHienThi;
                        if (hienThi != null && hienThi.MaPhieu == maPN)
                        {
                            lstPhieu.SelectedItem = item;
                            break;
                        }
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Xử lý thao tác sửa nhập khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>phieu.ThoiGianNhap.ToString</c>, <c>dongNhapMoi.Clear</c>, <c>dongNhapMoi.Add</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void SuaNhap_Click(object sender, RoutedEventArgs e)
        {
            PhieuNhapHienThi phieu = null;
            foreach (PhieuNhapHienThi p in danhSach)
                if (p.MaPhieu == maDangXem) phieu = p;
            if (phieu == null || phieu.TrangThai != "Nháp" || !phieu.CanSua)
            {
                MessageBox.Show("Phiếu này dùng lô cũ hoặc đã ghi sổ, nên không thể sửa theo luồng tạo lô mới.");
                return;
            }
            maDangSua = phieu.MaPhieu;
            lblCheDoNhap.Text = "CHỈNH SỬA PHIẾU NHÁP " + phieu.MaPhieu + " · CẬP NHẬT LÔ MỚI";
            formTaoMoi.Visibility = Visibility.Visible;
            panelXemPhieu.Visibility = Visibility.Collapsed;
            btnGhiSoCu.Visibility = Visibility.Collapsed;
            cboHoaDon.SelectedValue = phieu.MaHDN;
            cboKhoMoi.SelectedValue = phieu.MaKho;
            ngayNhapDaTao = phieu.ThoiGianNhap;
            lblNgayNhapMoi.Text = phieu.ThoiGianNhap.ToString("dd/MM/yyyy HH:mm");
            txtLyDo.Text = phieu.LyDo;
            txtGhiChu.Text = phieu.GhiChu;
            lblNguoiLapMoi.Text = phieu.TenNhanVien;
            dongNhapMoi.Clear();
            foreach (HangNhapHienThi h in phieu.HangHoa)
            {
                DongNhapTam d = new DongNhapTam();
                d.MaHH = h.MaHH;
                d.TenHang = h.TenHang;
                d.SoLo = h.TenLo;
                d.HanSuDung = h.HanSuDung;
                d.SoLuong = h.SoLuong;
                d.DonGia = h.DonGia;
                dongNhapMoi.Add(d);
            }
            HienDongMoi();
        }

        /// <summary>
        /// Xử lý thao tác ghi sổ cũ khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>TaiDuLieu</c>, <c>HienPhieuTheoMa</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void GhiSoCu_Click(object sender, RoutedEventArgs e)
        {
            PhieuNhapHienThi phieu = null;
            foreach (PhieuNhapHienThi p in danhSach)
                if (p.MaPhieu == maDangXem) phieu = p;
            if (phieu == null || phieu.TrangThai != "Nháp") return;
            MessageBoxResult xacNhan = MessageBox.Show(
                "Xác nhận ghi sổ phiếu " + phieu.MaPhieu + "? Tồn kho sẽ được cộng và không thể sửa phiếu.",
                "Ghi sổ phiếu nhập", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (xacNhan != MessageBoxResult.Yes) return;
            btnGhiSoCu.IsEnabled = false;
            btnGhiSoPhieu.IsEnabled = false;
            try
            {
                new BUS_NHAPKHO().GhiSo(phieu.MaPhieu);
                MessageBox.Show("Đã ghi sổ phiếu " + phieu.MaPhieu + ".", "Nhập kho");
                TaiDuLieu();
                HienPhieuTheoMa(phieu.MaPhieu);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể ghi sổ: " + ex.Message, "Nhập kho", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                btnGhiSoCu.IsEnabled = true;
                btnGhiSoPhieu.IsEnabled = true;
            }
        }

        /// <summary>
        /// Phản ánh lựa chọn chọn phiếu vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>HienPhieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void ChonPhieu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dangTai) return;
            PhieuNhapHienThi p = lstPhieu.SelectedItem as PhieuNhapHienThi;
            if (p != null) HienPhieu(p);
        }
        /// <summary>
        /// Tiếp nhận thay đổi lọc phiếu và cập nhật trạng thái/dữ liệu phụ thuộc.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>LocPhieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void LocPhieu_Changed(object sender, RoutedEventArgs e)
        {
            if (!dangTai && daTai) LocPhieu();
        }
        /// <summary>
        /// Phản ánh lựa chọn hóa đơn vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>HienNhaCungCap</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void HoaDon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!dangTai && daTai) HienNhaCungCap();
        }
        /// <summary>
        /// Xử lý thao tác tạo mới khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>KhoiTaoPhieuMoi</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void TaoMoi_Click(object sender, RoutedEventArgs e)
        {
            KhoiTaoPhieuMoi();
        }
        /// <summary>
        /// Xử lý thao tác lưu nháp khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>LuuPhieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void LuuNhap_Click(object sender, RoutedEventArgs e)
        {
            LuuPhieu(false);
        }
        /// <summary>
        /// Xử lý thao tác ghi sổ mới khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>LuuPhieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void GhiSoMoi_Click(object sender, RoutedEventArgs e)
        {
            LuuPhieu(true);
        }
        /// <summary>
        /// Xử lý thao tác tải lại khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>TaiDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void TaiLai_Click(object sender, RoutedEventArgs e)
        {
            TaiDuLieu();
        }
    }
}
