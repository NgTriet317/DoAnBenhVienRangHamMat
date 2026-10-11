using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BUS;
using RangHamMat.Controls;

namespace RangHamMat.Views;

public partial class HoaDon : UserControl, IPatientScoped
{
    private readonly BUS_HOADON finance = new();
    private bool updating;
    private string? visitId, prescriptionId;
    private string? invoiceId;
    public string? PatientId { get; private set; }
    public HoaDon() { InitializeComponent(); ClinicalUi.Observe(this, Refresh); }
    public void SetPatient(string id) => FocusContext(id);
    public void FocusContext(string? patientId, string? visitId = null, string? prescriptionId = null)
    {
        PatientId = patientId; this.visitId = visitId; this.prescriptionId = prescriptionId; invoiceId = null;
        if (!string.IsNullOrEmpty(prescriptionId)) DetailTabs.SelectedItem = DispenseTab;
        Refresh();
    }
    public void OpenDispensing(string? patientId, string? visitId = null, string? prescriptionId = null)
    { FocusContext(patientId, visitId, prescriptionId); DetailTabs.SelectedItem = DispenseTab; }
    private void Refresh()
    {
        if (updating || InvoiceList is null) return;
        updating = true;
        try
        {
            var employee = ClinicalUi.Selected(EmployeeFilter); var warehouse = ClinicalUi.Selected(WarehouseFilter);
            PatientFilter.ItemsSource = ClinicalData.Options("BENH_NHAN", true).ToArray(); PatientFilter.SelectedValue = PatientId ?? "";
            VisitFilter.ItemsSource = new[] { new ClinicalOption("", "Tất cả lần khám") }.Concat(ClinicalData.Rows("LICH_SU_KHAM_BENH")
                .Where(r => string.IsNullOrEmpty(PatientId) || ClinicalData.PatientId("LICH_SU_KHAM_BENH", r) == PatientId)
                .Select(r => new ClinicalOption(UiRecord.Text(r, "MaLS"), $"#{UiRecord.Text(r, "MaLS")} · {UiRecord.Date(r, "ThoiGianKham"):dd/MM/yyyy} · {UiRecord.Text(r, "TrangThai")}"))).ToArray();
            VisitFilter.SelectedValue = visitId ?? "";
            EmployeeFilter.ItemsSource = ClinicalData.Rows("NHAN_VIEN").Where(r => UiRecord.Text(r, "TrangThai") == "Đang làm việc")
                .Select(r => new ClinicalOption(UiRecord.Text(r, "MaNV"), ClinicalData.Label("NHAN_VIEN", r))).ToArray(); EmployeeFilter.SelectedValue = employee;
            WarehouseFilter.ItemsSource = ClinicalData.Rows("KHO").Where(r => UiRecord.Text(r, "TrangThai") == "Đang hoạt động")
                .Select(r => new ClinicalOption(UiRecord.Text(r, "MaKho"), UiRecord.Text(r, "TenKho"))).ToArray(); WarehouseFilter.SelectedValue = warehouse;
            PrescriptionFilter.ItemsSource = ClinicalData.Rows("DON_THUOC").Where(r => UiRecord.Text(r, "TrangThai") == "Đã xác nhận"
                && !string.IsNullOrEmpty(visitId) && UiRecord.Text(r, "MaLS") == visitId)
                .Select(r => new ClinicalOption(UiRecord.Text(r, "MaDT"), $"Đơn #{UiRecord.Text(r, "MaDT")} · {UiRecord.Date(r, "NgayKe"):dd/MM/yyyy HH:mm}")).ToArray(); PrescriptionFilter.SelectedValue = prescriptionId;
            var table = finance.GetInvoices(string.IsNullOrEmpty(visitId) ? null : visitId);
            table.DefaultView.RowFilter = string.IsNullOrEmpty(PatientId) ? "" : $"MaBN='{PatientId.Replace("'", "''")}'";
            InvoiceList.ItemsSource = table.DefaultView;
            InvoiceList.SelectedItem = table.DefaultView.Cast<DataRowView>().FirstOrDefault(r => Convert.ToString(r["MaHD"]) == invoiceId);
            InvoiceCount.Text = $"{table.DefaultView.Count} hóa đơn"; CreateButton.IsEnabled = HasVisit() && HasEmployee(); ShowInvoice(); ShowStock();
        }
        catch (Exception ex) { PageStatus.Text = "Không thể tải hóa đơn: " + ex.Message; }
        finally { updating = false; }
    }
    private void ShowInvoice()
    {
        var row = InvoiceList.SelectedItem as DataRowView;
        InvoicePanel.Visibility = row is null ? Visibility.Collapsed : Visibility.Visible; InvoiceEmpty.Visibility = row is null ? Visibility.Visible : Visibility.Collapsed;
        if (row is null) { invoiceId = null; return; }
        invoiceId = Convert.ToString(row["MaHD"]) ?? throw new InvalidOperationException("Hóa đơn thiếu mã.");
        InvoiceTitle.Text = $"Hóa đơn #{invoiceId} · {row["TrangThai"]}";
        InvoiceInfo.Text = $"{row["HoTenBN"]} · Lần khám #{row["MaLS"]} · {Convert.ToDateTime(row["NgayLap"]):dd/MM/yyyy HH:mm} · Lập bởi {row["NguoiLap"]}";
        DetailGrid.ItemsSource = finance.GetInvoiceDetails(invoiceId).DefaultView; PaymentGrid.ItemsSource = finance.GetPayments(invoiceId).DefaultView;
        TotalsText.Text = $"Tổng {Money(row["TongTien"])}  ·  Giảm {Money(row["GiamGia"])}  ·  Phải thu {Money(row["SoTienPhaiThu"])}\nĐã thu ròng {Money(row["DaThu"])}  ·  Còn phải trả {Money(row["ConPhaiThu"])}";
        ConfirmButton.IsEnabled = CancelButton.IsEnabled = Convert.ToString(row["TrangThai"]) == "Nháp";
        PaymentForm.IsEnabled = Convert.ToString(row["TrangThai"]) == "Đã xác nhận" && HasEmployee();
    }
    private void ShowStock()
    {
        prescriptionId = ClinicalUi.Selected(PrescriptionFilter); var rx = ClinicalData.Find("DON_THUOC", prescriptionId);
        var lines = ClinicalData.Children("CHI_TIET_DON_THUOC", "MaDT", prescriptionId);
        PrescriptionGrid.ItemsSource = rx is null ? null : lines.Select(r => new { Thuoc = ClinicalData.Lookup("MaThuoc", UiRecord.Get(r, "MaThuoc")), SoLuong = UiRecord.Get(r, "SoLuong"), LieuDung = UiRecord.Text(r, "LieuDung"), HuongDan = UiRecord.Text(r, "HuongDan") }).ToArray();
        var valid = !string.IsNullOrEmpty(prescriptionId) && !string.IsNullOrEmpty(ClinicalUi.Selected(WarehouseFilter)); StockGrid.ItemsSource = null;
        if (valid)
        {
            var stock = finance.GetStock(ClinicalUi.Selected(WarehouseFilter), prescriptionId!); stock.Columns.Add("Thuoc", typeof(string));
            foreach (DataRow row in stock.Rows) row["Thuoc"] = ClinicalData.Lookup("MaHH", row["MaHH"]); StockGrid.ItemsSource = stock.DefaultView;
        }
        var posted = ClinicalData.Rows("CHI_TIET_PHIEU_XUAT").Any(x => lines.Any(c => UiRecord.Text(c, "MaCTDT") == UiRecord.Text(x, "MaCTDT")) &&
            UiRecord.Text(ClinicalData.Find("PHIEU_XUAT", UiRecord.Get(x, "MaPX")) ?? new object(), "TrangThai") == "Đã ghi sổ");
        DispenseButton.IsEnabled = valid && HasEmployee() && !posted;
        DispenseInfo.Text = posted ? "Đơn đã cấp đủ và ghi sổ. Có thể lập hóa đơn cho các khoản chưa được thu."
            : rx is null ? "Chọn một lần khám, sau đó chọn đơn thuốc đã xác nhận và kho cấp thuốc."
            : "Tồn kho và trạng thái đơn được kiểm tra lại trong giao dịch. Nếu thiếu bất kỳ thuốc nào, toàn bộ lần cấp được hủy.";
    }
    private bool HasVisit() => !string.IsNullOrEmpty(visitId) && ClinicalData.Find("LICH_SU_KHAM_BENH", visitId) is object visit && UiRecord.Text(visit, "TrangThai") == "Đã xác nhận";
    private bool HasEmployee() => !string.IsNullOrEmpty(ClinicalUi.Selected(EmployeeFilter));
    private static string Money(object value) => TreatmentMoney.Format(Convert.ToDecimal(value));
    private string EmployeeId() => HasEmployee() ? ClinicalUi.Selected(EmployeeFilter) : throw new InvalidOperationException("Chọn nhân viên thực hiện.");
    private void Execute(Action operation, string success)
    {
        try { operation(); }
        catch (Exception ex) { PageStatus.Text = "Không thể thực hiện: " + ex.Message; return; }
        try { ClinicalData.RefreshFromDatabase(); Refresh(); PageStatus.Text = success; }
        catch (Exception ex) { PageStatus.Text = success + " Chưa tải lại được danh sách; bấm Tải lại để cập nhật: " + ex.Message; }
    }
    private void Reload_Click(object sender, RoutedEventArgs e)
    { try { ClinicalData.RefreshFromDatabase(); Refresh(); PageStatus.Text = "Đã tải lại từ database."; } catch (Exception ex) { PageStatus.Text = "Không thể tải lại dữ liệu: " + ex.Message; } }
    private void Patient_Changed(object sender, SelectionChangedEventArgs e)
    { if (updating) return; PatientId = ClinicalUi.Selected(PatientFilter); visitId = prescriptionId = null; invoiceId = null; Refresh(); }
    private void Visit_Changed(object sender, SelectionChangedEventArgs e)
    { if (updating) return; visitId = ClinicalUi.Selected(VisitFilter); prescriptionId = null; invoiceId = null; var visit = ClinicalData.Find("LICH_SU_KHAM_BENH", visitId); if (visit is not null) PatientId = ClinicalData.PatientId("LICH_SU_KHAM_BENH", visit); Refresh(); }
    private void Employee_Changed(object sender, SelectionChangedEventArgs e)
    { if (updating) return; try { CreateButton.IsEnabled = HasVisit() && HasEmployee(); ShowInvoice(); ShowStock(); } catch (Exception ex) { PageStatus.Text = ex.Message; } }
    private void Invoice_Changed(object sender, SelectionChangedEventArgs e)
    { if (updating) return; try { ShowInvoice(); } catch (Exception ex) { PageStatus.Text = "Không thể đọc chi tiết: " + ex.Message; } }
    private void Stock_Changed(object sender, SelectionChangedEventArgs e)
    { if (updating) return; try { ShowStock(); } catch (Exception ex) { PageStatus.Text = "Không thể đọc tồn kho: " + ex.Message; } }
    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var editor = new CuaSoHoaDon(); editor.ConfigureContext(PatientId, visitId, ClinicalUi.Selected(EmployeeFilter));
        editor.CreationCompleted += id => { invoiceId = id; CloseEditor(); Refresh(); PageStatus.Text = $"Đã lập hóa đơn nháp #{id}. Kiểm tra khoản thu rồi xác nhận để thanh toán."; };
        editor.CloseRequested += CloseEditor; EditorHost.Content = editor; EditorHost.Visibility = Visibility.Visible; InvoiceContent.Visibility = Visibility.Collapsed; DetailTabs.SelectedIndex = 0;
    }
    private void CloseEditor() { EditorHost.Content = null; EditorHost.Visibility = Visibility.Collapsed; InvoiceContent.Visibility = Visibility.Visible; }
    private void Confirm_Click(object sender, RoutedEventArgs e) => Execute(() => finance.ConfirmInvoice(invoiceId ?? ""), "Đã xác nhận hóa đơn. Có thể thu tiền.");
    private void Cancel_Click(object sender, RoutedEventArgs e) => Execute(() => finance.CancelInvoice(invoiceId ?? ""), "Đã hủy hóa đơn nháp và giải phóng các khoản thu.");
    private void Payment_Click(object sender, RoutedEventArgs e) => Execute(() =>
    {
        if (!decimal.TryParse(AmountBox.Text.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount)) throw new InvalidOperationException("Nhập số tiền hợp lệ, ví dụ 150000 hoặc 150000,50.");
        finance.RecordPayment(invoiceId ?? "", EmployeeId(), amount, ClinicalUi.Choice(PaymentType), ClinicalUi.Choice(MethodBox), ReasonBox.Text); AmountBox.Clear(); ReasonBox.Clear();
    }, "Đã ghi nhận giao dịch và cập nhật số còn phải trả.");
    private void Dispense_Click(object sender, RoutedEventArgs e) => Execute(() => finance.DispensePrescription(ClinicalUi.Selected(PrescriptionFilter), ClinicalUi.Selected(WarehouseFilter), EmployeeId()), "Đã cấp đủ đơn thuốc và ghi sổ phiếu xuất. Có thể lập hóa đơn.");
    private void Navigate(string module)
    { var row = InvoiceList.SelectedItem as DataRowView; ClinicalNavigator.Open(this, module, Convert.ToString(row?["MaBN"]) ?? PatientId ?? "", Convert.ToString(row?["MaLS"]) ?? visitId, prescriptionId); }
    private void Patient_Click(object sender, RoutedEventArgs e) => Navigate("BenhNhan");
    private void Visit_Click(object sender, RoutedEventArgs e) => Navigate("KhamDieuTri");
    private void Services_Click(object sender, RoutedEventArgs e) => Navigate("DichVuThucHien");
    private void Prescription_Click(object sender, RoutedEventArgs e) => Navigate("DonThuoc");
    private void Print_Click(object sender, RoutedEventArgs e)
    { try { if (invoiceId is null) return; var header = finance.GetInvoices().Rows.Cast<DataRow>().Single(r => Convert.ToString(r["MaHD"]) == invoiceId); new XemTruocHoaDon(header, finance.GetInvoiceDetails(invoiceId)) { Owner = Window.GetWindow(this) }.ShowDialog(); } catch (Exception ex) { PageStatus.Text = "Không thể xem trước hóa đơn: " + ex.Message; } }
}

public sealed class InvoiceMoneyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is null or DBNull || value == DependencyProperty.UnsetValue ? "—" : TreatmentMoney.Format(System.Convert.ToDecimal(value));
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
