// AI hỗ trợ: kiểm tra hành vi của các control chuyển từ bố cục C# sang XAML.
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RangHamMat.Controls;

/// <summary>Kiểm tra control WPF thật: biểu đồ cập nhật dữ liệu, lịch chia ca qua đêm/chồng giờ và chi tiết giữ nguồn an toàn.</summary>
internal static class GiaoDienXamlChecks
{
    /// <summary>Chạy các control trong host hiện có; theo dõi lỗi binding và lưu ảnh để kiểm tra phần bố cục XAML đã chuyển.</summary>
    public static void Run(Window host)
    {
        using var log = new StringWriter(); using var listener = new TextWriterTraceListener(log);
        PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
        try { CheckChart(host); CheckSchedule(host); CheckDetails(host); }
        finally { PresentationTraceSources.DataBindingSource.Listeners.Remove(listener); }
        Require(!log.ToString().Contains("Error", StringComparison.OrdinalIgnoreCase), "Binding giao diện XAML bị lỗi: " + log);
        Console.WriteLine("PASS: XAML biểu đồ trống/cột/đường; cập nhật collection/resize; lịch ca qua đêm/chồng giờ; chi tiết cha/con và ẩn mật khẩu.");
    }

    /// <summary>Kiểm tra dữ liệu sai không lên đồ thị, collection thay đổi cập nhật ngay và cùng dữ liệu được dùng cho cột/đường.</summary>
    private static void CheckChart(Window host)
    {
        var chart = new ChartView { Height = 300, PrimaryLabel = "Thu", SecondaryLabel = "Chi" };
        host.Content = chart; Drain(host);
        Require(((TextBlock)chart.FindName("EmptyText")).IsVisible, "Biểu đồ trống phải có hướng dẫn.");
        var data = new ObservableCollection<ChartPoint> { new("Kỳ 1", 100, 50), new("Kỳ 2", 200), new("Sai", double.NaN), new("Âm", -1) };
        chart.Points = data; Drain(host);
        Require(chart.PlotPoints.Length == 2 && !((TextBlock)chart.FindName("EmptyText")).IsVisible, "Chỉ số liệu hữu hạn không âm được vẽ.");
        Require(Math.Abs(chart.PlotPoints[1].PrimaryHeight - chart.PlotPoints[0].PrimaryHeight * 2) < .01, "Giá trị 200 phải có cột cao gấp đôi 100.");
        Require(Visuals(chart).OfType<TextBlock>().Any(text => text.Text == "Kỳ 1"), "Kỳ báo cáo phải hiện qua template XAML.");
        Capture(chart, "bieu-do-cot"); chart.IsLine = true; Drain(host);
        Require(chart.ShowLine && chart.LinePoints.Count == 2 && Visuals(chart).OfType<System.Windows.Shapes.Ellipse>().Any(dot => dot.IsVisible), "Chế độ đường phải hiện cả hai điểm và giữ số liệu.");
        Capture(chart, "bieu-do-duong");
        var priorWidth = chart.PlotWidth; host.Width = 1000; Drain(host);
        Require(chart.PlotWidth < priorWidth && chart.LinePoints.Count == 2, "Đổi kích thước phải đổi tọa độ mà không mất điểm.");
        host.Width = 1366; data.Clear(); Drain(host);
        Require(!chart.HasData && chart.LinePoints.Count == 0 && ((TextBlock)chart.FindName("EmptyText")).IsVisible, "Xóa nguồn phải xóa đồ thị cũ.");
    }

    /// <summary>Ca 22–02 phải hiện hai đoạn ngày kế tiếp; ca 20–23 chồng giờ phải có làn khác, dòng thiếu ca phải được báo.</summary>
    private static void CheckSchedule(Window host)
    {
        var monday = new DateTime(2026, 10, 12);
        var shifts = new ObservableCollection<object>
        {
            new Dictionary<string, object?> { ["MaCa"] = "DEM", ["GioBatDau"] = TimeSpan.FromHours(22), ["GioKetThuc"] = TimeSpan.FromHours(2) },
            new Dictionary<string, object?> { ["MaCa"] = "TOI", ["GioBatDau"] = TimeSpan.FromHours(20), ["GioKetThuc"] = TimeSpan.FromHours(23) }
        };
        var entries = new ObservableCollection<object>
        {
            new Dictionary<string, object?> { ["NgayLam"] = monday, ["MaCa"] = "DEM", ["MaNV"] = "NV-DEM", ["MaPhong"] = "P1" },
            new Dictionary<string, object?> { ["NgayLam"] = monday, ["MaCa"] = "TOI", ["MaNV"] = "NV-TOI", ["MaPhong"] = "P1" },
            new Dictionary<string, object?> { ["NgayLam"] = monday, ["MaCa"] = "THIEU" }
        };
        var schedule = new ScheduleView { Mode = "Shifts", ItemsSource = entries, ShiftsSource = shifts };
        host.Content = schedule; ((DatePicker)schedule.FindName("WeekDate")).SelectedDate = monday; Drain(host);
        Require(schedule.Days.Length == 7 && schedule.Days[0].Blocks.Length == 2 && schedule.Days[1].Blocks.Length == 1, "Ca qua đêm phải có đoạn thứ Hai và thứ Ba.");
        var night = schedule.Days[0].Blocks.Single(block => block.Description.Contains("NV-DEM"));
        Require(Math.Abs(night.Top - 1056) < .01 && Math.Abs(night.Height - 96) < .01 && Math.Abs(schedule.Days[1].Blocks[0].Height - 96) < .01, "Ca 22–02 phải có mỗi đoạn hai giờ, không bị mất phần sau nửa đêm.");
        Require(schedule.Days[0].Blocks.Select(block => block.Left).Distinct().Count() == 2, "Ca chồng giờ phải nằm hai làn riêng.");
        Require(schedule.StatusText.Contains("1 dòng chưa đủ"), "Dòng thiếu giờ phải được báo để tra ở bảng.");
        ((ScrollViewer)schedule.FindName("CalendarScroll")).ScrollToVerticalOffset(18 * 48); Drain(host); Capture(schedule, "lich-tuan-xaml");
        entries.Clear(); Drain(host); Require(schedule.Days.All(day => day.Blocks.Length == 0), "Bỏ các ca phải xóa thẻ lịch cũ.");
    }

    /// <summary>Chi tiết XAML giữ đúng dòng con gốc để nhấp đúp điều hướng, đồng thời không hiển thị mật khẩu/hash.</summary>
    private static void CheckDetails(Window host)
    {
        var source = new RangHamMat.Views.TaiKhoan();
        host.Content = source; Drain(host);
        var grid = Visuals(source).OfType<DataGrid>().First();
        var row = new Dictionary<string, object?> { ["MaTK"] = "TK-XAML", ["TenDangNhap"] = "tai-khoan-thu", ["MaNV"] = "NV01", ["MatKhau"] = "khong-hien", ["MatKhauHash"] = "hash-khong-hien" };
        var detail = new RecordDetails(grid, row, new[] { new RelatedRows("Dữ liệu liên quan", "TaiKhoan", grid, new object[] { row }, null) });
        host.Content = detail; Drain(host);
        var texts = Visuals(detail).OfType<TextBlock>().Select(text => text.Text).ToArray();
        Require(texts.Contains("tai-khoan-thu") && !texts.Any(text => text.Contains("khong-hien")), "Chi tiết phải hiện tài khoản và bỏ dữ liệu xác thực.");
        var child = Visuals(detail).OfType<DataGrid>().Single();
        Require(ReferenceEquals(child.Items[0], row) && Equals(child.Tag, grid.Tag), "Dòng con phải giữ nguyên bản ghi/Tag nguồn để workspace điều hướng đúng.");
        Capture(detail, "chi-tiet-xaml");
    }

    /// <summary>Duyệt cây visual sau render để kiểm tra các template đã hiện dữ liệu thay vì chỉ kiểm tra model.</summary>
    private static IEnumerable<DependencyObject> Visuals(DependencyObject root)
    { yield return root; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) foreach (var node in Visuals(VisualTreeHelper.GetChild(root, i))) yield return node; }
    /// <summary>Chờ binding/layout trong Dispatcher hoàn tất trước khi đánh giá kết quả hiển thị.</summary>
    private static void Drain(Window host) { host.UpdateLayout(); host.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); host.UpdateLayout(); }
    /// <summary>Xuất ảnh của control thật để xem biểu đồ/lịch/chi tiết sau khi chuyển XAML.</summary>
    private static void Capture(FrameworkElement control, string name)
    {
        var folder = Path.Combine(Environment.CurrentDirectory, "XemTruoc", "verification-xaml"); Directory.CreateDirectory(folder);
        var bitmap = new RenderTargetBitmap((int)control.ActualWidth, (int)control.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        // VisualBrush đặt control về gốc ảnh, tránh lấy cả offset khi control cao 300 đang được căn giữa host.
        var visual = new DrawingVisual(); using (var drawing = visual.RenderOpen())
        {
            var bounds = new Rect(0, 0, control.ActualWidth, control.ActualHeight);
            drawing.DrawRectangle(Brushes.White, null, bounds); drawing.DrawRectangle(new VisualBrush(control), null, bounds);
        }
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = File.Create(Path.Combine(folder, name + ".png")); encoder.Save(stream);
    }
    /// <summary>Điều kiện không đạt ném lỗi để WpfSmoke trả FAIL, không tiếp tục báo thành công.</summary>
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
