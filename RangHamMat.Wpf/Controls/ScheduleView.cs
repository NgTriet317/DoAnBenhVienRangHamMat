using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RangHamMat.Controls;

// A presentation-only weekly schedule. Both modes consume the same rows as their table.
public sealed class ScheduleView : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ScheduleView), new PropertyMetadata(null, SourceChanged));
    public static readonly DependencyProperty ShiftsSourceProperty = DependencyProperty.Register(nameof(ShiftsSource), typeof(IEnumerable), typeof(ScheduleView), new PropertyMetadata(null, SourceChanged));
    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public IEnumerable? ShiftsSource { get => (IEnumerable?)GetValue(ShiftsSourceProperty); set => SetValue(ShiftsSourceProperty, value); }
    public string Mode { get; set; } = "Appointments";
    private readonly DatePicker datePicker = new() { SelectedDate = DateTime.Today, Width = 150, Margin = new Thickness(8, 0, 16, 0) };
    private readonly Grid week = new() { MinWidth = 900 };
    private readonly Grid headers = new() { MinWidth = 900, Height = 54 };
    private readonly TextBlock status = new() { Margin = new Thickness(0, 12, 0, 14), TextWrapping = TextWrapping.Wrap };
    private readonly ScrollViewer scroll;
    private readonly HashSet<INotifyCollectionChanged> subscriptions = new();
    private Brush Brush(string key) => (Brush)FindResource(key);

    public ScheduleView()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        var toolbar = new WrapPanel();
        void AddButton(string label, Action action)
        {
            var button = new Button { Content = label }; button.Click += (_, _) => action(); toolbar.Children.Add(button);
        }
        AddButton("Tuần trước", () => datePicker.SelectedDate = (datePicker.SelectedDate ?? DateTime.Today).AddDays(-7));
        AddButton("Hôm nay", () => { datePicker.SelectedDate = DateTime.Today; Refresh(); });
        AddButton("Tuần sau", () => datePicker.SelectedDate = (datePicker.SelectedDate ?? DateTime.Today).AddDays(7));
        toolbar.Children.Add(datePicker);
        toolbar.Children.Add(new TextBlock { Text = "Lịch theo tuần · khung giờ 00:00–24:00", VerticalAlignment = VerticalAlignment.Center });
        System.Windows.Automation.AutomationProperties.SetName(datePicker, "Chọn ngày trong tuần cần xem");
        datePicker.SelectedDateChanged += (_, _) => Refresh();
        root.Children.Add(toolbar);
        Grid.SetRow(status, 1); root.Children.Add(status);
        scroll = new ScrollViewer { Content = week, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
        var headerScroll = new ScrollViewer { Content = headers, HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(0, 0, SystemParameters.VerticalScrollBarWidth, 0), Focusable = false };
        Grid.SetRow(headerScroll, 2); root.Children.Add(headerScroll);
        scroll.ScrollChanged += (_, e) => headerScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
        week.SizeChanged += (_, _) => headers.Width = week.ActualWidth;
        Grid.SetRow(scroll, 3); root.Children.Add(scroll); Content = root;
        Loaded += (_, _) => { Observe(); Refresh(); scroll.ScrollToVerticalOffset(6 * 48); };
        Unloaded += (_, _) => Unobserve();
    }
    private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (ScheduleView)d;
        if (view.IsLoaded) { view.Observe(); view.Refresh(); }
    }
    private void Changed(object? sender, NotifyCollectionChangedEventArgs e) => Refresh();
    private void Unobserve()
    {
        foreach (var source in subscriptions) source.CollectionChanged -= Changed;
        subscriptions.Clear();
    }
    private void Observe()
    {
        Unobserve();
        foreach (var source in new[] { ItemsSource, ShiftsSource }.OfType<INotifyCollectionChanged>())
            if (subscriptions.Add(source)) source.CollectionChanged += Changed;
    }
    public void Refresh()
    {
        if (!IsLoaded) return;
        var selected = datePicker.SelectedDate ?? DateTime.Today;
        var monday = selected.Date.AddDays(-((7 + (int)selected.DayOfWeek - 1) % 7));
        week.Children.Clear(); week.ColumnDefinitions.Clear(); week.RowDefinitions.Clear();
        headers.Children.Clear(); headers.ColumnDefinitions.Clear();
        headers.ColumnDefinitions.Add(new() { Width = new GridLength(68) });
        for (var i = 0; i < 7; i++) headers.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        week.ColumnDefinitions.Add(new() { Width = new GridLength(68) });
        for (var i = 0; i < 7; i++) week.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        
        for (var hour = 0; hour < 24; hour++) week.RowDefinitions.Add(new() { Height = new GridLength(48) });
        void Cell(UIElement child, int row, int col) { Grid.SetRow(child, row); Grid.SetColumn(child, col); week.Children.Add(child); }
        for (var day = 0; day < 7; day++)
        {
            var date = monday.AddDays(day);
            var header = new Border { Background = Brush(date == DateTime.Today ? "AccentSoft" : "Canvas"), BorderBrush = Brush("Line"), BorderThickness = new Thickness(0, 0, 1, 1), Child = new TextBlock { Text = (day == 6 ? "Chủ nhật" : "Thứ " + (day + 2)) + "\n" + date.ToString("dd/MM"), TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold } };
            Grid.SetColumn(header, day + 1); headers.Children.Add(header);
        }
        for (var hour = 0; hour < 24; hour++)
        {
            Cell(new TextBlock { Text = $"{hour:00}:00", Foreground = Brush("Muted"), Margin = new Thickness(8, 4, 8, 0) }, hour, 0);
            for (var day = 0; day < 7; day++) Cell(new Border { Background = Brushes.White, BorderBrush = Brush("Line"), BorderThickness = new Thickness(0, 0, 1, 1) }, hour, day + 1);
        }
        var count = 0; var unresolved = 0;
        var entries = new List<(DateTime Start, TimeSpan? Duration, string Text)>();
        foreach (var row in ItemsSource?.Cast<object>() ?? Enumerable.Empty<object>())
        {
            if (row == System.Windows.Data.CollectionView.NewItemPlaceholder) continue;
            if (Mode == "Shifts")
            {
                var day = UiRecord.Date(row, "NgayLam");
                var shift = ShiftsSource?.Cast<object>().FirstOrDefault(x => UiRecord.Text(x, "MaCa") == UiRecord.Text(row, "MaCa"));
                var from = shift is null ? null : UiRecord.Time(shift, "GioBatDau");
                var to = shift is null ? null : UiRecord.Time(shift, "GioKetThuc");
                if (day is null || from is null || to is null) { unresolved++; continue; }
                var duration = to.Value - from.Value;
                if (duration <= TimeSpan.Zero) duration += TimeSpan.FromDays(1);
                entries.Add((day.Value.Date + from.Value, duration, "Nhân viên " + UiRecord.Text(row, "MaNV") + " · Phòng " + UiRecord.Text(row, "MaPhong")));
            }
            else
            {
                var date = UiRecord.Date(row, "NgayGioKham");
                if (date is null) { unresolved++; continue; }
                entries.Add((date.Value, null, "Bệnh nhân " + UiRecord.Text(row, "MaBN") + " · BS " + UiRecord.Text(row, "MaBS") + "\n" + UiRecord.Text(row, "TrangThai")));
            }
        }
        // Each day has a separate lane for overlapping records; overnight shifts split at midnight.
        for (var day = 0; day < 7; day++)
        {
            var start = monday.AddDays(day); var end = start.AddDays(1);
            var segments = entries.Where(x => x.Duration is null ? x.Start >= start && x.Start < end : x.Start < end && x.Start + x.Duration > start).OrderBy(x => x.Start).ToList();
            var overlay = new Grid { IsHitTestVisible = true };
            var laneEnds = new List<double>();
            foreach (var entry in segments)
            {
                var top = Math.Max(0, (entry.Start - start).TotalMinutes) / 60 * 48;
                var bottom = entry.Duration is null ? top + 36 : Math.Min(24 * 48, (entry.Start + entry.Duration.Value - start).TotalMinutes / 60 * 48);
                var height = Math.Max(24, bottom - top);
                var lane = laneEnds.FindIndex(x => x <= top);
                if (lane < 0) { lane = laneEnds.Count; laneEnds.Add(0); overlay.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) }); }
                laneEnds[lane] = top + height;
                var block = new Border { Background = Brush("AccentSoft"), CornerRadius = new CornerRadius(6), VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(3, top, 3, 0), Height = Math.Min(height, 24 * 48 - top), Padding = new Thickness(6, 3, 6, 3), ClipToBounds = true };
                var description = entry.Start.ToString("HH:mm") + (entry.Duration is null ? " · Giờ bắt đầu" : "–" + (entry.Start + entry.Duration.Value).ToString("HH:mm")) + "\n" + entry.Text;
                block.ToolTip = description; block.Child = new TextBlock { Text = description, Foreground = Brush("AccentDark"), FontSize = 12, TextWrapping = TextWrapping.Wrap };
                System.Windows.Automation.AutomationProperties.SetName(block, description);
                Grid.SetColumn(block, lane); overlay.Children.Add(block); count++;
            }
            Grid.SetRow(overlay, 0); Grid.SetRowSpan(overlay, 24); Grid.SetColumn(overlay, day + 1); week.Children.Add(overlay);
        }
        status.Foreground = Brush("Muted");
        status.Text = $"{monday:dd/MM/yyyy} – {monday.AddDays(6):dd/MM/yyyy} · " + (count == 0 ? "Chưa có lịch trong tuần này." : "Di chuột lên lịch để xem đầy đủ thông tin.")
            + (Mode == "Appointments" ? " Lịch khám đánh dấu giờ bắt đầu, không giả định thời lượng." : " Ca qua đêm tiếp tục ở cột ngày kế tiếp.")
            + (unresolved == 0 ? "" : $" {unresolved} dòng chưa đủ dữ liệu giờ; xem tại Bảng dữ liệu.");
    }
}
