using ET;
using System.Windows;
using System.Windows.Controls;
namespace RangHamMat.Views;

public partial class KhamLamSang
{
    private KeDonThuocEditor? prescriptionEditor;
    private TongKetKham? clinicalSummary;
    private void RefreshClinicalSummary()
    {
        clinicalSummary=service.LayTongKetKham(visitId,actor);
        CompletionServicesList.ItemsSource=clinicalSummary.DichVu;
        CompletionDrugsList.ItemsSource=clinicalSummary.Thuoc;
        var noPrice=clinicalSummary.Thuoc.Any(x=>x.DonGia<=0);
        CompletionTotalText.Text=$"Dịch vụ: {clinicalSummary.TienDichVu:N0} đ · Thuốc: {clinicalSummary.TienThuoc:N0} đ"+
            (noPrice?" · Có thuốc chưa thiết lập giá":$" · Tổng: {clinicalSummary.TienDichVu+clinicalSummary.TienThuoc:N0} đ");
        if(clinicalSummary.SoDonNhap>0)CompletionTotalText.Text+=$"\nCòn {clinicalSummary.SoDonNhap} đơn nháp cần ký hoặc hủy.";
        ExecutionStatusText.Text=clinicalSummary.DaThucHien?"Đã xác nhận thực hiện. Có thể kê thuốc hoặc lưu lý do không kê thuốc.":"Chưa xác nhận thực hiện khám/điều trị.";
        ExecutionAcknowledged.IsChecked=clinicalSummary.DaThucHien;
        ExecutionAcknowledged.IsEnabled=Editable&&!clinicalSummary.DaThucHien;
        ExecutePlanButton.IsEnabled=Editable&&!clinicalSummary.DaThucHien;
        if(prescriptionEditor is null)
        {
            prescriptionEditor=new(actor,transaction);PrescriptionHost.Content=prescriptionEditor;
            ((FrameworkElement)prescriptionEditor.FindName("RxHeader")).Visibility=Visibility.Collapsed;
            prescriptionEditor.FocusContext(PatientId,visitId);
            prescriptionEditor.DonThuocThayDoi+=()=>Run(RefreshClinicalSummary,out _);
        }
        else prescriptionEditor.RefreshContext();
        if(historyReadOnly||!snapshot.DuocSua||!allowed("KhamDieuTri"))prescriptionEditor.RestrictToHistory();
        PrescriptionHost.IsEnabled=(clinicalSummary.DaThucHien||!snapshot.DuocSua)&&NoPrescription.IsChecked!=true;
        PrescriptionStageText.Text=!clinicalSummary.DaThucHien?"Xác nhận đã thực hiện ở bước 4 trước khi kê thuốc.":
            NoPrescription.IsChecked==true?"Lưu lý do không kê thuốc bằng nút Lưu khám, rồi chuyển Tổng kết.":"Chọn thuốc theo nhóm, nhập chỉ định, lưu nháp rồi ký đơn trước khi Tổng kết.";
        UpdateClinicalFooter();
    }
    public bool TryExecutePlan(out string error)=>Run(()=>
    {
        RequireEditable();
        if(CoThayDoiChuaLuu)throw new LoiKhamException("BanNhap","Lưu nội dung khám/kế hoạch/đơn đang nhập trước khi xác nhận thực hiện.");
        if(ExecutionAcknowledged.IsChecked!=true)throw new LoiKhamException("ThucHien","Xác nhận đã thực hiện xong các dịch vụ trước khi tiếp tục.");
        var before=clinicalSummary??service.LayTongKetKham(visitId,actor);
        service.XacNhanThucHien(visitId,before.DauVanTay,actor);
        var current=service.LayKhongGianKham(visitId,actor);SetPlans(current.KeHoach);
        if(plan is not null)FillPlan(service.LayKeHoach(plan.KeHoach.MaKHDT,actor));
        FillCompletionOverview(current);ClinicalStatus.Text="Đã ghi nhận dịch vụ thực hiện; chưa lập hóa đơn. Tiếp tục kê thuốc.";
        ClinicalTabs.SelectedItem=PrescriptionTab;SyncCache();
    },out error);
    private void ExecutePlan_Click(object sender,RoutedEventArgs e)=>TryExecutePlan(out _);
    private void ClinicalTabs_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(e.Source!=ClinicalTabs||CompleteClinicalButton is null)return;
        UpdateClinicalFooter();
        if(!loading&&snapshot is not null)Run(RefreshClinicalSummary,out _);
    }
    private void UpdateClinicalFooter()
    {
        if(CompleteClinicalButton is null||ReloadClinicalButton is null)return;
        var prescription=ClinicalTabs.SelectedItem==PrescriptionTab;
        CompleteClinicalButton.Visibility=ClinicalTabs.SelectedIndex==5?Visibility.Visible:Visibility.Collapsed;
        ReloadClinicalButton.Visibility=prescription?Visibility.Collapsed:Visibility.Visible;
        SaveClinicalButton.Visibility=prescription&&NoPrescription.IsChecked!=true?Visibility.Collapsed:Visibility.Visible;
    }
}
