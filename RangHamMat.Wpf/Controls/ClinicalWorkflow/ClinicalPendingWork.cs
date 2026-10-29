using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RangHamMat.Controls.ClinicalWorkflow;

internal interface IClinicalPendingWork
{
    string WorkTitle { get; }
    bool HasChanges { get; }
    bool MustStay => false;
    bool SaveBeforeLeave(out string error);
    bool DiscardBeforeLeave(out string error);
}

internal static class ClinicalPendingWork
{
    internal static IEnumerable<DependencyObject> Nodes(DependencyObject root)
    {
        var seen=new HashSet<DependencyObject>();var todo=new Stack<DependencyObject>();todo.Push(root);
        while(todo.Count>0)
        {
            var node=todo.Pop();if(!seen.Add(node))continue;yield return node;
            foreach(var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())todo.Push(child);
            if(node is Visual)for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)todo.Push(VisualTreeHelper.GetChild(node,i));
        }
    }
    internal static bool TryLeave(IEnumerable<DependencyObject> roots,Func<string,MessageBoxResult>? decide,out string error)
    {
        error="";
        var nodes=roots.SelectMany(Nodes).Distinct().ToArray();
        var work=nodes.OfType<IClinicalPendingWork>().ToList();
        work.AddRange(nodes.OfType<UserControl>().Where(BieuMauHoSo.IsClinicalForm).Select(f=>(IClinicalPendingWork)new FormWork(f)));
        if(work.Any(w=>w.MustStay))
        {error="Yêu cầu thu tiền hoặc giao thuốc đang xử lý/chưa rõ kết quả. Giữ nguyên màn hình và thử lại đúng yêu cầu để đối chiếu trước khi rời.";return false;}
        var pending=work.Where(w=>w.HasChanges).ToArray();if(pending.Length==0)return true;
        var decision=(decide??ShowDecision)(string.Join("\n",pending.Select(w=>w.WorkTitle)));
        if(decision is not (MessageBoxResult.Yes or MessageBoxResult.No))return false;
        foreach(var item in pending)
        {
            var ok=decision==MessageBoxResult.Yes?item.SaveBeforeLeave(out error):item.DiscardBeforeLeave(out error);
            if(!ok)return false;
        }
        if(work.Any(w=>w.HasChanges)){error="Còn nội dung chưa lưu. Kiểm tra lại trước khi rời.";return false;}
        return true;
    }
    private static MessageBoxResult ShowDecision(string titles)
    {
        var result=MessageBoxResult.Cancel;
        var content=new StackPanel{Margin=new Thickness(24)};
        content.Children.Add(new TextBlock{Text="Có thay đổi chưa lưu",FontSize=20,FontWeight=FontWeights.SemiBold});
        content.Children.Add(new TextBlock{Text=titles+"\nLưu nháp trước khi rời, hoặc bỏ phần đang nhập để tải lại dữ liệu đã lưu.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,16)});
        var actions=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};content.Children.Add(actions);
        var window=new Window{Title="Dữ liệu chưa lưu",Content=content,Width=490,SizeToContent=SizeToContent.Height,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var owner=Application.Current?.Windows.OfType<Window>().FirstOrDefault(w=>w.IsActive);if(owner is not null)window.Owner=owner;
        foreach(var pair in new[]{("Lưu nháp",MessageBoxResult.Yes),("Bỏ thay đổi",MessageBoxResult.No),("Hủy rời trang",MessageBoxResult.Cancel)})
        {var button=new Button{Content=pair.Item1,MinHeight=36,Margin=new Thickness(4),Padding=new Thickness(12,6,12,6),IsCancel=pair.Item2==MessageBoxResult.Cancel};button.Click+=(_,_)=>{result=pair.Item2;window.Close();};actions.Children.Add(button);}
        window.ShowDialog();return result;
    }
    private sealed class FormWork(UserControl form):IClinicalPendingWork
    {
        public string WorkTitle=>form is Views.TiepNhanKham?"Tiếp nhận khám":"Thông tin người bệnh/hồ sơ";
        public bool HasChanges=>BieuMauHoSo.HasInputChanges(form);
        public bool SaveBeforeLeave(out string error)=>BieuMauHoSo.SaveClinicalForm(form,out error);
        public bool DiscardBeforeLeave(out string error){error="";BieuMauHoSo.DiscardInput(form);return true;}
    }
}
