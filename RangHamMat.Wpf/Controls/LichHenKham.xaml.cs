// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ phần lịch hẹn ngày/tuần/tháng, dữ liệu khối lịch và xử lý chọn lịch hẹn; đã giải thích bố trí lịch trùng giờ và lịch ngoài giờ làm việc.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace RangHamMat.Controls;

/// <summary>
/// Ô thời gian trống của lịch: Date là thời điểm tạo lịch, Label là mô tả cho trình đọc màn hình.
/// </summary>
public sealed record CalendarSlot(DateTime Date)
{
    /// <summary>
    /// Nhãn dễ đọc cho lựa chọn/dòng dữ liệu; với ô lịch là mô tả hành động tạo lịch.
    /// </summary>
    public string Label => $"Thêm lịch hẹn {Date:dd/MM/yyyy HH:mm}";
}
/// <summary>
/// Ngày trên lịch: Date, danh sách Slots và Appointments, tiêu đề tiếng Việt cùng nền đánh dấu hôm nay.
/// </summary>
public sealed record CalendarDay(DateTime Date, CalendarSlot[] Slots, AppointmentDisplay[] Appointments)
{
    /// <summary>
    /// Tiêu đề thứ/ngày tiếng Việt của một ô ngày trên lịch.
    /// </summary>
    public string Heading => Date.ToString("ddd, dd/MM", CultureInfo.GetCultureInfo("vi-VN"));
    /// <summary>
    /// Nhãn ngày/tháng rút gọn dùng trong lưới tháng.
    /// </summary>
    public string MonthHeading => Date.ToString("dd/MM");
    /// <summary>
    /// Màu nền của ô ngày, đánh dấu hôm nay bằng màu nhạt riêng.
    /// </summary>
    public string DaySurface => Date.Date == DateTime.Today ? "#F3F8FF" : "White";
}
/// <summary>
/// Mô hình thẻ lịch từ bản ghi LICH_KHAM: giờ/thời lượng, nhãn danh mục, màu trạng thái, mô tả truy cập và tọa độ làn chống chồng thẻ.
/// </summary>
public sealed class AppointmentDisplay
{
    /// <summary>
    /// Bản ghi nguồn đã đọc để mô hình tra các trường/liên kết; không phải một bản SQL mới được tạo tự động.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Thời điểm bắt đầu lịch hẹn hoặc ngày dự kiến bắt đầu kế hoạch, tùy mô hình hiển thị.
    /// </summary>
    public DateTime Start => UiRecord.Date(Record, "NgayGioKham") ?? DateTime.Today;
    /// <summary>
    /// Thời lượng dự kiến của lịch hẹn; dữ liệu thiếu/không hợp lệ dùng mặc định 30 phút để bố trí thẻ.
    /// </summary>
    public int Duration => int.TryParse(UiRecord.Text(Record, "ThoiLuongDuKien"), out var duration) && duration > 0 ? duration : 30;
    /// <summary>
    /// Thời điểm kết thúc lịch hẹn hoặc ngày dự kiến kết thúc kế hoạch.
    /// </summary>
    public DateTime End => Start.AddMinutes(Duration);
    /// <summary>
    /// Giờ thực hiện hoặc khoảng giờ lịch hẹn được định dạng cho giao diện.
    /// </summary>
    public string Time => $"{Start:HH:mm} – {End:HH:mm}";
    /// <summary>
    /// Thông tin hoặc nhãn bệnh nhân liên kết với bản ghi/hồ sơ đang xem.
    /// </summary>
    public string Patient => DuLieuGiaoDien.Lookup("MaBN", UiRecord.Get(Record, "MaBN"));
    /// <summary>
    /// Nhãn bác sĩ hoặc nhân viên thực hiện được tra từ mã liên kết trong bản ghi nguồn.
    /// </summary>
    public string Doctor => DuLieuGiaoDien.Lookup("MaBS", UiRecord.Get(Record, "MaBS"));
    /// <summary>
    /// Nhãn phòng khám được tra từ danh mục theo mã phòng của bản ghi.
    /// </summary>
    public string Room => DuLieuGiaoDien.Lookup("MaPhong", UiRecord.Get(Record, "MaPhong"));
    /// <summary>
    /// Nhãn dịch vụ liên kết của dòng hoặc lịch hẹn.
    /// </summary>
    public string Service => DuLieuGiaoDien.Lookup("MaDV", UiRecord.Get(Record, "MaDV"));
    /// <summary>
    /// Trạng thái hiển thị của bản ghi; với bước kế hoạch có thể được suy ra thêm từ lượng thực hiện đã xác nhận.
    /// </summary>
    public string Status => UiRecord.Text(Record, "TrangThai");
    /// <summary>
    /// Màu nền thẻ lịch được chọn theo trạng thái, luôn hiển thị cùng nhãn trạng thái bằng chữ.
    /// </summary>
    public string Surface => Status switch { "Đã xác nhận" or "Chờ khám" => "#FFF9D9", "Hoàn thành" => "#E8F5E9", "Hủy" or "Đã đổi" => "#FFEBEE", _ => "#E3F2FD" };
    /// <summary>
    /// Màu đánh dấu thẻ lịch tương ứng trạng thái.
    /// </summary>
    public string Stripe => Status switch { "Đã xác nhận" or "Chờ khám" => "#EDBE45", "Hoàn thành" => "#83BE99", "Hủy" or "Đã đổi" => "#E99C9C", _ => "#82B7EB" };
    /// <summary>
    /// Màu chữ của thẻ lịch ứng với nền/trạng thái.
    /// </summary>
    public string Ink => Status switch { "Đã xác nhận" or "Chờ khám" => "#78530D", "Hoàn thành" => "#1E634F", "Hủy" or "Đã đổi" => "#963F39", _ => "#163C70" };
    /// <summary>
    /// Nhãn giờ và bệnh nhân ngắn gọn cho thẻ trong lịch tháng.
    /// </summary>
    public string MonthLabel => $"{Start:HH:mm} {Patient}";
    /// <summary>
    /// Mô tả đầy đủ giờ/bệnh nhân/bác sĩ/dịch vụ/phòng/trạng thái cho công cụ đọc màn hình.
    /// </summary>
    public string AccessibilityLabel => $"{Time}, {Patient}, {Doctor}, {Service}, {Room}, {Status}. Nhấp để xem chi tiết; chuột phải để đổi trạng thái.";
    /// <summary>
    /// Tọa độ ngang của thẻ lịch trong làn đã phân chia.
    /// </summary>
    public double Left { get; set; }
    /// <summary>
    /// Tọa độ dọc của thẻ lịch theo phút bắt đầu so với giờ đầu bảng.
    /// </summary>
    public double Top { get; set; }
    /// <summary>
    /// Chiều rộng thẻ lịch chia theo số làn trùng giờ.
    /// </summary>
    public double Width { get; set; }
    /// <summary>
    /// Chiều cao thẻ lịch tính từ thời lượng, có giới hạn tối thiểu và giới hạn cuối bảng.
    /// </summary>
    public double Height { get; set; }
    /// <summary>
    /// Chỉ số làn được gán cho lịch trong cụm các lịch chồng giờ.
    /// </summary>
    public int Lane { get; set; }
    /// <summary>
    /// Tổng số làn của cụm lịch giao nhau, dùng chia chiều rộng thẻ.
    /// </summary>
    public int Lanes { get; set; } = 1;
    /// <summary>
    /// Tạo đối tượng AppointmentDisplay. Mô hình thẻ lịch từ bản ghi LICH_KHAM: giờ/thời lượng, nhãn danh mục, màu trạng thái, mô tả truy cập và tọa độ làn chống chồng thẻ.
    /// </summary>
    public AppointmentDisplay(object record) => Record = record;
}
/// <summary>
/// Lịch ngày/tuần/tháng dựng bằng binding XAML; tính các thẻ và phát sự kiện cho màn hình cha mở/sửa/tạo lịch hoặc đổi trạng thái.
/// </summary>
public partial class LichHenKham : UserControl, INotifyPropertyChanged
{
    private object[] items = Array.Empty<object>();
    private DateTime selected = DateTime.Today;
    private bool ready;
    /// <summary>
    /// Chế độ hiển thị của control lịch, quyết định dữ liệu và bố cục cần dựng.
    /// </summary>
    public string Mode { get; private set; } = "Week";
    /// <summary>
    /// Nhãn tiếng Việt của chế độ lịch ngày/tuần/tháng đang xem.
    /// </summary>
    public string ModeLabel => Mode == "Day" ? "Ngày" : Mode == "Month" ? "Tháng" : "Tuần";
    /// <summary>
    /// Cờ hiển thị vùng lịch theo giờ khi chế độ không phải Month.
    /// </summary>
    public bool IsTimeView => Mode != "Month";
    /// <summary>
    /// Cờ hiển thị lưới lịch tháng khi Mode là Month.
    /// </summary>
    public bool IsMonthView => Mode == "Month";
    /// <summary>
    /// Số cột ngày của lịch: 1 cho ngày, 7 cho tuần/tháng.
    /// </summary>
    public int Columns { get; private set; } = 7;
    /// <summary>
    /// Tiêu đề ngày/khoảng tuần/tháng hiện tại của lịch.
    /// </summary>
    public string PeriodTitle { get; private set; } = "";
    /// <summary>
    /// Chiều rộng bảng lịch tính từ kích thước control và số làn tối thiểu để các lịch trùng giờ vẫn đọc được.
    /// </summary>
    public double BoardWidth { get; private set; }
    /// <summary>
    /// Chiều cao vùng thời gian tính theo số ô nửa giờ đang được hiển thị.
    /// </summary>
    public double TimeHeight { get; private set; }
    /// <summary>
    /// Các nhãn giờ HH:mm cho vùng lịch theo giờ.
    /// </summary>
    public string[] TimeLabels { get; private set; } = Array.Empty<string>();
    /// <summary>
    /// Các ngày điều trị hoặc các ô ngày trong lịch được gom để binding.
    /// </summary>
    public CalendarDay[] Days { get; private set; } = Array.Empty<CalendarDay>();
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<object>? OpenRecord;
    public event Action<object>? EditRecord;
    public event Action<object, string>? ChangeStatus;
    public event Action<DateTime>? CreateAppointment;
    /// <summary>
    /// Khởi tạo LichHenKham từ XAML.
    /// </summary>
    public LichHenKham()
    {
        InitializeComponent();
        DataContext = this;
        ready = true;
        Refresh();
    }
    /// <summary>
    /// Nhận snapshot lịch hẹn đã lọc từ màn hình cha rồi dựng lại lịch nếu control đã khởi tạo.
    /// </summary>
    public void SetItems(object[] rows) { items = rows; if (ready) Refresh(); }
    /// <summary>
    /// Tính khoảng ngày/tuần/tháng, mở rộng giờ nếu có lịch ngoài 08–20, chia làn trùng giờ và tính vị trí/kích thước các thẻ lịch; phát PropertyChanged cho XAML.
    /// </summary>
    private void Refresh()
    {
        var period = QuyTacHienThi.Period(selected, Mode);
        Columns = period.Columns;
        PeriodTitle = Mode == "Month" ? $"Tháng {selected:MM/yyyy}" : period.Days == 1 ? selected.ToString("dd/MM/yyyy") : $"{period.Start:dd/MM} – {period.Start.AddDays(6):dd/MM/yyyy}";
        var shown = items.Select(row => new AppointmentDisplay(row)).Where(app => app.Start.Date >= period.Start && app.Start.Date < period.Start.AddDays(period.Days)).ToArray();
        // Giữ khung giờ 08–20; mở rộng nếu có lịch ngoài giờ để không làm biến mất thẻ lịch.
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
    /// <summary>
    /// Xếp các lịch giao nhau vào các làn riêng, tái dùng làn đã kết thúc và gán số làn chung cho từng cụm để các thẻ không đè lên nhau.
    /// </summary>
    private static void LayoutLanes(AppointmentDisplay[] apps)
    {
        var group = new List<AppointmentDisplay>();
        var laneEnds = new List<DateTime>();
        // Chốt số làn của cụm lịch chồng giờ hiện tại và xóa bộ nhớ cụm để xử lý cụm tiếp theo.
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
    /// <summary>
    /// Dịch ngày đang xem theo tháng/ngày/tuần hiện tại rồi dựng lại lịch; direction âm là trước, dương là sau.
    /// </summary>
    private void Move(int direction) { selected = Mode == "Month" ? selected.AddMonths(direction) : selected.AddDays(direction * (Mode == "Day" ? 1 : 7)); Refresh(); }
    /// <summary>
    /// Chuyển về kỳ lịch trước theo chế độ ngày, tuần hoặc tháng đang dùng.
    /// </summary>
    private void Previous_Click(object sender, RoutedEventArgs e) => Move(-1);
    /// <summary>
    /// Chuyển sang kỳ lịch tiếp theo theo chế độ ngày, tuần hoặc tháng đang dùng.
    /// </summary>
    private void Next_Click(object sender, RoutedEventArgs e) => Move(1);
    /// <summary>
    /// Đặt ngày đang xem về hôm nay rồi dựng lại lịch.
    /// </summary>
    private void Today_Click(object sender, RoutedEventArgs e) { selected = DateTime.Today; Refresh(); }
    /// <summary>
    /// Đọc chế độ Day/Week/Month từ Tag nút và đổi bố cục lịch tương ứng.
    /// </summary>
    private void Mode_Click(object sender, RoutedEventArgs e) { Mode = (string)((Button)sender).Tag; Refresh(); }
    /// <summary>
    /// Phát yêu cầu tạo lịch lúc 09:00 của ngày đang xem; màn hình cha quyết định cách mở form và lưu.
    /// </summary>
    private void Add_Click(object sender, RoutedEventArgs e) => CreateAppointment?.Invoke(selected.Date.AddHours(9));
    /// <summary>
    /// Phát yêu cầu tạo lịch tại chính ngày/giờ của ô trống được bấm.
    /// </summary>
    private void Slot_Click(object sender, RoutedEventArgs e) { if (((Button)sender).DataContext is CalendarSlot slot) CreateAppointment?.Invoke(slot.Date); }
    /// <summary>
    /// Phát yêu cầu tạo lịch lúc 09:00 của ô ngày được bấm trong lịch tháng.
    /// </summary>
    private void MonthDay_Click(object sender, RoutedEventArgs e) { if (((Button)sender).DataContext is CalendarDay day) CreateAppointment?.Invoke(day.Date.AddHours(9)); }
    /// <summary>
    /// Phát sự kiện mở bản ghi lịch hẹn của thẻ đang bấm, đánh dấu sự kiện đã xử lý để tránh điều hướng lặp.
    /// </summary>
    private void Appointment_Click(object sender, RoutedEventArgs e) { e.Handled = true; if (((Button)sender).DataContext is AppointmentDisplay appointment) OpenRecord?.Invoke(appointment.Record); }
    /// <summary>
    /// Phát yêu cầu mở chi tiết lịch hẹn từ menu ngữ cảnh của thẻ.
    /// </summary>
    private void Details_Click(object sender, RoutedEventArgs e) { if (((MenuItem)sender).DataContext is AppointmentDisplay app) OpenRecord?.Invoke(app.Record); }
    /// <summary>
    /// Phát yêu cầu sửa/hẹn lại bản ghi lịch cho màn hình cha xử lý.
    /// </summary>
    private void Reschedule_Click(object sender, RoutedEventArgs e) { if (((MenuItem)sender).DataContext is AppointmentDisplay app) EditRecord?.Invoke(app.Record); }
    /// <summary>
    /// Phát bản ghi và trạng thái từ Tag menu cho màn hình cha; control lịch không tự ghi SQL.
    /// </summary>
    private void Status_Click(object sender, RoutedEventArgs e) { if (((MenuItem)sender).DataContext is AppointmentDisplay app) ChangeStatus?.Invoke(app.Record, (string)((MenuItem)sender).Tag); }
    /// <summary>
    /// Tính lại chiều rộng thẻ và làn khi vùng lịch đổi chiều ngang.
    /// </summary>
    private void Board_SizeChanged(object sender, SizeChangedEventArgs e) { if (ready && e.WidthChanged) Refresh(); }
    /// <summary>
    /// Đồng bộ vị trí cuộn ngang của hàng tiêu đề ngày với vùng lịch theo giờ.
    /// </summary>
    private void TimeScroll_Changed(object sender, ScrollChangedEventArgs e) { if (e.HorizontalChange != 0) HeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset); }
}
