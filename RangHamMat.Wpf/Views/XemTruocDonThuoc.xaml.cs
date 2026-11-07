using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Xem và in cùng một tài liệu; đọc lại đúng nguồn trước mỗi lần gửi máy in.</summary>
public partial class XemTruocDonThuoc : Window
{
    private readonly Func<BanInDonThuoc>? read;
    private readonly string fixedId;
    public FlowDocument? DonDaDoc { get; private set; }

    public XemTruocDonThuoc(Func<BanInDonThuoc> read)
    {
        InitializeComponent();
        RangHamMat.Controls.ClinicalWorkflow.ClinicalWindowLifecycle.Attach(this);
        this.read = read;
        var source = read();
        fixedId = source.Nguon.Don.MaDT;
        ShowDocument(DonThuocDocument.Tao(source));
    }

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

    private void ShowDocument(FlowDocument document)
    {
        DonDaDoc = document;
        var paginator = DonThuocDocument.PhanTrang(document, new Size(794, 1123));
        PrescriptionPages.ItemsSource = Enumerable.Range(0, paginator.PageCount).Select(page => new TrangDonThuoc(paginator, page)).ToArray();
        SourceCaption.Text = $"#{fixedId}   ·   Khổ A4   ·   {paginator.PageCount} trang";
        Title = "Đơn thuốc " + fixedId;
    }

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

    private static void ValidateSigned(RxCard card)
    {
        if (!card.CanPrint) throw new LoiKhamException("MaDT", "Chỉ in đơn đã xác nhận.");
        if (DuLieuGiaoDien.PhamViLamSang && new BUS.BUS_DONTHUOC().LayChiTietCoQuyen(card.Code,
            Controls.ClinicalWorkflow.QuyenNghiepVu.LayNguCanh()).DonThuoc.TrangThai != "Đã xác nhận")
            throw new LoiKhamException("MaDT", "Đơn đã đổi hiệu lực; tải lại trước khi in.");
    }

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
