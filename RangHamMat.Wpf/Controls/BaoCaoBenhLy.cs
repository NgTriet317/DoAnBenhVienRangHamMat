using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BUS;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Bộ lọc tháng và trình bày đối chiếu bệnh lý, dùng kết quả đã tải cho bảng và biểu đồ.</summary>
public partial class BaoCao
{
    private bool daMoBenhLy;

    private void ThangBenhLy_Changed(object sender, RoutedEventArgs e)
    {
        if(!ready||selected?.Ma!="20")return;
        MonthToDate.IsChecked=int.TryParse(DiseaseYear.Text,out var year)&&year==DateTime.Today.Year&&DiseaseMonth.SelectedIndex==DateTime.Today.Month-1;
        BoLoc_Changed(sender,e);
    }

    private DateTime DocThangBenhLy()
    {
        if(!KiemTraLuaChon(DiseaseMonth,"Tháng cần xem"))throw new LoiNhapLieu(nameof(DiseaseMonth),"Chọn tháng cần xem.");
        if(!int.TryParse(DiseaseYear.Text.Trim(),NumberStyles.None,CultureInfo.InvariantCulture,out var year)||year is <1901 or >9997)
        {
            const string message="Năm thống kê: nhập số nguyên từ 1901 đến 9997.";
            KiemTraNhapLieu.SetError(DiseaseYear,message);throw new LoiNhapLieu(nameof(DiseaseYear),message);
        }
        KiemTraNhapLieu.SetError(DiseaseYear,"");
        return new(year,DiseaseMonth.SelectedIndex+1,1);
    }

    private bool KiemTraBoLocBenhLy(out DateTime from,out DateTime to)
    {
        from=to=default;
        bool valid=KiemTraLuaChon(ComparisonMode,"Chế độ so sánh");
        valid=KiemTraLuaChon(SpecialtyCode,"Chuyên khoa của bác sĩ")&&valid;
        valid=KiemTraLuaChon(ServiceCode,"Dịch vụ tiếp nhận")&&valid;
        if(BaselineMonth.IsEnabled)valid=KiemTraLuaChon(BaselineMonth,"Tháng đối chiếu")&&valid;
        try
        {
            var filter=DocBoLoc();
            if(valid)
            {
                var pair=SoSanhThongKe.TaoCacKy(filter,DateTime.Today).Single();
                from=pair.HienTai.TuNgay;to=pair.HienTai.DenExclusive.AddDays(-1);
            }
        }
        catch(LoiNhapLieu){valid=false;}
        catch(ArgumentException ex){BaoLoiSoSanh(ex);valid=false;}
        LoiBoLoc(valid?"":"Sửa tháng/năm hoặc các ô được đánh dấu rồi tải lại báo cáo.");
        return valid;
    }

    private void HienTongQuanBenhLy(bool byPatients)
    {
        if(soSanh?.BenhLy is not KetQuaBenhLy disease)return;
        var first=soSanh.CacKy[0];var last=soSanh.CacKy[^1];
        string current=first.HienTai.TuNgay.ToString("MM/yyyy")+(soSanh.CacKy.Length>1?$" – {last.HienTai.TuNgay:MM/yyyy}":"");
        string baseline=first.KyGoc is null?"Không so sánh":first.KyGoc.TuNgay.ToString("MM/yyyy")+(soSanh.CacKy.Length>1?$" – {last.KyGoc!.TuNgay:MM/yyyy}":"");
        DiseasePeriodNote.Text=$"Kỳ cần xem: {first.HienTai.TuNgay:dd/MM/yyyy} – {last.HienTai.DenExclusive.AddDays(-1):dd/MM/yyyy}"+
            (first.KyGoc is null?" · Không so sánh":$" · Đối chiếu: {first.KyGoc.TuNgay:dd/MM/yyyy} – {last.KyGoc!.DenExclusive.AddDays(-1):dd/MM/yyyy}");
        DiseaseMostCommon.Text=ThongKeBenhLy.PhoBienNhat(disease,byPatients);
        DiseaseBaselineMostCommon.Text=soSanh.BenhLyGoc is KetQuaBenhLy old
            ?$"Kỳ đối chiếu {baseline}: {ThongKeBenhLy.PhoBienNhat(old,byPatients)}":"";
        DiseasePatientsColumn.Header=$"BN {current}";DiseaseBasePatientsColumn.Header=first.KyGoc is null?"BN đối chiếu":$"BN {baseline}";
        DiseaseChangeColumn.Header=byPatients?"Tăng / giảm BN":"Tăng / giảm lượt";
        DiseaseChangeColumn.Binding=new Binding(byPatients?nameof(DongSoSanhBenh.HienThiBienDongBenhNhan):nameof(DongSoSanhBenh.HienThiBienDongLuot));
        if(soSanh.BenhLyGoc is not null)
            DiseaseQuality.Text+=$"\nKỳ đối chiếu: {soSanh.BenhLyGoc.ChatLuong.ThongBaoXepHang}";
        if(selected?.Ma!="20")return;
        HienChiSoBenhLy(disease);
        HienDanhSachBenhNhan();
        FilterHint.Text="Chọn hai tháng bất kỳ, cùng năm hoặc khác năm. MTD đối chiếu cùng số ngày đã diễn ra.";
        SummaryPanel.Visibility=ComparisonPanel.Visibility=Visibility.Collapsed;
        TrendTitle.Text="So sánh bệnh / tình trạng giữa hai kỳ";
        TrendSubtitle.Text=byPatients?"Số bệnh nhân đến khám theo từng bệnh":"Số lượt khám theo từng bệnh";
        var rows=soSanh.CacBenhSoSanh.Where(x=>(byPatients?x.BenhNhanHienTai:x.LuotHienTai) is not null&&
            (first.KyGoc is null||(byPatients?x.BenhNhanGoc:x.LuotGoc) is not null)).Take(10).ToArray();
        ReportChart.Unit=ReportChart.SecondaryUnit=byPatients?"người":"lượt";
        ReportChart.PrimaryLabel=current;ReportChart.SecondaryLabel=first.KyGoc is null?"":baseline;
        ReportChart.MinWidth=Math.Max(300,rows.Length*150+94);
        ReportChart.Points=rows.Select(x=>new ChartPoint(x.TenTinhTrang,
            (byPatients?x.BenhNhanHienTai:x.LuotHienTai)!.Value,(byPatients?x.BenhNhanGoc:x.LuotGoc)??0,x.TenTinhTrang));
        TrendCaption.Text=(rows.Length==0?"Chưa có nhóm đủ số liệu công bố để vẽ biểu đồ. ":"")+
            "Tối đa 10 tình trạng theo tiêu chí xếp hạng; bảng và CSV có toàn bộ nhóm. Chỉ vẽ nhóm có đủ số liệu ở hai kỳ. "+last.GhiChu;
    }

    /// <summary>Bốn chỉ số nghiệp vụ; số bệnh nhân đếm mỗi người một lần trong kỳ, tách khỏi lượt khám.</summary>
    private void HienChiSoBenhLy(KetQuaBenhLy? disease)
    {
        var q=disease?.ChatLuong;
        var visible=disease?.CacBenh.Where(x=>!x.DaLamMo&&x.SoBenhNhan is >0).ToArray()??[];
        var max=visible.Length==0?0:visible.Max(x=>x.SoBenhNhan!.Value);
        var top=visible.Where(x=>x.SoBenhNhan==max).Select(x=>x.TenTinhTrang).ToArray();
        string name=disease is null?"—":q!.TongLuot==0?"Chưa có lượt khám":top.Length==0?"Chưa đủ dữ liệu":string.Join("; ",top);
        string BaseValue(int metric)=>soSanh?.TongCong[metric].GiaTriGoc is decimal n
            ?$"Đối chiếu: {n.ToString("N0",Vietnamese)} · {soSanh.TongCong[metric].GhiChu}":"";
        MetricItems.ItemsSource=new[]
        {
            new ChiSoBaoCao("Số bệnh nhân đến khám",q?.SoBenhNhan.ToString("N0",Vietnamese)??"—","người",Colors[1],
                "Mỗi bệnh nhân chỉ tính một lần trong kỳ, dù có nhiều lần khám.",BaseValue(1)),
            new ChiSoBaoCao("Tổng lượt khám",q?.TongLuot.ToString("N0",Vietnamese)??"—","lượt",Colors[0],
                "Mỗi lần khám đã xác nhận tính một lượt; bao gồm các lần tái khám.",BaseValue(0)),
            new ChiSoBaoCao("Số loại bệnh / tình trạng",disease?.CacBenh.Length.ToString("N0",Vietnamese)??"—","loại",Colors[2],
                "Số mã bệnh/tình trạng khác nhau đã ghi nhận trong kỳ; một bệnh nhân có thể có nhiều tình trạng.",
                soSanh?.BenhLyGoc is KetQuaBenhLy old?$"Đối chiếu: {old.CacBenh.Length:N0} loại":""),
            new ChiSoBaoCao("Tình trạng gặp nhiều nhất",name,"theo số bệnh nhân",Colors[3],
                disease is null?"Tải báo cáo để xem tình trạng gặp nhiều nhất.":ThongKeBenhLy.PhoBienNhat(disease),
                max>0?$"{max:N0} bệnh nhân"+(top.Length>1?" · Đồng hạng":""):"",17)
        };
    }

    private void TimBenhNhanBaoCao_Changed(object sender, TextChangedEventArgs e)
    {
        if(ready)HienDanhSachBenhNhan();
    }

    /// <summary>Tìm trong dữ liệu đã tải, không thay đổi bộ lọc kỳ và không tính một lượt thành nhiều dòng.</summary>
    private void HienDanhSachBenhNhan()
    {
        PatientVisitsPanel.Visibility=selected?.Ma=="20"?Visibility.Visible:Visibility.Collapsed;
        if(selected?.Ma!="20")return;
        bool allowed=ChinhSachBaoCao.ChoPhepChiTiet("20",quyen);
        PatientVisitSearch.IsEnabled=allowed&&soSanh is not null;
        if(!allowed||soSanh is null)
        {
            PatientVisitsGrid.ItemsSource=null;
            PatientVisitsNote.Text=allowed?"Tải báo cáo để xem bệnh nhân đến khám trong tháng đã chọn."
                :"Tài khoản hiện tại chưa được cấp quyền xem chi tiết bệnh nhân và dữ liệu lâm sàng.";
            return;
        }
        var words=BaoCaoDuLieu.BoDau(PatientVisitSearch.Text.Trim()).Split(' ',StringSplitOptions.RemoveEmptyEntries);
        var all=soSanh.ChiTietBenhNhan;
        var rows=all.Where(x=>words.All(word=>BaoCaoDuLieu.BoDau($"{x.MaBN} {x.HoTenBN} {x.MaLS} {x.DichVuKham} {x.ChanDoan} {x.TinhTrangBenh} {x.BacSi}").Contains(word))).ToArray();
        PatientVisitsGrid.ItemsSource=rows;
        PatientVisitsNote.Text=all.Length==0?"Không có lượt khám đã xác nhận trong kỳ và bộ lọc đang chọn.":
            rows.Length==0?"Không tìm thấy bệnh nhân hoặc nội dung khám phù hợp. Thử từ khóa khác.":
            $"{rows.Length:N0}/{all.Length:N0} lượt khám · {rows.Select(x=>x.MaBN).Distinct().Count():N0} bệnh nhân trong danh sách · Mỗi dòng là một lần khám, có thể gồm nhiều tình trạng.";
    }
}
