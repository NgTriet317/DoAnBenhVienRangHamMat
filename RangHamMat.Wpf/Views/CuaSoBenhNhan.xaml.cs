// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ form bệnh nhân: đọc BHYT/điện thoại/ngày sinh có thể trống, chọn chế độ thêm/sửa và cập nhật hồ sơ; đã giải thích kiểm tra đầu vào.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
/// <summary>
/// Màn hình biểu mẫu bệnh nhân/bệnh án: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class CuaSoBenhNhan : UserControl
{
    /// <summary>
    /// Khởi tạo biểu mẫu bệnh nhân/bệnh án từ XAML. Theo dõi cache khi Loaded và làm mới nội dung khi dữ liệu thay đổi; gỡ theo dõi khi Unloaded. Giới hạn ngày sinh tối đa hôm nay, không tự chọn ngày sinh.
    /// </summary>
    public CuaSoBenhNhan()
    {
        InitializeComponent();
        BirthDate.DisplayDateEnd = DateTime.Today;
        HoTroGiaoDien.Observe(this, RefreshForm);
    }
    /// <summary>
    /// Chuẩn bị danh mục, xác định tạo bệnh nhân/sửa bệnh nhân hay bệnh án; đổi tiêu đề và chỉ hiện lựa chọn tạo bệnh án tiếp khi đang thêm bệnh nhân mới.
    /// </summary>
    private void RefreshForm()
    {
        BieuMauHoSo.Prepare(this);
        var table=BieuMauHoSo.GetTable(this); var existing=BieuMauHoSo.GetRecord(this);
        var newPatient=table=="BENH_NHAN" && (existing is null || DuLieuGiaoDien.Find(table,UiRecord.Get(existing,"MaBN")) is null);
        CreateMedical.Visibility=newPatient?Visibility.Visible:Visibility.Collapsed;
        EditorTitle.Text=table=="HO_SO_BENH_AN"?"Hồ sơ bệnh án":newPatient?"Thêm bệnh nhân":"Cập nhật thông tin bệnh nhân";
    }
}
