using System.Globalization;
using System.Text;
using BUS;
using ET;

namespace RangHamMat.Controls;

public sealed record DongSoSanhBaoCao(string ChiTieu,string DonVi,string Ky,string KyGoc,
    decimal? HienTai,decimal? GiaTriGoc,decimal? ChenhLech,decimal? PhanTram,string GhiChu);

public sealed record DuLieuSoSanhBaoCao(CapKyThongKe[] CacKy,DongSoSanhBaoCao[] CacDong,
    KetQuaBenhLy? BenhLy,DateTime ThoiDiemTai,BoLocThongKe BoLoc,DongSoSanhBaoCao[] TongCong,bool XepTheoBenhNhan=true)
{
    public static DuLieuSoSanhBaoCao Doc(ChuyenDeBaoCao report,BoLocThongKe filter,
        Func<string,DateTime,DateTime,PhatSinhBaoCao[]> doc)
    {
        var periods=SoSanhThongKe.TaoCacKy(filter,DateTime.Today);
        var rows=new List<DongSoSanhBaoCao>();
        var cache=new Dictionary<KySoSanh,decimal?[]>();
        var clinicalCache=new Dictionary<KySoSanh,KetQuaBenhLy>();
        KetQuaBenhLy Clinical(KySoSanh ky)
        {
            if(!clinicalCache.TryGetValue(ky,out var value))clinicalCache[ky]=value=new BUS_BAOCAO().DocBenhLy(ky,filter.MaCK,filter.MaDV);
            return value;
        }
        decimal?[] Read(KySoSanh ky)
        {
            if(cache.TryGetValue(ky,out var found))return found;
            decimal?[] values;
            if(report.Ma=="20")
            {
                var q=Clinical(ky).ChatLuong;
                values=[q.TongLuot,q.SoBenhNhan,q.LuotCoMa,q.LuotChuaPhanLoai];
            }
            else
            {
                var total=BaoCaoDuLieu.TongHop(report.Ma,doc(report.Ma,ky.TuNgay,ky.DenExclusive.AddDays(-1)),ky.TuNgay,ky.DenExclusive.AddDays(-1),"Tháng").Tong;
                if(report.Ma=="00")
                {
                    var q=Clinical(ky).ChatLuong;
                    // Nguồn phát sinh 00 dùng Hai cho thu ròng; bệnh nhân/% phải tính lại trên toàn kỳ.
                    values=[q.TongLuot,q.SoBenhNhan,total.Hai,q.DoBaoPhu];
                }
                else values=[total.Mot,total.Hai,total.Ba,total.Bon??0];
            }
            cache[ky]=values;return values;
        }
        foreach(var period in periods)
        {
            var current=Read(period.HienTai); var previous=period.KyGoc is null?null:Read(period.KyGoc);
            for(int i=0;i<4;i++)
            {
                var delta=SoSanhThongKe.Tinh(current[i],previous?[i]);
                rows.Add(new(report.ChiSo[i],report.DonVi[i],period.HienTai.Nhan,period.KyGoc?.Nhan??"Không so sánh",
                    delta.HienTai,delta.KyGoc,delta.ChenhLech,delta.PhanTram,period.GhiChu+" · "+delta.DienGiai));
            }
        }
        KetQuaBenhLy? disease=null;
        if(report.Ma is "00" or "20")
        {
            var whole=new KySoSanh(periods[0].HienTai.TuNgay,periods[^1].HienTai.DenExclusive);
            disease=Clinical(whole);
            // Tổng nhiều tháng không cộng DISTINCT bệnh nhân từ các tháng.
            if(periods[0].KyGoc is KySoSanh firstBase && periods[^1].KyGoc is KySoSanh lastBase)
            {
                var baseline=new KySoSanh(firstBase.TuNgay,lastBase.DenExclusive);
                var old=Clinical(baseline).CacBenh.ToDictionary(x=>x.MaDMTT);
                foreach(var item in disease.CacBenh)
                {
                    // Khi nhóm ở một kỳ bị che, không xuất Δ để suy ngược ra nhóm nhỏ.
                    var before=old.GetValueOrDefault(item.MaDMTT);
                    item.BienDong=SoSanhThongKe.Tinh(item.SoLuotKham,before?.DaLamMo==true?null:before?.SoLuotKham??0);
                }
            }
            disease=disease with {CacBenh=BaoCaoDuLieu.XepHangBenh(disease.CacBenh)};
        }
        var wholePeriod=new KySoSanh(periods[0].HienTai.TuNgay,periods[^1].HienTai.DenExclusive);
        var wholeBase=periods[0].KyGoc is KySoSanh first && periods[^1].KyGoc is KySoSanh last?new KySoSanh(first.TuNgay,last.DenExclusive):null;
        var totals=Read(wholePeriod);var baseTotals=wholeBase is null?null:Read(wholeBase);
        var summary=Enumerable.Range(0,4).Select(i=>
        {
            var delta=SoSanhThongKe.Tinh(totals[i],baseTotals?[i]);
            return new DongSoSanhBaoCao(report.ChiSo[i],report.DonVi[i],"Tổng "+wholePeriod.Nhan,wholeBase?.Nhan??"Không so sánh",delta.HienTai,delta.KyGoc,delta.ChenhLech,delta.PhanTram,delta.DienGiai);
        }).ToArray();
        return new(periods,rows.ToArray(),disease,DateTime.Now,filter,summary);
    }

    public string TaoCsv(string nguoiXuat,DateTime lucXuat)
    {
        static string Cell(string text)=>"\""+((text.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@')?"'":"")+text.Replace("\"","\"\"")+"\"";
        static string Num(decimal? n)=>n?.ToString(CultureInfo.InvariantCulture)??"";
        var result=new StringBuilder();
        void Line(params string[] cells)=>result.AppendLine(string.Join(",",cells.Select(Cell)));
        Line("So sánh kỳ","Người xuất",nguoiXuat,"Lúc xuất",lucXuat.ToString("O"),"Tải lúc",ThoiDiemTai.ToString("O"));
        Line("Bộ lọc","So sánh",BoLoc.SoSanh.ToString(),"MTD",BoLoc.LuyKeThang.ToString(),"Chuyên khoa",BoLoc.MaCK??"Tất cả","Dịch vụ tiếp nhận",BoLoc.MaDV??"Tất cả");
        Line("Chỉ tiêu","Đơn vị","Kỳ này","Kỳ gốc","Giá trị kỳ này","Giá trị gốc","Chênh lệch","%","Diễn giải");
        foreach(var row in TongCong.Concat(CacDong))
            result.AppendLine(string.Join(",",new[]{Cell(row.ChiTieu),Cell(row.DonVi),Cell(row.Ky),Cell(row.KyGoc),Num(row.HienTai),Num(row.GiaTriGoc),Num(row.ChenhLech),Num(row.PhanTram),Cell(row.GhiChu)}));
        if(BenhLy is not null)
        {
            var q=BenhLy.ChatLuong;
            Line();Line("Chất lượng mã",q.CanhBao,"Độ bao phủ (%)",Num(q.DoBaoPhu),"Lượt chưa phân loại",q.LuotChuaPhanLoai.ToString(),"Dòng thiếu mã",q.DongChuaGanMa.ToString(),"Dòng trùng mã",q.DongTrungMa.ToString());
            Line("Đọc kết quả",q.ThongBaoXepHang,"Xếp theo",XepTheoBenhNhan?"Số bệnh nhân duy nhất":"Số lượt khám");
            Line("Tình trạng ghi nhận","Mã","Lượt khám","Bệnh nhân","Tỷ trọng lượt đã có mã (%)","Chênh lệch lượt","%","Ghi chú");
            foreach(var d in BenhLy.CacBenh.Take(10))
                result.AppendLine(string.Join(",",new[]{Cell(d.TenTinhTrang),Cell(d.MaDMTT),Num(d.SoLuotKham),Num(d.SoBenhNhan),Num(d.TyTrong),Num(d.BienDong?.ChenhLech),Num(d.BienDong?.PhanTram),Cell(d.DaLamMo?"Ẩn nhóm dưới 5 bệnh nhân":d.HienThiBienDong)}));
        }
        return result.ToString();
    }
}
