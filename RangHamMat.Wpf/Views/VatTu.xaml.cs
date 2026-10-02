using System.Windows.Controls;
using System.Windows.Data;
using RangHamMat.Controls;
namespace RangHamMat.Views;
public partial class VatTu : UserControl
{
    // Assign real data here when the application is connected; the two lists use independent views.
    public VatTu() => InitializeComponent();
    public void SetMaterials(System.Collections.IEnumerable materials, System.Collections.IEnumerable types)
    {
        var rows = materials.Cast<object>().ToList();
        var drugTypes = types.Cast<object>().Where(x => UiRecord.Text(x, "TenLVT").Trim().Equals("Thuốc", StringComparison.CurrentCultureIgnoreCase))
            .Select(x => UiRecord.Text(x, "MaLVT")).ToHashSet();
        var ordinary = new ListCollectionView(rows);
        ordinary.Filter = x => !drugTypes.Contains(UiRecord.Text(x, "MaLVT"));
        var medicines = new ListCollectionView(rows);
        medicines.Filter = x => drugTypes.Contains(UiRecord.Text(x, "MaLVT"));
        Grid_VAT_TU.ItemsSource = ordinary;
        Grid_VAT_TU_Thuoc.ItemsSource = medicines;
        Grid_LOAI_VAT_TU.ItemsSource = types;
    }
}
