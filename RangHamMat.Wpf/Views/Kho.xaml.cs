using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;

namespace RangHamMat.Views
{
    // Kho chỉ xem dữ liệu, mọi thao tác nhập / xuất được thực hiện trong NhapXuatVatTu.
    public partial class Kho : UserControl
    {
        public class KhoLuaChon
        {
            public string Ma { get; set; } = "";
            public string Ten { get; set; } = "";
        }

        public class KhoMatHang
        {
            public string MaKho { get; set; } = "";
            public string MaHH { get; set; } = "";
            public string TenHang { get; set; } = "";
            public string LoaiHang { get; set; } = "";
            public string DonViTinh { get; set; } = "";
            public decimal SoLuong { get; set; }
            public decimal MucToiThieu { get; set; }
            public string SoLuongHienThi { get { return SoLuong.ToString("N2"); } }
            public string TrangThaiTon
            {
                get
                {
                    if (SoLuong <= 0) return "Hết hàng";
                    if (MucToiThieu > 0 && SoLuong <= MucToiThieu) return "Sắp hết";
                    return "Còn hàng";
                }
            }
        }

        public class KhoLoHang
        {
            public string MaKho { get; set; } = "";
            public string MaHH { get; set; } = "";
            public string MaLo { get; set; } = "";
            public string SoLo { get; set; } = "";
            public decimal SoLuong { get; set; }
            public DateTime? HanSuDung { get; set; }
            public string SoLuongHienThi { get { return SoLuong.ToString("N2"); } }
            public string HanDungHienThi
            {
                get
                {
                    if (HanSuDung.HasValue) return HanSuDung.Value.ToString("dd/MM/yyyy");
                    return "Không có";
                }
            }
        }

        private readonly List<KhoMatHang> danhSachTon = new List<KhoMatHang>();
        private readonly List<KhoLoHang> danhSachLo = new List<KhoLoHang>();
        private readonly Dictionary<string, ET_KHO> khoTheoMa = new Dictionary<string, ET_KHO>();
        private bool dangTai;
        private bool daTai;

        public Kho()
        {
            InitializeComponent();
            Loaded += Kho_Loaded;
        }

        private void Kho_Loaded(object sender, RoutedEventArgs e)
        {
            if (!daTai) TaiDuLieu();
        }

        private string TaoKhoa(string a, string b)
        {
            return a + "\u001f" + b;
        }

        private string TaoKhoaLo(string a, string b, string c)
        {
            return a + "\u001f" + b + "\u001f" + c;
        }

        private bool DaGhiSo(string trangThai)
        {
            return string.Equals(trangThai, "Đã ghi sổ", StringComparison.OrdinalIgnoreCase);
        }

        private void TaiDuLieu()
        {
            string maKhoCu = "";
            if (cboKho.SelectedValue is string chon) maKhoCu = chon;
            dangTai = true;
            try
            {
                danhSachTon.Clear();
                danhSachLo.Clear();
                khoTheoMa.Clear();

                Dictionary<string, ET_HANGHOA> danhMuc = new Dictionary<string, ET_HANGHOA>();
                foreach (ET_HANGHOA hh in new BUS_HANGHOA().layToanBoHangHoa()) danhMuc[hh.MaHH] = hh;

                Dictionary<string, string> tenLoai = new Dictionary<string, string>();
                foreach (ET_LOAIHANGHOA loai in new BUS_LOAIHANGHOA().layToanBoLoaiHangHoa())
                    tenLoai[loai.MaLoaiHH] = loai.TenLoaiHH;

                Dictionary<string, decimal> mucCanhBao = new Dictionary<string, decimal>();
                foreach (ET_VATTU vt in new BUS_VATTU().layToanBoVatTu()) mucCanhBao[vt.MaHH] = vt.MucTonToiThieu;
                foreach (ET_THUOC thuoc in new BUS_THUOC().layToanBoThuoc()) mucCanhBao[thuoc.MaHH] = thuoc.MucTonToiThieu;

                Dictionary<string, ET_LOHANGHOA> thongTinLo = new Dictionary<string, ET_LOHANGHOA>();
                foreach (ET_LOHANGHOA lo in new BUS_LOHANGHOA().layToanBoLoHangHoa()) thongTinLo[lo.MaLo] = lo;

                foreach (ET_KHO kho in new BUS_KHO().layToanBoKho()) khoTheoMa[kho.MaKho] = kho;

                Dictionary<string, string> khoPhieuNhap = new Dictionary<string, string>();
                foreach (ET_PHIEUNHAP phieu in new BUS_PHIEUNHAP().layToanBoPhieuNhap())
                    if (DaGhiSo(phieu.TrangThai)) khoPhieuNhap[phieu.MaPN] = phieu.MaKho;

                Dictionary<string, string> khoPhieuXuat = new Dictionary<string, string>();
                foreach (ET_PHIEUXUAT phieu in new BUS_PHIEUXUAT().LayDanhSach())
                    if (DaGhiSo(phieu.TrangThai)) khoPhieuXuat[phieu.MaPX] = phieu.MaKho;

                Dictionary<string, decimal> tonHang = new Dictionary<string, decimal>();
                Dictionary<string, decimal> tonLo = new Dictionary<string, decimal>();

                foreach (ET_CHITIETPHIEUNHAP ct in new BUS_CHITIETPHIEUNHAP().layToanBoChiTietPhieuNhap())
                {
                    if (!khoPhieuNhap.ContainsKey(ct.MaPN)) continue;
                    string maKho = khoPhieuNhap[ct.MaPN];
                    string khoa = TaoKhoa(maKho, ct.MaHH);
                    if (!tonHang.ContainsKey(khoa)) tonHang[khoa] = 0;
                    tonHang[khoa] = tonHang[khoa] + ct.SoLuong;
                    if (!string.IsNullOrWhiteSpace(ct.MaLo))
                    {
                        string khoaLo = TaoKhoaLo(maKho, ct.MaHH, ct.MaLo);
                        if (!tonLo.ContainsKey(khoaLo)) tonLo[khoaLo] = 0;
                        tonLo[khoaLo] = tonLo[khoaLo] + ct.SoLuong;
                    }
                }

                foreach (ET_CHITIETPHIEUXUAT ct in new BUS_CHITIETPHIEUXUAT().LayDanhSach())
                {
                    if (!khoPhieuXuat.ContainsKey(ct.MaPX)) continue;
                    string maKho = khoPhieuXuat[ct.MaPX];
                    string khoa = TaoKhoa(maKho, ct.MaHH);
                    if (!tonHang.ContainsKey(khoa)) tonHang[khoa] = 0;
                    tonHang[khoa] = tonHang[khoa] - ct.SoLuong;
                    if (!string.IsNullOrWhiteSpace(ct.MaLo))
                    {
                        string khoaLo = TaoKhoaLo(maKho, ct.MaHH, ct.MaLo);
                        if (!tonLo.ContainsKey(khoaLo)) tonLo[khoaLo] = 0;
                        tonLo[khoaLo] = tonLo[khoaLo] - ct.SoLuong;
                    }
                }

                foreach (KeyValuePair<string, decimal> item in tonHang)
                {
                    string[] tach = item.Key.Split('\u001f');
                    if (tach.Length != 2 || !danhMuc.ContainsKey(tach[1])) continue;
                    if (!mucCanhBao.ContainsKey(tach[1])) continue;
                    ET_HANGHOA hang = danhMuc[tach[1]];
                    KhoMatHang dong = new KhoMatHang();
                    dong.MaKho = tach[0];
                    dong.MaHH = tach[1];
                    dong.TenHang = hang.TenHH;
                    dong.DonViTinh = hang.DonViTinh;
                    dong.LoaiHang = "Khác";
                    if (tenLoai.ContainsKey(hang.MaLoaiHH)) dong.LoaiHang = tenLoai[hang.MaLoaiHH];
                    if (mucCanhBao.ContainsKey(hang.MaHH)) dong.MucToiThieu = mucCanhBao[hang.MaHH];
                    dong.SoLuong = item.Value;
                    danhSachTon.Add(dong);
                }

                foreach (KeyValuePair<string, decimal> item in tonLo)
                {
                    string[] tach = item.Key.Split('\u001f');
                    if (tach.Length != 3 || !thongTinLo.ContainsKey(tach[2])) continue;
                    ET_LOHANGHOA lo = thongTinLo[tach[2]];
                    KhoLoHang dong = new KhoLoHang();
                    dong.MaKho = tach[0];
                    dong.MaHH = tach[1];
                    dong.MaLo = lo.MaLo;
                    dong.SoLo = lo.MaLo;
                    if (!string.IsNullOrWhiteSpace(lo.SoLo)) dong.SoLo = lo.SoLo;
                    dong.HanSuDung = lo.HanSuDung;
                    dong.SoLuong = item.Value;
                    danhSachLo.Add(dong);
                }

                List<KhoLuaChon> dsChon = new List<KhoLuaChon>();
                foreach (ET_KHO k in khoTheoMa.Values) dsChon.Add(new KhoLuaChon { Ma = k.MaKho, Ten = k.TenKho });
                dsChon.Sort(SoSanhKho);
                cboKho.ItemsSource = dsChon;
                cboKho.SelectedValue = maKhoCu;
                if (cboKho.SelectedIndex < 0 && dsChon.Count > 0) cboKho.SelectedIndex = 0;
                daTai = true;
                dangTai = false;
                ApDungBoLoc();
            }
            catch (Exception ex)
            {
                dangTai = false;
                MessageBox.Show("Không tải được dữ liệu kho: " + ex.Message, "Kho vật tư", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static int SoSanhKho(KhoLuaChon a, KhoLuaChon b)
        {
            return string.Compare(a.Ten, b.Ten, StringComparison.CurrentCultureIgnoreCase);
        }

        private void ApDungBoLoc()
        {
            if (!daTai || dangTai) return;
            string maKho = "";
            if (cboKho.SelectedValue is string chon) maKho = chon;
            if (khoTheoMa.ContainsKey(maKho))
            {
                ET_KHO kho = khoTheoMa[maKho];
                lblTenKho.Text = kho.TenKho;
                lblMoTaKho.Text = "Loại kho: " + kho.LoaiKho + "  ·  Trạng thái: " + kho.TrangThai;
            }
            else
            {
                lblTenKho.Text = "Danh sách tồn kho";
                lblMoTaKho.Text = "Chưa có kho";
            }

            string trangThai = "Tất cả tình trạng";
            if (cboTrangThai.SelectedItem is ComboBoxItem muc)
            {
                string? ten = Convert.ToString(muc.Content);
                if (ten != null) trangThai = ten;
            }
            string tim = txtTimKiem.Text.Trim();
            int soMatHang = 0;
            int sapHet = 0;
            List<KhoMatHang> ketQua = new List<KhoMatHang>();
            foreach (KhoMatHang hang in danhSachTon)
            {
                if (hang.MaKho != maKho) continue;
                if (hang.SoLuong > 0) soMatHang++;
                if (hang.TrangThaiTon == "Sắp hết" || hang.TrangThaiTon == "Hết hàng") sapHet++;
                if (trangThai != "Tất cả tình trạng" && hang.TrangThaiTon != trangThai) continue;
                if (tim.Length > 0)
                {
                    string chuoi = hang.TenHang + " " + hang.LoaiHang;
                    if (chuoi.IndexOf(tim, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }
                ketQua.Add(hang);
            }
            lblTongMatHang.Text = soMatHang.ToString();
            lblCanBoSung.Text = sapHet.ToString();
            gridTonKho.ItemsSource = ketQua;
            if (ketQua.Count > 0) gridTonKho.SelectedIndex = 0;
            else HienChiTiet(null);
        }

        private void HienChiTiet(KhoMatHang? hang)
        {
            if (hang == null)
            {
                lblTenHang.Text = "Chưa chọn mặt hàng";
                lblSoLuong.Text = "—";
                lblMucToiThieu.Text = "—";
                gridLoHang.ItemsSource = null;
                return;
            }
            lblTenHang.Text = hang.TenHang;
            lblSoLuong.Text = hang.SoLuong.ToString("N2") + " " + hang.DonViTinh;
            lblMucToiThieu.Text = hang.MucToiThieu.ToString("N2");
            List<KhoLoHang> lo = new List<KhoLoHang>();
            foreach (KhoLoHang dong in danhSachLo)
            {
                if (dong.MaKho == hang.MaKho && dong.MaHH == hang.MaHH && dong.SoLuong > 0) lo.Add(dong);
            }
            gridLoHang.ItemsSource = lo;
        }

        private void Kho_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApDungBoLoc();
        }

        private void TonKho_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!daTai || dangTai) return;
            HienChiTiet(gridTonKho.SelectedItem as KhoMatHang);
        }

        private void BoLoc_Changed(object sender, EventArgs e)
        {
            ApDungBoLoc();
        }

        private void TaiLai_Click(object sender, RoutedEventArgs e)
        {
            TaiDuLieu();
        }
    }
}
