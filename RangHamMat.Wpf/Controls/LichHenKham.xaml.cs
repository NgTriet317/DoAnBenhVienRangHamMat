using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace RangHamMat.Controls;

public sealed record CalendarSlot(DateTime Date)
{
    public string Label => $"Thêm lịch hẹn {Date:dd/MM/yyyy HH:mm}";
}
public sealed record CalendarDay(DateTime Date, CalendarSlot[] Slots, AppointmentDisplay[] Appointments)
{
    public string Heading => Date.ToString("ddd, dd/MM", CultureInfo.GetCultureInfo("vi-VN"));
    public string MonthHeading => Date.ToString("dd/MM");
    public string DaySurface => Date.Date == DateTime.Today ? "#F3F8FF" : "White";
}
public sealed class AppointmentDisplay
{
    public object Record { get; }
    public DateTime Start => UiRecord.Date(Record, "NgayGioKham") ?? DateTime.Today;
    public int Duration => int.TryParse(UiRecord.Text(Record, "ThoiLuongDuKien"), out var duration) && duration > 0 ? duration : 30;
    public DateTime End => Start.AddMinutes(Duration);
    public string Time => $"{Start:HH:mm} – {End:HH:mm}";
    public string Patient => ClinicalData.Lookup("MaBN", UiRecord.Get(Record, "MaBN"));
    public string Doctor => ClinicalData.Lookup("MaBS", UiRecord.Get(Record, "MaBS"));
    public string Room => ClinicalData.Lookup("MaPhong", UiRecord.Get(Record, "MaPhong"));
    public string Service => ClinicalData.Lookup("MaDV", UiRecord.Get(Record, "MaDV"));
    public string Status => UiRecord.Text(Record, "TrangThai");
    public string Surface => Status switch { "Đã xác nhận" or "Chờ khám" => "#FFF9D9", "Hoàn thành" => "#E8F5E9", "Hủy" or "Đã đổi" => "#FFEBEE", _ => "#E3F2FD" };
    public string Stripe => Status switch { "Đã xác nhận" or "Chờ khám" => "#EDBE45", "Hoàn thành" => "#83BE99", "Hủy" or "Đã đổi" => "#E99C9C", _ => "#82B7EB" };
    public string Ink => Status switch { "Đã xác nhận" or "Chờ khám" => "#78530D", "Hoàn thành" => "#1E634F", "Hủy" or "Đã đổi" => "#963F39", _ => "#163C70" };
    public string MonthLabel => $"{Start:HH:mm} {Patient}";
    public string AccessibilityLabel => $"{Time}, {Patient}, {Doctor}, {Service}, {Room}, {Status}. Nhấp để xem chi tiết; chuột phải để đổi trạng thái.";
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public int Lane { get; set; }
    public int Lanes { get; set; } = 1;
    public AppointmentDisplay(object record) => Record = record;
}
public partial class LichHenKham : UserControl, INotifyPropertyChanged
{
    private object[] items = Array.Empty<object>();
    private DateTime selected = DateTime.Today;
    private bool ready;
    public string Mode { get; private set; } = "Week";
    public string ModeLabel => Mode == "Day" ? "Ngày" : Mode == "Month" ? "Tháng" : "Tuần";
    public bool IsTimeView => Mode != "Month";
    public bool IsMonthView => Mode == "Month";
    public int Columns { get; private set; } = 7;
    public string PeriodTitle { get; private set; } = "";
    public double BoardWidth { get; private set; }
    public double TimeHeight { get; private set; }
    public string[] TimeLabels { get; private set; } = Array.Empty<string>();
    public CalendarDay[] Days { get; private set; } = Array.Empty<CalendarDay>();
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<object>? OpenRecord;
    public event Action<object>? EditRecord;
    public event Action<object, string>? ChangeStatus;
    public event Action<DateTime>? CreateAppointment;
    public LichHenKham()
    {
        InitializeComponent();
        DataContext = this;
        ready = true;
        Refresh();
    }
    public void SetItems(object[] rows) { items = rows; if (ready) Refresh(); }
    private void Refresh()
    {
        var period = ClinicalRules.Period(selected, Mode);
        Columns = period.Columns;
        PeriodTitle = Mode == "Month" ? $"Tháng {selected:MM/yyyy}" : period.Days == 1 ? selected.ToString("dd/MM/yyyy") : $"{period.Start:dd/MM} – {period.Start.AddDays(6):dd/MM/yyyy}";
        var shown = items.Select(row => new AppointmentDisplay(row)).Where(app => app.Start.Date >= period.Start && app.Start.Date < period.Start.AddDays(period.Days)).ToArray();
        // Keep the reference 08–20 range; expand for records outside office hours so none disappear.
        var startMinute = Math.Min(480, shown.Length == 0 ? 480 : (int)shown.Min(app => app.Start.TimeOfDay.TotalMinutes) / 30 * 30);
        var endMinute = Math.Max(1200, shown.Length == 0 ? 1200 : (int)Math.Ceiling(shown.Max(app => Math.Min(1440, app.Start.TimeOfDay.TotalMinutes + app.Duration)) / 30) * 30);
        TimeLabels = Enumerable.Range(0, (endMinute - startMinute) / 30).Select(i => TimeSpan.FromMinutes(startMinute + i * 30).ToString(@"hh\:mm")).ToArray();
        TimeHeight = TimeLabels.Length * 60;
        Days = Enumerable.Range(0, period.Days).Select(offset =>
        {
            var date = period.Start.AddDays(offset);
            var apps = shown.Where(app => app.Start.Date == date).OrderBy(app => app.Start).ToArray();
            LayoutLanes(apps);
            return new CalendarDay(date, Enumerable.Range(0, TimeLabels.Length).Select(i => new CalendarSlot(date.AddMinutes(startMinute + i * 30))).ToArray(), apps);
        }).ToArray();
        var maxLanes = Math.Max(1, shown.Select(app => app.Lanes).DefaultIfEmpty(1).Max());
        BoardWidth = Math.Max(Math.Max(0, ActualWidth - 18), 72 + Columns * (maxLanes > 1 ? maxLanes * 170 : 90));
        var dayWidth = (BoardWidth - 72) / Columns;
        foreach (var app in shown)
        {
            app.Width = dayWidth / app.Lanes - 2;
            app.Left = app.Lane * dayWidth / app.Lanes;
            app.Top = (app.Start.TimeOfDay.TotalMinutes - startMinute) * 2;
            app.Height = Math.Max(20, Math.Min(app.Duration * 2, TimeHeight - app.Top) - 2);
        }
        foreach (var property in new[] { nameof(Columns), nameof(PeriodTitle), nameof(Days), nameof(Mode), nameof(ModeLabel), nameof(IsTimeView), nameof(IsMonthView), nameof(BoardWidth), nameof(TimeHeight), nameof(TimeLabels) }) PropertyChanged?.Invoke(this, new(property));
    }
    private static void LayoutLanes(AppointmentDisplay[] apps)
    {
        var group = new List<AppointmentDisplay>();
        var laneEnds = new List<DateTime>();
        void Finish() { foreach (var item in group) item.Lanes = laneEnds.Count; group.Clear(); laneEnds.Clear(); }
        foreach (var app in apps)
        {
            if (group.Count > 0 && laneEnds.All(end => end <= app.Start)) Finish();
            var lane = laneEnds.FindIndex(end => end <= app.Start);
            if (lane < 0) { lane = laneEnds.Count; laneEnds.Add(app.End); } else laneEnds[lane] = app.End;
            app.Lane = lane;
            group.Add(app);
        }
        Finish();
    }
    private void Move(int direction) { selected = Mode == "Month" ? selected.AddMonths(direction) : selected.AddDays(direction * (Mode == "Day" ? 1 : 7)); Refresh(); }
    private void Previous_Click(object sender, RoutedEventArgs e) => Move(-1);
    private void Next_Click(object sender, RoutedEventArgs e) => Move(1);
    private void Today_Click(object sender, RoutedEventArgs e) { selected = DateTime.Today; Refresh(); }
    private void Mode_Click(object sender, RoutedEventArgs e) { Mode = (string)((Button)sender).Tag; Refresh(); }
    private void Add_Click(object sender, RoutedEventArgs e) => CreateAppointment?.Invoke(selected.Date.AddHours(9));
    private void Slot_Click(object sender, RoutedEventArgs e) { if (((Button)sender).DataContext is CalendarSlot slot) CreateAppointment?.Invoke(slot.Date); }
    private void MonthDay_Click(object sender, RoutedEventArgs e) { if (((Button)sender).DataContext is CalendarDay day) CreateAppointment?.Invoke(day.Date.AddHours(9)); }
    private void Appointment_Click(object sender, RoutedEventArgs e) { e.Handled = true; if (((Button)sender).DataContext is AppointmentDisplay appointment) OpenRecord?.Invoke(appointment.Record); }
    private void Details_Click(object sender, RoutedEventArgs e) { if (((MenuItem)sender).DataContext is AppointmentDisplay app) OpenRecord?.Invoke(app.Record); }
    private void Reschedule_Click(object sender, RoutedEventArgs e) { if (((MenuItem)sender).DataContext is AppointmentDisplay app) EditRecord?.Invoke(app.Record); }
    private void Status_Click(object sender, RoutedEventArgs e) { if (((MenuItem)sender).DataContext is AppointmentDisplay app) ChangeStatus?.Invoke(app.Record, (string)((MenuItem)sender).Tag); }
    private void Board_SizeChanged(object sender, SizeChangedEventArgs e) { if (ready && e.WidthChanged) Refresh(); }
    private void TimeScroll_Changed(object sender, ScrollChangedEventArgs e) { if (e.HorizontalChange != 0) HeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset); }
}
