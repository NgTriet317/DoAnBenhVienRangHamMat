// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ quy tắc trình bày trạng thái, tuần lịch bắt đầu thứ hai và định dạng số/ngày; đã giải thích đầu vào và kết quả từng hàm dùng ở giao diện.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Globalization;
namespace RangHamMat.Controls;
/// <summary>
/// Quy tắc trình bày tiến độ bước kế hoạch, phạm vi lịch và phân tích số từ ô nhập; các kiểm tra SQL cuối cùng vẫn thuộc tầng dữ liệu.
/// </summary>
public static class QuyTacHienThi
{
    /// <summary>
    /// Giữ trạng thái hủy/hoàn thành đã ghi, nếu số lượng thực hiện đủ thì hiện Hoàn thành, nếu mới thực hiện một phần thì hiện Đang thực hiện.
    /// </summary>
    public static string PlanStepStatus(string recordedStatus, decimal plannedQuantity, decimal completedQuantity)
    {
        if (recordedStatus == "Hủy" || recordedStatus == "Hoàn thành") return recordedStatus;
        if (plannedQuantity > 0 && completedQuantity >= plannedQuantity) return "Hoàn thành";
        return completedQuantity > 0 ? "Đang thực hiện" : recordedStatus;
    }
    /// <summary>
    /// Tính khoảng lịch ngày/tuần/tháng: tuần bắt đầu thứ Hai, tháng dùng lưới 42 ngày và 7 cột để hiển thị đủ tuần giao nhau.
    /// </summary>
    public static (DateTime Start, int Days, int Columns) Period(DateTime selected, string mode)
    {
        var date = selected.Date;
        if (mode == "Day") return (date, 1, 1);
        if (mode == "Month")
        {
            var first = new DateTime(date.Year, date.Month, 1);
            return (first.AddDays(-((int)first.DayOfWeek + 6) % 7), 42, 7);
        }
        return (date.AddDays(-((int)date.DayOfWeek + 6) % 7), 7, 7);
    }
    /// <summary>
    /// Phân tích số lượng thuốc dương và số ngày dùng nguyên dương; chỉ chấp nhận dấu phẩy hoặc chấm làm dấu thập phân, không nhận phân cách hàng nghìn.
    /// </summary>
    public static bool TryPrescriptionNumbers(string quantityText, string daysText, out decimal quantity, out int days)
    {
        // Nhận dấu phẩy hoặc chấm làm dấu thập phân; không hiểu dấu này là phân cách hàng nghìn.
        var parsed = TryNonNegativeNumber(quantityText,out quantity);
        return int.TryParse(daysText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out days) && parsed && quantity > 0 && days > 0;
    }
    /// <summary>
    /// Đổi dấu phẩy thành chấm rồi phân tích decimal không âm theo InvariantCulture; dùng cho số lượng/giá nhập ở giao diện.
    /// </summary>
    public static bool TryNonNegativeNumber(string text,out decimal value)
        => decimal.TryParse(text.Trim().Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out value) && value>=0;
}
