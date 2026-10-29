using System.Data;
using ET;
using DAL;

namespace BUS;

/// <summary>
/// Tầng nghiệp vụ quyền nghiệp vụ, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
/// </summary>
public sealed class BUS_QUYENNGHIEPVU
{
    /// <summary>
    /// Đối tượng truy cập dữ liệu quyền nghiệp vụ phục vụ các thao tác của lớp.
    /// </summary>
    private readonly DAL_QUYENNGHIEPVU dal;
    /// <summary>
    /// Kiểm tra quyền và phạm vi chỉnh sửa hồ sơ trước khi ghi dữ liệu. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.KiemTraGhiHoSo</c>.</remarks>
    /// <param name="bang">Giá trị bang dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="giaTri">Giá trị giá trị dùng khi thực hiện thao tác; kiểu IDictionary&lt;string, object?&gt;.</param>
    /// <param name="ma">Mã định danh của bản ghi cần xử lý.</param>
    /// <param name="xoa">Giá trị xóa dùng khi thực hiện thao tác; kiểu bool.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    public void KiemTraGhiHoSo(string bang, IDictionary<string, object?> giaTri, string? ma, bool xoa, NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => { dal.KiemTraGhiHoSo(bang, giaTri, ma, xoa, nguoiDung); return true; });
    /// <summary>
    /// Khởi tạo quyền nghiệp vụ.
    /// </summary>
    /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    public BUS_QUYENNGHIEPVU(IDbTransaction? transaction = null) => dal = new(transaction);
    /// <summary>
    /// Đọc ngữ cảnh nghiệp vụ từ định danh đăng nhập để áp dụng quyền và phạm vi dữ liệu. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayNguCanh</c>.</remarks>
    /// <param name="maNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
    /// <returns>Kết quả kiểu NguCanhNghiepVu dùng cho bước xử lý tiếp theo.</returns>
    public NguCanhNghiepVu LayNguCanh(string maNV) => XuLyLoi.ThucHien(() => dal.LayNguCanh(maNV));
    /// <summary>
    /// Kiểm tra quyền ghi lần khám trong ngữ cảnh nghiệp vụ được cung cấp. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.KiemTraQuyenGhiLanKham</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    public void KiemTraQuyenGhiLanKham(string maLS, NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => { dal.KiemTraQuyenGhiLanKham(maLS, nguoiDung); return true; });
}
