using System.Windows;
using System.Windows.Documents;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;
public static class DonThuocDocument
{
    private static FlowDocument New()=>new(){FontFamily=new System.Windows.Media.FontFamily("Segoe UI"),FontSize=14,PagePadding=new Thickness(30),ColumnWidth=900};
    public static FlowDocument Tao(BanInDonThuoc p)
    {
        var doc=New();doc.Blocks.Add(new Paragraph(new Bold(new Run("ĐƠN THUỐC · "+p.Nguon.Don.MaDT))){FontSize=22});
        doc.Blocks.Add(new Paragraph(new Run($"Người bệnh: {p.HoTenBN} ({p.Nguon.MaBN})\nNgày sinh: {p.NgaySinh?.ToString("dd/MM/yyyy")??"chưa ghi nhận"}\nLượt: {p.Nguon.Don.MaLS} · Bác sĩ: {p.TenBS} ({p.Nguon.Don.MaBS})\nNgày kê: {p.Nguon.Don.NgayKe:dd/MM/yyyy HH:mm:ss}\nChẩn đoán: {p.ChanDoan}")));
        foreach(var d in p.Nguon.CacDong)doc.Blocks.Add(new Paragraph(new Run($"{d.TenThuocChot} · {d.HamLuongChot}\nLượng: {d.SoLuong:0.###} {d.DonViTinhChot} · {d.SoNgayDung} ngày\nLiều: {d.LieuDung} · Đường dùng: {d.DuongDung}\nHướng dẫn: {d.HuongDan}")));
        if(!string.IsNullOrWhiteSpace(p.Nguon.Don.GhiChu))doc.Blocks.Add(new Paragraph(new Run("Ghi chú: "+p.Nguon.Don.GhiChu)));
        return doc;
    }
    public static FlowDocument Tao(RxCard p)
    {
        var doc=New();doc.Blocks.Add(new Paragraph(new Bold(new Run("ĐƠN THUỐC · "+p.Code))){FontSize=22});
        doc.Blocks.Add(new Paragraph(new Run($"Người bệnh: {p.Patient}\nĐiện thoại: {p.Phone}\nNgày sinh: {p.BirthDate}\nNgày kê: {p.When}\nBác sĩ: {p.Doctor}\nChẩn đoán: {p.Diagnosis}")));
        foreach(var d in p.Lines)doc.Blocks.Add(new Paragraph(new Run($"{d.Label}\nLượng: {d.SoLuong} {d.Unit} · {d.SoNgayDung} ngày\nLiều: {d.LieuDung} · Đường dùng: {d.DuongDung}\nHướng dẫn: {d.HuongDan}")));
        if(!string.IsNullOrWhiteSpace(p.Note))doc.Blocks.Add(new Paragraph(new Run("Ghi chú: "+p.Note)));
        return doc;
    }
}
