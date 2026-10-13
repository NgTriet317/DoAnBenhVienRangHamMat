// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ bộ lọc lịch, chuyển bảng/lịch, thêm/sửa lịch và chuyển tiếp nhận; đã giải thích phạm vi bệnh nhân và trạng thái lịch.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
/// <summary>
/// Mô hình lịch hẹn cho bảng và vùng chọn: nhãn bệnh nhân/bác sĩ/phòng/dịch vụ, giờ và trạng thái còn hiệu lực.
/// </summary>
public sealed class HospitalAppointment
{
    /// <summary>
    /// Bản ghi nguồn đã đọc để mô hình tra các trường/liên kết; không phải một bản SQL mới được tạo tự động.
    /// </summary>
    public object Record{get;}
    /// <summary>
    /// Mã khóa của bản ghi dùng để giữ lựa chọn và mở đúng chi tiết.
    /// </summary>
    public string Id=>UiRecord.Text(Record,"MaLich");
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string PatientId=>DuLieuGiaoDien.PatientId("LICH_KHAM",Record);
    /// <summary>
    /// Thông tin hoặc nhãn bệnh nhân liên kết với bản ghi/hồ sơ đang xem.
    /// </summary>
    public string Patient=>DuLieuGiaoDien.Lookup("MaBN",PatientId);
    /// <summary>
    /// Số điện thoại của bệnh nhân nguồn, giữ dạng chuỗi để không mất số 0 đầu.
    /// </summary>
    public string Phone=>UiRecord.Text(DuLieuGiaoDien.Find("BENH_NHAN",PatientId)! ,"SDT");
    /// <summary>
    /// Nhãn bác sĩ hoặc nhân viên thực hiện được tra từ mã liên kết trong bản ghi nguồn.
    /// </summary>
    public string Doctor=>DuLieuGiaoDien.Lookup("MaBS",UiRecord.Get(Record,"MaBS"));
    /// <summary>
    /// Nhãn phòng khám được tra từ danh mục theo mã phòng của bản ghi.
    /// </summary>
    public string Room=>DuLieuGiaoDien.Lookup("MaPhong",UiRecord.Get(Record,"MaPhong"));
    /// <summary>
    /// Nhãn dịch vụ liên kết của dòng hoặc lịch hẹn.
    /// </summary>
    public string Service=>UiRecord.Text(Record,"MaDV").Length==0?"Chưa chỉ định":DuLieuGiaoDien.Lookup("MaDV",UiRecord.Get(Record,"MaDV"));
    /// <summary>
    /// Ngày/giờ của dòng nghiệp vụ được định dạng để hiển thị; nội dung mặc định cho biết chưa có thời gian.
    /// </summary>
    public string When=>UiRecord.Date(Record,"NgayGioKham")?.ToString("dd/MM/yyyy HH:mm")??"Chưa ghi thời gian";
    /// <summary>
    /// Trạng thái hiển thị của bản ghi; với bước kế hoạch có thể được suy ra thêm từ lượng thực hiện đã xác nhận.
    /// </summary>
    public string Status=>UiRecord.Text(Record,"TrangThai");
    /// <summary>
    /// Lý do nghiệp vụ đọc từ tiếp nhận, lịch hẹn hoặc đơn thuốc nguồn.
    /// </summary>
    public string Reason=>UiRecord.Text(Record,"LyDoKham");
    /// <summary>
    /// Lịch còn hiệu lực khi chưa Hủy, Đã đổi hoặc Hoàn thành; dùng bật thao tác tiếp nhận/hủy.
    /// </summary>
    public bool Active=>Status is not "Hủy" and not "Đã đổi" and not "Hoàn thành";
    /// <summary>
    /// Tạo đối tượng HospitalAppointment. Mô hình lịch hẹn cho bảng và vùng chọn: nhãn bệnh nhân/bác sĩ/phòng/dịch vụ, giờ và trạng thái còn hiệu lực.
    /// </summary>
    public HospitalAppointment(object record){Record=record;}
}
/// <summary>
/// Màn hình lịch khám theo hồ sơ/toàn cục: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class LichKham : UserControl,IPatientScoped
{
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string? PatientId{get;private set;}
    private HospitalAppointment? selected;
    private bool refreshing;
    /// <summary>
    /// Khởi tạo lịch khám theo hồ sơ/toàn cục từ XAML. Theo dõi cache khi Loaded và làm mới nội dung khi dữ liệu thay đổi; gỡ theo dõi khi Unloaded.
    /// </summary>
    public LichKham(){InitializeComponent();HoTroGiaoDien.Observe(this,Refresh);}
    /// <summary>
    /// Giới hạn lịch theo một bệnh nhân và ẩn nút thêm bệnh nhân trong lịch nhúng ở hồ sơ.
    /// </summary>
    public void SetPatient(string id)
    {
        PatientId = string.IsNullOrWhiteSpace(id) ? null : id;
        AddPatientButton.Visibility = PatientId is null ? Visibility.Visible : Visibility.Collapsed;
        Refresh();
    }
    /// <summary>
    /// Lọc lịch theo bệnh nhân/từ khóa/ngày/trạng thái, cập nhật bảng và control lịch trực quan từ cùng nguồn dữ liệu.
    /// </summary>
    private void Refresh()
    {
        if(Appointments is null || StatusFilter is null)return;
        var id=selected?.Id;refreshing=true;
        var rows=DuLieuGiaoDien.Rows("LICH_KHAM").Where(row=>HoTroGiaoDien.Matches("LICH_KHAM",row,SearchBox.Text,HoTroGiaoDien.Choice(StatusFilter),PatientId) && (DayFilter.SelectedDate is null||UiRecord.Date(row,"NgayGioKham")?.Date==DayFilter.SelectedDate.Value.Date)).OrderBy(row=>UiRecord.Date(row,"NgayGioKham")).Select(row=>new HospitalAppointment(row)).ToArray();
        Appointments.ItemsSource=rows;Appointments.SelectedItem=rows.FirstOrDefault(row=>row.Id==id);refreshing=false;
        selected=Appointments.SelectedItem as HospitalAppointment;ShowSelection();
        EmptyList.Visibility=rows.Length==0?Visibility.Visible:Visibility.Collapsed;RecordCount.Text=$"{rows.Length} lịch hẹn";
    }
    /// <summary>
    /// Đưa lịch đang chọn vào vùng chi tiết, cập nhật nút sửa/xác nhận/hủy/tiếp nhận theo trạng thái; không chọn thì ẩn chi tiết.
    /// </summary>
    private void ShowSelection()
    {
        DetailPanel.DataContext=selected;DetailPanel.Visibility=selected is null?Visibility.Collapsed:Visibility.Visible;EmptyDetail.Visibility=selected is null?Visibility.Visible:Visibility.Collapsed;
        if(selected is null)return;
        EditButton.IsEnabled=selected.Active;ConfirmButton.IsEnabled=DuLieuGiaoDien.CanEdit("LICH_KHAM",selected.Record);CancelButton.IsEnabled=selected.Active;
        ReceiveButton.IsEnabled=selected.Active&&!DuLieuGiaoDien.Children("PHIEU_KHAM_BENH","MaLich",selected.Id).Any(row=>UiRecord.Text(row,"TrangThai")!="Hủy");
        Feedback.Text="";
    }
    /// <summary>
    /// Cập nhật bản ghi được chọn và vùng chi tiết; bỏ qua sự kiện phát sinh khi đang đổ lại dữ liệu.
    /// </summary>
    private void Selection_Changed(object sender,SelectionChangedEventArgs e){if(refreshing)return;selected=Appointments.SelectedItem as HospitalAppointment;ShowSelection();}
    /// <summary>
    /// Lọc và hiển thị lại danh sách theo nội dung tìm kiếm/trạng thái/ngày đang chọn; chưa thay đổi dữ liệu SQL.
    /// </summary>
    private void Filter_Changed(object sender,RoutedEventArgs e)=>Refresh();
    /// <summary>
    /// Bỏ các giá trị lọc hiện tại và hiển thị lại dữ liệu phù hợp.
    /// </summary>
    private void Clear_Click(object sender,RoutedEventArgs e){SearchBox.Clear();DayFilter.SelectedDate=null;StatusFilter.SelectedIndex=0;}
    /// <summary>
    /// Mở form đặt lịch mới, truyền mã bệnh nhân cha nếu trang đang giới hạn trong một hồ sơ.
    /// </summary>
    private void Create_Click(object sender,RoutedEventArgs e)=>HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("LichKham","LICH_KHAM",context:PatientId is null?null:new CreationContext("LICH_KHAM","MaBN",PatientId),patient:PatientId);
    /// <summary>
    /// Mở form sửa lịch nháp hoặc hẹn lại lịch đã xác nhận, giữ đúng bệnh nhân của lịch đang chọn.
    /// </summary>
    private void Edit_Click(object sender,RoutedEventArgs e){if(selected is not null)HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("LichKham","LICH_KHAM",selected.Record,patient:selected.PatientId);}
    /// <summary>
    /// Mở hồ sơ bệnh nhân nguồn của bản ghi đang chọn, giữ mã bệnh nhân qua điều hướng của workspace.
    /// </summary>
    private void Patient_Click(object sender,RoutedEventArgs e){if(selected is not null)DieuHuongHoSo.Open(this,"BenhNhan",selected.PatientId);}
    /// <summary>
    /// Chuyển lịch nháp đang chọn sang Đã xác nhận qua cache/BUS; lỗi nghiệp vụ được hiển thị cho người dùng.
    /// </summary>
    private void Confirm_Click(object sender,RoutedEventArgs e)
    {
        if(selected is null)return;
        try{DuLieuGiaoDien.SetValue("LICH_KHAM",selected.Record,"TrangThai","Đã xác nhận");}catch(InvalidOperationException ex){Feedback.Text=ex.Message;}
    }
    /// <summary>
    /// Kiểm tra lịch còn hiệu lực, hỏi người dùng xác nhận hủy rồi đổi TrangThai sang Hủy qua BUS; giữ bản ghi để tra cứu và hiện lỗi nếu thao tác bị chặn.
    /// </summary>
    private void Cancel_Click(object sender,RoutedEventArgs e)
    {
        if(selected is null||!selected.Active)return;
        if(MessageBox.Show("Hủy lịch hẹn này? Thông tin vẫn được giữ để tra cứu.","Hủy lịch hẹn",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)
            try {DuLieuGiaoDien.SetValue("LICH_KHAM",selected.Record,"TrangThai","Hủy");}catch(InvalidOperationException ex){Feedback.Text=ex.Message;}
    }
    /// <summary>
    /// Kiểm tra bệnh án của bệnh nhân rồi mở form tiếp nhận với các mã bệnh án/lịch/bác sĩ/phòng được điền từ lịch hẹn.
    /// </summary>
    private void Receive_Click(object sender,RoutedEventArgs e)
    {
        if(selected is null)return;
        var medical=DuLieuGiaoDien.Children("HO_SO_BENH_AN","MaBN",selected.PatientId).FirstOrDefault();
        if(medical is null){Feedback.Text="Bệnh nhân chưa có hồ sơ bệnh án. Mở hồ sơ bệnh nhân để lập bệnh án trước khi tiếp nhận.";return;}
        HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("TiepNhanKham","PHIEU_KHAM_BENH",new Dictionary<string,object?>{["MaHS"]=UiRecord.Get(medical,"MaHS"),["MaLich"]=selected.Id,["MaBS"]=UiRecord.Get(selected.Record,"MaBS"),["MaPhong"]=UiRecord.Get(selected.Record,"MaPhong"),["MaDV"]=UiRecord.Get(selected.Record,"MaDV"),["LyDoKham"]=selected.Reason},new CreationContext("PHIEU_KHAM_BENH","MaHS",UiRecord.Get(medical,"MaHS")!),selected.PatientId);
    }
}
