using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;

public partial class KhamLamSang:IClinicalPendingWork
{
    string IClinicalPendingWork.WorkTitle=>"Nội dung khám/kế hoạch điều trị";
    bool IClinicalPendingWork.HasChanges=>dirty||planDirty||conditionInputDirty;
    bool IClinicalPendingWork.SaveBeforeLeave(out string error)
    {error="";if((dirty||conditionInputDirty)&&!TrySave(out error))return false;return !planDirty||TrySavePlan(out error);}
    bool IClinicalPendingWork.DiscardBeforeLeave(out string error)=>Run(LoadWorkspace,out error);
}
public partial class KeDonThuocEditor:IClinicalPendingWork
{
    string IClinicalPendingWork.WorkTitle=>"Đơn thuốc";
    bool IClinicalPendingWork.HasChanges=>dirty;
    bool IClinicalPendingWork.SaveBeforeLeave(out string error)=>TrySave(out error);
    bool IClinicalPendingWork.DiscardBeforeLeave(out string error)=>Run(()=>Fill(saved is null?null:service.LayBanNhapDon(saved.Don.MaDT,actor)),out error);
}
public partial class BangTinhPhiEditor:IClinicalPendingWork
{
    string IClinicalPendingWork.WorkTitle=>"Yêu cầu thanh toán";
    bool IClinicalPendingWork.HasChanges=>KetQua is null&&PaymentPanel.Visibility==System.Windows.Visibility.Visible&&(GivenText.Text.Trim() is not ("" or "0")||ReferenceText.Text.Length>0||TransferChecked.IsChecked==true||MethodBox.SelectedIndex>0);
    bool IClinicalPendingWork.MustStay=>DangXuLy||ChoDoiChieu;
    bool IClinicalPendingWork.SaveBeforeLeave(out string error){error=StatusText.Text="Tiền đang nhập chưa được gửi. Kiểm tra và xác nhận thu tại màn hình này, hoặc chọn Bỏ thay đổi để rời; không tự thu khi lưu nháp.";return false;}
    bool IClinicalPendingWork.DiscardBeforeLeave(out string error){error="";Clear();return true;}
}
public partial class BanGiaoThuocEditor:IClinicalPendingWork
{
    string IClinicalPendingWork.WorkTitle=>"Yêu cầu bàn giao";
    bool IClinicalPendingWork.HasChanges=>false;
    bool IClinicalPendingWork.MustStay=>DangXuLy||pendingId is not null;
    bool IClinicalPendingWork.SaveBeforeLeave(out string error){error="";return false;}
    bool IClinicalPendingWork.DiscardBeforeLeave(out string error){error="";return false;}
}
