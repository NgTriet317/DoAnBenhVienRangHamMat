// AI bổ sung kiểm tra nhập liệu: kiểm tra giảm giá ngay khi nhập, không âm/tối đa 2 số lẻ/không vượt tổng.
// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ form chọn nguồn thu hợp lệ, tạo hóa đơn nháp, đọc giảm giá và lấy mã sau lưu; đã giải thích kiểm tra số tiền và làm mới màn hình cha.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using BUS;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>
/// Màn hình biểu mẫu lập hóa đơn nháp theo lần khám: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class CuaSoHoaDon : UserControl, IBieuMauLuu, IPatientScoped
{
    /// <summary>
    /// Đối tượng nghiệp vụ hóa đơn được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private readonly BUS_HOADON finance = new();
    /// <summary>
    /// Cờ Đang cập nhật; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool updating;
    /// <summary>
    /// Tổng được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private decimal total;
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string? PatientId { get; private set; }
    /// <summary>
    /// Mã hóa đơn đã tạo thành công; giúp chọn hóa đơn mới và chặn bấm lưu tạo lặp cùng một form.
    /// </summary>
    public string? SavedInvoiceId { get; private set; }
    /// <summary>
    /// Thông báo đã tạo xong cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    public event Action<string>? CreationCompleted;
    /// <summary>
    /// Thông báo đóng được yêu cầu cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    public event Action? CloseRequested;
    /// <summary>
    /// Khởi tạo biểu mẫu lập hóa đơn nháp theo lần khám từ XAML.
    /// </summary>
    public CuaSoHoaDon()
    {
        InitializeComponent();
        Loaded += (_, _) => { if (PatientBox.ItemsSource is null) ConfigureContext(); };
    }
    /// <summary>
    /// Nạp danh mục bệnh nhân/nhân viên còn làm việc/lần khám đã xác nhận; chọn và khóa bệnh nhân/lần khám nguồn khi được truyền từ màn hình khác.
    /// </summary>
    public void ConfigureContext(string? patientId = null, string? visitId = null, string? employeeId = null)
    {
        PatientId = patientId; updating = true;
        try
        {
            PatientBox.ItemsSource = DuLieuGiaoDien.Options("BENH_NHAN").ToArray(); PatientBox.SelectedValue = patientId;
            EmployeeBox.ItemsSource = DuLieuGiaoDien.Rows("NHAN_VIEN").Where(r => UiRecord.Text(r, "TrangThai") == "Đang làm việc")
                .Select(r => new LuaChonDanhMuc(UiRecord.Text(r, "MaNV"), DuLieuGiaoDien.Label("NHAN_VIEN", r))).ToArray(); EmployeeBox.SelectedValue = employeeId;
            PopulateVisits(); VisitBox.SelectedValue = visitId;
            PatientBox.IsEnabled = string.IsNullOrEmpty(patientId); VisitBox.IsEnabled = string.IsNullOrEmpty(visitId);
        }
        finally { updating = false; }
        LoadCharges();
    }
    /// <summary>
    /// Chỉ đưa những lần khám đã xác nhận của bệnh nhân đang chọn vào ComboBox lập hóa đơn.
    /// </summary>
    private void PopulateVisits()
    {
        VisitBox.ItemsSource = DuLieuGiaoDien.Rows("LICH_SU_KHAM_BENH").Where(r => UiRecord.Text(r, "TrangThai") == "Đã xác nhận"
            && !string.IsNullOrEmpty(PatientId) && DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", r) == PatientId)
            .Select(r => new LuaChonDanhMuc(UiRecord.Text(r, "MaLS"), $"#{UiRecord.Text(r, "MaLS")} · {UiRecord.Date(r, "ThoiGianKham"):dd/MM/yyyy HH:mm}")).ToArray();
    }
    /// <summary>
    /// Gọi BUS lấy các khoản đủ điều kiện chưa lập hóa đơn của lần khám, tính tổng và bật nút lưu khi có khoản; báo lỗi nếu không đọc được.
    /// </summary>
    private void LoadCharges()
    {
        if (updating || ChargeGrid is null) return;
        if (DuLieuGiaoDien.PhamViLamSang) { SaveButton.IsEnabled = false; Feedback.Text = "Chứng từ lịch sử chỉ được đối chiếu và in lại."; return; }
        try
        {
            var visit = HoTroGiaoDien.Selected(VisitBox);
            var table = !string.IsNullOrEmpty(visit) ? finance.GetEligibleCharges(visit) : new DataTable();
            ChargeGrid.ItemsSource = table.DefaultView; total = table.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r["ThanhTien"]));
            ChargesInfo.Text = table.Rows.Count == 0 ? "Chưa có khoản thu hợp lệ mới. Xác nhận dịch vụ hoặc cấp đủ thuốc rồi tải lại."
                : $"{table.Rows.Count} khoản thu · Tổng {TreatmentMoney.Format(total)}. Giá và nguồn được kiểm tra lại khi lưu.";
            SaveButton.IsEnabled = table.Rows.Count > 0 && SavedInvoiceId is null; ShowTotal(); Feedback.Text = "";
        }
        catch (Exception ex) { SaveButton.IsEnabled = false; Feedback.Text = "Không thể tải khoản thu: " + ex.Message; }
    }
    /// <summary>
    /// Phân tích giảm giá không âm và không vượt tổng; cập nhật số tiền phải thu hoặc thông báo kiểm tra mức giảm.
    /// </summary>
    private void ShowTotal()
    {
        if (NetTotal is null) return;
        var valid=TryDiscount(out var discount,out var error);
        NetTotal.Text=valid?TreatmentMoney.Format(total-discount):"—";
        KiemTraNhapLieu.SetError(DiscountBox,valid?"":error);
    }
    /// <summary>AI tạo: kiểm tra giảm giá theo decimal(18,2), không âm và không vượt tổng ngay tại giao diện.</summary>
    private bool TryDiscount(out decimal discount,out string error)
    {
        error="";
        if(!ParseMoney(DiscountBox.Text,out discount)){error="Giảm giá phải là số không âm; không dùng phân cách hàng nghìn.";return false;}
        try {KiemTraNhapLieu.ValidateValues("HOA_DON",new Dictionary<string,object?> {["GiamGia"]=discount});}
        catch(LoiNhapLieu ex){error=ex.Message;return false;}
        if(discount>total){error="Giảm giá không được vượt tổng tiền hóa đơn.";return false;}
        return true;
    }
    /// <summary>
    /// Phân tích chuỗi tiền với dấu phẩy/chấm thập phân theo InvariantCulture; không nhận dấu phân cách hàng nghìn.
    /// </summary>
    private static bool ParseMoney(string value, out decimal amount) => decimal.TryParse(value.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount);
    /// <summary>
    /// Kiểm tra lần khám/nhân viên/giảm giá rồi gọi BUS_HOADON.CreateInvoice; ghi nhớ mã để tránh tạo lặp và phân biệt lỗi lưu với lỗi tải lại sau khi đã lưu thành công.
    /// </summary>
    public bool Commit(out string error)
    {
        error = "";
        if (DuLieuGiaoDien.PhamViLamSang) { error = "Chứng từ lịch sử chỉ được đối chiếu và in lại."; return false; }
        if (SavedInvoiceId is not null) return true;
        if(!KiemTraNhapLieu.PrepareSave(this,out error)) return false;
        try
        {
            var visit = HoTroGiaoDien.Selected(VisitBox); var employee = HoTroGiaoDien.Selected(EmployeeBox);
            if (string.IsNullOrEmpty(visit) || string.IsNullOrEmpty(employee)) throw new InvalidOperationException("Chọn lần khám đã xác nhận và nhân viên lập hóa đơn.");
            if (!TryDiscount(out var discount,out var discountError))
            {
                KiemTraNhapLieu.SetError(DiscountBox,discountError); DiscountBox.Focus();
                throw new InvalidOperationException(discountError);
            }
            SavedInvoiceId = finance.CreateInvoice(visit, employee, discount); SaveButton.IsEnabled = false;
            try { DuLieuGiaoDien.RefreshFromDatabase(); }
            catch (Exception ex) { Feedback.Text = $"Hóa đơn #{SavedInvoiceId} đã được lưu. Tải lại danh sách để xem: {ex.Message}"; }
            return true;
        }
        catch (Exception ex) { error = ex.Message; KiemTraNhapLieu.ShowError(this,error); return false; }
    }
    /// <summary>
    /// Đổi bệnh nhân nguồn, nạp lại danh sách lần khám đã xác nhận và tính các khoản thu mới; bỏ qua sự kiện khi đang đổ dữ liệu.
    /// </summary>
    private void Patient_Changed(object sender, SelectionChangedEventArgs e)
    { if (updating) return; PatientId = HoTroGiaoDien.Selected(PatientBox); updating = true; PopulateVisits(); updating = false; LoadCharges(); }
    /// <summary>
    /// Tải lại các khoản đủ điều kiện và tổng tiền khi người dùng chọn một lần khám khác.
    /// </summary>
    private void Visit_Changed(object sender, SelectionChangedEventArgs e) => LoadCharges();
    /// <summary>
    /// Tính lại số tiền phải thu mỗi lần người dùng thay nội dung ô giảm giá.
    /// </summary>
    private void Discount_Changed(object sender, TextChangedEventArgs e) => ShowTotal();
    /// <summary>
    /// Quay lại màn hình trước bằng sự kiện BackRequested/CloseRequested hoặc workspace; không tự lưu dữ liệu đang nhập.
    /// </summary>
    private void Back_Click(object sender, RoutedEventArgs e)
    { if (CloseRequested is not null) CloseRequested.Invoke(); else HoTroGiaoDien.Workspace(this)?.ReturnToPage(); }
    /// <summary>
    /// Lưu hóa đơn nháp, phát CreationCompleted để màn hình danh sách chọn hóa đơn mới hoặc chuyển workspace về hóa đơn của cùng bệnh nhân/lần khám.
    /// </summary>
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!Commit(out _) || SavedInvoiceId is not string id) return;
        if (CreationCompleted is not null) CreationCompleted.Invoke(id);
        else { var workspace = HoTroGiaoDien.Workspace(this); workspace?.ReturnToPage(); workspace?.DieuHuongHoSo("HoaDon", PatientId ?? "", HoTroGiaoDien.Selected(VisitBox)); }
    }
}
