namespace ET;

/// <summary>
/// Gói dữ liệu bộ lọc bàn giao truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaBN">Mã bệnh nhân xác định đối tượng cần xử lý.</param>
/// <param name="MaLS">Mã lần khám xác định đối tượng cần xử lý.</param>
/// <param name="TuKhoa">Giá trị từ khóa dùng khi thực hiện thao tác; kiểu string?.</param>
/// <param name="TrangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="Trang">Giá trị trạng dùng khi thực hiện thao tác; kiểu int.</param>
/// <param name="KichThuocTrang">Giá trị kích thước trang dùng khi thực hiện thao tác; kiểu int.</param>
public sealed record BoLocBanGiao(string? MaBN=null,string? MaLS=null,string? TuKhoa=null,
    string TrangThai="Chờ giao",int Trang=1,int KichThuocTrang=25);
/// <summary>
/// Gói dữ liệu hàng bàn giao truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
/// <param name="MaLS">Mã lần khám xác định đối tượng cần xử lý.</param>
/// <param name="MaBN">Mã bệnh nhân xác định đối tượng cần xử lý.</param>
/// <param name="HoTenBN">Giá trị họ tên bệnh nhân dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="NgaySinh">Giá trị ngày sinh dùng khi thực hiện thao tác; kiểu DateTime?.</param>
/// <param name="NgayKe">Giá trị ngày kê dùng khi thực hiện thao tác; kiểu DateTime.</param>
/// <param name="MaHD">Mã hóa đơn xác định đối tượng cần xử lý.</param>
/// <param name="MaPX">Mã phiếu xuất xác định đối tượng cần xử lý.</param>
/// <param name="TrangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="MaNVGiao">Mã nhân viên giao xác định đối tượng cần xử lý.</param>
/// <param name="TenNguoiGiao">Giá trị tên người giao dùng khi thực hiện thao tác; kiểu string?.</param>
/// <param name="ThoiGianGiao">Giá trị thời gian giao dùng khi thực hiện thao tác; kiểu DateTime?.</param>
public sealed record HangBanGiao(string MaDT,string MaLS,string MaBN,string HoTenBN,DateTime? NgaySinh,
    DateTime NgayKe,string MaHD,string MaPX,string TrangThai,string? MaNVGiao,string? TenNguoiGiao,DateTime? ThoiGianGiao);
/// <summary>
/// Gói dữ liệu hàng đợi bàn giao truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="CacDon">Giá trị các đơn dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;HangBanGiao&gt;.</param>
/// <param name="TongSo">Giá trị tổng số dùng khi thực hiện thao tác; kiểu int.</param>
/// <param name="Trang">Giá trị trạng dùng khi thực hiện thao tác; kiểu int.</param>
/// <param name="KichThuocTrang">Giá trị kích thước trang dùng khi thực hiện thao tác; kiểu int.</param>
public sealed record HangDoiBanGiao(IReadOnlyList<HangBanGiao> CacDon,int TongSo,int Trang,int KichThuocTrang);
/// <summary>
/// Gói dữ liệu chi tiết bàn giao truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="Luot">Giá trị lượt dùng khi thực hiện thao tác; kiểu HangBanGiao.</param>
/// <param name="Don">Giá trị đơn dùng khi thực hiện thao tác; kiểu BanInDonThuoc.</param>
public sealed record ChiTietBanGiao(HangBanGiao Luot,BanInDonThuoc Don);
/// <summary>
/// Gói dữ liệu kết quả bàn giao truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
/// <param name="MaLS">Mã lần khám xác định đối tượng cần xử lý.</param>
/// <param name="MaHD">Mã hóa đơn xác định đối tượng cần xử lý.</param>
/// <param name="MaPX">Mã phiếu xuất xác định đối tượng cần xử lý.</param>
/// <param name="MaNVGiao">Mã nhân viên giao xác định đối tượng cần xử lý.</param>
/// <param name="ThoiGianGiao">Giá trị thời gian giao dùng khi thực hiện thao tác; kiểu DateTime.</param>
/// <param name="DaTonTai">Giá trị đã tồn tại dùng khi thực hiện thao tác; kiểu bool.</param>
/// <param name="DaGiaoHet">Giá trị đã giao hết dùng khi thực hiện thao tác; kiểu bool.</param>
public sealed record KetQuaBanGiao(string MaDT,string MaLS,string MaHD,string MaPX,string MaNVGiao,
    DateTime ThoiGianGiao,bool DaTonTai,bool DaGiaoHet);
/// <summary>
/// Gói dữ liệu dòng in hóa đơn truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaCTHD">Mã chi tiết hóa đơn xác định đối tượng cần xử lý.</param>
/// <param name="MaDVThucHien">Mã dịch vụ thực hiện xác định đối tượng cần xử lý.</param>
/// <param name="MaCTPX">Mã chi tiết phiếu xuất xác định đối tượng cần xử lý.</param>
/// <param name="MaCTDT">Mã chi tiết đơn thuốc xác định đối tượng cần xử lý.</param>
/// <param name="MaDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
/// <param name="NoiDung">Giá trị nội dung dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="DonViTinh">Giá trị đơn vị tính dùng khi thực hiện thao tác; kiểu string?.</param>
/// <param name="SoLuong">Giá trị số lượng dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="DonGia">Giá trị đơn giá dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="ThanhTien">Giá trị thành tiền dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="MaLo">Mã lô xác định đối tượng cần xử lý.</param>
/// <param name="SoLo">Giá trị số lô dùng khi thực hiện thao tác; kiểu string?.</param>
public sealed record DongInHoaDon(string MaCTHD,string? MaDVThucHien,string? MaCTPX,string? MaCTDT,string? MaDT,
    string NoiDung,string? DonViTinh,decimal SoLuong,decimal DonGia,decimal ThanhTien,string? MaLo,string? SoLo);
/// <summary>
/// Gói dữ liệu bản in hóa đơn truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="HoaDon">Giá trị hóa đơn dùng khi thực hiện thao tác; kiểu ET_HOADON.</param>
/// <param name="MaBN">Mã bệnh nhân xác định đối tượng cần xử lý.</param>
/// <param name="HoTenBN">Giá trị họ tên bệnh nhân dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="NguoiLap">Giá trị người lập dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="DaThu">Giá trị đã thu dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="ConPhaiThu">Giá trị còn phải thu dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="CacDong">Giá trị các dòng dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;DongInHoaDon&gt;.</param>
/// <param name="SoTienKhachDua">Giá trị số tiền khách đưa dùng khi thực hiện thao tác; kiểu decimal?.</param>
/// <param name="TienThua">Giá trị tiền thừa dùng khi thực hiện thao tác; kiểu decimal?.</param>
/// <param name="PhuongThuc">Giá trị phương thức dùng khi thực hiện thao tác; kiểu string?.</param>
/// <param name="MaThamChieu">Mã tham chiếu xác định đối tượng cần xử lý.</param>
public sealed record BanInHoaDon(ET_HOADON HoaDon,string MaBN,string HoTenBN,string NguoiLap,
    decimal DaThu,decimal ConPhaiThu,IReadOnlyList<DongInHoaDon> CacDong,
    decimal? SoTienKhachDua,decimal? TienThua,string? PhuongThuc,string? MaThamChieu);
