using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Form khai báo các cột SQL bằng Tag DB, tái sử dụng luồng điền/lưu và validation chung.</summary>
public partial class CuaSoNhaCungCap : UserControl
{
    /// <summary>
    /// Khởi tạo cửa sổ nhà cung cấp. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    public CuaSoNhaCungCap()
    {
        InitializeComponent();
        Loaded += (_, _) => txtTieuDe.Text = BieuMauHoSo.GetRecord(this) is null ? "Thêm nhà cung cấp" : "Cập nhật nhà cung cấp";
    }
}
