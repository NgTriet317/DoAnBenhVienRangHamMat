namespace ET;

/// <summary>
/// Gói dữ liệu yêu cầu thực hiện dịch vụ truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
public sealed record YeuCauThucHienDichVu
{
    /// <summary>
    /// Mã dịch vụ thực tế đã thực hiện, dùng truy về nguồn tính phí. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaDVThucHien { get; init; }
    /// <summary>
    /// Mã lần khám liên kết các chứng từ cùng lượt.
    /// </summary>
    public string MaLS { get; init; } = "";
    /// <summary>
    /// Mã dòng chi tiết kế hoạch điều trị. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaCTKH { get; init; }
    /// <summary>
    /// Mã dịch vụ.
    /// </summary>
    public string MaDV { get; init; } = "";
    /// <summary>
    /// Mã nhân viên thực hiện.
    /// </summary>
    public string MaNVThucHien { get; init; } = "";
    /// <summary>
    /// Thời gian thực hiện.
    /// </summary>
    public DateTime ThoiGianThucHien { get; init; }
    /// <summary>
    /// Số lượng của dòng dữ liệu.
    /// </summary>
    public decimal SoLuong { get; init; } = 1;
    /// <summary>
    /// Đơn giá áp dụng cho dòng dữ liệu.
    /// </summary>
    public decimal DonGia { get; init; }
    /// <summary>
    /// Kết quả. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? KetQua { get; init; }
    /// <summary>
    /// Lý do phát sinh. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? LyDoPhatSinh { get; init; }
    /// <summary>
    /// Dấu vân tay bản dữ liệu phục vụ đối chiếu khi lưu/xác nhận. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? DauVanTay { get; init; }
}
/// <summary>
/// Gói dữ liệu bản nháp thực hiện truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="DichVu">Giá trị dịch vụ dùng khi thực hiện thao tác; kiểu ET_DICHVUTHUCHIEN.</param>
/// <param name="DauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
/// <param name="DuocSua">Giá trị được sửa dùng khi thực hiện thao tác; kiểu bool.</param>
/// <param name="DuocXacNhan">Giá trị được xác nhận dùng khi thực hiện thao tác; kiểu bool.</param>
/// <param name="DaTinhPhi">Giá trị đã tính phí dùng khi thực hiện thao tác; kiểu bool.</param>
/// <param name="LyDoPhatSinh">Giá trị lý do phát sinh dùng khi thực hiện thao tác; kiểu string?.</param>
/// <param name="TenDV">Giá trị tên dịch vụ dùng khi thực hiện thao tác; kiểu string?.</param>
/// <param name="TenNguoiThucHien">Giá trị tên người thực hiện dùng khi thực hiện thao tác; kiểu string?.</param>
public sealed record BanNhapThucHien(ET_DICHVUTHUCHIEN DichVu, string DauVanTay,
    bool DuocSua, bool DuocXacNhan, bool DaTinhPhi, string? LyDoPhatSinh,
    string? TenDV=null, string? TenNguoiThucHien=null);
/// <summary>
/// Gói dữ liệu lượt thực hiện truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaLS">Mã lần khám xác định đối tượng cần xử lý.</param>
/// <param name="MaBN">Mã bệnh nhân xác định đối tượng cần xử lý.</param>
/// <param name="HoTenBN">Giá trị họ tên bệnh nhân dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="MaHS">Mã hồ sơ xác định đối tượng cần xử lý.</param>
/// <param name="MaBS">Mã bác sĩ xác định đối tượng cần xử lý.</param>
/// <param name="ThoiGianKham">Giá trị thời gian khám dùng khi thực hiện thao tác; kiểu DateTime.</param>
/// <param name="TrangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
public sealed record LuotThucHien(string MaLS, string MaBN, string HoTenBN, string MaHS,
    string MaBS, DateTime ThoiGianKham, string TrangThai);
/// <summary>
/// Gói dữ liệu người thực hiện dịch vụ truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
/// <param name="HoTen">Giá trị họ tên dùng khi thực hiện thao tác; kiểu string.</param>
public sealed record NguoiThucHienDichVu(string MaNV, string HoTen);
/// <summary>
/// Gói dữ liệu bước dự kiến thực hiện truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaCTKH">Mã chi tiết kế hoạch xác định đối tượng cần xử lý.</param>
/// <param name="MaKHDT">Mã kế hoạch điều trị xác định đối tượng cần xử lý.</param>
/// <param name="MaLSSource">Mã lần khám nguồn xác định đối tượng cần xử lý.</param>
/// <param name="MaDV">Mã dịch vụ xác định đối tượng cần xử lý.</param>
/// <param name="TenDV">Giá trị tên dịch vụ dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="LanThu">Giá trị lần thứ dùng khi thực hiện thao tác; kiểu int.</param>
/// <param name="NoiDung">Giá trị nội dung dùng khi thực hiện thao tác; kiểu string?.</param>
/// <param name="SoLuongDuKien">Giá trị số lượng dự kiến dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="DaXacNhan">Giá trị đã xác nhận dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="DangNhap">Giá trị đang nhập dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="ConLai">Giá trị còn lại dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="DonGiaDuKien">Giá trị đơn giá dự kiến dùng khi thực hiện thao tác; kiểu decimal.</param>
public sealed record BuocDuKienThucHien(string MaCTKH, string MaKHDT, string MaLSSource,
    string MaDV, string TenDV, int LanThu, string? NoiDung, decimal SoLuongDuKien,
    decimal DaXacNhan, decimal DangNhap, decimal ConLai, decimal DonGiaDuKien);
/// <summary>
/// Gói dữ liệu không gian thực hiện truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="Luot">Giá trị lượt dùng khi thực hiện thao tác; kiểu LuotThucHien.</param>
/// <param name="DuocTao">Giá trị được tạo dùng khi thực hiện thao tác; kiểu bool.</param>
/// <param name="DuKien">Giá trị dự kiến dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;BuocDuKienThucHien&gt;.</param>
/// <param name="ThucTe">Giá trị thực tế dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;BanNhapThucHien&gt;.</param>
/// <param name="DanhMuc">Giá trị danh mục dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;ET_DICHVU&gt;.</param>
/// <param name="NhanVien">Giá trị nhân viên dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;NguoiThucHienDichVu&gt;.</param>
public sealed record KhongGianThucHien(LuotThucHien Luot, bool DuocTao,
    IReadOnlyList<BuocDuKienThucHien> DuKien, IReadOnlyList<BanNhapThucHien> ThucTe,
    IReadOnlyList<ET_DICHVU> DanhMuc, IReadOnlyList<NguoiThucHienDichVu> NhanVien)
{
    /// <summary>
    /// Giá trị đã xác nhận được cộng từ các dòng thành phần trong ThucTe.
    /// </summary>
    public decimal GiaTriDaXacNhan => ThucTe.Where(s=>s.DichVu.TrangThai=="Đã xác nhận").Sum(s=>s.DichVu.ThanhTien);
}
