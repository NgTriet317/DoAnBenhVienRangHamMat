using System.IO;
using System.Reflection;
using System.Security.Claims;
using System.Windows;
using System.Windows.Controls;
using ET;
using RangHamMat;

// Không kết nối SQL: kiểm dữ liệu DPAPI và chính luồng buộc đăng xuất trên WPF thật.
static class Program
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static readonly Assembly Wpf = typeof(MainWindow).Assembly;
    static int passed;

    [STAThread] static int Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/RangHamMat.Wpf;component/Themes/Theme.xaml", UriKind.Relative) });
        try
        {
            PhienDangNhap.KhoiTaoPhienUngDung();
            RememberedSessionHasAuthorityToken();
            RestoreRequiresLatestServerToken();
            ForcedLogoutDiscardsDirtyOwnedModal();
            Console.WriteLine($"RESULT: {passed} passed");
            return 0;
        }
        catch (Exception error) { Console.WriteLine("FAIL: " + error.GetBaseException()); return 1; }
        finally { app.Shutdown(); }
    }

    // Cùng tài khoản/mật khẩu vẫn không đủ: token bị máy khác thay phải trả false và không đăng nhập mới.
    static void RestoreRequiresLatestServerToken()
    {
        var path = Path.Combine(Path.GetTempPath(), "rhm-restore-" + Guid.NewGuid().ToString("N"), "login.dat");
        var store = Activator.CreateInstance(Wpf.GetType("RangHamMat.KhoPhienDangNhap")!, All, null, [path], null)!;
        var account = new ET_TAIKHOAN("TK_RESTORE", "restore-user", "password-hash", "VT_USER", "NV_RESTORE", null, null, "Đang hoạt động");
        var oldSession = Guid.Parse("c0d5a575-94bd-43bc-b8e1-692839623f12");
        void Remember()
        {
            typeof(PhienDangNhap).GetProperty(nameof(PhienDangNhap.MaPhien))!.SetValue(null, oldSession);
            Call(store, "Luu", account);
            CallStatic("RangHamMat.PhienDangNhap", "XoaDinhDanh");
        }
        object Service(Action<Guid?> start) => Activator.CreateInstance(Wpf.GetType("RangHamMat.DichVuDangNhap")!, All, null,
            [store, (Func<string, ET_TAIKHOAN?>)(id => id == "TK_RESTORE" ? account : null),
             (Func<ET_TAIKHOAN, (bool HopLe, bool QuanTri)>)(_ => (true, false)), start], null)!;
        try
        {
            Remember();
            Guid? received = null;
            var revoked = Service(saved => { received = saved; throw new UnauthorizedAccessException("tài khoản đã được đăng nhập vào máy khác"); });
            Require(Call(revoked, "ThuKhoiPhuc") is false && received == oldSession && !File.Exists(path) && !PhienDangNhap.DaDangNhap,
                "Token bị thay trả false, xóa file cũ và không lấy lại phiên chỉ vì tài khoản còn hoạt động.");

            Remember();
            var newSession = Guid.Parse("ba158028-aaf4-45a1-a7dd-cf7170690f73");
            var latestClosed = Service(saved =>
            {
                if (saved != oldSession) throw new Exception("Khôi phục phải xin SQL đổi đúng token đã ghi nhớ.");
                typeof(PhienDangNhap).GetProperty(nameof(PhienDangNhap.MaPhien))!.SetValue(null, newSession);
                CallStatic("RangHamMat.PhienDangNhap", "GanPhienAudit", newSession);
            });
            Require(Call(latestClosed, "ThuKhoiPhuc") is true && PhienDangNhap.MaPhien == newSession && PhienDangNhap.DaDangNhap,
                "Đóng app bình thường vẫn khôi phục được khi SQL chấp nhận token mới nhất.");
            var refreshed = Call(store, "Doc")!;
            Require(refreshed.GetType().GetProperty("MaPhien")!.GetValue(refreshed) is Guid id && id == newSession,
                "Khôi phục thành công phải thay token trên đĩa cho lần mở ứng dụng sau.");
        }
        finally
        {
            CallStatic("RangHamMat.PhienDangNhap", "XoaDinhDanh");
            if (File.Exists(path)) Call(store, "Xoa");
            if (Directory.Exists(Path.GetDirectoryName(path))) Directory.Delete(Path.GetDirectoryName(path)!);
        }
    }

    // Nếu bỏ session khỏi v2 thì máy cũ có thể giành lại phiên mới bằng file ghi nhớ.
    static void RememberedSessionHasAuthorityToken()
    {
        var path = Path.Combine(Path.GetTempPath(), "rhm-session-" + Guid.NewGuid().ToString("N"), "login.dat");
        var store = Activator.CreateInstance(Wpf.GetType("RangHamMat.KhoPhienDangNhap")!, All, null, [path], null)!;
        try
        {
            var session = Guid.Parse("7aec3c71-7b05-4363-8956-227a32114c86");
            typeof(PhienDangNhap).GetProperty(nameof(PhienDangNhap.MaPhien))!.SetValue(null, session);
            var account = new ET_TAIKHOAN("TK_SESSION", "session-user", "password-hash", "VT_USER", "NV_SESSION", null, null, "Đang hoạt động");
            Call(store, "Luu", account);
            var saved = Call(store, "Doc")!;
            Require(saved.GetType().GetProperty("PhienBan")!.GetValue(saved) is 2,
                "Phiên ghi nhớ phải có định dạng v2 để không khôi phục file cũ thiếu token.");
            Require(saved.GetType().GetProperty("MaPhien")!.GetValue(saved) is Guid id && id == session,
                "Phiên ghi nhớ phải giữ đúng token authoritative của lần đăng nhập.");
            var legacy = Activator.CreateInstance(saved.GetType(), [1, account.MaTK, account.MaNV, new string('A', 64), Guid.Empty])!;
            Require(Call(legacy, "KhopTaiKhoan", account) is false, "File v1 thiếu token phải yêu cầu nhập mật khẩu lại.");
        }
        finally
        {
            if (File.Exists(path)) Call(store, "Xoa");
            typeof(PhienDangNhap).GetProperty(nameof(PhienDangNhap.MaPhien))!.SetValue(null, Guid.Empty);
            if (Directory.Exists(Path.GetDirectoryName(path))) Directory.Delete(Path.GetDirectoryName(path)!);
        }
    }

    // Nếu dùng điều hướng thông thường, lựa chọn Cancel/nháp đang xử lý sẽ giữ phiên bị thu hồi.
    static void ForcedLogoutDiscardsDirtyOwnedModal()
    {
        var main = new MainWindow();
        var notification = new List<string>();
        var forget = 0;
        typeof(MainWindow).GetProperty("ThongBaoMatPhien", All)!.SetValue(main, (Action<string>)(message => notification.Add(message)));
        typeof(MainWindow).GetProperty("XoaPhienGhiNho", All)!.SetValue(main, (Action)(() => forget++));
        var decisions = 0;
        typeof(MainWindow).GetProperty("LeaveDecision", All)!.SetValue(main,
            (Func<string, MessageBoxResult>)(_ => { decisions++; return MessageBoxResult.Cancel; }));
        main.Show();
        var child = new Window { Owner = main, Width = 240, Height = 120, ShowInTaskbar = false };
        CallStatic("RangHamMat.Controls.ClinicalWorkflow.ClinicalWindowLifecycle", "Attach", child);
        var input = new TextBox { Text = "Ban đầu" };
        var form = new UserControl { Content = input };
        var fields = Wpf.GetType("RangHamMat.Controls.BieuMauHoSo")!;
        var state = Activator.CreateInstance(fields.GetNestedType("InputState", BindingFlags.NonPublic)!, true)!;
        var states = fields.GetField("inputStates", All)!.GetValue(null)!;
        states.GetType().GetMethod("Add")!.Invoke(states, [form, state]);
        fields.GetMethod("AcceptInput", All)!.Invoke(null, [form]);
        input.Text = "Nội dung chưa lưu";
        Require(fields.GetMethod("HasInputChanges", All)!.Invoke(null, [form]) is true, "Fixture phải có nháp chưa lưu.");
        child.Loaded += (_, _) =>
        {
            // Gắn sau Loaded rồi buộc đóng ngay, trước các Loaded handler đọc SQL của form.
            child.Content = form;
            Thread.CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "TK_SESSION"),
                new Claim(DinhDanhNghiepVu.MaNhanVienClaim, "NV_SESSION"),
                new Claim("RHM.MaPhien", Guid.NewGuid().ToString("D"))], DinhDanhNghiepVu.KieuXacThuc));
            Call(main, "BuocDangXuatDoPhienBiThayThe");
        };
        child.ShowDialog();
        Require(!main.IsVisible && !child.IsVisible, "Phiên bị thay phải đóng cả cửa sổ chính và modal có nháp.");
        Require(decisions == 0 && forget == 1, "Buộc đăng xuất phải xóa token, không hỏi/lưu/bỏ nháp bằng luồng điều hướng.");
        Require(notification.SequenceEqual(["tài khoản đã được đăng nhập vào máy khác"]), "Phải hiển thị đúng thông báo một lần.");
        Require(!PhienDangNhap.DaDangNhap && PhienDangNhap.MaPhien == Guid.Empty,
            "Không được giữ principal hay quyền ghi của phiên cũ.");
        Call(main, "BuocDangXuatDoPhienBiThayThe");
        Require(notification.Count == 1 && forget == 1, "Nhận nhiều phản hồi thu hồi không được lặp thông báo/đăng xuất.");
    }

    static object? Call(object target, string method, params object?[] args)
        => target.GetType().GetMethod(method, All)!.Invoke(target, args);
    static object? CallStatic(string type, string method, params object?[] args)
        => Wpf.GetType(type)!.GetMethod(method, All)!.Invoke(null, args);
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        passed++;
        Console.WriteLine("PASS: " + message);
    }
}
