using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
namespace RangHamMat.Controls;

public sealed record ChartPoint(string Label, double Primary, double Secondary = 0);

// Values are supplied by the reporting layer; this control only lays them out.
public sealed class ChartView : UserControl
{
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(nameof(Points), typeof(IEnumerable<ChartPoint>), typeof(ChartView), new PropertyMetadata(null, OnPointsChanged));
    public IEnumerable<ChartPoint>? Points { get => (IEnumerable<ChartPoint>?)GetValue(PointsProperty); set => SetValue(PointsProperty, value); }
    public string PrimaryLabel { get; set; } = "Giá trị";
    public string SecondaryLabel { get; set; } = "";
    public bool IsLine { get; set; }
    private readonly Canvas plot = new() { ClipToBounds = true };
    private INotifyCollectionChanged? observed;
    public ChartView()
    {
        Content = plot;
        Loaded += (_, _) => { Observe(); Draw(); };
        Unloaded += (_, _) => { if (observed is not null) observed.CollectionChanged -= PointsChanged; observed = null; };
        SizeChanged += (_, _) => Draw();
    }
    private static void OnPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (ChartView)d; if (chart.IsLoaded) { chart.Observe(); chart.Draw(); }
    }
    private void Observe()
    {
        if (observed is not null) observed.CollectionChanged -= PointsChanged;
        observed = Points as INotifyCollectionChanged;
        if (observed is not null) observed.CollectionChanged += PointsChanged;
    }
    private void PointsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Draw();
    private void Draw()
    {
        if (!IsLoaded || ActualWidth < 180 || ActualHeight < 120) return;
        plot.Children.Clear();
        var blue = (Brush)FindResource("Accent"); var dark = (Brush)FindResource("AccentDark"); var muted = (Brush)FindResource("Muted"); var line = (Brush)FindResource("Line");
        var left = 78d; var top = 28d; var width = ActualWidth - left - 16; var height = ActualHeight - 88;
        void Put(UIElement element, double x, double y) { Canvas.SetLeft(element, x); Canvas.SetTop(element, y); plot.Children.Add(element); }
        void Label(string text, double x, double y, Brush color) => Put(new TextBlock { Text = text, Foreground = color, FontSize = 12 }, x, y);
        Label(PrimaryLabel, left, 0, blue);
        if (SecondaryLabel.Length > 0) Label(SecondaryLabel, left + 190, 0, dark);
        var data = (Points ?? Array.Empty<ChartPoint>()).Where(x => double.IsFinite(x.Primary) && double.IsFinite(x.Secondary) && x.Primary >= 0 && x.Secondary >= 0).ToArray();
        var max = data.Length == 0 ? 1 : Math.Max(1, data.Max(x => Math.Max(x.Primary, x.Secondary)));
        for (var i = 0; i <= 4; i++)
        {
            var y = top + height * i / 4;
            plot.Children.Add(new Line { X1 = left, X2 = left + width, Y1 = y, Y2 = y, Stroke = line });
            if (data.Length > 0) Label((max * (4 - i) / 4).ToString("N0"), 0, y - 8, muted);
        }
        if (data.Length == 0)
        {
            Put(new TextBlock { Text = "Chưa có dữ liệu để vẽ đồ thị", Foreground = muted, Width = width, TextAlignment = TextAlignment.Center }, left, top + height / 2);
            Label("Kỳ báo cáo", left + width / 2 - 30, top + height + 18, muted); return;
        }
        var step = width / data.Length; var polyline = new Polyline { Stroke = blue, StrokeThickness = 2 };
        for (var i = 0; i < data.Length; i++)
        {
            var point = data[i]; var center = left + (i + .5) * step;
            var y = top + height - point.Primary / max * height;
            if (IsLine)
            {
                polyline.Points.Add(new Point(center, y));
                Put(new Ellipse { Width = 7, Height = 7, Fill = blue, ToolTip = $"{point.Label}: {point.Primary:N0} VNĐ" }, center - 3.5, y - 3.5);
            }
            else
            {
                var barWidth = Math.Max(1, Math.Min(30, step * .3));
                Put(new Rectangle { Width = barWidth, Height = point.Primary / max * height, Fill = blue, ToolTip = $"{point.Label} · {PrimaryLabel}: {point.Primary:N0} VNĐ" }, center - barWidth - 2, y);
                Put(new Rectangle { Width = barWidth, Height = point.Secondary / max * height, Fill = dark, ToolTip = $"{point.Label} · {SecondaryLabel}: {point.Secondary:N0} VNĐ" }, center + 2, top + height - point.Secondary / max * height);
            }
            var tick = new TextBlock { Text = point.Label, Foreground = muted, FontSize = 12, Width = step, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = point.Label };
            Put(tick, left + i * step, top + height + 12);
        }
        if (IsLine) plot.Children.Add(polyline);
    }
}
