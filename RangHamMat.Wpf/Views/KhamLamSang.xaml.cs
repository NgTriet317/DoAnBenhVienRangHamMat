using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;

public partial class KhamLamSang : UserControl,IPatientScoped
{
    private readonly BUS_KHAMDIEUTRI service;
    private readonly NguCanhNghiepVu actor;
    private readonly IDbTransaction? transaction;
    private readonly string visitId;
    private readonly ObservableCollection<TinhTrangKhamNhap> conditions=new();
    private readonly ObservableCollection<BuocKeHoachNhap> steps=new();
    private BanNhapKham snapshot=null!;
    private BanNhapKeHoach? plan;
    private bool loading=true,dirty,planDirty,historyReadOnly;
    private Func<string,bool> allowed=_=>true;
    public string? PatientId {get;private set;}
    public bool CoThayDoiChuaLuu=>dirty||planDirty;
    public event Action? BackRequested;
    public KhamLamSang(string maLS,NguCanhNghiepVu? user=null,IDbTransaction? transaction=null)
    {
        this.transaction=transaction;visitId=maLS;service=new(transaction);actor=new BUS_QUYENNGHIEPVU(transaction).LayNguCanh(user?.MaNV??PhienDangNhap.MaNV);
        InitializeComponent();ConditionGrid.ItemsSource=conditions;PlanSteps.ItemsSource=steps;
        ConditionGrid.BeginningEdit+=(_,_)=>{if(!loading)dirty=true;};
        PlanSteps.BeginningEdit+=(_,_)=>{if(!loading)planDirty=true;};
        PlanStart.AddHandler(TextBox.TextChangedEvent,new TextChangedEventHandler(PlanDate_TextChanged));
        PlanEnd.AddHandler(TextBox.TextChangedEvent,new TextChangedEventHandler(PlanDate_TextChanged));
        conditions.CollectionChanged+=(_,_)=>{if(!loading)dirty=true;};steps.CollectionChanged+=(_,_)=>{if(!loading)planDirty=true;};
        LoadWorkspace();
    }
    public void ConfigureAccess(Func<string,bool> access){allowed=access;UpdateRights();}
    public void RestrictToHistory(){historyReadOnly=true;UpdateRights();ClinicalStatus.Text="Lịch sử chỉ xem. Mở chức năng Khám điều trị để tiếp tục nhập liệu.";}
    private bool Editable=>!historyReadOnly && snapshot.DuocSua && allowed("KhamDieuTri");
    private void RequireEditable(){if(!Editable)throw new TuChoiQuyenNghiepVuException();}
    private void LoadWorkspace()
    {
        loading=true;
        try
        {
            var data=service.LayKhongGianKham(visitId,actor);PatientId=data.BenhNhan.MaBN;
            PatientHeader.Text=$"{data.BenhNhan.HoTenBN} · {PatientId} · {data.BenhNhan.NgaySinh:dd/MM/yyyy}";
            SafetyHeader.Text=$"Dị ứng: {Known(data.BenhNhan.DiUng)} · Tiền sử: {Known(data.BenhNhan.TienSuBenh)}";
            ConditionCategoryColumn.ItemsSource=data.DanhMucTinhTrang.Select(c=>new LuaChonDanhMuc(c.MaDMTT,c.TenTinhTrang+(c.TrangThai=="Đang áp dụng"?"":" (ngừng áp dụng)"))).ToArray();
            PlanServiceColumn.ItemsSource=data.DichVu.Select(d=>new LuaChonDanhMuc(d.MaDV,d.TenDV+" ("+d.MaDV+")"+(d.TrangThai=="Đang hoạt động"?"":" · ngừng hoạt động"))).ToArray();
            FillClinical(data.BanNhap);SetPlans(data.KeHoach);ClearPlan();
        }
        finally{loading=false;dirty=planDirty=false;}
    }
    private static string Known(string? value)=>string.IsNullOrWhiteSpace(value)?"chưa ghi nhận, cần đối chiếu":value;
    private void FillClinical(BanNhapKham saved)
    {
        var prior=loading;loading=true;
        try
        {
            snapshot=saved;var v=saved.LanKham;ClinicalIdentity.Text=$"Lần khám {v.MaLS} · phiếu {v.MaPhieu} · bác sĩ {v.MaBS} · {v.TrangThai}";
            SymptomsEditor.Text=v.TrieuChung;DiagnosisEditor.Text=v.ChanDoan;TreatmentEditor.Text=v.KeHoachXuLy;ClinicalNote.Text=v.GhiChu??"";
            NoPrescription.IsChecked=v.KhongKeThuoc==1;NoPrescriptionReason.Text=v.LyDoKhongKeThuoc??"";conditions.Clear();
            foreach(var t in saved.TinhTrang)conditions.Add(new(){MaTTB=t.MaTTB,MaDMTT=t.MaDMTT,LoaiTinhTrang=t.LoaiTinhTrang,MoTaTinhTrang=t.MoTaTinhTrang,MucDo=t.MucDo,ViTriRang=t.ViTriRang,GhiChu=t.GhiChu});
            PlanSourcePicker.ItemsSource=saved.TinhTrang.Select(t=>new LuaChonDanhMuc(t.MaTTB,$"{t.MoTaTinhTrang} ({t.MaTTB})")).ToArray();dirty=false;UpdateRights();
        }
        finally{loading=prior;}
    }
    private void SetPlans(IEnumerable<ET_KEHOACHDIEUTRI> plans)
    {PlanPicker.ItemsSource=plans.Select(p=>new LuaChonDanhMuc(p.MaKHDT,$"{p.MaKHDT} · {p.TrangThai} · {p.GhiChu}")).ToArray();}
    private void FillPlan(BanNhapKeHoach saved)
    {
        var prior=loading;loading=true;
        try
        {
            plan=saved;PlanIdentity.Text=$"{saved.KeHoach.MaKHDT} · {saved.KeHoach.TrangThai} · nguồn {saved.KeHoach.MaTTBNguon??"thủ công"} · mẫu {saved.KeHoach.MaMauKH??"không dùng mẫu"} v{saved.KeHoach.PhienBanMau}";
            PlanStart.SelectedDate=saved.KeHoach.NgayBatDau;PlanEnd.SelectedDate=saved.KeHoach.NgayDuKienKetThuc;PlanNote.Text=saved.KeHoach.GhiChu??"";PlanSourcePicker.SelectedValue=saved.KeHoach.MaTTBNguon;
            steps.Clear();foreach(var s in saved.CacBuoc)steps.Add(new(){MaCTKH=s.MaCTKH,MaDV=s.MaDV,LanThu=s.LanThu,SoLuongDuKien=s.SoLuongDuKien,DonGiaDuKien=s.DonGiaDuKien,NoiDungBuoc=s.NoiDungBuoc??"",GhiChu=s.GhiChu});
            var items=PlanPicker.Items.OfType<LuaChonDanhMuc>().Where(p=>p.Value!=saved.KeHoach.MaKHDT).Append(new(saved.KeHoach.MaKHDT,$"{saved.KeHoach.MaKHDT} · {saved.KeHoach.TrangThai}")).ToArray();PlanPicker.ItemsSource=items;PlanPicker.SelectedValue=saved.KeHoach.MaKHDT;planDirty=false;UpdateRights();
        }
        finally{loading=prior;}
    }
    private void ClearPlan()
    {plan=null;PlanIdentity.Text="Kế hoạch thủ công mới; chưa lưu";PlanSourcePicker.SelectedIndex=-1;PlanStart.SelectedDate=PlanEnd.SelectedDate=null;PlanNote.Clear();steps.Clear();PlanPicker.SelectedIndex=-1;planDirty=false;UpdateRights();}
    private void UpdateRights()
    {
        if(snapshot is null)return;
        var edit=Editable;ClinicalInputPanel.IsEnabled=PrescriptionDecision.IsEnabled=edit;ConditionGrid.IsReadOnly=!edit;ConditionGrid.CanUserAddRows=ConditionGrid.CanUserDeleteRows=edit;
        SaveClinicalButton.IsEnabled=CompleteClinicalButton.IsEnabled=SuggestButton.IsEnabled=ApplyTemplateButton.IsEnabled=NewPlanButton.IsEnabled=edit;
        var planEdit=edit &&(plan is null||plan.KeHoach.TrangThai=="Nháp");PlanFields.IsEnabled=planEdit;PlanSourcePicker.IsEnabled=planEdit&&plan is null;PlanSteps.IsReadOnly=!planEdit;PlanSteps.CanUserAddRows=PlanSteps.CanUserDeleteRows=planEdit;CancelPlanButton.IsEnabled=planEdit;
        SavePlanButton.IsEnabled=planEdit;ConfirmPlanButton.IsEnabled=planEdit&&plan is not null;OpenServicesButton.IsEnabled=allowed("DichVuThucHien");OpenPrescriptionButton.IsEnabled=allowed("DonThuoc");
    }
    private bool Run(Action action,out string error)
    {error="";ClinicalError.Text="";ClinicalFormInput.ClearErrors(this);try{action();return true;}catch(InvalidOperationException ex){error=ClinicalError.Text=ClinicalFormInput.Message(ex);ClinicalFormInput.ShowError(this,ex);return false;}}
    private void SyncCache()
    {
        if(transaction is not null)return;
        try{DuLieuGiaoDien.RefreshFromDatabase();}catch(InvalidOperationException){ClinicalStatus.Text+=" Đã lưu dữ liệu; tải lại khi kết nối ổn định.";}
    }
    public bool TrySave(out string error)=>Run(()=>
    {
        RequireEditable();
        ClinicalFormInput.Commit(ConditionGrid,"TinhTrang");
        var request=new YeuCauLuuKham {MaLS=visitId,DauVanTay=snapshot.DauVanTay,TrieuChung=SymptomsEditor.Text,ChanDoan=DiagnosisEditor.Text,KeHoachXuLy=TreatmentEditor.Text,GhiChu=ClinicalNote.Text,KhongKeThuoc=NoPrescription.IsChecked==true,LyDoKhongKeThuoc=NoPrescriptionReason.Text,TinhTrang=conditions.ToArray()};
        FillClinical(service.LuuKham(request,actor));ClinicalStatus.Text="Đã lưu nháp; chưa chuyển chờ thanh toán.";SyncCache();
    },out error);
    public bool TryComplete(out string error)=>Run(()=>
    {
        RequireEditable();
        ClinicalFormInput.Commit(ConditionGrid,"TinhTrang");ClinicalFormInput.Commit(PlanSteps,"CacBuoc");
        if(dirty||planDirty)throw new LoiKhamException("BanNhap","Có thay đổi chưa lưu. Lưu nháp hoặc tải lại để đối chiếu trước khi ký.");
        FillClinical(service.HoanTatKham(visitId,snapshot.DauVanTay,actor));ClinicalStatus.Text="Đã hoàn tất chuyên môn. Phiếu chờ thanh toán; nội dung đã khóa.";SyncCache();
    },out error);
    public bool TrySavePlan(out string error)=>Run(()=>
    {
        RequireEditable();
        ClinicalFormInput.Commit(PlanSteps,"CacBuoc");var request=new YeuCauLuuKeHoach {MaLS=visitId,MaKHDT=plan?.KeHoach.MaKHDT,DauVanTay=plan?.DauVanTay,MaTTBNguon=PlanSourcePicker.SelectedValue as string,NgayBatDau=ClinicalFormInput.ReadDate(PlanStart),NgayDuKienKetThuc=ClinicalFormInput.ReadDate(PlanEnd),GhiChu=PlanNote.Text,CacBuoc=steps.ToArray()};
        FillPlan(service.LuuKeHoach(request,actor));ClinicalStatus.Text="Đã lưu kế hoạch nháp; chưa ghi dịch vụ thực hiện.";SyncCache();
    },out error);
    public bool TryConfirmPlan(out string error)=>Run(()=>
    {
        RequireEditable();
        ClinicalFormInput.Commit(PlanSteps,"CacBuoc");if(plan is null||planDirty)throw new LoiKhamException("KeHoach","Lưu nháp kế hoạch trước khi duyệt.");
        FillPlan(service.XacNhanKeHoach(plan.KeHoach.MaKHDT,plan.DauVanTay,actor));ClinicalStatus.Text="Đã duyệt kế hoạch; các bước dự kiến chưa phải khoản thu.";SyncCache();
    },out error);
    private void Suggest_Click(object sender,RoutedEventArgs e)=>Run(()=>{ClinicalFormInput.Commit(ConditionGrid,"TinhTrang");if(dirty)throw new LoiKhamException("TinhTrang","Lưu nháp các tình trạng trước khi gợi ý.");var rows=service.GoiYKeHoach(visitId,actor);SuggestionPicker.ItemsSource=rows;SuggestionPreview.Text=rows.Count==0?"Chưa có mẫu đã duyệt phù hợp. Có thể lập kế hoạch thủ công.":"Chọn từng mẫu để xem nội dung trước khi áp dụng.";},out _);
    private void Suggestion_Changed(object sender,SelectionChangedEventArgs e)
    {if(SuggestionPicker.SelectedItem is GoiYKeHoachItem g)SuggestionPreview.Text=$"Nguồn {g.TinhTrang.MaTTB} · v{g.Mau.PhienBan} · bác sĩ duyệt {g.Mau.MaBSDuyet} · {g.Mau.NgayDuyet:dd/MM/yyyy HH:mm}\n"+string.Join("\n",g.CacBuoc.Select(s=>$"{s.ThuTu}. Buổi {s.LanThu}: {s.NoiDung} · lượng {s.SoLuongDuKien}"));}
    private void Apply_Click(object sender,RoutedEventArgs e)=>Run(()=>{if(dirty||planDirty)throw new LoiKhamException("BanNhap","Lưu nháp trước khi áp dụng mẫu.");if(SuggestionPicker.SelectedItem is not GoiYKeHoachItem g)throw new LoiKhamException("MaMauKH","Chọn một mẫu đã duyệt.");FillPlan(service.ApDungMau(visitId,g.TinhTrang.MaTTB,g.Mau.MaMauKH,actor));ClinicalStatus.Text="Đã mở bản kế hoạch sao chép. Kiểm tra và chỉnh trước khi duyệt.";SyncCache();},out _);
    private void Plan_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(loading)return;
        if(planDirty){loading=true;PlanPicker.SelectedValue=plan?.KeHoach.MaKHDT;loading=false;ClinicalError.Text="Lưu nháp kế hoạch hoặc tải lại trước khi chuyển kế hoạch khác.";return;}
        if(PlanPicker.SelectedValue is string id)Run(()=>FillPlan(service.LayKeHoach(id,actor)),out _);
    }
    private void NewPlan_Click(object sender,RoutedEventArgs e)=>Run(()=>{if(planDirty)throw new LoiKhamException("KeHoach","Lưu nháp hoặc tải lại trước khi lập kế hoạch khác.");loading=true;try{ClearPlan();}finally{loading=false;}},out _);
    private void Reload_Click(object sender,RoutedEventArgs e)=>Run(()=>{LoadWorkspace();ClinicalStatus.Text="Đã tải lại dữ liệu; các thay đổi chưa lưu đã bỏ.";},out _);
    private void Clinical_Changed(object sender,RoutedEventArgs e){if(!loading)dirty=true;}
    private void Condition_Edit(object sender,DataGridCellEditEndingEventArgs e){if(!loading)dirty=true;}
    private void Plan_Edit(object sender,DataGridCellEditEndingEventArgs e){if(!loading)planDirty=true;}
    private void Plan_FieldChanged(object sender,RoutedEventArgs e){if(!loading)planDirty=true;}
    private void Plan_TextChanged(object sender,TextChangedEventArgs e){if(!loading)planDirty=true;}
    private void PlanDate_TextChanged(object sender,TextChangedEventArgs e){if(!loading && sender is DatePicker picker && picker.IsKeyboardFocusWithin)planDirty=true;}
    private void Save_Click(object sender,RoutedEventArgs e)=>TrySave(out _);
    private void SavePlan_Click(object sender,RoutedEventArgs e)=>TrySavePlan(out _);
    private void CancelPlan_Click(object sender,RoutedEventArgs e)
    {
        if(MessageBox.Show("Hủy kế hoạch nháp? Bản đã lưu vẫn được giữ để đối chiếu.","Hủy kế hoạch",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
        Run(()=>{if(plan is null){loading=true;try{ClearPlan();}finally{loading=false;}}else FillPlan(service.HuyKeHoach(plan.KeHoach.MaKHDT,plan.DauVanTay,actor));ClinicalStatus.Text="Đã hủy kế hoạch nháp.";SyncCache();},out _);
    }
    private void Complete_Click(object sender,RoutedEventArgs e)
    {if(dirty||planDirty){TryComplete(out _);return;}if(MessageBox.Show("Hoàn tất chuyên môn và khóa nội dung lần khám này?","Xác nhận chuyên môn",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)TryComplete(out _);}
    private void ConfirmPlan_Click(object sender,RoutedEventArgs e)
    {if(planDirty){TryConfirmPlan(out _);return;}if(MessageBox.Show("Xác nhận kế hoạch đã được bác sĩ kiểm tra?","Duyệt kế hoạch",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)TryConfirmPlan(out _);}
    private void OpenModule(string module)=>Run(()=>{if(dirty||planDirty)throw new LoiKhamException("BanNhap","Lưu nháp trước khi chuyển chức năng.");if(allowed(module)&&PatientId is not null)DieuHuongHoSo.Open(this,module,PatientId,visitId);},out _);
    private void Services_Click(object sender,RoutedEventArgs e)=>OpenModule("DichVuThucHien");
    private void Prescription_Click(object sender,RoutedEventArgs e)=>OpenModule("DonThuoc");
    private void Back_Click(object sender,RoutedEventArgs e){if(dirty||planDirty){ClinicalError.Text="Lưu nháp hoặc tải lại để bỏ thay đổi trước khi quay lại.";return;}BackRequested?.Invoke();}
}
