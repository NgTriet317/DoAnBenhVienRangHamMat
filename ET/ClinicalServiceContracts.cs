namespace ET;

public sealed record YeuCauThucHienDichVu
{
    public string? MaDVThucHien { get; init; }
    public string MaLS { get; init; } = "";
    public string? MaCTKH { get; init; }
    public string MaDV { get; init; } = "";
    public string MaNVThucHien { get; init; } = "";
    public DateTime ThoiGianThucHien { get; init; }
    public decimal SoLuong { get; init; } = 1;
    public decimal DonGia { get; init; }
    public string? KetQua { get; init; }
    public string? LyDoPhatSinh { get; init; }
    public string? DauVanTay { get; init; }
}
public sealed record BanNhapThucHien(ET_DICHVUTHUCHIEN DichVu, string DauVanTay,
    bool DuocSua, bool DuocXacNhan, bool DaTinhPhi, string? LyDoPhatSinh,
    string? TenDV=null, string? TenNguoiThucHien=null);
public sealed record LuotThucHien(string MaLS, string MaBN, string HoTenBN, string MaHS,
    string MaBS, DateTime ThoiGianKham, string TrangThai);
public sealed record NguoiThucHienDichVu(string MaNV, string HoTen);
public sealed record BuocDuKienThucHien(string MaCTKH, string MaKHDT, string MaLSSource,
    string MaDV, string TenDV, int LanThu, string? NoiDung, decimal SoLuongDuKien,
    decimal DaXacNhan, decimal DangNhap, decimal ConLai, decimal DonGiaDuKien);
public sealed record KhongGianThucHien(LuotThucHien Luot, bool DuocTao,
    IReadOnlyList<BuocDuKienThucHien> DuKien, IReadOnlyList<BanNhapThucHien> ThucTe,
    IReadOnlyList<ET_DICHVU> DanhMuc, IReadOnlyList<NguoiThucHienDichVu> NhanVien)
{
    public decimal GiaTriDaXacNhan => ThucTe.Where(s=>s.DichVu.TrangThai=="Đã xác nhận").Sum(s=>s.DichVu.ThanhTien);
}
