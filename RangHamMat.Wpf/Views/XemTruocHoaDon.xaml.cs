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
    /// <summary>
    /// Mã được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly string code="";
    /// <summary>
    /// Đọc được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly Func<BanInHoaDon>? read;
    /// <summary>
    /// Nguồn in. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public BanInHoaDon? NguonIn {get;private set;}
    /// <summary>
    /// Khởi tạo xem trước hóa đơn. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    /// <param name="read">Giá trị đọc dùng khi thực hiện thao tác; kiểu Func&lt;BanInHoaDon&gt;.</param>
    public XemTruocHoaDon(Func<BanInHoaDon> read)
    {InitializeComponent();RangHamMat.Controls.ClinicalWorkflow.ClinicalWindowLifecycle.Attach(this);this.read=read;var initial=read();code=initial.HoaDon.MaHD;Apply(initial);}
    /// <summary>
    /// Kiểm tra nguồn bản in và thực hiện in bằng hộp thoại in của ứng dụng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>read</c>, <c>Apply</c>, <c>send</c>, <c>InvoiceDocument.CreatePaginator</c>.</remarks>
    /// <param name="send">Giá trị gửi dùng khi thực hiện thao tác; kiểu Action&lt;DocumentPaginator&gt;.</param>
    /// <param name="pageSize">Giá trị số dòng mỗi trang dùng khi thực hiện thao tác; kiểu Size.</param>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool TryPrint(Action<DocumentPaginator> send,Size pageSize,out string? error)
    {
        error=null;try
        {
            if(read is not null){var source=read();if(source.HoaDon.MaHD!=code)throw new LoiKhamException("MaHD","Nguồn in đã đổi mã hóa đơn; đóng cửa sổ và mở lại đúng chứng từ.");Apply(source);}
            send(InvoiceDocument.CreatePaginator(pageSize));PrintStatus.Text="Đã gửi hóa đơn #"+code+" tới máy in đã chọn.";return true;
        }
        catch(Exception ex){error=ex is LoiKhamException or TuChoiQuyenNghiepVuException?ex.Message:"Không thể in hóa đơn #"+code+". In lại cùng chứng từ; kết quả thu tiền giữ nguyên.";PrintStatus.Text=error;return false;}
    }
    /// <summary>
    /// Áp dụng dữ liệu hoặc các thiết lập hiển thị cho thành phần theo các bước trong thân hàm.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>lines.Columns.Add</c>, <c>lines.Rows.Add</c>, <c>h.NgayLap.ToString</c>, <c>Money</c>.</remarks>
    /// <param name="p">Giá trị p dùng khi thực hiện thao tác; kiểu BanInHoaDon.</param>
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
        InitializeComponent();RangHamMat.Controls.ClinicalWorkflow.ClinicalWindowLifecycle.Attach(this); code = Convert.ToString(header["MaHD"]) ?? "";
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
