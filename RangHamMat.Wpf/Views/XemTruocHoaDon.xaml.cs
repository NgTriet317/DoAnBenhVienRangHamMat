// PHẦN AI ĐÃ THỰC HIỆN: AI đã đổi phần xem trước/in sang binding InvoiceDocument.xaml, chuẩn bị dữ liệu đầu hóa đơn/dòng dịch vụ/tổng tiền và dùng bộ phân trang; đã thay phần dựng TableRow/TableCell bằng C#.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>
/// Màn hình xem trước/in hóa đơn nhiều trang: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class XemTruocHoaDon : Window
{
    private readonly string code;
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
            Lines = lines.Rows.Cast<DataRow>().Select(line => new
            {
                Description = Convert.ToString(line["NoiDung"]) ?? "",
                Quantity = Convert.ToDecimal(line["SoLuong"]).ToString("0.###", CultureInfo.GetCultureInfo("vi-VN")),
                Price = Money(line["DonGiaChot"]), Amount = Money(line["ThanhTien"])
            }).ToArray()
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
        var dialog = new PrintDialog(); if (dialog.ShowDialog() != true) return;
        try
        {
            var paginator = InvoiceDocument.CreatePaginator(new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight));
            dialog.PrintDocument(paginator, $"Hóa đơn {code}"); PrintStatus.Text = "Đã gửi hóa đơn tới máy in đã chọn.";
        }
        catch (Exception ex) { PrintStatus.Text = "Không thể in hóa đơn: " + ex.Message; }
    }
}
