using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using RangHamMat.ClinicalChecks;
using RangHamMat.Controls;
using RangHamMat.Views;

namespace RangHamMat.ClinicalUiChecks;

internal static class KeyboardDeploymentChecks
{
    internal static int Run()
    {
        using var f=new Fixture();
        var editor=new KhamLamSang(f.Id("LSA"),f.User("A"),f.Transaction);
        var host=new Window{Content=editor,Width=1024,Height=768,ShowInTaskbar=false};host.Show();host.Activate();
        try
        {
            host.Dispatcher.Invoke(()=>host.UpdateLayout(),DispatcherPriority.ApplicationIdle);
            var symptoms=(TextBox)editor.FindName("SymptomsEditor");symptoms.Focus();symptoms.Text="FIXTURE nhập bằng bàn phím";
            var original=Keyboard.FocusedElement;
            if(!symptoms.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next))||ReferenceEquals(original,Keyboard.FocusedElement))throw new Exception("Tab không chuyển focus.");
            if(Keyboard.FocusedElement is not UIElement next||!next.IsKeyboardFocusWithin||!next.IsVisible)throw new Exception("Focus không đến ô hiển thị.");
            if(!next.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous))||symptoms.Text!="FIXTURE nhập bằng bàn phím")throw new Exception("Shift-Tab làm mất nhập liệu.");
            Console.WriteLine("PASS: P12 Tab/Shift-Tab thực trên form khám1024x768, focus tới trường hiển thị và giữ nội dung; fixture rollback.");return 0;
        }
        finally{host.Content=null;host.Close();}
    }
}
