using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;

public partial class CuaSoDonThuoc : UserControl, IClinicalEditor
{
    private readonly ObservableCollection<RxDraftLine> draft = new();
    public ObservableCollection<ClinicalOption> MedicineOptions { get; } = new();
    private bool initialized;
    public CuaSoDonThuoc()
    {
        InitializeComponent();
        MedicineRows.ItemsSource = draft;
        ClinicalUi.Observe(this, RefreshOptions);
    }
    private void RefreshOptions()
    {
        var options = ClinicalData.Rows("THUOC").Select(drug =>
        {
            var goods = ClinicalData.Find("HANG_HOA", UiRecord.Get(drug, "MaHH"));
            var unit = goods is null ? "" : UiRecord.Text(goods, "DonViTinh");
            var label = ClinicalData.Label("THUOC", drug);
            return new ClinicalOption(UiRecord.Text(drug, "MaThuoc"), unit.Length == 0 ? label : label + " · " + unit);
        }).ToArray();
        if (MedicineOptions.SequenceEqual(options)) return;
        MedicineOptions.Clear();
        foreach (var option in options) MedicineOptions.Add(option);
    }
    private void Form_Loaded(object sender, RoutedEventArgs e)
    {
        ClinicalForm.Prepare(this); RefreshOptions();
        if (initialized) return;
        initialized = true;
        var table = ClinicalForm.GetTable(this);
        var record = ClinicalForm.GetRecord(this);
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
                foreach (var line in ClinicalData.Children("CHI_TIET_DON_THUOC", "MaDT", UiRecord.Get(record, "MaDT"))) draft.Add(new RxDraftLine(line));
            if (record is not null) VisitBox.IsEnabled = false;
            if (draft.Count == 0) draft.Add(new RxDraftLine());
            UpdateVisit();
        }
    }
    private void UpdateVisit()
    {
        if (DiagnosisText is null) return;
        var visit = ClinicalData.Find("LICH_SU_KHAM_BENH", VisitBox.SelectedValue);
        DiagnosisText.Text = visit is null ? "Chọn lần khám để xem chẩn đoán" : UiRecord.Text(visit, "ChanDoan");
        if (visit is not null && DoctorBox.SelectedValue is null) DoctorBox.SelectedValue = UiRecord.Get(visit, "MaBS");
    }
    private void Visit_Changed(object sender, SelectionChangedEventArgs e) => UpdateVisit();
    private void AddMedicine_Click(object sender, RoutedEventArgs e) { e.Handled = true; draft.Add(new RxDraftLine()); }
    private void RemoveMedicine_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if ((sender as FrameworkElement)?.DataContext is RxDraftLine line) draft.Remove(line);
    }
    public bool Commit(out string error)
    {
        string failure="";
        try {var result=ClinicalData.Transaction(()=>CommitCore(out failure));error=failure;return result;}
        catch(InvalidOperationException ex) {error=ex.Message;return false;}
    }
    private bool CommitCore(out string error)
    {
        ClinicalForm.VerifyUnchanged(this);
        error = "";
        var table = ClinicalForm.GetTable(this);
        var existing = ClinicalForm.GetRecord(this);
        if (table is not "DON_THUOC" and not "CHI_TIET_DON_THUOC")
        { error = "Không xác định được loại dữ liệu đơn thuốc đang chỉnh sửa."; return false; }
        if (existing is not null && !ClinicalData.CanEdit(table, existing))
        { error = "Đơn đã xác nhận hoặc đã hủy được giữ nguyên. Hãy tạo đơn mới độc lập."; return false; }
        if (draft.Count == 0 || table == "CHI_TIET_DON_THUOC" && draft.Count != 1)
        { error = "Cần có ít nhất một thuốc trong đơn."; return false; }
        var values = new List<Dictionary<string, object?>>();
        for (var index = 0; index < draft.Count; index++)
        {
            var line = draft[index];
            var medicine = ClinicalData.Find("THUOC", line.MaThuoc);
            if (medicine is null || ClinicalData.Find("HANG_HOA", UiRecord.Get(medicine, "MaHH")) is null || string.IsNullOrWhiteSpace(line.LieuDung) || string.IsNullOrWhiteSpace(line.HuongDan))
            { error = $"Thuốc {index + 1}: chọn thuốc hợp lệ, nhập liều dùng và hướng dẫn cụ thể."; return false; }
            if (!ClinicalRules.TryPrescriptionNumbers(line.SoLuong, line.SoNgayDung, out var quantity, out var days))
            { error = $"Thuốc {index + 1}: số lượng phải lớn hơn 0, số ngày dùng phải là số nguyên lớn hơn 0."; return false; }
            if (line.Existing is object original &&
                (ClinicalData.Find("CHI_TIET_DON_THUOC", UiRecord.Get(original, "MaCTDT")) is not object current
                 || !ReferenceEquals(current, original) || !ClinicalData.CanEdit("CHI_TIET_DON_THUOC", current)))
            { error = $"Thuốc {index + 1} đã thay đổi trong bộ dữ liệu. Mở lại đơn thuốc trước khi lưu."; return false; }
            values.Add(new() { ["MaThuoc"] = line.MaThuoc, ["LieuDung"] = line.LieuDung.Trim(), ["SoLuong"] = quantity, ["SoNgayDung"] = days, ["HuongDan"] = line.HuongDan.Trim() });
        }
        if (table == "CHI_TIET_DON_THUOC")
        {
            var parent = ClinicalData.Find("DON_THUOC", ParentBox.SelectedValue);
            if (parent is null || !UiRecord.Text(parent, "TrangThai").Equals("Nháp", StringComparison.CurrentCultureIgnoreCase))
            { error = "Chỉ thêm hoặc sửa thuốc trong một đơn nháp hợp lệ."; return false; }
            var scope = ClinicalForm.GetPatientScope(this);
            if (!string.IsNullOrEmpty(scope) && ClinicalData.PatientId("DON_THUOC", parent) != scope
                || existing is not null && UiRecord.Text(existing, "MaDT") != UiRecord.Text(parent, "MaDT"))
            { error = "Dòng thuốc phải thuộc đơn nháp đang chỉnh sửa của đúng bệnh nhân."; return false; }
            values[0]["MaDT"] = UiRecord.Get(parent, "MaDT");
            ClinicalData.Upsert(table, values[0], existing);
            return true;
        }
        Dictionary<string, object?> header;
        try { header = ClinicalForm.Read(this); }
        catch (InvalidOperationException exception) { error = exception.Message; return false; }
        var visit = ClinicalData.Find("LICH_SU_KHAM_BENH", header.GetValueOrDefault("MaLS"));
        var patient = visit is null ? "" : ClinicalData.PatientId("LICH_SU_KHAM_BENH", visit);
        if (visit is null || string.IsNullOrEmpty(patient) || ClinicalData.Find("BENH_NHAN", patient) is null
            || ClinicalData.Find("NHAN_VIEN", header.GetValueOrDefault("MaBS")) is null || header.GetValueOrDefault("NgayKe") is not DateTime)
        { error = "Chọn lần khám có liên kết bệnh nhân, bác sĩ kê đơn và ngày kê hợp lệ."; return false; }
        var patientScope = ClinicalForm.GetPatientScope(this);
        if (!string.IsNullOrEmpty(patientScope) && patient != patientScope
            || existing is not null && UiRecord.Text(existing, "MaLS") != UiRecord.Text(visit, "MaLS"))
        { error = "Đơn thuốc phải gắn với lần khám đang chỉnh sửa của đúng bệnh nhân."; return false; }
        var existingId = existing is null ? "" : UiRecord.Text(existing, "MaDT");
        if (existing is not null && !ReferenceEquals(ClinicalData.Find("DON_THUOC", existingId), existing))
        { error = "Đơn thuốc đã thay đổi trong bộ dữ liệu. Mở lại đơn trước khi lưu."; return false; }
        if (draft.Any(line => line.Existing is object original && UiRecord.Text(original, "MaDT") != existingId))
        { error = "Dòng thuốc không thuộc đơn đang chỉnh sửa."; return false; }
        // All row and header input is validated before changing any session data.
        if (!ClinicalForm.TrySave(this, out var saved, out error) || saved is null) return false;
        var id = UiRecord.Get(saved, "MaDT");
        var originalLines = ClinicalData.Children("CHI_TIET_DON_THUOC", "MaDT", id);
        for (var index = 0; index < draft.Count; index++)
        {
            values[index]["MaDT"] = id;
            ClinicalData.Upsert("CHI_TIET_DON_THUOC", values[index], draft[index].Existing);
        }
        foreach (var removed in originalLines.Where(row => !draft.Any(line => ReferenceEquals(line.Existing, row))))
            ClinicalData.Delete("CHI_TIET_DON_THUOC", removed);
        return true;
    }
}
