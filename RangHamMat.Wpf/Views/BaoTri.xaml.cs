using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Lịch sử bảo trì đọc cache SQL; ghi qua BUS/DAL trong giao dịch chung.</summary>
public partial class BaoTri : UserControl
{
    private List<DongBaoTri> danhSach = new();
    private bool sanSang;

    public BaoTri()
    {
        InitializeComponent();
        cboTrangThai.ItemsSource = new[] { "Tất cả trạng thái", "Nháp", "Đang thực hiện", "Hoàn thành", "Đã xác nhận", "Hủy" };
        cboTrangThai.SelectedIndex = 0;
        HoTroGiaoDien.Observe(this, NapDanhSach);
    }

    private void NapDanhSach()
    {
        try
        {
            danhSach = DuLieuGiaoDien.Rows("BAO_TRI_THIET_BI")
                .OrderByDescending(row => UiRecord.Date(row, "NgayBatDau"))
                .ThenByDescending(row => long.TryParse(string.Concat(UiRecord.Text(row, "MaBT").Where(char.IsDigit)), out var so) ? so : 0)
                .Select(row => new DongBaoTri(row)).ToList();
            // Chỉ lần mới nhất chưa hủy của mỗi thiết bị quyết định lịch đến hạn.
            foreach (var nhom in danhSach.Where(row => row.Status != "Hủy").GroupBy(row => row.MaTB))
            {
                var moiNhat = nhom.First();
                moiNhat.DenHan = moiNhat.NgayTiepTheo?.Date <= DateTime.Today;
            }
            txtTongSo.Text = danhSach.Count.ToString();
            txtDangThucHien.Text = danhSach.Count(row => row.Status == "Đang thực hiện").ToString();
            txtHoanThanh.Text = danhSach.Count(row => row.Status is "Hoàn thành" or "Đã xác nhận").ToString();
            txtDenHan.Text = danhSach.Count(row => row.DenHan).ToString();
            sanSang = true;
            LocDanhSach();
        }
        catch (InvalidOperationException ex) { txtLoi.Text = ex.Message + " Chọn Tải lại để thử lại."; }
    }

    private void LocDanhSach()
    {
        if (!sanSang) return;
        try
        {
            var tuNgay = KiemTraNhapLieu.ReadDate(dpTuNgay);
            var denNgay = KiemTraNhapLieu.ReadDate(dpDenNgay);
            if (tuNgay > denNgay) throw new InvalidOperationException("Ngày kết thúc bộ lọc phải từ ngày bắt đầu trở đi.");
            var tuKhoa = txtTimKiem.Text.Trim();
            var trangThai = cboTrangThai.SelectedItem as string;
            var maDaChon = (Grid_BAO_TRI_THIET_BI.SelectedItem as DongBaoTri)?.MaBT;
            var ketQua = danhSach.Where(row =>
                (tuNgay is null || row.NgayBatDau.Date >= tuNgay.Value.Date) &&
                (denNgay is null || row.NgayBatDau.Date <= denNgay.Value.Date) &&
                (cboTrangThai.SelectedIndex <= 0 || row.Status == trangThai) &&
                (chkDenHan.IsChecked != true || row.DenHan) &&
                (tuKhoa.Length == 0 || row.TuKhoa.Contains(tuKhoa, StringComparison.CurrentCultureIgnoreCase))).ToList();
            Grid_BAO_TRI_THIET_BI.ItemsSource = ketQua;
            Grid_BAO_TRI_THIET_BI.SelectedItem = ketQua.FirstOrDefault(row => row.MaBT == maDaChon);
            KhongCoDuLieu.Visibility = ketQua.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            txtSoDong.Text = $"{ketQua.Count} / {danhSach.Count} lần bảo trì";
            txtLoi.Text = "";
        }
        catch (InvalidOperationException ex) { txtLoi.Text = ex.Message; }
    }

    private void BoLoc_ThayDoi(object sender, RoutedEventArgs e) => LocDanhSach();
    private void BoLoc_Click(object sender, RoutedEventArgs e)
    {
        sanSang = false;
        txtTimKiem.Clear(); dpTuNgay.SelectedDate = null; dpTuNgay.Text = "";
        dpDenNgay.SelectedDate = null; dpDenNgay.Text = "";
        cboTrangThai.SelectedIndex = 0; chkDenHan.IsChecked = false;
        sanSang = true; LocDanhSach();
    }
    private void TaiLai_Click(object sender, RoutedEventArgs e)
    {
        try { DuLieuGiaoDien.RefreshFromDatabase(); }
        catch (InvalidOperationException ex) { txtLoi.Text = ex.Message + " Chọn Tải lại để thử lại."; }
    }
    private void DanhSach_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ChiTiet.DataContext = Grid_BAO_TRI_THIET_BI.SelectedItem;
        ChiTiet.Visibility = ChiTiet.DataContext is null ? Visibility.Collapsed : Visibility.Visible;
        ChuaChon.Visibility = ChiTiet.DataContext is null ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Them_Click(object sender, RoutedEventArgs e) => MoBieuMau(null);
    private void Sua_Click(object sender, RoutedEventArgs e)
    {
        if (Grid_BAO_TRI_THIET_BI.SelectedItem is DongBaoTri { ChoSua: true } dong) MoBieuMau(dong.Record);
    }
    private void MoBieuMau(object? banGoc)
    {
        var workspace = HoTroGiaoDien.Workspace(this);
        if (workspace is null || !workspace.CanOpen("BaoTri")) return;
        var form = new CuaSoBaoTri();
        BieuMauHoSo.Configure(form, "BAO_TRI_THIET_BI", banGoc);
        workspace.ShowModal(form);
    }
    private void Xoa_Click(object sender, RoutedEventArgs e)
    {
        if (Grid_BAO_TRI_THIET_BI.SelectedItem is not DongBaoTri { ChoXoa: true } dong) return;
        if (MessageBox.Show($"Xóa bản nháp {dong.MaBT} của {dong.TenThietBi}?", "Xóa bản nháp bảo trì", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { DuLieuGiaoDien.Delete("BAO_TRI_THIET_BI", dong.Record); }
        catch (InvalidOperationException ex) { txtLoi.Text = ex.Message; }
    }
    // Đưa chi tiết xuống dưới khi khung hẹp, giữ hai vùng cuộn riêng.
    private void NoiDung_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var hep = e.NewSize.Width < 900;
        CotChiTiet.Width = new GridLength(hep ? 0 : e.NewSize.Width < 1150 ? 260 : 300);
        HangCach.Height = new GridLength(hep ? 12 : 0);
        HangChiTiet.Height = new GridLength(hep ? 170 : 0);
        Grid.SetRowSpan(VungDanhSach, hep ? 1 : 3);
        Grid.SetColumn(VungChiTiet, hep ? 0 : 2);
        Grid.SetRow(VungChiTiet, hep ? 2 : 0);
        Grid.SetRowSpan(VungChiTiet, hep ? 1 : 3);
        Grid.SetColumnSpan(VungChiTiet, hep ? 3 : 1);
    }
}

/// <summary>Dữ liệu trình bày, giữ bản ghi nguồn để kiểm tra sửa đồng thời.</summary>
public sealed class DongBaoTri
{
    public object Record { get; }
    public string MaBT => UiRecord.Text(Record, "MaBT");
    public string MaTB => UiRecord.Text(Record, "MaTB");
    public DateTime NgayBatDau => UiRecord.Date(Record, "NgayBatDau")!.Value;
    public DateTime? NgayTiepTheo => UiRecord.Date(Record, "NgayBaoTriTiepTheo");
    public decimal ChiPhi => Convert.ToDecimal(UiRecord.Get(Record, "ChiPhi"));
    public string Status => UiRecord.Text(Record, "TrangThai");
    public string NoiDung => UiRecord.Text(Record, "NoiDung");
    public string GhiChu => string.IsNullOrWhiteSpace(UiRecord.Text(Record, "GhiChu")) ? "Chưa có ghi chú" : UiRecord.Text(Record, "GhiChu");
    public string TenThietBi { get; }
    public string TenPhong { get; }
    public string ThietBiDayDu { get; }
    public string NhanVien { get; }
    public string ViTri => MaTB + " · " + TenPhong;
    public string ThoiGian => NgayBatDau.ToString("dd/MM/yyyy") + " → " + (UiRecord.Date(Record, "NgayKetThuc")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa kết thúc");
    public bool DenHan { get; set; }
    public string LichTiepTheo => NgayTiepTheo is null ? "Chưa lên lịch" : NgayTiepTheo.Value.ToString("dd/MM/yyyy") + (DenHan ? " · Đến / quá hạn" : "");
    public bool ChoSua => DuLieuGiaoDien.CanEdit("BAO_TRI_THIET_BI", Record);
    public bool ChoXoa => Status == "Nháp";
    public string TuKhoa => string.Join(" ", MaBT, ThietBiDayDu, NhanVien, TenPhong, NoiDung);

    public DongBaoTri(object record)
    {
        Record = record;
        var thietBi = DuLieuGiaoDien.Find("THIET_BI", MaTB);
        var hangHoa = DuLieuGiaoDien.Find("HANG_HOA", UiRecord.Get(thietBi, "MaHH"));
        TenThietBi = hangHoa is null ? MaTB : UiRecord.Text(hangHoa, "TenHH");
        var phong = DuLieuGiaoDien.Find("PHONG", UiRecord.Get(thietBi, "MaPhong"));
        TenPhong = phong is null ? "Chưa bố trí phòng" : UiRecord.Text(phong, "TenPhong");
        ThietBiDayDu = TenThietBi + " · " + MaTB + " · " + (thietBi is null ? "" : UiRecord.Text(thietBi, "SoSerial"));
        NhanVien = DuLieuGiaoDien.Lookup("MaNV", UiRecord.Get(record, "MaNV"));
    }
}
