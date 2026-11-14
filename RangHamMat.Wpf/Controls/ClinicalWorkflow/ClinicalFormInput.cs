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
        var key=error switch{LoiNhapLieu e=>e.Field,LoiKhamException e=>e.Truong,LoiTiepNhanException e=>e.Truong,_=>""};
        if(error.Data["ClinicalRow"] is object errorRow)
        {
            var rowGrid=root.FindName(errorRow is DongThuocKeDon?"RxLines":"PlanSteps") as DataGrid;
            if(rowGrid is not null){rowGrid.SelectedItem=errorRow;rowGrid.ScrollIntoView(errorRow);rowGrid.UpdateLayout();}
        }
        var names=key switch
        {
            "SoLuong"=>new[]{"QuantityEditor","RxLines"},"SoNgayDung"=>["DaysEditor","RxLines"],"DonGia"=>["PriceEditor"],"ThoiGianThucHien"=>["TimeEditor"],
            "TrieuChung"=>["SymptomsEditor"],"ChanDoan"=>["DiagnosisEditor"],"KeHoachXuLy"=>["TreatmentEditor"],
            "MaNVThucHien"=>["PerformerChoice"],"MaDV"=>["ServiceChoice","ServicePicker","PlanSteps"],"MaThuoc"=>["RxLines","DrugChoice","DrugGrid"],
            "MaBS"=>["DoctorPicker"],"MaPhong"=>["RoomPicker"],"LyDoKham"=>["ArrivalReason"],"LyDoDieuPhoiNgoaiLich"=>["DispatchReason"],
            "NgayBatDau"=>["PlanStart"],"NgayDuKienKetThuc"=>["PlanEnd"],"TenMau"=>["TemplateName"],"PhienBan"=>["VersionEditor"],
            "TenNhom"=>["GroupName"],"TinhTrang"=>["ConditionNameEditor","ConditionList"],
            "LieuDung"=>["DoseEditor","RxLines"],"DuongDung"=>["RouteEditor","RxLines"],"HuongDan"=>["InstructionEditor","RxLines"],
            "LyDoKhongKeThuoc"=>["NoPrescriptionReason"],"LyDoThayThe"=>["ReplacementReason"],
            "LoaiTinhTrang"=>["ConditionNameEditor"],"MoTaTinhTrang"=>["ConditionDetailEditor"],
            "CacDong"=>["RxLines"],"CacBuoc" or "SoLuongDuKien" or "LanThu" or "NoiDungBuoc"=>["PlanSteps"],
            "ThucHien"=>["ExecutionAcknowledged"],
            _=>Array.Empty<string>()
        };
        var target=names.Select(n=>root.FindName(n)).OfType<FrameworkElement>().FirstOrDefault(e=>e.IsEnabled)
            ??ClinicalPendingWork.Nodes(root).OfType<FrameworkElement>().FirstOrDefault(e=>e.IsEnabled&&e.Tag is string tag&&tag.StartsWith("DB:")&&tag.EndsWith("."+key));
        if(target is null)return;
        // Các tab chưa mở vẫn phải dẫn người dùng tới đúng trường đang lỗi.
        for(DependencyObject? node=target;node is not null;node=LogicalTreeHelper.GetParent(node))
        {
            if(node is TabItem tab)tab.IsSelected=true;
            if(node is Expander expander)expander.IsExpanded=true;
        }
        if(target is DataGrid grid)
        {
            var row=grid.SelectedItem??grid.Items.Cast<object>().FirstOrDefault(x=>x!=System.Windows.Data.CollectionView.NewItemPlaceholder);
            if(row is not null)
            {
                grid.SelectedItem=row;var column=grid.Columns.FirstOrDefault(c=>c.SortMemberPath==key)??grid.Columns.OfType<DataGridBoundColumn>().FirstOrDefault(c=>c.Binding is System.Windows.Data.Binding b&&b.Path?.Path==key)??grid.Columns.FirstOrDefault();
                if(column is not null){grid.CurrentCell=new DataGridCellInfo(row,column);grid.ScrollIntoView(row,column);}grid.UpdateLayout();
                if(column is {IsReadOnly:false,Visibility:Visibility.Visible}&&!grid.IsReadOnly){grid.BeginEdit();grid.UpdateLayout();}
                target=ClinicalPendingWork.Nodes(grid).OfType<FrameworkElement>().FirstOrDefault(field=>
                    field.IsVisible&&field.IsEnabled&&ReferenceEquals(field.DataContext,row)&&field.Tag is string tag&&tag.EndsWith("."+key)&&field is TextBox or ComboBox)??target;
            }
        }
        KiemTraNhapLieu.SetError(target,Message(error));
        target.BringIntoView();target.Focus();
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
        try{return KiemTraNhapLieu.ReadDate(picker);}
        catch(LoiNhapLieu error)
        {
            var field=picker.Name=="PlanEnd"?"NgayDuKienKetThuc":picker.Tag is string tag&&tag.StartsWith("DB:")?tag[(tag.LastIndexOf('.')+1)..]:"NgayBatDau";
            throw new LoiKhamException(field,error.Message);
        }
    }

    /// <summary>Đọc lượng cấp phát theo decimal(18,3), không nhận dấu phân cách hàng nghìn.</summary>
    public static decimal ReadQuantity(string text)
    {
        KiemTraNhapLieu.ValidateValues("CHI_TIET_DON_THUOC",new Dictionary<string,object?> { ["SoLuong"]=text });
        return decimal.Parse(text.Trim().Replace(',','.'),NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture);
    }

    /// <summary>Kiểm tra từng dòng, kể cả dòng chưa được chọn; giữ lại dòng và trường để mở đúng ô sửa.</summary>
    public static void ValidatePrescriptionLines(IEnumerable<DongThuocKeDon> source)
    {
        var rows=source.ToArray();
        if(rows.Length==0)throw new LoiKhamException("CacDong","Thêm ít nhất một thuốc vào đơn, rồi nhập cách dùng cho từng thuốc.");
        for(var index=0;index<rows.Length;index++)
        {
            var row=rows[index];
            try
            {
                KiemTraNhapLieu.ValidateValues("CHI_TIET_DON_THUOC",new Dictionary<string,object?>
                { ["MaThuoc"]=row.MaThuoc,["SoLuong"]=row.SoLuong,["SoNgayDung"]=row.SoNgayDung,["LieuDung"]=row.LieuDung,["DuongDung"]=row.DuongDung,["HuongDan"]=row.HuongDan });
                if(string.IsNullOrWhiteSpace(row.DuongDung))throw new LoiNhapLieu("DuongDung","Nhập đường dùng thuốc theo chỉ định của bác sĩ.");
            }
            catch(LoiNhapLieu error)
            {
                var failure=new LoiKhamException(error.Field,$"Dòng thuốc {index+1} ({row.TenHienThi}): {error.Message}");
                failure.Data["ClinicalRow"]=row;throw failure;
            }
        }
    }

    /// <summary>Kiểm tra nhóm ô nhập bằng cơ chế chung trước khi đọc dữ liệu.</summary>
    public static void Prepare(DependencyObject root)
    {
        if(!KiemTraNhapLieu.PrepareSave(root,out var error))throw new LoiKhamException("",error);
    }
}

/// <summary>Giữ dấu chấm/phẩy là dấu thập phân khi sửa lượng trong bảng kế hoạch.</summary>
public sealed class ClinicalDecimalConverter : System.Windows.Data.IValueConverter
{
    /// <summary>Hiển thị lượng hiện có theo ngôn ngữ biểu mẫu.</summary>
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is decimal number?number.ToString("0.###",culture):value;
    /// <summary>Chặn cách nhập có phân cách hàng nghìn và quá precision/scale trước khi cập nhật model.</summary>
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)
    {
        var text=System.Convert.ToString(value,culture)??"";
        try
        {
            KiemTraNhapLieu.ValidateValues("CHI_TIET_KE_HOACH",new Dictionary<string,object?>{["SoLuongDuKien"]=text});
            return decimal.Parse(text.Trim().Replace(',','.'),NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture);
        }
        catch(LoiNhapLieu error){throw new FormatException(error.Message);}
    }
}
