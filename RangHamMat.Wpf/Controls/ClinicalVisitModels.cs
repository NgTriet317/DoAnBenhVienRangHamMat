namespace RangHamMat.Controls;

// Display models deliberately expose named clinical fields; grids never inspect arbitrary SQL columns.
public sealed class ClinicalVisitSummary
{
    public object Record { get; }
    public TreatmentVisit Visit { get; }
    public string Code => Visit.Code;
    public string When => Visit.When;
    public string PatientId => ClinicalData.PatientId("LICH_SU_KHAM_BENH", Record);
    public string Patient => Visit.Patient;
    public string Status => Visit.Status;
    public string Doctor => ClinicalData.Lookup("MaBS", UiRecord.Get(Record, "MaBS"));
    public string Room => ClinicalData.Lookup("MaPhong", UiRecord.Get(Record, "MaPhong"));
    public string Receipt => UiRecord.Text(Record, "MaPhieu");
    public string ReceiptTime { get; }
    public string Reason { get; }
    public string Confirmation => UiRecord.Date(Record, "NgayXacNhan")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa xác nhận";
    public ClinicalVisitRow[] Conditions { get; }
    public ClinicalVisitPlan[] Plans { get; }
    public ClinicalVisitRow[] Services { get; }
    public ClinicalVisitRow[] OutsidePlan => Services.Where(row => string.IsNullOrEmpty(row.Step)).ToArray();
    public PrescriptionCard[] Orders { get; }
    public string ConditionsNote => Conditions.Length == 0 ? "Chưa ghi nhận tình trạng bệnh." : "";
    public string PlansNote => Plans.Length == 0 ? "Chưa lập kế hoạch điều trị." : "";
    public string ServicesNote => Services.Length == 0 ? "Chưa ghi nhận dịch vụ tại lần khám này." : "";
    public string OutsideNote => OutsidePlan.Length == 0 ? "Không có dịch vụ phát sinh ngoài kế hoạch tại lần khám này." : "";
    public string OrdersNote => Orders.Length == 0 ? "Chưa có đơn thuốc tại lần khám này." : "";
    public ClinicalVisitSummary(object record, TreatmentVisit? draft = null)
    {
        Record = record; Visit = new(record); if (draft is not null) Visit.CopyDraftFrom(draft);
        var receipt = ClinicalData.Find("PHIEU_KHAM_BENH", UiRecord.Get(record, "MaPhieu"));
        ReceiptTime = receipt is null ? "Chưa có thông tin" : UiRecord.Date(receipt, "ThoiGianTiepNhan")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa có thời gian";
        Reason = receipt is null ? "" : UiRecord.Text(receipt, "LyDoKham");
        Conditions = ClinicalData.Children("TINH_TRANG_BENH", "MaLS", Code).Select(row => new ClinicalVisitRow("TINH_TRANG_BENH", row)).ToArray();
        Plans = ClinicalData.Rows("KE_HOACH_DIEU_TRI").Where(row => PatientId.Length > 0 && ClinicalData.PatientId("KE_HOACH_DIEU_TRI", row) == PatientId)
            .OrderByDescending(row => UiRecord.Text(row, "MaLS") == Code).ThenByDescending(row => UiRecord.Date(row, "NgayLap")).Select(row => new ClinicalVisitPlan(row)).ToArray();
        Services = ClinicalData.Children("DICH_VU_THUC_HIEN", "MaLS", Code).OrderBy(row => UiRecord.Date(row, "ThoiGianThucHien")).Select(row => new ClinicalVisitRow("DICH_VU_THUC_HIEN", row)).ToArray();
        Orders = ClinicalData.Children("DON_THUOC", "MaLS", Code).OrderByDescending(row => UiRecord.Date(row, "NgayKe")).Select(row => new PrescriptionCard(row)).ToArray();
    }
}
public sealed class ClinicalVisitPlan
{
    public object Record { get; }
    public string Code => UiRecord.Text(Record, "MaKHDT");
    public string Status => UiRecord.Text(Record, "TrangThai");
    public string Doctor => ClinicalData.Lookup("MaBS", UiRecord.Get(Record, "MaBS"));
    public string Created => UiRecord.Date(Record, "NgayLap")?.ToString("dd/MM/yyyy") ?? "Chưa có ngày lập";
    public string OriginVisit => "Lần khám " + UiRecord.Text(Record, "MaLS");
    public string Start => UiRecord.Date(Record, "NgayBatDau")?.ToString("dd/MM/yyyy") ?? "Chưa dự kiến";
    public string End => UiRecord.Date(Record, "NgayDuKienKetThuc")?.ToString("dd/MM/yyyy") ?? "Chưa dự kiến";
    public string Note => UiRecord.Text(Record, "GhiChu");
    public bool CanEdit => ClinicalData.CanEdit("KE_HOACH_DIEU_TRI", Record) && Steps.All(row => row.CanEdit);
    public bool CanConfirm => ClinicalData.CanEdit("KE_HOACH_DIEU_TRI", Record) && Steps.Length > 0;
    public ClinicalVisitRow[] Steps { get; }
    public ClinicalVisitRow[] Executions { get; }
    public string Total => TreatmentMoney.Format(Steps.Where(row => row.Status != "Hủy").Sum(row => row.Amount));
    public string StepsNote => Steps.Length == 0 ? "Chưa có bước điều trị. Thêm bước trước khi xác nhận kế hoạch." : "";
    public string ExecutionsNote => Executions.Length == 0 ? "Chưa ghi nhận thực hiện cho kế hoạch này." : "";
    public ClinicalVisitPlan(object record)
    {
        Record = record;
        Steps = ClinicalData.Children("CHI_TIET_KE_HOACH", "MaKHDT", Code).OrderBy(row => TreatmentMoney.Number(row, "LanThu")).Select(row => new ClinicalVisitRow("CHI_TIET_KE_HOACH", row)).ToArray();
        var stepIds = Steps.Select(row => row.Code).ToHashSet(StringComparer.Ordinal);
        var patientId = ClinicalData.PatientId("KE_HOACH_DIEU_TRI", record);
        // A plan can be carried out at another visit. Only the same patient's executions belong here.
        Executions = ClinicalData.Rows("DICH_VU_THUC_HIEN").Where(row => stepIds.Contains(UiRecord.Text(row, "MaCTKH")) && patientId.Length > 0 && ClinicalData.PatientId("DICH_VU_THUC_HIEN", row) == patientId).OrderBy(row => UiRecord.Date(row, "ThoiGianThucHien")).Select(row => new ClinicalVisitRow("DICH_VU_THUC_HIEN", row)).ToArray();
    }
}
public sealed class ClinicalVisitRow
{
    public object Record { get; }
    public string Table { get; }
    public string Code => UiRecord.Text(Record, ClinicalData.Key(Table));
    public string VisitId => UiRecord.Text(Record, "MaLS");
    public string PatientId => ClinicalData.PatientId(Table, Record);
    public string Patient => ClinicalData.Lookup("MaBN",PatientId);
    public string Step => UiRecord.Text(Record, "MaCTKH");
    public string Type => UiRecord.Text(Record, "LoaiTinhTrang");
    public string Description => UiRecord.Text(Record, "MoTaTinhTrang");
    public string Severity => UiRecord.Text(Record, "MucDo");
    public string Note => UiRecord.Text(Record, "GhiChu");
    public string Service => ClinicalData.Lookup("MaDV", UiRecord.Get(Record, "MaDV"));
    public string Doctor => ClinicalData.Lookup("MaNVThucHien", UiRecord.Get(Record, "MaNVThucHien"));
    public string When => UiRecord.Date(Record, "ThoiGianThucHien")?.ToString("dd/MM/yyyy HH:mm") ?? "";
    public string Result => UiRecord.Text(Record, "KetQua");
    public string Sequence => UiRecord.Text(Record, "LanThu");
    public decimal CompletedQuantity => Table == "CHI_TIET_KE_HOACH" ? ClinicalData.Children("DICH_VU_THUC_HIEN", "MaCTKH", Code)
        .Where(row => UiRecord.Text(row, "TrangThai") == "Đã xác nhận" && ClinicalData.PatientId("DICH_VU_THUC_HIEN", row) == PatientId)
        .Sum(row => TreatmentMoney.Number(row, "SoLuong")) : 0;
    public decimal RemainingQuantity => Math.Max(0, Quantity - CompletedQuantity);
    public string Status => Table == "CHI_TIET_KE_HOACH" ? ClinicalRules.PlanStepStatus(UiRecord.Text(Record, "TrangThai"), Quantity, CompletedQuantity) : UiRecord.Text(Record, "TrangThai");
    public decimal Quantity => TreatmentMoney.Number(Record, Table == "CHI_TIET_KE_HOACH" ? "SoLuongDuKien" : "SoLuong");
    public decimal Price => TreatmentMoney.Number(Record, Table == "CHI_TIET_KE_HOACH" ? "DonGiaDuKien" : "DonGia");
    public decimal Amount => decimal.Round(Quantity * Price,2,MidpointRounding.AwayFromZero);
    public string PriceText => TreatmentMoney.Format(Price);
    public string AmountText => TreatmentMoney.Format(Amount);
    public bool CanEdit => ClinicalData.CanEdit(Table, Record);
    public bool CanExecute => Table == "CHI_TIET_KE_HOACH" && (Status is not "Hủy" and not "Hoàn thành") &&
        ClinicalData.Find("KE_HOACH_DIEU_TRI", UiRecord.Get(Record, "MaKHDT")) is object plan && (UiRecord.Text(plan, "TrangThai") is not "Hủy" and not "Hoàn thành");
    public ClinicalVisitRow(string table, object record) { Table = table; Record = record; }
}
public sealed class ClinicalVisitListItem
{
    public object Record { get; }
    public string Code => UiRecord.Text(Record, "MaLS");
    public string PatientId => ClinicalData.PatientId("LICH_SU_KHAM_BENH", Record);
    public string Patient => ClinicalData.Lookup("MaBN", PatientId);
    public string When => UiRecord.Date(Record, "ThoiGianKham")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa có thời gian";
    public string Status => UiRecord.Text(Record, "TrangThai");
    public string Diagnosis => UiRecord.Text(Record, "ChanDoan");
    public string Doctor => ClinicalData.Lookup("MaBS", UiRecord.Get(Record, "MaBS"));
    public ClinicalVisitListItem(object record) { Record = record; }
}
