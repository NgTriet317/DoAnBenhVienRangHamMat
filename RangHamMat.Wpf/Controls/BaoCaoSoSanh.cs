using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;

public partial class BaoCao
{
    private QuyenBaoCao quyen;
    private string? loiQuyen;
    private DuLieuSoSanhBaoCao? soSanh;
    private bool CoQuyenChuyenDe => ChinhSachBaoCao.ChoPhep(selected?.Ma??"00",quyen);

    private void PhienBaoCaoThayDoi()
    {
        XoaKetQua();quyen=QuyenBaoCao.Khong;
        LoadButton.IsEnabled=false;ReportCatalog.ItemsSource=null;
        ThongBao("Phiên đăng nhập đã thay đổi. Mở lại Báo cáo để xác thực quyền.",true);
    }

    private void NamGoc_Changed(object sender,TextChangedEventArgs e) => BoLoc_Changed(sender,e);
    private void DanhMucMoRong_Changed(object sender,RoutedEventArgs e) => HienDanhMuc();
    private void XepHangBenh_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(ready&&selected is not null&&soSanh?.BenhLy is not null)HienSoSanh(selected);
    }

    private BoLocThongKe DocBoLoc()
    {
        if(ComparisonMode.SelectedIndex==3 && !int.TryParse(BaselineYear.Text,out _))
            throw new ArgumentException("Nhập năm đối chiếu hợp lệ.");
        return new()
        {
            TuThang=FromDate.SelectedDate??DateTime.Today,DenThang=ToDate.SelectedDate??DateTime.Today,
            SoSanh=(CachSoSanh)Math.Max(0,ComparisonMode.SelectedIndex),LuyKeThang=MonthToDate.IsChecked==true,
            NamGoc=int.TryParse(BaselineYear.Text,out var year)?year:DateTime.Today.Year-1,
            MaCK=selected?.Ma=="20"?SpecialtyCode.SelectedValue as string:null,MaDV=selected?.Ma=="20"?ServiceCode.SelectedValue as string:null
        };
    }

    private void HienSoSanh(ChuyenDeBaoCao report)
    {
        int metric=Math.Max(0,ChartMetric.SelectedIndex);
        ReportChart.Unit=ReportChart.SecondaryUnit=report.DonVi[metric];
        if(soSanh is null)
        {
            if(result is not null && !report.TheoThoiDiem)
            {
                decimal Value(DongBaoCao d)=>metric switch{0=>d.Mot,1=>d.Hai,2=>d.Ba,_=>d.Bon??0};
                ReportChart.PrimaryLabel=report.ChiSo[metric];ReportChart.SecondaryLabel="";
                ReportChart.Points=result.CacKy.Select(r=>new ChartPoint(r.Ky,(double)Value(r),0,r.Ky));
            }
            return;
        }
        ComparisonPanel.Visibility=Visibility.Visible;
        ComparisonGrid.ItemsSource=soSanh.TongCong.Concat(soSanh.CacDong);
        var totals=result?.Tong;
        if(totals is not null)
        {
            var values=new[]{totals.Mot,totals.Hai,totals.Ba,totals.Bon};
            MetricItems.ItemsSource=report.ChiSo.Select((name,i)=>new ChiSoBaoCao(name,
                values[i]?.ToString(report.DonVi[i] is "VNĐ" or "%"?"N2":"N0",Vietnamese)??"—",report.DonVi[i],Colors[i],report.QuyTac,
                soSanh.TongCong[i].GhiChu)).ToArray();
        }
        ComparisonNote.Text=soSanh.CacKy[^1].GhiChu+" · Δ = kỳ này − kỳ gốc; % = Δ / |kỳ gốc| × 100. Kỳ gốc = 0: không tính %.";
        var chartRows=soSanh.CacDong.Where(r=>r.ChiTieu==report.ChiSo[metric]).ToArray();
        ReportChart.PrimaryLabel=report.ChiSo[metric]+" · Kỳ này";
        ReportChart.SecondaryLabel=soSanh.CacKy[0].KyGoc is null?"":"Kỳ đối chiếu";
        ReportChart.Points=chartRows.Select((r,i)=>(Row:r,Period:soSanh.CacKy[i]))
            .Where(x=>x.Row.HienTai is not null&&(x.Period.KyGoc is null||x.Row.GiaTriGoc is not null))
            .Select(x=>new ChartPoint(x.Period.HienTai.TuNgay.ToString("MM/yyyy"),(double)x.Row.HienTai!.Value,
                (double)(x.Row.GiaTriGoc??0),$"Kỳ này {x.Row.Ky} · Kỳ gốc {x.Row.KyGoc}"));
        ReportChart.MinWidth=chartRows.Length*150+94;
        TrendSubtitle.Text=report.ChiSo[metric]+" · "+report.DonVi[metric];
        TrendCaption.Text="Hai chuỗi dùng cùng chỉ tiêu, đơn vị, trạng thái và mốc thời gian. Kỳ không đủ mẫu số không được vẽ thành 0. "+soSanh.CacKy[^1].GhiChu;
        FilterHint.Text="Kỳ Tháng lấy trọn các tháng đã chọn; MTD chỉ lấy tới ngày hiện tại.";
        if(soSanh.BenhLy is not KetQuaBenhLy disease)return;
        bool byPatients=DiseaseRanking.SelectedIndex!=1;
        disease=disease with {CacBenh=BaoCaoDuLieu.XepHangBenh(disease.CacBenh,byPatients)};
        soSanh=soSanh with {BenhLy=disease,XepTheoBenhNhan=byPatients};
        DiseasePanel.Visibility=Visibility.Visible;DiseaseGrid.ItemsSource=disease.CacBenh.Take(10);
        var q=disease.ChatLuong;
        DiseaseQuality.Text=$"{q.ThongBaoXepHang}\nBao phủ mã: {(q.DoBaoPhu is decimal rate?rate.ToString("N2",Vietnamese)+"%":"Không có mẫu số")} · Chưa phân loại: {q.LuotChuaPhanLoai:N0} lượt · Dòng thiếu mã: {q.DongChuaGanMa:N0} · Dòng trùng: {q.DongTrungMa:N0}.";
        DiseaseEmpty.Visibility=disease.CacBenh.Length==0?Visibility.Visible:Visibility.Collapsed;
        DiseaseEmpty.Text=q.TongLuot>0?"Chưa có mã bệnh/tình trạng để xếp hạng. Lượt khám mới cần lưu lựa chọn danh mục của bác sĩ; hồ sơ cũ cần được rà soát chuyên môn.":"Không phát sinh lượt khám trong kỳ đã chọn.";
        DecisionAdvice.Text=q.LuotCoMa<q.TongLuot?"Ưu tiên hoàn thiện mã bệnh trước khi dùng thứ hạng để dự trù vật tư hoặc bố trí chuyên khoa. Có thể dùng số lượt khám, bệnh nhân duy nhất và thu ròng để đối chiếu hoạt động trong kỳ."
            :"Đối chiếu nhóm bệnh/tình trạng với lịch bác sĩ, cơ cấu dịch vụ và nhu cầu vật tư. Số lượt tăng có thể do tái khám; cần xem đồng thời số bệnh nhân duy nhất.";
        CompositionTitle.Text="Bệnh/tình trạng thường gặp";CompositionSubtitle.Text=byPatients?"Xếp theo số bệnh nhân duy nhất":"Xếp theo số lượt khám";
        var safe=disease.CacBenh.Take(10).Where(x=>!x.DaLamMo).ToArray();
        var max=safe.Length==0?1:safe.Max(x=>(byPatients?x.SoBenhNhan:x.SoLuotKham)??0);
        CompositionItems.ItemsSource=safe.Select(x=>new{Ten=x.TenTinhTrang,HienThi=byPatients?x.HienThiBenhNhan:x.HienThiLuot,TiLe=max==0?0:100d*((byPatients?x.SoBenhNhan:x.SoLuotKham)??0)/max});
        CompositionEmpty.Visibility=safe.Length==0?Visibility.Visible:Visibility.Collapsed;
        CompositionEmpty.Text=disease.CacBenh.Length==0?(q.TongLuot>0?"Chưa thể xếp hạng vì chưa có mã bệnh/tình trạng.":"Không phát sinh lượt khám trong kỳ."):"Các nhóm hiện có dưới 5 bệnh nhân; xem thông báo trong bảng.";
        if(report.Ma=="20") DetailsPanel.Visibility=Visibility.Collapsed;
    }
}
