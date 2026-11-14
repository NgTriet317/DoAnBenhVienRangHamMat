using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>
/// Phần mở rộng của màn hình báo cáo để áp dụng quyền, đối chiếu các kỳ và hiển thị chất lượng mã bệnh.
/// </summary>
public partial class BaoCao
{
    /// <summary>
    /// Tập quyền báo cáo đã xác thực cho phiên mở màn hình hiện tại.
    /// </summary>
    private QuyenBaoCao quyen;
    /// <summary>
    /// Thông báo lỗi xác thực quyền để màn hình giải thích vì sao không thể tải báo cáo.
    /// </summary>
    private string? loiQuyen;
    /// <summary>
    /// Kết quả so sánh đã tải, dùng chung cho bảng, biểu đồ và xuất dữ liệu.
    /// </summary>
    private DuLieuSoSanhBaoCao? soSanh;
    /// <summary>
    /// Kiểm tra quyền của chuyên đề đang chọn; khi chưa chọn dùng mã tổng quan 00.
    /// </summary>
    private bool CoQuyenChuyenDe => ChinhSachBaoCao.ChoPhep(selected?.Ma??"00",quyen);

    /// <summary>
    /// Xóa kết quả và vô hiệu hóa tải báo cáo khi định danh phiên đã thay đổi.
    /// </summary>
    private void PhienBaoCaoThayDoi()
    {
        XoaKetQua();quyen=QuyenBaoCao.Khong;
        LoadButton.IsEnabled=false;ReportCatalog.ItemsSource=null;
        ThongBao("Phiên đăng nhập đã thay đổi. Mở lại Báo cáo để xác thực quyền.",true);
    }

    /// <summary>
    /// Áp dụng lại bộ lọc khi người dùng đổi năm đối chiếu.
    /// </summary>
    private void NamGoc_Changed(object sender,TextChangedEventArgs e) => BoLoc_Changed(sender,e);
    /// <summary>
    /// Cập nhật danh mục chuyên đề theo lựa chọn mở rộng danh sách.
    /// </summary>
    private void DanhMucMoRong_Changed(object sender,RoutedEventArgs e) => HienDanhMuc();
    /// <summary>
    /// Vẽ lại bảng và biểu đồ bệnh lý khi đổi tiêu chí xếp hạng và đã có dữ liệu.
    /// </summary>
    private void XepHangBenh_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(ready&&selected is not null&&soSanh?.BenhLy is not null)HienSoSanh(selected);
    }

    /// <summary>
    /// Đọc khoảng tháng, chế độ so sánh và bộ lọc chuyên khoa/dịch vụ; yêu cầu năm hợp lệ khi so với năm chỉ định.
    /// </summary>
    /// <returns>Bộ lọc thống kê để tính các kỳ và giới hạn nguồn báo cáo.</returns>
    private BoLocThongKe DocBoLoc()
    {
        var from=KiemTraNhapLieu.ReadDate(FromDate)??throw new LoiNhapLieu(nameof(FromDate),"Vui lòng nhập Từ ngày hoặc chọn ngày trên lịch.");
        var to=KiemTraNhapLieu.ReadDate(ToDate)??throw new LoiNhapLieu(nameof(ToDate),"Vui lòng nhập Đến ngày hoặc chọn ngày trên lịch.");
        if(ComparisonMode.SelectedIndex<0||!Enum.IsDefined((CachSoSanh)ComparisonMode.SelectedIndex))
            throw new ArgumentException("Chế độ so sánh không hợp lệ. Chọn một mục trong danh sách.");
        return new()
        {
            TuThang=from,DenThang=to,
            SoSanh=(CachSoSanh)ComparisonMode.SelectedIndex,LuyKeThang=MonthToDate.IsChecked==true,
            NamGoc=DocNamDoiChieu(BaselineYear,ComparisonMode.SelectedIndex==3),
            MaCK=selected?.Ma=="20"?SpecialtyCode.SelectedValue as string:null,MaDV=selected?.Ma=="20"?ServiceCode.SelectedValue as string:null
        };
    }

    /// <summary>
    /// Đưa cùng kết quả so sánh lên bảng, chỉ số và biểu đồ; chỉ vẽ kỳ có số liệu và giữ quy tắc làm mờ nhóm bệnh nhỏ.
    /// </summary>
    /// <remarks>Nguồn tổng đã tải và soSanh được phối hợp để hiển thị chỉ tiêu cùng đơn vị; dữ liệu thiếu mẫu số không bị chuyển thành 0.</remarks>
    /// <param name="report">Chuyên đề cung cấp tên chỉ tiêu, đơn vị và quy tắc diễn giải.</param>
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
