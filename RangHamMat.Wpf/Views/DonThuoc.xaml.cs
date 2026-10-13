// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ bộ lọc đơn thuốc theo bệnh nhân/lần khám, mở chi tiết và tạo đơn từ nguồn hợp lệ; đã chú thích sự kiện và ngữ cảnh.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;

/// <summary>
/// Màn hình danh sách đơn thuốc: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class DonThuoc : UserControl, IPatientScoped
{
    private bool updating;
    private string? visitId;
    private string? selectedId;
    private string? requestedId;
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string? PatientId { get; private set; }
    /// <summary>
    /// Khởi tạo danh sách đơn thuốc từ XAML. Theo dõi cache khi Loaded và làm mới nội dung khi dữ liệu thay đổi; gỡ theo dõi khi Unloaded.
    /// </summary>
    public DonThuoc()
    {
        InitializeComponent();
        HoTroGiaoDien.Observe(this, Refresh);
        Refresh();
    }
    /// <summary>
    /// Áp dụng mã bệnh nhân nguồn và làm mới dữ liệu trong phạm vi hồ sơ này.
    /// </summary>
    public void SetPatient(string id) => FocusContext(id);
    /// <summary>
    /// Áp dụng bộ mã bệnh nhân/lần khám/đơn thuốc từ màn hình nguồn; lựa chọn đúng đơn hoặc hiển thị trống nếu mã không hợp lệ.
    /// </summary>
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
    /// <summary>
    /// Lọc đơn theo bệnh nhân/lần khám/từ khóa/trạng thái, giữ mã đơn được yêu cầu khi còn khớp và cập nhật vùng chưa chọn.
    /// </summary>
    private void Refresh()
    {
        if (PrescriptionList is null || updating) return;
        updating = true;
        PatientFilter.ItemsSource = DuLieuGiaoDien.Options("BENH_NHAN", all: true).ToArray();
        PatientFilter.SelectedValue = PatientId ?? "";
        var rows = DuLieuGiaoDien.Rows("DON_THUOC")
            .Where(row => HoTroGiaoDien.Matches("DON_THUOC", row, SearchBox.Text, HoTroGiaoDien.Choice(StatusFilter), PatientId))
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
    /// <summary>
    /// Tạo ChiTietDonThuoc cho đơn được chọn và truyền quyền truy cập, hoặc hiển thị trạng thái chưa có lựa chọn.
    /// </summary>
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
    /// <summary>
    /// Hiển thị chi tiết đơn thuốc mới được chọn; bỏ qua sự kiện nếu danh sách đang được làm mới.
    /// </summary>
    private void Order_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updating) return;
        selectedId = (PrescriptionList.SelectedItem as RxCard)?.Code;
        ShowSelected();
    }
    /// <summary>
    /// Lọc và hiển thị lại danh sách theo nội dung tìm kiếm/trạng thái/ngày đang chọn; chưa thay đổi dữ liệu SQL.
    /// </summary>
    private void Filter_Changed(object sender, RoutedEventArgs e) => Refresh();
    /// <summary>
    /// Cập nhật mã bệnh nhân theo ComboBox, bỏ lựa chọn không còn cùng phạm vi và làm mới danh sách; bỏ qua sự kiện trong lúc đổ dữ liệu.
    /// </summary>
    private void Patient_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updating) return;
        PatientId = string.IsNullOrEmpty(HoTroGiaoDien.Selected(PatientFilter)) ? null : HoTroGiaoDien.Selected(PatientFilter);
        visitId = null; requestedId = null; selectedId = null; Refresh();
    }
    /// <summary>
    /// Xóa các bộ lọc đang áp dụng và làm mới danh sách theo phạm vi còn lại.
    /// </summary>
    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        updating = true; SearchBox.Clear(); StatusFilter.SelectedIndex = 0; updating = false;
        PatientId = null; visitId = null; requestedId = null; selectedId = null; Refresh();
    }
    /// <summary>
    /// Mở form kê đơn mới, truyền phạm vi bệnh nhân và mã lần khám hiện tại nếu có.
    /// </summary>
    private void Create_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (PatientId is not null && DuLieuGiaoDien.Find("BENH_NHAN", PatientId) is null)
        { Feedback.Text = "Không tìm thấy bệnh nhân được yêu cầu."; return; }
        if (!DuLieuGiaoDien.Rows("LICH_SU_KHAM_BENH").Any(row =>
            (string.IsNullOrEmpty(PatientId) || DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", row) == PatientId)
            && (string.IsNullOrEmpty(visitId) || UiRecord.Text(row, "MaLS") == visitId)))
        { Feedback.Text = "Cần có một lần khám đã lưu của bệnh nhân trước khi kê đơn."; return; }
        var context = string.IsNullOrEmpty(visitId) ? null : new CreationContext("DON_THUOC", "MaLS", visitId);
        HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("DonThuoc", "DON_THUOC", context: context, patient: PatientId);
    }
}
