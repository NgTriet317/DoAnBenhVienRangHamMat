using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
public partial class BaoCao : UserControl
{
    public BaoCao() => InitializeComponent();
    private void ViewMode_Changed(object sender, RoutedEventArgs e)
    {
        if (TableView is null || ChartsView is null) return;
        var charts = (sender as RadioButton)?.Tag as string == "Charts";
        TableView.Visibility = charts ? Visibility.Collapsed : Visibility.Visible;
        ChartsView.Visibility = charts ? Visibility.Visible : Visibility.Collapsed;
    }
    // Presentation inputs only: the future data layer supplies approved report totals.
    public void SetChartData(IEnumerable<ChartPoint> hospital, IEnumerable<ChartPoint> warehouse)
    {
        HospitalChart.Points = hospital.ToArray();
        WarehouseChart.Points = warehouse.ToArray();
    }
    // The data layer explicitly supplies receipt/payment classification. Stock movement is not revenue.
    public void SetWarehouseEntries(System.Collections.IEnumerable receipts, System.Collections.IEnumerable payments)
    {
        Grid_HOA_DON_KHO.ItemsSource = receipts;
        Grid_HOA_DON_KHO_Chi.ItemsSource = payments;
    }
}
