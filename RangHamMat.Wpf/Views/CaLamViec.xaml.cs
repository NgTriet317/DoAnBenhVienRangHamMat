using BUS;
using ET;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RangHamMat.Views;

/// <summary>
/// Phần xử lý ca làm việc của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class CaLamViec : UserControl
{
    /// <summary>
    /// Đối tượng nghiệp vụ ca làm được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private BUS_CALAM busCaLam = new();
    /// <summary>
    /// Đối tượng nghiệp vụ lịch làm việc được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private BUS_LICHLAMVIEC busLichLamViec = new();
    /// <summary>
    /// Đối tượng nghiệp vụ ngày được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private BUS_CA_THEO_NGAY busCaiDatCa = new();
    /// <summary>
    /// Đối tượng nghiệp vụ phòng được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private BUS_PHONG busPhong = new();
    /// <summary>
    /// Danh sách phòng dùng trong ca làm việc được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private List<ET_PHONG> danhSachPhong = new();
    /// <summary>
    /// Danh sách nhân viên dùng trong ca làm việc được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private List<ET_NHANVIEN> danhSachNhanVien = new();

    /// <summary>
    /// Danh sách ca dùng trong ca làm việc được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private List<ET_CALAM> danhSachCa = new();
    /// <summary>
    /// Danh sách lịch dùng trong ca làm việc được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private List<ET_LICHLAMVIEC> danhSachLich = new();
    /// <summary>
    /// Danh sách cài đặt dùng trong ca làm việc được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private List<ET_CA_THEO_NGAY> danhSachCaiDat = new();
    /// <summary>
    /// Danh sách ca chọn dùng trong ca làm việc được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private List<ET_CALAM> danhSachCaChon = new();
    /// <summary>
    /// Tất ca ca được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private List<CaTrongNgay> tatCaCa = new();
    /// <summary>
    /// Ca hiển thị được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private List<CaTrongNgay> caHienThi = new();

    /// <summary>
    /// Đầu tuần được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private DateTime dauTuan = LayDauTuan(DateTime.Today);
    /// <summary>
    /// Ngày đang cài đặt được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private DateTime ngayDangCaiDat;
    /// <summary>
    /// Đang từ chối được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private DangKyCa? dangTuChoi;
    /// <summary>
    /// Đang duyệt được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private DangKyCa? dangDuyet;
    /// <summary>
    /// Mã nhân viên thực hiện hoặc được gán cho bản ghi được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private string maNV = "";
    /// <summary>
    /// Cờ Đã khôi tạo; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool daKhoiTao;

    /// <summary>
    /// Là quản lý.
    /// </summary>
    public bool IsManager { get; private set; }
    /// <summary>
    /// Giá trị Là nhân viên; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public bool IsEmployee => !IsManager && !string.IsNullOrWhiteSpace(maNV);

    /// <summary>
    /// Khởi tạo ca làm việc.
    /// </summary>
    public CaLamViec() : this(RangHamMat.PhienDangNhap.MaNV, RangHamMat.PhienDangNhap.IsAdmin) { }

    /// <summary>
    /// Khởi tạo ca làm việc. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    /// <param name="maNhanVien">Mã nhân viên xác định đối tượng cần xử lý.</param>
    /// <param name="laQuanLy">Giá trị là quản lý dùng khi thực hiện thao tác; kiểu bool.</param>
    public CaLamViec(string maNhanVien, bool laQuanLy)
    {
        InitializeComponent();

        // Quyền và mã nhân viên đã xác thực ở màn hình đăng nhập.
        maNV = RangHamMat.PhienDangNhap.MaNV;
        IsManager = RangHamMat.PhienDangNhap.IsAdmin;
        DataContext = this;

        //Nếu là quản lý thì các nút chức năng tạo ca và phân ca làm sẽ hiển thị
        //nếu là nhân viên thì nút đăng ký ca sẽ hiển thị
        btnCreate.Visibility = IsManager ? Visibility.Visible : Visibility.Collapsed;
        btnMyRequests.Visibility = IsEmployee ? Visibility.Visible : Visibility.Collapsed;
        btnDangKyTuan.Visibility = IsEmployee ? Visibility.Visible : Visibility.Collapsed;
        btnExport.Visibility = IsManager ? Visibility.Visible : Visibility.Collapsed;
        btnDongBoLichKham.Visibility = IsManager ? Visibility.Visible : Visibility.Collapsed;
        btnThongBaoDonXinNghi.Visibility = IsManager ? Visibility.Visible : Visibility.Collapsed;
        if (IsManager) HangTieuDeCa.Height = new GridLength(168);


        EmployeePicker.Visibility = Visibility.Collapsed;
        cboDepartment.Visibility = Visibility.Collapsed;
        cboLocation.Visibility = Visibility.Collapsed;
        

        daKhoiTao = true;
        LoadDuLieu();
        if (IsManager) CapNhatThongBaoDonXinNghi();
    }

    // Hộp thông báo chỉ xuất hiện với tài khoản quản lý.
    private void CapNhatThongBaoDonXinNghi()
    {
        if (!IsManager) return;
        try
        {
            BUS_DON_XIN_NGHI bus = new BUS_DON_XIN_NGHI();
            int soDon = bus.demDonChoDuyet();
            txtThongBaoDonXinNghi.Text = soDon + " đơn đang chờ quản lý xét duyệt · Nhấn để xem đơn và lịch sử";
        }
        catch (Exception ex)
        {
            txtThongBaoDonXinNghi.Text = "Không tải được số đơn mới · Nhấn để thử mở danh sách";
            btnThongBaoDonXinNghi.ToolTip = ex.Message;
        }
    }

    /// <summary>
    /// Xử lý thao tác nút thông báo đơn xin nghỉ khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnThongBaoDonXinNghi_Click(object sender, RoutedEventArgs e)
    {
        if (!IsManager) return;
        DonXinNghiQuanLy view = new DonXinNghiQuanLy();
        view.QuayLai += DonXinNghi_QuayLai;
        NoiDungDonXinNghi.Content = view;
        DonXinNghiOverlay.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Quay từ màn hình quản lý đơn nghỉ về lịch ca và cập nhật các dữ liệu liên quan.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CapNhatThongBaoDonXinNghi</c>, <c>LoadDuLieu</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void DonXinNghi_QuayLai(object? sender, EventArgs e)
    {
        DonXinNghiOverlay.Visibility = Visibility.Collapsed;
        NoiDungDonXinNghi.Content = null;
        CapNhatThongBaoDonXinNghi();
        LoadDuLieu();
    }

    /// <summary>
    /// Xử lý thao tác nút đồng bộ lịch khám khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>bus.dongBoLichKhamVaoCaLam</c>, <c>LoadDuLieu</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnDongBoLichKham_Click(object sender, RoutedEventArgs e)
    {
        if (!IsManager) return;
        if (MessageBox.Show("Bổ sung các lịch khám tương lai còn thiếu phân công Ca làm?",
            "Đồng bộ lịch khám", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            BUS_LICHKHAM bus = new BUS_LICHKHAM();
            int soCa = bus.dongBoLichKhamVaoCaLam();
            LoadDuLieu();
            MessageBox.Show("Đã thêm " + soCa + " lịch làm việc từ các lịch khám chưa đồng bộ.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể đồng bộ: " + ex.Message + ". Hãy kiểm tra các ca/phòng chưa khớp.");
        }
    }

    //Hàm lấy ngày đầu tuần bằng cách đưa vào ngày hiện tại
    private static DateTime LayDauTuan(DateTime ngay)
    {
        int thu = (int)ngay.DayOfWeek;
        if (thu == 0) thu = 7;
        return ngay.Date.AddDays(1 - thu);
    }

    //Hàm hiển thị dữ liệu
    private void LoadDuLieu()
    {
        try
        {
            danhSachCa = busCaLam.layToanBoCaLam().ToList();
            try
            {
                danhSachNhanVien = new BUS_NHANVIEN().layToanBoNhanVien().ToList();
            }
            catch
            {
                danhSachNhanVien = new();
            }
            danhSachLich = busLichLamViec.layLichTheoTuan(dauTuan);
            danhSachCaiDat = busCaiDatCa.layCaTheoTuan(dauTuan);
            try
            {
                danhSachPhong = busPhong.layToanBoPhong().ToList();
            }
            catch
            {
                danhSachPhong = new(); // Vẫn hiện lịch nếu danh sách phòng lỗi.
            }
            HienThiLich();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi tải lịch làm việc: " + ex.Message);
        }
    }


    //Hàm hiện giao diện cột lịch từ t2-t7
    private void HienThiLich()
    {
        if (!daKhoiTao) return;

        //tạo danh sách thứ trong tuần
        string[] thu = { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ nhật" };
        List<NgayTrongTuan> cacNgay = new();
        tatCaCa = new();
        caHienThi = new();

        //chạy vòng lặp mỗi ngày để hiện các ca làm được cài trong ngày
        for (int i = 0; i < 7; i++)
        {
            DateTime ngay = dauTuan.AddDays(i);
            NgayTrongTuan cot = new()
            {
                Date = ngay,
                DayName = thu[i],
                DateLabel = ngay.ToString("dd/MM/yyyy")
            };

            foreach (ET_CA_THEO_NGAY caiDat in danhSachCaiDat)
            {
                if (caiDat.NgayLam.Date != ngay) continue;

                ET_CALAM? ca = danhSachCa.FirstOrDefault(x => x.MaCa == caiDat.MaCa);
                if (ca == null) continue;

                CaTrongNgay item = TaoCaTrongNgay(ca, ngay);
                tatCaCa.Add(item);

                if (!KiemTraBoLoc(item)) continue;
                cot.Shifts.Add(item);
                caHienThi.Add(item);
            }
            cacNgay.Add(cot);
        }

        WeekItems.ItemsSource = cacNgay;
        ShiftGrid.ItemsSource = caHienThi;
        CapNhatThongKe();
    }

    //Tạo ca trong ngày
    private CaTrongNgay TaoCaTrongNgay(ET_CALAM ca, DateTime ngay)
    {
        //lưu thông tin ca mới
        CaTrongNgay item = new()
        {
            MaCa = ca.MaCa,
            Date = ngay,
            Name = string.IsNullOrWhiteSpace(ca.GhiChu)
                ? ca.GioBatDau.ToString(@"hh\:mm") + " – " + ca.GioKetThuc.ToString(@"hh\:mm") : ca.GhiChu,
            TimeRange = ca.GioBatDau.ToString(@"hh\:mm") + " - " + ca.GioKetThuc.ToString(@"hh\:mm"),
            Type = "Mẫu ca",
            Note = ca.GhiChu ?? "",
            BreakTime = "Chưa có thông tin",
            DurationCaption = "Thời lượng: " + (ca.GioKetThuc - ca.GioBatDau).TotalHours.ToString("0.#") + " giờ"
        };

        //duyệt toàn bộ lịch làm của ngày được đưa vào để thêm ca làm (hủy nếu mã ca không trùng với mã ca của lịch [chưa đc đăng kí])
        foreach (ET_LICHLAMVIEC lich in danhSachLich)
        {
            if (lich.MaCa != ca.MaCa || lich.NgayLam.Date != ngay) continue;
            

            //tạo thông tin hiển thị nhân viên (để lấy tt từ lịch và hiện lên giao diện)
            ET_PHONG? phong = danhSachPhong.FirstOrDefault(x => x.MaPhong == lich.MaPhong);
            string tenPhong = phong == null ? "Chưa phân phòng" : phong.TenPhong;
            ET_NHANVIEN? nhanVien = danhSachNhanVien.FirstOrDefault(x => x.MaNV == lich.MaNV);
            string hoTen = nhanVien == null ? "Chưa có thông tin nhân viên" : nhanVien.HoTen;

            NhanVienCa nv = new()
            {
                Name = hoTen,
                Code = lich.MaNV,
                Department = string.IsNullOrWhiteSpace(tenPhong) ? "Chưa phân phòng" : "Phòng: " + tenPhong
            };

            //tạo thông tin đăng ký ca (để lấy thông tin đăng ký ca và hiển thị trên các trường cần sử dụng)
            DangKyCa dangKy = new()
            {
                MaLLV = lich.MaLLV,
                Shift = item,
                Person = nv,
                Status = lich.TrangThai,
                ReviewNote = lich.GhiChu ?? ""
            };


            //phân công ca theo trạng thái
            if (lich.TrangThai == "Chờ xác nhận") item.Requests.Add(dangKy);
            if (lich.TrangThai == "Đã xác nhận") item.Assigned.Add(nv);
            if (lich.TrangThai != "Chờ xác nhận") item.History.Add(dangKy);
        }

        if (item.Assigned.Count > 0)
            item.Location = string.Join(", ", item.Assigned.Select(x => x.Department).Distinct());

        item.Status = item.Requests.Count > 0 || item.Assigned.Count == 0
            ? "Chờ xác nhận" : "Đã xác nhận";

        return item;
    }

    //tìm kiếm ca theo bộ lọc
    private bool KiemTraBoLoc(CaTrongNgay ca)
    {
        if (cboStatus.SelectedIndex == 1 && ca.Status != "Đã xác nhận") return false;
        if (cboStatus.SelectedIndex == 2 && ca.Status != "Chờ xác nhận") return false;

        string tuKhoa = txtSearch.Text.Trim().ToLower();
        if (tuKhoa == "") return true;

        //tìm ca theo tên hoặc mã ca
        if (ca.Name.ToLower().Contains(tuKhoa) || ca.MaCa.ToString().Contains(tuKhoa)) return true;

        //tìm ca theo mã nhân viên lấy thông tin từ nhanvienca đã khởi tạo phía trên
        foreach (NhanVienCa nv in ca.Assigned)
        {
            if (nv.Code.ToLower().Contains(tuKhoa)) return true;
        }

        //tìm ca theo mã nhân viên lấy thông tin từ dangkyca đã khởi tạo phía trên
        foreach (DangKyCa dk in ca.Requests)
        {
            if (dk.Person.Code.ToLower().Contains(tuKhoa)) return true;
        }
        return false;
    }


    //cập nhật thông tin tuần
    private void CapNhatThongKe()
    {
        //hiện tuần bắt đầu từ ngày mấy đến ngày mấy
        txtWeekRange.Text = dauTuan.ToString("dd/MM") + " - " + dauTuan.AddDays(6).ToString("dd/MM/yyyy");

        //hiện tuần thứ bao nhiêu trong năm
        txtWeekNumber.Text = "Tuần " + CultureInfo.GetCultureInfo("vi-VN").Calendar.GetWeekOfYear(
            dauTuan, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

        //đếm số lượng ca
        txtShiftCount.Text = caHienThi.Count.ToString();

        //đếm nhân viên đã đc xác nhận ca
        txtStaffCount.Text = danhSachLich.Where(x => x.TrangThai == "Đã xác nhận")
            .Select(x => x.MaNV).Distinct().Count().ToString();

        //tính tổng giờ các ca trong tuần
        double tongGio = 0;
        foreach (CaTrongNgay item in tatCaCa)
        {
            ET_CALAM? ca = danhSachCa.FirstOrDefault(x => x.MaCa == item.MaCa);
            if (ca != null) tongGio += (ca.GioKetThuc - ca.GioBatDau).TotalHours;
        }
        txtHours.Text = tongGio.ToString("0.#");
        txtConfirmed.Text = caHienThi.Count(x => x.AssignedCount > 0) + "/" + caHienThi.Count;
        txtVisibleCount.Text = "Đang hiển thị " + caHienThi.Count + " ca";

        txtFootHint.Text = "Nhân viên đăng ký ca, quản lý xác nhận trong chi tiết ca.";
    }

    /// <summary>
    /// Mở thông tin ca làm được chọn và cập nhật các thao tác theo trạng thái của ca.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ca.Date.ToString</c>.</remarks>
    /// <param name="ca">Giá trị ca dùng khi thực hiện thao tác; kiểu CaTrongNgay.</param>
    private void MoChiTiet(CaTrongNgay ca)
    {
        DetailContent.DataContext = ca;
        txtDetailSubtitle.Text = ca.Name + " - " + ca.Date.ToString("dd/MM/yyyy");
        DetailOverlay.Visibility = Visibility.Visible;
    }

    // Cập nhật thông tin ca sau khi duyệt hoặc từ chối.
    private void CapNhatChiTiet()
    {
        if (DetailOverlay.Visibility != Visibility.Visible) return;
        if (DetailContent.DataContext is not CaTrongNgay caCu) return;

        CaTrongNgay? caMoi = caHienThi.FirstOrDefault(x => x.MaCa == caCu.MaCa && x.Date == caCu.Date);
        if (caMoi == null) DetailOverlay.Visibility = Visibility.Collapsed;
        else MoChiTiet(caMoi);
    }

    /// <summary>
    /// Xử lý thao tác nút tuần trước khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>dauTuan.AddDays</c>, <c>LoadDuLieu</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnTuanTruoc_Click(object sender, RoutedEventArgs e)
    {
        dauTuan = dauTuan.AddDays(-7);
        LoadDuLieu();
    }

    /// <summary>
    /// Xử lý thao tác nút tuần sau khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>dauTuan.AddDays</c>, <c>LoadDuLieu</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnTuanSau_Click(object sender, RoutedEventArgs e)
    {
        dauTuan = dauTuan.AddDays(7);
        LoadDuLieu();
    }

    /// <summary>
    /// Xử lý thao tác nút tuần hiển thị tải khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>LayDauTuan</c>, <c>LoadDuLieu</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnTuanHienTai_Click(object sender, RoutedEventArgs e)
    {
        dauTuan = LayDauTuan(DateTime.Today);
        LoadDuLieu();
    }

    /// <summary>
    /// Tiếp nhận thay đổi bộ lọc và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>HienThiLich</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void BoLoc_ThayDoi(object sender, RoutedEventArgs e)
    {
        HienThiLich();
    }

    /// <summary>
    /// Xử lý thao tác nút xem tuần khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnXemTuan_Click(object sender, RoutedEventArgs e)
    {
        WeekView.Visibility = Visibility.Visible;
        ListView.Visibility = Visibility.Collapsed;

        btnListView.Background = Brushes.Transparent;
        btnListView.Foreground = Brushes.Gray;

        btnWeekView.Background = Brushes.White;
        btnWeekView.Foreground = Brushes.Blue;
    }

    /// <summary>
    /// Xử lý thao tác nút xem danh sách khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnXemDanhSach_Click(object sender, RoutedEventArgs e)
    {
        WeekView.Visibility = Visibility.Collapsed;
        ListView.Visibility = Visibility.Visible;

        btnWeekView.Background = Brushes.Transparent;
        btnWeekView.Foreground = Brushes.Gray;

        btnListView.Background = Brushes.White;
        btnListView.Foreground = Brushes.Blue;
    }

    /// <summary>
    /// Xử lý thao tác nút chi tiết ca khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MoChiTiet</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnChiTietCa_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CaTrongNgay ca) MoChiTiet(ca);
    }

    /// <summary>
    /// Mở nội dung hoặc thao tác chi tiết của bảng ca làm việc khi người dùng bấm đúp.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MoChiTiet</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void dgvCaLamViec_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ShiftGrid.SelectedItem is CaTrongNgay ca) MoChiTiet(ca);
    }

    /// <summary>
    /// Xử lý thao tác nút đóng chi tiết khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnDongChiTiet_Click(object sender, RoutedEventArgs e)
    {
        DetailOverlay.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Xử lý thao tác chuột trên nền chi tiết theo vị trí người dùng chọn.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NenChiTiet_MouseDown(object sender, MouseButtonEventArgs e)
    {
        DetailOverlay.Visibility = Visibility.Collapsed;
    }

    //private void cboNhanVien_SelectionChanged(object sender, SelectionChangedEventArgs e)
    //{
    //    // ComboBox chọn nhân viên demo đang được ẩn.
    //}

    private void btnDangKyCa_Click(object sender, RoutedEventArgs e)
    {
        if (!IsEmployee || sender is not Button btn || btn.Tag is not CaTrongNgay ca) return;

        ET_LICHLAMVIEC lich = new()
        {
            MaNV = maNV,
            MaCa = ca.MaCa,
            NgayLam = ca.Date,
            TrangThai = "Chờ xác nhận",
            TrangThaiHienDien = "Chưa chấm công"
        };

        try
        {
            bool ketQua = busLichLamViec.dangKyCa(lich);
            MessageBox.Show(ketQua ? "Đã gửi đăng ký ca." : "Không thể đăng ký ca này.");
            if (ketQua)
            {
                LoadDuLieu();
                CapNhatChiTiet();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi đăng ký ca: " + ex.Message);
        }
    }

    // Đăng ký nhanh tất cả ca hợp lệ từ hôm nay đến cuối tuần đang xem.
    private void btnDangKyTuan_Click(object sender, RoutedEventArgs e)
    {
        if (!IsEmployee) return;
        int soCa = danhSachCaiDat.Count(x => x.NgayLam.Date >= DateTime.Today
            && x.NgayLam.Date >= dauTuan && x.NgayLam.Date < dauTuan.AddDays(7));
        if (soCa == 0)
        {
            MessageBox.Show("Tuần đang xem không có ca sắp tới để đăng ký.", "Đăng ký tuần",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show($"Đăng ký các ca hợp lệ trong tuần {dauTuan:dd/MM} - {dauTuan.AddDays(6):dd/MM/yyyy}?\n"
                + "Ca đã có, trùng giờ hoặc nằm trong thời gian nghỉ được duyệt sẽ được bỏ qua.",
                "Xác nhận đăng ký tuần", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            btnDangKyTuan.IsEnabled = false;
            var ketQua = busLichLamViec.dangKyTuan(maNV, dauTuan);
            LoadDuLieu();
            MessageBox.Show($"Đăng ký thành công: {ketQua.ThanhCong} ca.\n"
                + $"Đã đăng ký/phân công: {ketQua.DaCo} ca.\n"
                + $"Trùng khung giờ: {ketQua.TrungGio} ca.\n"
                + $"Trong thời gian nghỉ được duyệt: {ketQua.DangNghi} ca.\n\n"
                + "Các ca vừa đăng ký đang chờ quản lý xác nhận.",
                "Kết quả đăng ký tuần", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể đăng ký tuần: " + ex.Message,
                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { btnDangKyTuan.IsEnabled = true; }
    }

    // Chọn phòng trước khi xác nhận lịch làm việc cho nhân viên.
    private void btnDuyetCa_Click(object sender, RoutedEventArgs e)
    {
        if (!IsManager || sender is not Button btn || btn.Tag is not DangKyCa dk) return;

        try
        {
            danhSachPhong = busPhong.layToanBoPhong()
                .Where(x => x.TrangThai == "Đang hoạt động")
                .ToList();
            if (danhSachPhong.Count == 0)
            {
                MessageBox.Show("Chưa có phòng để phân công.");
                return;
            }

            dangDuyet = dk;
            txtThongTinDuyet.Text = dk.Person.Code + " - " + dk.Shift.Name
                + " - " + dk.Shift.Date.ToString("dd/MM/yyyy") + " (" + dk.Shift.TimeRange + ")";
            cboPhongDuyet.ItemsSource = danhSachPhong;
            cboPhongDuyet.SelectedIndex = 0;
            ApproveOverlay.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi tải phòng: " + ex.Message);
        }
    }

    /// <summary>
    /// Xử lý thao tác nút xác nhận duyệt khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>busLichLamViec.duyetCa</c>, <c>LoadDuLieu</c>, <c>CapNhatChiTiet</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnXacNhanDuyet_Click(object sender, RoutedEventArgs e)
    {
        if (!IsManager || dangDuyet == null || cboPhongDuyet.SelectedItem is not ET_PHONG phong)
        {
            MessageBox.Show("Vui lòng chọn phòng khám.");
            return;
        }

        try
        {
            if (!busLichLamViec.duyetCa(dangDuyet.MaLLV, phong.MaPhong))
            {
                MessageBox.Show("Không thể duyệt. Nhân viên đã có lịch trùng giờ hoặc yêu cầu không còn chờ duyệt.");
                return;
            }

            dangDuyet = null;
            ApproveOverlay.Visibility = Visibility.Collapsed;
            LoadDuLieu();
            CapNhatChiTiet();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi duyệt ca: " + ex.Message);
        }
    }

    /// <summary>
    /// Xử lý thao tác nút dòng duyệt khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnDongDuyet_Click(object sender, RoutedEventArgs e)
    {
        dangDuyet = null;
        ApproveOverlay.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Xử lý thao tác nút từ chối ca khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>txtRejectReason.Clear</c>, <c>dk.Shift.Date.ToString</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnTuChoiCa_Click(object sender, RoutedEventArgs e)
    {
        if (!IsManager || sender is not Button btn || btn.Tag is not DangKyCa dk) return;

        dangTuChoi = dk;
        txtRejectReason.Clear();
        txtRejectTitle.Text = dk.Person.Name + " - " + dk.Shift.Date.ToString("dd/MM/yyyy");
        RejectOverlay.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Xử lý thao tác nút xác nhận từ chối khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>txtRejectReason.Text.Trim</c>, <c>MessageBox.Show</c>, <c>busLichLamViec.tuChoiCa</c>, <c>LoadDuLieu</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnXacNhanTuChoi_Click(object sender, RoutedEventArgs e)
    {
        if (!IsManager || dangTuChoi == null) return;
        string lyDo = txtRejectReason.Text.Trim();

        if (lyDo == "")
        {
            MessageBox.Show("Vui lòng nhập lý do từ chối.");
            return;
        }

        try
        {
            if (!busLichLamViec.tuChoiCa(dangTuChoi.MaLLV, lyDo))
            {
                MessageBox.Show("Không thể từ chối đăng ký này.");
                return;
            }

            dangTuChoi = null;
            RejectOverlay.Visibility = Visibility.Collapsed;
            LoadDuLieu();
            CapNhatChiTiet();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi từ chối ca: " + ex.Message);
        }
    }

    /// <summary>
    /// Xử lý thao tác nút dòng từ chối khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnDongTuChoi_Click(object sender, RoutedEventArgs e)
    {
        RejectOverlay.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Xử lý thao tác chuột trên nền từ chối theo vị trí người dùng chọn.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NenTuChoi_MouseDown(object sender, MouseButtonEventArgs e)
    {
        RejectOverlay.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Xử lý thao tác nút xem đăng ký khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>HienThiDangKy</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnXemDangKy_Click(object sender, RoutedEventArgs e)
    {
        HienThiDangKy();
        MyRequestsOverlay.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Hiển thị các đăng ký ca và thông tin chờ duyệt của phạm vi đang xem.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>danhSach.Add</c>.</remarks>
    private void HienThiDangKy()
    {
        List<DangKyCa> danhSach = new();
        foreach (CaTrongNgay ca in tatCaCa)
        {
            foreach (DangKyCa dk in ca.Requests)
            {
                if (dk.Person.Code == maNV) danhSach.Add(dk);
            }
            foreach (DangKyCa dk in ca.History)
            {
                if (dk.Person.Code == maNV) danhSach.Add(dk);
            }
        }

        MyRequestsList.ItemsSource = danhSach;
        txtMyRequestCount.Text = danhSach.Count + " đăng ký";
        EmptyMyRequests.Visibility = danhSach.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Xử lý thao tác nút hủy đăng ký khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>busLichLamViec.huyDangKy</c>, <c>LoadDuLieu</c>, <c>HienThiDangKy</c>, <c>MessageBox.Show</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnHuyDangKy_Click(object sender, RoutedEventArgs e)
    {
        if (!IsEmployee || sender is not Button btn || btn.Tag is not DangKyCa dk) return;

        try
        {
            if (busLichLamViec.huyDangKy(dk.MaLLV, maNV))
            {
                LoadDuLieu();
                HienThiDangKy();
            }
            else MessageBox.Show("Không thể hủy yêu cầu này.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi hủy đăng ký: " + ex.Message);
        }
    }

    /// <summary>
    /// Xử lý thao tác nút dòng danh sách đăng ký khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnDongDanhSachDangKy_Click(object sender, RoutedEventArgs e)
    {
        MyRequestsOverlay.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Xử lý thao tác chuột trên nền danh sách đăng ký theo vị trí người dùng chọn.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NenDanhSachDangKy_MouseDown(object sender, MouseButtonEventArgs e)
    {
        MyRequestsOverlay.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Xử lý thao tác nút cài đặt ca khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>danhSachCaiDat.Any</c>, <c>danhSachCaChon.Add</c>, <c>ca.GioBatDau.ToString</c>, <c>ca.GioKetThuc.ToString</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnCaiDatCa_Click(object sender, RoutedEventArgs e)
    {
        if (!IsManager || sender is not Button btn || btn.Tag is not DateTime ngay) return;
        
        ngayDangCaiDat = ngay;
        danhSachCaChon = new();
        List<string> tenCa = new();

        foreach (ET_CALAM ca in danhSachCa)
        {
            bool daCaiDat = danhSachCaiDat.Any(x => x.NgayLam.Date == ngay && x.MaCa == ca.MaCa);
            if (daCaiDat) continue;

            danhSachCaChon.Add(ca);
            string ten = string.IsNullOrWhiteSpace(ca.GhiChu) ? ca.GioBatDau.ToString(@"hh\:mm") + " – " + ca.GioKetThuc.ToString(@"hh\:mm") : ca.GhiChu;
            tenCa.Add(ten + " (" + ca.GioBatDau.ToString(@"hh\:mm") + " - " + ca.GioKetThuc.ToString(@"hh\:mm") + ")");
        }

        if (danhSachCaChon.Count == 0)
        {
            MessageBox.Show("Ngày này đã có tất cả các ca.");
            return;
        }

        txtNgayCaiDat.Text = "Ngày " + ngay.ToString("dd/MM/yyyy");
        cboCaCoSan.ItemsSource = tenCa;
        cboCaCoSan.SelectedIndex = 0;
        SetShiftOverlay.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Xử lý thao tác nút lưu cài đặt ca khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>busCaiDatCa.themCaTheoNgay</c>, <c>LoadDuLieu</c>, <c>MessageBox.Show</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnLuuCaiDatCa_Click(object sender, RoutedEventArgs e)
    {
        if (!IsManager || cboCaCoSan.SelectedIndex < 0) return;

        ET_CALAM ca = danhSachCaChon[cboCaCoSan.SelectedIndex];
        ET_CA_THEO_NGAY caiDat = new()
        {
            MaCa = ca.MaCa,
            NgayLam = ngayDangCaiDat
        };

        try
        {
            if (busCaiDatCa.themCaTheoNgay(caiDat))
            {
                SetShiftOverlay.Visibility = Visibility.Collapsed;
                LoadDuLieu();
            }
            else MessageBox.Show("Không thể cài đặt ca này.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi cài ca: " + ex.Message);
        }
    }

    /// <summary>
    /// Xử lý thao tác nút bộ ca khôi ngày khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>ca.Date.ToString</c>, <c>busCaiDatCa.xoaCaTheoNgay</c>, <c>LoadDuLieu</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnBoCaKhoiNgay_Click(object sender, RoutedEventArgs e)
    {
        if (!IsManager || sender is not Button btn || btn.Tag is not CaTrongNgay ca) return;

        if (ca.Date.Date < DateTime.Today)
        {
            MessageBox.Show("Không thể gỡ ca làm được xác nhận trong quá khứ.");
            return;
        }

        if (MessageBox.Show("Bỏ ca " + ca.Name + " ngày " + ca.Date.ToString("dd/MM/yyyy") + "?",
            "Xác nhận", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

        try
        {
            if (busCaiDatCa.xoaCaTheoNgay(ca.Date, ca.MaCa)) LoadDuLieu();
            else MessageBox.Show("Ca đang có nhân viên đăng ký hoặc đã được duyệt.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi bỏ ca: " + ex.Message);
        }
    }

    /// <summary>
    /// Xử lý thao tác nút dòng cài đặt ca khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnDongCaiDatCa_Click(object sender, RoutedEventArgs e)
    {
        SetShiftOverlay.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Xử lý thao tác chuột trên nền cài đặt ca theo vị trí người dùng chọn.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NenCaiDatCa_MouseDown(object sender, MouseButtonEventArgs e)
    {
        SetShiftOverlay.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Xử lý thao tác nút mở tạo ca khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CapNhatXemTruoc</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnMoTaoCa_Click(object sender, RoutedEventArgs e)
    {
        if (!IsManager) return;

        dpCreateDate.SelectedDate = dauTuan;
        cbCreateDay.SelectedIndex = 0;
        CapNhatXemTruoc();
        CreateOverlay.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Phản ánh lựa chọn hộp chọn ngày tạo ca vào dữ liệu và phần hiển thị liên quan.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>dauTuan.AddDays</c>, <c>CapNhatXemTruoc</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void cboNgayTaoCa_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!daKhoiTao || cbCreateDay.SelectedIndex < 0) return;
        dpCreateDate.SelectedDate = dauTuan.AddDays(cbCreateDay.SelectedIndex);
        CapNhatXemTruoc();
    }

    /// <summary>
    /// Đồng bộ ngày tạo ca vừa chọn với các trường phụ thuộc và phần xem trước.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CapNhatXemTruoc</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void dpNgayTaoCa_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        CapNhatXemTruoc();
    }

    /// <summary>
    /// Tiếp nhận thay đổi thông tin tạo ca và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CapNhatXemTruoc</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ThongTinTaoCa_Changed(object sender, RoutedEventArgs e)
    {
        CapNhatXemTruoc();
    }

    /// <summary>
    /// Cập nhật phần xem trước ca từ ngày, giờ và nội dung người dùng đang nhập.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>.ToString</c>.</remarks>
    private void CapNhatXemTruoc()
    {
        if (!daKhoiTao) return;

        txtCreatePreview.Text = (cbCreateName.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Ca mới";
        txtCreatePreviewSub.Text = txtCreateStart.Text + " - " + txtCreateEnd.Text + " · mẫu ca chưa lưu";
    }

    /// <summary>
    /// Xử lý thao tác nút lưu ca làm khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TimeSpan.TryParse</c>, <c>MessageBox.Show</c>, <c>.ToString</c>, <c>busCaLam.themCaLam</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnLuuCaLam_Click(object sender, RoutedEventArgs e)
    {
        if (!IsManager) return;

        bool hopLeBatDau = TimeSpan.TryParse(txtCreateStart.Text, out TimeSpan batDau);
        bool hopLeKetThuc = TimeSpan.TryParse(txtCreateEnd.Text, out TimeSpan ketThuc);

        if (!hopLeBatDau || !hopLeKetThuc || batDau >= ketThuc || ketThuc.TotalHours >= 24)
        {
            MessageBox.Show("Giờ bắt đầu và kết thúc không hợp lệ.");
            return;
        }
        ET_CALAM ca = new()
        {
            GioBatDau = batDau,
            GioKetThuc = ketThuc,
            TrangThai = "Đang hoạt động",
            GhiChu = (cbCreateName.SelectedItem as ComboBoxItem)?.Content?.ToString()
        };

        try
        {
            if (busCaLam.themCaLam(ca))
            {
                CreateOverlay.Visibility = Visibility.Collapsed;
                LoadDuLieu();
            }
            else MessageBox.Show("Không thể thêm ca làm.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi thêm ca: " + ex.Message);
        }
    }

    /// <summary>
    /// Xử lý thao tác nút dòng tạo ca khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void btnDongTaoCa_Click(object sender, RoutedEventArgs e)
    {
        CreateOverlay.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Xử lý thao tác chuột trên nền tạo ca theo vị trí người dùng chọn.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NenTaoCa_MouseDown(object sender, MouseButtonEventArgs e)
    {
        CreateOverlay.Visibility = Visibility.Collapsed;
    }

    /// Xuất lịch làm việc hiện tại ra file CSV.
    private void btnXuatLich_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog hopThoai = new()
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = "LichLamViec.csv"
        };

        if (hopThoai.ShowDialog() != true) return;

        List<string> dong = new() { "Ngay,MaCa,TenCa,KhungGio,MaNhanVien,TrangThai" };
        foreach (CaTrongNgay ca in caHienThi)
        {
            List<DangKyCa> dangKy = new();
            dangKy.AddRange(ca.Requests);
            dangKy.AddRange(ca.History);

            if (dangKy.Count == 0)
            {
                dong.Add(TaoDongCsv(ca, "", ""));
            }
            foreach (DangKyCa dk in dangKy)
            {
                dong.Add(TaoDongCsv(ca, dk.Person.Code, dk.Status));
            }
        }

        try
        {
            File.WriteAllLines(hopThoai.FileName, dong, System.Text.Encoding.UTF8);
            MessageBox.Show("Đã xuất lịch.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi xuất lịch: " + ex.Message);
        }
    }

    /// Tạo một dòng CSV từ thông tin ca và nhân viên.
    private string TaoDongCsv(CaTrongNgay ca, string maNhanVien, string trangThai)
    {
        string[] cot = { ca.Date.ToString("dd/MM/yyyy"), ca.MaCa.ToString(), ca.Name, ca.TimeRange, maNhanVien, trangThai };
        return string.Join(",", cot.Select(x => "\"" + x.Replace("\"", "\"\"") + "\""));
    }

    //// Giao diện còn sự kiện kéo thả, nhưng ca cố định không được chuyển ngày bằng kéo thả.
    //private void TheCa_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { }
    //private void TheCa_PreviewMouseMove(object sender, MouseEventArgs e) { }
    //private void CotNgay_DragOver(object sender, DragEventArgs e)
    //{
    //    e.Effects = DragDropEffects.None;
    //    e.Handled = true;
    //}
    //private void CotNgay_DragLeave(object sender, DragEventArgs e) { }
    //private void CotNgay_Drop(object sender, DragEventArgs e)
    //{
    //    e.Handled = true;
    //}
}

// Các lớp nhỏ dùng để hiển thị dữ liệu trên giao diện XAML.
public class NhanVienCa
{
    /// <summary>
    /// Tên.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Mã.
    /// </summary>
    public string Code { get; set; } = "";
    /// <summary>
    /// Chữ cái đầu.
    /// </summary>
    public string Initials { get; set; } = "NV";
    /// <summary>
    /// Khoa.
    /// </summary>
    public string Department { get; set; } = "";
}

/// <summary>
/// Phần xử lý đăng ký ca của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public class DangKyCa
{
    /// <summary>
    /// Mã lịch làm việc.
    /// </summary>
    public string MaLLV { get; set; } = "";
    /// <summary>
    /// Ca.
    /// </summary>
    public CaTrongNgay Shift { get; set; } = new();
    /// <summary>
    /// Người.
    /// </summary>
    public NhanVienCa Person { get; set; } = new();
    /// <summary>
    /// Trạng thái.
    /// </summary>
    public string Status { get; set; } = "";
    /// <summary>
    /// Ghi chú đối chiếu.
    /// </summary>
    public string ReviewNote { get; set; } = "";
    /// <summary>
    /// Giá trị Nhãn đối chiếu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string ReviewCaption => Status;
    /// <summary>
    /// Giá trị Nhãn thời điểm gửi; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string SubmittedCaption => "";
    /// <summary>
    /// Giá trị Tổng kết; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string Summary => Shift.Date.ToString("dd/MM/yyyy") + " - " + Shift.TimeRange;
    /// <summary>
    /// Giá trị Có thể hủy; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public bool CanCancel => Status == "Chờ xác nhận";
}

/// <summary>
/// Phần xử lý ca trong ngày của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public class CaTrongNgay
{
    /// <summary>
    /// Mã ca làm việc.
    /// </summary>
    public string MaCa { get; set; } = "";
    /// <summary>
    /// Ngày.
    /// </summary>
    public DateTime Date { get; set; }
    /// <summary>
    /// Tên.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Khoảng thời gian.
    /// </summary>
    public string TimeRange { get; set; } = "";
    /// <summary>
    /// Kiểu.
    /// </summary>
    public string Type { get; set; } = "";
    /// <summary>
    /// Ghi chú.
    /// </summary>
    public string Note { get; set; } = "";
    /// <summary>
    /// Vị trí.
    /// </summary>
    public string Location { get; set; } = "Chưa phân phòng";
    /// <summary>
    /// Nghỉ giữa ca thời gian.
    /// </summary>
    public string BreakTime { get; set; } = "";
    /// <summary>
    /// Nhãn thời lượng.
    /// </summary>
    public string DurationCaption { get; set; } = "";
    /// <summary>
    /// Trạng thái.
    /// </summary>
    public string Status { get; set; } = "";
    /// <summary>
    /// Đã phân công.
    /// </summary>
    public List<NhanVienCa> Assigned { get; set; } = new();
    /// <summary>
    /// Các yêu cầu.
    /// </summary>
    public List<DangKyCa> Requests { get; set; } = new();
    /// <summary>
    /// Lịch sử.
    /// </summary>
    public List<DangKyCa> History { get; set; } = new();
    /// <summary>
    /// Giá trị Số mục đã phân công; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public int AssignedCount => Assigned.Count;
    /// <summary>
    /// Giá trị Số yêu cầu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public int RequestCount => Requests.Count;
    /// <summary>
    /// Giá trị Có các yêu cầu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public bool HasRequests => Requests.Count > 0;
    /// <summary>
    /// Giá trị Nhãn số mục đã phân công; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string AssignedLabel => AssignedCount + " nhân viên";
    /// <summary>
    /// Giá trị Nhãn số yêu cầu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string RequestLabel => RequestCount + " yêu cầu";
}

/// <summary>
/// Phần xử lý ngày trong tuần của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public class NgayTrongTuan
{
    /// <summary>
    /// Ngày.
    /// </summary>
    public DateTime Date { get; set; }
    /// <summary>
    /// Tên ngày.
    /// </summary>
    public string DayName { get; set; } = "";
    /// <summary>
    /// Ngày nhãn.
    /// </summary>
    public string DateLabel { get; set; } = "";
    /// <summary>
    /// Các ca.
    /// </summary>
    public List<CaTrongNgay> Shifts { get; set; } = new();
    /// <summary>
    /// Giá trị Số lượng; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public int Count => Shifts.Count;
}
