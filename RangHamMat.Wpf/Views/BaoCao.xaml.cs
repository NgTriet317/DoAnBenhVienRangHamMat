using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>
/// Phần xử lý mục báo cáo của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public sealed class MucBaoCao : INotifyPropertyChanged
{
    /// <summary>
    /// Chuyên đề.
    /// </summary>
    public ChuyenDeBaoCao ChuyenDe { get; }
    /// <summary>
    /// Cờ Được chọn; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool duocChon;
    /// <summary>
    /// Giá trị Được chọn; được xác định bởi phần getter/setter của thuộc tính. Phát PropertyChanged khi gán để binding cập nhật nội dung liên quan.
    /// </summary>
    public bool DuocChon { get => duocChon; set { if (duocChon == value) return; duocChon = value; PropertyChanged?.Invoke(this, new(nameof(DuocChon))); } }
    /// <summary>
    /// Khởi tạo mục báo cáo.
    /// </summary>
    /// <param name="chuyenDe">Giá trị chuyên đề dùng khi thực hiện thao tác; kiểu ChuyenDeBaoCao.</param>
    public MucBaoCao(ChuyenDeBaoCao chuyenDe) => ChuyenDe = chuyenDe;
    /// <summary>
    /// Thông báo thay đổi thuộc tính cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}
/// <summary>
/// Phần xử lý nhóm báo cáo của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public sealed class NhomBaoCao : INotifyPropertyChanged
{
    /// <summary>
    /// Tên.
    /// </summary>
    public string Ten { get; init; } = "";
    /// <summary>
    /// Các mục.
    /// </summary>
    public MucBaoCao[] CacMuc { get; init; } = Array.Empty<MucBaoCao>();
    /// <summary>
    /// Giá trị Số lượng của dòng dữ liệu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public int SoLuong => CacMuc.Length;
    /// <summary>
    /// Cờ Mở rộng; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool moRong;
    /// <summary>
    /// Giá trị Mở rộng; được xác định bởi phần getter/setter của thuộc tính. Phát PropertyChanged khi gán để binding cập nhật nội dung liên quan.
    /// </summary>
    public bool MoRong { get => moRong; set { if (moRong == value) return; moRong = value; PropertyChanged?.Invoke(this, new(nameof(MoRong))); } }
    /// <summary>
    /// Thông báo thay đổi thuộc tính cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}
/// <summary>
/// Gói dữ liệu chỉ số báo cáo truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="Ten">Giá trị tên dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="GiaTri">Giá trị giá trị dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="DonVi">Giá trị đơn vị dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="Mau">Giá trị mẫu dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="GhiChu">Giá trị ghi chú dùng khi thực hiện thao tác; kiểu string.</param>
public sealed record ChiSoBaoCao(string Ten, string GiaTri, string DonVi, string Mau, string GhiChu);

/// <summary>XAML giữ toàn bộ bố cục. Code-behind chọn chuyên đề, tải snapshot chỉ đọc và xuất bảng tổng hợp.</summary>
public partial class BaoCao : UserControl
{
    /// <summary>
    /// Định danh dependency property quy định số cột chỉ số thống kê được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    public static readonly DependencyProperty KpiColumnsProperty = DependencyProperty.Register(nameof(KpiColumns), typeof(int), typeof(BaoCao), new PropertyMetadata(4));
    /// <summary>
    /// Giá trị Chỉ số tổng hợp các cột; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public int KpiColumns { get => (int)GetValue(KpiColumnsProperty); set => SetValue(KpiColumnsProperty, value); }
    /// <summary>
    /// Tiếng Việt được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    /// <summary>
    /// Bảng màu được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static readonly string[] Colors = { "#2468CD", "#087A73", "#76519E", "#986010" };
    /// <summary>
    /// Đọc được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly Func<string, PhatSinhBaoCao[]>? read;
    /// <summary>
    /// Trình đơn được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly MucBaoCao[] menu;
    /// <summary>
    /// Các nhóm được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly NhomBaoCao[] groups;
    /// <summary>
    /// Đang chọn được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private ChuyenDeBaoCao? selected;
    /// <summary>
    /// Kết quả được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private KetQuaBaoCao? result;
    /// <summary>
    /// Cờ Sẵn sàng, Đang thay đổi lựa chọn, Cờ đang điền dữ liệu, dùng tránh xử lý lại sự kiện phát sinh do cập nhật giao diện; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool ready, changingSelection, loading;
    /// <summary>
    /// Phiên bản được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private int version;
    /// <summary>
    /// Đã áp dụng từ đã áp dụng đến được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private DateTime appliedFrom, appliedTo;
    /// <summary>
    /// Đã áp dụng chế độ được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private string appliedMode = "Ngày";
    /// <summary>
    /// Đã áp dụng ngưỡng được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private int appliedThreshold = 30;

    /// <summary>
    /// Khởi tạo báo cáo.
    /// </summary>
    public BaoCao() : this(null) { }

    /// <summary>
    /// Khởi tạo báo cáo. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    /// <param name="read">Giá trị đọc dùng khi thực hiện thao tác; kiểu Func&lt;string, PhatSinhBaoCao[]&gt;?.</param>
    public BaoCao(Func<string, PhatSinhBaoCao[]>? read)
    {
        this.read = read;
        menu = BaoCaoDuLieu.DanhMuc.Select(item => new MucBaoCao(item)).ToArray();
        groups = menu.GroupBy(item => item.ChuyenDe.Nhom).Select(group => new NhomBaoCao
        { Ten = group.Key, CacMuc = group.ToArray(), MoRong = group.Key == "Khám chữa bệnh" }).ToArray();
        InitializeComponent();
        FromDate.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        ToDate.SelectedDate = DateTime.Today; AsOfDate.SelectedDate = DateTime.Today; PeriodMode.SelectedIndex = 0;
        // DatePicker có thể giữ SelectedDate cũ khi gõ sai: mọi lần sửa text đều khóa xuất ngay.
        AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(NgayDangSua), true);
        Unloaded += (_, _) => XoaKetQua();
        HoTroGiaoDien.Observe(this, () =>
        {
            if (result is null && !loading) return;
            XoaKetQua(); ThongBao("Dữ liệu hệ thống đã được làm mới. Nhấn Tải báo cáo để cập nhật kết quả.");
        });
        ready = true;
        HienDanhMuc(); ChonBaoCao("02");
    }

    /// <summary>
    /// Chọn chuyên đề báo cáo và đặt các bộ lọc, chỉ số, cột chi tiết tương ứng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XoaKetQua</c>, <c>ThongBao</c>, <c>BaoCaoDuLieu.DanhMuc.Single</c>, <c>group.CacMuc.Any</c>.</remarks>
    /// <param name="ma">Mã định danh của bản ghi cần xử lý.</param>
    public void ChonBaoCao(string ma)
    {
        if (!PhienDangNhap.IsAdmin)
        {
            XoaKetQua(); LoadButton.IsEnabled = false;
            ThongBao("Tài khoản hiện tại không có quyền xem báo cáo. Vui lòng liên hệ quản trị viên.", true);
            return;
        }
        var report = BaoCaoDuLieu.DanhMuc.Single(item => item.Ma == ma);
        if (selected?.Ma == ma) return;
        selected = report;
        foreach (var group in groups) group.MoRong = group.CacMuc.Any(item => item.ChuyenDe.Ma == ma);
        changingSelection = true;
        try { foreach (var item in menu) item.DuocChon = item.ChuyenDe.Ma == ma; }
        finally { changingSelection = false; }
        ReportTitle.Text = report.Ten; ReportDescription.Text = report.MoTa;
        SourceText.Text = report.Nguon; RuleText.Text = report.QuyTac;
        ReportChart.PrimaryLabel = report.ChiSo[0]; ReportChart.SecondaryLabel = report.ChiSo[1];
        ReportChart.Unit = report.Ma is "09" or "11" ? "VNĐ" : "";
        TrendSubtitle.Text = report.Ma == "09" ? "Tiền thu và tiền hoàn xác nhận · VNĐ" : report.ChiSo[0] + " / " + report.ChiSo[1];
        CompositionTitle.Text = report.Ma == "09" ? "Thu / hoàn theo phương thức" : report.Ma == "11" ? "Quỹ lương theo nhân viên" : report.Ma == "13" ? "Cơ cấu dòng nhập / xuất" : "Cơ cấu trạng thái";
        CompositionSubtitle.Text = report.Ma == "09" ? "Tổng giá trị giao dịch xác nhận · VNĐ" : report.Ma == "11" ? "Tổng lương đã chốt · VNĐ" : "Dữ liệu thuộc khoảng đã chọn";
        var columns = new[] { FirstColumn, SecondColumn, ThirdColumn, FourthColumn };
        var properties = new[] { "Mot", "Hai", "Ba", "Bon" };
        for (var i = 0; i < columns.Length; i++)
        {
            columns[i].Header = report.ChiSo[i] + " (" + report.DonVi[i] + ")";
            columns[i].Binding = new Binding(properties[i]) { StringFormat = report.DonVi[i] is "VNĐ" or "%" ? "N2" : "N0", ConverterCulture = Vietnamese, TargetNullValue = "—" };
        }
        RangeShortcuts.Visibility = DateRangeFields.Visibility = report.TheoThoiDiem ? Visibility.Collapsed : Visibility.Visible;
        SnapshotFields.Visibility = report.TheoThoiDiem ? Visibility.Visible : Visibility.Collapsed;
        ChartLayout.Visibility = report.TheoThoiDiem ? Visibility.Collapsed : Visibility.Visible;
        SummaryPanel.Visibility = report.TheoThoiDiem ? Visibility.Collapsed : Visibility.Visible;
        TableTitle.Text = report.TheoThoiDiem ? "Bảng kiểm tra hạn dùng" : "Bảng đối chiếu theo kỳ";
        ReportGrid.Visibility = report.TheoThoiDiem ? Visibility.Collapsed : Visibility.Visible;
        FilterHint.Text = report.TheoThoiDiem ? "Kiểm tra hạn dùng theo ngày chốt; không suy ra số dư tồn kho." : "Chỉ tính dữ liệu trong khoảng ngày đã chọn.";
        CauHinhChiTiet(report);
        XoaKetQua();
        LoadButton.IsEnabled = !loading && report.DaKetNoi;
        LoadButton.Content = report.DaKetNoi ? "Tải báo cáo" : "Chưa tích hợp dữ liệu";
        LoadButton.ToolTip = report.DaKetNoi ? "Đọc dữ liệu mới nhất và áp dụng bộ lọc hiện tại." : report.QuyTac;
        if (!report.DaKetNoi)
        {
            EmptyTitle.Text = "Chuyên đề chưa tích hợp nguồn dữ liệu";
            EmptyDescription.Text = "Danh mục và bộ lọc đã sẵn sàng. Chưa có số liệu để đối chiếu hoặc xuất.";
            ThongBao("Chưa tích hợp nguồn dữ liệu cho chuyên đề này. Các chỉ số chưa được tính, không phải bằng 0.");
        }
        else ThongBao("Chưa tải báo cáo. Chọn khoảng thời gian rồi nhấn Tải báo cáo.");
        ReportScroll.ScrollToTop();
    }

    /// <summary>
    /// Hiển thị danh sách chuyên đề để người dùng chọn báo cáo cần xem.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>BaoCaoDuLieu.BoDau</c>, <c>ReportSearch.Text.Trim</c>, <c>keyword.Split</c>, <c>groups.Select</c>.</remarks>
    private void HienDanhMuc()
    {
        if (!ready) return;
        if (!PhienDangNhap.IsAdmin) { ReportCatalog.ItemsSource = null; CatalogCount.Text = "Chưa được cấp quyền"; return; }
        var keyword = BaoCaoDuLieu.BoDau(ReportSearch.Text.Trim());
        var words = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var filtered = keyword.Length == 0 ? groups : groups.Select(group => new NhomBaoCao
        { Ten = group.Ten, MoRong = true, CacMuc = group.CacMuc.Where(item => words.All(word => item.ChuyenDe.TuKhoa.Contains(word))).ToArray() }).Where(group => group.SoLuong > 0).ToArray();
        ReportCatalog.ItemsSource = filtered;
        CatalogCount.Text = keyword.Length == 0 ? $"{groups.Length} nhóm · {menu.Length} chuyên đề" : $"{filtered.Sum(group => group.SoLuong)} / {menu.Length} chuyên đề";
        NoSearchResults.Visibility = filtered.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    /// <summary>
    /// Thiết lập các cột chi tiết theo chuyên đề báo cáo đang chọn.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>BaoCaoDuLieu.CotChiTiet</c>.</remarks>
    /// <param name="report">Giá trị báo cáo dùng khi thực hiện thao tác; kiểu ChuyenDeBaoCao.</param>
    private void CauHinhChiTiet(ChuyenDeBaoCao report)
    {
        var headers = BaoCaoDuLieu.CotChiTiet(report.Ma);
        DetailsPanel.Visibility = headers.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        if (headers.Length == 0) return;
        DetailsTitle.Text = report.Ma switch
        {
            "11" => "Bảng lương đã chốt", "13" => "Nhập – xuất theo mặt hàng",
            "14" => "Danh sách lô theo hạn dùng", _ => "Các đơn gửi trong kỳ"
        };
        DetailsHint.Text = report.Ma switch
        {
            "11" => "Giá trị đã chốt từ bảng lương; có mã bảng lương và nhân viên để đối chiếu.",
            "13" => "Mỗi dòng là một mặt hàng và đơn vị tính. Không cộng gộp số lượng của các đơn vị khác nhau.",
            "14" => "Danh mục lô đã nhập đến ngày chốt; số ngày âm nghĩa là đã quá hạn. Bảng không thể hiện số dư tồn kho.",
            _ => "Đơn được chọn theo ngày gửi. Kết quả duyệt, số ngày nghỉ và thay thế phản ánh trạng thái hiện tại của đơn."
        };
        var columns = new[] { DetailMa, DetailTen, DetailPhanLoai, DetailDonVi, DetailNgay, DetailDenNgay,
            DetailMot, DetailHai, DetailBa, DetailBon, DetailTrangThai, DetailGhiChu };
        for (var i = 0; i < columns.Length; i++)
        {
            columns[i].Header = headers[i]; columns[i].Visibility = headers[i].Length == 0 ? Visibility.Collapsed : Visibility.Visible;
            if (i is >= 6 and <= 9)
            {
                var format = report.Ma == "11" && i < 9 || report.Ma == "13" && i == 8 ? "N2"
                    : report.Ma == "13" ? "N3" : "N0";
                columns[i].Binding = new Binding(new[] { "Mot", "Hai", "Ba", "Bon" }[i - 6])
                { Mode = BindingMode.OneWay, StringFormat = format, ConverterCulture = Vietnamese, TargetNullValue = "—" };
            }
        }
    }
    /// <summary>
    /// Tiếp nhận thay đổi tìm kiểm và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>HienDanhMuc</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void TimKiem_Changed(object sender, TextChangedEventArgs e) => HienDanhMuc();
    /// <summary>
    /// Tiếp nhận thay đổi chuyên đề và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ChonBaoCao</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ChuyenDe_Changed(object sender, RoutedEventArgs e)
    {
        if (ready && !changingSelection && sender is RadioButton { DataContext: MucBaoCao item }) ChonBaoCao(item.ChuyenDe.Ma);
    }
    /// <summary>
    /// Tiếp nhận thay đổi bộ lọc và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XoaKetQua</c>, <c>ThongBao</c>, <c>KiemTraNgay</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void BoLoc_Changed(object sender, RoutedEventArgs e)
    {
        if (!ready || selected is null) return;
        XoaKetQua();
        ThongBao(selected.DaKetNoi ? "Bộ lọc đã thay đổi. Nhấn Tải báo cáo để cập nhật kết quả." : "Chuyên đề chưa tích hợp nguồn dữ liệu; chưa có số liệu để tải hoặc xuất.");
        KiemTraNgay(out _, out _);
    }
    /// <summary>
    /// Nhận diện nội dung ngày còn đang chỉnh để tránh dùng dữ liệu chưa hợp lệ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>VisualTreeHelper.GetParent</c>, <c>BoLoc_Changed</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NgayDangSua(object sender, TextChangedEventArgs e)
    {
        if (!ready || e.OriginalSource is not DependencyObject element) return;
        for (DependencyObject? node = element; node is not null && node != this; node = VisualTreeHelper.GetParent(node))
            if (node is DatePicker) { BoLoc_Changed(sender, e); break; }
    }
    /// <summary>
    /// Kiểm tra dấu lỗi hoặc nội dung ngày chưa hợp lệ của bộ lọc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XoaKetQua</c>, <c>LoiBoLoc</c>, <c>ThongBao</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NgayKhongHopLe(object? sender, DatePickerDateValidationErrorEventArgs e)
    {
        e.ThrowException = false;
        if (!ready) return;
        XoaKetQua(); LoiBoLoc("Ngày không hợp lệ. Vui lòng nhập theo định dạng dd/MM/yyyy.");
        ThongBao("Ngày vừa nhập không hợp lệ. Hãy sửa ngày hoặc chọn lại trên lịch.", true);
    }
    /// <summary>
    /// Xử lý thao tác chọn nhanh khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>today.AddDays</c>, <c>BoLoc_Changed</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ChonNhanh_Click(object sender, RoutedEventArgs e)
    {
        ready = false;
        try
        {
            var today = DateTime.Today;
            FromDate.SelectedDate = (sender as Button)?.Tag as string switch
            { "Today" => today, "Week" => today.AddDays(-6), _ => new DateTime(today.Year, today.Month, 1) };
            ToDate.SelectedDate = today;
        }
        finally { ready = true; }
        BoLoc_Changed(sender, e);
    }
    /// <summary>
    /// Hiển thị lỗi của bộ lọc và ngăn dùng tiêu chí chưa hợp lệ để tải báo cáo.
    /// </summary>
    /// <param name="message">Nội dung thông báo được chuyển đến nơi gọi hoặc hiển thị cho người dùng.</param>
    private void LoiBoLoc(string message)
    { FilterError.Text = message; FilterError.Visibility = message.Length == 0 ? Visibility.Collapsed : Visibility.Visible; }
    /// <summary>
    /// Kiểm tra khoảng ngày theo quy tắc bộ lọc của báo cáo.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>KiemTraNhapLieu.ReadDate</c>, <c>ReadDate</c>, <c>LoiBoLoc</c>.</remarks>
    /// <param name="from">Giá trị từ dùng khi thực hiện thao tác; kiểu DateTime.</param>
    /// <param name="to">Giá trị đến dùng khi thực hiện thao tác; kiểu DateTime.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    private bool KiemTraNgay(out DateTime from, out DateTime to)
    {
        from = default; to = default;
        var start = selected?.TheoThoiDiem == true ? AsOfDate : FromDate;
        var end = selected?.TheoThoiDiem == true ? AsOfDate : ToDate;
        bool ReadDate(DatePicker picker, out DateTime date)
        {
            date = default;
            try { if (KiemTraNhapLieu.ReadDate(picker) is DateTime value) { date = value.Date; return true; } }
            catch (InvalidOperationException) { }
            return false;
        }
        if (!ReadDate(start, out from) || !ReadDate(end, out to)) { LoiBoLoc("Nhập đầy đủ ngày hợp lệ theo định dạng dd/MM/yyyy."); return false; }
        if (from.Date > to.Date) { LoiBoLoc("Từ ngày phải trước hoặc bằng Đến ngày."); return false; }
        if ((to.Date - from.Date).TotalDays > 3660) { LoiBoLoc("Vui lòng chọn khoảng báo cáo tối đa 10 năm."); return false; }
        if (to.Date == DateTime.MaxValue.Date) { LoiBoLoc("Ngày kết thúc nằm ngoài phạm vi hỗ trợ."); return false; }
        LoiBoLoc(""); return true;
    }

    /// <summary>
    /// Xử lý thao tác tải khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TaiBaoCaoAsync</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private async void Tai_Click(object sender, RoutedEventArgs e) => await TaiBaoCaoAsync();
    /// <summary>
    /// Đọc báo cáo bất đồng bộ theo các tiêu chí đã kiểm tra, cập nhật kết quả và trạng thái đang tải.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XoaKetQua</c>, <c>ThongBao</c>, <c>KiemTraNgay</c>, <c>Task.Run</c>.</remarks>
    /// <returns>Tác vụ bất đồng bộ để nơi gọi theo dõi thời điểm kết thúc và kết quả xử lý.</returns>
    public async Task TaiBaoCaoAsync()
    {
        if (loading) return;
        XoaKetQua();
        if (!PhienDangNhap.IsAdmin) { ThongBao("Tài khoản hiện tại không có quyền đọc báo cáo.", true); return; }
        if (selected is null || !selected.DaKetNoi) { ThongBao("Chuyên đề chưa tích hợp nguồn dữ liệu."); return; }
        if (!KiemTraNgay(out var from, out var to)) { ThongBao("Kiểm tra lại bộ lọc ngày trước khi tải báo cáo.", true); return; }
        var report = selected; var mode = report.TheoThoiDiem ? "Ngày" : (PeriodMode.SelectedItem as ComboBoxItem)?.Content as string ?? "Ngày";
        var threshold = (ExpiryThreshold.SelectedIndex + 1) * 30;
        var request = ++version;
        loading = true; LoadButton.IsEnabled = false; LoadButton.Content = "Đang tải…"; LoadingProgress.Visibility = Visibility.Visible;
        ThongBao("Đang đọc dữ liệu báo cáo. Bạn có thể tiếp tục chọn chuyên đề hoặc điều chỉnh bộ lọc.");
        try
        {
            var data = await Task.Run(() => BaoCaoDuLieu.TongHop(report.Ma,
                read is null ? BaoCaoDuLieu.Doc(report.Ma, from, to, threshold) : read(report.Ma), from, to, mode));
            if (request != version) return;
            if (!PhienDangNhap.IsAdmin) { ThongBao("Phiên hiện tại không còn quyền xem báo cáo.", true); return; }
            result = data; appliedFrom = from; appliedTo = to; appliedMode = mode; appliedThreshold = threshold;
            HienKetQua(report, data);
            ThongBao(report.TheoThoiDiem ? $"Đã kiểm tra {data.ChiTiet.Length} lô · Ngày chốt {from:dd/MM/yyyy} · Ngưỡng {threshold} ngày · Cập nhật {DateTime.Now:HH:mm:ss}"
                : data.SoPhatSinh == 0 ? $"Không có phát sinh trong khoảng {from:dd/MM/yyyy} – {to:dd/MM/yyyy}. Các kỳ trong bảng có giá trị 0."
                : $"Đã tải {from:dd/MM/yyyy} – {to:dd/MM/yyyy} · Nhóm theo {mode.ToLowerInvariant()} · Cập nhật {DateTime.Now:HH:mm:ss}", false, true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or UnauthorizedAccessException or NotSupportedException or System.Data.Common.DbException or TimeoutException)
        {
            if (request != version) return;
            XoaKetQua(); EmptyTitle.Text = "Không tải được báo cáo"; EmptyDescription.Text = "Kiểm tra kết nối và bộ lọc, sau đó nhấn Thử lại.";
            var message = ex.GetBaseException() is System.Data.Common.DbException
                ? "Không truy cập được máy chủ dữ liệu. Kiểm tra kết nối rồi chọn Thử lại."
                : ex.Message + " Không có kết quả để xuất. Vui lòng thử lại.";
            ThongBao(message, true);
            LoadButton.Content = "Thử lại";
        }
        finally
        {
            loading = false; LoadingProgress.Visibility = Visibility.Collapsed;
            LoadButton.IsEnabled = PhienDangNhap.IsAdmin && selected?.DaKetNoi == true;
            if (!Equals(LoadButton.Content, "Thử lại")) LoadButton.Content = selected?.DaKetNoi == true ? "Tải báo cáo" : "Chưa tích hợp dữ liệu";
        }
    }

    /// <summary>
    /// Xóa các bảng/chỉ số kết quả đang hiển thị khi đổi chuyên đề hoặc cần đặt lại màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Array.Empty&lt;ChartPoint&gt;</c>, <c>LoiBoLoc</c>, <c>HienChiSo</c>.</remarks>
    private void XoaKetQua()
    {
        version++; result = null; ExportButton.IsEnabled = false; ReportGrid.ItemsSource = null;
        ReportChart.Points = Array.Empty<ChartPoint>(); CompositionItems.ItemsSource = null;
        ReportChart.MinWidth = 0; DetailsGrid.ItemsSource = null; DetailsCount.Text = "Chưa tải dữ liệu";
        DetailsEmpty.Visibility = Visibility.Visible; DetailsEmpty.Text = "Tải báo cáo để xem chi tiết từ dữ liệu nguồn.";
        CompositionEmpty.Visibility = Visibility.Visible; CompositionEmpty.Text = "Cơ cấu sẽ hiển thị sau khi tải dữ liệu.";
        TableEmpty.Visibility = Visibility.Visible; EmptyTitle.Text = "Chưa có kết quả để đối chiếu";
        EmptyDescription.Text = "Tải báo cáo để xem các kỳ và số liệu tương ứng."; RowCount.Text = "Chưa tải dữ liệu";
        TrendCaption.Text = "Biểu đồ và bảng sử dụng cùng bộ lọc.";
        TrendTitle.Text = "Xu hướng theo " + ((PeriodMode.SelectedItem as ComboBoxItem)?.Content as string ?? "Ngày").ToLowerInvariant();
        LoiBoLoc(""); HienChiSo(null);
    }
    /// <summary>
    /// Đưa các chỉ số tổng hợp của báo cáo lên vùng thống kê.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>selected.ChiSo.Select</c>, <c>values[i]!.Value.ToString</c>.</remarks>
    /// <param name="total">Giá trị tổng dùng khi thực hiện thao tác; kiểu DongBaoCao?.</param>
    private void HienChiSo(DongBaoCao? total)
    {
        if (selected is null) { MetricItems.ItemsSource = null; return; }
        var values = total is null ? null : new[] { total.Mot, total.Hai, total.Ba, total.Bon };
        MetricItems.ItemsSource = selected.ChiSo.Select((name, i) => new ChiSoBaoCao(name,
            values is null || values[i] is null ? "—" : values[i]!.Value.ToString(selected.DonVi[i] is "VNĐ" or "%" ? "N2" : "N0", Vietnamese),
            selected.DonVi[i], Colors[i], selected.QuyTac)).ToArray();
    }
    /// <summary>
    /// Hiển thị các dòng kết quả và dữ liệu biểu đồ/chi tiết của báo cáo đã tải.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>HienChiSo</c>, <c>appliedMode.ToLowerInvariant</c>, <c>Array.Empty&lt;ChartPoint&gt;</c>, <c>data.CacKy.Select</c>.</remarks>
    /// <param name="report">Giá trị báo cáo dùng khi thực hiện thao tác; kiểu ChuyenDeBaoCao.</param>
    /// <param name="data">Giá trị dữ liệu dùng khi thực hiện thao tác; kiểu KetQuaBaoCao.</param>
    private void HienKetQua(ChuyenDeBaoCao report, KetQuaBaoCao data)
    {
        HienChiSo(data.Tong); ReportGrid.ItemsSource = data.CacKy;
        TableEmpty.Visibility = Visibility.Collapsed;
        TrendTitle.Text = "Xu hướng theo " + appliedMode.ToLowerInvariant();
        string Label(DongBaoCao row) => appliedMode == "Tháng" ? $"Tháng {row.TuKy:MM/yyyy}" : appliedMode == "Tuần"
            ? $"{row.TuKy:dd/MM} –\n{row.DenKy:dd/MM}" : $"{row.TuKy:dd/MM}\n{row.TuKy:yyyy}";
        // Một điểm khớp đúng một kỳ trong bảng. Khoảng dài cuộn ngang, không cộng gộp các kỳ.
        ReportChart.Points = report.TheoThoiDiem ? Array.Empty<ChartPoint>() : data.CacKy.Select(row =>
            new ChartPoint(Label(row), (double)row.Mot, (double)row.Hai,
                $"{row.Ky} · Dữ liệu từ {row.TuKy:dd/MM/yyyy} đến {row.DenKy:dd/MM/yyyy}")).ToArray();
        ReportChart.MinWidth = report.TheoThoiDiem ? 0 : data.CacKy.Length * (appliedMode == "Ngày" ? 76 : 125) + 94;
        TrendScroll.ScrollToLeftEnd();
        TrendCaption.Text = $"Mỗi cặp cột = 1 {appliedMode.ToLowerInvariant()} · {data.CacKy.Length} kỳ. Cuộn ngang để xem đủ; kỳ đầu/cuối chỉ lấy ngày trong bộ lọc.";
        CompositionItems.ItemsSource = data.CoCau.Select(row => new { row.Ten, row.TiLe, HienThi = row.GiaTri.ToString(report.Ma is "09" or "11" ? "N2" : "N0", Vietnamese) });
        CompositionEmpty.Visibility = data.CoCau.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        CompositionEmpty.Text = "Không có phát sinh để phân tích cơ cấu.";
        RowCount.Text = $"{data.CacKy.Length} kỳ · {appliedFrom:dd/MM/yyyy} – {appliedTo:dd/MM/yyyy} · Dữ liệu chỉ đọc";
        TableHint.Text = "Bảng tổng hợp đầy đủ các kỳ; chưa bao gồm danh sách chứng từ nguồn.";
        DetailsGrid.ItemsSource = data.ChiTiet;
        DetailsEmpty.Visibility = data.ChiTiet.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        DetailsEmpty.Text = "Không có dữ liệu chi tiết phù hợp với bộ lọc.";
        DetailsCount.Text = $"{data.ChiTiet.Length} dòng · " + (report.TheoThoiDiem ? $"Ngày chốt {appliedFrom:dd/MM/yyyy} · Ngưỡng {appliedThreshold} ngày" : $"{appliedFrom:dd/MM/yyyy} – {appliedTo:dd/MM/yyyy}");
        ExportButton.IsEnabled = PhienDangNhap.IsAdmin;
    }
    /// <summary>
    /// Cập nhật nội dung và cách hiển thị vùng thông báo của màn hình.
    /// </summary>
    /// <param name="message">Nội dung thông báo được chuyển đến nơi gọi hoặc hiển thị cho người dùng.</param>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu bool.</param>
    /// <param name="success">Giá trị thành công dùng khi thực hiện thao tác; kiểu bool.</param>
    private void ThongBao(string message, bool error = false, bool success = false)
    {
        PageStatus.Text = message;
        PageStatus.Foreground = (Brush)new BrushConverter().ConvertFromString(error ? "#A12D26" : success ? "#176344" : "#1D5AAC")!;
        StatusSurface.Background = (Brush)new BrushConverter().ConvertFromString(error ? "#FFF0EE" : success ? "#E9F5EE" : "#EAF2FF")!;
    }
    /// <summary>
    /// Xử lý thao tác xuất khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>dialog.ShowDialog</c>, <c>File.WriteAllText</c>, <c>BaoCaoDuLieu.TaoCsv</c>, <c>ThongBao</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Xuat_Click(object sender, RoutedEventArgs e)
    {
        if (!PhienDangNhap.IsAdmin || result is null || selected is null || loading) return;
        var data = result; var report = selected; var generation = version;
        var dialog = new SaveFileDialog { Title = "Xuất " + report.Ten, Filter = "Báo cáo CSV (*.csv)|*.csv", DefaultExt = ".csv", AddExtension = true,
            FileName = $"BaoCao_{report.Ma}_{appliedFrom:yyyyMMdd}_{appliedTo:yyyyMMdd}.csv" };
        if (dialog.ShowDialog() != true || generation != version || !PhienDangNhap.IsAdmin) return;
        try
        {
            File.WriteAllText(dialog.FileName, BaoCaoDuLieu.TaoCsv(report, data, appliedFrom, appliedTo, appliedMode,
                PhienDangNhap.TenDangNhap, DateTime.Now, appliedThreshold), new UTF8Encoding(true));
            ThongBao("Đã xuất CSV: " + Path.GetFileName(dialog.FileName), false, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { ThongBao("Không ghi được tệp. Kiểm tra quyền thư mục hoặc đóng tệp đang mở rồi xuất lại.", true); }
    }
    /// <summary>
    /// Điều chỉnh phần hiển thị theo kích thước thực tế của nội dung.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Grid.SetColumn</c>, <c>Grid.SetRow</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NoiDung_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var compact = e.NewSize.Width < 820;
        KpiColumns = compact ? 2 : 4;
        TrendColumn.Width = new GridLength(compact ? 1 : 1.6, GridUnitType.Star);
        ChartGapColumn.Width = new GridLength(compact ? 0 : 14); CompositionColumn.Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        ChartGapRow.Height = new GridLength(compact ? 14 : 0);
        Grid.SetColumn(CompositionPanel, compact ? 0 : 2); Grid.SetRow(CompositionPanel, compact ? 2 : 0);
    }
}
