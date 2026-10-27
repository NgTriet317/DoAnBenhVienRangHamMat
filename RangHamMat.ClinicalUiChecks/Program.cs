using System.Data;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Dapper;
using DAL;
using ET;
using RangHamMat;
using RangHamMat.Controls;
using RangHamMat.Views;
using RangHamMat.ClinicalUiChecks;

internal static class Program
{
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    private static bool CanOpen(MainWindow home, string module)
        => (bool)typeof(MainWindow).GetMethod("CanOpen", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(home, new object[] { module })!;
    private static T Find<T>(FrameworkElement view, string name) where T : FrameworkElement => (T)view.FindName(name);

    private static void SignIn(string job)
    {
        using var db = new DAL_DONTHUOC().MoKetNoi();
        var account = db.QuerySingle<(string Employee, string Account, string Role)>("""
            SELECT TOP(1) n.MaNV Employee,t.MaTK Account,v.TenVaiTro Role FROM dbo.NHAN_VIEN n
            JOIN dbo.TAI_KHOAN t ON t.MaNV=n.MaNV JOIN dbo.VAI_TRO v ON v.MaVaiTro=t.MaVaiTro
            WHERE n.MaChucVu=@job AND n.TrangThai=N'Đang làm việc' AND t.TrangThai=N'Đang hoạt động' ORDER BY n.MaNV
            """, new { job });
        PhienDangNhap.MaNV = account.Employee;
        PhienDangNhap.IsAdmin = account.Role == "Admin";
        PhienDangNhap.KhoiTaoDinhDanhNghiepVu(account.Account);
    }

    [STAThread]
    private static int Main(string[] args)
    {
        var configured = Environment.GetEnvironmentVariable("RHM_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(configured) || !new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(configured).InitialCatalog.StartsWith("RHM_ClinicalWorkflow_Test_", StringComparison.OrdinalIgnoreCase))
        { Console.Error.WriteLine("FAIL: ClinicalUiChecks chỉ chạy với RHM_CONNECTION_STRING của bản sao RHM_ClinicalWorkflow_Test_*."); return 1; }
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/RangHamMat.Wpf;component/Themes/Theme.xaml", UriKind.Relative) });
        KiemTraNhapLieu.Register();
        var pass = 0; var fail = 0;
        void Check(string name, Action run)
        {
            try { run(); Console.WriteLine("PASS: " + name); pass++; }
            catch (Exception ex) { Console.Error.WriteLine("FAIL: " + name + ": " + ex.GetBaseException().Message); fail++; }
        }
        try
        {
            if(args.Contains("--payments")) return PaymentUiChecks.Run();
            if(args.Contains("--handover")) return HandoverUiChecks.Run();
            if(args.Contains("--experience")) return ExperienceUiChecks.Run();
            if(args.Contains("--experience-core")) return ExperienceUiChecks.Run(false);
            Check("P08 phân trang giữ trọn dòng truy vết CTPX ở nhiều khổ giấy",()=>
            {
                var document=new InvoiceDocument {DataContext=new {Lines=Enumerable.Range(0,10).Select(i=>new InvoicePrintLine("FIXTURE thuốc đã chốt "+i,1,1,1,Enumerable.Range(0,16).Select(j=>$"CTPX-{i:D2}-{j:D2}").ToArray())).ToArray()}};
                foreach(var height in new[]{240d,275d,310d,360d})
                {
                    var paginator=document.CreatePaginator(new Size(500,height));Require(paginator.PageCount>1,"Không thử được ngắt trang");
                    var source=(FrameworkElement)paginator.Source;var boundaries=new List<double>();var end=0d;
                    for(var i=0;i<paginator.PageCount;i++){var page=paginator.GetPage(i);end+=page.ContentBox.Height;boundaries.Add(end);}
                    var traces=TextBlocks(source).Where(t=>t.Text.StartsWith("Chi tiết xuất:")).ToArray();Require(traces.Length==10,"Mất truy vết khi phân trang");
                    foreach(var trace in traces)
                    {
                        Require(double.IsFinite(trace.LineHeight),"Dòng truy vết không có chiều cao hữu hạn, paginator bỏ qua dòng chữ");
                        var top=trace.TranslatePoint(new Point(),source).Y;
                        for(var offset=0d;offset<trace.ActualHeight-.01;offset+=trace.LineHeight)
                        {var bottom=top+Math.Min(offset+trace.LineHeight,trace.ActualHeight);Require(!boundaries.Any(b=>b>top+offset+.01&&b<bottom-.01),"Đường ngắt trang cắt đôi dòng truy vết");}
                    }
                }
            });
            Check("P08 tab mới giữ vị trí mở hóa đơn của giao diện legacy",()=>
            {
                var page=new HoaDon();var tabs=Find<TabControl>(page,"DetailTabs");tabs.SelectedIndex=0;
                Require(tabs.SelectedItem is TabItem t&&t.Visibility==Visibility.Visible&&Convert.ToString(t.Header)=="Hóa đơn / thu tiền","Lối mở hóa đơn cũ đang chọn tab ẩn");
            });
            Check("P08 xem bảng phí thực, đổi nguồn xóa bảng cũ và không ghi tài chính",()=>
            {
                using var f = new RangHamMat.ClinicalChecks.Fixture();
                f.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@id;DELETE dbo.DON_THUOC WHERE MaDT=@id",new{id=f.Id("DTA")});
                var a=f.User("A");var services=new BUS.BUS_DICHVUTHUCHIEN(f.Transaction);
                var draft=services.LuuThucHien(new(){MaLS=f.Id("LSA"),MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE TrangThai=N'Đang hoạt động' ORDER BY MaDV"),MaNVThucHien=a.MaNV,ThoiGianThucHien=f.Scalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())"),SoLuong=1,DonGia=100000,KetQua="FIXTURE riêng",LyDoPhatSinh="FIXTURE"},a);
                services.XacNhanThucHien(draft.DichVu.MaDVThucHien,draft.DauVanTay,a);
                var clinical=new BUS.BUS_KHAMDIEUTRI(f.Transaction);var s=clinical.LayBanNhap(f.Id("LSA"),a);
                s=clinical.LuuKham(new(){MaLS=f.Id("LSA"),DauVanTay=s.DauVanTay,TrieuChung="FIXTURE",ChanDoan="FIXTURE",KeHoachXuLy="FIXTURE",KhongKeThuoc=true,LyDoKhongKeThuoc="FIXTURE",TinhTrang=[new(){LoaiTinhTrang="FIXTURE",MoTaTinhTrang="FIXTURE",MucDo="FIXTURE"}]},a);clinical.HoanTatKham(f.Id("LSA"),s.DauVanTay,a);
                var before=f.Scalar<int>("SELECT COUNT(*) FROM dbo.HOA_DON");var view=new BangTinhPhiEditor(f.User("C"),f.Transaction);view.FocusContext(f.Id("LSA"));
                using var host=new WindowLifetime(new Window{Content=view,Width=1000,Height=720,ShowInTaskbar=false});host.Window.Show();host.Window.UpdateLayout();
                Require(view.TryLapBangTinh(out var error),error??"Không tính được phí");Require(view.BangTinh?.TongTien==100000,"Sai tổng hiển thị");
                Require(Find<DataGrid>(view,"ServicesGrid").Items.Count==1,"Thiếu nguồn dịch vụ");SavePreview(view,"P08-bang-tinh.png","obj/ClinicalWorkflowP08");
                view.FocusContext(f.Id("LSB"));Require(view.BangTinh is null&&Find<DataGrid>(view,"ServicesGrid").Items.Count==0,"Giữ phí của lượt cũ");
                Require(!view.TryLapBangTinh(out _),"Cho tính lượt chưa hoàn tất");Require(f.Scalar<int>("SELECT COUNT(*) FROM dbo.HOA_DON")==before,"Xem UI đã ghi hóa đơn");
            });
            Check("P08 gộp bản in theo dòng đơn/giá chốt, giữ mã CTPX và phần lẻ",()=>
            {
                var t=new DataTable();foreach(var c in new[]{"MaCTHD","MaCTPX","MaCTDT","MaDT","NoiDung"})t.Columns.Add(c);foreach(var c in new[]{"SoLuong","DonGiaChot","ThanhTien"})t.Columns.Add(c,typeof(decimal));
                t.Rows.Add("H1","X1","D1","RX1","Tên thuốc đã chốt",.005m,1m,.01m);t.Rows.Add("H2","X2","D1","RX1","Tên thuốc đã chốt",.005m,1m,.01m);t.Rows.Add("H3","X3","D1","RX1","Tên thuốc đã chốt",.005m,1m,.01m);
                t.Rows.Add("H4","X4","D2","RX2","Tên thuốc đã chốt",1m,2m,2m);
                var grouped=InvoicePrintLines.Group(t);Require(grouped.Count==2,"Gộp lẫn đơn/giá hoặc không gộp lô");var first=grouped[0];Require(first.ThanhTien==.03m&&first.SoLuong==.015m&&first.MaCTPX.SequenceEqual(new[]{"X1","X2","X3"})&&first.Description=="Tên thuốc đã chốt","Tính lại tiền/gỡ truy vết hoặc đọc tên danh mục hiện tại");
                var h=new DataTable();foreach(var c in new[]{"MaHD","TrangThai","HoTenBN","MaBN","MaLS","NguoiLap"})h.Columns.Add(c);h.Columns.Add("NgayLap",typeof(DateTime));foreach(var c in new[]{"TongTien","GiamGia","SoTienPhaiThu","DaThu","ConPhaiThu"})h.Columns.Add(c,typeof(decimal));
                h.Rows.Add("FIXTURE-HD","Đã xác nhận","FIXTURE","BN-IN","LS-IN","FIXTURE",DateTime.Now,2.03m,0m,2.03m,1m,1.03m);
                var preview=new XemTruocHoaDon(h.Rows[0],t);using var host=new WindowLifetime(preview);preview.Show();preview.UpdateLayout();
                var doc=Find<FrameworkElement>(preview,"InvoiceDocument");var data=doc.DataContext;var printRows=(IReadOnlyList<InvoicePrintLine>)data.GetType().GetProperty("Lines")!.GetValue(data)!;
                Require(printRows[0].Amount==first.Amount&&printRows[0].Trace.Contains("X3")&&data.GetType().GetProperty("Remaining")!.GetValue(data)!.ToString()!.Contains("1,03"),"Bản xem trước không dùng số chốt/truy vết hoặc mất lẻ cũ");
                SavePreview(doc,"P08-hoa-don-chot.png","obj/ClinicalWorkflowP08");
            });
            Check("P07 preview gắn đúng mã đơn dù editor đổi nguồn và đọc lại hiệu lực",()=>
            {
                using var f=new RangHamMat.ClinicalChecks.Fixture();f.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@id;DELETE dbo.DON_THUOC WHERE MaDT=@id",new{id=f.Id("DTA")});var actor=f.User("A");var b=new BUS.BUS_DONTHUOC(f.Transaction);var drug=f.Scalar<string>("SELECT TOP(1) MaThuoc FROM dbo.THUOC ORDER BY MaThuoc");var r=new YeuCauLuuDon{MaLS=f.Id("LSA"),CacDong=[new(){MaThuoc=drug,LieuDung="FIXTURE",DuongDung="FIXTURE",HuongDan="FIXTURE không dùng thật"}]};var a=b.LuuDon(r,actor);a=b.XacNhanDon(a.Don.MaDT,a.DauVanTay,actor);
                var view=new KeDonThuocEditor(actor,f.Transaction);view.FocusContext(f.Id("BN1"),f.Id("LSA"),a.Don.MaDT);var factory=view.GetType().GetMethod("TaoNguonInCoDinh")??throw new Exception("Preview chưa có nguồn riêng gắn mã đơn.");var reader=(Func<BanInDonThuoc>)factory.Invoke(view,null)!;
                var other=b.LuuDon(r with {GhiChu="FIXTURE bổ sung"},actor);other=b.XacNhanDon(other.Don.MaDT,other.DauVanTay,actor);view.FocusContext(f.Id("BN1"),f.Id("LSA"),other.Don.MaDT);Require(reader().Nguon.Don.MaDT==a.Don.MaDT,"Preview A chuyển thành nguồn B.");var replacement=b.LuuDon(r with {MaDTThayThe=a.Don.MaDT,LyDoThayThe="FIXTURE"},actor);b.XacNhanDon(replacement.Don.MaDT,replacement.DauVanTay,actor);var refused=false;try{reader();}catch(LoiKhamException){refused=true;}Require(refused,"Preview vẫn in bản đã ngừng áp dụng.");
            });
            Check("P07 cấu hình tìm được thuốc ngoài 100 kết quả đầu",()=>
            {
                using var f=new RangHamMat.ClinicalChecks.Fixture();var actor=f.User("A");f.Execute("DECLARE @i int=1,@kind nvarchar(50)=(SELECT TOP(1) MaLoaiHH FROM dbo.HANG_HOA ORDER BY MaHH);WHILE @i<=101 BEGIN INSERT dbo.HANG_HOA(MaHH,TenHH,MaLoaiHH,DonViTinh,MoTa,TrangThai) VALUES(@prefix+'HH'+CAST(@i as nvarchar(4)),N'AAA FIXTURE '+CAST(@i as nvarchar(4)),@kind,N'FIXTURE',N'FIXTURE',N'Đang hoạt động');INSERT dbo.THUOC(MaThuoc,MaHH,HamLuong,MucTonToiThieu,DonGiaBan) VALUES(@prefix+'TH'+CAST(@i as nvarchar(4)),@prefix+'HH'+CAST(@i as nvarchar(4)),N'FIXTURE',0,0);SET @i+=1;END",new{prefix=f.Prefix});var view=new HuongDanThuocEditor(actor,f.Transaction);Require(view.FindName("DrugQuery") is TextBox,"Cấu hình không có tìm thuốc ngoài 100 dòng.");Find<TextBox>(view,"DrugQuery").Text="TH001";Find<Button>(view,"FindDrugButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Require(Find<ComboBox>(view,"DrugChoice").Items.OfType<LuaChonDanhMuc>().Any(c=>c.Value=="TH001"),"Không tìm được thuốc sau dòng 100.");
            });
            Check("P07 cấu hình mới giữ mã thuốc khi chặn đổi nguồn và tải lại được",()=>
            {
                using var f=new RangHamMat.ClinicalChecks.Fixture();var view=new HuongDanThuocEditor(f.User("A"),f.Transaction);var picker=Find<ComboBox>(view,"DrugChoice");picker.SelectedIndex=0;var id=picker.SelectedValue;Find<TextBox>(view,"DoseEditor").Text="FIXTURE chưa lưu";picker.SelectedIndex=-1;Require(Equals(picker.SelectedValue,id),"Chặn đổi nguồn làm mất mã thuốc mới chưa lưu.");typeof(HuongDanThuocEditor).GetMethod("Reload_Click",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(view,new object[]{view,new RoutedEventArgs()});Require(!view.CoThayDoiChuaLuu&&Find<TextBox>(view,"DoseEditor").Text.Length==0,"Không bỏ được nháp mới để tiếp tục.");
            });
            Check("P07 cửa sổ in trực tiếp từ chối bản nháp",()=>
            {var denied=false;try{new XemTruocDonThuoc(new RxCard(new Dictionary<string,object?>{{"MaDT","__nháp__"},{"TrangThai","Nháp"}})).Close();}catch(LoiKhamException){denied=true;}Require(denied,"Cửa sổ in trực tiếp vẫn mở nháp.");});
            Check("P07 lịch sử/in dùng tên đơn vị chốt và từ chối cửa sổ in nháp",()=>
            {
                SignIn("CV005");DuLieuGiaoDien.ChonPhamViLamSang(true);var record=new Dictionary<string,object?>{{"MaThuoc","TH001"},{"TenThuocChot","FIXTURE tên đã ký"},{"HamLuongChot","FIXTURE hàm lượng"},{"DonViTinhChot","FIXTURE đơn vị"}};
                var line=new PrescriptionLine(record);Require(line.Label.Contains("FIXTURE tên đã ký")&&line.Label.Contains("FIXTURE hàm lượng")&&line.Unit=="FIXTURE đơn vị","Lịch sử/in đổi theo danh mục thay vì snapshot.");
                var denied=false;try{new XemTruocDonThuoc(new RxCard(new Dictionary<string,object?>{{"MaDT","__nháp__"},{"TrangThai","Nháp"}})).Close();}catch(LoiKhamException){denied=true;}Require(denied,"Cửa sổ in trực tiếp vẫn mở được nháp.");
            });
            Check("P07 form chung không tạo đơn thiếu dòng hoặc xóa bỏ nguồn nháp",()=>
            {
                SignIn("CV004");
                try
                {
                    using var f=new ReceptionFixture();var receiver=new BUS.BUS_QUYENNGHIEPVU().LayNguCanh(PhienDangNhap.MaNV);var receipt=new BUS.BUS_TIEPNHAN().TiepNhan(new(){MaBN=f.Patient,MaDV=f.Service,MaBS=f.Doctor,MaPhong=f.Room,LyDoKham="FIXTURE"},receiver);
                    PhienDangNhap.MaNV=f.Doctor;PhienDangNhap.KhoiTaoDinhDanhNghiepVu(f.Account);var actor=new BUS.BUS_QUYENNGHIEPVU().LayNguCanh(f.Doctor);var ls=new BUS.BUS_KHAMDIEUTRI().BatDauKham(receipt.MaPhieu,actor);DuLieuGiaoDien.ChonPhamViLamSang(true);
                    var blocked=false;try{DuLieuGiaoDien.Upsert("DON_THUOC",new Dictionary<string,object?>{{"MaLS",ls},{"MaBS",f.Doctor},{"NgayKe",DateTime.Now},{"TrangThai","Nháp"}});}catch(LoiKhamException){blocked=true;}
                    Require(blocked,"Form chung vẫn tạo được đơn rỗng ngoài aggregate P07.");
                    var root=new DonThuoc();root.FocusContext(f.Patient,ls);Require(root.Content is KeDonThuocEditor c&&c.PatientId==f.Patient,"Màn chính chưa nối editor P07.");
                }
                finally{SignIn("CV004");DuLieuGiaoDien.RefreshFromDatabase();}
            });
            Check("P07 chọn nhiều thuốc, chống trùng, lưu và in snapshot đã ký",()=>
            {
                using var f=new RangHamMat.ClinicalChecks.Fixture();f.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@id;DELETE dbo.DON_THUOC WHERE MaDT=@id",new{id=f.Id("DTA")});var actor=f.User("A");
                f.Execute("UPDATE dbo.BENH_NHAN SET NgaySinh='1990-01-02',DiUng=N'FIXTURE dị ứng riêng',TienSuBenh=N'FIXTURE tiền sử riêng' WHERE MaBN=@id",new{id=f.Id("BN1")});var type=typeof(DonThuoc).Assembly.GetType("RangHamMat.Views.KeDonThuocEditor")??throw new Exception("Thiếu editor kê đơn P07");var view=(UserControl)Activator.CreateInstance(type,actor,f.Transaction)!;
                type.GetMethod("FocusContext")!.Invoke(view,new object?[]{f.Id("BN1"),f.Id("LSA"),null});
                Require(view.FindName("SafetyHeader") is TextBlock safety&&safety.Text.Contains("FIXTURE dị ứng riêng")&&safety.Text.Contains("FIXTURE tiền sử riêng")&&safety.Text.Contains("02/01/1990")&&safety.Text.Contains("tuổi"),"Thiếu thông tin người bệnh cần kiểm tra trước ký.");
                using var host=new WindowLifetime(new Window{Content=view,Width=1366,Height=900,ShowInTaskbar=false});host.Window.Show();host.Window.UpdateLayout();
                var picker=Find<DataGrid>(view,"DrugGrid");foreach(var row in picker.Items.Cast<object>().Take(3).ToArray())picker.SelectedItems.Add(row);Find<Button>(view,"AddSelectedButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Find<Button>(view,"AddSelectedButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var lines=Find<DataGrid>(view,"RxLines");var input=((System.Collections.IList)lines.ItemsSource).Cast<DongThuocKeDon>().ToArray();Require(input.Length==3,"Thêm trùng hoặc không đủ ba thuốc.");Require(!InvokeTry(view,"TrySave"),"Lưu đơn thiếu chỉ định.");
                foreach(var line in input){line.LieuDung="FIXTURE không dùng thật";line.DuongDung="FIXTURE";line.HuongDan="FIXTURE không dùng thật";line.SoNgayDung=2;line.SoLuong=3;}
                Require(InvokeTry(view,"TrySave"),Find<TextBlock>(view,"ErrorText").Text);Require(!InvokeTry(view,"TryPreparePrint"),"In được nháp.");EditCell(lines,1,"FIXTURE ô đang nhập");Require((bool)type.GetProperty("CoThayDoiChuaLuu")!.GetValue(view)!,"Không nhận nháp ô đang nhập.");Require(!InvokeTry(view,"TrySign"),"Ký chỉ định đang sửa chưa lưu.");Find<Button>(view,"ReloadButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Require(InvokeTry(view,"TrySign"),Find<TextBlock>(view,"ErrorText").Text);Require(InvokeTry(view,"TryPreparePrint"),Find<TextBlock>(view,"ErrorText").Text);
                Require(lines.IsReadOnly&&!Find<Button>(view,"SaveButton").IsEnabled,"Ký rồi còn sửa dòng.");SavePreview(view,"don-thuoc-da-ky.png","obj/ClinicalWorkflowP07");
            });
            Check("P07 cấu hình cần bác sĩ duyệt và áp hướng dẫn có xác nhận phù hợp",()=>
            {
                using var f=new RangHamMat.ClinicalChecks.Fixture();var admin=f.User("X");var type=typeof(DonThuoc).Assembly.GetType("RangHamMat.Views.HuongDanThuocEditor")??throw new Exception("Thiếu cấu hình thuốc P07");var view=(UserControl)Activator.CreateInstance(type,admin,f.Transaction)!;
                var id=f.Scalar<string>("SELECT TOP(1) MaThuoc FROM dbo.THUOC ORDER BY MaThuoc");Find<ComboBox>(view,"DrugChoice").SelectedValue=id;Find<TextBox>(view,"PopulationEditor").Text="FIXTURE";Find<TextBox>(view,"RouteEditor").Text="FIXTURE";Find<TextBox>(view,"DoseEditor").Text="FIXTURE";Find<TextBox>(view,"InstructionEditor").Text="FIXTURE không dùng thật";
                Require(InvokeTry(view,"TrySave"),Find<TextBlock>(view,"ErrorText").Text);Require(!Find<Button>(view,"ApproveButton").IsEnabled&&!InvokeTry(view,"TryApprove"),"Admin duyệt hướng dẫn thay bác sĩ.");var doctor=f.User("A");var medical=(UserControl)Activator.CreateInstance(type,doctor,f.Transaction)!;Find<ComboBox>(medical,"DrugChoice").SelectedValue=id;Require(InvokeTry(medical,"TryApprove"),Find<TextBlock>(medical,"ErrorText").Text);
                var cart=new KeDonThuocEditor(doctor,f.Transaction);cart.FocusContext(f.Id("BN1"),f.Id("LSA"));Find<TextBox>(cart,"DrugSearch").Text=id;Find<Button>(cart,"SearchButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Find<DataGrid>(cart,"DrugGrid").SelectedIndex=0;Find<Button>(cart,"AddSelectedButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));var cartLines=Find<DataGrid>(cart,"RxLines");cartLines.SelectedIndex=0;Require(!cart.TryApplyGuidance(false,out _),"Áp mẫu chưa xác nhận phù hợp.");Require(cart.TryApplyGuidance(true,out var error),error);var row=(DongThuocKeDon)cartLines.SelectedItem;Require(row.DaXacNhanPhuHop&&row.PhienBanHuongDan==1&&row.LieuDung=="FIXTURE","Không áp đúng phiên bản đã duyệt.");
            });
            Check("P06 chọn lượt không tự khóa bộ chọn về một bệnh nhân",()=>
            {
                using var f=new RangHamMat.ClinicalChecks.Fixture();var actor=f.User("A");
                f.Execute("INSERT dbo.PHIEU_KHAM_BENH(MaPhieu,MaHS,MaPhong,MaBS,MaNVTiepNhan,SoThuTu,ThoiGianTiepNhan,LyDoKham,TrangThai) SELECT @id,MaHS,MaPhong,@doctor,MaNVTiepNhan,SoThuTu+1,ThoiGianTiepNhan,LyDoKham,TrangThai FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@source",new{id=f.Id("PHNEXT"),doctor=actor.MaNV,source=f.Id("PHU")});
                var other=new BUS.BUS_KHAMDIEUTRI(f.Transaction).BatDauKham(f.Id("PHNEXT"),actor);var view=new ThucHienDichVuEditor(actor,f.Transaction);var picker=Find<ComboBox>(view,"VisitPicker");
                picker.SelectedValue=f.Id("LSA");Require(picker.Items.OfType<LuaChonDanhMuc>().Any(c=>c.Value==other),"Chọn lượt đã làm mất người bệnh khác trong bộ chọn.");
                picker.SelectedValue=other;Require(Find<TextBlock>(view,"PatientHeader").Text.Contains(f.Id("BN2")),"Không chuyển được sang lượt người bệnh thứ hai.");
            });
            Check("P06 form chung không bỏ qua kiểm tra nguồn và xóa nháp",()=>
            {
                SignIn("CV004");
                try
                {
                    using var f=new ReceptionFixture();var receiver=new BUS.BUS_QUYENNGHIEPVU().LayNguCanh(PhienDangNhap.MaNV);
                    var receipt=new BUS.BUS_TIEPNHAN().TiepNhan(new(){MaBN=f.Patient,MaDV=f.Service,MaBS=f.Doctor,MaPhong=f.Room,LyDoKham="FIXTURE"},receiver);
                    PhienDangNhap.MaNV=f.Doctor;PhienDangNhap.KhoiTaoDinhDanhNghiepVu(f.Account);var actor=new BUS.BUS_QUYENNGHIEPVU().LayNguCanh(f.Doctor);var ls=new BUS.BUS_KHAMDIEUTRI().BatDauKham(receipt.MaPhieu,actor);DuLieuGiaoDien.ChonPhamViLamSang(true);
                    var blocked=false;try{DuLieuGiaoDien.Upsert("DICH_VU_THUC_HIEN",new Dictionary<string,object?>{{"MaLS",ls},{"MaDV",f.Service},{"MaNVThucHien",f.Doctor},{"ThoiGianThucHien",DateTime.Now},{"SoLuong",1m},{"DonGia",10000m},{"KetQua","FIXTURE"},{"TrangThai","Nháp"}});}catch(LoiKhamException){blocked=true;}
                    using var clock=new DAL_DONTHUOC().MoKetNoi();var actualTime=clock.ExecuteScalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())");
                    var line=new BUS.BUS_DICHVUTHUCHIEN().LuuThucHien(new(){MaLS=ls,MaDV=f.Service,MaNVThucHien=f.Doctor,ThoiGianThucHien=actualTime,SoLuong=1,DonGia=10000,LyDoPhatSinh="FIXTURE"},actor);
                    DuLieuGiaoDien.RefreshFromDatabase();var deleteBlocked=false;try{DuLieuGiaoDien.Delete("DICH_VU_THUC_HIEN",DuLieuGiaoDien.Find("DICH_VU_THUC_HIEN",line.DichVu.MaDVThucHien)!);}catch(LoiKhamException){deleteBlocked=true;}
                    Require(blocked&&deleteBlocked,"Form/cache chung vẫn ghi/xóa bỏ qua API nguồn P06.");
                }
                finally{SignIn("CV004");DuLieuGiaoDien.RefreshFromDatabase();}
            });
            Check("P06 tách dự kiến/thực tế, chọn bước giữ dịch vụ và ký riêng",()=>
            {
                using var f=new RangHamMat.ClinicalChecks.Fixture();var actor=f.User("A");
                var planBus=new BUS.BUS_KHAMDIEUTRI(f.Transaction);var serviceId=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE TrangThai=N'Đang hoạt động' ORDER BY MaDV");
                var plan=planBus.LuuKeHoach(new(){MaLS=f.Id("LSA"),CacBuoc=[new(){MaDV=serviceId,SoLuongDuKien=2,DonGiaDuKien=10000,NoiDungBuoc="FIXTURE"}]},actor);planBus.XacNhanKeHoach(plan.KeHoach.MaKHDT,plan.DauVanTay,actor);
                var type=typeof(DichVuThucHien).Assembly.GetType("RangHamMat.Views.ThucHienDichVuEditor")??throw new Exception("Thiếu workspace dịch vụ P06");
                var view=(UserControl)Activator.CreateInstance(type,actor,f.Transaction)!;type.GetMethod("FocusContext")!.Invoke(view,new object?[]{f.Id("BN1"),f.Id("LSA")});
                using var host=new WindowLifetime(new Window {Content=view,Width=1366,Height=900,ShowInTaskbar=false});host.Window.Show();host.Window.UpdateLayout();
                var planned=Find<DataGrid>(view,"PlannedGrid");Require(planned.Items.Count==1,"Không thấy bước kế hoạch.");planned.SelectedIndex=0;Find<Button>(view,"FromStepButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(!Find<ComboBox>(view,"ServiceChoice").IsEnabled && (string?)Find<ComboBox>(view,"ServiceChoice").SelectedValue==serviceId,"Thực hiện từ bước cho đổi dịch vụ.");
                Find<TextBox>(view,"ResultEditor").Text="FIXTURE kết quả";Require(InvokeTry(view,"TrySave"),Find<TextBlock>(view,"ErrorText").Text);
                Require(f.Scalar<int>("SELECT COUNT(*) FROM dbo.DICH_VU_THUC_HIEN WHERE MaLS=@id AND TrangThai=N'Nháp'",new{id=f.Id("LSA")})==1,"Lưu tự coi đã thực hiện.");
                Find<TextBox>(view,"QuantityEditor").Text="không phải số";Require(!InvokeTry(view,"TrySave"),"Ô lượng sai vẫn dùng giá trị cũ.");
                Find<Button>(view,"ReloadButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Require(InvokeTry(view,"TryConfirm"),Find<TextBlock>(view,"ErrorText").Text);
                Require(!Find<Button>(view,"SaveButton").IsEnabled,"Xác nhận rồi vẫn sửa.");SavePreview(view,"dich-vu-da-xac-nhan.png","obj/ClinicalWorkflowP06");
                var actual=Find<DataGrid>(view,"ActualGrid");var old=actual.SelectedItem;Find<Button>(view,"NewButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Require(actual.SelectedItem is null,"Dòng mới vẫn chọn dòng thực tế cũ.");actual.SelectedItem=old;Require(!Find<Button>(view,"SaveButton").IsEnabled,"Không mở lại được dòng cũ sau nhập dòng mới.");
            });
            Check("P06 điều dưỡng chỉ thấy và sửa kết quả dòng được giao",()=>
            {
                using var f=new RangHamMat.ClinicalChecks.Fixture();var doctor=f.User("A");var b=new BUS.BUS_DICHVUTHUCHIEN(f.Transaction);
                var serviceId=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE TrangThai=N'Đang hoạt động' ORDER BY MaDV");
                b.LuuThucHien(new(){MaLS=f.Id("LSA"),MaDV=serviceId,MaNVThucHien=f.Id("N"),ThoiGianThucHien=f.Scalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())"),SoLuong=1,DonGia=10000,LyDoPhatSinh="FIXTURE"},doctor);
                var nurse=f.User("N");var type=typeof(DichVuThucHien).Assembly.GetType("RangHamMat.Views.ThucHienDichVuEditor")??throw new Exception("Thiếu workspace dịch vụ P06");var view=(UserControl)Activator.CreateInstance(type,nurse,f.Transaction)!;
                type.GetMethod("FocusContext")!.Invoke(view,new object?[]{f.Id("BN1"),f.Id("LSA")});var grid=Find<DataGrid>(view,"ActualGrid");grid.SelectedIndex=0;
                Require(!Find<Button>(view,"NewButton").IsEnabled&&!Find<Button>(view,"ConfirmButton").IsEnabled&&!Find<TextBox>(view,"QuantityEditor").IsEnabled&&!Find<ComboBox>(view,"PerformerChoice").IsEnabled,"Điều dưỡng được đổi nguồn/lượng hoặc xác nhận.");
                Find<TextBox>(view,"ResultEditor").Text="FIXTURE đã thực hiện";Require(InvokeTry(view,"TrySave"),Find<TextBlock>(view,"ErrorText").Text);
                Require(f.Scalar<string>("SELECT KetQua FROM dbo.DICH_VU_THUC_HIEN WHERE MaLS=@id",new{id=f.Id("LSA")})=="FIXTURE đã thực hiện","Không lưu kết quả điều dưỡng.");
                Find<TextBox>(view,"ResultEditor").Text="FIXTURE nháp";type.GetMethod("FocusContext")!.Invoke(view,new object?[]{"__khác__","__khác__"});Require(Find<TextBox>(view,"ResultEditor").Text=="FIXTURE nháp","Đổi nguồn mất nháp điều dưỡng.");
            });
            Check("Kế hoạch cũ vẫn hiện dịch vụ đã ngừng hoạt động",()=>
            {
                using var f=new RangHamMat.ClinicalChecks.Fixture();var actor=f.User("A");var serviceId=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE TrangThai=N'Đang hoạt động' ORDER BY MaDV");
                var serviceName=f.Scalar<string>("SELECT TenDV FROM dbo.DICH_VU WHERE MaDV=@id",new{id=serviceId});
                var plan=new BUS.BUS_KHAMDIEUTRI(f.Transaction).LuuKeHoach(new(){MaLS=f.Id("LSA"),CacBuoc=[new(){MaDV=serviceId,NoiDungBuoc="FIXTURE"}]},actor);
                f.Execute("UPDATE dbo.DICH_VU SET TrangThai=N'Ngừng hoạt động' WHERE MaDV=@id",new{id=serviceId});
                var view=new KhamLamSang(f.Id("LSA"),actor,f.Transaction);Find<TabControl>(view,"ClinicalTabs").SelectedIndex=2;Find<ComboBox>(view,"PlanPicker").SelectedValue=plan.KeHoach.MaKHDT;
                using var host=new WindowLifetime(new Window {Content=view,Width=1366,Height=900,ShowInTaskbar=false});host.Window.Show();host.Window.UpdateLayout();
                var grid=Find<DataGrid>(view,"PlanSteps");var cell=grid.Columns[0].GetCellContent(grid.Items[0]) as ComboBox;
                Require(cell is not null && cell.Text.Contains(serviceName),"Dịch vụ nguồn đã ngừng bị hiện trống.");
            });
            foreach(var target in new[]{"ConditionGrid","PlanSteps","PlanStart","TemplateSteps","NewCategoryName"})
                Check("Bảo vệ nháp đang nhập thật: "+target,()=>
                {
                    using var f=new RangHamMat.ClinicalChecks.Fixture();var actor=f.User("A");
                    var clinical=target is "ConditionGrid" or "PlanSteps" or "PlanStart";
                    UserControl view=clinical?new KhamLamSang(f.Id("LSA"),actor,f.Transaction):new MauDieuTriEditor(actor,f.Transaction);
                    if(target=="ConditionGrid")
                    {
                        ((System.Collections.IList)Find<DataGrid>(view,target).ItemsSource).Add(new TinhTrangKhamNhap {LoaiTinhTrang="FIXTURE",MoTaTinhTrang="FIXTURE",MucDo="FIXTURE"});
                        Require(InvokeTry(view,"TrySave"),"Không chuẩn bị được tình trạng đã lưu.");
                    }
                    if(target=="PlanSteps")
                    {
                        ((System.Collections.IList)Find<DataGrid>(view,target).ItemsSource).Add(new BuocKeHoachNhap {MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE TrangThai=N'Đang hoạt động' ORDER BY MaDV"),NoiDungBuoc="FIXTURE"});
                        Require(InvokeTry(view,"TrySavePlan"),"Không chuẩn bị được kế hoạch đã lưu.");
                    }
                    if(target=="TemplateSteps")
                    {
                        var category=new ET_DMTINHTRANGBENH {TenTinhTrang="FIXTURE",TrangThai="Đang áp dụng"};new BUS.BUS_DMTINHTRANGBENH(f.Transaction).LuuNhap(category,actor);
                        var template=new BUS.BUS_KHAMDIEUTRI(f.Transaction).LuuMauDieuTri(new(){MaDMTT=category.MaDMTT,TenMau="FIXTURE",CacBuoc=[new(){MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE TrangThai=N'Đang hoạt động' ORDER BY MaDV"),NoiDungBuoc="FIXTURE"}]},actor);
                        // Reload picks up the fixture created after editor construction.
                        typeof(MauDieuTriEditor).GetMethod("Reload_Click",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(view,new object[]{view,new RoutedEventArgs()});
                        Find<ComboBox>(view,"TemplatePicker").SelectedValue=template.Mau.MaMauKH;
                    }
                    if(clinical)Find<TabControl>(view,"ClinicalTabs").SelectedIndex=target=="ConditionGrid"?1:2;
                    using var host=new WindowLifetime(new Window {Content=view,Width=1366,Height=900,ShowInTaskbar=false});host.Window.Show();host.Window.UpdateLayout();
                    TextBox? dateEditor=null;
                    if(target=="PlanStart")
                    {
                        var date=Find<DatePicker>(view,target);date.ApplyTemplate();
                        var text=(TextBox)date.Template.FindName("PART_TextBox",date);dateEditor=text;text.Focus();text.Text="31/";
                        Require(date.SelectedDate is null,"Ngày chưa commit phải chưa có SelectedDate.");
                    }
                    else if(target=="NewCategoryName")Find<TextBox>(view,target).Text="FIXTURE danh mục chưa lưu";
                    else EditCell(Find<DataGrid>(view,target),target=="ConditionGrid"?2:target=="PlanSteps"?4:3,"FIXTURE ô chưa commit");
                    try
                    {
                        var back=false;
                        if(view is KhamLamSang c)c.BackRequested+=()=>back=true;else ((MauDieuTriEditor)view).BackRequested+=()=>back=true;
                        view.GetType().GetMethod("Back_Click",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(view,new object[]{view,new RoutedEventArgs()});
                        Require(!back,"Quay lại đã bỏ nội dung chưa lưu trong "+target);
                    }
                    finally{dateEditor?.Clear();}
                });
            Check("Màn khám bắt đầu từ hàng đợi và không làm mất nháp khi đổi bộ lọc",()=>
            {
                SignIn("CV004");
                try
                {
                    using var f=new ReceptionFixture();var receiver=new BUS.BUS_QUYENNGHIEPVU().LayNguCanh(PhienDangNhap.MaNV);
                    var receipt=new BUS.BUS_TIEPNHAN().TiepNhan(new(){MaBN=f.Patient,MaDV=f.Service,MaBS=f.Doctor,MaPhong=f.Room,LyDoKham="FIXTURE P05 giao diện"},receiver);
                    PhienDangNhap.MaNV=f.Doctor;PhienDangNhap.KhoiTaoDinhDanhNghiepVu(f.Account);DuLieuGiaoDien.ChonPhamViLamSang(true);
                    var page=new KhamDieuTri();using var host=new WindowLifetime(new Window {Content=page,Width=1366,Height=768,ShowInTaskbar=false});host.Window.Show();host.Window.UpdateLayout();
                    Find<Button>(page,"AddVisitButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Find<ComboBox>(page,"ReceiptChoice").SelectedValue=receipt.MaPhieu;
                    Find<Button>(page,"CreateVisitButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));host.Window.UpdateLayout();
                    var profile=new HoSoBenhNhan(DuLieuGiaoDien.Find("BENH_NHAN",f.Patient)!);
                    var source=DuLieuGiaoDien.Rows("LICH_SU_KHAM_BENH").Single(r=>UiRecord.Text(r,"MaPhieu")==receipt.MaPhieu);profile.OpenVisit(UiRecord.Text(source,"MaLS"));
                    var history=(ChiTietKhamDieuTri)Find<ContentControl>(profile,"VisitHost").Content;
                    Require(history.Content is KhamLamSang historyView && !Find<Button>(historyView,"SaveClinicalButton").IsEnabled && Find<DataGrid>(historyView,"ConditionGrid").IsReadOnly,"Lịch sử hồ sơ còn mở nội dung nhập có thể mất khi đổi bệnh nhân.");
                    var detail=Find<ContentControl>(page,"DetailHost").Content as ChiTietKhamDieuTri ?? throw new Exception("Không mở lần khám sau bắt đầu.");
                    var clinical=detail.Content as UserControl ?? throw new Exception("Không có không gian khám.");Require(clinical.GetType().Name=="KhamLamSang","Chưa nối không gian khám P05.");
                    Find<TextBox>(clinical,"SymptomsEditor").Text="FIXTURE nháp chưa lưu";
                    SavePreview(page,"kham-bat-dau.png","obj/ClinicalWorkflowP05");
                    Find<TextBox>(page,"SearchBox").Text="__không có kết quả__";
                    Require(ReferenceEquals(Find<ContentControl>(page,"DetailHost").Content,detail) && Find<TextBox>(clinical,"SymptomsEditor").Text=="FIXTURE nháp chưa lưu","Đổi bộ lọc mất nháp đang nhập.");
                    page.FocusContext("__khác__","__khác__");Require(ReferenceEquals(Find<ContentControl>(page,"DetailHost").Content,detail),"Điều hướng đè nháp chưa lưu.");
                }
                finally{SignIn("CV004");DuLieuGiaoDien.RefreshFromDatabase();}
            });
            Check("Không gian khám giữ nháp qua tab, ký riêng và khóa sau hoàn tất",()=>
            {
                using var f=new RangHamMat.ClinicalChecks.Fixture();f.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@id;DELETE dbo.DON_THUOC WHERE MaDT=@id",new{id=f.Id("DTA")});
                var actor=f.User("A");var type=typeof(KhamDieuTri).Assembly.GetType("RangHamMat.Views.KhamLamSang")??throw new Exception("Chưa có không gian khám P05");
                var view=(UserControl)Activator.CreateInstance(type,f.Id("LSA"),actor,f.Transaction)!;
                Find<TextBox>(view,"SymptomsEditor").Text="FIXTURE đổi triệu chứng";
                ((System.Collections.IList)Find<DataGrid>(view,"ConditionGrid").ItemsSource).Add(new TinhTrangKhamNhap {LoaiTinhTrang="FIXTURE",MoTaTinhTrang="FIXTURE",MucDo="FIXTURE"});
                var tabs=Find<TabControl>(view,"ClinicalTabs");tabs.SelectedIndex=1;tabs.SelectedIndex=2;tabs.SelectedIndex=0;
                Require(Find<TextBox>(view,"SymptomsEditor").Text=="FIXTURE đổi triệu chứng","Chuyển tab mất nháp.");
                Require(!InvokeTry(view,"TryComplete"),"Ký nội dung chưa lưu.");
                Find<CheckBox>(view,"NoPrescription").IsChecked=true;Find<TextBox>(view,"NoPrescriptionReason").Text="FIXTURE không chỉ định";
                Require(InvokeTry(view,"TrySave"),Find<TextBlock>(view,"ClinicalError").Text);
                ((System.Collections.IList)Find<DataGrid>(view,"PlanSteps").ItemsSource).Add(new BuocKeHoachNhap {MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU ORDER BY MaDV"),NoiDungBuoc="FIXTURE buổi tương lai",LanThu=2});
                Require(InvokeTry(view,"TrySavePlan"),Find<TextBlock>(view,"ClinicalError").Text);
                Find<TextBox>(view,"PlanNote").Text="FIXTURE sửa kế hoạch thủ công đã lưu";
                Require(InvokeTry(view,"TrySavePlan"),Find<TextBlock>(view,"ClinicalError").Text);
                Require(!InvokeTry(view,"TryComplete"),"Hoàn tất khi còn kế hoạch nháp.");
                Require(InvokeTry(view,"TryConfirmPlan"),Find<TextBlock>(view,"ClinicalError").Text);
                Require(InvokeTry(view,"TryComplete"),Find<TextBlock>(view,"ClinicalError").Text);
                Require(!Find<Button>(view,"SaveClinicalButton").IsEnabled && f.Scalar<string>("SELECT TrangThai FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@id",new{id=f.Id("PHA")})=="Chờ thanh toán","Chưa khóa/chuyển quầy sau ký.");
            });
            Check("Cấu hình mẫu Admin chỉ soạn, bác sĩ duyệt bằng định danh phiên",()=>
            {
                using var f=new RangHamMat.ClinicalChecks.Fixture();var admin=f.User("X");
                var cat=new ET_DMTINHTRANGBENH {TenTinhTrang="FIXTURE không dùng thật",TrangThai="Đang áp dụng"};new BUS.BUS_DMTINHTRANGBENH(f.Transaction).LuuNhap(cat,admin);
                var type=typeof(KhamDieuTri).Assembly.GetType("RangHamMat.Views.MauDieuTriEditor")??throw new Exception("Chưa có cấu hình mẫu P05");
                var view=(UserControl)Activator.CreateInstance(type,admin,f.Transaction)!;
                Find<ComboBox>(view,"CategoryPicker").SelectedValue=cat.MaDMTT;Find<TextBox>(view,"TemplateName").Text="FIXTURE mẫu không dùng thật";
                ((System.Collections.IList)Find<DataGrid>(view,"TemplateSteps").ItemsSource).Add(new BuocKeHoachNhap {MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU ORDER BY MaDV"),NoiDungBuoc="FIXTURE không dùng thật"});
                Require(InvokeTry(view,"TrySaveTemplate"),Find<TextBlock>(view,"TemplateError").Text);
                var current=(BanNhapMauDieuTri)type.GetProperty("MauDangMo")!.GetValue(view)!;
                Require(!Find<Button>(view,"ApproveTemplateButton").IsEnabled && !InvokeTry(view,"TryApproveTemplate"),"Admin tự ký được mẫu.");
                var doctor=f.User("A");var medical=(UserControl)Activator.CreateInstance(type,doctor,f.Transaction)!;
                Find<ComboBox>(medical,"TemplatePicker").SelectedValue=current.Mau.MaMauKH;
                Require(InvokeTry(medical,"TryApproveTemplate"),Find<TextBlock>(medical,"TemplateError").Text);
                var approved=(BanNhapMauDieuTri)type.GetProperty("MauDangMo")!.GetValue(medical)!;
                Require(approved.Mau.MaBSDuyet==doctor.MaNV && approved.Mau.TrangThai=="Đã duyệt","Không ghi bác sĩ duyệt từ phiên.");
            });
            Check("Lịch sử lịch hẹn không mở nút sửa, xác nhận hoặc hủy",()=>
            {
                SignIn("CV004");DuLieuGiaoDien.ChonPhamViLamSang(true);
                var appointment=DuLieuGiaoDien.Rows("LICH_KHAM").First(v=>UiRecord.Text(v,"TrangThai") is "Nháp" or "Đã xác nhận" or "Chờ khám");
                var detail=new ChiTietHoSo("LICH_KHAM","Lịch sử lịch hẹn",appointment);
                Require(Find<Button>(detail,"EditButton").Visibility!=Visibility.Visible && Find<Button>(detail,"ConfirmButton").Visibility!=Visibility.Visible && Find<Button>(detail,"CancelButton").Visibility!=Visibility.Visible,"Lịch sử chỉ đọc vẫn có nút sửa/hủy.");
            });
            Check("Lần khám gần nhất của đường cache legacy không quét lại từng hồ sơ",()=>
            {
                SignIn("CV004");DuLieuGiaoDien.ChonPhamViLamSang(true);
                try
                {
                    DuLieuGiaoDien.Replace("BENH_NHAN",Enumerable.Range(0,1000).Select(i=>(object)new Dictionary<string,object?> {{"MaBN","PERFBN"+i},{"HoTenBN","FIXTURE "+i},{"TrangThai","Đang theo dõi"}}));
                    DuLieuGiaoDien.Replace("HO_SO_BENH_AN",Enumerable.Range(0,1000).Select(i=>(object)new Dictionary<string,object?> {{"MaHS","PERFHS"+i},{"MaBN","PERFBN"+i}}));
                    DuLieuGiaoDien.Replace("PHIEU_KHAM_BENH",Enumerable.Range(0,10000).Select(i=>(object)new Dictionary<string,object?> {{"MaPhieu","PERFPH"+i},{"MaHS","PERFHS"+(i%1000)}}));
                    DuLieuGiaoDien.Replace("LICH_SU_KHAM_BENH",Enumerable.Range(0,10000).Select(i=>(object)new Dictionary<string,object?> {{"MaLS","PERFLS"+i},{"MaPhieu","PERFPH"+i},{"ThoiGianKham",DateTime.Today.AddDays(i)}}));
                    typeof(DuLieuGiaoDien).GetField("<PhamViLamSang>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,false);
                    var timer=System.Diagnostics.Stopwatch.StartNew();var view=new BenhNhan();var grid=Find<DataGrid>(view,"Grid_BENH_NHAN");
                    Require(grid.Items.Count==15,"Ca cache legacy không có đủ trang 15 BN.");
                    foreach(var row in grid.Items.Cast<object>())
                    {
                        Require(UiRecord.Text(row,"LanKhamGanNhat").StartsWith("Khám gần nhất:"),"Mất lần khám gần nhất.");
                        var patientIndex=int.Parse(UiRecord.Text(row,"MaBN")[6..]);
                        Require(UiRecord.Text(row,"LanKhamGanNhat").EndsWith(DateTime.Today.AddDays(9000+patientIndex).ToString("dd/MM/yyyy")),"Lần khám gần nhất trộn bệnh nhân.");
                        Require(timer.Elapsed<TimeSpan.FromSeconds(3),"Trang bệnh nhân vượt 3 giây khi tính lịch sử ("+timer.ElapsedMilliseconds+"ms).");
                    }
                    Console.WriteLine("PERF: 1000 BN/10000 lượt, trang đầu và tính lần khám gần nhất: "+timer.ElapsedMilliseconds+"ms");
                }
                finally {typeof(DuLieuGiaoDien).GetField("<PhamViLamSang>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,true);DuLieuGiaoDien.RefreshFromDatabase();}
            });
            Check("WPF tiếp nhận lưu hồ sơ/phiếu rồi mới tải cache và báo hàng đợi",()=>
            {
                SignIn("CV004");
                using(var fixture=new ReceptionFixture())
                {
                    DuLieuGiaoDien.ChonPhamViLamSang(true);
                    var view=new TiepNhanKham();BieuMauHoSo.Configure(view,"PHIEU_KHAM_BENH",patient:fixture.Patient);
                    using var host=new WindowLifetime(new Window {Content=view,Width=850,Height=740,ShowInTaskbar=false});host.Window.Show();host.Window.UpdateLayout();
                    Find<ComboBox>(view,"ServicePicker").SelectedValue=fixture.Service;
                    Find<ComboBox>(view,"DoctorPicker").SelectedValue=fixture.Doctor;
                    Find<TextBox>(view,"ArrivalReason").Text="FIXTURE không dùng thật";
                    Find<TextBox>(view,"ArrivalReason").BringIntoView();host.Window.UpdateLayout();
                    SavePreview(view,"tiep-nhan-truoc-luu.png");
                    Require(view.TryReceive(out var error),error);
                    var receipt=view.SavedResult ?? throw new Exception("Thiếu kết quả lưu.");
                    Require(receipt.MaBN==fixture.Patient && receipt.MaBS==fixture.Doctor,"Sai người bệnh/bác sĩ sau lưu.");
                    Require(DuLieuGiaoDien.Find("PHIEU_KHAM_BENH",receipt.MaPhieu) is not null,"Không tải cache sau commit.");
                    Require(fixture.CountVisits(receipt.MaPhieu)==0 && view.FindName("OpenVisitAfterSave") is null,"Tiếp nhận mở/tạo chẩn đoán.");
                    Require(Find<TextBlock>(view,"SavedStatus").Text.Contains("Đã chuyển vào hàng đợi") && !Find<Button>(view,"SaveReceptionButton").IsEnabled,"Thiếu phản hồi hoặc còn cho gửi lặp.");
                    Require(view.TryReceive(out _) && view.SavedResult==receipt,"Bấm lại không giữ biên nhận.");
                    host.Window.UpdateLayout();SavePreview(view,"tiep-nhan-da-luu.png");
                }
                DuLieuGiaoDien.RefreshFromDatabase();
            });
            Check("Danh sách tìm ngày sinh và giữ mã bệnh nhân khi chọn",()=>
            {
                SignIn("CV004");DuLieuGiaoDien.ChonPhamViLamSang(true);
                var patient=DuLieuGiaoDien.Rows("BENH_NHAN").First(v=>UiRecord.Date(v,"NgaySinh") is not null);
                var view=new BenhNhan();Find<TextBox>(view,"SearchBox").Text=UiRecord.Date(patient,"NgaySinh")!.Value.ToString("dd/MM/yyyy");
                var list=Find<DataGrid>(view,"Grid_BENH_NHAN");Require(list.Items.Cast<object>().Any(v=>UiRecord.Text(v,"MaBN")==UiRecord.Text(patient,"MaBN")),"Tìm theo ngày sinh không có đúng BN.");
                view.OpenProfile(patient);Require(view.PatientId==UiRecord.Text(patient,"MaBN"),"Lựa chọn không theo mã BN.");
            });
            Check("Tiếp nhận đổi dịch vụ xóa bác sĩ/phòng, không cho nhập nhân viên và số thứ tự", () =>
            {
                SignIn("CV004"); DuLieuGiaoDien.ChonPhamViLamSang(true);
                var patient=DuLieuGiaoDien.Rows("BENH_NHAN").First();
                var view=new TiepNhanKham();
                BieuMauHoSo.Configure(view,"PHIEU_KHAM_BENH",patient:UiRecord.Text(patient,"MaBN"));
                using var host=new WindowLifetime(new Window {Content=view,Width=850,Height=740,ShowInTaskbar=false});host.Window.Show();host.Window.UpdateLayout();
                var service=Find<ComboBox>(view,"ServicePicker");var doctor=Find<ComboBox>(view,"DoctorPicker");var room=Find<ComboBox>(view,"RoomPicker");
                foreach(var item in service.Items.Cast<object>().ToArray())
                {
                    service.SelectedItem=item;
                    if(doctor.Items.Count>0) {doctor.SelectedIndex=0;break;}
                }
                Require(doctor.SelectedItem is not null,"Baseline cần bác sĩ phù hợp ít nhất một dịch vụ.");
                service.SelectedIndex=-1;
                Require(doctor.SelectedItem is null && room.SelectedItem is null,"Đổi dịch vụ còn bác sĩ/phòng cũ.");
                Require(Find<TextBox>(view,"QueueNumber").IsReadOnly,"Cho nhập số thứ tự từ UI.");
                Require(Find<ComboBox>(view,"AppointmentPicker").Items.Count>0,"Thiếu nguồn trực tiếp/lịch hẹn.");
                Require(Find<TextBlock>(view,"SystemInfo").Text.Contains("tự cấp"),"Không giải thích giờ/số do hệ thống cấp.");
            });
            Check("Bốn tab hồ sơ cho xem lịch hẹn dù không mở module lập lịch",()=>
            {
                SignIn("CV004");DuLieuGiaoDien.ChonPhamViLamSang(true);
                var view=new HoSoBenhNhan(DuLieuGiaoDien.Rows("BENH_NHAN").First());view.ConfigureAccess(module=>module!="LichKham");
                Require(Find<TabControl>(view,"ProfileTabs").Items.Count==4,"Không đủ bốn tab hồ sơ.");
                Require(Find<TabItem>(view,"AppointmentsTab").Visibility==Visibility.Visible,"Lịch sử hẹn bị che theo quyền lập lịch.");
                Require(!Find<Button>(Find<LichKham>(view,"PatientAppointments"),"AddAppointmentButton").IsEnabled,"Lịch sử hẹn cho sửa ngoài quyền.");
            });
            Check("Quyền mở năm module lâm sàng tách khỏi quyền module cũ", () =>
            {
                SignIn("CV002");
                var home = new MainWindow();
                try
                {
                    foreach (var module in new[] { "BenhNhan", "KhamDieuTri", "DonThuoc", "DichVuThucHien", "HoaDon" })
                        Require(CanOpen(home, module), "Bác sĩ không mở được " + module);
                    foreach (var module in new[] { "NhanSu", "BaoCao", "NhapKho", "XuatKho", "LichKham", "TaiKhoan" })
                        Require(!CanOpen(home, module), "Quyền ngoài phạm vi bị mở rộng: " + module);
                    foreach (var module in new[] { "TongQuan", "CaLamViec", "XinNghiViec" }) Require(CanOpen(home, module), "Mất quyền User baseline.");
                }
                finally { home.Close(); }
            });
            Check("SQL scope cấp vào view khám, bác sĩ không sửa hồ sơ hành chính", () =>
            {
                SignIn("CV002"); DuLieuGiaoDien.ChonPhamViLamSang(true);
                var patients = new BenhNhan(); patients.ConfigureAccess(_ => true);
                Require(!Find<Button>(patients, "AddPatientButton").IsEnabled, "Quyền mở hồ sơ bị dùng làm quyền tạo BN.");
                var visits = DuLieuGiaoDien.Rows("LICH_SU_KHAM_BENH");
                Require(visits.All(v => UiRecord.Text(v, "TrangThai") == "Đã xác nhận" || UiRecord.Text(v, "MaBS") == PhienDangNhap.MaNV), "View nhận nháp của bác sĩ khác.");
                var page = new KhamDieuTri();
                page.FocusContext(null, "__missing__");
                Require(Find<ContentControl>(page, "DetailHost").Content is null, "Mã không tồn tại tự chọn lượt khác.");
                var patient = DuLieuGiaoDien.Rows("BENH_NHAN").FirstOrDefault();
                if (patient is not null)
                {
                    var profile = new HoSoBenhNhan(patient); profile.ConfigureAccess(_ => true);
                    Require(!Find<Button>(profile, "EditPatientButton").IsEnabled && !Find<Button>(profile, "ReceptionButton").IsEnabled, "Bác sĩ được tự tiếp nhận/sửa BN.");
                }
                else Require(Find<TextBlock>(patients,"ListEmpty").Visibility == Visibility.Visible, "Chưa được phân BN phải hiển thị rỗng.");
            });
            Check("Đổi phiên xóa collection cũ trước khi tải phạm vi mới", () =>
            {
                SignIn("CV004"); DuLieuGiaoDien.ChonPhamViLamSang(true);
                var old = DuLieuGiaoDien.Rows("BENH_NHAN"); Require(old.Count > 0, "Baseline thiếu BN.");
                SignIn("CV002");
                Require(old.Count == 0, "Cache phiên tiếp nhận còn nguyên sau đổi phiên.");
                var own = DuLieuGiaoDien.Rows("BENH_NHAN");
                using var db = new DAL_DONTHUOC().MoKetNoi();
                var ids = db.Query<string>("SELECT DISTINCT hs.MaBN FROM dbo.HO_SO_BENH_AN hs JOIN dbo.PHIEU_KHAM_BENH p ON p.MaHS=hs.MaHS WHERE p.MaBS=@id AND p.TrangThai<>N'Hủy'", new { id = PhienDangNhap.MaNV }).ToHashSet();
                Require(own.All(p => ids.Contains(UiRecord.Text(p, "MaBN"))), $"Cache mới có BN ngoài phân công. Phiên={PhienDangNhap.MaNV}, nguồn={DuLieuGiaoDien.NguoiDungLamSang.MaNV}, quyền={DuLieuGiaoDien.NguoiDungLamSang.Quyen}, ngoài phạm vi={string.Join(',',own.Select(p=>UiRecord.Text(p,"MaBN")).Where(id=>!ids.Contains(id)))}");
            });
            Check("Lịch hẹn trong hồ sơ không gọi nguồn toàn viện trước lọc", () =>
            {
                SignIn("CV002"); DuLieuGiaoDien.ChonPhamViLamSang(true);
                var view = new LichKham();
                var rows=(IEnumerable<ET_LICHKHAM_HIENTHI>)typeof(LichKham).GetField("danhSach",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(view)!;
                var allowed=DuLieuGiaoDien.Rows("LICH_KHAM").Select(r=>UiRecord.Text(r,"MaLich")).ToHashSet();
                Require(rows.All(r=>allowed.Contains(r.MaLich)),"Control lịch nhúng tải lịch ngoài phạm vi dù tab bị ẩn.");
            });
            Check("Lịch khám độc lập giữ nguồn và thao tác cũ", () =>
            {
                SignIn("CV001"); DuLieuGiaoDien.ChonPhamViLamSang(false);
                var view=new LichKham();
                var rows=(IEnumerable<ET_LICHKHAM_HIENTHI>)typeof(LichKham).GetField("danhSach",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(view)!;
                using var db=new DAL_DONTHUOC().MoKetNoi();
                Require(rows.Count()==db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.LICH_KHAM"),"Màn hình lịch độc lập bị áp bộ lọc lâm sàng.");
                Require(Find<Button>(view,"AddAppointmentButton").IsEnabled,"Màn hình lịch độc lập mất thao tác cũ.");
            });
            Check("Dược sĩ có view đơn chỉ đọc, không ký hoặc tạo đơn", () =>
            {
                SignIn("CV005"); DuLieuGiaoDien.ChonPhamViLamSang(true);
                var record = DuLieuGiaoDien.Rows("DON_THUOC").First();
                var view = new ChiTietDonThuoc(record); view.ConfigureAccess(_ => true);
                foreach (var name in new[] { "EditButton", "ConfirmButton", "CancelButton", "NewButton" }) Require(!Find<Button>(view, name).IsEnabled, "Dược sĩ được sửa đơn: " + name);
            });
            Check("Gọi ghi trực tiếp qua cache vẫn bị DAL từ chối và rollback", () =>
            {
                SignIn("CV002"); DuLieuGiaoDien.ChonPhamViLamSang(true);
                using var db = new DAL_DONTHUOC().MoKetNoi();
                var count = db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.BENH_NHAN");
                var rejected = false;
                try { DuLieuGiaoDien.Upsert("BENH_NHAN", new Dictionary<string,object?> { ["HoTenBN"]="FIXTURE KHÔNG ĐƯỢC LƯU",["TrangThai"]="Đang theo dõi" }); }
                catch (TuChoiQuyenNghiepVuException) { rejected = true; }
                Require(rejected && db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.BENH_NHAN") == count, "Bỏ nút UI có thể tạo BN ngoài quyền.");
                var order = db.QueryFirst<ET_DONTHUOC>("SELECT TOP(1) * FROM dbo.DON_THUOC WHERE MaBS<>@id ORDER BY MaDT", new { id=PhienDangNhap.MaNV });
                var oldState=order.TrangThai;
                rejected=false;
                try { DuLieuGiaoDien.SetValue("DON_THUOC",DuLieuGiaoDien.Values(order),"TrangThai","Hủy"); }
                catch (TuChoiQuyenNghiepVuException) { rejected=true; }
                Require(rejected && db.ExecuteScalar<string>("SELECT TrangThai FROM dbo.DON_THUOC WHERE MaDT=@id",new{id=order.MaDT})==oldState,"Đổi trạng thái bỏ qua quyền bác sĩ của đơn.");
            });
            Check("Hóa đơn lịch sử chỉ đọc, không cấp hoặc thu lại qua nút cũ", () =>
            {
                SignIn("CV007"); DuLieuGiaoDien.ChonPhamViLamSang(true);
                var page = new HoaDon();
                using var host = new WindowLifetime(new Window { Content = page, Width = 1366, Height = 768, ShowInTaskbar = false });
                host.Window.Show(); host.Window.UpdateLayout();
                var list = Find<ListBox>(page, "InvoiceList");
                if (list.Items.Count > 0) list.SelectedIndex = 0;
                Require(!Find<Button>(page, "CreateButton").IsEnabled && !Find<Button>(page, "DispenseButton").IsEnabled, "Luồng ghi cũ còn bật.");
                Require(!Find<FrameworkElement>(page, "PaymentForm").IsEnabled, "Thu từng phần cũ vẫn dùng được.");
            });
        }
        finally { app.Shutdown(); }
        Console.WriteLine($"ClinicalUiChecks: {pass} PASS, {fail} FAIL; fixture tiếp nhận đã dọn, không giữ chứng từ SQL.");
        return fail == 0 ? 0 : 1;
    }

    private sealed class WindowLifetime(Window window) : IDisposable
    {
        public Window Window { get; } = window;
        public void Dispose() => Window.Close();
    }
    private static bool InvokeTry(UserControl view,string method)
    {
        var args=new object?[]{null};
        try{return (bool)view.GetType().GetMethod(method)!.Invoke(view,args)!;}
        catch(TargetInvocationException ex){throw ex.InnerException??ex;}
    }

    private static IEnumerable<TextBlock> TextBlocks(DependencyObject root)
    {
        if(root is TextBlock text)yield return text;
        for(var i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);i++)
            foreach(var child in TextBlocks(System.Windows.Media.VisualTreeHelper.GetChild(root,i)))yield return child;
    }

    private static void EditCell(DataGrid grid,int column,string text)
    {
        var row=grid.Items[0];grid.SelectedItem=row;grid.ScrollIntoView(row,grid.Columns[column]);grid.UpdateLayout();
        grid.CurrentCell=new DataGridCellInfo(row,grid.Columns[column]);grid.Focus();Require(grid.BeginEdit(),"Không mở được ô nhập thật.");grid.UpdateLayout();
        var editor=grid.Columns[column].GetCellContent(row) as TextBox ?? throw new Exception("Không thấy TextBox đang sửa.");editor.Text=text;
    }

    private static void SavePreview(FrameworkElement view,string name,string folder="obj/ClinicalWorkflowP04")
    {
        view.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.Render);view.UpdateLayout();
        var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(view.ActualWidth),(int)Math.Ceiling(view.ActualHeight),96,96,System.Windows.Media.PixelFormats.Pbgra32);
        bitmap.Render(view);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        System.IO.Directory.CreateDirectory(folder);
        using var file=System.IO.File.Create(System.IO.Path.Combine(folder,name));encoder.Save(file);
    }
}
