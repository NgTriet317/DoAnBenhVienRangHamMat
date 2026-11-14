using System.Windows.Controls;
using System.Windows;

namespace RangHamMat.Views;

/// <summary>
/// Dòng tiêu đề thuốc dùng trong tài liệu in, có các cột khớp hàng tiêu đề của phiếu.
/// </summary>
public partial class PrescriptionPrintLine : UserControl
{
    /// <summary>
    /// Nạp bố cục XAML của dòng thuốc để WPF có thể đo trước khi phân trang.
    /// </summary>
    public PrescriptionPrintLine() => InitializeComponent();

    /// <summary>
    /// Điền trực tiếp dữ liệu thuốc đã chốt để phép đo phân trang không phải chờ lượt cập nhật binding.
    /// </summary>
    /// <param name="medicine">Dòng thuốc trình bày gồm thứ tự, tên, hàm lượng, số lượng, đơn vị và số ngày dùng.</param>
    public PrescriptionPrintLine(DongDonThuocTrinhBay medicine) : this()
    {
        // Điền snapshot đồng bộ trước khi WPF phân trang; không phụ thuộc lượt cập nhật binding của Dispatcher.
        NumberText.Text = medicine.ThuTu;
        NameText.Text = medicine.TenThuoc;
        StrengthText.Text = medicine.HamLuong;
        StrengthText.Visibility = medicine.CoHamLuong ? Visibility.Visible : Visibility.Collapsed;
        QuantityText.Text = medicine.SoLuong;
        UnitText.Text = medicine.DonViTinh;
        DaysText.Text = medicine.SoNgay;
    }
}
