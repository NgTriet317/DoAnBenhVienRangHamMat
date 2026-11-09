using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace RangHamMat.Controls.ClinicalWorkflow;

/// <summary>
/// Mô hình và thao tác hỗ trợ lâm sàng cửa sổ vòng đời dùng bởi các thành phần liên quan.
/// </summary>
internal static class ClinicalWindowLifecycle
{
    /// <summary>
    /// Mô hình và thao tác hỗ trợ mốc đánh dấu dùng bởi các thành phần liên quan.
    /// </summary>
    private sealed class Marker { }
    /// <summary>
    /// Được theo dõi được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static readonly ConditionalWeakTable<Window,Marker> tracked=new();
    /// <summary>
    /// Gắn cửa sổ chính làm owner khi phù hợp để các cửa sổ nghiệp vụ quản lý tiêu điểm và vòng đời thống nhất.
    /// </summary>
    /// <param name="window">Giá trị cửa sổ dùng khi thực hiện thao tác; kiểu Window?.</param>
    /// <returns>Đối tượng kiểu MainWindow được tìm hoặc tính toán; có thể null theo nhánh xử lý trong hàm.</returns>
    internal static MainWindow? OwnerMain(Window? window)
    {while(window is not null){if(window is MainWindow main)return main;window=window.Owner;}return null;}
    /// <summary>
    /// Tạo hoặc cấu hình cửa sổ có owner theo quy tắc vòng đời của ứng dụng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>parent.OwnedWindows.Cast&lt;Window&gt;</c>, <c>Owned</c>.</remarks>
    /// <param name="parent">Giá trị bản ghi cha dùng khi thực hiện thao tác; kiểu Window.</param>
    /// <returns>Tập dữ liệu kiểu IEnumerable&lt;Window&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
    internal static IEnumerable<Window> Owned(Window parent)
    {foreach(var child in parent.OwnedWindows.Cast<Window>().ToArray()){yield return child;foreach(var descendant in Owned(child))yield return descendant;}}
    /// <summary>
    /// Nhận diện cửa sổ thuộc nhóm nghiệp vụ lâm sàng cần quản lý công việc chưa lưu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>tracked.TryGetValue</c>, <c>ClinicalPendingWork.Nodes</c>, <c>QuyenNghiepVu.LaPhanHe</c>, <c>c.GetType</c>.</remarks>
    /// <param name="window">Giá trị cửa sổ dùng khi thực hiện thao tác; kiểu Window.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    internal static bool IsClinical(Window window)=>tracked.TryGetValue(window,out _)
        ||window is Views.XemTruocHoaDon or Views.XemTruocDonThuoc
        ||ClinicalPendingWork.Nodes(window).Any(n=>n is IClinicalPendingWork||n is UserControl c&&(QuyenNghiepVu.LaPhanHe(c.GetType().Name)||BieuMauHoSo.IsClinicalForm(c)));
    /// <summary>
    /// Gắn xử lý vòng đời cửa sổ và kiểm tra thay đổi chưa lưu khi đóng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>tracked.TryGetValue</c>, <c>tracked.Add</c>, <c>OwnerMain</c>, <c>new[]{window}.Concat</c>.</remarks>
    /// <param name="window">Giá trị cửa sổ dùng khi thực hiện thao tác; kiểu Window?.</param>
    internal static void Attach(Window? window)
    {
        if(window is null or MainWindow||tracked.TryGetValue(window,out _))return;
        tracked.Add(window,new());
        window.Closing+=(_,e)=>
        {
            var owner=OwnerMain(window);if(owner is null)return;
            var roots=new[]{window}.Concat(Owned(window).Where(IsClinical)).Cast<DependencyObject>();
            if(ClinicalPendingWork.TryLeave(roots,owner.LeaveDecision,out var error))return;
            e.Cancel=true;
            var status=ClinicalPendingWork.Nodes(window).OfType<TextBlock>().FirstOrDefault(t=>t.Name is "ErrorText" or "ClinicalError" or "FormError" or "StatusText");
            if(status is not null&&!string.IsNullOrEmpty(error))status.Text=error;
        };
    }
}
