using System.Windows.Controls;
using System.Windows;

namespace RangHamMat.Views;

public partial class PrescriptionPrintLine : UserControl
{
    public PrescriptionPrintLine() => InitializeComponent();

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
