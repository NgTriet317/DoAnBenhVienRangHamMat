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
    public class PhieuNhapHienThi
    {
        public string MaPhieu { get; set; } = "";
        public string MaKho { get; set; } = "";
        public string MaHDN { get; set; } = "";
        public string GhiChu { get; set; } = "";
        public bool CanSua { get; set; } = true;
        public string NhaCungCap { get; set; } = "";
        public string SoHoaDon { get; set; } = "";
        public string TenKho { get; set; } = "";
        public string TenNhanVien { get; set; } = "";
        public string NgayNhap { get; set; } = "";
        public DateTime ThoiGianNhap { get; set; }
        public string TrangThai { get; set; } = "";
        public string LyDo { get; set; } = "";
        public List<HangNhapHienThi> HangHoa { get; set; } = new List<HangNhapHienThi>();
    }

    public class HangNhapHienThi
    {
        public string MaHH { get; set; } = "";
        public DateTime? HanSuDung { get; set; }
        public string TrangThaiLo { get; set; } = "";
        public string TenHang { get; set; } = "";
        public string TenLo { get; set; } = "";
        public string DonViTinh { get; set; } = "";
        public decimal SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string SoLuongHienThi { get { return SoLuong.ToString("N3"); } }
        public string DonGiaHienThi { get { return DonGia.ToString("N0") + " đ"; } }
        public string ThanhTienHienThi { get { return (SoLuong * DonGia).ToString("N0") + " đ"; } }
    }

    public class LuaChonNhap
    {
        public string Ma { get; set; } = "";
        public string Ten { get; set; } = "";
        public string NhomHang { get; set; } = "";
    }

    public class DongNhapTam
    {
        public string MaHH { get; set; } = "";
        public string TenHang { get; set; } = "";
        public string SoLo { get; set; } = "";
        public DateTime? HanSuDung { get; set; }
        public decimal SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string HanSuDungHienThi { get { if (HanSuDung.HasValue) return HanSuDung.Value.ToString("dd/MM/yyyy"); return "—"; } }
        public string SoLuongHienThi { get { return SoLuong.ToString("N3"); } }
        public string ThanhTienHienThi { get { return (SoLuong * DonGia).ToString("N0") + " đ"; } }
    }

    public partial class NhapKho : UserControl
    {
        private bool dangTai;
        private bool daTai;
        private readonly List<PhieuNhapHienThi> danhSach = new List<PhieuNhapHienThi>();
        private readonly List<DongNhapTam> dongNhapMoi = new List<DongNhapTam>();
        private readonly List<LuaChonNhap> danhSachHangChon = new List<LuaChonNhap>();
        private readonly Dictionary<string, ET_HOADONNHAP> hoaDon = new Dictionary<string, ET_HOADONNHAP>();
        private readonly Dictionary<string, string> nhaCungCap = new Dictionary<string, string>();
        private readonly Dictionary<string, string> tenHang = new Dictionary<string, string>();
        private readonly Dictionary<string, string> donVi = new Dictionary<string, string>();
        private readonly Dictionary<string, string> tenKho = new Dictionary<string, string>();
        private readonly Dictionary<string, string> tenNhanVien = new Dictionary<string, string>();
        private string maDangXem = "";
        private string maDangSua = "";
        private DateTime? ngayNhapDaTao;

        public NhapKho()
        {
            InitializeComponent();
            lblNgayNhapMoi.Text = "Tự động lấy khi lưu phiếu mới";
            txtLyDo.Text = "Nhập hàng từ nhà cung cấp";
            Loaded += NhapKho_Loaded;
        }

        private void NhapKho_Loaded(object sender, RoutedEventArgs e)
        {
            // Mỗi lần mở lại tab sẽ lấy phiếu mới, kể cả phiếu vừa ghi từ phân hệ khác.
            if (!dangTai) TaiDuLieu();
        }

        private static string LayTen(Dictionary<string, string> bang, string? ma, string macDinh)
        {
            if (ma != null && bang.ContainsKey(ma)) return bang[ma];
            return macDinh;
        }

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

        private static int SoSanhPhieu(PhieuNhapHienThi a, PhieuNhapHienThi b)
        {
            return b.ThoiGianNhap.CompareTo(a.ThoiGianNhap);
        }

        private void CapNhatNguoiLap()
        {
            string maNV = PhienDangNhap.MaNV;
            lblNguoiLapMoi.Text = LayTen(tenNhanVien, maNV, "Chưa có nhân viên đăng nhập");
        }

        private void HienNhaCungCap()
        {
            string maHDN = cboHoaDon.SelectedValue as string;
            if (maHDN != null && hoaDon.ContainsKey(maHDN))
                lblNCCMoi.Text = LayTen(nhaCungCap, hoaDon[maHDN].MaNCC, "Không rõ nhà cung cấp");
            else lblNCCMoi.Text = "Chọn hóa đơn";
        }

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

        private void LoaiHangMoi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dangTai || !daTai) return;
            LocHangMoi();
        }

        private static bool DocSo(string text, out decimal so)
        {
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out so)) return true;
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out so);
        }

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

        private void HienDongMoi()
        {
            gridDongMoi.ItemsSource = null;
            gridDongMoi.ItemsSource = dongNhapMoi;
            decimal tong = 0;
            foreach (DongNhapTam dong in dongNhapMoi) tong = tong + dong.SoLuong * dong.DonGia;
            lblTongTienMoi.Text = "Tổng tiền: " + tong.ToString("N0") + " đ";
        }

        private void XoaDong_Click(object sender, RoutedEventArgs e)
        {
            DongNhapTam dong = gridDongMoi.SelectedItem as DongNhapTam;
            if (dong == null) return;
            dongNhapMoi.Remove(dong);
            HienDongMoi();
        }

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

        private void ChonPhieu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dangTai) return;
            PhieuNhapHienThi p = lstPhieu.SelectedItem as PhieuNhapHienThi;
            if (p != null) HienPhieu(p);
        }
        private void LocPhieu_Changed(object sender, RoutedEventArgs e)
        {
            if (!dangTai && daTai) LocPhieu();
        }
        private void HoaDon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!dangTai && daTai) HienNhaCungCap();
        }
        private void TaoMoi_Click(object sender, RoutedEventArgs e)
        {
            KhoiTaoPhieuMoi();
        }
        private void LuuNhap_Click(object sender, RoutedEventArgs e)
        {
            LuuPhieu(false);
        }
        private void GhiSoMoi_Click(object sender, RoutedEventArgs e)
        {
            LuuPhieu(true);
        }
        private void TaiLai_Click(object sender, RoutedEventArgs e)
        {
            TaiDuLieu();
        }
    }
}
