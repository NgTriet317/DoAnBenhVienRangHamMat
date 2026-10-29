using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ET;
namespace RangHamMat.Controls.ClinicalWorkflow;

internal static class ClinicalFormInput
{
    public static string Message(Exception error)
    {
        for(Exception? inner=error;inner is not null;inner=inner.InnerException)
            if(inner is System.Data.Common.DbException)return "Không đọc/lưu được dữ liệu. Kiểm tra kết nối và tải lại; nội dung đang nhập vẫn được giữ.";
        return error.Message;
    }
    public static void ClearErrors(FrameworkElement root)
    {
        foreach(var error in ClinicalPendingWork.Nodes(root).OfType<TextBlock>().Where(t=>Equals(t.Tag,"ClinicalFieldError")).ToArray())
            if(error.Parent is Panel panel)panel.Children.Remove(error);
    }
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
    public static void Commit(DataGrid grid,string field)
    {
        if(!grid.CommitEdit(DataGridEditingUnit.Cell,true)||!grid.CommitEdit(DataGridEditingUnit.Row,true)||HasError(grid))
            throw new LoiKhamException(field,"Kiểm tra các ô nhập: lần phải là số nguyên, lượng và giá đúng định dạng.");
    }
    private static bool HasError(DependencyObject node)
    {
        if(Validation.GetHasError(node))return true;
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)if(HasError(VisualTreeHelper.GetChild(node,i)))return true;
        return false;
    }
    public static DateTime? ReadDate(DatePicker picker)
    {
        if(string.IsNullOrWhiteSpace(picker.Text))return null;
        if(DateTime.TryParse(picker.Text,picker.Language.GetSpecificCulture(),DateTimeStyles.None,out var date)&&picker.SelectedDate?.Date==date.Date)return date.Date;
        throw new LoiKhamException("NgayBatDau","Ngày chưa hợp lệ. Chọn ngày trên lịch hoặc nhập dd/MM/yyyy.");
    }
}
