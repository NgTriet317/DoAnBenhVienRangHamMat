using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Controls;

public static partial class BieuMauHoSo
{
    private sealed class InputState
    {
        public Dictionary<FrameworkElement,object?> Original=new();
        public bool InitializedAfterLoad;
    }
    private static readonly ConditionalWeakTable<UserControl,InputState> inputStates=new();
    internal static bool IsClinicalForm(UserControl form)=>inputStates.TryGetValue(form,out _);
    private static void TrackClinicalInput(UserControl form)
    {
        if(!DuLieuGiaoDien.PhamViLamSang||GetTable(form) is not ("BENH_NHAN" or "HO_SO_BENH_AN" or "PHIEU_KHAM_BENH"))return;
        if(inputStates.TryGetValue(form,out _)){AcceptInput(form);return;}
        var state=new InputState();inputStates.Add(form,state);AcceptInput(form);
        form.Loaded+=(_,_)=>{if(!state.InitializedAfterLoad){state.InitializedAfterLoad=true;AcceptInput(form);}};
    }
    private static object? InputValue(FrameworkElement field)=>field switch
    {TextBox t=>t.Text,ComboBox c=>c.SelectedItem,DatePicker d=>d.Text,CheckBox c=>c.IsChecked,_=>null};
    internal static void AcceptInput(UserControl form)
    {
        if(!inputStates.TryGetValue(form,out var state))return;
        state.Original=ClinicalPendingWork.Nodes(form).OfType<FrameworkElement>()
            .Where(e=>e is TextBox{IsReadOnly:false} or ComboBox or DatePicker or CheckBox).Distinct().ToDictionary(e=>e,InputValue);
    }
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
    internal static bool HasInputChanges(UserControl form)
    {
        if(form is Views.TiepNhanKham{SavedResult:not null})return false;
        return inputStates.TryGetValue(form,out var state)&&state.Original.Any(p=>!Equals(p.Value,InputValue(p.Key)));
    }
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
