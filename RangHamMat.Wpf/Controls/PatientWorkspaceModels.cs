namespace RangHamMat.Controls;

// Display-only projection. Arrival kind belongs to the reception record, not the patient.
public sealed class PatientVisitRow
{
    public object Record { get; }
    public PatientVisitRow(object record) => Record = record;
    public string Id => UiRecord.Text(Record, "MaLS");
    public string When => UiRecord.Date(Record, "ThoiGianKham")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa ghi giờ khám";
    public string Doctor => ClinicalData.Lookup("MaBS", UiRecord.Get(Record, "MaBS"));
    public string Diagnosis => UiRecord.Text(Record, "ChanDoan");
    public string Status => UiRecord.Text(Record, "TrangThai");
    public string Arrival
    {
        get
        {
            var receipt = ClinicalData.Find("PHIEU_KHAM_BENH", UiRecord.Get(Record, "MaPhieu"));
            return receipt is null ? "Thiếu phiếu tiếp nhận" : string.IsNullOrWhiteSpace(UiRecord.Text(receipt, "MaLich")) ? "Trực tiếp" : "Theo lịch hẹn";
        }
    }
}
