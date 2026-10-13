// PHẦN AI ĐÃ THỰC HIỆN: AI đã chuyển lịch sang ScheduleView.xaml, tạo dữ liệu binding ngày/khối lịch, chia ca qua nửa đêm, tính làn cho ca trùng giờ và đồng bộ ngày/tuần đang xem.
// AI hỗ trợ: chuyển lịch tuần từ cây control C# sang XAML và giải thích các bước tính lịch/làn.
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace RangHamMat.Controls;

/// <summary>Một đoạn lịch trong ngày: tọa độ, kích thước đã chia làn và mô tả đầy đủ cho tooltip/trình đọc màn hình.</summary>
public sealed record ScheduleBlock(double Top, double Left, double Width, double Height, string Description);
/// <summary>Dữ liệu một cột ngày của lịch tuần: tiêu đề thứ/ngày, cờ hôm nay và các đoạn lịch trong ngày đó.</summary>
public sealed record ScheduleDay(string Heading, bool IsToday, ScheduleBlock[] Blocks);

/// <summary>Lịch tuần có XAML hiện hữu cho lịch khám/ca làm; C# đọc nguồn, tính các đoạn qua đêm và vị trí làn, không tạo control.</summary>
public sealed partial class ScheduleView : UserControl, INotifyPropertyChanged
{
    /// <summary>Nguồn lịch khám/lịch làm việc do màn hình cha binding vào control.</summary>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ScheduleView), new PropertyMetadata(null, SourceChanged));
    /// <summary>Danh mục ca để tra giờ bắt đầu/kết thúc khi chế độ là Shifts.</summary>
    public static readonly DependencyProperty ShiftsSourceProperty = DependencyProperty.Register(nameof(ShiftsSource), typeof(IEnumerable), typeof(ScheduleView), new PropertyMetadata(null, SourceChanged));
    /// <summary>Chế độ lịch để đổi cách đọc nguồn: Appointments hoặc Shifts.</summary>
    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(string), typeof(ScheduleView), new PropertyMetadata("Appointments", SourceChanged));
    private readonly HashSet<INotifyCollectionChanged> subscriptions = new();
    /// <summary>Các bản ghi lịch từ bảng dữ liệu; chỉ hiển thị, không ghi SQL trong control này.</summary>
    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    /// <summary>Các ca chứa MaCa/GioBatDau/GioKetThuc dùng suy ra khoảng thời gian của lịch làm việc.</summary>
    public IEnumerable? ShiftsSource { get => (IEnumerable?)GetValue(ShiftsSourceProperty); set => SetValue(ShiftsSourceProperty, value); }
    /// <summary>Shifts hiện khoảng ca làm; Appointments chỉ đánh dấu giờ hẹn, không giả định thời lượng.</summary>
    public string Mode { get => (string)GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
    /// <summary>Bảy ngày từ thứ Hai với dữ liệu template các thẻ lịch đã chia làn.</summary>
    public ScheduleDay[] Days { get; private set; } = Array.Empty<ScheduleDay>();
    /// <summary>Nhãn 24 giờ từ 00:00 đến 23:00, mỗi ô XAML cao 48 DIP.</summary>
    public string[] HourLabels { get; } = Enumerable.Range(0, 24).Select(hour => $"{hour:00}:00").ToArray();
    /// <summary>Chiều rộng bảng lịch tối thiểu 900; đủ rộng thì lấp đầy vùng cuộn ngang.</summary>
    public double BoardWidth { get; private set; } = 900;
    /// <summary>Khoảng tuần, trạng thái trống và số dòng thiếu thông tin giờ cần xem tại bảng.</summary>
    public string StatusText { get; private set; } = "";
    /// <summary>Thông báo model trình bày đổi để các template XAML binding lại.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Nạp ScheduleView.xaml, chọn hôm nay và chỉ đăng ký nguồn khi Loaded; gỡ đăng ký khi control rời giao diện.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Nạp ScheduleView.xaml, chọn hôm nay và chỉ đăng ký nguồn khi Loaded; gỡ đăng ký khi control rời giao diện.</remarks>
    public ScheduleView()
    {
        InitializeComponent(); WeekDate.SelectedDate = DateTime.Today;
        Loaded += (_, _) => { Observe(); Refresh(); CalendarScroll.ScrollToVerticalOffset(6 * 48); };
        Unloaded += (_, _) => Unobserve();
    }
    /// <summary>Nguồn hoặc chế độ đổi thì đăng ký lại collection và tính lịch nếu control đang hiện.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Nguồn hoặc chế độ đổi thì đăng ký lại collection và tính lịch nếu control đang hiện.</remarks>
    private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    { var view = (ScheduleView)d; if (view.IsLoaded) { view.Observe(); view.Refresh(); } }
    /// <summary>Collection lịch/danh mục ca đổi thì tính lại lịch theo tuần đang xem.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Collection lịch/danh mục ca đổi thì tính lại lịch theo tuần đang xem.</remarks>
    private void Changed(object? sender, NotifyCollectionChangedEventArgs e) => Refresh();
    /// <summary>Hủy mọi sự kiện collection cũ để màn hình đã đóng không bị nguồn giữ tham chiếu.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Hủy mọi sự kiện collection cũ để màn hình đã đóng không bị nguồn giữ tham chiếu.</remarks>
    private void Unobserve() { foreach (var source in subscriptions) source.CollectionChanged -= Changed; subscriptions.Clear(); }
    /// <summary>Đăng ký mỗi collection lịch/ca một lần, sau khi gỡ hết nguồn cũ.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Đăng ký mỗi collection lịch/ca một lần, sau khi gỡ hết nguồn cũ.</remarks>
    private void Observe()
    { Unobserve(); foreach (var source in new[] { ItemsSource, ShiftsSource }.OfType<INotifyCollectionChanged>()) if (subscriptions.Add(source)) source.CollectionChanged += Changed; }
    /// <summary>Đổi về tuần trước bằng cách giảm ngày chọn bảy ngày.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Đổi về tuần trước bằng cách giảm ngày chọn bảy ngày.</remarks>
    private void Previous_Click(object sender, RoutedEventArgs e) => WeekDate.SelectedDate = (WeekDate.SelectedDate ?? DateTime.Today).AddDays(-7);
    /// <summary>Đổi ngày xem về hôm nay; vẫn cập nhật nếu ngày chọn vốn đã là hôm nay.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Đổi ngày xem về hôm nay; vẫn cập nhật nếu ngày chọn vốn đã là hôm nay.</remarks>
    private void Today_Click(object sender, RoutedEventArgs e) { WeekDate.SelectedDate = DateTime.Today; Refresh(); }
    /// <summary>Đổi sang tuần sau bằng cách tăng ngày chọn bảy ngày.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Đổi sang tuần sau bằng cách tăng ngày chọn bảy ngày.</remarks>
    private void Next_Click(object sender, RoutedEventArgs e) => WeekDate.SelectedDate = (WeekDate.SelectedDate ?? DateTime.Today).AddDays(7);
    /// <summary>Người dùng chọn ngày bất kỳ thì tính lại tuần chứa ngày đó.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Người dùng chọn ngày bất kỳ thì tính lại tuần chứa ngày đó.</remarks>
    private void Date_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Refresh(); }
    /// <summary>Đồng bộ tiêu đề khi cuộn ngang và tính lại chiều rộng/làn khi vùng hiển thị thay đổi.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Đồng bộ tiêu đề khi cuộn ngang và tính lại chiều rộng/làn khi vùng hiển thị thay đổi.</remarks>
    private void CalendarScroll_Changed(object sender, ScrollChangedEventArgs e)
    {
        if (e.HorizontalChange != 0) HeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
        if (IsLoaded && Math.Abs(BoardWidth - Math.Max(900, e.ViewportWidth)) > .5) Refresh();
    }
    /// <summary>
    /// Suy ra giờ từ nguồn lịch/ca, bỏ dòng thiếu giờ và tính khoảng tuần thứ Hai–Chủ nhật.
    /// Tách ca qua đêm ở ranh giới ngày, chia làn cho các thẻ chồng nhau và tạo dữ liệu tọa độ để XAML hiển thị.
    /// </summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Suy ra giờ từ nguồn lịch/ca, bỏ dòng thiếu giờ và tính khoảng tuần thứ Hai–Chủ nhật. Tách ca qua đêm ở ranh giới ngày, chia làn cho các thẻ chồng nhau và tạo dữ liệu tọa độ để XAML hiển thị.</remarks>
    public void Refresh()
    {
        if (!IsLoaded) return;
        var selected = WeekDate.SelectedDate ?? DateTime.Today;
        var monday = selected.Date.AddDays(-((7 + (int)selected.DayOfWeek - 1) % 7));
        BoardWidth = Math.Max(900, CalendarScroll.ViewportWidth);
        var dayWidth = (BoardWidth - 68) / 7; var unresolved = 0; var count = 0;
        var entries = new List<(DateTime Start, TimeSpan? Duration, string Text)>();
        foreach (var row in ItemsSource?.Cast<object>() ?? Enumerable.Empty<object>())
        {
            if (row == System.Windows.Data.CollectionView.NewItemPlaceholder) continue;
            if (Mode == "Shifts")
            {
                var day = UiRecord.Date(row, "NgayLam");
                var shift = ShiftsSource?.Cast<object>().FirstOrDefault(x => UiRecord.Text(x, "MaCa") == UiRecord.Text(row, "MaCa"));
                var from = shift is null ? null : UiRecord.Time(shift, "GioBatDau"); var to = shift is null ? null : UiRecord.Time(shift, "GioKetThuc");
                if (day is null || from is null || to is null) { unresolved++; continue; }
                var duration = to.Value - from.Value; if (duration <= TimeSpan.Zero) duration += TimeSpan.FromDays(1);
                entries.Add((day.Value.Date + from.Value, duration, "Nhân viên " + UiRecord.Text(row, "MaNV") + " · Phòng " + UiRecord.Text(row, "MaPhong")));
            }
            else
            {
                var date = UiRecord.Date(row, "NgayGioKham"); if (date is null) { unresolved++; continue; }
                entries.Add((date.Value, null, "Bệnh nhân " + UiRecord.Text(row, "MaBN") + " · BS " + UiRecord.Text(row, "MaBS") + "\n" + UiRecord.Text(row, "TrangThai")));
            }
        }
        var days = new List<ScheduleDay>();
        for (var day = 0; day < 7; day++)
        {
            var start = monday.AddDays(day); var end = start.AddDays(1);
            var segments = entries.Where(x => x.Duration is null ? x.Start >= start && x.Start < end : x.Start < end && x.Start + x.Duration > start).OrderBy(x => x.Start).ToArray();
            var laneEnds = new List<double>(); var positioned = new List<(double Top, double Height, int Lane, string Text)>();
            foreach (var entry in segments)
            {
                var top = Math.Max(0, (entry.Start - start).TotalMinutes) / 60 * 48;
                var bottom = entry.Duration is null ? top + 36 : Math.Min(1152, (entry.Start + entry.Duration.Value - start).TotalMinutes / 60 * 48);
                var height = Math.Min(Math.Max(24, bottom - top), 1152 - top);
                var lane = laneEnds.FindIndex(value => value <= top);
                if (lane < 0) { lane = laneEnds.Count; laneEnds.Add(0); }
                laneEnds[lane] = top + height;
                var description = entry.Start.ToString("HH:mm") + (entry.Duration is null ? " · Giờ bắt đầu" : "–" + (entry.Start + entry.Duration.Value).ToString("HH:mm")) + "\n" + entry.Text;
                positioned.Add((top, height, lane, description)); count++;
            }
            var laneWidth = dayWidth / Math.Max(1, laneEnds.Count);
            var blocks = positioned.Select(item => new ScheduleBlock(item.Top, item.Lane * laneWidth + 3, Math.Max(0, laneWidth - 6), item.Height, item.Text)).ToArray();
            days.Add(new ScheduleDay((day == 6 ? "Chủ nhật" : "Thứ " + (day + 2)) + "\n" + start.ToString("dd/MM"), start == DateTime.Today, blocks));
        }
        Days = days.ToArray();
        StatusText = $"{monday:dd/MM/yyyy} – {monday.AddDays(6):dd/MM/yyyy} · " + (count == 0 ? "Chưa có lịch trong tuần này." : "Di chuột lên lịch để xem đầy đủ thông tin.")
            + (Mode == "Appointments" ? " Lịch khám đánh dấu giờ bắt đầu, không giả định thời lượng." : " Ca qua đêm tiếp tục ở cột ngày kế tiếp.")
            + (unresolved == 0 ? "" : $" {unresolved} dòng chưa đủ dữ liệu giờ; xem tại Bảng dữ liệu.");
        foreach (var name in new[] { nameof(Days), nameof(BoardWidth), nameof(StatusText) }) PropertyChanged?.Invoke(this, new(name));
    }
}
