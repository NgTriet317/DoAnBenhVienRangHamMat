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
        if (DangXuLy || ChoDoiChieu) { StatusText.Text = "Cần đối chiếu yêu cầu đang thu trước khi đổi lượt."; return; }
        visitId = maLS; Clear();
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
        if (DangXuLy || ChoDoiChieu) { error = "Cần đối chiếu yêu cầu đang thu trước khi lập lại bảng tính."; StatusText.Text = error; return false; }
        Clear(); error = null;
        try
        {
            var s = bus.LapBangTinh(visitId ?? "", WarehouseFilter.SelectedValue as string, user);
            BangTinh = s; VisitText.Text = $"{s.HoTenBN} · Mã {s.MaBN} · Lần khám #{s.MaLS}";
            ServicesGrid.ItemsSource = s.DichVu.Select(d => new { d.MaDVThucHien, d.NoiDung, d.SoLuong, Gia = Money(d.DonGia), Tien = Money(d.ThanhTien) }).ToArray();
            DrugsGrid.ItemsSource = s.Thuoc.Select(d => new { Nguon = $"{d.MaDT} / {d.MaCTDT}", d.NoiDung, d.DonViTinh, d.SoLuong, Gia = Money(d.DonGia), Tien = Money(d.ThanhTien) }).ToArray();
            LotsGrid.ItemsSource = s.Thuoc.SelectMany(d => d.CacLo.Select(l => new { d.MaCTDT, l.MaLo, l.SoLo, l.SoLuong, Tien = Money(l.ThanhTien) })).ToArray();
            HistoryGrid.ItemsSource = s.HoaDonCu.Select(h => new { h.MaHD, h.TrangThai, PhaiThu = Money(h.PhaiThu), DaThu = Money(h.DaThu), ConPhaiThu = Money(h.ConPhaiThu) }).ToArray();
            TotalText.Text = s.DauVanTay is null ? "Đối chiếu chứng từ đã có" : $"Tổng dự kiến: {Money(s.TongTien)}";
            StatusText.Text = s.ThongBao; UpdatePaymentControls(); return true;
        }
        catch (Exception ex) { error = ex.Message; StatusText.Text = "Chưa lập được bảng tính: " + ex.Message; return false; }
    }
    private static string Money(decimal amount) => TreatmentMoney.Format(amount);
    private void Preview_Click(object sender, RoutedEventArgs e) => TryLapBangTinh(out _);
    private void Warehouse_Changed(object sender, SelectionChangedEventArgs e)
    { if (TotalText is null) return; if(DangXuLy || ChoDoiChieu) { WarehouseFilter.SelectedValue=BangTinh?.MaKhoCapThuoc??""; return; } Clear(); StatusText.Text = "Kho đã đổi. Xem lại bảng tính theo kho mới."; }
}
