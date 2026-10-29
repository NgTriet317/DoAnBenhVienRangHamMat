using ET;
using System.Windows;
using System.Windows.Controls;
namespace RangHamMat.Views;

/// <summary>
/// Phần xử lý khám lâm sàng của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class KhamLamSang
{
    /// <summary>
    /// Đơn thuốc trình nhập liệu được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private KeDonThuocEditor? prescriptionEditor;
    /// <summary>
    /// Đơn thuốc cửa sổ được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private KeDonThuocWindow? prescriptionWindow;
    /// <summary>
    /// Lâm sàng tổng kết được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private TongKetKham? clinicalSummary;
    /// <summary>
    /// Nạp lại tổng kết dịch vụ và thuốc cho lần khám đang mở.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>service.LayTongKetKham</c>, <c>clinicalSummary.Thuoc.Any</c>, <c>prescriptionEditor.FocusContext</c>, <c>Run</c>.</remarks>
    private void RefreshClinicalSummary()
    {
        clinicalSummary=service.LayTongKetKham(visitId,actor);
        CompletionServicesList.ItemsSource=clinicalSummary.DichVu;
        CompletionDrugsList.ItemsSource=clinicalSummary.Thuoc;
        var noPrice=clinicalSummary.Thuoc.Any(x=>x.DonGia<=0);
        CompletionTotalText.Text=$"Dịch vụ: {clinicalSummary.TienDichVu:N0} đ · Thuốc: {clinicalSummary.TienThuoc:N0} đ"+
            (noPrice?" · Có thuốc chưa thiết lập giá":$" · Tổng: {clinicalSummary.TienDichVu+clinicalSummary.TienThuoc:N0} đ");
        if(clinicalSummary.SoDonNhap>0)CompletionTotalText.Text+=$"\nCó {clinicalSummary.SoDonNhap} đơn chờ chốt; chốt lần khám sẽ ký đơn và lập hóa đơn.";
        ExecutionStatusText.Text=clinicalSummary.DaThucHien?"Đã xác nhận thực hiện. Có thể kê thuốc hoặc lưu lý do không kê thuốc.":"Chưa xác nhận thực hiện khám/điều trị.";
        ExecutionAcknowledged.IsChecked=clinicalSummary.DaThucHien;
        ExecutionAcknowledged.IsEnabled=Editable&&!clinicalSummary.DaThucHien;
        ExecutePlanButton.IsEnabled=Editable&&!clinicalSummary.DaThucHien;
        if(prescriptionEditor is null)
        {
            prescriptionEditor=new(actor,transaction);PrescriptionHost.Content=prescriptionEditor;
            prescriptionEditor.FocusContext(PatientId,visitId);
            prescriptionEditor.DonThuocThayDoi+=()=>Run(RefreshClinicalSummary,out _);
        }
        else prescriptionEditor.RefreshContext();
        if(historyReadOnly||!snapshot.DuocSua||!allowed("KhamDieuTri"))prescriptionEditor.RestrictToHistory();
        PrescriptionPreviewList.ItemsSource=clinicalSummary.Thuoc;
        PrescribeButton.Content=clinicalSummary.Thuoc.Count==0?"Kê thuốc":"Sửa danh sách thuốc";
        PrescribeButton.IsEnabled=Editable&&clinicalSummary.DaThucHien&&NoPrescription.IsChecked!=true;
        ReviewPrescriptionButton.IsEnabled=clinicalSummary.DaThucHien;
        PrescriptionPreviewHint.Text=clinicalSummary.Thuoc.Count==0?"Chưa chọn thuốc. Bấm Kê thuốc để lọc nhóm và lập danh sách, hoặc lưu lý do không kê thuốc.":
            $"{clinicalSummary.Thuoc.Count} dòng thuốc · Kiểm tra lại danh sách; có thể mở sửa trước khi chốt lần khám.";
        PrescriptionStageText.Text=!clinicalSummary.DaThucHien?"Xác nhận đã thực hiện ở bước 4 trước khi kê thuốc.":
            NoPrescription.IsChecked==true?"Lưu lý do không kê thuốc bằng nút Lưu khám, rồi chuyển Tổng kết.":"Bấm Kê thuốc, chọn và nhập chỉ định trong cửa sổ; Xác nhận để đưa danh sách về lần khám.";
        UpdateClinicalFooter();
    }
    /// <summary>
    /// Tạo cửa sổ kê thuốc gắn với đúng lần khám và phối hợp việc xác nhận bản đơn nháp.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>RequireEditable</c>, <c>editor.RefreshContext</c>, <c>Window.GetWindow</c>, <c>window.ReleaseEditor</c>.</remarks>
    /// <returns>Kết quả kiểu KeDonThuocWindow dùng cho bước xử lý tiếp theo.</returns>
    public KeDonThuocWindow TaoCuaSoKeThuoc()
    {
        RequireEditable();
        if(dirty||planDirty||conditionInputDirty)throw new LoiKhamException("BanNhap","Lưu nội dung khám và kế hoạch trước khi kê thuốc.");
        if(NoPrescription.IsChecked==true||clinicalSummary?.DaThucHien!=true)throw new LoiKhamException("ThucHien","Xác nhận thực hiện và bỏ lựa chọn Không kê thuốc trước khi kê đơn.");
        if(prescriptionWindow is not null)return prescriptionWindow;
        var editor=prescriptionEditor??throw new LoiKhamException("DonThuoc","Chưa nạp được bộ soạn đơn.");
        editor.RefreshContext();PrescriptionHost.Content=null;
        try
        {
            var window=new KeDonThuocWindow(editor);
            var owner=Window.GetWindow(this);if(owner?.IsVisible==true)window.Owner=owner;
            prescriptionWindow=window;
            window.Closed+=(_,_)=>
            {
                window.ReleaseEditor();PrescriptionHost.Content=editor;prescriptionWindow=null;
                Run(RefreshClinicalSummary,out _);
            };
            return window;
        }
        catch{PrescriptionHost.Content=editor;throw;}
    }
    /// <summary>
    /// Xử lý thao tác kê thuốc khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>TaoCuaSoKeThuoc</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Prescribe_Click(object sender,RoutedEventArgs e)=>Run(()=>TaoCuaSoKeThuoc().ShowDialog(),out _);
    /// <summary>
    /// Xử lý thao tác đối chiếu đơn thuốc khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ReviewPrescription_Click(object sender,RoutedEventArgs e)=>ClinicalTabs.SelectedIndex=5;
    /// <summary>
    /// Đối chiếu bản tổng kết và thử xác nhận dịch vụ đã thực hiện của lần khám.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>RequireEditable</c>, <c>service.LayTongKetKham</c>, <c>service.XacNhanThucHien</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
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
    /// <summary>
    /// Xử lý thao tác thực thi kế hoạch khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TryExecutePlan</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ExecutePlan_Click(object sender,RoutedEventArgs e)=>TryExecutePlan(out _);
    /// <summary>
    /// Tiếp nhận thay đổi lâm sàng các tab và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>UpdateClinicalFooter</c>, <c>Run</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ClinicalTabs_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(e.Source!=ClinicalTabs||CompleteClinicalButton is null)return;
        UpdateClinicalFooter();
        if(!loading&&snapshot is not null)Run(RefreshClinicalSummary,out _);
    }
    /// <summary>
    /// Cập nhật nút và thông tin cuối màn hình theo bước khám đang chọn.
    /// </summary>
    private void UpdateClinicalFooter()
    {
        if(CompleteClinicalButton is null||ReloadClinicalButton is null)return;
        var prescription=ClinicalTabs.SelectedItem==PrescriptionTab;
        CompleteClinicalButton.Visibility=ClinicalTabs.SelectedIndex==5?Visibility.Visible:Visibility.Collapsed;
        ReloadClinicalButton.Visibility=prescription?Visibility.Collapsed:Visibility.Visible;
        SaveClinicalButton.Visibility=prescription&&NoPrescription.IsChecked!=true&&!dirty&&!conditionInputDirty&&!planDirty?Visibility.Collapsed:Visibility.Visible;
        PrescribeButton.IsEnabled=Editable&&clinicalSummary?.DaThucHien==true&&NoPrescription.IsChecked!=true&&!dirty&&!conditionInputDirty&&!planDirty;
    }
}
