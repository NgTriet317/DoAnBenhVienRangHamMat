using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;

namespace RangHamMat.Views;

public partial class BangTinhPhiEditor : UserControl
{
    private readonly BUS_THANHTOANTRONGOI bus;
    private readonly NguCanhNghiepVu user;
    private string? visitId;
    private bool restoringWarehouse;
    private DongLanKhamDanhSach? linkedVisit;
    public BangTinhPhi? BangTinh { get; private set; }

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
    public void FocusContext(string? maLS)
    {
        if (maLS == visitId && BangTinh is not null) return;
        if (!CanChangeSource(out _))return;
        visitId = maLS;linkedVisit=null;Clear();
        VisitText.Text = string.IsNullOrEmpty(maLS) ? "Chọn một lượt khám ở bộ lọc phía trên." : "Lần khám #" + maLS;
        PreviewButton.IsEnabled = !string.IsNullOrEmpty(maLS);
        StatusText.Text = "Chọn kho nếu có thuốc, sau đó xem bảng tính.";
    }
    private void Clear()
    {
        BangTinh = null; TotalText.Text = "Chưa có bảng tính";
        ServicesGrid.ItemsSource = DrugsGrid.ItemsSource = LotsGrid.ItemsSource = HistoryGrid.ItemsSource = null;
        ResetPayment();
    }
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
            TotalText.Text = s.DauVanTay is null ? "Đối chiếu chứng từ đã có" : $"Tổng dự kiến: {Money(s.TongTien)}";
            StatusText.Text = s.ThongBao; UpdatePaymentControls(); return true;
        }
        catch (Exception ex) { error = ClinicalFormInput.Message(ex); StatusText.Text = "Chưa lập được bảng tính: " + error; return false; }
    }
    private static string Money(decimal amount) => TreatmentMoney.Format(amount);
    internal void SetVisitIdentity(DongLanKhamDanhSach? source)
    {linkedVisit=source?.MaLS==visitId?source:null;RenderVisitIdentity();}
    private void RenderVisitIdentity()
    {
        var name=BangTinh?.HoTenBN??linkedVisit?.HoTenBN;var patient=BangTinh?.MaBN??linkedVisit?.MaBN;
        VisitText.Text=name is null?(string.IsNullOrEmpty(visitId)?"Chọn một lượt khám ở bộ lọc phía trên.":"Lần khám #"+visitId):$"{name} · Mã {patient} · Lần khám #{visitId}";
        if(linkedVisit is not null)VisitText.Text+=$"\nBác sĩ: {linkedVisit.TenBS} ({linkedVisit.MaBS})";
    }
    private void Preview_Click(object sender, RoutedEventArgs e) => TryLapBangTinh(out _);
    private void Warehouse_Changed(object sender, SelectionChangedEventArgs e)
    {
        if(TotalText is null||restoringWarehouse)return;
        if(!CanChangeSource(out _))
        {restoringWarehouse=true;try{WarehouseFilter.SelectedValue=BangTinh?.MaKhoCapThuoc??"";}finally{restoringWarehouse=false;}return;}
        Clear();StatusText.Text="Kho đã đổi. Xem lại bảng tính theo kho mới.";
    }
    internal bool CanChangeSource(out string? error)
    {
        error=null;
        if(DangXuLy||ChoDoiChieu){error=StatusText.Text="Cần đối chiếu yêu cầu đang thu trước khi đổi nguồn hoặc lập lại bảng tính.";return false;}
        var decision=ClinicalWindowLifecycle.OwnerMain(Window.GetWindow(this))?.LeaveDecision;
        if(ClinicalPendingWork.TryLeave([this],decision,out var message))return true;
        error=string.IsNullOrEmpty(message)?"Đã giữ lượt, kho và tiền đang nhập.":message;StatusText.Text=error;return false;
    }
}
