// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS;

/// <summary>
/// Tầng nghiệp vụ chi tiết phiếu xuất, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_CHITIETPHIEUXUAT. Các
/// thao tác chuyển tiếp không bọc XuLyLoi tại lớp này; lỗi từ DAL được truyền nguyên lên nơi gọi.
/// </summary>
public sealed class BUS_CHITIETPHIEUXUAT
{
    /// <summary>
    /// Đối tượng DAL_CHITIETPHIEUXUAT thực hiện truy vấn/ghi chi tiết phiếu xuất trong giao dịch được truyền vào constructor.
    /// </summary>
    private readonly DAL_CHITIETPHIEUXUAT data;
    /// <summary>
    /// Khởi tạo DAL_CHITIETPHIEUXUAT bằng transaction tùy chọn để các thao tác chi tiết phiếu xuất dùng cùng giao dịch của nơi
    /// gọi.
    /// </summary>
    /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
    public BUS_CHITIETPHIEUXUAT(IDbTransaction? transaction = null) { data = new(transaction); }
    /// <summary>
    /// Gọi DAL_CHITIETPHIEUXUAT.LayDanhSach để đọc toàn bộ chi tiết phiếu xuất; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể
    /// rỗng khi chưa có dữ liệu.
    /// </summary>
    public ET_CHITIETPHIEUXUAT[] LayDanhSach() => data.LayDanhSach();
    /// <summary>
    /// Gọi DAL_CHITIETPHIEUXUAT.layTheoMa với ma để tra chi tiết phiếu xuất theo khóa; trả ET tương ứng hoặc null nếu không tìm
    /// thấy.
    /// </summary>
    /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
    public ET_CHITIETPHIEUXUAT? LayTheoMa(string ma) => data.layTheoMa(ma);
    /// <summary>
    /// Gọi DAL_CHITIETPHIEUXUAT.layTheoPhieu với maPX để lấy các dòng xuất của một phiếu, sắp theo mã chi tiết; trả danh sách ET
    /// đã đọc vào bộ nhớ.
    /// </summary>
    /// <param name="maPX">Mã phiếu xuất cần đọc chi tiết.</param>
    public IEnumerable<ET_CHITIETPHIEUXUAT> layTheoPhieu(string maPX) => data.layTheoPhieu(maPX);
}
