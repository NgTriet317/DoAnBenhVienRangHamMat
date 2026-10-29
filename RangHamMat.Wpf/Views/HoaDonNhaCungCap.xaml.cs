using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;

namespace RangHamMat.Views
{
    public class HoaDonNccDong
    {
        public ET_HOADONNHAP DuLieu { get; set; } = new ET_HOADONNHAP();
        public string SoHoaDonNCC { get { return DuLieu.SoHoaDonNCC; } }
        public string TenNCC { get; set; } = "";
        public string TenNhanVien { get; set; } = "";
        public string TrangThai { get { return DuLieu.TrangThai; } }
        public string TongTienHienThi { get { return DuLieu.TongTien.ToString("N0") + " đ"; } }
        public string NgayHienThi { get { return DuLieu.NgayHoaDon.ToString("dd/MM/yyyy"); } }
        public int SoPhieuNhap { get; set; }
        public string DanhSachPhieu { get; set; } = "";
    }

    public class ChonNhaCungCap
    {
        public string Ma { get; set; } = "";
        public string Ten { get; set; } = "";
    }

    public partial class HoaDonNhaCungCap : UserControl
    {
        private readonly BUS_HOADONNHAP bus = new BUS_HOADONNHAP();
        private readonly List<HoaDonNccDong> danhSach = new List<HoaDonNccDong>();
        private readonly Dictionary<string, string> tenNcc = new Dictionary<string, string>();
        private readonly Dictionary<string, string> tenNhanVien = new Dictionary<string, string>();
        private readonly CultureInfo dinhDang = CultureInfo.GetCultureInfo("vi-VN");
        private HoaDonNccDong? dangChon;
        private bool dangTai;
        private bool daTai;
        private bool dangTaoMoi = true;

        public HoaDonNhaCungCap()
        {
            InitializeComponent();
            dpNgayHoaDon.SelectedDate = DateTime.Today;
            Loaded += HoaDonNhaCungCap_Loaded;
        }

        private void HoaDonNhaCungCap_Loaded(object sender, RoutedEventArgs e)
        {
            if (!daTai) TaiDuLieu("");
        }

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

        private static string TenTheoMa(Dictionary<string, string> ds, string ma)
        {
            if (ds.ContainsKey(ma)) return ds[ma];
            return "Chưa có tên";
        }

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

        private static int SoSanhHoaDon(HoaDonNccDong a, HoaDonNccDong b)
        {
            return b.DuLieu.NgayHoaDon.CompareTo(a.DuLieu.NgayHoaDon);
        }

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

        private void TaoMoi_Click(object sender, RoutedEventArgs e)
        {
            TaoFormMoi();
        }

        private void LamMoi_Click(object sender, RoutedEventArgs e)
        {
            string ma = "";
            if (dangChon != null) ma = dangChon.DuLieu.MaHDN;
            TaiDuLieu(ma);
        }

        private void BoLoc_Changed(object sender, RoutedEventArgs e)
        {
            if (dangTai || !daTai) return;
            ApDungBoLoc();
        }

        private void HoaDon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dangTai) return;
            if (lstHoaDon.SelectedItem is HoaDonNccDong dong) DienHoaDon(dong);
        }
    }
}
