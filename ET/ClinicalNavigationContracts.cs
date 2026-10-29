namespace ET;
/// <summary>Định danh chuyển màn; DAL xác minh lại mọi liên kết và suy các khóa cha.</summary>
public sealed record NguCanhHoSo
{
    public string? MaBN {get;init;}
    public string? MaHS {get;init;}
    public string? MaPhieu {get;init;}
    public string? MaLS {get;init;}
    public string? MaDT {get;init;}
    public string? MaHD {get;init;}
}
public sealed record BoLocDanhSachHoSo
{
    public string? TuKhoa {get;init;}
    public string? GioiTinh {get;init;}
    public string? TrangThai {get;init;}
    public string? MaBN {get;init;}
    public string? MaLS {get;init;}
    public DateTime? TuNgay {get;init;}
    public DateTime? DenNgay {get;init;}
    public int Trang {get;init;}=1;
    public int KichThuocTrang {get;init;}=25;
    public bool ChiCuaToi {get;init;}=true;
}
public sealed record TrangHoSo<T>(IReadOnlyList<T> CacDong,int TongSo,int Trang,int KichThuocTrang);
public sealed class DongBenhNhanDanhSach:ET_BENHNHAN {public DateTime? LanKhamGanNhat {get;set;}}
public sealed class DongLanKhamDanhSach:ET_LICHSUKHAMBENH
{
    public string MaBN {get;set;}="";
    public string HoTenBN {get;set;}="";
    public string TenBS {get;set;}="";
    public string NguonTiepNhan {get;set;}="";
}
public sealed class DongLichSuDon:ET_DONTHUOC
{
    public string MaBN {get;set;}="";
    public string HoTenBN {get;set;}="";
    public string? SDT {get;set;}
    public DateTime? NgaySinh {get;set;}
    public DateTime ThoiGianKham {get;set;}
    public string TenBS {get;set;}="";
    public string ChanDoan {get;set;}="";
    public IReadOnlyList<ET_CHITIETDONTHUOC> CacDong {get;set;}=Array.Empty<ET_CHITIETDONTHUOC>();
}
