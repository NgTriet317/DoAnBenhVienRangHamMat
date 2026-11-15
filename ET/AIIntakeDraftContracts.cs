namespace ET;

public sealed record YeuCauLuuBanKhaiAI(
    Guid MaPhien, bool DongY, string? HoTen, DateOnly? NgaySinh,
    string? SoDienThoai, bool? DaTungKham, string? LyDoKham,
    string? TrieuChung, string? LoaiKham, bool CanHoTroGap);

public sealed record KetQuaLuuBanKhaiAI(string Status, string MaRutGon, Guid MaPhien);
