using System.IO;
using System.Text.Json;

namespace RangHamMat.Services;

internal sealed record CauHinhDongBo(bool Bat, TimeSpan ChuKy)
{
    public static CauHinhDongBo Read()
    {
        var path=Path.Combine(AppContext.BaseDirectory,"appsettings.json");
        if(!File.Exists(path))return new(true,TimeSpan.FromSeconds(2));
        using var json=JsonDocument.Parse(File.ReadAllText(path));
        if(!json.RootElement.TryGetProperty("DongBo",out var section))return new(true,TimeSpan.FromSeconds(2));
        var enabled=!section.TryGetProperty("Bat",out var flag)||flag.GetBoolean();
        var seconds=section.TryGetProperty("ChuKyGiay",out var interval)?interval.GetDouble():2;
        if(!double.IsFinite(seconds)||seconds<1||seconds>60)throw new InvalidOperationException("DongBo:ChuKyGiay phải từ 1 đến 60 giây.");
        return new(enabled,TimeSpan.FromSeconds(seconds));
    }
}
