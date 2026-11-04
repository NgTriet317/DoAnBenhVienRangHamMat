namespace ET;

// Chỉ dữ liệu tài chính; không chuyển kết quả khám hoặc chỉ định dùng thuốc cho quầy thu.
public sealed record DongPhiDichVu(string MaDVThucHien, string MaDV, string NoiDung,
    decimal SoLuong, decimal DonGia, decimal ThanhTien);
/// <summary>
/// Gói dữ liệu phân lô tính phí truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaLo">Mã lô xác định đối tượng cần xử lý.</param>
/// <param name="SoLo">Giá trị số lô dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="SoLuong">Giá trị số lượng dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="DonGia">Giá trị đơn giá dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="ThanhTien">Giá trị thành tiền dùng khi thực hiện thao tác; kiểu decimal.</param>
public sealed record PhanLoTinhPhi(string MaLo, string SoLo, decimal SoLuong, decimal DonGia, decimal ThanhTien);
/// <summary>
/// Gói dữ liệu dòng phí thuốc truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
/// <param name="MaCTDT">Mã chi tiết đơn thuốc xác định đối tượng cần xử lý.</param>
/// <param name="MaThuoc">Mã thuốc xác định đối tượng cần xử lý.</param>
/// <param name="NoiDung">Giá trị nội dung dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="DonViTinh">Giá trị đơn vị tính dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="SoLuong">Giá trị số lượng dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="DonGia">Giá trị đơn giá dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="CacLo">Giá trị các lô dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;PhanLoTinhPhi&gt;.</param>
public sealed record DongPhiThuoc(string MaDT, string MaCTDT, string MaThuoc, string NoiDung,
    string DonViTinh, decimal SoLuong, decimal DonGia, IReadOnlyList<PhanLoTinhPhi> CacLo)
{
    /// <summary>
    /// Thành tiền của dòng hoặc phần lô được cộng từ các dòng thành phần trong CacLo.
    /// </summary>
    public decimal ThanhTien => CacLo.Sum(l => l.ThanhTien);
}
/// <summary>
/// Gói dữ liệu hóa đơn đối chiếu truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaHD">Mã hóa đơn xác định đối tượng cần xử lý.</param>
/// <param name="TrangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="PhienBanQuyTrinh">Giá trị phiên bản quy trình dùng khi thực hiện thao tác; kiểu int.</param>
/// <param name="TongTien">Giá trị tổng tiền dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="GiamGia">Giá trị giảm giá dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="PhaiThu">Giá trị phải thu dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="DaThu">Giá trị đã thu dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="ConPhaiThu">Giá trị còn phải thu dùng khi thực hiện thao tác; kiểu decimal.</param>
public sealed record HoaDonDoiChieu(string MaHD, string TrangThai, int PhienBanQuyTrinh,
    decimal TongTien, decimal GiamGia, decimal PhaiThu, decimal DaThu, decimal ConPhaiThu);
/// <summary>
/// Gói dữ liệu bảng tính phí truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaLS">Mã lần khám xác định đối tượng cần xử lý.</param>
/// <param name="MaBN">Mã bệnh nhân xác định đối tượng cần xử lý.</param>
/// <param name="HoTenBN">Giá trị họ tên bệnh nhân dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="MaKhoCapThuoc">Mã kho cấp thuốc xác định đối tượng cần xử lý.</param>
/// <param name="DichVu">Giá trị dịch vụ dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;DongPhiDichVu&gt;.</param>
/// <param name="Thuoc">Giá trị thuốc dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;DongPhiThuoc&gt;.</param>
/// <param name="HoaDonCu">Giá trị hóa đơn cũ dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;HoaDonDoiChieu&gt;.</param>
/// <param name="DauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
/// <param name="DuocThanhToan">Giá trị được thanh toán dùng khi thực hiện thao tác; kiểu bool.</param>
/// <param name="ThongBao">Giá trị thông báo dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="MaHDChoThu">Mã hóa đơn chờ thu xác định đối tượng cần xử lý.</param>
public sealed record BangTinhPhi(string MaLS, string MaBN, string HoTenBN, string? MaKhoCapThuoc,
    IReadOnlyList<DongPhiDichVu> DichVu, IReadOnlyList<DongPhiThuoc> Thuoc,
    IReadOnlyList<HoaDonDoiChieu> HoaDonCu, string? DauVanTay, bool DuocThanhToan, string ThongBao,
    string? MaHDChoThu = null)
{
    /// <summary>
    /// Tổng tiền dịch vụ được cộng từ các dòng thành phần trong DichVu.
    /// </summary>
    public decimal TienDichVu => DichVu.Sum(d => d.ThanhTien);
    /// <summary>
    /// Tổng tiền thuốc được cộng từ các dòng thành phần trong Thuoc.
    /// </summary>
    public decimal TienThuoc => Thuoc.Sum(d => d.ThanhTien);
    /// <summary>
    /// Tổng tiền cần đối chiếu trước khi thu, bằng tiền dịch vụ cộng tiền thuốc.
    /// </summary>
    public decimal TongTien => TienDichVu + TienThuoc;
}
