namespace ET;

/// <summary>
/// Ngoại lệ lỗi nhập liệu khám để nơi gọi nhận diện lỗi và phản hồi phù hợp cho người dùng.
/// </summary>
public sealed class LoiKhamException : InvalidOperationException
{
    /// <summary>
    /// Tên trường dữ liệu liên quan đến lỗi để giao diện phản hồi đúng ô nhập.
    /// </summary>
    public string Truong { get; }
    /// <summary>
    /// Khởi tạo lỗi nhập liệu khám. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
    /// </summary>
    /// <param name="truong">Giá trị trường dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="message">Nội dung thông báo được chuyển đến nơi gọi hoặc hiển thị cho người dùng.</param>
    public LoiKhamException(string truong,string message) : base(message) => Truong=truong;
    /// <summary>
    /// Khởi tạo lỗi nhập liệu khám. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
    /// </summary>
    /// <param name="truong">Giá trị trường dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="message">Nội dung thông báo được chuyển đến nơi gọi hoặc hiển thị cho người dùng.</param>
    /// <param name="inner">Giá trị ngoại lệ gốc dùng khi thực hiện thao tác; kiểu Exception.</param>
    public LoiKhamException(string truong,string message,Exception inner) : base(message,inner) => Truong=truong;
}
/// <summary>
/// Ngoại lệ xung đột dữ liệu khám để nơi gọi nhận diện lỗi và phản hồi phù hợp cho người dùng.
/// </summary>
public sealed class XungDotDuLieuKhamException : InvalidOperationException
{
    /// <summary>
    /// Khởi tạo xung đột dữ liệu khám.
    /// </summary>
    public XungDotDuLieuKhamException():base("Dữ liệu đã thay đổi ở cửa sổ khác. Giữ nội dung đang nhập, tải lại để đối chiếu trước khi lưu."){}
    /// <summary>
    /// Khởi tạo xung đột dữ liệu khám. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
    /// </summary>
    /// <param name="message">Nội dung thông báo được chuyển đến nơi gọi hoặc hiển thị cho người dùng.</param>
    public XungDotDuLieuKhamException(string message):base(message){}
}

/// <summary>
/// Gói dữ liệu bản nháp khám truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="LanKham">Giá trị lần khám dùng khi thực hiện thao tác; kiểu ET_LICHSUKHAMBENH.</param>
/// <param name="TinhTrang">Giá trị tình trạng dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;ET_TINHTRANGBENH&gt;.</param>
/// <param name="DauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
/// <param name="DuocSua">Giá trị được sửa dùng khi thực hiện thao tác; kiểu bool.</param>
/// <param name="MaBN">Mã bệnh nhân xác định đối tượng cần xử lý.</param>
/// <param name="MaHS">Mã hồ sơ xác định đối tượng cần xử lý.</param>
/// <param name="MaHD">Mã hóa đơn xác định đối tượng cần xử lý.</param>
public sealed record BanNhapKham(ET_LICHSUKHAMBENH LanKham,IReadOnlyList<ET_TINHTRANGBENH> TinhTrang,string DauVanTay,bool DuocSua,string? MaBN=null,string? MaHS=null,string? MaHD=null);
/// <summary>
/// Gói dữ liệu dữ liệu không gian khám truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="BanNhap">Giá trị bản nháp dùng khi thực hiện thao tác; kiểu BanNhapKham.</param>
/// <param name="BenhNhan">Giá trị bệnh nhân dùng khi thực hiện thao tác; kiểu ET_BENHNHAN.</param>
/// <param name="DichVu">Giá trị dịch vụ dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;ET_DICHVU&gt;.</param>
/// <param name="KeHoach">Giá trị kế hoạch dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;ET_KEHOACHDIEUTRI&gt;.</param>
/// <param name="DinhHuong">Giá trị định hướng dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;ET_DMTINHTRANGBENH&gt;.</param>
/// <param name="DichVuTheoTinhTrang">Giá trị dịch vụ theo tình trạng dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;DichVuTinhTrang&gt;.</param>
public sealed record DuLieuKhongGianKham(BanNhapKham BanNhap,ET_BENHNHAN BenhNhan,IReadOnlyList<ET_DICHVU> DichVu,IReadOnlyList<ET_KEHOACHDIEUTRI> KeHoach,IReadOnlyList<ET_DMTINHTRANGBENH> DinhHuong,IReadOnlyList<DichVuTinhTrang> DichVuTheoTinhTrang);
/// <summary>
/// Mô hình và thao tác hỗ trợ dịch vụ tình trạng dùng bởi các thành phần liên quan.
/// </summary>
public sealed class DichVuTinhTrang
{
    /// <summary>
    /// Mã tình trạng trong danh mục tham khảo.
    /// </summary>
    public string MaDMTT {get;set;}="";
    /// <summary>
    /// Mã dịch vụ.
    /// </summary>
    public string MaDV {get;set;}="";
    /// <summary>
    /// Tên dịch vụ.
    /// </summary>
    public string TenDV {get;set;}="";
    /// <summary>
    /// Đơn giá áp dụng cho dòng dữ liệu.
    /// </summary>
    public decimal DonGia {get;set;}
    /// <summary>
    /// Nội dung để xuất.
    /// </summary>
    public string NoiDungDeXuat {get;set;}="";
    /// <summary>
    /// Thứ tự sắp xếp mục trong danh sách.
    /// </summary>
    public int ThuTu {get;set;}
}
/// <summary>
/// Mô hình và thao tác hỗ trợ tình trạng khám nhập dùng bởi các thành phần liên quan.
/// </summary>
public sealed class TinhTrangKhamNhap
{
    /// <summary>
    /// Mã tình trạng bệnh ghi nhận trong lần khám. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaTTB {get;set;}
    /// <summary>
    /// Loại tình trạng.
    /// </summary>
    public string LoaiTinhTrang {get;set;}="";
    /// <summary>
    /// Mô tả tình trạng.
    /// </summary>
    public string MoTaTinhTrang {get;set;}="";
    /// <summary>
    /// Nội dung ghi chú bổ sung của bản ghi. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? GhiChu {get;set;}
}
/// <summary>
/// Gói dữ liệu yêu cầu lưu khám truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
public sealed record YeuCauLuuKham
{
    /// <summary>
    /// Mã lần khám liên kết các chứng từ cùng lượt.
    /// </summary>
    public string MaLS {get;init;}="";
    /// <summary>
    /// Dấu vân tay bản dữ liệu phục vụ đối chiếu khi lưu/xác nhận.
    /// </summary>
    public string DauVanTay {get;init;}="";
    /// <summary>
    /// Triệu chứng được ghi trong lần khám.
    /// </summary>
    public string TrieuChung {get;init;}="";
    /// <summary>
    /// Chẩn đoán của bác sĩ trong lần khám.
    /// </summary>
    public string ChanDoan {get;init;}="";
    /// <summary>
    /// Hướng xử trí được ghi cho lần khám.
    /// </summary>
    public string KeHoachXuLy {get;init;}="";
    /// <summary>
    /// Nội dung ghi chú bổ sung của bản ghi. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? GhiChu {get;init;}
    /// <summary>
    /// Dấu hiệu lựa chọn không kê thuốc cho lần khám.
    /// </summary>
    public bool KhongKeThuoc {get;init;}
    /// <summary>
    /// Lý do không kê thuốc được ghi nhận cho lần khám. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? LyDoKhongKeThuoc {get;init;}
    /// <summary>
    /// Tình trạng.
    /// </summary>
    public IReadOnlyList<TinhTrangKhamNhap> TinhTrang {get;init;}=Array.Empty<TinhTrangKhamNhap>();
}
/// <summary>
/// Mô hình và thao tác hỗ trợ bước kế hoạch nhập dùng bởi các thành phần liên quan.
/// </summary>
public sealed class BuocKeHoachNhap : System.ComponentModel.INotifyPropertyChanged
{
    /// <summary>
    /// Thông báo thay đổi thuộc tính cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    /// <summary>
    /// Số lượng đơn giá được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private decimal quantity=1,price;
    /// <summary>
    /// Tên dịch vụ.
    /// </summary>
    public string TenDV {get;set;}="";
    /// <summary>
    /// Mã dòng chi tiết kế hoạch điều trị. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaCTKH {get;set;}
    /// <summary>
    /// Mã dịch vụ.
    /// </summary>
    public string MaDV {get;set;}="";
    /// <summary>
    /// Số lần hoặc số bước tương ứng trong kế hoạch.
    /// </summary>
    public int LanThu {get;set;}=1;
    /// <summary>
    /// Giá trị Số lượng dự kiến của bước kế hoạch; được xác định bởi phần getter/setter của thuộc tính. Phát PropertyChanged khi gán để binding cập nhật nội dung liên quan.
    /// </summary>
    public decimal SoLuongDuKien {get=>quantity;set{quantity=value;PropertyChanged?.Invoke(this,new(nameof(SoLuongDuKien)));}}
    /// <summary>
    /// Giá trị Đơn giá dự kiến của bước kế hoạch; được xác định bởi phần getter/setter của thuộc tính. Phát PropertyChanged khi gán để binding cập nhật nội dung liên quan.
    /// </summary>
    public decimal DonGiaDuKien {get=>price;set{price=value;PropertyChanged?.Invoke(this,new(nameof(DonGiaDuKien)));}}
    /// <summary>
    /// Nội dung bước.
    /// </summary>
    public string NoiDungBuoc {get;set;}="";
    /// <summary>
    /// Nội dung ghi chú bổ sung của bản ghi. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? GhiChu {get;set;}
}
/// <summary>
/// Gói dữ liệu bản nháp kế hoạch truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="KeHoach">Giá trị kế hoạch dùng khi thực hiện thao tác; kiểu ET_KEHOACHDIEUTRI.</param>
/// <param name="CacBuoc">Giá trị các bước dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;ET_CHITIETKEHOACH&gt;.</param>
/// <param name="DauVanTay">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
public sealed record BanNhapKeHoach(ET_KEHOACHDIEUTRI KeHoach,IReadOnlyList<ET_CHITIETKEHOACH> CacBuoc,string DauVanTay);
/// <summary>
/// Gói dữ liệu yêu cầu lưu kế hoạch truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
public sealed record YeuCauLuuKeHoach
{
    /// <summary>
    /// Mã lần khám liên kết các chứng từ cùng lượt.
    /// </summary>
    public string MaLS {get;init;}="";
    /// <summary>
    /// Mã kế hoạch điều trị. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaKHDT {get;init;}
    /// <summary>
    /// Mã tình trạng bệnh làm nguồn lập kế hoạch điều trị. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaTTBNguon {get;init;}
    /// <summary>
    /// Dấu vân tay bản dữ liệu phục vụ đối chiếu khi lưu/xác nhận. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? DauVanTay {get;init;}
    /// <summary>
    /// Ngày bắt đầu. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public DateTime? NgayBatDau {get;init;}
    /// <summary>
    /// Ngày dự kiến kết thúc. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public DateTime? NgayDuKienKetThuc {get;init;}
    /// <summary>
    /// Nội dung ghi chú bổ sung của bản ghi. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? GhiChu {get;init;}
    /// <summary>
    /// Các bước.
    /// </summary>
    public IReadOnlyList<BuocKeHoachNhap> CacBuoc {get;init;}=Array.Empty<BuocKeHoachNhap>();
}
