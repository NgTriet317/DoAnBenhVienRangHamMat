using System.Windows.Controls;
using System.Windows;
using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;
/// <summary>
/// Phần xử lý bàn giao thuốc trình nhập liệu của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class BanGiaoThuocEditor : UserControl
{
    /// <summary>
    /// Đối tượng nghiệp vụ bàn giao thuốc được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private readonly BUS_BANGIAOTHUOC bus=new();
    /// <summary>
    /// Ngữ cảnh định danh và quyền của người dùng đang thao tác.
    /// </summary>
    private readonly NguCanhNghiepVu actor;
    /// <summary>
    /// Phạm vi bệnh nhân phạm vi lần khám mã đang chờ xử lý được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private string? patientScope,visitScope,pendingId;
    /// <summary>
    /// Trang tổng được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private int page=1,total;
    /// <summary>
    /// Cờ Cờ đang điền dữ liệu, dùng tránh xử lý lại sự kiện phát sinh do cập nhật giao diện; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool loading;
    /// <summary>
    /// Dữ liệu chi tiết của mục đang xử lý. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public ChiTietBanGiao? ChiTiet {get;private set;}
    /// <summary>
    /// Đang xử lý.
    /// </summary>
    public bool DangXuLy {get;private set;}
    /// <summary>
    /// Kết quả. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public KetQuaBanGiao? KetQua {get;private set;}
    /// <summary>
    /// Giá trị Mã bệnh nhân; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? PatientId=>ChiTiet?.Luot.MaBN??patientScope;
    /// <summary>
    /// Khởi tạo bàn giao thuốc trình nhập liệu. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    public BanGiaoThuocEditor(NguCanhNghiepVu? user=null)
    {InitializeComponent();DrugLines.RowHeight=double.NaN;actor=user??QuyenNghiepVu.LayNguCanh();GuidanceButton.Visibility=actor.CoQuyen(QuyenLamViec.CauHinhHoTro)?Visibility.Visible:Visibility.Collapsed;Reload(null,false);}
    /// <summary>
    /// Chuyển màn hình tới đúng bệnh nhân/lần khám hoặc chứng từ theo ngữ cảnh được yêu cầu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Reload</c>.</remarks>
    /// <param name="patient">Giá trị bệnh nhân dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <param name="visit">Giá trị lần khám dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <param name="prescription">Giá trị đơn thuốc dùng khi thực hiện thao tác; kiểu string?.</param>
    public void FocusContext(string? patient,string? visit=null,string? prescription=null)
    {
        if(DangXuLy||pendingId is not null){StatusText.Text="Cần đối chiếu kết quả giao đúng đơn đang xử lý trước khi đổi nguồn.";return;}
        patientScope=string.IsNullOrWhiteSpace(patient)?null:patient;visitScope=string.IsNullOrWhiteSpace(visit)?null:visit;page=1;Reload(prescription,prescription is not null);
    }
    /// <summary>
    /// Tải lại dữ liệu của mục đang mở và đồng bộ phần hiển thị.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>bus.LayHangDoi</c>, <c>rows.CacDon.FirstOrDefault</c>, <c>Clear</c>, <c>LoadDetail</c>.</remarks>
    /// <param name="selected">Giá trị đang chọn dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <param name="keep">Giá trị giữ dùng khi thực hiện thao tác; kiểu bool.</param>
    private void Reload(string? selected,bool keep)
    {
        try
        {
            var rows=bus.LayHangDoi(new(patientScope,visitScope,SearchBox.Text,StateFilter.SelectedValue as string??"Chờ giao",page),actor);total=rows.TongSo;
            loading=true;QueueList.ItemsSource=rows.CacDon;QueueList.SelectedItem=rows.CacDon.FirstOrDefault(d=>d.MaDT==selected);loading=false;
            PageText.Text=$"Trang {page} · {total} đơn";
            var id=(QueueList.SelectedItem as HangBanGiao)?.MaDT??(keep?selected:null);
            if(id is null)Clear();else LoadDetail(id);
        }
        catch(Exception ex){Clear();StatusText.Text=ex is TuChoiQuyenNghiepVuException or LoiKhamException?ex.Message:"Không tải được hàng chờ. Tải lại khi kết nối ổn định.";}
        finally{loading=false;UpdateRights();}
    }
    /// <summary>
    /// Đặt lại dữ liệu hoặc phần hiển thị của thành phần theo các bước trong hàm.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ResetChecks</c>.</remarks>
    private void Clear(){ChiTiet=null;KetQua=null;DrugLines.ItemsSource=null;PatientText.Text="Chọn đơn đã thanh toán để bàn giao";SourceText.Text="";ResetChecks();}
    /// <summary>
    /// Đặt lại các ô đối chiếu bàn giao khi thay nguồn đơn hoặc tải lại chứng từ.
    /// </summary>
    private void ResetChecks(){ReceiverChecked.IsChecked=GuidanceChecked.IsChecked=false;}
    /// <summary>
    /// Đọc dữ liệu chi tiết của chứng từ đang mở để điền giao diện và kiểm tra quyền thao tác.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>bus.LayChiTiet</c>, <c>ResetChecks</c>, <c>Render</c>.</remarks>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    private void LoadDetail(string id)
    {
        var s=bus.LayChiTiet(id,actor);
        if((patientScope is not null&&s.Luot.MaBN!=patientScope)||(visitScope is not null&&s.Luot.MaLS!=visitScope))throw new LoiKhamException("MaDT","Đơn không thuộc bệnh nhân/lượt đang mở.");
        ChiTiet=s;KetQua=null;ResetChecks();Render();StatusText.Text=s.Luot.TrangThai=="Đã giao"?$"Đã giao bởi {s.Luot.TenNguoiGiao??s.Luot.MaNVGiao} · {s.Luot.ThoiGianGiao:dd/MM/yyyy HH:mm}":"Đối chiếu đúng người nhận, nội dung đơn và hướng dẫn trước xác nhận.";
    }
    /// <summary>
    /// Hiển thị dữ liệu hiện tại và cập nhật các vùng chi tiết liên quan.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>.ToString</c>, <c>s.Don.Nguon.CacDong.Select</c>.</remarks>
    private void Render()
    {
        if(ChiTiet is not{}s)return;
        PatientText.Text=$"{s.Luot.HoTenBN} · Mã {s.Luot.MaBN} · Ngày sinh {s.Luot.NgaySinh?.ToString("dd/MM/yyyy")??"chưa ghi nhận"}";
        SourceText.Text=$"Đơn #{s.Luot.MaDT} · Lượt #{s.Luot.MaLS}\nHóa đơn #{s.Luot.MaHD} · Xuất #{s.Luot.MaPX} · {s.Luot.TrangThai}\nBác sĩ: {s.Don.TenBS} · Ngày kê {s.Luot.NgayKe:dd/MM/yyyy HH:mm}";
        DrugLines.ItemsSource=s.Don.Nguon.CacDong.Select(d=>new{Thuoc=d.TenThuocChot+" · "+d.HamLuongChot,Luong=$"{d.SoLuong:0.###} {d.DonViTinhChot}",CachDung=$"{d.LieuDung}\n{d.DuongDung}\n{d.SoNgayDung} ngày",d.HuongDan}).ToArray();
    }
    /// <summary>
    /// Cập nhật trạng thái cho phép thao tác theo quyền và dữ liệu đang hiển thị.
    /// </summary>
    private void UpdateRights()
    {
        if(GiveButton is null)return;
        var editable=!DangXuLy&&pendingId is null&&ChiTiet?.Luot.TrangThai=="Chờ giao";
        ReceiverChecked.IsEnabled=GuidanceChecked.IsEnabled=editable;
        GiveButton.IsEnabled=!DangXuLy&&ChiTiet?.Luot.TrangThai=="Chờ giao"&&(pendingId is not null||ReceiverChecked.IsChecked==true&&GuidanceChecked.IsChecked==true);
        GiveButton.Content=pendingId is null?"Xác nhận đã giao":"Thử lại đúng đơn";
        QueueList.IsEnabled=FiltersPanel.IsEnabled=!DangXuLy&&pendingId is null;
        PrintRxButton.IsEnabled=PrintInvoiceButton.IsEnabled=ChiTiet is not null&&!DangXuLy&&pendingId is null;
    }
    /// <summary>
    /// Đối chiếu các mục bàn giao và thử xác nhận giao thuốc bất đồng bộ; làm mới chứng từ theo kết quả.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>UpdateRights</c>, <c>Task.Run</c>, <c>bus.XacNhanGiao</c>, <c>Reload</c>.</remarks>
    /// <returns>Tác vụ bất đồng bộ để nơi gọi theo dõi thời điểm kết thúc và kết quả xử lý.</returns>
    public async Task<bool> TryGiaoAsync()
    {
        if(DangXuLy||ChiTiet?.Luot.TrangThai!="Chờ giao")return false;
        if(pendingId is null&&(ReceiverChecked.IsChecked!=true||GuidanceChecked.IsChecked!=true)){StatusText.Text="Cần đối chiếu người nhận và hướng dẫn trước giao.";return false;}
        var id=pendingId??ChiTiet.Luot.MaDT;pendingId=id;DangXuLy=true;UpdateRights();StatusText.Text="Đang ghi nhận bàn giao…";
        try
        {
            var result=await Task.Run(()=>bus.XacNhanGiao(id,actor));pendingId=null;
            var old=ChiTiet;Reload(id,true);KetQua=result;
            if(ChiTiet is null&&old is not null){ChiTiet=old with{Luot=old.Luot with{TrangThai="Đã giao",MaNVGiao=result.MaNVGiao,ThoiGianGiao=result.ThoiGianGiao}};Render();}
            StatusText.Text=$"Đã giao đơn #{id} bởi {ChiTiet?.Luot.TenNguoiGiao??result.MaNVGiao} · {result.ThoiGianGiao:dd/MM/yyyy HH:mm}. "+(result.DaGiaoHet?"Lượt đã hoàn tất.":"Lượt còn đơn chờ giao.");
            try{DuLieuGiaoDien.RefreshFromDatabase();}catch{StatusText.Text+=" Tải lại lịch sử khi kết nối ổn định.";}
            return true;
        }
        catch(LoiKhamException ex) when(ex.Truong!="BanGiao"){pendingId=null;StatusText.Text=ex.Message;return false;}
        catch(TuChoiQuyenNghiepVuException ex){pendingId=null;Clear();StatusText.Text=ex.Message;return false;}
        catch(Exception){StatusText.Text="Chưa xác nhận được kết quả giao. Thử lại đúng đơn này để đối chiếu; không xuất thuốc lần nữa.";return false;}
        finally{DangXuLy=false;UpdateRights();}
    }
    /// <summary>
    /// Tiếp nhận thay đổi hàng đợi và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>QueueList.Items.Cast&lt;HangBanGiao&gt;</c>, <c>LoadDetail</c>, <c>Clear</c>, <c>UpdateRights</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Queue_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(loading)return;
        if(DangXuLy||pendingId is not null){loading=true;QueueList.SelectedItem=QueueList.Items.Cast<HangBanGiao>().FirstOrDefault(d=>d.MaDT==ChiTiet?.Luot.MaDT);loading=false;return;}
        try{if(QueueList.SelectedItem is HangBanGiao row)LoadDetail(row.MaDT);else Clear();}catch(Exception){Clear();StatusText.Text="Không đọc được đơn trong phạm vi; tải lại hàng chờ.";}UpdateRights();
    }
    /// <summary>
    /// Tiếp nhận thay đổi các mục kiểm tra và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>UpdateRights</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Checks_Changed(object sender,RoutedEventArgs e)=>UpdateRights();
    /// <summary>
    /// Xử lý thao tác bàn giao khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TryGiaoAsync</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private async void Give_Click(object sender,RoutedEventArgs e)=>await TryGiaoAsync();
    /// <summary>
    /// Xử lý thao tác tải lại khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Reload</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Reload_Click(object sender,RoutedEventArgs e){page=1;Reload(ChiTiet?.Luot.MaDT,false);}
    /// <summary>
    /// Xử lý thao tác trước khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Reload</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Previous_Click(object sender,RoutedEventArgs e){if(page>1){page--;Reload(null,false);}}
    /// <summary>
    /// Xử lý thao tác tiếp khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Reload</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Next_Click(object sender,RoutedEventArgs e){if(page*25<total){page++;Reload(null,false);}}
    /// <summary>
    /// Xử lý thao tác in đơn thuốc khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Window.GetWindow</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void PrintRx_Click(object sender,RoutedEventArgs e)
    {try{var id=ChiTiet?.Luot.MaDT??throw new LoiKhamException("MaDT","Chọn đơn.");new XemTruocDonThuoc(()=>new BUS_DONTHUOC().LayBanInDon(id,actor)){Owner=Window.GetWindow(this)}.Show();}catch(Exception){StatusText.Text="Không mở được bản in; trạng thái giao và thu tiền giữ nguyên.";}}
    /// <summary>
    /// Xử lý thao tác in hóa đơn khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Window.GetWindow</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void PrintInvoice_Click(object sender,RoutedEventArgs e)
    {try{var id=ChiTiet?.Luot.MaHD??throw new LoiKhamException("MaHD","Chọn đơn.");new XemTruocHoaDon(()=>new BUS_THANHTOANTRONGOI().LayBanInHoaDon(id,actor)){Owner=Window.GetWindow(this)}.Show();}catch(Exception){StatusText.Text="Không mở được hóa đơn; dùng in lại, không thu hoặc xuất lại.";}}
    /// <summary>
    /// Xử lý thao tác lịch sử khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>view.SetPatient</c>, <c>Window.GetWindow</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void History_Click(object sender,RoutedEventArgs e){var view=new DonThuoc(true);if(patientScope is not null)view.SetPatient(patientScope);new Window{Title="Lịch sử đơn thuốc · chỉ đọc",Content=view,Owner=Window.GetWindow(this),Width=1200,Height=800}.Show();}
    /// <summary>
    /// Xử lý thao tác hướng dẫn khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Guidance_Click(object sender,RoutedEventArgs e)
    {var original=Content;var editor=new HuongDanThuocEditor(actor);editor.BackRequested+=()=>Content=original;Content=editor;}
}
