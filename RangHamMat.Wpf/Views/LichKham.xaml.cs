using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
public sealed class HospitalAppointment
{
    public object Record{get;}
    public string Id=>UiRecord.Text(Record,"MaLich");
    public string PatientId=>ClinicalData.PatientId("LICH_KHAM",Record);
    public string Patient=>ClinicalData.Lookup("MaBN",PatientId);
    public string Phone=>UiRecord.Text(ClinicalData.Find("BENH_NHAN",PatientId)! ,"SDT");
    public string Doctor=>ClinicalData.Lookup("MaBS",UiRecord.Get(Record,"MaBS"));
    public string Room=>ClinicalData.Lookup("MaPhong",UiRecord.Get(Record,"MaPhong"));
    public string Service=>UiRecord.Text(Record,"MaDV").Length==0?"Chưa chỉ định":ClinicalData.Lookup("MaDV",UiRecord.Get(Record,"MaDV"));
    public string When=>UiRecord.Date(Record,"NgayGioKham")?.ToString("dd/MM/yyyy HH:mm")??"Chưa ghi thời gian";
    public string Status=>UiRecord.Text(Record,"TrangThai");
    public string Reason=>UiRecord.Text(Record,"LyDoKham");
    public bool Active=>Status is not "Hủy" and not "Đã đổi" and not "Hoàn thành";
    public HospitalAppointment(object record){Record=record;}
}
public partial class LichKham : UserControl,IPatientScoped
{
    public string? PatientId{get;private set;}
    private HospitalAppointment? selected;
    private bool refreshing;
    public LichKham(){InitializeComponent();ClinicalUi.Observe(this,Refresh);}
    public void SetPatient(string id)
    {
        PatientId = string.IsNullOrWhiteSpace(id) ? null : id;
        AddPatientButton.Visibility = PatientId is null ? Visibility.Visible : Visibility.Collapsed;
        Refresh();
    }
    private void Refresh()
    {
        if(Appointments is null || StatusFilter is null)return;
        var id=selected?.Id;refreshing=true;
        var rows=ClinicalData.Rows("LICH_KHAM").Where(row=>ClinicalUi.Matches("LICH_KHAM",row,SearchBox.Text,ClinicalUi.Choice(StatusFilter),PatientId) && (DayFilter.SelectedDate is null||UiRecord.Date(row,"NgayGioKham")?.Date==DayFilter.SelectedDate.Value.Date)).OrderBy(row=>UiRecord.Date(row,"NgayGioKham")).Select(row=>new HospitalAppointment(row)).ToArray();
        Appointments.ItemsSource=rows;Appointments.SelectedItem=rows.FirstOrDefault(row=>row.Id==id);refreshing=false;
        selected=Appointments.SelectedItem as HospitalAppointment;ShowSelection();
        EmptyList.Visibility=rows.Length==0?Visibility.Visible:Visibility.Collapsed;RecordCount.Text=$"{rows.Length} lịch hẹn";
    }
    private void ShowSelection()
    {
        DetailPanel.DataContext=selected;DetailPanel.Visibility=selected is null?Visibility.Collapsed:Visibility.Visible;EmptyDetail.Visibility=selected is null?Visibility.Visible:Visibility.Collapsed;
        if(selected is null)return;
        EditButton.IsEnabled=selected.Active;ConfirmButton.IsEnabled=ClinicalData.CanEdit("LICH_KHAM",selected.Record);CancelButton.IsEnabled=selected.Active;
        ReceiveButton.IsEnabled=selected.Active&&!ClinicalData.Children("PHIEU_KHAM_BENH","MaLich",selected.Id).Any(row=>UiRecord.Text(row,"TrangThai")!="Hủy");
        Feedback.Text="";
    }
    private void Selection_Changed(object sender,SelectionChangedEventArgs e){if(refreshing)return;selected=Appointments.SelectedItem as HospitalAppointment;ShowSelection();}
    private void Filter_Changed(object sender,RoutedEventArgs e)=>Refresh();
    private void Clear_Click(object sender,RoutedEventArgs e){SearchBox.Clear();DayFilter.SelectedDate=null;StatusFilter.SelectedIndex=0;}
    private void Create_Click(object sender,RoutedEventArgs e)=>ClinicalUi.Workspace(this)?.ShowClinicalEditor("LichKham","LICH_KHAM",context:PatientId is null?null:new CreationContext("LICH_KHAM","MaBN",PatientId),patient:PatientId);
    private void Edit_Click(object sender,RoutedEventArgs e){if(selected is not null)ClinicalUi.Workspace(this)?.ShowClinicalEditor("LichKham","LICH_KHAM",selected.Record,patient:selected.PatientId);}
    private void Patient_Click(object sender,RoutedEventArgs e){if(selected is not null)ClinicalNavigator.Open(this,"BenhNhan",selected.PatientId);}
    private void Confirm_Click(object sender,RoutedEventArgs e)
    {
        if(selected is null)return;
        try{ClinicalIntegrity.Validate("LICH_KHAM",ClinicalData.Values(selected.Record),selected.Id);ClinicalData.SetValue("LICH_KHAM",selected.Record,"TrangThai","Đã xác nhận");}catch(InvalidOperationException ex){Feedback.Text=ex.Message;}
    }
    private void Cancel_Click(object sender,RoutedEventArgs e)
    {
        if(selected is null||!selected.Active)return;
        if(MessageBox.Show("Hủy lịch hẹn này? Thông tin vẫn được giữ để tra cứu.","Hủy lịch hẹn",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)
            try {ClinicalData.SetValue("LICH_KHAM",selected.Record,"TrangThai","Hủy");}catch(InvalidOperationException ex){Feedback.Text=ex.Message;}
    }
    private void Receive_Click(object sender,RoutedEventArgs e)
    {
        if(selected is null)return;
        var medical=ClinicalData.Children("HO_SO_BENH_AN","MaBN",selected.PatientId).FirstOrDefault();
        if(medical is null){Feedback.Text="Bệnh nhân chưa có hồ sơ bệnh án. Mở hồ sơ bệnh nhân để lập bệnh án trước khi tiếp nhận.";return;}
        ClinicalUi.Workspace(this)?.ShowClinicalEditor("TiepNhanLamSang","PHIEU_KHAM_BENH",new Dictionary<string,object?>{["MaHS"]=UiRecord.Get(medical,"MaHS"),["MaLich"]=selected.Id,["MaBS"]=UiRecord.Get(selected.Record,"MaBS"),["MaPhong"]=UiRecord.Get(selected.Record,"MaPhong"),["MaDV"]=UiRecord.Get(selected.Record,"MaDV"),["LyDoKham"]=selected.Reason},new CreationContext("PHIEU_KHAM_BENH","MaHS",UiRecord.Get(medical,"MaHS")!),selected.PatientId);
    }
}
