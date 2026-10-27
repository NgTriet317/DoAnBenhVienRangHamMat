using System.Windows;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;
public partial class DonThuoc
{
    private void RefreshHistoryPage()
    {
        try
        {
            var actor=QuyenNghiepVu.LayNguCanh();var bus=new BUS.BUS_KHAMDIEUTRI();var status=HoTroGiaoDien.Choice(StatusFilter);
            var filter=new ET.BoLocDanhSachHoSo{MaBN=PatientId,MaLS=visitId,TuKhoa=SearchBox.Text,TrangThai=status=="Tất cả"?null:status,Trang=historyPage,KichThuocTrang=25};
            var page=bus.LayTrangLichSuDon(filter,actor);var pages=Math.Max(1,(int)Math.Ceiling(page.TongSo/25d));
            if(historyPage>pages){historyPage=pages;page=bus.LayTrangLichSuDon(filter with{Trang=historyPage},actor);}
            var rows=page.CacDong.Select(r=>new RxCard(r)).ToArray();PrescriptionList.ItemsSource=rows;PrescriptionList.SelectedItem=rows.FirstOrDefault(r=>r.Code==selectedId);
            RecordCount.Text=$"{page.TongSo} đơn thuốc · trang {historyPage}/{pages}";ListEmpty.Visibility=rows.Length==0?Visibility.Visible:Visibility.Collapsed;
            PrescriptionPages.Visibility=Visibility.Visible;PreviousPrescriptionPage.IsEnabled=historyPage>1;NextPrescriptionPage.IsEnabled=historyPage<pages;
        }
        catch(InvalidOperationException ex){PrescriptionList.ItemsSource=Array.Empty<object>();Feedback.Text=ClinicalFormInput.Message(ex);}
        finally{updating=false;}
        ShowSelected();
    }
    private void PreviousPrescription_Click(object sender,RoutedEventArgs e){historyPage=Math.Max(1,historyPage-1);Refresh();}
    private void NextPrescription_Click(object sender,RoutedEventArgs e){historyPage++;Refresh();}
}
