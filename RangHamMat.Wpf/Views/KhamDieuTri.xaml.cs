// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ lọc danh sách lần khám theo ngữ cảnh, mở chi tiết và tạo lần khám từ tiếp nhận hợp lệ; đã chú thích kiểm tra liên kết và làm mới.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;

/// <summary>
/// Màn hình danh sách lần khám/điều trị: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class KhamDieuTri : UserControl, IPatientScoped
{
    private bool updating;
    private string? selectedVisitId;
    private string? explicitVisitId;
    private Func<string, bool> allowed = _ => true;
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string? PatientId { get; private set; }
    /// <summary>
    /// Khởi tạo danh sách lần khám/điều trị từ XAML. Theo dõi cache khi Loaded và làm mới nội dung khi dữ liệu thay đổi; gỡ theo dõi khi Unloaded.
    /// </summary>
    public KhamDieuTri()
    {
        InitializeComponent();
        if (DuLieuGiaoDien.PhamViLamSang)
        {
            updating = true; FromDate.SelectedDate = ToDate.SelectedDate = DateTime.Today; updating = false;
            AddVisitButton.IsEnabled = Controls.ClinicalWorkflow.QuyenNghiepVu.DuocTao("LICH_SU_KHAM_BENH");
        }
        HoTroGiaoDien.Observe(this, Refresh);
        Refresh();
    }
    /// <summary>
    /// Nhận hàm kiểm tra quyền phân hệ từ màn hình cha, lưu lại và áp dụng cho các tab/nút liên quan trong control hiện tại.
    /// </summary>
    public void ConfigureAccess(Func<string, bool> canAccess)
    {
        allowed = canAccess;
        if (DetailHost.Content is ChiTietKhamDieuTri detail) detail.ConfigureAccess(canAccess);
    }
    /// <summary>
    /// Áp dụng mã bệnh nhân nguồn và làm mới dữ liệu trong phạm vi hồ sơ này.
    /// </summary>
    public void SetPatient(string id) => FocusContext(id);
    /// <summary>
    /// Áp dụng phạm vi bệnh nhân/lần khám nguồn, tìm đúng bản ghi và mở chi tiết; mã không tồn tại không được thay bằng lần khám đầu danh sách.
    /// </summary>
    public void FocusContext(string? patientId, string? visitId = null, string? prescriptionId = null)
    {
        if (!string.IsNullOrEmpty(prescriptionId))
        {
            var order = DuLieuGiaoDien.Find("DON_THUOC", prescriptionId);
            if (order is null || (!string.IsNullOrEmpty(patientId) && DuLieuGiaoDien.PatientId("DON_THUOC", order) != patientId))
            { PatientId = patientId; explicitVisitId = "__missing__"; selectedVisitId = null; Refresh(); ContextNote.Text = "Không tìm thấy đơn thuốc thuộc bệnh nhân được yêu cầu."; return; }
            visitId ??= UiRecord.Text(order, "MaLS");
        }
        var visit = DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", visitId);
        PatientId = patientId ?? (visit is null ? null : DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", visit));
        explicitVisitId = visitId;
        selectedVisitId = visitId;
        SearchBox.Clear(); StatusFilter.SelectedIndex = 0; FromDate.SelectedDate = null; ToDate.SelectedDate = null;
        Refresh();
    }
    /// <summary>
    /// Lọc danh sách lần khám theo bệnh nhân/từ khóa/trạng thái, giữ mã đang chọn khi phù hợp và cập nhật lựa chọn lượt tiếp nhận chưa có lần khám.
    /// </summary>
    private void Refresh()
    {
        if (VisitList is null || updating) return;
        updating = true;
        try
        {
            var patients = DuLieuGiaoDien.Options("BENH_NHAN", all: true).ToList();
            if (!string.IsNullOrEmpty(PatientId) && DuLieuGiaoDien.Find("BENH_NHAN", PatientId) is null) patients.Add(new(PatientId, "Không tìm thấy bệnh nhân · " + PatientId));
            PatientFilter.ItemsSource = patients; PatientFilter.SelectedValue = PatientId ?? "";
            var rows = DuLieuGiaoDien.Rows("LICH_SU_KHAM_BENH").Where(row => HoTroGiaoDien.Matches("LICH_SU_KHAM_BENH", row, SearchBox.Text, HoTroGiaoDien.Choice(StatusFilter), PatientId))
                .Where(Controls.ClinicalWorkflow.QuyenNghiepVu.DuocXemNoiDungKham)
                .Where(row => !DuLieuGiaoDien.PhamViLamSang || !DuLieuGiaoDien.NguoiDungLamSang.CoQuyen(ET.QuyenLamViec.Kham) || UiRecord.Text(row,"MaBS") == PhienDangNhap.MaNV || (explicitVisitId == UiRecord.Text(row,"MaLS") && UiRecord.Text(row,"TrangThai") == "Đã xác nhận"))
                .Where(row => FromDate.SelectedDate is null || UiRecord.Date(row, "ThoiGianKham")?.Date >= FromDate.SelectedDate)
                .Where(row => ToDate.SelectedDate is null || UiRecord.Date(row, "ThoiGianKham")?.Date <= ToDate.SelectedDate)
                .OrderByDescending(row => UiRecord.Date(row, "ThoiGianKham")).Select(row => new MucDanhSachLanKham(row)).ToArray();
            VisitList.ItemsSource = rows;
            VisitList.SelectedItem = rows.FirstOrDefault(row => row.Code == selectedVisitId);
            RecordCount.Text = $"{rows.Length} lần khám · {rows.Count(row => DuLieuGiaoDien.CanEdit("LICH_SU_KHAM_BENH", row.Record))} bản nháp";
            ListEmpty.Visibility = rows.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (VisitList.SelectedItem is MucDanhSachLanKham selected) ShowVisit(selected);
            else
            {
                DetailHost.Content = null; DetailEmpty.Visibility = Visibility.Visible;
                ContextNote.Text = !string.IsNullOrEmpty(explicitVisitId) ? "Không tìm thấy lần khám thuộc bệnh nhân được yêu cầu hoặc bộ lọc hiện tại." : "Thông tin khám, tình trạng bệnh, kế hoạch, dịch vụ thực hiện và đơn thuốc sẽ xuất hiện tại đây.";
            }
        }
        finally { updating = false; }
    }
    /// <summary>
    /// Tạo ChiTietKhamDieuTri dùng chung với lịch sử trong hồ sơ, truyền quyền và đặt vào vùng chi tiết lần khám.
    /// </summary>
    private void ShowVisit(MucDanhSachLanKham visit)
    {
        if (!string.IsNullOrEmpty(PatientId) && visit.PatientId != PatientId) return;
        selectedVisitId = visit.Code;
        if (DetailHost.Content is not ChiTietKhamDieuTri current || UiRecord.Text((current.DataContext as TongHopLanKham)?.Record!, "MaLS") != visit.Code)
        {
            ChiTietKhamDieuTri detail;
            try { detail = new ChiTietKhamDieuTri(visit.Record); detail.ConfigureAccess(allowed); }
            catch (InvalidOperationException ex) { DetailHost.Content = null; DetailEmpty.Visibility = Visibility.Visible; ContextNote.Text = ex.Message; return; }
            detail.BackRequested += () => { explicitVisitId = null; selectedVisitId = null; VisitList.SelectedItem = null; DetailHost.Content = null; DetailEmpty.Visibility = Visibility.Visible; };
            DetailHost.Content = detail;
        }
        DetailEmpty.Visibility = Visibility.Collapsed; NewVisitPanel.Visibility = Visibility.Collapsed;
    }
    /// <summary>
    /// Cập nhật lần khám hoặc vùng chi tiết theo lựa chọn mới, giữ đúng bệnh nhân nguồn.
    /// </summary>
    private void Visit_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!updating && VisitList.SelectedItem is MucDanhSachLanKham visit) { explicitVisitId = null; ShowVisit(visit); }
    }
    /// <summary>
    /// Lọc và hiển thị lại danh sách theo nội dung tìm kiếm/trạng thái/ngày đang chọn; chưa thay đổi dữ liệu SQL.
    /// </summary>
    private void Filter_Changed(object sender, RoutedEventArgs e) { if (VisitList is not null && !updating) Refresh(); }
    /// <summary>
    /// Áp dụng mã bệnh nhân vừa chọn, bỏ lựa chọn lần khám cũ và tải lại danh sách đúng phạm vi.
    /// </summary>
    private void PatientFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updating || VisitList is null) return;
        PatientId = string.IsNullOrEmpty(HoTroGiaoDien.Selected(PatientFilter)) ? null : HoTroGiaoDien.Selected(PatientFilter);
        selectedVisitId = null; explicitVisitId = null; NewVisitPanel.Visibility = Visibility.Collapsed; Refresh();
    }
    /// <summary>
    /// Xóa các bộ lọc đang áp dụng và làm mới danh sách theo phạm vi còn lại.
    /// </summary>
    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        updating = true; SearchBox.Clear(); StatusFilter.SelectedIndex = 0; FromDate.SelectedDate = null; ToDate.SelectedDate = null; PatientId = null; explicitVisitId = null; updating = false; Refresh();
    }
    /// <summary>
    /// Mở vùng chọn lượt tiếp nhận để tạo lần khám mới; cần tiếp nhận trước khi lập lịch sử khám.
    /// </summary>
    private void AddVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!allowed("KhamDieuTri")) return;
        var unused = DuLieuGiaoDien.Options("PHIEU_KHAM_BENH", patient: PatientId).Where(option => !DuLieuGiaoDien.Children("LICH_SU_KHAM_BENH", "MaPhieu", option.Value).Any()).ToArray();
        ReceiptChoice.ItemsSource = unused; ReceiptChoice.SelectedIndex = unused.Length == 1 ? 0 : -1;
        CreateVisitButton.IsEnabled = unused.Length > 0;
        ReceiptNote.Text = unused.Length == 0 ? "Chưa có lượt tiếp nhận chưa khám. Lập lượt tiếp nhận trước khi bắt đầu lần khám." : "Chọn lượt tiếp nhận đúng bệnh nhân trước khi tiếp tục.";
        NewVisitPanel.Visibility = Visibility.Visible;
    }
    /// <summary>
    /// Kiểm tra lượt tiếp nhận đang chọn, suy ra bệnh nhân và mở form lần khám với mã tiếp nhận/bác sĩ/phòng nguồn.
    /// </summary>
    private void CreateVisit_Click(object sender, RoutedEventArgs e)
    {
        var receiptId = HoTroGiaoDien.Selected(ReceiptChoice);
        var receipt = DuLieuGiaoDien.Find("PHIEU_KHAM_BENH", receiptId);
        if (receipt is null) { ReceiptNote.Text = "Chọn một lượt tiếp nhận để tiếp tục."; return; }
        var patientId = DuLieuGiaoDien.PatientId("PHIEU_KHAM_BENH", receipt);
        if (string.IsNullOrEmpty(patientId) || (!string.IsNullOrEmpty(PatientId) && patientId != PatientId) || DuLieuGiaoDien.Children("LICH_SU_KHAM_BENH", "MaPhieu", receiptId).Any())
        { ReceiptNote.Text = "Lượt tiếp nhận không còn hợp lệ. Chọn lại lượt tiếp nhận."; return; }
        HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("KhamDieuTri", "LICH_SU_KHAM_BENH", context: new("LICH_SU_KHAM_BENH", "MaPhieu", receiptId), patient: patientId);
    }
    /// <summary>
    /// Mở biểu mẫu tiếp nhận cho bệnh nhân/bệnh án nguồn; không tự tạo lần khám khi chưa lưu phiếu tiếp nhận.
    /// </summary>
    private void Reception_Click(object sender, RoutedEventArgs e) => HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("TiepNhanKham", "PHIEU_KHAM_BENH", patient: PatientId);
    /// <summary>
    /// Ẩn vùng tạo lần khám mới và giữ nguyên danh sách/chi tiết đang xem.
    /// </summary>
    private void CancelNewVisit_Click(object sender, RoutedEventArgs e) => NewVisitPanel.Visibility = Visibility.Collapsed;
}
