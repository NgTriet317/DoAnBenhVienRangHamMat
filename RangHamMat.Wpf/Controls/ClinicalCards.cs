namespace RangHamMat.Controls;

// Data for static XAML templates; these classes create no visual elements.
public sealed class ExaminationCard
{
    public object Record { get; }
    public bool ShowOpen { get; }
    public string Table => "LICH_SU_KHAM_BENH";
    public string Code => UiRecord.Text(Record, "MaLS");
    public string When => UiRecord.Date(Record, "ThoiGianKham")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa có thời gian khám";
    public string Patient { get; }
    public string Status => UiRecord.Text(Record, "TrangThai");
    public object[] Conditions { get; }
    public object[] Plans { get; }
    public object[] Services { get; }
    public string ConditionsNote => Conditions.Length == 0 ? "Chưa ghi nhận tình trạng bệnh." : "";
    public string PlansNote => Plans.Length == 0 ? "Chưa lập kế hoạch điều trị." : "";
    public string ServicesNote => Services.Length == 0 ? "Chưa có dịch vụ thực hiện." : "";
    public ExaminationCard(object record, bool detail = false)
    {
        Record = record;
        ShowOpen = !detail;
        var patient = ClinicalData.Find("BENH_NHAN", ClinicalData.PatientId(Table, record));
        Patient = patient is null ? "Chưa có thông tin bệnh nhân" : ClinicalData.Label("BENH_NHAN", patient);
        var key = UiRecord.Get(record, "MaLS");
        Conditions = ClinicalData.Children("TINH_TRANG_BENH", "MaLS", key);
        Plans = ClinicalData.Children("KE_HOACH_DIEU_TRI", "MaLS", key);
        Services = ClinicalData.Children("DICH_VU_THUC_HIEN", "MaLS", key);
    }
}

public sealed class PrescriptionCard
{
    public object Record { get; }
    public bool ShowOpen { get; }
    public string Table => "DON_THUOC";
    public string Code => UiRecord.Text(Record, "MaDT");
    public string Status => UiRecord.Text(Record, "TrangThai");
    public string When => UiRecord.Date(Record, "NgayKe")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa có ngày kê";
    public string Patient { get; }
    public string Doctor => ClinicalData.Lookup("MaBS", UiRecord.Get(Record, "MaBS"));
    public string Diagnosis { get; }
    public PrescriptionLine[] Lines { get; }
    public string LinesNote => Lines.Length == 0 ? "Chưa có thuốc trong đơn." : "";
    public bool CanAdd => Status.Equals("Nháp", StringComparison.CurrentCultureIgnoreCase) && UiRecord.Text(Record, "MaDT").Length > 0;
    public CreationContext AddContext => new("CHI_TIET_DON_THUOC", "MaDT", UiRecord.Get(Record, "MaDT")!);
    public PrescriptionCard(object record, bool detail = false)
    {
        Record = record;
        ShowOpen = !detail;
        var patient = ClinicalData.Find("BENH_NHAN", ClinicalData.PatientId(Table, record));
        Patient = patient is null ? "Chưa có thông tin bệnh nhân" : ClinicalData.Label("BENH_NHAN", patient);
        var exam = ClinicalData.Find("LICH_SU_KHAM_BENH", UiRecord.Get(record, "MaLS"));
        Diagnosis = exam is null ? "Chưa có thông tin" : UiRecord.Text(exam, "ChanDoan");
        Lines = ClinicalData.Children("CHI_TIET_DON_THUOC", "MaDT", UiRecord.Get(record, "MaDT")).Select(row => new PrescriptionLine(row)).ToArray();
    }
}

public sealed class PrescriptionLine
{
    public object Record { get; }
    public string Label => ClinicalData.Lookup("MaThuoc", UiRecord.Get(Record, "MaThuoc"));
    public string Unit { get; }
    public string LieuDung => UiRecord.Text(Record, "LieuDung");
    public string SoLuong => UiRecord.Text(Record, "SoLuong");
    public string SoNgayDung => UiRecord.Text(Record, "SoNgayDung");
    public string HuongDan => UiRecord.Text(Record, "HuongDan");
    public PrescriptionLine(object record)
    {
        Record = record;
        var drug = ClinicalData.Find("THUOC", UiRecord.Get(record, "MaThuoc"));
        var goods = drug is null ? null : ClinicalData.Find("HANG_HOA", UiRecord.Get(drug, "MaHH"));
        Unit = goods is null ? "" : UiRecord.Text(goods, "DonViTinh");
    }
}
