using BUS;
using ET;

namespace RangHamMat.Controls;

/// <summary>So sánh điều hành từ gói đã đọc nhất quán, dùng chung cho chỉ số, biểu đồ, bảng và CSV.</summary>
public sealed partial record DuLieuSoSanhBaoCao
{
    public GoiTongQuanDieuHanh? TongQuan { get; init; }

    public static DuLieuSoSanhBaoCao DocTongQuan(ChuyenDeBaoCao report,BoLocThongKe filter,GoiTongQuanDieuHanh packet)
    {
        if(report.Ma!="00")throw new ArgumentException("Gói điều hành chỉ dùng cho Tổng quan.");
        var pairs=SoSanhThongKe.TaoCacKy(filter,DateTime.Today);
        var whole=new KySoSanh(pairs[0].HienTai.TuNgay,pairs[^1].HienTai.DenExclusive);
        if(packet.KyHienTai!=whole)throw new ArgumentException("Gói điều hành không khớp kỳ đang chọn.");
        var rows=new List<DongSoSanhBaoCao>();
        DongSoSanhBaoCao[] Compare(KySoSanh current,KySoSanh? baseline,string label,string note)
        {
            var a=packet.CacKy[current].ChiSo;
            var b=baseline is null?null:packet.CacKy[baseline].ChiSo;
            return Enumerable.Range(0,4).Select(i=>
            {
                var d=SoSanhThongKe.Tinh(a[i],b?[i]);
                return new DongSoSanhBaoCao(report.ChiSo[i],report.DonVi[i],label,baseline?.Nhan??"Không so sánh",
                    d.HienTai,d.KyGoc,d.ChenhLech,d.PhanTram,note+" · "+d.DienGiai);
            }).ToArray();
        }
        foreach(var pair in pairs)rows.AddRange(Compare(pair.HienTai,pair.KyGoc,pair.HienTai.Nhan,pair.GhiChu));
        var wholeBase=pairs[0].KyGoc is KySoSanh first&&pairs[^1].KyGoc is KySoSanh last?new KySoSanh(first.TuNgay,last.DenExclusive):null;
        var totals=Compare(whole,wholeBase,"Tổng "+whole.Nhan,"So sánh trên toàn kỳ; bệnh nhân đếm mỗi người một lần");
        return new(pairs,rows.ToArray(),null,packet.ThoiDiemTai,filter,totals){TongQuan=packet};
    }
}
