using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BUS;
using DAL;
using Dapper;
using ET;
using RangHamMat.Controls;
using RangHamMat.Views;
namespace RangHamMat.ClinicalUiChecks;

internal static class LayoutUiChecks
{
    [StructLayout(LayoutKind.Sequential)]private struct NativeRect{public int Left,Top,Right,Bottom;}
    [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam);
    private static void Need(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void SignIn(string job)
    {
        using var db=new DAL_DONTHUOC().MoKetNoi();var a=db.QuerySingle<(string Employee,string Account,string Role)>("SELECT TOP(1) n.MaNV Employee,t.MaTK Account,v.TenVaiTro Role FROM dbo.NHAN_VIEN n JOIN dbo.TAI_KHOAN t ON t.MaNV=n.MaNV JOIN dbo.VAI_TRO v ON v.MaVaiTro=t.MaVaiTro WHERE n.MaChucVu=@job AND n.TrangThai=N'Đang làm việc' AND t.TrangThai=N'Đang hoạt động' ORDER BY n.MaNV",new{job});
        PhienDangNhap.MaNV=a.Employee;PhienDangNhap.IsAdmin=a.Role=="Admin";PhienDangNhap.KhoiTaoDinhDanhNghiepVu(a.Account);DuLieuGiaoDien.ChonPhamViLamSang(true);
    }
    private static IEnumerable<DependencyObject> Nodes(DependencyObject node)
    {
        yield return node;
        if(node is Visual)for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var item in Nodes(VisualTreeHelper.GetChild(node,i)))yield return item;
    }
    internal static void AddCases(Action<string,Action> check)
    {
        foreach(var screen in new[]{"BenhNhan","HoSo","TiepNhan","FormBenhNhan","Kham","DichVu","DonThuoc","BangTinh","BanGiao","HoaDon","ShellBenhNhan"})
        foreach(var size in new[]{(1024,768),(1366,768),(1920,1080)})foreach(var percent in new[]{100,125,150})
        {
            var (width,height)=size;var dpi=percent;var kind=screen;
            check($"P11 WPF {kind} {width}×{height} DPI{dpi}",()=>
            {
                using var data=Create(kind);var view=data.View;var host=new Window{Content=view,Width=width,Height=height,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,Left=0,Top=0};
                try
                {
                    host.Show();SetDpi(host,dpi,width,height);Idle(host);
                    var actualDpi=VisualTreeHelper.GetDpi(view).DpiScaleX;Need(Math.Abs(actualDpi-dpi/100d)<.01,$"HWND chưa nhận DPI{dpi}: {actualDpi}");
                    var panels=Nodes(view).OfType<Grid>().Where(g=>Equals(g.Tag,"ClinicalAdaptivePanel")).ToArray();
                    foreach(var panel in panels)Need(panel.ActualWidth<=view.ActualWidth+1,"Panel lớn hơn vùng nhìn tại DPI lớn");
                    var tabs=Nodes(view).OfType<TabControl>().FirstOrDefault(t=>Equals(t.Tag,"ClinicalAdaptiveTabs"));
                    if(panels.Length>0&&view.ActualWidth<900)Need(tabs is not null,"Cửa sổ hẹp chưa chuyển hai vùng thành tab");
                    if(view.FindName("Grid_BENH_NHAN") is DataGrid grid)Need(grid.Items.Count<=15,"Tải cả danh sách vào UI");
                    foreach(var tab in tabs is null?Array.Empty<TabItem>():tabs.Items.OfType<TabItem>().ToArray())
                    {
                        tabs!.SelectedItem=tab;Idle(host);
                        if(view.FindName("RxLines") is DataGrid rx&&rx.IsVisible)Need(rx.ActualHeight>=80,"Đơn dài làm vùng dòng thuốc bị co mất; cần cuộn rõ ràng");
                        foreach(var button in Nodes(view).OfType<Button>().Where(IsAction).ToArray())AssertVisible(view,button);
                    }
                    if(tabs is null)foreach(var button in Nodes(view).OfType<Button>().Where(IsAction).ToArray())AssertVisible(view,button);
                    if(width==1024&&dpi==150)Save(view,$"P11-{kind}-1024-dpi150.png",actualDpi);
                }
                finally{host.Close();}
            });
        }
    }
    private static bool IsAction(Button b)=>b.IsVisible&&b.Name is "AddPatientButton" or "SaveReceptionButton" or "SaveClinicalButton" or "CompleteClinicalButton" or "SaveButton" or "SignButton" or "ConfirmButton" or "PayButton" or "PreviewButton" or "GiveButton" or "CreateMedicalButton" or "ReceptionButton" or "FinanceSearchButton" or "btnTaiLaiDuLieu";
    internal sealed class Screen(UserControl view,IDisposable? fixture=null):IDisposable
    {internal UserControl View=>view;public void Dispose()=>fixture?.Dispose();}
    private sealed class Cleanup(Action action):IDisposable{public void Dispose()=>action();}
    internal static Screen Create(string kind)
    {
        if(kind is "Kham" or "DichVu" or "DonThuoc" or "BangTinh")
        {
            var f=new RangHamMat.ClinicalChecks.Fixture();
            try
            {
                f.Execute("UPDATE dbo.BENH_NHAN SET HoTenBN=REPLICATE(N'Người bệnh tên dài ',12) WHERE MaBN=@id",new{id=f.Id("BN1")});var actor=f.User("A");
                if(kind=="Kham"){var v=new KhamLamSang(f.Id("LSA"),actor,f.Transaction);((TextBox)v.FindName("DiagnosisEditor")).Text=string.Concat(Enumerable.Repeat("FIXTURE chẩn đoán dài có dấu. ",100));return new(v,f);}
                if(kind=="DichVu"){var v=new ThucHienDichVuEditor(actor,f.Transaction);v.FocusContext(f.Id("BN1"),f.Id("LSA"));((TextBox)v.FindName("ResultEditor")).Text=string.Concat(Enumerable.Repeat("FIXTURE kết quả dài. ",100));return new(v,f);}
                if(kind=="DonThuoc"){var v=new KeDonThuocEditor(actor,f.Transaction);v.FocusContext(f.Id("BN1"),f.Id("LSA"),f.Id("DTA"));((TextBox)v.FindName("NoteEditor")).Text=string.Concat(Enumerable.Repeat("FIXTURE hướng dẫn dài. ",100));return new(v,f);}
                f.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@id;DELETE dbo.DON_THUOC WHERE MaDT=@id",new{id=f.Id("DTA")});
                var services=new BUS_DICHVUTHUCHIEN(f.Transaction);var actual=services.LuuThucHien(new(){MaLS=f.Id("LSA"),MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE TrangThai=N'Đang hoạt động' ORDER BY MaDV"),MaNVThucHien=actor.MaNV,ThoiGianThucHien=f.Scalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())"),SoLuong=1,DonGia=0,LyDoPhatSinh="FIXTURE",KetQua="FIXTURE"},actor);services.XacNhanThucHien(actual.DichVu.MaDVThucHien,actual.DauVanTay,actor);
                var bus=new BUS_KHAMDIEUTRI(f.Transaction);var snapshot=bus.LayBanNhap(f.Id("LSA"),actor);snapshot=bus.LuuKham(new(){MaLS=f.Id("LSA"),DauVanTay=snapshot.DauVanTay,TrieuChung="FIXTURE",ChanDoan="FIXTURE",KeHoachXuLy="FIXTURE",KhongKeThuoc=true,LyDoKhongKeThuoc="FIXTURE",TinhTrang=[new(){LoaiTinhTrang="FIXTURE",MoTaTinhTrang="FIXTURE",MucDo="FIXTURE"}]},actor);bus.HoanTatKham(f.Id("LSA"),snapshot.DauVanTay,actor);
                var pricing=new BangTinhPhiEditor(f.User("C"),f.Transaction);pricing.FocusContext(f.Id("LSA"));Need(pricing.TryLapBangTinh(out var error),error??"Không lập được phí zero có nguồn");return new(pricing,f);
            }
            catch{f.Dispose();throw;}
        }
        SignIn(kind=="BanGiao"?"CV005":kind=="HoaDon"?"CV007":"CV004");
        if(kind=="ShellBenhNhan")
        {var main=new MainWindow();var workspace=new AppWorkspace(main,"Hồ sơ và tiếp nhận","User",new BenhNhan());return new(workspace,new Cleanup(main.Close));}
        if(kind=="HoaDon")
        {
            using var db=new DAL_DONTHUOC().MoKetNoi();var source=db.QueryFirst<(string Patient,string Visit)>("SELECT hs.MaBN Patient,l.MaLS Visit FROM dbo.LICH_SU_KHAM_BENH l JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS WHERE l.TrangThai=N'Đã xác nhận' AND l.KhongKeThuoc=1 AND NOT EXISTS(SELECT 1 FROM dbo.HOA_DON h WHERE h.MaLS=l.MaLS) AND EXISTS(SELECT 1 FROM dbo.DICH_VU_THUC_HIEN d WHERE d.MaLS=l.MaLS AND d.TrangThai=N'Đã xác nhận' AND d.DonGia=0) ORDER BY l.MaLS");
            var v=new HoaDon();v.FocusContext(source.Patient,source.Visit);var price=(BangTinhPhiEditor)((ContentControl)v.FindName("PricingHost")).Content;Need(price.TryLapBangTinh(out var error),error??"Không mở nguồn phí của layout");return new(v);
        }
        if(kind=="BanGiao"){var v=new BanGiaoThuocEditor();var queue=(ListBox)v.FindName("QueueList");if(queue.Items.Count>0)queue.SelectedIndex=0;return new(v);}
        if(kind=="BenhNhan")return new(new BenhNhan());
        var patient=DuLieuGiaoDien.Rows("BENH_NHAN").First();var id=UiRecord.Text(patient,"MaBN");typeof(DuLieuGiaoDien).GetMethod("BaoDamNguCanh",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!.Invoke(null,[new NguCanhHoSo{MaBN=id}]);
        if(kind=="HoSo")return new(new HoSoBenhNhan(DuLieuGiaoDien.Find("BENH_NHAN",id)!));
        if(kind=="TiepNhan"){var v=new TiepNhanKham();BieuMauHoSo.Configure(v,"PHIEU_KHAM_BENH",patient:id);return new(v);}
        var form=new CuaSoBenhNhan();BieuMauHoSo.Configure(form,"BENH_NHAN");
        foreach(var text in LogicalNodes(form).OfType<TextBox>().Where(t=>Equals(t.Tag,"DB:BENH_NHAN.HoTenBN")||Equals(t.Tag,"DB:BENH_NHAN.DiaChi")))text.Text=string.Concat(Enumerable.Repeat("FIXTURE địa chỉ và họ tên dài. ",8));
        return new(form);
    }
    private static IEnumerable<DependencyObject> LogicalNodes(DependencyObject node)
    {yield return node;foreach(var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())foreach(var item in LogicalNodes(child))yield return item;}
    private static void AssertVisible(FrameworkElement root,FrameworkElement target)
    {
        target.BringIntoView();Idle(Window.GetWindow(root));
        var box=target.TransformToAncestor(root).TransformBounds(new Rect(target.RenderSize));
        Need(box.Left>=-1&&box.Top>=-1&&box.Right<=root.ActualWidth+1&&box.Bottom<=root.ActualHeight+1,$"Nút {target.Name} nằm ngoài viewport {box}; root{root.ActualWidth}×{root.ActualHeight}");
        Need(target.ActualHeight>=36,"Nút thao tác quá thấp");
    }
    private static void SetDpi(Window host,int percent,int width,int height)
    {
        var dpi=96*percent/100;var ptr=Marshal.AllocHGlobal(Marshal.SizeOf<NativeRect>());
        try{Marshal.StructureToPtr(new NativeRect{Right=width,Bottom=height},ptr,false);SendMessage(new WindowInteropHelper(host).Handle,0x02E0,new IntPtr((dpi<<16)|dpi),ptr);}
        finally{Marshal.FreeHGlobal(ptr);}
    }
    private static void Idle(Window? host){host?.UpdateLayout();Dispatcher.CurrentDispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);host?.UpdateLayout();}
    private static void Save(FrameworkElement root,string name,double scale)
    {
        var image=new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth*scale),(int)Math.Ceiling(root.ActualHeight*scale),96*scale,96*scale,PixelFormats.Pbgra32);image.Render(root);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var file=System.IO.File.Create(System.IO.Path.Combine("obj/ClinicalWorkflowP11",name));encoder.Save(file);
    }
}
