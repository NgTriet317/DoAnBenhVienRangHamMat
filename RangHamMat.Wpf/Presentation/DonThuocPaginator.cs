using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace RangHamMat.Views;

/// <summary>Thêm định danh và số trang trong lề đã dành sẵn; giữ nguyên tọa độ nội dung FlowDocument.</summary>
internal sealed class DonThuocPaginator : DocumentPaginator
{
    /// <summary>
    /// Bộ phân trang gốc của FlowDocument chịu trách nhiệm bố trí nội dung đơn.
    /// </summary>
    private readonly DocumentPaginator inner;
    /// <summary>
    /// Tài liệu nguồn dùng để căn lại cột mỗi khi đổi kích thước trang.
    /// </summary>
    private readonly FlowDocument document;
    /// <summary>
    /// Dữ liệu trình bày lấy từ DataContext để ghi mã đơn và ngày kê trong lề trang.
    /// </summary>
    private readonly DonThuocTrinhBay? source;
    /// <summary>
    /// Nhận tài liệu nguồn và thiết lập khổ trang, đồng thời tính số trang ban đầu.
    /// </summary>
    /// <param name="document">FlowDocument chứa nội dung đơn và DataContext trình bày.</param>
    /// <param name="size">Kích thước trang in cần áp dụng.</param>
    public DonThuocPaginator(FlowDocument document, Size size)
    {
        this.document = document;
        source = document.DataContext as DonThuocTrinhBay;
        inner = ((IDocumentPaginatorSource)document).DocumentPaginator;
        PageSize = size;
    }
    /// <summary>
    /// Cho biết số trang do bộ phân trang gốc cung cấp đã hợp lệ hay chưa.
    /// </summary>
    public override bool IsPageCountValid => inner.IsPageCountValid;
    /// <summary>
    /// Tổng số trang nội dung do FlowDocument tính tại kích thước trang hiện tại.
    /// </summary>
    public override int PageCount => inner.PageCount;
    /// <summary>
    /// Nguồn tài liệu của bộ phân trang gốc.
    /// </summary>
    public override IDocumentPaginatorSource Source => inner.Source;
    /// <summary>
    /// Khổ trang hiện tại; khi gán sẽ kiểm tra kích thước tối thiểu, căn cột và tính lại số trang.
    /// </summary>
    public override Size PageSize
    {
        get => inner.PageSize;
        set
        {
            if (!double.IsFinite(value.Width) || !double.IsFinite(value.Height) || value.Width < 320 || value.Height < 240)
                throw new ArgumentOutOfRangeException(nameof(value), "Vùng in quá nhỏ để trình bày đơn thuốc.");
            DonThuocDocument.CanCot(document, value.Width);
            inner.PageSize = value;
            inner.ComputePageCount();
        }
    }
    /// <summary>
    /// Yêu cầu bộ phân trang gốc tính lại tổng số trang nội dung.
    /// </summary>
    public override void ComputePageCount() => inner.ComputePageCount();
    /// <summary>
    /// Ghép trang nội dung với nền trắng, đầu trang tiếp nối và chân trang chứa mã đơn, ngày kê, số trang.
    /// </summary>
    /// <param name="pageNumber">Chỉ số trang cần lấy, bắt đầu từ 0.</param>
    /// <returns>Trang đã thêm thông tin trong lề, hoặc DocumentPage.Missing nếu không có trang tương ứng.</returns>
    public override DocumentPage GetPage(int pageNumber)
    {
        var page = inner.GetPage(pageNumber);
        if (page == DocumentPage.Missing) return page;
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            var bounds = new Rect(PageSize);
            drawing.DrawRectangle(Brushes.White, null, bounds);
            drawing.DrawRectangle(new VisualBrush(page.Visual) { ViewboxUnits = BrushMappingMode.Absolute, Viewbox = bounds, Stretch = Stretch.Fill }, null, bounds);
            var rule = new Pen(new SolidColorBrush(Color.FromRgb(210, 225, 233)), 0.7);
            if (pageNumber > 0)
            {
                drawing.DrawText(Text("BỆNH VIỆN RĂNG HÀM MẶT"), new Point(38, 11));
                var code = Text("ĐƠN THUỐC · " + source?.MaDon);
                drawing.DrawText(code, new Point(PageSize.Width - 38 - code.Width, 11));
                drawing.DrawLine(rule, new Point(38, 29), new Point(PageSize.Width - 38, 29));
            }
            var footer = PageSize.Height - 34;
            drawing.DrawLine(rule, new Point(38, footer), new Point(PageSize.Width - 38, footer));
            drawing.DrawText(Text($"{source?.MaDon}  ·  {source?.NgayKe}"), new Point(38, footer + 7));
            var counter = Text($"Trang {pageNumber + 1} / {PageCount}");
            drawing.DrawText(counter, new Point(PageSize.Width - 38 - counter.Width, footer + 7));
        }
        return new DocumentPage(visual, PageSize, page.BleedBox, page.ContentBox);
    }
    /// <summary>
    /// Định dạng chữ nhỏ dùng ở đầu và chân trang theo ngôn ngữ vi-VN.
    /// </summary>
    /// <param name="value">Nội dung định danh hoặc số trang cần vẽ.</param>
    /// <returns>Đối tượng chữ đã định dạng để DrawingContext vẽ vào lề.</returns>
    private static FormattedText Text(string value) => new(value, CultureInfo.GetCultureInfo("vi-VN"), FlowDirection.LeftToRight,
        new Typeface("Segoe UI"), 9, new SolidColorBrush(Color.FromRgb(99, 121, 140)), 1);
}

/// <summary>
/// Nguồn binding cho một trang xem trước của đơn thuốc.
/// </summary>
/// <param name="BoPhanTrang">Bộ phân trang dùng chung cho toàn bộ tài liệu.</param>
/// <param name="SoTrang">Chỉ số trang trong tài liệu, bắt đầu từ 0.</param>
public sealed record TrangDonThuoc(DocumentPaginator BoPhanTrang, int SoTrang)
{
    /// <summary>
    /// Nhãn trang cho vùng xem trước, chuyển chỉ số từ 0 thành số hiển thị từ 1 với ít nhất hai chữ số.
    /// </summary>
    public string TieuDe => $"TRANG {SoTrang + 1:00}";
}
