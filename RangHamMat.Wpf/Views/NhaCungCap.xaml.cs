using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Danh bạ đối tác, đọc SQL qua cache chung và giữ bản nguồn khi mở form sửa.</summary>
public partial class NhaCungCap : UserControl
{
    private List<DongNhaCungCap> danhSach = new();
    private bool sanSang;

    public NhaCungCap()
    {
        InitializeComponent();
        cboTrangThai.ItemsSource = new[] { "Tất cả trạng thái", "Đang hoạt động", "Ngừng hoạt động" };
        cboTrangThai.SelectedIndex = 0;
        HoTroGiaoDien.Observe(this, NapDanhSach);
    }

    private void NapDanhSach()
    {
        try
        {
            danhSach = DuLieuGiaoDien.Rows("NHA_CUNG_CAP")
                .OrderBy(row => UiRecord.Text(row, "TenNCC"), StringComparer.CurrentCultureIgnoreCase)
                .Select(row => new DongNhaCungCap(row)).ToList();
            txtTongSo.Text = danhSach.Count.ToString();
            txtHoatDong.Text = danhSach.Count(row => row.TrangThai == "Đang hoạt động").ToString();
            txtNgungHoatDong.Text = danhSach.Count(row => row.TrangThai == "Ngừng hoạt động").ToString();
            sanSang = true;
            LocDanhSach();
        }
        catch (InvalidOperationException ex) { txtLoi.Text = ex.Message + " Chọn Tải lại để thử lại."; }
    }

    private void LocDanhSach()
    {
        if (!sanSang) return;
        var tuKhoa = txtTimKiem.Text.Trim();
        var trangThai = cboTrangThai.SelectedItem as string;
        var maDaChon = (Grid_NHA_CUNG_CAP.SelectedItem as DongNhaCungCap)?.MaNCC;
        var ketQua = danhSach.Where(row =>
            (cboTrangThai.SelectedIndex <= 0 || row.TrangThai == trangThai) &&
            (tuKhoa.Length == 0 || row.TuKhoa.Contains(tuKhoa, StringComparison.CurrentCultureIgnoreCase))).ToList();
        Grid_NHA_CUNG_CAP.ItemsSource = ketQua;
        Grid_NHA_CUNG_CAP.SelectedItem = ketQua.FirstOrDefault(row => row.MaNCC == maDaChon);
        KhongCoDuLieu.Visibility = ketQua.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        txtSoDong.Text = $"{ketQua.Count} / {danhSach.Count} nhà cung cấp";
        txtLoi.Text = "";
    }

    private void BoLoc_ThayDoi(object sender, RoutedEventArgs e) => LocDanhSach();
    private void BoLoc_Click(object sender, RoutedEventArgs e)
    {
        sanSang = false; txtTimKiem.Clear(); cboTrangThai.SelectedIndex = 0;
        sanSang = true; LocDanhSach();
    }
    private void TaiLai_Click(object sender, RoutedEventArgs e)
    {
        try { DuLieuGiaoDien.RefreshFromDatabase(); }
        catch (InvalidOperationException ex) { txtLoi.Text = ex.Message + " Chọn Tải lại để thử lại."; }
    }
    private void DanhSach_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ChiTiet.DataContext = Grid_NHA_CUNG_CAP.SelectedItem;
        ChiTiet.Visibility = ChiTiet.DataContext is null ? Visibility.Collapsed : Visibility.Visible;
        ChuaChon.Visibility = ChiTiet.DataContext is null ? Visibility.Visible : Visibility.Collapsed;
        txtThongBao.Text = "";
    }
    private void Them_Click(object sender, RoutedEventArgs e) => MoBieuMau(null);
    private void Sua_Click(object sender, RoutedEventArgs e)
    {
        if (Grid_NHA_CUNG_CAP.SelectedItem is DongNhaCungCap dong) MoBieuMau(dong.Record);
    }
    private void MoBieuMau(object? banGoc)
    {
        var workspace = HoTroGiaoDien.Workspace(this);
        if (workspace is null || !workspace.CanOpen("NhaCungCap")) return;
        var form = new CuaSoNhaCungCap();
        BieuMauHoSo.Configure(form, "NHA_CUNG_CAP", banGoc);
        workspace.ShowModal(form);
    }
    private void Xoa_Click(object sender, RoutedEventArgs e)
    {
        if (Grid_NHA_CUNG_CAP.SelectedItem is not DongNhaCungCap { ChoXoa: true } dong) return;
        if (MessageBox.Show($"Xóa nhà cung cấp {dong.MaNCC} — {dong.TenNCC}?", "Xóa nhà cung cấp", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { DuLieuGiaoDien.Delete("NHA_CUNG_CAP", dong.Record); }
        catch (InvalidOperationException ex) { txtLoi.Text = ex.Message; }
    }
    private void SaoChep_Click(object sender, RoutedEventArgs e)
    {
        if (Grid_NHA_CUNG_CAP.SelectedItem is not DongNhaCungCap dong) return;
        try
        {
            Clipboard.SetText(string.Join(Environment.NewLine, dong.TenNCC, "Người liên hệ: " + dong.NguoiLienHe,
                "Điện thoại: " + dong.DienThoai, "Email: " + dong.Email, "Địa chỉ: " + dong.DiaChi));
            txtThongBao.Text = "Đã sao chép thông tin liên hệ.";
        }
        catch (ExternalException) { txtLoi.Text = "Chưa thể sao chép. Vui lòng thử lại."; }
    }
    private void NoiDung_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var hep = e.NewSize.Width < 900;
        CotChiTiet.Width = new GridLength(hep ? 0 : e.NewSize.Width < 1150 ? 290 : 320);
        HangCach.Height = new GridLength(hep ? 12 : 0);
        HangChiTiet.Height = new GridLength(hep ? 220 : 0);
        Grid.SetRowSpan(VungDanhSach, hep ? 1 : 3);
        Grid.SetColumn(VungChiTiet, hep ? 0 : 2);
        Grid.SetRow(VungChiTiet, hep ? 2 : 0);
        Grid.SetRowSpan(VungChiTiet, hep ? 1 : 3);
        Grid.SetColumnSpan(VungChiTiet, hep ? 3 : 1);
    }
}

/// <summary>Chỉ chuẩn bị nhãn và số liên kết; ET và các câu SQL nằm trong các tầng hiện có.</summary>
public sealed class DongNhaCungCap
{
    public object Record { get; }
    public string MaNCC => UiRecord.Text(Record, "MaNCC");
    public string TenNCC => UiRecord.Text(Record, "TenNCC");
    public string NguoiLienHe => UiRecord.Text(Record, "NguoiLienHe");
    public string Email => UiRecord.Text(Record, "Email");
    public string TrangThai => UiRecord.Text(Record, "TrangThai");
    public string DienThoai => string.IsNullOrWhiteSpace(UiRecord.Text(Record, "SDT")) ? "Chưa có số điện thoại" : UiRecord.Text(Record, "SDT");
    public string DiaChi => string.IsNullOrWhiteSpace(UiRecord.Text(Record, "DiaChi")) ? "Chưa cập nhật địa chỉ" : UiRecord.Text(Record, "DiaChi");
    public int SoThietBi { get; }
    public int SoLoHang { get; }
    public int SoHoaDonNhap { get; }
    public bool ChoXoa => SoThietBi + SoLoHang + SoHoaDonNhap == 0;
    public string ThongTinLienKet => $"{SoThietBi} thiết bị · {SoLoHang} lô hàng · {SoHoaDonNhap} hóa đơn nhập";
    public string HuongDanXoa => ChoXoa ? "Chỉ xóa nhà cung cấp chưa có dữ liệu liên kết." : "Đã có dữ liệu liên kết. Chọn Cập nhật và chuyển Ngừng hoạt động nếu ngừng hợp tác.";
    public string TuKhoa => string.Join(" ", MaNCC, TenNCC, NguoiLienHe, UiRecord.Text(Record, "SDT"), Email, UiRecord.Text(Record, "DiaChi"));

    public DongNhaCungCap(object record)
    {
        Record = record;
        SoThietBi = DuLieuGiaoDien.Children("THIET_BI", "MaNCC", MaNCC).Length;
        SoLoHang = DuLieuGiaoDien.Children("LO_HANG_HOA", "MaNCC", MaNCC).Length;
        SoHoaDonNhap = DuLieuGiaoDien.Children("HOA_DON_NHAP", "MaNCC", MaNCC).Length;
    }
}
