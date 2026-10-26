using System.Windows.Controls;
using System.Windows;
using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;
public partial class BanGiaoThuocEditor : UserControl
{
    private readonly BUS_BANGIAOTHUOC bus=new();
    private readonly NguCanhNghiepVu actor;
    private string? patientScope,visitScope,pendingId;
    private int page=1,total;
    private bool loading;
    public ChiTietBanGiao? ChiTiet {get;private set;}
    public bool DangXuLy {get;private set;}
    public KetQuaBanGiao? KetQua {get;private set;}
    public string? PatientId=>ChiTiet?.Luot.MaBN??patientScope;
    public BanGiaoThuocEditor(NguCanhNghiepVu? user=null)
    {InitializeComponent();DrugLines.RowHeight=double.NaN;actor=user??QuyenNghiepVu.LayNguCanh();GuidanceButton.Visibility=actor.CoQuyen(QuyenLamViec.CauHinhHoTro)?Visibility.Visible:Visibility.Collapsed;Reload(null,false);}
    public void FocusContext(string? patient,string? visit=null,string? prescription=null)
    {
        if(DangXuLy||pendingId is not null){StatusText.Text="Cần đối chiếu kết quả giao đúng đơn đang xử lý trước khi đổi nguồn.";return;}
        patientScope=string.IsNullOrWhiteSpace(patient)?null:patient;visitScope=string.IsNullOrWhiteSpace(visit)?null:visit;page=1;Reload(prescription,prescription is not null);
    }
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
    private void Clear(){ChiTiet=null;KetQua=null;DrugLines.ItemsSource=null;PatientText.Text="Chọn đơn đã thanh toán để bàn giao";SourceText.Text="";ResetChecks();}
    private void ResetChecks(){ReceiverChecked.IsChecked=GuidanceChecked.IsChecked=false;}
    private void LoadDetail(string id)
    {
        var s=bus.LayChiTiet(id,actor);
        if((patientScope is not null&&s.Luot.MaBN!=patientScope)||(visitScope is not null&&s.Luot.MaLS!=visitScope))throw new LoiKhamException("MaDT","Đơn không thuộc bệnh nhân/lượt đang mở.");
        ChiTiet=s;KetQua=null;ResetChecks();Render();StatusText.Text=s.Luot.TrangThai=="Đã giao"?$"Đã giao bởi {s.Luot.TenNguoiGiao??s.Luot.MaNVGiao} · {s.Luot.ThoiGianGiao:dd/MM/yyyy HH:mm}":"Đối chiếu đúng người nhận, nội dung đơn và hướng dẫn trước xác nhận.";
    }
    private void Render()
    {
        if(ChiTiet is not{}s)return;
        PatientText.Text=$"{s.Luot.HoTenBN} · Mã {s.Luot.MaBN} · Ngày sinh {s.Luot.NgaySinh?.ToString("dd/MM/yyyy")??"chưa ghi nhận"}";
        SourceText.Text=$"Đơn #{s.Luot.MaDT} · Lượt #{s.Luot.MaLS}\nHóa đơn #{s.Luot.MaHD} · Xuất #{s.Luot.MaPX} · {s.Luot.TrangThai}\nBác sĩ: {s.Don.TenBS} · Ngày kê {s.Luot.NgayKe:dd/MM/yyyy HH:mm}";
        DrugLines.ItemsSource=s.Don.Nguon.CacDong.Select(d=>new{Thuoc=d.TenThuocChot+" · "+d.HamLuongChot,Luong=$"{d.SoLuong:0.###} {d.DonViTinhChot}",CachDung=$"{d.LieuDung}\n{d.DuongDung}\n{d.SoNgayDung} ngày",d.HuongDan}).ToArray();
    }
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
    private void Queue_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(loading)return;
        if(DangXuLy||pendingId is not null){loading=true;QueueList.SelectedItem=QueueList.Items.Cast<HangBanGiao>().FirstOrDefault(d=>d.MaDT==ChiTiet?.Luot.MaDT);loading=false;return;}
        try{if(QueueList.SelectedItem is HangBanGiao row)LoadDetail(row.MaDT);else Clear();}catch(Exception){Clear();StatusText.Text="Không đọc được đơn trong phạm vi; tải lại hàng chờ.";}UpdateRights();
    }
    private void Checks_Changed(object sender,RoutedEventArgs e)=>UpdateRights();
    private async void Give_Click(object sender,RoutedEventArgs e)=>await TryGiaoAsync();
    private void Reload_Click(object sender,RoutedEventArgs e){page=1;Reload(ChiTiet?.Luot.MaDT,false);}
    private void Previous_Click(object sender,RoutedEventArgs e){if(page>1){page--;Reload(null,false);}}
    private void Next_Click(object sender,RoutedEventArgs e){if(page*25<total){page++;Reload(null,false);}}
    private void PrintRx_Click(object sender,RoutedEventArgs e)
    {try{var id=ChiTiet?.Luot.MaDT??throw new LoiKhamException("MaDT","Chọn đơn.");new XemTruocDonThuoc(()=>new BUS_DONTHUOC().LayBanInDon(id,actor)){Owner=Window.GetWindow(this)}.Show();}catch(Exception){StatusText.Text="Không mở được bản in; trạng thái giao và thu tiền giữ nguyên.";}}
    private void PrintInvoice_Click(object sender,RoutedEventArgs e)
    {try{var id=ChiTiet?.Luot.MaHD??throw new LoiKhamException("MaHD","Chọn đơn.");new XemTruocHoaDon(()=>new BUS_THANHTOANTRONGOI().LayBanInHoaDon(id,actor)){Owner=Window.GetWindow(this)}.Show();}catch(Exception){StatusText.Text="Không mở được hóa đơn; dùng in lại, không thu hoặc xuất lại.";}}
    private void History_Click(object sender,RoutedEventArgs e){var view=new DonThuoc(true);if(patientScope is not null)view.SetPatient(patientScope);new Window{Title="Lịch sử đơn thuốc · chỉ đọc",Content=view,Owner=Window.GetWindow(this),Width=1200,Height=800}.Show();}
    private void Guidance_Click(object sender,RoutedEventArgs e)
    {var original=Content;var editor=new HuongDanThuocEditor(actor);editor.BackRequested+=()=>Content=original;Content=editor;}
}
