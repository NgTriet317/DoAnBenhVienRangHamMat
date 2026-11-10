using System.Windows;
using ET;

namespace RangHamMat.Views;

public partial class XemAnhXquang : Window
{
    public XemAnhXquang(DuLieuAnhXquang source)
    {
        InitializeComponent();Caption.Text=$"{source.Anh.TenTepGoc} · Lần khám {source.Anh.MaLS} · {source.Anh.NgayTao:dd/MM/yyyy HH:mm:ss}";
        FullImage.Source=AnhXquangLanKham.Decode(source.NoiDung);
    }
}
