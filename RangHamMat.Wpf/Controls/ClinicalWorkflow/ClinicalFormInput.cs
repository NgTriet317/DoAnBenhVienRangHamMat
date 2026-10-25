using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ET;
namespace RangHamMat.Controls.ClinicalWorkflow;

internal static class ClinicalFormInput
{
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
