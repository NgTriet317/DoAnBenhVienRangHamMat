using System.Windows;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;
public partial class HoSoBenhNhan
{
    private int historyPage=1;
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
    private void PreviousHistory_Click(object sender,RoutedEventArgs e){historyPage=Math.Max(1,historyPage-1);RefreshProfile();}
    private void NextHistory_Click(object sender,RoutedEventArgs e){historyPage++;RefreshProfile();}
}
