using System.Windows;
using System.Windows.Controls;
using ET;

namespace RangHamMat.Views;

/// <summary>
/// Phần xử lý khám lâm sàng của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class KhamLamSang
{
    /// <summary>
    /// Đang chọn tình trạng được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private TinhTrangKhamNhap? selectedCondition;
    /// <summary>
    /// Cờ Tình trạng đầu vào có thay đổi chưa lưu; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool conditionInputDirty;
    /// <summary>
    /// Tiếp nhận thay đổi tình trạng đầu vào và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>RefreshRecommendation</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ConditionInput_Changed(object sender,TextChangedEventArgs e){if(!loading){conditionInputDirty=true;RefreshRecommendation();}}
    /// <summary>
    /// Xóa vùng nhập tình trạng và đặt lại lựa chọn chỉnh sửa.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ConditionNameEditor.Clear</c>, <c>ConditionDetailEditor.Clear</c>, <c>ConditionNoteEditor.Clear</c>, <c>UpdateConditionRights</c>.</remarks>
    private void ClearConditionInput()
    {
        var prior=loading;loading=true;
        try{selectedCondition=null;ConditionList.SelectedItem=null;ConditionReferencePicker.SelectedIndex=-1;ConditionNameEditor.Clear();ConditionDetailEditor.Clear();ConditionNoteEditor.Clear();conditionInputDirty=false;}
        finally{loading=prior;}
        UpdateConditionRights();RefreshRecommendation();
    }
    /// <summary>
    /// Cập nhật quyền thêm/sửa/xóa tình trạng theo trạng thái lần khám.
    /// </summary>
    private void UpdateConditionRights()
    {
        if(snapshot is null)return;
        ConditionInputPanel.IsEnabled=Editable;AddConditionButton.IsEnabled=Editable;
        UpdateConditionButton.IsEnabled=DeleteConditionButton.IsEnabled=Editable&&selectedCondition is not null;
        ConditionCount.Text=$"{conditions.Count} tình trạng đã ghi nhận trong bản nháp / lần khám";
    }
    /// <summary>
    /// Kiểm tra và ghi nhận nội dung nhập tình trạng bệnh vào danh sách đang soạn.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>RequireEditable</c>, <c>ConditionNameEditor.Text.Trim</c>, <c>ConditionDetailEditor.Text.Trim</c>, <c>ConditionNoteEditor.Text.Trim</c>.</remarks>
    /// <param name="forceNew">Giá trị ép áp dụng mới dùng khi thực hiện thao tác; kiểu bool.</param>
    private void CommitConditionInput(bool forceNew=false)
    {
        RequireEditable();
        if(!conditionInputDirty&&!forceNew)return;
        var name=ConditionNameEditor.Text.Trim();
        if(name.Length==0)throw new LoiKhamException("TinhTrang","Nhập tình trạng bệnh trước khi thêm hoặc lưu.");
        var row=forceNew?new TinhTrangKhamNhap():selectedCondition??new TinhTrangKhamNhap();
        row.LoaiTinhTrang=name;row.MoTaTinhTrang=string.IsNullOrWhiteSpace(ConditionDetailEditor.Text)?name:ConditionDetailEditor.Text.Trim();
        row.GhiChu=string.IsNullOrWhiteSpace(ConditionNoteEditor.Text)?null:ConditionNoteEditor.Text.Trim();
        if(!conditions.Contains(row))conditions.Add(row);
        dirty=true;conditionInputDirty=false;ConditionList.Items.Refresh();ClearConditionInput();
        ClinicalStatus.Text="Đã ghi vào bản nháp. Bấm Lưu nháp để lưu database.";
    }
    /// <summary>
    /// Phản ánh lựa chọn tình trạng vào dữ liệu và phần hiển thị liên quan.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>UpdateConditionRights</c>, <c>RefreshRecommendation</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Condition_Selected(object sender,SelectionChangedEventArgs e)
    {
        if(loading)return;
        if(conditionInputDirty)
        {
            var prior=loading;loading=true;ConditionList.SelectedItem=selectedCondition;loading=prior;
            ClinicalError.Text="Còn tình trạng đang nhập. Thêm/Cập nhật, Lưu nháp hoặc Bỏ nhập trước khi chọn dòng khác.";return;
        }
        selectedCondition=ConditionList.SelectedItem as TinhTrangKhamNhap;
        var original=loading;loading=true;
        try{ConditionNameEditor.Text=selectedCondition?.LoaiTinhTrang??"";ConditionDetailEditor.Text=selectedCondition?.MoTaTinhTrang??"";ConditionNoteEditor.Text=selectedCondition?.GhiChu??"";conditionInputDirty=false;}
        finally{loading=original;}
        UpdateConditionRights();RefreshRecommendation();
    }
    /// <summary>
    /// Xử lý thao tác thêm tình trạng khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>CommitConditionInput</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void AddCondition_Click(object sender,RoutedEventArgs e)=>Run(()=>CommitConditionInput(forceNew:true),out _);
    /// <summary>
    /// Xử lý thao tác cập nhật tình trạng khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>CommitConditionInput</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void UpdateCondition_Click(object sender,RoutedEventArgs e)=>Run(()=>{if(selectedCondition is null)throw new LoiKhamException("TinhTrang","Chọn tình trạng cần cập nhật.");CommitConditionInput();},out _);
    /// <summary>
    /// Xử lý thao tác xóa tình trạng khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>RequireEditable</c>, <c>conditions.Remove</c>, <c>ClearConditionInput</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void DeleteCondition_Click(object sender,RoutedEventArgs e)=>Run(()=>{RequireEditable();if(selectedCondition is null)throw new LoiKhamException("TinhTrang","Chọn tình trạng cần xóa.");conditions.Remove(selectedCondition);dirty=true;ClearConditionInput();},out _);
    /// <summary>
    /// Xử lý thao tác xóa nội dung tình trạng khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ClearConditionInput</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void ClearCondition_Click(object sender,RoutedEventArgs e){if(Editable)ClearConditionInput();}
}
