using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RangHamMat.Controls.ClinicalWorkflow;

/// <summary>
/// Mô hình và thao tác hỗ trợ I lâm sàng chờ xử lý công việc dùng bởi các thành phần liên quan.
/// </summary>
internal interface IClinicalPendingWork
{
    /// <summary>
    /// Công việc tiêu đề.
    /// </summary>
    string WorkTitle { get; }
    /// <summary>
    /// Có thay đổi chưa lưu.
    /// </summary>
    bool HasChanges { get; }
    /// <summary>
    /// Giá trị Bắt buộc ở lại màn hình; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    bool MustStay => false;
    /// <summary>
    /// Thử lưu nội dung đang sửa trước khi rời màn hình; trả kết quả để nơi gọi quyết định có tiếp tục điều hướng.
    /// </summary>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    bool SaveBeforeLeave(out string error);
    /// <summary>
    /// Bỏ nội dung chưa lưu và đưa trạng thái theo dõi về bản đã chấp nhận.
    /// </summary>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    bool DiscardBeforeLeave(out string error);
}

/// <summary>
/// Mô hình và thao tác hỗ trợ lâm sàng chờ xử lý công việc dùng bởi các thành phần liên quan.
/// </summary>
internal static class ClinicalPendingWork
{
    /// <summary>
    /// Duyệt các phần tử con để tìm những thành phần cần được phối hợp theo dõi hoặc cập nhật.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>todo.Push</c>, <c>todo.Pop</c>, <c>seen.Add</c>, <c>LogicalTreeHelper.GetChildren</c>.</remarks>
    /// <param name="root">Giá trị phần tử gốc dùng khi thực hiện thao tác; kiểu DependencyObject.</param>
    /// <returns>Tập dữ liệu kiểu IEnumerable&lt;DependencyObject&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
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
    /// <summary>
    /// Kiểm tra công việc chưa lưu và xử lý lựa chọn lưu/bỏ/ở lại trước khi điều hướng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>roots.SelectMany</c>, <c>nodes.OfType&lt;IClinicalPendingWork&gt;</c>, <c>work.AddRange</c>, <c>nodes.OfType&lt;UserControl&gt;</c>.</remarks>
    /// <param name="roots">Giá trị các phần tử gốc dùng khi thực hiện thao tác; kiểu IEnumerable&lt;DependencyObject&gt;.</param>
    /// <param name="decide">Giá trị decide dùng khi thực hiện thao tác; kiểu Func&lt;string,MessageBoxResult&gt;?.</param>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
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
    /// <summary>
    /// Hiển thị lựa chọn xử lý công việc chưa lưu và trả quyết định của người dùng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>content.Children.Add</c>, <c>.Windows.OfType&lt;Window&gt;</c>, <c>window.Close</c>, <c>actions.Children.Add</c>.</remarks>
    /// <param name="titles">Giá trị các tiêu đề dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả kiểu MessageBoxResult dùng cho bước xử lý tiếp theo.</returns>
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
    /// <summary>
    /// Mô hình và thao tác hỗ trợ biểu mẫu công việc dùng bởi các thành phần liên quan.
    /// </summary>
    private sealed class FormWork(UserControl form):IClinicalPendingWork
    {
        /// <summary>
        /// Giá trị Công việc tiêu đề; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string WorkTitle=>form is Views.TiepNhanKham?"Tiếp nhận khám":"Thông tin người bệnh/hồ sơ";
        /// <summary>
        /// Giá trị Có thay đổi chưa lưu; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public bool HasChanges=>BieuMauHoSo.HasInputChanges(form);
        /// <summary>
        /// Thử lưu nội dung đang sửa trước khi rời màn hình; trả kết quả để nơi gọi quyết định có tiếp tục điều hướng.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>BieuMauHoSo.SaveClinicalForm</c>.</remarks>
        /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool SaveBeforeLeave(out string error)=>BieuMauHoSo.SaveClinicalForm(form,out error);
        /// <summary>
        /// Bỏ nội dung chưa lưu và đưa trạng thái theo dõi về bản đã chấp nhận.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>BieuMauHoSo.DiscardInput</c>.</remarks>
        /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool DiscardBeforeLeave(out string error){error="";BieuMauHoSo.DiscardInput(form);return true;}
    }
}
