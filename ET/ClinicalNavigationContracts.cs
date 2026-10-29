namespace ET;
/// <summary>Định danh chuyển màn; DAL xác minh lại mọi liên kết và suy các khóa cha.</summary>
public sealed record NguCanhHoSo
{
    /// <summary>
    /// Mã bệnh nhân. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaBN {get;init;}
    /// <summary>
    /// Mã hồ sơ bệnh án của bệnh nhân. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaHS {get;init;}
    /// <summary>
    /// Mã phiếu tiếp nhận khám. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaPhieu {get;init;}
    /// <summary>
    /// Mã lần khám liên kết các chứng từ cùng lượt. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaLS {get;init;}
    /// <summary>
    /// Mã đơn thuốc. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaDT {get;init;}
    /// <summary>
    /// Mã hóa đơn. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaHD {get;init;}
}
/// <summary>
/// Gói dữ liệu bộ lọc danh sách hồ sơ truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
public sealed record BoLocDanhSachHoSo
{
    /// <summary>
    /// Từ khóa tìm kiếm áp dụng cho danh sách. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? TuKhoa {get;init;}
    /// <summary>
    /// Giới tính được lưu trong hồ sơ. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? GioiTinh {get;init;}
    /// <summary>
    /// Trạng thái. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? TrangThai {get;init;}
    /// <summary>
    /// Mã bệnh nhân. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaBN {get;init;}
    /// <summary>
    /// Mã lần khám liên kết các chứng từ cùng lượt. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaLS {get;init;}
    /// <summary>
    /// Ngày bắt đầu khoảng áp dụng/lọc. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public DateTime? TuNgay {get;init;}
    /// <summary>
    /// Ngày kết thúc khoảng áp dụng/lọc. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public DateTime? DenNgay {get;init;}
    /// <summary>
    /// Số trang đang được yêu cầu.
    /// </summary>
    public int Trang {get;init;}=1;
    /// <summary>
    /// Số bản ghi tối đa của mỗi trang kết quả.
    /// </summary>
    public int KichThuocTrang {get;init;}=25;
    /// <summary>
    /// Chỉ các mục của người hiện tại.
    /// </summary>
    public bool ChiCuaToi {get;init;}=true;
}
/// <summary>
/// Gói dữ liệu trạng hồ sơ truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="CacDong">Giá trị các dòng dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;T&gt;.</param>
/// <param name="TongSo">Giá trị tổng số dùng khi thực hiện thao tác; kiểu int.</param>
/// <param name="Trang">Giá trị trạng dùng khi thực hiện thao tác; kiểu int.</param>
/// <param name="KichThuocTrang">Giá trị kích thước trang dùng khi thực hiện thao tác; kiểu int.</param>
/// <typeparam name="T">Kiểu dữ liệu dùng cho thao tác tổng quát này.</typeparam>
public sealed record TrangHoSo<T>(IReadOnlyList<T> CacDong,int TongSo,int Trang,int KichThuocTrang);
/// <summary>
/// Mô hình và thao tác hỗ trợ dòng bệnh nhân danh sách dùng bởi các thành phần liên quan.
/// </summary>
public sealed class DongBenhNhanDanhSach:ET_BENHNHAN {
    /// <summary>
    /// Lần khám gần nhất. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public DateTime? LanKhamGanNhat {get;set;}}
/// <summary>
/// Mô hình và thao tác hỗ trợ dòng lần khám danh sách dùng bởi các thành phần liên quan.
/// </summary>
public sealed class DongLanKhamDanhSach:ET_LICHSUKHAMBENH
{
    /// <summary>
    /// Mã bệnh nhân.
    /// </summary>
    public string MaBN {get;set;}="";
    /// <summary>
    /// Họ tên bệnh nhân.
    /// </summary>
    public string HoTenBN {get;set;}="";
    /// <summary>
    /// Tên bác sĩ.
    /// </summary>
    public string TenBS {get;set;}="";
    /// <summary>
    /// Nguồn tiếp nhận.
    /// </summary>
    public string NguonTiepNhan {get;set;}="";
}
/// <summary>
/// Mô hình và thao tác hỗ trợ dòng lịch sử đơn dùng bởi các thành phần liên quan.
/// </summary>
public sealed class DongLichSuDon:ET_DONTHUOC
{
    /// <summary>
    /// Mã bệnh nhân.
    /// </summary>
    public string MaBN {get;set;}="";
    /// <summary>
    /// Họ tên bệnh nhân.
    /// </summary>
    public string HoTenBN {get;set;}="";
    /// <summary>
    /// Số điện thoại liên hệ. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? SDT {get;set;}
    /// <summary>
    /// Ngày sinh. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public DateTime? NgaySinh {get;set;}
    /// <summary>
    /// Thời gian khám.
    /// </summary>
    public DateTime ThoiGianKham {get;set;}
    /// <summary>
    /// Tên bác sĩ.
    /// </summary>
    public string TenBS {get;set;}="";
    /// <summary>
    /// Chẩn đoán của bác sĩ trong lần khám.
    /// </summary>
    public string ChanDoan {get;set;}="";
    /// <summary>
    /// Các dòng.
    /// </summary>
    public IReadOnlyList<ET_CHITIETDONTHUOC> CacDong {get;set;}=Array.Empty<ET_CHITIETDONTHUOC>();
}
