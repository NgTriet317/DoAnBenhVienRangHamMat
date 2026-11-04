// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ các tab tình trạng/kế hoạch/dịch vụ/đơn thuốc, thao tác bản nháp và xác nhận; đã giải thích lưu dữ liệu, giữ bước hiện tại và điều hướng cùng lần khám.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;

/// <summary>
/// Màn hình chi tiết lần khám và điều trị: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class ChiTietKhamDieuTri : UserControl, IPatientScoped
{
    /// <summary>
    /// Mã lần khám được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly string visitId;
    /// <summary>
    /// Giá trị lưu nội bộ của thuộc tính PatientId, dùng bởi getter/setter tương ứng.
    /// </summary>
    private TongHopLanKham? summary;
    /// <summary>
    /// Quyền cho phép thao tác được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private Func<string, bool> allowed = _ => true;
    /// <summary>
    /// Lâm sàng trình nhập liệu được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private KhamLamSang? clinicalEditor;
    /// <summary>
    /// Giá trị Có thay đổi chưa lưu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public bool CoThayDoiChuaLuu=>clinicalEditor?.CoThayDoiChuaLuu==true;
    /// <summary>
    /// Thông báo quay lại được yêu cầu cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    public event Action? BackRequested;
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string? PatientId => summary?.PatientId;
    /// <summary>
    /// Khởi tạo chi tiết lần khám và điều trị từ XAML. Theo dõi cache khi Loaded và làm mới nội dung khi dữ liệu thay đổi; gỡ theo dõi khi Unloaded.
    /// </summary>
    public ChiTietKhamDieuTri(object visit,bool historyReadOnly=false)
    {
        visitId = UiRecord.Text(visit, "MaLS");
        if (DuLieuGiaoDien.PhamViLamSang) new BUS.BUS_KHAMDIEUTRI().LayChiTiet(visitId, Controls.ClinicalWorkflow.QuyenNghiepVu.LayNguCanh());
        InitializeComponent();
        HoTroGiaoDien.Observe(this, Refresh);
        Refresh();
        if(DuLieuGiaoDien.PhamViLamSang && (DuLieuGiaoDien.NguoiDungLamSang.LaAdmin || UiRecord.Text(visit,"MaBS")==PhienDangNhap.MaNV) && Controls.ClinicalWorkflow.QuyenNghiepVu.LayNguCanh().CoQuyen(ET.QuyenLamViec.Kham))
        {
            clinicalEditor=new KhamLamSang(visitId);if(historyReadOnly)clinicalEditor.RestrictToHistory();clinicalEditor.BackRequested+=()=>BackRequested?.Invoke();Content=clinicalEditor;
        }
    }
    /// <summary>
    /// Nhận hàm kiểm tra quyền phân hệ từ màn hình cha, lưu lại và áp dụng cho các tab/nút liên quan trong control hiện tại.
    /// </summary>
    public void ConfigureAccess(Func<string, bool> canAccess)
    {
        allowed = canAccess;
        clinicalEditor?.ConfigureAccess(canAccess);
        PrescriptionTab.Visibility = allowed("DonThuoc") ? Visibility.Visible : Visibility.Collapsed;
        ServicesButton.IsEnabled=allowed("DichVuThucHien");
        InvoiceButton.IsEnabled=allowed("HoaDon");
    }
    /// <summary>
    /// Tìm đúng lần khám, tạo mô hình tổng hợp mới nhưng sao chép nháp chưa lưu, giữ kế hoạch/bước đang chọn; bản ghi mất thì khóa màn hình.
    /// </summary>
    private void Refresh()
    {
        var record = DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", visitId);
        if (record is null) { IsEnabled = false; return; }
        var priorPlan = (PlanList.SelectedItem as KeHoachLanKham)?.Code;
        var priorStep = (StepGrid.SelectedItem as DongChiTietLanKham)?.Code;
        summary = new(record, summary?.Visit);
        DataContext = summary;
        PlanList.SelectedItem = summary.Plans.FirstOrDefault(plan => plan.Code == priorPlan) ?? summary.Plans.FirstOrDefault();
        if (PlanList.SelectedItem is KeHoachLanKham plan)
            StepGrid.SelectedItem = plan.Steps.FirstOrDefault(step => step.Code == priorStep) ?? plan.Steps.FirstOrDefault();
    }
    /// <summary>
    /// Quay lại màn hình trước bằng sự kiện BackRequested/CloseRequested hoặc workspace; không tự lưu dữ liệu đang nhập.
    /// </summary>
    private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke();
    /// <summary>
    /// Mở hồ sơ bệnh nhân nguồn của dữ liệu đang xem.
    /// </summary>
    private void OpenPatient_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(PatientId)) DieuHuongHoSo.Open(this, "BenhNhan", PatientId);
    }
    /// <summary>
    /// Mở phân hệ Dịch vụ thực hiện với cùng mã bệnh nhân và lần khám nguồn đang xem.
    /// </summary>
    private void OpenServices_Click(object sender,RoutedEventArgs e)
    { if(PatientId is not null) DieuHuongHoSo.Open(this,"DichVuThucHien",PatientId,visitId); }
    /// <summary>
    /// Mở phân hệ Hóa đơn với cùng mã bệnh nhân và lần khám nguồn đang xem.
    /// </summary>
    private void OpenInvoice_Click(object sender,RoutedEventArgs e)
    { if(PatientId is not null) DieuHuongHoSo.Open(this,"HoaDon",PatientId,visitId); }
    /// <summary>
    /// Mở biểu mẫu cho bảng, bản ghi hoặc ngữ cảnh cha nếu có dữ liệu lần khám và vai trò được phép dùng phân hệ tương ứng.
    /// </summary>
    private void Edit(string table, object? record = null, CreationContext? context = null, string module = "KhamDieuTri")
    {
        if (summary is not null && allowed(module)) HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo(module, table, record, context, PatientId);
    }
    /// <summary>
    /// Mở form sửa thông tin lần khám nếu lần khám còn được sửa theo trạng thái hiện tại.
    /// </summary>
    private void EditVisit_Click(object sender, RoutedEventArgs e)
    {
        if (summary?.Visit.CanEdit == true) Edit("LICH_SU_KHAM_BENH", summary.Record);
    }
    /// <summary>
    /// Kiểm tra lượt tiếp nhận/bác sĩ/phòng/giờ, gộp trường khám đang nhập và lưu Upsert kèm snapshot để chặn ghi đè; đánh dấu nháp đã lưu khi thành công.
    /// </summary>
    private bool SaveDraft(out object? saved, out string error)
    {
        saved = null; error = "";
        if (summary is null || !summary.Visit.CanEdit) { error = "Lần khám đã xác nhận được giữ nguyên."; return false; }
        var draft = summary.Visit;
        if (DuLieuGiaoDien.Find("PHIEU_KHAM_BENH", UiRecord.Get(draft.Record, "MaPhieu")) is null ||
            DuLieuGiaoDien.Find("NHAN_VIEN", draft.MaBS) is null || DuLieuGiaoDien.Find("PHONG", draft.MaPhong) is null || UiRecord.Date(draft.Record, "ThoiGianKham") is null)
        { error = "Chọn lượt tiếp nhận, bác sĩ, phòng và thời gian khám hợp lệ trước khi lưu."; return false; }
        try
        {
            var values = DuLieuGiaoDien.Values(draft.Record);
            values["MaBS"] = draft.MaBS; values["MaPhong"] = draft.MaPhong;
            values["TrieuChung"] = draft.TrieuChung; values["ChanDoan"] = draft.ChanDoan;
            values["KeHoachXuLy"] = draft.KeHoachXuLy; values["GhiChu"] = draft.GhiChu;
            saved = DuLieuGiaoDien.Upsert("LICH_SU_KHAM_BENH", values, draft.Record,draft.OriginalValues);
            draft.AcceptSaved(); summary?.Visit.AcceptSaved(); return true;
        }
        catch (InvalidOperationException ex) { error = ex.Message; return false; }
    }
    /// <summary>
    /// Lưu phần thông tin khám đang nhập nếu vai trò có quyền Khám điều trị; hiển thị lỗi kiểm tra/lưu nếu có.
    /// </summary>
    private void SaveVisit_Click(object sender, RoutedEventArgs e)
    {
        if (allowed("KhamDieuTri") && !SaveDraft(out _, out var error)) MessageBox.Show(error, "Lưu lần khám");
    }
    /// <summary>
    /// Yêu cầu chẩn đoán và quyền sửa, lưu nháp rồi chuyển Đã xác nhận trong cùng giao dịch; lỗi thì không giữ trạng thái xác nhận dở dang.
    /// </summary>
    private void ConfirmVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!allowed("KhamDieuTri") || summary?.Visit.CanEdit != true) return;
        if (string.IsNullOrWhiteSpace(summary.Visit.ChanDoan)) { MessageBox.Show("Nhập và lưu chẩn đoán trước khi xác nhận lần khám.", "Kiểm tra lần khám"); return; }
        string error="";
        try
        {
            var success=DuLieuGiaoDien.Transaction(()=>
            {
                if (!SaveDraft(out var saved,out error) || saved is null) return false;
                DuLieuGiaoDien.SetValue("LICH_SU_KHAM_BENH",saved,"TrangThai","Đã xác nhận");
                return true;
            });
            if (!success) MessageBox.Show(error,"Kiểm tra lần khám");
        }
        catch(InvalidOperationException ex) {MessageBox.Show(ex.Message,"Xác nhận lần khám");}
    }
    /// <summary>
    /// Đưa kế hoạch đang chọn vào vùng chi tiết, ẩn vùng khi không có lựa chọn và cập nhật bước/lần thực hiện tương ứng.
    /// </summary>
    private void Plan_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (PlanHost is null) return;
        var plan = PlanList.SelectedItem as KeHoachLanKham;
        PlanHost.DataContext = plan; PlanHost.Visibility = plan is null ? Visibility.Collapsed : Visibility.Visible;
        StepGrid.SelectedItem = plan?.Steps.FirstOrDefault(); UpdateStepExecutions();
    }
    /// <summary>
    /// Cập nhật bảng các lần thực hiện của bước kế hoạch vừa chọn.
    /// </summary>
    private void Step_Changed(object sender, SelectionChangedEventArgs e) => UpdateStepExecutions();
    /// <summary>
    /// Lọc các thực hiện của kế hoạch theo mã bước, đặt ItemsSource và thông báo nếu chưa có bước hoặc chưa có lần thực hiện.
    /// </summary>
    private void UpdateStepExecutions()
    {
        if (StepExecutionGrid is null) return;
        var step = StepGrid.SelectedItem as DongChiTietLanKham;
        var rows = (PlanList.SelectedItem as KeHoachLanKham)?.Executions.Where(row => row.Step == step?.Code).ToArray() ?? Array.Empty<DongChiTietLanKham>();
        StepExecutionGrid.ItemsSource = rows;
        StepEmpty.Text = step is null ? "Chọn một bước để xem các lần thực hiện." : rows.Length == 0 ? "Bước này chưa có dịch vụ thực hiện." : "";
    }
    /// <summary>
    /// Mở form ghi tình trạng bệnh với mã lần khám cha được khóa; chỉ cho thêm khi lần khám còn được sửa.
    /// </summary>
    private void AddCondition_Click(object sender, RoutedEventArgs e)
    {
        if (summary?.Visit.CanEdit == true) Edit("TINH_TRANG_BENH", context: new("TINH_TRANG_BENH", "MaLS", visitId));
    }
    /// <summary>
    /// Mở form kế hoạch điều trị mới gắn với lần khám đang xem.
    /// </summary>
    private void AddPlan_Click(object sender, RoutedEventArgs e) => Edit("KE_HOACH_DIEU_TRI", context: new("KE_HOACH_DIEU_TRI", "MaLS", visitId));
    /// <summary>
    /// Mở form sửa kế hoạch đang chọn nếu kế hoạch và các bước vẫn còn được sửa.
    /// </summary>
    private void EditPlan_Click(object sender, RoutedEventArgs e)
    {
        if (PlanList.SelectedItem is KeHoachLanKham plan && plan.CanEdit) Edit("KE_HOACH_DIEU_TRI", plan.Record);
    }
    /// <summary>
    /// Mở form thêm bước dịch vụ dự kiến với mã kế hoạch cha; chỉ mở khi kế hoạch còn sửa được.
    /// </summary>
    private void AddStep_Click(object sender, RoutedEventArgs e)
    {
        if (PlanList.SelectedItem is KeHoachLanKham plan && plan.CanEdit) Edit("CHI_TIET_KE_HOACH", context: new("CHI_TIET_KE_HOACH", "MaKHDT", plan.Code));
    }
    /// <summary>
    /// Kiểm tra quyền, bác sĩ/ngày lập và các bước có dịch vụ/số lượng/giá hợp lệ rồi đổi kế hoạch sang Đã xác nhận qua cache/BUS.
    /// </summary>
    private void ConfirmPlan_Click(object sender, RoutedEventArgs e)
    {
        if (!allowed("KhamDieuTri") || PlanList.SelectedItem is not KeHoachLanKham plan || !plan.CanConfirm) return;
        if (DuLieuGiaoDien.Find("NHAN_VIEN", UiRecord.Get(plan.Record, "MaBS")) is null || UiRecord.Date(plan.Record, "NgayLap") is null || plan.Steps.Length == 0 ||
            plan.Steps.Any(row => row.Quantity <= 0 || row.Price < 0 || DuLieuGiaoDien.Find("DICH_VU", UiRecord.Get(row.Record, "MaDV")) is null))
        { MessageBox.Show("Kế hoạch cần bác sĩ, ngày lập và ít nhất một bước có dịch vụ hợp lệ, số lượng dương, đơn giá không âm.", "Kiểm tra kế hoạch"); return; }
        try {DuLieuGiaoDien.SetValue("KE_HOACH_DIEU_TRI", plan.Record, "TrangThai", "Đã xác nhận");}
        catch(InvalidOperationException ex) {MessageBox.Show(ex.Message,"Xác nhận kế hoạch");}
    }
    /// <summary>
    /// Mở form ghi nhận dịch vụ thực tế gắn với mã lần khám hiện tại.
    /// </summary>
    private void AddExecution_Click(object sender, RoutedEventArgs e) => Edit("DICH_VU_THUC_HIEN", context: new("DICH_VU_THUC_HIEN", "MaLS", visitId));
    /// <summary>
    /// Kiểm tra bước còn thực hiện được và thuộc cùng bệnh nhân, mở nháp thực hiện với lượng còn lại/giá của bước và lần khám hiện tại.
    /// </summary>
    private void ExecuteStep_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not DongChiTietLanKham step || !step.CanExecute || summary is null || step.PatientId != PatientId) return;
        Edit("DICH_VU_THUC_HIEN", new Dictionary<string, object?> { ["MaLS"] = visitId, ["MaCTKH"] = step.Code, ["MaDV"] = UiRecord.Get(step.Record, "MaDV"), ["MaNVThucHien"] = UiRecord.Get(summary.Record, "MaBS"), ["SoLuong"] = step.RemainingQuantity, ["DonGia"] = step.Price, ["TrangThai"] = "Nháp" }, new("DICH_VU_THUC_HIEN", "MaCTKH", step.Code));
    }
    /// <summary>
    /// Mở form sửa dòng tình trạng/bước/dịch vụ nếu trạng thái cho phép và dòng thuộc đúng bệnh nhân đang xem.
    /// </summary>
    private void EditRow_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DongChiTietLanKham row && row.CanEdit && row.PatientId == PatientId) Edit(row.Table, row.Record);
    }
    /// <summary>
    /// Mở lần khám thực tế của dòng dịch vụ, giữ đúng bệnh nhân; dùng khi kế hoạch được thực hiện ở một lần khám khác.
    /// </summary>
    private void OpenActualVisit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DongChiTietLanKham row && row.PatientId == PatientId && !string.IsNullOrEmpty(row.VisitId))
            DieuHuongHoSo.Open(this, "KhamDieuTri", row.PatientId, row.VisitId);
    }
    /// <summary>
    /// Mở form kê đơn mới với lần khám hiện tại làm liên kết cha và kiểm tra quyền phân hệ Đơn thuốc.
    /// </summary>
    private void AddPrescription_Click(object sender, RoutedEventArgs e) => Edit("DON_THUOC", context: new("DON_THUOC", "MaLS", visitId), module: "DonThuoc");
    /// <summary>
    /// Mở chi tiết đơn đang bấm nếu có quyền và đơn thuộc cùng bệnh nhân/lần khám.
    /// </summary>
    private void OpenPrescription_Click(object sender, RoutedEventArgs e)
    {
        if (allowed("DonThuoc") && (sender as FrameworkElement)?.DataContext is PrescriptionCard order && DuLieuGiaoDien.PatientId("DON_THUOC", order.Record) == PatientId && PatientId is not null)
            DieuHuongHoSo.Open(this, "DonThuoc", PatientId, visitId, order.Code);
    }
}
