namespace RangHamMat.Controls;

/// <summary>Thông tin tại thời điểm thanh lý; không đổi theo tên danh mục hiện tại.</summary>
public sealed class DongThanhLy(object record)
{
    /// <summary>
    /// Bản ghi thiết bị nguồn có các trường lưu tại thời điểm thanh lý.
    /// </summary>
    public object Record { get; } = record;
    /// <summary>
    /// Mã thiết bị dùng để chọn lại đúng dòng sau khi lọc hoặc tải danh sách.
    /// </summary>
    public string MaTB => UiRecord.Text(Record, "MaTB");
    /// <summary>
    /// Số serial của thiết bị được tra cứu trong danh sách thanh lý.
    /// </summary>
    public string SoSerial => UiRecord.Text(Record, "SoSerial");
    /// <summary>
    /// Tên thiết bị đã lưu khi thanh lý để giữ nguyên thông tin lịch sử.
    /// </summary>
    public string TenThietBi => UiRecord.Text(Record, "TenThietBiThanhLy");
    /// <summary>
    /// Tên phòng đã lưu tại thời điểm thanh lý thiết bị.
    /// </summary>
    public string TenPhong => UiRecord.Text(Record, "TenPhongThanhLy");
    /// <summary>
    /// Tên nhân viên phụ trách được lưu cùng thông tin thanh lý.
    /// </summary>
    public string NhanVien => UiRecord.Text(Record, "TenNhanVienThanhLy");
    /// <summary>
    /// Mã nhân viên phụ trách thanh lý để đối chiếu với tên đã lưu.
    /// </summary>
    public string MaNV => UiRecord.Text(Record, "MaNVThanhLy");
    /// <summary>
    /// Ngày thanh lý của bản ghi; dùng DateTime.MinValue nếu nguồn không có ngày.
    /// </summary>
    public DateTime NgayThanhLy => UiRecord.Date(Record, "NgayThanhLy") ?? DateTime.MinValue;
    /// <summary>
    /// Lý do thanh lý đã được ghi nhận cho thiết bị.
    /// </summary>
    public string LyDo => UiRecord.Text(Record, "LyDoThanhLy");
    /// <summary>
    /// Giá trị thu hồi đã lưu khi thanh lý, dùng để tính tổng danh sách đang hiển thị.
    /// </summary>
    public decimal GiaTri => Convert.ToDecimal(UiRecord.Get(Record, "GiaTriThanhLy"));
    /// <summary>
    /// Chuỗi ghép mã, serial và thông tin thanh lý để tìm kiếm trên danh sách.
    /// </summary>
    public string TuKhoa => string.Join(" ", MaTB, SoSerial, TenThietBi, TenPhong, NhanVien, MaNV, LyDo);
}
