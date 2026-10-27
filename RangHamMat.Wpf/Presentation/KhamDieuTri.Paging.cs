using System.Windows;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;
public partial class KhamDieuTri
{
    private void RefreshSqlVisits()
    {
        try
        {
            var status=HoTroGiaoDien.Choice(StatusFilter);
            var filter=new ET.BoLocDanhSachHoSo{MaBN=PatientId,MaLS=explicitVisitId,TuKhoa=SearchBox.Text,TrangThai=status=="Tất cả"?null:status,TuNgay=FromDate.SelectedDate,DenNgay=ToDate.SelectedDate,Trang=visitPage,KichThuocTrang=25};
            var actor=QuyenNghiepVu.LayNguCanh();var bus=new BUS.BUS_KHAMDIEUTRI();var page=bus.LayTrangLanKham(filter,actor);
            var pages=Math.Max(1,(int)Math.Ceiling(page.TongSo/25d));
            if(visitPage>pages){visitPage=pages;page=bus.LayTrangLanKham(filter with{Trang=visitPage},actor);}
            var rows=page.CacDong.Select(r=>new MucDanhSachLanKham(r)).ToArray();VisitList.ItemsSource=rows;
            VisitList.SelectedItem=rows.FirstOrDefault(r=>r.Code==selectedVisitId);
            RecordCount.Text=$"{page.TongSo} lần khám · trang {visitPage}/{pages}";
            PreviousVisitPage.IsEnabled=visitPage>1;NextVisitPage.IsEnabled=visitPage<pages;VisitPages.Visibility=Visibility.Visible;
            ListEmpty.Visibility=rows.Length==0?Visibility.Visible:Visibility.Collapsed;
            if(VisitList.SelectedItem is MucDanhSachLanKham selected)ShowVisit(selected);
            else{DetailHost.Content=null;DetailEmpty.Visibility=Visibility.Visible;ContextNote.Text=explicitVisitId is null?"Chọn đúng người bệnh và lần khám để tiếp tục.":"Không tìm thấy lần khám được yêu cầu trong phạm vi; không chọn lượt khác.";}
        }
        catch(InvalidOperationException){VisitList.ItemsSource=Array.Empty<object>();DetailHost.Content=null;DetailEmpty.Visibility=Visibility.Visible;ContextNote.Text="Không tải được danh sách lần khám. Kiểm tra kết nối/quyền và tải lại.";}
    }
    private void PreviousVisit_Click(object sender,RoutedEventArgs e){if(!CanLeaveEditor())return;visitPage=Math.Max(1,visitPage-1);Refresh();}
    private void NextVisit_Click(object sender,RoutedEventArgs e){if(!CanLeaveEditor())return;visitPage++;Refresh();}
}
