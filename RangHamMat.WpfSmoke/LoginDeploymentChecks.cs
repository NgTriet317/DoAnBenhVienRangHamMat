using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RangHamMat;
using RangHamMat.Views;

internal static class LoginDeploymentChecks
{
    internal static void Run()
    {
        PhienDangNhap.KhoiTaoPhienUngDung();
        var login=new DangNhap();string? error=null;
        var timeout=new DispatcherTimer{Interval=TimeSpan.FromSeconds(10)};
        timeout.Tick+=(_,_)=>{error=((TextBlock)login.FindName("FormError")).Text;login.Close();};
        login.Loaded+=(_,_)=>login.Dispatcher.BeginInvoke(new Action(()=>
        {
            ((TextBox)login.FindName("UsernameBox")).Text="admin001";
            ((PasswordBox)login.FindName("PasswordBox")).Password="123456";
            typeof(DangNhap).GetMethod("DangNhap_Click",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(login,[login,new RoutedEventArgs()]);
        }),DispatcherPriority.ApplicationIdle);
        timeout.Start();var result=login.ShowDialog();timeout.Stop();
        if(result!=true||!PhienDangNhap.IsAdmin||PhienDangNhap.MaNV.Length==0)throw new Exception("Đăng nhập seed SQL không thành công: "+error);
        Console.WriteLine("PASS: đăng nhập admin seed bằng PBKDF2 từ SQL, principal có tài khoản/nhân viên thật.");
        DieuHuongPhanHeChecks.Run();
    }
}
