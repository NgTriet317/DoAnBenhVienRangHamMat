using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
namespace RangHamMat.Controls;

public sealed record CreationContext(string Table, string ParentField, object ParentValue);
public sealed record RelatedRows(string Title, string Module, DataGrid Source, object[] Rows, CreationContext? Creation);

// Opened only for a real selected DataGridRow. Never a replacement for the Add form.
public sealed class RecordDetails : UserControl
{
    public RecordDetails(DataGrid source, object record, IEnumerable<RelatedRows> related)
    {
        var root = new Grid();
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var title = new TextBlock { Text = "Thông tin chi tiết", Style = (Style)FindResource("Title") };
        root.Children.Add(title);
        var body = new StackPanel { Margin = new Thickness(0, 12, 12, 0) };
        var fields = new WrapPanel();
        foreach (var column in source.Columns.OfType<DataGridTextColumn>())
        {
            var key = column.SortMemberPath;
            if (string.IsNullOrWhiteSpace(key) || key == "MatKhau") continue;
            var value = UiRecord.Get(record, key);
            var display = value is DateTime date ? date.ToString("dd/MM/yyyy HH:mm") : Convert.ToString(value);
            var field = new StackPanel { Width = 270, Margin = new Thickness(0, 0, 20, 22) };
            field.Children.Add(new TextBlock { Text = Convert.ToString(column.Header), Foreground = (Brush)FindResource("Muted"), Margin = new Thickness(0, 0, 0, 7), TextWrapping = TextWrapping.Wrap });
            field.Children.Add(new TextBlock { Text = string.IsNullOrEmpty(display) ? "Chưa có thông tin" : display, FontSize = 15, TextWrapping = TextWrapping.Wrap });
            fields.Children.Add(field);
        }
        body.Children.Add(new Border { Style = (Style)FindResource("Card"), Child = fields, Margin = new Thickness(0, 0, 0, 22) });
        foreach (var relation in related)
        {
            var heading = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
            if (relation.Creation is not null)
            {
                var add = new Button { Content = "Thêm " + relation.Title.ToLower(), Tag = "MoCuaSo:" + relation.Module, CommandParameter = relation.Creation, Margin = new Thickness(16, 0, 0, 0) };
                DockPanel.SetDock(add, Dock.Right); heading.Children.Add(add);
            }
            heading.Children.Add(new TextBlock { Text = relation.Title, Style = (Style)FindResource("SectionTitle"), VerticalAlignment = VerticalAlignment.Center });
            body.Children.Add(heading);
            var grid = new DataGrid { Tag = relation.Source.Tag, ItemsSource = relation.Rows, AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false, SelectionMode = DataGridSelectionMode.Single, SelectionUnit = DataGridSelectionUnit.FullRow, MaxHeight = 320, MinHeight = 100, Margin = new Thickness(0, 0, 0, 10), ToolTip = "Nhấp đúp vào dòng để xem chi tiết." };
            foreach (var column in relation.Source.Columns.OfType<DataGridTextColumn>())
                grid.Columns.Add(new DataGridTextColumn { Header = column.Header, SortMemberPath = column.SortMemberPath, Binding = new Binding(column.SortMemberPath), Width = column.Width });
            body.Children.Add(grid);
            if (relation.Rows.Length == 0) body.Children.Add(new TextBlock { Text = "Chưa có thông tin liên quan đến bản ghi này.", Foreground = (Brush)FindResource("Muted"), Margin = new Thickness(0, 0, 0, 20) });
        }
        var scroll = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetRow(scroll, 1); root.Children.Add(scroll);
        var footer = new WrapPanel { Margin = new Thickness(0, 18, 0, 0) };
        footer.Children.Add(new Button { Content = "Quay lại", Tag = "DongCuaSo" });
        footer.Children.Add(new Button { Content = "Về màn hình chính", Tag = "VeTrangChu" });
        footer.Children.Add(new TextBlock { Text = "Chế độ xem chi tiết", Foreground = (Brush)FindResource("Muted"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) });
        Grid.SetRow(footer, 2); root.Children.Add(footer); Content = root;
    }
}
