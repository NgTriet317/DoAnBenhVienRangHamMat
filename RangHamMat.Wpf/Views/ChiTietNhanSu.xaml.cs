using ET;
using System.Windows;
using System.Windows.Controls;

namespace RangHamMat.Views;

public partial class ChiTietNhanSu : Window
{    

    public ChiTietNhanSu(ET_NHANVIEN nhanVien)
    {
        InitializeComponent();

        DataContext = nhanVien;
    }
}
