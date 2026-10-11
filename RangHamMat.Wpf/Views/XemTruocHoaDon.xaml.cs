using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RangHamMat.Controls;

namespace RangHamMat.Views;

public partial class XemTruocHoaDon : Window
{
    private readonly string code;
    public XemTruocHoaDon(DataRow header, DataTable lines)
    {
        InitializeComponent(); code = Convert.ToString(header["MaHD"]) ?? "";
        InvoiceDocument.DataContext = new
        {
            Code = $"Hóa đơn #{code}", Status = Convert.ToString(header["TrangThai"]), Patient = $"{header["HoTenBN"]} · Mã {header["MaBN"]}", Visit = Convert.ToString(header["MaLS"]),
            Date = Convert.ToDateTime(header["NgayLap"]).ToString("dd/MM/yyyy HH:mm"), Employee = Convert.ToString(header["NguoiLap"]),
            Total = Money(header["TongTien"]), Discount = Money(header["GiamGia"]), Due = Money(header["SoTienPhaiThu"]), Collected = Money(header["DaThu"]), Remaining = Money(header["ConPhaiThu"])
        };
        foreach (DataRow line in lines.Rows)
        {
            var row = new TableRow();
            row.Cells.Add(Cell(Convert.ToString(line["NoiDung"]) ?? ""));
            row.Cells.Add(Cell(Convert.ToDecimal(line["SoLuong"]).ToString("0.###", CultureInfo.GetCultureInfo("vi-VN"))));
            row.Cells.Add(Cell(Money(line["DonGiaChot"]))); row.Cells.Add(Cell(Money(line["ThanhTien"])));
            InvoiceLines.RowGroups[0].Rows.Add(row);
        }
    }
    private static string Money(object value) => TreatmentMoney.Format(Convert.ToDecimal(value));
    private static TableCell Cell(string value) => new(new Paragraph(new Run(value)) { Margin = new Thickness(0) })
    { Padding = new Thickness(8), BorderBrush = new SolidColorBrush(Color.FromRgb(217, 226, 237)), BorderThickness = new Thickness(0, 0, 0, 1) };
    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog(); if (dialog.ShowDialog() != true) return;
        try
        {
            InvoiceDocument.PageWidth = dialog.PrintableAreaWidth; InvoiceDocument.PageHeight = dialog.PrintableAreaHeight;
            dialog.PrintDocument(((IDocumentPaginatorSource)InvoiceDocument).DocumentPaginator, $"Hóa đơn {code}"); PrintStatus.Text = "Đã gửi hóa đơn tới máy in đã chọn.";
        }
        catch (Exception ex) { PrintStatus.Text = "Không thể in hóa đơn: " + ex.Message; }
    }
}
