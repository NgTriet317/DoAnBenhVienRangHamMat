using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Views;

/// <summary>
/// Phần xử lý bảng tính phí trình nhập liệu của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class BangTinhPhiEditor : UserControl
{
    /// <summary>
    /// Đối tượng nghiệp vụ thanh toán trọn gói được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private readonly BUS_THANHTOANTRONGOI bus;
    /// <summary>
    /// Ngữ cảnh định danh và quyền của người dùng đang thao tác.
    /// </summary>
    private readonly NguCanhNghiepVu user;
    /// <summary>
    /// Mã lần khám được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private string? visitId;
    /// <summary>
    /// Cờ Đang khôi phục kho; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool restoringWarehouse;
    /// <summary>
    /// Đã liên kết lần khám được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private DongLanKhamDanhSach? linkedVisit;
    /// <summary>
    /// Bảng tính. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public BangTinhPhi? BangTinh { get; private set; }

    /// <summary>
    /// Khởi tạo bảng tính phí trình nhập liệu. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    public BangTinhPhiEditor(NguCanhNghiepVu? user = null, IDbTransaction? tx = null)
    {
        InitializeComponent();
        this.user = user ?? QuyenNghiepVu.LayNguCanh(); bus = new(tx);
        WarehouseFilter.ItemsSource = new[] { new LuaChonDanhMuc("", "Không dùng kho") }.Concat(
            new BUS_KHO(tx).layToanBoKho().Where(k => k.TrangThai == "Đang hoạt động")
                .Select(k => new LuaChonDanhMuc(k.MaKho, k.TenKho))).ToArray();
        WarehouseFilter.SelectedIndex = 0;
        InitializePayment();
        FocusContext(null);
    }
    /// <summary>
    /// Chuyển màn hình tới đúng bệnh nhân/lần khám hoặc chứng từ theo ngữ cảnh được yêu cầu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanChangeSource</c>, <c>Clear</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    public void FocusContext(string? maLS)
    {
        if (maLS == visitId && BangTinh is not null) return;
        if (!CanChangeSource(out _))return;
        visitId = maLS;linkedVisit=null;Clear();
        VisitText.Text = string.IsNullOrEmpty(maLS) ? "Chọn một lượt khám ở bộ lọc phía trên." : "Lần khám #" + maLS;
        PreviewButton.IsEnabled = !string.IsNullOrEmpty(maLS);
        StatusText.Text = "Chọn kho nếu có thuốc, sau đó xem bảng tính.";
    }
    /// <summary>
    /// Đặt lại dữ liệu hoặc phần hiển thị của thành phần theo các bước trong hàm.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ResetPayment</c>.</remarks>
    private void Clear()
    {
        BangTinh = null; TotalText.Text = "Chưa có bảng tính";
        ServicesGrid.ItemsSource = DrugsGrid.ItemsSource = LotsGrid.ItemsSource = HistoryGrid.ItemsSource = null;
        ResetPayment();
    }
    /// <summary>
    /// Kiểm tra nguồn lần khám/kho và lập bảng tính phí trước khi cho phép thu tiền.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanChangeSource</c>, <c>Clear</c>, <c>bus.LapBangTinh</c>, <c>RenderVisitIdentity</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool TryLapBangTinh(out string? error)
    {
        if(!CanChangeSource(out error))return false;
        Clear(); error = null;
        try
        {
            var s = bus.LapBangTinh(visitId ?? "", WarehouseFilter.SelectedValue as string, user);
            BangTinh = s;RenderVisitIdentity();
            ServicesGrid.ItemsSource = s.DichVu.Select(d => new { d.MaDVThucHien, d.NoiDung, d.SoLuong, Gia = Money(d.DonGia), Tien = Money(d.ThanhTien) }).ToArray();
            DrugsGrid.ItemsSource = s.Thuoc.Select(d => new { Nguon = $"{d.MaDT} / {d.MaCTDT}", d.NoiDung, d.DonViTinh, d.SoLuong, Gia = Money(d.DonGia), Tien = Money(d.ThanhTien) }).ToArray();
            LotsGrid.ItemsSource = s.Thuoc.SelectMany(d => d.CacLo.Select(l => new { d.MaCTDT, l.MaLo, l.SoLo, l.SoLuong, Tien = Money(l.ThanhTien) })).ToArray();
            HistoryGrid.ItemsSource = s.HoaDonCu.Select(h => new { h.MaHD, h.TrangThai, PhaiThu = Money(h.PhaiThu), DaThu = Money(h.DaThu), ConPhaiThu = Money(h.ConPhaiThu) }).ToArray();
            TotalText.Text = s.MaHDChoThu is not null ? $"Hóa đơn chờ thu #{s.MaHDChoThu} · Phải thu: {Money(s.TongTien)}" :
                s.DauVanTay is null ? "Đối chiếu chứng từ đã có" : $"Tổng dự kiến: {Money(s.TongTien)}";
            StatusText.Text = s.ThongBao; UpdatePaymentControls(); return true;
        }
        catch (Exception ex) { error = ClinicalFormInput.Message(ex); StatusText.Text = "Chưa lập được bảng tính: " + error; return false; }
    }
    /// <summary>
    /// Định dạng giá trị tiền dùng trong phần hiển thị.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TreatmentMoney.Format</c>.</remarks>
    /// <param name="amount">Giá trị số tiền dùng khi thực hiện thao tác; kiểu decimal.</param>
    /// <returns>Chuỗi kết quả của thao tác tiền.</returns>
    private static string Money(decimal amount) => TreatmentMoney.Format(amount);
    /// <summary>
    /// Ghi nhận thông tin nhận diện lần khám dùng cho vùng bảng tính và thanh toán.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>RenderVisitIdentity</c>.</remarks>
    /// <param name="source">Giá trị nguồn dùng khi thực hiện thao tác; kiểu DongLanKhamDanhSach?.</param>
    internal void SetVisitIdentity(DongLanKhamDanhSach? source)
    {linkedVisit=source?.MaLS==visitId?source:null;RenderVisitIdentity();}
    /// <summary>
    /// Hiển thị thông tin bệnh nhân/lần khám đã được xác định cho bảng tính.
    /// </summary>
    private void RenderVisitIdentity()
    {
        var name=BangTinh?.HoTenBN??linkedVisit?.HoTenBN;var patient=BangTinh?.MaBN??linkedVisit?.MaBN;
        VisitText.Text=name is null?(string.IsNullOrEmpty(visitId)?"Chọn một lượt khám ở bộ lọc phía trên.":"Lần khám #"+visitId):$"{name} · Mã {patient} · Lần khám #{visitId}";
        if(linkedVisit is not null)VisitText.Text+=$"\nBác sĩ: {linkedVisit.TenBS} ({linkedVisit.MaBS})";
    }
    /// <summary>
    /// Xử lý thao tác xem trước khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TryLapBangTinh</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Preview_Click(object sender, RoutedEventArgs e) => TryLapBangTinh(out _);
    /// <summary>
    /// Tiếp nhận thay đổi kho và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>CanChangeSource</c>, <c>Clear</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Warehouse_Changed(object sender, SelectionChangedEventArgs e)
    {
        if(TotalText is null||restoringWarehouse)return;
        if(!CanChangeSource(out _))
        {restoringWarehouse=true;try{WarehouseFilter.SelectedValue=BangTinh?.MaKhoCapThuoc??"";}finally{restoringWarehouse=false;}return;}
        Clear();StatusText.Text="Kho đã đổi. Xem lại bảng tính theo kho mới.";
    }
    /// <summary>
    /// Kiểm tra có thể đổi nguồn bảng tính mà không bỏ công việc thanh toán đang xử lý.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ClinicalWindowLifecycle.OwnerMain</c>, <c>Window.GetWindow</c>, <c>ClinicalPendingWork.TryLeave</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    internal bool CanChangeSource(out string? error)
    {
        error=null;
        if(DangXuLy||ChoDoiChieu){error=StatusText.Text="Cần đối chiếu yêu cầu đang thu trước khi đổi nguồn hoặc lập lại bảng tính.";return false;}
        var decision=ClinicalWindowLifecycle.OwnerMain(Window.GetWindow(this))?.LeaveDecision;
        if(ClinicalPendingWork.TryLeave([this],decision,out var message))return true;
        error=string.IsNullOrEmpty(message)?"Đã giữ lượt, kho và tiền đang nhập.":message;StatusText.Text=error;return false;
    }
}
