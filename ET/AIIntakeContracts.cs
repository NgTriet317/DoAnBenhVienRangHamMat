namespace ET;

public sealed class KetQuaTaoPhieuAI
{
    public string Status { get; set; } = "";
    public string MaPhieu { get; set; } = "";
    public int SoThuTu { get; set; }
    public string? MaPhong { get; set; }
    public string? MaBS { get; set; }
    public string? Phong { get; set; }
    public string? BacSi { get; set; }
    public DateTimeOffset? ThoiGianTiepNhan { get; set; }
    public string? TrangThai { get; set; }
    public bool RequiresStaff { get; set; }
}

public sealed class LuaChonTiepNhanAI
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? RoomId { get; set; }
    public string? RoomName { get; set; }
}
