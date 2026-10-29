using Dapper.Contrib.Extensions;

namespace ET;

[Table("TINH_TRANG_BENH")]
public class ET_TINHTRANGBENH
{
    [ExplicitKey] public string MaTTB {get;set;}="";
    public string MaLS {get;set;}="";
    public string LoaiTinhTrang {get;set;}="";
    public string MoTaTinhTrang {get;set;}="";
    public string? GhiChu {get;set;}
    // Chỉ bảo toàn liên kết danh mục cũ; giao diện mới không yêu cầu danh mục.
    public string? MaDMTT {get;set;}
    public ET_TINHTRANGBENH(){}
    public ET_TINHTRANGBENH(string maTTB,string maLS,string loaiTinhTrang,string moTaTinhTrang,string? ghiChu)
    {MaTTB=maTTB;MaLS=maLS;LoaiTinhTrang=loaiTinhTrang;MoTaTinhTrang=moTaTinhTrang;GhiChu=ghiChu;}
}
