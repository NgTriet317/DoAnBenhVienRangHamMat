namespace ET;

// Chỉ dữ liệu tài chính; không chuyển kết quả khám hoặc chỉ định dùng thuốc cho quầy thu.
public sealed record DongPhiDichVu(string MaDVThucHien, string MaDV, string NoiDung,
    decimal SoLuong, decimal DonGia, decimal ThanhTien);
public sealed record PhanLoTinhPhi(string MaLo, string SoLo, decimal SoLuong, decimal DonGia, decimal ThanhTien);
public sealed record DongPhiThuoc(string MaDT, string MaCTDT, string MaThuoc, string NoiDung,
    string DonViTinh, decimal SoLuong, decimal DonGia, IReadOnlyList<PhanLoTinhPhi> CacLo)
{
    public decimal ThanhTien => CacLo.Sum(l => l.ThanhTien);
}
public sealed record HoaDonDoiChieu(string MaHD, string TrangThai, int PhienBanQuyTrinh,
    decimal TongTien, decimal GiamGia, decimal PhaiThu, decimal DaThu, decimal ConPhaiThu);
public sealed record BangTinhPhi(string MaLS, string MaBN, string HoTenBN, string? MaKhoCapThuoc,
    IReadOnlyList<DongPhiDichVu> DichVu, IReadOnlyList<DongPhiThuoc> Thuoc,
    IReadOnlyList<HoaDonDoiChieu> HoaDonCu, string? DauVanTay, bool DuocThanhToan, string ThongBao)
{
    public decimal TienDichVu => DichVu.Sum(d => d.ThanhTien);
    public decimal TienThuoc => Thuoc.Sum(d => d.ThanhTien);
    public decimal TongTien => TienDichVu + TienThuoc;
}
