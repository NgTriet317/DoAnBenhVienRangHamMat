namespace ET;

public sealed class LoiKhamException(string truong,string message) : InvalidOperationException(message)
{ public string Truong { get; } = truong; }
public sealed class XungDotDuLieuKhamException() : InvalidOperationException("Dữ liệu đã thay đổi ở cửa sổ khác. Giữ nội dung đang nhập, tải lại để đối chiếu trước khi lưu.");

public sealed record BanNhapKham(ET_LICHSUKHAMBENH LanKham,IReadOnlyList<ET_TINHTRANGBENH> TinhTrang,string DauVanTay,bool DuocSua,string? MaBN=null,string? MaHS=null);
public sealed record DuLieuKhongGianKham(BanNhapKham BanNhap,ET_BENHNHAN BenhNhan,IReadOnlyList<ET_DICHVU> DichVu,IReadOnlyList<ET_DMTINHTRANGBENH> DanhMucTinhTrang,IReadOnlyList<ET_KEHOACHDIEUTRI> KeHoach);
public sealed class TinhTrangKhamNhap
{
    public string? MaTTB {get;set;}
    public string? MaDMTT {get;set;}
    public string LoaiTinhTrang {get;set;}="";
    public string MoTaTinhTrang {get;set;}="";
    public string MucDo {get;set;}="";
    public string? ViTriRang {get;set;}
    public string? GhiChu {get;set;}
}
public sealed record YeuCauLuuKham
{
    public string MaLS {get;init;}="";
    public string DauVanTay {get;init;}="";
    public string TrieuChung {get;init;}="";
    public string ChanDoan {get;init;}="";
    public string KeHoachXuLy {get;init;}="";
    public string? GhiChu {get;init;}
    public bool KhongKeThuoc {get;init;}
    public string? LyDoKhongKeThuoc {get;init;}
    public IReadOnlyList<TinhTrangKhamNhap> TinhTrang {get;init;}=Array.Empty<TinhTrangKhamNhap>();
}
public sealed class BuocKeHoachNhap
{
    public string? MaCTKH {get;set;}
    public string MaDV {get;set;}="";
    public int LanThu {get;set;}=1;
    public decimal SoLuongDuKien {get;set;}=1;
    public decimal DonGiaDuKien {get;set;}
    public string NoiDungBuoc {get;set;}="";
    public string? GhiChu {get;set;}
}
public sealed record BanNhapKeHoach(ET_KEHOACHDIEUTRI KeHoach,IReadOnlyList<ET_CHITIETKEHOACH> CacBuoc,string DauVanTay);
public sealed record YeuCauLuuKeHoach
{
    public string MaLS {get;init;}="";
    public string? MaKHDT {get;init;}
    public string? MaTTBNguon {get;init;}
    public string? DauVanTay {get;init;}
    public DateTime? NgayBatDau {get;init;}
    public DateTime? NgayDuKienKetThuc {get;init;}
    public string? GhiChu {get;init;}
    public IReadOnlyList<BuocKeHoachNhap> CacBuoc {get;init;}=Array.Empty<BuocKeHoachNhap>();
}
public sealed record GoiYKeHoachItem(ET_TINHTRANGBENH TinhTrang,ET_MAUKEHOACHDIEUTRI Mau,IReadOnlyList<ET_CHITIETMAUKEHOACH> CacBuoc,bool KhopMucDo)
{ public string Nhan=>$"{TinhTrang.MoTaTinhTrang} ({TinhTrang.MaTTB}) → {Mau.TenMau} · v{Mau.PhienBan} · {(KhopMucDo?"khớp mức độ":"mẫu tổng quát")}"; }
public sealed record BanNhapMauDieuTri(ET_MAUKEHOACHDIEUTRI Mau,IReadOnlyList<ET_CHITIETMAUKEHOACH> CacBuoc,string DauVanTay);
public sealed record YeuCauLuuMauDieuTri
{
    public string? MaMauKH {get;init;}
    public string MaDMTT {get;init;}="";
    public string TenMau {get;init;}="";
    public string? MucDo {get;init;}
    public int PhienBan {get;init;}=1;
    public string? DauVanTay {get;init;}
    public IReadOnlyList<BuocKeHoachNhap> CacBuoc {get;init;}=Array.Empty<BuocKeHoachNhap>();
}
