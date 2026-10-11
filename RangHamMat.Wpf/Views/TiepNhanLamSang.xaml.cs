using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
public partial class TiepNhanLamSang : UserControl
{
    public TiepNhanLamSang() { InitializeComponent(); }
    private void Form_Loaded(object sender,RoutedEventArgs e)
    {
        ClinicalForm.Prepare(this);
        var record=ClinicalForm.GetRecord(this);
        var appointment=record is null ? "" : UiRecord.Text(record,"MaLich");
        var hasAppointment=!string.IsNullOrEmpty(appointment);
        ArrivalKind.Text=hasAppointment?"Đến khám theo lịch hẹn qua điện thoại":"Đến trực tiếp bệnh viện";
        AppointmentBlock.Visibility=hasAppointment?Visibility.Visible:Visibility.Collapsed;
        AppointmentPicker.IsEnabled=false;
        if(!hasAppointment) AppointmentPicker.SelectedIndex=-1;
        if(record is null || ClinicalData.Find("PHIEU_KHAM_BENH",UiRecord.Get(record,"MaPhieu")) is null)
        {
            var order=ClinicalData.Rows("PHIEU_KHAM_BENH").Where(r=>UiRecord.Date(r,"ThoiGianTiepNhan")?.Date==DateTime.Today).Select(r=>int.TryParse(UiRecord.Text(r,"SoThuTu"),out var value)?value:0).DefaultIfEmpty().Max()+1;
            QueueNumber.Text=order.ToString();
        }
        if(ClinicalUi.Workspace(this)?.CanOpen("KhamDieuTri")!=true) { OpenVisitAfterSave.IsChecked=false; OpenVisitAfterSave.IsEnabled=false; }
    }
}
