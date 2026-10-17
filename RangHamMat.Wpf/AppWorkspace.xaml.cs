// AI bổ sung kiểm tra nhập liệu: kiểm tra ô đang sửa/ngày trước khi mở transaction, báo lỗi và giữ modal để sửa.
// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ khung làm việc, mở form/modal, lịch sử quay lại và giữ ngữ cảnh bệnh nhân/lần khám khi chuyển màn hình; đã chú thích xử lý nút theo Tag và luồng lưu form.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RangHamMat.Controls;
using RangHamMat.Views;

namespace RangHamMat;

/// <summary>
/// Khung chứa phân hệ: giữ màn hình gốc, lịch sử điều hướng/modal, quyền hiển thị và xử lý các nút có Tag dùng chung.
/// </summary>
public partial class AppWorkspace : UserControl
{
    private readonly MainWindow home;
    private readonly UserControl page;
    private readonly Stack<object> history = new();
    private IInputElement? previousFocus;
    private readonly Stack<object> modalHistory = new();
    private readonly Dictionary<DataGrid, List<DataGridColumn>> extraColumns = new();

    /// <summary>
    /// Khởi tạo AppWorkspace từ XAML.
    /// </summary>
    public AppWorkspace(MainWindow home, string title, string role, UserControl page)
    {
        InitializeComponent();
        this.home = home;
        this.page = page;
        AppTitle.Text = title;
        RoleLabel.Text = role;
        Workspace.Content = page;
        if (page is XinNghiViec)
        {
            btnXinNghiViec.Visibility = Visibility.Collapsed;
            btnTaiLaiDuLieu.Visibility = Visibility.Collapsed;
        }
        if(page is BenhNhan patients) patients.ConfigureAccess(home.CanOpen);
        if(page is KhamDieuTri visits) visits.ConfigureAccess(home.CanOpen);
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape && ModalLayer.Visibility == Visibility.Visible) { ReturnToPage(); e.Handled = true; } };
        AddHandler(Button.ClickEvent, new RoutedEventHandler(Navigate));
        AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(OpenDetails), true);
    }

    // Mở giao diện đơn xin nghỉ phép / nghỉ việc từ bất kỳ phân hệ nào.
    private void XinNghiViec_Click(object sender, RoutedEventArgs e)
    {
        home.OpenApp("XinNghiViec");
    }

    /// <summary>
    /// Chuyển việc kiểm tra quyền hiển thị phân hệ về MainWindow để dùng cùng bộ quyền cho mọi màn hình con.
    /// </summary>
    public bool CanOpen(string module) => home.CanOpen(module);
    /// <summary>
    /// Truyền ngữ cảnh bệnh nhân/lần khám/đơn thuốc cho đúng loại màn hình; lịch khám chỉ nhận phạm vi bệnh nhân.
    /// </summary>
    public void FocusHoSo(string? patientId, string? visitId = null, string? prescriptionId = null)
    {
        if (page is BenhNhan patients) patients.FocusContext(patientId,visitId,prescriptionId);
        else if (page is KhamDieuTri visits) visits.FocusContext(patientId,visitId,prescriptionId);
        else if (page is DonThuoc orders) orders.FocusContext(patientId,visitId,prescriptionId);
        else if (page is DichVuThucHien services) services.FocusContext(patientId,visitId,prescriptionId);
        else if (page is HoaDon invoices) invoices.FocusContext(patientId,visitId,prescriptionId);
        else if (page is LichKham appointments && !string.IsNullOrWhiteSpace(patientId)) appointments.SetPatient(patientId);
    }
    /// <summary>
    /// Kiểm tra bệnh nhân tồn tại và lần khám, đơn thuốc thuộc đúng hồ sơ trước khi yêu cầu cửa sổ chính mở phân hệ đích.
    /// </summary>
    public void DieuHuongHoSo(string module, string patientId, string? visitId = null, string? prescriptionId = null)
    {
        if (DuLieuGiaoDien.Find("BENH_NHAN",patientId) is null) { MessageBox.Show("Không tìm thấy bệnh nhân liên kết. Hãy kiểm tra nguồn dữ liệu."); return; }
        if (!string.IsNullOrEmpty(visitId))
        {
            var visit=DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH",visitId);
            if(visit is null || DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH",visit)!=patientId) { MessageBox.Show("Lần khám không thuộc bệnh nhân đang xem."); return; }
        }
        if (!string.IsNullOrEmpty(prescriptionId))
        {
            var order=DuLieuGiaoDien.Find("DON_THUOC",prescriptionId);
            if(order is null || DuLieuGiaoDien.PatientId("DON_THUOC",order)!=patientId || !string.IsNullOrEmpty(visitId) && UiRecord.Text(order,"MaLS")!=visitId) { MessageBox.Show("Đơn thuốc không thuộc hồ sơ/lần khám đang xem."); return; }
        }
        home.OpenHoSoApp(module,patientId,visitId,prescriptionId);
    }

    /// <summary>
    /// Đóng lớp biểu mẫu hoặc quay về nội dung trước đó từ stack lịch sử; khi hết biểu mẫu thì bật lại màn hình nền và khôi phục focus bàn phím.
    /// </summary>
    internal bool ReturnToPage()
    {
        if (ModalLayer.Visibility == Visibility.Visible)
        {
            if (modalHistory.Count > 0) { ModalContent.Content = modalHistory.Pop(); if(ModalContent.Content is UserControl prior) Dispatcher.BeginInvoke(new Action(()=>prior.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)))); }
            else { ModalLayer.Visibility = Visibility.Collapsed; ModalContent.Content = null; Workspace.IsEnabled=true; ShellBar.IsEnabled=true; if(previousFocus is not null) Keyboard.Focus(previousFocus); }
            return true;
        }
        if (history.Count == 0) return false;
        Workspace.Content = history.Pop();
        return true;
    }

    /// <summary>
    /// Hiển thị biểu mẫu trong modal hoặc nội dung workspace theo loại form; chọn đúng tab bảng và khóa liên kết cha được truyền từ bản ghi nguồn.
    /// </summary>
    internal void ShowForm(UserControl form, string? table = null, CreationContext? context = null)
    {
        if (context is { Table: "CHI_TIET_DON_THUOC", ParentField: "MaDT" })
        {
            var prescription = DuLieuGiaoDien.Find("DON_THUOC", context.ParentValue);
            if (prescription is null || !UiRecord.Text(prescription, "TrangThai").Equals("Nháp", StringComparison.CurrentCultureIgnoreCase))
            {
                MessageBox.Show("Chỉ thêm thuốc vào đơn nháp. Đơn đã xác nhận cần lập đơn mới.", "Đơn thuốc", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }
        if (form is CuaSoBenhNhan or CuaSoLichKham or CuaSoKhamDieuTri or CuaSoDonThuoc or TiepNhanKham)
        {
            var name = table?.Split('|')[0] ?? (form switch { CuaSoBenhNhan => "BENH_NHAN", CuaSoLichKham => "LICH_KHAM", CuaSoDonThuoc => "DON_THUOC", TiepNhanKham => "PHIEU_KHAM_BENH", _ => "LICH_SU_KHAM_BENH" });
            BieuMauHoSo.Configure(form, name, context: context, patient: (Workspace.Content as IPatientScoped)?.PatientId);
            ShowModal(form); return;
        }
        history.Push(Workspace.Content); Workspace.Content = form;
        var tableName = table?.Split('|')[0];
        if (form.FindName("EditorTabs") is TabControl tabs)
        {
            TabItem? selected = null;
            foreach (var tab in tabs.Items.OfType<TabItem>())
            {
                var visible = selected is null && (tableName is null || (string?)tab.Tag == "Table:" + tableName);
                tab.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                if (visible) selected = tab;
            }
            if (selected is not null) tabs.SelectedItem = selected;
            if (context is not null)
                foreach (var field in Elements(form).OfType<ComboBox>())
                    if ((string?)field.Tag == "DB:" + context.Table + "." + context.ParentField)
                    {
                        field.ItemsSource = form is CuaSoBenhNhan or CuaSoLichKham or CuaSoKhamDieuTri or CuaSoDonThuoc
                            ? new[] { new LuaChonDanhMuc(Convert.ToString(context.ParentValue)!, DuLieuGiaoDien.Lookup(context.ParentField, context.ParentValue)) }
                            : new object[] { context.ParentValue };
                        field.SelectedIndex = 0;
                        field.IsEnabled = false;
                        field.ToolTip = "Liên kết với bản ghi đang xem";
                    }
            if (table?.EndsWith("|Thuoc", StringComparison.Ordinal) == true)
            {
                // Màn hình thuốc dùng quan hệ loại vật tư sẵn có của giao diện cũ; nhánh này không thêm cột SQL.
                var source = page.FindName("Grid_LOAI_VAT_TU") as DataGrid;
                var type = source?.Items.Cast<object>().FirstOrDefault(x => UiRecord.Text(x, "TenLVT").Trim().Equals("Thuốc", StringComparison.CurrentCultureIgnoreCase));
                foreach (var field in Elements(form).OfType<ComboBox>())
                    if ((string?)field.Tag == "DB:VAT_TU.MaLVT")
                    {
                        field.IsEnabled = false;
                        field.ToolTip = "Loại vật tư: Thuốc";
                        field.ItemsSource = new[] { type is null ? "Thuốc" : UiRecord.Text(type, "MaLVT") + " · Thuốc" };
                        field.SelectedIndex = 0;
                    }
            }
        }
    }
    /// <summary>
    /// Kiểm tra quyền và trạng thái sửa, tạo form hồ sơ phù hợp, nạp dữ liệu cũ/ngữ cảnh cha bằng BieuMauHoSo rồi mở modal.
    /// </summary>
    public void MoBieuMauHoSo(string module, string table, object? existing = null, CreationContext? context = null, string? patient = null)
    {
        var permission = module == "TiepNhanKham" ? "BenhNhan" : module;
        if (!home.CanOpen(permission)) { MessageBox.Show("Vai trò hiện tại không được thực hiện thao tác này."); return; }
        if (existing is not null && DuLieuGiaoDien.Find(table,UiRecord.Get(existing,DuLieuGiaoDien.Key(table))) is object actual && !DuLieuGiaoDien.CanEdit(table,actual) && table != "LICH_KHAM") { MessageBox.Show("Bản ghi đã xác nhận được giữ nguyên. Hãy lập bản mới nếu cần thay đổi."); return; }
        UserControl form = module switch { "BenhNhan" => new CuaSoBenhNhan(), "LichKham" => new CuaSoLichKham(), "DonThuoc" => new CuaSoDonThuoc(), "TiepNhanKham" => new TiepNhanKham(), _ => new CuaSoKhamDieuTri() };
        BieuMauHoSo.Configure(form, table, existing, context, patient ?? (Workspace.Content as IPatientScoped)?.PatientId);
        ShowModal(form);
    }
    /// <summary>
    /// Lưu modal đang mở vào lịch sử, khóa màn hình phía sau, đặt chiều rộng form và chuyển focus vào trường đầu tiên.
    /// </summary>
    public void ShowModal(UserControl control)
    {
        if (ModalLayer.Visibility == Visibility.Visible && ModalContent.Content is object previous) modalHistory.Push(previous);
        if (ModalLayer.Visibility != Visibility.Visible) previousFocus=Keyboard.FocusedElement;
        Workspace.IsEnabled=false; ShellBar.IsEnabled=false;
        ModalFrame.MaxWidth = control is CuaSoBenhNhan or TiepNhanKham ? 850 : 1160;
        ModalContent.Content = control; ModalLayer.Visibility = Visibility.Visible;
        Dispatcher.BeginInvoke(new Action(() => control.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))));
    }

    /// <summary>
    /// Duyệt đệ quy cây logical của control để tìm các phần tử có Tag dữ liệu hoặc các ô nhập cần xử lý.
    /// </summary>
    private static IEnumerable<DependencyObject> Elements(DependencyObject parent)
    {
        yield return parent;
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
            foreach (var element in Elements(child)) yield return element;
    }

    private static readonly (string Parent, string Child, string Key, string Title)[] Relations =
    {
        ("BENH_NHAN", "HO_SO_BENH_AN", "MaBN", "Hồ sơ bệnh án"),
        ("LICH_SU_KHAM_BENH", "TINH_TRANG_BENH", "MaLS", "Tình trạng bệnh"),
        ("KE_HOACH_DIEU_TRI", "CHI_TIET_KE_HOACH", "MaKHDT", "Chi tiết kế hoạch điều trị"),
        ("DON_THUOC", "CHI_TIET_DON_THUOC", "MaDT", "Chi tiết đơn thuốc"),
        ("HOA_DON", "CHI_TIET_HOA_DON", "MaHD", "Chi tiết hóa đơn"),
        ("HOA_DON", "THANH_TOAN", "MaHD", "Các lần thanh toán"),
        ("GIAO_DICH_KHO", "HOA_DON_KHO", "MaGD", "Hóa đơn kho"),
        ("VAT_TU", "LO_VAT_TU", "MaVT", "Các lô hàng / lô thuốc")
    };

    /// <summary>
    /// Mở hồ sơ bệnh nhân, chi tiết lần khám/đơn thuốc hoặc modal ChiTietHoSo theo tên bảng; trả false nếu bảng chưa có màn hình chuyên dụng.
    /// </summary>
    public bool ShowHoSoDetails(string table, object record)
    {
        if (table == "BENH_NHAN")
        {
            if (page is BenhNhan patients) { patients.OpenProfile(record, home.CanOpen); Workspace.Content = page; return true; }
            var profile = new HoSoBenhNhan(record);
            profile.ConfigureAccess(home.CanOpen);
            history.Push(Workspace.Content);
            Workspace.Content = profile;
            return true;
        }
        if (table == "LICH_SU_KHAM_BENH") { DieuHuongHoSo("KhamDieuTri",DuLieuGiaoDien.PatientId(table,record),UiRecord.Text(record,"MaLS")); return true; }
        if (table == "DON_THUOC") { DieuHuongHoSo("DonThuoc",DuLieuGiaoDien.PatientId(table,record),UiRecord.Text(record,"MaLS"),UiRecord.Text(record,"MaDT")); return true; }
        var titles = new Dictionary<string, string>
        {
            ["HO_SO_BENH_AN"] = "Hồ sơ bệnh án", ["LICH_KHAM"] = "Chi tiết lịch khám",
            ["LICH_SU_KHAM_BENH"] = "Thông tin lần khám", ["TINH_TRANG_BENH"] = "Tình trạng bệnh",
            ["KE_HOACH_DIEU_TRI"] = "Kế hoạch điều trị", ["CHI_TIET_KE_HOACH"] = "Dịch vụ dự kiến",
            ["DICH_VU_THUC_HIEN"] = "Dịch vụ thực hiện", ["DON_THUOC"] = "Thông tin đơn thuốc",
            ["CHI_TIET_DON_THUOC"] = "Chi tiết thuốc"
        };
        if (!titles.TryGetValue(table, out var title)) return false;
        ShowModal(new ChiTietHoSo(table, title, record));
        return true;
    }

    /// <summary>
    /// Bắt nhấp đúp trên dòng DataGrid, tìm bảng từ Tag, mở chi tiết chuyên dụng hoặc gom các dòng con theo khóa cha; bỏ qua luồng nhân sự đã có cửa sổ riêng.
    /// </summary>
    private void OpenDetails(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ClickCount != 2) return;
        var current = e.OriginalSource as DependencyObject;
        while (current is not null && current is not DataGridRow)
            current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
        if (current is not DataGridRow row || row.Item == System.Windows.Data.CollectionView.NewItemPlaceholder
            || ItemsControl.ItemsControlFromItemContainer(row) is not DataGrid source
            || source.Tag is not string tag || !tag.StartsWith("Table:", StringComparison.Ordinal)) return;
        var table = tag[6..];
        // Nhân viên có cửa sổ chi tiết riêng được mở bởi MouseDoubleClick trên DataGrid.
        // Giữ luồng nhân sự của repository gốc.
        if (table == "NHAN_VIEN") return;
        var selectedRecord = row.Item is TreatmentRow treatment ? treatment.Record : row.Item;
        if (ShowHoSoDetails(table, selectedRecord)) { e.Handled = true; return; }
        var related = new List<RelatedRows>();
        foreach (var relation in Relations.Where(x => x.Parent == table))
        {
            if (page.FindName("Grid_" + relation.Child) is not DataGrid child) continue;
            var key = UiRecord.Text(row.Item, relation.Key);
            // Mã cha trống phải trả danh sách con trống để không hiện các dòng không liên quan.
            var rows = child.Items.Cast<object>();
            if (page.FindName("Grid_" + relation.Child + "_Chi") is DataGrid payments)
                rows = rows.Concat(payments.Items.Cast<object>()).Distinct();
            var items = key.Length == 0 ? Array.Empty<object>() : rows
                .Where(x => x != System.Windows.Data.CollectionView.NewItemPlaceholder && UiRecord.Text(x, relation.Key) == key).ToArray();
            var module = relation.Child switch
            {
                "HO_SO_BENH_AN" => "BenhNhan",
                "TINH_TRANG_BENH" or "CHI_TIET_KE_HOACH" => "KhamDieuTri",
                "CHI_TIET_DON_THUOC" => "DonThuoc",
                "CHI_TIET_HOA_DON" or "THANH_TOAN" => "HoaDon",
                "HOA_DON_KHO" => "NhapXuatVatTu",
                "LO_VAT_TU" => "VatTu",
                _ => ""
            };
            var parentValue = UiRecord.Get(row.Item, relation.Key);
            var creation = key.Length == 0 || parentValue is null ? null : new CreationContext(relation.Child, relation.Key, parentValue);
            related.Add(new RelatedRows(relation.Title, module, child, items, creation));
        }
        e.Handled = true;
        history.Push(Workspace.Content);
        Workspace.Content = new RecordDetails(source, row.Item, related);
    }

    /// <summary>
    /// Đọc lại dữ liệu SQL cho bộ cache giao diện; nếu lỗi kết nối thì hiển thị thông báo và giữ dữ liệu đang xem.
    /// </summary>
    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        try {DuLieuGiaoDien.RefreshFromDatabase();}
        catch(InvalidOperationException ex) {MessageBox.Show(ex.Message,"Tải dữ liệu",MessageBoxButton.OK,MessageBoxImage.Error);}
    }

    /// <summary>
    /// Bắt routed event từ nút có Tag: lưu form trong giao dịch, mở trang/form/chi tiết, hiện cột bổ sung hoặc quay về; sau lưu tiếp nhận có thể chuyển sang tạo lần khám.
    /// </summary>
    private void Navigate(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Button { Tag: string tag } button) return;

        if (tag == "HoSoSave" && ModalContent.Content is UserControl editor)
        {
            e.Handled = true;
            string error=""; bool saved;
            // AI bổ sung: chặn binding/ngày sai trước khi mở giao dịch và giữ nguyên modal để sửa.
            if (!KiemTraNhapLieu.PrepareSave(editor, out error))
            {
                if (editor.FindName("FormError") is TextBlock inputError) inputError.Text = error;
                else MessageBox.Show(error, "Kiểm tra thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                saved = DuLieuGiaoDien.Transaction(()=>
                {
                    var success=editor is IBieuMauLuu custom ? custom.Commit(out error) : BieuMauHoSo.TrySave(editor, out _, out error);
                    if (success && BieuMauHoSo.GetTable(editor)=="PHIEU_KHAM_BENH" && BieuMauHoSo.GetSaved(editor) is object receipt && DuLieuGiaoDien.Find("LICH_KHAM",UiRecord.Get(receipt,"MaLich")) is object appointment && (UiRecord.Text(appointment,"TrangThai") is "Nháp" or "Đã xác nhận"))
                        DuLieuGiaoDien.SetValue("LICH_KHAM",appointment,"TrangThai","Chờ khám");
                    return success;
                });
            }
            catch (Exception ex) when (ex is InvalidOperationException or FormatException or OverflowException or ArgumentOutOfRangeException) { saved = false; error = ex.Message; }
            if (saved)
            {
                var table=BieuMauHoSo.GetTable(editor); var record=BieuMauHoSo.GetSaved(editor);
                var newPatient=table=="BENH_NHAN" && (BieuMauHoSo.GetRecord(editor) is null || DuLieuGiaoDien.Find(table,UiRecord.Get(BieuMauHoSo.GetRecord(editor)!,"MaBN")) is null);
                var openMedical=(editor.FindName("CreateMedical") as CheckBox)?.IsChecked==true;
                var openVisit=(editor.FindName("OpenVisitAfterSave") as CheckBox)?.IsChecked==true;
                modalHistory.Clear(); ReturnToPage();
                if(record is not null && table=="BENH_NHAN")
                {
                    var id=UiRecord.Text(record,"MaBN"); home.OpenHoSoApp("BenhNhan",id);
                    if(newPatient && openMedical && DuLieuGiaoDien.Children("HO_SO_BENH_AN","MaBN",id).Length==0 && home.AppHostWorkspace() is AppWorkspace target) target.MoBieuMauHoSo("BenhNhan","HO_SO_BENH_AN",context:new CreationContext("HO_SO_BENH_AN","MaBN",id),patient:id);
                }
                else if(record is not null && table=="HO_SO_BENH_AN" && page is BenhNhan patients) patients.OpenMedicalRecord();
                else if(record is not null && table=="LICH_SU_KHAM_BENH") home.OpenHoSoApp("KhamDieuTri",DuLieuGiaoDien.PatientId(table,record),UiRecord.Text(record,"MaLS"));
                // Lưu dữ liệu con làm mới lần khám tại chỗ, giữ phần thông tin khám đang nhập chưa lưu.
                else if(record is not null && table=="DON_THUOC" && page is DonThuoc) FocusHoSo(DuLieuGiaoDien.PatientId(table,record),UiRecord.Text(record,"MaLS"),UiRecord.Text(record,"MaDT"));
                else if(record is not null && table=="PHIEU_KHAM_BENH" && openVisit && home.CanOpen("KhamDieuTri"))
                {
                    var id=DuLieuGiaoDien.PatientId(table,record); home.OpenHoSoApp("KhamDieuTri",id);
                    home.AppHostWorkspace()?.MoBieuMauHoSo("KhamDieuTri","LICH_SU_KHAM_BENH", new Dictionary<string,object?> { ["MaPhieu"]=UiRecord.Get(record,"MaPhieu"), ["MaBS"]=UiRecord.Get(record,"MaBS"), ["MaPhong"]=UiRecord.Get(record,"MaPhong") }, new CreationContext("LICH_SU_KHAM_BENH","MaPhieu",UiRecord.Get(record,"MaPhieu")!), id);
                }
            }
            else if (editor.FindName("FormError") is TextBlock label) label.Text = error;
            else MessageBox.Show(error, "Kiểm tra thông tin", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else if (tag == "HoSoDetails")
        {
            if (ModalContent.Content is ChiTietHoSo) { e.Handled = true; return; }
            if (button.CommandParameter is ExaminationCard exam) ShowHoSoDetails(exam.Table, exam.Record);
            if (button.CommandParameter is PrescriptionCard prescription) ShowHoSoDetails(prescription.Table, prescription.Record);
            e.Handled = true;
        }
        else if (tag == "ToggleColumns" && button.CommandParameter is string table
            && page.FindName("Grid_" + table) is DataGrid grid)
        {
            e.Handled = true;
            if (!extraColumns.TryGetValue(grid, out var columns))
            {
                columns = grid.Columns.Where(column => column.Visibility == Visibility.Collapsed).ToList();
                extraColumns[grid] = columns;
            }
            if (columns.Count == 0) return;
            var show = columns[0].Visibility == Visibility.Collapsed;
            foreach (var column in columns) column.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            button.Content = show ? "Thu gọn cột" : "Hiện đủ cột";
        }
        else if (tag == "VeTrangChu")
        {
            e.Handled = true;
            home.ShowHome();
        }
        else if (tag == "DongCuaSo")
        {
            e.Handled = true;
            ReturnToPage();
        }
        else if (tag.StartsWith("MoTrang:", StringComparison.Ordinal))
        {
            e.Handled = true;
            home.OpenApp(tag["MoTrang:".Length..]);
        }
        else if (tag.StartsWith("MoCuaSo:", StringComparison.Ordinal))
        {
            e.Handled = true;
            var context = button.CommandParameter as CreationContext;
            home.OpenModuleWindow(tag["MoCuaSo:".Length..], this, context?.Table ?? button.CommandParameter as string, context);
        }
    }
}
