using ET;
using System.Windows;
namespace RangHamMat.Views;
public partial class KhamLamSang
{
    private void FillCompletionOverview(DuLieuKhongGianKham data)
    {
        var catalog=data.DichVu.ToDictionary(x=>x.MaDV,x=>x.TenDV);
        CompletionServicesList.ItemsSource=data.KeHoach.Where(x=>x.TrangThai!="Hủy").SelectMany(p=>service.LayKeHoach(p.MaKHDT,actor).CacBuoc.Select(s=>new{
            DichVu=catalog.GetValueOrDefault(s.MaDV,s.MaDV),s.LanThu,s.SoLuongDuKien,s.DonGiaDuKien,ThanhTien=s.SoLuongDuKien*s.DonGiaDuKien,TrangThai=p.TrangThai})).ToArray();
        CompletionInvoiceText.Text=data.BanNhap.MaHD is null?"Hoàn tất sẽ ghi nhận các dịch vụ đã làm và lập hóa đơn chờ thu ngay trong lần khám này.":$"Đã lập hóa đơn {data.BanNhap.MaHD}. Mở Hóa đơn để xem và thanh toán.";
        OpenInvoiceButton.IsEnabled=data.BanNhap.MaHD is not null&&allowed("HoaDon");
    }
    private void Invoice_Click(object sender,RoutedEventArgs e)=>OpenModule("HoaDon");
}
