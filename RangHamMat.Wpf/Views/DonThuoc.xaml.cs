using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Views;

/// <summary>Tra cứu đơn đã kê và bàn giao; việc kê đơn thuộc màn hình khám điều trị.</summary>
public partial class DonThuoc : UserControl, IPatientScoped
{
    /// <summary>
    /// Ngữ cảnh định danh và quyền của người dùng đang thao tác.
    /// </summary>
    private readonly NguCanhNghiepVu actor;
    /// <summary>
    /// Đối tượng nghiệp vụ khám điều trị được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private readonly BUS_KHAMDIEUTRI history;
    /// <summary>
    /// Đối tượng nghiệp vụ đơn thuốc được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private readonly BUS_DONTHUOC prescriptions;
    /// <summary>
    /// Giao dịch được giữ để các thao tác liên quan dùng cùng một phạm vi commit/rollback.
    /// </summary>
    private readonly IDbTransaction? transaction;
    /// <summary>
    /// Cờ Đang cập nhật; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool updating = true;
    /// <summary>
    /// Cờ Không hợp lệ ngữ cảnh; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool invalidContext;
    /// <summary>
    /// Mã lần khám đang chọn mã được yêu cầu mã được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private string? visitId, selectedId, requestedId;
    /// <summary>
    /// Trang lịch sử hiện tại được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private int historyPage = 1;
    /// <summary>
    /// Mã bệnh nhân. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? PatientId { get; private set; }
    /// <summary>
    /// Khởi tạo đơn thuốc.
    /// </summary>
    public DonThuoc() : this(QuyenNghiepVu.LayNguCanh()) { }
    /// <summary>
    /// Khởi tạo đơn thuốc.
    /// </summary>
    /// <param name="historyOnly">Giá trị lịch sử chỉ dùng khi thực hiện thao tác; kiểu bool.</param>
    internal DonThuoc(bool historyOnly) : this() { }
    /// <summary>
    /// Khởi tạo đơn thuốc. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    /// <param name="actor">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    public DonThuoc(NguCanhNghiepVu actor, IDbTransaction? transaction = null)
    {
        this.actor = actor;
        this.transaction = transaction;
        history = new(transaction);
        prescriptions = new(transaction);
        InitializeComponent();
        updating = false;
        HoTroGiaoDien.Observe(this, Refresh);
        Refresh();
    }

    /// <summary>
    /// Áp dụng bệnh nhân được chỉ định làm phạm vi dữ liệu của màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>FocusContext</c>.</remarks>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    public void SetPatient(string id) => FocusContext(id);
    /// <summary>
    /// Chuyển màn hình tới đúng bệnh nhân/lần khám hoặc chứng từ theo ngữ cảnh được yêu cầu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MustKeepSource</c>, <c>history.XacMinhNguCanh</c>, <c>prescriptions.LayChiTietCoQuyen</c>, <c>ClinicalFormInput.Message</c>.</remarks>
    /// <param name="patientId">Giá trị mã bệnh nhân dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <param name="visitId">Giá trị mã lần khám dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <param name="prescriptionId">Giá trị đơn thuốc mã dùng khi thực hiện thao tác; kiểu string?.</param>
    public void FocusContext(string? patientId, string? visitId = null, string? prescriptionId = null)
    {
        if (MustKeepSource()) return;
        PatientId = string.IsNullOrWhiteSpace(patientId) ? null : patientId;
        this.visitId = string.IsNullOrWhiteSpace(visitId) ? null : visitId;
        requestedId = selectedId = string.IsNullOrWhiteSpace(prescriptionId) ? null : prescriptionId;
        historyPage = 1;
        invalidContext = false;
        DetailHost.Content = null;
        updating = true;
        SearchBox.Text = requestedId ?? "";
        StatusFilter.SelectedIndex = 0;
        try
        {
            var source = history.XacMinhNguCanh(new() { MaBN = PatientId, MaLS = this.visitId, MaDT = requestedId }, actor);
            PatientId = source.MaBN;
            this.visitId = source.MaLS;
            if (requestedId is not null)
            {
                var prescription = prescriptions.LayChiTietCoQuyen(requestedId, actor);
                if (prescription.DonThuoc.TrangThai == "Ngừng áp dụng") StatusFilter.SelectedIndex = 1;
            }
        }
        catch (InvalidOperationException ex)
        {
            invalidContext = true;
            Feedback.Text = ClinicalFormInput.Message(ex);
        }
        finally { updating = false; }
        Refresh();
    }

    /// <summary>
    /// Kiểm tra nguồn chứng từ đang được giữ để ngăn đổi lựa chọn khi công việc phụ thuộc chưa kết thúc.
    /// </summary>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    private bool MustKeepSource()
    {
        if (DetailHost?.Content is PrescriptionHistoryDetail { MustKeepSource: true })
        { Feedback.Text = "Cần đối chiếu kết quả giao đúng đơn đang xử lý trước khi đổi nguồn."; return true; }
        return false;
    }

    /// <summary>
    /// Làm mới danh sách và chi tiết theo phạm vi, bộ lọc hoặc lựa chọn hiện tại.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MustKeepSource</c>, <c>Array.Empty&lt;IssuedPrescriptionCard&gt;</c>, <c>Array.Empty&lt;LuaChonDanhMuc&gt;</c>, <c>ShowSelected</c>.</remarks>
    private void Refresh()
    {
        if (updating || PrescriptionList is null || MustKeepSource()) return;
        updating = true;
        if (invalidContext)
        {
            PrescriptionList.ItemsSource = Array.Empty<IssuedPrescriptionCard>();
            PatientFilter.ItemsSource = Array.Empty<LuaChonDanhMuc>();
            RecordCount.Text = "0 đơn thuốc";
            PrescriptionPages.Visibility = Visibility.Collapsed;
            ListEmpty.Visibility = Visibility.Visible;
            updating = false;
            ShowSelected();
            return;
        }
        RefreshHistoryPage();
    }

    /// <summary>
    /// Hiển thị nội dung mục được chọn và cập nhật trạng thái điều khiển phù hợp.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>history.XacMinhNguCanh</c>, <c>ClinicalFormInput.Message</c>.</remarks>
    private void ShowSelected()
    {
        if (PrescriptionList.SelectedItem is IssuedPrescriptionCard card)
        {
            try
            {
                history.XacMinhNguCanh(new() { MaBN = card.PatientId, MaLS = card.Visit, MaDT = card.Code }, actor);
                selectedId = card.Code;
                requestedId = null;
                var detail = new PrescriptionHistoryDetail(card.Record, actor, transaction);
                detail.SourceLockChanged += locked =>
                {
                    PrescriptionList.IsEnabled = FiltersPanel.IsEnabled = PrescriptionPages.IsEnabled = !locked;
                };
                DetailHost.Content = detail;
                DetailEmpty.Visibility = Visibility.Collapsed;
                return;
            }
            catch (InvalidOperationException ex) { Feedback.Text = ClinicalFormInput.Message(ex); }
        }
        DetailHost.Content = null;
        DetailEmpty.Visibility = Visibility.Visible;
        DetailEmptyText.Text = invalidContext ? "Không đọc được đơn trong phạm vi được phép."
            : requestedId is not null ? "Không tìm thấy đơn đã kê trong ngữ cảnh bệnh nhân và lần khám này."
            : "Chọn đơn để xem nội dung bác sĩ đã kê và trạng thái bàn giao.";
    }

    /// <summary>
    /// Tiếp nhận thay đổi đơn và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MustKeepSource</c>, <c>ShowSelected</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Order_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updating || MustKeepSource()) return;
        selectedId = (PrescriptionList.SelectedItem as IssuedPrescriptionCard)?.Code;
        ShowSelected();
    }
    /// <summary>
    /// Tiếp nhận thay đổi bộ lọc và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MustKeepSource</c>, <c>Refresh</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        if (updating || MustKeepSource()) return;
        selectedId = requestedId = null;
        historyPage = 1;
        Refresh();
    }
    /// <summary>
    /// Tiếp nhận thay đổi bệnh nhân và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>MustKeepSource</c>, <c>FocusContext</c>, <c>HoTroGiaoDien.Selected</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Patient_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updating || MustKeepSource()) return;
        FocusContext(HoTroGiaoDien.Selected(PatientFilter));
    }
    /// <summary>
    /// Xử lý thao tác xóa nội dung các bộ lọc khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>FocusContext</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ClearFilters_Click(object sender, RoutedEventArgs e) => FocusContext(null);
}

/// <summary>Thẻ danh sách chỉ dùng DTO đã kiểm tra quyền, không tra dòng thuốc trong cache toàn cục.</summary>
internal sealed class IssuedPrescriptionCard(DongLichSuDon source)
{
    /// <summary>
    /// Bản ghi.
    /// </summary>
    public DongLichSuDon Record { get; } = source;
    /// <summary>
    /// Giá trị Mã; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string Code => Record.MaDT;
    /// <summary>
    /// Giá trị Trạng thái; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string Status => Record.TrangThai;
    /// <summary>
    /// Giá trị Mã bệnh nhân; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string PatientId => Record.MaBN;
    /// <summary>
    /// Giá trị Lần khám; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string Visit => Record.MaLS;
    /// <summary>
    /// Giá trị Bệnh nhân; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string Patient => $"{Record.HoTenBN} ({Record.MaBN})";
    /// <summary>
    /// Giá trị Bác sĩ; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string Doctor => Record.TenBS;
    /// <summary>
    /// Giá trị Thời điểm; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string When => Record.NgayKe.ToString("dd/MM/yyyy HH:mm");
}
