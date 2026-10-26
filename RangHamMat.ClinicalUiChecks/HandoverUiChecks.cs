using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using BUS;
using DAL;
using Dapper;
using ET;
using RangHamMat.ClinicalChecks;
using RangHamMat.Views;
using RangHamMat.Controls;

namespace RangHamMat.ClinicalUiChecks;
internal static class HandoverUiChecks
{
    private static void Need(bool value,string message){if(!value)throw new Exception(message);}
    private static T Find<T>(FrameworkElement v,string name)where T:FrameworkElement=>(T)v.FindName(name);
    private static bool Pump(Task<bool> task){if(!task.IsCompleted){var frame=new DispatcherFrame();var d=Dispatcher.CurrentDispatcher;task.ContinueWith(_=>d.BeginInvoke(new Action(()=>frame.Continue=false)));Dispatcher.PushFrame(frame);}return task.GetAwaiter().GetResult();}
    private sealed class Sample:IDisposable
    {
        public Fixture F{get;}=new(); public NguCanhNghiepVu Cash{get;} public NguCanhNghiepVu Doctor{get;}
        public string Rx{get;} public string Warehouse{get;} public KetQuaThanhToan? Paid{get;}
        public Sample(bool pay=true,bool second=false,bool longText=false)
        {
            try
            {
                F.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@id;DELETE dbo.DON_THUOC WHERE MaDT=@id",new{id=F.Id("DTA")});Doctor=F.User("A");
                var drug=F.Scalar<string>("SELECT TOP(1) MaThuoc FROM dbo.THUOC ORDER BY MaThuoc");F.Execute("UPDATE dbo.THUOC SET DonGiaBan=2000 WHERE MaThuoc=@drug",new{drug});
                var b=new BUS_DONTHUOC(F.Transaction);var r=new YeuCauLuuDon{MaLS=F.Id("LSA"),GhiChu="FIXTURE ghi chú đã ký",CacDong=[new(){MaThuoc=drug,SoLuong=2,SoNgayDung=2,LieuDung="FIXTURE liều",DuongDung="FIXTURE đường",HuongDan=longText?string.Join(" ",Enumerable.Repeat("FIXTURE không dùng thật",250))+" KẾT THÚC FIXTURE":"FIXTURE không dùng thật"}]};var s=b.LuuDon(r,Doctor);b.XacNhanDon(s.Don.MaDT,s.DauVanTay,Doctor);Rx=s.Don.MaDT;
                if(second){var n=b.LuuDon(r with{GhiChu="FIXTURE bổ sung"},Doctor);b.XacNhanDon(n.Don.MaDT,n.DauVanTay,Doctor);}
                var clinical=new BUS_KHAMDIEUTRI(F.Transaction);var c=clinical.LayBanNhap(F.Id("LSA"),Doctor);c=clinical.LuuKham(new(){MaLS=c.LanKham.MaLS,DauVanTay=c.DauVanTay,TrieuChung="FIXTURE",ChanDoan="FIXTURE",KeHoachXuLy="FIXTURE",TinhTrang=[new(){LoaiTinhTrang="FIXTURE",MoTaTinhTrang="FIXTURE",MucDo="FIXTURE"}]},Doctor);clinical.HoanTatKham(c.LanKham.MaLS,c.DauVanTay,Doctor);
                Warehouse=F.Id("WUI10");
                F.Execute("""
                    DECLARE @supplier nvarchar(50)=(SELECT TOP(1) MaNCC FROM dbo.NHA_CUNG_CAP ORDER BY MaNCC),@goods nvarchar(50)=(SELECT MaHH FROM dbo.THUOC WHERE MaThuoc=@drug);
                    INSERT dbo.KHO(MaKho,TenKho,MaNV,LoaiKho,TrangThai) VALUES(@warehouse,N'FIXTURE UI P10',@nv,N'Kho thuốc',N'Đang hoạt động');
                    INSERT dbo.HOA_DON_NHAP(MaHDN,SoHoaDonNCC,MaNCC,MaNV,NgayHoaDon,TongTien,TrangThai) VALUES(@prefix+'HNUI',@prefix,@supplier,@nv,DATEADD(day,-4,SYSDATETIME()),0,N'Nháp');
                    INSERT dbo.PHIEU_NHAP(MaPN,MaHDN,MaKho,MaNV,NgayNhap,TrangThai,LyDo) VALUES(@prefix+'PNUI',@prefix+'HNUI',@warehouse,@nv,DATEADD(day,-3,SYSDATETIME()),N'Nháp',N'FIXTURE');
                    INSERT dbo.LO_HANG_HOA(MaLo,MaHH,MaNCC,MaKho,SoLo,NgayNhap,HanSuDung,TrangThai) VALUES(@prefix+'LUI',@goods,@supplier,@warehouse,N'FIXTURE UI',DATEADD(day,-3,SYSDATETIME()),DATEADD(day,100,SYSDATETIME()),N'Còn sử dụng');
                    INSERT dbo.CHI_TIET_PHIEU_NHAP(MaCTPN,MaPN,MaHH,MaLo,SoLuong,DonGia) VALUES(@prefix+'CUI',@prefix+'PNUI',@goods,@prefix+'LUI',100,1);
                    UPDATE dbo.PHIEU_NHAP SET TrangThai=N'Đã ghi sổ' WHERE MaPN=@prefix+'PNUI';
                    """,new{warehouse=Warehouse,drug,prefix=F.Prefix,nv=F.Id("P")});
                Cash=F.User("C");var fee=new BUS_THANHTOANTRONGOI(F.Transaction).LapBangTinh(F.Id("LSA"),Warehouse,Cash);F.Transaction.Commit();
                if(pay)Paid=new BUS_THANHTOANTRONGOI().ThanhToan(new(){MaLS=fee.MaLS,MaKhoCapThuoc=Warehouse,MaYeuCau=Guid.NewGuid(),DauVanTayBangTinh=fee.DauVanTay!,SoTienKhachDua=500000},Cash);
            }
            catch{F.Dispose();throw;}
        }
        public NguCanhNghiepVu Use(string role){F.ActAs(role);return new BUS_QUYENNGHIEPVU().LayNguCanh(F.Id(role));}
        public string Financial()=>F.Connection.ExecuteScalar<string>("SELECT CONCAT((SELECT COUNT(*) FROM dbo.HOA_DON),':',(SELECT COUNT(*) FROM dbo.PHIEU_XUAT),':',(SELECT COUNT(*) FROM dbo.THANH_TOAN),':',(SELECT SUM(SoLuongTon) FROM dbo.VW_TON_KHO))")!;
        public void Dispose()=>F.Dispose();
    }
    internal static int Run()
    {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        using(var db=new DAL_DONTHUOC().MoKetNoi())if(!db.Database.StartsWith("RHM_ClinicalWorkflow_Test_P10Handover_",StringComparison.Ordinal))throw new InvalidOperationException("P10 UI cần DB riêng.");
        var pass=0;var fail=0;void Check(string name,Action action){try{action();pass++;Console.WriteLine("PASS: "+name);}catch(Exception ex){fail++;Console.Error.WriteLine("FAIL: "+name+": "+ex.GetBaseException().Message);}}
        Check("P10 WPF giao cần đối chiếu người nhận và hướng dẫn, không xuất thêm",()=>
        {using var s=new Sample();var v=new BanGiaoThuocEditor(s.Use("P"));v.FocusContext(s.F.Id("BN1"),s.F.Id("LSA"),s.Rx);Need(!Pump(v.TryGiaoAsync()),"Chưa đối chiếu vẫn giao");Find<CheckBox>(v,"ReceiverChecked").IsChecked=true;Find<CheckBox>(v,"GuidanceChecked").IsChecked=true;var before=s.Financial();Need(Pump(v.TryGiaoAsync())&&v.KetQua?.MaDT==s.Rx&&s.Financial()==before,"Sai đơn hoặc xuất thêm");Need(!Find<Button>(v,"GiveButton").IsEnabled,"Còn bật giao lại");var w=new Window{Content=v,Width=1366,Height=850,ShowInTaskbar=false};try{w.Show();w.UpdateLayout();var grid=Find<DataGrid>(v,"DrugLines");Need(grid.Items.Count>0&&grid.Columns.Count==4,"Mất cột/thuốc khi hiển thị");grid.ScrollIntoView(grid.Items[0]);grid.UpdateLayout();var cell=grid.Columns[0].GetCellContent(grid.Items[0]) as TextBlock;Need(cell is not null&&!string.IsNullOrEmpty(cell.Text)&&grid.Columns.All(c=>c.ActualWidth>0),$"Không hiển thị tên và hướng dẫn: cell={cell?.Text}, widths={string.Join(',',grid.Columns.Select(c=>c.ActualWidth))}");v.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);v.UpdateLayout();Need(grid.Columns[0].ActualWidth>=140&&grid.Columns[1].ActualWidth>=80&&grid.Columns[2].ActualWidth>=160&&grid.Columns[3].ActualWidth>=200,"Cột bị co về min width, không đọc được thuốc/hướng dẫn");var route=grid.Columns[2].GetCellContent(grid.Items[0]) as TextBlock;var row=(DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(grid.Items[0]);Need(route is not null,"Thiếu ô liều/đường/ngày");var probe=new TextBlock{Text=route!.Text,FontFamily=route.FontFamily,FontSize=route.FontSize,FontStyle=route.FontStyle,FontWeight=route.FontWeight,TextWrapping=TextWrapping.Wrap};probe.Measure(new Size(route.ActualWidth,double.PositiveInfinity));var holder=(DataGridCell)route.Parent;var available=row.ActualHeight-route.Margin.Top-route.Margin.Bottom-holder.Padding.Top-holder.Padding.Bottom;Need(available>=probe.DesiredSize.Height-0.5,$"Hàng cắt liều/đường/ngày: có {available}, cần {probe.DesiredSize.Height}");Save(v,"P10-da-giao.png");}finally{w.Close();}});
        Check("P10 WPF đang giao giữ đúng nguồn, đổi đơn xóa xác nhận cũ",()=>
        {using var s=new Sample(second:true);var v=new BanGiaoThuocEditor(s.Use("P"));v.FocusContext(s.F.Id("BN1"),s.F.Id("LSA"),s.Rx);Find<CheckBox>(v,"ReceiverChecked").IsChecked=Find<CheckBox>(v,"GuidanceChecked").IsChecked=true;using var db=new DAL_DONTHUOC().MoKetNoi();using var tx=db.BeginTransaction();db.Execute("DECLARE @r int;EXEC @r=sys.sp_getapplock @Resource=N'RHM:ThanhToan:Write',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=1000;IF @r<0 THROW 52900,N'Không giữ khóa',1",transaction:tx);var task=v.TryGiaoAsync();Need(v.DangXuLy&&!Find<Button>(v,"GiveButton").IsEnabled,"Không khóa khi giao");v.FocusContext(s.F.Id("BN2"),s.F.Id("LSU"),null);Need(v.ChiTiet?.Luot.MaDT==s.Rx,"Đổi nguồn khi đang giao");tx.Commit();Need(Pump(task),"Giao thất bại");var other=s.Paid!.CacDon.Single(d=>d.MaDT!=s.Rx).MaDT;v.FocusContext(s.F.Id("BN1"),s.F.Id("LSA"),other);Need(Find<CheckBox>(v,"ReceiverChecked").IsChecked!=true&&Find<CheckBox>(v,"GuidanceChecked").IsChecked!=true,"Giữ xác nhận của đơn trước");});
        Check("P10 WPF in lỗi sau thu giữ kết quả và in lại cùng hóa đơn",()=>
        {using var s=new Sample(pay:false);var v=new BangTinhPhiEditor(s.Cash);v.FocusContext(s.F.Id("LSA"));Find<ComboBox>(v,"WarehouseFilter").SelectedValue=s.Warehouse;Need(v.TryLapBangTinh(out var error),error??"Không tính phí");Find<TextBox>(v,"GivenText").Text="500000";Need(Pump(v.TryThanhToanAsync()),"Không thu được");var result=v.KetQua;var before=s.Financial();Need(!v.TryInHoaDon(_=>throw new System.IO.IOException("FIXTURE printer"),out _)&&ReferenceEquals(result,v.KetQua)&&Find<Button>(v,"PrintAgainButton").IsEnabled,"In lỗi làm mất kết quả thu");Need(v.TryInHoaDon(p=>Need(p.HoaDon.MaHD==result!.MaHD,"In sang hóa đơn khác"),out _)&&s.Financial()==before,"In lại đã thu/xuất lại");});
        Check("P10 WPF nguồn hóa đơn cố định và thử lỗi paginator không đổi tiền",()=>
        {using var s=new Sample();var id=s.Paid!.MaHD;var reads=0;var w=new XemTruocHoaDon(()=>{reads++;return new BUS_THANHTOANTRONGOI().LayBanInHoaDon(id,s.Cash);});try{var before=s.Financial();var date=w.NguonIn!.HoaDon.NgayLap;Need(!w.TryPrint(_=>throw new System.IO.IOException("FIXTURE spooler"),new Size(780,600),out _),"Không bắt lỗi in");Need(w.TryPrint(p=>Need(p.PageCount>0,"Không có trang"),new Size(780,600),out _)&&w.NguonIn!.HoaDon.MaHD==id&&w.NguonIn.HoaDon.NgayLap==date&&reads>=3&&s.Financial()==before,"In không đọc lại đúng chứng từ/đổi tiền");var doc=Find<FrameworkElement>(w,"InvoiceDocument");var lines=(IReadOnlyList<InvoicePrintLine>)doc.DataContext.GetType().GetProperty("Lines")!.GetValue(doc.DataContext)!;var unit=w.NguonIn!.CacDong.First(d=>d.MaCTPX is not null).DonViTinh!;Need(lines.Where(l=>l.MaCTPX.Count>0).All(l=>l.Quantity.Contains(unit)),"Bản in mất đơn vị chốt");}finally{w.Close();}});
        Check("P10 WPF đơn ký dài phân nhiều trang, giữ đủ chỉ định chốt",()=>
        {using var s=new Sample(longText:true);s.Use("A");var w=new XemTruocDonThuoc(()=>new BUS_DONTHUOC().LayBanInDon(s.Rx,s.Doctor));try{var pages=0;Need(w.TryPrint(p=>{p.ComputePageCount();pages=p.PageCount;for(var i=0;i<pages;i++)Need(p.GetPage(i)!=DocumentPage.Missing,"Thiếu trang");},new Size(500,350),out var error),error??"Không dựng bản in");var text=w.DonDaDoc is null?"":new TextRange(w.DonDaDoc.ContentStart,w.DonDaDoc.ContentEnd).Text;Need(pages>2&&text.Contains("KẾT THÚC FIXTURE"),"Mất cuối hướng dẫn dài hoặc thu nhỏ một trang");Need(text.Contains("FIXTURE ghi chú đã ký"),"Mất ghi chú ký trên bản in");}finally{w.Close();}});
        Check("P10 WPF nguồn in bị đổi mã sau mở không in nhầm chứng từ",()=>
        {using var first=new Sample();using var other=new Sample();first.Use("C");var id=first.Paid!.MaHD;var w=new XemTruocHoaDon(()=>new BUS_THANHTOANTRONGOI().LayBanInHoaDon(id,first.Cash));try{id=other.Paid!.MaHD;var sent=false;Need(!w.TryPrint(_=>sent=true,new Size(780,600),out _)&&!sent&&w.NguonIn!.HoaDon.MaHD==first.Paid.MaHD,"Reader đổi mã làm in nhầm hóa đơn");}finally{w.Close();}});
        Check("P10 WPF module dược sĩ mở hàng bàn giao và giữ đúng ngữ cảnh",()=>
        {using var s=new Sample();s.Use("P");DuLieuGiaoDien.ChonPhamViLamSang(true);try{var root=new DonThuoc();root.FocusContext(s.F.Id("BN1"),s.F.Id("LSA"),s.Rx);Need(root.Content is BanGiaoThuocEditor v&&v.ChiTiet?.Luot.MaDT==s.Rx&&root.PatientId==s.F.Id("BN1"),"Không vào bàn giao đúng đơn");}finally{DuLieuGiaoDien.ChonPhamViLamSang(false);}});
        Check("P10 WPF lỗi giao giữ mã đơn để thử lại, không đổi sang bệnh nhân khác",()=>
        {using var s=new Sample();var v=new BanGiaoThuocEditor(s.Use("P"));v.FocusContext(s.F.Id("BN1"),s.F.Id("LSA"),s.Rx);Find<CheckBox>(v,"ReceiverChecked").IsChecked=Find<CheckBox>(v,"GuidanceChecked").IsChecked=true;var before=s.Financial();s.F.Connection.Execute("CREATE TRIGGER dbo.CW_Test_UiHandoverFailure ON dbo.NHAT_KY_THAO_TAC AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE HanhDong=N'Bàn giao thuốc') THROW 52922,N'FIXTURE UI handover fail',1;END;");try{Need(!Pump(v.TryGiaoAsync()),"Lỗi cuối vẫn báo giao thành công");v.FocusContext(s.F.Id("BN2"),s.F.Id("LSU"),null);Need(v.ChiTiet?.Luot.MaDT==s.Rx&&!Find<CheckBox>(v,"ReceiverChecked").IsEnabled,"Đổi nguồn sau lỗi chưa rõ kết quả");}finally{s.F.Connection.Execute("DROP TRIGGER dbo.CW_Test_UiHandoverFailure");}Need(Pump(v.TryGiaoAsync())&&v.KetQua?.MaDT==s.Rx&&s.Financial()==before,"Thử lại sai mã hoặc xuất/thu thêm");});
        Console.WriteLine($"HandoverUiChecks: {pass} PASS, {fail} FAIL; commit chỉ trên DB P10Handover riêng.");return fail==0?0:1;
    }
    private static void Save(FrameworkElement view,string file)
    {view.Dispatcher.Invoke(()=>{},DispatcherPriority.Render);view.UpdateLayout();var bmp=new System.Windows.Media.Imaging.RenderTargetBitmap((int)view.ActualWidth,(int)view.ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);bmp.Render(view);var enc=new System.Windows.Media.Imaging.PngBitmapEncoder();enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));System.IO.Directory.CreateDirectory("obj/ClinicalWorkflowP10");using var output=System.IO.File.Create(System.IO.Path.Combine("obj/ClinicalWorkflowP10",file));enc.Save(output);}
}
