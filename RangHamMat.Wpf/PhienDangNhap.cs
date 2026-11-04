using System.Security.Claims;
using ET;

namespace RangHamMat;

// Phiên tài khoản đã xác thực, dùng chung trong lần chạy ứng dụng.
public static class PhienDangNhap
{
    /// <summary>
    /// Mã nhân viên thực hiện hoặc được gán cho bản ghi.
    /// </summary>
    public static string MaNV { get; set; } = "";
    /// <summary>
    /// Tên đăng nhập của tài khoản.
    /// </summary>
    public static string TenDangNhap { get; set; } = "";
    /// <summary>
    /// Là quản trị viên.
    /// </summary>
    public static bool IsAdmin { get; set; }
    /// <summary>
    /// Thông báo định danh thay đổi cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    public static event Action? DinhDanhThayDoi;
    /// <summary>
    /// Thông báo trước khi đổi định danh cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    internal static event Func<bool>? TruocKhiDoiDinhDanh;
    /// <summary>
    /// Tài khoản đã lưu tên đã lưu được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static string? accountCommitted,nameCommitted;
    /// <summary>
    /// Cờ Quản trị viên đã lưu; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private static bool adminCommitted;
    /// <summary>
    /// Ứng dụng desktop định danh bảo mật được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static DesktopPrincipal? desktopPrincipal;
    /// <summary>
    /// Mô hình và thao tác hỗ trợ ứng dụng desktop định danh bảo mật dùng bởi các thành phần liên quan.
    /// </summary>
    private sealed class DesktopPrincipal : ClaimsPrincipal
    {
        /// <summary>
        /// Hiện tại.
        /// </summary>
        internal ClaimsPrincipal Current { get; set; }=new();
        /// <summary>
        /// Giá trị Các định danh; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public override IEnumerable<ClaimsIdentity> Identities=>Current.Identities;
        /// <summary>
        /// Giá trị Định danh; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public override System.Security.Principal.IIdentity? Identity=>Current.Identity;
        /// <summary>
        /// Giá trị Các claim; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public override IEnumerable<Claim> Claims=>Current.Claims;
        /// <summary>
        /// Đọc claim đầu tiên khớp tiêu chí từ định danh đang dùng.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>Current.FindFirst</c>.</remarks>
        /// <param name="type">Giá trị kiểu dùng khi thực hiện thao tác; kiểu string.</param>
        /// <returns>Đối tượng kiểu Claim được tìm hoặc tính toán; có thể null theo nhánh xử lý trong hàm.</returns>
        public override Claim? FindFirst(string type)=>Current.FindFirst(type);
        /// <summary>
        /// Đọc claim đầu tiên khớp tiêu chí từ định danh đang dùng.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>Current.FindFirst</c>.</remarks>
        /// <param name="match">Giá trị khớp dùng khi thực hiện thao tác; kiểu Predicate&lt;Claim&gt;.</param>
        /// <returns>Đối tượng kiểu Claim được tìm hoặc tính toán; có thể null theo nhánh xử lý trong hàm.</returns>
        public override Claim? FindFirst(Predicate<Claim> match)=>Current.FindFirst(match);
        /// <summary>
        /// Đọc toàn bộ claim khớp tiêu chí từ định danh đang dùng.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>Current.FindAll</c>.</remarks>
        /// <param name="type">Giá trị kiểu dùng khi thực hiện thao tác; kiểu string.</param>
        /// <returns>Tập dữ liệu kiểu IEnumerable&lt;Claim&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
        public override IEnumerable<Claim> FindAll(string type)=>Current.FindAll(type);
        /// <summary>
        /// Đọc toàn bộ claim khớp tiêu chí từ định danh đang dùng.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>Current.FindAll</c>.</remarks>
        /// <param name="match">Giá trị khớp dùng khi thực hiện thao tác; kiểu Predicate&lt;Claim&gt;.</param>
        /// <returns>Tập dữ liệu kiểu IEnumerable&lt;Claim&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
        public override IEnumerable<Claim> FindAll(Predicate<Claim> match)=>Current.FindAll(match);
        /// <summary>
        /// Kiểm tra sự tồn tại của claim theo loại/giá trị được yêu cầu.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>Current.HasClaim</c>.</remarks>
        /// <param name="type">Giá trị kiểu dùng khi thực hiện thao tác; kiểu string.</param>
        /// <param name="value">Giá trị string được truyền vào để đọc, kiểm tra hoặc cập nhật trong thao tác này.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public override bool HasClaim(string type,string value)=>Current.HasClaim(type,value);
        /// <summary>
        /// Kiểm tra sự tồn tại của claim theo loại/giá trị được yêu cầu.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>Current.HasClaim</c>.</remarks>
        /// <param name="match">Giá trị khớp dùng khi thực hiện thao tác; kiểu Predicate&lt;Claim&gt;.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public override bool HasClaim(Predicate<Claim> match)=>Current.HasClaim(match);
        /// <summary>
        /// Kiểm tra định danh hiện tại thuộc vai trò được yêu cầu hay không.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>Current.IsInRole</c>.</remarks>
        /// <param name="role">Giá trị vai trò dùng khi thực hiện thao tác; kiểu string.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
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
