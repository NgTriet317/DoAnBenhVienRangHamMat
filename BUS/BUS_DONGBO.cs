using DAL;
using ET;

namespace BUS;

/// <summary>Đọc thông báo thay đổi SQL bằng cấu hình cố định của phiên, độc lập principal giao diện.</summary>
public sealed class BUS_DONGBO(string connectionString)
{
    private readonly DAL_DONGBO dal = new(connectionString);

    /// <summary>Trả mốc commit và bảng thay đổi; null hoặc mốc mất hiệu lực yêu cầu tải đầy đủ.</summary>
    public Task<DotThayDoiDuLieu> DocThayDoiAsync(long? version, CancellationToken cancellationToken)
        => dal.DocThayDoiAsync(version, cancellationToken);
}
