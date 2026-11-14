using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Mẫu phiếu chuyên khoa: căn cột thuốc, tách hướng dẫn và giữ nội dung snapshot của đơn.</summary>
public static class DonThuocDocument
{
    /// <summary>
    /// Màu xanh đậm dùng cho tiêu đề và phần nhận diện của phiếu đơn thuốc.
    /// </summary>
    private static readonly Brush Navy = Brush("#173C54");
    /// <summary>
    /// Màu nhấn dùng cho nhãn mục và đường phân cách trên phiếu.
    /// </summary>
    private static readonly Brush Teal = Brush("#167D8D");
    /// <summary>
    /// Màu chữ chính cho nội dung chỉ định và thông tin bệnh nhân.
    /// </summary>
    private static readonly Brush Ink = Brush("#203B50");
    /// <summary>
    /// Màu chữ phụ cho nhãn, định danh và thông tin đối chiếu.
    /// </summary>
    private static readonly Brush Muted = Brush("#63798C");
    /// <summary>
    /// Màu viền dùng để phân tách các vùng thông tin của đơn thuốc.
    /// </summary>
    private static readonly Brush Line = Brush("#DCE7EE");
    /// <summary>
    /// Màu nền nhạt cho vùng chẩn đoán, thông tin bệnh nhân và hướng dẫn.
    /// </summary>
    private static readonly Brush Pale = Brush("#F2F7FA");

    /// <summary>
    /// Tạo phiếu in đầy đủ từ nội dung đơn đã ký, gồm thông tin bệnh nhân và vùng chữ ký.
    /// </summary>
    /// <param name="source">Bản in đơn đã được tầng nghiệp vụ kiểm tra nguồn và quyền.</param>
    /// <returns>FlowDocument dùng để phân trang và gửi máy in.</returns>
    public static FlowDocument Tao(BanInDonThuoc source) => Build(DonThuocTrinhBay.TuBanIn(source), true);
    /// <summary>
    /// Tạo phiếu in từ thẻ đơn thuốc theo nguồn trình bày tương thích của giao diện.
    /// </summary>
    /// <param name="source">Thẻ đơn đã được nơi gọi kiểm tra điều kiện in.</param>
    /// <returns>FlowDocument chứa nội dung thẻ và phần nhận diện phiếu in.</returns>
    public static FlowDocument Tao(RxCard source) => Build(DonThuocTrinhBay.TuTheCu(source), true);
    /// <summary>
    /// Tạo nội dung chi tiết của bản ký để nhúng vào màn hình, lược phần đầu phiếu và chữ ký.
    /// </summary>
    /// <param name="source">Nội dung đơn đã ký dùng làm nguồn hiển thị.</param>
    /// <returns>Tài liệu chi tiết với lề dành cho vùng xem trên màn hình.</returns>
    public static FlowDocument TaoChiTiet(BanInDonThuoc source) => Build(DonThuocTrinhBay.TuBanIn(source), false);
    /// <summary>
    /// Tạo tài liệu tra cứu từ dòng lịch sử và chi tiết đơn thuộc cùng ngữ cảnh.
    /// </summary>
    /// <param name="source">Thông tin bệnh nhân, bác sĩ và chẩn đoán của dòng lịch sử.</param>
    /// <param name="detail">Chi tiết đơn đã được nơi gọi kiểm tra quyền và liên kết nguồn.</param>
    /// <returns>Tài liệu hiển thị nội dung đơn trong lịch sử.</returns>
    public static FlowDocument TaoLichSu(DongLichSuDon source, DonThuocChiTiet detail) => Build(DonThuocTrinhBay.TuLichSu(source, detail), false);

    /// <summary>
    /// Bọc tài liệu bằng bộ phân trang có đầu trang tiếp nối và số trang trong lề.
    /// </summary>
    /// <param name="document">Tài liệu đơn thuốc cần chia trang.</param>
    /// <param name="size">Kích thước trang in theo đơn vị WPF.</param>
    /// <returns>Bộ phân trang đã áp dụng kích thước vùng in.</returns>
    public static DocumentPaginator PhanTrang(FlowDocument document, Size size) =>
        new DonThuocPaginator(document, size);

    /// <summary>
    /// Dựng tài liệu từ dữ liệu trình bày chung, giữ nguyên nội dung chỉ định và chọn phần đầu/chữ ký theo chế độ.
    /// </summary>
    /// <param name="source">Dữ liệu trình bày đã ánh xạ từ đơn nguồn.</param>
    /// <param name="print">True để thêm đầu phiếu, bệnh nhân và chữ ký; false cho vùng chi tiết nhúng.</param>
    /// <returns>Tài liệu đã có các khối chẩn đoán, thuốc, hướng dẫn và ghi chú.</returns>
    private static FlowDocument Build(DonThuocTrinhBay source, bool print)
    {
        var document = new FlowDocument
        {
            DataContext = source, FontFamily = new FontFamily("Segoe UI"), FontSize = 12.5,
            Foreground = Ink, Background = Brushes.White, PagePadding = print ? new Thickness(38, 34, 38, 52) : new Thickness(18),
            ColumnWidth = 2000, ColumnGap = 0, IsHyphenationEnabled = false,
            TextAlignment = TextAlignment.Left, Language = System.Windows.Markup.XmlLanguage.GetLanguage("vi-VN")
        };
        if (print)
        {
            document.Blocks.Add(Letterhead(source));
            document.Blocks.Add(Text("ĐƠN THUỐC", 29, Navy, true, new Thickness(0, 0, 0, 3)));
            document.Blocks.Add(Text("Thông tin kê đơn & hướng dẫn sử dụng", 11, Muted, false, new Thickness(0, 0, 0, 16)));
            document.Blocks.Add(PatientInformation(source));
        }

        var diagnosis = Text("01   CHẨN ĐOÁN", 10, Teal, true);
        diagnosis.Inlines.Add(new LineBreak());
        diagnosis.Inlines.Add(new Run(string.IsNullOrWhiteSpace(source.ChanDoan) ? "Chưa ghi nhận" : source.ChanDoan)
        {
            FontSize = 12.5, FontWeight = FontWeights.Normal, Foreground = Ink
        });
        diagnosis.Background = Pale;
        diagnosis.BorderBrush = Teal;
        diagnosis.BorderThickness = new Thickness(3, 0, 0, 0);
        diagnosis.Padding = new Thickness(12, 8, 12, 10);
        diagnosis.Margin = new Thickness(0, 0, 0, 17);
        diagnosis.LineHeight = 22;
        diagnosis.KeepWithNext = true;
        document.Blocks.Add(diagnosis);

        var heading = Text("02   THUỐC VÀ CÁCH SỬ DỤNG", 11, Navy, true, new Thickness(0, 0, 0, 9));
        heading.Inlines.Add(new Run($"     /     {source.SoLoaiThuoc} loại thuốc") { FontSize = 10, FontWeight = FontWeights.Normal, Foreground = Muted });
        heading.KeepWithNext = true;
        document.Blocks.Add(heading);
        document.Blocks.Add(MedicineColumns());

        foreach (var medicine in source.CacThuoc)
        {
            document.Blocks.Add(MedicineHeading(medicine));
            var dose = Text("", 12.5, Ink, false, new Thickness(44, 7, 0, 4));
            dose.Inlines.Add(new Bold(new Run("Liều dùng: ")));
            dose.Inlines.Add(new Run(medicine.LieuDung));
            if (medicine.CoDuongDung)
            {
                dose.Inlines.Add(new Run("     ·     ") { Foreground = Muted });
                dose.Inlines.Add(new Bold(new Run("Đường dùng: ")));
                dose.Inlines.Add(new Run(medicine.DuongDung));
            }
            dose.KeepWithNext = true;
            document.Blocks.Add(dose);
            var guidance = Text("", 12.5, Ink, false, new Thickness(44, 0, 0, 12));
            guidance.Inlines.Add(new Bold(new Run("Hướng dẫn: ")));
            guidance.Inlines.Add(new Run(medicine.HuongDan));
            guidance.Background = Pale;
            guidance.Padding = new Thickness(10, 7, 10, 9);
            guidance.BorderBrush = Line;
            guidance.BorderThickness = new Thickness(0, 0, 0, 1);
            guidance.KeepTogether = false;
            document.Blocks.Add(guidance);
        }

        if (source.CoGhiChu)
        {
            var note = Text("03   GHI CHÚ CỦA BÁC SĨ", 10, Brush("#8D6835"), true);
            note.Inlines.Add(new LineBreak());
            note.Inlines.Add(new Run(source.GhiChu) { FontSize = 12.5, FontWeight = FontWeights.Normal, Foreground = Ink });
            note.LineHeight = 22;
            note.Background = Brush("#FBF8F1");
            note.BorderBrush = Brush("#D9BE8C");
            note.BorderThickness = new Thickness(3, 0, 0, 0);
            note.Padding = new Thickness(12, 9, 12, 11);
            note.Margin = new Thickness(0, 6, 0, 0);
            document.Blocks.Add(note);
        }

        if (print) document.Blocks.Add(Signature(source));
        CanCot(document, 794);
        return document;
    }

    /// <summary>
    /// Tạo đầu phiếu với tên bệnh viện, mã đơn và thời điểm kê.
    /// </summary>
    /// <param name="source">Nguồn định danh đơn thuốc để in trên đầu phiếu.</param>
    /// <returns>Bảng đầu phiếu có cột nhận diện bệnh viện và cột mã đơn.</returns>
    private static Table Letterhead(DonThuocTrinhBay source)
    {
        var table = Table(new Thickness(0, 0, 0, 19), 58, -1, 174);
        table.BorderBrush = Teal;
        table.BorderThickness = new Thickness(0, 0, 0, 2);
        var row = new TableRow();
        var mark = Cell(Text("RHM", 15, Brushes.White, true), new Thickness(8, 12, 8, 12));
        mark.Background = Navy;
        mark.TextAlignment = TextAlignment.Center;
        row.Cells.Add(mark);
        var hospital = Cell(Text("BỆNH VIỆN RĂNG HÀM MẶT", 14, Navy, true), new Thickness(12, 4, 6, 12));
        hospital.Blocks.Add(Text("Chuyên khoa Răng · Hàm · Mặt", 10.5, Muted, false, new Thickness(0, 5, 0, 0)));
        row.Cells.Add(hospital);
        var code = Cell(Text("MÃ ĐƠN THUỐC", 9, Muted), new Thickness(0, 3, 0, 12));
        code.TextAlignment = TextAlignment.Right;
        code.Blocks.Add(Text(source.MaDon, 13, Navy, true, new Thickness(0, 3, 0, 3)));
        code.Blocks.Add(Text(source.NgayKe, 10, Muted));
        row.Cells.Add(code);
        table.RowGroups[0].Rows.Add(row);
        return table;
    }

    /// <summary>
    /// Tạo vùng bệnh nhân và bác sĩ từ dữ liệu của đơn, chỉ thêm điện thoại khi đã ghi nhận.
    /// </summary>
    /// <param name="source">Thông tin định danh đã ánh xạ cho phiếu.</param>
    /// <returns>Bảng hai cột để đối chiếu bệnh nhân, lần khám và bác sĩ.</returns>
    private static Table PatientInformation(DonThuocTrinhBay source)
    {
        var table = Table(new Thickness(0, 0, 0, 16), -1.35, -1);
        table.Background = Pale;
        table.BorderBrush = Line;
        table.BorderThickness = new Thickness(1);
        var row = new TableRow();
        var patient = Cell(Text("NGƯỜI BỆNH", 9, Muted, true), new Thickness(13, 10, 13, 10));
        patient.Blocks.Add(Text(source.TenBenhNhan, 16, Navy, true, new Thickness(0, 4, 0, 6)));
        patient.Blocks.Add(Text($"Mã bệnh nhân: {source.MaBenhNhan}", 11, Ink));
        patient.Blocks.Add(Text($"Ngày sinh: {source.NgaySinh}", 11, Ink, false, new Thickness(0, 3, 0, 0)));
        if (source.CoDienThoai) patient.Blocks.Add(Text($"Điện thoại: {source.DienThoai}", 11, Ink, false, new Thickness(0, 3, 0, 0)));
        row.Cells.Add(patient);
        var doctor = Cell(Text("BÁC SĨ KÊ ĐƠN", 9, Muted, true), new Thickness(13, 10, 13, 10));
        doctor.BorderBrush = Line; doctor.BorderThickness = new Thickness(1, 0, 0, 0);
        doctor.Blocks.Add(Text(source.BacSi, 13, Navy, true, new Thickness(0, 5, 0, 9)));
        doctor.Blocks.Add(Text($"Lần khám: {source.MaLanKham}", 11, Ink));
        row.Cells.Add(doctor);
        table.RowGroups[0].Rows.Add(row);
        return table;
    }

    /// <summary>
    /// Tạo hàng tiêu đề cho các cột số thứ tự, thuốc/hàm lượng, số lượng và thời gian dùng.
    /// </summary>
    /// <returns>Bảng tiêu đề có độ rộng cột được bộ phân trang căn lại theo vùng in.</returns>
    private static Table MedicineColumns()
    {
        var table = Table(new Thickness(0), 36, -1, 86, 76);
        var row = new TableRow { Background = Navy };
        foreach (var (label, index) in new[] { "STT", "THUỐC / HÀM LƯỢNG", "SỐ LƯỢNG", "THỜI GIAN" }.Select((value, index) => (value, index)))
        {
            var cell = Cell(Text(label, 9, Brushes.White, true), new Thickness(6, 8, 6, 8));
            cell.TextAlignment = index >= 2 ? TextAlignment.Center : TextAlignment.Left;
            row.Cells.Add(cell);
        }
        table.RowGroups[0].Rows.Add(row);
        return table;
    }

    /// <summary>
    /// Nhúng dòng thuốc XAML vào Paragraph để giữ đầu thuốc cùng phần chỉ định kế tiếp khi ngắt trang.
    /// </summary>
    /// <param name="medicine">Dòng thuốc đã chốt cần hiển thị.</param>
    /// <returns>Đoạn văn chứa điều khiển dòng thuốc với KeepWithNext.</returns>
    private static Paragraph MedicineHeading(DongDonThuocTrinhBay medicine)
    {
        // Paragraph có KeepWithNext; Table không có. Khối XAML vẫn căn cột và được giữ cùng đầu chỉ định.
        var line = new PrescriptionPrintLine(medicine) { Width = 718 };
        return new Paragraph(new InlineUIContainer(line))
        {
            Margin = new Thickness(0), Padding = new Thickness(0), FontSize = 1,
            KeepTogether = true, KeepWithNext = true
        };
    }

    /// <summary>
    /// Tạo vùng đối chiếu đơn/lần khám và chỗ ký tên của bác sĩ kê đơn.
    /// </summary>
    /// <param name="source">Định danh đơn và tên bác sĩ dùng cho phần cuối phiếu.</param>
    /// <returns>Bảng cuối phiếu gồm thông tin tái khám và vùng chữ ký.</returns>
    private static Table Signature(DonThuocTrinhBay source)
    {
        var table = Table(new Thickness(0, 20, 0, 0), -1, 240);
        var row = new TableRow();
        var context = Cell(Text("THÔNG TIN ĐỐI CHIẾU", 9, Muted, true), new Thickness(0, 8, 24, 0));
        context.Blocks.Add(Text($"Đơn {source.MaDon} · Lần khám {source.MaLanKham}", 10, Muted, false, new Thickness(0, 5, 0, 5)));
        context.Blocks.Add(Text("Vui lòng mang theo đơn thuốc khi tái khám.", 10, Muted));
        row.Cells.Add(context);
        var signature = Cell(Text("BÁC SĨ KÊ ĐƠN", 12, Navy, true), new Thickness(0));
        signature.TextAlignment = TextAlignment.Center;
        signature.Blocks.Add(Text("(Ký, ghi rõ họ tên)", 10, Muted, false, new Thickness(0, 4, 0, 34)));
        signature.Blocks.Add(Text(source.TenBacSi, 13, Navy, true));
        row.Cells.Add(signature);
        table.RowGroups[0].Rows.Add(row);
        return table;
    }

    /// <summary>
    /// Tạo khung bảng và lưu quy tắc độ rộng cột để CanCot tính lại theo bề ngang trang.
    /// </summary>
    /// <param name="margin">Lề bên ngoài bảng.</param>
    /// <param name="columns">Giá trị dương là độ rộng cố định; giá trị âm là trọng số chia phần còn lại.</param>
    /// <returns>Bảng có cột và nhóm dòng rỗng, kèm metadata RxColumnWidths.</returns>
    private static Table Table(Thickness margin, params double[] columns)
    {
        var table = new Table { CellSpacing = 0, Margin = margin };
        table.Resources["RxColumnWidths"] = columns;
        foreach (var width in columns) table.Columns.Add(new TableColumn());
        table.RowGroups.Add(new TableRowGroup());
        return table;
    }
    /// <summary>
    /// Phân bổ lại cột bảng và chiều rộng dòng thuốc theo vùng nội dung thực tế khi đổi khổ in.
    /// </summary>
    /// <param name="document">Tài liệu chứa bảng và các dòng thuốc cần căn.</param>
    /// <param name="pageWidth">Chiều rộng trang trước khi trừ lề, viền và khoảng đệm.</param>
    internal static void CanCot(FlowDocument document, double pageWidth)
    {
        // FlowDocument.Table không phân bổ chiều rộng sao giống Grid. Tính phần còn lại theo vùng in thực.
        foreach (var table in document.Blocks.OfType<Table>())
        {
            if (table.Resources["RxColumnWidths"] is not double[] widths) continue;
            var available = pageWidth - document.PagePadding.Left - document.PagePadding.Right
                - table.Margin.Left - table.Margin.Right - table.BorderThickness.Left - table.BorderThickness.Right;
            var fixedWidth = widths.Where(width => width > 0).Sum();
            var weights = widths.Where(width => width < 0).Sum(width => -width);
            for (var index = 0; index < widths.Length; index++)
                table.Columns[index].Width = new GridLength(widths[index] > 0 ? widths[index] : Math.Max(1, available - fixedWidth) * -widths[index] / weights);
        }
        foreach (var paragraph in document.Blocks.OfType<Paragraph>())
            foreach (var inline in paragraph.Inlines.OfType<InlineUIContainer>())
                if (inline.Child is PrescriptionPrintLine line)
                    line.Width = Math.Max(1, pageWidth - document.PagePadding.Left - document.PagePadding.Right
                        - paragraph.Margin.Left - paragraph.Margin.Right - paragraph.Padding.Left - paragraph.Padding.Right);
    }
    /// <summary>
    /// Bọc một đoạn văn trong ô bảng với khoảng đệm chỉ định.
    /// </summary>
    /// <param name="content">Đoạn văn đầu tiên của ô.</param>
    /// <param name="padding">Khoảng đệm bên trong ô.</param>
    /// <returns>Ô bảng sẵn sàng thêm vào hàng.</returns>
    private static TableCell Cell(Paragraph content, Thickness padding) => new(content) { Padding = padding };
    /// <summary>
    /// Tạo đoạn chữ với cỡ, màu, giãn dòng và lề thống nhất cho tài liệu đơn thuốc.
    /// </summary>
    /// <param name="value">Nội dung chữ; null được hiển thị thành chuỗi rỗng.</param>
    /// <param name="size">Cỡ chữ theo đơn vị WPF.</param>
    /// <param name="color">Màu chữ của đoạn.</param>
    /// <param name="bold">True để dùng độ đậm SemiBold.</param>
    /// <param name="margin">Lề tùy chọn; null để dùng lề bằng 0.</param>
    /// <returns>Đoạn văn cho phép phân trang, với giãn dòng bằng 1,5 lần cỡ chữ.</returns>
    private static Paragraph Text(string? value, double size, Brush color, bool bold = false, Thickness? margin = null) => new(new Run(value ?? ""))
    {
        FontSize = size, Foreground = color, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
        LineHeight = size * 1.5, Margin = margin ?? new Thickness(0), KeepTogether = false
    };
    /// <summary>
    /// Chuyển mã màu thành cọ tô và đóng băng cọ để dùng chung trong tài liệu.
    /// </summary>
    /// <param name="color">Mã màu đọc theo định dạng bất biến.</param>
    /// <returns>Cọ màu đã Freeze.</returns>
    private static Brush Brush(string color)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromInvariantString(color)!;
        brush.Freeze();
        return brush;
    }
}
