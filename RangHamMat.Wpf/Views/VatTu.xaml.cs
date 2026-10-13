// PHẦN AI ĐÃ THỰC HIỆN: AI đã giải thích cách nhận danh sách vật tư qua SetMaterials và gán nguồn cho các vùng hiển thị.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows.Controls;
using System.Windows.Data;
using RangHamMat.Controls;
namespace RangHamMat.Views;
/// <summary>
/// Màn hình vật tư: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class VatTu : UserControl
{
    // Gán dữ liệu được cung cấp vào đây; hai bảng vật tư/thuốc dùng collection view riêng.
    /// <summary>
    /// Khởi tạo vật tư từ XAML. Chưa có xử lý đọc/lưu SQL riêng trong constructor.
    /// </summary>
    public VatTu() => InitializeComponent();
    /// <summary>
    /// Nhận nguồn vật tư và loại vật tư từ nơi gọi, gán vào bảng tương ứng để hiển thị danh mục.
    /// </summary>
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
