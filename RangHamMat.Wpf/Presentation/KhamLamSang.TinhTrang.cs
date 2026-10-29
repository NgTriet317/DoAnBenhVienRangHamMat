using System.Windows;
using System.Windows.Controls;
using ET;

namespace RangHamMat.Views;

public partial class KhamLamSang
{
    private TinhTrangKhamNhap? selectedCondition;
    private bool conditionInputDirty;
    private void ConditionInput_Changed(object sender,TextChangedEventArgs e){if(!loading){conditionInputDirty=true;RefreshRecommendation();}}
    private void ClearConditionInput()
    {
        var prior=loading;loading=true;
        try{selectedCondition=null;ConditionList.SelectedItem=null;ConditionReferencePicker.SelectedIndex=-1;ConditionNameEditor.Clear();ConditionDetailEditor.Clear();ConditionNoteEditor.Clear();conditionInputDirty=false;}
        finally{loading=prior;}
        UpdateConditionRights();RefreshRecommendation();
    }
    private void UpdateConditionRights()
    {
        if(snapshot is null)return;
        ConditionInputPanel.IsEnabled=Editable;AddConditionButton.IsEnabled=Editable;
        UpdateConditionButton.IsEnabled=DeleteConditionButton.IsEnabled=Editable&&selectedCondition is not null;
        ConditionCount.Text=$"{conditions.Count} tình trạng đã ghi nhận trong bản nháp / lần khám";
    }
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
    private void AddCondition_Click(object sender,RoutedEventArgs e)=>Run(()=>CommitConditionInput(forceNew:true),out _);
    private void UpdateCondition_Click(object sender,RoutedEventArgs e)=>Run(()=>{if(selectedCondition is null)throw new LoiKhamException("TinhTrang","Chọn tình trạng cần cập nhật.");CommitConditionInput();},out _);
    private void DeleteCondition_Click(object sender,RoutedEventArgs e)=>Run(()=>{RequireEditable();if(selectedCondition is null)throw new LoiKhamException("TinhTrang","Chọn tình trạng cần xóa.");conditions.Remove(selectedCondition);dirty=true;ClearConditionInput();},out _);
    private void ClearCondition_Click(object sender,RoutedEventArgs e){if(Editable)ClearConditionInput();}
}
