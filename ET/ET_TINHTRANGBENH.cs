using Dapper.Contrib.Extensions;

namespace ET;

/// <summary>
/// Dữ liệu tình trạng bệnh trao đổi giữa các tầng và ánh xạ với kết quả truy vấn SQL.
/// </summary>
[Table("TINH_TRANG_BENH")]
public class ET_TINHTRANGBENH
{
    /// <summary>
    /// Mã tình trạng bệnh ghi nhận trong lần khám.
    /// </summary>
    [ExplicitKey] public string MaTTB {get;set;}="";
    /// <summary>
    /// Mã lần khám liên kết các chứng từ cùng lượt.
    /// </summary>
    public string MaLS {get;set;}="";
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
    // Chỉ bảo toàn liên kết danh mục cũ; giao diện mới không yêu cầu danh mục.
    public string? MaDMTT {get;set;}
    /// <summary>
    /// Khởi tạo tình trạng bệnh.
    /// </summary>
    public ET_TINHTRANGBENH(){}
    /// <summary>
    /// Khởi tạo tình trạng bệnh. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
    /// </summary>
    /// <param name="maTTB">Mã tình trạng bệnh xác định đối tượng cần xử lý.</param>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="loaiTinhTrang">Giá trị loại tình trạng dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="moTaTinhTrang">Giá trị mô tả tình trạng dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="ghiChu">Giá trị ghi chú dùng khi thực hiện thao tác; kiểu string?.</param>
    public ET_TINHTRANGBENH(string maTTB,string maLS,string loaiTinhTrang,string moTaTinhTrang,string? ghiChu)
    {MaTTB=maTTB;MaLS=maLS;LoaiTinhTrang=loaiTinhTrang;MoTaTinhTrang=moTaTinhTrang;GhiChu=ghiChu;}
}
