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

/// <summary>
/// Quản lý ảnh X-quang trong một lần khám, phối hợp quyền tải/xem/ẩn và phân công nhân viên chụp.
/// </summary>
public partial class AnhXquangLanKham : UserControl
{
    /// <summary>
    /// Mã lần khám cố định dùng cho mọi yêu cầu đọc, tải và ẩn ảnh của điều khiển.
    /// </summary>
    private readonly string visitId;
    /// <summary>
    /// Ngữ cảnh người thao tác; tầng nghiệp vụ kiểm tra lại quyền theo lần khám và dịch vụ.
    /// </summary>
    private readonly NguCanhNghiepVu actor;
    /// <summary>
    /// Đầu mối nghiệp vụ ảnh dùng chung cho danh sách, nội dung tệp và phân công.
    /// </summary>
    private readonly BUS_ANHXQUANG bus;
    /// <summary>
    /// Cấu hình kho ảnh tùy chọn; null để đọc cấu hình mặc định khi cần truy cập kho.
    /// </summary>
    private readonly CauHinhKhoAnh? config;
    /// <summary>
    /// Danh sách ảnh, dịch vụ và các quyền thao tác được đọc trong lần tải gần nhất.
    /// </summary>
    private DanhSachAnhXquang? data;
    /// <summary>
    /// Các cờ giới hạn lịch sử, khóa thao tác, chặn sự kiện khi nạp và ghi nhận khả năng truy cập kho ảnh.
    /// </summary>
    private bool readOnly,busy,loading,storeReady;
    /// <summary>
    /// Mở ảnh của đúng lần khám với ngữ cảnh người dùng, giao dịch và cấu hình kho được cấp.
    /// </summary>
    /// <param name="maLS">Mã lần khám cần hiển thị ảnh.</param>
    /// <param name="user">Người dùng thực hiện các thao tác ảnh trong phạm vi được phép.</param>
    /// <param name="transaction">Giao dịch tùy chọn truyền cho tầng nghiệp vụ; nơi cấp quản lý kết thúc giao dịch.</param>
    /// <param name="cauHinh">Cấu hình kho ảnh tùy chọn cho lần mở điều khiển.</param>
    public AnhXquangLanKham(string maLS,NguCanhNghiepVu user,IDbTransaction? transaction=null,CauHinhKhoAnh? cauHinh=null)
    {
        visitId=maLS;actor=user;config=cauHinh;bus=new(transaction,cauHinh);InitializeComponent();TaiLai();
    }
    /// <summary>
    /// Giới hạn điều khiển thành chế độ xem lịch sử, tắt tải lên, ẩn ảnh và phân công.
    /// </summary>
    public void RestrictToHistory(){readOnly=true;UpdateButtons();}
    /// <summary>
    /// Cập nhật trạng thái nút theo quyền vừa tải, chế độ lịch sử, trạng thái ảnh và khả năng truy cập kho.
    /// </summary>
    private void UpdateButtons()
    {
        if(UploadButton is null)return;
        UploadButton.IsEnabled=UploadFields.IsEnabled=!busy&&!readOnly&&storeReady&&data?.DuocTaiLen==true;
        ViewButton.IsEnabled=!busy&&storeReady&&ImageList.SelectedItem is AnhXquangHienThi v&&v.Anh.TrangThai=="Đang sử dụng";
        HideButton.IsEnabled=!busy&&!readOnly&&ImageList.SelectedItem is AnhXquangHienThi h&&h.DuocAn;
        AssignmentPanel.Visibility=data?.DuocPhanCong==true&&!readOnly?Visibility.Visible:Visibility.Collapsed;
        AssignButton.IsEnabled=!busy&&!readOnly&&data?.DuocPhanCong==true;
    }
    /// <summary>
    /// Tải lại ảnh và dịch vụ của lần khám, phục hồi lựa chọn, kiểm tra kho ảnh rồi cập nhật quyền trên giao diện.
    /// </summary>
    /// <param name="select">Mã ảnh muốn chọn lại sau khi tải; null để chọn ảnh đầu tiên.</param>
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
    /// <summary>
    /// Xóa ảnh xem trước cũ và đọc ảnh đang sử dụng qua nghiệp vụ; ảnh đã ẩn chỉ hiện metadata.
    /// </summary>
    private void PreviewSelected()
    {
        SelectedPreview.Source=null;SelectedCaption.Text="Chọn ảnh trong danh sách để xem trước.";
        if(!storeReady||ImageList.SelectedItem is not AnhXquangHienThi row)return;
        SelectedCaption.Text=$"{row.Anh.TenTepGoc}\n{row.NguoiTai} · {row.Anh.NgayTao:dd/MM/yyyy HH:mm:ss}\n{row.Anh.GhiChu}";
        if(row.Anh.TrangThai!="Đang sử dụng"){SelectedCaption.Text+="\nẢnh đã ẩn; thông tin và ảnh trong kho được giữ lại.";return;}
        Guard(()=>SelectedPreview.Source=Decode(bus.MoAnh(visitId,row.Anh.MaAnh,actor).NoiDung,360),out _);
    }
    /// <summary>
    /// Thực hiện thao tác ảnh và chuyển các lỗi dự kiến thành thông báo; xóa nguồn hiển thị nếu quyền bị thu hồi.
    /// </summary>
    /// <param name="action">Thao tác đọc, ghi hoặc hiển thị ảnh cần bảo vệ.</param>
    /// <param name="error">Nhận thông báo lỗi; null nếu thao tác hoàn tất.</param>
    /// <returns>True khi thao tác chạy xong; false khi gặp một trong các lỗi được xử lý.</returns>
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
    /// <summary>
    /// Giải mã ảnh vào bộ nhớ trước khi đóng stream, tùy chọn giảm bề rộng và đóng băng BitmapImage.
    /// </summary>
    /// <param name="bytes">Nội dung ảnh cần giải mã.</param>
    /// <param name="width">Bề rộng pixel cần giải mã; 0 để giữ kích thước nguồn.</param>
    /// <returns>BitmapImage có thể dùng sau khi stream nguồn đã được giải phóng.</returns>
    internal static BitmapImage Decode(byte[] bytes,int width=0)
    {
        using var stream=new MemoryStream(bytes);var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;
        if(width>0)image.DecodePixelWidth=width;image.StreamSource=stream;image.EndInit();image.Freeze();return image;
    }
    /// <summary>
    /// Khóa thao tác trong lúc tải ảnh, kiểm tra chế độ và quyền rồi lưu nội dung qua nghiệp vụ của lần khám.
    /// </summary>
    /// <param name="content">Stream chứa ảnh do nơi gọi mở và quản lý vòng đời.</param>
    /// <param name="name">Tên tệp gốc để ghi nhận cùng ảnh.</param>
    /// <param name="error">Nhận lý do từ chối hoặc lỗi đọc/lưu; null nếu hoàn tất.</param>
    /// <returns>True khi thao tác tải hoàn tất, kể cả khi nghiệp vụ dùng lại ảnh có nội dung trùng.</returns>
    public bool TryTaiLen(Stream content,string name,out string? error)
    {
        if(busy){error="Ảnh đang được xử lý. Vui lòng chờ.";return false;}
        busy=true;UpdateButtons();
        try{return Guard(()=>{
            if(readOnly||data?.DuocTaiLen!=true)throw new TuChoiQuyenNghiepVuException();
            var link=ServicePicker.SelectedValue as string;
            var saved=bus.TaiLen(visitId,content,name,string.IsNullOrWhiteSpace(link)?null:link,ImageNote.Text,actor);
            ImageNote.Clear();TaiLai(saved.MaAnh);
            StatusText.Text=Path.GetExtension(name).ToLowerInvariant() is ".dcm" or ".dicom"
                ?"Đã chuyển khung đầu tiên của DICOM sang PNG và lưu trong hồ sơ lần khám. Nội dung trùng đã có sẽ dùng lại ảnh hiện có."
                :"Đã lưu ảnh trong hồ sơ lần khám. Nội dung trùng đã có sẽ dùng lại ảnh hiện có.";
        },out error);}finally{busy=false;UpdateButtons();}
    }
    /// <summary>
    /// Đọc lại ảnh đang chọn qua nghiệp vụ trước khi chuyển nội dung cho cửa sổ xem lớn.
    /// </summary>
    /// <param name="show">Thao tác mở cửa sổ hoặc hiển thị dữ liệu ảnh đã được phép đọc.</param>
    /// <param name="error">Nhận thông báo nếu chưa chọn ảnh hoặc không đọc được ảnh.</param>
    /// <returns>True khi đã đọc và chuyển ảnh cho hàm hiển thị.</returns>
    public bool TryXemLon(Action<DuLieuAnhXquang> show,out string? error)=>Guard(()=>
    {
        if(ImageList.SelectedItem is not AnhXquangHienThi row)throw new LoiAnhXquangException("ChonAnh","Chọn ảnh để xem.");
        show(bus.MoAnh(visitId,row.Anh.MaAnh,actor));
    },out error);
    /// <summary>
    /// Ẩn ảnh được chọn khi được phép, rồi tải lại danh sách; ảnh trong kho và metadata vẫn được giữ.
    /// </summary>
    /// <param name="error">Nhận lỗi từ chối quyền hoặc lỗi nghiệp vụ khi ẩn ảnh.</param>
    /// <returns>True khi yêu cầu ẩn và tải lại hoàn tất.</returns>
    public bool TryAnAnh(out string? error)=>Guard(()=>
    {
        if(readOnly||ImageList.SelectedItem is not AnhXquangHienThi row||!row.DuocAn)throw new TuChoiQuyenNghiepVuException();
        bus.AnAnh(visitId,row.Anh.MaAnh,actor);TaiLai();StatusText.Text="Đã ẩn ảnh; thông tin và ảnh trong kho vẫn được giữ trong hồ sơ.";
    },out error);
    /// <summary>
    /// Đổi ảnh xem trước và cập nhật nút khi người dùng chọn ảnh, bỏ qua sự kiện phát sinh trong lúc nạp.
    /// </summary>
    private void Image_Changed(object sender,SelectionChangedEventArgs e){if(!loading){PreviewSelected();UpdateButtons();}}
    /// <summary>
    /// Tải lại ảnh nhưng giữ lựa chọn theo mã ảnh hiện tại nếu ảnh vẫn còn trong danh sách.
    /// </summary>
    private void Reload_Click(object sender,RoutedEventArgs e)=>TaiLai((ImageList.SelectedItem as AnhXquangHienThi)?.Anh.MaAnh);
    /// <summary>
    /// Tải lại danh sách khi đổi lựa chọn xem ảnh ẩn, bỏ qua sự kiện trong quá trình khởi tạo.
    /// </summary>
    private void Hidden_Changed(object sender,RoutedEventArgs e){if(ImageList is not null&&!loading)TaiLai();}
    /// <summary>
    /// Mở hộp chọn một ảnh JPG/PNG/DICOM, quản lý stream tệp và chuyển nội dung cho TryTaiLen.
    /// </summary>
    private void Upload_Click(object sender,RoutedEventArgs e)
    {
        var dialog=new OpenFileDialog{Filter="Ảnh X-quang (JPG, PNG, DICOM)|*.jpg;*.jpeg;*.png;*.dcm;*.dicom|Ảnh DICOM|*.dcm;*.dicom|Ảnh JPG/PNG|*.jpg;*.jpeg;*.png",Multiselect=false,CheckFileExists=true};
        if(dialog.ShowDialog(Window.GetWindow(this))!=true)return;
        Guard(()=>{using var stream=File.OpenRead(dialog.FileName);TryTaiLen(stream,Path.GetFileName(dialog.FileName),out _);},out _);
    }
    /// <summary>
    /// Mở cửa sổ xem ảnh lớn với nội dung vừa được kiểm tra quyền đọc.
    /// </summary>
    private void View_Click(object sender,RoutedEventArgs e)=>TryXemLon(p=>new XemAnhXquang(p){Owner=Window.GetWindow(this)}.Show(),out _);
    /// <summary>
    /// Yêu cầu chọn dịch vụ chụp và kỹ thuật viên hợp lệ trước khi phân công tải ảnh cho lần khám.
    /// </summary>
    private void Assign_Click(object sender,RoutedEventArgs e)=>Guard(()=>
    {
        if(readOnly||data?.DuocPhanCong!=true||ServicePicker.SelectedValue is not string source||string.IsNullOrEmpty(source)||TechnicianPicker.SelectedValue is not string tech)
            throw new LoiAnhXquangException("PhanCong","Chọn dịch vụ chụp và nhân viên kỹ thuật để phân công.");
        bus.PhanCong(visitId,source,tech,actor);TaiLai();StatusText.Text="Đã phân công tải ảnh. Nhân viên mở Dịch vụ thực hiện → Ảnh X-quang được giao.";
    },out _);
    /// <summary>
    /// Xin xác nhận trước khi ẩn ảnh đang chọn; nội dung tệp và lịch sử vẫn được lưu.
    /// </summary>
    private void Hide_Click(object sender,RoutedEventArgs e)
    {if(MessageBox.Show(Window.GetWindow(this),"Ẩn ảnh được chọn khỏi danh sách đang sử dụng? Tệp gốc và lịch sử vẫn được giữ.","Ẩn ảnh X-quang",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)TryAnAnh(out _);}
}
