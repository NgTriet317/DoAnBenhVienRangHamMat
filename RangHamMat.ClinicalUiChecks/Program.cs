using System.Data;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Dapper;
using DAL;
using ET;
using RangHamMat;
using RangHamMat.Controls;
using RangHamMat.Views;

internal static class Program
{
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    private static bool CanOpen(MainWindow home, string module)
        => (bool)typeof(MainWindow).GetMethod("CanOpen", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(home, new object[] { module })!;
    private static T Find<T>(FrameworkElement view, string name) where T : FrameworkElement => (T)view.FindName(name);

    private static void SignIn(string job)
    {
        using var db = new DAL_DONTHUOC().MoKetNoi();
        var account = db.QuerySingle<(string Employee, string Account, string Role)>("""
            SELECT TOP(1) n.MaNV Employee,t.MaTK Account,v.TenVaiTro Role FROM dbo.NHAN_VIEN n
            JOIN dbo.TAI_KHOAN t ON t.MaNV=n.MaNV JOIN dbo.VAI_TRO v ON v.MaVaiTro=t.MaVaiTro
            WHERE n.MaChucVu=@job AND n.TrangThai=N'Đang làm việc' AND t.TrangThai=N'Đang hoạt động' ORDER BY n.MaNV
            """, new { job });
        PhienDangNhap.MaNV = account.Employee;
        PhienDangNhap.IsAdmin = account.Role == "Admin";
        PhienDangNhap.KhoiTaoDinhDanhNghiepVu(account.Account);
    }

    [STAThread]
    private static int Main()
    {
        var configured = Environment.GetEnvironmentVariable("RHM_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(configured) || !new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(configured).InitialCatalog.StartsWith("RHM_ClinicalWorkflow_Test_", StringComparison.OrdinalIgnoreCase))
        { Console.Error.WriteLine("FAIL: ClinicalUiChecks chỉ chạy với RHM_CONNECTION_STRING của bản sao RHM_ClinicalWorkflow_Test_*."); return 1; }
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/RangHamMat.Wpf;component/Themes/Theme.xaml", UriKind.Relative) });
        KiemTraNhapLieu.Register();
        var pass = 0; var fail = 0;
        void Check(string name, Action run)
        {
            try { run(); Console.WriteLine("PASS: " + name); pass++; }
            catch (Exception ex) { Console.Error.WriteLine("FAIL: " + name + ": " + ex.GetBaseException().Message); fail++; }
        }
        try
        {
            Check("Quyền mở năm module lâm sàng tách khỏi quyền module cũ", () =>
            {
                SignIn("CV002");
                var home = new MainWindow();
                try
                {
                    foreach (var module in new[] { "BenhNhan", "KhamDieuTri", "DonThuoc", "DichVuThucHien", "HoaDon" })
                        Require(CanOpen(home, module), "Bác sĩ không mở được " + module);
                    foreach (var module in new[] { "NhanSu", "BaoCao", "NhapKho", "XuatKho", "LichKham", "TaiKhoan" })
                        Require(!CanOpen(home, module), "Quyền ngoài phạm vi bị mở rộng: " + module);
                    foreach (var module in new[] { "TongQuan", "CaLamViec", "XinNghiViec" }) Require(CanOpen(home, module), "Mất quyền User baseline.");
                }
                finally { home.Close(); }
            });
            Check("SQL scope cấp vào view khám, bác sĩ không sửa hồ sơ hành chính", () =>
            {
                SignIn("CV002"); DuLieuGiaoDien.ChonPhamViLamSang(true);
                var patients = new BenhNhan(); patients.ConfigureAccess(_ => true);
                Require(!Find<Button>(patients, "AddPatientButton").IsEnabled, "Quyền mở hồ sơ bị dùng làm quyền tạo BN.");
                var visits = DuLieuGiaoDien.Rows("LICH_SU_KHAM_BENH");
                Require(visits.All(v => UiRecord.Text(v, "TrangThai") == "Đã xác nhận" || UiRecord.Text(v, "MaBS") == PhienDangNhap.MaNV), "View nhận nháp của bác sĩ khác.");
                var page = new KhamDieuTri();
                page.FocusContext(null, "__missing__");
                Require(Find<ContentControl>(page, "DetailHost").Content is null, "Mã không tồn tại tự chọn lượt khác.");
                var patient = DuLieuGiaoDien.Rows("BENH_NHAN").FirstOrDefault();
                if (patient is not null)
                {
                    var profile = new HoSoBenhNhan(patient); profile.ConfigureAccess(_ => true);
                    Require(!Find<Button>(profile, "EditPatientButton").IsEnabled && !Find<Button>(profile, "ReceptionButton").IsEnabled, "Bác sĩ được tự tiếp nhận/sửa BN.");
                }
                else Require(Find<TextBlock>(patients,"ListEmpty").Visibility == Visibility.Visible, "Chưa được phân BN phải hiển thị rỗng.");
            });
            Check("Đổi phiên xóa collection cũ trước khi tải phạm vi mới", () =>
            {
                SignIn("CV004"); DuLieuGiaoDien.ChonPhamViLamSang(true);
                var old = DuLieuGiaoDien.Rows("BENH_NHAN"); Require(old.Count > 0, "Baseline thiếu BN.");
                SignIn("CV002");
                Require(old.Count == 0, "Cache phiên tiếp nhận còn nguyên sau đổi phiên.");
                var own = DuLieuGiaoDien.Rows("BENH_NHAN");
                using var db = new DAL_DONTHUOC().MoKetNoi();
                var ids = db.Query<string>("SELECT DISTINCT hs.MaBN FROM dbo.HO_SO_BENH_AN hs JOIN dbo.PHIEU_KHAM_BENH p ON p.MaHS=hs.MaHS WHERE p.MaBS=@id AND p.TrangThai<>N'Hủy'", new { id = PhienDangNhap.MaNV }).ToHashSet();
                Require(own.All(p => ids.Contains(UiRecord.Text(p, "MaBN"))), $"Cache mới có BN ngoài phân công. Phiên={PhienDangNhap.MaNV}, nguồn={DuLieuGiaoDien.NguoiDungLamSang.MaNV}, quyền={DuLieuGiaoDien.NguoiDungLamSang.Quyen}, ngoài phạm vi={string.Join(',',own.Select(p=>UiRecord.Text(p,"MaBN")).Where(id=>!ids.Contains(id)))}");
            });
            Check("Lịch hẹn trong hồ sơ không gọi nguồn toàn viện trước lọc", () =>
            {
                SignIn("CV002"); DuLieuGiaoDien.ChonPhamViLamSang(true);
                var view = new LichKham();
                var rows=(IEnumerable<ET_LICHKHAM_HIENTHI>)typeof(LichKham).GetField("danhSach",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(view)!;
                var allowed=DuLieuGiaoDien.Rows("LICH_KHAM").Select(r=>UiRecord.Text(r,"MaLich")).ToHashSet();
                Require(rows.All(r=>allowed.Contains(r.MaLich)),"Control lịch nhúng tải lịch ngoài phạm vi dù tab bị ẩn.");
            });
            Check("Lịch khám độc lập giữ nguồn và thao tác cũ", () =>
            {
                SignIn("CV001"); DuLieuGiaoDien.ChonPhamViLamSang(false);
                var view=new LichKham();
                var rows=(IEnumerable<ET_LICHKHAM_HIENTHI>)typeof(LichKham).GetField("danhSach",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(view)!;
                using var db=new DAL_DONTHUOC().MoKetNoi();
                Require(rows.Count()==db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.LICH_KHAM"),"Màn hình lịch độc lập bị áp bộ lọc lâm sàng.");
                Require(Find<Button>(view,"AddAppointmentButton").IsEnabled,"Màn hình lịch độc lập mất thao tác cũ.");
            });
            Check("Dược sĩ có view đơn chỉ đọc, không ký hoặc tạo đơn", () =>
            {
                SignIn("CV005"); DuLieuGiaoDien.ChonPhamViLamSang(true);
                var record = DuLieuGiaoDien.Rows("DON_THUOC").First();
                var view = new ChiTietDonThuoc(record); view.ConfigureAccess(_ => true);
                foreach (var name in new[] { "EditButton", "ConfirmButton", "CancelButton", "NewButton" }) Require(!Find<Button>(view, name).IsEnabled, "Dược sĩ được sửa đơn: " + name);
            });
            Check("Gọi ghi trực tiếp qua cache vẫn bị DAL từ chối và rollback", () =>
            {
                SignIn("CV002"); DuLieuGiaoDien.ChonPhamViLamSang(true);
                using var db = new DAL_DONTHUOC().MoKetNoi();
                var count = db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.BENH_NHAN");
                var rejected = false;
                try { DuLieuGiaoDien.Upsert("BENH_NHAN", new Dictionary<string,object?> { ["HoTenBN"]="FIXTURE KHÔNG ĐƯỢC LƯU",["TrangThai"]="Đang theo dõi" }); }
                catch (TuChoiQuyenNghiepVuException) { rejected = true; }
                Require(rejected && db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.BENH_NHAN") == count, "Bỏ nút UI có thể tạo BN ngoài quyền.");
                var order = db.QueryFirst<ET_DONTHUOC>("SELECT TOP(1) * FROM dbo.DON_THUOC WHERE MaBS<>@id ORDER BY MaDT", new { id=PhienDangNhap.MaNV });
                var oldState=order.TrangThai;
                rejected=false;
                try { DuLieuGiaoDien.SetValue("DON_THUOC",DuLieuGiaoDien.Values(order),"TrangThai","Hủy"); }
                catch (TuChoiQuyenNghiepVuException) { rejected=true; }
                Require(rejected && db.ExecuteScalar<string>("SELECT TrangThai FROM dbo.DON_THUOC WHERE MaDT=@id",new{id=order.MaDT})==oldState,"Đổi trạng thái bỏ qua quyền bác sĩ của đơn.");
            });
            Check("Hóa đơn lịch sử chỉ đọc, không cấp hoặc thu lại qua nút cũ", () =>
            {
                SignIn("CV007"); DuLieuGiaoDien.ChonPhamViLamSang(true);
                var page = new HoaDon();
                using var host = new WindowLifetime(new Window { Content = page, Width = 1366, Height = 768, ShowInTaskbar = false });
                host.Window.Show(); host.Window.UpdateLayout();
                var list = Find<ListBox>(page, "InvoiceList");
                if (list.Items.Count > 0) list.SelectedIndex = 0;
                Require(!Find<Button>(page, "CreateButton").IsEnabled && !Find<Button>(page, "DispenseButton").IsEnabled, "Luồng ghi cũ còn bật.");
                Require(!Find<FrameworkElement>(page, "PaymentForm").IsEnabled, "Thu từng phần cũ vẫn dùng được.");
            });
        }
        finally { app.Shutdown(); }
        Console.WriteLine($"ClinicalUiChecks: {pass} PASS, {fail} FAIL; không ghi fixture/chứng từ SQL.");
        return fail == 0 ? 0 : 1;
    }

    private sealed class WindowLifetime(Window window) : IDisposable
    {
        public Window Window { get; } = window;
        public void Dispose() => Window.Close();
    }
}
