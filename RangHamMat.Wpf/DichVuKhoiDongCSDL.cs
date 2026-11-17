using Microsoft.Win32;

namespace RangHamMat;

/// <summary>Yêu cầu xác nhận SQL một lần cho mỗi lần cài đặt, trước khi tự khôi phục kết nối.</summary>
internal sealed class DichVuKhoiDongCSDL
{
    private const string TenCo = "CanCauHinhDatabase";
    private readonly string duongDanRegistry;

    internal DichVuKhoiDongCSDL(string? duongDanRegistry = null)
        => this.duongDanRegistry = duongDanRegistry ?? @"Software\RangHamMat\CaiDat";

    /// <summary>Cờ MSI có giá trị 1 khi cài mới/cài lại; thiếu hoặc sai kiểu cũng cần xác nhận.</summary>
    internal async Task<bool> CanHienCauHinhAsync(Func<Task<bool>> khoiPhucKetNoi)
    {
        using var key = Registry.CurrentUser.OpenSubKey(duongDanRegistry);
        if (key?.GetValue(TenCo) is not int daXacNhan || daXacNhan != 0)
            return true;

        return !await khoiPhucKetNoi();
    }

    /// <summary>Chỉ gọi khi màn hình đã kiểm tra và lưu SQL thành công; không ghi nhận khi hủy/lỗi.</summary>
    internal void XacNhanCauHinh()
    {
        using var key = Registry.CurrentUser.CreateSubKey(duongDanRegistry);
        key.SetValue(TenCo, 0, RegistryValueKind.DWord);
    }
}
