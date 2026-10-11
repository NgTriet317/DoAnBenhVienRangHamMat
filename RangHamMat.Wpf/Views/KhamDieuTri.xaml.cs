using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;

public partial class KhamDieuTri : UserControl, IPatientScoped
{
    private bool updating;
    private string? selectedVisitId;
    private string? explicitVisitId;
    private Func<string, bool> allowed = _ => true;
    public string? PatientId { get; private set; }
    public KhamDieuTri()
    {
        InitializeComponent();
        ClinicalUi.Observe(this, Refresh);
        Refresh();
    }
    public void ConfigureAccess(Func<string, bool> canAccess)
    {
        allowed = canAccess;
        if (DetailHost.Content is ChiTietKhamDieuTri detail) detail.ConfigureAccess(canAccess);
    }
    public void SetPatient(string id) => FocusContext(id);
    public void FocusContext(string? patientId, string? visitId = null, string? prescriptionId = null)
    {
        if (!string.IsNullOrEmpty(prescriptionId))
        {
            var order = ClinicalData.Find("DON_THUOC", prescriptionId);
            if (order is null || (!string.IsNullOrEmpty(patientId) && ClinicalData.PatientId("DON_THUOC", order) != patientId))
            { PatientId = patientId; explicitVisitId = "__missing__"; selectedVisitId = null; Refresh(); ContextNote.Text = "Không tìm thấy đơn thuốc thuộc bệnh nhân được yêu cầu."; return; }
            visitId ??= UiRecord.Text(order, "MaLS");
        }
        var visit = ClinicalData.Find("LICH_SU_KHAM_BENH", visitId);
        PatientId = patientId ?? (visit is null ? null : ClinicalData.PatientId("LICH_SU_KHAM_BENH", visit));
        explicitVisitId = visitId;
        selectedVisitId = visitId;
        SearchBox.Clear(); StatusFilter.SelectedIndex = 0; FromDate.SelectedDate = null; ToDate.SelectedDate = null;
        Refresh();
    }
    private void Refresh()
    {
        if (VisitList is null || updating) return;
        updating = true;
        try
        {
            var patients = ClinicalData.Options("BENH_NHAN", all: true).ToList();
            if (!string.IsNullOrEmpty(PatientId) && ClinicalData.Find("BENH_NHAN", PatientId) is null) patients.Add(new(PatientId, "Không tìm thấy bệnh nhân · " + PatientId));
            PatientFilter.ItemsSource = patients; PatientFilter.SelectedValue = PatientId ?? "";
            var rows = ClinicalData.Rows("LICH_SU_KHAM_BENH").Where(row => ClinicalUi.Matches("LICH_SU_KHAM_BENH", row, SearchBox.Text, ClinicalUi.Choice(StatusFilter), PatientId))
                .Where(row => FromDate.SelectedDate is null || UiRecord.Date(row, "ThoiGianKham")?.Date >= FromDate.SelectedDate)
                .Where(row => ToDate.SelectedDate is null || UiRecord.Date(row, "ThoiGianKham")?.Date <= ToDate.SelectedDate)
                .OrderByDescending(row => UiRecord.Date(row, "ThoiGianKham")).Select(row => new ClinicalVisitListItem(row)).ToArray();
            VisitList.ItemsSource = rows;
            VisitList.SelectedItem = rows.FirstOrDefault(row => row.Code == selectedVisitId);
            RecordCount.Text = $"{rows.Length} lần khám · {rows.Count(row => ClinicalData.CanEdit("LICH_SU_KHAM_BENH", row.Record))} bản nháp";
            ListEmpty.Visibility = rows.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (VisitList.SelectedItem is ClinicalVisitListItem selected) ShowVisit(selected);
            else
            {
                DetailHost.Content = null; DetailEmpty.Visibility = Visibility.Visible;
                ContextNote.Text = !string.IsNullOrEmpty(explicitVisitId) ? "Không tìm thấy lần khám thuộc bệnh nhân được yêu cầu hoặc bộ lọc hiện tại." : "Thông tin khám, tình trạng bệnh, kế hoạch, dịch vụ thực hiện và đơn thuốc sẽ xuất hiện tại đây.";
            }
        }
        finally { updating = false; }
    }
    private void ShowVisit(ClinicalVisitListItem visit)
    {
        if (!string.IsNullOrEmpty(PatientId) && visit.PatientId != PatientId) return;
        selectedVisitId = visit.Code;
        if (DetailHost.Content is not ChiTietKhamDieuTri current || UiRecord.Text((current.DataContext as ClinicalVisitSummary)?.Record!, "MaLS") != visit.Code)
        {
            var detail = new ChiTietKhamDieuTri(visit.Record); detail.ConfigureAccess(allowed);
            detail.BackRequested += () => { explicitVisitId = null; selectedVisitId = null; VisitList.SelectedItem = null; DetailHost.Content = null; DetailEmpty.Visibility = Visibility.Visible; };
            DetailHost.Content = detail;
        }
        DetailEmpty.Visibility = Visibility.Collapsed; NewVisitPanel.Visibility = Visibility.Collapsed;
    }
    private void Visit_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!updating && VisitList.SelectedItem is ClinicalVisitListItem visit) { explicitVisitId = null; ShowVisit(visit); }
    }
    private void Filter_Changed(object sender, RoutedEventArgs e) { if (VisitList is not null && !updating) Refresh(); }
    private void PatientFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updating || VisitList is null) return;
        PatientId = string.IsNullOrEmpty(ClinicalUi.Selected(PatientFilter)) ? null : ClinicalUi.Selected(PatientFilter);
        selectedVisitId = null; explicitVisitId = null; NewVisitPanel.Visibility = Visibility.Collapsed; Refresh();
    }
    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        updating = true; SearchBox.Clear(); StatusFilter.SelectedIndex = 0; FromDate.SelectedDate = null; ToDate.SelectedDate = null; PatientId = null; explicitVisitId = null; updating = false; Refresh();
    }
    private void AddVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!allowed("KhamDieuTri")) return;
        var unused = ClinicalData.Options("PHIEU_KHAM_BENH", patient: PatientId).Where(option => !ClinicalData.Children("LICH_SU_KHAM_BENH", "MaPhieu", option.Value).Any()).ToArray();
        ReceiptChoice.ItemsSource = unused; ReceiptChoice.SelectedIndex = unused.Length == 1 ? 0 : -1;
        CreateVisitButton.IsEnabled = unused.Length > 0;
        ReceiptNote.Text = unused.Length == 0 ? "Chưa có lượt tiếp nhận chưa khám. Lập lượt tiếp nhận trước khi bắt đầu lần khám." : "Chọn lượt tiếp nhận đúng bệnh nhân trước khi tiếp tục.";
        NewVisitPanel.Visibility = Visibility.Visible;
    }
    private void CreateVisit_Click(object sender, RoutedEventArgs e)
    {
        var receiptId = ClinicalUi.Selected(ReceiptChoice);
        var receipt = ClinicalData.Find("PHIEU_KHAM_BENH", receiptId);
        if (receipt is null) { ReceiptNote.Text = "Chọn một lượt tiếp nhận để tiếp tục."; return; }
        var patientId = ClinicalData.PatientId("PHIEU_KHAM_BENH", receipt);
        if (string.IsNullOrEmpty(patientId) || (!string.IsNullOrEmpty(PatientId) && patientId != PatientId) || ClinicalData.Children("LICH_SU_KHAM_BENH", "MaPhieu", receiptId).Any())
        { ReceiptNote.Text = "Lượt tiếp nhận không còn hợp lệ. Chọn lại lượt tiếp nhận."; return; }
        ClinicalUi.Workspace(this)?.ShowClinicalEditor("KhamDieuTri", "LICH_SU_KHAM_BENH", context: new("LICH_SU_KHAM_BENH", "MaPhieu", receiptId), patient: patientId);
    }
    private void Reception_Click(object sender, RoutedEventArgs e) => ClinicalUi.Workspace(this)?.ShowClinicalEditor("TiepNhanLamSang", "PHIEU_KHAM_BENH", patient: PatientId);
    private void CancelNewVisit_Click(object sender, RoutedEventArgs e) => NewVisitPanel.Visibility = Visibility.Collapsed;
}
