// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ bộ lọc/phân trang bệnh nhân, mở hồ sơ đúng mã và chuyển vào bệnh án/lần khám; đã chú thích sự kiện lựa chọn và làm mới dữ liệu.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>
/// Màn hình danh sách bệnh nhân và hồ sơ: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class BenhNhan : UserControl, IPatientScoped
{
    /// <summary>
    /// Số dòng mỗi trang được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private const int PageSize = 15;
    /// <summary>
    /// Cờ Sẵn sàng; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool ready;
    /// <summary>
    /// Cờ Đang làm mới; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool refreshing;
    /// <summary>
    /// Trang hiện tại được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private int currentPage = 1;
    /// <summary>
    /// Đã lọc được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private object[] filtered = Array.Empty<object>();
    /// <summary>
    /// Giá trị lưu nội bộ của thuộc tính PatientId, dùng bởi getter/setter tương ứng.
    /// </summary>
    private HoSoBenhNhan? profile;
    /// <summary>
    /// Quyền truy cập được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private Func<string, bool>? access;
    /// <summary>
    /// Được yêu cầu mã bệnh nhân được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private string? requestedPatientId;
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string? PatientId => profile?.PatientId;

    /// <summary>
    /// Khởi tạo danh sách bệnh nhân và hồ sơ từ XAML. Theo dõi cache khi Loaded và làm mới nội dung khi dữ liệu thay đổi; gỡ theo dõi khi Unloaded.
    /// </summary>
    public BenhNhan()
    {
        InitializeComponent();
        ready = true;
        HoTroGiaoDien.Observe(this, Refresh);
        Refresh();
    }

    /// <summary>
    /// Nhận hàm kiểm tra quyền phân hệ từ màn hình cha, lưu lại và áp dụng cho các tab/nút liên quan trong control hiện tại.
    /// </summary>
    public void ConfigureAccess(Func<string, bool> allowed)
    {
        access = allowed;
        AddPatientButton.IsEnabled = allowed("BenhNhan") && Controls.ClinicalWorkflow.QuyenNghiepVu.DuocTao("BENH_NHAN");
        profile?.ConfigureAccess(allowed);
    }

    /// <summary>
    /// Tạo HoSoBenhNhan cho bản ghi được chọn, truyền quyền truy cập và đặt vào vùng chi tiết của trang danh sách.
    /// </summary>
    public void OpenProfile(object record, Func<string, bool>? allowed = null)
    {
        if (allowed is not null) access = allowed;
        var id = UiRecord.Text(record, "MaBN");
        try { DuLieuGiaoDien.BaoDamNguCanh(new(){MaBN=id}); }
        catch(InvalidOperationException ex){ClearProfile(ex.Message);return;}
        var patient = DuLieuGiaoDien.Find("BENH_NHAN", id);
        if (patient is null) { ClearProfile("Bệnh nhân không còn trong dữ liệu. Chọn một bệnh nhân khác."); return; }
        if (profile?.PatientId != id)
        {
            profile = new HoSoBenhNhan(patient);
            ProfileHost.Content = profile;
        }
        if (access is not null) profile.ConfigureAccess(access);
        ProfileHost.Visibility = Visibility.Visible;
        ProfileEmpty.Visibility = Visibility.Collapsed;
        refreshing = true;
        Grid_BENH_NHAN.SelectedItem = Grid_BENH_NHAN.Items.Cast<object>().FirstOrDefault(row => UiRecord.Text(row, "MaBN") == id);
        refreshing = false;
    }

    /// <summary>
    /// Chọn đúng bệnh nhân theo mã nguồn và mở bệnh án/lần khám tương ứng nếu có; mã không tìm thấy không tự chọn bệnh nhân khác.
    /// </summary>
    public void FocusContext(string? patientId, string? visitId = null, string? prescriptionId = null)
    {
        requestedPatientId=patientId;
        var patient = DuLieuGiaoDien.Find("BENH_NHAN", patientId);
        if (patient is null)
        {
            ClearProfile(string.IsNullOrWhiteSpace(patientId) ? "Chọn một bệnh nhân để mở hồ sơ." : "Không tìm thấy bệnh nhân được yêu cầu. Chọn lại từ danh sách.");
            return;
        }
        // Xóa các bộ lọc cũ để bệnh nhân được yêu cầu luôn xuất hiện trong danh sách.
        refreshing = true;
        SearchBox.Clear(); GenderFilter.SelectedIndex = 0; StatusFilter.SelectedIndex = 0;
        refreshing = false;
        Refresh();
        var index = Array.FindIndex(filtered, row => UiRecord.Text(row, "MaBN") == patientId);
        currentPage = index < 0 ? 1 : index / PageSize + 1;
        Refresh(); OpenProfile(patient);
        if (!string.IsNullOrWhiteSpace(prescriptionId))
        {
            var prescription = DuLieuGiaoDien.Find("DON_THUOC", prescriptionId);
            if (prescription is null || DuLieuGiaoDien.PatientId("DON_THUOC", prescription) != PatientId)
            { MessageBox.Show("Đơn thuốc không tồn tại hoặc không thuộc bệnh nhân này.", "Hồ sơ bệnh nhân"); return; }
            DieuHuongHoSo.Open(this, "DonThuoc", PatientId!, UiRecord.Text(prescription, "MaLS"), prescriptionId);
            return;
        }
        if (!string.IsNullOrWhiteSpace(visitId)) profile?.OpenVisit(visitId);
    }

    /// <summary>
    /// Yêu cầu control hồ sơ đang mở chuyển sang tab bệnh án.
    /// </summary>
    public void OpenMedicalRecord() => profile?.OpenMedicalRecord();

    /// <summary>
    /// Gỡ hồ sơ đang mở và hiển thị lại trạng thái chưa chọn bệnh nhân.
    /// </summary>
    private void ClearProfile(string message)
    {
        profile = null; ProfileHost.Content = null; ProfileHost.Visibility = Visibility.Collapsed;
        ProfileEmpty.Visibility = Visibility.Visible; ProfileEmptyText.Text = message;
        refreshing = true; Grid_BENH_NHAN.SelectedItem = null; refreshing = false;
    }

    /// <summary>
    /// Lọc và phân trang bệnh nhân, giữ hồ sơ đang chọn nếu còn trong dữ liệu; cập nhật số lượng, điều hướng trang và trạng thái danh sách trống.
    /// </summary>
    private void Refresh()
    {
        if (!ready) return;
        if(DuLieuGiaoDien.PhamViLamSang){RefreshSqlPage();return;}
        var selected = PatientId;
        filtered = DuLieuGiaoDien.Rows("BENH_NHAN").Where(row =>
            HoTroGiaoDien.Matches("BENH_NHAN", row, "", HoTroGiaoDien.Choice(StatusFilter)) &&
            (HoTroGiaoDien.Matches("BENH_NHAN",row,SearchBox.Text,"Tất cả") || Controls.ClinicalWorkflow.HoSoViewModel.KhopNgaySinh(row,SearchBox.Text)) &&
            (HoTroGiaoDien.Choice(GenderFilter) == "Tất cả" || UiRecord.Text(row, "GioiTinh") == HoTroGiaoDien.Choice(GenderFilter)))
            .OrderBy(row => UiRecord.Text(row, "HoTenBN"), StringComparer.CurrentCultureIgnoreCase).ToArray();
        var pages = Math.Max(1, (int)Math.Ceiling(filtered.Length / (double)PageSize));
        currentPage = Math.Clamp(currentPage, 1, pages);
        refreshing = true;
        var latest=Controls.ClinicalWorkflow.HoSoViewModel.LanKhamGanNhatTheoBenhNhan();
        Grid_BENH_NHAN.ItemsSource = filtered.Skip((currentPage - 1) * PageSize).Take(PageSize).Select(r=>new Controls.ClinicalWorkflow.BenhNhanDanhSachItem(r,latest.TryGetValue(UiRecord.Text(r,"MaBN"),out var when)?when:null)).ToArray();
        Grid_BENH_NHAN.SelectedItem = Grid_BENH_NHAN.Items.Cast<object>().FirstOrDefault(row => UiRecord.Text(row, "MaBN") == selected);
        refreshing = false;
        RecordCount.Text = $"{filtered.Length} bệnh nhân · chọn để mở hồ sơ";
        PageLabel.Text = $"{currentPage} / {pages}";
        PreviousButton.IsEnabled = currentPage > 1; NextButton.IsEnabled = currentPage < pages;
        ListEmpty.Visibility = filtered.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        ListEmpty.Text = DuLieuGiaoDien.Rows("BENH_NHAN").Count == 0 ? "Chưa có bệnh nhân. Thêm bệnh nhân để bắt đầu." : "Không có bệnh nhân phù hợp với tìm kiếm và bộ lọc.";
        if (selected is not null && DuLieuGiaoDien.Find("BENH_NHAN", selected) is null) ClearProfile("Bệnh nhân không còn trong dữ liệu. Chọn lại từ danh sách.");
    }

    /// <summary>
    /// Lọc và hiển thị lại danh sách theo nội dung tìm kiếm/trạng thái/ngày đang chọn; chưa thay đổi dữ liệu SQL.
    /// </summary>
    private void Filter_Changed(object sender, RoutedEventArgs e) { if (!ready || refreshing) return; requestedPatientId=null;currentPage = 1; Refresh(); }
    /// <summary>
    /// Giảm số trang danh sách bệnh nhân rồi lọc và hiển thị lại trang trước.
    /// </summary>
    private void Previous_Click(object sender, RoutedEventArgs e) { currentPage--; Refresh(); }
    /// <summary>
    /// Tăng số trang danh sách bệnh nhân rồi lọc và hiển thị lại trang sau.
    /// </summary>
    private void Next_Click(object sender, RoutedEventArgs e) { currentPage++; Refresh(); }
    /// <summary>
    /// Mở form thêm bệnh nhân mới qua workspace; chưa có mã bệnh nhân cho đến khi lưu SQL.
    /// </summary>
    private void AddPatient_Click(object sender, RoutedEventArgs e) => HoTroGiaoDien.Workspace(this)?.MoBieuMauHoSo("BenhNhan", "BENH_NHAN");
    /// <summary>
    /// Mở hồ sơ của bệnh nhân người dùng vừa chọn; bỏ qua sự kiện khi danh sách đang làm mới.
    /// </summary>
    private void Patient_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || refreshing || Grid_BENH_NHAN.SelectedItem is not object patient) return;
        var workspace = HoTroGiaoDien.Workspace(this);
        if (workspace is not null) workspace.ShowHoSoDetails("BENH_NHAN", patient);
        else OpenProfile(patient);
    }
}
