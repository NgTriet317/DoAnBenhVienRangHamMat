// AI hỗ trợ: bổ sung kiểm tra mở các phân hệ hiện hữu để rà tình trạng chức năng của dự án.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RangHamMat;
using System.Runtime.InteropServices;
using System.Text;

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
        string? dialog=null;
        var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(20)};
        timer.Tick+=(_,_)=>EnumThreadWindows(GetCurrentThreadId(),(h,_)=>
        {
            var name=new StringBuilder(64);GetClassName(h,name,name.Capacity);
            if(name.ToString()=="#32770")
            {
                var lines=new List<string>();
                EnumChildWindows(h,(child,_)=>{var cls=new StringBuilder(64);GetClassName(child,cls,cls.Capacity);if(cls.ToString()=="Static"){var text=new StringBuilder(4000);GetWindowText(child,text,text.Capacity);lines.Add(text.ToString());}return true;},IntPtr.Zero);
                dialog=string.Join(" ",lines);SendMessage(h,0x10,IntPtr.Zero,IntPtr.Zero);
            }
            return true;
        },IntPtr.Zero);
        timer.Start();
        try
        {
            home.Show();
            var launchers = ((Panel)home.FindName("AppGrid")).Children.OfType<Button>().ToArray();
            if (launchers.Length != 25) throw new InvalidOperationException("Trang chủ phải có 25 phân hệ.");
            foreach (var button in launchers)
            {
                var module = (string)button.Tag;
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
                if(dialog is not null)throw new InvalidOperationException(module+": "+dialog);
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
        finally { timer.Stop();home.Close(); }
    }
    private delegate bool WindowCallback(IntPtr h,IntPtr state);
    [DllImport("kernel32.dll")]private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")]private static extern bool EnumThreadWindows(uint thread,WindowCallback callback,IntPtr state);
    [DllImport("user32.dll")]private static extern bool EnumChildWindows(IntPtr h,WindowCallback callback,IntPtr state);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetClassName(IntPtr h,StringBuilder text,int length);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetWindowText(IntPtr h,StringBuilder text,int length);
    [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
}
