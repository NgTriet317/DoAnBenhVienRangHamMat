using System.Windows;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;
/// <summary>
/// Phần xử lý hồ sơ bệnh nhân của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class HoSoBenhNhan
{
    /// <summary>
    /// Trang lịch sử hiện tại được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private int historyPage=1;
    /// <summary>
    /// Nạp trang lịch sử từ SQL và đồng bộ lựa chọn trên giao diện.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>QuyenNghiepVu.LayNguCanh</c>, <c>bus.LayTrangLanKham</c>, <c>page.CacDong.Select</c>, <c>Array.Empty&lt;object&gt;</c>.</remarks>
    private void RefreshHistoryPage()
    {
        try
        {
            if(string.IsNullOrEmpty(PatientId))return;
            var bus=new BUS.BUS_KHAMDIEUTRI();var actor=QuyenNghiepVu.LayNguCanh();var filter=new ET.BoLocDanhSachHoSo{MaBN=PatientId,ChiCuaToi=false,Trang=historyPage,KichThuocTrang=25};
            var page=bus.LayTrangLanKham(filter,actor);var pages=Math.Max(1,(int)Math.Ceiling(page.TongSo/25d));
            if(historyPage>pages){historyPage=pages;page=bus.LayTrangLanKham(filter with{Trang=historyPage},actor);}
            HistoryGrid.ItemsSource=page.CacDong.Select(r=>new PatientVisitRow(r)).ToArray();HistoryCount.Text=$"{page.TongSo} lần khám · trang {historyPage}/{pages} · BN {PatientId}";
            HistoryEmpty.Visibility=page.CacDong.Count==0?Visibility.Visible:Visibility.Collapsed;
            HistoryPages.Visibility=Visibility.Visible;PreviousHistoryPage.IsEnabled=historyPage>1;NextHistoryPage.IsEnabled=historyPage<pages;
        }
        catch(InvalidOperationException){HistoryGrid.ItemsSource=Array.Empty<object>();HistoryCount.Text="Không tải được lịch sử. Kiểm tra kết nối/quyền và tải lại.";}
    }
    /// <summary>
    /// Xử lý thao tác trước lịch sử khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>RefreshProfile</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void PreviousHistory_Click(object sender,RoutedEventArgs e){historyPage=Math.Max(1,historyPage-1);RefreshProfile();}
    /// <summary>
    /// Xử lý thao tác tiếp lịch sử khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>RefreshProfile</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NextHistory_Click(object sender,RoutedEventArgs e){historyPage++;RefreshProfile();}
}
