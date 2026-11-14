// PHẦN AI ĐÃ THỰC HIỆN: AI đã tạo phần dữ liệu binding ChartTick/ChartPlotPoint, chuyển bố cục sang ChartView.xaml, tính thang đo/tọa độ cột và đường, xử lý đổi nguồn/resize và trạng thái không có dữ liệu.
// AI hỗ trợ: chuyển bố cục biểu đồ sang ChartView.xaml và giải thích cách tính dữ liệu hiển thị.
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RangHamMat.Controls;

/// <summary>Một điểm báo cáo: Label là kỳ, Primary/Secondary là hai số liệu do màn hình cha cung cấp.</summary>
public sealed record ChartPoint(string Label, double Primary, double Secondary = 0, string? PeriodDescription = null);

/// <summary>Nhãn trục tung và tọa độ Top đã tính; XAML dùng cùng dữ liệu để hiện nhãn và đường lưới.</summary>
public sealed record ChartTick(string Label, double Top);

/// <summary>
/// Dữ liệu một điểm/cặp cột: các tọa độ và chiều cao tính từ thang đo, nhãn kỳ, tooltip và cờ hiển thị cột/điểm.
/// Đây là dữ liệu binding; không chứa hoặc tạo phần tử giao diện.
/// </summary>
public sealed record ChartPlotPoint(string Label, double LabelLeft, double LabelWidth, double PrimaryLeft, double SecondaryLeft,
    double BarWidth, double PrimaryTop, double SecondaryTop, double PrimaryHeight, double SecondaryHeight,
    double DotLeft, double DotTop, string PrimaryTip, string SecondaryTip, bool ShowBars, bool ShowDot,
    double SecondaryDotTop = 0);

/// <summary>Biểu đồ báo cáo có bố cục hiện hữu trong XAML; C# chỉ theo dõi nguồn và tính thang đo/tọa độ cho binding.</summary>
public sealed partial class ChartView : UserControl, INotifyPropertyChanged
{
    /// <summary>Dependency property nguồn điểm cho binding từ màn hình báo cáo.</summary>
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(nameof(Points), typeof(IEnumerable<ChartPoint>), typeof(ChartView), new PropertyMetadata(null, OnPointsChanged));
    /// <summary>Nhãn chuỗi chính; đổi nhãn làm mới tooltip và chú giải.</summary>
    public static readonly DependencyProperty PrimaryLabelProperty = DependencyProperty.Register(nameof(PrimaryLabel), typeof(string), typeof(ChartView), new PropertyMetadata("Giá trị", OnPresentationChanged));
    /// <summary>Nhãn chuỗi phụ; có thể để trống nếu báo cáo chỉ dùng chuỗi chính.</summary>
    public static readonly DependencyProperty SecondaryLabelProperty = DependencyProperty.Register(nameof(SecondaryLabel), typeof(string), typeof(ChartView), new PropertyMetadata("", OnPresentationChanged));
    /// <summary>Chọn biểu đồ đường hoặc cột; dùng dependency property để thay chế độ sau Loaded vẫn cập nhật.</summary>
    public static readonly DependencyProperty IsLineProperty = DependencyProperty.Register(nameof(IsLine), typeof(bool), typeof(ChartView), new PropertyMetadata(false, OnPresentationChanged));
    /// <summary>
    /// Giá trị lưu nội bộ của thuộc tính Unit, dùng bởi getter/setter tương ứng.
    /// </summary>
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(nameof(Unit), typeof(string), typeof(ChartView), new PropertyMetadata("VNĐ", OnPresentationChanged));
    /// <summary>Đơn vị tooltip do chuyên đề cung cấp; chuỗi rỗng cho dữ liệu đếm có tên chỉ tiêu rõ ràng.</summary>
    public string Unit { get => (string)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    /// <summary>
    /// Dependency property cho đơn vị chuỗi số liệu phụ; thay đổi giá trị yêu cầu tính lại phần trình bày.
    /// </summary>
    public static readonly DependencyProperty SecondaryUnitProperty = DependencyProperty.Register(nameof(SecondaryUnit), typeof(string), typeof(ChartView), new PropertyMetadata(null, OnPresentationChanged));
    /// <summary>
    /// Đơn vị cho tooltip chuỗi phụ; null để dùng chung đơn vị của chuỗi chính.
    /// </summary>
    public string? SecondaryUnit { get => (string?)GetValue(SecondaryUnitProperty); set => SetValue(SecondaryUnitProperty, value); }
    /// <summary>
    /// Được theo dõi được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private INotifyCollectionChanged? observed;
    /// <summary>Nguồn điểm báo cáo; control không tự lấy hoặc tính tổng tiền từ SQL.</summary>
    public IEnumerable<ChartPoint>? Points { get => (IEnumerable<ChartPoint>?)GetValue(PointsProperty); set => SetValue(PointsProperty, value); }
    /// <summary>Tên số liệu chính trong chú giải.</summary>
    public string PrimaryLabel { get => (string)GetValue(PrimaryLabelProperty); set => SetValue(PrimaryLabelProperty, value); }
    /// <summary>Tên số liệu phụ trong chú giải.</summary>
    public string SecondaryLabel { get => (string)GetValue(SecondaryLabelProperty); set => SetValue(SecondaryLabelProperty, value); }
    /// <summary>True hiện đường/điểm, false hiện hai cột cho mỗi kỳ.</summary>
    public bool IsLine { get => (bool)GetValue(IsLineProperty); set => SetValue(IsLineProperty, value); }
    /// <summary>Năm mốc trục tung từ cực đại về 0, kèm vị trí đường lưới.</summary>
    public ChartTick[] Ticks { get; private set; } = Array.Empty<ChartTick>();
    /// <summary>Các tọa độ cột/điểm và nhãn đã tính cho template XAML.</summary>
    public ChartPlotPoint[] PlotPoints { get; private set; } = Array.Empty<ChartPlotPoint>();
    /// <summary>Tập tọa độ hình học của đường; Polyline được khai báo trong XAML.</summary>
    public PointCollection LinePoints { get; private set; } = new();
    /// <summary>
    /// Các tọa độ của chuỗi phụ để XAML vẽ đường so sánh trên cùng thang đo.
    /// </summary>
    public PointCollection SecondaryLinePoints { get; private set; } = new();
    /// <summary>
    /// Tọa độ dọc của giá trị 0 để cột âm và dương cùng xuất phát từ đúng đường cơ sở.
    /// </summary>
    public double ZeroTop { get; private set; }
    /// <summary>Chiều rộng vùng vẽ hiện có dùng để kéo đường lưới đến mép phải.</summary>
    public double PlotWidth { get; private set; }
    /// <summary>Có ít nhất một điểm số hữu hạn không âm sau khi lọc đầu vào.</summary>
    public bool HasData => PlotPoints.Length > 0;
    /// <summary>Chỉ hiện đường khi chọn chế độ đường và có dữ liệu.</summary>
    public bool ShowLine => IsLine && HasData;
    /// <summary>Thông báo thuộc tính hiển thị thay đổi để WPF cập nhật template.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Nạp ChartView.xaml; theo dõi collection khi control hiện và gỡ theo dõi khi đóng để tránh giữ nguồn cũ.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Nạp ChartView.xaml; theo dõi collection khi control hiện và gỡ theo dõi khi đóng để tránh giữ nguồn cũ.</remarks>
    public ChartView()
    {
        InitializeComponent();
        Loaded += (_, _) => { Observe(); RefreshPlot(); };
        Unloaded += (_, _) => { if (observed is not null) observed.CollectionChanged -= PointsChanged; observed = null; };
    }
    /// <summary>Đăng ký collection mới và tính lại plot khi binding Points được thay nguồn.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Đăng ký collection mới và tính lại plot khi binding Points được thay nguồn.</remarks>
    private static void OnPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    { var chart = (ChartView)d; if (chart.IsLoaded) { chart.Observe(); chart.RefreshPlot(); } }
    /// <summary>Tính lại dữ liệu trình bày khi đổi chế độ hoặc nhãn số liệu.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Tính lại dữ liệu trình bày khi đổi chế độ hoặc nhãn số liệu.</remarks>
    private static void OnPresentationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    { var chart = (ChartView)d; if (chart.IsLoaded) chart.RefreshPlot(); }
    /// <summary>Hủy collection cũ trước khi nghe Changed của nguồn mới; chỉ collection hỗ trợ thông báo mới cần đăng ký.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Hủy collection cũ trước khi nghe Changed của nguồn mới; chỉ collection hỗ trợ thông báo mới cần đăng ký.</remarks>
    private void Observe()
    {
        if (observed is not null) observed.CollectionChanged -= PointsChanged;
        observed = Points as INotifyCollectionChanged;
        if (observed is not null) observed.CollectionChanged += PointsChanged;
    }
    /// <summary>Collection thêm/bỏ điểm thì tính lại toàn bộ thang đo và tọa độ để không giữ số liệu cũ.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Collection thêm/bỏ điểm thì tính lại toàn bộ thang đo và tọa độ để không giữ số liệu cũ.</remarks>
    private void PointsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshPlot();
    /// <summary>Vùng XAML đổi kích thước thì tính lại tọa độ theo kích thước thật, không dựng lại cây control bằng C#.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Vùng XAML đổi kích thước thì tính lại tọa độ theo kích thước thật, không dựng lại cây control bằng C#.</remarks>
    private void Plot_SizeChanged(object sender, SizeChangedEventArgs e) { if (IsLoaded) RefreshPlot(); }
    /// <summary>
    /// Lọc số không hợp lệ/âm, tìm thang đo cực đại tối thiểu 1, chia vùng vẽ theo kỳ rồi tính cột và điểm.
    /// Các collection/geometry trả về chỉ là dữ liệu; ItemsControl và Polyline trong XAML chịu trách nhiệm hiển thị.
    /// </summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Lọc số không hợp lệ/âm, tìm thang đo cực đại tối thiểu 1, chia vùng vẽ theo kỳ rồi tính cột và điểm. Các collection/geometry trả về chỉ là dữ liệu; ItemsControl và Polyline trong XAML chịu trách nhiệm hiển thị.</remarks>
    private void RefreshPlot()
    {
        PlotWidth = Math.Max(0, PlotArea.ActualWidth);
        var height = Math.Max(0, PlotArea.ActualHeight);
        var data = (Points ?? Array.Empty<ChartPoint>()).Where(p => double.IsFinite(p.Primary) && double.IsFinite(p.Secondary)).ToArray();
        var max = data.Length == 0 ? 1 : Math.Max(0,data.Max(p => Math.Max(p.Primary,p.Secondary)));
        var min = data.Length == 0 ? 0 : Math.Min(0,data.Min(p => Math.Min(p.Primary,p.Secondary)));
        if(max==min)max=min+1;
        var range=max-min;
        double Y(double value)=>height*(max-value)/range;
        ZeroTop=Y(0);
        Ticks = Enumerable.Range(0, 5).Select(i => new ChartTick(data.Length == 0 ? "" : (max-range*i/4).ToString(Unit=="VNĐ"?"N0":"0.##"), height*i/4)).ToArray();
        var step = data.Length == 0 ? 0 : PlotWidth / data.Length;
        var bar = Math.Max(1, Math.Min(30, step * .3));
        LinePoints = new PointCollection(data.Select((p, i) => new Point((i + .5) * step, Y(p.Primary))));
        SecondaryLinePoints = new PointCollection(data.Select((p, i) => new Point((i + .5) * step, Y(p.Secondary))));
        PlotPoints = data.Select((p, i) =>
        {
            var center = (i + .5) * step; var primaryY=Y(p.Primary);var secondaryY=Y(p.Secondary);
            var secondUnit=SecondaryUnit??Unit;
            return new ChartPlotPoint(p.Label, i * step, step, center - bar - 2, center + 2, bar,
                Math.Min(ZeroTop,primaryY), Math.Min(ZeroTop,secondaryY), Math.Abs(primaryY-ZeroTop), Math.Abs(secondaryY-ZeroTop), center - 3.5, primaryY - 3.5,
                $"{p.PeriodDescription ?? p.Label} · {PrimaryLabel}: {p.Primary.ToString(Unit == "VNĐ" ? "N2" : "N0")} {Unit}".TrimEnd(),
                $"{p.PeriodDescription ?? p.Label} · {SecondaryLabel}: {p.Secondary.ToString(secondUnit == "VNĐ" ? "N2" : "N0")} {secondUnit}".TrimEnd(), !IsLine, IsLine,secondaryY-3.5);
        }).ToArray();
        foreach (var name in new[] { nameof(Ticks), nameof(PlotPoints), nameof(LinePoints), nameof(SecondaryLinePoints), nameof(ZeroTop), nameof(PlotWidth), nameof(HasData), nameof(ShowLine) }) PropertyChanged?.Invoke(this, new(name));
    }
}
