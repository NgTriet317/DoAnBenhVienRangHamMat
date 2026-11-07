using System.Windows;
using System.Windows.Controls.Primitives;
using RangHamMat.Views;

namespace RangHamMat.Controls;

/// <summary>DocumentPaginator của WPF là thuộc tính CLR; lớp này nhận trang qua dependency property để binding XAML.</summary>
public sealed class PrescriptionPageView : DocumentPageView
{
    public static readonly DependencyProperty PageProperty = DependencyProperty.Register(nameof(Page), typeof(TrangDonThuoc),
        typeof(PrescriptionPageView), new PropertyMetadata(null, PageChanged));
    public TrangDonThuoc? Page { get => (TrangDonThuoc?)GetValue(PageProperty); set => SetValue(PageProperty, value); }
    private static void PageChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        var view = (PrescriptionPageView)target;
        var page = (TrangDonThuoc?)args.NewValue;
        view.DocumentPaginator = page?.BoPhanTrang;
        view.PageNumber = page?.SoTrang ?? 0;
    }
}
