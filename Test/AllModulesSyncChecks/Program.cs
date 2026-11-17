using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BUS;
using Dapper;
using ET;
using Microsoft.Data.SqlClient;
using RangHamMat;
using RangHamMat.Services;

// Runs real navigation, registry, prepared SQL reads and application on an isolated full-schema fixture.
// This does not claim to exercise every business button, or two physical computers.
internal static class Program
{
    private const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    [STAThread] public static int Main(string[] args)
    {
        if(args.Length!=2||!System.Text.RegularExpressions.Regex.IsMatch(args[1],"^RHM_Hospital_Test_[A-Za-z0-9_]+$"))
            throw new ArgumentException("Only isolated RHM_Hospital_Test fixtures are allowed.");
        var cs=new SqlConnectionStringBuilder{DataSource=args[0],InitialCatalog=args[1],IntegratedSecurity=true,TrustServerCertificate=true,Encrypt=SqlConnectionEncryptOption.Optional}.ConnectionString;
        Environment.SetEnvironmentVariable("RHM_CONNECTION_STRING",cs);
        var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
        app.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("/RangHamMat.Wpf;component/Themes/Theme.xaml",UriKind.Relative)});
        int result=1;
        app.Dispatcher.BeginInvoke(new Action(async()=>
        {
            MainWindow? main=null;
            try
            {
                await using var db=new SqlConnection(cs);await db.OpenAsync();
                var admin=await db.QuerySingleAsync<ET_TAIKHOAN>("SELECT * FROM dbo.TAI_KHOAN WHERE TenDangNhap=N'admin001'");
                PhienDangNhap.KhoiTaoPhienUngDung();PhienDangNhap.MaNV=admin.MaNV;PhienDangNhap.TenDangNhap=admin.TenDangNhap;
                PhienDangNhap.IsAdmin=true;PhienDangNhap.KhoiTaoDinhDanhNghiepVu(admin.MaTK);
                typeof(MainWindow).Assembly.GetType("RangHamMat.PhienNhatKy")!.GetMethod("BatDau",All)!.Invoke(null,[null]);
                main=new MainWindow();main.Show();await Task.Delay(300);
                // Deterministic full invalidation per view; other suites exercise the actual two-second SQL poll.
                typeof(MainWindow).GetMethod("HuyDongBo",All)!.Invoke(main,null);
                var routes=(IEnumerable<string>)typeof(MainWindow).GetField("AllPages",All)!.GetValue(null)!;
                int count=0;
                foreach(var route in routes)
                {
                    Require((bool)typeof(MainWindow).GetMethod("OpenApp",All)!.Invoke(main,[route])!,"Cannot navigate to "+route);
                    await Task.Delay(180);Keyboard.ClearFocus();
                    var workspace=(UserControl)((ContentControl)main.FindName("AppHost")).Content;
                    var view=(UserControl)((ContentControl)workspace.FindName("Workspace")).Content;
                    if(route is "TongQuan" or "TroLyAI")
                    {
                        Console.WriteLine("PASS "+route+": static/local content, no shared SQL list");count++;continue;
                    }
                    await Check(main,route);
                    count++;
                    if(route=="NhapXuatVatTu")
                    {
                        var tabs=Nodes(view).OfType<TabControl>().First();
                        for(int i=1;i<tabs.Items.Count;i++)
                        {
                            tabs.SelectedIndex=i;await Task.Delay(180);Keyboard.ClearFocus();
                            // The import tab initially shows its new-receipt form. Select an existing receipt to browse.
                            if(i==2)
                            {
                                var import=(UserControl)((ContentControl)view.FindName("viewNhap")).Content;
                                var receipts=(ListBox)import.FindName("lstPhieu");
                                Require(receipts.Items.Count>0,"Import fixture must contain a receipt to leave the new form");
                                receipts.SelectedIndex=0;
                            }
                            await Check(main,route+" tab "+i);count++;
                        }
                    }
                }
                Console.WriteLine($"RESULT: {count} route/tab checks passed with real WPF and SQL reads.");
                result=0;
            }
            catch(Exception error){Console.WriteLine("FAIL "+error);}
            finally
            {
                if(main is not null&&main.IsVisible){((ContentControl)main.FindName("AppHost")).Content=null;main.Close();}
                app.Shutdown();
            }
        }));
        app.Run();return result;
    }
    private static async Task Check(MainWindow main,string label)
    {
        var registrations=((IEnumerable)typeof(DongBoManHinh).GetField("active",All)!.GetValue(null)!).Cast<object>()
            .Where(r=>((UserControl)r.GetType().GetProperty("Control")!.GetValue(r)!).IsVisible).ToArray();
        Require(registrations.Length>0,label+": no visible adapter registered");
        bool busy=(bool)typeof(DongBoManHinh).GetMethod("IsBusy",All)!.Invoke(null,[main])!;
        if(busy)
        {
            var reasons=registrations.Where(r=>(r.GetType().GetProperty("Busy")!.GetValue(r) as Func<bool>)?.Invoke()==true)
                .Select(r=>((UserControl)r.GetType().GetProperty("Control")!.GetValue(r)!).GetType().Name);
            throw new Exception(label+": clean screen permanently busy: "+string.Join(",",reasons));
        }
        var before=registrations.ToDictionary(r=>r,r=>(long)r.GetType().GetProperty("Applied")!.GetValue(r)!,ReferenceEqualityComparer.Instance);
        // Request and apply an observed full invalidation through the production coordinator.
        typeof(DongBoManHinh).GetMethod("ObserveChanges",All)!.Invoke(null,[new[]{"*"}]);
        var pending=(TrangThaiDongBo)typeof(MainWindow).GetField("dongBoPending",All)!.GetValue(main)!;
        pending.GhiNhan(["*"]);
        await (Task)typeof(MainWindow).GetMethod("ApDungDongBoAsync",All)!.Invoke(main,[CancellationToken.None])!;
        Require(!pending.CoThayDoi,label+": refresh left pending changes: "+((TextBlock)typeof(MainWindow).GetField("dongBoStatus",All)!.GetValue(main)!).Text);
        Require(registrations.All(r=>(long)r.GetType().GetProperty("Applied")!.GetValue(r)!>before[r]),label+": an adapter was not refreshed");
        Console.WriteLine("PASS "+label+": "+registrations.Length+" visible adapters read and applied SQL data");
    }
    private static IEnumerable<DependencyObject> Nodes(DependencyObject root)
    {
        yield return root;
        for(int i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);i++)
            foreach(var node in Nodes(System.Windows.Media.VisualTreeHelper.GetChild(root,i)))yield return node;
    }
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
