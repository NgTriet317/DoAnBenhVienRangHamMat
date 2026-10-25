// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ giao diện tiếp nhận trực tiếp/từ lịch hẹn, chọn nguồn và kiểm tra trạng thái hàng đợi; đã chú thích quyền thao tác và luồng mở lần khám.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
/// <summary>
/// Màn hình tiếp nhận bệnh nhân: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class TiepNhanKham : UserControl
{
    /// <summary>
    /// Khởi tạo tiếp nhận bệnh nhân từ XAML.
    /// </summary>
    public TiepNhanKham() { InitializeComponent(); }
    /// <summary>
    /// Chuẩn bị danh mục, hiện nguồn theo lịch hoặc đến trực tiếp, khóa lựa chọn lịch nguồn; tính số thứ tự tiếp nhận mới theo ngày và chặn mở lần khám sau lưu nếu thiếu quyền.
    /// </summary>
    private void Form_Loaded(object sender,RoutedEventArgs e)
    {
        BieuMauHoSo.Prepare(this);
        var record=BieuMauHoSo.GetRecord(this);
        var appointment=record is null ? "" : UiRecord.Text(record,"MaLich");
        var hasAppointment=!string.IsNullOrEmpty(appointment);
        ArrivalKind.Text=hasAppointment?"Đến khám theo lịch hẹn qua điện thoại":"Đến trực tiếp bệnh viện";
        AppointmentBlock.Visibility=hasAppointment?Visibility.Visible:Visibility.Collapsed;
        AppointmentPicker.IsEnabled=false;
        if(!hasAppointment) AppointmentPicker.SelectedIndex=-1;
        if(record is null || DuLieuGiaoDien.Find("PHIEU_KHAM_BENH",UiRecord.Get(record,"MaPhieu")) is null)
        {
            var order=DuLieuGiaoDien.Rows("PHIEU_KHAM_BENH").Where(r=>UiRecord.Date(r,"ThoiGianTiepNhan")?.Date==DateTime.Today).Select(r=>int.TryParse(UiRecord.Text(r,"SoThuTu"),out var value)?value:0).DefaultIfEmpty().Max()+1;
            QueueNumber.Text=order.ToString();
        }
        if(DuLieuGiaoDien.PhamViLamSang || HoTroGiaoDien.Workspace(this)?.CanOpen("KhamDieuTri")!=true) { OpenVisitAfterSave.IsChecked=false; OpenVisitAfterSave.IsEnabled=false; }
    }
}
