using ET;

namespace BUS;

/// <summary>
/// Tính biến động và ghép kỳ thống kê, gồm kỳ tháng đầy đủ và kỳ lũy kế đến hôm nay.
/// </summary>
public static class SoSanhThongKe
{
    /// <summary>
    /// Tính chênh lệch và phần trăm theo trị tuyệt đối kỳ gốc, giữ null khi thiếu dữ liệu.
    /// </summary>
    /// <param name="hienTai">Giá trị kỳ hiện tại; null nếu không có dữ liệu được phép so sánh.</param>
    /// <param name="kyGoc">Giá trị đối chiếu; bằng 0 thì không tính phần trăm.</param>
    /// <returns>Giá trị hai kỳ, chênh lệch và tỷ lệ phần trăm có thể tính được.</returns>
    public static KetQuaSoSanh Tinh(decimal? hienTai, decimal? kyGoc) =>
        new(hienTai, kyGoc, hienTai - kyGoc, hienTai is null || kyGoc is null || kyGoc == 0
            ? null : (hienTai - kyGoc) / Math.Abs(kyGoc.Value) * 100m);

    /// <summary>
    /// Kiểm tra bộ lọc rồi tạo tối đa 24 cặp kỳ tháng với mốc kết thúc không bao gồm.
    /// </summary>
    /// <remarks>Kỳ gốc phải đã diễn ra; lũy kế dùng cùng số ngày và chặn ở cuối tháng gốc nếu tháng đó ngắn hơn.</remarks>
    /// <param name="filter">Khoảng tháng, chế độ đối chiếu và lựa chọn lũy kế cần tạo.</param>
    /// <param name="homNay">Ngày chốt dùng xác định tháng hiện tại và độ dài kỳ lũy kế.</param>
    /// <returns>Các kỳ hiện tại và kỳ gốc tương ứng; kỳ gốc null khi không chọn so sánh.</returns>
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
