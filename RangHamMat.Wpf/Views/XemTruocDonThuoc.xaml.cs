using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Xem và in cùng một tài liệu; đọc lại đúng nguồn trước mỗi lần gửi máy in.</summary>
public partial class XemTruocDonThuoc : Window
{
    /// <summary>
    /// Hàm đọc lại bản ký trước khi in; null khi đang dùng đường hiển thị tương thích từ thẻ đơn.
    /// </summary>
    private readonly Func<BanInDonThuoc>? read;
    /// <summary>
    /// Mã đơn cố định từ lúc mở cửa sổ, dùng để chặn nguồn in bị đổi sang đơn khác.
    /// </summary>
    private readonly string fixedId;
    /// <summary>
    /// Tài liệu hiện được dùng cho bản xem trước; việc in dựng tài liệu riêng.
    /// </summary>
    public FlowDocument? DonDaDoc { get; private set; }

    /// <summary>
    /// Đọc bản ký qua hàm được cấp, chốt mã nguồn và tạo các trang xem trước.
    /// </summary>
    /// <param name="read">Hàm đọc bản in đã kiểm tra quyền; được gọi lại trước mỗi lần gửi máy in.</param>
    public XemTruocDonThuoc(Func<BanInDonThuoc> read)
    {
        InitializeComponent();
        RangHamMat.Controls.ClinicalWorkflow.ClinicalWindowLifecycle.Attach(this);
        this.read = read;
        var source = read();
        fixedId = source.Nguon.Don.MaDT;
        ShowDocument(DonThuocDocument.Tao(source));
    }

    /// <summary>
    /// Mở xem trước từ thẻ đơn đã xác nhận, ưu tiên bản ký có snapshot đầy đủ trong phạm vi lâm sàng.
    /// </summary>
    /// <param name="card">Thẻ đơn cần xem và in; trạng thái được kiểm tra trước khi tạo tài liệu.</param>
    public XemTruocDonThuoc(RxCard card)
    {
        ValidateSigned(card);
        InitializeComponent();
        RangHamMat.Controls.ClinicalWorkflow.ClinicalWindowLifecycle.Attach(this);
        DataContext = card;
        fixedId = card.Code;
        if (DuLieuGiaoDien.PhamViLamSang)
        {
            var actor = Controls.ClinicalWorkflow.QuyenNghiepVu.LayNguCanh();
            var service = new BUS.BUS_DONTHUOC();
            var detail = service.LayChiTietCoQuyen(card.Code, actor);
            if (detail.CacDong.Count > 0 && detail.CacDong.All(line =>
                !string.IsNullOrWhiteSpace(line.TenThuocChot) && !string.IsNullOrWhiteSpace(line.HamLuongChot) && !string.IsNullOrWhiteSpace(line.DonViTinhChot)))
            {
                var id = card.Code;
                read = () => service.LayBanInDon(id, actor);
                ShowDocument(DonThuocDocument.Tao(read()));
                return;
            }
        }
        ShowDocument(DonThuocDocument.Tao(card));
    }

    /// <summary>
    /// Phân trang A4 cho vùng xem trước và cập nhật tiêu đề cùng nhãn nguồn đơn.
    /// </summary>
    /// <param name="document">Tài liệu mới cần hiển thị trong cửa sổ.</param>
    private void ShowDocument(FlowDocument document)
    {
        DonDaDoc = document;
        var paginator = DonThuocDocument.PhanTrang(document, new Size(794, 1123));
        PrescriptionPages.ItemsSource = Enumerable.Range(0, paginator.PageCount).Select(page => new TrangDonThuoc(paginator, page)).ToArray();
        SourceCaption.Text = $"#{fixedId}   ·   Khổ A4   ·   {paginator.PageCount} trang";
        Title = "Đơn thuốc " + fixedId;
    }

    /// <summary>
    /// Đọc lại đúng đơn nguồn, dựng tài liệu riêng theo vùng in và gửi qua hàm in được cấp.
    /// </summary>
    /// <remarks>Mã nguồn phải khớp fixedId; phân trang trên tài liệu riêng giúp bản xem trước giữ nguyên bố cục.</remarks>
    /// <param name="send">Hàm nhận bộ phân trang để gửi máy in hoặc xuất tài liệu.</param>
    /// <param name="pageSize">Kích thước vùng in thực tế do máy in cung cấp.</param>
    /// <param name="error">Nhận thông báo khi không thể đọc nguồn hoặc gửi in; null nếu hoàn tất.</param>
    /// <returns>True khi hàm gửi in đã chạy xong; false nếu quá trình chuẩn bị hoặc gửi in phát sinh lỗi.</returns>
    public bool TryPrint(Action<DocumentPaginator> send, Size pageSize, out string? error)
    {
        error = null;
        try
        {
            FlowDocument document;
            if (read is not null)
            {
                var source = read();
                if (source.Nguon.Don.MaDT != fixedId)
                    throw new LoiKhamException("MaDT", "Nguồn in đã đổi mã đơn; mở lại đúng bản ký.");
                document = DonThuocDocument.Tao(source);
            }
            else if (DataContext is RxCard card)
            {
                if (card.Code != fixedId)
                    throw new LoiKhamException("MaDT", "Nguồn in đã đổi mã đơn; mở lại đúng bản ký.");
                ValidateSigned(card);
                document = DonThuocDocument.Tao(card);
            }
            else throw new LoiKhamException("MaDT", "Thiếu nguồn đơn đã ký.");
            // Phân trang trên bản riêng, giữ bố cục xem trước ổn định sau khi in/xuất PDF.
            var paginator = DonThuocDocument.PhanTrang(document, pageSize);
            send(paginator);
            PrintStatus.Text = "Đã gửi đơn thuốc " + fixedId + " tới máy in đã chọn.";
            return true;
        }
        catch (Exception ex)
        {
            error = ex is LoiKhamException or TuChoiQuyenNghiepVuException ? ex.Message
                : "Không thể in đơn. In lại cùng bản ký; trạng thái giao và thu tiền giữ nguyên.";
            PrintStatus.Text = error;
            return false;
        }
    }

    /// <summary>
    /// Yêu cầu đơn đã xác nhận và kiểm tra lại trạng thái qua nghiệp vụ khi đang trong phạm vi lâm sàng.
    /// </summary>
    /// <param name="card">Thẻ đơn cần xác minh trước khi tạo tài liệu in.</param>
    private static void ValidateSigned(RxCard card)
    {
        if (!card.CanPrint) throw new LoiKhamException("MaDT", "Chỉ in đơn đã xác nhận.");
        if (DuLieuGiaoDien.PhamViLamSang && new BUS.BUS_DONTHUOC().LayChiTietCoQuyen(card.Code,
            Controls.ClinicalWorkflow.QuyenNghiepVu.LayNguCanh()).DonThuoc.TrangThai != "Đã xác nhận")
            throw new LoiKhamException("MaDT", "Đơn đã đổi hiệu lực; tải lại trước khi in.");
    }

    /// <summary>
    /// Mở hộp thoại chọn máy in và truyền vùng in thực tế cho TryPrint; hiển thị thông báo nếu không mở được máy in.
    /// </summary>
    private void Print_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true) return;
            TryPrint(p => dialog.PrintDocument(p, "Đơn thuốc " + fixedId),
                new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight), out _);
        }
        catch (Exception) { PrintStatus.Text = "Không mở được máy in. Có thể in lại cùng bản ký."; }
    }
}
