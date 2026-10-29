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
    private readonly DongLichSuDon source;
    private readonly NguCanhNghiepVu actor;
    private readonly BUS_DONTHUOC prescriptions;
    private readonly BUS_BANGIAOTHUOC handover;
    private readonly bool externalTransaction;
    private string? pendingId;
    private bool canPrint;
    public string OrderId => source.MaDT;
    public ChiTietBanGiao? ChiTiet { get; private set; }
    public KetQuaBanGiao? KetQua { get; private set; }
    public bool DangXuLy { get; private set; }
    public bool MustKeepSource => DangXuLy || pendingId is not null;
    public event Action<bool>? SourceLockChanged;

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

    private BanInDonThuoc ReadSigned()
    {
        var signed = prescriptions.LayBanInDon(OrderId, actor);
        ValidateSource(signed.Nguon.MaBN, signed.Nguon.Don.MaLS, signed.Nguon.Don.MaDT);
        return signed;
    }
    private void ValidateSource(string patient, string visit, string prescription)
    {
        if (patient != source.MaBN || visit != source.MaLS || prescription != OrderId)
            throw new LoiKhamException("MaDT", "Nguồn đơn đã đổi. Mở lại đúng bệnh nhân và lần khám.");
    }
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
    private void RenderHandover()
    {
        if (ChiTiet is not { } detail) return;
        HandoverText.Text = $"Hóa đơn #{detail.Luot.MaHD} · Xuất #{detail.Luot.MaPX} · {detail.Luot.TrangThai}";
        if (detail.Luot.TrangThai == "Đã giao")
            HandoverText.Text += $"\nNgười giao: {detail.Luot.TenNguoiGiao ?? detail.Luot.MaNVGiao} · {detail.Luot.ThoiGianGiao:dd/MM/yyyy HH:mm}";
    }
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

    private void Checks_Changed(object sender, RoutedEventArgs e) => UpdateRights();
    private async void Give_Click(object sender, RoutedEventArgs e) => await TryGiaoAsync();
    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        if (MustKeepSource) return;
        try { LoadDetail(); } catch (InvalidOperationException ex) { canPrint = false; ChiTiet = null; StatusText.Text = ClinicalFormInput.Message(ex); UpdateRights(); }
    }
    private void PrintRx_Click(object sender, RoutedEventArgs e)
    {
        try { new XemTruocDonThuoc(ReadSigned) { Owner = Window.GetWindow(this) }.Show(); }
        catch (InvalidOperationException ex) { StatusText.Text = ClinicalFormInput.Message(ex); }
    }
    string IClinicalPendingWork.WorkTitle => "Yêu cầu bàn giao";
    bool IClinicalPendingWork.HasChanges => false;
    bool IClinicalPendingWork.MustStay => MustKeepSource;
    bool IClinicalPendingWork.SaveBeforeLeave(out string error) { error = "Cần đối chiếu kết quả giao đúng đơn đang xử lý."; return false; }
    bool IClinicalPendingWork.DiscardBeforeLeave(out string error) { error = "Cần đối chiếu kết quả giao đúng đơn đang xử lý."; return false; }
}
