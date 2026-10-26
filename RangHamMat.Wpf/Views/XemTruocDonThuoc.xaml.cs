// PHẦN AI ĐÃ THỰC HIỆN: AI đã giải thích cách đưa dữ liệu đơn thuốc vào vùng xem trước và luồng in/xuất PDF qua hộp thoại in WPF.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RangHamMat.Controls;
using ET;
using System.Windows.Documents;
namespace RangHamMat.Views;

/// <summary>
/// Màn hình xem trước/in đơn thuốc: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class XemTruocDonThuoc : Window
{
    private readonly Func<BanInDonThuoc>? read;
    private readonly string? fixedId;
    private FlowDocumentScrollViewer? viewer;
    public FlowDocument? DonDaDoc {get;private set;}
    public XemTruocDonThuoc(Func<BanInDonThuoc> read)
    {
        InitializeComponent();this.read=read;var source=read();fixedId=source.Nguon.Don.MaDT;ShowModern(source);
    }
    private void ShowModern(BanInDonThuoc source)
    {
        DonDaDoc=DonThuocDocument.Tao(source);
        var button=new Button{Content="In / in lại bản đã ký",Margin=new Thickness(10)};button.Click+=Print_Click;
        viewer=new FlowDocumentScrollViewer{Document=DonDaDoc,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};var panel=new DockPanel();
        // Vùng trạng thái riêng của bản in nhiều trang.
        var feedback=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(10)};
        modernStatus=feedback;DockPanel.SetDock(button,Dock.Top);DockPanel.SetDock(feedback,Dock.Top);panel.Children.Add(button);panel.Children.Add(feedback);panel.Children.Add(viewer);Content=panel;Title="Đơn thuốc "+fixedId;
    }
    private TextBlock? modernStatus;
    private void Feedback(string text)=>(modernStatus??PrintStatus).Text=text;
    public bool TryPrint(Action<DocumentPaginator> send,Size pageSize,out string? error)
    {
        error=null;try
        {
            if(read is not null){var source=read();if(source.Nguon.Don.MaDT!=fixedId)throw new LoiKhamException("MaDT","Nguồn in đã đổi mã đơn; mở lại đúng bản ký.");DonDaDoc=DonThuocDocument.Tao(source);}
            else if(DataContext is RxCard card){ValidateSigned(card);DonDaDoc=DonThuocDocument.Tao(card);}
            else throw new LoiKhamException("MaDT","Thiếu nguồn đơn đã ký.");
            if(viewer is not null)viewer.Document=DonDaDoc;
            var paginator=((IDocumentPaginatorSource)DonDaDoc).DocumentPaginator;paginator.PageSize=pageSize;paginator.ComputePageCount();send(paginator);Feedback("Đã gửi đúng bản ký tới máy in.");return true;
        }
        catch(Exception ex){error=ex is LoiKhamException or TuChoiQuyenNghiepVuException?ex.Message:"Không thể in đơn. In lại cùng bản ký; trạng thái giao và thu tiền giữ nguyên.";Feedback(error);return false;}
    }
    /// <summary>
    /// Khởi tạo xem trước/in đơn thuốc từ XAML.
    /// </summary>
    public XemTruocDonThuoc(RxCard card)
    {
        ValidateSigned(card);InitializeComponent();DataContext=card;
        if(DuLieuGiaoDien.PhamViLamSang)
        {
            var actor=Controls.ClinicalWorkflow.QuyenNghiepVu.LayNguCanh();var service=new BUS.BUS_DONTHUOC();var detail=service.LayChiTietCoQuyen(card.Code,actor);
            if(detail.CacDong.Count>0&&detail.CacDong.All(d=>!string.IsNullOrWhiteSpace(d.TenThuocChot)&&!string.IsNullOrWhiteSpace(d.HamLuongChot)&&!string.IsNullOrWhiteSpace(d.DonViTinhChot)))
            {var id=card.Code;read=()=>service.LayBanInDon(id,actor);fixedId=id;ShowModern(read());}
        }
    }
    private static void ValidateSigned(RxCard card)
    {
        if(!card.CanPrint)throw new ET.LoiKhamException("MaDT","Chỉ in đơn đã xác nhận.");
        if(DuLieuGiaoDien.PhamViLamSang && new BUS.BUS_DONTHUOC().LayChiTietCoQuyen(card.Code,Controls.ClinicalWorkflow.QuyenNghiepVu.LayNguCanh()).DonThuoc.TrangThai!="Đã xác nhận")throw new ET.LoiKhamException("MaDT","Đơn đã đổi hiệu lực; tải lại trước khi in.");
    }
    /// <summary>
    /// Mở hộp thoại chọn máy in; sau khi người dùng đồng ý thì in vùng đơn thuốc, có thể chọn Microsoft Print to PDF để xuất tệp.
    /// </summary>
    private void Print_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new PrintDialog();if(dialog.ShowDialog()!=true)return;
            TryPrint(p=>dialog.PrintDocument(p,$"Đơn thuốc {fixedId??(DataContext as RxCard)?.Code}"),new Size(dialog.PrintableAreaWidth,dialog.PrintableAreaHeight),out _);
        }
        catch(Exception){Feedback("Không mở được máy in. Có thể in lại cùng bản ký.");}
    }
}
