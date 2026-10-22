using BUS;
using ET;
using RangHamMat.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace RangHamMat.Views
{
    // Các lớp này chỉ phục vụ hiển thị. Database vẫn sử dụng mã khóa chính.
    public class VatTuDong
    {
        public string MaHH { get; set; } = "";
        public string TenHang { get; set; } = "";
        public string TenLoai { get; set; } = "";
        public string DonViTinh { get; set; } = "";
        public string NhomHang { get; set; } = "";
        public decimal MucToiThieu { get; set; }
        public decimal Ton { get; set; }
        public bool CoNhapXuat { get; set; }
        public ET_VATTU? VatTu { get; set; }
        public ET_THUOC? Thuoc { get; set; }
        public bool CanBoSung { get { return Ton < MucToiThieu; } }
        public string TonHienThi { get { return Ton.ToString("N3") + " " + DonViTinh; } }
        public string CanhBao
        {
            get
            {
                if (Ton < 0) return "Tồn âm - cần kiểm tra";
                if (CanBoSung) return "Dưới ngưỡng";
                if (!CoNhapXuat) return "Chưa có giao dịch";
                return "Đủ mức";
            }
        }
    }

    public class LoHangDong
    {
        public ET_LOHANGHOA DuLieu { get; set; } = new ET_LOHANGHOA();
        public string TenHang { get; set; } = "";
        public string TenKho { get; set; } = "";
        public string TenNCC { get; set; } = "";
        public string DonViTinh { get; set; } = "";
        public decimal Ton { get; set; }
        public bool CoGiaoDich { get; set; }
        public string TonHienThi { get { return Ton.ToString("N3") + " " + DonViTinh; } }
        public string HanDungHienThi
        {
            get
            {
                if (DuLieu.HanSuDung.HasValue) return DuLieu.HanSuDung.Value.ToString("dd/MM/yyyy");
                return "Chưa cập nhật";
            }
        }
        public bool SapHetHan
        {
            get
            {
                if (Ton <= 0 || !DuLieu.HanSuDung.HasValue) return false;
                DateTime ngay = DuLieu.HanSuDung.Value.Date;
                return ngay >= DateTime.Today && ngay <= DateTime.Today.AddDays(30);
            }
        }
        public string CanhBao
        {
            get
            {
                if (Ton < 0) return "Tồn âm";
                if (Ton == 0) return "Không còn tồn";
                if (DuLieu.HanSuDung.HasValue)
                {
                    if (DuLieu.HanSuDung.Value.Date < DateTime.Today) return "Đã hết hạn";
                    if (SapHetHan) return "Sắp hết hạn";
                }
                return "Đang theo dõi";
            }
        }
    }

    public class KhoDong
    {
        public ET_KHO DuLieu { get; set; } = new ET_KHO();
        public string TenKho { get { return DuLieu.TenKho; } }
        public string LoaiKho { get { return DuLieu.LoaiKho; } }
        public string TrangThai { get { return DuLieu.TrangThai; } }
        public string NguoiQuanLy { get; set; } = "Chưa xác định";
        public int SoMatHang { get; set; }
    }

    public partial class VatTu : UserControl
    {
        private readonly BUS_VATTU busVatTu = new BUS_VATTU();
        private readonly BUS_THUOC busThuoc = new BUS_THUOC();
        private readonly List<VatTuDong> danhSachVatTu = new List<VatTuDong>();
        private readonly List<LoHangDong> danhSachLo = new List<LoHangDong>();
        private readonly List<KhoDong> danhSachKho = new List<KhoDong>();
        private readonly List<ET_HANGHOA> danhMucHang = new List<ET_HANGHOA>();
        private readonly Dictionary<string, decimal> tonTheoKho = new Dictionary<string, decimal>();
        private readonly Dictionary<string, decimal> tonTheoLo = new Dictionary<string, decimal>();
        private readonly HashSet<string> hangCoGiaoDich = new HashSet<string>();
        private bool dangNap;
        private bool daNap;
        private bool dangSua;
        private bool dangThem;

        public VatTu()
        {
            InitializeComponent();
            Loaded += VatTu_Loaded;
        }

        private void VatTu_Loaded(object sender, RoutedEventArgs e)
        {
            if (!daNap) TaiDuLieu("");
        }

        private string KhoaTon(string maHH, string maKho)
        {
            return maHH + "\u001f" + maKho;
        }

        private void CongTon(string maHH, string maKho, string maLo, decimal soLuong)
        {
            string khoa = KhoaTon(maHH, maKho);
            if (!tonTheoKho.ContainsKey(khoa)) tonTheoKho[khoa] = 0;
            tonTheoKho[khoa] = tonTheoKho[khoa] + soLuong;
            hangCoGiaoDich.Add(khoa);
            if (!string.IsNullOrWhiteSpace(maLo))
            {
                if (!tonTheoLo.ContainsKey(maLo)) tonTheoLo[maLo] = 0;
                tonTheoLo[maLo] = tonTheoLo[maLo] + soLuong;
            }
        }

        private decimal LayTon(string maHH, string maKho)
        {
            if (maKho.Length > 0)
            {
                string khoa = KhoaTon(maHH, maKho);
                if (tonTheoKho.ContainsKey(khoa)) return tonTheoKho[khoa];
                return 0;
            }
            decimal tong = 0;
            foreach (KeyValuePair<string, decimal> item in tonTheoKho)
            {
                if (item.Key.StartsWith(maHH + "\u001f", StringComparison.Ordinal)) tong += item.Value;
            }
            return tong;
        }

        private bool CoGiaoDich(string maHH, string maKho)
        {
            if (maKho.Length > 0) return hangCoGiaoDich.Contains(KhoaTon(maHH, maKho));
            foreach (string khoa in hangCoGiaoDich)
            {
                if (khoa.StartsWith(maHH + "\u001f", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private string TenHang(string maHH)
        {
            foreach (ET_HANGHOA hang in danhMucHang)
            {
                if (hang.MaHH == maHH) return hang.TenHH;
            }
            return "Chưa có tên hàng";
        }

        private string DonVi(string maHH)
        {
            foreach (ET_HANGHOA hang in danhMucHang)
            {
                if (hang.MaHH == maHH) return hang.DonViTinh;
            }
            return "";
        }

        private void TaiDuLieu(string maHangChon)
        {
            try
            {
                dangNap = true;
                daNap = false;
                List<ET_VATTU> vatTus = new List<ET_VATTU>(busVatTu.layToanBoVatTu());
                List<ET_THUOC> thuocs = new List<ET_THUOC>(busThuoc.layToanBoThuoc());
                List<ET_HANGHOA> hangs = new List<ET_HANGHOA>(new BUS_HANGHOA().layToanBoHangHoa());
                List<ET_LOAIHANGHOA> loais = new List<ET_LOAIHANGHOA>(new BUS_LOAIHANGHOA().layToanBoLoaiHangHoa());
                List<ET_LOHANGHOA> los = new List<ET_LOHANGHOA>(new BUS_LOHANGHOA().layToanBoLoHangHoa());
                List<ET_KHO> khos = new List<ET_KHO>(new BUS_KHO().layToanBoKho());
                List<ET_NHACUNGCAP> nhaCungCaps = new List<ET_NHACUNGCAP>(new BUS_NHACUNGCAP().layToanBoNhaCungCap());
                List<ET_NHANVIEN> nhanViens = new List<ET_NHANVIEN>(new BUS_NHANVIEN().layToanBoNhanVien());
                List<ET_PHIEUNHAP> phieuNhaps = new List<ET_PHIEUNHAP>(new BUS_PHIEUNHAP().layToanBoPhieuNhap());
                List<ET_CHITIETPHIEUNHAP> chiTietNhaps = new List<ET_CHITIETPHIEUNHAP>(new BUS_CHITIETPHIEUNHAP().layToanBoChiTietPhieuNhap());
                List<ET_PHIEUXUAT> phieuXuats = new List<ET_PHIEUXUAT>(new BUS_PHIEUXUAT().LayDanhSach());
                List<ET_CHITIETPHIEUXUAT> chiTietXuats = new List<ET_CHITIETPHIEUXUAT>(new BUS_CHITIETPHIEUXUAT().LayDanhSach());

                danhMucHang.Clear();
                danhMucHang.AddRange(hangs);
                tonTheoKho.Clear();
                tonTheoLo.Clear();
                hangCoGiaoDich.Clear();

                // Chỉ phiếu Đã ghi sổ mới được cộng/trừ số tồn.
                foreach (ET_PHIEUNHAP phieu in phieuNhaps)
                {
                    if (phieu.TrangThai != "Đã ghi sổ") continue;
                    foreach (ET_CHITIETPHIEUNHAP ct in chiTietNhaps)
                    {
                        if (ct.MaPN == phieu.MaPN) CongTon(ct.MaHH, phieu.MaKho, ct.MaLo ?? "", ct.SoLuong);
                    }
                }
                foreach (ET_PHIEUXUAT phieu in phieuXuats)
                {
                    if (phieu.TrangThai != "Đã ghi sổ") continue;
                    foreach (ET_CHITIETPHIEUXUAT ct in chiTietXuats)
                    {
                        if (ct.MaPX == phieu.MaPX) CongTon(ct.MaHH, phieu.MaKho, ct.MaLo ?? "", -ct.SoLuong);
                    }
                }

                danhSachVatTu.Clear();
                foreach (ET_VATTU vt in vatTus)
                {
                    VatTuDong dong = new VatTuDong();
                    dong.MaHH = vt.MaHH;
                    dong.TenHang = TenHang(vt.MaHH);
                    dong.DonViTinh = DonVi(vt.MaHH);
                    dong.NhomHang = "Vật tư";
                    dong.MucToiThieu = vt.MucTonToiThieu;
                    dong.VatTu = vt;
                    foreach (ET_HANGHOA hang in hangs)
                    {
                        if (hang.MaHH != vt.MaHH) continue;
                        foreach (ET_LOAIHANGHOA loai in loais)
                        {
                            if (loai.MaLoaiHH == hang.MaLoaiHH) dong.TenLoai = loai.TenLoaiHH;
                        }
                    }
                    danhSachVatTu.Add(dong);
                }
                foreach (ET_THUOC thuoc in thuocs)
                {
                    VatTuDong dong = new VatTuDong();
                    dong.MaHH = thuoc.MaHH;
                    dong.TenHang = TenHang(thuoc.MaHH);
                    dong.DonViTinh = DonVi(thuoc.MaHH);
                    dong.NhomHang = "Thuốc";
                    dong.MucToiThieu = thuoc.MucTonToiThieu;
                    dong.Thuoc = thuoc;
                    foreach (ET_HANGHOA hang in hangs)
                    {
                        if (hang.MaHH != thuoc.MaHH) continue;
                        foreach (ET_LOAIHANGHOA loai in loais)
                        {
                            if (loai.MaLoaiHH == hang.MaLoaiHH) dong.TenLoai = loai.TenLoaiHH;
                        }
                    }
                    danhSachVatTu.Add(dong);
                }

                danhSachLo.Clear();
                foreach (ET_LOHANGHOA lo in los)
                {
                    bool phuHop = false;
                    foreach (VatTuDong dong in danhSachVatTu)
                    {
                        if (dong.MaHH == lo.MaHH) phuHop = true;
                    }
                    if (!phuHop) continue;
                    LoHangDong dongLo = new LoHangDong();
                    dongLo.DuLieu = lo;
                    dongLo.TenHang = TenHang(lo.MaHH);
                    dongLo.DonViTinh = DonVi(lo.MaHH);
                    if (tonTheoLo.ContainsKey(lo.MaLo))
                    {
                        dongLo.Ton = tonTheoLo[lo.MaLo];
                        dongLo.CoGiaoDich = true;
                    }
                    dongLo.TenKho = "Chưa xác định";
                    foreach (ET_KHO kho in khos) if (kho.MaKho == lo.MaKho) dongLo.TenKho = kho.TenKho;
                    dongLo.TenNCC = "Chưa xác định";
                    foreach (ET_NHACUNGCAP ncc in nhaCungCaps) if (ncc.MaNCC == lo.MaNCC) dongLo.TenNCC = ncc.TenNCC;
                    danhSachLo.Add(dongLo);
                }

                danhSachKho.Clear();
                foreach (ET_KHO kho in khos)
                {
                    KhoDong dongKho = new KhoDong();
                    dongKho.DuLieu = kho;
                    foreach (ET_NHANVIEN nv in nhanViens)
                    {
                        if (nv.MaNV == kho.MaNV) dongKho.NguoiQuanLy = nv.HoTen;
                    }
                    foreach (VatTuDong vt in danhSachVatTu)
                    {
                        if (LayTon(vt.MaHH, kho.MaKho) != 0) dongKho.SoMatHang++;
                    }
                    danhSachKho.Add(dongKho);
                }

                List<LuaChonDanhMuc> dsKho = new List<LuaChonDanhMuc>();
                dsKho.Add(new LuaChonDanhMuc("", "Tất cả kho"));
                foreach (ET_KHO kho in khos) dsKho.Add(new LuaChonDanhMuc(kho.MaKho, kho.TenKho));
                string maKhoCu = cboKhoLoc.SelectedValue as string ?? "";
                cboKhoLoc.ItemsSource = dsKho;
                cboKhoLoc.SelectedValue = maKhoCu;
                if (cboKhoLoc.SelectedIndex < 0) cboKhoLoc.SelectedIndex = 0;
                if (cboTinhTrangLoc.Items.Count == 0)
                {
                    cboTinhTrangLoc.Items.Add("Tất cả trạng thái");
                    cboTinhTrangLoc.Items.Add("Cần chú ý");
                    cboTinhTrangLoc.Items.Add("Bình thường");
                    cboTinhTrangLoc.Items.Add("Chưa có giao dịch");
                }
                if (cboTinhTrangLoc.SelectedIndex < 0) cboTinhTrangLoc.SelectedIndex = 0;

                List<LuaChonDanhMuc> hangCoTheThem = new List<LuaChonDanhMuc>();
                foreach (ET_HANGHOA hang in hangs)
                {
                    bool daDangKy = false;
                    foreach (VatTuDong dong in danhSachVatTu) if (dong.MaHH == hang.MaHH) daDangKy = true;
                    bool laThietBi = false;
                    foreach (ET_LOAIHANGHOA loai in loais)
                    {
                        if (loai.MaLoaiHH == hang.MaLoaiHH && loai.TenLoaiHH.Contains("thiết bị", StringComparison.CurrentCultureIgnoreCase)) laThietBi = true;
                    }
                    if (!daDangKy && !laThietBi) hangCoTheThem.Add(new LuaChonDanhMuc(hang.MaHH, hang.TenHH));
                }
                foreach (VatTuDong dong in danhSachVatTu) hangCoTheThem.Add(new LuaChonDanhMuc(dong.MaHH, dong.TenHang));
                cboHangHoa.ItemsSource = hangCoTheThem;

                lblTongDanhMuc.Text = danhSachVatTu.Count.ToString();
                lblTongKho.Text = danhSachKho.Count.ToString();
                daNap = true;
                dangNap = false;
                CapNhatSoLieu();
                ApDungBoLoc();

                VatTuDong? chon = null;
                foreach (VatTuDong dong in danhSachVatTu) if (dong.MaHH == maHangChon) chon = dong;
                if (chon == null && danhSachVatTu.Count > 0) chon = danhSachVatTu[0];
                Grid_VAT_TU.SelectedItem = chon;
                if (chon == null) HienChuaChon();
                lblThongBao.Text = "";
            }
            catch (Exception ex)
            {
                dangNap = false;
                daNap = false;
                lblThongBao.Text = "Không tải được dữ liệu kho: " + ex.Message;
                MessageBox.Show(lblThongBao.Text, "Vật tư và tồn kho", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CapNhatSoLieu()
        {
            if (!daNap) return;
            string kho = cboKhoLoc.SelectedValue as string ?? "";
            int canThem = 0;
            foreach (VatTuDong dong in danhSachVatTu)
            {
                if (LayTon(dong.MaHH, kho) < dong.MucToiThieu) canThem++;
            }
            int sapHetHan = 0;
            foreach (LoHangDong dong in danhSachLo)
            {
                if (kho.Length > 0 && dong.DuLieu.MaKho != kho) continue;
                if (dong.SapHetHan || (dong.Ton > 0 && dong.DuLieu.HanSuDung.HasValue && dong.DuLieu.HanSuDung.Value.Date < DateTime.Today)) sapHetHan++;
            }
            lblDuoiNguong.Text = canThem.ToString();
            lblSapHetHan.Text = sapHetHan.ToString();
        }

        private void ApDungBoLoc()
        {
            if (!daNap || dangNap) return;
            string tim = txtTimKiem.Text.Trim();
            string tinhTrang = cboTinhTrangLoc.SelectedItem as string ?? "Tất cả trạng thái";
            string kho = cboKhoLoc.SelectedValue as string ?? "";
            CapNhatSoLieu();
            int tab = tabKho.SelectedIndex;
            if (tab < 0) tab = 0;
            cboTinhTrangLoc.IsEnabled = tab != 2;
            Grid_VAT_TU.Visibility = tab == 0 ? Visibility.Visible : Visibility.Collapsed;
            Grid_LO_HANG_HOA.Visibility = tab == 1 ? Visibility.Visible : Visibility.Collapsed;
            Grid_KHO.Visibility = tab == 2 ? Visibility.Visible : Visibility.Collapsed;
            formVatTu.Visibility = tab == 0 ? Visibility.Visible : Visibility.Collapsed;
            formLo.Visibility = tab == 1 ? Visibility.Visible : Visibility.Collapsed;
            formKho.Visibility = tab == 2 ? Visibility.Visible : Visibility.Collapsed;
            panelThaoTac.Visibility = tab == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (tab == 0)
            {
                lblTenDanhSach.Text = "Danh sách vật tư & thuốc";
                List<VatTuDong> ketQua = new List<VatTuDong>();
                foreach (VatTuDong dong in danhSachVatTu)
                {
                    decimal ton = LayTon(dong.MaHH, kho);
                    bool coGiaoDich = CoGiaoDich(dong.MaHH, kho);
                    dong.Ton = ton;
                    dong.CoNhapXuat = coGiaoDich;
                    if (tinhTrang == "Cần chú ý" && ton >= dong.MucToiThieu) continue;
                    if (tinhTrang == "Bình thường" && ton < dong.MucToiThieu) continue;
                    if (tinhTrang == "Chưa có giao dịch" && coGiaoDich) continue;
                    string tra = dong.TenHang + " " + dong.TenLoai + " " + dong.NhomHang + " " + dong.MaHH;
                    if (tim.Length > 0 && !tra.Contains(tim, StringComparison.CurrentCultureIgnoreCase)) continue;
                    ketQua.Add(dong);
                }
                VatTuDong? chonCu = Grid_VAT_TU.SelectedItem as VatTuDong;
                Grid_VAT_TU.ItemsSource = null;
                Grid_VAT_TU.ItemsSource = ketQua;
                if (chonCu != null && ketQua.Contains(chonCu)) Grid_VAT_TU.SelectedItem = chonCu;
                if (Grid_VAT_TU.SelectedItem == null && ketQua.Count > 0) Grid_VAT_TU.SelectedItem = ketQua[0];
                lblDangHien.Text = ketQua.Count + " mặt hàng";
                lblTrong.Visibility = ketQua.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                if (ketQua.Count == 0) HienChuaChon();
            }
            else if (tab == 1)
            {
                lblTenDanhSach.Text = "Lô hàng và hạn dùng";
                List<LoHangDong> ketQua = new List<LoHangDong>();
                foreach (LoHangDong dong in danhSachLo)
                {
                    if (kho.Length > 0 && dong.DuLieu.MaKho != kho) continue;
                    if (tinhTrang == "Cần chú ý" && dong.CanhBao != "Sắp hết hạn" && dong.CanhBao != "Đã hết hạn" && dong.CanhBao != "Tồn âm") continue;
                    if (tinhTrang == "Bình thường" && dong.CanhBao != "Đang theo dõi") continue;
                    if (tinhTrang == "Chưa có giao dịch" && dong.CoGiaoDich) continue;
                    string tra = dong.TenHang + " " + dong.TenKho + " " + dong.TenNCC + " " + dong.DuLieu.SoLo;
                    if (tim.Length > 0 && !tra.Contains(tim, StringComparison.CurrentCultureIgnoreCase)) continue;
                    ketQua.Add(dong);
                }
                LoHangDong? chonCu = Grid_LO_HANG_HOA.SelectedItem as LoHangDong;
                Grid_LO_HANG_HOA.ItemsSource = null;
                Grid_LO_HANG_HOA.ItemsSource = ketQua;
                if (chonCu != null && ketQua.Contains(chonCu)) Grid_LO_HANG_HOA.SelectedItem = chonCu;
                if (Grid_LO_HANG_HOA.SelectedItem == null && ketQua.Count > 0) Grid_LO_HANG_HOA.SelectedItem = ketQua[0];
                lblDangHien.Text = ketQua.Count + " lô hàng";
                lblTrong.Visibility = ketQua.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                if (ketQua.Count == 0) HienChuaChon();
            }
            else
            {
                lblTenDanhSach.Text = "Danh sách kho";
                List<KhoDong> ketQua = new List<KhoDong>();
                foreach (KhoDong dong in danhSachKho)
                {
                    if (kho.Length > 0 && dong.DuLieu.MaKho != kho) continue;
                    string tra = dong.TenKho + " " + dong.LoaiKho + " " + dong.NguoiQuanLy;
                    if (tim.Length > 0 && !tra.Contains(tim, StringComparison.CurrentCultureIgnoreCase)) continue;
                    ketQua.Add(dong);
                }
                KhoDong? chonCu = Grid_KHO.SelectedItem as KhoDong;
                Grid_KHO.ItemsSource = null;
                Grid_KHO.ItemsSource = ketQua;
                if (chonCu != null && ketQua.Contains(chonCu)) Grid_KHO.SelectedItem = chonCu;
                if (Grid_KHO.SelectedItem == null && ketQua.Count > 0) Grid_KHO.SelectedItem = ketQua[0];
                lblDangHien.Text = ketQua.Count + " kho";
                lblTrong.Visibility = ketQua.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                if (ketQua.Count == 0) HienChuaChon();
            }
        }

        private void BoLoc_Changed(object sender, EventArgs e)
        {
            ApDungBoLoc();
        }

        private void XoaLoc_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem.Clear();
            cboTinhTrangLoc.SelectedIndex = 0;
            cboKhoLoc.SelectedIndex = 0;
            ApDungBoLoc();
        }

        private void TabKho_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != tabKho) return;
            dangSua = false;
            dangThem = false;
            ApDungBoLoc();
        }

        private void DanhSachVatTu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dangNap || !daNap || tabKho.SelectedIndex != 0 || dangSua || dangThem) return;
            VatTuDong? dong = Grid_VAT_TU.SelectedItem as VatTuDong;
            if (dong != null) HienVatTu(dong);
        }

        private void DanhSachLo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dangNap || !daNap || tabKho.SelectedIndex != 1) return;
            LoHangDong? dong = Grid_LO_HANG_HOA.SelectedItem as LoHangDong;
            if (dong == null) return;
            lblCheDo.Text = "HỒ SƠ LÔ HÀNG";
            lblTieuDe.Text = dong.TenHang;
            lblPhuDe.Text = "Số lô: " + (dong.DuLieu.SoLo ?? dong.DuLieu.MaLo);
            lblLoHang.Text = dong.TenHang;
            lblLoKho.Text = dong.TenKho;
            lblLoNCC.Text = dong.TenNCC;
            lblLoHan.Text = dong.HanDungHienThi;
            lblLoTon.Text = dong.TonHienThi;
            lblLoCanhBao.Text = dong.CanhBao;
            lblThongBao.Text = "";
        }

        private void DanhSachKho_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dangNap || !daNap || tabKho.SelectedIndex != 2) return;
            KhoDong? dong = Grid_KHO.SelectedItem as KhoDong;
            if (dong == null) return;
            lblCheDo.Text = "THÔNG TIN KHO";
            lblTieuDe.Text = dong.TenKho;
            lblPhuDe.Text = "Danh sách người phụ trách và tình trạng";
            lblKhoTen.Text = dong.TenKho;
            lblKhoNguoi.Text = dong.NguoiQuanLy;
            lblKhoLoai.Text = dong.LoaiKho;
            lblKhoTrangThai.Text = dong.TrangThai;
            lblKhoTongTon.Text = dong.SoMatHang + " mặt hàng còn tồn trong kho";
            lblThongBao.Text = "";
        }

        private void HienChuaChon()
        {
            lblCheDo.Text = "HỒ SƠ KHO";
            lblTieuDe.Text = "Chưa chọn dữ liệu";
            lblPhuDe.Text = "Chọn một dòng trong danh sách để xem thông tin.";
            btnSua.Visibility = Visibility.Collapsed;
            btnLuu.Visibility = Visibility.Collapsed;
            btnHuy.Visibility = Visibility.Collapsed;
        }

        private void HienVatTu(VatTuDong dong)
        {
            dangSua = false;
            dangThem = false;
            lblCheDo.Text = "HỒ SƠ MẶT HÀNG";
            lblTieuDe.Text = dong.TenHang;
            lblPhuDe.Text = dong.NhomHang + " · " + dong.TenLoai;
            cboHangHoa.SelectedValue = dong.MaHH;
            cboLoaiDangKy.SelectedIndex = dong.Thuoc == null ? 0 : 1;
            txtMucTon.Text = dong.MucToiThieu.ToString("0.###", CultureInfo.CurrentCulture);
            txtHamLuong.Text = "";
            txtGiaBan.Text = "";
            if (dong.Thuoc != null)
            {
                txtHamLuong.Text = dong.Thuoc.HamLuong;
                txtGiaBan.Text = dong.Thuoc.DonGiaBan.ToString("0.##", CultureInfo.CurrentCulture);
            }
            lblTonChiTiet.Text = dong.TonHienThi;
            lblThongBao.Text = "";
            DoiCheDo(false);
        }

        private void DoiCheDo(bool dangChinhSua)
        {
            cboHangHoa.IsEnabled = dangThem;
            cboLoaiDangKy.IsEnabled = dangThem;
            txtMucTon.IsReadOnly = !dangChinhSua;
            txtHamLuong.IsReadOnly = !dangChinhSua;
            txtGiaBan.IsReadOnly = !dangChinhSua;
            btnHuy.Visibility = dangChinhSua ? Visibility.Visible : Visibility.Collapsed;
            btnLuu.Visibility = dangChinhSua ? Visibility.Visible : Visibility.Collapsed;
            btnSua.Visibility = dangChinhSua ? Visibility.Collapsed : Visibility.Visible;
            if (Grid_VAT_TU.SelectedItem == null && !dangThem) btnSua.Visibility = Visibility.Collapsed;
        }

        private void LoaiDangKy_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (formThuoc == null || cboLoaiDangKy == null) return;
            formThuoc.Visibility = cboLoaiDangKy.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Sua_Click(object sender, RoutedEventArgs e)
        {
            if (tabKho.SelectedIndex != 0) return;
            if (Grid_VAT_TU.SelectedItem is not VatTuDong) return;
            dangSua = true;
            dangThem = false;
            lblCheDo.Text = "CHỈNH SỬA MẶT HÀNG";
            DoiCheDo(true);
        }

        private void ThemMoi_Click(object sender, RoutedEventArgs e)
        {
            if (!daNap) return;
            tabKho.SelectedIndex = 0;
            dangThem = true;
            dangSua = false;
            Grid_VAT_TU.SelectedItem = null;
            lblCheDo.Text = "ĐĂNG KÝ MẶT HÀNG MỚI";
            lblTieuDe.Text = "Thêm vật tư / thuốc";
            lblPhuDe.Text = "Chọn hàng hóa đã có trong danh mục để đăng ký";
            cboHangHoa.SelectedIndex = -1;
            cboLoaiDangKy.SelectedIndex = 0;
            txtMucTon.Text = "0";
            txtHamLuong.Clear();
            txtGiaBan.Text = "0";
            lblTonChiTiet.Text = "Chưa có giao dịch";
            lblThongBao.Text = "";
            DoiCheDo(true);
        }

        private void Huy_Click(object sender, RoutedEventArgs e)
        {
            dangSua = false;
            dangThem = false;
            VatTuDong? dong = Grid_VAT_TU.SelectedItem as VatTuDong;
            if (dong != null) HienVatTu(dong);
            else HienChuaChon();
        }

        private bool DocSoKhongAm(string text, out decimal ketQua)
        {
            ketQua = 0;
            if (!decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out ketQua)) return false;
            return ketQua >= 0;
        }

        private void Luu_Click(object sender, RoutedEventArgs e)
        {
            if (!dangSua && !dangThem) return;
            decimal muc;
            if (!DocSoKhongAm(txtMucTon.Text, out muc))
            {
                lblThongBao.Text = "Mức tồn tối thiểu phải là số không âm.";
                return;
            }
            string maHH = cboHangHoa.SelectedValue as string ?? "";
            if (maHH.Length == 0)
            {
                lblThongBao.Text = "Vui lòng chọn tên mặt hàng.";
                return;
            }
            bool laThuoc = cboLoaiDangKy.SelectedIndex == 1;
            decimal giaBan = 0;
            if (laThuoc)
            {
                if (txtHamLuong.Text.Trim().Length == 0)
                {
                    lblThongBao.Text = "Vui lòng nhập hàm lượng thuốc.";
                    return;
                }
                if (!DocSoKhongAm(txtGiaBan.Text, out giaBan))
                {
                    lblThongBao.Text = "Đơn giá bán phải là số không âm.";
                    return;
                }
            }
            try
            {
                if (dangThem)
                {
                    foreach (VatTuDong dong in danhSachVatTu)
                    {
                        if (dong.MaHH == maHH)
                        {
                            lblThongBao.Text = "Hàng hóa này đã được đăng ký. Hãy chọn dòng để chỉnh sửa.";
                            return;
                        }
                    }
                    if (laThuoc)
                    {
                        ET_THUOC thuoc = new ET_THUOC();
                        thuoc.MaHH = maHH;
                        thuoc.MucTonToiThieu = muc;
                        thuoc.HamLuong = txtHamLuong.Text.Trim();
                        thuoc.DonGiaBan = giaBan;
                        busThuoc.themThuoc(thuoc);
                    }
                    else
                    {
                        ET_VATTU vatTu = new ET_VATTU();
                        vatTu.MaHH = maHH;
                        vatTu.MucTonToiThieu = muc;
                        busVatTu.themVatTu(vatTu);
                    }
                }
                else
                {
                    VatTuDong? chon = Grid_VAT_TU.SelectedItem as VatTuDong;
                    if (chon == null) return;
                    if (chon.Thuoc != null)
                    {
                        ET_THUOC cu = chon.Thuoc;
                        ET_THUOC moi = new ET_THUOC(cu.MaThuoc, cu.MaHH, txtHamLuong.Text.Trim(), muc, giaBan);
                        busThuoc.suaThuoc(moi, cu);
                    }
                    else if (chon.VatTu != null)
                    {
                        ET_VATTU cu = chon.VatTu;
                        ET_VATTU moi = new ET_VATTU(cu.MaVT, cu.MaHH, muc);
                        busVatTu.suaVatTu(moi, cu);
                    }
                }
                dangSua = false;
                dangThem = false;
                TaiDuLieu(maHH);
                MessageBox.Show("Đã lưu thông tin mặt hàng.", "Vật tư và tồn kho", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                lblThongBao.Text = "Không thể lưu: " + ex.Message;
            }
        }

        private void TaiLai_Click(object sender, RoutedEventArgs e)
        {
            string ma = "";
            VatTuDong? dong = Grid_VAT_TU.SelectedItem as VatTuDong;
            if (dong != null) ma = dong.MaHH;
            dangSua = false;
            dangThem = false;
            TaiDuLieu(ma);
        }
    }
}
