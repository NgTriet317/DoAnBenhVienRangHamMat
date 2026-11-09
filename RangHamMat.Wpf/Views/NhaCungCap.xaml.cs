using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Danh bạ đối tác, đọc SQL qua cache chung và giữ bản nguồn khi mở form sửa.</summary>
public partial class NhaCungCap : UserControl
{
    /// <summary>
    /// Danh sách  dùng trong nhà cung cấp được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private List<DongNhaCungCap> danhSach = new();
    /// <summary>
    /// Cờ Sẵn sàng; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool sanSang;

    /// <summary>
    /// Khởi tạo nhà cung cấp. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    public NhaCungCap()
    {
        InitializeComponent();
        cboTrangThai.ItemsSource = new[] { "Tất cả trạng thái", "Đang hoạt động", "Ngừng hoạt động" };
        cboTrangThai.SelectedIndex = 0;
        HoTroGiaoDien.Observe(this, NapDanhSach);
    }

    /// <summary>
    /// Nạp các danh mục cần tra tên, đọc danh sách nguồn rồi cập nhật bộ lọc và lựa chọn hiện tại.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DuLieuGiaoDien.Rows</c>, <c>UiRecord.Text</c>, <c>danhSach.Count.ToString</c>, <c>danhSach.Count</c>.</remarks>
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

    /// <summary>
    /// Lọc danh sách nguồn theo tiêu chí trên giao diện mà không thay đổi dữ liệu trong SQL.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>txtTimKiem.Text.Trim</c>, <c>danhSach.Where</c>, <c>row.TuKhoa.Contains</c>, <c>ketQua.FirstOrDefault</c>.</remarks>
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

    /// <summary>
    /// Tiếp nhận thay đổi bộ lọc và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>LocDanhSach</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void BoLoc_ThayDoi(object sender, RoutedEventArgs e) => LocDanhSach();
    /// <summary>
    /// Xử lý thao tác bộ lọc khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>txtTimKiem.Clear</c>, <c>LocDanhSach</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void BoLoc_Click(object sender, RoutedEventArgs e)
    {
        sanSang = false; txtTimKiem.Clear(); cboTrangThai.SelectedIndex = 0;
        sanSang = true; LocDanhSach();
    }
    /// <summary>
    /// Xử lý thao tác tải lại khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DuLieuGiaoDien.RefreshFromDatabase</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void TaiLai_Click(object sender, RoutedEventArgs e)
    {
        try { DuLieuGiaoDien.RefreshFromDatabase(); }
        catch (InvalidOperationException ex) { txtLoi.Text = ex.Message + " Chọn Tải lại để thử lại."; }
    }
    /// <summary>
    /// Phản ánh lựa chọn danh sách vào dữ liệu và phần hiển thị liên quan.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void DanhSach_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ChiTiet.DataContext = Grid_NHA_CUNG_CAP.SelectedItem;
        ChiTiet.Visibility = ChiTiet.DataContext is null ? Visibility.Collapsed : Visibility.Visible;
        ChuaChon.Visibility = ChiTiet.DataContext is null ? Visibility.Visible : Visibility.Collapsed;
        txtThongBao.Text = "";
    }
    /// <summary>
    /// Xử lý thao tác thêm khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MoBieuMau</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Them_Click(object sender, RoutedEventArgs e) => MoBieuMau(null);
    /// <summary>
    /// Xử lý thao tác sửa khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MoBieuMau</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Sua_Click(object sender, RoutedEventArgs e)
    {
        if (Grid_NHA_CUNG_CAP.SelectedItem is DongNhaCungCap dong) MoBieuMau(dong.Record);
    }
    /// <summary>
    /// Mở cửa sổ biểu mẫu tương ứng bản ghi/chế độ được chọn, rồi cập nhật danh sách sau khi lưu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>HoTroGiaoDien.Workspace</c>, <c>workspace.CanOpen</c>, <c>BieuMauHoSo.Configure</c>, <c>workspace.ShowModal</c>.</remarks>
    /// <param name="banGoc">Bản dữ liệu trước khi chỉnh sửa để đối chiếu với giá trị hiện tại.</param>
    private void MoBieuMau(object? banGoc)
    {
        var workspace = HoTroGiaoDien.Workspace(this);
        if (workspace is null || !workspace.CanOpen("NhaCungCap")) return;
        var form = new CuaSoNhaCungCap();
        BieuMauHoSo.Configure(form, "NHA_CUNG_CAP", banGoc);
        workspace.ShowModal(form);
    }
    /// <summary>
    /// Xử lý thao tác xóa khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>DuLieuGiaoDien.Delete</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Xoa_Click(object sender, RoutedEventArgs e)
    {
        if (Grid_NHA_CUNG_CAP.SelectedItem is not DongNhaCungCap { ChoXoa: true } dong) return;
        if (MessageBox.Show($"Xóa nhà cung cấp {dong.MaNCC} — {dong.TenNCC}?", "Xóa nhà cung cấp", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { DuLieuGiaoDien.Delete("NHA_CUNG_CAP", dong.Record); }
        catch (InvalidOperationException ex) { txtLoi.Text = ex.Message; }
    }
    /// <summary>
    /// Xử lý thao tác sao chép khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Clipboard.SetText</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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
    /// <summary>
    /// Điều chỉnh phần hiển thị theo kích thước thực tế của nội dung.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Grid.SetRowSpan</c>, <c>Grid.SetColumn</c>, <c>Grid.SetRow</c>, <c>Grid.SetColumnSpan</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
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
    /// <summary>
    /// Bản ghi.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Giá trị Mã nhà cung cấp; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string MaNCC => UiRecord.Text(Record, "MaNCC");
    /// <summary>
    /// Giá trị Tên nhà cung cấp; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string TenNCC => UiRecord.Text(Record, "TenNCC");
    /// <summary>
    /// Giá trị Người liên hệ; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string NguoiLienHe => UiRecord.Text(Record, "NguoiLienHe");
    /// <summary>
    /// Giá trị Email; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string Email => UiRecord.Text(Record, "Email");
    /// <summary>
    /// Giá trị Trạng thái; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string TrangThai => UiRecord.Text(Record, "TrangThai");
    /// <summary>
    /// Giá trị Điện thoại; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string DienThoai => string.IsNullOrWhiteSpace(UiRecord.Text(Record, "SDT")) ? "Chưa có số điện thoại" : UiRecord.Text(Record, "SDT");
    /// <summary>
    /// Giá trị Địa chỉ; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string DiaChi => string.IsNullOrWhiteSpace(UiRecord.Text(Record, "DiaChi")) ? "Chưa cập nhật địa chỉ" : UiRecord.Text(Record, "DiaChi");
    /// <summary>
    /// Số thiết bị.
    /// </summary>
    public int SoThietBi { get; }
    /// <summary>
    /// Số lô hàng.
    /// </summary>
    public int SoLoHang { get; }
    /// <summary>
    /// Số hóa đơn nhập.
    /// </summary>
    public int SoHoaDonNhap { get; }
    /// <summary>
    /// Giá trị Chờ xóa; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public bool ChoXoa => SoThietBi + SoLoHang + SoHoaDonNhap == 0;
    /// <summary>
    /// Giá trị Thông tin liên kết; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string ThongTinLienKet => $"{SoThietBi} thiết bị · {SoLoHang} lô hàng · {SoHoaDonNhap} hóa đơn nhập";
    /// <summary>
    /// Giá trị Hướng dẫn xóa; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string HuongDanXoa => ChoXoa ? "Chỉ xóa nhà cung cấp chưa có dữ liệu liên kết." : "Đã có dữ liệu liên kết. Chọn Cập nhật và chuyển Ngừng hoạt động nếu ngừng hợp tác.";
    /// <summary>
    /// Giá trị Từ khóa tìm kiếm áp dụng cho danh sách; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string TuKhoa => string.Join(" ", MaNCC, TenNCC, NguoiLienHe, UiRecord.Text(Record, "SDT"), Email, UiRecord.Text(Record, "DiaChi"));

    /// <summary>
    /// Khởi tạo dòng nhà cung cấp.
    /// </summary>
    /// <param name="record">Giá trị bản ghi dùng khi thực hiện thao tác; kiểu object.</param>
    public DongNhaCungCap(object record)
    {
        Record = record;
        SoThietBi = DuLieuGiaoDien.Children("THIET_BI", "MaNCC", MaNCC).Length;
        SoLoHang = DuLieuGiaoDien.Children("LO_HANG_HOA", "MaNCC", MaNCC).Length;
        SoHoaDonNhap = DuLieuGiaoDien.Children("HOA_DON_NHAP", "MaNCC", MaNCC).Length;
    }
}
