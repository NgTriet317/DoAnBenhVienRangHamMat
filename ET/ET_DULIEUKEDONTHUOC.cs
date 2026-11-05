namespace ET;

/// <summary>
/// Gói dữ liệu bộ lọc thuốc truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaNhomThuoc">Mã nhóm thuốc xác định đối tượng cần xử lý.</param>
/// <param name="TuKhoa">Giá trị từ khóa dùng khi thực hiện thao tác; kiểu string?.</param>
/// <param name="DangBaoChe">Giá trị đang báo Che dùng khi thực hiện thao tác; kiểu string?.</param>
/// <param name="HamLuong">Giá trị hàm lượng dùng khi thực hiện thao tác; kiểu string?.</param>
public sealed record BoLocThuoc(string? MaNhomThuoc=null,string? TuKhoa=null,string? DangBaoChe=null,string? HamLuong=null);
/// <summary>
/// Gói dữ liệu thuốc kê đơn truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaThuoc">Mã thuốc xác định đối tượng cần xử lý.</param>
/// <param name="MaHH">Mã hàng hóa xác định đối tượng cần xử lý.</param>
/// <param name="TenThuoc">Giá trị tên thuốc dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="HamLuong">Giá trị hàm lượng dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="DonViTinh">Giá trị đơn vị tính dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="DonGiaThamKhao">Giá trị đơn giá tham khảo dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="TonThamKhao">Giá trị tồn tham khảo dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="HuongDan">Giá trị hướng dẫn dùng khi thực hiện thao tác; kiểu ET_CAUHINHTHUOCKEDON?.</param>
/// <param name="MaNhomThuoc">Mã nhóm thuốc xác định đối tượng cần xử lý.</param>
/// <param name="TenNhomThuoc">Giá trị tên nhóm thuốc dùng khi thực hiện thao tác; kiểu string?.</param>
public sealed record ThuocKeDon(string MaThuoc,string MaHH,string TenThuoc,string HamLuong,string DonViTinh,decimal DonGiaThamKhao,decimal TonThamKhao,ET_CAUHINHTHUOCKEDON? HuongDan,string? MaNhomThuoc=null,string? TenNhomThuoc=null)
{
    /// <summary>
    /// Giá trị Giá hiển thị; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string GiaHienThi=>DonGiaThamKhao>0?DonGiaThamKhao.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))+" đ":"Chưa thiết lập giá";
}
/// <summary>
/// Mô hình và thao tác hỗ trợ dòng thuốc kê đơn dùng bởi các thành phần liên quan.
/// </summary>
public sealed class DongThuocKeDon
{
    /// <summary>
    /// Mã dòng chi tiết đơn thuốc. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaCTDT{get;set;}
    /// <summary>
    /// Mã thuốc.
    /// </summary>
    public string MaThuoc{get;set;}="";
    /// <summary>
    /// Tên hiển thị.
    /// </summary>
    public string TenHienThi{get;set;}="";
    /// <summary>
    /// Liều dùng được người kê nhập và xác nhận.
    /// </summary>
    public string LieuDung{get;set;}="";
    /// <summary>
    /// Đường dùng thuốc được lưu cùng dòng kê đơn.
    /// </summary>
    public string DuongDung{get;set;}="";
    /// <summary>
    /// Hướng dẫn.
    /// </summary>
    public string HuongDan{get;set;}="";
    /// <summary>
    /// Số ngày dùng thuốc được ghi trên dòng kê đơn.
    /// </summary>
    public int SoNgayDung{get;set;}=1;
    /// <summary>
    /// Số lượng của dòng dữ liệu.
    /// </summary>
    public decimal SoLuong{get;set;}=1;
    /// <summary>
    /// Phiên bản hướng dẫn. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public int? PhienBanHuongDan{get;set;}
    /// <summary>
    /// Đã xác nhận phù hợp.
    /// </summary>
    public bool DaXacNhanPhuHop{get;set;}
}
/// <summary>
/// Gói dữ liệu yêu cầu lưu đơn truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
public sealed record YeuCauLuuDon
{
    /// <summary>
    /// Mã đơn thuốc. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaDT{get;init;}
    /// <summary>
    /// Mã lần khám liên kết các chứng từ cùng lượt.
    /// </summary>
    public string MaLS{get;init;}="";
    /// <summary>
    /// Mã đơn thuốc được thay thế bởi đơn hiện tại. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaDTThayThe{get;init;}
    /// <summary>
    /// Lý do thay thế. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? LyDoThayThe{get;init;}
    /// <summary>
    /// Nội dung ghi chú bổ sung của bản ghi. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? GhiChu{get;init;}
    /// <summary>
    /// Dấu vân tay bản dữ liệu phục vụ đối chiếu khi lưu/xác nhận. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? DauVanTay{get;init;}
    /// <summary>
    /// Các dòng.
    /// </summary>
    public IReadOnlyList<DongThuocKeDon> CacDong{get;init;}=Array.Empty<DongThuocKeDon>();
}
/// <summary>
/// Gói dữ liệu bản nháp đơn truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="Don">Giá trị đơn dùng khi thực hiện thao tác; kiểu ET_DONTHUOC.</param>
/// <param name="CacDong">Giá trị các dòng dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;ET_CHITIETDONTHUOC&gt;.</param>
/// <param name="MaBN">Mã bệnh nhân xác định đối tượng cần xử lý.</param>
/// <param name="DauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
/// <param name="DuocSua">Giá trị được sửa dùng khi thực hiện thao tác; kiểu bool.</param>
/// <param name="DuocThay">Giá trị được thay dùng khi thực hiện thao tác; kiểu bool.</param>
public sealed record BanNhapDon(ET_DONTHUOC Don,IReadOnlyList<ET_CHITIETDONTHUOC> CacDong,string MaBN,string DauVanTay,bool DuocSua,bool DuocThay);
/// <summary>
/// Gói dữ liệu lượt kê đơn truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaLS">Mã lần khám xác định đối tượng cần xử lý.</param>
/// <param name="MaBN">Mã bệnh nhân xác định đối tượng cần xử lý.</param>
/// <param name="HoTenBN">Giá trị họ tên bệnh nhân dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="MaBS">Mã bác sĩ xác định đối tượng cần xử lý.</param>
/// <param name="TrangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="ThoiGianKham">Giá trị thời gian khám dùng khi thực hiện thao tác; kiểu DateTime.</param>
public sealed record LuotKeDon(string MaLS,string MaBN,string HoTenBN,string MaBS,string TrangThai,DateTime ThoiGianKham);
/// <summary>
/// Gói dữ liệu không gian kê đơn truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="Luot">Giá trị lượt dùng khi thực hiện thao tác; kiểu LuotKeDon.</param>
/// <param name="ChanDoan">Giá trị chẩn đoán dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="DuocTao">Giá trị được tạo dùng khi thực hiện thao tác; kiểu bool.</param>
/// <param name="CacDon">Giá trị các đơn dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;ET_DONTHUOC&gt;.</param>
/// <param name="NgaySinh">Giá trị ngày sinh dùng khi thực hiện thao tác; kiểu DateTime?.</param>
/// <param name="DiUng">Giá trị dị ứng dùng khi thực hiện thao tác; kiểu string?.</param>
/// <param name="TienSuBenh">Giá trị tiền sử bệnh dùng khi thực hiện thao tác; kiểu string?.</param>
public sealed record KhongGianKeDon(LuotKeDon Luot,string ChanDoan,bool DuocTao,IReadOnlyList<ET_DONTHUOC> CacDon,DateTime? NgaySinh=null,string? DiUng=null,string? TienSuBenh=null);
/// <summary>
/// Gói dữ liệu bản in đơn thuốc truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="Nguon">Giá trị nguồn dùng khi thực hiện thao tác; kiểu BanNhapDon.</param>
/// <param name="HoTenBN">Giá trị họ tên bệnh nhân dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="TenBS">Giá trị tên bác sĩ dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="ChanDoan">Giá trị chẩn đoán dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="NgaySinh">Giá trị ngày sinh dùng khi thực hiện thao tác; kiểu DateTime?.</param>
public sealed record BanInDonThuoc(BanNhapDon Nguon,string HoTenBN,string TenBS,string ChanDoan,DateTime? NgaySinh=null);
/// <summary>
/// Gói dữ liệu bản nháp hướng dẫn thuốc truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="CauHinh">Giá trị cấu hình dùng khi thực hiện thao tác; kiểu ET_CAUHINHTHUOCKEDON.</param>
/// <param name="DauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
public sealed record BanNhapHuongDanThuoc(ET_CAUHINHTHUOCKEDON CauHinh,string DauVanTay);
