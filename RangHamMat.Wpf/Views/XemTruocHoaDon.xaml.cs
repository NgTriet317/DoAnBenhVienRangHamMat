// PHẦN AI ĐÃ THỰC HIỆN: AI đã đổi phần xem trước/in sang binding InvoiceDocument.xaml, chuẩn bị dữ liệu đầu hóa đơn/dòng dịch vụ/tổng tiền và dùng bộ phân trang; đã thay phần dựng TableRow/TableCell bằng C#.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
using ET;
using System.Windows.Documents;

namespace RangHamMat.Views;

/// <summary>
/// Màn hình xem trước/in hóa đơn nhiều trang: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class XemTruocHoaDon : Window
{
    private readonly string code="";
    private readonly Func<BanInHoaDon>? read;
    public BanInHoaDon? NguonIn {get;private set;}
    public XemTruocHoaDon(Func<BanInHoaDon> read)
    {InitializeComponent();this.read=read;var initial=read();code=initial.HoaDon.MaHD;Apply(initial);}
    public bool TryPrint(Action<DocumentPaginator> send,Size pageSize,out string? error)
    {
        error=null;try
        {
            if(read is not null){var source=read();if(source.HoaDon.MaHD!=code)throw new LoiKhamException("MaHD","Nguồn in đã đổi mã hóa đơn; đóng cửa sổ và mở lại đúng chứng từ.");Apply(source);}
            send(InvoiceDocument.CreatePaginator(pageSize));PrintStatus.Text="Đã gửi hóa đơn #"+code+" tới máy in đã chọn.";return true;
        }
        catch(Exception ex){error=ex is LoiKhamException or TuChoiQuyenNghiepVuException?ex.Message:"Không thể in hóa đơn #"+code+". In lại cùng chứng từ; kết quả thu tiền giữ nguyên.";PrintStatus.Text=error;return false;}
    }
    private void Apply(BanInHoaDon p)
    {
        NguonIn=p;var h=p.HoaDon;var lines=new DataTable();foreach(var name in new[]{"MaCTHD","MaCTPX","MaCTDT","MaDT","NoiDung","DonViTinh"})lines.Columns.Add(name);foreach(var name in new[]{"SoLuong","DonGiaChot","ThanhTien"})lines.Columns.Add(name,typeof(decimal));
        foreach(var d in p.CacDong)lines.Rows.Add(d.MaCTHD,d.MaCTPX,d.MaCTDT,d.MaDT,d.NoiDung,d.DonViTinh,d.SoLuong,d.DonGia,d.ThanhTien);
        InvoiceDocument.DataContext=new{Code=(h.TrangThai=="Nháp"?"Bản nháp hóa đơn #":"Hóa đơn #")+h.MaHD,Status=h.TrangThai,Patient=$"{p.HoTenBN} · Mã {p.MaBN}",Visit=h.MaLS,Date=h.NgayLap.ToString("dd/MM/yyyy HH:mm:ss"),Employee=p.NguoiLap,
            Total=Money(h.TongTien),Discount=Money(h.GiamGia),Due=Money(h.SoTienPhaiThu),Collected=Money(p.DaThu),Remaining=Money(p.ConPhaiThu),
            CashInfo=p.SoTienKhachDua is decimal given?$"Khách đưa: {Money(given)} · Tiền thối: {Money(p.TienThua??0)}\n{p.PhuongThuc}"+(string.IsNullOrEmpty(p.MaThamChieu)?"":" · Tham chiếu: "+p.MaThamChieu):"",Lines=InvoicePrintLines.Group(lines)};
    }
    /// <summary>
    /// AI hỗ trợ: nạp chứng từ XAML và dữ liệu đầu/chi tiết hóa đơn; các dòng binding vào ItemsControl, không tạo TableRow bằng C#.
    /// </summary>
    public XemTruocHoaDon(DataRow header, DataTable lines)
    {
        InitializeComponent(); code = Convert.ToString(header["MaHD"]) ?? "";
        InvoiceDocument.DataContext = new
        {
            Code = $"Hóa đơn #{code}", Status = Convert.ToString(header["TrangThai"]), Patient = $"{header["HoTenBN"]} · Mã {header["MaBN"]}", Visit = Convert.ToString(header["MaLS"]),
            Date = Convert.ToDateTime(header["NgayLap"]).ToString("dd/MM/yyyy HH:mm"), Employee = Convert.ToString(header["NguoiLap"]),
            Total = Money(header["TongTien"]), Discount = Money(header["GiamGia"]), Due = Money(header["SoTienPhaiThu"]), Collected = Money(header["DaThu"]), Remaining = Money(header["ConPhaiThu"]),
            CashInfo="", Lines = InvoicePrintLines.Group(lines)
        };
    }
    /// <summary>
    /// Định dạng giá trị tiền của chứng từ bằng TreatmentMoney.Format để bản in thống nhất với màn hình.
    /// </summary>
    private static string Money(object value) => TreatmentMoney.Format(Convert.ToDecimal(value));
    /// <summary>
    /// AI hỗ trợ: mở hộp thoại máy in và phân trang visual XAML theo vùng in, giữ tất cả dòng và mô tả dài khi xuất PDF.
    /// </summary>
    private void Print_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new PrintDialog(); if (dialog.ShowDialog() != true) return;
            TryPrint(p=>dialog.PrintDocument(p,$"Hóa đơn {code}"),new Size(dialog.PrintableAreaWidth,dialog.PrintableAreaHeight),out _);
        }
        catch (Exception) { PrintStatus.Text = "Không mở được máy in. In lại cùng hóa đơn; kết quả thu tiền giữ nguyên."; }
    }
}
