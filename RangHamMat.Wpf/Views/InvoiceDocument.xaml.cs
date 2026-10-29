// PHẦN AI ĐÃ THỰC HIỆN: AI đã tạo phần dữ liệu cho InvoiceDocument.xaml và bộ phân trang InvoiceVisualPaginator; đo chiều cao nội dung, chọn điểm ngắt theo dòng chữ và sao chép vùng hiển thị để in hóa đơn nhiều trang.
// AI hỗ trợ: chứng từ XAML và phân trang visual đã khai báo, ngày 13/10/2026.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RangHamMat.Views;

/// <summary>AI hỗ trợ: chứng từ tải bố cục XAML và cung cấp paginator cho các dòng dài, không dựng bảng bằng C#.</summary>
public partial class InvoiceDocument : UserControl, IDocumentPaginatorSource
{
    /// <summary>AI hỗ trợ: khởi tạo các đầu mục, dòng và tổng tiền đã được khai báo trong InvoiceDocument.xaml.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: AI hỗ trợ: khởi tạo các đầu mục, dòng và tổng tiền đã được khai báo trong InvoiceDocument.xaml.</remarks>
    public InvoiceDocument() => InitializeComponent();

    /// <summary>AI hỗ trợ: paginator mặc định theo trang gần khổ A4 để preview và smoke kiểm tra cùng cơ chế in.</summary>
    public DocumentPaginator DocumentPaginator => CreatePaginator(new Size(780, 1100));

    /// <summary>AI hỗ trợ: materialize một bản chứng từ XAML riêng với cùng dữ liệu, tránh đổi chiều rộng màn hình khi in.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: AI hỗ trợ: materialize một bản chứng từ XAML riêng với cùng dữ liệu, tránh đổi chiều rộng màn hình khi in.</remarks>
    public DocumentPaginator CreatePaginator(Size pageSize) => new InvoiceVisualPaginator(new InvoiceDocument { DataContext = DataContext }, pageSize);
}

/// <summary>AI hỗ trợ: phân trang bằng dải visual của chứng từ XAML; ngắt ở biên dòng chữ, không bỏ dòng cao hơn trang.</summary>
internal sealed class InvoiceVisualPaginator : DocumentPaginator
{
    /// <summary>
    /// Giá trị lưu nội bộ của thuộc tính PageSize, dùng bởi getter/setter tương ứng.
    /// </summary>
    private const double Padding = 34;
    /// <summary>
    /// Giá trị lưu nội bộ của thuộc tính Source, dùng bởi getter/setter tương ứng.
    /// </summary>
    private readonly InvoiceDocument document;
    /// <summary>
    /// Giá trị lưu nội bộ của thuộc tính PageCount, dùng bởi getter/setter tương ứng.
    /// </summary>
    private Rect[] slices = Array.Empty<Rect>();
    /// <summary>
    /// Giá trị lưu nội bộ của thuộc tính PageSize, dùng bởi getter/setter tương ứng.
    /// </summary>
    private Size pageSize;

    /// <summary>AI hỗ trợ: nhận chứng từ XAML và kích thước vùng in rồi đo đầy đủ nội dung trước khi xác định trang.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: AI hỗ trợ: nhận chứng từ XAML và kích thước vùng in rồi đo đầy đủ nội dung trước khi xác định trang.</remarks>
    internal InvoiceVisualPaginator(InvoiceDocument document, Size pageSize)
    {
        this.document = document;
        PageSize = pageSize;
    }

    /// <summary>AI hỗ trợ: tất cả trang đã tính đồng bộ sau khi đo chứng từ nên PageCount luôn có hiệu lực.</summary>
    public override bool IsPageCountValid => true;
    /// <summary>AI hỗ trợ: số dải liên tiếp phủ toàn bộ chiều cao chứng từ, gồm các tổng và phần ký cuối.</summary>
    public override int PageCount => slices.Length;
    /// <summary>AI hỗ trợ: kích thước vùng in; thay đổi kích thước sẽ đo lại XAML và tính lại các biên trang.</summary>
    public override Size PageSize
    {
        get => pageSize;
        set
        {
            if (!double.IsFinite(value.Width) || !double.IsFinite(value.Height) || value.Width <= 2 * Padding + 30 || value.Height <= 2 * Padding + 30)
                throw new ArgumentOutOfRangeException(nameof(value), "Vùng in quá nhỏ để trình bày hóa đơn.");
            pageSize = value;
            Paginate();
        }
    }
    /// <summary>AI hỗ trợ: nguồn tài liệu giữ hợp đồng IDocumentPaginatorSource chuẩn của WPF.</summary>
    public override IDocumentPaginatorSource Source => document;

    /// <summary>AI hỗ trợ: vẽ dải vào trang bằng đối tượng đồ họa; control và bố cục đều đã có trong XAML.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: AI hỗ trợ: vẽ dải vào trang bằng đối tượng đồ họa; control và bố cục đều đã có trong XAML.</remarks>
    public override DocumentPage GetPage(int pageNumber)
    {
        if (pageNumber < 0 || pageNumber >= slices.Length) return DocumentPage.Missing;
        var slice = slices[pageNumber];
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.White, null, new Rect(pageSize));
            var brush = new VisualBrush(document) { ViewboxUnits = BrushMappingMode.Absolute, Viewbox = slice, Stretch = Stretch.Fill, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top };
            drawing.DrawRectangle(brush, null, new Rect(Padding, Padding, slice.Width, slice.Height));
        }
        var content = new Rect(Padding, Padding, slice.Width, slice.Height);
        return new DocumentPage(visual, pageSize, new Rect(pageSize), content);
    }

    /// <summary>AI hỗ trợ: đo tất cả dòng không ảo hóa và chọn biên trang ngoài dải dòng chữ để không cắt đôi ký tự.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: AI hỗ trợ: đo tất cả dòng không ảo hóa và chọn biên trang ngoài dải dòng chữ để không cắt đôi ký tự.</remarks>
    private void Paginate()
    {
        var width = pageSize.Width - 2 * Padding;
        var height = pageSize.Height - 2 * Padding;
        document.Width = width;
        document.Measure(new Size(width, double.PositiveInfinity));
        document.Arrange(new Rect(0, 0, width, document.DesiredSize.Height));
        document.UpdateLayout();
        var bands = TextBands(document).OrderBy(band => band.Top).ToArray();
        var pages = new List<Rect>();
        var top = 0d;
        while (top < document.ActualHeight)
        {
            var bottom = Math.Min(top + height, document.ActualHeight);
            // Lùi về đầu dòng giao biên trang; lặp để tránh cắt dòng ở cột bên cạnh.
            while (bands.FirstOrDefault(band => band.Top < bottom - 0.01 && band.Bottom > bottom + 0.01) is { Height: > 0 } crossing)
                bottom = crossing.Top;
            if (bottom <= top + 0.01) throw new InvalidOperationException("Vùng in không đủ chứa một dòng chữ của hóa đơn.");
            pages.Add(new Rect(0, top, width, bottom - top));
            top = bottom;
        }
        slices = pages.ToArray();
    }

    /// <summary>AI hỗ trợ: đọc dải từng dòng TextBlock theo LineHeight XAML, kể cả mô tả có hàng nghìn ký tự.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: AI hỗ trợ: đọc dải từng dòng TextBlock theo LineHeight XAML, kể cả mô tả có hàng nghìn ký tự.</remarks>
    private IEnumerable<Rect> TextBands(DependencyObject current)
    {
        if (current is TextBlock text && text.ActualHeight > 0)
        {
            var position = text.TranslatePoint(new Point(0, 0), document);
            var lineHeight = text.LineHeight;
            for (var offset = 0d; offset < text.ActualHeight - 0.01; offset += lineHeight)
                yield return new Rect(position.X, position.Y + offset, text.ActualWidth, Math.Min(lineHeight, text.ActualHeight - offset));
        }
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(current); index++)
            foreach (var band in TextBands(VisualTreeHelper.GetChild(current, index))) yield return band;
    }
}
