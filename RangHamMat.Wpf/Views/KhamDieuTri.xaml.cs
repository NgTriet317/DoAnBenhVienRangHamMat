using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Views;

/// <summary>Hàng tiếp nhận của bác sĩ là công việc chính; lịch sử đã khám ở tab riêng.</summary>
public partial class KhamDieuTri : UserControl,IPatientScoped
{
    private bool updating,configurationOpen,changingTabs;
    private int visitPage=1,queuePage=1,activeTab;
    private string? selectedVisitId,explicitVisitId,selectedReceiptId,explicitReceiptId;
    private Func<string,bool> allowed=_=>true;
    private readonly DispatcherTimer queueTimer=new(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(10)};
    public string? PatientId{get;private set;}
    private bool QueueMode=>WorkTabs?.SelectedIndex==0;
    private bool EditorDirty=>DetailHost?.Content is ChiTietKhamDieuTri d&&d.CoThayDoiChuaLuu;

    public KhamDieuTri()
    {
        updating=true;InitializeComponent();
        if(DuLieuGiaoDien.PhamViLamSang)
        {
            var actor=QuyenNghiepVu.LayNguCanh();
            QueueTab.IsEnabled=actor.CoQuyen(QuyenLamViec.Kham);
            if(!QueueTab.IsEnabled)WorkTabs.SelectedIndex=1;
            DoctorFilter.ItemsSource=new[]{new LuaChonDanhMuc("","Tất cả bác sĩ")}.Concat(DuLieuGiaoDien.Rows("NHAN_VIEN").Where(r=>UiRecord.Text(r,"MaChucVu")=="CV002").Select(r=>new LuaChonDanhMuc(UiRecord.Text(r,"MaNV"),UiRecord.Text(r,"HoTen")))).ToArray();
            DoctorFilter.SelectedValue="";
            ScopeText.Text=actor.LaAdmin?"Theo dõi hàng tiếp nhận của tất cả bác sĩ; có thể lọc bác sĩ nhận khám.":$"Hàng tiếp nhận của bác sĩ {DuLieuGiaoDien.Lookup("MaBS",actor.MaNV)} ({actor.MaNV}).";
        }
        else{QueueTab.IsEnabled=false;WorkTabs.SelectedIndex=1;}
        activeTab=WorkTabs.SelectedIndex;updating=false;UpdateMode();
        HoTroGiaoDien.Observe(this,Refresh);
        queueTimer.Tick+=(_,_)=>{if(QueueMode&&!EditorDirty&&!configurationOpen)RefreshQueue();};
        Loaded+=(_,_)=>queueTimer.Start();Unloaded+=(_,_)=>queueTimer.Stop();Refresh();
    }
    public void ConfigureAccess(Func<string,bool> canAccess){allowed=canAccess;if(DetailHost.Content is ChiTietKhamDieuTri d)d.ConfigureAccess(canAccess);}
    public void SetPatient(string id)=>FocusContext(id);
    private void UpdateMode()
    {
        StatusFilterPanel.Visibility=QueueMode?Visibility.Visible:Visibility.Collapsed;
        DoctorFilterPanel.Visibility=QueueMode&&DuLieuGiaoDien.PhamViLamSang&&DuLieuGiaoDien.NguoiDungLamSang.LaAdmin?Visibility.Visible:Visibility.Collapsed;
        EmptyHeading.Text=QueueMode?"Chọn người bệnh trong hàng chờ":"Chọn một lượt đã khám";
    }
    private void Refresh()
    {
        if(VisitList is null||updating||configurationOpen||EditorDirty)return;
        updating=true;
        try
        {
            var choices=DuLieuGiaoDien.Options("BENH_NHAN",all:true).ToList();
            if(PatientId is not null&&choices.All(x=>x.Value!=PatientId))choices.Add(new(PatientId,"Bệnh nhân "+PatientId));
            PatientFilter.ItemsSource=choices;PatientFilter.SelectedValue=PatientId??"";
            if(QueueMode)RefreshQueue();
            else if(DuLieuGiaoDien.PhamViLamSang)RefreshSqlVisits();
            else
            {
                var rows=DuLieuGiaoDien.Rows("LICH_SU_KHAM_BENH").Where(r=>UiRecord.Text(r,"TrangThai")=="Đã xác nhận"&&HoTroGiaoDien.Matches("LICH_SU_KHAM_BENH",r,SearchBox.Text,"Tất cả",PatientId)).Select(r=>new MucDanhSachLanKham(r)).ToArray();
                VisitList.ItemsSource=rows;RecordCount.Text=$"{rows.Length} lượt đã khám";ListEmpty.Visibility=rows.Length==0?Visibility.Visible:Visibility.Collapsed;
            }
        }
        finally{updating=false;}
    }
    public void FocusContext(string? patientId,string? visitId=null,string? prescriptionId=null)
    {
        if(!CanLeaveEditor())return;
        try
        {
            var context=DuLieuGiaoDien.PhamViLamSang?new BUS_KHAMDIEUTRI().XacMinhNguCanh(new(){MaBN=patientId,MaLS=visitId,MaDT=prescriptionId},QuyenNghiepVu.LayNguCanh()):new NguCanhHoSo{MaBN=patientId,MaLS=visitId};
            var visit=context.MaLS is null||!DuLieuGiaoDien.PhamViLamSang?null:new BUS_KHAMDIEUTRI().LayChiTiet(context.MaLS,QuyenNghiepVu.LayNguCanh());
            updating=true;changingTabs=true;
            PatientId=context.MaBN;selectedVisitId=explicitVisitId=visit?.LanKham.MaLS??visitId;
            selectedReceiptId=explicitReceiptId=visit?.LanKham.TrangThai=="Nháp"?context.MaPhieu:null;
            SearchBox.Clear();StatusFilter.SelectedIndex=0;FromDate.SelectedDate=ToDate.SelectedDate=null;visitPage=queuePage=1;
            WorkTabs.SelectedIndex=visit?.LanKham.TrangThai=="Đã xác nhận"||!QueueTab.IsEnabled?1:0;activeTab=WorkTabs.SelectedIndex;
            ClearDetail();configurationOpen=false;changingTabs=false;updating=false;UpdateMode();Refresh();
            if(visit is not null&&QueueMode)ShowVisit(new MucDanhSachLanKham(visit.LanKham));
        }
        catch(InvalidOperationException ex){PageStatus.Text=ClinicalFormInput.Message(ex);}
        finally{changingTabs=false;updating=false;}
    }
    internal void FocusReceipt(string id)
    {
        if(!CanLeaveEditor()||!QueueTab.IsEnabled)return;
        updating=true;changingTabs=true;WorkTabs.SelectedIndex=0;activeTab=0;selectedReceiptId=explicitReceiptId=id;explicitVisitId=null;queuePage=1;ClearDetail();changingTabs=false;updating=false;UpdateMode();RefreshQueue();
    }
    private void ShowVisit(MucDanhSachLanKham visit)
    {
        if(PatientId is not null&&visit.PatientId!=PatientId)return;
        if(!CanLeaveEditor())return;
        selectedVisitId=visit.Code;configurationOpen=false;
        var prior=updating;updating=true;
        try
        {
            if(DuLieuGiaoDien.PhamViLamSang)DuLieuGiaoDien.BaoDamNguCanh(new(){MaBN=visit.PatientId,MaLS=visit.Code});
            var detail=new ChiTietKhamDieuTri(visit.Record,historyReadOnly:!QueueMode);detail.ConfigureAccess(allowed);
            detail.BackRequested+=()=>{if(!CanLeaveEditor())return;ClearDetail();selectedVisitId=explicitVisitId=null;Refresh();};
            DetailHost.Content=detail;DetailEmpty.Visibility=QueuePreview.Visibility=Visibility.Collapsed;
        }
        catch(InvalidOperationException ex){ClearDetail();PageStatus.Text=ClinicalFormInput.Message(ex);}
        finally{updating=prior;}
    }
    private void ClearDetail(){DetailHost.Content=null;DetailEmpty.Visibility=Visibility.Visible;QueuePreview.Visibility=Visibility.Collapsed;QueueStartButton.IsEnabled=false;}
    private void Visit_Changed(object sender,SelectionChangedEventArgs e){if(!updating&&VisitList.SelectedItem is MucDanhSachLanKham v){explicitVisitId=null;ShowVisit(v);}}
    private void WorkTab_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(e.Source!=WorkTabs||updating||changingTabs||VisitList is null)return;
        if(!CanLeaveEditor()){changingTabs=true;WorkTabs.SelectedIndex=activeTab;changingTabs=false;return;}
        activeTab=WorkTabs.SelectedIndex;configurationOpen=false;ClearDetail();explicitVisitId=explicitReceiptId=null;PageStatus.Text="";UpdateMode();Refresh();
    }
    private bool CanLeaveEditor(){if(!EditorDirty)return true;PageStatus.Text="Có thay đổi chưa lưu. Lưu nháp hoặc tải lại để bỏ thay đổi trước khi đổi người bệnh, bộ lọc hoặc tab.";return false;}
    private void Filter_Changed(object sender,RoutedEventArgs e){if(VisitList is null||updating||!CanLeaveEditor())return;queuePage=visitPage=1;explicitVisitId=explicitReceiptId=null;Refresh();}
    private void PatientFilter_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(updating||VisitList is null)return;
        if(!CanLeaveEditor()){updating=true;PatientFilter.SelectedValue=PatientId??"";updating=false;return;}
        PatientId=HoTroGiaoDien.Selected(PatientFilter) is {Length:>0} id?id:null;selectedReceiptId=selectedVisitId=explicitVisitId=explicitReceiptId=null;queuePage=visitPage=1;ClearDetail();Refresh();
    }
    private void DoctorFilter_Changed(object sender,SelectionChangedEventArgs e)=>Filter_Changed(sender,e);
    private void ClearFilters_Click(object sender,RoutedEventArgs e)
    {
        if(!CanLeaveEditor())return;updating=true;SearchBox.Clear();StatusFilter.SelectedIndex=0;DoctorFilter.SelectedValue="";FromDate.SelectedDate=ToDate.SelectedDate=null;PatientId=null;explicitVisitId=explicitReceiptId=selectedVisitId=selectedReceiptId=null;queuePage=visitPage=1;ClearDetail();updating=false;Refresh();
    }
    private void Reload_Click(object sender,RoutedEventArgs e){if(!CanLeaveEditor())return;Refresh();}
}
