using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;

public partial class ChiTietKhamDieuTri : UserControl, IPatientScoped
{
    private readonly string visitId;
    private ClinicalVisitSummary? summary;
    private Func<string, bool> allowed = _ => true;
    public event Action? BackRequested;
    public string? PatientId => summary?.PatientId;
    public ChiTietKhamDieuTri(object visit)
    {
        visitId = UiRecord.Text(visit, "MaLS");
        InitializeComponent();
        ClinicalUi.Observe(this, Refresh);
        Refresh();
    }
    public void ConfigureAccess(Func<string, bool> canAccess)
    {
        allowed = canAccess;
        PrescriptionTab.Visibility = allowed("DonThuoc") ? Visibility.Visible : Visibility.Collapsed;
        ServicesButton.IsEnabled=allowed("DichVuThucHien");
        InvoiceButton.IsEnabled=allowed("HoaDon");
    }
    private void Refresh()
    {
        var record = ClinicalData.Find("LICH_SU_KHAM_BENH", visitId);
        if (record is null) { IsEnabled = false; return; }
        var priorPlan = (PlanList.SelectedItem as ClinicalVisitPlan)?.Code;
        var priorStep = (StepGrid.SelectedItem as ClinicalVisitRow)?.Code;
        summary = new(record, summary?.Visit);
        DataContext = summary;
        PlanList.SelectedItem = summary.Plans.FirstOrDefault(plan => plan.Code == priorPlan) ?? summary.Plans.FirstOrDefault();
        if (PlanList.SelectedItem is ClinicalVisitPlan plan)
            StepGrid.SelectedItem = plan.Steps.FirstOrDefault(step => step.Code == priorStep) ?? plan.Steps.FirstOrDefault();
    }
    private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke();
    private void OpenPatient_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(PatientId)) ClinicalNavigator.Open(this, "BenhNhan", PatientId);
    }
    private void OpenServices_Click(object sender,RoutedEventArgs e)
    { if(PatientId is not null) ClinicalNavigator.Open(this,"DichVuThucHien",PatientId,visitId); }
    private void OpenInvoice_Click(object sender,RoutedEventArgs e)
    { if(PatientId is not null) ClinicalNavigator.Open(this,"HoaDon",PatientId,visitId); }
    private void Edit(string table, object? record = null, CreationContext? context = null, string module = "KhamDieuTri")
    {
        if (summary is not null && allowed(module)) ClinicalUi.Workspace(this)?.ShowClinicalEditor(module, table, record, context, PatientId);
    }
    private void EditVisit_Click(object sender, RoutedEventArgs e)
    {
        if (summary?.Visit.CanEdit == true) Edit("LICH_SU_KHAM_BENH", summary.Record);
    }
    private bool SaveDraft(out object? saved, out string error)
    {
        saved = null; error = "";
        if (summary is null || !summary.Visit.CanEdit) { error = "Lần khám đã xác nhận được giữ nguyên."; return false; }
        var draft = summary.Visit;
        if (ClinicalData.Find("PHIEU_KHAM_BENH", UiRecord.Get(draft.Record, "MaPhieu")) is null ||
            ClinicalData.Find("NHAN_VIEN", draft.MaBS) is null || ClinicalData.Find("PHONG", draft.MaPhong) is null || UiRecord.Date(draft.Record, "ThoiGianKham") is null)
        { error = "Chọn lượt tiếp nhận, bác sĩ, phòng và thời gian khám hợp lệ trước khi lưu."; return false; }
        try
        {
            var values = ClinicalData.Values(draft.Record);
            values["MaBS"] = draft.MaBS; values["MaPhong"] = draft.MaPhong;
            values["TrieuChung"] = draft.TrieuChung; values["ChanDoan"] = draft.ChanDoan;
            values["KeHoachXuLy"] = draft.KeHoachXuLy; values["GhiChu"] = draft.GhiChu;
            saved = ClinicalData.Upsert("LICH_SU_KHAM_BENH", values, draft.Record,draft.OriginalValues);
            draft.AcceptSaved(); summary?.Visit.AcceptSaved(); return true;
        }
        catch (InvalidOperationException ex) { error = ex.Message; return false; }
    }
    private void SaveVisit_Click(object sender, RoutedEventArgs e)
    {
        if (allowed("KhamDieuTri") && !SaveDraft(out _, out var error)) MessageBox.Show(error, "Lưu lần khám");
    }
    private void ConfirmVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!allowed("KhamDieuTri") || summary?.Visit.CanEdit != true) return;
        if (string.IsNullOrWhiteSpace(summary.Visit.ChanDoan)) { MessageBox.Show("Nhập và lưu chẩn đoán trước khi xác nhận lần khám.", "Kiểm tra lần khám"); return; }
        string error="";
        try
        {
            var success=ClinicalData.Transaction(()=>
            {
                if (!SaveDraft(out var saved,out error) || saved is null) return false;
                ClinicalData.SetValue("LICH_SU_KHAM_BENH",saved,"TrangThai","Đã xác nhận");
                return true;
            });
            if (!success) MessageBox.Show(error,"Kiểm tra lần khám");
        }
        catch(InvalidOperationException ex) {MessageBox.Show(ex.Message,"Xác nhận lần khám");}
    }
    private void Plan_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (PlanHost is null) return;
        var plan = PlanList.SelectedItem as ClinicalVisitPlan;
        PlanHost.DataContext = plan; PlanHost.Visibility = plan is null ? Visibility.Collapsed : Visibility.Visible;
        StepGrid.SelectedItem = plan?.Steps.FirstOrDefault(); UpdateStepExecutions();
    }
    private void Step_Changed(object sender, SelectionChangedEventArgs e) => UpdateStepExecutions();
    private void UpdateStepExecutions()
    {
        if (StepExecutionGrid is null) return;
        var step = StepGrid.SelectedItem as ClinicalVisitRow;
        var rows = (PlanList.SelectedItem as ClinicalVisitPlan)?.Executions.Where(row => row.Step == step?.Code).ToArray() ?? Array.Empty<ClinicalVisitRow>();
        StepExecutionGrid.ItemsSource = rows;
        StepEmpty.Text = step is null ? "Chọn một bước để xem các lần thực hiện." : rows.Length == 0 ? "Bước này chưa có dịch vụ thực hiện." : "";
    }
    private void AddCondition_Click(object sender, RoutedEventArgs e)
    {
        if (summary?.Visit.CanEdit == true) Edit("TINH_TRANG_BENH", context: new("TINH_TRANG_BENH", "MaLS", visitId));
    }
    private void AddPlan_Click(object sender, RoutedEventArgs e) => Edit("KE_HOACH_DIEU_TRI", context: new("KE_HOACH_DIEU_TRI", "MaLS", visitId));
    private void EditPlan_Click(object sender, RoutedEventArgs e)
    {
        if (PlanList.SelectedItem is ClinicalVisitPlan plan && plan.CanEdit) Edit("KE_HOACH_DIEU_TRI", plan.Record);
    }
    private void AddStep_Click(object sender, RoutedEventArgs e)
    {
        if (PlanList.SelectedItem is ClinicalVisitPlan plan && plan.CanEdit) Edit("CHI_TIET_KE_HOACH", context: new("CHI_TIET_KE_HOACH", "MaKHDT", plan.Code));
    }
    private void ConfirmPlan_Click(object sender, RoutedEventArgs e)
    {
        if (!allowed("KhamDieuTri") || PlanList.SelectedItem is not ClinicalVisitPlan plan || !plan.CanConfirm) return;
        if (ClinicalData.Find("NHAN_VIEN", UiRecord.Get(plan.Record, "MaBS")) is null || UiRecord.Date(plan.Record, "NgayLap") is null || plan.Steps.Length == 0 ||
            plan.Steps.Any(row => row.Quantity <= 0 || row.Price < 0 || ClinicalData.Find("DICH_VU", UiRecord.Get(row.Record, "MaDV")) is null))
        { MessageBox.Show("Kế hoạch cần bác sĩ, ngày lập và ít nhất một bước có dịch vụ hợp lệ, số lượng dương, đơn giá không âm.", "Kiểm tra kế hoạch"); return; }
        try {ClinicalData.SetValue("KE_HOACH_DIEU_TRI", plan.Record, "TrangThai", "Đã xác nhận");}
        catch(InvalidOperationException ex) {MessageBox.Show(ex.Message,"Xác nhận kế hoạch");}
    }
    private void AddExecution_Click(object sender, RoutedEventArgs e) => Edit("DICH_VU_THUC_HIEN", context: new("DICH_VU_THUC_HIEN", "MaLS", visitId));
    private void ExecuteStep_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ClinicalVisitRow step || !step.CanExecute || summary is null || step.PatientId != PatientId) return;
        Edit("DICH_VU_THUC_HIEN", new Dictionary<string, object?> { ["MaLS"] = visitId, ["MaCTKH"] = step.Code, ["MaDV"] = UiRecord.Get(step.Record, "MaDV"), ["MaNVThucHien"] = UiRecord.Get(summary.Record, "MaBS"), ["SoLuong"] = step.RemainingQuantity, ["DonGia"] = step.Price, ["TrangThai"] = "Nháp" }, new("DICH_VU_THUC_HIEN", "MaCTKH", step.Code));
    }
    private void EditRow_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ClinicalVisitRow row && row.CanEdit && row.PatientId == PatientId) Edit(row.Table, row.Record);
    }
    private void OpenActualVisit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ClinicalVisitRow row && row.PatientId == PatientId && !string.IsNullOrEmpty(row.VisitId))
            ClinicalNavigator.Open(this, "KhamDieuTri", row.PatientId, row.VisitId);
    }
    private void AddPrescription_Click(object sender, RoutedEventArgs e) => Edit("DON_THUOC", context: new("DON_THUOC", "MaLS", visitId), module: "DonThuoc");
    private void OpenPrescription_Click(object sender, RoutedEventArgs e)
    {
        if (allowed("DonThuoc") && (sender as FrameworkElement)?.DataContext is PrescriptionCard order && ClinicalData.PatientId("DON_THUOC", order.Record) == PatientId && PatientId is not null)
            ClinicalNavigator.Open(this, "DonThuoc", PatientId, visitId, order.Code);
    }
}
