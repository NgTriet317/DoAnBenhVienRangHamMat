// PHẦN AI ĐÃ THỰC HIỆN: AI đã bổ sung chú thích tiếng Việt cho PatientWorkspaceModels.cs: Mô hình một dòng lịch sử khám trong hồ sơ bệnh nhân, tra bác sĩ và nguồn tiếp nhận từ các bản ghi liên kết. Giải thích được đặt cạnh khai báo/hàm tương ứng.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
namespace RangHamMat.Controls;

// Mô hình chỉ để hiển thị; nguồn đến khám thuộc lượt tiếp nhận, không phải thuộc tính bệnh nhân.
/// <summary>
/// Mô hình một dòng lịch sử khám trong hồ sơ bệnh nhân, tra bác sĩ và nguồn tiếp nhận từ các bản ghi liên kết.
/// </summary>
public sealed class PatientVisitRow
{
    /// <summary>
    /// Bản ghi nguồn đã đọc để mô hình tra các trường/liên kết; không phải một bản SQL mới được tạo tự động.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Tạo đối tượng PatientVisitRow. Mô hình một dòng lịch sử khám trong hồ sơ bệnh nhân, tra bác sĩ và nguồn tiếp nhận từ các bản ghi liên kết.
    /// </summary>
    public PatientVisitRow(object record) => Record = record;
    /// <summary>
    /// Mã khóa của bản ghi dùng để giữ lựa chọn và mở đúng chi tiết.
    /// </summary>
    public string Id => UiRecord.Text(Record, "MaLS");
    /// <summary>
    /// Ngày/giờ của dòng nghiệp vụ được định dạng để hiển thị; nội dung mặc định cho biết chưa có thời gian.
    /// </summary>
    public string When => UiRecord.Date(Record, "ThoiGianKham")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa ghi giờ khám";
    /// <summary>
    /// Nhãn bác sĩ hoặc nhân viên thực hiện được tra từ mã liên kết trong bản ghi nguồn.
    /// </summary>
    public string Doctor => DuLieuGiaoDien.Lookup("MaBS", UiRecord.Get(Record, "MaBS"));
    /// <summary>
    /// Chẩn đoán của lần khám nguồn để đọc cùng hồ sơ hoặc đơn thuốc.
    /// </summary>
    public string Diagnosis => UiRecord.Text(Record, "ChanDoan");
    /// <summary>
    /// Trạng thái hiển thị của bản ghi; với bước kế hoạch có thể được suy ra thêm từ lượng thực hiện đã xác nhận.
    /// </summary>
    public string Status => UiRecord.Text(Record, "TrangThai");
    /// <summary>
    /// Thông tin nguồn tiếp nhận của dòng lịch sử khám, giúp phân biệt lượt có lịch hẹn và lượt trực tiếp.
    /// </summary>
    public string Arrival
    {
        get
        {
            var receipt = DuLieuGiaoDien.Find("PHIEU_KHAM_BENH", UiRecord.Get(Record, "MaPhieu"));
            return receipt is null ? "Thiếu phiếu tiếp nhận" : string.IsNullOrWhiteSpace(UiRecord.Text(receipt, "MaLich")) ? "Trực tiếp" : "Theo lịch hẹn";
        }
    }
}
