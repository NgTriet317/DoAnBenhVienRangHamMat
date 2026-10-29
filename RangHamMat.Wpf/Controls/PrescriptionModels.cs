// PHẦN AI ĐÃ THỰC HIỆN: AI đã bổ sung chú thích tiếng Việt cho PrescriptionModels.cs: Thông tin một đơn thuốc để hiển thị/in: bệnh nhân, lần khám, chẩn đoán, bác sĩ, các dòng thuốc và khả năng thao tác theo trạng thái. Giải thích được đặt cạnh khai báo/hàm tương ứng.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
namespace RangHamMat.Controls;

// Dữ liệu hiển thị của đơn thuốc; các bố cục giao diện vẫn khai báo trong XAML.
/// <summary>
/// Thông tin một đơn thuốc để hiển thị/in: bệnh nhân, lần khám, chẩn đoán, bác sĩ, các dòng thuốc và khả năng thao tác theo trạng thái.
/// </summary>
public sealed class RxCard
{
    /// <summary>
    /// Bản ghi nguồn đã đọc để mô hình tra các trường/liên kết; không phải một bản SQL mới được tạo tự động.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Mã khóa của bản ghi dùng để hiển thị và xác định chính xác dòng đang thao tác.
    /// </summary>
    public string Code => UiRecord.Text(Record, "MaDT");
    /// <summary>
    /// Trạng thái hiển thị của bản ghi; với bước kế hoạch có thể được suy ra thêm từ lượng thực hiện đã xác nhận.
    /// </summary>
    public string Status => UiRecord.Text(Record, "TrangThai");
    /// <summary>
    /// Cho biết đơn còn ở trạng thái Nháp, dùng để bật các thao tác sửa/xác nhận/hủy.
    /// </summary>
    public bool IsDraft => Status.Equals("Nháp", StringComparison.CurrentCultureIgnoreCase);
    /// <summary>
    /// Cho biết đơn nháp được phép hủy theo mô hình hiển thị.
    /// </summary>
    public bool CanCancel => IsDraft;
    /// <summary>
    /// Chỉ bật in đơn khi trạng thái đơn là Đã xác nhận.
    /// </summary>
    public bool CanPrint => Status.Equals("Đã xác nhận", StringComparison.CurrentCultureIgnoreCase);
    /// <summary>
    /// Trạng thái mở rộng phần chi tiết của thẻ đơn thuốc ở giao diện.
    /// </summary>
    public bool IsExpanded { get; set; }
    /// <summary>
    /// Ngày/giờ của dòng nghiệp vụ được định dạng để hiển thị; nội dung mặc định cho biết chưa có thời gian.
    /// </summary>
    public string When => UiRecord.Date(Record, "NgayKe")?.ToString("dd/MM/yyyy HH:mm") ?? "—";
    /// <summary>
    /// Nhãn bác sĩ hoặc nhân viên thực hiện được tra từ mã liên kết trong bản ghi nguồn.
    /// </summary>
    public string Doctor => Record is ET.DongLichSuDon row?row.TenBS:DuLieuGiaoDien.Lookup("MaBS", UiRecord.Get(Record, "MaBS"));
    /// <summary>
    /// Lần khám nguồn của dữ liệu đang xem; dùng để mở lại lần khám hoặc tổng hợp thông tin điều trị.
    /// </summary>
    public string Visit => UiRecord.Text(Record, "MaLS");
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string PatientId => Record is ET.DongLichSuDon row?row.MaBN:DuLieuGiaoDien.PatientId("DON_THUOC", Record);
    /// <summary>
    /// Ngày/giờ của lần khám nguồn; hiển thị thông báo khi liên kết lần khám không tồn tại.
    /// </summary>
    public string VisitWhen => Record is ET.DongLichSuDon row?row.ThoiGianKham.ToString("dd/MM/yyyy HH:mm"):DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", Visit) is object visit
        ? UiRecord.Date(visit, "ThoiGianKham")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa có thời gian khám"
        : "Không tìm thấy lần khám";
    /// <summary>
    /// Chẩn đoán của lần khám nguồn để đọc cùng hồ sơ hoặc đơn thuốc.
    /// </summary>
    public string Diagnosis { get; }
    /// <summary>
    /// Thông tin hoặc nhãn bệnh nhân liên kết với bản ghi/hồ sơ đang xem.
    /// </summary>
    public string Patient { get; }
    /// <summary>
    /// Số điện thoại của bệnh nhân nguồn, giữ dạng chuỗi để không mất số 0 đầu.
    /// </summary>
    public string Phone { get; }
    /// <summary>
    /// Ngày sinh.
    /// </summary>
    public string BirthDate { get; }
    /// <summary>
    /// Ghi chú của bản ghi/dòng nháp để hiển thị hoặc nhập bổ sung trước khi lưu.
    /// </summary>
    public string Note => UiRecord.Text(Record, "GhiChu");
    /// <summary>
    /// Lý do nghiệp vụ đọc từ tiếp nhận, lịch hẹn hoặc đơn thuốc nguồn.
    /// </summary>
    public string Reason => UiRecord.Text(Record, "LyDoThayThe");
    /// <summary>
    /// Các dòng thuốc thuộc đúng đơn đang hiển thị.
    /// </summary>
    public PrescriptionLine[] Lines { get; }
    /// <summary>
    /// Thông báo khi đơn thuốc chưa có dòng thuốc.
    /// </summary>
    public string LinesNote => Lines.Length == 0 ? "Chưa có thuốc trong đơn." : "";
    /// <summary>
    /// Tạo đối tượng RxCard. Thông tin một đơn thuốc để hiển thị/in: bệnh nhân, lần khám, chẩn đoán, bác sĩ, các dòng thuốc và khả năng thao tác theo trạng thái.
    /// </summary>
    public RxCard(object record, bool expanded = false)
    {
        Record = record; IsExpanded = expanded;
        if(record is ET.DongLichSuDon dto)
        {
            Patient=$"{dto.HoTenBN} ({dto.MaBN})";Phone=dto.SDT??"—";BirthDate=dto.NgaySinh?.ToString("dd/MM/yyyy")??"chưa ghi nhận";Diagnosis=dto.ChanDoan;
            Lines=dto.CacDong.Select(r=>new PrescriptionLine(r)).ToArray();return;
        }
        var patient = DuLieuGiaoDien.Find("BENH_NHAN", DuLieuGiaoDien.PatientId("DON_THUOC", record));
        Patient = patient is null ? "—" : DuLieuGiaoDien.Label("BENH_NHAN", patient);
        Phone = patient is null ? "—" : UiRecord.Text(patient, "SDT");
        BirthDate = patient is null?"chưa ghi nhận":UiRecord.Date(patient,"NgaySinh")?.ToString("dd/MM/yyyy")??"chưa ghi nhận";
        var visit = DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", UiRecord.Get(record, "MaLS"));
        Diagnosis = visit is null ? "—" : UiRecord.Text(visit, "ChanDoan");
        Lines = DuLieuGiaoDien.Children("CHI_TIET_DON_THUOC", "MaDT", UiRecord.Get(record, "MaDT")).Select(row => new PrescriptionLine(row)).ToArray();
    }
}

/// <summary>
/// Dòng thuốc đang nhập trong form; giữ dữ liệu chuỗi và tham chiếu dòng cũ, chỉ được chuyển sang ET/lưu SQL khi người dùng lưu đơn.
/// </summary>
public sealed class RxDraftLine
{
    /// <summary>
    /// Tham chiếu bản ghi cũ của dòng nháp; null nghĩa là thêm mới, có giá trị nghĩa là sửa dòng đang có.
    /// </summary>
    public object? Existing { get; init; }
    /// <summary>
    /// Mã thuốc. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string MaThuoc { get; set; } = "";
    /// <summary>
    /// Liều dùng thuốc. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string LieuDung { get; set; } = "";
    /// <summary>
    /// Số lượng thuốc/hàng hóa/dịch vụ. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string SoLuong { get; set; } = "1";
    /// <summary>
    /// Số ngày sử dụng thuốc. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string SoNgayDung { get; set; } = "1";
    /// <summary>
    /// Hướng dẫn sử dụng thuốc. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string HuongDan { get; set; } = "";
    /// <summary>
    /// Tạo đối tượng RxDraftLine. Dòng thuốc đang nhập trong form; giữ dữ liệu chuỗi và tham chiếu dòng cũ, chỉ được chuyển sang ET/lưu SQL khi người dùng lưu đơn.
    /// </summary>
    public RxDraftLine() { }
    /// <summary>
    /// Tạo đối tượng RxDraftLine. Dòng thuốc đang nhập trong form; giữ dữ liệu chuỗi và tham chiếu dòng cũ, chỉ được chuyển sang ET/lưu SQL khi người dùng lưu đơn.
    /// </summary>
    public RxDraftLine(object row)
    {
        Existing = row; MaThuoc = UiRecord.Text(row, "MaThuoc"); LieuDung = UiRecord.Text(row, "LieuDung");
        SoLuong = UiRecord.Text(row, "SoLuong"); SoNgayDung = UiRecord.Text(row, "SoNgayDung"); HuongDan = UiRecord.Text(row, "HuongDan");
    }
}

// Kiểm tra toàn bộ đơn đã lưu trước khi xác nhận và khóa nội dung chỉ định.
/// <summary>
/// Kiểm tra toàn bộ đơn và các dòng thuốc trước khi chuyển sang trạng thái xác nhận, trả lỗi tiếng Việt theo dữ liệu còn thiếu.
/// </summary>
public static class PrescriptionValidation
{
    /// <summary>
    /// Kiểm tra liên kết bệnh nhân/lần khám/bác sĩ/ngày kê và toàn bộ các dòng thuốc trước khi xác nhận; thiếu thuốc hoặc liều/số lượng/ngày/hướng dẫn trả lỗi cụ thể.
    /// </summary>
    public static bool TryValidate(object order, out string error)
    {
        error = "";
        var visit = DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", UiRecord.Get(order, "MaLS"));
        var patientId = DuLieuGiaoDien.PatientId("DON_THUOC", order);
        if (visit is null || string.IsNullOrEmpty(patientId) || DuLieuGiaoDien.Find("BENH_NHAN", patientId) is null
            || DuLieuGiaoDien.Find("NHAN_VIEN", UiRecord.Get(order, "MaBS")) is null || UiRecord.Date(order, "NgayKe") is null)
        { error = "Kiểm tra liên kết lần khám, bệnh nhân, bác sĩ và ngày kê trước khi xác nhận."; return false; }
        var lines = DuLieuGiaoDien.Children("CHI_TIET_DON_THUOC", "MaDT", UiRecord.Get(order, "MaDT"));
        if (lines.Length == 0) { error = "Đơn cần có ít nhất một thuốc trước khi xác nhận."; return false; }
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var drug = DuLieuGiaoDien.Find("THUOC", UiRecord.Get(line, "MaThuoc"));
            if (drug is null || DuLieuGiaoDien.Find("HANG_HOA", UiRecord.Get(drug, "MaHH")) is null
                || string.IsNullOrWhiteSpace(UiRecord.Text(line, "LieuDung")) || string.IsNullOrWhiteSpace(UiRecord.Text(line, "HuongDan"))
                || !QuyTacHienThi.TryPrescriptionNumbers(UiRecord.Text(line, "SoLuong"), UiRecord.Text(line, "SoNgayDung"), out _, out _))
            { error = $"Thuốc {index + 1}: kiểm tra danh mục thuốc, liều dùng, số lượng, số ngày và hướng dẫn."; return false; }
        }
        return true;
    }
}
