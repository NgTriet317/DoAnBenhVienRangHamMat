// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BUS;
using RangHamMat.Controls;
using RangHamMat.Views;

/// <summary>
/// Điểm chạy bộ kiểm tra WPF: mở control thật, kiểm tra binding/lưu/rollback/in và xuất ảnh kết quả trên Windows.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Khởi tạo Application WPF trên luồng STA, nạp theme và SQL; thử luồng hồ sơ/form/tài chính, báo PASS/FAIL và luôn đóng cửa sổ/ứng dụng trong finally.
    /// </summary>
    [STAThread]
    private static int Main(string[] args)
    {
        // Dùng control WPF và dữ liệu SQL thật; các thao tác ghi thử đều được rollback.
        // Kiểm tra lỗi cũ: binding lịch sử chỉ đọc và nút thêm bệnh nhân trong lịch thuộc một hồ sơ.
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Window? host = null;
        try
        {
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/RangHamMat.Wpf;component/Themes/Theme.xaml", UriKind.Relative) });
            KiemTraNhapLieu.Register();
            DuLieuGiaoDien.RefreshFromDatabase();
            if (args.Contains("--validation"))
            {
                host = new Window { Width = 1100, Height = 760, ShowInTaskbar = false, Title = "Kiểm tra dữ liệu nhập" };
                host.Show();
                NhapLieuChecks.Run(host);
                return 0;
            }
            var patients = DuLieuGiaoDien.Rows("BENH_NHAN").OrderBy(row => UiRecord.Text(row, "MaBN")).ToArray();
            var patient = patients.First(row=>DuLieuGiaoDien.Rows("LICH_SU_KHAM_BENH").Any(visit=>DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH",visit)==UiRecord.Text(row,"MaBN")));
            var profile = new HoSoBenhNhan(patient);
            host = new Window { Content = profile, Width = 1366, Height = 768, ShowInTaskbar = false, Title = "Kiểm tra lịch sử khám" };
            host.Show(); Drain(host);
            var tabs = (TabControl)profile.FindName("ProfileTabs");
            tabs.SelectedItem = profile.FindName("HistoryTab"); Drain(host);
            var history = (DataGrid)profile.FindName("HistoryGrid");
            var patientId=UiRecord.Text(patient,"MaBN");
            var visits=DuLieuGiaoDien.Rows("LICH_SU_KHAM_BENH").Where(visit=>DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH",visit)==patientId).ToArray();
            Require(history.Items.Count == visits.Length, "Lịch sử phải khớp dữ liệu SQL của bệnh nhân.");
            history.SelectedIndex = 0; Drain(host);
            var details = ((ContentControl)profile.FindName("VisitHost")).Content as ChiTietKhamDieuTri;
            Require(details is not null && details.PatientId == UiRecord.Text(patient, "MaBN"), "Chọn lịch sử phải mở đúng bệnh nhân.");
            tabs.SelectedItem = profile.FindName("AppointmentsTab"); Drain(host);
            var scoped = (LichKham)profile.FindName("PatientAppointments");
            Require(!HasVisibleAddPatient(scoped), "Lịch trong hồ sơ không được có nút Thêm bệnh nhân.");
            var global = new LichKham(); host.Content = global; Drain(host);
            Require(HasVisibleAddPatient(global), "Mục Lịch khám độc lập phải giữ nút Thêm bệnh nhân.");
            var services=new DichVuThucHien();host.Content=services;services.FocusContext(patientId,UiRecord.Text(visits[0],"MaLS"));Drain(host);
            var serviceGrid=(DataGrid)services.FindName("Grid_DICH_VU_THUC_HIEN");
            Require(serviceGrid.Items.OfType<DongChiTietLanKham>().All(row=>row.PatientId==patientId && row.VisitId==UiRecord.Text(visits[0],"MaLS")),"Dịch vụ phải thuộc đúng bệnh nhân/lần khám.");
            services.FocusContext(patientId,"__missing__");Drain(host);
            Require(serviceGrid.Items.Count==0,"Lần khám không tồn tại không được hiển thị dịch vụ khác.");
            var editor=new CuaSoBenhNhan();BieuMauHoSo.Configure(editor,"BENH_NHAN");host.Content=editor;Drain(host);
            Require(((DatePicker)editor.FindName("BirthDate")).SelectedDate is null,"Không tự đặt ngày sinh cho bệnh nhân mới.");
            Require(Elements(editor).OfType<FrameworkElement>().Any(element=>Equals(element.Tag,"DB:BENH_NHAN.MaBHYT")),"Form cần có MaBHYT đúng v8.");
            Capture(host,"benh-nhan-form");
            Require(new TreatmentDraft {Quantity=0.005m,Price=1m}.Amount==0.01m,"Tiền nháp phải làm tròn từng dòng giống SQL.");
            var count=DuLieuGiaoDien.Rows("BENH_NHAN").Count;string? temporaryId=null;
            DuLieuGiaoDien.Transaction(()=>
            {
                Elements(editor).OfType<TextBox>().Single(box=>Equals(box.Tag,"DB:BENH_NHAN.HoTenBN")).Text="Bệnh nhân chỉ có thông tin bắt buộc";
                Require(BieuMauHoSo.TrySave(editor,out var minimal,out var minimalError),"Form phải cho phép bỏ trống trường nullable v8: "+minimalError);
                Require(UiRecord.Get(minimal!,"GioiTinh") is null && UiRecord.Get(minimal!,"NgaySinh") is null,"Không tự thêm giới tính/ngày sinh chưa biết.");
                var saved=DuLieuGiaoDien.Upsert("BENH_NHAN",new Dictionary<string,object?>{{"HoTenBN","Kiểm tra WPF rollback"},{"GioiTinh","Nữ"},{"SDT","0900123456"},{"TrangThai","Đang theo dõi"}});
                temporaryId=UiRecord.Text(saved,"MaBN");
                Require(DuLieuGiaoDien.Find("BENH_NHAN",temporaryId) is not null,"Cache chỉ nhận bản ghi đã lưu SQL.");
                var first=new CuaSoBenhNhan();var second=new CuaSoBenhNhan();
                BieuMauHoSo.Configure(first,"BENH_NHAN",saved);BieuMauHoSo.Configure(second,"BENH_NHAN",saved);
                var firstName=Elements(first).OfType<TextBox>().Single(box=>Equals(box.Tag,"DB:BENH_NHAN.HoTenBN"));
                firstName.Text="Tên mới từ cửa sổ thứ nhất";
                Require(BieuMauHoSo.TrySave(first,out _,out var firstError),"Cửa sổ thứ nhất phải lưu được: "+firstError);
                Require(!BieuMauHoSo.TrySave(second,out _,out _),"Cửa sổ cũ phải bị chặn ghi đè tên mới.");
                CheckEditors(host,saved);
                return false;
            });
            Require(DuLieuGiaoDien.Rows("BENH_NHAN").Count==count && DuLieuGiaoDien.Find("BENH_NHAN",temporaryId) is null,"Rollback phải phục hồi cache.");
            DuLieuGiaoDien.RefreshFromDatabase();
            Require(DuLieuGiaoDien.Find("BENH_NHAN",temporaryId) is null,"Rollback phải phục hồi SQL.");
            CheckSqlAbort();
            CheckFinance(host);
            GiaoDienXamlChecks.Run(host);
            NhapLieuChecks.Run(host);
            Console.WriteLine("PASS: SQL → hồ sơ/lịch sử; lịch theo bệnh nhân; phạm vi dịch vụ; form v8; lưu/sửa đơn nhiều dòng; kế hoạch/dịch vụ; hóa đơn/phân trang in; rollback SQL và cache.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL: " + ex); return 1; }
        finally { host?.Close(); app.Shutdown(); }
    }
    /// <summary>
    /// Thử lỗi trigger và ngắt kết nối giữa giao dịch; kiểm tra cả SQL lẫn cache đã rollback và vẫn có thể mở giao dịch mới sau lỗi.
    /// </summary>
    private static void CheckSqlAbort()
    {
        var count = DuLieuGiaoDien.Rows("BENH_NHAN").Count;
        var doctor = UiRecord.Text(DuLieuGiaoDien.Rows("NHAN_VIEN").First(), "MaNV");
        var room = UiRecord.Text(DuLieuGiaoDien.Rows("PHONG").First(), "MaPhong");
        // Gộp các cặp tên cột/giá trị thành dictionary rồi gọi luồng lưu dùng chung của ca thử; nằm trong transaction sẽ rollback.
        object Save(string table, params (string, object?)[] values) => DuLieuGiaoDien.Upsert(table, values.ToDictionary(v => v.Item1, v => v.Item2));
        var rejected = false;
        try
        {
            DuLieuGiaoDien.Transaction(() =>
            {
                var patient = Save("BENH_NHAN", ("HoTenBN", "Kiểm tra lỗi trigger"), ("TrangThai", "Đang theo dõi"));
                var history = Save("HO_SO_BENH_AN", ("MaBN", UiRecord.Get(patient, "MaBN")), ("NgayLap", DateTime.Now), ("LyDoKhamBanDau", "Kiểm tra"), ("TrangThai", "Đang theo dõi"));
                var receipt = Save("PHIEU_KHAM_BENH", ("MaHS", UiRecord.Get(history, "MaHS")), ("MaPhong", room), ("MaBS", doctor), ("MaNVTiepNhan", doctor), ("SoThuTu", 1), ("ThoiGianTiepNhan", DateTime.Now), ("LyDoKham", "Kiểm tra"), ("TrangThai", "Chờ khám"));
                // Trigger SQL hủy giao dịch vì thời gian khám không được trước thời gian tiếp nhận.
                Save("LICH_SU_KHAM_BENH", ("MaPhieu", UiRecord.Get(receipt, "MaPhieu")), ("MaBS", doctor), ("MaPhong", room), ("ThoiGianKham", DateTime.Now.AddDays(-1)), ("TrieuChung", ""), ("ChanDoan", ""), ("KeHoachXuLy", ""), ("TrangThai", "Nháp"));
                return true;
            });
        }
        catch (InvalidOperationException) { rejected = true; }
        Require(rejected, "SQL trigger phải chặn lần khám trước giờ tiếp nhận.");
        DuLieuGiaoDien.RefreshFromDatabase();
        Require(DuLieuGiaoDien.Rows("BENH_NHAN").Count == count, "Lỗi trigger không được lưu bệnh nhân hoặc khóa cache.");
        DuLieuGiaoDien.Transaction(() => { Save("BENH_NHAN", ("HoTenBN", "Lưu sau lỗi SQL"), ("TrangThai", "Đang theo dõi")); return false; });
        Require(DuLieuGiaoDien.Rows("BENH_NHAN").Count == count, "Sau lỗi SQL phải mở được giao dịch mới và rollback cache.");
        try
        {
            DuLieuGiaoDien.Transaction(() =>
            {
                Save("BENH_NHAN", ("HoTenBN", "Kiểm tra ngắt kết nối"), ("TrangThai", "Đang theo dõi"));
                // Cố ý gây lỗi bằng cách đóng riêng kết nối của giao dịch thử hiện tại.
                var pending = (IDbTransaction)typeof(DuLieuGiaoDien).GetField("transaction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.GetValue(null)!;
                pending.Connection!.Close();
                return false;
            });
        }
        catch (InvalidOperationException) { }
        DuLieuGiaoDien.RefreshFromDatabase();
        Require(DuLieuGiaoDien.Rows("BENH_NHAN").Count == count, "Mất kết nối không được giữ cache trong trạng thái đang lưu.");
    }

    /// <summary>
    /// Thử lưu/sửa đơn nhiều dòng, kế hoạch và dịch vụ thực hiện; kiểm tra tiền làm tròn và dữ liệu SQL từ các form WPF thật.
    /// </summary>
    private static void CheckEditors(Window host,object patient)
    {
        // Gộp các cặp tên cột/giá trị thành dictionary rồi gọi luồng lưu dùng chung của ca thử; nằm trong transaction sẽ rollback.
        object Save(string table,params (string,object?)[] values)=>DuLieuGiaoDien.Upsert(table,values.ToDictionary(v=>v.Item1,v=>v.Item2));
        var doctor=UiRecord.Text(DuLieuGiaoDien.Rows("NHAN_VIEN").First(),"MaNV");
        var room=UiRecord.Text(DuLieuGiaoDien.Rows("PHONG").First(),"MaPhong");
        var service=UiRecord.Text(DuLieuGiaoDien.Rows("DICH_VU").First(),"MaDV");
        var medicine=UiRecord.Text(DuLieuGiaoDien.Rows("THUOC").First(),"MaThuoc");
        var history=Save("HO_SO_BENH_AN",("MaBN",UiRecord.Get(patient,"MaBN")),("NgayLap",DateTime.Now.AddMinutes(-3)),("LyDoKhamBanDau","Kiểm tra WPF"),("TrangThai","Đang theo dõi"));
        var receipt=Save("PHIEU_KHAM_BENH",("MaHS",UiRecord.Get(history,"MaHS")),("MaPhong",room),("MaBS",doctor),("MaNVTiepNhan",doctor),("SoThuTu",1),("ThoiGianTiepNhan",DateTime.Now.AddMinutes(-2)),("LyDoKham","Kiểm tra"),("TrangThai","Chờ khám"));
        var visit=Save("LICH_SU_KHAM_BENH",("MaPhieu",UiRecord.Get(receipt,"MaPhieu")),("MaBS",doctor),("MaPhong",room),("ThoiGianKham",DateTime.Now.AddMinutes(-1)),("TrieuChung","Kiểm tra"),("ChanDoan","Chẩn đoán kiểm tra"),("KeHoachXuLy","Kiểm tra"),("TrangThai","Nháp"));
        var visitId=UiRecord.Text(visit,"MaLS");
        var rx=new CuaSoDonThuoc(); BieuMauHoSo.Configure(rx,"DON_THUOC",context:new("DON_THUOC","MaLS",visitId));host.Content=rx;Drain(host);
        Require(((ComboBox)rx.FindName("DoctorBox")).SelectedValue?.ToString()==doctor,"Kê đơn phải tự chọn bác sĩ của lần khám.");
        var lines=(ObservableCollection<RxDraftLine>)((ItemsControl)rx.FindName("MedicineRows")).ItemsSource;
        lines.Clear();lines.Add(new(){MaThuoc=medicine,LieuDung="Sáng một viên",SoLuong="2,5",SoNgayDung="2",HuongDan="Sau ăn"});
        lines.Add(new(){MaThuoc=medicine,LieuDung="Tối một viên",SoLuong="1",SoNgayDung="1",HuongDan="Sau ăn"});
        Require(rx.Commit(out var rxError),"Lưu đơn nhiều dòng: "+rxError);
        var order=BieuMauHoSo.GetSaved(rx)!;var orderId=UiRecord.Text(order,"MaDT");
        Require(DuLieuGiaoDien.Children("CHI_TIET_DON_THUOC","MaDT",orderId).Length==2,"Phải lưu đủ hai dòng thuốc.");
        var edit=new CuaSoDonThuoc();BieuMauHoSo.Configure(edit,"DON_THUOC",order);host.Content=edit;Drain(host);
        var edits=(ObservableCollection<RxDraftLine>)((ItemsControl)edit.FindName("MedicineRows")).ItemsSource;
        edits[0].LieuDung="Liều đã chỉnh";edits.RemoveAt(1);
        Require(edit.Commit(out var editError),"Sửa đơn và xóa dòng: "+editError);
        var actual=DuLieuGiaoDien.Children("CHI_TIET_DON_THUOC","MaDT",orderId);
        Require(actual.Length==1 && UiRecord.Text(actual[0],"LieuDung")=="Liều đã chỉnh","Sửa đơn phải lưu đúng chi tiết.");
        var plan=new CuaSoKhamDieuTri();BieuMauHoSo.Configure(plan,"KE_HOACH_DIEU_TRI",context:new("KE_HOACH_DIEU_TRI","MaLS",visitId));host.Content=plan;Drain(host);
        ((ComboBox)Elements(plan).OfType<FrameworkElement>().Single(e=>Equals(e.Tag,"DB:KE_HOACH_DIEU_TRI.MaBS"))).SelectedValue=doctor;
        var steps=(ObservableCollection<TreatmentDraft>)((DataGrid)plan.FindName("PlanDrafts")).ItemsSource;
        steps.Add(new(){MaDV=service,LanThu=1,Quantity=2.5m,Price=10000.50m});steps.Add(new(){MaDV=service,LanThu=2,Quantity=1m,Price=100m});
        Require(plan.Commit(out var planError),"Lưu kế hoạch: "+planError);
        var savedPlan=BieuMauHoSo.GetSaved(plan)!;
        Require(TreatmentMoney.Number(savedPlan,"TongTienDuKien")==25101.25m,"Tổng kế hoạch phải đúng trigger SQL.");
        var progress=new CuaSoKhamDieuTri();BieuMauHoSo.Configure(progress,"DICH_VU_THUC_HIEN",context:new("DICH_VU_THUC_HIEN","MaLS",visitId));host.Content=progress;Drain(host);
        var rows=(ObservableCollection<TreatmentDraft>)((DataGrid)progress.FindName("ProgressDrafts")).ItemsSource;
        rows.Add(new(){MaDV=service,Label=DuLieuGiaoDien.Lookup("MaDV",service),MaNVThucHien=doctor,Quantity=0.005m,Price=1m});rows.Add(new(){MaDV=service,Label=DuLieuGiaoDien.Lookup("MaDV",service),MaNVThucHien=doctor,Quantity=2.5m,Price=10000.50m});
        Drain(host);Capture(host,"dich-vu-form");
        Require(progress.Commit(out var progressError),"Lưu dịch vụ nhiều dòng: "+progressError);
        var executions=DuLieuGiaoDien.Children("DICH_VU_THUC_HIEN","MaLS",visitId);
        Require(executions.Length==2 && executions.Sum(row=>TreatmentMoney.Number(row,"ThanhTien"))==25001.26m,"Tiền dịch vụ phải khớp SQL.");
    }
    /// <summary>
    /// Kiểm tra danh sách hóa đơn đúng bệnh nhân/lần khám, ngữ cảnh form lập hóa đơn và phân trang bản in hóa đơn dài.
    /// </summary>
    private static void CheckFinance(Window host)
    {
        var finance=new BUS_HOADON();var invoices=finance.GetInvoices();Require(invoices.Rows.Count>0,"Cần hóa đơn SQL để kiểm tra màn hình.");
        var header=invoices.Rows.Cast<DataRow>().First(row=>finance.GetInvoiceDetails(Convert.ToString(row["MaHD"])!).Rows.Count>0);var patient=Convert.ToString(header["MaBN"])!;var visit=Convert.ToString(header["MaLS"])!;
        var page=new HoaDon();host.Content=page;page.FocusContext(patient,visit);Drain(host);
        var list=(ListBox)page.FindName("InvoiceList");
        Require(list.Items.Count>0 && list.Items.OfType<DataRowView>().All(row=>Convert.ToString(row["MaBN"])==patient&&Convert.ToString(row["MaLS"])==visit),"Hóa đơn phải đúng bệnh nhân/lần khám.");
        list.SelectedIndex=0;Drain(host);Capture(host,"hoa-don");
        page.FocusContext(patient,"__missing__");Drain(host);Require(list.Items.Count==0,"Lần khám sai không được trả tất cả hóa đơn.");
        var editor=new CuaSoHoaDon();editor.ConfigureContext(patient,visit);host.Content=editor;Drain(host);
        Require(((ComboBox)editor.FindName("PatientBox")).SelectedValue?.ToString()==patient,"Form hóa đơn phải giữ bệnh nhân nguồn.");
        var details=finance.GetInvoiceDetails(Convert.ToString(header["MaHD"])!);
        var longInvoice=details.Clone();longInvoice.Columns["NoiDung"]!.MaxLength=-1;
        for(var i=0;i<120;i++)
        {
            longInvoice.ImportRow(details.Rows[i%details.Rows.Count]);
            longInvoice.Rows[i]["NoiDung"]=$"Dòng hóa đơn {i:D3}: "+details.Rows[i%details.Rows.Count]["NoiDung"];
        }
        var longDescription="Mô tả dài bắt đầu\n"+string.Join("\n",Enumerable.Repeat("Nội dung phải được in đủ khi một dòng vượt chiều cao trang.",180))+"\nMô tả dài kết thúc";
        longInvoice.Rows[58]["NoiDung"]=longDescription;
        var preview=new XemTruocHoaDon(header,longInvoice){ShowInTaskbar=false};
        try
        {
            preview.Show();Drain(preview);var document=(InvoiceDocument)preview.FindName("InvoiceDocument");
            Capture(preview,"xem-truoc-hoa-don");
            var paginator=((IDocumentPaginatorSource)document).DocumentPaginator;paginator.ComputePageCount();
            Require(paginator.PageCount>1 && paginator.GetPage(paginator.PageCount-1)!=DocumentPage.Missing,"Hóa đơn dài phải phân trang đủ dòng.");
            CheckInvoicePages(paginator,longInvoice);
            paginator.PageSize=new Size(640,720);
            CheckInvoicePages(paginator,longInvoice);
            Require(((ItemsControl)document.FindName("InvoiceLines")).Items.Count==120,"Đổi khổ in phải giữ đủ dòng trong bản xem trước.");
        }
        finally {preview.Close();}
    }
    /// <summary>
    /// AI hỗ trợ: kiểm tra visual bản in chứa đủ 120 dòng, mô tả dài, tổng/ký; các dải trang liên tục phủ hết nội dung và không cắt dòng chữ.
    /// </summary>
    private static void CheckInvoicePages(DocumentPaginator paginator,DataTable expected)
    {
        var bottom=0d;
        InvoiceDocument? source=null;
        var slices=new List<Rect>();
        for(var pageIndex=0;pageIndex<paginator.PageCount;pageIndex++)
        {
            var page=paginator.GetPage(pageIndex);
            Require(page!=DocumentPage.Missing,"Mỗi trang đã đếm phải có bản in thật.");
            var brush=((DrawingVisual)page.Visual).Drawing.Children.OfType<GeometryDrawing>().Select(drawing=>drawing.Brush).OfType<VisualBrush>().Single();
            source=(InvoiceDocument)brush.Visual;
            var slice=brush.Viewbox;slices.Add(slice);
            Require(Math.Abs(slice.Top-bottom)<0.01 && slice.Height>0,"Dải trang phải liên tục, không bỏ sót hoặc lặp nội dung.");
            Require(slice.Height<=paginator.PageSize.Height-68+0.01,"Mỗi dải phải nằm trong chiều cao vùng in trừ lề giấy.");
            bottom=slice.Bottom;
        }
        Require(source is not null && Math.Abs(bottom-source.ActualHeight)<0.01,"Các trang phải phủ tới hết tổng tiền và phần ký cuối hóa đơn.");
        var rows=(ItemsControl)source!.FindName("InvoiceLines");
        Require(rows.Items.Count==expected.Rows.Count,"Chứng từ in phải materialize đủ mọi dòng hóa đơn.");
        var textBlocks=InvoiceVisualElements(source).OfType<TextBlock>().ToArray();
        foreach(DataRow row in expected.Rows)
            Require(textBlocks.Any(text=>text.Text==Convert.ToString(row["NoiDung"])),"Bản in phải giữ đầy đủ từng mô tả, kể cả dòng cuối.");
        var longText=textBlocks.Single(text=>text.Text.StartsWith("Mô tả dài bắt đầu",StringComparison.Ordinal));
        var longTop=longText.TranslatePoint(new Point(),source).Y;
        Require(slices.Count(slice=>slice.Top<longTop+longText.ActualHeight && slice.Bottom>longTop)>1,"Mô tả cao hơn trang phải được in nối tiếp qua nhiều trang.");
        Require(textBlocks.Any(text=>text.Text.Contains("Còn phải trả:")) && textBlocks.Any(text=>text.Text=="NHÂN VIÊN THU TIỀN"),"Bản in phải giữ tổng cuối và phần ký.");
        foreach(var text in textBlocks)
        {
            var top=text.TranslatePoint(new Point(),source).Y;
            for(var offset=0d;offset<text.ActualHeight-0.01;offset+=text.LineHeight)
            {
                var lineTop=top+offset;var lineBottom=lineTop+Math.Min(text.LineHeight,text.ActualHeight-offset);
                Require(slices.Any(slice=>slice.Top<=lineTop+0.01 && slice.Bottom>=lineBottom-0.01),$"Một dòng chữ phải nằm trọn trong một trang: {text.Text[..Math.Min(35,text.Text.Length)]}, {lineTop}–{lineBottom}; biên trang {string.Join(", ",slices.Select(slice=>slice.Bottom))}.");
            }
        }
        Require(paginator.GetPage(paginator.PageCount)==DocumentPage.Missing,"Trang ngoài phạm vi phải trả Missing.");
    }
    /// <summary>
    /// AI hỗ trợ: duyệt cây visual thực của chứng từ XAML để kiểm tra các TextBlock trong DataTemplate đã materialize.
    /// </summary>
    private static IEnumerable<DependencyObject> InvoiceVisualElements(DependencyObject root)
    {
        yield return root;
        for(var index=0;index<VisualTreeHelper.GetChildrenCount(root);index++)
            foreach(var child in InvoiceVisualElements(VisualTreeHelper.GetChild(root,index)))yield return child;
    }
    /// <summary>
    /// Render cửa sổ WPF hiện tại thành ảnh PNG để lưu bằng chứng kiểm tra giao diện.
    /// </summary>
    private static void Capture(Window host,string name)
    {
        var directory=System.IO.Path.Combine(Environment.CurrentDirectory,"XemTruoc","verification-v8");System.IO.Directory.CreateDirectory(directory);
        var content=(FrameworkElement)host.Content;
        var bitmap=new RenderTargetBitmap((int)content.ActualWidth,(int)content.ActualHeight,96,96,PixelFormats.Pbgra32);
        var visual=new DrawingVisual();using(var drawing=visual.RenderOpen())
        {
            var bounds=new Rect(0,0,content.ActualWidth,content.ActualHeight);
            drawing.DrawRectangle(host.Background??Brushes.White,null,bounds);drawing.DrawRectangle(new VisualBrush(content),null,bounds);
        }
        bitmap.Render(visual);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output=System.IO.File.Create(System.IO.Path.Combine(directory,name+".png"));encoder.Save(output);
    }
    /// <summary>
    /// Chạy các công việc Dispatcher chờ xử lý rồi cập nhật layout để kiểm tra control sau khi Loaded/binding/selection hoàn tất.
    /// </summary>
    private static void Drain(Window host)
    {
        host.UpdateLayout();
        host.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        host.UpdateLayout();
    }
    /// <summary>
    /// Duyệt control của màn hình lịch để kiểm tra nút Thêm bệnh nhân có đang hiển thị hay không.
    /// </summary>
    private static bool HasVisibleAddPatient(DependencyObject root)
    {
        if (root is Button button && Equals(button.Content, "Thêm bệnh nhân") && button.IsVisible) return true;
        return LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>().Any(HasVisibleAddPatient);
    }
    /// <summary>
    /// Duyệt các phần tử con của giao diện phục vụ kiểm tra control và Tag dữ liệu.
    /// </summary>
    private static IEnumerable<DependencyObject> Elements(DependencyObject root)
    {
        yield return root;
        foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach(var item in Elements(child)) yield return item;
    }
    /// <summary>
    /// Ném InvalidOperationException nếu điều kiện kiểm tra không đạt, giúp chương trình in FAIL và trả mã thoát khác 0.
    /// </summary>
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
