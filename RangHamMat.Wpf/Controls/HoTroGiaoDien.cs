// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ các hàm dùng chung: tìm khung làm việc, theo dõi đổi dữ liệu, đọc/lọc ComboBox và converter cho binding; đã giải thích vòng đời đăng ký sự kiện.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BUS;
using ET;
using System.Data;

namespace RangHamMat.Controls;

/// <summary>
/// Một lựa chọn ComboBox: Value là mã lưu/so sánh, Label là nhãn tiếng Việt hiện cho người dùng.
/// </summary>
public sealed record LuaChonDanhMuc(string Value, string Label);
/// <summary>
/// Hợp đồng cho form có luồng lưu riêng: Commit trả true khi lưu được, false cùng thông báo error nếu kiểm tra hoặc lưu thất bại.
/// </summary>
public interface IBieuMauLuu
{
    /// <summary>
    /// Hàm lưu do form triển khai: trả true khi thành công, false và thông báo error khi kiểm tra/lưu thất bại; nơi gọi dùng kết quả để commit hoặc rollback.
    /// </summary>
    bool Commit(out string error);
}
/// <summary>
/// Hợp đồng cho màn hình gắn với bệnh nhân; PatientId giúp workspace giữ phạm vi hồ sơ khi mở form hoặc chuyển phân hệ.
/// </summary>
public interface IPatientScoped
{
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    string? PatientId { get; }
}

/// <summary>
/// Converter đọc trường bản ghi và đổi ngày/khóa ngoại thành chuỗi dễ đọc cho DataTemplate; chỉ hỗ trợ binding một chiều.
/// </summary>
public sealed class RecordValueConverter : IValueConverter
{
    /// <summary>
    /// Đọc trường được chỉ định trong ConverterParameter, định dạng ngày hoặc tra nhãn khóa ngoại; thiếu dữ liệu hiện Chưa có thông tin.
    /// </summary>
    public object Convert(object value, Type type, object parameter, CultureInfo culture)
    {
        if (value is null || value == DependencyProperty.UnsetValue) return "Chưa có thông tin";
        var field = System.Convert.ToString(parameter) ?? "";
        var data = UiRecord.Get(value, field);
        if (data is DateTime date) return date.ToString(field == "NgaySinh" ? "dd/MM/yyyy" : "dd/MM/yyyy HH:mm", culture);
        var text = DuLieuGiaoDien.Lookup(field, data);
        return string.IsNullOrWhiteSpace(text) ? "Chưa có thông tin" : text;
    }
    /// <summary>
    /// Không hỗ trợ ghi ngược từ nhãn hiển thị về bản ghi; các trường này chỉ dùng để đọc.
    /// </summary>
    public object ConvertBack(object value, Type type, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Hàm dùng chung để tìm workspace, quản lý đăng ký Changed, đọc lựa chọn ComboBox và lọc dữ liệu bảng theo hồ sơ/từ khóa/trạng thái.
/// </summary>
public static class HoTroGiaoDien
{
    /// <summary>
    /// Đi từ control lên các phần tử cha trong cây visual/logical để tìm AppWorkspace đang chứa màn hình.
    /// </summary>
    public static AppWorkspace? Workspace(DependencyObject child)
    {
        DependencyObject? current = child;
        while (current is not null)
        {
            if (current is AppWorkspace workspace) return workspace;
            current = current is System.Windows.Media.Visual ? System.Windows.Media.VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
        }
        return null;
    }
    // Chỉ nghe sự kiện khi control còn hiển thị; hủy đăng ký để sự kiện tĩnh không giữ trang đã đóng.
    /// <summary>
    /// Đăng ký Changed khi control Loaded, gọi refresh và hủy đăng ký khi Unloaded để sự kiện tĩnh không giữ các màn hình đã đóng trong bộ nhớ.
    /// </summary>
    public static void Observe(UserControl control, Action refresh)
    {
        var listening = false;
        EventHandler changed = (_, _) => refresh();
        control.Loaded += (_, _) => { if (!listening) { DuLieuGiaoDien.Changed += changed; listening = true; } refresh(); };
        control.Unloaded += (_, _) => { if (listening) { DuLieuGiaoDien.Changed -= changed; listening = false; } };
    }
    /// <summary>
    /// Đọc nội dung ComboBoxItem đang chọn; khi chưa chọn hoặc control chưa có thì trả Tất cả.
    /// </summary>
    public static string Choice(ComboBox? box) => (box?.SelectedItem as ComboBoxItem)?.Content as string ?? "Tất cả";
    /// <summary>
    /// Đọc mã chuỗi trong SelectedValue của ComboBox; chưa chọn trả chuỗi rỗng.
    /// </summary>
    public static string Selected(ComboBox? box) => box?.SelectedValue as string ?? "";
    /// <summary>
    /// Tạo ListCollectionView trên cache của bảng, gắn predicate lọc và đặt làm ItemsSource của DataGrid.
    /// </summary>
    public static ICollectionView Bind(DataGrid grid, string table, Predicate<object> filter)
    {
        var view = new ListCollectionView(DuLieuGiaoDien.Rows(table)) { Filter = filter };
        grid.ItemsSource = view;
        return view;
    }
    /// <summary>
    /// Lọc theo bệnh nhân, trạng thái và các từ tìm kiếm; ghép cả tên/SĐT bệnh nhân và bác sĩ, dòng thuốc kế thừa trạng thái đơn cha.
    /// </summary>
    public static bool Matches(string table, object row, string? query, string status, string? patient = null)
    {
        if (!string.IsNullOrEmpty(patient) && DuLieuGiaoDien.PatientId(table, row) != patient) return false;
        var state = UiRecord.Text(row, "TrangThai");
        if (state.Length == 0 && table == "CHI_TIET_DON_THUOC")
        {
            var parent = DuLieuGiaoDien.Find("DON_THUOC", UiRecord.Get(row, "MaDT"));
            state = parent is null ? "" : UiRecord.Text(parent, "TrangThai");
        }
        if (status != "Tất cả" && state.Length > 0 && !state.Equals(status, StringComparison.CurrentCultureIgnoreCase)) return false;
        var text = query?.Trim() ?? "";
        if (text.Length == 0) return true;
        var values = row is IDictionary<string, object?> dictionary ? dictionary.Values.Select(value => System.Convert.ToString(value) ?? "") : TypeDescriptor.GetProperties(row).Cast<PropertyDescriptor>().Select(property => System.Convert.ToString(property.GetValue(row)) ?? "");
        var patientRow = DuLieuGiaoDien.Find("BENH_NHAN", DuLieuGiaoDien.PatientId(table, row));
        var combined = string.Join(" ", values) + " " + (patientRow is null ? "" : DuLieuGiaoDien.Label("BENH_NHAN", patientRow) + " " + UiRecord.Text(patientRow, "SDT")) + " " + DuLieuGiaoDien.Lookup("MaBS", UiRecord.Get(row, "MaBS"));
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(term => combined.Contains(term, StringComparison.CurrentCultureIgnoreCase));
    }
}

