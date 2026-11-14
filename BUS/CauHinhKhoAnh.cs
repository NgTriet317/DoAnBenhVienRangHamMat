using System.Text.Json;
using ET;

namespace BUS;

/// <summary>Cấu hình kho chung. Không có thư mục máy trạm mặc định.</summary>
public sealed record CauHinhKhoAnh
{
    public string ThuMucDungChung { get; init; } = "";
    public bool ChoPhepDuongDanCucBo { get; init; }
    public int DungLuongToiDaMB { get; init; } = 20;
    public long DiemAnhToiDa { get; init; } = 40000000;

    /// <summary>
    /// Đọc cấu hình XQuang trong appsettings.json cạnh ứng dụng; biến RHM_XRAY_ROOT được ưu tiên cho thư mục kho.
    /// </summary>
    /// <returns>Cấu hình kho; việc kiểm tra đường dẫn và giới hạn được thực hiện khi truy cập kho.</returns>
    public static CauHinhKhoAnh Doc()
    {
        CauHinhKhoAnh config=new();
        var path=Path.Combine(AppContext.BaseDirectory,"appsettings.json");
        if(File.Exists(path))
        {
            try
            {
                using var json=JsonDocument.Parse(File.ReadAllText(path));
                if(json.RootElement.TryGetProperty("XQuang",out var section))
                    config=section.Deserialize<CauHinhKhoAnh>()??throw new JsonException();
            }
            catch(Exception ex) when(ex is JsonException or InvalidOperationException)
            {throw new LoiAnhXquangException("CauHinh","Cấu hình kho ảnh chưa hợp lệ. Liên hệ quản trị viên.",ex);}
        }
        var environment=Environment.GetEnvironmentVariable("RHM_XRAY_ROOT");
        return string.IsNullOrWhiteSpace(environment)?config:config with{ThuMucDungChung=environment};
    }
}
