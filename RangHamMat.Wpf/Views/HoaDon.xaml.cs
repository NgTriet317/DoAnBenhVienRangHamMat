namespace RangHamMat.Views;

public partial class HoaDon
{
    public HoaDon() => InitializeComponent();
    // The data layer explicitly supplies receipt/payment classification. Stock movement is not revenue.
    public void SetWarehouseEntries(System.Collections.IEnumerable receipts, System.Collections.IEnumerable payments)
    {
        Grid_HOA_DON_KHO.ItemsSource = receipts;
        Grid_HOA_DON_KHO_Chi.ItemsSource = payments;
    }
}
