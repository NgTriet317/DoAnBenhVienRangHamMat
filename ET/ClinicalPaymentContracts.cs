namespace ET;

/// <summary>
/// Gói dữ liệu yêu cầu thanh toán truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
public sealed record YeuCauThanhToan
{
    /// <summary>
    /// Mã lần khám liên kết các chứng từ cùng lượt.
    /// </summary>
    public string MaLS { get; init; } = "";
    /// <summary>
    /// Mã kho được chọn để cấp thuốc cho lần khám. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaKhoCapThuoc { get; init; }
    /// <summary>
    /// Mã duy nhất nhận diện yêu cầu, phục vụ đối chiếu khi gọi lại.
    /// </summary>
    public Guid MaYeuCau { get; init; }
    /// <summary>
    /// Dấu vân tay bảng tính mà người dùng đã xem trước khi xác nhận thu.
    /// </summary>
    public string DauVanTayBangTinh { get; init; } = "";
    /// <summary>
    /// Phương thức thanh toán, được kiểm tra khi xác nhận thu tiền.
    /// </summary>
    public string PhuongThuc { get; init; } = "Tiền mặt";
    /// <summary>
    /// Số tiền khách đưa hoặc số tiền chuyển khoản dùng khi thu.
    /// </summary>
    public decimal SoTienKhachDua { get; init; }
    /// <summary>
    /// Mã tham chiếu. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaThamChieu { get; init; }
    /// <summary>
    /// Xác nhận của người thu rằng khoản chuyển đã được đối chiếu thủ công.
    /// </summary>
    public bool DaDoiChieuChuyenKhoan { get; init; }
}
/// <summary>
/// Gói dữ liệu đơn thuốc đã thu truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
/// <param name="MaPX">Mã phiếu xuất xác định đối tượng cần xử lý.</param>
public sealed record DonThuocDaThu(string MaDT, string MaPX)
{
    /// <summary>
    /// Trạng thái bàn giao thuốc cho bệnh nhân.
    /// </summary>
    public string TrangThaiGiao { get; init; } = "Chờ giao";
    /// <summary>
    /// Mã nhân viên xác nhận bàn giao thuốc. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaNVGiao { get; init; }
    /// <summary>
    /// Thời điểm xác nhận bàn giao thuốc. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public DateTime? ThoiGianGiao { get; init; }
}
/// <summary>
/// Gói dữ liệu kết quả thanh toán truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaLS">Mã lần khám xác định đối tượng cần xử lý.</param>
/// <param name="MaHD">Mã hóa đơn xác định đối tượng cần xử lý.</param>
/// <param name="MaTT">Mã thanh toán xác định đối tượng cần xử lý.</param>
/// <param name="CacDon">Giá trị các đơn dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;DonThuocDaThu&gt;.</param>
/// <param name="SoTienThu">Giá trị số tiền thu dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="SoTienKhachDua">Giá trị số tiền khách đưa dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="TienThua">Giá trị tiền thừa dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="DaTonTai">Giá trị đã tồn tại dùng khi thực hiện thao tác; kiểu bool.</param>
public sealed record KetQuaThanhToan(string MaLS, string MaHD, string? MaTT,
    IReadOnlyList<DonThuocDaThu> CacDon, decimal SoTienThu, decimal SoTienKhachDua,
    decimal TienThua, bool DaTonTai);
