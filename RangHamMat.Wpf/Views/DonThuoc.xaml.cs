using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Views;

/// <summary>Tra cứu đơn đã kê và bàn giao; việc kê đơn thuộc màn hình khám điều trị.</summary>
public partial class DonThuoc : UserControl, IPatientScoped
{
    private readonly NguCanhNghiepVu actor;
    private readonly BUS_KHAMDIEUTRI history;
    private readonly BUS_DONTHUOC prescriptions;
    private readonly IDbTransaction? transaction;
    private bool updating = true;
    private bool invalidContext;
    private string? visitId, selectedId, requestedId;
    private int historyPage = 1;
    public string? PatientId { get; private set; }
    public DonThuoc() : this(QuyenNghiepVu.LayNguCanh()) { }
    internal DonThuoc(bool historyOnly) : this() { }
    public DonThuoc(NguCanhNghiepVu actor, IDbTransaction? transaction = null)
    {
        this.actor = actor;
        this.transaction = transaction;
        history = new(transaction);
        prescriptions = new(transaction);
        InitializeComponent();
        updating = false;
        HoTroGiaoDien.Observe(this, Refresh);
        Refresh();
    }

    public void SetPatient(string id) => FocusContext(id);
    public void FocusContext(string? patientId, string? visitId = null, string? prescriptionId = null)
    {
        if (MustKeepSource()) return;
        PatientId = string.IsNullOrWhiteSpace(patientId) ? null : patientId;
        this.visitId = string.IsNullOrWhiteSpace(visitId) ? null : visitId;
        requestedId = selectedId = string.IsNullOrWhiteSpace(prescriptionId) ? null : prescriptionId;
        historyPage = 1;
        invalidContext = false;
        DetailHost.Content = null;
        updating = true;
        SearchBox.Text = requestedId ?? "";
        StatusFilter.SelectedIndex = 0;
        try
        {
            var source = history.XacMinhNguCanh(new() { MaBN = PatientId, MaLS = this.visitId, MaDT = requestedId }, actor);
            PatientId = source.MaBN;
            this.visitId = source.MaLS;
            if (requestedId is not null)
            {
                var prescription = prescriptions.LayChiTietCoQuyen(requestedId, actor);
                if (prescription.DonThuoc.TrangThai == "Ngừng áp dụng") StatusFilter.SelectedIndex = 1;
            }
        }
        catch (InvalidOperationException ex)
        {
            invalidContext = true;
            Feedback.Text = ClinicalFormInput.Message(ex);
        }
        finally { updating = false; }
        Refresh();
    }

    private bool MustKeepSource()
    {
        if (DetailHost?.Content is PrescriptionHistoryDetail { MustKeepSource: true })
        { Feedback.Text = "Cần đối chiếu kết quả giao đúng đơn đang xử lý trước khi đổi nguồn."; return true; }
        return false;
    }

    private void Refresh()
    {
        if (updating || PrescriptionList is null || MustKeepSource()) return;
        updating = true;
        if (invalidContext)
        {
            PrescriptionList.ItemsSource = Array.Empty<IssuedPrescriptionCard>();
            PatientFilter.ItemsSource = Array.Empty<LuaChonDanhMuc>();
            RecordCount.Text = "0 đơn thuốc";
            PrescriptionPages.Visibility = Visibility.Collapsed;
            ListEmpty.Visibility = Visibility.Visible;
            updating = false;
            ShowSelected();
            return;
        }
        RefreshHistoryPage();
    }

    private void ShowSelected()
    {
        if (PrescriptionList.SelectedItem is IssuedPrescriptionCard card)
        {
            try
            {
                history.XacMinhNguCanh(new() { MaBN = card.PatientId, MaLS = card.Visit, MaDT = card.Code }, actor);
                selectedId = card.Code;
                requestedId = null;
                var detail = new PrescriptionHistoryDetail(card.Record, actor, transaction);
                detail.SourceLockChanged += locked =>
                {
                    PrescriptionList.IsEnabled = FiltersPanel.IsEnabled = PrescriptionPages.IsEnabled = !locked;
                };
                DetailHost.Content = detail;
                DetailEmpty.Visibility = Visibility.Collapsed;
                return;
            }
            catch (InvalidOperationException ex) { Feedback.Text = ClinicalFormInput.Message(ex); }
        }
        DetailHost.Content = null;
        DetailEmpty.Visibility = Visibility.Visible;
        DetailEmptyText.Text = invalidContext ? "Không đọc được đơn trong phạm vi được phép."
            : requestedId is not null ? "Không tìm thấy đơn đã kê trong ngữ cảnh bệnh nhân và lần khám này."
            : "Chọn đơn để xem nội dung bác sĩ đã kê và trạng thái bàn giao.";
    }

    private void Order_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updating || MustKeepSource()) return;
        selectedId = (PrescriptionList.SelectedItem as IssuedPrescriptionCard)?.Code;
        ShowSelected();
    }
    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        if (updating || MustKeepSource()) return;
        selectedId = requestedId = null;
        historyPage = 1;
        Refresh();
    }
    private void Patient_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updating || MustKeepSource()) return;
        FocusContext(HoTroGiaoDien.Selected(PatientFilter));
    }
    private void ClearFilters_Click(object sender, RoutedEventArgs e) => FocusContext(null);
}

/// <summary>Thẻ danh sách chỉ dùng DTO đã kiểm tra quyền, không tra dòng thuốc trong cache toàn cục.</summary>
internal sealed class IssuedPrescriptionCard(DongLichSuDon source)
{
    public DongLichSuDon Record { get; } = source;
    public string Code => Record.MaDT;
    public string Status => Record.TrangThai;
    public string PatientId => Record.MaBN;
    public string Visit => Record.MaLS;
    public string Patient => $"{Record.HoTenBN} ({Record.MaBN})";
    public string Doctor => Record.TenBS;
    public string When => Record.NgayKe.ToString("dd/MM/yyyy HH:mm");
}
