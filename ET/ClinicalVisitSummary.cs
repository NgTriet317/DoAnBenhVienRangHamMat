namespace ET;

/// <summary>
/// Gói dữ liệu dịch vụ tổng kết khám truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaDVThucHien">Mã dịch vụ thực hiện xác định đối tượng cần xử lý.</param>
/// <param name="MaDV">Mã dịch vụ xác định đối tượng cần xử lý.</param>
/// <param name="TenDV">Giá trị tên dịch vụ dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="SoLuong">Giá trị số lượng dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="DonGia">Giá trị đơn giá dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="ThanhTien">Giá trị thành tiền dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="ThoiGianThucHien">Giá trị thời gian thực hiện dùng khi thực hiện thao tác; kiểu DateTime.</param>
/// <param name="KetQua">Giá trị kết quả dùng khi thực hiện thao tác; kiểu string?.</param>
public sealed record DichVuTongKetKham(string MaDVThucHien,string MaDV,string TenDV,
    decimal SoLuong,decimal DonGia,decimal ThanhTien,DateTime ThoiGianThucHien,string? KetQua);
/// <summary>
/// Gói dữ liệu thuốc tổng kết khám truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
/// <param name="MaCTDT">Mã chi tiết đơn thuốc xác định đối tượng cần xử lý.</param>
/// <param name="MaThuoc">Mã thuốc xác định đối tượng cần xử lý.</param>
/// <param name="TenThuoc">Giá trị tên thuốc dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="HamLuong">Giá trị hàm lượng dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="DonViTinh">Giá trị đơn vị tính dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="SoLuong">Giá trị số lượng dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="LieuDung">Giá trị liều dùng dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="DuongDung">Giá trị đường dùng dùng khi thực hiện thao tác; kiểu string?.</param>
/// <param name="SoNgayDung">Giá trị số ngày dụng dùng khi thực hiện thao tác; kiểu int.</param>
/// <param name="HuongDan">Giá trị hướng dẫn dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="DonGia">Giá trị đơn giá dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="TrangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
public sealed record ThuocTongKetKham(string MaDT,string MaCTDT,string MaThuoc,string TenThuoc,
    string HamLuong,string DonViTinh,decimal SoLuong,string LieuDung,string? DuongDung,
    int SoNgayDung,string HuongDan,decimal DonGia,string TrangThai)
{
    /// <summary>
    /// Giá trị Thành tiền của dòng hoặc phần lô; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public decimal ThanhTien=>decimal.Round(SoLuong*DonGia,2,MidpointRounding.AwayFromZero);
}
/// <summary>
/// Gói dữ liệu tổng kết khám truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="DichVu">Giá trị dịch vụ dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;DichVuTongKetKham&gt;.</param>
/// <param name="Thuoc">Giá trị thuốc dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;ThuocTongKetKham&gt;.</param>
/// <param name="DaThucHien">Giá trị đã thực hiện dùng khi thực hiện thao tác; kiểu bool.</param>
/// <param name="SoDonNhap">Giá trị số đơn nhập dùng khi thực hiện thao tác; kiểu int.</param>
/// <param name="DauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
public sealed record TongKetKham(IReadOnlyList<DichVuTongKetKham> DichVu,
    IReadOnlyList<ThuocTongKetKham> Thuoc,bool DaThucHien,int SoDonNhap,string DauVanTay)
{
    /// <summary>
    /// Tổng tiền dịch vụ được cộng từ các dòng thành phần trong DichVu.
    /// </summary>
    public decimal TienDichVu=>DichVu.Sum(x=>x.ThanhTien);
    /// <summary>
    /// Tổng tiền thuốc được cộng từ các dòng thành phần trong Thuoc.
    /// </summary>
    public decimal TienThuoc=>Thuoc.Sum(x=>x.ThanhTien);
}
