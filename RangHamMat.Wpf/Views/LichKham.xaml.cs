using System.Windows;
using System.Windows.Controls;
namespace RangHamMat.Views;
public partial class LichKham : UserControl
{
    public LichKham() => InitializeComponent();
    private void ViewMode_Changed(object sender, RoutedEventArgs e)
    {
        if (TableView is null || Schedule is null) return;
        var calendar = (sender as RadioButton)?.Tag as string == "Schedule";
        TableView.Visibility = calendar ? Visibility.Collapsed : Visibility.Visible;
        Schedule.Visibility = calendar ? Visibility.Visible : Visibility.Collapsed;
        if (calendar) Schedule.Refresh();
    }
}
