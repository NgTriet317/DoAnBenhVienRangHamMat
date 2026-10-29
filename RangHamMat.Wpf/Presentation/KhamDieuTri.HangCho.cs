using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Views;

public partial class KhamDieuTri
{
    private BoLocHangDoi QueueFilter()=>new(){MaBN=PatientId,MaBS=HoTroGiaoDien.Selected(DoctorFilter) is {Length:>0} id?id:null,
        MaPhieu=explicitReceiptId,TuNgay=FromDate.SelectedDate,DenNgay=ToDate.SelectedDate,TuKhoa=SearchBox.Text,
        TrangThai=HoTroGiaoDien.Choice(StatusFilter) is "Tất cả"?null:HoTroGiaoDien.Choice(StatusFilter),Trang=queuePage,KichThuocTrang=25};
    private void RefreshQueue()
    {
        if(!QueueMode||EditorDirty||configurationOpen||!DuLieuGiaoDien.PhamViLamSang)return;
        var prior=updating;updating=true;
        try
        {
            var bus=new BUS_KHAMDIEUTRI();var actor=QuyenNghiepVu.LayNguCanh();var page=bus.LayTrangHangDoi(QueueFilter(),actor);
            var pages=Math.Max(1,(int)Math.Ceiling(page.TongSo/25d));
            if(queuePage>pages){queuePage=pages;page=bus.LayTrangHangDoi(QueueFilter(),actor);}
            QueueList.ItemsSource=page.CacDong;QueueList.SelectedItem=page.CacDong.FirstOrDefault(r=>r.MaPhieu==selectedReceiptId);
            QueueCount.Text=$"{page.TongSo} người bệnh chờ / đang khám";QueuePageText.Text=$"{queuePage}/{pages}";
            PreviousQueuePage.IsEnabled=queuePage>1;NextQueuePage.IsEnabled=queuePage<pages;QueueEmpty.Visibility=page.TongSo==0?Visibility.Visible:Visibility.Collapsed;
            if(QueueList.SelectedItem is HangDoiKhamItem selected){if(DetailHost.Content is null)PreviewReceipt(selected);}
            else if(DetailHost.Content is not ChiTietKhamDieuTri)
            {
                if(selectedReceiptId is not null)PageStatus.Text="Phiếu được chọn không còn trong hàng chờ/bộ lọc. Chọn lại đúng người bệnh; không chuyển sang người khác.";
                selectedReceiptId=null;ClearDetail();
            }
        }
        catch(InvalidOperationException ex){QueueList.ItemsSource=Array.Empty<HangDoiKhamItem>();QueueStartButton.IsEnabled=false;PageStatus.Text=ClinicalFormInput.Message(ex);}
        finally{updating=prior;}
    }
    private void Queue_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(updating||!QueueMode)return;
        if(!CanLeaveEditor()){updating=true;QueueList.SelectedItem=QueueList.Items.OfType<HangDoiKhamItem>().FirstOrDefault(r=>r.MaPhieu==selectedReceiptId);updating=false;return;}
        if(QueueList.SelectedItem is HangDoiKhamItem row){selectedReceiptId=row.MaPhieu;selectedVisitId=null;ClearDetail();PreviewReceipt(row);}
    }
    private void PreviewReceipt(HangDoiKhamItem row)
    {
        QueuePatientText.Text=row.HoTenBN;QueueReceiptText.Text=$"{row.MaBN} · phiếu {row.MaPhieu} · số {row.SoThuTu} · sinh {row.NgaySinh:dd/MM/yyyy}";
        QueueArrivalText.Text=$"Tiếp nhận: {row.ThoiGianTiepNhan:dd/MM/yyyy HH:mm:ss} · {row.TrangThai}";
        QueueDoctorText.Text=$"{row.TenBS} ({row.MaBS})\n{row.TenPhong} ({row.MaPhong})\nDịch vụ: {row.TenDV}";QueueReasonText.Text=row.LyDoKham;
        QueueStartButton.Content=row.ThaoTac;QueueStartButton.IsEnabled=allowed("KhamDieuTri");QueuePreview.Visibility=Visibility.Visible;DetailEmpty.Visibility=Visibility.Collapsed;
    }
    private void StartQueue_Click(object sender,RoutedEventArgs e)
    {
        if(!CanLeaveEditor()||!allowed("KhamDieuTri")||QueueList.SelectedItem is not HangDoiKhamItem row)return;
        string? started=null;var prior=updating;updating=true;
        try
        {
            var actor=QuyenNghiepVu.LayNguCanh();var bus=new BUS_KHAMDIEUTRI();
            if(bus.LayTrangHangDoi(new(){MaPhieu=row.MaPhieu,MaBN=row.MaBN},actor).TongSo!=1)throw new InvalidOperationException("Phiếu không còn chờ khám của bác sĩ hiện tại. Tải lại để đối chiếu.");
            started=bus.BatDauKham(row.MaPhieu,actor);selectedReceiptId=row.MaPhieu;
            DuLieuGiaoDien.BaoDamNguCanh(new(){MaBN=row.MaBN,MaLS=started});
            // Editor đã mở có định danh riêng, không phụ thuộc việc dòng còn nằm trong bộ lọc/trang.
            var detail=bus.LayChiTiet(started,actor);ShowVisit(new MucDanhSachLanKham(detail.LanKham));RefreshQueue();
            PageStatus.Text=DetailHost.Content is ChiTietKhamDieuTri?$"Đang khám {row.HoTenBN} · {row.MaBN} · lượt {started}.":$"Đã bắt đầu lượt {started}; tải lại để mở tiếp đúng phiếu.";
        }
        catch(InvalidOperationException ex)
        {
            var message=started is null?ClinicalFormInput.Message(ex):$"Đã bắt đầu lượt {started}. Tải lại để mở tiếp đúng phiếu. {ClinicalFormInput.Message(ex)}";
            if(started is null)RefreshQueue();
            PageStatus.Text=message;
        }
        finally{updating=prior;}
    }
    private void PreviousQueue_Click(object sender,RoutedEventArgs e){if(!CanLeaveEditor())return;queuePage=Math.Max(1,queuePage-1);RefreshQueue();}
    private void NextQueue_Click(object sender,RoutedEventArgs e){if(!CanLeaveEditor())return;queuePage++;RefreshQueue();}
}
