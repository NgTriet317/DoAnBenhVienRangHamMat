using Dapper.Contrib.Extensions;

namespace ET;

/// <summary>Metadata ảnh theo lần khám; dữ liệu ảnh thực nằm trong kho tệp dùng chung.</summary>
[Table("ANH_XQUANG_LAN_KHAM")]
public sealed class ET_ANHXQUANGLANKHAM
{
    [ExplicitKey]
    public string MaAnh { get; set; } = "";
    public string MaLS { get; set; } = "";
    public string? MaDVThucHien { get; set; }
    public string TenTepGoc { get; set; } = "";
    public string TenTepLuu { get; set; } = "";
    public string DuongDanTuongDoi { get; set; } = "";
    public string MimeType { get; set; } = "";
    public long KichThuocBytes { get; set; }
    public string Sha256 { get; set; } = "";
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }
    public string MaNVUpload { get; set; } = "";
    public string TrangThai { get; set; } = "Đang sử dụng";
    public DateTime? NgayAn { get; set; }
}
