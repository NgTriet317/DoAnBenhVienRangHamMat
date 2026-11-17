using System.Collections;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Windows;
using System.Windows.Controls;
using ET;
using RangHamMat.Services;
using RangHamMat.Views;

namespace RuntimeCoverageChecks;

/// <summary>Real WPF controls and production adapter metadata, with in-memory prepared payloads. No SQL connection.</summary>
internal static class Program
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    [STAThread]
    private static int Main()
    {
        Thread.CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/RangHamMat.Wpf;component/Themes/Theme.xaml", UriKind.Relative) });
        var checks = new (string Name, Action Run)[]
        {
            ("audit-invalid-filter-keeps-last-applied-filter-and-results", InvalidAuditFilter),
            ("audit-default-filter-accepts-login-logout-and-utc", AuditDefaults),
            ("audit-prepared-refresh-keeps-selected-event-and-detail", AuditSelection),
            ("warehouse-supplier-name-dependency", () => Dependencies(new Kho(), "NHA_CUNG_CAP")),
            ("reception-department-name-dependency", () => Dependencies(new PhieuHuongDan(), "KHOA")),
            ("payroll-reconciliation-invoice-lines-dependency", () => Dependencies(new BangLuong(), "CHI_TIET_HOA_DON")),
            ("clinical-reference-conditions-and-links-dependencies", () => ConstructorTables(typeof(KhamLamSang), "DM_TINH_TRANG_BENH", "DICH_VU_TINH_TRANG", "PHIEU_KHAM_BENH", "THUOC", "HANG_HOA", "CHI_TIET_HOA_DON", "CHI_TIET_PHIEU_XUAT", "AI_KET_QUA_TAO_PHIEU", "AI_YEU_CAU_TAO_PHIEU")),
            ("prescription-drug-inventory-and-guidance-dependencies", () => ConstructorTables(typeof(KeDonThuocEditor), "HANG_HOA", "CAU_HINH_THUOC_KE_DON", "PHIEU_XUAT", "CHI_TIET_PHIEU_XUAT", "DICH_VU_THUC_HIEN", "PHIEU_KHAM_BENH", "BENH_NHAN")),
            ("completed-services-name-and-invoice-dependencies", () => ConstructorTables(typeof(ThucHienDichVuEditor), "BENH_NHAN", "DICH_VU", "NHAN_VIEN", "CHI_TIET_HOA_DON")),
            ("fee-drug-price-and-ai-surcharge-dependencies", () => ConstructorTables(typeof(BangTinhPhiEditor), "THUOC", "HANG_HOA", "DICH_VU", "AI_KET_QUA_TAO_PHIEU", "PHIEU_KHAM_BENH", "PHIEU_XUAT", "CHI_TIET_PHIEU_XUAT")),
            ("delivery-stock-lines-and-patient-dependencies", () => ConstructorTables(typeof(BanGiaoThuocEditor), "CHI_TIET_HOA_DON", "CHI_TIET_PHIEU_XUAT", "BENH_NHAN", "NHAN_VIEN", "PHIEU_KHAM_BENH", "LICH_SU_KHAM_BENH")),
            ("guidance-child-has-explicit-configuration-adapter", () => ConstructorTables(typeof(HuongDanThuocEditor), "CAU_HINH_THUOC_KE_DON", "NHOM_THUOC_KE_DON", "THUOC", "HANG_HOA")),
            ("prescription-history-delivery-and-patient-dependencies", () => ConstructorTables(typeof(PrescriptionHistoryDetail), "PHIEU_XUAT", "CHI_TIET_PHIEU_XUAT", "CHI_TIET_HOA_DON", "BENH_NHAN", "NHAN_VIEN", "LICH_SU_KHAM_BENH", "PHIEU_KHAM_BENH")),
            ("guidance-child-protects-unfocused-drafts", GuidancePending),
            ("prescription-nested-refresh-failure-is-retryable", PrescriptionRefreshFailure)
        };
        int failed = 0;
        foreach (var check in checks)
        {
            try { check.Run(); Console.WriteLine("PASS " + check.Name); }
            catch (Exception error) { failed++; Console.WriteLine("FAIL " + check.Name + "\n" + error); }
        }
        Console.WriteLine($"RESULT: {checks.Length - failed} passed, {failed} failed; no SQL used.");
        return failed == 0 ? 0 : 1;
    }

    private static NhatKy Audit()
    {
        var view = new NhatKy();
        Field(view, "ready").SetValue(view, true);
        var actions = (ComboBox)view.FindName("ActionFilter");
        actions.ItemsSource = HanhDongNhatKy.LuaChon;
        actions.SelectedIndex = 0;
        Payload(view, [Row("NK-1", "LOGIN_SUCCESS")]);
        Call(view, "Load", true);
        return view;
    }

    // Before validation happens in DAL, Load must keep the previous valid filter for the next sync tick.
    private static void InvalidAuditFilter()
    {
        var view = Audit();
        var applied = Field(view, "applied").GetValue(view);
        var grid = (DataGrid)view.FindName("AuditGrid");
        var source = grid.ItemsSource;
        ((DatePicker)view.FindName("FromDate")).SelectedDate = new DateTime(2026, 11, 19);
        ((DatePicker)view.FindName("ToDate")).SelectedDate = new DateTime(2026, 11, 17);
        Call(view, "Load", true);
        Require(ReferenceEquals(Field(view, "applied").GetValue(view), applied), "An invalid range replaced the valid applied filter; all following automatic reads would fail.");
        Require(ReferenceEquals(grid.ItemsSource, source), "Invalid filter input discarded the last readable audit rows.");
        Require(((TextBlock)view.FindName("Message")).Text.Length > 0, "Invalid range did not show its validation message.");
    }

    private static void AuditDefaults()
    {
        var view = Audit();
        var filter = (BoLocNhatKy)Field(view, "applied").GetValue(view)!;
        filter.KiemTra();
        Require(filter.TuUtc is null && filter.DenUtc is null && filter.HanhDong == "", "Default audit filter excludes session events.");
        Payload(view, [Row("NK-2", "LOGOUT"), Row("NK-1", "LOGIN_SUCCESS")]);
        Call(view, "Load", false);
        var rows = (DataView)((DataGrid)view.FindName("AuditGrid")).ItemsSource;
        Require(rows.Count == 2 && Convert.ToString(rows[0]["HanhDong"]) == "Đăng xuất", "Session event codes were not displayed correctly.");
        Require(Convert.ToString(rows[0]["ThoiGianHienThi"]) == "17/11/2026 12:00:00 UTC+7", "UTC timestamp conversion changed.");
    }

    private static void AuditSelection()
    {
        var view = Audit();
        var grid = (DataGrid)view.FindName("AuditGrid");
        grid.SelectedItem = grid.Items[0];
        Payload(view, [Row("NK-2", "LOGOUT"), Row("NK-1", "LOGIN_SUCCESS")]);
        var helper = typeof(NhatKy).Assembly.GetType("RangHamMat.Views.DongBoLuaChon")!;
        helper.GetMethod("GiuDong", All)!.Invoke(null, [grid, new[] { "MaNK" }, (Action)(() => Call(view, "Load", false))]);
        Require(grid.SelectedItem is DataRowView row && Convert.ToString(row["MaNK"]) == "NK-1", "Automatic refresh moved selection to a different session event.");
        Require(((TextBlock)view.FindName("DetailId")).Text == "NK-1", "Selected audit detail was lost after prepared refresh.");
        Payload(view, [Row("NK-2", "LOGOUT")]);
        helper.GetMethod("GiuDong", All)!.Invoke(null, [grid, new[] { "MaNK" }, (Action)(() => Call(view, "Load", false))]);
        Require(grid.SelectedItem is null && ((FrameworkElement)view.FindName("DetailContent")).Visibility == Visibility.Collapsed, "Removed audit row left a different event selected.");
    }

    private static Dictionary<string, object> Row(string id, string action) => new()
    {
        ["MaNK"] = id, ["ThoiGianUtc"] = new DateTime(2026, 11, 17, 5, 0, 0),
        ["ThoiGian"] = new DateTime(2026, 11, 17, 12, 0, 0), ["HanhDong"] = action,
        ["TenDangNhap"] = "fixture", ["HoTen"] = "Fixture", ["KetQua"] = "Thành công",
        ["PhanHe"] = "Phiên đăng nhập", ["LoaiDoiTuong"] = "PHIEN_DANG_NHAP", ["MaDoiTuong"] = "fixture-session"
    };

    private static void Payload(NhatKy view, IReadOnlyList<IDictionary<string, object>> rows)
    {
        var payloadType = typeof(NhatKy).GetNestedType("AnhChupNhatKy", All)!;
        var payload = Activator.CreateInstance(payloadType, new NguCanhNghiepVu { LaAdmin = true }, (1, 0, 0), new TrangNhatKy(rows, rows.Count));
        Field(view, "dongBoNhatKy").SetValue(view, payload);
    }

    // Raising Loaded registers the real adapter; daTai prevents the view's initial business SQL read.
    private static void Dependencies(UserControl view, params string[] required)
    {
        Field(view, "daTai").SetValue(view, true);
        view.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        try
        {
            var active = (IEnumerable)typeof(DongBoManHinh).GetField("active", All)!.GetValue(null)!;
            var registration = active.Cast<object>().Single(r => ReferenceEquals(r.GetType().GetProperty("Control")!.GetValue(r), view));
            var tables = (string[])registration.GetType().GetProperty("Tables")!.GetValue(registration)!;
            Require(required.All(tables.Contains), view.GetType().Name + " omits the SQL dependency " + string.Join(", ", required.Except(tables)));
        }
        finally { view.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent)); }
    }

    private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, All)!;
    private static void PrescriptionRefreshFailure()
    {
        var view = (KeDonThuocEditor)RuntimeHelpers.GetUninitializedObject(typeof(KeDonThuocEditor));
        Field(view, "ErrorText").SetValue(view, new TextBlock());
        Exception? failure = null;
        Action refresh = () => Call(view, "Run", (Action)(() => throw new InvalidOperationException("fixture prescription refresh failed")), null);
        try { typeof(DongBoManHinh).GetMethod("ApplyPrepared", All)!.Invoke(null, [refresh]); }
        catch (TargetInvocationException error) { failure = error.InnerException; }
        Require(failure is InvalidOperationException && failure.Message.Contains("fixture prescription refresh failed"), "Nested prescription refresh swallowed its read failure, so the pending batch would be acknowledged.");
    }
    private static void GuidancePending()
    {
        var type = typeof(HuongDanThuocEditor);
        var pending = type.Assembly.GetType("RangHamMat.Controls.ClinicalWorkflow.IClinicalPendingWork")!;
        Require(pending.IsAssignableFrom(type), "Guidance editor is omitted from workspace pending-work protection.");
        var view = (HuongDanThuocEditor)RuntimeHelpers.GetUninitializedObject(type);
        var group = new TextBox();
        Field(view, "GroupName").SetValue(view, group);
        bool Dirty() => (bool)pending.GetProperty("HasChanges")!.GetValue(view)!;
        Require(!Dirty(), "Empty guidance editor unnecessarily blocks refresh.");
        group.Text = "Unsaved drug group";
        Require(Dirty(), "Unfocused new-group input can be replaced by automatic refresh.");
        group.Clear();
        Field(view, "dirty").SetValue(view, true);
        Require(Dirty(), "Unfocused guidance input can be replaced by automatic refresh.");
    }
    // Decode compiled string constants without running constructors that deliberately query SQL.
    // Dependencies come from the DAL reads documented in DONG_BO_RUNTIME_MA_TRAN.md.
    private static void ConstructorTables(Type type, params string[] required)
    {
        var strings = new HashSet<string>();
        var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
            .ToDictionary(o => unchecked((ushort)o.Value));
        foreach (var ctor in type.GetConstructors(All).Cast<MethodBase>().Concat(type.GetMethods(All).Where(m => m.Name == "DangKyDongBoManHinh")))
        {
            var bytes = ctor.GetMethodBody()?.GetILAsByteArray() ?? [];
            for (int at = 0; at < bytes.Length;)
            {
                ushort code = bytes[at++];
                if (code == 0xfe) code = (ushort)(0xfe00 | bytes[at++]);
                var op = opcodes[code];
                if (op == OpCodes.Ldstr) strings.Add(ctor.Module.ResolveString(BitConverter.ToInt32(bytes, at)));
                at += op.OperandType switch
                {
                    OperandType.InlineNone => 0,
                    OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                    OperandType.InlineVar => 2,
                    OperandType.InlineI8 or OperandType.InlineR => 8,
                    OperandType.InlineSwitch => 4 + BitConverter.ToInt32(bytes, at) * 4,
                    _ => 4
                };
            }
        }
        Require(required.All(strings.Contains), type.Name + " omits the SQL dependency " + string.Join(", ", required.Except(strings)));
    }
    private static void Call(object target, string name, params object?[] args)
    {
        try { target.GetType().GetMethod(name, All)!.Invoke(target, args); }
        catch (TargetInvocationException error) when (error.InnerException is not null) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
