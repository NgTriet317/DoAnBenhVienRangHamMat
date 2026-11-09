using System.Data;
using System.Globalization;
using RangHamMat.Controls;
namespace RangHamMat.Views;

/// <summary>
/// Gói dữ liệu hóa đơn in dòng truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="Description">Giá trị mô tả dùng khi thực hiện thao tác; kiểu string.</param>
/// <param name="SoLuong">Giá trị số lượng dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="DonGia">Giá trị đơn giá dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="ThanhTien">Giá trị thành tiền dùng khi thực hiện thao tác; kiểu decimal.</param>
/// <param name="MaCTPX">Giá trị mã chi tiết phiếu xuất dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;string&gt;.</param>
public sealed record InvoicePrintLine(string Description, decimal SoLuong, decimal DonGia, decimal ThanhTien,
    IReadOnlyList<string> MaCTPX)
{
    /// <summary>
    /// Đơn vị.
    /// </summary>
    public string Unit { get; init; } = "";
    /// <summary>
    /// Giá trị Số lượng; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string Quantity => SoLuong.ToString("0.###", CultureInfo.GetCultureInfo("vi-VN"))+(Unit.Length==0?"":" "+Unit);
    /// <summary>
    /// Giá trị Đơn giá; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string Price => TreatmentMoney.Format(DonGia);
    /// <summary>
    /// Giá trị Số tiền; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string Amount => TreatmentMoney.Format(ThanhTien);
    /// <summary>
    /// Giá trị Dấu vết; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string Trace => MaCTPX.Count == 0 ? "" : "Chi tiết xuất: " + string.Join(", ", MaCTPX);
}
/// <summary>
/// Phần xử lý hóa đơn in các dòng của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public static class InvoicePrintLines
{
    /// <summary>
    /// Nhóm các dòng nguồn thành các nhóm hiển thị/in theo quy tắc của tài liệu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>r.Table.Columns.Contains</c>, <c>lines.Rows.Cast&lt;DataRow&gt;</c>, <c>Text</c>, <c>g.Sum</c>.</remarks>
    /// <param name="lines">Giá trị các dòng dùng khi thực hiện thao tác; kiểu DataTable.</param>
    /// <returns>Tập dữ liệu kiểu IReadOnlyList&lt;InvoicePrintLine&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
    public static IReadOnlyList<InvoicePrintLine> Group(DataTable lines)
    {
        static string Text(DataRow r, string key) => r.Table.Columns.Contains(key) ? Convert.ToString(r[key]) ?? "" : "";
        // Chỉ gộp các phần lô cùng dòng chỉ định và giá chốt; dịch vụ/nguồn cũ thiếu mã giữ riêng từng dòng.
        return lines.Rows.Cast<DataRow>().Select((r, i) => new
        {
            Row = r,
            Source = string.IsNullOrEmpty(Text(r, "MaCTDT")) ? "row:" + i : "rx:" + Text(r, "MaDT") + ":" + Text(r, "MaCTDT"),
            Description = Text(r, "NoiDung"), Price = Convert.ToDecimal(r["DonGiaChot"]),Unit=Text(r,"DonViTinh")
        }).GroupBy(r => (r.Source, r.Description, r.Price,r.Unit)).Select(g => new InvoicePrintLine(
            g.Key.Description, g.Sum(r => Convert.ToDecimal(r.Row["SoLuong"])), g.Key.Price,
            g.Sum(r => Convert.ToDecimal(r.Row["ThanhTien"])),
            g.Select(r => Text(r.Row, "MaCTPX")).Where(id => id.Length > 0).Distinct().ToArray()){Unit=g.Key.Unit}).ToArray();
    }
}
