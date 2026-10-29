using System.Data;
using DAL;
using ET;

namespace BUS;

/// <summary>
/// Tầng nghiệp vụ tiếp nhận, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
/// </summary>
public sealed class BUS_TIEPNHAN(IDbTransaction? transaction=null)
{
    /// <summary>
    /// Đối tượng truy cập dữ liệu tiếp nhận phục vụ các thao tác của lớp.
    /// </summary>
    private readonly DAL_TIEPNHAN dal=new(transaction);
    /// <summary>
    /// Đọc danh sách bác sĩ theo tiêu chí và phạm vi áp dụng tại nơi gọi. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayBacSi</c>.</remarks>
    /// <param name="maDV">Mã dịch vụ xác định đối tượng cần xử lý.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Tập dữ liệu kiểu IReadOnlyList&lt;BacSiTiepNhan&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
    public IReadOnlyList<BacSiTiepNhan> LayBacSi(string maDV,NguCanhNghiepVu nguoiDung) => XuLyLoi.ThucHien(()=>dal.LayBacSi(maDV,nguoiDung));
    /// <summary>
    /// Kiểm tra đầu vào và quyền tiếp nhận trước khi tạo hoặc liên kết hồ sơ, phiếu khám và người phụ trách. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.TiepNhan</c>.</remarks>
    /// <param name="yeuCau">Giá trị yêu cầu dùng khi thực hiện thao tác; kiểu YeuCauTiepNhan.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu KetQuaTiepNhan dùng cho bước xử lý tiếp theo.</returns>
    public KetQuaTiepNhan TiepNhan(YeuCauTiepNhan yeuCau,NguCanhNghiepVu nguoiDung) => XuLyLoi.ThucHien(()=>dal.TiepNhan(yeuCau,nguoiDung));
}
