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
using RangHamMat.ClinicalUiChecks;

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
            Check("Lịch sử lịch hẹn không mở nút sửa, xác nhận hoặc hủy",()=>
            {
                SignIn("CV004");DuLieuGiaoDien.ChonPhamViLamSang(true);
                var appointment=DuLieuGiaoDien.Rows("LICH_KHAM").First(v=>UiRecord.Text(v,"TrangThai") is "Nháp" or "Đã xác nhận" or "Chờ khám");
                var detail=new ChiTietHoSo("LICH_KHAM","Lịch sử lịch hẹn",appointment);
                Require(Find<Button>(detail,"EditButton").Visibility!=Visibility.Visible && Find<Button>(detail,"ConfirmButton").Visibility!=Visibility.Visible && Find<Button>(detail,"CancelButton").Visibility!=Visibility.Visible,"Lịch sử chỉ đọc vẫn có nút sửa/hủy.");
            });
            Check("Lần khám gần nhất trên 10000 lượt không quét lại từng hồ sơ",()=>
            {
                SignIn("CV004");DuLieuGiaoDien.ChonPhamViLamSang(true);
                try
                {
                    DuLieuGiaoDien.Replace("BENH_NHAN",Enumerable.Range(0,1000).Select(i=>(object)new Dictionary<string,object?> {{"MaBN","PERFBN"+i},{"HoTenBN","FIXTURE "+i},{"TrangThai","Đang theo dõi"}}));
                    DuLieuGiaoDien.Replace("HO_SO_BENH_AN",Enumerable.Range(0,1000).Select(i=>(object)new Dictionary<string,object?> {{"MaHS","PERFHS"+i},{"MaBN","PERFBN"+i}}));
                    DuLieuGiaoDien.Replace("PHIEU_KHAM_BENH",Enumerable.Range(0,10000).Select(i=>(object)new Dictionary<string,object?> {{"MaPhieu","PERFPH"+i},{"MaHS","PERFHS"+(i%1000)}}));
                    DuLieuGiaoDien.Replace("LICH_SU_KHAM_BENH",Enumerable.Range(0,10000).Select(i=>(object)new Dictionary<string,object?> {{"MaLS","PERFLS"+i},{"MaPhieu","PERFPH"+i},{"ThoiGianKham",DateTime.Today.AddDays(i)}}));
                    var timer=System.Diagnostics.Stopwatch.StartNew();var view=new BenhNhan();var grid=Find<DataGrid>(view,"Grid_BENH_NHAN");
                    foreach(var row in grid.Items.Cast<object>())
                    {
                        Require(UiRecord.Text(row,"LanKhamGanNhat").StartsWith("Khám gần nhất:"),"Mất lần khám gần nhất.");
                        var patientIndex=int.Parse(UiRecord.Text(row,"MaBN")[6..]);
                        Require(UiRecord.Text(row,"LanKhamGanNhat").EndsWith(DateTime.Today.AddDays(9000+patientIndex).ToString("dd/MM/yyyy")),"Lần khám gần nhất trộn bệnh nhân.");
                        Require(timer.Elapsed<TimeSpan.FromSeconds(3),"Trang bệnh nhân vượt 3 giây khi tính lịch sử ("+timer.ElapsedMilliseconds+"ms).");
                    }
                    Console.WriteLine("PERF: 1000 BN/10000 lượt, trang đầu và tính lần khám gần nhất: "+timer.ElapsedMilliseconds+"ms");
                }
                finally {DuLieuGiaoDien.RefreshFromDatabase();}
            });
            Check("WPF tiếp nhận lưu hồ sơ/phiếu rồi mới tải cache và báo hàng đợi",()=>
            {
                SignIn("CV004");
                using(var fixture=new ReceptionFixture())
                {
                    DuLieuGiaoDien.ChonPhamViLamSang(true);
                    var view=new TiepNhanKham();BieuMauHoSo.Configure(view,"PHIEU_KHAM_BENH",patient:fixture.Patient);
                    using var host=new WindowLifetime(new Window {Content=view,Width=850,Height=740,ShowInTaskbar=false});host.Window.Show();host.Window.UpdateLayout();
                    Find<ComboBox>(view,"ServicePicker").SelectedValue=fixture.Service;
                    Find<ComboBox>(view,"DoctorPicker").SelectedValue=fixture.Doctor;
                    Find<TextBox>(view,"ArrivalReason").Text="FIXTURE không dùng thật";
                    Find<TextBox>(view,"ArrivalReason").BringIntoView();host.Window.UpdateLayout();
                    SavePreview(view,"tiep-nhan-truoc-luu.png");
                    Require(view.TryReceive(out var error),error);
                    var receipt=view.SavedResult ?? throw new Exception("Thiếu kết quả lưu.");
                    Require(receipt.MaBN==fixture.Patient && receipt.MaBS==fixture.Doctor,"Sai người bệnh/bác sĩ sau lưu.");
                    Require(DuLieuGiaoDien.Find("PHIEU_KHAM_BENH",receipt.MaPhieu) is not null,"Không tải cache sau commit.");
                    Require(fixture.CountVisits(receipt.MaPhieu)==0 && view.FindName("OpenVisitAfterSave") is null,"Tiếp nhận mở/tạo chẩn đoán.");
                    Require(Find<TextBlock>(view,"SavedStatus").Text.Contains("Đã chuyển vào hàng đợi") && !Find<Button>(view,"SaveReceptionButton").IsEnabled,"Thiếu phản hồi hoặc còn cho gửi lặp.");
                    Require(view.TryReceive(out _) && view.SavedResult==receipt,"Bấm lại không giữ biên nhận.");
                    host.Window.UpdateLayout();SavePreview(view,"tiep-nhan-da-luu.png");
                }
                DuLieuGiaoDien.RefreshFromDatabase();
            });
            Check("Danh sách tìm ngày sinh và giữ mã bệnh nhân khi chọn",()=>
            {
                SignIn("CV004");DuLieuGiaoDien.ChonPhamViLamSang(true);
                var patient=DuLieuGiaoDien.Rows("BENH_NHAN").First(v=>UiRecord.Date(v,"NgaySinh") is not null);
                var view=new BenhNhan();Find<TextBox>(view,"SearchBox").Text=UiRecord.Date(patient,"NgaySinh")!.Value.ToString("dd/MM/yyyy");
                var list=Find<DataGrid>(view,"Grid_BENH_NHAN");Require(list.Items.Cast<object>().Any(v=>UiRecord.Text(v,"MaBN")==UiRecord.Text(patient,"MaBN")),"Tìm theo ngày sinh không có đúng BN.");
                view.OpenProfile(patient);Require(view.PatientId==UiRecord.Text(patient,"MaBN"),"Lựa chọn không theo mã BN.");
            });
            Check("Tiếp nhận đổi dịch vụ xóa bác sĩ/phòng, không cho nhập nhân viên và số thứ tự", () =>
            {
                SignIn("CV004"); DuLieuGiaoDien.ChonPhamViLamSang(true);
                var patient=DuLieuGiaoDien.Rows("BENH_NHAN").First();
                var view=new TiepNhanKham();
                BieuMauHoSo.Configure(view,"PHIEU_KHAM_BENH",patient:UiRecord.Text(patient,"MaBN"));
                using var host=new WindowLifetime(new Window {Content=view,Width=850,Height=740,ShowInTaskbar=false});host.Window.Show();host.Window.UpdateLayout();
                var service=Find<ComboBox>(view,"ServicePicker");var doctor=Find<ComboBox>(view,"DoctorPicker");var room=Find<ComboBox>(view,"RoomPicker");
                foreach(var item in service.Items.Cast<object>().ToArray())
                {
                    service.SelectedItem=item;
                    if(doctor.Items.Count>0) {doctor.SelectedIndex=0;break;}
                }
                Require(doctor.SelectedItem is not null,"Baseline cần bác sĩ phù hợp ít nhất một dịch vụ.");
                service.SelectedIndex=-1;
                Require(doctor.SelectedItem is null && room.SelectedItem is null,"Đổi dịch vụ còn bác sĩ/phòng cũ.");
                Require(Find<TextBox>(view,"QueueNumber").IsReadOnly,"Cho nhập số thứ tự từ UI.");
                Require(Find<ComboBox>(view,"AppointmentPicker").Items.Count>0,"Thiếu nguồn trực tiếp/lịch hẹn.");
                Require(Find<TextBlock>(view,"SystemInfo").Text.Contains("tự cấp"),"Không giải thích giờ/số do hệ thống cấp.");
            });
            Check("Bốn tab hồ sơ cho xem lịch hẹn dù không mở module lập lịch",()=>
            {
                SignIn("CV004");DuLieuGiaoDien.ChonPhamViLamSang(true);
                var view=new HoSoBenhNhan(DuLieuGiaoDien.Rows("BENH_NHAN").First());view.ConfigureAccess(module=>module!="LichKham");
                Require(Find<TabControl>(view,"ProfileTabs").Items.Count==4,"Không đủ bốn tab hồ sơ.");
                Require(Find<TabItem>(view,"AppointmentsTab").Visibility==Visibility.Visible,"Lịch sử hẹn bị che theo quyền lập lịch.");
                Require(!Find<Button>(Find<LichKham>(view,"PatientAppointments"),"AddAppointmentButton").IsEnabled,"Lịch sử hẹn cho sửa ngoài quyền.");
            });
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
        Console.WriteLine($"ClinicalUiChecks: {pass} PASS, {fail} FAIL; fixture tiếp nhận đã dọn, không giữ chứng từ SQL.");
        return fail == 0 ? 0 : 1;
    }

    private sealed class WindowLifetime(Window window) : IDisposable
    {
        public Window Window { get; } = window;
        public void Dispose() => Window.Close();
    }

    private static void SavePreview(FrameworkElement view,string name)
    {
        var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(view.ActualWidth),(int)Math.Ceiling(view.ActualHeight),96,96,System.Windows.Media.PixelFormats.Pbgra32);
        bitmap.Render(view);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        System.IO.Directory.CreateDirectory("obj/ClinicalWorkflowP04");
        using var file=System.IO.File.Create(System.IO.Path.Combine("obj/ClinicalWorkflowP04",name));encoder.Save(file);
    }
}
