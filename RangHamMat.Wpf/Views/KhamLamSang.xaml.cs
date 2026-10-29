using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;

/// <summary>
/// Phần xử lý khám lâm sàng của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class KhamLamSang : UserControl,IPatientScoped
{
    /// <summary>
    /// Đối tượng nghiệp vụ khám điều trị được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private readonly BUS_KHAMDIEUTRI service;
    /// <summary>
    /// Ngữ cảnh định danh và quyền của người dùng đang thao tác.
    /// </summary>
    private readonly NguCanhNghiepVu actor;
    /// <summary>
    /// Giao dịch được giữ để các thao tác liên quan dùng cùng một phạm vi commit/rollback.
    /// </summary>
    private readonly IDbTransaction? transaction;
    /// <summary>
    /// Mã lần khám được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly string visitId;
    /// <summary>
    /// Các tình trạng được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly ObservableCollection<TinhTrangKhamNhap> conditions=new();
    /// <summary>
    /// Các bước được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly ObservableCollection<BuocKeHoachNhap> steps=new();
    /// <summary>
    /// Bản chụp dữ liệu được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private BanNhapKham snapshot=null!;
    /// <summary>
    /// Kế hoạch được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private BanNhapKeHoach? plan;
    /// <summary>
    /// Cờ Cờ đang điền dữ liệu, dùng tránh xử lý lại sự kiện phát sinh do cập nhật giao diện, Cờ có nội dung thay đổi chưa lưu, Kế hoạch có thay đổi chưa lưu, Cờ mở màn hình ở chế độ chỉ xem lịch sử; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool loading=true,dirty,planDirty,historyReadOnly;
    /// <summary>
    /// Quyền cho phép thao tác được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private Func<string,bool> allowed=_=>true;
    /// <summary>
    /// Mã bệnh nhân. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? PatientId {get;private set;}
    /// <summary>
    /// Giá trị Có thay đổi chưa lưu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public bool CoThayDoiChuaLuu=>dirty||planDirty||conditionInputDirty||prescriptionEditor?.CoThayDoiChuaLuu==true;
    /// <summary>
    /// Thông báo quay lại được yêu cầu cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    public event Action? BackRequested;
    /// <summary>
    /// Khởi tạo khám lâm sàng. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    public KhamLamSang(string maLS,NguCanhNghiepVu? user=null,IDbTransaction? transaction=null)
    {
        this.transaction=transaction;visitId=maLS;service=new(transaction);actor=new BUS_QUYENNGHIEPVU(transaction).LayNguCanh(user?.MaNV??PhienDangNhap.MaNV);
        InitializeComponent();ConditionList.ItemsSource=conditions;PlanSteps.ItemsSource=steps;
        Loaded+=(_,_)=>{PlanSteps.FrozenColumnCount=0;PlanSteps.EnableColumnVirtualization=false;};
        PlanSteps.BeginningEdit+=(_,_)=>{if(!loading)planDirty=true;};
        PlanStart.AddHandler(TextBox.TextChangedEvent,new TextChangedEventHandler(PlanDate_TextChanged));
        PlanEnd.AddHandler(TextBox.TextChangedEvent,new TextChangedEventHandler(PlanDate_TextChanged));
        conditions.CollectionChanged+=(_,_)=>{if(!loading)dirty=true;};steps.CollectionChanged+=(_,e)=>{if(!loading)planDirty=true;if(e.NewItems is not null)foreach(BuocKeHoachNhap row in e.NewItems)row.PropertyChanged+=(_,_)=>UpdatePlanTotal();UpdatePlanTotal();};
        LoadWorkspace();
    }
    /// <summary>
    /// Thiết lập quyền truy cập và các chức năng được mở của màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>UpdateRights</c>.</remarks>
    /// <param name="access">Giá trị quyền truy cập dùng khi thực hiện thao tác; kiểu Func&lt;string,bool&gt;.</param>
    public void ConfigureAccess(Func<string,bool> access){allowed=access;UpdateRights();}
    /// <summary>
    /// Đưa màn hình về phạm vi tra cứu lịch sử và cập nhật quyền thao tác tương ứng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>.RestrictToHistory</c>, <c>UpdateRights</c>.</remarks>
    public void RestrictToHistory(){historyReadOnly=true;prescriptionEditor?.RestrictToHistory();UpdateRights();ClinicalStatus.Text="Lịch sử chỉ xem. Mở chức năng Khám điều trị để tiếp tục nhập liệu.";}
    /// <summary>
    /// Giá trị Được sửa; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    private bool Editable=>!historyReadOnly && snapshot.DuocSua && allowed("KhamDieuTri");
    /// <summary>
    /// Yêu cầu bản dữ liệu hiện tại cho phép sửa trước khi áp dụng thay đổi.
    /// </summary>
    private void RequireEditable(){if(!Editable)throw new TuChoiQuyenNghiepVuException();}
    /// <summary>
    /// Nạp dữ liệu cho không gian khám hiện tại và đồng bộ các vùng hiển thị.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>service.LayKhongGianKham</c>, <c>Known</c>, <c>data.DichVu.Select</c>, <c>ConfigureConditionPicker</c>.</remarks>
    private void LoadWorkspace()
    {
        loading=true;
        try
        {
            var data=service.LayKhongGianKham(visitId,actor);PatientId=data.BenhNhan.MaBN;
            PatientHeader.Text=$"{data.BenhNhan.HoTenBN} · {PatientId} · {data.BenhNhan.NgaySinh:dd/MM/yyyy}";
            SafetyHeader.Text=$"Dị ứng: {Known(data.BenhNhan.DiUng)} · Tiền sử: {Known(data.BenhNhan.TienSuBenh)}";
            PlanServiceColumn.ItemsSource=data.DichVu.Select(d=>new LuaChonDanhMuc(d.MaDV,d.TenDV+(d.TrangThai=="Đang hoạt động"?"":" · ngừng hoạt động"))).ToArray();
            relatedServices=data.DichVuTheoTinhTrang;serviceCatalog=data.DichVu;referenceConditions=data.DinhHuong;ConfigureConditionPicker();
            FillClinical(data.BanNhap);SetPlans(data.KeHoach);ClearPlan();FillCompletionOverview(data);
        }
        finally{loading=false;dirty=planDirty=false;}
    }
    /// <summary>
    /// Kiểm tra hoặc lấy thông tin đã xác định của dữ liệu hiện tại.
    /// </summary>
    /// <param name="value">Giá trị string được truyền vào để đọc, kiểm tra hoặc cập nhật trong thao tác này.</param>
    /// <returns>Chuỗi kết quả của thao tác đã biết.</returns>
    private static string Known(string? value)=>string.IsNullOrWhiteSpace(value)?"chưa ghi nhận, cần đối chiếu":value;
    /// <summary>
    /// Điền nội dung bản khám đã đọc vào các trường nhập và danh sách tình trạng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>conditions.Clear</c>, <c>conditions.Add</c>, <c>ClearConditionInput</c>, <c>UpdateRights</c>.</remarks>
    /// <param name="saved">Giá trị đã lưu dùng khi thực hiện thao tác; kiểu BanNhapKham.</param>
    private void FillClinical(BanNhapKham saved)
    {
        var prior=loading;loading=true;
        try
        {
            snapshot=saved;var v=saved.LanKham;ClinicalIdentity.Text=$"Lần khám {v.MaLS} · phiếu {v.MaPhieu} · bác sĩ {v.MaBS} · {v.TrangThai}";
            SymptomsEditor.Text=v.TrieuChung;DiagnosisEditor.Text=v.ChanDoan;TreatmentEditor.Text=v.KeHoachXuLy;ClinicalNote.Text=v.GhiChu??"";
            NoPrescription.IsChecked=v.KhongKeThuoc==1;NoPrescriptionReason.Text=v.LyDoKhongKeThuoc??"";conditions.Clear();
            foreach(var t in saved.TinhTrang)conditions.Add(new(){MaTTB=t.MaTTB,LoaiTinhTrang=t.LoaiTinhTrang,MoTaTinhTrang=t.MoTaTinhTrang,GhiChu=t.GhiChu});
            ClearConditionInput();dirty=false;UpdateRights();
        }
        finally{loading=prior;}
    }
    /// <summary>
    /// Đặt nguồn danh sách kế hoạch và khôi phục lựa chọn phù hợp.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>plans.Select</c>.</remarks>
    /// <param name="plans">Giá trị các kế hoạch dùng khi thực hiện thao tác; kiểu IEnumerable&lt;ET_KEHOACHDIEUTRI&gt;.</param>
    private void SetPlans(IEnumerable<ET_KEHOACHDIEUTRI> plans)
    {PlanPicker.ItemsSource=plans.Select(p=>new LuaChonDanhMuc(p.MaKHDT,$"{p.MaKHDT} · {p.TrangThai} · {p.GhiChu}")).ToArray();}
    /// <summary>
    /// Điền đầu kế hoạch và các bước vào vùng chỉnh kế hoạch.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>steps.Clear</c>, <c>steps.Add</c>, <c>PlanPicker.Items.OfType&lt;LuaChonDanhMuc&gt;</c>, <c>UpdateRights</c>.</remarks>
    /// <param name="saved">Giá trị đã lưu dùng khi thực hiện thao tác; kiểu BanNhapKeHoach.</param>
    private void FillPlan(BanNhapKeHoach saved)
    {
        var prior=loading;loading=true;
        try
        {
            plan=saved;planSourceCondition=saved.KeHoach.MaTTBNguon;PlanIdentity.Text=$"{saved.KeHoach.MaKHDT} · {saved.KeHoach.TrangThai}";
            PlanStart.SelectedDate=saved.KeHoach.NgayBatDau;PlanEnd.SelectedDate=saved.KeHoach.NgayDuKienKetThuc;PlanNote.Text=saved.KeHoach.GhiChu??"";
            steps.Clear();foreach(var s in saved.CacBuoc)steps.Add(new(){MaCTKH=s.MaCTKH,MaDV=s.MaDV,LanThu=s.LanThu,SoLuongDuKien=s.SoLuongDuKien,DonGiaDuKien=s.DonGiaDuKien,NoiDungBuoc=s.NoiDungBuoc??"",GhiChu=s.GhiChu});
            var items=PlanPicker.Items.OfType<LuaChonDanhMuc>().Where(p=>p.Value!=saved.KeHoach.MaKHDT).Append(new(saved.KeHoach.MaKHDT,$"{saved.KeHoach.MaKHDT} · {saved.KeHoach.TrangThai}")).ToArray();PlanPicker.ItemsSource=items;PlanPicker.SelectedValue=saved.KeHoach.MaKHDT;planDirty=false;UpdateRights();
        }
        finally{loading=prior;}
    }
    /// <summary>
    /// Đặt vùng chỉnh kế hoạch về trạng thái chưa chọn hoặc tạo mới.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>PlanNote.Clear</c>, <c>steps.Clear</c>, <c>UpdateRights</c>.</remarks>
    private void ClearPlan()
    {plan=null;planSourceCondition=null;PlanIdentity.Text="Kế hoạch mới · chưa lưu";PlanStart.SelectedDate=PlanEnd.SelectedDate=null;PlanNote.Clear();steps.Clear();PlanPicker.SelectedIndex=-1;planDirty=false;UpdateRights();}
    /// <summary>
    /// Cập nhật trạng thái cho phép thao tác theo quyền và dữ liệu đang hiển thị.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>.RestrictToHistory</c>, <c>UpdateConditionRights</c>, <c>allowed</c>, <c>RefreshRecommendation</c>.</remarks>
    private void UpdateRights()
    {
        if(snapshot is null)return;
        if(!Editable)prescriptionEditor?.RestrictToHistory();
        var edit=Editable;ClinicalInputPanel.IsEnabled=PrescriptionDecision.IsEnabled=edit;UpdateConditionRights();
        SaveClinicalButton.IsEnabled=CompleteClinicalButton.IsEnabled=NewPlanButton.IsEnabled=edit;
        var planEdit=edit &&(plan is null||plan.KeHoach.TrangThai=="Nháp");PlanFields.IsEnabled=planEdit;PlanSteps.IsReadOnly=!planEdit;PlanSteps.CanUserAddRows=PlanSteps.CanUserDeleteRows=planEdit;CancelPlanButton.IsEnabled=RefreshPlanPricesButton.IsEnabled=planEdit;
        SavePlanButton.IsEnabled=planEdit;ConfirmPlanButton.IsEnabled=planEdit&&plan is not null;OpenServicesButton.IsEnabled=allowed("DichVuThucHien");OpenPrescriptionButton.IsEnabled=allowed("DonThuoc");RefreshRecommendation();UpdatePlanTotal();
    }
    /// <summary>
    /// Thực thi thao tác được truyền vào và xử lý phản hồi theo quy tắc của màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ClinicalFormInput.ClearErrors</c>, <c>action</c>, <c>ClinicalFormInput.Message</c>, <c>ClinicalFormInput.ShowError</c>.</remarks>
    /// <param name="action">Giá trị action dùng khi thực hiện thao tác; kiểu Action.</param>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    private bool Run(Action action,out string error)
    {error="";ClinicalError.Text="";ClinicalFormInput.ClearErrors(this);try{action();return true;}catch(InvalidOperationException ex){error=ClinicalError.Text=ClinicalFormInput.Message(ex);ClinicalFormInput.ShowError(this,ex);return false;}}
    /// <summary>
    /// Cập nhật dữ liệu dùng chung của giao diện sau khi thao tác khám đã thành công.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DuLieuGiaoDien.RefreshFromDatabase</c>.</remarks>
    private void SyncCache()
    {
        if(transaction is not null)return;
        try{DuLieuGiaoDien.RefreshFromDatabase();}catch(InvalidOperationException){ClinicalStatus.Text+=" Đã lưu dữ liệu; tải lại khi kết nối ổn định.";}
    }
    /// <summary>
    /// Kiểm tra và thử lưu nội dung hiện tại; cập nhật trạng thái giao diện theo kết quả.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>RequireEditable</c>, <c>CommitConditionInput</c>, <c>conditions.ToArray</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool TrySave(out string error)=>Run(()=>
    {
        RequireEditable();
        CommitConditionInput();
        var request=new YeuCauLuuKham {MaLS=visitId,DauVanTay=snapshot.DauVanTay,TrieuChung=SymptomsEditor.Text,ChanDoan=DiagnosisEditor.Text,KeHoachXuLy=TreatmentEditor.Text,GhiChu=ClinicalNote.Text,KhongKeThuoc=NoPrescription.IsChecked==true,LyDoKhongKeThuoc=NoPrescriptionReason.Text,TinhTrang=conditions.ToArray()};
        FillClinical(service.LuuKham(request,actor));RefreshClinicalSummary();ClinicalStatus.Text="Đã lưu nháp; chưa chuyển chờ thanh toán.";SyncCache();
    },out error);
    /// <summary>
    /// Kiểm tra nội dung tổng kết và thử chốt khám/lập hóa đơn theo quy trình hiện tại.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>RequireEditable</c>, <c>CommitConditionInput</c>, <c>ClinicalFormInput.Commit</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool TryComplete(out string error)=>Run(()=>
    {
        RequireEditable();
        CommitConditionInput();ClinicalFormInput.Commit(PlanSteps,"CacBuoc");
        if(CoThayDoiChuaLuu)throw new LoiKhamException("BanNhap","Có thay đổi chưa lưu. Lưu nháp hoặc tải lại để đối chiếu trước khi ký.");
        var reviewed=clinicalSummary??throw new LoiKhamException("TongKet","Tải Tổng kết trước khi chốt.");
        FillClinical(service.ChotKhamVaLapHoaDon(visitId,snapshot.DauVanTay,reviewed.DauVanTay,actor));FillCompletionOverview(service.LayKhongGianKham(visitId,actor));ClinicalStatus.Text=$"Đã chốt đơn và lập hóa đơn {snapshot.MaHD}; chờ thanh toán và nhận thuốc.";SyncCache();
    },out error);
    /// <summary>
    /// Kiểm tra nội dung và thử lưu bản kế hoạch đang soạn.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>RequireEditable</c>, <c>ClinicalFormInput.Commit</c>, <c>ClinicalFormInput.ReadDate</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool TrySavePlan(out string error)=>Run(()=>
    {
        RequireEditable();
        ClinicalFormInput.Commit(PlanSteps,"CacBuoc");var request=new YeuCauLuuKeHoach {MaLS=visitId,MaKHDT=plan?.KeHoach.MaKHDT,DauVanTay=plan?.DauVanTay,MaTTBNguon=plan?.KeHoach.MaTTBNguon??planSourceCondition,NgayBatDau=ClinicalFormInput.ReadDate(PlanStart),NgayDuKienKetThuc=ClinicalFormInput.ReadDate(PlanEnd),GhiChu=PlanNote.Text,CacBuoc=steps.ToArray()};
        FillPlan(service.LuuKeHoach(request,actor));FillCompletionOverview(service.LayKhongGianKham(visitId,actor));ClinicalStatus.Text="Đã lưu kế hoạch nháp. Duyệt kế hoạch trước khi hoàn tất.";SyncCache();
    },out error);
    /// <summary>
    /// Kiểm tra bản kế hoạch hiện tại và thử duyệt theo dấu vân tay đã đọc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>RequireEditable</c>, <c>ClinicalFormInput.Commit</c>, <c>FillPlan</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool TryConfirmPlan(out string error)=>Run(()=>
    {
        RequireEditable();
        ClinicalFormInput.Commit(PlanSteps,"CacBuoc");if(plan is null||planDirty)throw new LoiKhamException("KeHoach","Lưu nháp kế hoạch trước khi duyệt.");
        FillPlan(service.XacNhanKeHoach(plan.KeHoach.MaKHDT,plan.DauVanTay,actor));FillCompletionOverview(service.LayKhongGianKham(visitId,actor));ClinicalStatus.Text="Đã duyệt kế hoạch. Khi điều trị xong, Hoàn tất để chốt dịch vụ và lập hóa đơn.";SyncCache();
    },out error);
    /// <summary>
    /// Tiếp nhận thay đổi kế hoạch và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>FillPlan</c>, <c>service.LayKeHoach</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Plan_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(loading)return;
        if(planDirty){loading=true;PlanPicker.SelectedValue=plan?.KeHoach.MaKHDT;loading=false;ClinicalError.Text="Lưu nháp kế hoạch hoặc tải lại trước khi chuyển kế hoạch khác.";return;}
        if(PlanPicker.SelectedValue is string id)Run(()=>FillPlan(service.LayKeHoach(id,actor)),out _);
    }
    /// <summary>
    /// Xử lý thao tác mới kế hoạch khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>ClearPlan</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NewPlan_Click(object sender,RoutedEventArgs e)=>Run(()=>{if(planDirty)throw new LoiKhamException("KeHoach","Lưu nháp hoặc tải lại trước khi lập kế hoạch khác.");loading=true;try{ClearPlan();}finally{loading=false;}},out _);
    /// <summary>
    /// Xử lý thao tác tải lại khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>LoadWorkspace</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Reload_Click(object sender,RoutedEventArgs e)=>Run(()=>{LoadWorkspace();ClinicalStatus.Text="Đã tải lại dữ liệu; các thay đổi chưa lưu đã bỏ.";},out _);
    /// <summary>
    /// Tiếp nhận thay đổi lâm sàng và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>UpdateClinicalFooter</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Clinical_Changed(object sender,RoutedEventArgs e){if(!loading){dirty=true;if(PrescriptionHost is not null)PrescriptionHost.IsEnabled=clinicalSummary?.DaThucHien==true&&NoPrescription.IsChecked!=true;UpdateClinicalFooter();}}
    /// <summary>
    /// Tiếp nhận thay đổi dòng bước kế hoạch và cập nhật dữ liệu/tổng dự kiến liên quan.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Dispatcher.BeginInvoke</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Plan_Edit(object sender,DataGridCellEditEndingEventArgs e){if(!loading){planDirty=true;Dispatcher.BeginInvoke(new Action(UpdatePlanTotal));}}
    /// <summary>
    /// Tiếp nhận thay đổi kế hoạch và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Plan_FieldChanged(object sender,RoutedEventArgs e){if(!loading)planDirty=true;}
    /// <summary>
    /// Tiếp nhận thay đổi kế hoạch và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Plan_TextChanged(object sender,TextChangedEventArgs e){if(!loading)planDirty=true;}
    /// <summary>
    /// Tiếp nhận thay đổi kế hoạch ngày và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void PlanDate_TextChanged(object sender,TextChangedEventArgs e){if(!loading && sender is DatePicker picker && picker.IsKeyboardFocusWithin)planDirty=true;}
    /// <summary>
    /// Xử lý thao tác lưu khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TrySave</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Save_Click(object sender,RoutedEventArgs e)=>TrySave(out _);
    /// <summary>
    /// Xử lý thao tác lưu kế hoạch khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TrySavePlan</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void SavePlan_Click(object sender,RoutedEventArgs e)=>TrySavePlan(out _);
    /// <summary>
    /// Xử lý thao tác hủy kế hoạch khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MessageBox.Show</c>, <c>Run</c>, <c>ClearPlan</c>, <c>FillPlan</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void CancelPlan_Click(object sender,RoutedEventArgs e)
    {
        if(MessageBox.Show("Hủy kế hoạch nháp? Bản đã lưu vẫn được giữ để đối chiếu.","Hủy kế hoạch",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
        Run(()=>{if(plan is null){loading=true;try{ClearPlan();}finally{loading=false;}}else FillPlan(service.HuyKeHoach(plan.KeHoach.MaKHDT,plan.DauVanTay,actor));ClinicalStatus.Text="Đã hủy kế hoạch nháp.";SyncCache();},out _);
    }
    /// <summary>
    /// Xử lý thao tác hoàn tất khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TryComplete</c>, <c>MessageBox.Show</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Complete_Click(object sender,RoutedEventArgs e)
    {if(CoThayDoiChuaLuu){TryComplete(out _);return;}if(MessageBox.Show("Đã kiểm tra đủ dịch vụ và danh sách thuốc? Chốt sẽ ký đơn, khóa lần khám và lập hóa đơn để thanh toán, nhận thuốc tại quầy.","Chốt và lập hóa đơn",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)TryComplete(out _);}
    /// <summary>
    /// Xử lý thao tác xác nhận kế hoạch khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TryConfirmPlan</c>, <c>MessageBox.Show</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ConfirmPlan_Click(object sender,RoutedEventArgs e)
    {if(planDirty){TryConfirmPlan(out _);return;}if(MessageBox.Show("Xác nhận kế hoạch đã được bác sĩ kiểm tra?","Duyệt kế hoạch",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)TryConfirmPlan(out _);}
    /// <summary>
    /// Điều hướng đến phân hệ được yêu cầu sau khi kiểm tra quyền và công việc chưa lưu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>allowed</c>, <c>DieuHuongHoSo.Open</c>.</remarks>
    /// <param name="module">Giá trị phân hệ dùng khi thực hiện thao tác; kiểu string.</param>
    private void OpenModule(string module)=>Run(()=>{if(CoThayDoiChuaLuu)throw new LoiKhamException("BanNhap","Lưu nháp trước khi chuyển chức năng.");if(allowed(module)&&PatientId is not null)DieuHuongHoSo.Open(this,module,PatientId,visitId);},out _);
    /// <summary>
    /// Xử lý thao tác các dịch vụ khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>OpenModule</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Services_Click(object sender,RoutedEventArgs e)=>OpenModule("DichVuThucHien");
    /// <summary>
    /// Xử lý thao tác đơn thuốc khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>OpenModule</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Prescription_Click(object sender,RoutedEventArgs e)=>OpenModule("DonThuoc");
    /// <summary>
    /// Xử lý thao tác quay lại khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>.Invoke</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Back_Click(object sender,RoutedEventArgs e){if(CoThayDoiChuaLuu){ClinicalError.Text="Lưu nháp hoặc tải lại để bỏ thay đổi trước khi quay lại.";return;}BackRequested?.Invoke();}
}
