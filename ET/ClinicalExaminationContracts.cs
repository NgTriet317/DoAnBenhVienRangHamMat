namespace ET;

public sealed class LoiKhamException : InvalidOperationException
{
    public string Truong { get; }
    public LoiKhamException(string truong,string message) : base(message) => Truong=truong;
    public LoiKhamException(string truong,string message,Exception inner) : base(message,inner) => Truong=truong;
}
public sealed class XungDotDuLieuKhamException : InvalidOperationException
{
    public XungDotDuLieuKhamException():base("Dữ liệu đã thay đổi ở cửa sổ khác. Giữ nội dung đang nhập, tải lại để đối chiếu trước khi lưu."){}
    public XungDotDuLieuKhamException(string message):base(message){}
}

public sealed record BanNhapKham(ET_LICHSUKHAMBENH LanKham,IReadOnlyList<ET_TINHTRANGBENH> TinhTrang,string DauVanTay,bool DuocSua,string? MaBN=null,string? MaHS=null);
public sealed record DuLieuKhongGianKham(BanNhapKham BanNhap,ET_BENHNHAN BenhNhan,IReadOnlyList<ET_DICHVU> DichVu,IReadOnlyList<ET_KEHOACHDIEUTRI> KeHoach,IReadOnlyList<ET_DMTINHTRANGBENH> DinhHuong,IReadOnlyList<DichVuTinhTrang> DichVuTheoTinhTrang);
public sealed class DichVuTinhTrang
{
    public string MaDMTT {get;set;}="";
    public string MaDV {get;set;}="";
    public string TenDV {get;set;}="";
    public decimal DonGia {get;set;}
    public string NoiDungDeXuat {get;set;}="";
    public int ThuTu {get;set;}
}
public sealed class TinhTrangKhamNhap
{
    public string? MaTTB {get;set;}
    public string LoaiTinhTrang {get;set;}="";
    public string MoTaTinhTrang {get;set;}="";
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
public sealed class BuocKeHoachNhap : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private decimal quantity=1,price;
    public string TenDV {get;set;}="";
    public string? MaCTKH {get;set;}
    public string MaDV {get;set;}="";
    public int LanThu {get;set;}=1;
    public decimal SoLuongDuKien {get=>quantity;set{quantity=value;PropertyChanged?.Invoke(this,new(nameof(SoLuongDuKien)));}}
    public decimal DonGiaDuKien {get=>price;set{price=value;PropertyChanged?.Invoke(this,new(nameof(DonGiaDuKien)));}}
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
