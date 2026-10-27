using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RangHamMat.Controls.ClinicalWorkflow;

/// <summary>Chỉ gắn ở các view lâm sàng; giữ nguyên control/dữ liệu khi chuyển hai vùng thành tab.</summary>
public static class ClinicalLayout
{
    public static readonly DependencyProperty IsEnabledProperty=DependencyProperty.RegisterAttached("IsEnabled",typeof(bool),typeof(ClinicalLayout),new PropertyMetadata(false,Changed));
    public static bool GetIsEnabled(DependencyObject node)=>(bool)node.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject node,bool value)=>node.SetValue(IsEnabledProperty,value);
    private static void Changed(DependencyObject node,DependencyPropertyChangedEventArgs e)
    {
        if(node is not FrameworkElement root||e.NewValue is not true)return;
        root.Loaded+=(_,_)=>{Apply(root);ClinicalWindowLifecycle.Attach(Window.GetWindow(root));};
    }
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Grid,AdaptiveColumns> panels=new();
    private static void Apply(FrameworkElement root)
    {
        var nodes=ClinicalPendingWork.Nodes(root).ToArray();
        foreach(var table in nodes.OfType<DataGrid>())
        {
            table.SetCurrentValue(DataGrid.RowHeightProperty,double.NaN);table.MinRowHeight=Math.Max(46,table.MinRowHeight);
            table.EnableRowVirtualization=table.EnableColumnVirtualization=true;
            table.SetCurrentValue(VirtualizingPanel.ScrollUnitProperty,ScrollUnit.Pixel);
            if(table.Columns.Count>1)table.FrozenColumnCount=1;
            if(root.TryFindResource("ClinicalCell") is Style textStyle)
                foreach(var column in table.Columns.OfType<DataGridTextColumn>())
                    if(column.ReadLocalValue(DataGridBoundColumn.ElementStyleProperty)==DependencyProperty.UnsetValue)column.ElementStyle=textStyle;
        }
        foreach(var grid in nodes.OfType<Grid>().Where(IsTwoPane))
            if(!panels.TryGetValue(grid,out _))panels.Add(grid,new AdaptiveColumns(grid));
    }
    private static bool IsTwoPane(Grid grid)=>grid.ColumnDefinitions.Count==3
        &&grid.ColumnDefinitions[1].Width.IsAbsolute&&grid.ColumnDefinitions[1].Width.Value<=24
        &&(grid.ColumnDefinitions[0].Width.IsAbsolute&&grid.ColumnDefinitions[0].Width.Value>=220||grid.ColumnDefinitions[2].Width.IsAbsolute&&grid.ColumnDefinitions[2].Width.Value>=220)
        &&grid.Children.OfType<FrameworkElement>().Any(c=>Grid.GetColumn(c)==0)&&grid.Children.OfType<FrameworkElement>().Any(c=>Grid.GetColumn(c)==2);
    private sealed class AdaptiveColumns
    {
        private readonly Grid grid;
        private readonly FrameworkElement left,right;
        private readonly UIElement[] original;
        private readonly (GridLength Width,double Min,double Max)[] columns;
        private readonly TabControl tabs=new(){Tag="ClinicalAdaptiveTabs"};
        private readonly TabItem list=new(){Header="Danh sách / chọn nguồn"},detail=new(){Header="Chi tiết / nhập liệu"};
        private bool narrow,changing;
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
