using BUS;
using ET;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RangHamMat.Views
{
    // Chỉ là dữ liệu hiển thị. Các mã khóa chính vẫn nằm trong SQL.
    public class SoKhoPhieu
    {
        public string Loai { get; set; } = "";
        public string SoPhieu { get; set; } = "";
        public string MaKho { get; set; } = "";
        public string TenKho { get; set; } = "";
        public string TenNhanVien { get; set; } = "";
        public DateTime Ngay { get; set; }
        public string TrangThai { get; set; } = "";
        public string LyDo { get; set; } = "";
        public string DoiTac { get; set; } = "";
        public string GhiChu { get; set; } = "";
        public List<SoKhoHang> HangHoa { get; set; } = new List<SoKhoHang>();
        public string NgayHienThi { get { return Ngay.ToString("dd/MM/yyyy HH:mm"); } }
    }

    public class SoKhoHang
    {
        public string TenHang { get; set; } = "";
        public string TenLo { get; set; } = "";
        public string DonViTinh { get; set; } = "";
        public decimal SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public bool LaPhieuNhap { get; set; }
        public string DonGiaHienThi { get { if (LaPhieuNhap) return DonGia.ToString("N0"); return "—"; } }
        public string ThanhTienHienThi { get { if (LaPhieuNhap) return (DonGia * SoLuong).ToString("N0"); return "—"; } }
    }

    public class SoKhoLuaChon
    {
        public string Ma { get; set; } = "";
        public string Ten { get; set; } = "";
    }

    public partial class NhapXuatVatTu : UserControl
    {
        private readonly List<SoKhoPhieu> tatCaPhieu = new List<SoKhoPhieu>();
        private bool dangTai;
        private bool daTai;
        private string loaiLoc = "TatCa";

        public NhapXuatVatTu()
        {
            InitializeComponent();
            viewHoaDon.Content = new HoaDonNhaCungCap();
            viewNhap.Content = new NhapKho();
            viewXuat.Content = new XuatKho();
            Loaded += NhapXuatVatTu_Loaded;
        }

        public void ChonCheDo(string cheDo)
        {
            if (cheDo == "HoaDon") tabNghiepVu.SelectedIndex = 1;
            else if (cheDo == "Nhap") tabNghiepVu.SelectedIndex = 2;
            else if (cheDo == "Xuat") tabNghiepVu.SelectedIndex = 3;
            else tabNghiepVu.SelectedIndex = 0;
        }

        // Quay lại sổ chứng từ sẽ đọc lại các phiếu vừa được nhập hoặc ghi sổ.
        private void TabNghiepVu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != tabNghiepVu) return;
            if (tabNghiepVu.SelectedIndex == 0 && daTai && !dangTai) TaiDuLieu();
        }

        private void MoHoaDon_Click(object sender, RoutedEventArgs e)
        {
            ChonCheDo("HoaDon");
        }

        private void MoNhap_Click(object sender, RoutedEventArgs e)
        {
            ChonCheDo("Nhap");
        }

        private void MoXuat_Click(object sender, RoutedEventArgs e)
        {
            ChonCheDo("Xuat");
        }

        private void NhapXuatVatTu_Loaded(object sender, RoutedEventArgs e)
        {
            if (!daTai) TaiDuLieu();
        }

        private string TenTheoMa(Dictionary<string, string> danhSach, string ma, string giaTriMacDinh)
        {
            if (string.IsNullOrWhiteSpace(ma)) return giaTriMacDinh;
            if (danhSach.ContainsKey(ma)) return danhSach[ma];
            return ma;
        }

        private void TaiDuLieu()
        {
            string maDaChon = "";
            string loaiDaChon = "";
            if (lstPhieu.SelectedItem is SoKhoPhieu cu)
            {
                maDaChon = cu.SoPhieu;
                loaiDaChon = cu.Loai;
            }

            dangTai = true;
            try
            {
                Dictionary<string, string> tenKho = new Dictionary<string, string>();
                Dictionary<string, string> tenNhanVien = new Dictionary<string, string>();
                Dictionary<string, string> tenHang = new Dictionary<string, string>();
                Dictionary<string, string> donVi = new Dictionary<string, string>();
                Dictionary<string, string> tenLo = new Dictionary<string, string>();
                Dictionary<string, string> tenKhoa = new Dictionary<string, string>();
                Dictionary<string, string> tenPhong = new Dictionary<string, string>();
                Dictionary<string, string> tenHoaDon = new Dictionary<string, string>();
                Dictionary<string, string> tenNCC = new Dictionary<string, string>();

                foreach (ET_KHO k in new BUS_KHO().layToanBoKho())
                    tenKho[k.MaKho] = k.TenKho;
                foreach (ET_NHANVIEN n in new BUS_NHANVIEN().layToanBoNhanVien())
                    tenNhanVien[n.MaNV] = n.HoTen;
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
                foreach (ET_KHOA k in new BUS_KHOA().layToanBoKhoa())
                    tenKhoa[k.MaKhoa] = k.TenKhoa;
                foreach (ET_PHONG p in new BUS_PHONG().layToanBoPhong())
                    tenPhong[p.MaPhong] = p.TenPhong;
                foreach (ET_NHACUNGCAP n in new BUS_NHACUNGCAP().layToanBoNhaCungCap())
                    tenNCC[n.MaNCC] = n.TenNCC;
                foreach (ET_HOADONNHAP h in new BUS_HOADONNHAP().layToanBoHoaDonNhap())
                {
                    string ncc = TenTheoMa(tenNCC, h.MaNCC, "Chưa xác định");
                    tenHoaDon[h.MaHDN] = ncc + " · " + h.SoHoaDonNCC;
                }

                tatCaPhieu.Clear();
                Dictionary<string, SoKhoPhieu> phieuNhap = new Dictionary<string, SoKhoPhieu>();
                Dictionary<string, SoKhoPhieu> phieuXuat = new Dictionary<string, SoKhoPhieu>();

                foreach (ET_PHIEUNHAP p in new BUS_PHIEUNHAP().layToanBoPhieuNhap())
                {
                    SoKhoPhieu dong = new SoKhoPhieu();
                    dong.Loai = "Nhập kho";
                    dong.SoPhieu = p.MaPN;
                    dong.MaKho = p.MaKho;
                    dong.TenKho = TenTheoMa(tenKho, p.MaKho, "Chưa có tên kho");
                    dong.TenNhanVien = TenTheoMa(tenNhanVien, p.MaNV, "Chưa có tên nhân viên");
                    dong.Ngay = p.NgayNhap;
                    dong.TrangThai = p.TrangThai;
                    dong.LyDo = p.LyDo;
                    dong.GhiChu = p.GhiChu;
                    dong.DoiTac = TenTheoMa(tenHoaDon, p.MaHDN, "Chưa có hóa đơn nhập");
                    phieuNhap[p.MaPN] = dong;
                    tatCaPhieu.Add(dong);
                }
                foreach (ET_PHIEUXUAT p in new BUS_PHIEUXUAT().LayDanhSach())
                {
                    SoKhoPhieu dong = new SoKhoPhieu();
                    dong.Loai = "Xuất kho";
                    dong.SoPhieu = p.MaPX;
                    dong.MaKho = p.MaKho;
                    dong.TenKho = TenTheoMa(tenKho, p.MaKho, "Chưa có tên kho");
                    dong.TenNhanVien = TenTheoMa(tenNhanVien, p.MaNV, "Chưa có tên nhân viên");
                    dong.Ngay = p.NgayXuat;
                    dong.TrangThai = p.TrangThai;
                    dong.LyDo = p.LyDo;
                    dong.GhiChu = p.GhiChu;
                    if (!string.IsNullOrWhiteSpace(p.MaPhong))
                        dong.DoiTac = TenTheoMa(tenPhong, p.MaPhong, "Phòng chưa xác định");
                    else if (!string.IsNullOrWhiteSpace(p.MaKhoa))
                        dong.DoiTac = TenTheoMa(tenKhoa, p.MaKhoa, "Khoa chưa xác định");
                    else if (!string.IsNullOrWhiteSpace(p.MaLS))
                        dong.DoiTac = "Lần khám " + p.MaLS;
                    else dong.DoiTac = "Xuất sử dụng nội bộ";
                    phieuXuat[p.MaPX] = dong;
                    tatCaPhieu.Add(dong);
                }
                foreach (ET_CHITIETPHIEUNHAP c in new BUS_CHITIETPHIEUNHAP().layToanBoChiTietPhieuNhap())
                {
                    if (!phieuNhap.ContainsKey(c.MaPN)) continue;
                    SoKhoHang h = new SoKhoHang();
                    h.TenHang = TenTheoMa(tenHang, c.MaHH, "Chưa có tên mặt hàng");
                    h.TenLo = TenTheoMa(tenLo, c.MaLo, "Không theo lô");
                    h.DonViTinh = TenTheoMa(donVi, c.MaHH, "");
                    h.SoLuong = c.SoLuong;
                    h.DonGia = c.DonGia;
                    h.LaPhieuNhap = true;
                    phieuNhap[c.MaPN].HangHoa.Add(h);
                }
                foreach (ET_CHITIETPHIEUXUAT c in new BUS_CHITIETPHIEUXUAT().LayDanhSach())
                {
                    if (!phieuXuat.ContainsKey(c.MaPX)) continue;
                    SoKhoHang h = new SoKhoHang();
                    h.TenHang = TenTheoMa(tenHang, c.MaHH, "Chưa có tên mặt hàng");
                    h.TenLo = TenTheoMa(tenLo, c.MaLo, "Không theo lô");
                    h.DonViTinh = TenTheoMa(donVi, c.MaHH, "");
                    h.SoLuong = c.SoLuong;
                    h.LaPhieuNhap = false;
                    phieuXuat[c.MaPX].HangHoa.Add(h);
                }
                tatCaPhieu.Sort(SoSanhPhieu);

                string maKhoLoc = "";
                if (cboKho.SelectedValue is string daChon) maKhoLoc = daChon;
                List<SoKhoLuaChon> cacKho = new List<SoKhoLuaChon>();
                cacKho.Add(new SoKhoLuaChon { Ma = "", Ten = "Tất cả kho" });
                foreach (KeyValuePair<string, string> k in tenKho)
                    cacKho.Add(new SoKhoLuaChon { Ma = k.Key, Ten = k.Value });
                cboKho.ItemsSource = cacKho;
                cboKho.SelectedValue = maKhoLoc;
                if (cboKho.SelectedIndex < 0) cboKho.SelectedIndex = 0;
                daTai = true;
                dangTai = false;
                ApDungBoLoc(maDaChon, loaiDaChon);
            }
            catch (Exception ex)
            {
                dangTai = false;
                MessageBox.Show("Không tải được chứng từ kho: " + ex.Message, "Nhập / xuất vật tư", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static int SoSanhPhieu(SoKhoPhieu a, SoKhoPhieu b)
        {
            return b.Ngay.CompareTo(a.Ngay);
        }

        private void ApDungBoLoc(string maDaChon = "", string loaiDaChon = "")
        {
            if (dangTai) return;
            if (lstPhieu.SelectedItem is SoKhoPhieu hienTai && maDaChon.Length == 0)
            {
                maDaChon = hienTai.SoPhieu;
                loaiDaChon = hienTai.Loai;
            }
            string tim = "";
            if (txtTimKiem != null) tim = txtTimKiem.Text.Trim();
            string kho = "";
            if (cboKho.SelectedValue is string maKho) kho = maKho;
            string trangThai = "Tất cả";
            if (cboTrangThai.SelectedItem is ComboBoxItem muc)
                trangThai = muc.Content.ToString();

            List<SoKhoPhieu> ketQua = new List<SoKhoPhieu>();
            foreach (SoKhoPhieu p in tatCaPhieu)
            {
                if (loaiLoc == "Nhap" && p.Loai != "Nhập kho") continue;
                if (loaiLoc == "Xuat" && p.Loai != "Xuất kho") continue;
                if (kho.Length > 0 && p.MaKho != kho) continue;
                if (trangThai != "Tất cả" && !p.TrangThai.Equals(trangThai, StringComparison.OrdinalIgnoreCase)) continue;
                if (tim.Length > 0)
                {
                    bool trung = p.SoPhieu.Contains(tim, StringComparison.OrdinalIgnoreCase)
                        || p.TenKho.Contains(tim, StringComparison.OrdinalIgnoreCase)
                        || p.TenNhanVien.Contains(tim, StringComparison.OrdinalIgnoreCase)
                        || p.DoiTac.Contains(tim, StringComparison.OrdinalIgnoreCase)
                        || p.LyDo.Contains(tim, StringComparison.OrdinalIgnoreCase);
                    if (!trung)
                    {
                        foreach (SoKhoHang h in p.HangHoa)
                        {
                            if (h.TenHang.Contains(tim, StringComparison.OrdinalIgnoreCase))
                            {
                                trung = true;
                                break;
                            }
                        }
                    }
                    if (!trung) continue;
                }
                ketQua.Add(p);
            }

            lstPhieu.ItemsSource = ketQua;
            lblSoPhieu.Text = ketQua.Count.ToString() + " chứng từ / " + tatCaPhieu.Count.ToString() + " tổng số";
            foreach (SoKhoPhieu p in ketQua)
            {
                if (p.SoPhieu == maDaChon && p.Loai == loaiDaChon)
                {
                    lstPhieu.SelectedItem = p;
                    break;
                }
            }
            if (lstPhieu.SelectedItem == null && ketQua.Count > 0) lstPhieu.SelectedIndex = 0;
            if (ketQua.Count == 0) HienPhieu(null);
        }

        private void HienPhieu(SoKhoPhieu? phieu)
        {
            if (phieu == null)
            {
                panelChuaChon.Visibility = Visibility.Visible;
                panelChiTiet.Visibility = Visibility.Collapsed;
                return;
            }
            panelChuaChon.Visibility = Visibility.Collapsed;
            panelChiTiet.Visibility = Visibility.Visible;
            if (phieu.Loai == "Nhập kho") lblTieuDe.Text = "PHIẾU NHẬP KHO";
            else lblTieuDe.Text = "PHIẾU XUẤT KHO";
            lblMaPhieu.Text = "Số phiếu: " + phieu.SoPhieu;
            lblTrangThai.Text = phieu.TrangThai;
            lblKho.Text = phieu.TenKho;
            lblNhanVien.Text = phieu.TenNhanVien;
            lblNgay.Text = phieu.NgayHienThi;
            lblDoiTac.Text = phieu.DoiTac;
            if (string.IsNullOrWhiteSpace(phieu.LyDo)) lblLyDo.Text = "Không ghi lý do";
            else lblLyDo.Text = phieu.LyDo;
            if (string.IsNullOrWhiteSpace(phieu.GhiChu)) lblGhiChu.Text = "Không có ghi chú";
            else lblGhiChu.Text = "Ghi chú: " + phieu.GhiChu;
            lblSoDong.Text = phieu.HangHoa.Count.ToString() + " dòng hàng";
            gridChiTiet.ItemsSource = phieu.HangHoa;
            decimal soLuong = 0;
            decimal thanhTien = 0;
            foreach (SoKhoHang h in phieu.HangHoa)
            {
                soLuong = soLuong + h.SoLuong;
                thanhTien = thanhTien + h.SoLuong * h.DonGia;
            }
            lblTongSoLuong.Text = "Tổng lượng: " + soLuong.ToString("N3");
            if (phieu.Loai == "Nhập kho") lblThanhTien.Text = thanhTien.ToString("N0") + " đ";
            else lblThanhTien.Text = "Phiếu xuất kho";
            if (phieu.Loai == "Nhập kho") vienLoaiPhieu.Background = new SolidColorBrush(Color.FromRgb(24, 126, 101));
            else vienLoaiPhieu.Background = new SolidColorBrush(Color.FromRgb(215, 143, 54));
        }

        private void ChonPhieu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dangTai) return;
            HienPhieu(lstPhieu.SelectedItem as SoKhoPhieu);
        }

        private void BoLoc_Changed(object sender, EventArgs e)
        {
            if (!daTai || dangTai) return;
            ApDungBoLoc();
        }

        private void LocLoai_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton b && b.Tag is string loai) loaiLoc = loai;
            if (daTai && !dangTai) ApDungBoLoc();
        }

        private void TaiLai_Click(object sender, RoutedEventArgs e)
        {
            TaiDuLieu();
        }
    }
}
