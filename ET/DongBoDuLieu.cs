namespace ET;

/// <summary>Mốc SQL đã quan sát và các bảng cần đọc lại; không mang dữ liệu bệnh nhân hoặc quyền UI.</summary>
public sealed record DotThayDoiDuLieu(long Version, string DatabaseIdentity, bool FullRefresh, string[] Tables);
