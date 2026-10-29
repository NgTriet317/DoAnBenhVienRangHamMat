using ET;
namespace BUS;
/// <summary>
/// Tầng nghiệp vụ dịch vụ thực hiện, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
/// </summary>
public partial class BUS_DICHVUTHUCHIEN
{
    /// <summary>
    /// Đọc các lần khám có nguồn dịch vụ thực hiện thuộc phạm vi tra cứu của người dùng. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDichVuThucHien.LayLuotThucHien</c>.</remarks>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <param name="maBN">Mã bệnh nhân xác định đối tượng cần xử lý.</param>
    /// <returns>Tập dữ liệu kiểu IReadOnlyList&lt;LuotThucHien&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
    public IReadOnlyList<LuotThucHien> LayLuotThucHien(NguCanhNghiepVu user,string? maBN=null)=>XuLyLoi.ThucHien(()=>dalDichVuThucHien.LayLuotThucHien(user,maBN));
    /// <summary>
    /// Nạp dữ liệu lần khám và các dịch vụ lịch sử để màn hình chỉ tra cứu. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDichVuThucHien.LayKhongGianThucHien</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu KhongGianThucHien dùng cho bước xử lý tiếp theo.</returns>
    public KhongGianThucHien LayKhongGianThucHien(string maLS,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDichVuThucHien.LayKhongGianThucHien(maLS,user));
    /// <summary>
    /// Đọc thông tin dịch vụ thực tế theo mã trong phạm vi người dùng được truy cập. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDichVuThucHien.LayThucHien</c>.</remarks>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapThucHien dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapThucHien LayThucHien(string id,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDichVuThucHien.LayThucHien(id,user));
    /// <summary>
    /// API tương thích cũ luôn từ chối thay đổi dịch vụ độc lập bằng LoiKhamException. Lịch sử dịch vụ chỉ đọc; ghi nhận thực hiện qua quy trình khám.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDichVuThucHien.LuuThucHien</c>.</remarks>
    /// <param name="request">Gói dữ liệu đầu vào cho thao tác; các trường được kiểm tra trong quá trình xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Không trả kết quả bình thường; DAL luôn phát LoiKhamException để chặn ghi ngoài quy trình khám.</returns>
    public BanNhapThucHien LuuThucHien(YeuCauThucHienDichVu request,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDichVuThucHien.LuuThucHien(request,user));
    /// <summary>
    /// API tương thích cũ luôn từ chối thay đổi dịch vụ độc lập bằng LoiKhamException. Lịch sử dịch vụ chỉ đọc; ghi nhận thực hiện qua quy trình khám.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDichVuThucHien.XacNhanThucHien</c>.</remarks>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="dauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Không trả kết quả bình thường; DAL luôn phát LoiKhamException để chặn ghi ngoài quy trình khám.</returns>
    public BanNhapThucHien XacNhanThucHien(string id,string dauVanTay,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDichVuThucHien.XacNhanThucHien(id,dauVanTay,user));
    /// <summary>
    /// API tương thích cũ luôn từ chối thay đổi dịch vụ độc lập bằng LoiKhamException. Lịch sử dịch vụ chỉ đọc; ghi nhận thực hiện qua quy trình khám.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dalDichVuThucHien.HuyThucHien</c>.</remarks>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="dauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Không trả kết quả bình thường; DAL luôn phát LoiKhamException để bảo toàn lịch sử dịch vụ.</returns>
    public BanNhapThucHien HuyThucHien(string id,string dauVanTay,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDichVuThucHien.HuyThucHien(id,dauVanTay,user));
}
