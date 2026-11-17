using System.Data;
using System.Reflection;
using System.Security.Claims;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BUS;
using Dapper;
using ET;
using Microsoft.Data.SqlClient;
using RangHamMat;

internal static class Program
{
 private const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 [STAThread] public static int Main(string[] args)
 {
  if(args.Length!=2||!System.Text.RegularExpressions.Regex.IsMatch(args[1],"^RHM_Hospital_Test_[A-Za-z0-9_]+$"))throw new ArgumentException("Only isolated RHM_Hospital_Test fixtures are allowed.");
  var cs=new SqlConnectionStringBuilder{DataSource=args[0],InitialCatalog=args[1],IntegratedSecurity=true,TrustServerCertificate=true,Encrypt=SqlConnectionEncryptOption.Optional}.ConnectionString;
  Environment.SetEnvironmentVariable("RHM_CONNECTION_STRING",cs);
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
  app.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("/RangHamMat.Wpf;component/Themes/Theme.xaml",UriKind.Relative)});
  int exit=1;
  app.Dispatcher.BeginInvoke(new Action(async()=>
  {
   MainWindow? main=null;Guid otherSession=Guid.Empty;ET_TAIKHOAN? other=null;
   try
   {
    await using var db=new SqlConnection(cs);await db.OpenAsync();
    var admin=await db.QuerySingleAsync<ET_TAIKHOAN>("SELECT * FROM dbo.TAI_KHOAN WHERE TenDangNhap=N'admin001'");
    other=await db.QueryFirstAsync<ET_TAIKHOAN>("SELECT TOP(1)* FROM dbo.TAI_KHOAN WHERE MaTK<>@MaTK AND TrangThai=N'Đang hoạt động' ORDER BY MaTK",new{admin.MaTK});
    PhienDangNhap.KhoiTaoPhienUngDung();PhienDangNhap.MaNV=admin.MaNV;PhienDangNhap.TenDangNhap=admin.TenDangNhap;PhienDangNhap.IsAdmin=true;PhienDangNhap.KhoiTaoDinhDanhNghiepVu(admin.MaTK);
    typeof(MainWindow).Assembly.GetType("RangHamMat.PhienNhatKy")!.GetMethod("BatDau",All)!.Invoke(null,[null]);
    var oldSession=PhienDangNhap.MaPhien;
    main=new MainWindow();bool closed=false;string? message=null;
    main.Closed+=(_,_)=>closed=true;
    typeof(MainWindow).GetProperty("XoaPhienGhiNho",All)!.SetValue(main,(Action)(()=>{}));
    typeof(MainWindow).GetProperty("ThongBaoMatPhien",All)!.SetValue(main,(Action<string>)(text=>message=text));
    main.Show();
    if(!(bool)typeof(MainWindow).GetMethod("OpenApp",All)!.Invoke(main,["NhatKy"])!)throw new Exception("Cannot open audit view");
    await Task.Delay(300);
    var workspace=(UserControl)((ContentControl)main.FindName("AppHost")).Content;
    var audit=(UserControl)((ContentControl)workspace.FindName("Workspace")).Content;
    var filter=(TextBox)audit.FindName("AccountFilter");filter.Text=other.TenDangNhap;
    audit.GetType().GetMethod("Load",All)!.Invoke(audit,[true]);
    Keyboard.Focus(filter);
    var grid=(DataGrid)audit.FindName("AuditGrid");
    bool Has(Guid session,string action)=>grid.Items.OfType<DataRowView>().Any(row=>Convert.ToString(row["MaDoiTuong"])==session.ToString()&&Convert.ToString(row["HanhDong"])==HanhDongNhatKy.Nhan(action));
    otherSession=await As(other,()=>new BUS_NHATKY().BatDauPhien(new(){MaTK=other.MaTK,MaNV=other.MaNV}));
    await Until(()=>Has(otherSession,"LOGIN_SUCCESS"),main);
    Require(ReferenceEquals(Keyboard.FocusedElement,filter)&&filter.Text==other.TenDangNhap,"Audit refresh lost the focused filter");
    Console.WriteLine("PASS remote LOGIN appears without reload while audit filter retains focus");
    await As(other,()=>{new BUS_NHATKY().KetThucPhien(otherSession);return true;},otherSession);
    await Until(()=>Has(otherSession,"LOGOUT"),main);
    Console.WriteLine("PASS remote LOGOUT appears without reload in the open audit view");
    await AiModal(main,db,other);
    var newAdmin=await As(admin,()=>new BUS_NHATKY().BatDauPhien(new(){MaTK=admin.MaTK,MaNV=admin.MaNV}));
    await Until(()=>closed&&message==ThongBaoPhien.DangNhapMayKhac,main);
    Require(!PhienDangNhap.DaDangNhap&&PhienDangNhap.MaPhien==Guid.Empty,"Revoked UI retained its account/session");
    Console.WriteLine("PASS real session watchdog logs out the earlier machine with the exact notification");
    await As(admin,()=>{new BUS_NHATKY().KetThucPhien(newAdmin);return true;},newAdmin);
    Console.WriteLine("PASS the replacing session remains usable after the earlier UI exits");
    exit=0;
   }
   catch(Exception error){Console.WriteLine("FAIL "+error);}
   finally
   {
    if(main is not null&&main.IsVisible){((ContentControl)main.FindName("AppHost")).Content=null;main.Close();}
    app.Shutdown();
   }
  }));
  app.Run();return exit;
 }
 private static async Task AiModal(MainWindow main,SqlConnection db,ET_TAIKHOAN sender)
 {
  var first=Guid.NewGuid();var second=Guid.NewGuid();
  ET_AI_BAN_KHAI Draft(Guid id)=>new(){MaPhien=id,HoTen="Fixture runtime AI",NgaySinh=new DateTime(2000,1,1),SoDienThoai="0901234567",LyDoKham="Kiểm tra danh sách đồng bộ",LoaiKham="ThongThuong"};
  try
  {
   await As(sender,()=>new BUS_AITIEPNHAN().luuBanKhai(Draft(first)));
   var counts=await db.QuerySingleAsync<(int,int)>("SELECT (SELECT COUNT(*) FROM dbo.BENH_NHAN),(SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH)");
   var modal=new RangHamMat.Views.TiepNhanAI{Owner=main};Exception? failure=null;
   modal.Loaded+=async(_,_)=>
   {
    try
    {
     var list=(DataGrid)modal.FindName("gridPhien");
     list.SelectedItem=list.Items.OfType<ET_AITIEPNHAN>().Single(x=>x.MaPhien==first);
     var editor=(TextBox)modal.FindName("txtLyDoDuyet");editor.Text="Nội dung đối chiếu chưa lưu";Keyboard.Focus(editor);
     await As(sender,()=>new BUS_AITIEPNHAN().luuBanKhai(Draft(second)));
     await Until(()=>list.Items.OfType<ET_AITIEPNHAN>().Any(x=>x.MaPhien==second),main);
     Require(editor.Text=="Nội dung đối chiếu chưa lưu","AI modal list overwrote the current review");
     Console.WriteLine("PASS remote AI draft appears through the real poll while a modal review retains input");
     await db.ExecuteAsync("UPDATE dbo.AI_PHIEN_TIEP_NHAN SET TrangThai=N'CanHoTro',NgayCapNhat=SYSUTCDATETIME() WHERE MaPhien=@first",new{first});
     await Until(()=>!((Button)modal.FindName("btnTaoPhieu")).IsEnabled,main);
     Require(editor.Text=="Nội dung đối chiếu chưa lưu","Remote status change overwrote review input");
     var after=await db.QuerySingleAsync<(int,int)>("SELECT (SELECT COUNT(*) FROM dbo.BENH_NHAN),(SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH)");
     Require(after==counts,"Automatic AI refresh created a patient or official receipt");
     Console.WriteLine("PASS remote AI status disables stale approval without creating records or losing input");
    }
    catch(Exception error){failure=error;}
    finally{modal.Close();}
   };
   modal.ShowDialog();if(failure is not null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
  }
  finally
  {
   await db.ExecuteAsync("DELETE dbo.AI_SU_KIEN WHERE MaPhien IN(@first,@second);DELETE dbo.AI_TIEP_NHAN_NHAP WHERE MaPhien IN(@first,@second);DELETE dbo.AI_PHIEN_TIEP_NHAN WHERE MaPhien IN(@first,@second)",new{first,second});
  }
 }
 private static async Task<T> As<T>(ET_TAIKHOAN account,Func<T> action,Guid? session=null)
  =>await Task.Run(()=>
  {
   var previous=Thread.CurrentPrincipal;
   var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,account.MaTK),new(DinhDanhNghiepVu.MaNhanVienClaim,account.MaNV)};
   if(session.HasValue)claims.Add(new("RHM.MaPhien",session.Value.ToString()));
   try{Thread.CurrentPrincipal=new ClaimsPrincipal(new ClaimsIdentity(claims,DinhDanhNghiepVu.KieuXacThuc));return action();}
   finally{Thread.CurrentPrincipal=previous;}
  });
 private static void Require(bool value,string message){if(!value)throw new Exception(message);}
 private static async Task Until(Func<bool> condition,MainWindow main)
 {
  var until=DateTime.UtcNow.AddSeconds(25);
  while(!condition()&&DateTime.UtcNow<until)await Task.Delay(100);
  if(!condition())throw new Exception("Runtime timeout: "+((TextBlock)typeof(MainWindow).GetField("dongBoStatus",All)!.GetValue(main)!).Text);
 }
}
