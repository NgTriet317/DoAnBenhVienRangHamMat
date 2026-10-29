using System.ComponentModel;
using System.Windows;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Views;

public partial class KeDonThuocWindow:Window
{
    public KeDonThuocEditor Editor{get;}
    public bool DaXacNhan{get;private set;}
    public Func<string,MessageBoxResult>? LeaveDecision{get;set;}

    public KeDonThuocWindow(KeDonThuocEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        InitializeComponent();
        Width=Math.Min(Width,SystemParameters.WorkArea.Width);
        Height=Math.Min(Height,SystemParameters.WorkArea.Height);
        Editor=editor;
        Editor.UseDraftDialog();
        EditorHost.Content=editor;
    }

    public bool TryConfirm(out string error)
    {
        var success=Editor.TryConfirmDraft(out error);
        WindowError.Text=error;
        DaXacNhan=success;
        return success;
    }

    public void ReleaseEditor()=>EditorHost.Content=null;

    protected override void OnClosing(CancelEventArgs e)
    {
        var decide=LeaveDecision??ClinicalWindowLifecycle.OwnerMain(this)?.LeaveDecision;
        if(!ClinicalPendingWork.TryLeave(new DependencyObject[]{this},decide,out var error))
        {
            e.Cancel=true;
            WindowError.Text=error;
            // A lifecycle Closing handler may also be attached. Do not raise it
            // when this guard has already kept the window open.
            return;
        }
        base.OnClosing(e);
    }

    private void Confirm_Click(object sender,RoutedEventArgs e)
    {
        if(TryConfirm(out _))Close();
    }
    private void Close_Click(object sender,RoutedEventArgs e)=>Close();
}
