using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;
namespace RangHamMat.Views;
/// <summary>
/// Phần xử lý thực hiện dịch vụ trình nhập liệu của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class ThucHienDichVuEditor:UserControl,IPatientScoped
{
 /// <summary>
 /// Đối tượng nghiệp vụ dịch vụ thực hiện được màn hình dùng để đọc/ghi dữ liệu.
 /// </summary>
 private readonly BUS_DICHVUTHUCHIEN service;
 /// <summary>
 /// Ngữ cảnh định danh và quyền của người dùng đang thao tác.
 /// </summary>
 private readonly NguCanhNghiepVu actor;
 /// <summary>
 /// Phạm vi đang chọn được giữ trong phạm vi lớp để các thao tác dùng lại.
 /// </summary>
 private string? scope,selected;
 /// <summary>
 /// Cờ Cờ đang điền dữ liệu, dùng tránh xử lý lại sự kiện phát sinh do cập nhật giao diện; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
 /// </summary>
 private bool loading;
 /// <summary>
 /// Mã bệnh nhân. Có thể để null khi dữ liệu chưa được cung cấp.
 /// </summary>
 public string? PatientId{get;private set;}
 /// <summary>
 /// Giá trị Có thay đổi chưa lưu; được xác định bởi phần getter/setter của thuộc tính.
 /// </summary>
 public bool CoThayDoiChuaLuu=>false;
 /// <summary>
 /// Khởi tạo thực hiện dịch vụ trình nhập liệu. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
 /// </summary>
 /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
 /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
 public ThucHienDichVuEditor(NguCanhNghiepVu? user=null,IDbTransaction? transaction=null)
 {service=new(transaction);actor=new BUS_QUYENNGHIEPVU(transaction).LayNguCanh(user?.MaNV??PhienDangNhap.MaNV);InitializeComponent();LoadHistory();}
 /// <summary>
 /// Áp dụng bệnh nhân được chỉ định làm phạm vi dữ liệu của màn hình.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>FocusContext</c>.</remarks>
 /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
 public void SetPatient(string id)=>FocusContext(id);
 /// <summary>
 /// Chuyển màn hình tới đúng bệnh nhân/lần khám hoặc chứng từ theo ngữ cảnh được yêu cầu.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>LoadHistory</c>.</remarks>
 /// <param name="patientId">Giá trị mã bệnh nhân dùng khi thực hiện thao tác; kiểu string?.</param>
 /// <param name="visitId">Giá trị mã lần khám dùng khi thực hiện thao tác; kiểu string?.</param>
 public void FocusContext(string? patientId,string? visitId=null){scope=PatientId=patientId;selected=visitId;LoadHistory();}
 /// <summary>
 /// Nạp lịch sử dịch vụ theo phạm vi bệnh nhân/lần khám để hiển thị.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>service.LayLuotThucHien</c>, <c>rows.Select</c>, <c>rows.All</c>, <c>ShowHistory</c>.</remarks>
 private void LoadHistory()
 {
  ErrorText.Text="";loading=true;
  try{var rows=service.LayLuotThucHien(actor,scope);VisitPicker.ItemsSource=rows.Select(x=>new LuaChonDanhMuc(x.MaLS,$"{x.ThoiGianKham:dd/MM/yyyy HH:mm} · {x.HoTenBN} · {x.MaLS}")).ToArray();VisitPicker.SelectedValue=selected;if(selected is not null&&rows.All(x=>x.MaLS!=selected)){ActualGrid.ItemsSource=null;PatientHeader.Text="Chưa có dịch vụ đã chốt ở lần khám được yêu cầu.";VisitHeader.Text="Chọn một lần khám đã hoàn tất để xem lịch sử.";ActualSummary.Text="";}else if(selected is not null)ShowHistory(selected);else{ActualGrid.ItemsSource=null;PatientHeader.Text="Chọn người bệnh và lần khám để tra cứu.";VisitHeader.Text="Chỉ hiển thị lần khám đã hoàn tất.";ActualSummary.Text=$"{rows.Count} lần khám trong phạm vi";}}
  catch(InvalidOperationException ex){ActualGrid.ItemsSource=null;ErrorText.Text=ex.Message;}
  finally{loading=false;}
 }
 /// <summary>
 /// Hiển thị nguồn dịch vụ lịch sử đã chọn ở chế độ tra cứu.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>service.LayKhongGianThucHien</c>.</remarks>
 /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
 private void ShowHistory(string id)
 {var board=service.LayKhongGianThucHien(id,actor);selected=id;PatientId=board.Luot.MaBN;PatientHeader.Text=$"{board.Luot.HoTenBN} · {PatientId}";VisitHeader.Text=$"{board.Luot.MaLS} · {board.Luot.ThoiGianKham:dd/MM/yyyy HH:mm} · đã hoàn tất";ActualGrid.ItemsSource=board.ThucTe;ActualSummary.Text=$"{board.ThucTe.Count} dịch vụ · Giá trị đã chốt {board.GiaTriDaXacNhan:N0} đ";}
 /// <summary>
 /// Tiếp nhận thay đổi lần khám và cập nhật trạng thái/dữ liệu phụ thuộc.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>ShowHistory</c>.</remarks>
 /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
 /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
 private void Visit_Changed(object sender,SelectionChangedEventArgs e){if(loading||VisitPicker.SelectedValue is not string id)return;try{ShowHistory(id);ErrorText.Text="";}catch(InvalidOperationException ex){ActualGrid.ItemsSource=null;ErrorText.Text=ex.Message;}}
 /// <summary>
 /// Xử lý thao tác tải lại khi người dùng bấm nút tương ứng trên màn hình.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>LoadHistory</c>.</remarks>
 /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
 /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
 private void Reload_Click(object sender,RoutedEventArgs e)=>LoadHistory();
}