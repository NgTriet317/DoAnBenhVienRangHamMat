using System.Windows;
using System.Windows.Controls;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Trình bày tổng quan theo hoạt động khám, lịch hẹn, thu tiền và dịch vụ.</summary>
public partial class BaoCao
{
    private string DinhDangDieuHanh(decimal? value,string unit)=>value is null?"—":value.Value.ToString(unit=="VNĐ"?"N2":"N0",Vietnamese)+" "+unit;

    private string ThayDoiDieuHanh(DongSoSanhBaoCao row)
    {
        if(row.GiaTriGoc is null)return "Không chọn đối chiếu";
        if(row.ChenhLech==0)return "Không thay đổi";
        return (row.ChenhLech>0?"Tăng ":"Giảm ")+DinhDangDieuHanh(Math.Abs(row.ChenhLech!.Value),row.DonVi)+
            (row.PhanTram is decimal percent?$" ({Math.Abs(percent).ToString("N1",Vietnamese)}%)":" · Chưa tính được % vì kỳ gốc bằng 0");
    }

    private void HienTongQuanDieuHanh()
    {
        bool active=selected?.Ma=="00";
        OverviewPanel.Visibility=active?Visibility.Visible:Visibility.Collapsed;
        OverviewComparePanel.Visibility=active&&soSanh?.TongQuan is not null?Visibility.Visible:Visibility.Collapsed;
        OverviewAppointmentItems.ItemsSource=OverviewMoneyItems.ItemsSource=OverviewCompareGrid.ItemsSource=null;
        if(!active)return;
        DiseasePanel.Visibility=Visibility.Collapsed;
        if(soSanh?.TongQuan is not GoiTongQuanDieuHanh overview)
        {
            OverviewPeriodNote.Text="Hoạt động trong kỳ đã chọn";
            OverviewWorkNote.Text="Tải báo cáo để xem tình hình tiếp nhận và hồ sơ khám.";
            OverviewAppointmentNote.Text="Lịch có ngày hẹn trong kỳ; trạng thái tại lúc tải.";
            return;
        }
        var q=overview.HienTai;
        ComparisonPanel.Visibility=Visibility.Collapsed;
        SummaryPanel.Visibility=soSanh.CacKy.Length>1?Visibility.Visible:Visibility.Collapsed;
        OverviewPeriodNote.Text=$"Hoạt động từ {overview.KyHienTai.TuNgay:dd/MM/yyyy} đến {overview.KyHienTai.DenExclusive.AddDays(-1):dd/MM/yyyy}";
        OverviewWorkNote.Text=$"{q.SoPhieuTiepNhan:N0} phiếu tiếp nhận trong kỳ · {q.LuotKhamNhap:N0} lượt khám còn nháp · {q.LuotChuaPhanLoai:N0} lượt khám chưa phân loại tình trạng. Trạng thái hồ sơ tại lúc tải.";
        int completed=overview.TrangThaiLichHen.Where(x=>x.TrangThai is "Đã khám" or "Hoàn thành").Sum(x=>x.SoLuong);
        OverviewAppointmentNote.Text=q.TongLichHen==0?"Không có lịch hẹn trong kỳ đã chọn.":
            $"{q.TongLichHen:N0} lịch có ngày hẹn trong kỳ · {completed:N0} lịch đã khám ({(100m*completed/q.TongLichHen).ToString("N1",Vietnamese)}%). Trạng thái hiện tại khi tải.";
        OverviewAppointmentItems.ItemsSource=overview.TrangThaiLichHen.Select(x=>new{Ten=x.TrangThai,GiaTri=$"{x.SoLuong:N0} lịch"});
        OverviewMoneyItems.ItemsSource=new[]
        {
            new{Ten="Tiền đã thu",GiaTri=DinhDangDieuHanh(q.TienThu,"VNĐ")},
            new{Ten="Tiền đã hoàn",GiaTri=DinhDangDieuHanh(q.TienHoan,"VNĐ")},
            new{Ten="Tiền thực thu",GiaTri=DinhDangDieuHanh(q.TienThucThu,"VNĐ")},
            new{Ten="Giao dịch đã xác nhận",GiaTri=$"{q.SoGiaoDich:N0} giao dịch"}
        };
        string[] tips=["Mỗi người tính một lần trong toàn kỳ dù tái khám nhiều lần.","Mỗi lần khám đã xác nhận tính một lượt.",
            "Tiền đã thu trừ tiền đã hoàn từ giao dịch xác nhận; có thể âm nếu hoàn tiền nhiều hơn thu.","Tất cả lịch có ngày giờ hẹn trong kỳ, gồm các trạng thái hiện tại của lịch."];
        MetricItems.ItemsSource=soSanh.TongCong.Select((row,i)=>new ChiSoBaoCao(row.ChiTieu,
            row.HienTai?.ToString(row.DonVi=="VNĐ"?"N2":"N0",Vietnamese)??"—",row.DonVi,Colors[i],tips[i],
            row.GiaTriGoc is null?"Không chọn đối chiếu":$"Đối chiếu: {DinhDangDieuHanh(row.GiaTriGoc,row.DonVi)} · {ThayDoiDieuHanh(row)}")).ToArray();
        OverviewCompareNote.Text=$"Kỳ đang xem: {overview.KyHienTai.Nhan} · Đối chiếu: {soSanh.TongCong[0].KyGoc}. {soSanh.CacKy[^1].GhiChu}";
        OverviewCompareGrid.ItemsSource=soSanh.TongCong.Select(row=>new{Ten=row.ChiTieu,HienTai=DinhDangDieuHanh(row.HienTai,row.DonVi),
            KyGoc=row.GiaTriGoc is null?"Không so sánh":DinhDangDieuHanh(row.GiaTriGoc,row.DonVi),ThayDoi=ThayDoiDieuHanh(row)});
        TrendTitle.Text="Xu hướng hoạt động theo tháng";
        TrendCaption.Text="Chọn chỉ tiêu trên biểu đồ để xem bệnh nhân, lượt khám, tiền thực thu hoặc lịch hẹn. "+soSanh.CacKy[^1].GhiChu;
        CompositionTitle.Text="5 dịch vụ thực hiện nhiều";
        CompositionSubtitle.Text="Số lượt khám có dịch vụ đã xác nhận · Theo ngày thực hiện";
        int max=overview.DichVu.Length==0?1:overview.DichVu.Max(x=>x.SoLuotKham);
        CompositionItems.ItemsSource=overview.DichVu.Select(x=>new{Ten=x.TenDV,HienThi=$"{x.SoLuotKham:N0} lượt",TiLe=max==0?0:100d*x.SoLuotKham/max});
        CompositionEmpty.Visibility=overview.DichVu.Length==0?Visibility.Visible:Visibility.Collapsed;
        CompositionEmpty.Text="Không có dịch vụ đã xác nhận trong kỳ thực hiện.";
        TableTitle.Text="Hoạt động từng tháng";
        TableHint.Text="Số bệnh nhân tính riêng mỗi tháng; tổng toàn kỳ đếm lại mỗi người một lần.";
        FilterHint.Text="Lấy trọn các tháng đã chọn; bật lũy kế (MTD) khi xem tháng hiện tại.";
    }

    private void MoChuyenDeTongQuan_Click(object sender,RoutedEventArgs e)
    {
        if(sender is not Button{Tag:string code}||!BaoCaoDuLieu.DanhMuc.Any(x=>x.Ma==code))return;
        if(code is "04" or "07")ShowAllTopics.IsChecked=true;
        ChonBaoCao(code);
    }

    private void ChiTieuBieuDo_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!ready||selected is null)return;
        if(selected.Ma=="00"&&soSanh?.TongQuan is not null){HienSoSanh(selected);return;}
        BoLoc_Changed(sender,e);
    }
}
