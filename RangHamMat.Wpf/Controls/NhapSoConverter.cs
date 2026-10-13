// PHẦN AI ĐÃ THỰC HIỆN: tạo converter cho ô số lượng/đơn giá; chặn dấu phân cách hàng nghìn, số âm, precision/scale sai tại binding.
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System.Windows.Controls;

namespace RangHamMat.Controls;

/// <summary>Đọc dấu phẩy/chấm như phần thập phân thống nhất giữa form và DataGrid; đầu vào sai trả UnsetValue để không gây lỗi runtime.</summary>
public sealed class NhapSoConverter : IValueConverter
{
    /// <summary>Hiện số nháp theo InvariantCulture để không chèn dấu phân cách hàng nghìn.</summary>
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
        => System.Convert.ToString(value,CultureInfo.InvariantCulture)??"";

    /// <summary>Kiểm tra số nhập và giới hạn v8 trước khi cập nhật model; giữ model cũ nếu sai. ValidationRule cung cấp thông báo lỗi.</summary>
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)
    {
        var text=System.Convert.ToString(value)?.Trim()??"";
        if(!QuyTacHienThi.TryNonNegativeNumber(text,out var number))
            return DependencyProperty.UnsetValue;
        var field=System.Convert.ToString(parameter)=="Price"?"DonGiaDuKien":"SoLuongDuKien";
        try {KiemTraNhapLieu.ValidateValues("CHI_TIET_KE_HOACH",new Dictionary<string,object?> {[field]=number});}
        catch(LoiNhapLieu){return DependencyProperty.UnsetValue;}
        return number;
    }
}

/// <summary>AI tạo: WPF kiểm tra chuỗi trước khi chuyển sang model, hiện lỗi tiếng Việt và chặn commit ô sai.</summary>
public sealed class SoNhapValidationRule : ValidationRule
{
    /// <summary>Quantity kiểm tra số lượng decimal(18,3); Price kiểm tra đơn giá decimal(18,2).</summary>
    public string Field {get;set;}="Quantity";
    /// <summary>Không ném ngoại lệ: trả kết quả hợp lệ hoặc nội dung lỗi để WPF vẽ viền đỏ cho ô đang sửa.</summary>
    public override ValidationResult Validate(object value,CultureInfo cultureInfo)
    {
        var text=System.Convert.ToString(value)?.Trim()??"";
        if(!QuyTacHienThi.TryNonNegativeNumber(text,out var number))
            return new ValidationResult(false,"Nhập số không âm; dùng dấu phẩy/chấm cho phần lẻ, không dùng phân cách hàng nghìn.");
        var field=Field=="Price"?"DonGiaDuKien":"SoLuongDuKien";
        try {KiemTraNhapLieu.ValidateValues("CHI_TIET_KE_HOACH",new Dictionary<string,object?> {[field]=number});return ValidationResult.ValidResult;}
        catch(LoiNhapLieu ex){return new ValidationResult(false,ex.Message);}
    }
}
