using System.Security.Claims;
using ET;

namespace RangHamMat;

// Phiên tài khoản đã xác thực, dùng chung trong lần chạy ứng dụng.
public static class PhienDangNhap
{
    public static string MaNV { get; set; } = "";
    public static string TenDangNhap { get; set; } = "";
    public static bool IsAdmin { get; set; }

    /// <summary>Chỉ gọi sau khi đăng nhập thành công; chốt định danh cho các API quy trình khám.</summary>
    public static void KhoiTaoDinhDanhNghiepVu(string maTK)
    {
        if (string.IsNullOrWhiteSpace(MaNV) || string.IsNullOrWhiteSpace(maTK) || MaNV.Length > 50 || maTK.Length > 50)
            throw new InvalidOperationException("Phiên đăng nhập chưa có định danh hợp lệ.");
        Thread.CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, maTK),
            new Claim(DinhDanhNghiepVu.MaNhanVienClaim, MaNV)
        }, DinhDanhNghiepVu.KieuXacThuc));
    }
}
