using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RangHamMat.Models;

public enum DepartmentGroup
{
    DAU_CAP_TINH, NHO_RANG_KHON, CHINH_NHA_THAM_MY,
    IMPLANT_PHUC_HINH, KHAM_TONG_QUAT, TAI_KHAM
}

public enum HospitalQueryType { BANG_GIA, BAO_HIEM_Y_TE, THOI_GIAN_CHO, VI_TRI_PHONG }

public sealed class ChatMessage
{
    public string Role { get; init; } = "assistant";
    public string Content { get; init; } = "";
    public bool IsUser => Role == "user";
    public DateTime CreatedAt { get; init; } = DateTime.Now;
}

public sealed record DepartmentRoom(string RoomCode, string Name, DepartmentGroup Group,
                                    int WaitingCount, bool Available = true);

public sealed record SlipRequest(string PatientName, string PhoneNumber,
    DepartmentGroup DepartmentGroup, bool HasRecentXray, string SymptomSummary,
    Guid SessionId, DateTime DateOfBirth, bool PatientConfirmed = false);

public sealed class GuidanceSlip
{
    public string Code { get; init; } = "";
    public string PatientName { get; init; } = "";
    public string PhoneNumber { get; init; } = "";
    public DepartmentGroup DepartmentGroup { get; init; }
    public string SymptomSummary { get; init; } = "";
    public bool HasRecentXray { get; init; }
    public DateTime DateOfBirth { get; init; }
    public int QueueNumber { get; init; }
    public string AssignedRoomCode { get; init; } = "";
    public string AssignedRoomName { get; init; } = "";
    public IReadOnlyList<string> Route { get; init; } = Array.Empty<string>();
    public string RouteText => string.Join("  →  ", Route);
    public int EstimatedWaitMinutes { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public bool IsDemo { get; init; }
}

public sealed class PatientDraft
{
    public DepartmentGroup? Group { get; set; }
    public string Symptom { get; set; } = "";
    public bool? HasRecentXray { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
    public string CompactState =>
        $"Nhóm: {Group?.ToString() ?? "?"}; triệu chứng: {Short(Symptom, 110)}; " +
        $"X-quang 6 tháng: {(HasRecentXray.HasValue ? (HasRecentXray.Value ? "có" : "không") : "?")}; " +
        $"tên: {Short(Name, 70)}; SĐT: {Phone}; ngày sinh: {DateOfBirth:dd/MM/yyyy}.";
    private static string Short(string s, int n) => s.Length <= n ? s : s[..n];
    public void Clear() { Group = null; Symptom = ""; HasRecentXray = null; Name = ""; Phone = ""; DateOfBirth = null; }
}

// DTO của chat/completions: giữ cặp assistant tool_calls / tool message nguyên vẹn.
public sealed class GroqChatMessage
{
    [JsonPropertyName("role")] public string Role { get; set; } = "user";
    [JsonPropertyName("content")] public string? Content { get; set; }
    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }
    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GroqToolCall>? ToolCalls { get; set; }
}

public sealed class GroqToolCall
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "function";
    [JsonPropertyName("function")] public GroqFunctionCall Function { get; set; } = new();
}

public sealed class GroqFunctionCall
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("arguments")] public string Arguments { get; set; } = "{}";
}

public sealed class GroqCompletionResponse
{
    [JsonPropertyName("choices")] public List<GroqChoice> Choices { get; set; } = new();
}
public sealed class GroqChoice
{
    [JsonPropertyName("message")] public GroqChatMessage Message { get; set; } = new();
    [JsonPropertyName("finish_reason")] public string? FinishReason { get; set; }
}

public sealed class GroqSettings
{
    public string Endpoint { get; set; } = "https://api.groq.com/openai/v1/chat/completions";
    public string Model { get; set; } = "openai/gpt-oss-20b";
    public string KeyVariable { get; set; } = "GROQ_API_KEY";
    public string PromptFile { get; set; } = "AI/Prompts/LeTanKiosk.md";
    public int TimeoutSeconds { get; set; } = 8;
    public int MaxRecentMessages { get; set; } = 6;
    public int MaxCompletionTokens { get; set; } = 300;
    public bool UseMockRepository { get; set; } = false;
}
