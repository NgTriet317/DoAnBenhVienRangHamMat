// PHẦN AI ĐÃ THỰC HIỆN: AI đã bổ sung chú thích tiếng Việt cho TheHoSo.cs: Dữ liệu cho template thẻ lần khám gồm bệnh nhân, tình trạng, kế hoạch và dịch vụ; lớp này không tạo phần tử giao diện. Giải thích được đặt cạnh khai báo/hàm tương ứng.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
namespace RangHamMat.Controls;

// Dữ liệu cho các template XAML; các lớp này không tự tạo control giao diện.
/// <summary>
/// Dữ liệu cho template thẻ lần khám gồm bệnh nhân, tình trạng, kế hoạch và dịch vụ; lớp này không tạo phần tử giao diện.
/// </summary>
public sealed class ExaminationCard
{
    /// <summary>
    /// Bản ghi nguồn đã đọc để mô hình tra các trường/liên kết; không phải một bản SQL mới được tạo tự động.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Cờ hiển thị nút mở chi tiết trên thẻ; ẩn khi thẻ đã nằm trong chế độ xem chi tiết.
    /// </summary>
    public bool ShowOpen { get; }
    /// <summary>
    /// Tên bảng/loại nghiệp vụ nguồn để đọc trường và mở biểu mẫu đúng loại.
    /// </summary>
    public string Table => "LICH_SU_KHAM_BENH";
    /// <summary>
    /// Mã khóa của bản ghi dùng để hiển thị và xác định chính xác dòng đang thao tác.
    /// </summary>
    public string Code => UiRecord.Text(Record, "MaLS");
    /// <summary>
    /// Ngày/giờ của dòng nghiệp vụ được định dạng để hiển thị; nội dung mặc định cho biết chưa có thời gian.
    /// </summary>
    public string When => UiRecord.Date(Record, "ThoiGianKham")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa có thời gian khám";
    /// <summary>
    /// Thông tin hoặc nhãn bệnh nhân liên kết với bản ghi/hồ sơ đang xem.
    /// </summary>
    public string Patient { get; }
    /// <summary>
    /// Trạng thái hiển thị của bản ghi; với bước kế hoạch có thể được suy ra thêm từ lượng thực hiện đã xác nhận.
    /// </summary>
    public string Status => UiRecord.Text(Record, "TrangThai");
    /// <summary>
    /// Các dòng tình trạng bệnh gắn với lần khám hiện tại.
    /// </summary>
    public object[] Conditions { get; }
    /// <summary>
    /// Các kế hoạch điều trị liên quan được mô hình tổng hợp để binding giao diện.
    /// </summary>
    public object[] Plans { get; }
    /// <summary>
    /// Các dịch vụ thực hiện của lần khám hiện tại.
    /// </summary>
    public object[] Services { get; }
    /// <summary>
    /// Thông báo trạng thái trống khi chưa ghi nhận tình trạng bệnh.
    /// </summary>
    public string ConditionsNote => Conditions.Length == 0 ? "Chưa ghi nhận tình trạng bệnh." : "";
    /// <summary>
    /// Thông báo trạng thái trống khi chưa có kế hoạch điều trị.
    /// </summary>
    public string PlansNote => Plans.Length == 0 ? "Chưa lập kế hoạch điều trị." : "";
    /// <summary>
    /// Thông báo trạng thái trống khi chưa có dịch vụ thực hiện.
    /// </summary>
    public string ServicesNote => Services.Length == 0 ? "Chưa có dịch vụ thực hiện." : "";
    /// <summary>
    /// Tạo đối tượng ExaminationCard. Dữ liệu cho template thẻ lần khám gồm bệnh nhân, tình trạng, kế hoạch và dịch vụ; lớp này không tạo phần tử giao diện.
    /// </summary>
    public ExaminationCard(object record, bool detail = false)
    {
        Record = record;
        ShowOpen = !detail;
        var patient = DuLieuGiaoDien.Find("BENH_NHAN", DuLieuGiaoDien.PatientId(Table, record));
        Patient = patient is null ? "Chưa có thông tin bệnh nhân" : DuLieuGiaoDien.Label("BENH_NHAN", patient);
        var key = UiRecord.Get(record, "MaLS");
        Conditions = DuLieuGiaoDien.Children("TINH_TRANG_BENH", "MaLS", key);
        Plans = DuLieuGiaoDien.Children("KE_HOACH_DIEU_TRI", "MaLS", key);
        Services = DuLieuGiaoDien.Children("DICH_VU_THUC_HIEN", "MaLS", key);
    }
}

/// <summary>
/// Dữ liệu cho template thẻ đơn thuốc gồm bệnh nhân, bác sĩ, chẩn đoán, dòng thuốc và ngữ cảnh thêm thuốc vào đơn nháp.
/// </summary>
public sealed class PrescriptionCard
{
    /// <summary>
    /// Bản ghi nguồn đã đọc để mô hình tra các trường/liên kết; không phải một bản SQL mới được tạo tự động.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Cờ hiển thị nút mở chi tiết trên thẻ; ẩn khi thẻ đã nằm trong chế độ xem chi tiết.
    /// </summary>
    public bool ShowOpen { get; }
    /// <summary>
    /// Tên bảng/loại nghiệp vụ nguồn để đọc trường và mở biểu mẫu đúng loại.
    /// </summary>
    public string Table => "DON_THUOC";
    /// <summary>
    /// Mã khóa của bản ghi dùng để hiển thị và xác định chính xác dòng đang thao tác.
    /// </summary>
    public string Code => UiRecord.Text(Record, "MaDT");
    /// <summary>
    /// Trạng thái hiển thị của bản ghi; với bước kế hoạch có thể được suy ra thêm từ lượng thực hiện đã xác nhận.
    /// </summary>
    public string Status => UiRecord.Text(Record, "TrangThai");
    /// <summary>
    /// Ngày/giờ của dòng nghiệp vụ được định dạng để hiển thị; nội dung mặc định cho biết chưa có thời gian.
    /// </summary>
    public string When => UiRecord.Date(Record, "NgayKe")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa có ngày kê";
    /// <summary>
    /// Thông tin hoặc nhãn bệnh nhân liên kết với bản ghi/hồ sơ đang xem.
    /// </summary>
    public string Patient { get; }
    /// <summary>
    /// Nhãn bác sĩ hoặc nhân viên thực hiện được tra từ mã liên kết trong bản ghi nguồn.
    /// </summary>
    public string Doctor => DuLieuGiaoDien.Lookup("MaBS", UiRecord.Get(Record, "MaBS"));
    /// <summary>
    /// Chẩn đoán của lần khám nguồn để đọc cùng hồ sơ hoặc đơn thuốc.
    /// </summary>
    public string Diagnosis { get; }
    /// <summary>
    /// Các dòng thuốc thuộc đúng đơn đang hiển thị.
    /// </summary>
    public PrescriptionLine[] Lines { get; }
    /// <summary>
    /// Thông báo khi đơn thuốc chưa có dòng thuốc.
    /// </summary>
    public string LinesNote => Lines.Length == 0 ? "Chưa có thuốc trong đơn." : "";
    /// <summary>
    /// Cho phép thêm thuốc khi đơn có mã và đang ở trạng thái Nháp.
    /// </summary>
    public bool CanAdd => Status.Equals("Nháp", StringComparison.CurrentCultureIgnoreCase) && UiRecord.Text(Record, "MaDT").Length > 0;
    /// <summary>
    /// Ngữ cảnh thêm dòng thuốc với MaDT là khóa cha cố định của đơn đang xem.
    /// </summary>
    public CreationContext AddContext => new("CHI_TIET_DON_THUOC", "MaDT", UiRecord.Get(Record, "MaDT")!);
    /// <summary>
    /// Tạo đối tượng PrescriptionCard. Dữ liệu cho template thẻ đơn thuốc gồm bệnh nhân, bác sĩ, chẩn đoán, dòng thuốc và ngữ cảnh thêm thuốc vào đơn nháp.
    /// </summary>
    public PrescriptionCard(object record, bool detail = false)
    {
        Record = record;
        ShowOpen = !detail;
        var patient = DuLieuGiaoDien.Find("BENH_NHAN", DuLieuGiaoDien.PatientId(Table, record));
        Patient = patient is null ? "Chưa có thông tin bệnh nhân" : DuLieuGiaoDien.Label("BENH_NHAN", patient);
        var exam = DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", UiRecord.Get(record, "MaLS"));
        Diagnosis = exam is null ? "Chưa có thông tin" : UiRecord.Text(exam, "ChanDoan");
        Lines = DuLieuGiaoDien.Children("CHI_TIET_DON_THUOC", "MaDT", UiRecord.Get(record, "MaDT")).Select(row => new PrescriptionLine(row)).ToArray();
    }
}

/// <summary>
/// Mô hình dòng thuốc đã lưu: nhãn thuốc, đơn vị từ HANG_HOA, liều, lượng, số ngày và hướng dẫn dùng cho màn hình/in đơn.
/// </summary>
public sealed class PrescriptionLine
{
    /// <summary>
    /// Bản ghi nguồn đã đọc để mô hình tra các trường/liên kết; không phải một bản SQL mới được tạo tự động.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Nhãn dễ đọc cho lựa chọn/dòng dữ liệu; với ô lịch là mô tả hành động tạo lịch.
    /// </summary>
    public string Label => string.IsNullOrWhiteSpace(UiRecord.Text(Record,"TenThuocChot"))?DuLieuGiaoDien.Lookup("MaThuoc", UiRecord.Get(Record, "MaThuoc")):$"{UiRecord.Text(Record,"TenThuocChot")} · {UiRecord.Text(Record,"HamLuongChot")}";
    public string DuongDung => UiRecord.Text(Record,"DuongDung");
    /// <summary>
    /// Đơn vị tính của thuốc lấy từ hàng hóa liên kết.
    /// </summary>
    public string Unit { get; }
    /// <summary>
    /// Liều dùng thuốc. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string LieuDung => UiRecord.Text(Record, "LieuDung");
    /// <summary>
    /// Số lượng thuốc/hàng hóa/dịch vụ. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string SoLuong => UiRecord.Text(Record, "SoLuong");
    /// <summary>
    /// Số ngày sử dụng thuốc. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string SoNgayDung => UiRecord.Text(Record, "SoNgayDung");
    /// <summary>
    /// Hướng dẫn sử dụng thuốc. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string HuongDan => UiRecord.Text(Record, "HuongDan");
    /// <summary>
    /// Tạo đối tượng PrescriptionLine. Mô hình dòng thuốc đã lưu: nhãn thuốc, đơn vị từ HANG_HOA, liều, lượng, số ngày và hướng dẫn dùng cho màn hình/in đơn.
    /// </summary>
    public PrescriptionLine(object record)
    {
        Record = record;
        var drug = DuLieuGiaoDien.Find("THUOC", UiRecord.Get(record, "MaThuoc"));
        var goods = drug is null ? null : DuLieuGiaoDien.Find("HANG_HOA", UiRecord.Get(drug, "MaHH"));
        var snapshotUnit=UiRecord.Text(record,"DonViTinhChot");Unit = !string.IsNullOrWhiteSpace(snapshotUnit)?snapshotUnit:goods is null ? "" : UiRecord.Text(goods, "DonViTinh");
    }
}
