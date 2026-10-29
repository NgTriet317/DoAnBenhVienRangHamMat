using System.Windows;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;
/// <summary>
/// Phần xử lý bệnh nhân của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class BenhNhan
{
    /// <summary>
    /// Nạp trang dữ liệu mới từ SQL theo bộ lọc đang áp dụng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Filter</c>, <c>HoTroGiaoDien.Choice</c>, <c>QuyenNghiepVu.LayNguCanh</c>, <c>bus.LayTrangBenhNhan</c>.</remarks>
    private void RefreshSqlPage()
    {
        if(refreshing)return;
        try
        {
            var selected=PatientId;
            string? Filter(string s)=>s=="Tất cả"?null:s;
            var query=new ET.BoLocDanhSachHoSo{MaBN=requestedPatientId,TuKhoa=SearchBox.Text,GioiTinh=Filter(HoTroGiaoDien.Choice(GenderFilter)),TrangThai=Filter(HoTroGiaoDien.Choice(StatusFilter)),Trang=currentPage,KichThuocTrang=PageSize};
            var bus=new BUS.BUS_KHAMDIEUTRI();var actor=QuyenNghiepVu.LayNguCanh();var page=bus.LayTrangBenhNhan(query,actor);
            var pages=Math.Max(1,(int)Math.Ceiling(page.TongSo/(double)PageSize));
            if(currentPage>pages){currentPage=pages;page=bus.LayTrangBenhNhan(query with{Trang=currentPage},actor);}
            filtered=page.CacDong.Cast<object>().ToArray();refreshing=true;
            Grid_BENH_NHAN.ItemsSource=page.CacDong.Select(r=>new BenhNhanDanhSachItem(r,r.LanKhamGanNhat)).ToArray();
            Grid_BENH_NHAN.SelectedItem=Grid_BENH_NHAN.Items.Cast<object>().FirstOrDefault(r=>UiRecord.Text(r,"MaBN")==selected);
            RecordCount.Text=$"{page.TongSo} bệnh nhân · chọn để mở hồ sơ";PageLabel.Text=$"{currentPage} / {pages}";
            PreviousButton.IsEnabled=currentPage>1;NextButton.IsEnabled=currentPage<pages;
            ListEmpty.Visibility=page.CacDong.Count==0?Visibility.Visible:Visibility.Collapsed;ListEmpty.Text="Không có bệnh nhân phù hợp với tìm kiếm và bộ lọc.";
        }
        catch(InvalidOperationException){Grid_BENH_NHAN.ItemsSource=Array.Empty<object>();ListEmpty.Text="Không tải được danh sách. Kiểm tra kết nối/quyền và tải lại.";ListEmpty.Visibility=Visibility.Visible;}
        finally{refreshing=false;}
    }
}
