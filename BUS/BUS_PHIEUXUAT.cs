// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS;

/// <summary>
/// Tầng nghiệp vụ phiếu xuất và cấp thuốc, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_PHIEUXUAT. Các thao
/// tác chuyển tiếp không bọc XuLyLoi tại lớp này; lỗi từ DAL được truyền nguyên lên nơi gọi.
/// </summary>
public sealed class BUS_PHIEUXUAT
{
    /// <summary>
    /// Tên gọi tiếng Việt của DispensePrescription; chuyển nguyên mã đơn, kho và nhân viên, trả mã phiếu xuất sau khi cấp đủ đơn.
    /// </summary>
    /// <param name="maDT">Mã đơn thuốc cần cấp.</param>
    /// <param name="maKho">Mã kho cấp thuốc.</param>
    /// <param name="maNV">Mã nhân viên thực hiện.</param>
    public string CapDonThuoc(string maDT, string maKho, string maNV) => DispensePrescription(maDT, maKho, maNV);
    /// <summary>
    /// Đối tượng DAL_PHIEUXUAT thực hiện truy vấn/ghi phiếu xuất và cấp thuốc trong giao dịch được truyền vào constructor.
    /// </summary>
    private readonly DAL_PHIEUXUAT data;
    /// <summary>
    /// Khởi tạo DAL_PHIEUXUAT bằng transaction tùy chọn để các thao tác phiếu xuất và cấp thuốc dùng cùng giao dịch của nơi gọi.
    /// </summary>
    /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
    public BUS_PHIEUXUAT(IDbTransaction? transaction = null) { data = new(transaction); }
    /// <summary>
    /// Gọi DAL_PHIEUXUAT.LayDanhSach để đọc toàn bộ phiếu xuất và cấp thuốc; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể
    /// rỗng khi chưa có dữ liệu.
    /// </summary>
    public ET_PHIEUXUAT[] LayDanhSach() => data.LayDanhSach();
    /// <summary>
    /// Gọi DAL_PHIEUXUAT.layTheoMa với ma để tra phiếu xuất và cấp thuốc theo khóa; trả ET tương ứng hoặc null nếu không tìm
    /// thấy.
    /// </summary>
    /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
    public ET_PHIEUXUAT? LayTheoMa(string ma) => data.layTheoMa(ma);
    /// <summary>
    /// Lấy bảng lô thuốc trong warehouseId phục vụ prescriptionId, có tồn dương, còn sử dụng, đã nhập và chưa hết hạn; tồn bằng
    /// nhập trừ xuất đã ghi sổ, sắp theo ngày nhập rồi hạn dùng. Gọi trực tiếp DAL_PHIEUXUAT.GetStock.
    /// </summary>
    /// <param name="warehouseId">Mã kho xem tồn hoặc cấp thuốc.</param>
    /// <param name="prescriptionId">Mã đơn thuốc cần xem tồn hoặc cấp đủ.</param>
    public DataTable GetStock(string warehouseId, string prescriptionId) => data.GetStock(warehouseId, prescriptionId);
    /// <summary>
    /// Chuyển prescriptionId, warehouseId và employeeId trực tiếp đến DAL_PHIEUXUAT.DispensePrescription; DAL kiểm tra nhân
    /// viên/kho, đơn đã xác nhận, ngày kê đã đến và chưa cấp, khóa nguồn tồn rồi phân bổ đủ từng thuốc theo thứ tự lô. Thiếu
    /// thuốc phát lỗi trước khi tạo phiếu; đủ thuốc tạo chi tiết và ghi sổ phiếu xuất trong giao dịch, trả mã phiếu.
    /// </summary>
    /// <param name="prescriptionId">Mã đơn thuốc cần xem tồn hoặc cấp đủ.</param>
    /// <param name="warehouseId">Mã kho xem tồn hoặc cấp thuốc.</param>
    /// <param name="employeeId">Mã nhân viên thực hiện; DAL yêu cầu đang làm việc.</param>
    public string DispensePrescription(string prescriptionId, string warehouseId, string employeeId) => data.DispensePrescription(prescriptionId, warehouseId, employeeId);
}
