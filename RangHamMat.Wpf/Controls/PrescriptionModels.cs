namespace RangHamMat.Controls;

// Prescription-specific presentation data. All visual layout remains in XAML.
public sealed class RxCard
{
    public object Record { get; }
    public string Code => UiRecord.Text(Record, "MaDT");
    public string Status => UiRecord.Text(Record, "TrangThai");
    public bool IsDraft => Status.Equals("Nháp", StringComparison.CurrentCultureIgnoreCase);
    public bool CanCancel => IsDraft;
    public bool CanPrint => Status.Equals("Đã xác nhận", StringComparison.CurrentCultureIgnoreCase);
    public bool IsExpanded { get; set; }
    public string When => UiRecord.Date(Record, "NgayKe")?.ToString("dd/MM/yyyy HH:mm") ?? "—";
    public string Doctor => ClinicalData.Lookup("MaBS", UiRecord.Get(Record, "MaBS"));
    public string Visit => UiRecord.Text(Record, "MaLS");
    public string PatientId => ClinicalData.PatientId("DON_THUOC", Record);
    public string VisitWhen => ClinicalData.Find("LICH_SU_KHAM_BENH", Visit) is object visit
        ? UiRecord.Date(visit, "ThoiGianKham")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa có thời gian khám"
        : "Không tìm thấy lần khám";
    public string Diagnosis { get; }
    public string Patient { get; }
    public string Phone { get; }
    public string Note => UiRecord.Text(Record, "GhiChu");
    public string Reason => UiRecord.Text(Record, "LyDoThayThe");
    public PrescriptionLine[] Lines { get; }
    public string LinesNote => Lines.Length == 0 ? "Chưa có thuốc trong đơn." : "";
    public RxCard(object record, bool expanded = false)
    {
        Record = record; IsExpanded = expanded;
        var patient = ClinicalData.Find("BENH_NHAN", ClinicalData.PatientId("DON_THUOC", record));
        Patient = patient is null ? "—" : ClinicalData.Label("BENH_NHAN", patient);
        Phone = patient is null ? "—" : UiRecord.Text(patient, "SDT");
        var visit = ClinicalData.Find("LICH_SU_KHAM_BENH", UiRecord.Get(record, "MaLS"));
        Diagnosis = visit is null ? "—" : UiRecord.Text(visit, "ChanDoan");
        Lines = ClinicalData.Children("CHI_TIET_DON_THUOC", "MaDT", UiRecord.Get(record, "MaDT")).Select(row => new PrescriptionLine(row)).ToArray();
    }
}

public sealed class RxDraftLine
{
    public object? Existing { get; init; }
    public string MaThuoc { get; set; } = "";
    public string LieuDung { get; set; } = "";
    public string SoLuong { get; set; } = "1";
    public string SoNgayDung { get; set; } = "1";
    public string HuongDan { get; set; } = "";
    public RxDraftLine() { }
    public RxDraftLine(object row)
    {
        Existing = row; MaThuoc = UiRecord.Text(row, "MaThuoc"); LieuDung = UiRecord.Text(row, "LieuDung");
        SoLuong = UiRecord.Text(row, "SoLuong"); SoNgayDung = UiRecord.Text(row, "SoNgayDung"); HuongDan = UiRecord.Text(row, "HuongDan");
    }
}

// Validate the complete saved order before its immutable confirmation transition.
public static class PrescriptionValidation
{
    public static bool TryValidate(object order, out string error)
    {
        error = "";
        var visit = ClinicalData.Find("LICH_SU_KHAM_BENH", UiRecord.Get(order, "MaLS"));
        var patientId = ClinicalData.PatientId("DON_THUOC", order);
        if (visit is null || string.IsNullOrEmpty(patientId) || ClinicalData.Find("BENH_NHAN", patientId) is null
            || ClinicalData.Find("NHAN_VIEN", UiRecord.Get(order, "MaBS")) is null || UiRecord.Date(order, "NgayKe") is null)
        { error = "Kiểm tra liên kết lần khám, bệnh nhân, bác sĩ và ngày kê trước khi xác nhận."; return false; }
        var lines = ClinicalData.Children("CHI_TIET_DON_THUOC", "MaDT", UiRecord.Get(order, "MaDT"));
        if (lines.Length == 0) { error = "Đơn cần có ít nhất một thuốc trước khi xác nhận."; return false; }
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var drug = ClinicalData.Find("THUOC", UiRecord.Get(line, "MaThuoc"));
            if (drug is null || ClinicalData.Find("HANG_HOA", UiRecord.Get(drug, "MaHH")) is null
                || string.IsNullOrWhiteSpace(UiRecord.Text(line, "LieuDung")) || string.IsNullOrWhiteSpace(UiRecord.Text(line, "HuongDan"))
                || !ClinicalRules.TryPrescriptionNumbers(UiRecord.Text(line, "SoLuong"), UiRecord.Text(line, "SoNgayDung"), out _, out _))
            { error = $"Thuốc {index + 1}: kiểm tra danh mục thuốc, liều dùng, số lượng, số ngày và hướng dẫn."; return false; }
        }
        return true;
    }
}
