using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using BUS;
using ET;
using Microsoft.Win32;
using RangHamMat.Controls;

namespace RangHamMat.Views;

public partial class AnhXquangLanKham : UserControl
{
    private readonly string visitId;
    private readonly NguCanhNghiepVu actor;
    private readonly BUS_ANHXQUANG bus;
    private readonly CauHinhKhoAnh? config;
    private DanhSachAnhXquang? data;
    private bool readOnly,busy,loading,storeReady;
    public AnhXquangLanKham(string maLS,NguCanhNghiepVu user,IDbTransaction? transaction=null,CauHinhKhoAnh? cauHinh=null)
    {
        visitId=maLS;actor=user;config=cauHinh;bus=new(transaction,cauHinh);InitializeComponent();TaiLai();
    }
    public void RestrictToHistory(){readOnly=true;UpdateButtons();}
    private void UpdateButtons()
    {
        if(UploadButton is null)return;
        UploadButton.IsEnabled=UploadFields.IsEnabled=!busy&&!readOnly&&storeReady&&data?.DuocTaiLen==true;
        ViewButton.IsEnabled=!busy&&storeReady&&ImageList.SelectedItem is AnhXquangHienThi v&&v.Anh.TrangThai=="Đang sử dụng";
        HideButton.IsEnabled=!busy&&!readOnly&&ImageList.SelectedItem is AnhXquangHienThi h&&h.DuocAn;
        AssignmentPanel.Visibility=data?.DuocPhanCong==true&&!readOnly?Visibility.Visible:Visibility.Collapsed;
        AssignButton.IsEnabled=!busy&&!readOnly&&data?.DuocPhanCong==true;
    }
    private void TaiLai(string? select=null)
    {
        loading=true;SelectedPreview.Source=null;
        try
        {
            data=bus.LayDanhSach(visitId,ShowHidden.IsChecked==true,actor);
            IdentityText.Text=$"{data.HoTenBN} · {data.MaBN} · Lần khám {data.MaLS}";
            var prior=ServicePicker.SelectedValue as string;
            var services=data.DichVu.Select(s=>new LuaChonDanhMuc(s.MaDVThucHien,$"{s.TenDV} · {s.MaDVThucHien}")).ToList();
            if(actor.LaAdmin||actor.CoQuyen(QuyenLamViec.Kham))services.Insert(0,new("","Không liên kết dịch vụ chụp"));
            ServicePicker.ItemsSource=services;ServicePicker.SelectedValue=prior??"";
            if(ServicePicker.SelectedItem is null&&services.Count>0)ServicePicker.SelectedIndex=0;
            TechnicianPicker.ItemsSource=data.DuocPhanCong?bus.LayKyThuatVien(visitId,actor):Array.Empty<NhanVienChupAnh>();
            ImageList.ItemsSource=data.CacAnh;
            ImageList.SelectedItem=data.CacAnh.FirstOrDefault(x=>x.Anh.MaAnh==select)??data.CacAnh.FirstOrDefault();
            storeReady=false;
            try{new KhoLuuAnhXquang(config??CauHinhKhoAnh.Doc()).ThuMucGoc();storeReady=true;}
            catch(LoiAnhXquangException ex){StatusText.Text=ex.Message;}
            if(storeReady)StatusText.Text=data.CacAnh.Count==0?"Chưa có ảnh trong lần khám này.":$"{data.CacAnh.Count} ảnh · Mở ảnh sẽ được ghi nhật ký truy cập.";
            if(!data.DuocTaiLen)StatusText.Text+=" Chỉ xem trong phạm vi được phép; lượt đã hoàn tất không nhận thêm ảnh.";
        }
        catch(InvalidOperationException ex){data=null;ImageList.ItemsSource=null;StatusText.Text=ex.Message;IdentityText.Text="Không tải được ảnh trong phạm vi được phép.";storeReady=false;}
        finally{loading=false;UpdateButtons();}
        PreviewSelected();
    }
    private void PreviewSelected()
    {
        SelectedPreview.Source=null;SelectedCaption.Text="Chọn ảnh trong danh sách để xem trước.";
        if(!storeReady||ImageList.SelectedItem is not AnhXquangHienThi row)return;
        SelectedCaption.Text=$"{row.Anh.TenTepGoc}\n{row.NguoiTai} · {row.Anh.NgayTao:dd/MM/yyyy HH:mm:ss}\n{row.Anh.GhiChu}";
        if(row.Anh.TrangThai!="Đang sử dụng"){SelectedCaption.Text+="\nẢnh đã ẩn; metadata và tệp gốc được giữ lại.";return;}
        Guard(()=>SelectedPreview.Source=Decode(bus.MoAnh(visitId,row.Anh.MaAnh,actor).NoiDung,360),out _);
    }
    private bool Guard(Action action,out string? error)
    {
        error=null;
        try{action();return true;}
        catch(Exception ex) when(ex is InvalidOperationException or IOException or UnauthorizedAccessException or NotSupportedException or FormatException)
        {
            error=StatusText.Text=ex is InvalidOperationException?ex.Message:"Không hiển thị hoặc đọc được ảnh. Kiểm tra kho ảnh và tải lại.";SelectedPreview.Source=null;
            if(ex is TuChoiQuyenNghiepVuException){data=null;ImageList.ItemsSource=null;IdentityText.Text="Phiên không còn quyền xem ảnh này.";}
            UpdateButtons();return false;
        }
    }
    internal static BitmapImage Decode(byte[] bytes,int width=0)
    {
        using var stream=new MemoryStream(bytes);var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;
        if(width>0)image.DecodePixelWidth=width;image.StreamSource=stream;image.EndInit();image.Freeze();return image;
    }
    public bool TryTaiLen(Stream content,string name,out string? error)
    {
        if(busy){error="Ảnh đang được xử lý. Vui lòng chờ.";return false;}
        busy=true;UpdateButtons();
        try{return Guard(()=>{
            if(readOnly||data?.DuocTaiLen!=true)throw new TuChoiQuyenNghiepVuException();
            var link=ServicePicker.SelectedValue as string;
            var saved=bus.TaiLen(visitId,content,name,string.IsNullOrWhiteSpace(link)?null:link,ImageNote.Text,actor);
            ImageNote.Clear();TaiLai(saved.MaAnh);StatusText.Text="Đã lưu ảnh trong hồ sơ lần khám. Nội dung trùng đã có sẽ dùng lại ảnh hiện có.";
        },out error);}finally{busy=false;UpdateButtons();}
    }
    public bool TryXemLon(Action<DuLieuAnhXquang> show,out string? error)=>Guard(()=>
    {
        if(ImageList.SelectedItem is not AnhXquangHienThi row)throw new LoiAnhXquangException("ChonAnh","Chọn ảnh để xem.");
        show(bus.MoAnh(visitId,row.Anh.MaAnh,actor));
    },out error);
    public bool TryAnAnh(out string? error)=>Guard(()=>
    {
        if(readOnly||ImageList.SelectedItem is not AnhXquangHienThi row||!row.DuocAn)throw new TuChoiQuyenNghiepVuException();
        bus.AnAnh(visitId,row.Anh.MaAnh,actor);TaiLai();StatusText.Text="Đã ẩn ảnh; metadata và tệp gốc vẫn được giữ trong hồ sơ.";
    },out error);
    private void Image_Changed(object sender,SelectionChangedEventArgs e){if(!loading){PreviewSelected();UpdateButtons();}}
    private void Reload_Click(object sender,RoutedEventArgs e)=>TaiLai((ImageList.SelectedItem as AnhXquangHienThi)?.Anh.MaAnh);
    private void Hidden_Changed(object sender,RoutedEventArgs e){if(ImageList is not null&&!loading)TaiLai();}
    private void Upload_Click(object sender,RoutedEventArgs e)
    {
        var dialog=new OpenFileDialog{Filter="Ảnh JPG/PNG|*.jpg;*.jpeg;*.png",Multiselect=false,CheckFileExists=true};
        if(dialog.ShowDialog(Window.GetWindow(this))!=true)return;
        Guard(()=>{using var stream=File.OpenRead(dialog.FileName);TryTaiLen(stream,Path.GetFileName(dialog.FileName),out _);},out _);
    }
    private void View_Click(object sender,RoutedEventArgs e)=>TryXemLon(p=>new XemAnhXquang(p){Owner=Window.GetWindow(this)}.Show(),out _);
    private void Assign_Click(object sender,RoutedEventArgs e)=>Guard(()=>
    {
        if(readOnly||data?.DuocPhanCong!=true||ServicePicker.SelectedValue is not string source||string.IsNullOrEmpty(source)||TechnicianPicker.SelectedValue is not string tech)
            throw new LoiAnhXquangException("PhanCong","Chọn dịch vụ chụp và nhân viên kỹ thuật để phân công.");
        bus.PhanCong(visitId,source,tech,actor);TaiLai();StatusText.Text="Đã phân công tải ảnh. Nhân viên mở Dịch vụ thực hiện → Ảnh X-quang được giao.";
    },out _);
    private void Hide_Click(object sender,RoutedEventArgs e)
    {if(MessageBox.Show(Window.GetWindow(this),"Ẩn ảnh được chọn khỏi danh sách đang sử dụng? Tệp gốc và lịch sử vẫn được giữ.","Ẩn ảnh X-quang",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)TryAnAnh(out _);}
}
