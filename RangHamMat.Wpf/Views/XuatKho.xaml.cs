using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;

namespace RangHamMat.Views
{
    public class PhieuXuatHienThi
    {
        public string MaPhieu { get; set; } = "";
        public string SoPhieu { get; set; } = "";
        public string MaKho { get; set; } = "";
        public string MaNV { get; set; } = "";
        public string? MaKhoa { get; set; }
        public string? MaPhong { get; set; }
        public string? MaLS { get; set; }
        public string TenKho { get; set; } = "";
        public string TenDich { get; set; } = "";
        public string NguoiLap { get; set; } = "";
        public string LyDo { get; set; } = "";
        public string TrangThai { get; set; } = "";
        public string GhiChu { get; set; } = "";
        public bool CoDongDonThuoc { get; set; }
        public DateTime NgayXuat { get; set; }
        public List<HangXuatHienThi> HangHoa { get; set; } = new List<HangXuatHienThi>();
        public string NgayHienThi { get { return NgayXuat.ToString("dd/MM/yyyy"); } }
    }

    public class HangXuatHienThi
    {
        public string MaHH { get; set; } = "";
        public string MaLo { get; set; } = "";
        public string TenHang { get; set; } = "";
        public string TenLo { get; set; } = "";
        public string DonViTinh { get; set; } = "";
        public string GhiChu { get; set; } = "";
        public decimal SoLuong { get; set; }
        public string SoLuongHienThi { get { return SoLuong.ToString("N3"); } }
    }

    public class LuaChonXuat
    {
        public string Ma { get; set; } = "";
        public string Ten { get; set; } = "";
    }

    public class DongXuatTam
    {
        public string MaHH { get; set; } = "";
        public string MaLo { get; set; } = "";
        public string TenHang { get; set; } = "";
        public string TenLo { get; set; } = "";
        public string DonViTinh { get; set; } = "";
        public decimal SoLuong { get; set; }
        public string GhiChu { get; set; } = "";
        public string SoLuongHienThi { get { return SoLuong.ToString("N3"); } }
    }

    public partial class XuatKho : UserControl
    {
        private readonly BUS_XUATKHO busXuat = new BUS_XUATKHO();
        private readonly List<PhieuXuatHienThi> danhSach = new List<PhieuXuatHienThi>();
        private readonly List<DongXuatTam> cacDongMoi = new List<DongXuatTam>();
        private readonly List<ET_TONLOXUAT> tonTheoLo = new List<ET_TONLOXUAT>();
        private readonly Dictionary<string, string> tenKho = new Dictionary<string, string>();
        private readonly Dictionary<string, string> tenNguoi = new Dictionary<string, string>();
        private readonly Dictionary<string, string> tenKhoa = new Dictionary<string, string>();
        private readonly Dictionary<string, string> tenPhong = new Dictionary<string, string>();
        private readonly Dictionary<string, string> tenHang = new Dictionary<string, string>();
        private readonly Dictionary<string, string> donVi = new Dictionary<string, string>();
        private readonly Dictionary<string, string> tenLo = new Dictionary<string, string>();
        private readonly List<LuaChonXuat> cacKhoa = new List<LuaChonXuat>();
        private readonly List<LuaChonXuat> cacPhong = new List<LuaChonXuat>();
        private bool dangTai;
        private bool daTai;
        private bool duocSua;
        private string maDangSua = "";
        private string maKhoDangDung = "";

        public XuatKho()
        {
            InitializeComponent();
            dpNgayXuat.SelectedDate = DateTime.Today;
            Loaded += XuatKho_Loaded;
        }

        private void XuatKho_Loaded(object sender, RoutedEventArgs e)
        {
            if (!dangTai) TaiDuLieu("");
        }

        private static string LayTen(Dictionary<string, string> bang, string? ma, string macDinh)
        {
            if (ma != null && bang.ContainsKey(ma)) return bang[ma];
            return macDinh;
        }

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
                    if (n.TrangThai == "Đang làm việc") luaChonNhanVien.Add(new LuaChonXuat { Ma = n.MaNV, Ten = n.HoTen });
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
                if (cboNguoiLap.SelectedIndex < 0 && luaChonNhanVien.Count > 0) cboNguoiLap.SelectedIndex = 0;

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

        private static int SoSanhPhieu(PhieuXuatHienThi a, PhieuXuatHienThi b)
        {
            return b.NgayXuat.CompareTo(a.NgayXuat);
        }

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
                cboNguoiLap.SelectedIndex = -1;
                cboLoaiNhan.SelectedIndex = 0;
                dpNgayXuat.SelectedDate = DateTime.Today;
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
                if (!string.IsNullOrEmpty(phieu.MaPhong)) cboLoaiNhan.SelectedIndex = 2;
                else if (!string.IsNullOrEmpty(phieu.MaKhoa)) cboLoaiNhan.SelectedIndex = 1;
                else cboLoaiNhan.SelectedIndex = 0;
                CapNhatNoiNhan();
                if (!string.IsNullOrEmpty(phieu.MaPhong)) cboNoiNhan.SelectedValue = phieu.MaPhong;
                if (!string.IsNullOrEmpty(phieu.MaKhoa)) cboNoiNhan.SelectedValue = phieu.MaKhoa;
                dpNgayXuat.SelectedDate = phieu.NgayXuat.Date;
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

        private void BatCheDoSua(bool choPhep)
        {
            duocSua = choPhep;
            cboKhoMoi.IsEnabled = choPhep;
            cboNguoiLap.IsEnabled = choPhep;
            cboLoaiNhan.IsEnabled = choPhep;
            cboNoiNhan.IsEnabled = choPhep && cboLoaiNhan.SelectedIndex > 0;
            dpNgayXuat.IsEnabled = choPhep;
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
            if (cboNguoiLap.SelectedIndex < 0 && cboNguoiLap.Items.Count > 0) cboNguoiLap.SelectedIndex = 0;
            dangTai = false;
            BatCheDoSua(true);
            CapNhatTonKho();
            lblHuongDan.Text = "Mỗi dòng phải chọn lô còn tồn trong kho đã chọn.";
        }

        private void Sua_Click(object sender, RoutedEventArgs e)
        {
            if (lstPhieuXuat.SelectedItem is not PhieuXuatHienThi p) return;
            if (p.TrangThai != "Nháp" || !string.IsNullOrEmpty(p.MaLS) || p.CoDongDonThuoc) return;
            maDangSua = p.MaPhieu;
            BatCheDoSua(true);
            CapNhatTonKho();
            lblHuongDan.Text = "Đang sửa phiếu Nháp. Lưu lại trước khi rời màn hình.";
        }

        private void CapNhatNoiNhan()
        {
            if (cboLoaiNhan.SelectedIndex == 1) cboNoiNhan.ItemsSource = cacKhoa;
            else if (cboLoaiNhan.SelectedIndex == 2) cboNoiNhan.ItemsSource = cacPhong;
            else cboNoiNhan.ItemsSource = new List<LuaChonXuat>();
            cboNoiNhan.IsEnabled = duocSua && cboLoaiNhan.SelectedIndex > 0;
            if (cboLoaiNhan.SelectedIndex > 0 && cboNoiNhan.SelectedIndex < 0 && cboNoiNhan.Items.Count > 0)
                cboNoiNhan.SelectedIndex = 0;
        }

        private void LoaiNhan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboNoiNhan == null || dangTai) return;
            CapNhatNoiNhan();
        }

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

        private void HangMoi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboLoMoi == null || dangTai) return;
            CapNhatLoTheoHang();
        }

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

        private void LoMoi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HienTonLo();
        }

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

        private void XoaDong_Click(object sender, RoutedEventArgs e)
        {
            if (!duocSua) return;
            if (gridDongMoi.SelectedItem is not DongXuatTam dong) return;
            cacDongMoi.Remove(dong);
            HienDanhSachDong();
            HienTonLo();
        }

        private void HienDanhSachDong()
        {
            gridDongMoi.ItemsSource = null;
            gridDongMoi.ItemsSource = new List<DongXuatTam>(cacDongMoi);
            decimal tong = 0;
            foreach (DongXuatTam dong in cacDongMoi) tong += dong.SoLuong;
            lblTongSL.Text = tong.ToString("N3");
        }

        private void LuuNhap_Click(object sender, RoutedEventArgs e)
        {
            LuuPhieu(false);
        }

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

        private void LuuPhieu(bool ghiSo)
        {
            if (!duocSua) return;
            try
            {
                string maKho = cboKhoMoi.SelectedValue as string;
                string maNV = cboNguoiLap.SelectedValue as string;
                string? maKhoa = null;
                string? maPhong = null;
                if (cboLoaiNhan.SelectedIndex == 1) maKhoa = cboNoiNhan.SelectedValue as string;
                if (cboLoaiNhan.SelectedIndex == 2) maPhong = cboNoiNhan.SelectedValue as string;
                if (cboLoaiNhan.SelectedIndex > 0 && string.IsNullOrWhiteSpace(maKhoa) && string.IsNullOrWhiteSpace(maPhong))
                    throw new InvalidOperationException("Hãy chọn khoa/phòng tiếp nhận.");
                if (!dpNgayXuat.SelectedDate.HasValue) throw new InvalidOperationException("Hãy chọn ngày xuất.");
                DateTime ngay = dpNgayXuat.SelectedDate.Value.Date.Add(DateTime.Now.TimeOfDay);
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

        private void ChonPhieu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dangTai) return;
            HienPhieu(lstPhieuXuat.SelectedItem as PhieuXuatHienThi);
        }

        private void BoLoc_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!daTai || dangTai) return;
            ApDungBoLoc();
        }

        private void TimKiem_Changed(object sender, TextChangedEventArgs e)
        {
            if (!daTai || dangTai) return;
            ApDungBoLoc();
        }

        private void TaiLai_Click(object sender, RoutedEventArgs e)
        {
            daTai = false;
            TaiDuLieu("");
        }
    }
}
