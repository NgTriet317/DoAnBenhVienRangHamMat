using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
public partial class ChiTietLamSang : UserControl, IPatientScoped
{
    private readonly string table;
    private readonly object record;
    public string? PatientId { get; }
    public ChiTietLamSang(string table, string title, object record)
    {
        this.table = table; this.record = record;
        InitializeComponent();
        EditButton.IsEnabled = ClinicalData.CanEdit(table, record) || (table == "LICH_KHAM" && (UiRecord.Text(record, "TrangThai") is "Đã xác nhận" or "Chờ khám"));
        if (table == "LICH_KHAM")
        {
            EditButton.Content = ClinicalData.CanEdit(table, record) ? "Chỉnh sửa" : "Hẹn lại";
            ConfirmButton.Visibility = ClinicalData.CanEdit(table, record) ? Visibility.Visible : Visibility.Collapsed;
            CancelButton.Visibility = UiRecord.Text(record, "TrangThai") is "Hủy" or "Đã đổi" or "Hoàn thành" ? Visibility.Collapsed : Visibility.Visible;
            ReceptionButton.Visibility = UiRecord.Text(record, "TrangThai") is "Đã xác nhận" or "Chờ khám" ? Visibility.Visible : Visibility.Collapsed;
        }
        PatientId = ClinicalData.PatientId(table, record);
        DetailTitle.Text = title;
        DetailBody.Content = record;
        DetailBody.ContentTemplate = (DataTemplate)FindResource(table + "Details");
        if (table is "LICH_SU_KHAM_BENH" or "DON_THUOC")
        {
            RelatedCard.Content = table == "DON_THUOC" ? (object)new PrescriptionCard(record, true) : new ExaminationCard(record, true);
            RelatedCard.ContentTemplate = (DataTemplate)FindResource(table == "DON_THUOC" ? "PrescriptionCard" : "ExaminationCard");
        }
        if (table == "KE_HOACH_DIEU_TRI")
        {
            PlanDetails.Visibility = Visibility.Visible;
            PlanSteps.ItemsSource = ClinicalData.Children("CHI_TIET_KE_HOACH", "MaKHDT", UiRecord.Get(record, "MaKHDT"));
            AddStep.CommandParameter = new CreationContext("CHI_TIET_KE_HOACH", "MaKHDT", UiRecord.Get(record, "MaKHDT")!);
            AddStep.IsEnabled = UiRecord.Text(record, "MaKHDT").Length > 0;
        }
        if (table == "LICH_SU_KHAM_BENH" && UiRecord.Text(record, "MaLS").Length > 0)
        {
            RelatedActions.Visibility = Visibility.Visible;
            var id = UiRecord.Get(record, "MaLS")!;
            AddCondition.CommandParameter = new CreationContext("TINH_TRANG_BENH", "MaLS", id);
            AddPlan.CommandParameter = new CreationContext("KE_HOACH_DIEU_TRI", "MaLS", id);
            AddService.CommandParameter = new CreationContext("DICH_VU_THUC_HIEN", "MaLS", id);
        }
    }
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        var module = table switch { "BENH_NHAN" or "HO_SO_BENH_AN" => "BenhNhan", "LICH_KHAM" => "LichKham", "DON_THUOC" or "CHI_TIET_DON_THUOC" => "DonThuoc", _ => "KhamDieuTri" };
        ClinicalUi.Workspace(this)?.ShowClinicalEditor(module, table, record, patient: PatientId);
    }
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (UiRecord.Date(record, "NgayGioKham") is null || ClinicalData.Find("BENH_NHAN",UiRecord.Get(record,"MaBN")) is null || ClinicalData.Find("NHAN_VIEN",UiRecord.Get(record,"MaBS")) is null) { MessageBox.Show("Kiểm tra ngày giờ, bệnh nhân và bác sĩ trước khi xác nhận."); return; }
        try {ClinicalData.SetValue(table, record, "TrangThai", "Đã xác nhận"); ClinicalUi.Workspace(this)?.ReturnToPage();}
        catch(InvalidOperationException ex) {MessageBox.Show(ex.Message,"Xác nhận lịch khám");}
    }
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Hủy lịch hẹn này?", "Lịch khám", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try {ClinicalData.SetValue(table, record, "TrangThai", "Hủy"); ClinicalUi.Workspace(this)?.ReturnToPage();}
        catch(InvalidOperationException ex) {MessageBox.Show(ex.Message,"Hủy lịch khám");}
    }
    private void Reception_Click(object sender, RoutedEventArgs e)
    {
        var medical = ClinicalData.Children("HO_SO_BENH_AN", "MaBN", PatientId).FirstOrDefault();
        if (medical is null) { MessageBox.Show("Lập hồ sơ bệnh án cho bệnh nhân trước khi tiếp nhận."); return; }
        ClinicalUi.Workspace(this)?.ShowClinicalEditor("TiepNhanLamSang", "PHIEU_KHAM_BENH", new Dictionary<string, object?> { ["MaHS"] = UiRecord.Get(medical,"MaHS"), ["MaLich"] = UiRecord.Get(record,"MaLich"), ["MaBS"] = UiRecord.Get(record,"MaBS"), ["MaPhong"] = UiRecord.Get(record,"MaPhong"), ["MaDV"] = UiRecord.Get(record,"MaDV"), ["LyDoKham"] = UiRecord.Get(record,"LyDoKham") }, patient: PatientId);
    }
}
