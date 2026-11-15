using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using Dapper;
using RangHamMat;
using ET;
using RangHamMat.Controls;

internal static class Program
{
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    [STAThread]
    public static int Main(string[] args)
    {
        if(args.Length!=2||!System.Text.RegularExpressions.Regex.IsMatch(args[1],"^RHM_Hospital_Test_[A-Za-z0-9_]+$"))
            throw new ArgumentException("Usage: server RHM_Hospital_Test_database (isolated fixture only)");
        var cs=new SqlConnectionStringBuilder{DataSource=args[0],InitialCatalog=args[1],IntegratedSecurity=true,TrustServerCertificate=true,Encrypt=SqlConnectionEncryptOption.Optional}.ConnectionString;
        Environment.SetEnvironmentVariable("RHM_CONNECTION_STRING",cs);
        var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
        app.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("/RangHamMat.Wpf;component/Themes/Theme.xaml",UriKind.Relative)});
        int result=1;
        app.Dispatcher.BeginInvoke(new Action(async()=>
        {
            MainWindow? window=null;string? role=null,original=null,supplier=null;
            await using var db=new SqlConnection(cs);
            try
            {
                await db.OpenAsync();
                var account=await db.QuerySingleAsync<ET_TAIKHOAN>("SELECT * FROM dbo.TAI_KHOAN WHERE TenDangNhap=N'admin001'");
                PhienDangNhap.MaNV=account.MaNV;PhienDangNhap.TenDangNhap=account.TenDangNhap;PhienDangNhap.IsAdmin=true;
                PhienDangNhap.KhoiTaoDinhDanhNghiepVu(account.MaTK);
                var first=await db.QueryFirstAsync<ET_CHUCVU>("SELECT TOP(1) * FROM dbo.CHUC_VU ORDER BY MaChucVu");
                role=first.MaChucVu;original=first.TenChucVu;
                window=new MainWindow();window.Show();
                Require((bool)typeof(MainWindow).GetMethod("OpenApp",Flags)!.Invoke(window,["NhanSu"])!,"Cannot open staff view");
                await Task.Delay(500);
                var host=(ContentControl)window.FindName("AppHost");var workspace=(UserControl)host.Content;
                var view=(UserControl)((ContentControl)workspace.FindName("Workspace")).Content;
                var grid=(DataGrid)view.FindName("dgvChucVu");var name=(TextBox)view.FindName("txtHoTen");
                string Label()=>grid.Items.OfType<ET_CHUCVU>().Single(x=>x.MaChucVu==role).TenChucVu;
                var key="SyncCheck-"+Guid.NewGuid().ToString("N")[..8];
                await db.ExecuteAsync("UPDATE dbo.CHUC_VU SET TenChucVu=@name WHERE MaChucVu=@role",new{name=key,role});
                await Until(()=>Label()==key,window);
                Console.WriteLine("PASS external commit reaches an already open staff screen through the real poll loop");
                name.Text="Nội dung chưa lưu";
                await db.ExecuteAsync("UPDATE dbo.CHUC_VU SET TenChucVu=@name WHERE MaChucVu=@role",new{name=key+"-2",role});
                await Task.Delay(4800);
                Require(name.Text=="Nội dung chưa lưu"&&Label()==key,"Automatic sync overwrote pending input or updated while dirty");
                Console.WriteLine("PASS unsaved input remains unchanged while external commits wait");
                view.GetType().GetMethod("btnLamMoiNhanVien_Click",Flags)!.Invoke(view,[view,new RoutedEventArgs()]);
                await Until(()=>Label()==key+"-2",window);
                Console.WriteLine("PASS pending external changes apply after the form is reset");
                supplier="SYNC_"+Guid.NewGuid().ToString("N")[..12];
                await db.ExecuteAsync("INSERT dbo.NHA_CUNG_CAP(MaNCC,TenNCC,SDT,Email,DiaChi,NguoiLienHe,TrangThai) VALUES(@supplier,N'Kiểm tra đồng bộ','0912345678','sync@example.invalid',N'Fixture',N'Kiểm tra',N'Đang hoạt động')",new{supplier});
                Require((bool)typeof(MainWindow).GetMethod("OpenApp",Flags)!.Invoke(window,["NhaCungCap"])!,"Cannot open supplier cache screen");
                await Task.Delay(300);
                workspace=(UserControl)host.Content;
                var suppliers=(UserControl)((ContentControl)workspace.FindName("Workspace")).Content;
                var supplierGrid=(DataGrid)suppliers.FindName("Grid_NHA_CUNG_CAP");
                string SupplierName()=>supplierGrid.Items.Cast<object>().Where(x=>UiRecord.Text(x,"MaNCC")==supplier).Select(x=>UiRecord.Text(x,"TenNCC")).SingleOrDefault()??"";
                await db.ExecuteAsync("UPDATE dbo.NHA_CUNG_CAP SET TenNCC=N'Đã đồng bộ nền' WHERE MaNCC=@supplier",new{supplier});
                await Until(()=>SupplierName()=="Đã đồng bộ nền",window);
                Require(UiRecord.Text(DuLieuGiaoDien.Find("NHA_CUNG_CAP",supplier)!,"TenNCC")=="Đã đồng bộ nền","Background snapshot did not publish to shared cache");
                Console.WriteLine("PASS background SQL snapshot updates shared cache and the visible supplier list");
                await db.ExecuteAsync("DELETE dbo.NHA_CUNG_CAP WHERE MaNCC=@supplier",new{supplier});
                await Until(()=>SupplierName()=="",window);
                Console.WriteLine("PASS external deletion disappears from the already open shared-cache screen");
                result=0;
            }
            catch(Exception error){Console.WriteLine("FAIL "+error);}
            finally
            {
                if(role is not null)await db.ExecuteAsync("UPDATE dbo.CHUC_VU SET TenChucVu=@original WHERE MaChucVu=@role",new{original,role});
                if(supplier is not null)await db.ExecuteAsync("DELETE dbo.NHA_CUNG_CAP WHERE MaNCC=@supplier",new{supplier});
                if(window is not null)
                {
                    var host=(ContentControl)window.FindName("AppHost");host.Content=null;
                    window.Close();
                }
                app.Shutdown();
            }
        }));
        app.Run();return result;
    }
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static async Task Until(Func<bool> condition,MainWindow window)
    {
        var deadline=DateTime.UtcNow.AddSeconds(20);
        while(!condition()&&DateTime.UtcNow<deadline)await Task.Delay(200);
        if(!condition())
        {
            var status=typeof(MainWindow).GetField("dongBoStatus",Flags)!.GetValue(window) as TextBlock;
            throw new InvalidOperationException("Timed out waiting for sync: "+status?.Text);
        }
    }
}
