using System.Windows;
using System.Windows.Controls.Primitives;
using RangHamMat.Views;

namespace RangHamMat.Controls;

/// <summary>DocumentPaginator của WPF là thuộc tính CLR; lớp này nhận trang qua dependency property để binding XAML.</summary>
public sealed class PrescriptionPageView : DocumentPageView
{
    /// <summary>
    /// Dependency property nhận dữ liệu một trang đơn thuốc và gọi PageChanged khi binding đổi nguồn.
    /// </summary>
    public static readonly DependencyProperty PageProperty = DependencyProperty.Register(nameof(Page), typeof(TrangDonThuoc),
        typeof(PrescriptionPageView), new PropertyMetadata(null, PageChanged));
    /// <summary>
    /// Trang đơn thuốc cần hiển thị, gồm bộ phân trang và chỉ số trang bắt đầu từ 0.
    /// </summary>
    public TrangDonThuoc? Page { get => (TrangDonThuoc?)GetValue(PageProperty); set => SetValue(PageProperty, value); }
    /// <summary>
    /// Chuyển dữ liệu trang từ binding sang các thuộc tính CLR của DocumentPageView.
    /// </summary>
    /// <param name="target">Điều khiển đang nhận trang mới.</param>
    /// <param name="args">Giá trị mới chứa bộ phân trang và số trang; null để bỏ nguồn tài liệu.</param>
    private static void PageChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        var view = (PrescriptionPageView)target;
        var page = (TrangDonThuoc?)args.NewValue;
        view.DocumentPaginator = page?.BoPhanTrang;
        view.PageNumber = page?.SoTrang ?? 0;
    }
}
