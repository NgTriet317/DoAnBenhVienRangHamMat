using System.Security.Claims;
using ET;

namespace RangHamMat;

// Phiên tài khoản đã xác thực, dùng chung trong lần chạy ứng dụng.
public static class PhienDangNhap
{
    public static string MaNV { get; set; } = "";
    public static string TenDangNhap { get; set; } = "";
    public static bool IsAdmin { get; set; }
    public static event Action? DinhDanhThayDoi;
    internal static event Func<bool>? TruocKhiDoiDinhDanh;
    private static string? accountCommitted,nameCommitted;
    private static bool adminCommitted;

    /// <summary>Chỉ gọi sau khi đăng nhập thành công; chốt định danh cho các API quy trình khám.</summary>
    public static void KhoiTaoDinhDanhNghiepVu(string maTK)
    {
        if (string.IsNullOrWhiteSpace(MaNV) || string.IsNullOrWhiteSpace(maTK) || MaNV.Length > 50 || maTK.Length > 50)
            throw new InvalidOperationException("Phiên đăng nhập chưa có định danh hợp lệ.");
        var prior=Thread.CurrentPrincipal as ClaimsPrincipal;
        var priorEmployee=prior?.FindFirst(DinhDanhNghiepVu.MaNhanVienClaim)?.Value;
        var priorAccount=prior?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(priorEmployee is not null&&(priorEmployee!=MaNV||priorAccount!=maTK))
        {
            var proposedEmployee=MaNV;var proposedName=TenDangNhap;var proposedAdmin=IsAdmin;
            MaNV=priorEmployee;IsAdmin=priorAccount==accountCommitted&&adminCommitted;TenDangNhap=priorAccount==accountCommitted?nameCommitted??"":"";
            var allow=false;
            try
            {
                allow=TruocKhiDoiDinhDanh?.GetInvocationList().Cast<Func<bool>>().All(check=>check())??true;
                if(!allow)throw new InvalidOperationException("Đã giữ phiên hiện tại. Lưu/bỏ nháp hoặc đối chiếu yêu cầu đang xử lý trước khi đổi tài khoản.");
            }
            finally{if(allow){MaNV=proposedEmployee;IsAdmin=proposedAdmin;TenDangNhap=proposedName;}}
        }
        Thread.CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, maTK),
            new Claim(DinhDanhNghiepVu.MaNhanVienClaim, MaNV)
        }, DinhDanhNghiepVu.KieuXacThuc));
        accountCommitted=maTK;adminCommitted=IsAdmin;nameCommitted=TenDangNhap;
        DinhDanhThayDoi?.Invoke();
    }
}
