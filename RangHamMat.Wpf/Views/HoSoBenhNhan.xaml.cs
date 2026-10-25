// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ trình bày thông tin bệnh nhân, lịch sử và lịch hẹn, vùng chi tiết dùng chung và điều hướng sang tiếp nhận; đã giải thích ngữ cảnh từng thao tác.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>
/// Màn hình hồ sơ bệnh nhân: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class HoSoBenhNhan : UserControl, IPatientScoped
{
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string? PatientId { get; }
    private Func<string, bool> allowed = _ => true;
    private bool ready;
    private bool refreshing;
    private string? openedVisitId;
    private ChiTietKhamDieuTri? visitDetails;

    /// <summary>
    /// Khởi tạo hồ sơ bệnh nhân từ XAML. Theo dõi cache khi Loaded và làm mới nội dung khi dữ liệu thay đổi; gỡ theo dõi khi Unloaded.
    /// </summary>
    public HoSoBenhNhan(object patient)
    {
        PatientId = UiRecord.Text(patient, "MaBN");
        InitializeComponent();
        PatientAppointments.SetPatient(PatientId);
        ready = true;
        RefreshProfile();
        HoTroGiaoDien.Observe(this, RefreshProfile);
    }

    /// <summary>
    /// Thông tin hoặc nhãn bệnh nhân liên kết với bản ghi/hồ sơ đang xem.
    /// </summary>
    private object? Patient => DuLieuGiaoDien.Find("BENH_NHAN", PatientId);
    /// <summary>
    /// Bệnh án đầu tiên liên kết với bệnh nhân hiện tại trong cache để hiển thị tab bệnh án.
    /// </summary>
    private object? Medical => DuLieuGiaoDien.Children("HO_SO_BENH_AN", "MaBN", PatientId).FirstOrDefault();

    /// <summary>
    /// Nạp thông tin bệnh nhân, bệnh án và lịch sử từ cache, giữ lựa chọn lần khám hiện tại khi còn tồn tại và áp dụng lại quyền nút theo vai trò.
    /// </summary>
    private void RefreshProfile()
    {
        if (!ready) return;
        var patient = Patient;
        var medical = Medical;
        DataContext = new { Record = patient, MedicalRecord = medical };
        PatientSummary.Text = patient is null ? "Bệnh nhân không còn trong dữ liệu" : $"Mã BN: {PatientId} · {UiRecord.Text(patient, "GioiTinh")} · {UiRecord.Text(patient, "SDT")}";
        PatientSafetySummary.Text=patient is null?"":$"Dị ứng: {SafetyValue(patient,"DiUng")} · Tiền sử: {SafetyValue(patient,"TienSuBenh")}";
        PatientSafetySummary.Visibility=!DuLieuGiaoDien.PhamViLamSang || DuLieuGiaoDien.NguoiDungLamSang.LaAdmin || DuLieuGiaoDien.NguoiDungLamSang.CoQuyen(ET.QuyenLamViec.TiepNhan) || DuLieuGiaoDien.NguoiDungLamSang.CoQuyen(ET.QuyenLamViec.Kham) ? Visibility.Visible:Visibility.Collapsed;
        var hasMedical = medical is not null;
        CreateMedicalButton.Visibility = hasMedical ? Visibility.Collapsed : Visibility.Visible;
        EditMedicalButton.Visibility = hasMedical ? Visibility.Visible : Visibility.Collapsed;
        MedicalEmpty.Visibility = hasMedical ? Visibility.Collapsed : Visibility.Visible;
        MedicalFields.Visibility = hasMedical ? Visibility.Visible : Visibility.Collapsed;
        ArrivalHint.Text = hasMedical ? "Tiếp nhận trực tiếp hoặc chọn lịch hẹn của người bệnh khi đến khám." : "Chưa có hồ sơ bệnh án; hệ thống lập một hồ sơ khi tiếp nhận thành công.";
        ReceptionButton.ToolTip = "Chọn dịch vụ và bác sĩ phù hợp trước khi xác nhận tiếp nhận";
        ApplyAccess();
        refreshing = true;
        var visits = DuLieuGiaoDien.Rows("LICH_SU_KHAM_BENH")
            .Where(Controls.ClinicalWorkflow.QuyenNghiepVu.DuocXemNoiDungKham)
            .Where(row => patient is not null && !string.IsNullOrWhiteSpace(PatientId) && DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", row) == PatientId)
            .OrderByDescending(row => UiRecord.Date(row, "ThoiGianKham"))
            .Select(row => new PatientVisitRow(row)).ToArray();
        HistoryGrid.ItemsSource = visits;
        HistoryCount.Text = $"{visits.Length} lần khám · Chỉ hiển thị dữ liệu của bệnh nhân {PatientId}";
        HistoryEmpty.Visibility = visits.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        refreshing = false;
        // Kiểm tra lại liên kết sau mỗi lần dữ liệu đổi trước khi giữ vùng chi tiết lần khám.
        if (openedVisitId is not null)
        {
            var visit = DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", openedVisitId);
            if (visit is null || DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", visit) != PatientId || !allowed("KhamDieuTri")) ReturnToHistory();
        }
    }

    private static string SafetyValue(object patient,string field)=>string.IsNullOrWhiteSpace(UiRecord.Text(patient,field))?"chưa ghi nhận, cần đối chiếu":UiRecord.Text(patient,field);

    /// <summary>
    /// Nhận hàm kiểm tra quyền phân hệ từ màn hình cha, lưu lại và áp dụng cho các tab/nút liên quan trong control hiện tại.
    /// </summary>
    public void ConfigureAccess(Func<string, bool> access)
    {
        allowed = access;
        ApplyAccess();
        visitDetails?.ConfigureAccess(access);
    }

    /// <summary>
    /// Áp dụng quyền hiển thị/sử dụng các tab và nút lịch khám, điều trị, dịch vụ, hóa đơn cho hồ sơ đang xem.
    /// </summary>
    private void ApplyAccess()
    {
        var patientExists = Patient is not null;
        var reception = Controls.ClinicalWorkflow.QuyenNghiepVu.DuocTao("BENH_NHAN");
        EditPatientButton.IsEnabled = patientExists && allowed("BenhNhan") && reception;
        CreateMedicalButton.IsEnabled = patientExists && allowed("BenhNhan") && reception;
        EditMedicalButton.IsEnabled = patientExists && allowed("BenhNhan") && reception;
        ReceptionButton.IsEnabled = patientExists && allowed("BenhNhan") && reception;
        PhoneButton.IsEnabled = patientExists && allowed("LichKham");
        ServicesButton.IsEnabled = patientExists && allowed("DichVuThucHien");
        InvoiceButton.IsEnabled = patientExists && allowed("HoaDon");
        AppointmentsTab.Visibility = allowed("BenhNhan") ? Visibility.Visible : Visibility.Collapsed;
        HistoryTab.Visibility = allowed("KhamDieuTri") ? Visibility.Visible : Visibility.Collapsed;
        if (ProfileTabs.SelectedItem is TabItem selected && selected.Visibility != Visibility.Visible) ProfileTabs.SelectedItem = InformationTab;
        if (!allowed("KhamDieuTri")) ReturnToHistory();
    }

    /// <summary>
    /// Chọn tab bệnh án để người dùng xem hoặc tạo/sửa thông tin bệnh án của bệnh nhân hiện tại.
    /// </summary>
    public void OpenMedicalRecord()
    {
        ProfileTabs.SelectedItem = MedicalTab;
        if (Medical is null) CreateMedical_Click(this, new RoutedEventArgs());
    }

    /// <summary>
    /// Tìm lần khám theo mã và kiểm tra thuộc bệnh nhân hiện tại trước khi mở control chi tiết; không chọn lần khám khác thay thế mã sai.
    /// </summary>
    public void OpenVisit(string id)
    {
        var visit = DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", id);
        if (!allowed("KhamDieuTri")) { MessageBox.Show("Vai trò hiện tại không có quyền xem khám điều trị.", "Hồ sơ bệnh nhân"); return; }
        if (visit is null || DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", visit) != PatientId)
        { ReturnToHistory(); MessageBox.Show("Lần khám không tồn tại hoặc không thuộc bệnh nhân này.", "Hồ sơ bệnh nhân"); return; }
        openedVisitId = id;
        try { visitDetails = new ChiTietKhamDieuTri(visit,historyReadOnly:true); }
        catch (InvalidOperationException ex) { ReturnToHistory(); HistoryCount.Text = ex.Message; return; }
        visitDetails.ConfigureAccess(allowed);
        visitDetails.BackRequested += ReturnToHistory;
        VisitHost.Content = visitDetails;
        VisitHost.Visibility = Visibility.Visible;
        HistoryListPanel.Visibility = Visibility.Collapsed;
        ProfileTabs.SelectedItem = HistoryTab;
    }

    /// <summary>
    /// Đưa tab lịch sử trở lại bảng danh sách và gỡ vùng chi tiết lần khám đang mở.
    /// </summary>
    private void ReturnToHistory()
    {
        if (visitDetails is not null) visitDetails.BackRequested -= ReturnToHistory;
        visitDetails = null; openedVisitId = null;
        VisitHost.Content = null; VisitHost.Visibility = Visibility.Collapsed; HistoryListPanel.Visibility = Visibility.Visible;
        refreshing = true; HistoryGrid.SelectedItem = null; refreshing = false;
    }

    /// <summary>
    /// Mở biểu mẫu sửa bệnh nhân hiện tại trong phạm vi hồ sơ này.
    /// </summary>
    private void EditPatient_Click(object sender, RoutedEventArgs e)
    {
        if (Patient is object patient && allowed("BenhNhan")) HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("BenhNhan", "BENH_NHAN", patient, patient: PatientId);
    }
    /// <summary>
    /// Tạo bệnh án mới nếu bệnh nhân chưa có bệnh án; mã bệnh nhân được truyền làm liên kết cha của biểu mẫu.
    /// </summary>
    private void CreateMedical_Click(object sender, RoutedEventArgs e)
    {
        if (Patient is not object patient || !allowed("BenhNhan")) return;
        if (Medical is not null) { RefreshProfile(); return; }
        HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("BenhNhan", "HO_SO_BENH_AN", context: new CreationContext("HO_SO_BENH_AN", "MaBN", UiRecord.Get(patient, "MaBN")!), patient: PatientId);
    }
    /// <summary>
    /// Mở biểu mẫu sửa bệnh án hiện tại, giữ liên kết bệnh nhân nguồn.
    /// </summary>
    private void EditMedical_Click(object sender, RoutedEventArgs e)
    {
        if (Medical is object medical && allowed("BenhNhan")) HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("BenhNhan", "HO_SO_BENH_AN", medical, patient: PatientId);
    }
    /// <summary>
    /// Kiểm tra bệnh án và mở form tiếp nhận trực tiếp gắn với bệnh án của bệnh nhân hiện tại.
    /// </summary>
    private void Reception_Click(object sender, RoutedEventArgs e)
    {
        if (Patient is null || !allowed("BenhNhan")) return;
        var context=Medical is object medical ? new CreationContext("PHIEU_KHAM_BENH","MaHS",UiRecord.Get(medical,"MaHS")!) : null;
        HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("TiepNhanKham", "PHIEU_KHAM_BENH", context:context, patient: PatientId);
    }
    /// <summary>
    /// Mở form đặt lịch khám qua điện thoại với mã bệnh nhân hiện tại làm liên kết nguồn.
    /// </summary>
    private void PhoneAppointment_Click(object sender, RoutedEventArgs e)
    {
        if (Patient is object patient && allowed("LichKham")) HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("LichKham", "LICH_KHAM", context: new CreationContext("LICH_KHAM", "MaBN", UiRecord.Get(patient, "MaBN")!), patient: PatientId);
    }
    /// <summary>
    /// Mở chi tiết lần khám được chọn trong tab lịch sử của đúng bệnh nhân.
    /// </summary>
    private void History_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || refreshing || HistoryGrid.SelectedItem is not PatientVisitRow row) return;
        OpenVisit(row.Id);
    }
    /// <summary>
    /// Mở phân hệ Khám điều trị với ngữ cảnh bệnh nhân/lần khám của hồ sơ đang xem.
    /// </summary>
    private void Treatment_Click(object sender, RoutedEventArgs e)
    {
        if (Patient is not null && allowed("KhamDieuTri")) DieuHuongHoSo.Open(this, "KhamDieuTri", PatientId!);
    }
    /// <summary>
    /// Mở danh sách Dịch vụ thực hiện, giữ bệnh nhân/lần khám nguồn.
    /// </summary>
    private void Services_Click(object sender, RoutedEventArgs e)
    {
        if (Patient is not null && allowed("DichVuThucHien")) DieuHuongHoSo.Open(this, "DichVuThucHien", PatientId!);
    }
    /// <summary>
    /// Mở phân hệ Hóa đơn cùng mã bệnh nhân/lần khám/đơn thuốc hiện tại nếu có.
    /// </summary>
    private void Invoice_Click(object sender, RoutedEventArgs e)
    {
        if (Patient is not null && allowed("HoaDon")) DieuHuongHoSo.Open(this, "HoaDon", PatientId!);
    }
}
