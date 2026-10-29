using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace RangHamMat.Controls.ClinicalWorkflow;

internal static class ClinicalWindowLifecycle
{
    private sealed class Marker { }
    private static readonly ConditionalWeakTable<Window,Marker> tracked=new();
    internal static MainWindow? OwnerMain(Window? window)
    {while(window is not null){if(window is MainWindow main)return main;window=window.Owner;}return null;}
    internal static IEnumerable<Window> Owned(Window parent)
    {foreach(var child in parent.OwnedWindows.Cast<Window>().ToArray()){yield return child;foreach(var descendant in Owned(child))yield return descendant;}}
    internal static bool IsClinical(Window window)=>tracked.TryGetValue(window,out _)
        ||window is Views.XemTruocHoaDon or Views.XemTruocDonThuoc
        ||ClinicalPendingWork.Nodes(window).Any(n=>n is IClinicalPendingWork||n is UserControl c&&(QuyenNghiepVu.LaPhanHe(c.GetType().Name)||BieuMauHoSo.IsClinicalForm(c)));
    internal static void Attach(Window? window)
    {
        if(window is null or MainWindow||tracked.TryGetValue(window,out _))return;
        tracked.Add(window,new());
        window.Closing+=(_,e)=>
        {
            var owner=OwnerMain(window);if(owner is null)return;
            var roots=new[]{window}.Concat(Owned(window).Where(IsClinical)).Cast<DependencyObject>();
            if(ClinicalPendingWork.TryLeave(roots,owner.LeaveDecision,out var error))return;
            e.Cancel=true;
            var status=ClinicalPendingWork.Nodes(window).OfType<TextBlock>().FirstOrDefault(t=>t.Name is "ErrorText" or "ClinicalError" or "FormError" or "StatusText");
            if(status is not null&&!string.IsNullOrEmpty(error))status.Text=error;
        };
    }
}
