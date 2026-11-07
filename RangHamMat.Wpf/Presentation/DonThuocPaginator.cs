using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace RangHamMat.Views;

/// <summary>Thêm định danh và số trang trong lề đã dành sẵn; giữ nguyên tọa độ nội dung FlowDocument.</summary>
internal sealed class DonThuocPaginator : DocumentPaginator
{
    private readonly DocumentPaginator inner;
    private readonly FlowDocument document;
    private readonly DonThuocTrinhBay? source;
    public DonThuocPaginator(FlowDocument document, Size size)
    {
        this.document = document;
        source = document.DataContext as DonThuocTrinhBay;
        inner = ((IDocumentPaginatorSource)document).DocumentPaginator;
        PageSize = size;
    }
    public override bool IsPageCountValid => inner.IsPageCountValid;
    public override int PageCount => inner.PageCount;
    public override IDocumentPaginatorSource Source => inner.Source;
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
    public override void ComputePageCount() => inner.ComputePageCount();
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
    private static FormattedText Text(string value) => new(value, CultureInfo.GetCultureInfo("vi-VN"), FlowDirection.LeftToRight,
        new Typeface("Segoe UI"), 9, new SolidColorBrush(Color.FromRgb(99, 121, 140)), 1);
}

public sealed record TrangDonThuoc(DocumentPaginator BoPhanTrang, int SoTrang)
{
    public string TieuDe => $"TRANG {SoTrang + 1:00}";
}
