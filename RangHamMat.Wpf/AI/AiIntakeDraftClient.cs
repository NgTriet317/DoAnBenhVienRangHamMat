using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace RangHamMat.AI;

public sealed class AiIntakeDraftClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient http;
    private readonly string path;

    public AiIntakeDraftClient(string apiBaseUrl, string endpointPath, int timeoutSeconds = 15)
    {
        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttp || !uri.IsLoopback)
            throw new InvalidOperationException("API bản khai chỉ được gọi qua loopback của KioskServer.");
        if (string.IsNullOrWhiteSpace(endpointPath) || Uri.TryCreate(endpointPath, UriKind.Absolute, out _))
            throw new InvalidOperationException("Đường dẫn API bản khai không hợp lệ.");
        path = endpointPath.TrimStart('/');
        http = new HttpClient { BaseAddress = uri, Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
    }

    public async Task<AiIntakeDraftResult> SubmitAsync(AiIntakeDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(path, request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string message = response.StatusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => "Thông tin bản khai chưa hợp lệ.",
                System.Net.HttpStatusCode.Conflict => "Phiên đã được nhân viên xử lý; vui lòng nhờ nhân viên hỗ trợ.",
                System.Net.HttpStatusCode.Forbidden => "API bản khai từ chối yêu cầu này.",
                System.Net.HttpStatusCode.ServiceUnavailable => "Chưa lưu được bản khai. Kiểm tra SQL17 và kết nối SQL Server.",
                (System.Net.HttpStatusCode)429 => "Đã gửi quá nhiều yêu cầu. Vui lòng đợi rồi thử lại.",
                _ => "Chưa lưu được bản khai. Vui lòng kiểm tra kết nối hoặc gặp nhân viên."
            };
            throw new InvalidOperationException(message);
        }
        var saved = await response.Content.ReadFromJsonAsync<AiIntakeDraftResult>(JsonOptions, cancellationToken);
        if (saved is null || saved.Status != "saved" || saved.MaPhien != request.SessionId
            || !System.Text.RegularExpressions.Regex.IsMatch(saved.MaRutGon, "^AI-[0-9A-F]{8}$"))
            throw new InvalidOperationException("API chưa xác nhận đã lưu bản khai.");
        return saved;
    }

    public void Dispose() => http.Dispose();
}

public sealed record AiIntakeDraftRequest(Guid SessionId, bool PatientConfirmed,
    bool RequiresStaff, AiIntakeDraftPatient Patient, AiIntakeDraftVisit Visit);
public sealed record AiIntakeDraftPatient(string? FullName, DateOnly? DateOfBirth,
    string? Phone, string? PreviousVisit);
public sealed record AiIntakeDraftVisit(string? Reason, string? ExamType);
public sealed class AiIntakeDraftResult
{
    public string Status { get; set; } = "";
    public string MaRutGon { get; set; } = "";
    public Guid MaPhien { get; set; }
}
