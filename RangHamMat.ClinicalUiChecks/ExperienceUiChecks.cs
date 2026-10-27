using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.ClinicalChecks;
using RangHamMat.Views;

namespace RangHamMat.ClinicalUiChecks;

internal static class ExperienceUiChecks
{
    private static void Need(bool value,string message) { if(!value)throw new InvalidOperationException(message); }
    private static T Find<T>(FrameworkElement node,string name) where T:FrameworkElement => (T)node.FindName(name);
    private static IEnumerable<DependencyObject> Nodes(DependencyObject node)
    {yield return node;foreach(var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())foreach(var item in Nodes(child))yield return item;}
    private static bool Leave(MainWindow window,MessageBoxResult choice)
    {
        var method=typeof(MainWindow).GetMethod("TryLeaveClinicalWork",BindingFlags.Public|BindingFlags.Instance)
            ??throw new InvalidOperationException("Khung ứng dụng chưa có chốt dữ liệu chưa lưu.");
        return (bool)method.Invoke(window,new object?[]{new Func<string,MessageBoxResult>(_=>choice)})!;
    }
    private static MainWindow Host(UserControl content)
    {
        var actor=PhienDangNhap.MaNV;
        MainWindow window;
        try { PhienDangNhap.MaNV="";window=new MainWindow(); }
        finally { PhienDangNhap.MaNV=actor; }
        typeof(MainWindow).GetProperty("LeaveDecision",BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(window,new Func<string,MessageBoxResult>(_=>MessageBoxResult.No));
        Find<ContentControl>(window,"AppHost").Content=new AppWorkspace(window,"FIXTURE P11","User",content);
        return window;
    }
    private static void SetPrivate(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(target,value);
    internal static int Run(bool includeLayout=true)
    {
        using(var db=new DAL.DAL_DONTHUOC().MoKetNoi())
            if(!db.Database.StartsWith("RHM_ClinicalWorkflow_Test_P10Handover_",StringComparison.Ordinal)&&!db.Database.StartsWith("RHM_ClinicalWorkflow_Test_P11Experience_",StringComparison.Ordinal)&&!db.Database.StartsWith("RHM_ClinicalWorkflow_Test_P09Payments_P12",StringComparison.Ordinal))
                throw new InvalidOperationException("Kiểm tra P11 có ca lưu thật cần DB P10Handover/P11Experience mới riêng.");
        var pass=0;var fail=0;
        void Check(string name,Action run){try{run();Console.WriteLine("PASS: "+name);pass++;}catch(Exception ex){Console.Error.WriteLine("FAIL: "+name+": "+ex.GetBaseException().Message);fail++;}}
        Check("P11 hủy rời ứng dụng giữ đúng nháp khám",()=>
        {
            using var f=new Fixture();var view=new KhamLamSang(f.Id("LSA"),f.User("A"),f.Transaction);var home=Host(view);
            Find<TextBox>(view,"SymptomsEditor").Text="FIXTURE chưa lưu";
            Need(!Leave(home,MessageBoxResult.Cancel),"Hủy vẫn rời trang");
            Need(view.CoThayDoiChuaLuu&&Find<TextBox>(view,"SymptomsEditor").Text=="FIXTURE chưa lưu","Mất nháp khi hủy");home.Close();
        });
        Check("P11 lưu trước rời chỉ lưu nháp, không ký",()=>
        {
            using var f=new Fixture();var view=new KhamLamSang(f.Id("LSA"),f.User("A"),f.Transaction);var home=Host(view);
            Find<TextBox>(view,"SymptomsEditor").Text="FIXTURE lưu trước rời";
            Need(Leave(home,MessageBoxResult.Yes),"Không lưu được trước rời");
            Need(!view.CoThayDoiChuaLuu&&f.Scalar<string>("SELECT TrieuChung FROM dbo.LICH_SU_KHAM_BENH WHERE MaLS=@id",new{id=f.Id("LSA")})=="FIXTURE lưu trước rời","Chưa lưu đúng nguồn");
            Need(f.Scalar<string>("SELECT TrangThai FROM dbo.LICH_SU_KHAM_BENH WHERE MaLS=@id",new{id=f.Id("LSA")})=="Nháp","Tự ký khi rời");home.Close();
        });
        Check("P11 bỏ thay đổi tải lại nguồn, không ghi SQL",()=>
        {
            using var f=new Fixture();var view=new KhamLamSang(f.Id("LSA"),f.User("A"),f.Transaction);var home=Host(view);
            var before=Find<TextBox>(view,"SymptomsEditor").Text;Find<TextBox>(view,"SymptomsEditor").Text="FIXTURE bỏ";
            Need(Leave(home,MessageBoxResult.No),"Không bỏ được nháp");
            Need(!view.CoThayDoiChuaLuu&&Find<TextBox>(view,"SymptomsEditor").Text==before,"Không khôi phục nguồn trước khi rời");home.Close();
        });
        Check("P11 lỗi nguồn đã đổi chặn rời và giữ nội dung",()=>
        {
            using var f=new Fixture();var actor=f.User("A");var view=new KhamLamSang(f.Id("LSA"),actor,f.Transaction);var home=Host(view);
            Find<TextBox>(view,"SymptomsEditor").Text="FIXTURE đang nhập";
            var bus=new BUS_KHAMDIEUTRI(f.Transaction);var saved=bus.LayBanNhap(f.Id("LSA"),actor);
            bus.LuuKham(new(){MaLS=f.Id("LSA"),DauVanTay=saved.DauVanTay,TrieuChung="FIXTURE nguồn vừa đổi",ChanDoan="FIXTURE",KeHoachXuLy="FIXTURE",TinhTrang=[]},actor);
            Need(!Leave(home,MessageBoxResult.Yes),"Lưu xung đột vẫn rời");Need(view.CoThayDoiChuaLuu&&Find<TextBox>(view,"SymptomsEditor").Text=="FIXTURE đang nhập","Mất nội dung sau lỗi");
            Leave(home,MessageBoxResult.No);home.Close();
        });
        Check("P11 yêu cầu thu chưa rõ kết quả không được bỏ hoặc rời",()=>
        {
            using var f=new Fixture();var view=new BangTinhPhiEditor(f.User("C"),f.Transaction);var home=Host(view);
            SetPrivate(view,"pendingPayment",new YeuCauThanhToan{MaLS=f.Id("LSA"),MaYeuCau=Guid.NewGuid(),DauVanTayBangTinh="FIXTURE"});
            Need(!Leave(home,MessageBoxResult.No)&&!Leave(home,MessageBoxResult.Yes),"Mất mã thử lại thu tiền");SetPrivate(view,"pendingPayment",null!);home.Close();
        });
        Check("P11 form người bệnh trong modal được bảo vệ khi Escape",()=>
        {
            using var f=new Fixture();f.User("R");
            var data=new BUS_KHAMDIEUTRI(f.Transaction).LayDuLieuHoSo(f.User("R"));SeedCache(data);
            var home=Host(new UserControl());var workspace=(AppWorkspace)Find<ContentControl>(home,"AppHost").Content;
            var form=new CuaSoBenhNhan();BieuMauHoSo.Configure(form,"BENH_NHAN");workspace.ShowModal(form);
            var field=Nodes(form).OfType<TextBox>().Single(t=>Equals(t.Tag,"DB:BENH_NHAN.HoTenBN"));field.Text="FIXTURE đang nhập";
            Need(!Leave(home,MessageBoxResult.Cancel),"Bỏ qua nháp modal");Need(field.Text=="FIXTURE đang nhập","Mất tên đang nhập");
            typeof(MainWindow).GetProperty("LeaveDecision",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(home,new Func<string,MessageBoxResult>(_=>MessageBoxResult.Cancel));
            var close=typeof(AppWorkspace).GetMethod("ReturnToPage",BindingFlags.Instance|BindingFlags.NonPublic)!;close.Invoke(workspace,null);
            Need(((Grid)workspace.FindName("ModalLayer")).Visibility==Visibility.Visible&&field.Text=="FIXTURE đang nhập","Escape/Đóng bỏ qua lựa chọn Hủy");
            typeof(MainWindow).GetProperty("LeaveDecision",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(home,new Func<string,MessageBoxResult>(_=>MessageBoxResult.No));close.Invoke(workspace,null);
            Need(((ContentControl)workspace.FindName("ModalContent")).Content is null,"Bỏ thay đổi chưa đóng modal");home.Close();
        });
        Check("P11 nút trang chủ/đổi module/đóng cửa sổ đều giữ nháp khi hủy",()=>
        {
            using var f=new Fixture();var view=new KhamLamSang(f.Id("LSA"),f.User("A"),f.Transaction);var home=Host(view);
            typeof(MainWindow).GetProperty("LeaveDecision",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(home,new Func<string,MessageBoxResult>(_=>MessageBoxResult.Cancel));
            Find<TextBox>(view,"SymptomsEditor").Text="FIXTURE giữ lại";var workspace=Find<ContentControl>(home,"AppHost").Content;
            typeof(MainWindow).GetMethod("ShowHome",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(home,null);
            Need(ReferenceEquals(Find<ContentControl>(home,"AppHost").Content,workspace),"Trang chủ bỏ qua chốt");
            typeof(MainWindow).GetMethod("OpenApp",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(home,["TongQuan"]);
            Need(ReferenceEquals(Find<ContentControl>(home,"AppHost").Content,workspace),"Đổi module bỏ qua chốt");
            home.Show();home.Close();Need(home.IsVisible&&view.CoThayDoiChuaLuu,"Đóng cửa sổ vẫn bỏ nháp");
            Leave(home,MessageBoxResult.No);home.Close();
        });
        Check("P11 cửa sổ bàn giao chưa rõ kết quả chặn rời dù chọn bỏ",()=>
        {
            using var db=new DAL.DAL_DONTHUOC().MoKetNoi();var account=Dapper.SqlMapper.QuerySingle<(string Employee,string Account)>(db,"SELECT TOP(1) n.MaNV Employee,t.MaTK Account FROM dbo.NHAN_VIEN n JOIN dbo.TAI_KHOAN t ON t.MaNV=n.MaNV WHERE n.MaChucVu=N'CV005' AND n.TrangThai=N'Đang làm việc' AND t.TrangThai=N'Đang hoạt động' ORDER BY n.MaNV");
            PhienDangNhap.MaNV=account.Employee;PhienDangNhap.KhoiTaoDinhDanhNghiepVu(account.Account);
            var view=new BanGiaoThuocEditor();var home=Host(view);SetPrivate(view,"pendingId","FIXTURE-DON");
            Need(!Leave(home,MessageBoxResult.No),"Mất mã đối chiếu bàn giao");SetPrivate(view,"pendingId",null!);home.Close();
        });
        Check("P11 nhập lượng sai hiện lỗi dưới trường và đưa focus về ô sai",()=>
        {
            using var f=new Fixture();var view=new ThucHienDichVuEditor(f.User("A"),f.Transaction);view.FocusContext(f.Id("BN1"),f.Id("LSA"));
            var host=new Window{Content=view,Width=1024,Height=768,ShowInTaskbar=false};host.Show();host.UpdateLayout();
            try
            {
                var amount=Find<TextBox>(view,"QuantityEditor");amount.Text="không phải số";
                Need(!view.TrySave(out _),"Số sai vẫn lưu");host.UpdateLayout();
                Need(amount.IsKeyboardFocusWithin,"Không chuyển focus về ô lượng sai");
                Need(Nodes(view).OfType<TextBlock>().Any(t=>Equals(t.Tag,"ClinicalFieldError")&&t.Text.Contains("Nhập số hợp lệ")),"Thiếu thông báo ngay dưới trường");
                Need(amount.Text=="không phải số"&&view.CoThayDoiChuaLuu,"Mất dữ liệu nhập sau lỗi");
            }
            finally{host.Close();}
        });
        Check("P11 hủy đổi phiên giữ định danh cũ và nội dung đang nhập",()=>
        {
            using var f=new Fixture();var view=new KhamLamSang(f.Id("LSA"),f.User("A"),f.Transaction);var home=Host(view);
            typeof(MainWindow).GetProperty("LeaveDecision",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(home,new Func<string,MessageBoxResult>(_=>MessageBoxResult.Cancel));
            Find<TextBox>(view,"SymptomsEditor").Text="FIXTURE giữ phiên cũ";
            try
            {
                var rejected=false;try{PhienDangNhap.MaNV=f.Id("B");PhienDangNhap.KhoiTaoDinhDanhNghiepVu(f.Id("TKB"));}catch(InvalidOperationException){rejected=true;}
                Need(rejected&&PhienDangNhap.MaNV==f.Id("A"),"Đổi phiên bỏ qua lựa chọn Hủy hoặc không khôi phục MaNV");
                Need(view.CoThayDoiChuaLuu&&Find<TextBox>(view,"SymptomsEditor").Text=="FIXTURE giữ phiên cũ","Mất nội dung sau hủy đổi phiên");
            }
            finally{Find<ContentControl>(home,"AppHost").Content=null;f.ActAs("A");home.Close();}
        });
        Check("P11 đổi phiên sau bỏ nháp xóa màn và cửa sổ phiên cũ",()=>
        {
            using var f=new Fixture();var view=new KhamLamSang(f.Id("LSA"),f.User("A"),f.Transaction);var home=Host(view);home.Show();
            var child=new Window{Owner=home,Content=new TextBlock{Text="FIXTURE phiên cũ"},ShowInTaskbar=false};child.Show();
            Find<TextBox>(view,"SymptomsEditor").Text="FIXTURE bỏ trước đổi phiên";
            try
            {
                PhienDangNhap.MaNV=f.Id("B");PhienDangNhap.KhoiTaoDinhDanhNghiepVu(f.Id("TKB"));
                Need(Find<ContentControl>(home,"AppHost").Content is null&&!child.IsVisible,"Phiên mới còn xem màn/cửa sổ phiên cũ");
            }
            finally{Find<ContentControl>(home,"AppHost").Content=null;child.Close();home.Close();}
        });
        Check("P11 làm mới cùng định danh không bỏ nháp hoặc thay màn",()=>
        {
            using var f=new Fixture();var view=new KhamLamSang(f.Id("LSA"),f.User("A"),f.Transaction);var home=Host(view);var workspace=Find<ContentControl>(home,"AppHost").Content;
            Find<TextBox>(view,"SymptomsEditor").Text="FIXTURE cùng phiên";
            try{f.ActAs("A");Need(ReferenceEquals(workspace,Find<ContentControl>(home,"AppHost").Content)&&view.CoThayDoiChuaLuu,"Làm mới cùng phiên làm mất nháp");}
            finally{Find<ContentControl>(home,"AppHost").Content=null;home.Close();}
        });
        Check("P11 đã nhập tiền nhưng chưa gửi không tự lưu/thu khi rời",()=>
        {
            using var source=LayoutUiChecks.Create("BangTinh");var view=(BangTinhPhiEditor)source.View;var home=Host(view);Find<TextBox>(view,"GivenText").Text="500000";
            try
            {
                Need(!Leave(home,MessageBoxResult.Cancel),"Bỏ qua tiền đang nhập");Need(!Leave(home,MessageBoxResult.Yes),"Lưu trước rời đã tự gửi thu hoặc bỏ dữ liệu");
                Need(view.KetQua is null&&Find<TextBox>(view,"GivenText").Text=="500000","Tự thu hoặc mất đầu vào");Need(Leave(home,MessageBoxResult.No),"Không bỏ được đầu vào chưa gửi");
            }
            finally{Find<ContentControl>(home,"AppHost").Content=null;home.Close();}
        });
        Check("P11 lỗi kết nối khi xem phí có thông báo dễ hiểu và không báo thành công",()=>
        {
            var old=Environment.GetEnvironmentVariable("RHM_CONNECTION_STRING");
            using var db=new DAL.DAL_DONTHUOC().MoKetNoi();var account=Dapper.SqlMapper.QuerySingle<(string Employee,string Account)>(db,"SELECT TOP(1) n.MaNV Employee,t.MaTK Account FROM dbo.NHAN_VIEN n JOIN dbo.TAI_KHOAN t ON t.MaNV=n.MaNV WHERE n.MaChucVu=N'CV007' AND n.TrangThai=N'Đang làm việc' AND t.TrangThai=N'Đang hoạt động' ORDER BY n.MaNV");
            PhienDangNhap.MaNV=account.Employee;PhienDangNhap.KhoiTaoDinhDanhNghiepVu(account.Account);var view=new BangTinhPhiEditor();view.FocusContext("FIXTURE-KHONG-GHI");
            try
            {
                Environment.SetEnvironmentVariable("RHM_CONNECTION_STRING","Data Source=127.0.0.1,1;Initial Catalog=RHM_ClinicalWorkflow_Test_Unavailable;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=1;");
                Need(!view.TryLapBangTinh(out var error)&&view.BangTinh is null,"Lỗi SQL vẫn báo có bảng tính");
                Need(error is not null&&!error.Contains("provider",StringComparison.OrdinalIgnoreCase)&&!error.Contains("SQL Server",StringComparison.OrdinalIgnoreCase)&&error.Contains("kết nối",StringComparison.OrdinalIgnoreCase),"Hiện lỗi kết nối kỹ thuật thô");
            }
            finally{Environment.SetEnvironmentVariable("RHM_CONNECTION_STRING",old);}
        });
        Check("P11 lỗi cuối transaction lưu người bệnh giữ nháp và rollback SQL",()=>
        {
            using var source=LayoutUiChecks.Create("FormBenhNhan");var form=source.View;var home=Host(form);
            var field=Nodes(form).OfType<TextBox>().Single(t=>Equals(t.Tag,"DB:BENH_NHAN.HoTenBN"));field.Text="FIXTURE lỗi sau lưu trước commit";
            using var db=new DAL.DAL_DONTHUOC().MoKetNoi();var before=Dapper.SqlMapper.ExecuteScalar<int>(db,"SELECT COUNT(*) FROM dbo.BENH_NHAN");
            try
            {
                var failed=false;
                try{DuLieuGiaoDien.Transaction(()=>{Need(BieuMauHoSo.TrySave(form,out _,out var error),error);throw new InvalidOperationException("FIXTURE sau lưu trước commit");});}
                catch(InvalidOperationException){failed=true;}
                Need(failed&&before==Dapper.SqlMapper.ExecuteScalar<int>(db,"SELECT COUNT(*) FROM dbo.BENH_NHAN"),"Không rollback lưu người bệnh");
                Need(!Leave(home,MessageBoxResult.Cancel)&&field.Text=="FIXTURE lỗi sau lưu trước commit","Lỗi transaction đã đánh dấu hết nháp");
            }
            finally{Find<ContentControl>(home,"AppHost").Content=null;home.Close();}
        });
        ExperienceReviewChecks.AddCases(Check,Host);
        ExperienceReachabilityChecks.AddCases(Check,Host);
        if(includeLayout)LayoutUiChecks.AddCases(Check);
        Console.WriteLine($"ExperienceUiChecks: {pass} PASS, {fail} FAIL; ca lưu thật chỉ trên DB riêng.");return fail==0?0:1;
    }
    internal static void SeedCache(DuLieuHoSoTheoQuyen data)
    {
        var type=typeof(DuLieuGiaoDien);type.GetField("<PhamViLamSang>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,true);
        type.GetField("clinicalUser",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,data.NguoiDung);
        var tables=(System.Collections.IDictionary)type.GetField("tables",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;tables.Clear();
        // Cache dùng ObservableCollection<object>, giữ đúng cấu trúc thật.
        foreach(var entry in data.CacBang)tables[entry.Key]=new System.Collections.ObjectModel.ObservableCollection<object>(entry.Value);
        type.GetField("loaded",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,true);
    }
}
