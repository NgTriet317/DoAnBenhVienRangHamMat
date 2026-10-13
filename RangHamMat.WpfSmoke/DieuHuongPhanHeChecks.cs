// AI hỗ trợ: bổ sung kiểm tra mở các phân hệ hiện hữu để rà tình trạng chức năng của dự án.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RangHamMat;

/// <summary>
/// Mở từng phân hệ bằng nút thật ở trang chủ. Kiểm tra này xác nhận màn hình nạp được,
/// không coi việc mở được giao diện là bằng chứng chức năng đã đọc/lưu SQL.
/// </summary>
internal static class DieuHuongPhanHeChecks
{
    /// <summary>
    /// Dùng vai trò xem mặc định, bấm 25 nút và kiểm tra workspace chứa đúng màn hình đích.
    /// Chỉ mở/đọc màn hình, không bấm lưu và không ghi dữ liệu thử vào database.
    /// </summary>
    public static void Run()
    {
        var home = new MainWindow { ShowInTaskbar = false };
        try
        {
            home.Show();
            var launchers = ((Panel)home.FindName("AppGrid")).Children.OfType<Button>().ToArray();
            if (launchers.Length != 25) throw new InvalidOperationException("Trang chủ phải có 25 phân hệ.");
            foreach (var button in launchers)
            {
                var module = (string)button.Tag;
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
                home.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                home.UpdateLayout();
                var workspace = ((ContentControl)home.FindName("AppHost")).Content as AppWorkspace;
                var page = (workspace?.FindName("Workspace") as ContentControl)?.Content;
                if (page?.GetType().Name != module)
                    throw new InvalidOperationException("Không mở được phân hệ: " + module);
                Console.WriteLine("PASS mở giao diện: " + module);
            }
            Console.WriteLine("PASS: 25/25 phân hệ mở đúng màn hình XAML từ trang chủ; không kiểm tra lưu ở các form mẫu.");
        }
        finally { home.Close(); }
    }
}
