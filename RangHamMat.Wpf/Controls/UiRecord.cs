using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace RangHamMat.Controls;

public static class UiRecord
{
    public static object? Get(object row, string field)
    {
        var value = row is IDictionary dictionary ? dictionary[field] : TypeDescriptor.GetProperties(row)[field]?.GetValue(row);
        return value is DBNull ? null : value;
    }
    public static string Text(object row, string field) => Convert.ToString(Get(row, field), CultureInfo.CurrentCulture) ?? "";
    public static DateTime? Date(object row, string field)
        => Get(row, field) is DateTime date ? date : DateTime.TryParse(Text(row, field), CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed) ? parsed : null;
    public static TimeSpan? Time(object row, string field)
        => Get(row, field) is TimeSpan time ? time : TimeSpan.TryParse(Text(row, field), CultureInfo.CurrentCulture, out var parsed) ? parsed : null;
}
