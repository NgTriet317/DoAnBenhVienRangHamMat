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
    private static DesktopPrincipal? desktopPrincipal;
    private sealed class DesktopPrincipal : ClaimsPrincipal
    {
        internal ClaimsPrincipal Current { get; set; }=new();
        public override IEnumerable<ClaimsIdentity> Identities=>Current.Identities;
        public override System.Security.Principal.IIdentity? Identity=>Current.Identity;
        public override IEnumerable<Claim> Claims=>Current.Claims;
        public override Claim? FindFirst(string type)=>Current.FindFirst(type);
        public override Claim? FindFirst(Predicate<Claim> match)=>Current.FindFirst(match);
        public override IEnumerable<Claim> FindAll(string type)=>Current.FindAll(type);
        public override IEnumerable<Claim> FindAll(Predicate<Claim> match)=>Current.FindAll(match);
        public override bool HasClaim(string type,string value)=>Current.HasClaim(type,value);
        public override bool HasClaim(Predicate<Claim> match)=>Current.HasClaim(match);
        public override bool IsInRole(string role)=>Current.IsInRole(role);
    }

    /// <summary>Khởi tạo một lần ở App.Startup: định danh mặc định tồn tại qua các ExecutionContext của Dispatcher.</summary>
    public static void KhoiTaoPhienUngDung()
    {
        if(desktopPrincipal is not null)return;
        var principal=new DesktopPrincipal();
        AppDomain.CurrentDomain.SetThreadPrincipal(principal);
        desktopPrincipal=principal;
        Thread.CurrentPrincipal=principal;
    }

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
        var identity=new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, maTK),
            new Claim(DinhDanhNghiepVu.MaNhanVienClaim, MaNV)
        }, DinhDanhNghiepVu.KieuXacThuc);
        if(desktopPrincipal is null)Thread.CurrentPrincipal=new ClaimsPrincipal(identity);
        else
        {
            desktopPrincipal.Current=new ClaimsPrincipal(identity);
            Thread.CurrentPrincipal=desktopPrincipal;
        }
        accountCommitted=maTK;adminCommitted=IsAdmin;nameCommitted=TenDangNhap;
        DinhDanhThayDoi?.Invoke();
    }
}
