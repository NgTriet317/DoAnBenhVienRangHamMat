using System.Data;
using DAL;
using ET;
namespace BUS;

/// <summary>
/// Tầng nghiệp vụ thanh toán trọn gói, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
/// </summary>
public sealed class BUS_THANHTOANTRONGOI
{
    /// <summary>
    /// Đối tượng truy cập dữ liệu thanh toán trọn gói phục vụ các thao tác của lớp.
    /// </summary>
    private readonly DAL_THANHTOANTRONGOI dal;
    /// <summary>
    /// Giao dịch được giữ để các thao tác liên quan dùng cùng một phạm vi commit/rollback.
    /// </summary>
    private readonly IDbTransaction? callerTransaction;
    /// <summary>
    /// Khởi tạo thanh toán trọn gói.
    /// </summary>
    /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    public BUS_THANHTOANTRONGOI(IDbTransaction? transaction = null)
    { dal = new(transaction); callerTransaction = transaction; }
    /// <summary>
    /// Đọc nguồn phí dịch vụ, thuốc và tồn lô để lập bảng tính trước khi thu; trả các điều kiện cho phép thanh toán. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>dal.LapBangTinh</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="maKhoCapThuoc">Mã kho cấp thuốc xác định đối tượng cần xử lý.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BangTinhPhi dùng cho bước xử lý tiếp theo.</returns>
    public BangTinhPhi LapBangTinh(string maLS, string? maKhoCapThuoc, NguCanhNghiepVu nguoiDung)
        => dal.LapBangTinh(maLS, maKhoCapThuoc, nguoiDung);
    /// <summary>
    /// Đọc hóa đơn và các dòng nguồn đã chốt để tạo nội dung bản in. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>dal.LayBanInHoaDon</c>.</remarks>
    /// <param name="maHD">Mã hóa đơn xác định đối tượng cần xử lý.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanInHoaDon dùng cho bước xử lý tiếp theo.</returns>
    public BanInHoaDon LayBanInHoaDon(string maHD,NguCanhNghiepVu nguoiDung)
    {
        if(callerTransaction is not null || System.Transactions.Transaction.Current is not null)
            throw new LoiKhamException("MaHD","Nguồn in phải được đọc sau commit từ kết nối riêng.");
        try{return dal.LayBanInHoaDon(maHD,nguoiDung);}
        catch(System.Data.Common.DbException ex){throw new LoiKhamException("MaHD","Không đọc được hóa đơn đã lưu. Tải lại khi kết nối ổn định; không thu lại.",ex);}
    }
    /// <summary>
    /// Điều phối toàn bộ nghiệp vụ thu tiền, xác nhận hóa đơn và xuất thuốc trong giao dịch do đầu mối quản lý. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>dal.BatDauGiaoDich</c>, <c>tx.Commit</c>, <c>tx.Rollback</c>.</remarks>
    /// <param name="yeuCau">Giá trị yêu cầu dùng khi thực hiện thao tác; kiểu YeuCauThanhToan.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu KetQuaThanhToan dùng cho bước xử lý tiếp theo.</returns>
    public KetQuaThanhToan ThanhToan(YeuCauThanhToan yeuCau, NguCanhNghiepVu nguoiDung)
    {
        if (callerTransaction is not null || System.Transactions.Transaction.Current is not null)
            throw new LoiKhamException("ThanhToan", "Thu trọn gói phải tự quản lý giao dịch để chỉ trả kết quả sau commit.");
        try
        {
            using var tx = dal.BatDauGiaoDich(); using var connection = tx.Connection!;
            try
            {
                var result = new DAL_THANHTOANTRONGOI(tx).ThanhToan(yeuCau, nguoiDung);
                tx.Commit(); return result;
            }
            catch
            {
                try { if (tx.Connection is not null) tx.Rollback(); } catch { /* Mất kết nối: thử lại cùng mã. */ }
                throw;
            }
        }
        catch (System.Data.Common.DbException ex)
        {
            throw new LoiKhamException("ThanhToan", "Chưa xác nhận được kết quả thanh toán. Thử lại cùng yêu cầu để đối chiếu kết quả đã lưu; không lập yêu cầu khác.", ex);
        }
    }
}
