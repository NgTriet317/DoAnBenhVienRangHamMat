using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BUS;
using DAL;
using Dapper;
using ET;
using RangHamMat.Controls;
using RangHamMat.ClinicalChecks;
using RangHamMat.Views;
namespace RangHamMat.ClinicalUiChecks;
internal static class ExperienceReachabilityChecks
{
    private static void Need(bool ok,string error){if(!ok)throw new InvalidOperationException(error);}
    private static T Find<T>(FrameworkElement root,string name)where T:FrameworkElement=>(T)(root.FindName(name)??throw new InvalidOperationException("Thiếu điều khiển "+name));
    private sealed class Data
    {
        internal string Prefix="",Rx="";
        internal string Id(string suffix)=>Prefix+suffix;
        internal string Patient=>Id("OB70");internal string Visit=>Id("OL70");internal string UnpaidVisit=>Id("OL69");internal string Today=>Id("OA1");
        internal void ActAs(string suffix)
        {PhienDangNhap.MaNV=Id(suffix);PhienDangNhap.IsAdmin=false;PhienDangNhap.KhoiTaoDinhDanhNghiepVu(Id("TK"+suffix));DuLieuGiaoDien.ChonPhamViLamSang(true);}
    }
    private static Data? seeded;
    private static Data Dataset()
    {
        if(seeded is not null)return seeded;
        using var f=new Fixture();var data=new Data{Prefix=f.Prefix};
        f.Execute("""
            DECLARE @now datetime2(0)=DATEADD(minute,-30,SYSDATETIME()),@room nvarchar(50)=(SELECT TOP(1) MaPhong FROM dbo.PHONG ORDER BY MaPhong);
            SELECT TOP(151) CONVERT(int,ROW_NUMBER() OVER(ORDER BY a.object_id)) n INTO #P11Reach FROM sys.all_objects a;
            INSERT dbo.BENH_NHAN(MaBN,HoTenBN,TrangThai) SELECT @prefix+'OB'+CAST(n as nvarchar(5)),N'FIXTURE P11 OUTSIDE '+CAST(n as nvarchar(5)),N'Đang theo dõi' FROM #P11Reach WHERE n<=70;
            INSERT dbo.HO_SO_BENH_AN(MaHS,MaBN,NgayLap,LyDoKhamBanDau,TrangThai) SELECT @prefix+'OH'+CAST(n as nvarchar(5)),@prefix+'OB'+CAST(n as nvarchar(5)),DATEADD(day,-1,@now),N'FIXTURE',N'Đang theo dõi' FROM #P11Reach WHERE n<=70;
            INSERT dbo.PHIEU_KHAM_BENH(MaPhieu,MaHS,MaPhong,MaBS,MaNVTiepNhan,SoThuTu,ThoiGianTiepNhan,LyDoKham,TrangThai) SELECT @prefix+'OP'+CAST(n as nvarchar(5)),@prefix+'OH'+CAST((n-1)%70+1 as nvarchar(5)),@room,@prefix+'A',@prefix+'R',20000+n,@now,N'FIXTURE',N'Đang khám' FROM #P11Reach WHERE n<=110;
            INSERT dbo.LICH_SU_KHAM_BENH(MaLS,MaPhieu,MaBS,MaPhong,ThoiGianKham,TrieuChung,ChanDoan,KeHoachXuLy,TrangThai) SELECT @prefix+'OL'+CAST(n as nvarchar(5)),@prefix+'OP'+CAST(n as nvarchar(5)),@prefix+'A',@room,DATEADD(minute,1,@now),N'FIXTURE',N'FIXTURE',N'FIXTURE',N'Nháp' FROM #P11Reach WHERE n<=110;
            DROP TABLE #P11Reach;
            """,new{prefix=f.Prefix});
        var actor=f.User("A");var clinic=new BUS_KHAMDIEUTRI(f.Transaction);var drugs=new BUS_DONTHUOC(f.Transaction);var services=new BUS_DICHVUTHUCHIEN(f.Transaction);
        for(var i=1;i<=110;i++)
        {
            var id=data.Id("OL"+i);
            if(i==70)
            {var rx=drugs.LuuDon(new(){MaLS=id,CacDong=[new(){MaThuoc=f.Scalar<string>("SELECT TOP(1) MaThuoc FROM dbo.THUOC ORDER BY MaThuoc"),LieuDung="FIXTURE liều chốt",DuongDung="FIXTURE",HuongDan="FIXTURE hướng dẫn chốt",SoNgayDung=1,SoLuong=1}]},actor);rx=drugs.XacNhanDon(rx.Don.MaDT,rx.DauVanTay,actor);data.Rx=rx.Don.MaDT;}
            if(i==69)
            {var actual=services.LuuThucHien(new(){MaLS=id,MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE TrangThai=N'Đang hoạt động' ORDER BY MaDV"),MaNVThucHien=actor.MaNV,ThoiGianThucHien=f.Scalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())"),SoLuong=1,DonGia=0,LyDoPhatSinh="FIXTURE",KetQua="FIXTURE"},actor);services.XacNhanThucHien(actual.DichVu.MaDVThucHien,actual.DauVanTay,actor);}
            var snapshot=clinic.LayBanNhap(id,actor);snapshot=clinic.LuuKham(new(){MaLS=id,DauVanTay=snapshot.DauVanTay,TrieuChung="FIXTURE",ChanDoan="FIXTURE",KeHoachXuLy="FIXTURE",KhongKeThuoc=i!=70,LyDoKhongKeThuoc=i!=70?"FIXTURE":null,TinhTrang=[new(){LoaiTinhTrang="FIXTURE",MoTaTinhTrang="FIXTURE",MucDo="FIXTURE"}]},actor);clinic.HoanTatKham(id,snapshot.DauVanTay,actor);
        }
        f.Execute("""
            INSERT dbo.HOA_DON(MaHD,MaLS,MaNV,NgayLap,TongTien,GiamGia,TrangThai) VALUES(@prefix+'OHD',@prefix+'OL70',@prefix+'A',SYSDATETIME(),0,0,N'Nháp');
            DECLARE @room nvarchar(50)=(SELECT TOP(1) MaPhong FROM dbo.PHONG ORDER BY MaPhong),@shift nvarchar(50)=(SELECT TOP(1) MaCa FROM dbo.CA_LAM ORDER BY MaCa),@dv nvarchar(50)=(SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE MaCK=@ck AND TrangThai=N'Đang hoạt động' ORDER BY MaDV),@day datetime2(0)=CONVERT(date,SYSDATETIME());
            SELECT TOP(151) CONVERT(int,ROW_NUMBER() OVER(ORDER BY a.object_id)) n INTO #P11App FROM sys.all_objects a;
            INSERT dbo.LICH_KHAM(MaLich,MaBN,MaBS,MaCa,MaPhong,MaDV,NgayGioKham,ThoiLuongDuKien,UuTien,LyDoKham,TrangThai,NgayTao)
            SELECT @prefix+'OA'+CAST(n as nvarchar(5)),@prefix+'OB70',@prefix+'A',@shift,@room,@dv,DATEADD(hour,10,DATEADD(day,n-1,@day)),30,N'Bình thường',N'FIXTURE',N'Nháp',DATEADD(day,-1,@day) FROM #P11App;
            DROP TABLE #P11App;
            """,new{prefix=f.Prefix,ck=actor.MaCK});
        f.Transaction.Commit();seeded=data;return data;
    }
    internal static void AddCases(Action<string,Action> check,Func<UserControl,MainWindow> host)
    {
        check("P11 review quầy thu tìm lượt ngoài 50 BN/100 lượt đầu",()=>
        {
            var data=Dataset();data.ActAs("C");Need(DuLieuGiaoDien.Find("BENH_NHAN",data.Id("OB69")) is null,"Fixture chưa vượt cache đầu");var view=new HoaDon();var home=host(view);Find<ContentControl>(home,"AppHost").Visibility=Visibility.Visible;Find<Grid>(home,"HomeSurface").Visibility=Visibility.Collapsed;home.Show();home.UpdateLayout();
            try
            {
                Find<TextBox>(view,"FinanceSearch").Text=data.UnpaidVisit;Find<Button>(view,"FinanceSearchButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var picker=Find<ComboBox>(view,"VisitFilter");Need(picker.Items.OfType<LuaChonDanhMuc>().Any(c=>c.Value==data.UnpaidVisit),"Không tìm được lượt ngoài cache");picker.SelectedValue=data.UnpaidVisit;
                Need(view.PatientId==data.Id("OB69"),"Chọn lượt ngoài cache giữ nhầm bệnh nhân");
                var price=(BangTinhPhiEditor)Find<ContentControl>(view,"PricingHost").Content;Need(price.TryLapBangTinh(out var error),error??"Không xem được phí đúng lượt ngoài cache");
            }
            finally{Find<ContentControl>(home,"AppHost").Content=null;home.Close();}
        });
        check("P11 review lịch sử đơn tìm và mở đơn ngoài snapshot đầu",()=>
        {
            var data=Dataset();data.ActAs("R");Need(DuLieuGiaoDien.Find("DON_THUOC",data.Rx) is null,"Fixture đơn chưa ngoài cache");
            var view=(DonThuoc)typeof(DonThuoc).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,[typeof(bool)],null)!.Invoke([true]);var window=new Window{Content=view,Width=1100,Height=750,ShowInTaskbar=false};window.Show();window.UpdateLayout();
            try
            {
                Find<TextBox>(view,"SearchBox").Text=data.Rx;var list=Find<ListBox>(view,"PrescriptionList");var row=list.Items.OfType<RxCard>().SingleOrDefault(c=>c.Code==data.Rx);
                Need(row is not null&&row.PatientId==data.Patient&&row.Lines.Length==1,"Không tìm được đơn/dòng/đúng BN ngoài cache");list.SelectedItem=row;window.UpdateLayout();
                Need(Find<ContentControl>(view,"DetailHost").Content is ChiTietDonThuoc details&&details.OrderId==data.Rx,"Không mở được chi tiết đơn ngoài cache");
            }
            finally{window.Close();}
        });
        check("P11 review lịch hẹn lịch sử và hẹn hôm nay không mất sau 100 hẹn",()=>
        {
            var data=Dataset();data.ActAs("R");typeof(DuLieuGiaoDien).GetMethod("BaoDamNguCanh",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[new NguCanhHoSo{MaBN=data.Patient}]);
            var appointments=new LichKham();appointments.SetPatient(data.Patient);Find<DatePicker>(appointments,"dpNgayKham").SelectedDate=DateTime.Today;Find<TextBox>(appointments,"txtTimKiem").Text=data.Today;
            Need(Find<ListBox>(appointments,"lstLichKham").Items.Count==1,"Lọc mã lịch cũ/hôm nay không lấy lại được lịch bị cap100");
            var view=new TiepNhanKham();BieuMauHoSo.Configure(view,"PHIEU_KHAM_BENH",patient:data.Patient);var window=new Window{Content=view,Width=850,Height=720,ShowInTaskbar=false};window.Show();window.UpdateLayout();
            try{Need(Find<ComboBox>(view,"AppointmentPicker").Items.OfType<LuaChonDanhMuc>().Any(c=>c.Value==data.Today),"Hẹn hôm nay bị hẹn tương lai đẩy khỏi tiếp nhận");}
            finally{window.Close();}
        });
        check("P11 review điều hướng hóa đơn hợp lệ không phụ thuộc có sẵn trong cache",()=>
        {
            var data=Dataset();data.ActAs("R");Need(DuLieuGiaoDien.Find("BENH_NHAN",data.Patient) is null,"Fixture nguồn còn trong cache");var home=host(new UserControl());home.Show();var previous=Find<ContentControl>(home,"AppHost").Content;using var dialogs=new UnexpectedDialog();
            try
            {
                ((AppWorkspace)previous).DieuHuongHoSo("KhamDieuTri",data.Patient,data.Visit,data.Rx,data.Id("OH70"),data.Id("OP70"),data.Id("OHD"));
                Need(!dialogs.Shown&&!ReferenceEquals(previous,Find<ContentControl>(home,"AppHost").Content),"Nguồn SQL hợp lệ ngoài cache bị từ chối điều hướng");
            }
            finally{Find<ContentControl>(home,"AppHost").Content=null;home.Close();}
        });
    }
    private sealed class UnexpectedDialog:IDisposable
    {
        private readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(20)};
        internal bool Shown;
        internal UnexpectedDialog(){timer.Tick+=(_,_)=>EnumThreadWindows(GetCurrentThreadId(),(h,_)=>{var name=new StringBuilder(40);GetClassName(h,name,name.Capacity);if(name.ToString()=="#32770"){Shown=true;SendMessage(h,0x0010,IntPtr.Zero,IntPtr.Zero);}return true;},IntPtr.Zero);timer.Start();}
        public void Dispose()=>timer.Stop();
        private delegate bool EnumWindow(IntPtr handle,IntPtr data);
        [DllImport("kernel32.dll")]private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")]private static extern bool EnumThreadWindows(uint thread,EnumWindow callback,IntPtr data);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetClassName(IntPtr handle,StringBuilder name,int count);
        [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr wParam,IntPtr lParam);
    }
}
