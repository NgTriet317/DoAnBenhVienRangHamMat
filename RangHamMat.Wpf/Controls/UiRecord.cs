// PHẦN AI ĐÃ THỰC HIỆN: AI đã bổ sung chú thích tiếng Việt cho UiRecord.cs: Đọc trường từ bản ghi nhiều dạng và chuyển dictionary thành ET đúng kiểu; là điểm chuyển dữ liệu giữa binding/form và các đối tượng thực thể. Giải thích được đặt cạnh khai báo/hàm tương ứng.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace RangHamMat.Controls;

/// <summary>
/// Đọc trường từ bản ghi nhiều dạng và chuyển dictionary thành ET đúng kiểu; là điểm chuyển dữ liệu giữa binding/form và các đối tượng thực thể.
/// </summary>
public static class UiRecord
{
    /// <summary>
    /// Tạo ET từ dictionary, tra property bằng TypeDescriptor và chuyển kiểu nullable/số/ngày/giờ; báo rõ tên trường nếu giá trị không đúng kiểu.
    /// </summary>
    public static T ChuyenDoi<T>(IDictionary<string, object?> values) where T : new()
    {
        var record = new T();
        foreach (var pair in values)
        {
            var property = TypeDescriptor.GetProperties(record)[pair.Key]
                ?? throw new InvalidOperationException("Trường không có trong ET: " + pair.Key);
            var value = pair.Value is DBNull ? null : pair.Value;
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (value is string empty && empty.Length == 0 && Nullable.GetUnderlyingType(property.PropertyType) is not null) value = null;
            try
            {
                if (value is not null && !type.IsInstanceOfType(value))
                    value = type == typeof(decimal) && value is string text
                        ? decimal.Parse(text.Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)
                        : type == typeof(TimeSpan) ? TimeSpan.Parse(Convert.ToString(value)!, CultureInfo.CurrentCulture)
                        : Convert.ChangeType(value, type, CultureInfo.CurrentCulture);
                property.SetValue(record, value);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException or InvalidCastException)
            { throw new InvalidOperationException("Giá trị " + pair.Key + " không đúng kiểu dữ liệu.", ex); }
        }
        return record;
    }
    /// <summary>
    /// Đọc một trường từ dictionary hoặc property đối tượng; quy đổi DBNull về null để control không hiển thị giá trị SQL rỗng như dữ liệu thật.
    /// </summary>
    public static object? Get(object? row, string field)
    {
        if (row is null) return null;
        var value = row is IDictionary<string, object?> values ? (values.TryGetValue(field, out var item) ? item : null)
            : row is IDictionary dictionary ? dictionary[field]
            : TypeDescriptor.GetProperties(row).Find(field, true)?.GetValue(row);
        return value is DBNull ? null : value;
    }
    /// <summary>
    /// Đọc trường qua Get rồi chuyển sang chuỗi theo culture hiện tại; thiếu dữ liệu trả chuỗi rỗng.
    /// </summary>
    public static string Text(object row, string field) => Convert.ToString(Get(row, field), CultureInfo.CurrentCulture) ?? "";
    /// <summary>
    /// Đọc DateTime sẵn có hoặc thử phân tích chuỗi ngày theo culture hiện tại; trả null nếu không đọc được.
    /// </summary>
    public static DateTime? Date(object row, string field)
        => Get(row, field) is DateTime date ? date : DateTime.TryParse(Text(row, field), CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed) ? parsed : null;
    /// <summary>
    /// Đọc TimeSpan sẵn có hoặc thử phân tích chuỗi giờ; trả null nếu không có giờ hợp lệ.
    /// </summary>
    public static TimeSpan? Time(object row, string field)
        => Get(row, field) is TimeSpan time ? time : TimeSpan.TryParse(Text(row, field), CultureInfo.CurrentCulture, out var parsed) ? parsed : null;
}
