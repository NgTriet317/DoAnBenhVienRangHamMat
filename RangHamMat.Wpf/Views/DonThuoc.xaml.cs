using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;

public partial class DonThuoc : UserControl, IPatientScoped
{
    private bool updating;
    private string? visitId;
    private string? selectedId;
    private string? requestedId;
    public string? PatientId { get; private set; }
    public DonThuoc()
    {
        InitializeComponent();
        ClinicalUi.Observe(this, Refresh);
        Refresh();
    }
    public void SetPatient(string id) => FocusContext(id);
    public void FocusContext(string? patientId, string? visitId = null, string? prescriptionId = null)
    {
        PatientId = patientId;
        this.visitId = visitId;
        requestedId = prescriptionId;
        selectedId = prescriptionId;
        updating = true;
        SearchBox.Clear(); StatusFilter.SelectedIndex = 0;
        updating = false;
        Refresh();
    }
    private void Refresh()
    {
        if (PrescriptionList is null || updating) return;
        updating = true;
        PatientFilter.ItemsSource = ClinicalData.Options("BENH_NHAN", all: true).ToArray();
        PatientFilter.SelectedValue = PatientId ?? "";
        var rows = ClinicalData.Rows("DON_THUOC")
            .Where(row => ClinicalUi.Matches("DON_THUOC", row, SearchBox.Text, ClinicalUi.Choice(StatusFilter), PatientId))
            .Where(row => string.IsNullOrEmpty(visitId) || UiRecord.Text(row, "MaLS") == visitId)
            .OrderByDescending(row => UiRecord.Date(row, "NgayKe"))
            .Select(row => new RxCard(row)).ToArray();
        PrescriptionList.ItemsSource = rows;
        PrescriptionList.SelectedItem = rows.FirstOrDefault(row => row.Code == selectedId);
        RecordCount.Text = $"{rows.Length} đơn thuốc";
        ListEmpty.Visibility = rows.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        updating = false;
        ShowSelected();
    }
    private void ShowSelected()
    {
        if (PrescriptionList.SelectedItem is RxCard card)
        {
            selectedId = card.Code;
            requestedId = null;
            if (DetailHost.Content is not ChiTietDonThuoc detail || detail.OrderId != card.Code)
            {
                var view = new ChiTietDonThuoc(card.Record);
                view.BackRequested += () => { PrescriptionList.SelectedItem = null; };
                DetailHost.Content = view;
            }
            DetailEmpty.Visibility = Visibility.Collapsed;
        }
        else
        {
            DetailHost.Content = null;
            DetailEmpty.Visibility = Visibility.Visible;
            DetailEmptyText.Text = requestedId is not null
                ? "Không tìm thấy đơn thuốc được yêu cầu trong ngữ cảnh bệnh nhân và lần khám này."
                : "Thông tin lần khám và thuốc kê sẽ hiện tại đây.";
        }
    }
    private void Order_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updating) return;
        selectedId = (PrescriptionList.SelectedItem as RxCard)?.Code;
        ShowSelected();
    }
    private void Filter_Changed(object sender, RoutedEventArgs e) => Refresh();
    private void Patient_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updating) return;
        PatientId = string.IsNullOrEmpty(ClinicalUi.Selected(PatientFilter)) ? null : ClinicalUi.Selected(PatientFilter);
        visitId = null; requestedId = null; selectedId = null; Refresh();
    }
    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        updating = true; SearchBox.Clear(); StatusFilter.SelectedIndex = 0; updating = false;
        PatientId = null; visitId = null; requestedId = null; selectedId = null; Refresh();
    }
    private void Create_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (PatientId is not null && ClinicalData.Find("BENH_NHAN", PatientId) is null)
        { Feedback.Text = "Không tìm thấy bệnh nhân được yêu cầu."; return; }
        if (!ClinicalData.Rows("LICH_SU_KHAM_BENH").Any(row =>
            (string.IsNullOrEmpty(PatientId) || ClinicalData.PatientId("LICH_SU_KHAM_BENH", row) == PatientId)
            && (string.IsNullOrEmpty(visitId) || UiRecord.Text(row, "MaLS") == visitId)))
        { Feedback.Text = "Cần có một lần khám đã lưu của bệnh nhân trước khi kê đơn."; return; }
        var context = string.IsNullOrEmpty(visitId) ? null : new CreationContext("DON_THUOC", "MaLS", visitId);
        ClinicalUi.Workspace(this)?.ShowClinicalEditor("DonThuoc", "DON_THUOC", context: context, patient: PatientId);
    }
}
