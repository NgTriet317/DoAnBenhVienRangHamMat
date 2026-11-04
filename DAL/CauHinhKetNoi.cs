using System.Text.Json;

namespace DAL;

/// <summary>Cấu hình chung cho mọi DAL; biến môi trường ưu tiên hơn file cạnh ứng dụng.</summary>
public static class CauHinhKetNoi
{
    /// <summary>
    /// Đọc chuỗi kết nối theo thứ tự ưu tiên cấu hình và trả giá trị dùng mở SQL Server.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Environment.GetEnvironmentVariable</c>, <c>Path.Combine</c>, <c>File.Exists</c>, <c>JsonDocument.Parse</c>.</remarks>
    /// <returns>Chuỗi kết quả của thao tác lấy chuỗi kết nối.</returns>
    public static string LayChuoiKetNoi()
    {
        var environment=Environment.GetEnvironmentVariable("RHM_CONNECTION_STRING");
        if(!string.IsNullOrWhiteSpace(environment))return environment;
        var path=Path.Combine(AppContext.BaseDirectory,"appsettings.json");
        if(!File.Exists(path))return "Data Source=.;Initial Catalog=QLBenhVienRangHamMat;Integrated Security=True;TrustServerCertificate=True;";
        try
        {
            using var document=JsonDocument.Parse(File.ReadAllText(path));
            var value=document.RootElement.GetProperty("ConnectionStrings").GetProperty("RangHamMat").GetString();
            if(string.IsNullOrWhiteSpace(value))throw new JsonException();
            return value;
        }
        catch(Exception ex) when(ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {throw new InvalidOperationException("Kiểm tra ConnectionStrings:RangHamMat trong appsettings.json cạnh ứng dụng.",ex);}
    }
}
