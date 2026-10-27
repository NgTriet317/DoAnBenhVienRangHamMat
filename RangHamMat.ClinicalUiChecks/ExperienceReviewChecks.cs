using System.Data;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Dapper;
using DAL;
using RangHamMat.Controls;
using RangHamMat.ClinicalChecks;
using RangHamMat.Views;
namespace RangHamMat.ClinicalUiChecks;
internal static class ExperienceReviewChecks
{
    private static void Need(bool ok,string text){if(!ok)throw new InvalidOperationException(text);}
    private static T Find<T>(FrameworkElement root,string name)where T:FrameworkElement=>(T)root.FindName(name);
    private static bool Leave(MainWindow home,MessageBoxResult choice)=>(bool)typeof(MainWindow).GetMethod("TryLeaveClinicalWork")!.Invoke(home,[new Func<string,MessageBoxResult>(_=>choice)])!;
    private static void Decide(MainWindow home,MessageBoxResult value)=>typeof(MainWindow).GetProperty("LeaveDecision",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(home,new Func<string,MessageBoxResult>(_=>value));
    private static IEnumerable<DependencyObject> Nodes(DependencyObject root){yield return root;foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())foreach(var item in Nodes(child))yield return item;}
    internal static void AddCases(Action<string,Action> check,Func<UserControl,MainWindow> host)
    {
        check("P11 review cửa sổ in đóng khi đổi phiên sau về trang chủ",()=>
        {
            using var f=new Fixture();var v=new KhamLamSang(f.Id("LSA"),f.User("A"),f.Transaction);var home=host(v);home.Show();
            var headers=new DataTable();foreach(var key in new[]{"MaHD","TrangThai","HoTenBN","MaBN","MaLS","NguoiLap"})headers.Columns.Add(key);headers.Columns.Add("NgayLap",typeof(DateTime));foreach(var key in new[]{"TongTien","GiamGia","SoTienPhaiThu","DaThu","ConPhaiThu"})headers.Columns.Add(key,typeof(decimal));headers.Rows.Add("FIXTURE-HD","Đã xác nhận","FIXTURE",f.Id("BN1"),f.Id("LSA"),"FIXTURE",DateTime.Today,0m,0m,0m,0m,0m);
            var print=new XemTruocHoaDon(headers.Rows[0],new DataTable()){Owner=home};print.Show();
            try
            {
                typeof(MainWindow).GetMethod("ShowHome",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(home,null);
                Need(Find<ContentControl>(home,"AppHost").Content is null,"Không về trang chủ");f.ActAs("B");
                Need(!print.IsVisible,"Phiên mới còn xem bản in của phiên cũ sau Home");
            }
            finally{Find<ContentControl>(home,"AppHost").Content=null;print.Close();home.Close();}
        });
        check("P11 review nháp trong cửa sổ con chặn đóng con và cửa sổ chính",()=>
        {
            using var f=new Fixture();var actor=f.User("A");var home=host(new UserControl());home.Show();var editor=new HuongDanThuocEditor(actor,f.Transaction);var child=new Window{Owner=home,Content=editor,Width=900,Height=650,ShowInTaskbar=false};child.Show();
            Find<TextBox>(editor,"DoseEditor").Text="FIXTURE nháp cửa sổ con";Decide(home,MessageBoxResult.Cancel);
            try
            {
                child.Close();Need(child.IsVisible&&editor.CoThayDoiChuaLuu,"Đóng cửa sổ con đã mất nháp");home.Close();Need(home.IsVisible&&child.IsVisible,"Đóng Main bỏ qua nháp cửa sổ con");
            }
            finally{child.Content=null;Decide(home,MessageBoxResult.No);child.Close();Find<ContentControl>(home,"AppHost").Content=null;home.Close();}
        });
        foreach(var editExisting in new[]{false,true})
        check("P11 review form lưu xong bị giữ lại có thể sửa/lưu lần hai "+(editExisting?"bản có sẵn":"bản mới"),()=>
        {
            using var source=LayoutUiChecks.Create("FormBenhNhan");var form=source.View;var unique="FIXTURE P11 REBASE "+Guid.NewGuid().ToString("N");var field=Nodes(form).OfType<TextBox>().Single(t=>Equals(t.Tag,"DB:BENH_NHAN.HoTenBN"));field.Text=unique+" 1";var home=host(form);
            try
            {
                Need(Leave(home,MessageBoxResult.Yes),"Không lưu form ban đầu");var first=BieuMauHoSo.GetSaved(form)!;var id=UiRecord.Text(first,"MaBN");
                if(editExisting)
                {
                    form=new CuaSoBenhNhan();BieuMauHoSo.Configure(form,"BENH_NHAN",DuLieuGiaoDien.Find("BENH_NHAN",id));Find<ContentControl>((AppWorkspace)Find<ContentControl>(home,"AppHost").Content,"Workspace").Content=form;
                    field=Nodes(form).OfType<TextBox>().Single(t=>Equals(t.Tag,"DB:BENH_NHAN.HoTenBN"));field.Text=unique+" 2";Need(Leave(home,MessageBoxResult.Yes),"Không lưu lần sửa đầu");
                }
                field.Text=unique+" 3";Need(Leave(home,MessageBoxResult.Yes),"Form còn giữ snapshot trước lần lưu của chính mình");
                Need(UiRecord.Text(BieuMauHoSo.GetSaved(form)!,"MaBN")==id,"Lưu lại tạo bệnh nhân thứ hai");
                using var db=new DAL_DONTHUOC().MoKetNoi();Need(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.BENH_NHAN WHERE HoTenBN LIKE @q",new{q=unique+"%"})==1,"Có bản trùng sau lưu hai lần");
                var key=Nodes(form).OfType<TextBox>().Single(t=>Equals(t.Tag,"DB:BENH_NHAN.MaBN"));Need(key.Text==id,"Không cập nhật mã bản đã lưu trên form");
            }
            finally{Find<ContentControl>(home,"AppHost").Content=null;home.Close();}
        });
        foreach(var action in new[]{"lượt","kho","xem lại"})check("P11 review hủy đổi "+action+" giữ tiền và nguồn trong màn thu",()=>
        {
            using var source=LayoutUiChecks.Create("BangTinh");var pricing=(BangTinhPhiEditor)source.View;var home=host(pricing);Decide(home,MessageBoxResult.Cancel);var given=Find<TextBox>(pricing,"GivenText");given.Text="500000";var oldVisit=Find<TextBlock>(pricing,"VisitText").Text;var warehouse=Find<ComboBox>(pricing,"WarehouseFilter");var oldWarehouse=warehouse.SelectedValue;
            try
            {
                if(action=="lượt")pricing.FocusContext("FIXTURE-khac");else if(action=="kho")warehouse.SelectedIndex=warehouse.Items.Count>1?1:0;else pricing.TryLapBangTinh(out _);
                Need(given.Text=="500000"&&Find<TextBlock>(pricing,"VisitText").Text==oldVisit&&Equals(warehouse.SelectedValue,oldWarehouse),"Đổi nguồn nội bộ xóa đầu vào dù chọn Hủy");Need(pricing.KetQua is null,"Đổi nguồn tự gửi thu");
            }
            finally{Find<ContentControl>(home,"AppHost").Content=null;home.Close();}
        });
        check("P11 review đơn thiếu liều đưa focus về dòng thuốc sai",()=>
        {
            using var f=new Fixture();var view=new KeDonThuocEditor(f.User("A"),f.Transaction);view.FocusContext(f.Id("BN1"),f.Id("LSA"),f.Id("DTA"));var window=new Window{Content=view,Width=1000,Height=720,ShowInTaskbar=false};window.Show();window.UpdateLayout();
            try
            {
                var grid=Find<DataGrid>(view,"RxLines");grid.SelectedIndex=0;var row=grid.Items[0];var column=grid.Columns.OfType<DataGridTextColumn>().Single(c=>c.Binding is System.Windows.Data.Binding b&&b.Path.Path=="LieuDung");grid.CurrentCell=new DataGridCellInfo(row,column);grid.ScrollIntoView(row,column);window.UpdateLayout();Need(grid.BeginEdit(),"Không bắt đầu được ô liều dùng");window.UpdateLayout();var cell=column.GetCellContent(row) as TextBox??throw new InvalidOperationException("Không tạo editor liều dùng thật");cell.Text="";Find<TextBox>(view,"NoteEditor").Focus();
                Need(!view.TrySave(out _),"Đơn thiếu liều vẫn lưu");Need(grid.IsKeyboardFocusWithin,"Không đưa focus về dòng thuốc lỗi");Need(view.CoThayDoiChuaLuu,"Mất nháp đơn sau lỗi");
            }
            finally{window.Close();}
        });
        check("P11 review mất kết nối tiếp nhận không hiện lỗi SQL thô",()=>
        {
            var old=Environment.GetEnvironmentVariable("RHM_CONNECTION_STRING");using var source=LayoutUiChecks.Create("TiepNhan");var view=(TiepNhanKham)source.View;var window=new Window{Content=view,Width=850,Height=720,ShowInTaskbar=false};window.Show();window.UpdateLayout();
            try
            {
                Find<ComboBox>(view,"ServicePicker").SelectedIndex=0;var doctor=Find<ComboBox>(view,"DoctorPicker");Need(doctor.Items.Count>0,"Fixture thiếu bác sĩ");doctor.SelectedIndex=0;Find<ComboBox>(view,"RoomPicker").SelectedIndex=0;Find<TextBox>(view,"DispatchReason").Text="FIXTURE";Find<TextBox>(view,"ArrivalReason").Text="FIXTURE";
                Environment.SetEnvironmentVariable("RHM_CONNECTION_STRING","Data Source=127.0.0.1,1;Initial Catalog=RHM_ClinicalWorkflow_Test_Unavailable;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=1;");
                Need(!view.TryReceive(out var error),"Mất kết nối vẫn tiếp nhận thành công");Need(!error.Contains("SQL Server",StringComparison.OrdinalIgnoreCase)&&!error.Contains("provider",StringComparison.OrdinalIgnoreCase),"Tiếp nhận hiện lỗi SQL thô");
            }
            finally{Environment.SetEnvironmentVariable("RHM_CONNECTION_STRING",old);window.Close();}
        });
    }
}
