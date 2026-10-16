// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ hiển thị dòng thuốc/chẩn đoán/bác sĩ và các thao tác sửa/xác nhận/hủy/in; đã chú thích điều kiện bản nháp và quyền thao tác.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;

/// <summary>
/// Màn hình chi tiết đơn thuốc: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class ChiTietDonThuoc : UserControl, IPatientScoped
{
    public event Action? BackRequested;
    private Func<string, bool> canOpen = _ => true;
    /// <summary>
    /// Mã đơn thuốc đang xem, dùng tìm đúng dữ liệu sau mỗi lần cache thay đổi.
    /// </summary>
    public string OrderId { get; }
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string? PatientId => Card?.PatientId;
    /// <summary>
    /// Mô hình RxCard hiện tại của màn hình chi tiết đơn thuốc.
    /// </summary>
    private RxCard? Card => DataContext as RxCard;
    /// <summary>
    /// Khởi tạo chi tiết đơn thuốc từ XAML. Theo dõi cache khi Loaded và làm mới nội dung khi dữ liệu thay đổi; gỡ theo dõi khi Unloaded.
    /// </summary>
    public ChiTietDonThuoc(object order)
    {
        OrderId = UiRecord.Text(order, "MaDT");
        InitializeComponent();
        DataContext = new RxCard(order);
        HoTroGiaoDien.Observe(this, Refresh);
    }
    /// <summary>
    /// Nhận hàm kiểm tra quyền phân hệ từ màn hình cha, lưu lại và áp dụng cho các tab/nút liên quan trong control hiện tại.
    /// </summary>
    public void ConfigureAccess(Func<string, bool> access)
    {
        canOpen = access;
        UpdateAccess();
    }
    /// <summary>
    /// Bật các nút sửa/xác nhận/hủy cho đơn nháp có quyền, nút kê mới khi có đơn; áp dụng quyền riêng cho liên kết dịch vụ và hóa đơn.
    /// </summary>
    private void UpdateAccess()
    {
        var allowed = canOpen("DonThuoc") && (HoTroGiaoDien.Workspace(this)?.CanOpen("DonThuoc") ?? true);
        EditButton.IsEnabled = ConfirmButton.IsEnabled = CancelButton.IsEnabled = allowed && Card?.IsDraft == true;
        NewButton.IsEnabled = allowed && Card is not null;
        ServicesButton.IsEnabled=canOpen("DichVuThucHien");
        InvoiceButton.IsEnabled=canOpen("HoaDon");
    }
    /// <summary>
    /// Kiểm tra quyền từ cả màn hình và workspace trước thao tác đổi đơn; thiếu quyền thì ghi thông báo lên Feedback.
    /// </summary>
    private bool CanModify()
    {
        if (canOpen("DonThuoc") && (HoTroGiaoDien.Workspace(this)?.CanOpen("DonThuoc") ?? true)) return true;
        Feedback.Text = "Vai trò hiện tại không được thay đổi đơn thuốc.";
        return false;
    }
    /// <summary>
    /// Đọc đúng đơn theo OrderId, tạo RxCard mới và cập nhật nút; nếu đơn đã mất thì xóa DataContext và thông báo.
    /// </summary>
    private void Refresh()
    {
        var order = DuLieuGiaoDien.Find("DON_THUOC", OrderId);
        if (order is null) { DataContext = null; UpdateAccess(); Feedback.Text = "Đơn thuốc này không còn trong bộ dữ liệu hiện tại."; return; }
        DataContext = new RxCard(order); UpdateAccess();
    }
    /// <summary>
    /// Quay lại màn hình trước bằng sự kiện BackRequested/CloseRequested hoặc workspace; không tự lưu dữ liệu đang nhập.
    /// </summary>
    private void Back_Click(object sender, RoutedEventArgs e) { e.Handled = true; BackRequested?.Invoke(); }
    /// <summary>
    /// Mở hồ sơ bệnh nhân nguồn của bản ghi đang chọn, giữ mã bệnh nhân qua điều hướng của workspace.
    /// </summary>
    private void Patient_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (Card is not RxCard card || DuLieuGiaoDien.Find("BENH_NHAN", card.PatientId) is null)
        { Feedback.Text = "Không tìm thấy hồ sơ bệnh nhân của đơn thuốc."; return; }
        DieuHuongHoSo.Open(this, "BenhNhan", card.PatientId);
    }
    /// <summary>
    /// Mở lần khám nguồn của bản ghi hiện tại trong cùng hồ sơ bệnh nhân.
    /// </summary>
    private void Visit_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (Card is not RxCard card || !ValidVisit(card)) return;
        DieuHuongHoSo.Open(this, "KhamDieuTri", card.PatientId, card.Visit);
    }
    /// <summary>
    /// Mở danh sách Dịch vụ thực hiện, giữ bệnh nhân/lần khám nguồn.
    /// </summary>
    private void Services_Click(object sender,RoutedEventArgs e)
    { if(Card is RxCard card) DieuHuongHoSo.Open(this,"DichVuThucHien",card.PatientId,card.Visit,card.Code); }
    /// <summary>
    /// Mở phân hệ Hóa đơn cùng mã bệnh nhân/lần khám/đơn thuốc hiện tại nếu có.
    /// </summary>
    private void Invoice_Click(object sender,RoutedEventArgs e)
    { if(Card is RxCard card) DieuHuongHoSo.Open(this,"HoaDon",card.PatientId,card.Visit,card.Code); }
    /// <summary>
    /// Kiểm tra lần khám và bệnh nhân nguồn tồn tại, lần khám thuộc đúng bệnh nhân của đơn; ghi lỗi liên kết nếu không hợp lệ.
    /// </summary>
    private bool ValidVisit(RxCard card)
    {
        var visit = DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", card.Visit);
        if (visit is not null && !string.IsNullOrEmpty(card.PatientId)
            && DuLieuGiaoDien.Find("BENH_NHAN", card.PatientId) is not null
            && DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", visit) == card.PatientId) return true;
        Feedback.Text = "Liên kết lần khám hoặc bệnh nhân không còn hợp lệ.";
        return false;
    }
    /// <summary>
    /// Kiểm tra quyền, đơn nháp và liên kết lần khám rồi mở form sửa cả đơn trong phạm vi bệnh nhân.
    /// </summary>
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (!CanModify()) return;
        if (Card is RxCard { IsDraft: true } card && ValidVisit(card))
            HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("DonThuoc", "DON_THUOC", card.Record, patient: card.PatientId);
    }
    /// <summary>
    /// Mở form kê một đơn mới độc lập cho lần khám nguồn; giữ nguyên đơn đã xác nhận khi thay đổi chỉ định.
    /// </summary>
    private void New_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (!CanModify()) return;
        if (Card is not RxCard card || !ValidVisit(card)) return;
        HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("DonThuoc", "DON_THUOC",
            context: new CreationContext("DON_THUOC", "MaLS", card.Visit), patient: card.PatientId);
    }
    /// <summary>
    /// Kiểm tra quyền/đơn nháp/toàn bộ dòng thuốc, hỏi xác nhận, kiểm tra lại bản ghi chưa đổi rồi lưu trạng thái Đã xác nhận qua BUS.
    /// </summary>
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (!CanModify()) return;
        if (Card is not RxCard { IsDraft: true } card || !ValidVisit(card)) return;
        if (!PrescriptionValidation.TryValidate(card.Record, out var error)) { Feedback.Text = error; return; }
        if (MessageBox.Show("Xác nhận đơn thuốc đã được kiểm tra? Sau khi xác nhận, đơn được giữ nguyên. Thay đổi chỉ định cần kê đơn mới.", "Xác nhận đơn thuốc", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        // Kiểm tra lại ngay trước khi đổi trạng thái; phần nội dung đã xác nhận được giữ nguyên.
        if (!card.IsDraft || !ReferenceEquals(DuLieuGiaoDien.Find("DON_THUOC", card.Code), card.Record))
        { Feedback.Text = "Đơn thuốc đã thay đổi. Mở lại đơn trước khi xác nhận."; return; }
        if (!PrescriptionValidation.TryValidate(card.Record, out error)) { Feedback.Text = error; return; }
        try {DuLieuGiaoDien.SetValue("DON_THUOC", card.Record, "TrangThai", "Đã xác nhận"); Feedback.Text = "Đã lưu xác nhận đơn thuốc vào database.";}
        catch(InvalidOperationException ex) {Feedback.Text=ex.Message;}
    }
    /// <summary>
    /// Chỉ hủy đơn nháp có quyền sau xác nhận của người dùng và kiểm tra bản ghi chưa đổi; giữ các dòng thuốc để tra cứu.
    /// </summary>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (!CanModify()) return;
        if (Card is not RxCard { IsDraft: true } card) return;
        if (MessageBox.Show("Hủy đơn nháp này? Các dòng thuốc vẫn được giữ để tra cứu.", "Hủy đơn nháp", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        if (!card.IsDraft || !ReferenceEquals(DuLieuGiaoDien.Find("DON_THUOC", card.Code), card.Record))
        { Feedback.Text = "Đơn thuốc đã thay đổi. Mở lại đơn trước khi hủy."; return; }
        try {DuLieuGiaoDien.SetValue("DON_THUOC", card.Record, "TrangThai", "Hủy");Feedback.Text = "Đã hủy đơn nháp.";}
        catch(InvalidOperationException ex) {Feedback.Text=ex.Message;}
    }
    /// <summary>
    /// Mở cửa sổ xem trước/in đơn khi đơn đã được xác nhận, dùng RxCard của đơn đang xem làm nguồn.
    /// </summary>
    private void Print_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (Card is RxCard { CanPrint: true } card)
            new XemTruocDonThuoc(card) { Owner = Window.GetWindow(this) }.ShowDialog();
    }
}
