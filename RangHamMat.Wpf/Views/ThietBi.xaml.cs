using BUS;
using ET;
using RangHamMat.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RangHamMat.Views
{
    // Lớp dùng để hiển thị thông tin thiết bị trên DataGrid.
    public class ThietBiDong
    {
        public ET_THIETBI DuLieu { get; set; } = new ET_THIETBI();
        public string TenThietBi { get; set; } = "";
        public string TenPhong { get; set; } = "";
        public string NhaCungCap { get; set; } = "";
        public bool CanChuY { get; set; }
        public bool SapHetBaoHanh { get; set; }

        // Quy đổi tên tình trạng cũ khi hiển thị, không tự sửa bản ghi trong SQL.
        public static string HienThiTinhTrang(string? tinhTrang)
        {
            if (tinhTrang == "Tốt" || tinhTrang == "Bình thường")
                return "Đang hoạt động";
            if (tinhTrang == "Hỏng" || tinhTrang == "Cần bảo trì" || tinhTrang == "Đang bảo trì")
                return "Hư hại";
            if (tinhTrang == "Ngừng sử dụng")
                return "Ngưng sử dụng";
            return string.IsNullOrWhiteSpace(tinhTrang) ? "Chưa cập nhật" : tinhTrang;
        }

        public string SoSerialHienThi
        {
            get { return "Serial: " + DuLieu.SoSerial; }
        }

        public string BaoHanhHienThi
        {
            get
            {
                if (DuLieu.HanBaoHanh.HasValue)
                    return DuLieu.HanBaoHanh.Value.ToString("dd/MM/yyyy");
                return "Chưa cập nhật";
            }
        }

        public string TinhTrang
        {
            get { return HienThiTinhTrang(DuLieu.TinhTrang); }
        }
    }

    public partial class ThietBi : UserControl
    {
        private BUS_THIETBI busThietBi = new BUS_THIETBI();
        private BUS_HANGHOA busHangHoa = new BUS_HANGHOA();
        private BUS_LOAIHANGHOA busLoaiHangHoa = new BUS_LOAIHANGHOA();
        private BUS_PHONG busPhong = new BUS_PHONG();
        private BUS_NHACUNGCAP busNhaCungCap = new BUS_NHACUNGCAP();

        private List<ET_THIETBI> danhSachThietBi = new List<ET_THIETBI>();
        private List<ET_HANGHOA> danhSachHangHoa = new List<ET_HANGHOA>();
        private List<ET_PHONG> danhSachPhong = new List<ET_PHONG>();
        private List<ET_NHACUNGCAP> danhSachNhaCungCap = new List<ET_NHACUNGCAP>();

        private List<ThietBiDong> danhSachHienThi = new List<ThietBiDong>();

        private ET_THIETBI? thietBiDangChon;
        private ET_THIETBI? banGoc;

        private bool dangSua = false;

        private bool daTaiDuLieu = false;

        public ThietBi()
        {
            InitializeComponent();
            Loaded += ThietBi_Loaded;
            DangKySuKienXoaBaoDo();
        }

        private void ThietBi_Loaded(object sender, RoutedEventArgs e)
        {
            if (!daTaiDuLieu)
                LoadDuLieuThietBi();
        }

        // Đọc dữ liệu từ BUS và chuẩn bị dữ liệu cho DataGrid, ComboBox.
        private void LoadDuLieuThietBi(string? maCanChon = null)
        {
            try
            {
                daTaiDuLieu = false;
                danhSachThietBi = new List<ET_THIETBI>(busThietBi.layToanBoThietBi());
                danhSachHangHoa = new List<ET_HANGHOA>(busHangHoa.layToanBoHangHoa());
                danhSachPhong = new List<ET_PHONG>(busPhong.layToanBoPhong());
                danhSachNhaCungCap = new List<ET_NHACUNGCAP>(busNhaCungCap.layToanBoNhaCungCap());
                List<ET_LOAIHANGHOA> danhSachLoai = new List<ET_LOAIHANGHOA>(busLoaiHangHoa.layToanBoLoaiHangHoa());

                LoadDanhMuc(danhSachLoai);
                TaoDanhSachHienThi();

                int soChuaPhanPhong = 0;
                int soSapHetBaoHanh = 0;

                foreach (ThietBiDong dong in danhSachHienThi)
                {
                    if (string.IsNullOrWhiteSpace(dong.DuLieu.MaPhong))
                        soChuaPhanPhong++;
                    if (dong.SapHetBaoHanh)
                        soSapHetBaoHanh++;
                }

                lblTong.Text = danhSachHienThi.Count.ToString();
                lblCanTheoDoi.Text = soChuaPhanPhong.ToString();
                lblSapHetBaoHanh.Text = soSapHetBaoHanh.ToString();

                daTaiDuLieu = true;

                ApDungBoLocThietBi(maCanChon);
                lblThongBao.Text = "";
            }
            catch (Exception ex)
            {
                daTaiDuLieu = false;
                lblThongBao.Text = "Không thể tải danh sách thiết bị: " + ex.Message;
                MessageBox.Show("Lỗi khi tải thiết bị: " + ex.Message,
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDanhMuc(List<ET_LOAIHANGHOA> danhSachLoai)
        {
            //lấy all mã loại của thiết bị
            List<string> maLoaiThietBi = new List<string>();
            foreach (ET_LOAIHANGHOA loai in danhSachLoai)
            {
                if (loai.TenLoaiHH != null && loai.TenLoaiHH.IndexOf("thiết bị", StringComparison.OrdinalIgnoreCase) >= 0)
                    maLoaiThietBi.Add(loai.MaLoaiHH);
            }

            //so sánh hàng hóa để lọc ra hàng hóa nào là thiết bị
            List<LuaChonDanhMuc> hangHoa = new List<LuaChonDanhMuc>();
            foreach (ET_HANGHOA hh in danhSachHangHoa)
            {
                bool laThietBi = maLoaiThietBi.Contains(hh.MaLoaiHH);
                foreach (ET_THIETBI tb in danhSachThietBi)
                {
                    if (tb.MaHH == hh.MaHH)
                        laThietBi = true;
                }
                if (laThietBi)
                    hangHoa.Add(new LuaChonDanhMuc(hh.MaHH, hh.TenHH));
            }

            // Chưa có nhóm hàng "thiết bị" thì cho chọn hàng hóa từ danh mục.
            if (hangHoa.Count == 0)
            {
                foreach (ET_HANGHOA hh in danhSachHangHoa)
                    hangHoa.Add(new LuaChonDanhMuc(hh.MaHH, hh.TenHH));
            }
            cboHangHoa.ItemsSource = hangHoa;

            //lấy phòng fill combobox
            List<LuaChonDanhMuc> phong = new List<LuaChonDanhMuc>();

            phong.Add(new LuaChonDanhMuc("", "Chưa phân phòng"));

            foreach (ET_PHONG p in danhSachPhong)
            {
                phong.Add(new LuaChonDanhMuc(p.MaPhong, p.TenPhong));
            }

            cboPhong.ItemsSource = phong;


            //lấy ncc fill cbbox
            List<LuaChonDanhMuc> nhaCungCap = new List<LuaChonDanhMuc>();

            nhaCungCap.Add(new LuaChonDanhMuc("", "Chưa xác định"));

            foreach (ET_NHACUNGCAP ncc in danhSachNhaCungCap)
            {
                nhaCungCap.Add(new LuaChonDanhMuc(ncc.MaNCC, ncc.TenNCC));
            }
            cboNhaCungCap.ItemsSource = nhaCungCap;

            //lấy ncc fill bộ lọc
            List<LuaChonDanhMuc> phongLoc = new List<LuaChonDanhMuc>();

            phongLoc.Add(new LuaChonDanhMuc("", "Tất cả phòng"));
            phongLoc.Add(new LuaChonDanhMuc("CHUA_PHAN_PHONG", "Chưa phân phòng"));

            foreach (ET_PHONG p in danhSachPhong)
            {
                phongLoc.Add(new LuaChonDanhMuc(p.MaPhong, p.TenPhong));
            }

            cboPhongLoc.ItemsSource = phongLoc;
            cboPhongLoc.SelectedIndex = 0;

            // Bộ lọc tình trạng thiết bị; không dùng trạng thái quản lý để lọc.
            List<string> tinhTrangLoc = new List<string>()
                { "Tất cả tình trạng", "Đang hoạt động", "Hư hại", "Ngưng sử dụng" };
            foreach (ET_THIETBI tb in danhSachThietBi)
            {
                string tinhTrang = ThietBiDong.HienThiTinhTrang(tb.TinhTrang);
                if (!tinhTrangLoc.Contains(tinhTrang))
                    tinhTrangLoc.Add(tinhTrang);
            }
            cboTrangThaiLoc.ItemsSource = tinhTrangLoc;
            cboTrangThaiLoc.SelectedIndex = 0;
        }

        private void TaoDanhSachHienThi()
        {
            danhSachHienThi.Clear();
            foreach (ET_THIETBI tb in danhSachThietBi)
            {
                string tenThietBi = "Thiết bị chưa có tên";
                string tenPhong = "Chưa phân phòng";
                string tenNhaCungCap = "Chưa xác định";

                foreach (ET_HANGHOA hh in danhSachHangHoa)
                {
                    if (hh.MaHH == tb.MaHH)
                    {
                        tenThietBi = hh.TenHH;
                        break;
                    }
                }
                foreach (ET_PHONG p in danhSachPhong)
                {
                    if (p.MaPhong == tb.MaPhong)
                    {
                        tenPhong = p.TenPhong;
                        break;
                    }
                }
                foreach (ET_NHACUNGCAP ncc in danhSachNhaCungCap)
                {
                    if (ncc.MaNCC == tb.MaNCC)
                    {
                        tenNhaCungCap = ncc.TenNCC;
                        break;
                    }
                }

                bool sapHetBaoHanh = false;
                bool hetBaoHanh = false;
                if (tb.HanBaoHanh.HasValue)
                {
                    if (tb.HanBaoHanh.Value.Date < DateTime.Today)
                        hetBaoHanh = true;
                    else if (tb.HanBaoHanh.Value.Date <= DateTime.Today.AddDays(30))
                        sapHetBaoHanh = true;
                }

                ThietBiDong dong = new ThietBiDong();
                dong.DuLieu = tb;
                dong.TenThietBi = tenThietBi;
                dong.TenPhong = tenPhong;
                dong.NhaCungCap = tenNhaCungCap;
                dong.SapHetBaoHanh = sapHetBaoHanh;
                dong.CanChuY = CoVanDe(tb.TinhTrang) || CoVanDe(tb.TrangThai) || sapHetBaoHanh || hetBaoHanh;
                danhSachHienThi.Add(dong);
            }
        }

        private bool CoVanDe(string? noiDung)
        {
            if (string.IsNullOrWhiteSpace(noiDung))
                return false;
            string s = noiDung.ToLower();
            return s.Contains("hỏng") || s.Contains("lỗi") || s.Contains("bảo trì")
                || s.Contains("sửa chữa") || s.Contains("ngừng") || s.Contains("ngưng")
                || s.Contains("hư hại") || s.Contains("chờ xử lý");
        }

        private void ApDungBoLocThietBi(string? maCanChon = null)
        {
            if (!daTaiDuLieu)
                return;

            string tuKhoa = txtTimKiem.Text.Trim();
            string trangThaiLoc = "Tất cả tình trạng";
            string maPhongLoc = "";

            if (cboTrangThaiLoc.SelectedItem is string trangThaiDaChon)
                trangThaiLoc = trangThaiDaChon;
            if (cboPhongLoc.SelectedValue is string phongDaChon)
                maPhongLoc = phongDaChon;

            if (string.IsNullOrEmpty(maCanChon))
            {
                if (dgvThietBi.SelectedItem is ThietBiDong dongDangChon)
                    maCanChon = dongDangChon.DuLieu.MaTB;
            }

            List<ThietBiDong> ketQua = new List<ThietBiDong>();
            foreach (ThietBiDong dong in danhSachHienThi)
            {
                if (trangThaiLoc != "Tất cả tình trạng" && dong.TinhTrang != trangThaiLoc)
                    continue;
                if (maPhongLoc == "CHUA_PHAN_PHONG" && !string.IsNullOrWhiteSpace(dong.DuLieu.MaPhong))
                    continue;
                if (maPhongLoc != "" && maPhongLoc != "CHUA_PHAN_PHONG" && dong.DuLieu.MaPhong != maPhongLoc)
                    continue;

                string chuoiTim = dong.DuLieu.MaTB + " " + dong.TenThietBi + " " + dong.DuLieu.SoSerial
                    + " " + dong.TenPhong + " " + dong.NhaCungCap;
                if (tuKhoa != "" && chuoiTim.IndexOf(tuKhoa, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                ketQua.Add(dong);
            }

            dgvThietBi.ItemsSource = ketQua;
            lblDangHien.Text = ketQua.Count + " thiết bị";

            if (ketQua.Count == 0)
                lblTrong.Visibility = Visibility.Visible;
            else
                lblTrong.Visibility = Visibility.Collapsed;

            if (dangSua)
                return;

            ThietBiDong? canChon = null;
            foreach (ThietBiDong dong in ketQua)
            {
                if (dong.DuLieu.MaTB == maCanChon)
                {
                    canChon = dong;
                    break;
                }
            }
            if (canChon == null && ketQua.Count > 0)
                canChon = ketQua[0];

            dgvThietBi.SelectedItem = canChon;
            if (canChon != null)
                HienChiTiet(canChon.DuLieu, canChon.TenThietBi);
            else
                HienTrangThaiTrong();
        }

        private void BoLoc_Changed(object sender, EventArgs e)
        {
            ApDungBoLocThietBi();
        }

        private void XoaLoc_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem.Clear();
            cboTrangThaiLoc.SelectedIndex = 0;
            cboPhongLoc.SelectedIndex = 0;
            ApDungBoLocThietBi();
        }

        private void DanhSach_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!daTaiDuLieu || dangSua)
                return;

            if (dgvThietBi.SelectedItem is ThietBiDong dong)
                HienChiTiet(dong.DuLieu, dong.TenThietBi);
        }

        private ET_THIETBI SaoChepThietBi(ET_THIETBI tb)
        {
            return new ET_THIETBI(tb.MaTB, tb.MaHH, tb.SoSerial, tb.NgayMua, tb.MaNCC,
                tb.MaPhong, tb.TinhTrang, tb.HanBaoHanh, tb.TrangThai, tb.GhiChu);
        }

        private void HienChiTiet(ET_THIETBI tb, string tenThietBi)
        {
            thietBiDangChon = tb;
            banGoc = SaoChepThietBi(tb);
            dangSua = false;

            lblCheDo.Text = "PHÂN PHÒNG THIẾT BỊ";
            lblTieuDeChiTiet.Text = tenThietBi;
            lblMaThietBi.Text = "Mã hồ sơ: " + tb.MaTB;
            cboHangHoa.SelectedValue = tb.MaHH;
            txtSerial.Text = tb.SoSerial;
            cboNhaCungCap.SelectedValue = tb.MaNCC;
            cboPhong.SelectedValue = tb.MaPhong;
            
            if (string.IsNullOrWhiteSpace(tb.MaNCC))
                cboNhaCungCap.SelectedIndex = 0;
            if (string.IsNullOrWhiteSpace(tb.MaPhong))
                cboPhong.SelectedIndex = 0;

            lblTinhTrang.Text = ThietBiDong.HienThiTinhTrang(tb.TinhTrang);
            dpNgayMua.SelectedDate = tb.NgayMua;
            dpHanBaoHanh.SelectedDate = tb.HanBaoHanh;
            txtGhiChu.Text = tb.GhiChu;
            lblThongBao.Text = "";

            XoaToanBoBaoDo();
            DoiCheDo(false);
        }

        private void HienTrangThaiTrong()
        {
            thietBiDangChon = null;
            banGoc = null;
            dangSua = false;
            lblCheDo.Text = "PHÂN PHÒNG THIẾT BỊ";
            lblTieuDeChiTiet.Text = "Chưa chọn thiết bị";
            lblMaThietBi.Text = "Chọn thiết bị bên trái để phân vào phòng.";
            XoaForm();
            DoiCheDo(false);
        }

        private void XoaForm()
        {
            cboHangHoa.SelectedIndex = -1;
            cboNhaCungCap.SelectedIndex = 0;
            cboPhong.SelectedIndex = 0;
            txtSerial.Clear();
            lblTinhTrang.Text = "Chưa chọn thiết bị";
            dpNgayMua.SelectedDate = null;
            dpHanBaoHanh.SelectedDate = null;
            txtGhiChu.Clear();
            lblThongBao.Text = "";
            XoaToanBoBaoDo();
        }

        private void DoiCheDo(bool choPhepNhap)
        {
            dgvThietBi.IsEnabled = !choPhepNhap;
            cboHangHoa.IsEnabled = false;
            txtSerial.IsReadOnly = true;
            cboNhaCungCap.IsEnabled = false;
            // Thiết bị hư/ngưng vẫn được thu hồi khỏi phòng, nhưng không được phân sang phòng khác.
            bool hoatDong = thietBiDangChon != null &&
                ThietBiDong.HienThiTinhTrang(thietBiDangChon.TinhTrang) == "Đang hoạt động"
                && (thietBiDangChon.TrangThai == "Chờ bàn giao" || thietBiDangChon.TrangThai == "Đang sử dụng"
                    || thietBiDangChon.TrangThai == "Đang hoạt động");
            cboPhong.IsEnabled = choPhepNhap && hoatDong;
            dpNgayMua.IsEnabled = false;
            dpHanBaoHanh.IsEnabled = false;
            txtGhiChu.IsReadOnly = true;

            btnSua.Visibility = Visibility.Collapsed;
            btnLuu.Visibility = Visibility.Collapsed;
            btnHuy.Visibility = Visibility.Collapsed;
            btnHuHai.Visibility = Visibility.Collapsed;
            btnNgungSuDung.Visibility = Visibility.Collapsed;
            if (choPhepNhap)
            {
                btnLuu.Visibility = Visibility.Visible;
                btnHuy.Visibility = Visibility.Visible;
                btnLuu.Content = hoatDong ? "Xác nhận phân phòng" : "Xác nhận thu hồi";
            }
            else if (thietBiDangChon != null)
            {
                if (hoatDong || !string.IsNullOrWhiteSpace(thietBiDangChon.MaPhong))
                    btnSua.Visibility = Visibility.Visible;
                bool daNgung = thietBiDangChon.TinhTrang == "Ngưng sử dụng"
                    || thietBiDangChon.TinhTrang == "Ngừng sử dụng"
                    || thietBiDangChon.TrangThai == "Ngưng sử dụng"
                    || thietBiDangChon.TrangThai == "Ngừng sử dụng";
                if (!daNgung)
                {
                    btnNgungSuDung.Visibility = Visibility.Visible;
                    if (ThietBiDong.HienThiTinhTrang(thietBiDangChon.TinhTrang) != "Hư hại")
                        btnHuHai.Visibility = Visibility.Visible;
                }
            }
        }

        private void PhanPhong_Click(object sender, RoutedEventArgs e)
        {
            if (thietBiDangChon == null)
            {
                MessageBox.Show("Hãy chọn một thiết bị trong danh sách để phân vào phòng.",
                    "Phân phòng thiết bị", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (dangSua)
                return;

            bool hoatDong = ThietBiDong.HienThiTinhTrang(thietBiDangChon.TinhTrang) == "Đang hoạt động"
                && (thietBiDangChon.TrangThai == "Chờ bàn giao" || thietBiDangChon.TrangThai == "Đang sử dụng"
                    || thietBiDangChon.TrangThai == "Đang hoạt động");
            if (!hoatDong && string.IsNullOrWhiteSpace(thietBiDangChon.MaPhong))
            {
                MessageBox.Show("Thiết bị không hoạt động, không thể phân phòng.",
                    "Phân phòng thiết bị", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dangSua = true;
            lblCheDo.Text = hoatDong ? "PHÂN PHÒNG / ĐIỀU CHUYỂN" : "THU HỒI THIẾT BỊ";
            DoiCheDo(true);
            if (!hoatDong)
                cboPhong.SelectedIndex = 0;
            else
                cboPhong.Focus();
        }

        private void Huy_Click(object sender, RoutedEventArgs e)
        {
            dangSua = false;
            if (banGoc != null)
            {
                string tenThietBi = "Thiết bị";
                foreach (ThietBiDong dong in danhSachHienThi)
                {
                    if (dong.DuLieu.MaTB == banGoc.MaTB)
                    {
                        tenThietBi = dong.TenThietBi;
                        dgvThietBi.SelectedItem = dong;
                        break;
                    }
                }
                HienChiTiet(banGoc, tenThietBi);
            }
            else
                ApDungBoLocThietBi();
        }

        private void XoaToanBoBaoDo()
        {
            cboPhong.ClearValue(Control.BorderBrushProperty);
            cboPhong.ClearValue(Control.BorderThicknessProperty);
        }

        private void DangKySuKienXoaBaoDo()
        {
            cboPhong.SelectionChanged += (sender, e) => XoaToanBoBaoDo();
        }

        private void Luu_Click(object sender, RoutedEventArgs e)
        {
            if (!dangSua || thietBiDangChon == null || banGoc == null)
                return;

            if (cboPhong.SelectedIndex < 0)
            {
                lblThongBao.Text = "Vui lòng chọn phòng hoặc chọn Chưa phân phòng.";
                MessageBox.Show(lblThongBao.Text, "Phân phòng thiết bị",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string? maPhong = cboPhong.SelectedValue as string;
            if (string.IsNullOrWhiteSpace(maPhong))
                maPhong = null;

            if (maPhong == banGoc.MaPhong)
            {
                lblThongBao.Text = "Thiết bị đang ở đúng vị trí đã chọn, không có thay đổi.";
                return;
            }

            try
            {
                bool thanhCong = busThietBi.phanThietBiVaoPhong(banGoc.MaTB, maPhong, banGoc);
                if (thanhCong)
                {
                    string maTB = banGoc.MaTB;
                    dangSua = false;
                    LoadDuLieuThietBi(maTB);
                    MessageBox.Show(maPhong == null
                        ? "Đã thu hồi thiết bị về trạng thái chưa phân phòng."
                        : "Đã phân / điều chuyển thiết bị vào phòng thành công.",
                        "Phân phòng thiết bị", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                lblThongBao.Text = "Không thể phân phòng thiết bị: " + ex.Message;
                MessageBox.Show(lblThongBao.Text, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HuHai_Click(object sender, RoutedEventArgs e)
        {
            if (dangSua || banGoc == null)
                return;
            try
            {
                if (busThietBi.capNhatTinhTrangThietBi(banGoc.MaTB, "Hư hại", banGoc))
                {
                    string maTB = banGoc.MaTB;
                    LoadDuLieuThietBi(maTB);
                    MessageBox.Show("Đã chuyển tình trạng thiết bị thành Hư hại.",
                        "Thiết bị", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                lblThongBao.Text = "Không thể báo hư hại thiết bị: " + ex.Message;
                MessageBox.Show(lblThongBao.Text, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NgungSuDung_Click(object sender, RoutedEventArgs e)
        {
            if (dangSua || banGoc == null)
                return;

            MessageBoxResult traLoi = MessageBox.Show(
                "Sau khi nhấn nút này, thiết bị sẽ bị đặt vào trạng thái ngưng sử dụng hoàn toàn và không thể khôi phục, bạn có chắc chắn chứ?",
                "Xác nhận ngưng sử dụng", MessageBoxButton.YesNo, MessageBoxImage.Warning,
                MessageBoxResult.No);
            if (traLoi != MessageBoxResult.Yes)
                return;

            try
            {
                if (busThietBi.capNhatTinhTrangThietBi(banGoc.MaTB, "Ngưng sử dụng", banGoc))
                {
                    string maTB = banGoc.MaTB;
                    LoadDuLieuThietBi(maTB);
                    MessageBox.Show("Tình trạng thiết bị đã chuyển thành Ngưng sử dụng vĩnh viễn.",
                        "Thiết bị", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                lblThongBao.Text = "Không thể ngưng sử dụng thiết bị: " + ex.Message;
                MessageBox.Show(lblThongBao.Text, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TaiLai_Click(object sender, RoutedEventArgs e)
        {
            if (dangSua)
            {
                MessageBoxResult traLoi = MessageBox.Show("Bạn đang chọn phòng nhưng chưa lưu. Bạn có muốn tải lại?",
                    "Phân phòng thiết bị", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (traLoi != MessageBoxResult.Yes)
                    return;
            }

            string? maCanChon = null;

            if (dgvThietBi.SelectedItem is ThietBiDong dong)
                maCanChon = dong.DuLieu.MaTB;

            dangSua = false;
            LoadDuLieuThietBi(maCanChon);
        }
    }
}
