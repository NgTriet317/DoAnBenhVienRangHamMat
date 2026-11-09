using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Hàng đợi thiết bị hư hại theo serial và lịch sử bảo trì của từng tài sản.</summary>
public partial class BaoTri : UserControl
{
    /// <summary>
    /// Danh sách  dùng trong bảo trì được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private List<DongBaoTri> danhSach = new();
    /// <summary>
    /// Thiết bị hư được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private List<DongThietBiBaoTri> thietBiHu = new();
    /// <summary>
    /// Cờ Sẵn sàng; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool sanSang;

    /// <summary>
    /// Khởi tạo bảo trì. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    public BaoTri()
    {
        InitializeComponent();
        cboTrangThai.ItemsSource = new[] { "Tất cả trạng thái", "Chưa xác nhận", "Đã xác nhận", "Đang thực hiện", "Đã hoàn thành", "Hủy" };
        cboTrangThai.SelectedIndex = 0;
        HoTroGiaoDien.Observe(this, NapDanhSach);
        // Thiết bị có thể được sửa trực tiếp ở phân hệ khác: đọc lại SQL mỗi lần vào trang.
        Loaded += (_, _) => TaiLaiDuLieu();
    }

    /// <summary>
    /// Nạp các danh mục cần tra tên, đọc danh sách nguồn rồi cập nhật bộ lọc và lựa chọn hiện tại.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DuLieuGiaoDien.Rows</c>, <c>UiRecord.Date</c>, <c>long.TryParse</c>, <c>UiRecord.Text</c>.</remarks>
    private void NapDanhSach()
    {
        try
        {
            danhSach = DuLieuGiaoDien.Rows("BAO_TRI_THIET_BI")
                .OrderByDescending(row => UiRecord.Date(row, "NgayBatDau"))
                .ThenByDescending(row => long.TryParse(string.Concat(UiRecord.Text(row, "MaBT").Where(char.IsDigit)), out var so) ? so : 0)
                .Select(row => new DongBaoTri(row)).ToList();
            foreach (var nhom in danhSach.Where(row => row.Status != "Hủy").GroupBy(row => row.MaTB))
                nhom.First().DenHan = nhom.First().NgayTiepTheo?.Date <= DateTime.Today;
            var dangBaoTri = danhSach.Where(row => row.Status is "Chưa xác nhận" or "Đã xác nhận" or "Đang thực hiện").Select(row => row.MaTB).ToHashSet();
            thietBiHu = DuLieuGiaoDien.Rows("THIET_BI")
                .Where(row => UiRecord.Text(row, "TinhTrang") == "Hư hại" && !dangBaoTri.Contains(UiRecord.Text(row, "MaTB")))
                .Select(row => new DongThietBiBaoTri(row)).OrderBy(row => row.TenThietBi).ThenBy(row => row.SoSerial).ToList();
            txtTongSo.Text = danhSach.Count.ToString();
            txtChuaXacNhan.Text = danhSach.Count(row => row.Status == "Chưa xác nhận").ToString();
            txtDangThucHien.Text = danhSach.Count(row => row.Status == "Đang thực hiện").ToString();
            txtHoanThanh.Text = danhSach.Count(row => row.Status == "Đã hoàn thành").ToString();
            sanSang = true;
            LocThietBi(); LocDanhSach();
        }
        catch (InvalidOperationException ex) { txtLoi.Text = ex.Message + " Chọn Tải lại để thử lại."; }
    }

    /// <summary>
    /// Lọc danh sách nguồn theo tiêu chí trên giao diện mà không thay đổi dữ liệu trong SQL.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>KiemTraNhapLieu.ReadDate</c>, <c>txtTimKiem.Text.Trim</c>, <c>danhSach.Where</c>, <c>row.TuKhoa.Contains</c>.</remarks>
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

    /// <summary>
    /// Lọc danh sách thiết bị có thể chọn làm nguồn phiếu bảo trì.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>txtTimThietBi.Text.Trim</c>, <c>thietBiHu.Where</c>, <c>row.TuKhoa.Contains</c>, <c>rows.FirstOrDefault</c>.</remarks>
    private void LocThietBi()
    {
        if (!sanSang) return;
        var maDaChon = (GridThietBiHu.SelectedItem as DongThietBiBaoTri)?.MaTB;
        var query = txtTimThietBi.Text.Trim();
        var rows = thietBiHu.Where(row => row.TuKhoa.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToList();
        GridThietBiHu.ItemsSource = rows;
        GridThietBiHu.SelectedItem = rows.FirstOrDefault(row => row.MaTB == maDaChon);
        KhongCoThietBi.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        txtSoThietBi.Text = $"{rows.Count} / {thietBiHu.Count} thiết bị hư hại đang chờ lập phiếu";
        txtHangDoi.Text = $"Thiết bị hư hại ({thietBiHu.Count})";
    }

    /// <summary>
    /// Tiếp nhận thay đổi bộ lọc và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>LocDanhSach</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void BoLoc_ThayDoi(object sender, RoutedEventArgs e) => LocDanhSach();
    /// <summary>
    /// Tiếp nhận thay đổi tìm thiết bị và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>LocThietBi</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void TimThietBi_ThayDoi(object sender, TextChangedEventArgs e) => LocThietBi();
    /// <summary>
    /// Xử lý thao tác bộ lọc khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>txtTimKiem.Clear</c>, <c>LocDanhSach</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void BoLoc_Click(object sender, RoutedEventArgs e)
    {
        sanSang = false;
        txtTimKiem.Clear(); dpTuNgay.SelectedDate = null; dpTuNgay.Text = "";
        dpDenNgay.SelectedDate = null; dpDenNgay.Text = "";
        cboTrangThai.SelectedIndex = 0; chkDenHan.IsChecked = false;
        sanSang = true; LocDanhSach();
    }
    /// <summary>
    /// Xử lý thao tác tải lại khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TaiLaiDuLieu</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void TaiLai_Click(object sender, RoutedEventArgs e) => TaiLaiDuLieu();
    /// <summary>
    /// Nạp lại dữ liệu bảo trì và thiết bị khi nguồn đã thay đổi sau thao tác của cửa sổ con.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DuLieuGiaoDien.RefreshFromDatabase</c>.</remarks>
    private void TaiLaiDuLieu()
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
        ChiTiet.DataContext = Grid_BAO_TRI_THIET_BI.SelectedItem;
        ChiTiet.Visibility = ChiTiet.DataContext is null ? Visibility.Collapsed : Visibility.Visible;
        ChuaChon.Visibility = ChiTiet.DataContext is null ? Visibility.Visible : Visibility.Collapsed;
    }
    /// <summary>
    /// Phản ánh lựa chọn thiết bị vào dữ liệu và phần hiển thị liên quan.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ThietBi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => btnLapPhieu.IsEnabled = GridThietBiHu.SelectedItem is DongThietBiBaoTri;
    /// <summary>
    /// Xử lý thao tác thêm khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MoBieuMau</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Them_Click(object sender, RoutedEventArgs e)
    {
        if (GridThietBiHu.SelectedItem is DongThietBiBaoTri dong) MoBieuMau(null, dong.MaTB);
    }
    /// <summary>
    /// Xử lý thao tác sửa khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MoBieuMau</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Sua_Click(object sender, RoutedEventArgs e)
    {
        if (Grid_BAO_TRI_THIET_BI.SelectedItem is DongBaoTri { ChoSua: true } dong) MoBieuMau(dong.Record, dong.MaTB);
    }
    /// <summary>
    /// Mở cửa sổ biểu mẫu tương ứng bản ghi/chế độ được chọn, rồi cập nhật danh sách sau khi lưu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>HoTroGiaoDien.Workspace</c>, <c>workspace.CanOpen</c>, <c>BieuMauHoSo.Configure</c>, <c>form.SetThietBi</c>.</remarks>
    /// <param name="banGoc">Bản dữ liệu trước khi chỉnh sửa để đối chiếu với giá trị hiện tại.</param>
    /// <param name="maTB">Mã thiết bị xác định đối tượng cần xử lý.</param>
    private void MoBieuMau(object? banGoc, string maTB)
    {
        var workspace = HoTroGiaoDien.Workspace(this);
        if (workspace is null || !workspace.CanOpen("BaoTri")) return;
        var form = new CuaSoBaoTri();
        BieuMauHoSo.Configure(form, "BAO_TRI_THIET_BI", banGoc, new CreationContext("BAO_TRI_THIET_BI", "MaTB", maTB));
        form.SetThietBi(maTB);
        form.Unloaded += (_, _) =>
        {
            if (BieuMauHoSo.GetSaved(form) is not object saved) return;
            // Đưa người dùng đến phiếu vừa lưu để tiếp tục bước xác nhận / thực hiện.
            BoLoc_Click(this, new RoutedEventArgs());
            CacDanhSach.SelectedIndex = 1;
            Grid_BAO_TRI_THIET_BI.SelectedItem = danhSach.FirstOrDefault(row => row.MaBT == UiRecord.Text(saved, "MaBT"));
            if (Grid_BAO_TRI_THIET_BI.SelectedItem is object selected) Grid_BAO_TRI_THIET_BI.ScrollIntoView(selected);
        };
        workspace.ShowModal(form);
    }
    /// <summary>
    /// Xử lý thao tác chuyển trạng thái khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>HoTroGiaoDien.Workspace</c>, <c>workspace.CanOpen</c>, <c>MessageBox.Show</c>, <c>DuLieuGiaoDien.ChuyenTrangThaiBaoTri</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ChuyenTrangThai_Click(object sender, RoutedEventArgs e)
    {
        var workspace = HoTroGiaoDien.Workspace(this);
        if (workspace is null || !workspace.CanOpen("BaoTri")) return;
        if (sender is not Button { Tag: string target } || Grid_BAO_TRI_THIET_BI.SelectedItem is not DongBaoTri dong) return;
        var hopLe = (dong.Status, target) is ("Chưa xác nhận", "Đã xác nhận") or ("Chưa xác nhận", "Hủy") or ("Đã xác nhận", "Đang thực hiện") or ("Đang thực hiện", "Đã hoàn thành");
        if (!hopLe) return;
        var action = target switch { "Đã xác nhận" => "Xác nhận phiếu và khóa nội dung", "Hủy" => "Hủy phiếu", "Đang thực hiện" => "Bắt đầu bảo trì", _ => "Hoàn thành bảo trì" };
        if (MessageBox.Show($"{action} {dong.MaBT}?\n{dong.TenThietBi} · Serial: {dong.SoSerial}", "Bảo trì thiết bị", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { DuLieuGiaoDien.ChuyenTrangThaiBaoTri(dong.Record, target); }
        catch (InvalidOperationException ex) { txtLoi.Text = ex.Message; }
    }
    /// <summary>
    /// Điều chỉnh phần hiển thị theo kích thước thực tế của nội dung.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Grid.SetRowSpan</c>, <c>Grid.SetColumn</c>, <c>Grid.SetRow</c>, <c>Grid.SetColumnSpan</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NoiDung_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var hep = e.NewSize.Width < 1050;
        CotChiTiet.Width = new GridLength(hep ? 0 : 300);
        HangCach.Height = new GridLength(hep ? 12 : 0);
        HangChiTiet.Height = new GridLength(hep ? Math.Min(300, Math.Max(190, e.NewSize.Height * .42)) : 0);
        Grid.SetRowSpan(VungDanhSach, hep ? 1 : 3);
        Grid.SetColumn(VungChiTiet, hep ? 0 : 2);
        Grid.SetRow(VungChiTiet, hep ? 2 : 0);
        Grid.SetRowSpan(VungChiTiet, hep ? 1 : 3);
        Grid.SetColumnSpan(VungChiTiet, hep ? 3 : 1);
    }
}
