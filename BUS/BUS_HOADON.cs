// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS;

/// <summary>
/// Đầu mối nghiệp vụ hóa đơn khám chữa bệnh: kiểm tra mã/giảm giá và phối hợp DAL hóa đơn, BUS chi tiết, thanh toán và xuất thuốc
/// dùng chung giao dịch. Các thao tác chuyển tiếp không bọc XuLyLoi tại lớp này; lỗi từ DAL được truyền nguyên lên nơi gọi.
/// </summary>
public sealed class BUS_HOADON
{
    /// <summary>
    /// Tên gọi tiếng Việt của CreateInvoice; chuyển nguyên mã lần khám, mã nhân viên và giảm giá, trả mã hóa đơn mới.
    /// </summary>
    /// <param name="maLS">Mã lần khám lập hóa đơn.</param>
    /// <param name="maNV">Mã nhân viên thực hiện.</param>
    /// <param name="giamGia">Số tiền giảm giá của hóa đơn, mặc định 0.</param>
    public string LapHoaDon(string maLS, string maNV, decimal giamGia = 0) => CreateInvoice(maLS, maNV, giamGia);
    /// <summary>
    /// Tên gọi tiếng Việt của ConfirmInvoice; chuyển mã hóa đơn vào cùng luồng kiểm tra và xác nhận hóa đơn nháp.
    /// </summary>
    /// <param name="maHD">Mã hóa đơn cần xử lý.</param>
    public void XacNhanHoaDon(string maHD) => ConfirmInvoice(maHD);
    /// <summary>
    /// Tên gọi tương thích của CancelInvoice: hủy hóa đơn khám chữa bệnh ở trạng thái nháp, không phải hóa đơn nhập hàng; chuyển
    /// nguyên mã hóa đơn.
    /// </summary>
    /// <param name="maHD">Mã hóa đơn cần xử lý.</param>
    public void HuyHoaDonNhap(string maHD) => CancelInvoice(maHD);
    /// <summary>
    /// Đối tượng DAL_HOADON thực hiện truy vấn/ghi hóa đơn khám chữa bệnh trong giao dịch được truyền vào constructor.
    /// </summary>
    private readonly DAL_HOADON data;
    /// <summary>
    /// BUS chi tiết hóa đơn dùng đọc các dòng hóa đơn và nguồn khoản thu mới.
    /// </summary>
    private readonly BUS_CHITIETHOADON details;
    /// <summary>
    /// BUS thanh toán dùng đọc lịch sử và ghi nhận thu/hoàn tiền.
    /// </summary>
    private readonly BUS_THANHTOAN payments;
    /// <summary>
    /// BUS phiếu xuất dùng xem lô tồn và cấp đủ đơn thuốc.
    /// </summary>
    private readonly BUS_PHIEUXUAT exports;
    /// <summary>
    /// Khởi tạo DAL hóa đơn và các BUS chi tiết, thanh toán, phiếu xuất bằng cùng transaction để các thao tác có thể tham gia một
    /// giao dịch.
    /// </summary>
    /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
    public BUS_HOADON(IDbTransaction? transaction = null)
    { data = new(transaction); details = new(transaction); payments = new(transaction); exports = new(transaction); }
    /// <summary>
    /// Gọi DAL_HOADON.LayDanhSach để đọc toàn bộ hóa đơn khám chữa bệnh; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng
    /// khi chưa có dữ liệu.
    /// </summary>
    public ET_HOADON[] LayDanhSach() => data.LayDanhSach();
    /// <summary>
    /// Gọi DAL_HOADON.layTheoMa với ma để tra hóa đơn khám chữa bệnh theo khóa; trả ET tương ứng hoặc null nếu không tìm thấy.
    /// </summary>
    /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
    public ET_HOADON? LayTheoMa(string ma) => data.layTheoMa(ma);
    /// <summary>
    /// Gọi DAL_HOADON.GetInvoices để lấy bảng hóa đơn kèm bệnh nhân, người lập, tiền đã thu ròng và còn phải thu; visitId null
    /// lấy mọi lần khám, có giá trị chỉ lấy lần khám đó.
    /// </summary>
    /// <param name="visitId">Mã lần khám; với GetInvoices có thể null để lấy tất cả.</param>
    public DataTable GetInvoices(string? visitId = null) => data.GetInvoices(visitId);
    /// <summary>
    /// Lấy bảng các khoản thu của invoiceId, gồm nguồn dịch vụ/thuốc đã cấp, số lượng, đơn giá chốt và thông tin lô; BUS_HOADON
    /// chuyển qua BUS_CHITIETHOADON, lớp chi tiết gọi DAL_CHITIETHOADON.GetInvoiceDetails.
    /// </summary>
    /// <param name="invoiceId">Mã hóa đơn cần đọc hoặc xử lý.</param>
    public DataTable GetInvoiceDetails(string invoiceId) => details.GetInvoiceDetails(invoiceId);
    /// <summary>
    /// Lấy bảng lịch sử thu/hoàn của invoiceId kèm tên người thực hiện, theo thời gian và mã giảm dần; BUS_HOADON chuyển qua
    /// BUS_THANHTOAN, lớp thanh toán gọi DAL_THANHTOAN.GetPayments.
    /// </summary>
    /// <param name="invoiceId">Mã hóa đơn cần đọc hoặc xử lý.</param>
    public DataTable GetPayments(string invoiceId) => payments.GetPayments(invoiceId);
    /// <summary>
    /// Lấy bảng khoản thu mới của visitId: dịch vụ đã xác nhận và thuốc trên phiếu xuất đã ghi sổ, chưa thuộc chi tiết hóa đơn
    /// nào; chuyển qua BUS_CHITIETHOADON.GetEligibleCharges đến DAL_CHITIETHOADON.
    /// </summary>
    /// <param name="visitId">Mã lần khám; với GetInvoices có thể null để lấy tất cả.</param>
    public DataTable GetEligibleCharges(string visitId) => details.GetEligibleCharges(visitId);
    /// <summary>
    /// Lấy bảng lô thuốc trong warehouseId phục vụ prescriptionId, có tồn dương, còn sử dụng, đã nhập và chưa hết hạn; tồn bằng
    /// nhập trừ xuất đã ghi sổ, sắp theo ngày nhập rồi hạn dùng. Chuyển qua BUS_PHIEUXUAT.GetStock đến DAL_PHIEUXUAT.GetStock.
    /// </summary>
    /// <param name="warehouseId">Mã kho xem tồn hoặc cấp thuốc.</param>
    /// <param name="prescriptionId">Mã đơn thuốc cần xem tồn hoặc cấp đủ.</param>
    public DataTable GetStock(string warehouseId, string prescriptionId) => exports.GetStock(warehouseId, prescriptionId);
    /// <summary>
    /// Kiểm tra mã lần khám/nhân viên và giảm giá không âm, tối đa 2 chữ số lẻ; gọi DAL_HOADON.CreateInvoice để kiểm tra nhân
    /// viên/lần khám, lấy khoản thu mới, tạo hóa đơn nháp cùng chi tiết và áp dụng giảm giá trong giao dịch; trả mã hóa đơn.
    /// </summary>
    /// <param name="visitId">Mã lần khám; với GetInvoices có thể null để lấy tất cả.</param>
    /// <param name="employeeId">Mã nhân viên thực hiện; DAL yêu cầu đang làm việc.</param>
    /// <param name="discount">Số tiền giảm giá, mặc định 0, không âm và tối đa 2 chữ số lẻ.</param>
    public string CreateInvoice(string visitId, string employeeId, decimal discount = 0)
    {
        RequireIds(visitId, employeeId);
        if (discount < 0 || decimal.Round(discount, 2) != discount) throw new InvalidOperationException("Giảm giá phải không âm và có tối đa 2 chữ số thập phân.");
        return data.CreateInvoice(visitId, employeeId, discount);
    }
    /// <summary>
    /// Kiểm tra invoiceId rồi gọi DAL_HOADON.ConfirmInvoice; DAL khóa hóa đơn, chỉ cho xác nhận bản nháp và ghi thời điểm xác
    /// nhận. Không trả dữ liệu; lỗi nghiệp vụ được truyền lên.
    /// </summary>
    /// <param name="invoiceId">Mã hóa đơn cần đọc hoặc xử lý.</param>
    public void ConfirmInvoice(string invoiceId) { RequireIds(invoiceId); data.ConfirmInvoice(invoiceId); }
    /// <summary>
    /// Kiểm tra invoiceId rồi gọi DAL_HOADON.CancelInvoice; DAL chỉ hủy bản nháp, đặt giảm giá về 0, gỡ các chi tiết để giải
    /// phóng nguồn khoản thu và đổi trạng thái Hủy. Không trả dữ liệu.
    /// </summary>
    /// <param name="invoiceId">Mã hóa đơn cần đọc hoặc xử lý.</param>
    public void CancelInvoice(string invoiceId) { RequireIds(invoiceId); data.CancelInvoice(invoiceId); }
    /// <summary>
    /// Chuyển invoiceId, employeeId, amount, type, method và reason qua BUS_THANHTOAN.RecordPayment đến
    /// DAL_THANHTOAN.RecordPayment; DAL kiểm tra nhân viên, hóa đơn đã xác nhận, số tiền dương/tối đa 2 chữ số lẻ, loại Thu/Hoàn
    /// và phương thức. Hoàn bắt buộc lý do; thu không vượt dư nợ, hoàn không vượt tiền thu ròng; trả mã thanh toán mới. BUS kiểm
    /// tra mã đầu vào bằng RequireIds trước khi chuyển tiếp.
    /// </summary>
    /// <param name="invoiceId">Mã hóa đơn cần đọc hoặc xử lý.</param>
    /// <param name="employeeId">Mã nhân viên thực hiện; DAL yêu cầu đang làm việc.</param>
    /// <param name="amount">Số tiền dương của giao dịch thu/hoàn, tối đa 2 chữ số lẻ.</param>
    /// <param name="type">Loại giao dịch Thu hoặc Hoàn.</param>
    /// <param name="method">Phương thức thanh toán không trống, dài tối đa 50 ký tự.</param>
    /// <param name="reason">Lý do giao dịch; bắt buộc khi hoàn tiền.</param>
    public string RecordPayment(string invoiceId, string employeeId, decimal amount, string type, string method, string? reason = null)
    { RequireIds(invoiceId, employeeId); return payments.RecordPayment(invoiceId, employeeId, amount, type, method, reason); }
    /// <summary>
    /// Chuyển prescriptionId, warehouseId và employeeId qua BUS_PHIEUXUAT.DispensePrescription đến
    /// DAL_PHIEUXUAT.DispensePrescription; DAL kiểm tra nhân viên/kho, đơn đã xác nhận, ngày kê đã đến và chưa cấp, khóa nguồn
    /// tồn rồi phân bổ đủ từng thuốc theo thứ tự lô. Thiếu thuốc phát lỗi trước khi tạo phiếu; đủ thuốc tạo chi tiết và ghi sổ
    /// phiếu xuất trong giao dịch, trả mã phiếu. BUS kiểm tra mã đầu vào bằng RequireIds trước khi chuyển tiếp.
    /// </summary>
    /// <param name="prescriptionId">Mã đơn thuốc cần xem tồn hoặc cấp đủ.</param>
    /// <param name="warehouseId">Mã kho xem tồn hoặc cấp thuốc.</param>
    /// <param name="employeeId">Mã nhân viên thực hiện; DAL yêu cầu đang làm việc.</param>
    public string DispensePrescription(string prescriptionId, string warehouseId, string employeeId)
    { RequireIds(prescriptionId, warehouseId, employeeId); return exports.DispensePrescription(prescriptionId, warehouseId, employeeId); }
    /// <summary>
    /// Duyệt các mã ids trước thao tác tài chính/cấp thuốc; mã trống, chỉ có khoảng trắng hoặc dài hơn 50 ký tự gây
    /// InvalidOperationException ngay tại BUS, không gọi DAL.
    /// </summary>
    /// <param name="ids">Các mã cần kiểm tra trước khi xử lý.</param>
    private static void RequireIds(params string[] ids)
    { if (ids.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 50)) throw new InvalidOperationException("Chọn đầy đủ chứng từ, nhân viên và liên kết cần thiết."); }
}
