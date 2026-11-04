using System.Windows;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
namespace RangHamMat.Views;
/// <summary>
/// Phần xử lý hóa đơn của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class HoaDon
{
    /// <summary>
    /// Trang dữ liệu nguồn được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private int sourcePage=1;
    /// <summary>
    /// Nguồn truy vấn được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private string sourceQuery="";
    /// <summary>
    /// Nguồn các lần khám được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private IReadOnlyList<ET.DongLanKhamDanhSach> sourceVisits=Array.Empty<ET.DongLanKhamDanhSach>();
    /// <summary>
    /// Nạp các lựa chọn lâm sàng theo quyền cho bộ lọc hóa đơn.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>QuyenNghiepVu.LayNguCanh</c>, <c>bus.LayTrangBenhNhan</c>, <c>bus.LayTrangLanKham</c>, <c>patients.CacDong.ToList</c>.</remarks>
    private void LoadClinicalSelectors()
    {
        FinanceSearchPanel.Visibility=Visibility.Visible;
        FinanceSourceExpander.IsExpanded=string.IsNullOrEmpty(visitId);
        var bus=new BUS.BUS_KHAMDIEUTRI();var actor=QuyenNghiepVu.LayNguCanh();
        var filter=new ET.BoLocDanhSachHoSo{MaBN=PatientId,TuKhoa=sourceQuery,Trang=sourcePage,KichThuocTrang=25,ChiCuaToi=false};
        var patients=bus.LayTrangBenhNhan(filter with{MaBN=null},actor);var visits=bus.LayTrangLanKham(filter,actor);
        var pages=Math.Max(1,(int)Math.Ceiling(Math.Max(patients.TongSo,visits.TongSo)/25d));
        if(sourcePage>pages){sourcePage=pages;filter=filter with{Trang=sourcePage};patients=bus.LayTrangBenhNhan(filter with{MaBN=null},actor);visits=bus.LayTrangLanKham(filter,actor);}
        var patientRows=patients.CacDong.ToList();var visitRows=visits.CacDong.ToList();
        if(!string.IsNullOrEmpty(PatientId)&&patientRows.All(r=>r.MaBN!=PatientId))patientRows.AddRange(bus.LayTrangBenhNhan(new(){MaBN=PatientId},actor).CacDong);
        if(!string.IsNullOrEmpty(visitId)&&visitRows.All(r=>r.MaLS!=visitId))visitRows.AddRange(bus.LayTrangLanKham(new(){MaBN=PatientId,MaLS=visitId,ChiCuaToi=false},actor).CacDong);
        PatientFilter.ItemsSource=new[]{new LuaChonDanhMuc("","Tất cả bệnh nhân")}.Concat(patientRows.Select(r=>new LuaChonDanhMuc(r.MaBN,$"{r.HoTenBN} ({r.MaBN})"))).ToArray();PatientFilter.SelectedValue=PatientId??"";
        sourceVisits=visitRows;
        VisitFilter.ItemsSource=new[]{new LuaChonDanhMuc("","Tất cả lần khám")}.Concat(visitRows.Select(r=>new LuaChonDanhMuc(r.MaLS,$"{r.HoTenBN} · {r.MaBN} · {r.MaLS} · {r.ThoiGianKham:dd/MM/yyyy}"))).ToArray();VisitFilter.SelectedValue=visitId??"";
        FinancePageText.Text=$"Trang {sourcePage}/{pages} · {patients.TongSo} BN / {visits.TongSo} lượt";PreviousFinancePage.IsEnabled=sourcePage>1;NextFinancePage.IsEnabled=sourcePage<pages;
    }
    /// <summary>
    /// Xử lý thao tác tài chính tìm kiếm khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanChangePricingContext</c>, <c>FinanceSearch.Text.Trim</c>, <c>Refresh</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void FinanceSearch_Click(object sender,RoutedEventArgs e)
    {if(!CanChangePricingContext())return;sourceQuery=FinanceSearch.Text.Trim();sourcePage=1;PatientId=visitId=prescriptionId=invoiceId=null;Refresh();}
    /// <summary>
    /// Xử lý thao tác trước tài chính khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanChangePricingContext</c>, <c>Refresh</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void PreviousFinance_Click(object sender,RoutedEventArgs e){if(!CanChangePricingContext())return;sourcePage=Math.Max(1,sourcePage-1);Refresh();}
    /// <summary>
    /// Xử lý thao tác tiếp tài chính khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanChangePricingContext</c>, <c>Refresh</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NextFinance_Click(object sender,RoutedEventArgs e){if(!CanChangePricingContext())return;sourcePage++;Refresh();}
    /// <summary>
    /// Đảm bảo chứng từ được chọn có ngữ cảnh lần khám hợp lệ trước khi mở thao tác liên quan.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DuLieuGiaoDien.BaoDamNguCanh</c>.</remarks>
    private void EnsureSelectedClinicalContext()
    {
        if(!DuLieuGiaoDien.PhamViLamSang||string.IsNullOrEmpty(PatientId))return;
        var prior=updating;updating=true;
        try{DuLieuGiaoDien.BaoDamNguCanh(new(){MaBN=PatientId,MaLS=visitId});}
        finally{updating=prior;}
    }
}
