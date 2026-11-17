using System;

namespace ET;

/// <summary>Phòng khám thật, trạng thái chờ được tra từ SQL Server.</summary>
public sealed class ET_AI_PHONG_LETAN
{
    public string MaPhong { get; set; } = "";
    public string TenPhong { get; set; } = "";
    public string MaKhoa { get; set; } = "";
    public int SoDangCho { get; set; }
}

public sealed class ET_AI_PHIEN_LETAN
{
    public string TrangThai { get; set; } = "";
    public string HoTen { get; set; } = "";
    public DateTime? NgaySinh { get; set; }
    public string SoDienThoai { get; set; } = "";
    public string LyDoKham { get; set; } = "";
}

public sealed class ET_AI_BANGGIA_LETAN
{
    public string TenDV { get; set; } = "";
    public decimal DonGia { get; set; }
}

public sealed class ET_AI_CHO_LETAN
{
    public string TenPhong { get; set; } = "";
    public int SoCho { get; set; }
}
