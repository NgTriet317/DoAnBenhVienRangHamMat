using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;

public partial class MauDieuTriEditor : UserControl
{
    private readonly NguCanhNghiepVu actor;
    private readonly IDbTransaction? transaction;
    private readonly BUS_KHAMDIEUTRI service;
    private readonly ObservableCollection<BuocKeHoachNhap> steps=new();
    private IReadOnlyList<ET_DICHVU> serviceCatalog=Array.Empty<ET_DICHVU>();
    private bool loading=true,dirty;
    public BanNhapMauDieuTri? MauDangMo {get;private set;}
    private bool CategoryPending=>NewCategoryName?.Text.Length>0||NewCategoryDescription?.Text.Length>0;
    public bool CoThayDoiChuaLuu=>dirty||CategoryPending;
    public event Action? BackRequested;
    public MauDieuTriEditor(NguCanhNghiepVu? user=null,IDbTransaction? transaction=null)
    {
        this.transaction=transaction;service=new(transaction);actor=new BUS_QUYENNGHIEPVU(transaction).LayNguCanh(user?.MaNV??PhienDangNhap.MaNV);
        if(!actor.LaAdmin && !(actor.CoQuyen(QuyenLamViec.Kham)&&actor.CoQuyen(QuyenLamViec.CauHinhHoTro)))throw new TuChoiQuyenNghiepVuException();
        InitializeComponent();TemplateSteps.ItemsSource=steps;steps.CollectionChanged+=(_,_)=>{if(!loading)dirty=true;};
        TemplateSteps.BeginningEdit+=(_,_)=>{if(!loading)dirty=true;};
        ReloadLists();ClearForm();loading=false;
    }
    private void ReloadLists()
    {
        CategoryPicker.ItemsSource=new BUS_DMTINHTRANGBENH(transaction).LayDanhSach(actor).Select(c=>new LuaChonDanhMuc(c.MaDMTT,c.TenTinhTrang+(c.TrangThai=="Đang áp dụng"?"":" (ngừng áp dụng)"))).ToArray();
        serviceCatalog=new BUS_DICHVU(transaction).layToanBoDichVu().ToArray();UpdateServiceChoices();
        TemplatePicker.ItemsSource=new BUS_MAUKEHOACHDIEUTRI(transaction).LayDanhSach(actor).Select(m=>new LuaChonDanhMuc(m.MaMauKH,$"{m.TenMau} · v{m.PhienBan} · {m.TrangThai} ({m.MaMauKH})")).ToArray();
    }
    private void ClearForm()
    {
        MauDangMo=null;TemplatePicker.SelectedIndex=-1;CategoryPicker.SelectedIndex=-1;TemplateName.Clear();SeverityEditor.Clear();VersionEditor.Text="1";steps.Clear();UpdateServiceChoices();TemplateIdentity.Text="Mẫu mới — chưa duyệt";ReviewIdentity.Text="Chưa có bác sĩ duyệt";dirty=false;UpdateRights();
    }
    private void Fill(BanNhapMauDieuTri data)
    {
        var previous=loading;loading=true;
        try
        {
            MauDangMo=data;CategoryPicker.SelectedValue=data.Mau.MaDMTT;TemplateName.Text=data.Mau.TenMau;SeverityEditor.Text=data.Mau.MucDo??"";VersionEditor.Text=data.Mau.PhienBan.ToString();
            steps.Clear();foreach(var s in data.CacBuoc)steps.Add(new(){MaDV=s.MaDV,LanThu=s.LanThu,SoLuongDuKien=s.SoLuongDuKien,NoiDungBuoc=s.NoiDung});
            UpdateServiceChoices();
            TemplateIdentity.Text=$"{data.Mau.MaMauKH} · {data.Mau.TrangThai} · phiên bản {data.Mau.PhienBan}";
            ReviewIdentity.Text=data.Mau.MaBSDuyet is null?"Chưa có bác sĩ duyệt":$"Bác sĩ duyệt: {data.Mau.MaBSDuyet} · {data.Mau.NgayDuyet:dd/MM/yyyy HH:mm}";
            var items=TemplatePicker.Items.OfType<LuaChonDanhMuc>().Where(i=>i.Value!=data.Mau.MaMauKH).Append(new(data.Mau.MaMauKH,$"{data.Mau.TenMau} · v{data.Mau.PhienBan} · {data.Mau.TrangThai} ({data.Mau.MaMauKH})")).ToArray();TemplatePicker.ItemsSource=items;TemplatePicker.SelectedValue=data.Mau.MaMauKH;dirty=false;UpdateRights();
        }
        finally{loading=previous;}
    }
    private void UpdateRights()
    {
        var edit=MauDangMo is null||MauDangMo.Mau.TrangThai=="Nháp";TemplateFields.IsEnabled=edit;TemplateSteps.IsReadOnly=!edit;TemplateSteps.CanUserAddRows=TemplateSteps.CanUserDeleteRows=edit;SaveTemplateButton.IsEnabled=edit;
        ApproveTemplateButton.IsEnabled=edit&&MauDangMo is not null&&actor.CoQuyen(QuyenLamViec.DuyetMauDieuTri);RetireTemplateButton.IsEnabled=MauDangMo?.Mau.TrangThai=="Đã duyệt";
    }
    private void UpdateServiceChoices()
    {
        var referenced=steps.Select(s=>s.MaDV).ToHashSet(StringComparer.Ordinal);
        TemplateServiceColumn.ItemsSource=serviceCatalog.Where(d=>d.TrangThai=="Đang hoạt động"||referenced.Contains(d.MaDV)).Select(d=>new LuaChonDanhMuc(d.MaDV,$"{d.TenDV} ({d.MaDV})"+(d.TrangThai=="Đang hoạt động"?"":" · ngừng hoạt động"))).ToArray();
    }
    private bool Run(Action action,out string error)
    {error="";TemplateError.Text="";try{action();return true;}catch(InvalidOperationException ex){error=TemplateError.Text=ex.Message;return false;}}
    private void SyncCache(){if(transaction is not null)return;try{DuLieuGiaoDien.RefreshFromDatabase();}catch(InvalidOperationException){TemplateStatus.Text+=" Đã lưu dữ liệu; tải lại sau khi kết nối ổn định.";}}
    public bool TrySaveTemplate(out string error)=>Run(()=>
    {
        ClinicalFormInput.Commit(TemplateSteps,"CacBuoc");if(!int.TryParse(VersionEditor.Text,out var version)||version<=0)throw new LoiKhamException("PhienBan","Phiên bản phải là số nguyên dương.");
        var request=new YeuCauLuuMauDieuTri {MaMauKH=MauDangMo?.Mau.MaMauKH,DauVanTay=MauDangMo?.DauVanTay,MaDMTT=Convert.ToString(CategoryPicker.SelectedValue)??"",TenMau=TemplateName.Text,MucDo=SeverityEditor.Text,PhienBan=version,CacBuoc=steps.ToArray()};
        Fill(service.LuuMauDieuTri(request,actor));TemplateStatus.Text="Đã lưu mẫu nháp; chưa được dùng làm gợi ý.";SyncCache();
    },out error);
    public bool TryApproveTemplate(out string error)=>Run(()=>
    {
        ClinicalFormInput.Commit(TemplateSteps,"CacBuoc");if(MauDangMo is null||dirty)throw new LoiKhamException("MaMauKH","Lưu mẫu nháp trước khi duyệt.");
        Fill(service.DuyetMauDieuTri(MauDangMo.Mau.MaMauKH,MauDangMo.DauVanTay,actor));TemplateStatus.Text="Đã duyệt bằng định danh bác sĩ đang đăng nhập.";SyncCache();
    },out error);
    private void Template_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(loading)return;if(dirty){loading=true;TemplatePicker.SelectedValue=MauDangMo?.Mau.MaMauKH;loading=false;TemplateError.Text="Lưu nháp hoặc tải lại trước khi đổi mẫu.";return;}
        if(TemplatePicker.SelectedValue is string id)Run(()=>Fill(service.LayMauDieuTri(id,actor)),out _);
    }
    private void AddCategory_Click(object sender,RoutedEventArgs e)=>Run(()=>
    {
        if(string.IsNullOrWhiteSpace(NewCategoryName.Text))throw new LoiKhamException("TenTinhTrang","Nhập tên tình trạng.");
        var item=new ET_DMTINHTRANGBENH {TenTinhTrang=NewCategoryName.Text,MoTa=NewCategoryDescription.Text,TrangThai="Đang áp dụng"};new BUS_DMTINHTRANGBENH(transaction).LuuNhap(item,actor);
        CategoryPicker.ItemsSource=CategoryPicker.Items.OfType<LuaChonDanhMuc>().Append(new(item.MaDMTT,item.TenTinhTrang)).ToArray();CategoryPicker.SelectedValue=item.MaDMTT;NewCategoryName.Clear();NewCategoryDescription.Clear();TemplateStatus.Text="Đã lưu danh mục tình trạng.";SyncCache();
    },out _);
    private void New_Click(object sender,RoutedEventArgs e)=>Run(()=>{if(dirty)throw new LoiKhamException("MaMauKH","Lưu hoặc tải lại để bỏ thay đổi trước khi tạo mẫu mới.");loading=true;try{ClearForm();}finally{loading=false;}},out _);
    private void Copy_Click(object sender,RoutedEventArgs e)=>Run(()=>
    {
        if(MauDangMo is null||dirty)throw new LoiKhamException("MaMauKH","Chọn mẫu đã lưu để sao chép.");var source=MauDangMo;loading=true;
        try{MauDangMo=null;TemplatePicker.SelectedIndex=-1;VersionEditor.Text=checked(source.Mau.PhienBan+1).ToString();TemplateIdentity.Text="Bản sao mới — cần lưu và duyệt lại";ReviewIdentity.Text="Chưa có bác sĩ duyệt";dirty=true;UpdateRights();}finally{loading=false;}
    },out _);
    private void Reload_Click(object sender,RoutedEventArgs e)=>Run(()=>{var id=MauDangMo?.Mau.MaMauKH;loading=true;try{NewCategoryName.Clear();NewCategoryDescription.Clear();ReloadLists();if(id is null)ClearForm();else Fill(service.LayMauDieuTri(id,actor));}finally{loading=false;}TemplateStatus.Text="Đã tải lại, bỏ thay đổi chưa lưu.";},out _);
    private void Retire_Click(object sender,RoutedEventArgs e)
    {if(MauDangMo is not null&&MessageBox.Show("Ngừng dùng mẫu cho gợi ý mới? Kế hoạch đã sao chép được giữ nguyên.","Ngừng áp dụng mẫu",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)Run(()=>{service.NgungApDungMau(MauDangMo.Mau.MaMauKH,MauDangMo.DauVanTay,actor);Fill(service.LayMauDieuTri(MauDangMo.Mau.MaMauKH,actor));TemplateStatus.Text="Đã ngừng áp dụng mẫu.";SyncCache();},out _);}
    private void Save_Click(object sender,RoutedEventArgs e)=>TrySaveTemplate(out _);
    private void Approve_Click(object sender,RoutedEventArgs e)
    {if(dirty){TryApproveTemplate(out _);return;}if(MessageBox.Show("Bác sĩ đã kiểm tra nội dung và xác nhận duyệt mẫu này?","Duyệt nội dung chuyên môn",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)TryApproveTemplate(out _);}
    private void Field_Changed(object sender,RoutedEventArgs e){if(!loading)dirty=true;}
    private void Text_Changed(object sender,TextChangedEventArgs e){if(!loading)dirty=true;}
    private void Step_Edit(object sender,DataGridCellEditEndingEventArgs e){if(!loading)dirty=true;}
    private void Back_Click(object sender,RoutedEventArgs e){if(CoThayDoiChuaLuu){TemplateError.Text="Lưu mẫu/danh mục hoặc tải lại để bỏ thay đổi trước khi quay lại.";return;}BackRequested?.Invoke();}
}
