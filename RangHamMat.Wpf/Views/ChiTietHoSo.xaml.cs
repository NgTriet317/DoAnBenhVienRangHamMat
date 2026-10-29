// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ vùng chi tiết dùng template XAML, thao tác sửa/xác nhận/hủy và chuyển lịch/tiếp nhận; đã giải thích cách giữ mã hồ sơ nguồn.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
/// <summary>
/// Màn hình chi tiết hồ sơ/lịch hẹn/bản ghi liên quan: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class ChiTietHoSo : UserControl, IPatientScoped
{
    /// <summary>
    /// Bảng được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly string table;
    /// <summary>
    /// Bản ghi được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly object record;
    /// <summary>
    /// Cờ Lịch hẹn chỉ đọc; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private readonly bool appointmentReadOnly;
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string? PatientId { get; }
    /// <summary>
    /// Khởi tạo chi tiết hồ sơ/lịch hẹn/bản ghi liên quan từ XAML.
    /// </summary>
    public ChiTietHoSo(string table, string title, object record)
    {
        this.table = table; this.record = record;
        appointmentReadOnly=table=="LICH_KHAM" && DuLieuGiaoDien.PhamViLamSang;
        InitializeComponent();
        EditButton.IsEnabled = DuLieuGiaoDien.CanEdit(table, record) || (table == "LICH_KHAM" && (UiRecord.Text(record, "TrangThai") is "Đã xác nhận" or "Chờ khám"));
        if (table == "LICH_KHAM")
        {
            EditButton.Content = DuLieuGiaoDien.CanEdit(table, record) ? "Chỉnh sửa" : "Hẹn lại";
            ConfirmButton.Visibility = DuLieuGiaoDien.CanEdit(table, record) ? Visibility.Visible : Visibility.Collapsed;
            CancelButton.Visibility = UiRecord.Text(record, "TrangThai") is "Hủy" or "Đã đổi" or "Hoàn thành" ? Visibility.Collapsed : Visibility.Visible;
            ReceptionButton.Visibility = UiRecord.Text(record, "TrangThai") is "Đã xác nhận" or "Chờ khám" ? Visibility.Visible : Visibility.Collapsed;
            if(appointmentReadOnly)
            {
                EditButton.Visibility=ConfirmButton.Visibility=CancelButton.Visibility=Visibility.Collapsed;
                if(!DuLieuGiaoDien.NguoiDungLamSang.CoQuyen(ET.QuyenLamViec.TiepNhan))ReceptionButton.Visibility=Visibility.Collapsed;
            }
        }
        PatientId = DuLieuGiaoDien.PatientId(table, record);
        DetailTitle.Text = title;
        DetailBody.Content = record;
        DetailBody.ContentTemplate = (DataTemplate)FindResource(table + "Details");
        if (table is "LICH_SU_KHAM_BENH" or "DON_THUOC")
        {
            RelatedCard.Content = table == "DON_THUOC" ? (object)new PrescriptionCard(record, true) : new ExaminationCard(record, true);
            RelatedCard.ContentTemplate = (DataTemplate)FindResource(table == "DON_THUOC" ? "PrescriptionCard" : "ExaminationCard");
        }
        if (table == "KE_HOACH_DIEU_TRI")
        {
            PlanDetails.Visibility = Visibility.Visible;
            PlanSteps.ItemsSource = DuLieuGiaoDien.Children("CHI_TIET_KE_HOACH", "MaKHDT", UiRecord.Get(record, "MaKHDT"));
            AddStep.CommandParameter = new CreationContext("CHI_TIET_KE_HOACH", "MaKHDT", UiRecord.Get(record, "MaKHDT")!);
            AddStep.IsEnabled = UiRecord.Text(record, "MaKHDT").Length > 0;
        }
        if (table == "LICH_SU_KHAM_BENH" && UiRecord.Text(record, "MaLS").Length > 0)
        {
            RelatedActions.Visibility = Visibility.Visible;
            var id = UiRecord.Get(record, "MaLS")!;
            AddCondition.CommandParameter = new CreationContext("TINH_TRANG_BENH", "MaLS", id);
            AddPlan.CommandParameter = new CreationContext("KE_HOACH_DIEU_TRI", "MaLS", id);
            AddService.CommandParameter = new CreationContext("DICH_VU_THUC_HIEN", "MaLS", id);
        }
    }
    /// <summary>
    /// Đọc bảng/bản ghi hiện tại và mở biểu mẫu sửa chuyên dụng nếu trạng thái cho phép.
    /// </summary>
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if(appointmentReadOnly)return;
        var module = table switch { "BENH_NHAN" or "HO_SO_BENH_AN" => "BenhNhan", "LICH_KHAM" => "LichKham", "DON_THUOC" or "CHI_TIET_DON_THUOC" => "DonThuoc", _ => "KhamDieuTri" };
        HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo(module, table, record, patient: PatientId);
    }
    /// <summary>
    /// Kiểm tra ngày giờ lịch, bệnh nhân và bác sĩ, chuyển lịch sang Đã xác nhận qua BUS rồi đóng modal; báo lỗi khi dữ liệu không hợp lệ.
    /// </summary>
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if(appointmentReadOnly)return;
        if (UiRecord.Date(record, "NgayGioKham") is null || DuLieuGiaoDien.Find("BENH_NHAN",UiRecord.Get(record,"MaBN")) is null || DuLieuGiaoDien.Find("NHAN_VIEN",UiRecord.Get(record,"MaBS")) is null) { MessageBox.Show("Kiểm tra ngày giờ, bệnh nhân và bác sĩ trước khi xác nhận."); return; }
        try {DuLieuGiaoDien.SetValue(table, record, "TrangThai", "Đã xác nhận"); HoTroGiaoDien.Workspace(this)?.ReturnToPage();}
        catch(InvalidOperationException ex) {MessageBox.Show(ex.Message,"Xác nhận lịch khám");}
    }
    /// <summary>
    /// Hỏi người dùng xác nhận hủy lịch rồi đổi trạng thái sang Hủy và đóng modal; bản ghi được giữ để tra cứu.
    /// </summary>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if(appointmentReadOnly)return;
        if (MessageBox.Show("Hủy lịch hẹn này?", "Lịch khám", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try {DuLieuGiaoDien.SetValue(table, record, "TrangThai", "Hủy"); HoTroGiaoDien.Workspace(this)?.ReturnToPage();}
        catch(InvalidOperationException ex) {MessageBox.Show(ex.Message,"Hủy lịch khám");}
    }
    /// <summary>
    /// Mở form tiếp nhận từ lịch/bệnh án đang xem, giữ liên kết bệnh nhân nguồn.
    /// </summary>
    private void Reception_Click(object sender, RoutedEventArgs e)
    {
        var medical = DuLieuGiaoDien.Children("HO_SO_BENH_AN", "MaBN", PatientId).FirstOrDefault();
        if (medical is null) { MessageBox.Show("Lập hồ sơ bệnh án cho bệnh nhân trước khi tiếp nhận."); return; }
        HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("TiepNhanKham", "PHIEU_KHAM_BENH", new Dictionary<string, object?> { ["MaHS"] = UiRecord.Get(medical,"MaHS"), ["MaLich"] = UiRecord.Get(record,"MaLich"), ["MaBS"] = UiRecord.Get(record,"MaBS"), ["MaPhong"] = UiRecord.Get(record,"MaPhong"), ["MaDV"] = UiRecord.Get(record,"MaDV"), ["LyDoKham"] = UiRecord.Get(record,"LyDoKham") }, patient: PatientId);
    }
}
