using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

public partial class DichVuThucHien : UserControl, IPatientScoped
{
    private bool updating;
    private string? visitId;
    private string? prescriptionId;
    private string? selectedId;
    public string? PatientId { get; private set; }
    private ClinicalVisitRow? Selected => Grid_DICH_VU_THUC_HIEN.SelectedItem as ClinicalVisitRow;

    public DichVuThucHien()
    {
        InitializeComponent();
        ClinicalUi.Observe(this, Refresh);
        Refresh();
    }

    public void SetPatient(string id) => FocusContext(id);
    public void FocusContext(string? patientId, string? visitId = null, string? prescriptionId = null)
    {
        var order = ClinicalData.Find("DON_THUOC", prescriptionId);
        var requestedVisit = visitId ?? (order is null ? null : UiRecord.Text(order, "MaLS"));
        var visit = ClinicalData.Find("LICH_SU_KHAM_BENH", requestedVisit);
        PatientId = patientId ?? (visit is null ? null : ClinicalData.PatientId("LICH_SU_KHAM_BENH", visit));
        this.visitId = requestedVisit;
        this.prescriptionId = prescriptionId;
        selectedId = null;
        updating = true; SearchBox.Clear(); StatusFilter.SelectedIndex = 0; updating = false;
        Feedback.Text = "";
        if (!string.IsNullOrEmpty(prescriptionId) && (order is null ||
            (!string.IsNullOrEmpty(PatientId) && ClinicalData.PatientId("DON_THUOC", order) != PatientId) ||
            UiRecord.Text(order, "MaLS") != requestedVisit))
        {
            this.visitId = "__missing__";
            Feedback.Text = "Không tìm thấy đơn thuốc thuộc bệnh nhân và lần khám được yêu cầu.";
        }
        Refresh();
    }

    private void Refresh()
    {
        if (Grid_DICH_VU_THUC_HIEN is null || updating) return;
        updating = true;
        try
        {
            var patients = ClinicalData.Options("BENH_NHAN", all: true).ToList();
            if (!string.IsNullOrEmpty(PatientId) && ClinicalData.Find("BENH_NHAN", PatientId) is null)
                patients.Add(new(PatientId, "Không tìm thấy bệnh nhân · " + PatientId));
            PatientFilter.ItemsSource = patients; PatientFilter.SelectedValue = PatientId ?? "";
            var visits = ClinicalData.Options("LICH_SU_KHAM_BENH", all: true, patient: PatientId).ToList();
            if (!string.IsNullOrEmpty(visitId) && visits.All(option => option.Value != visitId))
                visits.Add(new(visitId, "Không tìm thấy lần khám được yêu cầu"));
            VisitFilter.ItemsSource = visits; VisitFilter.SelectedValue = visitId ?? "";
            var terms = SearchBox.Text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var rows = ClinicalData.Rows("DICH_VU_THUC_HIEN")
                .Where(row => ClinicalUi.Matches("DICH_VU_THUC_HIEN", row, null, ClinicalUi.Choice(StatusFilter), PatientId))
                .Where(row => string.IsNullOrEmpty(visitId) || UiRecord.Text(row, "MaLS") == visitId)
                .OrderByDescending(row => UiRecord.Date(row, "ThoiGianThucHien"))
                .Select(row => new ClinicalVisitRow("DICH_VU_THUC_HIEN", row))
                .Where(row => terms.All(term => string.Join(" ", row.Code, row.VisitId, row.Service, row.Doctor, row.Result,
                    ClinicalData.Lookup("MaBN", row.PatientId)).Contains(term, StringComparison.CurrentCultureIgnoreCase)))
                .ToArray();
            Grid_DICH_VU_THUC_HIEN.ItemsSource = rows;
            Grid_DICH_VU_THUC_HIEN.SelectedItem = rows.FirstOrDefault(row => row.Code == selectedId);
            RecordCount.Text = $"{rows.Length} dịch vụ · {rows.Count(row => row.Status == "Đã xác nhận")} đã xác nhận";
            ListEmpty.Visibility = rows.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            AddButton.IsEnabled = (ClinicalUi.Workspace(this)?.CanOpen("KhamDieuTri") ?? true) &&
                ClinicalData.Rows("LICH_SU_KHAM_BENH").Any(row =>
                    (string.IsNullOrEmpty(PatientId) || ClinicalData.PatientId("LICH_SU_KHAM_BENH", row) == PatientId) &&
                    (string.IsNullOrEmpty(visitId) || UiRecord.Text(row, "MaLS") == visitId));
            ShowSelected();
        }
        finally { updating = false; }
    }

    private void ShowSelected()
    {
        ServiceDetail.DataContext = Selected;
        ServiceDetail.Visibility = Selected is null ? Visibility.Collapsed : Visibility.Visible;
        if (Selected is not ClinicalVisitRow row) return;
        selectedId = row.Code;
        PatientContext.Text = $"{ClinicalData.Lookup("MaBN", row.PatientId)} · Lần khám {row.VisitId} · {row.When}";
        EditButton.IsEnabled = ConfirmButton.IsEnabled = row.CanEdit && (ClinicalUi.Workspace(this)?.CanOpen("KhamDieuTri") ?? true);
        var billed = ClinicalData.Rows("CHI_TIET_HOA_DON").Any(line => UiRecord.Text(line, "MaDVThucHien") == row.Code &&
            ClinicalData.Find("HOA_DON", UiRecord.Get(line, "MaHD")) is object invoice && UiRecord.Text(invoice, "TrangThai") != "Hủy");
        BillingNote.Text = billed ? "Dịch vụ đã được đưa vào hóa đơn. Mở Hóa đơn / thanh toán để xem khoản thu." :
            row.Status == "Đã xác nhận" ? "Dịch vụ đã xác nhận, có thể đưa vào hóa đơn của lần khám." : "Chỉ dịch vụ đã xác nhận được đưa vào hóa đơn.";
    }

    private void Service_Changed(object sender, SelectionChangedEventArgs e) { if (!updating) ShowSelected(); }
    private void Filter_Changed(object sender, RoutedEventArgs e) => Refresh();
    private void Patient_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updating) return;
        PatientId = EmptyToNull(ClinicalUi.Selected(PatientFilter));
        visitId = prescriptionId = selectedId = null; Feedback.Text = ""; Refresh();
    }
    private void Visit_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updating) return;
        visitId = EmptyToNull(ClinicalUi.Selected(VisitFilter));
        prescriptionId = selectedId = null; Feedback.Text = ""; Refresh();
    }
    private static string? EmptyToNull(string value) => string.IsNullOrEmpty(value) ? null : value;
    private void Clear_Click(object sender, RoutedEventArgs e) => FocusContext(null);
    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        try { ClinicalData.RefreshFromDatabase(); Feedback.Text = ""; Refresh(); }
        catch (InvalidOperationException ex) { Feedback.Text = ex.Message; }
    }
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (!AddButton.IsEnabled) return;
        var context = string.IsNullOrEmpty(visitId) ? null : new CreationContext("DICH_VU_THUC_HIEN", "MaLS", visitId);
        ClinicalUi.Workspace(this)?.ShowClinicalEditor("KhamDieuTri", "DICH_VU_THUC_HIEN", context: context, patient: PatientId);
    }
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is ClinicalVisitRow { CanEdit: true } row)
            ClinicalUi.Workspace(this)?.ShowClinicalEditor("KhamDieuTri", "DICH_VU_THUC_HIEN", row.Record, patient: row.PatientId);
    }
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not ClinicalVisitRow { CanEdit: true } row || !ConfirmButton.IsEnabled) return;
        try
        {
            var actual = ClinicalData.Find("DICH_VU_THUC_HIEN", row.Code);
            if (actual is null || !ClinicalData.CanEdit("DICH_VU_THUC_HIEN", actual))
                throw new InvalidOperationException("Dịch vụ đã thay đổi. Làm mới danh sách trước khi xác nhận.");
            ClinicalData.SetValue("DICH_VU_THUC_HIEN", actual, "TrangThai", "Đã xác nhận");
            Feedback.Text = "";
        }
        catch (InvalidOperationException ex) { Feedback.Text = ex.Message; }
    }
    private void Open(string module)
    {
        if (Selected is not ClinicalVisitRow row) return;
        var actualVisit = ClinicalData.Find("LICH_SU_KHAM_BENH", row.VisitId);
        if (actualVisit is null || string.IsNullOrEmpty(row.PatientId) || ClinicalData.PatientId("LICH_SU_KHAM_BENH", actualVisit) != row.PatientId)
        { Feedback.Text = "Liên kết bệnh nhân hoặc lần khám không còn hợp lệ. Làm mới danh sách."; return; }
        ClinicalNavigator.Open(this, module, row.PatientId, module == "BenhNhan" ? null : row.VisitId,
            module == "DonThuoc" ? prescriptionId : null);
    }
    private void Patient_Click(object sender, RoutedEventArgs e) => Open("BenhNhan");
    private void Treatment_Click(object sender, RoutedEventArgs e) => Open("KhamDieuTri");
    private void Prescription_Click(object sender, RoutedEventArgs e) => Open("DonThuoc");
    private void Invoice_Click(object sender, RoutedEventArgs e) => Open("HoaDon");
}
