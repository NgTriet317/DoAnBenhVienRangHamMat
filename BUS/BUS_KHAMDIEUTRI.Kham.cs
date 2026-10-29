using ET;
namespace BUS;
/// <summary>
/// Tầng nghiệp vụ khám điều trị, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
/// </summary>
public sealed partial class BUS_KHAMDIEUTRI
{
    /// <summary>
    /// Chốt lần khám từ bản tổng kết đã xem, phối hợp xác nhận đơn thuốc và lập hóa đơn để giữ nhất quán các chứng từ. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.ChotKhamVaLapHoaDon</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="dauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
    /// <param name="dauVanTayTongKet">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapKham dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapKham ChotKhamVaLapHoaDon(string maLS,string dauVanTay,string dauVanTayTongKet,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.ChotKhamVaLapHoaDon(maLS,dauVanTay,dauVanTayTongKet,user));
    /// <summary>
    /// Đọc các dịch vụ và thuốc thuộc lần khám để tổng kết trước khi chốt; giữ thông tin nguồn và dấu vân tay đối chiếu. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayTongKetKham</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu TongKetKham dùng cho bước xử lý tiếp theo.</returns>
    public TongKetKham LayTongKetKham(string maLS,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.LayTongKetKham(maLS,user));
    /// <summary>
    /// Xác nhận các dịch vụ đã thực hiện sau khi đối chiếu dữ liệu hiện tại và quyền của người thao tác. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.XacNhanThucHien</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="dauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu TongKetKham dùng cho bước xử lý tiếp theo.</returns>
    public TongKetKham XacNhanThucHien(string maLS,string dauVanTay,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.XacNhanThucHien(maLS,dauVanTay,user));
    /// <summary>
    /// Nạp bản khám, bệnh nhân, dịch vụ, kế hoạch và danh mục tình trạng để màn hình mở đúng ngữ cảnh. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayKhongGianKham</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu DuLieuKhongGianKham dùng cho bước xử lý tiếp theo.</returns>
    public DuLieuKhongGianKham LayKhongGianKham(string maLS,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.LayKhongGianKham(maLS,user));
    /// <summary>
    /// Bắt đầu khám từ phiếu tiếp nhận: kiểm tra bác sĩ/quyền, dùng lại lượt đã tạo khi phù hợp và cập nhật phiếu sang Đang khám. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.BatDauKham</c>.</remarks>
    /// <param name="maPhieu">Mã phiếu xác định đối tượng cần xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Chuỗi kết quả của thao tác bắt đầu khám.</returns>
    public string BatDauKham(string maPhieu,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.BatDauKham(maPhieu,user));
    /// <summary>
    /// Đọc bản khám hiện tại cùng tình trạng bệnh, quyền sửa và dấu vân tay dữ liệu. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayBanNhap</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapKham dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapKham LayBanNhap(string maLS,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.LayBanNhap(maLS,user));
    /// <summary>
    /// Lưu nháp nội dung khám và các tình trạng bệnh; kiểm tra lựa chọn không kê thuốc và quan hệ kế hoạch trước khi thay các dòng. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LuuKham</c>.</remarks>
    /// <param name="request">Gói dữ liệu đầu vào cho thao tác; các trường được kiểm tra trong quá trình xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapKham dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapKham LuuKham(YeuCauLuuKham request,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.LuuKham(request,user));
    /// <summary>
    /// Đọc đầu kế hoạch, các bước và dấu vân tay trong phạm vi lần khám mà người dùng được phép truy cập. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayKeHoach</c>.</remarks>
    /// <param name="maKHDT">Mã kế hoạch điều trị xác định đối tượng cần xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapKeHoach dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapKeHoach LayKeHoach(string maKHDT,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.LayKeHoach(maKHDT,user));
    /// <summary>
    /// Lưu nháp kế hoạch và các bước sau khi kiểm tra nguồn tình trạng, quyền sửa, ngày và dữ liệu dịch vụ. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LuuKeHoach</c>.</remarks>
    /// <param name="request">Gói dữ liệu đầu vào cho thao tác; các trường được kiểm tra trong quá trình xử lý.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapKeHoach dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapKeHoach LuuKeHoach(YeuCauLuuKeHoach request,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.LuuKeHoach(request,user));
    /// <summary>
    /// Duyệt kế hoạch nháp sau khi kiểm tra nội dung từng bước và dịch vụ đang áp dụng. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.XacNhanKeHoach</c>.</remarks>
    /// <param name="maKHDT">Mã kế hoạch điều trị xác định đối tượng cần xử lý.</param>
    /// <param name="dauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapKeHoach dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapKeHoach XacNhanKeHoach(string maKHDT,string dauVanTay,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.XacNhanKeHoach(maKHDT,dauVanTay,user));
    /// <summary>
    /// Hủy kế hoạch nháp khi chưa có dịch vụ thực hiện liên kết; giữ lịch sử kế hoạch đã tạo. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.HuyKeHoach</c>.</remarks>
    /// <param name="maKHDT">Mã kế hoạch điều trị xác định đối tượng cần xử lý.</param>
    /// <param name="dauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapKeHoach dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapKeHoach HuyKeHoach(string maKHDT,string dauVanTay,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.HuyKeHoach(maKHDT,dauVanTay,user));
    /// <summary>
    /// Kiểm tra nội dung khám, đơn thuốc và dịch vụ trước khi hoàn tất; cập nhật phiếu và lập hóa đơn trong giao dịch. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.HoanTatKham</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="dauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BanNhapKham dùng cho bước xử lý tiếp theo.</returns>
    public BanNhapKham HoanTatKham(string maLS,string dauVanTay,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.HoanTatKham(maLS,dauVanTay,user));
}
