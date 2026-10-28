using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
namespace RangHamMat.Views;

public partial class KhamLamSang
{
    private IReadOnlyList<ET_DMTINHTRANGBENH> referenceConditions=Array.Empty<ET_DMTINHTRANGBENH>();
    private ET_DMTINHTRANGBENH? recommendation;
    private void RefreshRecommendation()
    {
        if(ConditionRecommendation is null)return;
        var name=ConditionNameEditor.Text.Trim();
        if(name.Length==0)name=conditions.LastOrDefault()?.LoaiTinhTrang??"";
        recommendation=DinhHuongDieuTri.Tim(name,referenceConditions);
        ConditionRecommendation.Text=recommendation?.MoTa??"Chọn hoặc nhập đúng tên tình trạng thường gặp để xem định hướng. Tình trạng ngoài danh mục cần bác sĩ lập kế hoạch riêng.";
        UseRecommendationButton.IsEnabled=snapshot is not null&&Editable&&recommendation is not null;
    }
    private void Reference_Selected(object sender,SelectionChangedEventArgs e)
    {
        if(loading||ConditionReferencePicker.SelectedItem is not ET_DMTINHTRANGBENH r)return;
        if(conditionInputDirty){loading=true;try{ConditionReferencePicker.SelectedItem=DinhHuongDieuTri.Tim(ConditionNameEditor.Text,referenceConditions);}finally{loading=false;}ClinicalError.Text="Lưu hoặc Bỏ nhập trước khi chọn tình trạng khác.";return;}
        ConditionNameEditor.Text=r.TenTinhTrang;
    }
    private void UseRecommendation_Click(object sender,RoutedEventArgs e)=>Run(()=>
    {
        RequireEditable();RefreshRecommendation();
        if(recommendation is null)throw new LoiKhamException("TinhTrang","Chưa có định hướng cho tình trạng này.");
        if(planDirty)throw new LoiKhamException("KeHoach","Lưu kế hoạch đang nhập hoặc Tải lại trước khi dùng đề xuất khác.");
        var text=recommendation.TenTinhTrang+": "+recommendation.MoTa;
        var previous=loading;loading=true;
        try{ClearPlan();PlanNote.Text=text;steps.Add(new(){NoiDungBuoc="Khám đánh giá và lựa chọn dịch vụ phù hợp cho "+recommendation.TenTinhTrang});}
        finally{loading=previous;}
        planDirty=true;ClinicalTabs.SelectedIndex=2;
        ClinicalStatus.Text="Đã lập kế hoạch đề xuất chưa lưu. Bác sĩ chỉnh bước, chọn dịch vụ và kiểm tra giá trước khi Lưu nháp/Duyệt kế hoạch.";
    },out _);
}
