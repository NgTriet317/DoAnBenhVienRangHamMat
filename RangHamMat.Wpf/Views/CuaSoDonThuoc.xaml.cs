// AI bổ sung kiểm tra nhập liệu: kiểm tra độ dài liều dùng, số lẻ/range lượng và lỗi binding trước khi lưu đầu đơn.
// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ form đơn thuốc nhiều dòng, chọn bác sĩ/chẩn đoán, thêm/xóa dòng và kiểm tra trước khi lưu; đã giải thích dữ liệu bản nháp và giao dịch lưu.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;

/// <summary>
/// Màn hình biểu mẫu đơn thuốc và dòng thuốc: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class CuaSoDonThuoc : UserControl, IBieuMauLuu
{
    /// <summary>
    /// Bản nháp được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly ObservableCollection<RxDraftLine> draft = new();
    /// <summary>
    /// Collection mã/nhãn thuốc dùng làm ItemsSource cho các dòng kê đơn; cập nhật khi danh mục đổi.
    /// </summary>
    public ObservableCollection<LuaChonDanhMuc> MedicineOptions { get; } = new();
    /// <summary>
    /// Cờ Đã khởi tạo; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool initialized;
    /// <summary>
    /// Khởi tạo biểu mẫu đơn thuốc và dòng thuốc từ XAML. Theo dõi cache khi Loaded và làm mới nội dung khi dữ liệu thay đổi; gỡ theo dõi khi Unloaded.
    /// </summary>
    public CuaSoDonThuoc()
    {
        InitializeComponent();
        MedicineRows.ItemsSource = draft;
        HoTroGiaoDien.Observe(this, RefreshOptions);
    }
    /// <summary>
    /// Nạp danh mục thuốc với tên, hàm lượng và đơn vị tính; chỉ thay collection lựa chọn khi nội dung đã đổi để giữ lựa chọn đang nhập.
    /// </summary>
    private void RefreshOptions()
    {
        var options = DuLieuGiaoDien.Rows("THUOC").Select(drug =>
        {
            var goods = DuLieuGiaoDien.Find("HANG_HOA", UiRecord.Get(drug, "MaHH"));
            var unit = goods is null ? "" : UiRecord.Text(goods, "DonViTinh");
            var label = DuLieuGiaoDien.Label("THUOC", drug);
            return new LuaChonDanhMuc(UiRecord.Text(drug, "MaThuoc"), unit.Length == 0 ? label : label + " · " + unit);
        }).ToArray();
        if (MedicineOptions.SequenceEqual(options)) return;
        MedicineOptions.Clear();
        foreach (var option in options) MedicineOptions.Add(option);
    }
    /// <summary>
    /// Chuẩn bị danh mục một lần, chọn chế độ kê cả đơn hoặc sửa một dòng thuốc, nạp nháp cũ và khóa lần khám/đơn cha khi chỉnh sửa.
    /// </summary>
    private void Form_Loaded(object sender, RoutedEventArgs e)
    {
        BieuMauHoSo.Prepare(this); RefreshOptions();
        if (initialized) return;
        initialized = true;
        var table = BieuMauHoSo.GetTable(this);
        var record = BieuMauHoSo.GetRecord(this);
        if (table == "CHI_TIET_DON_THUOC")
        {
            HeaderFields.Visibility = Visibility.Collapsed; LineParentFields.Visibility = Visibility.Visible;
            AddButton.Visibility = Visibility.Collapsed; if (record is not null) ParentBox.IsEnabled = false; EditorTitle.Text = record is null ? "Thêm thuốc vào đơn nháp" : "Sửa thuốc trong đơn nháp";
            draft.Add(record is null ? new RxDraftLine() : new RxDraftLine(record));
        }
        else
        {
            EditorTitle.Text = record is null ? "Kê đơn thuốc mới" : "Cập nhật đơn thuốc nháp";
            if (record is not null)
                foreach (var line in DuLieuGiaoDien.Children("CHI_TIET_DON_THUOC", "MaDT", UiRecord.Get(record, "MaDT"))) draft.Add(new RxDraftLine(line));
            if (record is not null) VisitBox.IsEnabled = false;
            if (draft.Count == 0) draft.Add(new RxDraftLine());
            UpdateVisit();
        }
    }
    /// <summary>
    /// Hiển thị chẩn đoán của lần khám được chọn và tự điền bác sĩ kê nếu người dùng chưa chọn bác sĩ.
    /// </summary>
    private void UpdateVisit()
    {
        if (DiagnosisText is null) return;
        var visit = DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", VisitBox.SelectedValue);
        DiagnosisText.Text = visit is null ? "Chọn lần khám để xem chẩn đoán" : UiRecord.Text(visit, "ChanDoan");
        if (visit is not null && DoctorBox.SelectedValue is null) DoctorBox.SelectedValue = UiRecord.Get(visit, "MaBS");
    }
    /// <summary>
    /// Cập nhật chẩn đoán và bác sĩ mặc định khi đổi lần khám trong biểu mẫu kê đơn.
    /// </summary>
    private void Visit_Changed(object sender, SelectionChangedEventArgs e) => UpdateVisit();
    /// <summary>
    /// Thêm dòng thuốc mới vào collection nháp; chỉ thay dữ liệu đang nhập, chưa ghi SQL.
    /// </summary>
    private void AddMedicine_Click(object sender, RoutedEventArgs e) { e.Handled = true; draft.Add(new RxDraftLine()); }
    /// <summary>
    /// Bỏ dòng thuốc đang chọn khỏi collection nháp; việc xóa SQL chỉ diễn ra khi lưu cả đơn thành công.
    /// </summary>
    private void RemoveMedicine_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if ((sender as FrameworkElement)?.DataContext is RxDraftLine line) draft.Remove(line);
    }
    /// <summary>
    /// Mở hoặc dùng lại giao dịch chung, gọi CommitCore kiểm tra/lưu form; trả true khi lưu được hoặc false kèm lỗi và rollback khi không thành công.
    /// </summary>
    public bool Commit(out string error)
    {
        if (!KiemTraNhapLieu.PrepareSave(this, out error)) return false;
        string failure="";
        try {var result=DuLieuGiaoDien.Transaction(()=>CommitCore(out failure));error=failure;return result;}
        catch(LoiNhapLieu ex) {KiemTraNhapLieu.MarkError(this,ex);error=ex.Message;return false;}
        catch(Exception ex) when(ex is InvalidOperationException or FormatException or OverflowException or ArgumentOutOfRangeException) {error=ex.Message;return false;}
    }
    /// <summary>
    /// Kiểm tra toàn bộ đầu đơn và dòng thuốc trước khi ghi; lưu đầu đơn, thêm/sửa các dòng và xóa dòng đã bỏ trong giao dịch do Commit mở, lỗi ở bất kỳ dòng nào hủy toàn bộ.
    /// </summary>
    private bool CommitCore(out string error)
    {
        BieuMauHoSo.VerifyUnchanged(this);
        error = "";
        var table = BieuMauHoSo.GetTable(this);
        var existing = BieuMauHoSo.GetRecord(this);
        if (table is not "DON_THUOC" and not "CHI_TIET_DON_THUOC")
        { error = "Không xác định được loại dữ liệu đơn thuốc đang chỉnh sửa."; return false; }
        if (existing is not null && !DuLieuGiaoDien.CanEdit(table, existing))
        { error = "Đơn đã xác nhận hoặc đã hủy được giữ nguyên. Hãy tạo đơn mới độc lập."; return false; }
        if (draft.Count == 0 || table == "CHI_TIET_DON_THUOC" && draft.Count != 1)
        { error = "Cần có ít nhất một thuốc trong đơn."; return false; }
        var values = new List<Dictionary<string, object?>>();
        for (var index = 0; index < draft.Count; index++)
        {
            var line = draft[index];
            var medicine = DuLieuGiaoDien.Find("THUOC", line.MaThuoc);
            if (medicine is null || DuLieuGiaoDien.Find("HANG_HOA", UiRecord.Get(medicine, "MaHH")) is null || string.IsNullOrWhiteSpace(line.LieuDung) || string.IsNullOrWhiteSpace(line.HuongDan))
            { error = $"Thuốc {index + 1}: chọn thuốc hợp lệ, nhập liều dùng và hướng dẫn cụ thể."; return false; }
            if (!QuyTacHienThi.TryPrescriptionNumbers(line.SoLuong, line.SoNgayDung, out var quantity, out var days))
            { error = $"Thuốc {index + 1}: số lượng phải lớn hơn 0, số ngày dùng phải là số nguyên lớn hơn 0."; return false; }
            if (line.Existing is object original &&
                (DuLieuGiaoDien.Find("CHI_TIET_DON_THUOC", UiRecord.Get(original, "MaCTDT")) is not object current
                 || !ReferenceEquals(current, original) || !DuLieuGiaoDien.CanEdit("CHI_TIET_DON_THUOC", current)))
            { error = $"Thuốc {index + 1} đã thay đổi trong bộ dữ liệu. Mở lại đơn thuốc trước khi lưu."; return false; }
            values.Add(new() { ["MaThuoc"] = line.MaThuoc, ["LieuDung"] = line.LieuDung.Trim(), ["SoLuong"] = quantity, ["SoNgayDung"] = days, ["HuongDan"] = line.HuongDan.Trim() });
            // AI bổ sung: kiểm tra độ dài liều dùng và precision số lượng của từng dòng trước khi lưu đầu đơn.
            try { KiemTraNhapLieu.ValidateValues("CHI_TIET_DON_THUOC", values[^1]); }
            catch(LoiNhapLieu ex) { error=$"Thuốc {index+1}: {ex.Message}"; return false; }
        }
        if (table == "CHI_TIET_DON_THUOC")
        {
            var parent = DuLieuGiaoDien.Find("DON_THUOC", ParentBox.SelectedValue);
            if (parent is null || !UiRecord.Text(parent, "TrangThai").Equals("Nháp", StringComparison.CurrentCultureIgnoreCase))
            { error = "Chỉ thêm hoặc sửa thuốc trong một đơn nháp hợp lệ."; return false; }
            var scope = BieuMauHoSo.GetPatientScope(this);
            if (!string.IsNullOrEmpty(scope) && DuLieuGiaoDien.PatientId("DON_THUOC", parent) != scope
                || existing is not null && UiRecord.Text(existing, "MaDT") != UiRecord.Text(parent, "MaDT"))
            { error = "Dòng thuốc phải thuộc đơn nháp đang chỉnh sửa của đúng bệnh nhân."; return false; }
            values[0]["MaDT"] = UiRecord.Get(parent, "MaDT");
            DuLieuGiaoDien.Upsert(table, values[0], existing);
            return true;
        }
        Dictionary<string, object?> header;
        try { header = BieuMauHoSo.Read(this); }
        catch (InvalidOperationException exception) { error = exception.Message; return false; }
        var visit = DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", header.GetValueOrDefault("MaLS"));
        var patient = visit is null ? "" : DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", visit);
        if (visit is null || string.IsNullOrEmpty(patient) || DuLieuGiaoDien.Find("BENH_NHAN", patient) is null
            || DuLieuGiaoDien.Find("NHAN_VIEN", header.GetValueOrDefault("MaBS")) is null || header.GetValueOrDefault("NgayKe") is not DateTime)
        { error = "Chọn lần khám có liên kết bệnh nhân, bác sĩ kê đơn và ngày kê hợp lệ."; return false; }
        var patientScope = BieuMauHoSo.GetPatientScope(this);
        if (!string.IsNullOrEmpty(patientScope) && patient != patientScope
            || existing is not null && UiRecord.Text(existing, "MaLS") != UiRecord.Text(visit, "MaLS"))
        { error = "Đơn thuốc phải gắn với lần khám đang chỉnh sửa của đúng bệnh nhân."; return false; }
        var existingId = existing is null ? "" : UiRecord.Text(existing, "MaDT");
        if (existing is not null && !ReferenceEquals(DuLieuGiaoDien.Find("DON_THUOC", existingId), existing))
        { error = "Đơn thuốc đã thay đổi trong bộ dữ liệu. Mở lại đơn trước khi lưu."; return false; }
        if (draft.Any(line => line.Existing is object original && UiRecord.Text(original, "MaDT") != existingId))
        { error = "Dòng thuốc không thuộc đơn đang chỉnh sửa."; return false; }
        // Kiểm tra toàn bộ đầu đơn và các dòng thuốc trước khi thay đổi dữ liệu trong phiên/giao dịch.
        if (!BieuMauHoSo.TrySave(this, out var saved, out error) || saved is null) return false;
        var id = UiRecord.Get(saved, "MaDT");
        var originalLines = DuLieuGiaoDien.Children("CHI_TIET_DON_THUOC", "MaDT", id);
        for (var index = 0; index < draft.Count; index++)
        {
            values[index]["MaDT"] = id;
            DuLieuGiaoDien.Upsert("CHI_TIET_DON_THUOC", values[index], draft[index].Existing);
        }
        foreach (var removed in originalLines.Where(row => !draft.Any(line => ReferenceEquals(line.Existing, row))))
            // Chỉ xóa dòng đã có nhưng người dùng bỏ khỏi nháp; nếu xóa lỗi thì giao dịch hủy cả đầu đơn và các dòng vừa ghi.
            DuLieuGiaoDien.Delete("CHI_TIET_DON_THUOC", removed);
        return true;
    }
}
