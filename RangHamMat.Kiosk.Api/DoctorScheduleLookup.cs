using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;

// Cong cu tra cuu da duoc phep. KHONG chay SQL do model viet ra.
// Khong truy cap ho so benh an, tai khoan, so dien thoai hay ly do nghi viec.
sealed class ScheduleIntent
{
    public string? PhamVi { get; set; }     // hom_nay, ngay_mai, tuan_nay, tuan_sau, ngay_cu_the, 7_ngay_toi, chua_ro
    public string? Ngay { get; set; }        // yyyy-MM-dd, chi dung neu co ngay cu the
    public string? TenBacSi { get; set; }    // ten duoc nguoi benh hoi
    public string? Buoi { get; set; }        // sang, chieu, toi, tat_ca
}

sealed class DoctorShiftRow
{
    public string HoTenBacSi { get; set; } = "";
    public DateTime NgayLam { get; set; }
    public TimeSpan GioBatDau { get; set; }
    public TimeSpan GioKetThuc { get; set; }
    public string TenPhong { get; set; } = "";
}

static class DoctorScheduleLookup
{
    private const string PrefixNeedMore = "Để tra cứu lịch làm việc,";
    private static readonly Regex ScheduleWords = new(
        @"\b(?:bác\s*sĩ|bsi|bs\.?|lịch\s*(?:làm\s*việc|trực|khám)|ca\s*(?:trực|làm)|trực\s*(?:bệnh\s*viện|hôm|ngày)|ai\s*(?:đang\s*)?(?:trực|khám|làm\s*việc))\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex WorkWords = new(
        @"(?:làm\s*(?:việc|ngày|ca|không|ko|hôm|thứ|mai)|đi\s*làm|có\s*(?:lịch|trực|khám|làm)|lịch|trực|ca\s*(?:sáng|chiều|tối)|khám\s*(?:ngày|hôm|khi\s*nào)|ngày\s*(?:mai|nào|mấy)|mai\s+(?:bác|bs|có)|thứ\s*[2-7]|tuần)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex DateWords = new(@"(?<!\d)(?:\d{1,2}[/.-]\d{1,2}[/.-]\d{4}|\d{4}-\d{1,2}-\d{1,2})(?!\d)",
        RegexOptions.Compiled);

    public static bool IsScheduleQuestion(string text, IReadOnlyList<AiHistoryMessage> history)
    {
        if (history.Count > 0 && history[^1].Role == "TroLy"
            && history[^1].Text.StartsWith(PrefixNeedMore, StringComparison.OrdinalIgnoreCase)) return true;
        // Chi chan cau hoi ve lich, khong chan cau tra loi ho ten/ngay sinh thong thuong.
        bool cue = ScheduleWords.IsMatch(text);
        return cue && (WorkWords.IsMatch(text) || Regex.IsMatch(text,@"\b(?:khi\s*nào|hôm\s*nào|ngày\s*nào)\b",RegexOptions.IgnoreCase));
    }

    public static async Task<string> AnswerAsync(SqlConnection db, ScheduleIntent request,
        string patientQuestion, CancellationToken ct)
    {
        DateTime today = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date;
        string scope = (request.PhamVi ?? "chua_ro").ToLowerInvariant().Trim();
        string period = (request.Buoi ?? "tat_ca").ToLowerInvariant().Trim();
        if (period is not ("sang" or "chieu" or "toi")) period = "tat_ca";
        if (Regex.IsMatch(patientQuestion,@"(?:ca|buổi|sáng)\s+sáng|sáng\s+mai",RegexOptions.IgnoreCase)) period="sang";
        else if (Regex.IsMatch(patientQuestion,@"(?:ca|buổi)\s+chiều|chiều\s+mai",RegexOptions.IgnoreCase)) period="chieu";
        else if (Regex.IsMatch(patientQuestion,@"(?:ca|buổi)\s+tối|tối\s+mai",RegexOptions.IgnoreCase)) period="toi";
        var dateText = DateWords.Match(patientQuestion).Value;
        DateTime from = default, to = default;
        bool hasDate=false;
        // Ngay cu the benh nhan go la nguon uu tien. Khong de model thay ngay.
        if (dateText.Length>0 && TryDate(dateText,out DateTime supplied))
        { from=to=supplied; hasDate=true; }
        else if (Regex.IsMatch(patientQuestion,
            @"\b(?:ngày\s+mai|sáng\s+mai|chiều\s+mai|tối\s+mai|mai\s+(?:bác|có|ai|bs|trực))\b",
            RegexOptions.IgnoreCase))
        {from=to=today.AddDays(1);hasDate=true;}
        else if (patientQuestion.Contains("hôm nay",StringComparison.OrdinalIgnoreCase))
        {from=to=today;hasDate=true;}
        else if (scope=="hom_nay") {from=to=today;hasDate=true;}
        else if (scope=="ngay_mai") {from=to=today.AddDays(1);hasDate=true;}
        else if (scope=="tuan_nay" || scope=="tuan_sau")
        {
            int diff = ((int)today.DayOfWeek+6)%7;
            from=today.AddDays(-diff+(scope=="tuan_sau"?7:0));
            if (from<today) from=today; // khong tra lich ca da qua
            to=today.AddDays(-diff+(scope=="tuan_sau"?13:6));
            hasDate=true;
        }
        else if (scope=="7_ngay_toi") {from=today;to=today.AddDays(6);hasDate=true;}
        else if (scope=="ngay_cu_the" && TryDate(request.Ngay,out DateTime chosen))
        {from=to=chosen;hasDate=true;}
        if (!hasDate)
            return PrefixNeedMore + " bạn muốn xem ngày nào? Bạn có thể hỏi ví dụ: “Bác sĩ Bảo có làm ngày mai không?” hoặc “Ngày mai có những bác sĩ nào làm việc?”.";
        if (from < today || to > today.AddDays(60) || to<from || (to-from).TotalDays>6)
            return "Hiện tại tôi chỉ tra cứu lịch đã công bố trong 60 ngày tới, theo từng ngày hoặc tuần. Bạn muốn xem ngày nào trong khoảng đó?";

        string? doctor = string.IsNullOrWhiteSpace(request.TenBacSi) ? null : request.TenBacSi.Trim();
        if (doctor != null)
        {
            doctor=Regex.Replace(doctor,@"^(?:bác\s*sĩ|bs\.?|bsi)\s*", "",RegexOptions.IgnoreCase).Trim();
            if (doctor.Length<2) doctor=null;
            if (doctor is { Length: > 100 }) doctor=doctor[..100];
        }
        if (patientQuestion.Contains("bác sĩ nào",StringComparison.OrdinalIgnoreCase)
            || patientQuestion.Contains("ai trực",StringComparison.OrdinalIgnoreCase)
            || patientQuestion.Contains("những bác sĩ",StringComparison.OrdinalIgnoreCase)) doctor=null;

        using var grids = await db.QueryMultipleAsync(new CommandDefinition(
            "dbo.AI_TraCuuLichBacSi",new {TuNgay=from,DenNgay=to,TenBacSi=doctor,Buoi=period},
            commandType:System.Data.CommandType.StoredProcedure,cancellationToken:ct));
        var doctorNames=(await grids.ReadAsync<string>()).ToList();
        var shifts=(await grids.ReadAsync<DoctorShiftRow>()).ToList();

        string range=from==to?from.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture)
            : $"{from:dd/MM/yyyy} đến {to:dd/MM/yyyy}";
        string filter = period switch {"sang"=>" (ca sáng)","chieu"=>" (ca chiều)","toi"=>" (ca tối)",_=>""};
        if (doctorNames.Count==0 && doctor!=null)
            return $"Tôi chưa tìm thấy bác sĩ có tên phù hợp với “{doctor}” trong danh sách bác sĩ hiện đang công tác. Bạn có thể cho biết tên đầy đủ hoặc hỏi lịch các bác sĩ khác không?";
        if (shifts.Count==0)
            return $"Tôi đã kiểm tra lịch {range}{filter}{(doctor!=null?$" của bác sĩ {doctor}":"")}, nhưng hiện chưa có ca làm việc được xác nhận phù hợp trong hệ thống. Điều này không đồng nghĩa bác sĩ chắc chắn nghỉ. Bạn có thể hỏi nhân viên tiếp nhận để xác minh thêm.";

        var answer = new StringBuilder($"Theo lịch làm việc ĐÃ XÁC NHẬN tại bệnh viện, từ {range}{filter}:\n");
        foreach (var shift in shifts.Take(20))
        {
            answer.Append("• Bác sĩ ").Append(shift.HoTenBacSi)
                .Append(" — ").Append(shift.NgayLam.ToString("dd/MM",CultureInfo.InvariantCulture))
                .Append(", ").Append(shift.GioBatDau.ToString(@"hh\:mm",CultureInfo.InvariantCulture))
                .Append("–").Append(shift.GioKetThuc.ToString(@"hh\:mm",CultureInfo.InvariantCulture))
                .Append(", ").Append(shift.TenPhong).Append('\n');
        }
        if (shifts.Count>20) answer.Append("Còn các ca khác chưa hiển thị. Bạn có thể hỏi theo tên bác sĩ hoặc ngày cụ thể.\n");
        answer.Append("Đây là lịch làm việc, không phải lịch hẹn hoặc xác nhận còn chỗ. Lịch có thể thay đổi; nhân viên sẽ kiểm tra lại khi tiếp nhận. Bạn có thể tiếp tục khai báo thông tin khám.");
        return answer.ToString();
    }

    private static bool TryDate(string? raw, out DateTime value)
    {
        return DateTime.TryParseExact(raw?.Trim(),
            new [] {"yyyy-MM-dd","yyyy-M-d","dd/MM/yyyy","d/M/yyyy","dd-MM-yyyy","d-M-yyyy","dd.MM.yyyy","d.M.yyyy"},
            CultureInfo.InvariantCulture,DateTimeStyles.None,out value);
    }
}
