using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using BUS;
using ET;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Views;

/// <summary>Chi tiết chỉ đọc từ nguồn được kiểm tra quyền; bàn giao theo hợp đồng thanh toán đã chốt.</summary>
public partial class PrescriptionHistoryDetail : UserControl, IClinicalPendingWork
{
    /// <summary>
    /// Nguồn được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private readonly DongLichSuDon source;
    /// <summary>
    /// Ngữ cảnh định danh và quyền của người dùng đang thao tác.
    /// </summary>
    private readonly NguCanhNghiepVu actor;
    /// <summary>
    /// Đối tượng nghiệp vụ đơn thuốc được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private readonly BUS_DONTHUOC prescriptions;
    /// <summary>
    /// Đối tượng nghiệp vụ bàn giao thuốc được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private readonly BUS_BANGIAOTHUOC handover;
    /// <summary>
    /// Cờ Được cấp từ ngoài giao dịch; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private readonly bool externalTransaction;
    /// <summary>
    /// Mã đang chờ xử lý được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private string? pendingId;
    /// <summary>
    /// Cờ Cần in; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool canPrint;
    /// <summary>
    /// Giá trị Mã đơn thuốc; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string OrderId => source.MaDT;
    /// <summary>
    /// Dữ liệu chi tiết của mục đang xử lý. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public ChiTietBanGiao? ChiTiet { get; private set; }
    /// <summary>
    /// Kết quả. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public KetQuaBanGiao? KetQua { get; private set; }
    /// <summary>
    /// Đang xử lý.
    /// </summary>
    public bool DangXuLy { get; private set; }
    /// <summary>
    /// Giá trị Bắt buộc giữ nguồn; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public bool MustKeepSource => DangXuLy || pendingId is not null;
    /// <summary>
    /// Thông báo nguồn khóa thay đổi cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    public event Action<bool>? SourceLockChanged;

    /// <summary>
    /// Khởi tạo đơn thuốc lịch sử chi tiết. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    /// <param name="source">Giá trị nguồn dùng khi thực hiện thao tác; kiểu DongLichSuDon.</param>
    /// <param name="actor">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    public PrescriptionHistoryDetail(DongLichSuDon source, NguCanhNghiepVu actor, IDbTransaction? transaction = null)
    {
        this.source = source;
        this.actor = actor;
        externalTransaction = transaction is not null;
        prescriptions = new(transaction);
        handover = new(transaction);
        InitializeComponent();
        HandoverPanel.Visibility = actor.LaAdmin || actor.CoQuyen(QuyenLamViec.GiaoThuoc) ? Visibility.Visible : Visibility.Collapsed;
        LoadDetail();
    }

    /// <summary>
    /// Đọc dữ liệu chi tiết của chứng từ đang mở để điền giao diện và kiểm tra quyền thao tác.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>UpdateRights</c>, <c>prescriptions.LayChiTietCoQuyen</c>, <c>.ToString</c>, <c>HistoryDocument</c>.</remarks>
    private void LoadDetail()
    {
        canPrint = false;
        ChiTiet = null;
        PrescriptionViewer.Document = null;
        SourceText.Text = PatientText.Text = HandoverText.Text = "";
        UpdateRights();
        var detail = prescriptions.LayChiTietCoQuyen(OrderId, actor);
        if (detail.MaBN != source.MaBN || detail.DonThuoc.MaLS != source.MaLS || detail.DonThuoc.MaDT != OrderId
            || detail.DonThuoc.TrangThai is not ("Đã xác nhận" or "Ngừng áp dụng"))
            throw new LoiKhamException("MaDT", "Đơn không còn thuộc lịch sử đã kê trong ngữ cảnh đang mở. Tải lại danh sách.");
        SourceText.Text = $"Đơn #{OrderId} · Lượt #{source.MaLS} · {detail.DonThuoc.TrangThai}";
        PatientText.Text = $"{source.HoTenBN} · Mã {source.MaBN} · Ngày sinh {source.NgaySinh?.ToString("dd/MM/yyyy") ?? "chưa ghi nhận"}\nBác sĩ: {source.TenBS} · Ngày kê {detail.DonThuoc.NgayKe:dd/MM/yyyy HH:mm}";
        PrescriptionViewer.Document = HistoryDocument(detail);
        canPrint = false;
        StatusText.Text = "Nội dung đơn chỉ đọc.";
        if (detail.DonThuoc.TrangThai == "Đã xác nhận" && (actor.LaAdmin || actor.CoQuyen(QuyenLamViec.Kham) || actor.CoQuyen(QuyenLamViec.GiaoThuoc)))
        {
            try
            {
                var signed = ReadSigned();
                PrescriptionViewer.Document = DonThuocDocument.Tao(signed);
                canPrint = true;
            }
            catch (LoiKhamException ex) { StatusText.Text = ex.Message; }
        }
        ChiTiet = null;
        if (HandoverPanel.Visibility == Visibility.Visible)
        {
            try
            {
                ChiTiet = handover.LayChiTiet(OrderId, actor);
                ValidateSource(ChiTiet.Luot.MaBN, ChiTiet.Luot.MaLS, ChiTiet.Luot.MaDT);
                RenderHandover();
            }
            catch (InvalidOperationException ex) { ChiTiet = null; HandoverText.Text = ClinicalFormInput.Message(ex); }
        }
        ReceiverChecked.IsChecked = GuidanceChecked.IsChecked = false;
        UpdateRights();
    }

    /// <summary>
    /// Đọc nội dung chứng từ đã ký dùng cho tra cứu hoặc in.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>prescriptions.LayBanInDon</c>, <c>ValidateSource</c>.</remarks>
    /// <returns>Kết quả kiểu BanInDonThuoc dùng cho bước xử lý tiếp theo.</returns>
    private BanInDonThuoc ReadSigned()
    {
        var signed = prescriptions.LayBanInDon(OrderId, actor);
        ValidateSource(signed.Nguon.MaBN, signed.Nguon.Don.MaLS, signed.Nguon.Don.MaDT);
        return signed;
    }
    /// <summary>
    /// Đối chiếu nguồn dữ liệu chứng từ trước khi dùng nội dung tra cứu/in.
    /// </summary>
    /// <param name="patient">Giá trị bệnh nhân dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="visit">Giá trị lần khám dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="prescription">Giá trị đơn thuốc dùng khi thực hiện thao tác; kiểu string.</param>
    private void ValidateSource(string patient, string visit, string prescription)
    {
        if (patient != source.MaBN || visit != source.MaLS || prescription != OrderId)
            throw new LoiKhamException("MaDT", "Nguồn đơn đã đổi. Mở lại đúng bệnh nhân và lần khám.");
    }
    /// <summary>
    /// Tạo nguồn dữ liệu tài liệu từ đơn thuốc lịch sử đã đọc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>document.Blocks.Add</c>.</remarks>
    /// <param name="detail">Giá trị chi tiết dùng khi thực hiện thao tác; kiểu DonThuocChiTiet.</param>
    /// <returns>Kết quả kiểu FlowDocument dùng cho bước xử lý tiếp theo.</returns>
    private FlowDocument HistoryDocument(DonThuocChiTiet detail)
    {
        var document = new FlowDocument { FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), FontSize = 14, PagePadding = new Thickness(12), ColumnWidth = 900 };
        document.Blocks.Add(new Paragraph(new Bold(new Run("Nội dung đơn thuốc"))) { FontSize = 20 });
        document.Blocks.Add(new Paragraph(new Run("Chẩn đoán: " + source.ChanDoan)));
        foreach (var line in detail.CacDong)
        {
            // Đơn cũ thiếu thông tin chốt chỉ hiện mã nguồn, không tra ngược danh mục/cache hiện tại.
            var name = string.IsNullOrWhiteSpace(line.TenThuocChot) ? "Thuốc #" + line.MaThuoc : line.TenThuocChot;
            document.Blocks.Add(new Paragraph(new Run($"{name} · {line.HamLuongChot}\nLượng: {line.SoLuong:0.###} {line.DonViTinhChot} · {line.SoNgayDung} ngày\nLiều: {line.LieuDung} · Đường dùng: {line.DuongDung}\nHướng dẫn: {line.HuongDan}")));
        }
        if (!string.IsNullOrWhiteSpace(detail.DonThuoc.GhiChu)) document.Blocks.Add(new Paragraph(new Run("Ghi chú: " + detail.DonThuoc.GhiChu)));
        return document;
    }
    /// <summary>
    /// Hiển thị dữ liệu bàn giao hiện tại cùng thông tin nhân viên/thời điểm nếu đã giao.
    /// </summary>
    private void RenderHandover()
    {
        if (ChiTiet is not { } detail) return;
        HandoverText.Text = $"Hóa đơn #{detail.Luot.MaHD} · Xuất #{detail.Luot.MaPX} · {detail.Luot.TrangThai}";
        if (detail.Luot.TrangThai == "Đã giao")
            HandoverText.Text += $"\nNgười giao: {detail.Luot.TenNguoiGiao ?? detail.Luot.MaNVGiao} · {detail.Luot.ThoiGianGiao:dd/MM/yyyy HH:mm}";
    }
    /// <summary>
    /// Cập nhật trạng thái cho phép thao tác theo quyền và dữ liệu đang hiển thị.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>.Invoke</c>.</remarks>
    private void UpdateRights()
    {
        if (GiveButton is null) return;
        var eligible = ChiTiet?.Luot.TrangThai == "Chờ giao";
        ReceiverChecked.IsEnabled = GuidanceChecked.IsEnabled = eligible && !MustKeepSource;
        GiveButton.IsEnabled = eligible && !DangXuLy && (pendingId is not null || ReceiverChecked.IsChecked == true && GuidanceChecked.IsChecked == true);
        GiveButton.Content = pendingId is null ? "Xác nhận đã giao" : "Thử lại đúng đơn";
        PrintRxButton.IsEnabled = canPrint && !MustKeepSource;
        ReloadButton.IsEnabled = !MustKeepSource;
        SourceLockChanged?.Invoke(MustKeepSource);
    }

    /// <summary>
    /// Đối chiếu các mục bàn giao và thử xác nhận giao thuốc bất đồng bộ; làm mới chứng từ theo kết quả.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>UpdateRights</c>, <c>Task.Run</c>, <c>handover.XacNhanGiao</c>, <c>RenderHandover</c>.</remarks>
    /// <returns>Tác vụ bất đồng bộ để nơi gọi theo dõi thời điểm kết thúc và kết quả xử lý.</returns>
    public async Task<bool> TryGiaoAsync()
    {
        if (DangXuLy || ChiTiet?.Luot.TrangThai != "Chờ giao") return false;
        if (externalTransaction)
        { StatusText.Text = "Bàn giao cần giao dịch độc lập; đóng phiên đối chiếu trước khi xác nhận giao."; return false; }
        if (pendingId is null && (ReceiverChecked.IsChecked != true || GuidanceChecked.IsChecked != true))
        { StatusText.Text = "Cần đối chiếu người nhận và hướng dẫn trước giao."; return false; }
        var id = pendingId ?? OrderId;
        pendingId = id;
        DangXuLy = true;
        UpdateRights();
        StatusText.Text = "Đang ghi nhận bàn giao…";
        try
        {
            var result = await Task.Run(() => handover.XacNhanGiao(id, actor));
            pendingId = null;
            KetQua = result;
            var previous = ChiTiet;
            ChiTiet = previous with { Luot = previous.Luot with { TrangThai = "Đã giao", MaNVGiao = result.MaNVGiao, ThoiGianGiao = result.ThoiGianGiao } };
            RenderHandover();
            StatusText.Text = $"Đã giao đơn #{id} · {result.ThoiGianGiao:dd/MM/yyyy HH:mm}. " + (result.DaGiaoHet ? "Lượt đã hoàn tất." : "Lượt còn đơn chờ giao.");
            return true;
        }
        catch (LoiKhamException ex) when (ex.Truong != "BanGiao")
        { pendingId = null; ChiTiet = null; StatusText.Text = ex.Message; return false; }
        catch (TuChoiQuyenNghiepVuException ex)
        { pendingId = null; ChiTiet = null; StatusText.Text = ex.Message; return false; }
        catch (Exception)
        { StatusText.Text = "Chưa xác nhận được kết quả giao. Thử lại đúng đơn này để đối chiếu; không xuất thuốc lần nữa."; return false; }
        finally { DangXuLy = false; UpdateRights(); }
    }

    /// <summary>
    /// Tiếp nhận thay đổi các mục kiểm tra và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>UpdateRights</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Checks_Changed(object sender, RoutedEventArgs e) => UpdateRights();
    /// <summary>
    /// Xử lý thao tác bàn giao khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TryGiaoAsync</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private async void Give_Click(object sender, RoutedEventArgs e) => await TryGiaoAsync();
    /// <summary>
    /// Xử lý thao tác tải lại khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>LoadDetail</c>, <c>ClinicalFormInput.Message</c>, <c>UpdateRights</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        if (MustKeepSource) return;
        try { LoadDetail(); } catch (InvalidOperationException ex) { canPrint = false; ChiTiet = null; StatusText.Text = ClinicalFormInput.Message(ex); UpdateRights(); }
    }
    /// <summary>
    /// Xử lý thao tác in đơn thuốc khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Window.GetWindow</c>, <c>ClinicalFormInput.Message</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void PrintRx_Click(object sender, RoutedEventArgs e)
    {
        try { new XemTruocDonThuoc(ReadSigned) { Owner = Window.GetWindow(this) }.Show(); }
        catch (InvalidOperationException ex) { StatusText.Text = ClinicalFormInput.Message(ex); }
    }
    /// <summary>
    /// Giá trị Công việc tiêu đề; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    string IClinicalPendingWork.WorkTitle => "Yêu cầu bàn giao";
    /// <summary>
    /// Giá trị Có thay đổi chưa lưu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    bool IClinicalPendingWork.HasChanges => false;
    /// <summary>
    /// Giá trị Bắt buộc ở lại màn hình; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    bool IClinicalPendingWork.MustStay => MustKeepSource;
    /// <summary>
    /// Thử lưu nội dung đang sửa trước khi rời màn hình; trả kết quả để nơi gọi quyết định có tiếp tục điều hướng.
    /// </summary>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    bool IClinicalPendingWork.SaveBeforeLeave(out string error) { error = "Cần đối chiếu kết quả giao đúng đơn đang xử lý."; return false; }
    /// <summary>
    /// Bỏ nội dung chưa lưu và đưa trạng thái theo dõi về bản đã chấp nhận.
    /// </summary>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    bool IClinicalPendingWork.DiscardBeforeLeave(out string error) { error = "Cần đối chiếu kết quả giao đúng đơn đang xử lý."; return false; }
}
