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
            get
            {
                if (string.IsNullOrWhiteSpace(DuLieu.TinhTrang))
                    return "Chưa cập nhật";
                return DuLieu.TinhTrang;
            }
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
        private bool dangThem = false;
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

                int soCanChuY = 0;
                int soSapHetBaoHanh = 0;
                foreach (ThietBiDong dong in danhSachHienThi)
                {
                    if (dong.CanChuY)
                        soCanChuY++;
                    if (dong.SapHetBaoHanh)
                        soSapHetBaoHanh++;
                }

                lblTong.Text = danhSachHienThi.Count.ToString();
                lblCanTheoDoi.Text = soCanChuY.ToString();
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
            List<string> maLoaiThietBi = new List<string>();
            foreach (ET_LOAIHANGHOA loai in danhSachLoai)
            {
                if (loai.TenLoaiHH != null && loai.TenLoaiHH.IndexOf("thiết bị", StringComparison.OrdinalIgnoreCase) >= 0)
                    maLoaiThietBi.Add(loai.MaLoaiHH);
            }

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

            List<LuaChonDanhMuc> phong = new List<LuaChonDanhMuc>();
            phong.Add(new LuaChonDanhMuc("", "Chưa phân phòng"));
            foreach (ET_PHONG p in danhSachPhong)
                phong.Add(new LuaChonDanhMuc(p.MaPhong, p.TenPhong));
            cboPhong.ItemsSource = phong;

            List<LuaChonDanhMuc> nhaCungCap = new List<LuaChonDanhMuc>();
            nhaCungCap.Add(new LuaChonDanhMuc("", "Chưa xác định"));
            foreach (ET_NHACUNGCAP ncc in danhSachNhaCungCap)
                nhaCungCap.Add(new LuaChonDanhMuc(ncc.MaNCC, ncc.TenNCC));
            cboNhaCungCap.ItemsSource = nhaCungCap;

            List<LuaChonDanhMuc> phongLoc = new List<LuaChonDanhMuc>();
            phongLoc.Add(new LuaChonDanhMuc("", "Tất cả phòng"));
            foreach (ET_PHONG p in danhSachPhong)
                phongLoc.Add(new LuaChonDanhMuc(p.MaPhong, p.TenPhong));
            cboPhongLoc.ItemsSource = phongLoc;
            cboPhongLoc.SelectedIndex = 0;

            List<string> trangThaiLoc = new List<string>();
            trangThaiLoc.Add("Tất cả trạng thái");
            List<string> tinhTrang = new List<string>() { "Tốt", "Bình thường", "Cần bảo trì", "Đang bảo trì", "Hỏng" };
            List<string> trangThai = new List<string>() { "Đang sử dụng", "Ngừng sử dụng", "Đang bảo trì" };
            foreach (ET_THIETBI tb in danhSachThietBi)
            {
                if (!string.IsNullOrWhiteSpace(tb.TrangThai) && !trangThaiLoc.Contains(tb.TrangThai))
                    trangThaiLoc.Add(tb.TrangThai);
                if (!string.IsNullOrWhiteSpace(tb.TrangThai) && !trangThai.Contains(tb.TrangThai))
                    trangThai.Add(tb.TrangThai);
                if (!string.IsNullOrWhiteSpace(tb.TinhTrang) && !tinhTrang.Contains(tb.TinhTrang))
                    tinhTrang.Add(tb.TinhTrang);
            }
            cboTrangThaiLoc.ItemsSource = trangThaiLoc;
            cboTrangThaiLoc.SelectedIndex = 0;
            cboTinhTrang.ItemsSource = tinhTrang;
            cboTrangThai.ItemsSource = trangThai;
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
                || s.Contains("sửa chữa") || s.Contains("ngừng") || s.Contains("chờ xử lý");
        }

        private void ApDungBoLocThietBi(string? maCanChon = null)
        {
            if (!daTaiDuLieu)
                return;

            string tuKhoa = txtTimKiem.Text.Trim();
            string trangThaiLoc = "Tất cả trạng thái";
            string maPhongLoc = "";
            if (cboTrangThaiLoc.SelectedItem is string trangThaiDaChon)
                trangThaiLoc = trangThaiDaChon;
            if (cboPhongLoc.SelectedValue is string phongDaChon)
                maPhongLoc = phongDaChon;

            if (string.IsNullOrEmpty(maCanChon))
            {
                if (Grid_THIET_BI.SelectedItem is ThietBiDong dongDangChon)
                    maCanChon = dongDangChon.DuLieu.MaTB;
            }

            List<ThietBiDong> ketQua = new List<ThietBiDong>();
            foreach (ThietBiDong dong in danhSachHienThi)
            {
                if (trangThaiLoc != "Tất cả trạng thái" && dong.DuLieu.TrangThai != trangThaiLoc)
                    continue;
                if (maPhongLoc != "" && dong.DuLieu.MaPhong != maPhongLoc)
                    continue;

                string chuoiTim = dong.DuLieu.MaTB + " " + dong.TenThietBi + " " + dong.DuLieu.SoSerial
                    + " " + dong.TenPhong + " " + dong.NhaCungCap;
                if (tuKhoa != "" && chuoiTim.IndexOf(tuKhoa, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                ketQua.Add(dong);
            }

            Grid_THIET_BI.ItemsSource = ketQua;
            lblDangHien.Text = ketQua.Count + " thiết bị";
            if (ketQua.Count == 0)
                lblTrong.Visibility = Visibility.Visible;
            else
                lblTrong.Visibility = Visibility.Collapsed;

            if (dangThem || dangSua)
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

            Grid_THIET_BI.SelectedItem = canChon;
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
            if (!daTaiDuLieu || dangThem || dangSua)
                return;
            if (Grid_THIET_BI.SelectedItem is ThietBiDong dong)
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
            dangThem = false;
            dangSua = false;

            lblCheDo.Text = "HỒ SƠ THIẾT BỊ";
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
            cboTinhTrang.Text = tb.TinhTrang;
            cboTrangThai.Text = tb.TrangThai;
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
            dangThem = false;
            dangSua = false;
            lblCheDo.Text = "HỒ SƠ THIẾT BỊ";
            lblTieuDeChiTiet.Text = "Chưa chọn thiết bị";
            lblMaThietBi.Text = "Chọn một thiết bị bên trái hoặc thêm mới.";
            XoaForm();
            DoiCheDo(false);
        }

        private void XoaForm()
        {
            cboHangHoa.SelectedIndex = -1;
            cboNhaCungCap.SelectedIndex = 0;
            cboPhong.SelectedIndex = 0;
            txtSerial.Clear();
            cboTinhTrang.Text = "";
            cboTrangThai.Text = "";
            dpNgayMua.SelectedDate = null;
            dpHanBaoHanh.SelectedDate = null;
            txtGhiChu.Clear();
            lblThongBao.Text = "";
            XoaToanBoBaoDo();
        }

        private void DoiCheDo(bool choPhepNhap)
        {
            Grid_THIET_BI.IsEnabled = !choPhepNhap;
            cboHangHoa.IsEnabled = choPhepNhap;
            txtSerial.IsReadOnly = !choPhepNhap;
            cboNhaCungCap.IsEnabled = choPhepNhap;
            cboPhong.IsEnabled = choPhepNhap;
            cboTinhTrang.IsEnabled = choPhepNhap;
            cboTrangThai.IsEnabled = choPhepNhap;
            dpNgayMua.IsEnabled = choPhepNhap;
            dpHanBaoHanh.IsEnabled = choPhepNhap;
            txtGhiChu.IsReadOnly = !choPhepNhap;

            btnSua.Visibility = Visibility.Collapsed;
            btnLuu.Visibility = Visibility.Collapsed;
            btnHuy.Visibility = Visibility.Collapsed;
            if (choPhepNhap)
            {
                btnLuu.Visibility = Visibility.Visible;
                btnHuy.Visibility = Visibility.Visible;
            }
            else if (thietBiDangChon != null)
                btnSua.Visibility = Visibility.Visible;
        }

        private void ThemMoi_Click(object sender, RoutedEventArgs e)
        {
            thietBiDangChon = null;
            banGoc = null;
            dangThem = true;
            dangSua = false;
            Grid_THIET_BI.SelectedItem = null;
            XoaForm();
            lblCheDo.Text = "THÊM THIẾT BỊ";
            lblTieuDeChiTiet.Text = "Thiết bị mới";
            lblMaThietBi.Text = "Mã thiết bị sẽ được tạo khi lưu";
            cboTinhTrang.Text = "Tốt";
            cboTrangThai.Text = "Đang sử dụng";
            DoiCheDo(true);
            cboHangHoa.Focus();
        }

        private void Sua_Click(object sender, RoutedEventArgs e)
        {
            if (thietBiDangChon == null)
                return;
            dangSua = true;
            dangThem = false;
            lblCheDo.Text = "CHỈNH SỬA THÔNG TIN";
            DoiCheDo(true);
            cboHangHoa.Focus();
        }

        private void Huy_Click(object sender, RoutedEventArgs e)
        {
            dangThem = false;
            dangSua = false;
            if (banGoc != null)
            {
                string tenThietBi = "Thiết bị";
                foreach (ThietBiDong dong in danhSachHienThi)
                {
                    if (dong.DuLieu.MaTB == banGoc.MaTB)
                    {
                        tenThietBi = dong.TenThietBi;
                        Grid_THIET_BI.SelectedItem = dong;
                        break;
                    }
                }
                HienChiTiet(banGoc, tenThietBi);
            }
            else
                ApDungBoLocThietBi();
        }

        private void DatBaoDo(Control control)
        {
            control.BorderBrush = Brushes.Red;
            control.BorderThickness = new Thickness(1.5);
        }

        private void XoaBaoDo(Control control)
        {
            control.ClearValue(Control.BorderBrushProperty);
            control.ClearValue(Control.BorderThicknessProperty);
        }

        private void XoaToanBoBaoDo()
        {
            XoaBaoDo(cboHangHoa);
            XoaBaoDo(txtSerial);
            XoaBaoDo(cboTinhTrang);
            XoaBaoDo(cboTrangThai);
            XoaBaoDo(dpNgayMua);
            XoaBaoDo(dpHanBaoHanh);
        }

        private void XoaThongBaoLoi(object sender, EventArgs e)
        {
            if (sender is Control control)
                XoaBaoDo(control);
        }

        private void DangKySuKienXoaBaoDo()
        {
            cboHangHoa.SelectionChanged += XoaThongBaoLoi;
            txtSerial.TextChanged += XoaThongBaoLoi;
            cboTinhTrang.SelectionChanged += XoaThongBaoLoi;
            cboTrangThai.SelectionChanged += XoaThongBaoLoi;
            dpNgayMua.SelectedDateChanged += XoaThongBaoLoi;
            dpHanBaoHanh.SelectedDateChanged += XoaThongBaoLoi;
        }

        private bool KiemTraDuLieu(out string thongBaoLoi)
        {
            thongBaoLoi = "";
            XoaToanBoBaoDo();

            if (cboHangHoa.SelectedValue == null)
            {
                DatBaoDo(cboHangHoa);
                thongBaoLoi = "Vui lòng chọn tên thiết bị.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtSerial.Text))
            {
                DatBaoDo(txtSerial);
                thongBaoLoi = "Số serial không được để trống.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(cboTinhTrang.Text))
            {
                DatBaoDo(cboTinhTrang);
                thongBaoLoi = "Vui lòng nhập tình trạng thiết bị.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(cboTrangThai.Text))
            {
                DatBaoDo(cboTrangThai);
                thongBaoLoi = "Vui lòng nhập trạng thái quản lý.";
                return false;
            }
            if (dpNgayMua.SelectedDate.HasValue && dpNgayMua.SelectedDate.Value.Date > DateTime.Today)
            {
                DatBaoDo(dpNgayMua);
                thongBaoLoi = "Ngày mua không được lớn hơn ngày hiện tại.";
                return false;
            }
            if (dpNgayMua.SelectedDate.HasValue && dpHanBaoHanh.SelectedDate.HasValue)
            {
                if (dpHanBaoHanh.SelectedDate.Value.Date < dpNgayMua.SelectedDate.Value.Date)
                {
                    DatBaoDo(dpHanBaoHanh);
                    thongBaoLoi = "Hạn bảo hành không được trước ngày mua.";
                    return false;
                }
            }
            return true;
        }

        private void Luu_Click(object sender, RoutedEventArgs e)
        {
            if (!dangThem && !dangSua)
                return;

            string thongBaoLoi;
            if (!KiemTraDuLieu(out thongBaoLoi))
            {
                lblThongBao.Text = thongBaoLoi;
                MessageBox.Show(thongBaoLoi, "Kiểm tra dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string maTB = "";
                if (thietBiDangChon != null)
                    maTB = thietBiDangChon.MaTB;

                string maHH = cboHangHoa.SelectedValue.ToString();
                string? maNCC = cboNhaCungCap.SelectedValue as string;
                string? maPhong = cboPhong.SelectedValue as string;
                if (string.IsNullOrWhiteSpace(maNCC))
                    maNCC = null;
                if (string.IsNullOrWhiteSpace(maPhong))
                    maPhong = null;

                string? ghiChu = txtGhiChu.Text.Trim();
                if (ghiChu == "")
                    ghiChu = null;

                ET_THIETBI thietBi = new ET_THIETBI(
                    maTB,
                    maHH,
                    txtSerial.Text.Trim(),
                    dpNgayMua.SelectedDate,
                    maNCC,
                    maPhong,
                    cboTinhTrang.Text.Trim(),
                    dpHanBaoHanh.SelectedDate,
                    cboTrangThai.Text.Trim(),
                    ghiChu
                );

                bool thanhCong = false;
                if (dangThem)
                    thanhCong = busThietBi.themThietBi(thietBi);
                else
                    thanhCong = busThietBi.suaThietBi(thietBi, banGoc);

                if (thanhCong)
                {
                    dangThem = false;
                    dangSua = false;
                    LoadDuLieuThietBi(thietBi.MaTB);
                    MessageBox.Show("Lưu thông tin thiết bị thành công!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    lblThongBao.Text = "Không thể lưu thông tin thiết bị.";
                    MessageBox.Show(lblThongBao.Text, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                lblThongBao.Text = "Lỗi khi lưu thiết bị: " + ex.Message;
                MessageBox.Show(lblThongBao.Text, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TaiLai_Click(object sender, RoutedEventArgs e)
        {
            if (dangThem || dangSua)
            {
                MessageBoxResult traLoi = MessageBox.Show("Thông tin đang nhập chưa lưu. Bạn có muốn tải lại?",
                    "Trang thiết bị", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (traLoi != MessageBoxResult.Yes)
                    return;
            }

            string? maCanChon = null;
            if (Grid_THIET_BI.SelectedItem is ThietBiDong dong)
                maCanChon = dong.DuLieu.MaTB;
            dangThem = false;
            dangSua = false;
            LoadDuLieuThietBi(maCanChon);
        }
    }
}
