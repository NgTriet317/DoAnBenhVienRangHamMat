using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Form dùng Tag DB để đọc/điền dữ liệu và lưu theo XAML → BUS → DAL → SQL.</summary>
public partial class CuaSoBaoTri : UserControl
{
    public CuaSoBaoTri()
    {
        InitializeComponent();
        dpBatDau.SelectedDate = DateTime.Today;
        Loaded += (_, _) =>
        {
            txtTieuDe.Text = BieuMauHoSo.GetRecord(this) is null ? "Thêm lần bảo trì" : "Cập nhật lần bảo trì";
        };
    }
}
