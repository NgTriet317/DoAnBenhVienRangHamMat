using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;
namespace RangHamMat.Views;
public partial class ThucHienDichVuEditor:UserControl,IPatientScoped
{
 private readonly BUS_DICHVUTHUCHIEN service;
 private readonly NguCanhNghiepVu actor;
 private string? scope,selected;
 private bool loading;
 public string? PatientId{get;private set;}
 public bool CoThayDoiChuaLuu=>false;
 public ThucHienDichVuEditor(NguCanhNghiepVu? user=null,IDbTransaction? transaction=null)
 {service=new(transaction);actor=new BUS_QUYENNGHIEPVU(transaction).LayNguCanh(user?.MaNV??PhienDangNhap.MaNV);InitializeComponent();LoadHistory();}
 public void SetPatient(string id)=>FocusContext(id);
 public void FocusContext(string? patientId,string? visitId=null){scope=PatientId=patientId;selected=visitId;LoadHistory();}
 private void LoadHistory()
 {
  ErrorText.Text="";loading=true;
  try{var rows=service.LayLuotThucHien(actor,scope);VisitPicker.ItemsSource=rows.Select(x=>new LuaChonDanhMuc(x.MaLS,$"{x.ThoiGianKham:dd/MM/yyyy HH:mm} · {x.HoTenBN} · {x.MaLS}")).ToArray();VisitPicker.SelectedValue=selected;if(selected is not null&&rows.All(x=>x.MaLS!=selected)){ActualGrid.ItemsSource=null;PatientHeader.Text="Chưa có dịch vụ đã chốt ở lần khám được yêu cầu.";VisitHeader.Text="Chọn một lần khám đã hoàn tất để xem lịch sử.";ActualSummary.Text="";}else if(selected is not null)ShowHistory(selected);else{ActualGrid.ItemsSource=null;PatientHeader.Text="Chọn người bệnh và lần khám để tra cứu.";VisitHeader.Text="Chỉ hiển thị lần khám đã hoàn tất.";ActualSummary.Text=$"{rows.Count} lần khám trong phạm vi";}}
  catch(InvalidOperationException ex){ActualGrid.ItemsSource=null;ErrorText.Text=ex.Message;}
  finally{loading=false;}
 }
 private void ShowHistory(string id)
 {var board=service.LayKhongGianThucHien(id,actor);selected=id;PatientId=board.Luot.MaBN;PatientHeader.Text=$"{board.Luot.HoTenBN} · {PatientId}";VisitHeader.Text=$"{board.Luot.MaLS} · {board.Luot.ThoiGianKham:dd/MM/yyyy HH:mm} · đã hoàn tất";ActualGrid.ItemsSource=board.ThucTe;ActualSummary.Text=$"{board.ThucTe.Count} dịch vụ · Giá trị đã chốt {board.GiaTriDaXacNhan:N0} đ";}
 private void Visit_Changed(object sender,SelectionChangedEventArgs e){if(loading||VisitPicker.SelectedValue is not string id)return;try{ShowHistory(id);ErrorText.Text="";}catch(InvalidOperationException ex){ActualGrid.ItemsSource=null;ErrorText.Text=ex.Message;}}
 private void Reload_Click(object sender,RoutedEventArgs e)=>LoadHistory();
}