using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
/// <summary>
/// Phần xử lý dịch vụ thực hiện của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class DichVuThucHien:UserControl,IPatientScoped
{
 /// <summary>
 /// Lịch sử được giữ trong phạm vi lớp để các thao tác dùng lại.
 /// </summary>
 private readonly ThucHienDichVuEditor history;
 /// <summary>
 /// Giá trị Mã bệnh nhân; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
 /// </summary>
 public string? PatientId=>history.PatientId;
 /// <summary>
 /// Khởi tạo dịch vụ thực hiện. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
 /// </summary>
 public DichVuThucHien(){InitializeComponent();history=new();HistoryHost.Content=history;}
 /// <summary>
 /// Áp dụng bệnh nhân được chỉ định làm phạm vi dữ liệu của màn hình.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>history.SetPatient</c>.</remarks>
 /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
 public void SetPatient(string id)=>history.SetPatient(id);
 /// <summary>
 /// Chuyển màn hình tới đúng bệnh nhân/lần khám hoặc chứng từ theo ngữ cảnh được yêu cầu.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>history.FocusContext</c>.</remarks>
 /// <param name="patientId">Giá trị mã bệnh nhân dùng khi thực hiện thao tác; kiểu string?.</param>
 /// <param name="visitId">Giá trị mã lần khám dùng khi thực hiện thao tác; kiểu string?.</param>
 /// <param name="prescriptionId">Giá trị đơn thuốc mã dùng khi thực hiện thao tác; kiểu string?.</param>
 public void FocusContext(string? patientId,string? visitId=null,string? prescriptionId=null)=>history.FocusContext(patientId,visitId);
}