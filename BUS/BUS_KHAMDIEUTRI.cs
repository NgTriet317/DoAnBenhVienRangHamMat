using System.Data;
using ET;
using DAL;

namespace BUS;

/// <summary>
/// Tầng nghiệp vụ khám điều trị, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
/// </summary>
public sealed partial class BUS_KHAMDIEUTRI
{
    /// <summary>
    /// Đối tượng truy cập dữ liệu khám điều trị phục vụ các thao tác của lớp.
    /// </summary>
    private readonly DAL_KHAMDIEUTRI dal;
    /// <summary>
    /// Đọc dữ liệu hồ sơ và các thành phần liên quan trong phạm vi người dùng được truy cập. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayDuLieuHoSo</c>.</remarks>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu DuLieuHoSoTheoQuyen dùng cho bước xử lý tiếp theo.</returns>
    public DuLieuHoSoTheoQuyen LayDuLieuHoSo(NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => dal.LayDuLieuHoSo(nguoiDung));
    /// <summary>
    /// Chuẩn bị nguồn hồ sơ theo quyền để giao diện tra cứu và điều hướng dữ liệu liên quan. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayDuLieuHoSoGiaoDien</c>.</remarks>
    /// <param name="context">Giá trị ngữ cảnh dùng khi thực hiện thao tác; kiểu NguCanhHoSo.</param>
    /// <param name="actor">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu DuLieuHoSoTheoQuyen dùng cho bước xử lý tiếp theo.</returns>
    public DuLieuHoSoTheoQuyen LayDuLieuHoSoGiaoDien(NguCanhHoSo context,NguCanhNghiepVu actor)=>XuLyLoi.ThucHien(()=>dal.LayDuLieuHoSoGiaoDien(context,actor));
    /// <summary>
    /// Đọc một trang bệnh nhân trong phạm vi quyền và bộ lọc được yêu cầu. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayTrangBenhNhan</c>.</remarks>
    /// <param name="filter">Giá trị bộ lọc dùng khi thực hiện thao tác; kiểu BoLocDanhSachHoSo.</param>
    /// <param name="actor">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu TrangHoSo&lt;DongBenhNhanDanhSach&gt; dùng cho bước xử lý tiếp theo.</returns>
    public TrangHoSo<DongBenhNhanDanhSach> LayTrangBenhNhan(BoLocDanhSachHoSo filter,NguCanhNghiepVu actor)=>XuLyLoi.ThucHien(()=>dal.LayTrangBenhNhan(filter,actor));
    /// <summary>
    /// Đọc một trang các lần khám theo phạm vi hồ sơ và bộ lọc. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayTrangLanKham</c>.</remarks>
    /// <param name="filter">Giá trị bộ lọc dùng khi thực hiện thao tác; kiểu BoLocDanhSachHoSo.</param>
    /// <param name="actor">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu TrangHoSo&lt;DongLanKhamDanhSach&gt; dùng cho bước xử lý tiếp theo.</returns>
    public TrangHoSo<DongLanKhamDanhSach> LayTrangLanKham(BoLocDanhSachHoSo filter,NguCanhNghiepVu actor)=>XuLyLoi.ThucHien(()=>dal.LayTrangLanKham(filter,actor));
    /// <summary>
    /// Đọc một trang lịch sử đơn thuốc theo phạm vi và bộ lọc của người dùng. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayTrangLichSuDon</c>.</remarks>
    /// <param name="filter">Giá trị bộ lọc dùng khi thực hiện thao tác; kiểu BoLocDanhSachHoSo.</param>
    /// <param name="actor">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu TrangHoSo&lt;DongLichSuDon&gt; dùng cho bước xử lý tiếp theo.</returns>
    public TrangHoSo<DongLichSuDon> LayTrangLichSuDon(BoLocDanhSachHoSo filter,NguCanhNghiepVu actor)=>XuLyLoi.ThucHien(()=>dal.LayTrangLichSuDon(filter,actor));
    /// <summary>
    /// Tra mã lần khám gắn với hóa đơn để mở đúng nguồn nghiệp vụ. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayMaLuotHoaDon</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="actor">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Tập dữ liệu kiểu IReadOnlyList&lt;string&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
    public IReadOnlyList<string> LayMaLuotHoaDon(string? maLS,NguCanhNghiepVu actor)=>XuLyLoi.ThucHien(()=>dal.LayMaLuotHoaDon(maLS,actor));

    /// <summary>
    /// Đối chiếu liên kết hồ sơ/bệnh nhân/lần khám trước khi điều hướng hoặc dùng dữ liệu liên quan.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XacMinhNguCanh</c>.</remarks>
    /// <param name="maBN">Mã bệnh nhân xác định đối tượng cần xử lý.</param>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="maDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    public void KiemTraLienKet(string? maBN, string? maLS, string? maDT, NguCanhNghiepVu nguoiDung)
        =>XacMinhNguCanh(new(){MaBN=string.IsNullOrEmpty(maBN)?null:maBN,MaLS=string.IsNullOrEmpty(maLS)?null:maLS,MaDT=string.IsNullOrEmpty(maDT)?null:maDT},nguoiDung);
    /// <summary>
    /// Đối chiếu quan hệ giữa bệnh nhân, hồ sơ, phiếu và lần khám trước khi sử dụng ngữ cảnh. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.XacMinhNguCanh</c>.</remarks>
    /// <param name="context">Giá trị ngữ cảnh dùng khi thực hiện thao tác; kiểu NguCanhHoSo.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu NguCanhHoSo dùng cho bước xử lý tiếp theo.</returns>
    public NguCanhHoSo XacMinhNguCanh(NguCanhHoSo context,NguCanhNghiepVu nguoiDung)
        =>XuLyLoi.ThucHien(()=>dal.XacMinhNguCanh(context,nguoiDung));
    /// <summary>
    /// Khởi tạo khám điều trị.
    /// </summary>
    /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    public BUS_KHAMDIEUTRI(IDbTransaction? transaction = null) => dal = new(transaction);
    /// <summary>
    /// Đọc danh sách hàng đợi theo phạm vi dữ liệu và quyền của người dùng. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayHangDoi</c>.</remarks>
    /// <param name="boLoc">Giá trị bộ lọc dùng khi thực hiện thao tác; kiểu BoLocHangDoi.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Tập dữ liệu kiểu IReadOnlyList&lt;HangDoiKhamItem&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
    public IReadOnlyList<HangDoiKhamItem> LayHangDoi(BoLocHangDoi boLoc, NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => dal.LayHangDoi(boLoc, nguoiDung));
    /// <summary>
    /// Đọc một trang hàng chờ khám cùng tổng số mục khớp điều kiện. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayTrangHangDoi</c>.</remarks>
    /// <param name="boLoc">Giá trị bộ lọc dùng khi thực hiện thao tác; kiểu BoLocHangDoi.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu TrangHoSo&lt;HangDoiKhamItem&gt; dùng cho bước xử lý tiếp theo.</returns>
    public TrangHoSo<HangDoiKhamItem> LayTrangHangDoi(BoLocHangDoi boLoc,NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => dal.LayTrangHangDoi(boLoc,nguoiDung));
    /// <summary>
    /// Đọc dữ liệu chi tiết của mục được chọn để hiển thị hoặc tiếp tục xử lý. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>XuLyLoi.ThucHien</c>, <c>dal.LayChiTiet</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu LanKhamChiTiet dùng cho bước xử lý tiếp theo.</returns>
    public LanKhamChiTiet LayChiTiet(string maLS, NguCanhNghiepVu nguoiDung)
        => XuLyLoi.ThucHien(() => dal.LayChiTiet(maLS, nguoiDung));
}
