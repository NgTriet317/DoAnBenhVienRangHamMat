using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ET;
namespace RangHamMat.Controls.ClinicalWorkflow;

/// <summary>
/// Mô hình và thao tác hỗ trợ lâm sàng biểu mẫu đầu vào dùng bởi các thành phần liên quan.
/// </summary>
internal static class ClinicalFormInput
{
    /// <summary>
    /// Chuyển ngoại lệ hoặc thông báo thành nội dung phản hồi phù hợp cho trường nhập.
    /// </summary>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu Exception.</param>
    /// <returns>Chuỗi kết quả của thao tác thông báo.</returns>
    public static string Message(Exception error)
    {
        for(Exception? inner=error;inner is not null;inner=inner.InnerException)
            if(inner is System.Data.Common.DbException)return "Không đọc/lưu được dữ liệu. Kiểm tra kết nối và tải lại; nội dung đang nhập vẫn được giữ.";
        return error.Message;
    }
    /// <summary>
    /// Xóa đánh dấu lỗi và thông báo của các trường đã kiểm tra.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ClinicalPendingWork.Nodes</c>, <c>Equals</c>, <c>panel.Children.Remove</c>.</remarks>
    /// <param name="root">Giá trị phần tử gốc dùng khi thực hiện thao tác; kiểu FrameworkElement.</param>
    public static void ClearErrors(FrameworkElement root)
    {
        foreach(var error in ClinicalPendingWork.Nodes(root).OfType<TextBlock>().Where(t=>Equals(t.Tag,"ClinicalFieldError")).ToArray())
            if(error.Parent is Panel panel)panel.Children.Remove(error);
    }
    /// <summary>
    /// Đưa thông báo lỗi đến đúng trường hoặc vùng phản hồi của biểu mẫu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Array.Empty&lt;string&gt;</c>, <c>names.Select</c>, <c>root.FindName</c>, <c>grid.Items.Cast&lt;object&gt;</c>.</remarks>
    /// <param name="root">Giá trị phần tử gốc dùng khi thực hiện thao tác; kiểu FrameworkElement.</param>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu Exception.</param>
    public static void ShowError(FrameworkElement root,Exception error)
    {
        var key=error switch{LoiKhamException e=>e.Truong,LoiTiepNhanException e=>e.Truong,_=>""};
        var names=key switch
        {
            "SoLuong"=>new[]{"QuantityEditor","RxLines"},"DonGia"=>["PriceEditor"],"ThoiGianThucHien"=>["TimeEditor"],
            "TrieuChung"=>["SymptomsEditor"],"ChanDoan"=>["DiagnosisEditor"],"KeHoachXuLy"=>["TreatmentEditor"],
            "MaNVThucHien"=>["PerformerChoice"],"MaDV"=>["ServiceChoice","ServicePicker"],"MaThuoc"=>["RxLines","DrugChoice","DrugGrid"],
            "MaBS"=>["DoctorPicker"],"MaPhong"=>["RoomPicker"],"LyDoKham"=>["ArrivalReason"],"LyDoDieuPhoiNgoaiLich"=>["DispatchReason"],
            "NgayBatDau"=>["PlanStart"],"NgayDuKienKetThuc"=>["PlanEnd"],"TenMau"=>["TemplateName"],"PhienBan"=>["VersionEditor"],
            "TenNhom"=>["GroupName"],"TinhTrang"=>["ConditionNameEditor","ConditionList"],
            "CacDong" or "LieuDung" or "DuongDung" or "HuongDan"=>["RxLines","DoseEditor","InstructionEditor"],
            _=>Array.Empty<string>()
        };
        var target=names.Select(n=>root.FindName(n)).OfType<FrameworkElement>().FirstOrDefault(e=>e.IsEnabled);
        if(target is null)return;
        if(target is DataGrid grid)
        {
            var row=grid.SelectedItem??grid.Items.Cast<object>().FirstOrDefault(x=>x!=System.Windows.Data.CollectionView.NewItemPlaceholder);
            if(row is not null)
            {
                grid.SelectedItem=row;var column=grid.Columns.OfType<DataGridBoundColumn>().FirstOrDefault(c=>c.Binding is System.Windows.Data.Binding b&&b.Path?.Path==key)??grid.Columns.FirstOrDefault();
                if(column is not null){grid.CurrentCell=new DataGridCellInfo(row,column);grid.ScrollIntoView(row,column);}grid.UpdateLayout();
            }
        }
        target.BringIntoView();target.Focus();
        if(target.Parent is StackPanel parent)
        {
            var message=new TextBlock{Tag="ClinicalFieldError",Text=Message(error),TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Firebrick,FontSize=13,Margin=new Thickness(0,4,0,0)};
            parent.Children.Insert(parent.Children.IndexOf(target)+1,message);
        }
    }
    /// <summary>
    /// Kiểm tra và ghi nhận dữ liệu hiện tại theo thao tác của thành phần.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>grid.CommitEdit</c>, <c>HasError</c>.</remarks>
    /// <param name="grid">Giá trị bảng hiển thị dùng khi thực hiện thao tác; kiểu DataGrid.</param>
    /// <param name="field">Giá trị trường dùng khi thực hiện thao tác; kiểu string.</param>
    public static void Commit(DataGrid grid,string field)
    {
        if(!grid.CommitEdit(DataGridEditingUnit.Cell,true)||!grid.CommitEdit(DataGridEditingUnit.Row,true)||HasError(grid))
            throw new LoiKhamException(field,"Kiểm tra các ô nhập: lần phải là số nguyên, lượng và giá đúng định dạng.");
    }
    /// <summary>
    /// Kiểm tra trường hoặc biểu mẫu hiện có lỗi được đánh dấu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Validation.GetHasError</c>, <c>VisualTreeHelper.GetChildrenCount</c>, <c>HasError</c>, <c>VisualTreeHelper.GetChild</c>.</remarks>
    /// <param name="node">Giá trị nút dùng khi thực hiện thao tác; kiểu DependencyObject.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    private static bool HasError(DependencyObject node)
    {
        if(Validation.GetHasError(node))return true;
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)if(HasError(VisualTreeHelper.GetChild(node,i)))return true;
        return false;
    }
    /// <summary>
    /// Đọc giá trị ngày từ trường nhập và xử lý trường hợp thiếu hoặc không hợp lệ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DateTime.TryParse</c>, <c>picker.Language.GetSpecificCulture</c>.</remarks>
    /// <param name="picker">Giá trị ô chọn dùng khi thực hiện thao tác; kiểu DatePicker.</param>
    /// <returns>Đối tượng kiểu DateTime được tìm hoặc tính toán; có thể null theo nhánh xử lý trong hàm.</returns>
    public static DateTime? ReadDate(DatePicker picker)
    {
        if(string.IsNullOrWhiteSpace(picker.Text))return null;
        if(DateTime.TryParse(picker.Text,picker.Language.GetSpecificCulture(),DateTimeStyles.None,out var date)&&picker.SelectedDate?.Date==date.Date)return date.Date;
        throw new LoiKhamException("NgayBatDau","Ngày chưa hợp lệ. Chọn ngày trên lịch hoặc nhập dd/MM/yyyy.");
    }
}
