using System.Globalization;
using System.Text;
using BUS;
using ET;

namespace RangHamMat.Controls;

/// <summary>
/// Một chỉ tiêu so sánh giữa hai kỳ, giữ giá trị null khi không thể công bố hoặc tính chênh lệch.
/// </summary>
/// <param name="ChiTieu">Tên chỉ tiêu được thống kê.</param>
/// <param name="DonVi">Đơn vị của cả giá trị hiện tại và kỳ gốc.</param>
/// <param name="Ky">Nhãn kỳ hiện tại.</param>
/// <param name="KyGoc">Nhãn kỳ đối chiếu hoặc thông báo không so sánh.</param>
/// <param name="HienTai">Giá trị kỳ hiện tại; null khi không có giá trị công bố.</param>
/// <param name="GiaTriGoc">Giá trị kỳ đối chiếu nếu có.</param>
/// <param name="ChenhLech">Hiệu giữa kỳ hiện tại và kỳ gốc khi đủ dữ liệu.</param>
/// <param name="PhanTram">Tỷ lệ biến động; null khi không đủ điều kiện tính.</param>
/// <param name="GhiChu">Giải thích phạm vi kỳ và ý nghĩa kết quả so sánh.</param>
public sealed record DongSoSanhBaoCao(string ChiTieu,string DonVi,string Ky,string KyGoc,
    decimal? HienTai,decimal? GiaTriGoc,decimal? ChenhLech,decimal? PhanTram,string GhiChu);

/// <summary>
/// Kết quả so sánh dùng chung cho hiển thị và CSV, gồm từng kỳ, tổng toàn khoảng và chất lượng dữ liệu bệnh lý.
/// </summary>
/// <param name="CacKy">Các cặp kỳ hiện tại và kỳ gốc đã được xác định từ bộ lọc.</param>
/// <param name="CacDong">Các chỉ tiêu so sánh cho từng cặp kỳ.</param>
/// <param name="BenhLy">Thống kê bệnh lý toàn khoảng, nếu chuyên đề có sử dụng.</param>
/// <param name="ThoiDiemTai">Thời điểm lấy dữ liệu để ghi nguồn khi xuất.</param>
/// <param name="BoLoc">Bộ lọc đã dùng để tải và tính kết quả.</param>
/// <param name="TongCong">Các chỉ tiêu tổng được tính trên toàn khoảng thời gian.</param>
/// <param name="XepTheoBenhNhan">True khi bảng bệnh lý xếp theo số bệnh nhân duy nhất.</param>
public sealed record DuLieuSoSanhBaoCao(CapKyThongKe[] CacKy,DongSoSanhBaoCao[] CacDong,
    KetQuaBenhLy? BenhLy,DateTime ThoiDiemTai,BoLocThongKe BoLoc,DongSoSanhBaoCao[] TongCong,bool XepTheoBenhNhan=true)
{
    /// <summary>
    /// Tổng hợp từng cặp kỳ và toàn khoảng, dùng bộ nhớ đệm để tránh đọc lại; bệnh nhân duy nhất được tính lại cho cả khoảng.
    /// </summary>
    /// <remarks>DocBenhLy cung cấp số bệnh nhân duy nhất và chất lượng mã; khi kỳ gốc bị làm mờ, biến động nhóm không được công bố.</remarks>
    /// <param name="report">Chuyên đề xác định bốn chỉ tiêu và đơn vị cần đối chiếu.</param>
    /// <param name="filter">Khoảng tháng, kiểu so sánh và các mã giới hạn nguồn.</param>
    /// <param name="doc">Hàm đọc phát sinh theo mã chuyên đề, từ ngày và đến ngày gồm cả hai đầu.</param>
    /// <returns>Kết quả gồm biến động từng kỳ, tổng toàn khoảng và thống kê bệnh lý nếu chuyên đề yêu cầu.</returns>
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

    /// <summary>
    /// Xuất đúng kết quả đã tải ra CSV kèm bộ lọc, thời điểm và chất lượng mã; bảo vệ ô văn bản có tiền tố công thức.
    /// </summary>
    /// <param name="nguoiXuat">Tên người xuất được ghi vào phần thông tin nguồn.</param>
    /// <param name="lucXuat">Thời điểm thực hiện xuất, tách biệt với thời điểm tải dữ liệu.</param>
    /// <returns>Nội dung CSV gồm tổng, từng kỳ và tối đa 10 nhóm bệnh theo kết quả hiện có.</returns>
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
