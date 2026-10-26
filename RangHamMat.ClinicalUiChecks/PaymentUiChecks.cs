using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BUS;
using DAL;
using Dapper;
using ET;
using RangHamMat.ClinicalChecks;
using RangHamMat.Views;
using RangHamMat.Controls;

namespace RangHamMat.ClinicalUiChecks;

internal static class PaymentUiChecks
{
    private static void Need(bool value,string message) {if(!value)throw new Exception(message);}
    private static T Find<T>(FrameworkElement view,string name) where T:FrameworkElement => (T)view.FindName(name);
    private static bool Pump(Task<bool> task)
    {
        if(!task.IsCompleted){var frame=new DispatcherFrame();var dispatcher=Dispatcher.CurrentDispatcher;task.ContinueWith(_=>dispatcher.BeginInvoke(new Action(()=>frame.Continue=false)));Dispatcher.PushFrame(frame);}
        return task.GetAwaiter().GetResult();
    }
    private static NguCanhNghiepVu Prepare(Fixture f)
    {
        f.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@id;DELETE dbo.DON_THUOC WHERE MaDT=@id",new{id=f.Id("DTA")});var a=f.User("A");
        var services=new BUS_DICHVUTHUCHIEN(f.Transaction);var draft=services.LuuThucHien(new(){MaLS=f.Id("LSA"),MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE TrangThai=N'Đang hoạt động' ORDER BY MaDV"),MaNVThucHien=a.MaNV,ThoiGianThucHien=f.Scalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())"),SoLuong=1,DonGia=450000,KetQua="FIXTURE riêng",LyDoPhatSinh="FIXTURE"},a);services.XacNhanThucHien(draft.DichVu.MaDVThucHien,draft.DauVanTay,a);
        var b=new BUS_KHAMDIEUTRI(f.Transaction);var s=b.LayBanNhap(f.Id("LSA"),a);s=b.LuuKham(new(){MaLS=s.LanKham.MaLS,DauVanTay=s.DauVanTay,TrieuChung="FIXTURE",ChanDoan="FIXTURE",KeHoachXuLy="FIXTURE",KhongKeThuoc=true,LyDoKhongKeThuoc="FIXTURE",TinhTrang=[new(){LoaiTinhTrang="FIXTURE",MoTaTinhTrang="FIXTURE",MucDo="FIXTURE"}]},a);b.HoanTatKham(f.Id("LSA"),s.DauVanTay,a);var cashier=f.User("C");f.Transaction.Commit();return cashier;
    }
    private static BangTinhPhiEditor View(Fixture f,NguCanhNghiepVu u)
    {var v=new BangTinhPhiEditor(u);v.FocusContext(f.Id("LSA"));Need(v.TryLapBangTinh(out var error),error??"Không lập phí");return v;}
    internal static int Run()
    {
        // Main test không gọi Application.Run; mô phỏng đúng context của handler WPF thật cho await.
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        using(var db=new DAL_DONTHUOC().MoKetNoi())if(!db.Database.StartsWith("RHM_ClinicalWorkflow_Test_P09Payments_",StringComparison.Ordinal))throw new InvalidOperationException("UI thu tiền cần DB P09Payments riêng.");
        var pass=0;var fail=0;void Check(string name,Action action){try{action();Console.WriteLine("PASS: "+name);pass++;}catch(Exception ex){Console.Error.WriteLine("FAIL: "+name+": "+ex.GetBaseException().Message);fail++;}}
        Check("P09 WPF tiền mặt, khóa nút khi thu và chặn bấm hai lần",()=>
        {
            using var f=new Fixture();var u=Prepare(f);var v=View(f,u);Find<TextBox>(v,"GivenText").Text="500000";
            using var db=new DAL_DONTHUOC().MoKetNoi();using var tx=db.BeginTransaction(IsolationLevel.Serializable);
            db.Execute("DECLARE @r int;EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=1000;IF @r<0 THROW 52900,N'Không giữ được khóa test',1",new{key="RHM:ThanhToan:YeuCau:"+v.MaYeuCauHienTai.ToString("D")},tx);
            var task=v.TryThanhToanAsync();Need(v.DangXuLy&&!Find<Button>(v,"PayButton").IsEnabled,"Chưa khóa nút trong lúc thu");Need(!Pump(v.TryThanhToanAsync()),"Bấm lần hai đã nhận yêu cầu");tx.Commit();Need(Pump(task),"Thu thất bại");Need(v.KetQua?.SoTienThu==450000&&v.KetQua.TienThua==50000&&!Find<Button>(v,"PayButton").IsEnabled,"Sai kết quả hoặc còn bật thu lại");
            Need(f.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.THANH_TOAN WHERE MaHD=@MaHD",v.KetQua)==1,"UI ghi thu hai lần");
            var window=new Window{Content=v,Width=1100,Height=800,ShowInTaskbar=false};try{window.Show();window.UpdateLayout();Save(v,"P09-thu-thanh-cong.png");}finally{window.Close();}
        });
        Check("P09 WPF chuyển khoản cần đối chiếu và tham chiếu",()=>
        {using var f=new Fixture();var u=Prepare(f);var v=View(f,u);Find<ComboBox>(v,"MethodBox").SelectedValue="Chuyển khoản";Find<TextBox>(v,"GivenText").Text="450000";Find<TextBox>(v,"ReferenceText").Text="FIXTURE-BANK";Need(!Pump(v.TryThanhToanAsync()),"Không đối chiếu vẫn nhận thu");Find<CheckBox>(v,"TransferChecked").IsChecked=true;Need(Pump(v.TryThanhToanAsync())&&v.KetQua?.TienThua==0,"Không thu được chuyển khoản hợp lệ");});
        Check("P09 WPF lỗi chưa rõ kết quả giữ nguyên yêu cầu và nguồn khi thử lại",()=>
        {using var f=new Fixture();var u=Prepare(f);var v=View(f,u);Find<TextBox>(v,"GivenText").Text="500000";var request=v.MaYeuCauHienTai;
            f.Connection.Execute("CREATE TRIGGER dbo.CW_Test_UiPaymentFailure ON dbo.THANH_TOAN AFTER INSERT AS BEGIN THROW 52911,N'FIXTURE UI failure',1;END;");try{Need(!Pump(v.TryThanhToanAsync())&&v.ChoDoiChieu&&v.MaYeuCauHienTai==request,"Mất yêu cầu sau lỗi");Need(!Find<FrameworkElement>(v,"PaymentInputs").IsEnabled,"Cho đổi đầu vào khi chưa đối chiếu");v.FocusContext(f.Id("LSB"));Need(v.BangTinh?.MaLS==f.Id("LSA"),"Đổi lượt khi chưa biết kết quả");Need(!v.TryLapBangTinh(out _),"Lập lại bảng phí đã xóa yêu cầu chưa rõ kết quả");}finally{f.Connection.Execute("DROP TRIGGER dbo.CW_Test_UiPaymentFailure");}
            Find<TextBox>(v,"GivenText").Text="600000";Need(Pump(v.TryThanhToanAsync())&&v.KetQua?.SoTienKhachDua==500000&&v.KetQua.TienThua==50000,"Thử lại đổi đầu vào hoặc thu sai");});
        Check("P09 WPF bác sĩ xem phí nhưng không có quyền thu",()=>
        {using var f=new Fixture();Prepare(f);f.ActAs("A");var a=new BUS_QUYENNGHIEPVU().LayNguCanh(f.Id("A"));var v=View(f,a);Need(!Find<Button>(v,"PayButton").IsEnabled&&!Pump(v.TryThanhToanAsync()),"Bác sĩ được thao tác thu");});
        Check("P09 WPF hóa đơn giữ nguồn khi thu và tải lịch sử sau commit",()=>
        {
            using var f=new Fixture();Prepare(f);DuLieuGiaoDien.ChonPhamViLamSang(true);var page=new HoaDon();page.FocusContext(f.Id("BN1"),f.Id("LSA"));
            var window=new Window{Content=page,Width=1366,Height=900,ShowInTaskbar=false};try
            {
                window.Show();window.UpdateLayout();var v=(BangTinhPhiEditor)Find<ContentControl>(page,"PricingHost").Content;Need(v.TryLapBangTinh(out var error),error??"Không tính phí");Find<TextBox>(v,"GivenText").Text="500000";
                using var db=new DAL_DONTHUOC().MoKetNoi();using var tx=db.BeginTransaction(IsolationLevel.Serializable);db.Execute("DECLARE @r int;EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=1000;IF @r<0 THROW 52900,N'Không giữ khóa test',1",new{key="RHM:ThanhToan:YeuCau:"+v.MaYeuCauHienTai.ToString("D")},tx);
                var task=v.TryThanhToanAsync();Find<ComboBox>(page,"PatientFilter").SelectedValue=f.Id("BN2");Find<ComboBox>(page,"VisitFilter").SelectedValue=f.Id("LSH");page.FocusContext(f.Id("BN2"),f.Id("LSU"));Need(page.PatientId==f.Id("BN1")&&v.BangTinh?.MaLS==f.Id("LSA")&&Convert.ToString(Find<ComboBox>(page,"VisitFilter").SelectedValue)==f.Id("LSA"),"Mất nguồn khi đang thu");tx.Commit();Need(Pump(task),"Không thu được từ màn hóa đơn");
                Need(v.KetQua is not null && Find<ListBox>(page,"InvoiceList").SelectedItem is System.Data.DataRowView row&&Convert.ToString(row["MaHD"])==v.KetQua.MaHD,"Chưa tải/chọn hóa đơn đã commit hoặc refresh xóa kết quả thu");Save(page,"P09-hoa-don-sau-thu.png");
            }
            finally{window.Close();DuLieuGiaoDien.ChonPhamViLamSang(false);}
        });
        Console.WriteLine($"PaymentUiChecks: {pass} PASS, {fail} FAIL; commit chỉ trên database P09Payments riêng.");return fail==0?0:1;
    }
    private static void Save(FrameworkElement view,string file)
    {
        view.Dispatcher.Invoke(()=>{},DispatcherPriority.Render);view.UpdateLayout();var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)view.ActualWidth,(int)view.ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);bitmap.Render(view);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));System.IO.Directory.CreateDirectory("obj/ClinicalWorkflowP09");using var output=System.IO.File.Create(System.IO.Path.Combine("obj/ClinicalWorkflowP09",file));encoder.Save(output);
    }
}
