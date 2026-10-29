using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Controls;

/// <summary>
/// Mô hình và thao tác hỗ trợ biểu mẫu hồ sơ dùng bởi các thành phần liên quan.
/// </summary>
public static partial class BieuMauHoSo
{
    /// <summary>
    /// Mô hình và thao tác hỗ trợ trạng thái đầu vào dùng bởi các thành phần liên quan.
    /// </summary>
    private sealed class InputState
    {
        /// <summary>
        /// Bản gốc được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        public Dictionary<FrameworkElement,object?> Original=new();
        /// <summary>
        /// Cờ Đã khởi tạo sau nạp; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        public bool InitializedAfterLoad;
    }
    /// <summary>
    /// Đầu vào các trạng thái được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static readonly ConditionalWeakTable<UserControl,InputState> inputStates=new();
    /// <summary>
    /// Nhận diện biểu mẫu thuộc phạm vi lâm sàng cần được theo dõi thay đổi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>inputStates.TryGetValue</c>.</remarks>
    /// <param name="form">Giá trị biểu mẫu dùng khi thực hiện thao tác; kiểu UserControl.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    internal static bool IsClinicalForm(UserControl form)=>inputStates.TryGetValue(form,out _);
    /// <summary>
    /// Đăng ký theo dõi các trường nhập để nhận biết nội dung chưa lưu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>GetTable</c>, <c>inputStates.TryGetValue</c>, <c>AcceptInput</c>, <c>inputStates.Add</c>.</remarks>
    /// <param name="form">Giá trị biểu mẫu dùng khi thực hiện thao tác; kiểu UserControl.</param>
    private static void TrackClinicalInput(UserControl form)
    {
        if(!DuLieuGiaoDien.PhamViLamSang||GetTable(form) is not ("BENH_NHAN" or "HO_SO_BENH_AN" or "PHIEU_KHAM_BENH"))return;
        if(inputStates.TryGetValue(form,out _)){AcceptInput(form);return;}
        var state=new InputState();inputStates.Add(form,state);AcceptInput(form);
        form.Loaded+=(_,_)=>{if(!state.InitializedAfterLoad){state.InitializedAfterLoad=true;AcceptInput(form);}};
    }
    /// <summary>
    /// Đọc giá trị của trường nhập dùng khi so sánh thay đổi.
    /// </summary>
    /// <param name="field">Giá trị trường dùng khi thực hiện thao tác; kiểu FrameworkElement.</param>
    /// <returns>Đối tượng kiểu object được tìm hoặc tính toán; có thể null theo nhánh xử lý trong hàm.</returns>
    private static object? InputValue(FrameworkElement field)=>field switch
    {TextBox t=>t.Text,ComboBox c=>c.SelectedItem,DatePicker d=>d.Text,CheckBox c=>c.IsChecked,_=>null};
    /// <summary>
    /// Ghi nhận nội dung hiện tại làm mốc so sánh cho những lần sửa tiếp theo.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>inputStates.TryGetValue</c>, <c>ClinicalPendingWork.Nodes</c>.</remarks>
    /// <param name="form">Giá trị biểu mẫu dùng khi thực hiện thao tác; kiểu UserControl.</param>
    internal static void AcceptInput(UserControl form)
    {
        if(!inputStates.TryGetValue(form,out var state))return;
        state.Original=ClinicalPendingWork.Nodes(form).OfType<FrameworkElement>()
            .Where(e=>e is TextBox{IsReadOnly:false} or ComboBox or DatePicker or CheckBox).Distinct().ToDictionary(e=>e,InputValue);
    }
    /// <summary>
    /// Cập nhật mốc theo dõi sau khi dữ liệu đã được lưu thành công.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>IsClinicalForm</c>, <c>states.TryGetValue</c>, <c>GetSaved</c>, <c>UiRecord.Text</c>.</remarks>
    /// <param name="form">Giá trị biểu mẫu dùng khi thực hiện thao tác; kiểu UserControl.</param>
    internal static void RebaseCommittedInput(UserControl form)
    {
        if(IsClinicalForm(form)&&states.TryGetValue(form,out var state)&&GetSaved(form) is object saved)
        {
            var id=UiRecord.Text(saved,DuLieuGiaoDien.Key(state.Table));
            DuLieuGiaoDien.GiuBanGhiDaCommit(state.Table,saved);
            var originals=state.Originals.Where(o=>o.Table!=state.Table).Append(new OriginalRow(state.Table,id,DuLieuGiaoDien.Values(saved))).ToArray();
            states.Remove(form);states.Add(form,state with{Record=saved,Originals=originals});
            foreach(var field in ClinicalPendingWork.Nodes(form).OfType<TextBox>().Where(t=>Equals(t.Tag,"DB:"+state.Table+"."+DuLieuGiaoDien.Key(state.Table))))field.Text=id;
        }
        AcceptInput(form);
    }
    /// <summary>
    /// So sánh nội dung nhập với mốc đã chấp nhận để nhận diện thay đổi chưa lưu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>inputStates.TryGetValue</c>, <c>state.Original.Any</c>, <c>Equals</c>, <c>InputValue</c>.</remarks>
    /// <param name="form">Giá trị biểu mẫu dùng khi thực hiện thao tác; kiểu UserControl.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    internal static bool HasInputChanges(UserControl form)
    {
        if(form is Views.TiepNhanKham{SavedResult:not null})return false;
        return inputStates.TryGetValue(form,out var state)&&state.Original.Any(p=>!Equals(p.Value,InputValue(p.Key)));
    }
    /// <summary>
    /// Khôi phục hoặc bỏ các thay đổi nhập đang được theo dõi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>inputStates.TryGetValue</c>, <c>AcceptInput</c>.</remarks>
    /// <param name="form">Giá trị biểu mẫu dùng khi thực hiện thao tác; kiểu UserControl.</param>
    internal static void DiscardInput(UserControl form)
    {
        if(!inputStates.TryGetValue(form,out var state))return;
        foreach(var pair in state.Original)
        {
            switch(pair.Key)
            {case TextBox t:t.Text=pair.Value as string??"";break;case ComboBox c:c.SelectedItem=pair.Value;break;case DatePicker d:d.Text=pair.Value as string??"";break;case CheckBox c:c.IsChecked=pair.Value as bool?;break;}
        }
        AcceptInput(form);
    }
    /// <summary>
    /// Kiểm tra và lưu nội dung của biểu mẫu lâm sàng hiện tại.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>reception.TryReceive</c>, <c>DuLieuGiaoDien.Transaction</c>, <c>TrySave</c>, <c>RebaseCommittedInput</c>.</remarks>
    /// <param name="form">Giá trị biểu mẫu dùng khi thực hiện thao tác; kiểu UserControl.</param>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    internal static bool SaveClinicalForm(UserControl form,out string error)
    {
        if(form is Views.TiepNhanKham reception)return reception.TryReceive(out error);
        var message="";
        try
        {
            var ok=DuLieuGiaoDien.Transaction(()=>TrySave(form,out _,out message));
            error=message;if(ok)RebaseCommittedInput(form);return ok;
        }
        catch(InvalidOperationException ex){error=ex.Message;KiemTraNhapLieu.ShowError(form,error);return false;}
    }
}
