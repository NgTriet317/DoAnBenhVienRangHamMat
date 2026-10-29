namespace ET;

public sealed record BoLocBanGiao(string? MaBN=null,string? MaLS=null,string? TuKhoa=null,
    string TrangThai="Chờ giao",int Trang=1,int KichThuocTrang=25);
public sealed record HangBanGiao(string MaDT,string MaLS,string MaBN,string HoTenBN,DateTime? NgaySinh,
    DateTime NgayKe,string MaHD,string MaPX,string TrangThai,string? MaNVGiao,string? TenNguoiGiao,DateTime? ThoiGianGiao);
public sealed record HangDoiBanGiao(IReadOnlyList<HangBanGiao> CacDon,int TongSo,int Trang,int KichThuocTrang);
public sealed record ChiTietBanGiao(HangBanGiao Luot,BanInDonThuoc Don);
public sealed record KetQuaBanGiao(string MaDT,string MaLS,string MaHD,string MaPX,string MaNVGiao,
    DateTime ThoiGianGiao,bool DaTonTai,bool DaGiaoHet);
public sealed record DongInHoaDon(string MaCTHD,string? MaDVThucHien,string? MaCTPX,string? MaCTDT,string? MaDT,
    string NoiDung,string? DonViTinh,decimal SoLuong,decimal DonGia,decimal ThanhTien,string? MaLo,string? SoLo);
public sealed record BanInHoaDon(ET_HOADON HoaDon,string MaBN,string HoTenBN,string NguoiLap,
    decimal DaThu,decimal ConPhaiThu,IReadOnlyList<DongInHoaDon> CacDong,
    decimal? SoTienKhachDua,decimal? TienThua,string? PhuongThuc,string? MaThamChieu);
