namespace RangHamMat;

// Phiên tài khoản đã xác thực, dùng chung trong lần chạy ứng dụng.
public static class PhienDangNhap
{
    public static string MaNV { get; set; } = "";
    public static string TenDangNhap { get; set; } = "";
    public static bool IsAdmin { get; set; }
}
