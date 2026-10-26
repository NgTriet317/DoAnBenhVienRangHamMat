namespace ET;

public sealed record YeuCauThanhToan
{
    public string MaLS { get; init; } = "";
    public string? MaKhoCapThuoc { get; init; }
    public Guid MaYeuCau { get; init; }
    public string DauVanTayBangTinh { get; init; } = "";
    public string PhuongThuc { get; init; } = "Tiền mặt";
    public decimal SoTienKhachDua { get; init; }
    public string? MaThamChieu { get; init; }
    public bool DaDoiChieuChuyenKhoan { get; init; }
}
public sealed record DonThuocDaThu(string MaDT, string MaPX);
public sealed record KetQuaThanhToan(string MaLS, string MaHD, string? MaTT,
    IReadOnlyList<DonThuocDaThu> CacDon, decimal SoTienThu, decimal SoTienKhachDua,
    decimal TienThua, bool DaTonTai);
