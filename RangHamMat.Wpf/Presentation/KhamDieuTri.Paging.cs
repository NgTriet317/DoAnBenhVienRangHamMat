using System.Windows;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;
/// <summary>
/// Phần xử lý khám điều trị của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class KhamDieuTri
{
    /// <summary>
    /// Nạp trang lần khám từ SQL theo phạm vi và các bộ lọc đang chọn.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>QuyenNghiepVu.LayNguCanh</c>, <c>bus.LayTrangLanKham</c>, <c>page.CacDong.Select</c>, <c>rows.FirstOrDefault</c>.</remarks>
    private void RefreshSqlVisits()
    {
        try
        {
            var filter=new ET.BoLocDanhSachHoSo{MaBN=PatientId,MaLS=explicitVisitId,TuKhoa=SearchBox.Text,TrangThai="Đã xác nhận",TuNgay=FromDate.SelectedDate,DenNgay=ToDate.SelectedDate,Trang=visitPage,KichThuocTrang=25};
            var actor=QuyenNghiepVu.LayNguCanh();var bus=new BUS.BUS_KHAMDIEUTRI();var page=bus.LayTrangLanKham(filter,actor);
            var pages=Math.Max(1,(int)Math.Ceiling(page.TongSo/25d));
            if(visitPage>pages){visitPage=pages;page=bus.LayTrangLanKham(filter with{Trang=visitPage},actor);}
            var rows=page.CacDong.Select(r=>new MucDanhSachLanKham(r)).ToArray();VisitList.ItemsSource=rows;
            VisitList.SelectedItem=rows.FirstOrDefault(r=>r.Code==selectedVisitId);
            RecordCount.Text=$"{page.TongSo} lượt đã khám · trang {visitPage}/{pages}";
            PreviousVisitPage.IsEnabled=visitPage>1;NextVisitPage.IsEnabled=visitPage<pages;VisitPages.Visibility=Visibility.Visible;
            ListEmpty.Visibility=rows.Length==0?Visibility.Visible:Visibility.Collapsed;
            if(VisitList.SelectedItem is MucDanhSachLanKham selected)ShowVisit(selected);
            else{DetailHost.Content=null;DetailEmpty.Visibility=Visibility.Visible;ContextNote.Text=explicitVisitId is null?"Chọn đúng người bệnh và lần khám để tiếp tục.":"Không tìm thấy lần khám được yêu cầu trong phạm vi; không chọn lượt khác.";}
        }
        catch(InvalidOperationException){VisitList.ItemsSource=Array.Empty<object>();DetailHost.Content=null;DetailEmpty.Visibility=Visibility.Visible;ContextNote.Text="Không tải được danh sách lần khám. Kiểm tra kết nối/quyền và tải lại.";}
    }
    /// <summary>
    /// Xử lý thao tác trước lần khám khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanLeaveEditor</c>, <c>Refresh</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void PreviousVisit_Click(object sender,RoutedEventArgs e){if(!CanLeaveEditor())return;visitPage=Math.Max(1,visitPage-1);Refresh();}
    /// <summary>
    /// Xử lý thao tác tiếp lần khám khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanLeaveEditor</c>, <c>Refresh</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NextVisit_Click(object sender,RoutedEventArgs e){if(!CanLeaveEditor())return;visitPage++;Refresh();}
}
