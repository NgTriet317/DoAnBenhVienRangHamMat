namespace ET;

public sealed record BoLocThuoc(string? MaNhomThuoc=null,string? TuKhoa=null,string? DangBaoChe=null,string? HamLuong=null);
public sealed record ThuocKeDon(string MaThuoc,string MaHH,string TenThuoc,string HamLuong,string DonViTinh,decimal DonGiaThamKhao,decimal TonThamKhao,ET_CAUHINHTHUOCKEDON? HuongDan,string? MaNhomThuoc=null,string? TenNhomThuoc=null)
{
    public string GiaHienThi=>DonGiaThamKhao>0?DonGiaThamKhao.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))+" đ":"Chưa thiết lập giá";
}
public sealed class DongThuocKeDon
{
    public string? MaCTDT{get;set;}
    public string MaThuoc{get;set;}="";
    public string TenHienThi{get;set;}="";
    public string LieuDung{get;set;}="";
    public string DuongDung{get;set;}="";
    public string HuongDan{get;set;}="";
    public int SoNgayDung{get;set;}=1;
    public decimal SoLuong{get;set;}=1;
    public int? PhienBanHuongDan{get;set;}
    public bool DaXacNhanPhuHop{get;set;}
}
public sealed record YeuCauLuuDon
{
    public string? MaDT{get;init;}
    public string MaLS{get;init;}="";
    public string? MaDTThayThe{get;init;}
    public string? LyDoThayThe{get;init;}
    public string? GhiChu{get;init;}
    public string? DauVanTay{get;init;}
    public IReadOnlyList<DongThuocKeDon> CacDong{get;init;}=Array.Empty<DongThuocKeDon>();
}
public sealed record BanNhapDon(ET_DONTHUOC Don,IReadOnlyList<ET_CHITIETDONTHUOC> CacDong,string MaBN,string DauVanTay,bool DuocSua,bool DuocThay);
public sealed record LuotKeDon(string MaLS,string MaBN,string HoTenBN,string MaBS,string TrangThai,DateTime ThoiGianKham);
public sealed record KhongGianKeDon(LuotKeDon Luot,string ChanDoan,bool DuocTao,IReadOnlyList<ET_DONTHUOC> CacDon,DateTime? NgaySinh=null,string? DiUng=null,string? TienSuBenh=null);
public sealed record BanInDonThuoc(BanNhapDon Nguon,string HoTenBN,string TenBS,string ChanDoan,DateTime? NgaySinh=null);
public sealed record BanNhapHuongDanThuoc(ET_CAUHINHTHUOCKEDON CauHinh,string DauVanTay);
