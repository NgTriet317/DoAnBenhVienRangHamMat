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
    /// <summary>
    /// Cờ Đang cập nhật, Cấu hình mở, Đang thay đổi các tab; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool updating,configurationOpen,changingTabs;
    /// <summary>
    /// Lần khám trang hàng đợi trang đang hoạt động tab được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private int visitPage=1,queuePage=1,activeTab;
    /// <summary>
    /// Đang chọn mã lần khám chỉ định tường minh mã lần khám đang chọn mã phiếu tiếp nhận chỉ định tường minh mã phiếu tiếp nhận được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private string? selectedVisitId,explicitVisitId,selectedReceiptId,explicitReceiptId;
    /// <summary>
    /// Quyền cho phép thao tác được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private Func<string,bool> allowed=_=>true;
    /// <summary>
    /// Hàng đợi bộ đếm thời gian được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly DispatcherTimer queueTimer=new(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(10)};
    /// <summary>
    /// Mã bệnh nhân. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? PatientId{get;private set;}
    /// <summary>
    /// Giá trị Hàng đợi chế độ; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    private bool QueueMode=>WorkTabs?.SelectedIndex==0;
    /// <summary>
    /// Giá trị Có thay đổi chưa lưu ở trình nhập liệu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    private bool EditorDirty=>DetailHost?.Content is ChiTietKhamDieuTri d&&d.CoThayDoiChuaLuu;

    /// <summary>
    /// Khởi tạo khám điều trị. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
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
    /// <summary>
    /// Thiết lập quyền truy cập và các chức năng được mở của màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>d.ConfigureAccess</c>.</remarks>
    /// <param name="canAccess">Giá trị cần quyền truy cập dùng khi thực hiện thao tác; kiểu Func&lt;string,bool&gt;.</param>
    public void ConfigureAccess(Func<string,bool> canAccess){allowed=canAccess;if(DetailHost.Content is ChiTietKhamDieuTri d)d.ConfigureAccess(canAccess);}
    /// <summary>
    /// Áp dụng bệnh nhân được chỉ định làm phạm vi dữ liệu của màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>FocusContext</c>.</remarks>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    public void SetPatient(string id)=>FocusContext(id);
    /// <summary>
    /// Cập nhật bố cục/trạng thái màn hình theo chế độ tra cứu hoặc khám đang chọn.
    /// </summary>
    private void UpdateMode()
    {
        StatusFilterPanel.Visibility=QueueMode?Visibility.Visible:Visibility.Collapsed;
        DoctorFilterPanel.Visibility=QueueMode&&DuLieuGiaoDien.PhamViLamSang&&DuLieuGiaoDien.NguoiDungLamSang.LaAdmin?Visibility.Visible:Visibility.Collapsed;
        EmptyHeading.Text=QueueMode?"Chọn người bệnh trong hàng chờ":"Chọn một lượt đã khám";
    }
    /// <summary>
    /// Làm mới danh sách và chi tiết theo phạm vi, bộ lọc hoặc lựa chọn hiện tại.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DuLieuGiaoDien.Options</c>, <c>choices.All</c>, <c>choices.Add</c>, <c>RefreshQueue</c>.</remarks>
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
    /// <summary>
    /// Chuyển màn hình tới đúng bệnh nhân/lần khám hoặc chứng từ theo ngữ cảnh được yêu cầu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanLeaveEditor</c>, <c>QuyenNghiepVu.LayNguCanh</c>, <c>SearchBox.Clear</c>, <c>ClearDetail</c>.</remarks>
    /// <param name="patientId">Giá trị mã bệnh nhân dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <param name="visitId">Giá trị mã lần khám dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <param name="prescriptionId">Giá trị đơn thuốc mã dùng khi thực hiện thao tác; kiểu string?.</param>
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
    /// <summary>
    /// Chọn phiếu tiếp nhận được yêu cầu để mở đúng mục trong hàng chờ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanLeaveEditor</c>, <c>ClearDetail</c>, <c>UpdateMode</c>, <c>RefreshQueue</c>.</remarks>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    internal void FocusReceipt(string id)
    {
        if(!CanLeaveEditor()||!QueueTab.IsEnabled)return;
        updating=true;changingTabs=true;WorkTabs.SelectedIndex=0;activeTab=0;selectedReceiptId=explicitReceiptId=id;explicitVisitId=null;queuePage=1;ClearDetail();changingTabs=false;updating=false;UpdateMode();RefreshQueue();
    }
    /// <summary>
    /// Hiển thị lần khám được chọn và cập nhật vùng làm việc liên quan.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanLeaveEditor</c>, <c>DuLieuGiaoDien.BaoDamNguCanh</c>, <c>detail.ConfigureAccess</c>, <c>ClearDetail</c>.</remarks>
    /// <param name="visit">Giá trị lần khám dùng khi thực hiện thao tác; kiểu MucDanhSachLanKham.</param>
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
    /// <summary>
    /// Xóa vùng chi tiết khi không còn nguồn hợp lệ để hiển thị.
    /// </summary>
    private void ClearDetail(){DetailHost.Content=null;DetailEmpty.Visibility=Visibility.Visible;QueuePreview.Visibility=Visibility.Collapsed;QueueStartButton.IsEnabled=false;}
    /// <summary>
    /// Tiếp nhận thay đổi lần khám và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ShowVisit</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Visit_Changed(object sender,SelectionChangedEventArgs e){if(!updating&&VisitList.SelectedItem is MucDanhSachLanKham v){explicitVisitId=null;ShowVisit(v);}}
    /// <summary>
    /// Tiếp nhận thay đổi công việc tab và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanLeaveEditor</c>, <c>ClearDetail</c>, <c>UpdateMode</c>, <c>Refresh</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void WorkTab_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(e.Source!=WorkTabs||updating||changingTabs||VisitList is null)return;
        if(!CanLeaveEditor()){changingTabs=true;WorkTabs.SelectedIndex=activeTab;changingTabs=false;return;}
        activeTab=WorkTabs.SelectedIndex;configurationOpen=false;ClearDetail();explicitVisitId=explicitReceiptId=null;PageStatus.Text="";UpdateMode();Refresh();
    }
    /// <summary>
    /// Kiểm tra trình nhập liệu khám có cho phép rời khi người dùng đổi tab hoặc lựa chọn.
    /// </summary>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    private bool CanLeaveEditor(){if(!EditorDirty)return true;PageStatus.Text="Có thay đổi chưa lưu. Lưu nháp hoặc tải lại để bỏ thay đổi trước khi đổi người bệnh, bộ lọc hoặc tab.";return false;}
    /// <summary>
    /// Tiếp nhận thay đổi bộ lọc và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanLeaveEditor</c>, <c>Refresh</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Filter_Changed(object sender,RoutedEventArgs e){if(VisitList is null||updating||!CanLeaveEditor())return;queuePage=visitPage=1;explicitVisitId=explicitReceiptId=null;Refresh();}
    /// <summary>
    /// Tiếp nhận thay đổi bệnh nhân bộ lọc và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanLeaveEditor</c>, <c>HoTroGiaoDien.Selected</c>, <c>ClearDetail</c>, <c>Refresh</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void PatientFilter_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(updating||VisitList is null)return;
        if(!CanLeaveEditor()){updating=true;PatientFilter.SelectedValue=PatientId??"";updating=false;return;}
        PatientId=HoTroGiaoDien.Selected(PatientFilter) is {Length:>0} id?id:null;selectedReceiptId=selectedVisitId=explicitVisitId=explicitReceiptId=null;queuePage=visitPage=1;ClearDetail();Refresh();
    }
    /// <summary>
    /// Tiếp nhận thay đổi bác sĩ bộ lọc và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Filter_Changed</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void DoctorFilter_Changed(object sender,SelectionChangedEventArgs e)=>Filter_Changed(sender,e);
    /// <summary>
    /// Xử lý thao tác xóa nội dung các bộ lọc khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanLeaveEditor</c>, <c>SearchBox.Clear</c>, <c>ClearDetail</c>, <c>Refresh</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ClearFilters_Click(object sender,RoutedEventArgs e)
    {
        if(!CanLeaveEditor())return;updating=true;SearchBox.Clear();StatusFilter.SelectedIndex=0;DoctorFilter.SelectedValue="";FromDate.SelectedDate=ToDate.SelectedDate=null;PatientId=null;explicitVisitId=explicitReceiptId=selectedVisitId=selectedReceiptId=null;queuePage=visitPage=1;ClearDetail();updating=false;Refresh();
    }
    /// <summary>
    /// Xử lý thao tác tải lại khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanLeaveEditor</c>, <c>Refresh</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Reload_Click(object sender,RoutedEventArgs e){if(!CanLeaveEditor())return;Refresh();}
}
