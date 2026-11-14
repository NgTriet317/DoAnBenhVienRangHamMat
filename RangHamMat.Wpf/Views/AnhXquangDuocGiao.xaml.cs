using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>
/// Danh sách lượt chụp đang mở được giao cho người dùng, mở ảnh theo đúng lần khám đã chọn.
/// </summary>
public partial class AnhXquangDuocGiao : UserControl
{
    /// <summary>
    /// Ngữ cảnh kỹ thuật viên hoặc người dùng đang xem các lượt được giao.
    /// </summary>
    private readonly NguCanhNghiepVu actor;
    /// <summary>
    /// Giao dịch tùy chọn được truyền tiếp cho danh sách và màn hình ảnh của từng lần khám.
    /// </summary>
    private readonly IDbTransaction? transaction;
    /// <summary>
    /// Cấu hình kho ảnh dùng chung khi mở một lượt chụp từ danh sách.
    /// </summary>
    private readonly CauHinhKhoAnh? config;
    /// <summary>
    /// Nạp giao diện và các lượt chụp trong phạm vi phân công của người dùng.
    /// </summary>
    /// <param name="user">Ngữ cảnh người dùng cần đọc các lượt được giao.</param>
    /// <param name="transaction">Giao dịch tùy chọn dùng cho các lời gọi nghiệp vụ liên quan.</param>
    /// <param name="cauHinh">Cấu hình kho ảnh tùy chọn truyền cho màn hình chi tiết.</param>
    public AnhXquangDuocGiao(NguCanhNghiepVu user,IDbTransaction? transaction=null,CauHinhKhoAnh? cauHinh=null)
    {actor=user;this.transaction=transaction;config=cauHinh;InitializeComponent();Reload();}
    /// <summary>
    /// Xóa chi tiết cũ trước khi đọc lại các lượt đang mở được giao; hiển thị lỗi nếu không đọc được phạm vi.
    /// </summary>
    private void Reload()
    {
        ImageHost.Content=null;
        try
        {
            var rows=new BUS_ANHXQUANG(transaction,config).LayLuotDuocGiao(actor);
            VisitPicker.ItemsSource=rows.Select(x=>new LuaChonDanhMuc(x.MaLS,$"{x.ThoiGianKham:dd/MM HH:mm} · {x.HoTenBN} ({x.MaBN}) · {x.MaLS}")).ToArray();
            StatusText.Text=rows.Count==0?"Chưa có lượt chụp đang mở được giao trong phạm vi của bạn.":$"{rows.Count} lượt chụp đang mở. Quyền được kiểm tra lại khi tải hoặc xem ảnh.";
        }
        catch(InvalidOperationException ex){VisitPicker.ItemsSource=null;StatusText.Text=ex.Message;}
    }
    /// <summary>
    /// Mở điều khiển ảnh của lần khám đang chọn với cùng người dùng, giao dịch và cấu hình kho.
    /// </summary>
    private void Visit_Changed(object sender,SelectionChangedEventArgs e)
    {ImageHost.Content=VisitPicker.SelectedValue is string id?new AnhXquangLanKham(id,actor,transaction,config):null;}
    /// <summary>
    /// Tải lại danh sách các lượt chụp đang mở được giao cho người dùng.
    /// </summary>
    private void Reload_Click(object sender,RoutedEventArgs e)=>Reload();
}
