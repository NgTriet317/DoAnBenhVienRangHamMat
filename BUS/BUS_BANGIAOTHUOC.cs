using System.Data;
using DAL;
using ET;
namespace BUS;

/// <summary>
/// Tầng nghiệp vụ bàn giao thuốc, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
/// </summary>
public sealed class BUS_BANGIAOTHUOC
{
    /// <summary>
    /// Đối tượng truy cập dữ liệu bàn giao thuốc phục vụ các thao tác của lớp.
    /// </summary>
    private readonly DAL_BANGIAOTHUOC dal;
    /// <summary>
    /// Giao dịch được giữ để các thao tác liên quan dùng cùng một phạm vi commit/rollback.
    /// </summary>
    private readonly IDbTransaction? callerTransaction;
    /// <summary>
    /// Khởi tạo bàn giao thuốc.
    /// </summary>
    /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    public BUS_BANGIAOTHUOC(IDbTransaction? transaction=null){dal=new(transaction);callerTransaction=transaction;}
    /// <summary>
    /// Đọc thông tin bàn giao tương ứng với đơn thuốc được chỉ định. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayTheoDon</c>.</remarks>
    /// <param name="maDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Đối tượng kiểu ET_BANGIAOTHUOC được tìm hoặc tính toán; có thể null theo nhánh xử lý trong hàm.</returns>
    public ET_BANGIAOTHUOC? LayTheoDon(string maDT,NguCanhNghiepVu nguoiDung)=>XuLyLoi.ThucHien(()=>dal.LayTheoDon(maDT,nguoiDung));
    /// <summary>
    /// Đọc danh sách hàng đợi theo phạm vi dữ liệu và quyền của người dùng. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>dal.LayHangDoi</c>.</remarks>
    /// <param name="filter">Giá trị bộ lọc dùng khi thực hiện thao tác; kiểu BoLocBanGiao.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu HangDoiBanGiao dùng cho bước xử lý tiếp theo.</returns>
    public HangDoiBanGiao LayHangDoi(BoLocBanGiao filter,NguCanhNghiepVu user)=>dal.LayHangDoi(filter,user);
    /// <summary>
    /// Đọc dữ liệu chi tiết của mục được chọn để hiển thị hoặc tiếp tục xử lý. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>dal.LayChiTiet</c>.</remarks>
    /// <param name="maDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu ChiTietBanGiao dùng cho bước xử lý tiếp theo.</returns>
    public ChiTietBanGiao LayChiTiet(string maDT,NguCanhNghiepVu user)=>dal.LayChiTiet(maDT,user);
    /// <summary>
    /// Kiểm tra chứng từ và nội dung đối chiếu trước khi xác nhận bàn giao thuốc. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>dal.BatDauGiaoDich</c>, <c>tx.Commit</c>, <c>tx.Rollback</c>.</remarks>
    /// <param name="maDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu KetQuaBanGiao dùng cho bước xử lý tiếp theo.</returns>
    public KetQuaBanGiao XacNhanGiao(string maDT,NguCanhNghiepVu user)
    {
        if(callerTransaction is not null || System.Transactions.Transaction.Current is not null)
            throw new LoiKhamException("BanGiao","Bàn giao phải tự quản lý giao dịch và chỉ trả kết quả sau commit.");
        try
        {
            using var tx=dal.BatDauGiaoDich();using var connection=tx.Connection!;
            try{var result=new DAL_BANGIAOTHUOC(tx).XacNhanGiao(maDT,user);tx.Commit();return result;}
            catch{try{if(tx.Connection is not null)tx.Rollback();}catch{}throw;}
        }
        catch(System.Data.Common.DbException ex){throw new LoiKhamException("BanGiao","Chưa xác nhận được kết quả giao. Thử lại đúng đơn để đối chiếu; không xuất thuốc lần nữa.",ex);}
    }
}
