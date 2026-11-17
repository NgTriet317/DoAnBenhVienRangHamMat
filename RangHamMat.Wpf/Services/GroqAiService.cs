using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RangHamMat.Models;

namespace RangHamMat.Services;

public sealed class GroqUnavailableException : Exception
{
    public GroqUnavailableException(string message) : base(message) { }
}

public sealed record GroqTurn(string? Text, GroqToolCall? ToolCall, GroqChatMessage AssistantMessage);

public sealed class GroqAiService
{
    private static readonly HttpClient Http = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly GroqSettings _settings;
    private readonly string _instructions;
    private readonly JsonElement _tools;

    public GroqAiService()
    {
        string root = AppContext.BaseDirectory;
        string config = Path.Combine(root, "AI", "Config", "ReceptionistSettings.json");
        _settings = File.Exists(config)
            ? JsonSerializer.Deserialize<GroqSettings>(File.ReadAllText(config), new JsonSerializerOptions
              { PropertyNameCaseInsensitive = true }) ?? new GroqSettings()
            : new GroqSettings();
        if (!Uri.TryCreate(_settings.Endpoint, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme != Uri.UriSchemeHttps || endpoint.Host != "api.groq.com"
            || _settings.MaxRecentMessages != 6 || _settings.TimeoutSeconds != 8
            || _settings.MaxCompletionTokens is < 128 or > 1000)
            throw new InvalidOperationException("Cấu hình Groq lễ tân không hợp lệ.");
        string promptPath = Path.GetFullPath(Path.Combine(root, _settings.PromptFile.Replace('/', Path.DirectorySeparatorChar)));
        if (!promptPath.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Đường dẫn prompt nằm ngoài thư mục ứng dụng.");
        _instructions = File.ReadAllText(promptPath, Encoding.UTF8);
        if (string.IsNullOrWhiteSpace(_instructions)) throw new InvalidOperationException("Thiếu prompt lễ tân AI.");

        // Chỉ đúng hai function, schema phẳng; tránh tăng lỗi tool_use_failed của 20B.
        const string schemas = """
        [
          {"type":"function","function":{"name":"lookup_hospital_data",
            "description":"Tra cứu dữ liệu bệnh viện, không tạo phiếu.",
            "parameters":{"type":"object","additionalProperties":false,
              "properties":{"query_type":{"type":"string","enum":["BANG_GIA","BAO_HIEM_Y_TE","THOI_GIAN_CHO","VI_TRI_PHONG"]}},
              "required":["query_type"]}}},
          {"type":"function","function":{"name":"create_guidance_slip",
            "description":"Chuẩn bị tạo phiếu khám chính thức sau khi có ngày sinh, họ tên, điện thoại và nhu cầu; C# xác nhận trước khi ghi SQL.",
            "parameters":{"type":"object","additionalProperties":false,
              "properties":{
                "patient_name":{"type":"string"},
                "phone_number":{"type":"string"},
                "date_of_birth":{"type":"string","description":"Ngày sinh YYYY-MM-DD, dựa vào bệnh nhân cung cấp"},
                "department_group":{"type":"string","enum":["DAU_CAP_TINH","NHO_RANG_KHON","CHINH_NHA_THAM_MY","IMPLANT_PHUC_HINH","KHAM_TONG_QUAT","TAI_KHAM"]},
                "has_recent_xray":{"type":"boolean"},
                "symptom_summary":{"type":"string"}},
              "required":["patient_name","phone_number","date_of_birth","department_group","has_recent_xray","symptom_summary"]}}}
        ]
        """;
        using var parsed = JsonDocument.Parse(schemas);
        _tools = parsed.RootElement.Clone();
    }


    // Tương thích cách đọc API key của TroLyAI cũ; không ghi key vào source hoặc log.
    private string? LoadApiKey()
    {
        string? environmentKey = Environment.GetEnvironmentVariable(_settings.KeyVariable);
        if (!string.IsNullOrWhiteSpace(environmentKey)) return environmentKey.Trim();

        string secretFile = Path.Combine(AppContext.BaseDirectory, "AI", "Config", "AiSecrets.local.json");
        if (!File.Exists(secretFile)) return null;
        try
        {
            using var secret = JsonDocument.Parse(File.ReadAllText(secretFile, Encoding.UTF8));
            if (secret.RootElement.ValueKind == JsonValueKind.Object &&
                secret.RootElement.TryGetProperty("GroqApiKey", out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                string? localKey = value.GetString()?.Trim();
                return string.IsNullOrWhiteSpace(localKey) ? null : localKey;
            }
        }
        catch (JsonException)
        {
            throw new GroqUnavailableException("File AiSecrets.local.json không phải JSON hợp lệ.");
        }
        catch (IOException)
        {
            throw new GroqUnavailableException("Không đọc được AiSecrets.local.json. Kiểm tra quyền truy cập file.");
        }
        catch (UnauthorizedAccessException)
        {
            throw new GroqUnavailableException("Không có quyền đọc AiSecrets.local.json.");
        }
        return null;
    }

    public async Task<GroqTurn> CompleteAsync(IReadOnlyList<GroqChatMessage> history,
        PatientDraft draft, CancellationToken ct)
    {
        string? key = LoadApiKey();
        if (string.IsNullOrWhiteSpace(key))
            throw new GroqUnavailableException("Không tìm thấy GROQ_API_KEY hoặc GroqApiKey trong AI/Config/AiSecrets.local.json.");

        var messages = new List<GroqChatMessage>
        {
            new() { Role = "system", Content = _instructions + "\nTrạng thái đã được ứng dụng xác nhận: " + draft.CompactState }
        };
        messages.AddRange(BuildRecentWindow(history, 6));
        var requestBody = new
        {
            model = _settings.Model,
            messages,
            tools = _tools,
            tool_choice = "auto",
            parallel_tool_calls = false,
            temperature = 0.25,
            reasoning_effort = "low",
            include_reasoning = false,
            max_completion_tokens = _settings.MaxCompletionTokens
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        try
        {
            using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                timeout.Token).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new GroqUnavailableException("AI đang quá tải hoặc hết hạn mức; dùng chọn nhanh.");
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new GroqUnavailableException("Groq từ chối API key (HTTP " + (int)response.StatusCode + "). Kiểm tra key và quyền model.");
            if (response.StatusCode == HttpStatusCode.BadRequest)
                throw new GroqUnavailableException("Groq từ chối request (HTTP 400). Kiểm tra cấu hình model, giới hạn tool và tham số API.");
            if ((int)response.StatusCode >= 500)
                throw new GroqUnavailableException("Máy chủ AI tạm thời không phản hồi; dùng chọn nhanh.");
            if (!response.IsSuccessStatusCode)
                throw new GroqUnavailableException($"Không thể dùng AI (HTTP {(int)response.StatusCode}); dùng chọn nhanh.");

            await using var body = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            var result = await JsonSerializer.DeserializeAsync<GroqCompletionResponse>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, timeout.Token).ConfigureAwait(false);
            var message = result?.Choices.FirstOrDefault()?.Message
                ?? throw new GroqUnavailableException("AI trả về dữ liệu không hợp lệ.");
            if (message.ToolCalls is { Count: > 1 })
                throw new GroqUnavailableException("AI gọi quá nhiều tác vụ cùng lúc; chuyển sang chọn nhanh.");
            if (message.ToolCalls is { Count: 1 } calls)
            {
                var call = calls[0];
                if (string.IsNullOrWhiteSpace(call.Id) || call.Type != "function" || call.Function is null ||
                    call.Function.Name is not ("lookup_hospital_data" or "create_guidance_slip"))
                    throw new GroqUnavailableException("AI yêu cầu thao tác không được phép.");
                return new GroqTurn(null, call, message);
            }
            if (string.IsNullOrWhiteSpace(message.Content))
                throw new GroqUnavailableException("AI chưa tạo được câu trả lời.");
            return new GroqTurn(LimitSentences(message.Content), null, message);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new GroqUnavailableException("AI phản hồi quá 8 giây; dùng chọn nhanh."); }
        catch (HttpRequestException)
        { throw new GroqUnavailableException("Mất kết nối AI; dùng chọn nhanh."); }
        catch (JsonException)
        { throw new GroqUnavailableException("Phản hồi AI không hợp lệ; dùng chọn nhanh."); }
    }

    public static IReadOnlyList<GroqChatMessage> BuildRecentWindow(
        IReadOnlyList<GroqChatMessage> history, int maxMessages = 6)
    {
        // Mỗi nhóm assistant tool_calls + tool response là không thể chia tách.
        var units = new List<List<GroqChatMessage>>();
        for (int i = 0; i < history.Count; i++)
        {
            var msg = history[i];
            if (msg.Role == "tool") continue; // Không gửi tool response đơn độc.
            if (msg.Role == "assistant" && msg.ToolCalls is { Count: > 0 } calls)
            {
                int j = i + 1;
                var unit = new List<GroqChatMessage> { msg };
                while (j < history.Count && history[j].Role == "tool")
                    unit.Add(history[j++]);
                if (unit.Count == calls.Count + 1 &&
                    calls.All(c => unit.Skip(1).Any(t => t.ToolCallId == c.Id)))
                    units.Add(unit);
                i = j - 1;
            }
            else units.Add(new List<GroqChatMessage> { msg });
        }
        var selected = new List<List<GroqChatMessage>>();
        int count = 0;
        for (int i = units.Count - 1; i >= 0; i--)
        {
            if (count + units[i].Count > maxMessages) break;
            selected.Add(units[i]);
            count += units[i].Count;
        }
        selected.Reverse();
        return selected.SelectMany(x => x).ToArray();
    }

    private static string LimitSentences(string answer)
    {
        answer = answer.Trim();
        // Lớp chặn dự phòng: prompt không đủ để bảo đảm mô hình không khẳng định bệnh/kê liều.
        if (Regex.IsMatch(answer, @"(?i)(bạn (đang )?bị|chắc chắn (là|bị)|hãy uống|uống mỗi ngày|liều lượng|\b\d+\s*mg\b|chẩn đoán (là|bạn))"))
            return "Tôi không thể chẩn đoán hoặc kê thuốc. Vui lòng gặp bác sĩ để được kiểm tra, tôi có thể hướng dẫn bạn đến phòng khám phù hợp.";
        // Không cắt tại dấu chấm trong "P.102"; chỉ cắt giữa các câu.
        var sentences = Regex.Split(answer, @"(?<=[.!?])\s+(?=\p{L})");
        answer = string.Join(" ", sentences.Take(3));
        return answer.Length <= 380 ? answer : answer[..380].TrimEnd() + "…";
    }
}
