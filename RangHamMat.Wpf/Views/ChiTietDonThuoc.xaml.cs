using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;

public partial class ChiTietDonThuoc : UserControl, IPatientScoped
{
    public event Action? BackRequested;
    private Func<string, bool> canOpen = _ => true;
    public string OrderId { get; }
    public string? PatientId => Card?.PatientId;
    private RxCard? Card => DataContext as RxCard;
    public ChiTietDonThuoc(object order)
    {
        OrderId = UiRecord.Text(order, "MaDT");
        InitializeComponent();
        DataContext = new RxCard(order);
        ClinicalUi.Observe(this, Refresh);
    }
    public void ConfigureAccess(Func<string, bool> access)
    {
        canOpen = access;
        UpdateAccess();
    }
    private void UpdateAccess()
    {
        var allowed = canOpen("DonThuoc") && (ClinicalUi.Workspace(this)?.CanOpen("DonThuoc") ?? true);
        EditButton.IsEnabled = ConfirmButton.IsEnabled = CancelButton.IsEnabled = allowed && Card?.IsDraft == true;
        NewButton.IsEnabled = allowed && Card is not null;
        ServicesButton.IsEnabled=canOpen("DichVuThucHien");
        InvoiceButton.IsEnabled=canOpen("HoaDon");
    }
    private bool CanModify()
    {
        if (canOpen("DonThuoc") && (ClinicalUi.Workspace(this)?.CanOpen("DonThuoc") ?? true)) return true;
        Feedback.Text = "Vai trò hiện tại không được thay đổi đơn thuốc.";
        return false;
    }
    private void Refresh()
    {
        var order = ClinicalData.Find("DON_THUOC", OrderId);
        if (order is null) { DataContext = null; UpdateAccess(); Feedback.Text = "Đơn thuốc này không còn trong bộ dữ liệu hiện tại."; return; }
        DataContext = new RxCard(order); UpdateAccess();
    }
    private void Back_Click(object sender, RoutedEventArgs e) { e.Handled = true; BackRequested?.Invoke(); }
    private void Patient_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (Card is not RxCard card || ClinicalData.Find("BENH_NHAN", card.PatientId) is null)
        { Feedback.Text = "Không tìm thấy hồ sơ bệnh nhân của đơn thuốc."; return; }
        ClinicalNavigator.Open(this, "BenhNhan", card.PatientId);
    }
    private void Visit_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (Card is not RxCard card || !ValidVisit(card)) return;
        ClinicalNavigator.Open(this, "KhamDieuTri", card.PatientId, card.Visit);
    }
    private void Services_Click(object sender,RoutedEventArgs e)
    { if(Card is RxCard card) ClinicalNavigator.Open(this,"DichVuThucHien",card.PatientId,card.Visit,card.Code); }
    private void Invoice_Click(object sender,RoutedEventArgs e)
    { if(Card is RxCard card) ClinicalNavigator.Open(this,"HoaDon",card.PatientId,card.Visit,card.Code); }
    private bool ValidVisit(RxCard card)
    {
        var visit = ClinicalData.Find("LICH_SU_KHAM_BENH", card.Visit);
        if (visit is not null && !string.IsNullOrEmpty(card.PatientId)
            && ClinicalData.Find("BENH_NHAN", card.PatientId) is not null
            && ClinicalData.PatientId("LICH_SU_KHAM_BENH", visit) == card.PatientId) return true;
        Feedback.Text = "Liên kết lần khám hoặc bệnh nhân không còn hợp lệ.";
        return false;
    }
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (!CanModify()) return;
        if (Card is RxCard { IsDraft: true } card && ValidVisit(card))
            ClinicalUi.Workspace(this)?.ShowClinicalEditor("DonThuoc", "DON_THUOC", card.Record, patient: card.PatientId);
    }
    private void New_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (!CanModify()) return;
        if (Card is not RxCard card || !ValidVisit(card)) return;
        ClinicalUi.Workspace(this)?.ShowClinicalEditor("DonThuoc", "DON_THUOC",
            context: new CreationContext("DON_THUOC", "MaLS", card.Visit), patient: card.PatientId);
    }
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (!CanModify()) return;
        if (Card is not RxCard { IsDraft: true } card || !ValidVisit(card)) return;
        if (!PrescriptionValidation.TryValidate(card.Record, out var error)) { Feedback.Text = error; return; }
        if (MessageBox.Show("Xác nhận đơn thuốc đã được kiểm tra? Sau khi xác nhận, đơn được giữ nguyên. Thay đổi chỉ định cần kê đơn mới.", "Xác nhận đơn thuốc", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        // Recheck immediately before changing status; confirmed content is never edited here.
        if (!card.IsDraft || !ReferenceEquals(ClinicalData.Find("DON_THUOC", card.Code), card.Record))
        { Feedback.Text = "Đơn thuốc đã thay đổi. Mở lại đơn trước khi xác nhận."; return; }
        if (!PrescriptionValidation.TryValidate(card.Record, out error)) { Feedback.Text = error; return; }
        try {ClinicalData.SetValue("DON_THUOC", card.Record, "TrangThai", "Đã xác nhận"); Feedback.Text = "Đã lưu xác nhận đơn thuốc vào database.";}
        catch(InvalidOperationException ex) {Feedback.Text=ex.Message;}
    }
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (!CanModify()) return;
        if (Card is not RxCard { IsDraft: true } card) return;
        if (MessageBox.Show("Hủy đơn nháp này? Các dòng thuốc vẫn được giữ để tra cứu.", "Hủy đơn nháp", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        if (!card.IsDraft || !ReferenceEquals(ClinicalData.Find("DON_THUOC", card.Code), card.Record))
        { Feedback.Text = "Đơn thuốc đã thay đổi. Mở lại đơn trước khi hủy."; return; }
        try {ClinicalData.SetValue("DON_THUOC", card.Record, "TrangThai", "Hủy");Feedback.Text = "Đã hủy đơn nháp.";}
        catch(InvalidOperationException ex) {Feedback.Text=ex.Message;}
    }
    private void Print_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (Card is RxCard { CanPrint: true } card)
            new XemTruocDonThuoc(card) { Owner = Window.GetWindow(this) }.ShowDialog();
    }
}
