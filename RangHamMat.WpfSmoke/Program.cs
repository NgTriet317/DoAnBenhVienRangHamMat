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

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        // Real WPF views and real SQL data; all write probes roll back.
        // Regressions: read-only history binding throws; patient-scoped appointment offers a new patient.
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Window? host = null;
        try
        {
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/RangHamMat.Wpf;component/Themes/Theme.xaml", UriKind.Relative) });
            ClinicalData.RefreshFromDatabase();
            var patients = ClinicalData.Rows("BENH_NHAN").OrderBy(row => UiRecord.Text(row, "MaBN")).ToArray();
            var patient = patients.First(row=>ClinicalData.Rows("LICH_SU_KHAM_BENH").Any(visit=>ClinicalData.PatientId("LICH_SU_KHAM_BENH",visit)==UiRecord.Text(row,"MaBN")));
            var profile = new HoSoBenhNhan(patient);
            host = new Window { Content = profile, Width = 1366, Height = 768, ShowInTaskbar = false, Title = "Kiểm tra lịch sử khám" };
            host.Show(); Drain(host);
            var tabs = (TabControl)profile.FindName("ProfileTabs");
            tabs.SelectedItem = profile.FindName("HistoryTab"); Drain(host);
            var history = (DataGrid)profile.FindName("HistoryGrid");
            var patientId=UiRecord.Text(patient,"MaBN");
            var visits=ClinicalData.Rows("LICH_SU_KHAM_BENH").Where(visit=>ClinicalData.PatientId("LICH_SU_KHAM_BENH",visit)==patientId).ToArray();
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
            Require(serviceGrid.Items.OfType<ClinicalVisitRow>().All(row=>row.PatientId==patientId && row.VisitId==UiRecord.Text(visits[0],"MaLS")),"Dịch vụ phải thuộc đúng bệnh nhân/lần khám.");
            services.FocusContext(patientId,"__missing__");Drain(host);
            Require(serviceGrid.Items.Count==0,"Lần khám không tồn tại không được hiển thị dịch vụ khác.");
            var editor=new CuaSoBenhNhan();ClinicalForm.Configure(editor,"BENH_NHAN");host.Content=editor;Drain(host);
            Require(((DatePicker)editor.FindName("BirthDate")).SelectedDate is null,"Không tự đặt ngày sinh cho bệnh nhân mới.");
            Require(Elements(editor).OfType<FrameworkElement>().Any(element=>Equals(element.Tag,"DB:BENH_NHAN.MaBHYT")),"Form cần có MaBHYT đúng v8.");
            Capture(host,"benh-nhan-form");
            Require(new TreatmentDraft {Quantity=0.005m,Price=1m}.Amount==0.01m,"Tiền nháp phải làm tròn từng dòng giống SQL.");
            var count=ClinicalData.Rows("BENH_NHAN").Count;string? temporaryId=null;
            ClinicalData.Transaction(()=>
            {
                Elements(editor).OfType<TextBox>().Single(box=>Equals(box.Tag,"DB:BENH_NHAN.HoTenBN")).Text="Bệnh nhân chỉ có thông tin bắt buộc";
                Require(ClinicalForm.TrySave(editor,out var minimal,out var minimalError),"Form phải cho phép bỏ trống trường nullable v8: "+minimalError);
                Require(UiRecord.Get(minimal!,"GioiTinh") is null && UiRecord.Get(minimal!,"NgaySinh") is null,"Không tự thêm giới tính/ngày sinh chưa biết.");
                var saved=ClinicalData.Upsert("BENH_NHAN",new Dictionary<string,object?>{{"HoTenBN","Kiểm tra WPF rollback"},{"GioiTinh","Nữ"},{"SDT","0900123456"},{"TrangThai","Đang theo dõi"}});
                temporaryId=UiRecord.Text(saved,"MaBN");
                Require(ClinicalData.Find("BENH_NHAN",temporaryId) is not null,"Cache chỉ nhận bản ghi đã lưu SQL.");
                var first=new CuaSoBenhNhan();var second=new CuaSoBenhNhan();
                ClinicalForm.Configure(first,"BENH_NHAN",saved);ClinicalForm.Configure(second,"BENH_NHAN",saved);
                var firstName=Elements(first).OfType<TextBox>().Single(box=>Equals(box.Tag,"DB:BENH_NHAN.HoTenBN"));
                firstName.Text="Tên mới từ cửa sổ thứ nhất";
                Require(ClinicalForm.TrySave(first,out _,out var firstError),"Cửa sổ thứ nhất phải lưu được: "+firstError);
                Require(!ClinicalForm.TrySave(second,out _,out _),"Cửa sổ cũ phải bị chặn ghi đè tên mới.");
                CheckEditors(host,saved);
                return false;
            });
            Require(ClinicalData.Rows("BENH_NHAN").Count==count && ClinicalData.Find("BENH_NHAN",temporaryId) is null,"Rollback phải phục hồi cache.");
            ClinicalData.RefreshFromDatabase();
            Require(ClinicalData.Find("BENH_NHAN",temporaryId) is null,"Rollback phải phục hồi SQL.");
            CheckFinance(host);
            Console.WriteLine("PASS: SQL → hồ sơ/lịch sử; lịch theo bệnh nhân; phạm vi dịch vụ; form v8; lưu/sửa đơn nhiều dòng; kế hoạch/dịch vụ; hóa đơn/phân trang in; rollback SQL và cache.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL: " + ex); return 1; }
        finally { host?.Close(); app.Shutdown(); }
    }
    private static void CheckEditors(Window host,object patient)
    {
        object Save(string table,params (string,object?)[] values)=>ClinicalData.Upsert(table,values.ToDictionary(v=>v.Item1,v=>v.Item2));
        var doctor=UiRecord.Text(ClinicalData.Rows("NHAN_VIEN").First(),"MaNV");
        var room=UiRecord.Text(ClinicalData.Rows("PHONG").First(),"MaPhong");
        var service=UiRecord.Text(ClinicalData.Rows("DICH_VU").First(),"MaDV");
        var medicine=UiRecord.Text(ClinicalData.Rows("THUOC").First(),"MaThuoc");
        var history=Save("HO_SO_BENH_AN",("MaBN",UiRecord.Get(patient,"MaBN")),("NgayLap",DateTime.Now.AddMinutes(-3)),("LyDoKhamBanDau","Kiểm tra WPF"),("TrangThai","Đang theo dõi"));
        var receipt=Save("PHIEU_KHAM_BENH",("MaHS",UiRecord.Get(history,"MaHS")),("MaPhong",room),("MaBS",doctor),("MaNVTiepNhan",doctor),("SoThuTu",1),("ThoiGianTiepNhan",DateTime.Now.AddMinutes(-2)),("LyDoKham","Kiểm tra"),("TrangThai","Chờ khám"));
        var visit=Save("LICH_SU_KHAM_BENH",("MaPhieu",UiRecord.Get(receipt,"MaPhieu")),("MaBS",doctor),("MaPhong",room),("ThoiGianKham",DateTime.Now.AddMinutes(-1)),("TrieuChung","Kiểm tra"),("ChanDoan","Chẩn đoán kiểm tra"),("KeHoachXuLy","Kiểm tra"),("TrangThai","Nháp"));
        var visitId=UiRecord.Text(visit,"MaLS");
        var rx=new CuaSoDonThuoc(); ClinicalForm.Configure(rx,"DON_THUOC",context:new("DON_THUOC","MaLS",visitId));host.Content=rx;Drain(host);
        Require(((ComboBox)rx.FindName("DoctorBox")).SelectedValue?.ToString()==doctor,"Kê đơn phải tự chọn bác sĩ của lần khám.");
        var lines=(ObservableCollection<RxDraftLine>)((ItemsControl)rx.FindName("MedicineRows")).ItemsSource;
        lines.Clear();lines.Add(new(){MaThuoc=medicine,LieuDung="Sáng một viên",SoLuong="2,5",SoNgayDung="2",HuongDan="Sau ăn"});
        lines.Add(new(){MaThuoc=medicine,LieuDung="Tối một viên",SoLuong="1",SoNgayDung="1",HuongDan="Sau ăn"});
        Require(rx.Commit(out var rxError),"Lưu đơn nhiều dòng: "+rxError);
        var order=ClinicalForm.GetSaved(rx)!;var orderId=UiRecord.Text(order,"MaDT");
        Require(ClinicalData.Children("CHI_TIET_DON_THUOC","MaDT",orderId).Length==2,"Phải lưu đủ hai dòng thuốc.");
        var edit=new CuaSoDonThuoc();ClinicalForm.Configure(edit,"DON_THUOC",order);host.Content=edit;Drain(host);
        var edits=(ObservableCollection<RxDraftLine>)((ItemsControl)edit.FindName("MedicineRows")).ItemsSource;
        edits[0].LieuDung="Liều đã chỉnh";edits.RemoveAt(1);
        Require(edit.Commit(out var editError),"Sửa đơn và xóa dòng: "+editError);
        var actual=ClinicalData.Children("CHI_TIET_DON_THUOC","MaDT",orderId);
        Require(actual.Length==1 && UiRecord.Text(actual[0],"LieuDung")=="Liều đã chỉnh","Sửa đơn phải lưu đúng chi tiết.");
        var plan=new CuaSoKhamDieuTri();ClinicalForm.Configure(plan,"KE_HOACH_DIEU_TRI",context:new("KE_HOACH_DIEU_TRI","MaLS",visitId));host.Content=plan;Drain(host);
        ((ComboBox)Elements(plan).OfType<FrameworkElement>().Single(e=>Equals(e.Tag,"DB:KE_HOACH_DIEU_TRI.MaBS"))).SelectedValue=doctor;
        var steps=(ObservableCollection<TreatmentDraft>)((DataGrid)plan.FindName("PlanDrafts")).ItemsSource;
        steps.Add(new(){MaDV=service,LanThu=1,Quantity=2.5m,Price=10000.50m});steps.Add(new(){MaDV=service,LanThu=2,Quantity=1m,Price=100m});
        Require(plan.Commit(out var planError),"Lưu kế hoạch: "+planError);
        var savedPlan=ClinicalForm.GetSaved(plan)!;
        Require(TreatmentMoney.Number(savedPlan,"TongTienDuKien")==25101.25m,"Tổng kế hoạch phải đúng trigger SQL.");
        var progress=new CuaSoKhamDieuTri();ClinicalForm.Configure(progress,"DICH_VU_THUC_HIEN",context:new("DICH_VU_THUC_HIEN","MaLS",visitId));host.Content=progress;Drain(host);
        var rows=(ObservableCollection<TreatmentDraft>)((DataGrid)progress.FindName("ProgressDrafts")).ItemsSource;
        rows.Add(new(){MaDV=service,Label=ClinicalData.Lookup("MaDV",service),MaNVThucHien=doctor,Quantity=0.005m,Price=1m});rows.Add(new(){MaDV=service,Label=ClinicalData.Lookup("MaDV",service),MaNVThucHien=doctor,Quantity=2.5m,Price=10000.50m});
        Drain(host);Capture(host,"dich-vu-form");
        Require(progress.Commit(out var progressError),"Lưu dịch vụ nhiều dòng: "+progressError);
        var executions=ClinicalData.Children("DICH_VU_THUC_HIEN","MaLS",visitId);
        Require(executions.Length==2 && executions.Sum(row=>TreatmentMoney.Number(row,"ThanhTien"))==25001.26m,"Tiền dịch vụ phải khớp SQL.");
    }
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
        var longInvoice=details.Clone();for(var i=0;i<120;i++)longInvoice.ImportRow(details.Rows[i%details.Rows.Count]);
        var preview=new XemTruocHoaDon(header,longInvoice){ShowInTaskbar=false};
        try
        {
            preview.Show();Drain(preview);var document=(FlowDocument)preview.FindName("InvoiceDocument");
            Capture(preview,"xem-truoc-hoa-don");
            var paginator=((IDocumentPaginatorSource)document).DocumentPaginator;paginator.ComputePageCount();
            Require(paginator.PageCount>1 && paginator.GetPage(paginator.PageCount-1)!=DocumentPage.Missing,"Hóa đơn dài phải phân trang đủ dòng.");
        }
        finally {preview.Close();}
    }
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
    private static void Drain(Window host)
    {
        host.UpdateLayout();
        host.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        host.UpdateLayout();
    }
    private static bool HasVisibleAddPatient(DependencyObject root)
    {
        if (root is Button button && Equals(button.Content, "Thêm bệnh nhân") && button.IsVisible) return true;
        return LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>().Any(HasVisibleAddPatient);
    }
    private static IEnumerable<DependencyObject> Elements(DependencyObject root)
    {
        yield return root;
        foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach(var item in Elements(child)) yield return item;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
