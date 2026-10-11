using System.Globalization;
namespace RangHamMat.Controls;
public static class ClinicalRules
{
    public static string PlanStepStatus(string recordedStatus, decimal plannedQuantity, decimal completedQuantity)
    {
        if (recordedStatus == "Hủy" || recordedStatus == "Hoàn thành") return recordedStatus;
        if (plannedQuantity > 0 && completedQuantity >= plannedQuantity) return "Hoàn thành";
        return completedQuantity > 0 ? "Đang thực hiện" : recordedStatus;
    }
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
    public static bool TryPrescriptionNumbers(string quantityText, string daysText, out decimal quantity, out int days)
    {
        // Accept one comma OR one dot as the decimal separator; never treat it as a thousands separator.
        var parsed = TryNonNegativeNumber(quantityText,out quantity);
        return int.TryParse(daysText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out days) && parsed && quantity > 0 && days > 0;
    }
    public static bool TryNonNegativeNumber(string text,out decimal value)
        => decimal.TryParse(text.Trim().Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out value) && value>=0;
}
