using System.ComponentModel;
using System.Windows;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Views;

/// <summary>
/// Phần xử lý kê đơn thuốc cửa sổ của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class KeDonThuocWindow:Window
{
    /// <summary>
    /// Trình nhập liệu.
    /// </summary>
    public KeDonThuocEditor Editor{get;}
    /// <summary>
    /// Đã xác nhận.
    /// </summary>
    public bool DaXacNhan{get;private set;}
    /// <summary>
    /// Rời màn hình lựa chọn. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public Func<string,MessageBoxResult>? LeaveDecision{get;set;}

    /// <summary>
    /// Khởi tạo kê đơn thuốc cửa sổ. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    /// <param name="editor">Giá trị trình nhập liệu dùng khi thực hiện thao tác; kiểu KeDonThuocEditor.</param>
    public KeDonThuocWindow(KeDonThuocEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        InitializeComponent();
        Width=Math.Min(Width,SystemParameters.WorkArea.Width);
        Height=Math.Min(Height,SystemParameters.WorkArea.Height);
        Editor=editor;
        Editor.UseDraftDialog();
        EditorHost.Content=editor;
    }

    /// <summary>
    /// Xác nhận nội dung cửa sổ kê thuốc theo điều kiện của trình nhập liệu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Editor.TryConfirmDraft</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool TryConfirm(out string error)
    {
        var success=Editor.TryConfirmDraft(out error);
        WindowError.Text=error;
        DaXacNhan=success;
        return success;
    }

    /// <summary>
    /// Tháo trình nhập liệu khỏi cửa sổ và giải phóng liên kết giao diện liên quan.
    /// </summary>
    public void ReleaseEditor()=>EditorHost.Content=null;

    /// <summary>
    /// Kiểm tra nội dung chưa lưu khi đóng cửa sổ và giữ cửa sổ mở nếu người dùng chọn ở lại.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ClinicalWindowLifecycle.OwnerMain</c>, <c>ClinicalPendingWork.TryLeave</c>, <c>base.OnClosing</c>.</remarks>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    protected override void OnClosing(CancelEventArgs e)
    {
        var decide=LeaveDecision??ClinicalWindowLifecycle.OwnerMain(this)?.LeaveDecision;
        if(!ClinicalPendingWork.TryLeave(new DependencyObject[]{this},decide,out var error))
        {
            e.Cancel=true;
            WindowError.Text=error;
            // A lifecycle Closing handler may also be attached. Do not raise it
            // when this guard has already kept the window open.
            return;
        }
        base.OnClosing(e);
    }

    /// <summary>
    /// Xử lý thao tác xác nhận khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TryConfirm</c>, <c>Close</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Confirm_Click(object sender,RoutedEventArgs e)
    {
        if(TryConfirm(out _))Close();
    }
    /// <summary>
    /// Xử lý thao tác đóng khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Close</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Close_Click(object sender,RoutedEventArgs e)=>Close();
}
