// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS;

/// <summary>
/// Tầng nghiệp vụ chi tiết hóa đơn, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_CHITIETHOADON. Các thao
/// tác chuyển tiếp không bọc XuLyLoi tại lớp này; lỗi từ DAL được truyền nguyên lên nơi gọi.
/// </summary>
public sealed class BUS_CHITIETHOADON
{
    /// <summary>
    /// Đối tượng DAL_CHITIETHOADON thực hiện truy vấn/ghi chi tiết hóa đơn trong giao dịch được truyền vào constructor.
    /// </summary>
    private readonly DAL_CHITIETHOADON data;
    /// <summary>
    /// Khởi tạo DAL_CHITIETHOADON bằng transaction tùy chọn để các thao tác chi tiết hóa đơn dùng cùng giao dịch của nơi gọi.
    /// </summary>
    /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
    public BUS_CHITIETHOADON(IDbTransaction? transaction = null) { data = new(transaction); }
    /// <summary>
    /// Gọi DAL_CHITIETHOADON.LayDanhSach để đọc toàn bộ chi tiết hóa đơn; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng
    /// khi chưa có dữ liệu.
    /// </summary>
    public ET_CHITIETHOADON[] LayDanhSach() => data.LayDanhSach();
    /// <summary>
    /// Gọi DAL_CHITIETHOADON.layTheoMa với ma để tra chi tiết hóa đơn theo khóa; trả ET tương ứng hoặc null nếu không tìm thấy.
    /// </summary>
    /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
    public ET_CHITIETHOADON? LayTheoMa(string ma) => data.layTheoMa(ma);
    /// <summary>
    /// Lấy bảng các khoản thu của invoiceId, gồm nguồn dịch vụ/thuốc đã cấp, số lượng, đơn giá chốt và thông tin lô; Lớp này gọi
    /// trực tiếp DAL_CHITIETHOADON.GetInvoiceDetails.
    /// </summary>
    /// <param name="invoiceId">Mã hóa đơn cần đọc hoặc xử lý.</param>
    public DataTable GetInvoiceDetails(string invoiceId) => data.GetInvoiceDetails(invoiceId);
    /// <summary>
    /// Lấy bảng khoản thu mới của visitId: dịch vụ đã xác nhận và thuốc trên phiếu xuất đã ghi sổ, chưa thuộc chi tiết hóa đơn
    /// nào; gọi trực tiếp DAL_CHITIETHOADON.GetEligibleCharges.
    /// </summary>
    /// <param name="visitId">Mã lần khám; với GetInvoices có thể null để lấy tất cả.</param>
    public DataTable GetEligibleCharges(string visitId) => data.GetEligibleCharges(visitId);
}
