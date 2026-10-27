using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls.ClinicalWorkflow;
using RangHamMat.Controls;
namespace RangHamMat.Views;

public partial class ThucHienDichVuEditor : UserControl,IPatientScoped
{
    private readonly BUS_DICHVUTHUCHIEN service;
    private readonly NguCanhNghiepVu actor;
    private readonly IDbTransaction? transaction;
    private KhongGianThucHien? board;
    private BanNhapThucHien? saved;
    private string? stepId;
    private string? patientScope;
    private bool loading=true,dirty;
    public string? PatientId{get;private set;}
    public bool CoThayDoiChuaLuu=>dirty;
    public ThucHienDichVuEditor(NguCanhNghiepVu? user=null,IDbTransaction? transaction=null)
    {
        this.transaction=transaction;service=new(transaction);actor=new BUS_QUYENNGHIEPVU(transaction).LayNguCanh(user?.MaNV??PhienDangNhap.MaNV);
        InitializeComponent();LoadVisitChoices();loading=false;UpdateRights();
    }
    private void LoadVisitChoices()
    {VisitPicker.ItemsSource=service.LayLuotThucHien(actor,patientScope).Select(v=>new LuaChonDanhMuc(v.MaLS,$"{v.MaLS} · {v.HoTenBN} ({v.MaBN}) · {v.ThoiGianKham:dd/MM/yyyy HH:mm}")).ToArray();}
    public void SetPatient(string id)=>FocusContext(id);
    public void FocusContext(string? patientId,string? visitId=null)
    {
        if(!CanLeave())return;
        Run(()=>
        {
            var next=visitId is null?null:service.LayKhongGianThucHien(visitId,actor);
            if(next is not null && patientId is not null && next.Luot.MaBN!=patientId)throw new TuChoiQuyenNghiepVuException();
            loading=true;
            try{patientScope=patientId;PatientId=patientId??next?.Luot.MaBN;LoadVisitChoices();board=next;VisitPicker.SelectedValue=visitId;ShowBoard();}finally{loading=false;}
        },out _);
    }
    private void ShowBoard(string? selectId=null)
    {
        var prior=loading;loading=true;
        try
        {
            PatientHeader.Text=board is null?"Chọn đúng lượt để xem dịch vụ.":$"{board.Luot.HoTenBN} · {board.Luot.MaBN} · hồ sơ {board.Luot.MaHS}";
            VisitHeader.Text=board is null?"":$"Lượt {board.Luot.MaLS} · bác sĩ phụ trách {board.Luot.MaBS} · {board.Luot.TrangThai}";
            PlannedGrid.ItemsSource=board?.DuKien;ActualGrid.ItemsSource=board?.ThucTe;
            ActualSummary.Text=board is null?"":$"{board.ThucTe.Count} dòng · Giá trị đã xác nhận {board.GiaTriDaXacNhan:N0} đ. Khoản thu thuộc đúng lượt hiện tại.";
            var row=board?.ThucTe.FirstOrDefault(r=>r.DichVu.MaDVThucHien==selectId)??board?.ThucTe.FirstOrDefault();
            ActualGrid.SelectedItem=row;Fill(row);dirty=false;
        }
        finally{loading=prior;}
    }
    private void Fill(BanNhapThucHien? row)
    {
        var prior=loading;loading=true;
        try
        {
            saved=row;stepId=row?.DichVu.MaCTKH;ActualGrid.SelectedItem=row;
            var choices=board?.DanhMuc.Select(d=>new LuaChonDanhMuc(d.MaDV,d.TenDV+" ("+d.MaDV+")")).ToList()??new();
            if(row is not null && choices.All(c=>c.Value!=row.DichVu.MaDV))choices.Add(new(row.DichVu.MaDV,row.TenDV??row.DichVu.MaDV));ServiceChoice.ItemsSource=choices;ServiceChoice.SelectedValue=row?.DichVu.MaDV;
            var people=board?.NhanVien.Select(n=>new LuaChonDanhMuc(n.MaNV,n.HoTen+" ("+n.MaNV+")")).ToList()??new();
            if(row is not null&&people.All(p=>p.Value!=row.DichVu.MaNVThucHien))people.Add(new(row.DichVu.MaNVThucHien,row.TenNguoiThucHien??row.DichVu.MaNVThucHien));PerformerChoice.ItemsSource=people;PerformerChoice.SelectedValue=row?.DichVu.MaNVThucHien??actor.MaNV;
            TimeEditor.Text=(row?.DichVu.ThoiGianThucHien??DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss");QuantityEditor.Text=(row?.DichVu.SoLuong??1).ToString("0.###",CultureInfo.GetCultureInfo("vi-VN"));PriceEditor.Text=(row?.DichVu.DonGia??0).ToString("0.##",CultureInfo.GetCultureInfo("vi-VN"));
            ResultEditor.Text=row?.DichVu.KetQua??"";ReasonEditor.Text=row?.LyDoPhatSinh??"";
            SourceHeader.Text=row is null?"Dịch vụ phát sinh mới — cần lý do":$"{row.DichVu.MaDVThucHien} · {row.DichVu.TrangThai} · bước {stepId??"phát sinh"}";dirty=false;UpdateRights();
        }
        finally{loading=prior;}
    }
    private void UpdateRights()
    {
        var edit=board is not null&&(saved?.DuocSua??board.DuocTao);var doctor=board?.DuocTao==true;
        NewButton.IsEnabled=FromStepButton.IsEnabled=doctor;TimeEditor.IsEnabled=ResultEditor.IsEnabled=SaveButton.IsEnabled=edit;
        QuantityEditor.IsEnabled=PriceEditor.IsEnabled=PerformerChoice.IsEnabled=edit&&doctor;ServiceChoice.IsEnabled=edit&&doctor&&saved is null&&stepId is null;ReasonEditor.IsEnabled=edit&&doctor&&stepId is null;
        ConfirmButton.IsEnabled=saved?.DuocXacNhan==true;CancelButton.IsEnabled=edit&&doctor&&saved is not null;
    }
    private bool CanLeave(){if(dirty)ErrorText.Text="Có thay đổi chưa lưu. Lưu nháp hoặc tải lại để bỏ thay đổi trước khi chuyển nguồn.";return !dirty;}
    private bool Run(Action action,out string error)
    {error="";ErrorText.Text="";ClinicalFormInput.ClearErrors(this);try{action();return true;}catch(InvalidOperationException ex){error=ErrorText.Text=ClinicalFormInput.Message(ex);ClinicalFormInput.ShowError(this,ex);return false;}}
    private static decimal Number(TextBox editor,string field)
    {if(decimal.TryParse(editor.Text.Trim().Replace(',','.'),NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out var value))return value;throw new LoiKhamException(field,"Nhập số hợp lệ; dùng dấu phẩy cho phần lẻ.");}
    public bool TrySave(out string error)=>Run(()=>
    {
        if(board is null||!(saved?.DuocSua??board.DuocTao))throw new TuChoiQuyenNghiepVuException();
        if(!DateTime.TryParseExact(TimeEditor.Text.Trim(),new[]{"dd/MM/yyyy HH:mm:ss","dd/MM/yyyy HH:mm"},CultureInfo.GetCultureInfo("vi-VN"),DateTimeStyles.None,out var time))throw new LoiKhamException("ThoiGianThucHien","Nhập ngày giờ theo dd/MM/yyyy HH:mm:ss.");
        var request=new YeuCauThucHienDichVu {MaDVThucHien=saved?.DichVu.MaDVThucHien,MaLS=board.Luot.MaLS,MaCTKH=stepId,MaDV=ServiceChoice.SelectedValue as string??"",MaNVThucHien=PerformerChoice.SelectedValue as string??"",SoLuong=Number(QuantityEditor,"SoLuong"),DonGia=Number(PriceEditor,"DonGia"),ThoiGianThucHien=time,KetQua=ResultEditor.Text,LyDoPhatSinh=ReasonEditor.Text,DauVanTay=saved?.DauVanTay};
        var result=service.LuuThucHien(request,actor);Reload(result.DichVu.MaDVThucHien);StatusText.Text="Đã lưu nháp; chờ bác sĩ kiểm tra và xác nhận.";SyncCache();
    },out error);
    public bool TryConfirm(out string error)=>Run(()=>
    {
        if(saved is null||!saved.DuocXacNhan)throw new TuChoiQuyenNghiepVuException();if(dirty)throw new LoiKhamException("BanNhap","Lưu nháp trước khi xác nhận nội dung đã thực hiện.");
        var result=service.XacNhanThucHien(saved.DichVu.MaDVThucHien,saved.DauVanTay,actor);Reload(result.DichVu.MaDVThucHien);StatusText.Text="Đã xác nhận thực hiện, nguồn đã khóa; chưa tự tạo hóa đơn.";SyncCache();
    },out error);
    public bool TryCancel(out string error)=>Run(()=>
    {
        if(saved is null||!CancelButton.IsEnabled)throw new TuChoiQuyenNghiepVuException();var result=service.HuyThucHien(saved.DichVu.MaDVThucHien,saved.DauVanTay,actor);Reload(result.DichVu.MaDVThucHien);StatusText.Text="Đã hủy nháp và trả lại phần lượng dự kiến.";SyncCache();
    },out error);
    private void Reload(string? selectId=null){if(board is null)return;board=service.LayKhongGianThucHien(board.Luot.MaLS,actor);ShowBoard(selectId??saved?.DichVu.MaDVThucHien);}
    private void SyncCache(){if(transaction is not null)return;try{DuLieuGiaoDien.RefreshFromDatabase();}catch(InvalidOperationException){StatusText.Text+=" Đã lưu dữ liệu; tải lại khi kết nối ổn định.";}}
    private void FromStep_Click(object sender,RoutedEventArgs e)
    {
        if(!CanLeave())return;Run(()=>
        {
            if(board?.DuocTao!=true||PlannedGrid.SelectedItem is not BuocDuKienThucHien step||step.ConLai<=0)throw new LoiKhamException("MaCTKH","Chọn bước còn lượng để thực hiện.");
            Fill(null);loading=true;try{stepId=step.MaCTKH;ServiceChoice.SelectedValue=step.MaDV;QuantityEditor.Text=Math.Min(1,step.ConLai).ToString("0.###",CultureInfo.GetCultureInfo("vi-VN"));PriceEditor.Text=step.DonGiaDuKien.ToString("0.##",CultureInfo.GetCultureInfo("vi-VN"));SourceHeader.Text=$"Từ bước {step.MaCTKH} · {step.TenDV} · buổi {step.LanThu}";UpdateRights();}finally{loading=false;}SourceTabs.SelectedIndex=1;
        },out _);
    }
    private void New_Click(object sender,RoutedEventArgs e){if(CanLeave()&&board?.DuocTao==true)Fill(null);}
    private void Actual_Changed(object sender,SelectionChangedEventArgs e)
    {if(loading)return;if(!CanLeave()){loading=true;ActualGrid.SelectedItem=board?.ThucTe.FirstOrDefault(r=>r.DichVu.MaDVThucHien==saved?.DichVu.MaDVThucHien);loading=false;return;}if(ActualGrid.SelectedItem is BanNhapThucHien row)Fill(row);}
    private void Visit_Changed(object sender,SelectionChangedEventArgs e)
    {if(loading)return;if(!CanLeave()){loading=true;VisitPicker.SelectedValue=board?.Luot.MaLS;loading=false;return;}if(VisitPicker.SelectedValue is string id)FocusContext(patientScope,id);}
    private void Field_Changed(object sender,RoutedEventArgs e){if(!loading)dirty=true;}
    private void Input_Changed(object sender,TextChangedEventArgs e){if(!loading)dirty=true;}
    private void Save_Click(object sender,RoutedEventArgs e)=>TrySave(out _);
    private void Reload_Click(object sender,RoutedEventArgs e)=>Run(()=>{Reload();StatusText.Text="Đã tải lại; bỏ thay đổi chưa lưu.";},out _);
    private void Confirm_Click(object sender,RoutedEventArgs e){if(dirty){TryConfirm(out _);return;}if(MessageBox.Show("Xác nhận dịch vụ đã được thực hiện và kết quả đã kiểm tra?","Xác nhận thực hiện",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)TryConfirm(out _);}
    private void Cancel_Click(object sender,RoutedEventArgs e){if(MessageBox.Show("Hủy dòng nháp đã lưu? Nguồn vẫn được giữ để đối chiếu.","Hủy dịch vụ nháp",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)TryCancel(out _);}
}
