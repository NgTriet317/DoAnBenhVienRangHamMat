using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;

/// <summary>
/// Phần xử lý khám lâm sàng của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class KhamLamSang:IClinicalPendingWork
{
    /// <summary>
    /// Giá trị Công việc tiêu đề; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    string IClinicalPendingWork.WorkTitle=>"Nội dung khám/kế hoạch điều trị";
    /// <summary>
    /// Giá trị Có thay đổi chưa lưu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    bool IClinicalPendingWork.HasChanges=>dirty||planDirty||conditionInputDirty;
    /// <summary>
    /// Thử lưu nội dung đang sửa trước khi rời màn hình; trả kết quả để nơi gọi quyết định có tiếp tục điều hướng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TrySave</c>, <c>TrySavePlan</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    bool IClinicalPendingWork.SaveBeforeLeave(out string error)
    {error="";if((dirty||conditionInputDirty)&&!TrySave(out error))return false;return !planDirty||TrySavePlan(out error);}
    /// <summary>
    /// Bỏ nội dung chưa lưu và đưa trạng thái theo dõi về bản đã chấp nhận.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    bool IClinicalPendingWork.DiscardBeforeLeave(out string error)=>Run(LoadWorkspace,out error);
}
/// <summary>
/// Phần xử lý kê đơn thuốc trình nhập liệu của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class KeDonThuocEditor:IClinicalPendingWork
{
    /// <summary>
    /// Giá trị Công việc tiêu đề; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    string IClinicalPendingWork.WorkTitle=>"Đơn thuốc";
    /// <summary>
    /// Giá trị Có thay đổi chưa lưu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    bool IClinicalPendingWork.HasChanges=>dirty;
    /// <summary>
    /// Thử lưu nội dung đang sửa trước khi rời màn hình; trả kết quả để nơi gọi quyết định có tiếp tục điều hướng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TrySave</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    bool IClinicalPendingWork.SaveBeforeLeave(out string error)=>TrySave(out error);
    /// <summary>
    /// Bỏ nội dung chưa lưu và đưa trạng thái theo dõi về bản đã chấp nhận.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>Fill</c>, <c>service.LayBanNhapDon</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    bool IClinicalPendingWork.DiscardBeforeLeave(out string error)=>Run(()=>Fill(saved is null?null:service.LayBanNhapDon(saved.Don.MaDT,actor)),out error);
}
/// <summary>
/// Phần xử lý bảng tính phí trình nhập liệu của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class BangTinhPhiEditor:IClinicalPendingWork
{
    /// <summary>
    /// Giá trị Công việc tiêu đề; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    string IClinicalPendingWork.WorkTitle=>"Yêu cầu thanh toán";
    /// <summary>
    /// Giá trị Có thay đổi chưa lưu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    bool IClinicalPendingWork.HasChanges=>KetQua is null&&PaymentPanel.Visibility==System.Windows.Visibility.Visible&&(GivenText.Text.Trim() is not ("" or "0")||ReferenceText.Text.Length>0||TransferChecked.IsChecked==true||MethodBox.SelectedIndex>0);
    /// <summary>
    /// Giá trị Bắt buộc ở lại màn hình; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    bool IClinicalPendingWork.MustStay=>DangXuLy||ChoDoiChieu;
    /// <summary>
    /// Thử lưu nội dung đang sửa trước khi rời màn hình; trả kết quả để nơi gọi quyết định có tiếp tục điều hướng.
    /// </summary>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    bool IClinicalPendingWork.SaveBeforeLeave(out string error){error=StatusText.Text="Tiền đang nhập chưa được gửi. Kiểm tra và xác nhận thu tại màn hình này, hoặc chọn Bỏ thay đổi để rời; không tự thu khi lưu nháp.";return false;}
    /// <summary>
    /// Bỏ nội dung chưa lưu và đưa trạng thái theo dõi về bản đã chấp nhận.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Clear</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    bool IClinicalPendingWork.DiscardBeforeLeave(out string error){error="";Clear();return true;}
}
/// <summary>
/// Phần xử lý bàn giao thuốc trình nhập liệu của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class BanGiaoThuocEditor:IClinicalPendingWork
{
    /// <summary>
    /// Giá trị Công việc tiêu đề; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    string IClinicalPendingWork.WorkTitle=>"Yêu cầu bàn giao";
    /// <summary>
    /// Giá trị Có thay đổi chưa lưu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    bool IClinicalPendingWork.HasChanges=>false;
    /// <summary>
    /// Giá trị Bắt buộc ở lại màn hình; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    bool IClinicalPendingWork.MustStay=>DangXuLy||pendingId is not null;
    /// <summary>
    /// Thử lưu nội dung đang sửa trước khi rời màn hình; trả kết quả để nơi gọi quyết định có tiếp tục điều hướng.
    /// </summary>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    bool IClinicalPendingWork.SaveBeforeLeave(out string error){error="";return false;}
    /// <summary>
    /// Bỏ nội dung chưa lưu và đưa trạng thái theo dõi về bản đã chấp nhận.
    /// </summary>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    bool IClinicalPendingWork.DiscardBeforeLeave(out string error){error="";return false;}
}
