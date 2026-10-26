using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;

public partial class KeDonThuocEditor:UserControl,IPatientScoped
{
    private readonly NguCanhNghiepVu actor;private readonly IDbTransaction? transaction;private readonly BUS_DONTHUOC service;
    private readonly ObservableCollection<DongThuocKeDon> lines=new();private KhongGianKeDon? board;private BanNhapDon? saved;private string? replacement,patientScope;private bool loading=true,dirty;
    public string? PatientId{get;private set;}public bool CoThayDoiChuaLuu=>dirty||GuidanceHost.Content is HuongDanThuocEditor c&&c.CoThayDoiChuaLuu;public FlowDocument? BanInDangMo{get;private set;}
    public KeDonThuocEditor(NguCanhNghiepVu? user=null,IDbTransaction? transaction=null)
    {
        this.transaction=transaction;service=new(transaction);actor=new BUS_QUYENNGHIEPVU(transaction).LayNguCanh(user?.MaNV??PhienDangNhap.MaNV);if(!actor.CoQuyen(QuyenLamViec.Kham))throw new TuChoiQuyenNghiepVuException();
        InitializeComponent();RxLines.ItemsSource=lines;lines.CollectionChanged+=(_,_)=>{if(!loading)dirty=true;};RxLines.BeginningEdit+=(_,_)=>{if(!loading)dirty=true;};LoadGroups();LoadVisits();Search();loading=false;UpdateRights();
    }
    private bool Editable=>saved?.DuocSua??(board?.DuocTao==true||replacement is not null);
    private void LoadVisits()=>VisitPicker.ItemsSource=service.LayLuotKeDon(actor,patientScope).Select(v=>new LuaChonDanhMuc(v.MaLS,$"{v.MaLS} · {v.HoTenBN} ({v.MaBN}) · {v.ThoiGianKham:dd/MM/yyyy}")).ToArray();
    private void LoadGroups(){GroupChoice.ItemsSource=new[]{new LuaChonDanhMuc("","Tất cả nhóm")}.Concat(new BUS_NHOMTHUOCKEDON(transaction).LayDanhSach(actor).Where(g=>g.TrangThai=="Đang áp dụng").Select(g=>new LuaChonDanhMuc(g.MaNhomThuoc,g.TenNhom))).ToArray();GroupChoice.SelectedIndex=0;}
    public void SetPatient(string id)=>FocusContext(id);
    public void FocusContext(string? patientId,string? visitId=null,string? prescriptionId=null)
    {
        if(!CanLeave())return;Run(()=>
        {
            var requested=prescriptionId is null?null:service.LayBanNhapDon(prescriptionId,actor);visitId??=requested?.Don.MaLS;
            if(requested is not null&&(requested.Don.MaLS!=visitId||patientId is not null&&requested.MaBN!=patientId))throw new TuChoiQuyenNghiepVuException();
            var next=visitId is null?null:service.LayKhongGianKeDon(visitId,actor);if(next is not null&&patientId is not null&&next.Luot.MaBN!=patientId)throw new TuChoiQuyenNghiepVuException();
            loading=true;try{patientScope=patientId;PatientId=next?.Luot.MaBN??patientId;board=next;LoadVisits();VisitPicker.SelectedValue=visitId;RefreshOrders(requested?.Don.MaDT);Fill(requested);}finally{loading=false;}
        },out _);
    }
    private void RefreshOrders(string? select=null)
    {
        PatientHeader.Text=board is null?"Chọn lượt của bác sĩ để kê đơn.":$"{board.Luot.HoTenBN} · {board.Luot.MaBN}";
        VisitHeader.Text=board is null?"":$"Lượt {board.Luot.MaLS} · bác sĩ {board.Luot.MaBS} · {board.Luot.TrangThai} · Chẩn đoán: {board.ChanDoan}";
        SafetyHeader.Text=board is null?"":$"Ngày sinh: {board.NgaySinh?.ToString("dd/MM/yyyy")??"chưa ghi nhận"} · Tuổi: {Age(board.NgaySinh)}\nDị ứng: {Known(board.DiUng)} · Tiền sử: {Known(board.TienSuBenh)}. Đối chiếu trước khi ký.";
        OrderChoice.ItemsSource=board?.CacDon.Select(d=>new LuaChonDanhMuc(d.MaDT,$"{d.MaDT} · {d.TrangThai} · {d.NgayKe:dd/MM/yyyy HH:mm}"));OrderChoice.SelectedValue=select;
    }
    private void Fill(BanNhapDon? s)
    {
        var prior=loading;loading=true;
        try
        {
            saved=s;replacement=s?.Don.MaDTThayThe;lines.Clear();
            if(s is not null)foreach(var d in s.CacDong)lines.Add(new(){MaCTDT=d.MaCTDT,MaThuoc=d.MaThuoc,TenHienThi=Label(d),LieuDung=d.LieuDung,DuongDung=d.DuongDung??"",HuongDan=d.HuongDan,SoNgayDung=d.SoNgayDung,SoLuong=d.SoLuong,PhienBanHuongDan=d.PhienBanHuongDan,DaXacNhanPhuHop=d.PhienBanHuongDan is not null});
            ReplacementReason.Text=s?.Don.LyDoThayThe??"";NoteEditor.Text=s?.Don.GhiChu??"";OrderChoice.SelectedValue=s?.Don.MaDT;
            OrderHeader.Text=s is null?"Đơn mới — chưa lưu":$"{s.Don.MaDT} · {s.Don.TrangThai} · thay {replacement??"không có"}";dirty=false;UpdateRights();
        }
        finally{loading=prior;}
    }
    private string Label(ET_CHITIETDONTHUOC d)
    {if(!string.IsNullOrWhiteSpace(d.TenThuocChot))return $"{d.TenThuocChot} · {d.HamLuongChot} · {d.DonViTinhChot} ({d.MaThuoc})";var drug=service.TimThuoc(new(TuKhoa:d.MaThuoc),actor).FirstOrDefault(t=>t.MaThuoc==d.MaThuoc);return drug is null?d.MaThuoc+" (ngừng hoạt động)":$"{drug.TenThuoc} · {drug.HamLuong} · {drug.DonViTinh} ({drug.MaThuoc})";}
    private void UpdateRights(){var edit=board is not null&&Editable;RxLines.IsReadOnly=!edit;RxLines.CanUserDeleteRows=edit;AddSelectedButton.IsEnabled=ApplyGuidanceButton.IsEnabled=UnlinkButton.IsEnabled=SaveButton.IsEnabled=edit;ReplacementReason.IsEnabled=edit&&replacement is not null;NoteEditor.IsEnabled=edit;SignButton.IsEnabled=edit&&saved is not null;NewButton.IsEnabled=board?.DuocTao==true;ReplaceButton.IsEnabled=saved?.DuocThay==true;CancelButton.IsEnabled=edit&&saved is not null;PrintButton.IsEnabled=saved?.Don.TrangThai=="Đã xác nhận";}
    private bool Run(Action a,out string error){error="";ErrorText.Text="";try{a();return true;}catch(InvalidOperationException ex){error=ErrorText.Text=ex.Message;return false;}}
    private bool CanLeave(){if(CoThayDoiChuaLuu)ErrorText.Text="Lưu nháp hoặc tải lại để bỏ thay đổi trước khi chuyển nguồn.";return !CoThayDoiChuaLuu;}
    private void Search()=>DrugGrid.ItemsSource=service.TimThuoc(new(GroupChoice.SelectedValue is string g&&g.Length>0?g:null,DrugSearch.Text,FormFilter.Text,StrengthFilter.Text),actor);
    public bool TrySave(out string error)=>Run(()=>
    {if(board is null||!Editable)throw new TuChoiQuyenNghiepVuException();ClinicalFormInput.Commit(RxLines,"CacDong");var r=new YeuCauLuuDon{MaDT=saved?.Don.MaDT,MaLS=board.Luot.MaLS,MaDTThayThe=replacement,LyDoThayThe=ReplacementReason.Text,GhiChu=NoteEditor.Text,DauVanTay=saved?.DauVanTay,CacDong=lines.ToArray()};AfterWrite(service.LuuDon(r,actor),"Đã lưu nháp; chưa ký và không trừ kho.");},out error);
    public bool TrySign(out string error)=>Run(()=>{ClinicalFormInput.Commit(RxLines,"CacDong");if(saved is null||!saved.DuocSua)throw new TuChoiQuyenNghiepVuException();if(dirty)throw new LoiKhamException("BanNhap","Lưu mọi thay đổi trước khi ký.");AfterWrite(service.XacNhanDon(saved.Don.MaDT,saved.DauVanTay,actor),"Đã ký và khóa chỉ định; tồn kho không thay đổi.");},out error);
    public bool TryCancel(out string error)=>Run(()=>{if(saved is null||!saved.DuocSua)throw new TuChoiQuyenNghiepVuException();AfterWrite(service.HuyDonNhap(saved.Don.MaDT,saved.DauVanTay,actor),"Đã hủy nháp; đơn nguồn giữ nguyên hiệu lực.");},out error);
    public bool TryPreparePrint(out string error)=>Run(()=>{if(saved is null)throw new LoiKhamException("MaDT","Chọn bản đã ký.");BanInDangMo=BuildPrint(service.LayBanInDon(saved.Don.MaDT,actor));},out error);
    public Func<BanInDonThuoc> TaoNguonInCoDinh(){var id=saved?.Don.MaDT??throw new LoiKhamException("MaDT","Chọn bản đã ký.");service.LayBanInDon(id,actor);return ()=>service.LayBanInDon(id,actor);}
    public bool TryApplyGuidance(bool confirmed,out string error)=>Run(()=>
    {if(!Editable||RxLines.SelectedItem is not DongThuocKeDon row)throw new LoiKhamException("MaThuoc","Chọn một dòng thuốc đang soạn.");ClinicalFormInput.Commit(RxLines,"CacDong");var c=service.TimThuoc(new(TuKhoa:row.MaThuoc),actor).FirstOrDefault(d=>d.MaThuoc==row.MaThuoc)?.HuongDan;if(c is null||!confirmed)throw new LoiKhamException("HuongDan","Cần hướng dẫn đã duyệt và bác sĩ xác nhận phù hợp đối tượng.");row.LieuDung=c.LieuDungGoiY??"";row.DuongDung=c.DuongDung??"";row.HuongDan=c.HuongDanGoiY??"";row.PhienBanHuongDan=c.PhienBan;row.DaXacNhanPhuHop=true;RxLines.Items.Refresh();dirty=true;},out error);
    private void AfterWrite(BanNhapDon s,string message)
    {Fill(s);StatusText.Text=message;loading=true;try{board=service.LayKhongGianKeDon(s.Don.MaLS,actor);RefreshOrders(s.Don.MaDT);}catch(InvalidOperationException){StatusText.Text+=$" Đã lưu {s.Don.MaDT}; tải lại danh sách khi kết nối ổn định.";}finally{loading=false;}UpdateRights();if(transaction is null)try{DuLieuGiaoDien.RefreshFromDatabase();}catch(InvalidOperationException){StatusText.Text+=" Đã lưu dữ liệu; tải lại màn hình khi kết nối ổn định.";}}
    private void Add_Click(object sender,RoutedEventArgs e)=>Run(()=>
    {if(!Editable)throw new TuChoiQuyenNghiepVuException();ClinicalFormInput.Commit(RxLines,"CacDong");foreach(var drug in DrugGrid.SelectedItems.Cast<ThuocKeDon>().ToArray()){var existing=lines.FirstOrDefault(r=>r.MaThuoc==drug.MaThuoc);if(existing is not null){RxLines.SelectedItem=existing;RxLines.ScrollIntoView(existing);StatusText.Text="Thuốc đã có; chọn dòng hiện tại để chỉnh.";continue;}lines.Add(new(){MaThuoc=drug.MaThuoc,TenHienThi=$"{drug.TenThuoc} · {drug.HamLuong} · {drug.DonViTinh} ({drug.MaThuoc})"});}},out _);
    private void New_Click(object sender,RoutedEventArgs e){if(CanLeave()&&board?.DuocTao==true)Fill(null);}
    private void Replace_Click(object sender,RoutedEventArgs e)=>Run(()=>{if(!CanLeave()||saved?.DuocThay!=true)return;var old=saved;Fill(old);loading=true;try{saved=null;replacement=old.Don.MaDT;foreach(var row in lines)row.MaCTDT=null;ReplacementReason.Clear();NoteEditor.Clear();OrderChoice.SelectedIndex=-1;OrderHeader.Text=$"Thay đơn {replacement} — đơn cũ vẫn hiệu lực tới khi ký mới";dirty=true;UpdateRights();}finally{loading=false;}},out _);
    private void Order_Changed(object sender,SelectionChangedEventArgs e){if(loading)return;if(!CanLeave()){loading=true;OrderChoice.SelectedValue=saved?.Don.MaDT;loading=false;return;}if(OrderChoice.SelectedValue is string id&&!Run(()=>Fill(service.LayBanNhapDon(id,actor)),out _)){loading=true;OrderChoice.SelectedValue=saved?.Don.MaDT;loading=false;}}
    private void Visit_Changed(object sender,SelectionChangedEventArgs e){if(loading)return;if(!CanLeave()){loading=true;VisitPicker.SelectedValue=board?.Luot.MaLS;loading=false;return;}if(VisitPicker.SelectedValue is string id){FocusContext(patientScope,id);loading=true;VisitPicker.SelectedValue=board?.Luot.MaLS;loading=false;}}
    private void Reload_Click(object sender,RoutedEventArgs e)=>Run(()=>{if(board is null)return;var id=saved?.Don.MaDT??replacement;loading=true;try{board=service.LayKhongGianKeDon(board.Luot.MaLS,actor);RefreshOrders(id);Fill(id is null?null:service.LayBanNhapDon(id,actor));}finally{loading=false;}StatusText.Text="Đã tải lại và bỏ thay đổi chưa lưu.";},out _);
    private void Search_Click(object sender,RoutedEventArgs e)=>Run(Search,out _);
    private void Input_Changed(object sender,TextChangedEventArgs e){if(!loading)dirty=true;}
    private void Line_Edit(object sender,DataGridCellEditEndingEventArgs e){if(!loading)dirty=true;}
    private void Save_Click(object sender,RoutedEventArgs e)=>TrySave(out _);
    private void Sign_Click(object sender,RoutedEventArgs e){if(dirty){TrySign(out _);return;}if(MessageBox.Show("Bác sĩ đã kiểm tra toàn bộ chỉ định và xác nhận ký đơn?","Ký đơn",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)TrySign(out _);}
    private void Cancel_Click(object sender,RoutedEventArgs e){if(MessageBox.Show("Hủy bản nháp đang soạn?","Hủy đơn",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)TryCancel(out _);}
    private void ApplyGuidance_Click(object sender,RoutedEventArgs e)=>Run(()=>{if(RxLines.SelectedItem is not DongThuocKeDon r)throw new LoiKhamException("MaThuoc","Chọn dòng thuốc.");var c=service.TimThuoc(new(TuKhoa:r.MaThuoc),actor).FirstOrDefault(d=>d.MaThuoc==r.MaThuoc)?.HuongDan;if(c is null)throw new LoiKhamException("HuongDan","Chưa có hướng dẫn đã duyệt; nhập thủ công.");var yes=MessageBox.Show($"Đối tượng: {c.DoiTuongApDung}\nĐường dùng: {c.DuongDung}\nLiều: {c.LieuDungGoiY}\nHướng dẫn: {c.HuongDanGoiY}\nPhiên bản {c.PhienBan} · người duyệt {c.MaNguoiDuyet}\nBác sĩ xác nhận phù hợp người bệnh hiện tại?","Kiểm tra hướng dẫn",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes;TryApplyGuidance(yes,out _);},out _);
    private void Unlink_Click(object sender,RoutedEventArgs e){if(Editable&&RxLines.SelectedItem is DongThuocKeDon r){r.PhienBanHuongDan=null;r.DaXacNhanPhuHop=false;dirty=true;RxLines.Items.Refresh();}}
    private void Guidance_Click(object sender,RoutedEventArgs e){if(!CanLeave())return;Run(()=>{var c=new HuongDanThuocEditor(actor,transaction);c.BackRequested+=()=>{GuidanceHost.Content=null;GuidanceHost.Visibility=Visibility.Collapsed;MainSurface.Visibility=Visibility.Visible;LoadGroups();Search();};GuidanceHost.Content=c;GuidanceHost.Visibility=Visibility.Visible;MainSurface.Visibility=Visibility.Collapsed;},out _);}
    private static FlowDocument BuildPrint(BanInDonThuoc p)
        =>DonThuocDocument.Tao(p);
    private void Print_Click(object sender,RoutedEventArgs e)
    {
        Run(()=>new XemTruocDonThuoc(TaoNguonInCoDinh()){Owner=Window.GetWindow(this)}.Show(),out _);
    }
    private static string Known(string? text)=>string.IsNullOrWhiteSpace(text)?"chưa ghi nhận, cần đối chiếu":text;
    private static string Age(DateTime? dob){if(dob is not DateTime birth)return "chưa rõ";if(birth.Date>DateTime.Today)return "ngày sinh cần đối chiếu";var y=DateTime.Today.Year-birth.Year;if(birth.Date.AddYears(y)>DateTime.Today)y--;return y+" tuổi";}
}
