// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS;

/// <summary>
/// Tầng nghiệp vụ giao dịch thu/hoàn tiền, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_THANHTOAN. Các thao
/// tác chuyển tiếp không bọc XuLyLoi tại lớp này; lỗi từ DAL được truyền nguyên lên nơi gọi.
/// </summary>
public sealed class BUS_THANHTOAN
{
    /// <summary>
    /// Tên gọi tiếng Việt của RecordPayment; chuyển nguyên thông tin thu/hoàn và trả mã thanh toán do DAL tạo.
    /// </summary>
    /// <param name="maHD">Mã hóa đơn cần xử lý.</param>
    /// <param name="maNV">Mã nhân viên thực hiện.</param>
    /// <param name="soTien">Số tiền của giao dịch thu/hoàn.</param>
    /// <param name="loaiGiaoDich">Loại giao dịch Thu hoặc Hoàn.</param>
    /// <param name="phuongThuc">Phương thức thanh toán.</param>
    /// <param name="lyDo">Lý do giao dịch; bắt buộc khi hoàn tiền.</param>
    public string GhiNhanThanhToan(string maHD, string maNV, decimal soTien, string loaiGiaoDich, string phuongThuc, string? lyDo = null)
        => RecordPayment(maHD, maNV, soTien, loaiGiaoDich, phuongThuc, lyDo);
    /// <summary>
    /// Đối tượng DAL_THANHTOAN thực hiện truy vấn/ghi giao dịch thu/hoàn tiền trong giao dịch được truyền vào constructor.
    /// </summary>
    private readonly DAL_THANHTOAN data;
    /// <summary>
    /// Khởi tạo DAL_THANHTOAN bằng transaction tùy chọn để các thao tác giao dịch thu/hoàn tiền dùng cùng giao dịch của nơi gọi.
    /// </summary>
    /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
    public BUS_THANHTOAN(IDbTransaction? transaction = null) { data = new(transaction); }
    /// <summary>
    /// Gọi DAL_THANHTOAN.LayDanhSach để đọc toàn bộ giao dịch thu/hoàn tiền; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể
    /// rỗng khi chưa có dữ liệu.
    /// </summary>
    public ET_THANHTOAN[] LayDanhSach() => data.LayDanhSach();
    /// <summary>
    /// Gọi DAL_THANHTOAN.layTheoMa với ma để tra giao dịch thu/hoàn tiền theo khóa; trả ET tương ứng hoặc null nếu không tìm
    /// thấy.
    /// </summary>
    /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
    public ET_THANHTOAN? LayTheoMa(string ma) => data.layTheoMa(ma);
    /// <summary>
    /// Lấy bảng lịch sử thu/hoàn của invoiceId kèm tên người thực hiện, theo thời gian và mã giảm dần; Lớp này gọi trực tiếp
    /// DAL_THANHTOAN.GetPayments.
    /// </summary>
    /// <param name="invoiceId">Mã hóa đơn cần đọc hoặc xử lý.</param>
    public DataTable GetPayments(string invoiceId) => data.GetPayments(invoiceId);
    /// <summary>
    /// Chuyển invoiceId, employeeId, amount, type, method và reason trực tiếp đến DAL_THANHTOAN.RecordPayment; DAL kiểm tra nhân
    /// viên, hóa đơn đã xác nhận, số tiền dương/tối đa 2 chữ số lẻ, loại Thu/Hoàn và phương thức. Hoàn bắt buộc lý do; thu không
    /// vượt dư nợ, hoàn không vượt tiền thu ròng; trả mã thanh toán mới.
    /// </summary>
    /// <param name="invoiceId">Mã hóa đơn cần đọc hoặc xử lý.</param>
    /// <param name="employeeId">Mã nhân viên thực hiện; DAL yêu cầu đang làm việc.</param>
    /// <param name="amount">Số tiền dương của giao dịch thu/hoàn, tối đa 2 chữ số lẻ.</param>
    /// <param name="type">Loại giao dịch Thu hoặc Hoàn.</param>
    /// <param name="method">Phương thức thanh toán không trống, dài tối đa 50 ký tự.</param>
    /// <param name="reason">Lý do giao dịch; bắt buộc khi hoàn tiền.</param>
    public string RecordPayment(string invoiceId, string employeeId, decimal amount, string type, string method, string? reason = null) => data.RecordPayment(invoiceId, employeeId, amount, type, method, reason);
}
