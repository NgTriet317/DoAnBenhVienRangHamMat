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
    /// Phần xử lý phiếu xuất hiển thị của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public class PhieuXuatHienThi
    {
        /// <summary>
        /// Mã phiếu tiếp nhận khám.
        /// </summary>
        public string MaPhieu { get; set; } = "";
        /// <summary>
        /// Số phiếu.
        /// </summary>
        public string SoPhieu { get; set; } = "";
        /// <summary>
        /// Mã kho.
        /// </summary>
        public string MaKho { get; set; } = "";
        /// <summary>
        /// Mã nhân viên thực hiện hoặc được gán cho bản ghi.
        /// </summary>
        public string MaNV { get; set; } = "";
        /// <summary>
        /// Mã khóa. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MaKhoa { get; set; }
        /// <summary>
        /// Mã phòng. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MaPhong { get; set; }
        /// <summary>
        /// Mã lần khám liên kết các chứng từ cùng lượt. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MaLS { get; set; }
        /// <summary>
        /// Tên kho.
        /// </summary>
        public string TenKho { get; set; } = "";
        /// <summary>
        /// Tên đích.
        /// </summary>
        public string TenDich { get; set; } = "";
        /// <summary>
        /// Người lập.
        /// </summary>
        public string NguoiLap { get; set; } = "";
        /// <summary>
        /// Lý do.
        /// </summary>
        public string LyDo { get; set; } = "";
        /// <summary>
        /// Trạng thái.
        /// </summary>
        public string TrangThai { get; set; } = "";
        /// <summary>
        /// Nội dung ghi chú bổ sung của bản ghi.
        /// </summary>
        public string GhiChu { get; set; } = "";
        /// <summary>
        /// Có dòng đơn thuốc.
        /// </summary>
        public bool CoDongDonThuoc { get; set; }
        /// <summary>
        /// Ngày xuất.
        /// </summary>
        public DateTime NgayXuat { get; set; }
        /// <summary>
        /// Hàng hóa.
        /// </summary>
        public List<HangXuatHienThi> HangHoa { get; set; } = new List<HangXuatHienThi>();
        /// <summary>
        /// Giá trị Ngày hiển thị; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string NgayHienThi { get { return NgayXuat.ToString("dd/MM/yyyy HH:mm"); } }
    }

    /// <summary>
    /// Phần xử lý hàng xuất hiển thị của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public class HangXuatHienThi
    {
        /// <summary>
        /// Mã hàng hóa.
        /// </summary>
        public string MaHH { get; set; } = "";
        /// <summary>
        /// Mã lô hàng dùng quản lý hạn dùng và tồn kho.
        /// </summary>
        public string MaLo { get; set; } = "";
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
        /// Nội dung ghi chú bổ sung của bản ghi.
        /// </summary>
        public string GhiChu { get; set; } = "";
        /// <summary>
        /// Số lượng của dòng dữ liệu.
        /// </summary>
        public decimal SoLuong { get; set; }
        /// <summary>
        /// Giá trị Số lượng hiển thị; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string SoLuongHienThi { get { return SoLuong.ToString("N3"); } }
    }

    /// <summary>
    /// Phần xử lý lựa chọn xuất của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public class LuaChonXuat
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
    /// Phần xử lý dòng xuất tạm của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public class DongXuatTam
    {
        /// <summary>
        /// Mã hàng hóa.
        /// </summary>
        public string MaHH { get; set; } = "";
        /// <summary>
        /// Mã lô hàng dùng quản lý hạn dùng và tồn kho.
        /// </summary>
        public string MaLo { get; set; } = "";
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
        /// Nội dung ghi chú bổ sung của bản ghi.
        /// </summary>
        public string GhiChu { get; set; } = "";
        /// <summary>
        /// Giá trị Số lượng hiển thị; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string SoLuongHienThi { get { return SoLuong.ToString("N3"); } }
    }

    /// <summary>
    /// Phần xử lý xuất kho của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public partial class XuatKho : UserControl
    {
        /// <summary>
        /// Đối tượng nghiệp vụ xuất kho được màn hình dùng để đọc/ghi dữ liệu.
        /// </summary>
        private readonly BUS_XUATKHO busXuat = new BUS_XUATKHO();
        /// <summary>
        /// Danh sách  dùng trong xuất kho được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly List<PhieuXuatHienThi> danhSach = new List<PhieuXuatHienThi>();
        /// <summary>
        /// Các dòng mới được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly List<DongXuatTam> cacDongMoi = new List<DongXuatTam>();
        /// <summary>
        /// Tồn theo lô được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly List<ET_TONLOXUAT> tonTheoLo = new List<ET_TONLOXUAT>();
        /// <summary>
        /// Tên kho được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, string> tenKho = new Dictionary<string, string>();
        /// <summary>
        /// Tên người được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, string> tenNguoi = new Dictionary<string, string>();
        /// <summary>
        /// Tên khóa được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, string> tenKhoa = new Dictionary<string, string>();
        /// <summary>
        /// Tên phòng được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, string> tenPhong = new Dictionary<string, string>();
        /// <summary>
        /// Tên hàng được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, string> tenHang = new Dictionary<string, string>();
        /// <summary>
        /// Đơn vị được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, string> donVi = new Dictionary<string, string>();
        /// <summary>
        /// Tên lô được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Dictionary<string, string> tenLo = new Dictionary<string, string>();
        /// <summary>
        /// Các khóa được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly List<LuaChonXuat> cacKhoa = new List<LuaChonXuat>();
        /// <summary>
        /// Các phòng được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly List<LuaChonXuat> cacPhong = new List<LuaChonXuat>();
        /// <summary>
        /// Cờ Đang tải; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool dangTai;
        /// <summary>
        /// Cờ Đã tải; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool daTai;
        /// <summary>
        /// Cờ Quyền cho phép sửa bản dữ liệu trong ngữ cảnh hiện tại; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool duocSua;
        /// <summary>
        /// Mã đang sửa được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maDangSua = "";
        /// <summary>
        /// Mã kho đang dụng được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string maKhoDangDung = "";
        /// <summary>
        /// Ngày xuất đã tạo được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private DateTime? ngayXuatDaTao;

        /// <summary>
        /// Khởi tạo xuất kho. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
        /// </summary>
        public XuatKho()
        {
            InitializeComponent();
            lblNgayXuatMoi.Text = "Tự động lấy khi lưu phiếu mới";
            Loaded += XuatKho_Loaded;
        }

        /// <summary>
        /// Nạp dữ liệu khởi đầu khi xuất kho được tải lên giao diện.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>TaiDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void XuatKho_Loaded(object sender, RoutedEventArgs e)
        {
            if (!dangTai) TaiDuLieu("");
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
        /// <remarks>Các bước được phối hợp qua <c>tenKho.Clear</c>, <c>tenNguoi.Clear</c>, <c>tenKhoa.Clear</c>, <c>tenPhong.Clear</c>.</remarks>
        /// <param name="maChon">Mã chọn xác định đối tượng cần xử lý.</param>
        private void TaiDuLieu(string maChon)
        {
            if (dangTai) return;
            dangTai = true;
            try
            {
                if (maChon.Length == 0 && lstPhieuXuat.SelectedItem is PhieuXuatHienThi phieuCu)
                    maChon = phieuCu.MaPhieu;
                string maKhoLoc = cboKhoLoc.SelectedValue as string;
                tenKho.Clear(); tenNguoi.Clear(); tenKhoa.Clear(); tenPhong.Clear();
                tenHang.Clear(); donVi.Clear(); tenLo.Clear();
                cacKhoa.Clear(); cacPhong.Clear();
                List<LuaChonXuat> luaChonKho = new List<LuaChonXuat>();
                List<LuaChonXuat> locKho = new List<LuaChonXuat>();
                List<LuaChonXuat> luaChonNhanVien = new List<LuaChonXuat>();
                locKho.Add(new LuaChonXuat { Ma = "", Ten = "Tất cả kho" });

                foreach (ET_KHO k in new BUS_KHO().layToanBoKho())
                {
                    tenKho[k.MaKho] = k.TenKho;
                    if (k.TrangThai == "Đang hoạt động") luaChonKho.Add(new LuaChonXuat { Ma = k.MaKho, Ten = k.TenKho });
                    locKho.Add(new LuaChonXuat { Ma = k.MaKho, Ten = k.TenKho });
                }
                foreach (ET_NHANVIEN n in new BUS_NHANVIEN().layToanBoNhanVien())
                {
                    tenNguoi[n.MaNV] = n.HoTen;
                    if (n.TrangThai == "Đang làm việc" || n.MaNV == PhienDangNhap.MaNV)
                        luaChonNhanVien.Add(new LuaChonXuat { Ma = n.MaNV, Ten = n.HoTen });
                }
                foreach (ET_KHOA k in new BUS_KHOA().layToanBoKhoa())
                {
                    tenKhoa[k.MaKhoa] = k.TenKhoa;
                    cacKhoa.Add(new LuaChonXuat { Ma = k.MaKhoa, Ten = k.TenKhoa });
                }
                foreach (ET_PHONG p in new BUS_PHONG().layToanBoPhong())
                {
                    tenPhong[p.MaPhong] = p.TenPhong;
                    cacPhong.Add(new LuaChonXuat { Ma = p.MaPhong, Ten = p.TenPhong });
                }
                foreach (ET_HANGHOA h in new BUS_HANGHOA().layToanBoHangHoa())
                {
                    tenHang[h.MaHH] = h.TenHH;
                    donVi[h.MaHH] = h.DonViTinh;
                }
                foreach (ET_LOHANGHOA l in new BUS_LOHANGHOA().layToanBoLoHangHoa())
                {
                    if (string.IsNullOrWhiteSpace(l.SoLo)) tenLo[l.MaLo] = l.MaLo;
                    else tenLo[l.MaLo] = l.SoLo;
                }

                cboKhoLoc.ItemsSource = locKho;
                cboKhoLoc.SelectedValue = maKhoLoc;
                if (cboKhoLoc.SelectedIndex < 0) cboKhoLoc.SelectedIndex = 0;
                cboKhoMoi.ItemsSource = luaChonKho;
                cboNguoiLap.ItemsSource = luaChonNhanVien;
                if (cboKhoMoi.SelectedIndex < 0 && luaChonKho.Count > 0) cboKhoMoi.SelectedIndex = 0;
                if (cboNguoiLap.SelectedIndex < 0 && !string.IsNullOrWhiteSpace(PhienDangNhap.MaNV))
                    cboNguoiLap.SelectedValue = PhienDangNhap.MaNV;

                danhSach.Clear();
                Dictionary<string, PhieuXuatHienThi> theoMa = new Dictionary<string, PhieuXuatHienThi>();
                foreach (ET_PHIEUXUAT p in new BUS_PHIEUXUAT().LayDanhSach())
                {
                    PhieuXuatHienThi dong = new PhieuXuatHienThi();
                    dong.MaPhieu = p.MaPX;
                    dong.SoPhieu = p.MaPX;
                    dong.MaKho = p.MaKho;
                    dong.MaNV = p.MaNV;
                    dong.MaKhoa = p.MaKhoa;
                    dong.MaPhong = p.MaPhong;
                    dong.MaLS = p.MaLS;
                    dong.TenKho = LayTen(tenKho, p.MaKho, "Kho chưa xác định");
                    dong.NguoiLap = LayTen(tenNguoi, p.MaNV, "Chưa xác định");
                    dong.NgayXuat = p.NgayXuat;
                    dong.TrangThai = p.TrangThai;
                    dong.LyDo = p.LyDo;
                    if (p.GhiChu != null) dong.GhiChu = p.GhiChu;
                    if (!string.IsNullOrWhiteSpace(p.MaPhong)) dong.TenDich = LayTen(tenPhong, p.MaPhong, "Phòng chưa xác định");
                    else if (!string.IsNullOrWhiteSpace(p.MaKhoa)) dong.TenDich = LayTen(tenKhoa, p.MaKhoa, "Khoa chưa xác định");
                    else if (!string.IsNullOrWhiteSpace(p.MaLS)) dong.TenDich = "Cấp thuốc lần khám " + p.MaLS;
                    else dong.TenDich = "Sử dụng nội bộ";
                    danhSach.Add(dong);
                    theoMa[p.MaPX] = dong;
                }
                foreach (ET_CHITIETPHIEUXUAT c in new BUS_CHITIETPHIEUXUAT().LayDanhSach())
                {
                    if (!theoMa.ContainsKey(c.MaPX)) continue;
                    HangXuatHienThi dong = new HangXuatHienThi();
                    dong.MaHH = c.MaHH;
                    if (c.MaLo != null) dong.MaLo = c.MaLo;
                    dong.TenHang = LayTen(tenHang, c.MaHH, "Chưa xác định hàng");
                    dong.TenLo = LayTen(tenLo, c.MaLo, "Không theo lô");
                    dong.DonViTinh = LayTen(donVi, c.MaHH, "");
                    dong.SoLuong = c.SoLuong;
                    dong.GhiChu = c.GhiChu;
                    theoMa[c.MaPX].HangHoa.Add(dong);
                    if (c.MaCTDT != null) theoMa[c.MaPX].CoDongDonThuoc = true;
                }
                danhSach.Sort(SoSanhPhieu);
                daTai = true;
                dangTai = false;
                ApDungBoLoc(maChon);
            }
            catch (Exception ex)
            {
                dangTai = false;
                MessageBox.Show("Không tải được phiếu xuất: " + ex.Message, "Xuất kho", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// So sánh hai phiếu để xác định thứ tự hiển thị của danh sách.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>b.NgayXuat.CompareTo</c>.</remarks>
        /// <param name="a">Giá trị a dùng khi thực hiện thao tác; kiểu PhieuXuatHienThi.</param>
        /// <param name="b">Giá trị b dùng khi thực hiện thao tác; kiểu PhieuXuatHienThi.</param>
        /// <returns>Giá trị số được đọc, đếm hoặc tính toán trong hàm.</returns>
        private static int SoSanhPhieu(PhieuXuatHienThi a, PhieuXuatHienThi b)
        {
            return b.NgayXuat.CompareTo(a.NgayXuat);
        }

        /// <summary>
        /// Áp dụng các tiêu chí đang chọn để cập nhật danh sách và trạng thái rỗng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>txtTimLenh.Text.Trim</c>, <c>tt.Content.ToString</c>, <c>chuoi.IndexOf</c>, <c>h.TenHang.IndexOf</c>.</remarks>
        /// <param name="maChon">Mã chọn xác định đối tượng cần xử lý.</param>
        private void ApDungBoLoc(string maChon = "")
        {
            if (!daTai || dangTai) return;
            if (maChon.Length == 0 && lstPhieuXuat.SelectedItem is PhieuXuatHienThi cu) maChon = cu.MaPhieu;
            string tim = txtTimLenh.Text.Trim();
            string maKho = cboKhoLoc.SelectedValue as string;
            string trangThai = "Tất cả";
            if (cboTrangThaiLoc.SelectedItem is ComboBoxItem tt && tt.Content != null) trangThai = tt.Content.ToString();
            List<PhieuXuatHienThi> ketQua = new List<PhieuXuatHienThi>();
            foreach (PhieuXuatHienThi p in danhSach)
            {
                if (!string.IsNullOrEmpty(maKho) && p.MaKho != maKho) continue;
                if (trangThai != "Tất cả" && p.TrangThai != trangThai) continue;
                if (tim.Length > 0)
                {
                    string chuoi = p.SoPhieu + " " + p.TenKho + " " + p.TenDich + " " + p.NguoiLap + " " + p.LyDo;
                    bool khop = chuoi.IndexOf(tim, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!khop)
                    {
                        foreach (HangXuatHienThi h in p.HangHoa)
                        {
                            if (h.TenHang.IndexOf(tim, StringComparison.OrdinalIgnoreCase) >= 0) khop = true;
                        }
                    }
                    if (!khop) continue;
                }
                ketQua.Add(p);
            }
            lstPhieuXuat.ItemsSource = null;
            lstPhieuXuat.ItemsSource = ketQua;
            lblTongPhieu.Text = ketQua.Count.ToString() + " phiếu xuất";
            foreach (PhieuXuatHienThi p in ketQua)
            {
                if (p.MaPhieu == maChon)
                {
                    lstPhieuXuat.SelectedItem = p;
                    break;
                }
            }
            if (lstPhieuXuat.SelectedItem == null && ketQua.Count > 0) lstPhieuXuat.SelectedIndex = 0;
            if (ketQua.Count == 0) HienPhieu(null);
        }

        /// <summary>
        /// Hiển thị đầu phiếu, các dòng hàng và trạng thái cho phép thao tác của phiếu được chọn.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>cacDongMoi.Clear</c>, <c>CapNhatNoiNhan</c>, <c>cacDongMoi.Add</c>, <c>HienDanhSachDong</c>.</remarks>
        /// <param name="phieu">Giá trị phiếu dùng khi thực hiện thao tác; kiểu PhieuXuatHienThi?.</param>
        private void HienPhieu(PhieuXuatHienThi? phieu)
        {
            maDangSua = "";
            duocSua = false;
            dangTai = true;
            cacDongMoi.Clear();
            if (phieu == null)
            {
                lblTieuDe.Text = "Chưa chọn phiếu";
                lblTrangThai.Text = "—";
                cboKhoMoi.SelectedIndex = -1;
                maKhoDangDung = "";
                cboNguoiLap.SelectedValue = PhienDangNhap.MaNV;
                cboLoaiNhan.SelectedIndex = 0;
                ngayXuatDaTao = null;
                lblNgayXuatMoi.Text = "Tự động lấy khi lưu phiếu mới";
                txtLyDoMoi.Text = "";
                txtGhiChuMoi.Text = "";
            }
            else
            {
                lblTieuDe.Text = phieu.MaPhieu;
                lblTrangThai.Text = phieu.TrangThai;
                cboKhoMoi.SelectedValue = phieu.MaKho;
                maKhoDangDung = phieu.MaKho;
                cboNguoiLap.SelectedValue = phieu.MaNV;
                ngayXuatDaTao = phieu.NgayXuat;
                lblNgayXuatMoi.Text = phieu.NgayHienThi;
                if (!string.IsNullOrEmpty(phieu.MaPhong)) cboLoaiNhan.SelectedIndex = 2;
                else if (!string.IsNullOrEmpty(phieu.MaKhoa)) cboLoaiNhan.SelectedIndex = 1;
                else cboLoaiNhan.SelectedIndex = 0;
                CapNhatNoiNhan();
                if (!string.IsNullOrEmpty(phieu.MaPhong)) cboNoiNhan.SelectedValue = phieu.MaPhong;
                if (!string.IsNullOrEmpty(phieu.MaKhoa)) cboNoiNhan.SelectedValue = phieu.MaKhoa;
                txtLyDoMoi.Text = phieu.LyDo;
                txtGhiChuMoi.Text = phieu.GhiChu;
                foreach (HangXuatHienThi h in phieu.HangHoa)
                {
                    cacDongMoi.Add(new DongXuatTam { MaHH = h.MaHH, MaLo = h.MaLo,
                        TenHang = h.TenHang, TenLo = h.TenLo, DonViTinh = h.DonViTinh,
                        SoLuong = h.SoLuong, GhiChu = h.GhiChu });
                }
            }
            dangTai = false;
            HienDanhSachDong();
            BatCheDoSua(false);
            if (phieu != null && phieu.TrangThai == "Nháp" && string.IsNullOrEmpty(phieu.MaLS) && !phieu.CoDongDonThuoc)
            {
                btnSua.IsEnabled = true;
                btnGhiSo.IsEnabled = true;
                lblHuongDan.Text = "Phiếu Nháp: chọn Chỉnh sửa hoặc Ghi sổ sau khi kiểm tra tồn.";
            }
            else if (phieu != null)
            {
                lblHuongDan.Text = "Chứng từ chỉ đọc; phiếu cấp thuốc theo lần khám xử lý tại phân hệ Đơn thuốc.";
            }
        }

        /// <summary>
        /// Bật hoặc tắt các trường và thao tác sửa phiếu theo trạng thái hiện tại.
        /// </summary>
        /// <param name="choPhep">Giá trị chờ phép dùng khi thực hiện thao tác; kiểu bool.</param>
        private void BatCheDoSua(bool choPhep)
        {
            duocSua = choPhep;
            cboKhoMoi.IsEnabled = choPhep;
            cboNguoiLap.IsEnabled = false;
            cboLoaiNhan.IsEnabled = choPhep;
            cboNoiNhan.IsEnabled = choPhep && cboLoaiNhan.SelectedIndex > 0;
            txtLyDoMoi.IsReadOnly = !choPhep;
            txtGhiChuMoi.IsReadOnly = !choPhep;
            cboHangMoi.IsEnabled = choPhep;
            cboLoMoi.IsEnabled = choPhep;
            txtSoLuongMoi.IsReadOnly = !choPhep;
            txtGhiChuDong.IsReadOnly = !choPhep;
            btnThemDong.IsEnabled = choPhep;
            btnXoaDong.IsEnabled = choPhep;
            btnLuuNhap.IsEnabled = choPhep;
            btnGhiSo.IsEnabled = choPhep;
            btnSua.IsEnabled = false;
        }

        /// <summary>
        /// Xử lý thao tác tạo mới khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>HienPhieu</c>, <c>BatCheDoSua</c>, <c>CapNhatTonKho</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void TaoMoi_Click(object sender, RoutedEventArgs e)
        {
            dangTai = true;
            lstPhieuXuat.SelectedItem = null;
            HienPhieu(null);
            maDangSua = "";
            lblTieuDe.Text = "Phiếu xuất mới";
            lblTrangThai.Text = "Chưa lưu";
            txtLyDoMoi.Text = "Cấp phát vật tư nội bộ";
            cboKhoMoi.SelectedIndex = 0;
            maKhoDangDung = cboKhoMoi.SelectedValue as string;
            cboNguoiLap.SelectedValue = PhienDangNhap.MaNV;
            dangTai = false;
            BatCheDoSua(true);
            CapNhatTonKho();
            lblHuongDan.Text = "Mỗi dòng phải chọn lô còn tồn trong kho đã chọn.";
        }

        /// <summary>
        /// Xử lý thao tác sửa khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>BatCheDoSua</c>, <c>CapNhatTonKho</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void Sua_Click(object sender, RoutedEventArgs e)
        {
            if (lstPhieuXuat.SelectedItem is not PhieuXuatHienThi p) return;
            if (p.TrangThai != "Nháp" || !string.IsNullOrEmpty(p.MaLS) || p.CoDongDonThuoc) return;
            maDangSua = p.MaPhieu;
            cboNguoiLap.SelectedValue = PhienDangNhap.MaNV;
            BatCheDoSua(true);
            CapNhatTonKho();
            lblHuongDan.Text = "Đang sửa phiếu Nháp. Lưu lại trước khi rời màn hình.";
        }

        /// <summary>
        /// Cập nhật danh sách đối tượng nhận theo loại nơi nhận đã chọn.
        /// </summary>
        private void CapNhatNoiNhan()
        {
            if (cboLoaiNhan.SelectedIndex == 1) cboNoiNhan.ItemsSource = cacKhoa;
            else if (cboLoaiNhan.SelectedIndex == 2) cboNoiNhan.ItemsSource = cacPhong;
            else cboNoiNhan.ItemsSource = new List<LuaChonXuat>();
            cboNoiNhan.IsEnabled = duocSua && cboLoaiNhan.SelectedIndex > 0;
            if (cboLoaiNhan.SelectedIndex > 0 && cboNoiNhan.SelectedIndex < 0 && cboNoiNhan.Items.Count > 0)
                cboNoiNhan.SelectedIndex = 0;
        }

        /// <summary>
        /// Phản ánh lựa chọn loại nhận vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>CapNhatNoiNhan</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void LoaiNhan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboNoiNhan == null || dangTai) return;
            CapNhatNoiNhan();
        }

        /// <summary>
        /// Phản ánh lựa chọn kho mới vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>CapNhatTonKho</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void KhoMoi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboHangMoi == null || dangTai) return;
            if (!duocSua) return;
            if (cacDongMoi.Count > 0)
            {
                MessageBox.Show("Hãy xóa các dòng hàng trước khi đổi kho.", "Xuất kho", MessageBoxButton.OK, MessageBoxImage.Information);
                dangTai = true;
                cboKhoMoi.SelectedValue = maKhoDangDung;
                dangTai = false;
                return;
            }
            maKhoDangDung = cboKhoMoi.SelectedValue as string;
            CapNhatTonKho();
        }

        /// <summary>
        /// Nạp/cập nhật tồn của kho đang chọn để giới hạn các lựa chọn hàng xuất.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>tonTheoLo.Clear</c>, <c>tonTheoLo.AddRange</c>, <c>busXuat.LayTonTheoLo</c>, <c>MessageBox.Show</c>.</remarks>
        private void CapNhatTonKho()
        {
            tonTheoLo.Clear();
            string maKho = cboKhoMoi.SelectedValue as string;
            if (!string.IsNullOrEmpty(maKho))
            {
                try { tonTheoLo.AddRange(busXuat.LayTonTheoLo(maKho)); }
                catch (Exception ex)
                {
                    MessageBox.Show("Không tải được tồn kho: " + ex.Message, "Xuất kho", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            List<LuaChonXuat> hang = new List<LuaChonXuat>();
            HashSet<string> daCo = new HashSet<string>();
            foreach (ET_TONLOXUAT l in tonTheoLo)
            {
                if (daCo.Add(l.MaHH)) hang.Add(new LuaChonXuat { Ma = l.MaHH, Ten = l.TenHH + " · " + l.DonViTinh });
            }
            cboHangMoi.ItemsSource = hang;
            if (hang.Count > 0) cboHangMoi.SelectedIndex = 0;
            CapNhatLoTheoHang();
        }

        /// <summary>
        /// Phản ánh lựa chọn hàng mới vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>CapNhatLoTheoHang</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void HangMoi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboLoMoi == null || dangTai) return;
            CapNhatLoTheoHang();
        }

        /// <summary>
        /// Đọc các lô phù hợp với mặt hàng đang chọn và cập nhật lựa chọn lô xuất.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>l.HanSuDung.Value.ToString</c>, <c>lo.Add</c>, <c>l.SoLuongTon.ToString</c>, <c>HienTonLo</c>.</remarks>
        private void CapNhatLoTheoHang()
        {
            string maHang = cboHangMoi.SelectedValue as string;
            List<LuaChonXuat> lo = new List<LuaChonXuat>();
            foreach (ET_TONLOXUAT l in tonTheoLo)
            {
                if (l.MaHH != maHang) continue;
                string han = "không có HSD";
                if (l.HanSuDung.HasValue) han = l.HanSuDung.Value.ToString("dd/MM/yyyy");
                lo.Add(new LuaChonXuat { Ma = l.MaLo, Ten = l.SoLo + " · tồn " + l.SoLuongTon.ToString("N3") + " · " + han });
            }
            cboLoMoi.ItemsSource = lo;
            if (lo.Count > 0) cboLoMoi.SelectedIndex = 0;
            HienTonLo();
        }

        /// <summary>
        /// Phản ánh lựa chọn lô mới vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>HienTonLo</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void LoMoi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HienTonLo();
        }

        /// <summary>
        /// Hiển thị tồn hiện tại của lô đang được chọn để hỗ trợ nhập số lượng xuất.
        /// </summary>
        private void HienTonLo()
        {
            if (lblTonLo == null || cboLoMoi == null) return;
            lblTonLo.Text = "Chọn lô để xem tồn khả dụng";
            string maLo = cboLoMoi.SelectedValue as string;
            foreach (ET_TONLOXUAT l in tonTheoLo)
            {
                if (l.MaLo != maLo) continue;
                decimal daThem = 0;
                foreach (DongXuatTam d in cacDongMoi) if (d.MaLo == maLo) daThem += d.SoLuong;
                lblTonLo.Text = "Lô: " + l.SoLo + " · Có thể thêm: " + (l.SoLuongTon - daThem).ToString("N3") + " " + l.DonViTinh;
                break;
            }
        }

        /// <summary>
        /// Xử lý thao tác thêm dòng khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>decimal.TryParse</c>, <c>txtSoLuongMoi.Text.Trim</c>, <c>decimal.Round</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void ThemDong_Click(object sender, RoutedEventArgs e)
        {
            if (!duocSua) return;
            string maHH = cboHangMoi.SelectedValue as string;
            string maLo = cboLoMoi.SelectedValue as string;
            if (string.IsNullOrEmpty(maHH) || string.IsNullOrEmpty(maLo))
            {
                MessageBox.Show("Hãy chọn vật tư và lô còn tồn."); return;
            }
            decimal soLuong;
            NumberStyles kieuSo = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign
                | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
            if (!decimal.TryParse(txtSoLuongMoi.Text.Trim(), kieuSo, CultureInfo.CurrentCulture, out soLuong)
                && !decimal.TryParse(txtSoLuongMoi.Text.Trim(), kieuSo, CultureInfo.InvariantCulture, out soLuong))
            {
                MessageBox.Show("Số lượng không hợp lệ."); return;
            }
            if (soLuong <= 0 || decimal.Round(soLuong, 3) != soLuong)
            {
                MessageBox.Show("Số lượng phải lớn hơn 0 và tối đa 3 chữ số thập phân."); return;
            }
            ET_TONLOXUAT? lo = null;
            foreach (ET_TONLOXUAT item in tonTheoLo) if (item.MaLo == maLo) lo = item;
            if (lo == null || lo.MaHH != maHH)
            {
                MessageBox.Show("Lô không còn hợp lệ. Hãy làm mới tồn kho."); return;
            }
            decimal daThem = 0;
            foreach (DongXuatTam d in cacDongMoi) if (d.MaLo == maLo) daThem += d.SoLuong;
            if (daThem + soLuong > lo.SoLuongTon)
            {
                MessageBox.Show("Số lượng xuất vượt tồn khả dụng của lô."); return;
            }
            DongXuatTam dong = new DongXuatTam();
            dong.MaHH = maHH;
            dong.MaLo = maLo;
            dong.TenHang = lo.TenHH;
            dong.TenLo = lo.SoLo;
            dong.DonViTinh = lo.DonViTinh;
            dong.SoLuong = soLuong;
            dong.GhiChu = txtGhiChuDong.Text.Trim();
            cacDongMoi.Add(dong);
            txtSoLuongMoi.Text = "";
            txtGhiChuDong.Text = "";
            HienDanhSachDong();
            HienTonLo();
        }

        /// <summary>
        /// Xử lý thao tác xóa dòng khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>cacDongMoi.Remove</c>, <c>HienDanhSachDong</c>, <c>HienTonLo</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void XoaDong_Click(object sender, RoutedEventArgs e)
        {
            if (!duocSua) return;
            if (gridDongMoi.SelectedItem is not DongXuatTam dong) return;
            cacDongMoi.Remove(dong);
            HienDanhSachDong();
            HienTonLo();
        }

        /// <summary>
        /// Cập nhật bảng các dòng hàng đang có trong phiếu xuất nháp.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>tong.ToString</c>.</remarks>
        private void HienDanhSachDong()
        {
            gridDongMoi.ItemsSource = null;
            gridDongMoi.ItemsSource = new List<DongXuatTam>(cacDongMoi);
            decimal tong = 0;
            foreach (DongXuatTam dong in cacDongMoi) tong += dong.SoLuong;
            lblTongSL.Text = tong.ToString("N3");
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
        /// Xử lý thao tác ghi sổ khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>busXuat.GhiSo</c>, <c>TaiDuLieu</c>, <c>LuuPhieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void GhiSo_Click(object sender, RoutedEventArgs e)
        {
            if (!duocSua && lstPhieuXuat.SelectedItem is PhieuXuatHienThi p && p.TrangThai == "Nháp"
                && string.IsNullOrEmpty(p.MaLS) && !p.CoDongDonThuoc)
            {
                try
                {
                    if (MessageBox.Show("Ghi sổ phiếu " + p.MaPhieu + " và trừ tồn kho?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                    busXuat.GhiSo(p.MaPhieu);
                    TaiDuLieu(p.MaPhieu);
                    MessageBox.Show("Đã ghi sổ phiếu xuất.", "Xuất kho", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Không ghi sổ được", MessageBoxButton.OK, MessageBoxImage.Warning); }
                return;
            }
            if (!duocSua) return;
            if (MessageBox.Show("Ghi sổ sẽ trừ tồn kho. Xác nhận?", "Xuất kho", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                LuuPhieu(true);
        }

        /// <summary>
        /// Kiểm tra và lưu đầu phiếu cùng các dòng hàng; nội dung được ghi theo trạng thái và quyền hiện tại.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dong.Add</c>, <c>busXuat.LuuPhieu</c>, <c>TaiDuLieu</c>, <c>MessageBox.Show</c>.</remarks>
        /// <param name="ghiSo">Giá trị ghi sổ dùng khi thực hiện thao tác; kiểu bool.</param>
        private void LuuPhieu(bool ghiSo)
        {
            if (!duocSua) return;
            try
            {
                string maKho = cboKhoMoi.SelectedValue as string;
                string maNV = PhienDangNhap.MaNV;
                if (string.IsNullOrWhiteSpace(maNV))
                    throw new InvalidOperationException("Không xác định được nhân viên đang đăng nhập.");
                string? maKhoa = null;
                string? maPhong = null;
                if (cboLoaiNhan.SelectedIndex == 1) maKhoa = cboNoiNhan.SelectedValue as string;
                if (cboLoaiNhan.SelectedIndex == 2) maPhong = cboNoiNhan.SelectedValue as string;
                if (cboLoaiNhan.SelectedIndex > 0 && string.IsNullOrWhiteSpace(maKhoa) && string.IsNullOrWhiteSpace(maPhong))
                    throw new InvalidOperationException("Hãy chọn khoa/phòng tiếp nhận.");
                DateTime ngay = maDangSua.Length > 0 && ngayXuatDaTao.HasValue ? ngayXuatDaTao.Value : DateTime.Now;
                List<ET_DONGXUATMOI> dong = new List<ET_DONGXUATMOI>();
                foreach (DongXuatTam d in cacDongMoi)
                    dong.Add(new ET_DONGXUATMOI { MaHH = d.MaHH, MaLo = d.MaLo, SoLuong = d.SoLuong, GhiChu = d.GhiChu });
                string ma = busXuat.LuuPhieu(maDangSua, maKho, maNV, maKhoa, maPhong,
                    ngay, txtLyDoMoi.Text, txtGhiChuMoi.Text, dong, ghiSo);
                maDangSua = "";
                TaiDuLieu(ma);
                if (ghiSo) MessageBox.Show("Đã ghi sổ phiếu xuất và cập nhật tồn kho.", "Xuất kho", MessageBoxButton.OK, MessageBoxImage.Information);
                else MessageBox.Show("Đã lưu phiếu xuất Nháp, tồn kho chưa thay đổi.", "Xuất kho", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không lưu được phiếu xuất: " + ex.Message, "Xuất kho", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            HienPhieu(lstPhieuXuat.SelectedItem as PhieuXuatHienThi);
        }

        /// <summary>
        /// Tiếp nhận thay đổi bộ lọc và cập nhật trạng thái/dữ liệu phụ thuộc.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ApDungBoLoc</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void BoLoc_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!daTai || dangTai) return;
            ApDungBoLoc();
        }

        /// <summary>
        /// Tiếp nhận thay đổi tìm kiểm và cập nhật trạng thái/dữ liệu phụ thuộc.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ApDungBoLoc</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void TimKiem_Changed(object sender, TextChangedEventArgs e)
        {
            if (!daTai || dangTai) return;
            ApDungBoLoc();
        }

        /// <summary>
        /// Xử lý thao tác tải lại khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>TaiDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void TaiLai_Click(object sender, RoutedEventArgs e)
        {
            daTai = false;
            TaiDuLieu("");
        }
    }
}
