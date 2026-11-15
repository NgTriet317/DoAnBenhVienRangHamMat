using System;

namespace ET;

// Chỉ dùng cho danh sách nhân viên xem, không trả về trình duyệt bệnh nhân.
public class ET_AITIEPNHAN
{
    public Guid MaPhien { get; set; }
    public string MaRutGon => "AI-" + MaPhien.ToString("N")[..8].ToUpperInvariant();
    public string TrangThai { get; set; } = "";
    public string? HoTen { get; set; }
    public DateTime? NgaySinh { get; set; }
    public string? SoDienThoai { get; set; }
    public bool? DaTungKham { get; set; }
    public string? LyDoKham { get; set; }
    public string? TrieuChung { get; set; }
    public bool CanHoTroGap { get; set; }
    public string? LoaiKham { get; set; }
    public DateTime NgayTao { get; set; }
    public string? MaPhieu { get; set; }
    public int? SoThuTu { get; set; }
}
