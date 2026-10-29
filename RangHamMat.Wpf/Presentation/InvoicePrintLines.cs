using System.Data;
using System.Globalization;
using RangHamMat.Controls;
namespace RangHamMat.Views;

public sealed record InvoicePrintLine(string Description, decimal SoLuong, decimal DonGia, decimal ThanhTien,
    IReadOnlyList<string> MaCTPX)
{
    public string Unit { get; init; } = "";
    public string Quantity => SoLuong.ToString("0.###", CultureInfo.GetCultureInfo("vi-VN"))+(Unit.Length==0?"":" "+Unit);
    public string Price => TreatmentMoney.Format(DonGia);
    public string Amount => TreatmentMoney.Format(ThanhTien);
    public string Trace => MaCTPX.Count == 0 ? "" : "Chi tiết xuất: " + string.Join(", ", MaCTPX);
}
public static class InvoicePrintLines
{
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
