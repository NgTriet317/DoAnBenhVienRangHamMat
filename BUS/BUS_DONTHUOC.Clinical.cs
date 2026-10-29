using ET;
namespace BUS;
/// <summary>
/// Tầng nghiệp vụ đơn thuốc, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
/// </summary>
public partial class BUS_DONTHUOC
{
    /// <summary>
    /// Tra thuốc theo bộ lọc và phạm vi dữ liệu kê đơn để cung cấp lựa chọn cho bác sĩ. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDonThuoc.TimThuoc</c>.</remarks>
    /// <param name="filter">Giá trị bộ lọc dùng khi thực hiện thao tác; kiểu BoLocThuoc.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Tập dữ liệu kiểu IReadOnlyList&lt;ThuocKeDon&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
    public IReadOnlyList<ThuocKeDon> TimThuoc(BoLocThuoc filter,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.TimThuoc(filter,user));
    /// <summary>
    /// Đọc các lần khám có liên quan đến kê đơn trong phạm vi người dùng. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDonThuoc.LayLuotKeDon</c>.</remarks>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <param name="patient">Giá trị bệnh nhân dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <returns>Tập dữ liệu kiểu IReadOnlyList&lt;LuotKeDon&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
    public IReadOnlyList<LuotKeDon> LayLuotKeDon(NguCanhNghiepVu user,string? patient=null)=>XuLyLoi.ThucHien(()=>dalDonThuoc.LayLuotKeDon(user,patient));
    /// <summary>
    /// Nạp ngữ cảnh lần khám, các đơn và dữ liệu cần thiết cho màn hình kê thuốc. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDonThuoc.LayKhongGianKeDon</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu KhongGianKeDon dùng cho bước xử lý tiếp theo.</returns>
    public KhongGianKeDon LayKhongGianKeDon(string maLS,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.LayKhongGianKeDon(maLS,user));
    /// <summary>
    /// Đọc đơn thuốc và các dòng chi tiết kèm thông tin phiên bản để tiếp tục soạn hoặc đối chiếu. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDonThuoc.LayBanNhapDon</c>.</remarks>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapDon dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapDon LayBanNhapDon(string id,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.LayBanNhapDon(id,user));
    /// <summary>
    /// Kiểm tra và lưu đơn nháp cùng các dòng thuốc; giữ liên kết lần khám và thông tin thay thế khi có. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDonThuoc.LuuDon</c>.</remarks>
    /// <param name="r">Gói dữ liệu đầu vào cho thao tác; các trường được kiểm tra trong quá trình xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapDon dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapDon LuuDon(YeuCauLuuDon r,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.LuuDon(r,user));
    /// <summary>
    /// Kiểm tra quyền, bản dữ liệu và điều kiện kê thuốc trước khi ký xác nhận đơn nháp. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDonThuoc.XacNhanDon</c>.</remarks>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="token">Giá trị dấu vân tay đối chiếu dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapDon dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapDon XacNhanDon(string id,string token,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.XacNhanDon(id,token,user));
    /// <summary>
    /// Hủy đơn nháp theo quyền và phiên bản dữ liệu mà người dùng đã đọc. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDonThuoc.HuyDonNhap</c>.</remarks>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="token">Giá trị dấu vân tay đối chiếu dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapDon dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapDon HuyDonNhap(string id,string token,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.HuyDonNhap(id,token,user));
    /// <summary>
    /// Đọc nội dung đơn dùng cho bản in theo quyền và điều kiện hiệu lực của chứng từ. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDonThuoc.LayBanInDon</c>.</remarks>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanInDonThuoc dùng cho bước xử lý tiếp theo.</returns>
    public BanInDonThuoc LayBanInDon(string id,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.LayBanInDon(id,user));
    /// <summary>
    /// Kiểm tra đơn thuốc có đủ điều kiện tham gia bước thu tiền của lần khám. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDonThuoc.KiemTraDonChoThu</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool KiemTraDonChoThu(string maLS,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.KiemTraDonChoThu(maLS,user));
    /// <summary>
    /// Đọc cấu hình hướng dẫn thuốc và các nhóm liên quan cho màn hình quản lý. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDonThuoc.LayHuongDan</c>.</remarks>
    /// <param name="drug">Giá trị thuốc dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Đối tượng kiểu BanNhapHuongDanThuoc được tìm hoặc tính toán; có thể null theo nhánh xử lý trong hàm.</returns>
    public BanNhapHuongDanThuoc? LayHuongDan(string drug,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.LayHuongDan(drug,user));
    /// <summary>
    /// Lưu bản cấu hình hướng dẫn thuốc theo đầu vào và quyền cấu hình của người thực hiện. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDonThuoc.LuuHuongDan</c>.</remarks>
    /// <param name="config">Giá trị cấu hình dùng khi thực hiện thao tác; kiểu ET_CAUHINHTHUOCKEDON.</param>
    /// <param name="token">Giá trị dấu vân tay đối chiếu dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapHuongDanThuoc dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapHuongDanThuoc LuuHuongDan(ET_CAUHINHTHUOCKEDON config,string? token,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.LuuHuongDan(config,token,user));
    /// <summary>
    /// Duyệt phiên bản hướng dẫn thuốc sau các bước kiểm tra dữ liệu và quyền. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDonThuoc.DuyetHuongDan</c>.</remarks>
    /// <param name="drug">Giá trị thuốc dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="token">Giá trị dấu vân tay đối chiếu dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapHuongDanThuoc dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapHuongDanThuoc DuyetHuongDan(string drug,string token,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.DuyetHuongDan(drug,token,user));
}
