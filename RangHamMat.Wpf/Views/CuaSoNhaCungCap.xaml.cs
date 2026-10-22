using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Form khai báo các cột SQL bằng Tag DB, tái sử dụng luồng điền/lưu và validation chung.</summary>
public partial class CuaSoNhaCungCap : UserControl
{
    public CuaSoNhaCungCap()
    {
        InitializeComponent();
        Loaded += (_, _) => txtTieuDe.Text = BieuMauHoSo.GetRecord(this) is null ? "Thêm nhà cung cấp" : "Cập nhật nhà cung cấp";
    }
}
