using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

public partial class HoSoBenhNhan : UserControl, IPatientScoped
{
    public string? PatientId { get; }
    private Func<string, bool> allowed = _ => true;
    private bool ready;
    private bool refreshing;
    private string? openedVisitId;
    private ChiTietKhamDieuTri? visitDetails;

    public HoSoBenhNhan(object patient)
    {
        PatientId = UiRecord.Text(patient, "MaBN");
        InitializeComponent();
        PatientAppointments.SetPatient(PatientId);
        ready = true;
        RefreshProfile();
        ClinicalUi.Observe(this, RefreshProfile);
    }

    private object? Patient => ClinicalData.Find("BENH_NHAN", PatientId);
    private object? Medical => ClinicalData.Children("HO_SO_BENH_AN", "MaBN", PatientId).FirstOrDefault();

    private void RefreshProfile()
    {
        if (!ready) return;
        var patient = Patient;
        var medical = Medical;
        DataContext = new { Record = patient, MedicalRecord = medical };
        PatientSummary.Text = patient is null ? "Bệnh nhân không còn trong dữ liệu" : $"Mã BN: {PatientId} · {UiRecord.Text(patient, "GioiTinh")} · {UiRecord.Text(patient, "SDT")}";
        var hasMedical = medical is not null;
        CreateMedicalButton.Visibility = hasMedical ? Visibility.Collapsed : Visibility.Visible;
        EditMedicalButton.Visibility = hasMedical ? Visibility.Visible : Visibility.Collapsed;
        MedicalEmpty.Visibility = hasMedical ? Visibility.Collapsed : Visibility.Visible;
        MedicalFields.Visibility = hasMedical ? Visibility.Visible : Visibility.Collapsed;
        ArrivalHint.Text = hasMedical ? "Trực tiếp: tạo lượt tiếp nhận. Qua điện thoại: tạo lịch hẹn, tiếp nhận khi bệnh nhân đến." : "Cần lập hồ sơ bệnh án trước khi tiếp nhận. Có thể đặt lịch qua điện thoại trước.";
        ReceptionButton.ToolTip = hasMedical ? "Tạo lượt tiếp nhận không liên kết lịch hẹn" : "Lập hồ sơ bệnh án trước khi tiếp nhận";
        ApplyAccess();
        refreshing = true;
        var visits = ClinicalData.Rows("LICH_SU_KHAM_BENH")
            .Where(row => patient is not null && !string.IsNullOrWhiteSpace(PatientId) && ClinicalData.PatientId("LICH_SU_KHAM_BENH", row) == PatientId)
            .OrderByDescending(row => UiRecord.Date(row, "ThoiGianKham"))
            .Select(row => new PatientVisitRow(row)).ToArray();
        HistoryGrid.ItemsSource = visits;
        HistoryCount.Text = $"{visits.Length} lần khám · Chỉ hiển thị dữ liệu của bệnh nhân {PatientId}";
        HistoryEmpty.Visibility = visits.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        refreshing = false;
        // Revalidate links after each session update before retaining a visit view.
        if (openedVisitId is not null)
        {
            var visit = ClinicalData.Find("LICH_SU_KHAM_BENH", openedVisitId);
            if (visit is null || ClinicalData.PatientId("LICH_SU_KHAM_BENH", visit) != PatientId || !allowed("KhamDieuTri")) ReturnToHistory();
        }
    }

    public void ConfigureAccess(Func<string, bool> access)
    {
        allowed = access;
        ApplyAccess();
        visitDetails?.ConfigureAccess(access);
    }

    private void ApplyAccess()
    {
        var patientExists = Patient is not null;
        EditPatientButton.IsEnabled = patientExists && allowed("BenhNhan");
        CreateMedicalButton.IsEnabled = patientExists && allowed("BenhNhan");
        EditMedicalButton.IsEnabled = patientExists && allowed("BenhNhan");
        ReceptionButton.IsEnabled = patientExists && allowed("BenhNhan");
        PhoneButton.IsEnabled = patientExists && allowed("LichKham");
        ServicesButton.IsEnabled = patientExists && allowed("DichVuThucHien");
        InvoiceButton.IsEnabled = patientExists && allowed("HoaDon");
        AppointmentsTab.Visibility = allowed("LichKham") ? Visibility.Visible : Visibility.Collapsed;
        HistoryTab.Visibility = allowed("KhamDieuTri") ? Visibility.Visible : Visibility.Collapsed;
        if (ProfileTabs.SelectedItem is TabItem selected && selected.Visibility != Visibility.Visible) ProfileTabs.SelectedItem = InformationTab;
        if (!allowed("KhamDieuTri")) ReturnToHistory();
    }

    public void OpenMedicalRecord()
    {
        ProfileTabs.SelectedItem = MedicalTab;
        if (Medical is null) CreateMedical_Click(this, new RoutedEventArgs());
    }

    public void OpenVisit(string id)
    {
        var visit = ClinicalData.Find("LICH_SU_KHAM_BENH", id);
        if (!allowed("KhamDieuTri")) { MessageBox.Show("Vai trò hiện tại không có quyền xem khám điều trị.", "Hồ sơ bệnh nhân"); return; }
        if (visit is null || ClinicalData.PatientId("LICH_SU_KHAM_BENH", visit) != PatientId)
        { ReturnToHistory(); MessageBox.Show("Lần khám không tồn tại hoặc không thuộc bệnh nhân này.", "Hồ sơ bệnh nhân"); return; }
        openedVisitId = id;
        visitDetails = new ChiTietKhamDieuTri(visit);
        visitDetails.ConfigureAccess(allowed);
        visitDetails.BackRequested += ReturnToHistory;
        VisitHost.Content = visitDetails;
        VisitHost.Visibility = Visibility.Visible;
        HistoryListPanel.Visibility = Visibility.Collapsed;
        ProfileTabs.SelectedItem = HistoryTab;
    }

    private void ReturnToHistory()
    {
        if (visitDetails is not null) visitDetails.BackRequested -= ReturnToHistory;
        visitDetails = null; openedVisitId = null;
        VisitHost.Content = null; VisitHost.Visibility = Visibility.Collapsed; HistoryListPanel.Visibility = Visibility.Visible;
        refreshing = true; HistoryGrid.SelectedItem = null; refreshing = false;
    }

    private void EditPatient_Click(object sender, RoutedEventArgs e)
    {
        if (Patient is object patient && allowed("BenhNhan")) ClinicalUi.Workspace(this)?.ShowClinicalEditor("BenhNhan", "BENH_NHAN", patient, patient: PatientId);
    }
    private void CreateMedical_Click(object sender, RoutedEventArgs e)
    {
        if (Patient is not object patient || !allowed("BenhNhan")) return;
        if (Medical is not null) { RefreshProfile(); return; }
        ClinicalUi.Workspace(this)?.ShowClinicalEditor("BenhNhan", "HO_SO_BENH_AN", context: new CreationContext("HO_SO_BENH_AN", "MaBN", UiRecord.Get(patient, "MaBN")!), patient: PatientId);
    }
    private void EditMedical_Click(object sender, RoutedEventArgs e)
    {
        if (Medical is object medical && allowed("BenhNhan")) ClinicalUi.Workspace(this)?.ShowClinicalEditor("BenhNhan", "HO_SO_BENH_AN", medical, patient: PatientId);
    }
    private void Reception_Click(object sender, RoutedEventArgs e)
    {
        if (Patient is null || !allowed("BenhNhan")) return;
        if (Medical is not object medical) { ProfileTabs.SelectedItem = MedicalTab; MessageBox.Show("Lập và lưu hồ sơ bệnh án trước khi tiếp nhận bệnh nhân.", "Tiếp nhận trực tiếp"); return; }
        ClinicalUi.Workspace(this)?.ShowClinicalEditor("TiepNhanLamSang", "PHIEU_KHAM_BENH", context: new CreationContext("PHIEU_KHAM_BENH", "MaHS", UiRecord.Get(medical, "MaHS")!), patient: PatientId);
    }
    private void PhoneAppointment_Click(object sender, RoutedEventArgs e)
    {
        if (Patient is object patient && allowed("LichKham")) ClinicalUi.Workspace(this)?.ShowClinicalEditor("LichKham", "LICH_KHAM", context: new CreationContext("LICH_KHAM", "MaBN", UiRecord.Get(patient, "MaBN")!), patient: PatientId);
    }
    private void History_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || refreshing || HistoryGrid.SelectedItem is not PatientVisitRow row) return;
        OpenVisit(row.Id);
    }
    private void Treatment_Click(object sender, RoutedEventArgs e)
    {
        if (Patient is not null && allowed("KhamDieuTri")) ClinicalNavigator.Open(this, "KhamDieuTri", PatientId!);
    }
    private void Services_Click(object sender, RoutedEventArgs e)
    {
        if (Patient is not null && allowed("DichVuThucHien")) ClinicalNavigator.Open(this, "DichVuThucHien", PatientId!);
    }
    private void Invoice_Click(object sender, RoutedEventArgs e)
    {
        if (Patient is not null && allowed("HoaDon")) ClinicalNavigator.Open(this, "HoaDon", PatientId!);
    }
}
