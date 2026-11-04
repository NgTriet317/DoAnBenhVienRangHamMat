using ET;
using System.Windows;
namespace RangHamMat.Views;
/// <summary>
/// Phần xử lý khám lâm sàng của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class KhamLamSang
{
    /// <summary>
    /// Điền các bước dịch vụ và thông tin hóa đơn vào phần tổng kết lần khám.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>data.DichVu.ToDictionary</c>, <c>data.KeHoach.Where</c>, <c>service.LayKeHoach</c>, <c>catalog.GetValueOrDefault</c>.</remarks>
    /// <param name="data">Giá trị dữ liệu dùng khi thực hiện thao tác; kiểu DuLieuKhongGianKham.</param>
    private void FillCompletionOverview(DuLieuKhongGianKham data)
    {
        var catalog=data.DichVu.ToDictionary(x=>x.MaDV,x=>x.TenDV);
        ExecutionPlanGrid.ItemsSource=data.KeHoach.Where(x=>x.TrangThai!="Hủy").SelectMany(p=>service.LayKeHoach(p.MaKHDT,actor).CacBuoc.Select(s=>new{
            DichVu=catalog.GetValueOrDefault(s.MaDV,s.MaDV),s.LanThu,s.SoLuongDuKien,s.DonGiaDuKien,ThanhTien=s.SoLuongDuKien*s.DonGiaDuKien,TrangThai=p.TrangThai})).ToArray();
        RefreshClinicalSummary();
        CompletionInvoiceText.Text=data.BanNhap.MaHD is null?"Kiểm tra danh sách rồi Chốt để ký đơn và lập hóa đơn chờ thanh toán.":$"Đã lập hóa đơn {data.BanNhap.MaHD}. Người bệnh thanh toán và nhận thuốc tại quầy.";
        OpenInvoiceButton.IsEnabled=data.BanNhap.MaHD is not null&&allowed("HoaDon");
    }
    /// <summary>
    /// Xử lý thao tác hóa đơn khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>OpenModule</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Invoice_Click(object sender,RoutedEventArgs e)=>OpenModule("HoaDon");
}
