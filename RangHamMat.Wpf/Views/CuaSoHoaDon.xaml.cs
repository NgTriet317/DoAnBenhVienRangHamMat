using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using BUS;
using RangHamMat.Controls;

namespace RangHamMat.Views;

public partial class CuaSoHoaDon : UserControl, IClinicalEditor, IPatientScoped
{
    private readonly BUS_HOADON finance = new();
    private bool updating;
    private decimal total;
    public string? PatientId { get; private set; }
    public string? SavedInvoiceId { get; private set; }
    public event Action<string>? CreationCompleted;
    public event Action? CloseRequested;
    public CuaSoHoaDon()
    {
        InitializeComponent();
        Loaded += (_, _) => { if (PatientBox.ItemsSource is null) ConfigureContext(); };
    }
    public void ConfigureContext(string? patientId = null, string? visitId = null, string? employeeId = null)
    {
        PatientId = patientId; updating = true;
        try
        {
            PatientBox.ItemsSource = ClinicalData.Options("BENH_NHAN").ToArray(); PatientBox.SelectedValue = patientId;
            EmployeeBox.ItemsSource = ClinicalData.Rows("NHAN_VIEN").Where(r => UiRecord.Text(r, "TrangThai") == "Đang làm việc")
                .Select(r => new ClinicalOption(UiRecord.Text(r, "MaNV"), ClinicalData.Label("NHAN_VIEN", r))).ToArray(); EmployeeBox.SelectedValue = employeeId;
            PopulateVisits(); VisitBox.SelectedValue = visitId;
            PatientBox.IsEnabled = string.IsNullOrEmpty(patientId); VisitBox.IsEnabled = string.IsNullOrEmpty(visitId);
        }
        finally { updating = false; }
        LoadCharges();
    }
    private void PopulateVisits()
    {
        VisitBox.ItemsSource = ClinicalData.Rows("LICH_SU_KHAM_BENH").Where(r => UiRecord.Text(r, "TrangThai") == "Đã xác nhận"
            && !string.IsNullOrEmpty(PatientId) && ClinicalData.PatientId("LICH_SU_KHAM_BENH", r) == PatientId)
            .Select(r => new ClinicalOption(UiRecord.Text(r, "MaLS"), $"#{UiRecord.Text(r, "MaLS")} · {UiRecord.Date(r, "ThoiGianKham"):dd/MM/yyyy HH:mm}")).ToArray();
    }
    private void LoadCharges()
    {
        if (updating || ChargeGrid is null) return;
        try
        {
            var visit = ClinicalUi.Selected(VisitBox);
            var table = !string.IsNullOrEmpty(visit) ? finance.GetEligibleCharges(visit) : new DataTable();
            ChargeGrid.ItemsSource = table.DefaultView; total = table.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r["ThanhTien"]));
            ChargesInfo.Text = table.Rows.Count == 0 ? "Chưa có khoản thu hợp lệ mới. Xác nhận dịch vụ hoặc cấp đủ thuốc rồi tải lại."
                : $"{table.Rows.Count} khoản thu · Tổng {TreatmentMoney.Format(total)}. Giá và nguồn được kiểm tra lại khi lưu.";
            SaveButton.IsEnabled = table.Rows.Count > 0 && SavedInvoiceId is null; ShowTotal(); Feedback.Text = "";
        }
        catch (Exception ex) { SaveButton.IsEnabled = false; Feedback.Text = "Không thể tải khoản thu: " + ex.Message; }
    }
    private void ShowTotal()
    {
        if (NetTotal is null) return;
        NetTotal.Text = ParseMoney(DiscountBox.Text, out var discount) && discount >= 0 && discount <= total
            ? TreatmentMoney.Format(total - discount) : "Kiểm tra mức giảm giá";
    }
    private static bool ParseMoney(string value, out decimal amount) => decimal.TryParse(value.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount);
    public bool Commit(out string error)
    {
        error = "";
        if (SavedInvoiceId is not null) return true;
        try
        {
            var visit = ClinicalUi.Selected(VisitBox); var employee = ClinicalUi.Selected(EmployeeBox);
            if (string.IsNullOrEmpty(visit) || string.IsNullOrEmpty(employee)) throw new InvalidOperationException("Chọn lần khám đã xác nhận và nhân viên lập hóa đơn.");
            if (!ParseMoney(DiscountBox.Text, out var discount)) throw new InvalidOperationException("Nhập giảm giá là số tiền không âm, tối đa 2 chữ số thập phân.");
            SavedInvoiceId = finance.CreateInvoice(visit, employee, discount); SaveButton.IsEnabled = false;
            try { ClinicalData.RefreshFromDatabase(); }
            catch (Exception ex) { Feedback.Text = $"Hóa đơn #{SavedInvoiceId} đã được lưu. Tải lại danh sách để xem: {ex.Message}"; }
            return true;
        }
        catch (Exception ex) { error = ex.Message; Feedback.Text = "Không thể lưu hóa đơn: " + error; return false; }
    }
    private void Patient_Changed(object sender, SelectionChangedEventArgs e)
    { if (updating) return; PatientId = ClinicalUi.Selected(PatientBox); updating = true; PopulateVisits(); updating = false; LoadCharges(); }
    private void Visit_Changed(object sender, SelectionChangedEventArgs e) => LoadCharges();
    private void Discount_Changed(object sender, TextChangedEventArgs e) => ShowTotal();
    private void Back_Click(object sender, RoutedEventArgs e)
    { if (CloseRequested is not null) CloseRequested.Invoke(); else ClinicalUi.Workspace(this)?.ReturnToPage(); }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!Commit(out _) || SavedInvoiceId is not string id) return;
        if (CreationCompleted is not null) CreationCompleted.Invoke(id);
        else { var workspace = ClinicalUi.Workspace(this); workspace?.ReturnToPage(); workspace?.NavigateClinical("HoaDon", PatientId ?? "", ClinicalUi.Selected(VisitBox)); }
    }
}
