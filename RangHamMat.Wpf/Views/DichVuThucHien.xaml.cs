// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ bộ lọc dịch vụ theo kế hoạch/lần khám, mở chi tiết, sửa và xác nhận; đã giải thích liên kết nguồn và các bước cập nhật giao diện.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>
/// Màn hình dịch vụ thực hiện: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class DichVuThucHien : UserControl, IPatientScoped
{
    private bool updating;
    private string? visitId;
    private string? prescriptionId;
    private string? selectedId;
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string? PatientId { get; private set; }
    /// <summary>
    /// Dịch vụ/dòng đang chọn hoặc cờ đánh dấu dịch vụ trong danh mục trước khi thêm vào nháp.
    /// </summary>
    private DongChiTietLanKham? Selected => Grid_DICH_VU_THUC_HIEN.SelectedItem as DongChiTietLanKham;

    /// <summary>
    /// Khởi tạo dịch vụ thực hiện từ XAML. Theo dõi cache khi Loaded và làm mới nội dung khi dữ liệu thay đổi; gỡ theo dõi khi Unloaded.
    /// </summary>
    public DichVuThucHien()
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
    /// Áp dụng phạm vi bệnh nhân/lần khám nguồn và làm mới danh sách dịch vụ; không dùng lần khám khác khi mã nguồn không tồn tại.
    /// </summary>
    public void FocusContext(string? patientId, string? visitId = null, string? prescriptionId = null)
    {
        var order = DuLieuGiaoDien.Find("DON_THUOC", prescriptionId);
        var requestedVisit = visitId ?? (order is null ? null : UiRecord.Text(order, "MaLS"));
        var visit = DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", requestedVisit);
        PatientId = patientId ?? (visit is null ? null : DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", visit));
        this.visitId = requestedVisit;
        this.prescriptionId = prescriptionId;
        selectedId = null;
        updating = true; SearchBox.Clear(); StatusFilter.SelectedIndex = 0; updating = false;
        Feedback.Text = "";
        if (!string.IsNullOrEmpty(prescriptionId) && (order is null ||
            (!string.IsNullOrEmpty(PatientId) && DuLieuGiaoDien.PatientId("DON_THUOC", order) != PatientId) ||
            UiRecord.Text(order, "MaLS") != requestedVisit))
        {
            this.visitId = "__missing__";
            Feedback.Text = "Không tìm thấy đơn thuốc thuộc bệnh nhân và lần khám được yêu cầu.";
        }
        Refresh();
    }

    /// <summary>
    /// Nạp lựa chọn bệnh nhân/lần khám/kế hoạch/dịch vụ và lọc các dòng thực hiện đúng ngữ cảnh, từ khóa, trạng thái; giữ lựa chọn hiện tại nếu còn phù hợp.
    /// </summary>
    private void Refresh()
    {
        if (Grid_DICH_VU_THUC_HIEN is null || updating) return;
        updating = true;
        try
        {
            var patients = DuLieuGiaoDien.Options("BENH_NHAN", all: true).ToList();
            if (!string.IsNullOrEmpty(PatientId) && DuLieuGiaoDien.Find("BENH_NHAN", PatientId) is null)
                patients.Add(new(PatientId, "Không tìm thấy bệnh nhân · " + PatientId));
            PatientFilter.ItemsSource = patients; PatientFilter.SelectedValue = PatientId ?? "";
            var visits = DuLieuGiaoDien.Options("LICH_SU_KHAM_BENH", all: true, patient: PatientId).ToList();
            if (!string.IsNullOrEmpty(visitId) && visits.All(option => option.Value != visitId))
                visits.Add(new(visitId, "Không tìm thấy lần khám được yêu cầu"));
            VisitFilter.ItemsSource = visits; VisitFilter.SelectedValue = visitId ?? "";
            var terms = SearchBox.Text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var rows = DuLieuGiaoDien.Rows("DICH_VU_THUC_HIEN")
                .Where(row => HoTroGiaoDien.Matches("DICH_VU_THUC_HIEN", row, null, HoTroGiaoDien.Choice(StatusFilter), PatientId))
                .Where(row => string.IsNullOrEmpty(visitId) || UiRecord.Text(row, "MaLS") == visitId)
                .OrderByDescending(row => UiRecord.Date(row, "ThoiGianThucHien"))
                .Select(row => new DongChiTietLanKham("DICH_VU_THUC_HIEN", row))
                .Where(row => terms.All(term => string.Join(" ", row.Code, row.VisitId, row.Service, row.Doctor, row.Result,
                    DuLieuGiaoDien.Lookup("MaBN", row.PatientId)).Contains(term, StringComparison.CurrentCultureIgnoreCase)))
                .ToArray();
            Grid_DICH_VU_THUC_HIEN.ItemsSource = rows;
            Grid_DICH_VU_THUC_HIEN.SelectedItem = rows.FirstOrDefault(row => row.Code == selectedId);
            RecordCount.Text = $"{rows.Length} dịch vụ · {rows.Count(row => row.Status == "Đã xác nhận")} đã xác nhận";
            ListEmpty.Visibility = rows.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            AddButton.IsEnabled = (HoTroGiaoDien.Workspace(this)?.CanOpen("KhamDieuTri") ?? true) &&
                DuLieuGiaoDien.Rows("LICH_SU_KHAM_BENH").Any(row =>
                    (string.IsNullOrEmpty(PatientId) || DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", row) == PatientId) &&
                    (string.IsNullOrEmpty(visitId) || UiRecord.Text(row, "MaLS") == visitId));
            ShowSelected();
        }
        finally { updating = false; }
    }

    /// <summary>
    /// Hiện thông tin dòng dịch vụ đang chọn và bật nút sửa/xác nhận theo trạng thái cùng quyền sửa.
    /// </summary>
    private void ShowSelected()
    {
        ServiceDetail.DataContext = Selected;
        ServiceDetail.Visibility = Selected is null ? Visibility.Collapsed : Visibility.Visible;
        if (Selected is not DongChiTietLanKham row) return;
        selectedId = row.Code;
        PatientContext.Text = $"{DuLieuGiaoDien.Lookup("MaBN", row.PatientId)} · Lần khám {row.VisitId} · {row.When}";
        EditButton.IsEnabled = ConfirmButton.IsEnabled = row.CanEdit && (HoTroGiaoDien.Workspace(this)?.CanOpen("KhamDieuTri") ?? true);
        var billed = DuLieuGiaoDien.Rows("CHI_TIET_HOA_DON").Any(line => UiRecord.Text(line, "MaDVThucHien") == row.Code &&
            DuLieuGiaoDien.Find("HOA_DON", UiRecord.Get(line, "MaHD")) is object invoice && UiRecord.Text(invoice, "TrangThai") != "Hủy");
        BillingNote.Text = billed ? "Dịch vụ đã được đưa vào hóa đơn. Mở Hóa đơn / thanh toán để xem khoản thu." :
            row.Status == "Đã xác nhận" ? "Dịch vụ đã xác nhận, có thể đưa vào hóa đơn của lần khám." : "Chỉ dịch vụ đã xác nhận được đưa vào hóa đơn.";
    }

    /// <summary>
    /// Hiển thị dòng dịch vụ thực hiện mới được chọn; không ghi SQL khi chỉ đổi lựa chọn.
    /// </summary>
    private void Service_Changed(object sender, SelectionChangedEventArgs e) { if (!updating) ShowSelected(); }
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
        PatientId = EmptyToNull(HoTroGiaoDien.Selected(PatientFilter));
        visitId = prescriptionId = selectedId = null; Feedback.Text = ""; Refresh();
    }
    /// <summary>
    /// Cập nhật lần khám hoặc vùng chi tiết theo lựa chọn mới, giữ đúng bệnh nhân nguồn.
    /// </summary>
    private void Visit_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updating) return;
        visitId = EmptyToNull(HoTroGiaoDien.Selected(VisitFilter));
        prescriptionId = selectedId = null; Feedback.Text = ""; Refresh();
    }
    /// <summary>
    /// Đổi chuỗi rỗng thành null để các bộ lọc tùy chọn hiểu rằng chưa giới hạn theo mã đó.
    /// </summary>
    private static string? EmptyToNull(string value) => string.IsNullOrEmpty(value) ? null : value;
    /// <summary>
    /// Bỏ các giá trị lọc hiện tại và hiển thị lại dữ liệu phù hợp.
    /// </summary>
    private void Clear_Click(object sender, RoutedEventArgs e) => FocusContext(null);
    /// <summary>
    /// Đọc lại dữ liệu từ SQL và làm mới danh sách thực hiện; hiển thị thông báo nếu tải không thành công.
    /// </summary>
    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        try { DuLieuGiaoDien.RefreshFromDatabase(); Feedback.Text = ""; Refresh(); }
        catch (InvalidOperationException ex) { Feedback.Text = ex.Message; }
    }
    /// <summary>
    /// Mở biểu mẫu thêm dịch vụ thực hiện, truyền mã lần khám và phạm vi bệnh nhân đang lọc.
    /// </summary>
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (!AddButton.IsEnabled) return;
        var context = string.IsNullOrEmpty(visitId) ? null : new CreationContext("DICH_VU_THUC_HIEN", "MaLS", visitId);
        HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("KhamDieuTri", "DICH_VU_THUC_HIEN", context: context, patient: PatientId);
    }
    /// <summary>
    /// Mở biểu mẫu sửa dòng thực hiện đang chọn khi dòng còn được sửa.
    /// </summary>
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is DongChiTietLanKham { CanEdit: true } row)
            HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("KhamDieuTri", "DICH_VU_THUC_HIEN", row.Record, patient: row.PatientId);
    }
    /// <summary>
    /// Kiểm tra các thông tin của dịch vụ thực hiện rồi cập nhật trạng thái Đã xác nhận; khoản này mới đủ điều kiện đưa vào hóa đơn.
    /// </summary>
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not DongChiTietLanKham { CanEdit: true } row || !ConfirmButton.IsEnabled) return;
        try
        {
            var actual = DuLieuGiaoDien.Find("DICH_VU_THUC_HIEN", row.Code);
            if (actual is null || !DuLieuGiaoDien.CanEdit("DICH_VU_THUC_HIEN", actual))
                throw new InvalidOperationException("Dịch vụ đã thay đổi. Làm mới danh sách trước khi xác nhận.");
            DuLieuGiaoDien.SetValue("DICH_VU_THUC_HIEN", actual, "TrangThai", "Đã xác nhận");
            Feedback.Text = "";
        }
        catch (InvalidOperationException ex) { Feedback.Text = ex.Message; }
    }
    /// <summary>
    /// Mở phân hệ liên quan với bệnh nhân/lần khám của dòng dịch vụ chọn hoặc ngữ cảnh lọc hiện tại.
    /// </summary>
    private void Open(string module)
    {
        if (Selected is not DongChiTietLanKham row) return;
        var actualVisit = DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", row.VisitId);
        if (actualVisit is null || string.IsNullOrEmpty(row.PatientId) || DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", actualVisit) != row.PatientId)
        { Feedback.Text = "Liên kết bệnh nhân hoặc lần khám không còn hợp lệ. Làm mới danh sách."; return; }
        DieuHuongHoSo.Open(this, module, row.PatientId, module == "BenhNhan" ? null : row.VisitId,
            module == "DonThuoc" ? prescriptionId : null);
    }
    /// <summary>
    /// Mở hồ sơ bệnh nhân nguồn của bản ghi đang chọn, giữ mã bệnh nhân qua điều hướng của workspace.
    /// </summary>
    private void Patient_Click(object sender, RoutedEventArgs e) => Open("BenhNhan");
    /// <summary>
    /// Mở phân hệ Khám điều trị với ngữ cảnh bệnh nhân/lần khám của hồ sơ đang xem.
    /// </summary>
    private void Treatment_Click(object sender, RoutedEventArgs e) => Open("KhamDieuTri");
    /// <summary>
    /// Mở phân hệ Đơn thuốc của bệnh nhân/lần khám nguồn đang xem.
    /// </summary>
    private void Prescription_Click(object sender, RoutedEventArgs e) => Open("DonThuoc");
    /// <summary>
    /// Mở phân hệ Hóa đơn cùng mã bệnh nhân/lần khám/đơn thuốc hiện tại nếu có.
    /// </summary>
    private void Invoice_Click(object sender, RoutedEventArgs e) => Open("HoaDon");
}
