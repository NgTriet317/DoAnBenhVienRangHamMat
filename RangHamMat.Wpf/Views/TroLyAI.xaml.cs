using System.Windows.Controls;

namespace RangHamMat.Views;

/// <summary>
/// Vỏ tương thích điều hướng cho nút TroLyAI hiện có.
/// Logic chat và tạo phiếu được quản lý bởi ReceptionistViewModel, không chạy code-behind cũ.
/// </summary>
public partial class TroLyAI : UserControl
{
    public TroLyAI() => InitializeComponent();
}
