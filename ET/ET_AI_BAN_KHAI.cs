using System;

namespace ET;

// Bản khai do bệnh nhân cung cấp, chưa phải bệnh án hay phiếu khám chính thức.
public sealed class ET_AI_BAN_KHAI
{
    public Guid MaPhien { get; set; }
    public string HoTen { get; set; } = "";
    public DateTime? NgaySinh { get; set; }
    public string SoDienThoai { get; set; } = "";
    public bool? DaTungKham { get; set; }
    public string LyDoKham { get; set; } = "";
    public string? TrieuChung { get; set; }
    public string? LoaiKham { get; set; }
    public bool CanHoTroGap { get; set; }
}
