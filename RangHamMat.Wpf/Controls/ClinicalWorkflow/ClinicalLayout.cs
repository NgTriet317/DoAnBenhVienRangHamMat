using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RangHamMat.Controls.ClinicalWorkflow;

/// <summary>Chỉ gắn ở các view lâm sàng; giữ nguyên control/dữ liệu khi chuyển hai vùng thành tab.</summary>
public static class ClinicalLayout
{
    /// <summary>
    /// Định danh attached property bật hoặc tắt hành vi hỗ trợ của điều khiển được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    public static readonly DependencyProperty IsEnabledProperty=DependencyProperty.RegisterAttached("IsEnabled",typeof(bool),typeof(ClinicalLayout),new PropertyMetadata(false,Changed));
    /// <summary>
    /// Đọc attached property IsEnabled để biết thành phần có bật hỗ trợ bố cục lâm sàng hay không.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>node.GetValue</c>.</remarks>
    /// <param name="node">Giá trị nút dùng khi thực hiện thao tác; kiểu DependencyObject.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public static bool GetIsEnabled(DependencyObject node)=>(bool)node.GetValue(IsEnabledProperty);
    /// <summary>
    /// Gán attached property IsEnabled; callback đã đăng ký xử lý việc bật hành vi hỗ trợ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>node.SetValue</c>.</remarks>
    /// <param name="node">Giá trị nút dùng khi thực hiện thao tác; kiểu DependencyObject.</param>
    /// <param name="value">Giá trị bool được truyền vào để đọc, kiểm tra hoặc cập nhật trong thao tác này.</param>
    public static void SetIsEnabled(DependencyObject node,bool value)=>node.SetValue(IsEnabledProperty,value);
    /// <summary>
    /// Tiếp nhận giá trị attached property mới và đăng ký các bước khởi tạo hành vi khi điều khiển được tải.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Apply</c>, <c>ClinicalWindowLifecycle.Attach</c>, <c>Window.GetWindow</c>.</remarks>
    /// <param name="node">Giá trị nút dùng khi thực hiện thao tác; kiểu DependencyObject.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private static void Changed(DependencyObject node,DependencyPropertyChangedEventArgs e)
    {
        if(node is not FrameworkElement root||e.NewValue is not true)return;
        root.Loaded+=(_,_)=>{Apply(root);ClinicalWindowLifecycle.Attach(Window.GetWindow(root));};
    }
    /// <summary>
    /// Các vùng bố cục được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Grid,AdaptiveColumns> panels=new();
    /// <summary>
    /// Áp dụng dữ liệu hoặc các thiết lập hiển thị cho thành phần theo các bước trong thân hàm.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ClinicalPendingWork.Nodes</c>, <c>nodes.OfType&lt;DataGrid&gt;</c>, <c>table.SetCurrentValue</c>, <c>table.ReadLocalValue</c>.</remarks>
    /// <param name="root">Giá trị phần tử gốc dùng khi thực hiện thao tác; kiểu FrameworkElement.</param>
    private static void Apply(FrameworkElement root)
    {
        var nodes=ClinicalPendingWork.Nodes(root).ToArray();
        foreach(var table in nodes.OfType<DataGrid>())
        {
            table.SetCurrentValue(DataGrid.RowHeightProperty,double.NaN);table.MinRowHeight=Math.Max(46,table.MinRowHeight);
            table.EnableRowVirtualization=true;
            if(table.ReadLocalValue(DataGrid.EnableColumnVirtualizationProperty)==DependencyProperty.UnsetValue)table.EnableColumnVirtualization=true;
            table.SetCurrentValue(VirtualizingPanel.ScrollUnitProperty,ScrollUnit.Pixel);
            if(table.Columns.Count>1&&table.ReadLocalValue(DataGrid.FrozenColumnCountProperty)==DependencyProperty.UnsetValue)table.FrozenColumnCount=1;
            if(root.TryFindResource("ClinicalCell") is Style textStyle)
                foreach(var column in table.Columns.OfType<DataGridTextColumn>())
                    if(column.ReadLocalValue(DataGridBoundColumn.ElementStyleProperty)==DependencyProperty.UnsetValue)column.ElementStyle=textStyle;
        }
        foreach(var grid in nodes.OfType<Grid>().Where(IsTwoPane))
            if(!panels.TryGetValue(grid,out _))panels.Add(grid,new AdaptiveColumns(grid));
    }
    /// <summary>
    /// Nhận diện Grid có bố cục hai vùng đủ điều kiện được điều chỉnh theo chiều rộng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>grid.Children.OfType&lt;FrameworkElement&gt;</c>, <c>Grid.GetColumn</c>.</remarks>
    /// <param name="grid">Giá trị bảng hiển thị dùng khi thực hiện thao tác; kiểu Grid.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    private static bool IsTwoPane(Grid grid)=>grid.ColumnDefinitions.Count==3
        &&grid.ColumnDefinitions[1].Width.IsAbsolute&&grid.ColumnDefinitions[1].Width.Value<=24
        &&(grid.ColumnDefinitions[0].Width.IsAbsolute&&grid.ColumnDefinitions[0].Width.Value>=220||grid.ColumnDefinitions[2].Width.IsAbsolute&&grid.ColumnDefinitions[2].Width.Value>=220)
        &&grid.Children.OfType<FrameworkElement>().Any(c=>Grid.GetColumn(c)==0)&&grid.Children.OfType<FrameworkElement>().Any(c=>Grid.GetColumn(c)==2);
    /// <summary>
    /// Mô hình và thao tác hỗ trợ thích ứng các cột dùng bởi các thành phần liên quan.
    /// </summary>
    private sealed class AdaptiveColumns
    {
        /// <summary>
        /// Bảng hiển thị được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly Grid grid;
        /// <summary>
        /// Vùng trái vùng phải được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly FrameworkElement left,right;
        /// <summary>
        /// Bản dữ liệu gốc được giữ để đối chiếu trước khi cập nhật hoặc khôi phục nội dung đang sửa.
        /// </summary>
        private readonly UIElement[] original;
        /// <summary>
        /// Các cột được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly (GridLength Width,double Min,double Max)[] columns;
        /// <summary>
        /// Các tab được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly TabControl tabs=new(){Tag="ClinicalAdaptiveTabs"};
        /// <summary>
        /// Danh sách chi tiết được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private readonly TabItem list=new(){Header="Danh sách / chọn nguồn"},detail=new(){Header="Chi tiết / nhập liệu"};
        /// <summary>
        /// Cờ Bố cục hẹp, Đang thay đổi; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool narrow,changing;
        /// <summary>
        /// Khởi tạo thích ứng các cột.
        /// </summary>
        /// <param name="grid">Giá trị bảng hiển thị dùng khi thực hiện thao tác; kiểu Grid.</param>
        public AdaptiveColumns(Grid grid)
        {
            this.grid=grid;grid.Tag="ClinicalAdaptivePanel";original=grid.Children.Cast<UIElement>().ToArray();
            left=original.OfType<FrameworkElement>().First(c=>Grid.GetColumn(c)==0);right=original.OfType<FrameworkElement>().First(c=>Grid.GetColumn(c)==2);
            columns=grid.ColumnDefinitions.Select(c=>(c.Width,c.MinWidth,c.MaxWidth)).ToArray();
            tabs.Items.Add(list);tabs.Items.Add(detail);Grid.SetColumnSpan(tabs,3);
            left.AddHandler(Selector.SelectionChangedEvent,new SelectionChangedEventHandler((_,e)=>
            {if(narrow&&e.OriginalSource is FrameworkElement{Name:"Grid_BENH_NHAN" or "VisitList" or "QueueList"})tabs.SelectedItem=detail;}));
            left.AddHandler(Button.ClickEvent,new RoutedEventHandler((_,e)=>{if(narrow&&e.OriginalSource is FrameworkElement{Name:"AddSelectedButton"})tabs.SelectedItem=detail;}));
            grid.SizeChanged+=(_,_)=>Update();Update();
        }
        /// <summary>
        /// Cập nhật bố cục các cột theo chiều rộng thực tế và giữ các điều khiển/dữ liệu hiện có.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>grid.Children.Clear</c>, <c>grid.Children.Add</c>.</remarks>
        private void Update()
        {
            if(changing||grid.ActualWidth<=0)return;var useTabs=grid.ActualWidth<900;if(useTabs==narrow)return;
            changing=true;
            try
            {
                narrow=useTabs;
                if(narrow)
                {
                    grid.Children.Clear();list.Content=left;detail.Content=right;
                    for(var i=0;i<3;i++){var c=grid.ColumnDefinitions[i];c.MinWidth=0;c.MaxWidth=double.PositiveInfinity;c.Width=i==0?new GridLength(1,GridUnitType.Star):new GridLength(0);}
                    grid.Children.Add(tabs);
                }
                else
                {
                    list.Content=detail.Content=null;grid.Children.Clear();
                    for(var i=0;i<3;i++){var c=grid.ColumnDefinitions[i];c.Width=columns[i].Width;c.MinWidth=columns[i].Min;c.MaxWidth=columns[i].Max;}
                    foreach(var item in original)grid.Children.Add(item);
                }
            }
            finally{changing=false;}
        }
    }
}
