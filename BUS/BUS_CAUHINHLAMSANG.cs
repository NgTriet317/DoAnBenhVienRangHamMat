using System.Data;
using DAL;
using ET;

namespace BUS;

/// <summary>
/// Tầng nghiệp vụ cấu hình lâm sàng, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu dùng cho thao tác tổng quát này.</typeparam>
public abstract class BUS_CAUHINHLAMSANG<T>(DAL_CAUHINHLAMSANG<T> dal) where T:class,new()
{
    /// <summary>
    /// Đọc danh sách cấu hình theo điều kiện được truyền vào. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayDanhSach</c>.</remarks>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Tập dữ liệu kiểu IReadOnlyList&lt;T&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
    public IReadOnlyList<T> LayDanhSach(NguCanhNghiepVu nguoiDung) => XuLyLoi.ThucHien(()=>dal.LayDanhSach(nguoiDung));
    /// <summary>
    /// Kiểm tra và lưu bản cấu hình nháp cho nghiệp vụ lâm sàng. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LuuNhap</c>.</remarks>
    /// <param name="duLieu">Giá trị T được truyền vào để đọc, kiểm tra hoặc cập nhật trong thao tác này.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <param name="banGoc">Bản dữ liệu trước khi chỉnh sửa để đối chiếu với giá trị hiện tại.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool LuuNhap(T duLieu,NguCanhNghiepVu nguoiDung,T? banGoc=null) => XuLyLoi.ThucHien(()=>dal.LuuNhap(duLieu,nguoiDung,banGoc));
}

/// <summary>
/// Tầng nghiệp vụ nhóm thuốc kê đơn, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
/// </summary>
public sealed class BUS_NHOMTHUOCKEDON(IDbTransaction? transaction=null) : BUS_CAUHINHLAMSANG<ET_NHOMTHUOCKEDON>(new DAL_NHOMTHUOCKEDON(transaction));
/// <summary>
/// Tầng nghiệp vụ cấu hình thuốc kê đơn, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
/// </summary>
public sealed class BUS_CAUHINHTHUOCKEDON(IDbTransaction? transaction=null) : BUS_CAUHINHLAMSANG<ET_CAUHINHTHUOCKEDON>(new DAL_CAUHINHTHUOCKEDON(transaction));
