using System.Windows;

namespace RangHamMat.Views;

public partial class DangNhap : Window
{
    public DangNhap() => InitializeComponent();

    private void DangNhap_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Thoat_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

}
