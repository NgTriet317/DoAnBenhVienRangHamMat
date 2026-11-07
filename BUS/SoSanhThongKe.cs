using ET;

namespace BUS;

public static class SoSanhThongKe
{
    public static KetQuaSoSanh Tinh(decimal? hienTai, decimal? kyGoc) =>
        new(hienTai, kyGoc, hienTai - kyGoc, hienTai is null || kyGoc is null || kyGoc == 0
            ? null : (hienTai - kyGoc) / Math.Abs(kyGoc.Value) * 100m);

    public static CapKyThongKe[] TaoCacKy(BoLocThongKe filter, DateTime homNay)
    {
        static DateTime Thang(DateTime d) => new(d.Year, d.Month, 1);
        var from = Thang(filter.TuThang); var end = Thang(filter.DenThang);
        if (from.Year < 1901 || end.Year > 9997 || from > end || (end.Year - from.Year) * 12 + end.Month - from.Month >= 24)
            throw new ArgumentException("Chọn khoảng từ 1 đến 24 tháng, năm từ 1901 đến 9997.");
        if (!Enum.IsDefined(filter.SoSanh)) throw new ArgumentException("Chế độ so sánh không hợp lệ.");
        if (filter.NamGoc is < 1900 or > 9997) throw new ArgumentException("Năm đối chiếu không hợp lệ.");
        if (filter.SoSanh == CachSoSanh.NamDuocChon && from.Year != end.Year)
            throw new ArgumentException("So sánh hai năm: chọn các tháng trong cùng một năm.");
        if (end >= Thang(homNay) && (!filter.LuyKeThang || end != Thang(homNay)))
            throw new ArgumentException("Mặc định chỉ so sánh tháng hoàn chỉnh. Bật lũy kế (MTD) để xem tháng đang diễn ra.");
        var list = new List<CapKyThongKe>();
        for (var month = from; month <= end; month = month.AddMonths(1))
        {
            bool mtd = filter.LuyKeThang && month == Thang(homNay);
            var until = mtd ? homNay.Date.AddDays(1) : month.AddMonths(1);
            DateTime? previous = filter.SoSanh switch
            {
                CachSoSanh.ThangTruoc => month.AddMonths(-1),
                CachSoSanh.CungKyNamTruoc => month.AddYears(-1),
                CachSoSanh.NamDuocChon => new DateTime(filter.NamGoc, month.Month, 1),
                _ => null
            };
            var baseline = previous is DateTime start ? new KySoSanh(start,
                mtd ? start.AddDays(Math.Min(homNay.Day, DateTime.DaysInMonth(start.Year, start.Month))) : start.AddMonths(1)) : null;
            if (baseline is not null && baseline.DenExclusive > (mtd ? homNay.Date.AddDays(1) : Thang(homNay)))
                throw new ArgumentException("Kỳ gốc phải là tháng hoàn chỉnh đã kết thúc hoặc kỳ MTD tương ứng đã diễn ra.");
            list.Add(new(new(month, until), baseline, mtd, mtd
                ? $"Lũy kế tới ngày {homNay:dd/MM/yyyy}; kỳ gốc chốt tối đa tới cuối tháng nếu thiếu ngày tương ứng."
                : "Tháng hoàn chỉnh"));
        }
        return list.ToArray();
    }
}
