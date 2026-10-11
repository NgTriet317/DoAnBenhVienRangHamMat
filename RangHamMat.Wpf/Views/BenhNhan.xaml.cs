using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

public partial class BenhNhan : UserControl, IPatientScoped
{
    private const int PageSize = 15;
    private bool ready;
    private bool refreshing;
    private int currentPage = 1;
    private object[] filtered = Array.Empty<object>();
    private HoSoBenhNhan? profile;
    private Func<string, bool>? access;
    public string? PatientId => profile?.PatientId;

    public BenhNhan()
    {
        InitializeComponent();
        ready = true;
        ClinicalUi.Observe(this, Refresh);
        Refresh();
    }

    public void ConfigureAccess(Func<string, bool> allowed)
    {
        access = allowed;
        AddPatientButton.IsEnabled = allowed("BenhNhan");
        profile?.ConfigureAccess(allowed);
    }

    public void OpenProfile(object record, Func<string, bool>? allowed = null)
    {
        if (allowed is not null) access = allowed;
        var id = UiRecord.Text(record, "MaBN");
        var patient = ClinicalData.Find("BENH_NHAN", id);
        if (patient is null) { ClearProfile("Bệnh nhân không còn trong dữ liệu. Chọn một bệnh nhân khác."); return; }
        if (profile?.PatientId != id)
        {
            profile = new HoSoBenhNhan(patient);
            ProfileHost.Content = profile;
        }
        if (access is not null) profile.ConfigureAccess(access);
        ProfileHost.Visibility = Visibility.Visible;
        ProfileEmpty.Visibility = Visibility.Collapsed;
        refreshing = true;
        Grid_BENH_NHAN.SelectedItem = Grid_BENH_NHAN.Items.Cast<object>().FirstOrDefault(row => UiRecord.Text(row, "MaBN") == id);
        refreshing = false;
    }

    public void FocusContext(string? patientId, string? visitId = null, string? prescriptionId = null)
    {
        var patient = ClinicalData.Find("BENH_NHAN", patientId);
        if (patient is null)
        {
            ClearProfile(string.IsNullOrWhiteSpace(patientId) ? "Chọn một bệnh nhân để mở hồ sơ." : "Không tìm thấy bệnh nhân được yêu cầu. Chọn lại từ danh sách.");
            return;
        }
        // Make an explicitly requested patient visible even when filters were active.
        refreshing = true;
        SearchBox.Clear(); GenderFilter.SelectedIndex = 0; StatusFilter.SelectedIndex = 0;
        refreshing = false;
        Refresh();
        var index = Array.FindIndex(filtered, row => UiRecord.Text(row, "MaBN") == patientId);
        currentPage = index < 0 ? 1 : index / PageSize + 1;
        Refresh(); OpenProfile(patient);
        if (!string.IsNullOrWhiteSpace(prescriptionId))
        {
            var prescription = ClinicalData.Find("DON_THUOC", prescriptionId);
            if (prescription is null || ClinicalData.PatientId("DON_THUOC", prescription) != PatientId)
            { MessageBox.Show("Đơn thuốc không tồn tại hoặc không thuộc bệnh nhân này.", "Hồ sơ bệnh nhân"); return; }
            ClinicalNavigator.Open(this, "DonThuoc", PatientId!, UiRecord.Text(prescription, "MaLS"), prescriptionId);
            return;
        }
        if (!string.IsNullOrWhiteSpace(visitId)) profile?.OpenVisit(visitId);
    }

    public void OpenMedicalRecord() => profile?.OpenMedicalRecord();

    private void ClearProfile(string message)
    {
        profile = null; ProfileHost.Content = null; ProfileHost.Visibility = Visibility.Collapsed;
        ProfileEmpty.Visibility = Visibility.Visible; ProfileEmptyText.Text = message;
        refreshing = true; Grid_BENH_NHAN.SelectedItem = null; refreshing = false;
    }

    private void Refresh()
    {
        if (!ready) return;
        var selected = PatientId;
        filtered = ClinicalData.Rows("BENH_NHAN").Where(row =>
            ClinicalUi.Matches("BENH_NHAN", row, SearchBox.Text, ClinicalUi.Choice(StatusFilter)) &&
            (ClinicalUi.Choice(GenderFilter) == "Tất cả" || UiRecord.Text(row, "GioiTinh") == ClinicalUi.Choice(GenderFilter)))
            .OrderBy(row => UiRecord.Text(row, "HoTenBN"), StringComparer.CurrentCultureIgnoreCase).ToArray();
        var pages = Math.Max(1, (int)Math.Ceiling(filtered.Length / (double)PageSize));
        currentPage = Math.Clamp(currentPage, 1, pages);
        refreshing = true;
        Grid_BENH_NHAN.ItemsSource = filtered.Skip((currentPage - 1) * PageSize).Take(PageSize).ToArray();
        Grid_BENH_NHAN.SelectedItem = Grid_BENH_NHAN.Items.Cast<object>().FirstOrDefault(row => UiRecord.Text(row, "MaBN") == selected);
        refreshing = false;
        RecordCount.Text = $"{filtered.Length} bệnh nhân · chọn để mở hồ sơ";
        PageLabel.Text = $"{currentPage} / {pages}";
        PreviousButton.IsEnabled = currentPage > 1; NextButton.IsEnabled = currentPage < pages;
        ListEmpty.Visibility = filtered.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        ListEmpty.Text = ClinicalData.Rows("BENH_NHAN").Count == 0 ? "Chưa có bệnh nhân. Thêm bệnh nhân để bắt đầu." : "Không có bệnh nhân phù hợp với tìm kiếm và bộ lọc.";
        if (selected is not null && ClinicalData.Find("BENH_NHAN", selected) is null) ClearProfile("Bệnh nhân không còn trong dữ liệu. Chọn lại từ danh sách.");
    }

    private void Filter_Changed(object sender, RoutedEventArgs e) { if (!ready || refreshing) return; currentPage = 1; Refresh(); }
    private void Previous_Click(object sender, RoutedEventArgs e) { currentPage--; Refresh(); }
    private void Next_Click(object sender, RoutedEventArgs e) { currentPage++; Refresh(); }
    private void AddPatient_Click(object sender, RoutedEventArgs e) => ClinicalUi.Workspace(this)?.ShowClinicalEditor("BenhNhan", "BENH_NHAN");
    private void Patient_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || refreshing || Grid_BENH_NHAN.SelectedItem is not object patient) return;
        var workspace = ClinicalUi.Workspace(this);
        if (workspace is not null) workspace.ShowClinicalDetails("BENH_NHAN", patient);
        else OpenProfile(patient);
    }
}
