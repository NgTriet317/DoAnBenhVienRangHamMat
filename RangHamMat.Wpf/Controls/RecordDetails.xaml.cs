// PHẦN AI ĐÃ THỰC HIỆN: AI đã chuyển vùng chi tiết sang RecordDetails.xaml, tạo dữ liệu binding trường/bảng liên quan và converter cho ô, giữ bản ghi gốc để điều hướng, lọc thông tin mật khẩu và thêm constructor cho Designer.
// AI hỗ trợ: chuyển bố cục chi tiết sang XAML và chú thích tiếng Việt ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RangHamMat.Controls;

/// <summary>Ngữ cảnh thêm dòng con; form khóa trường cha để bản ghi mới thuộc đúng hồ sơ đang xem.</summary>
public sealed record CreationContext(string Table, string ParentField, object ParentValue);

/// <summary>Nhóm bản ghi liên quan giữ DataGrid nguồn và ngữ cảnh thêm mới để bảo toàn điều hướng.</summary>
public sealed record RelatedRows(string Title, string Module, DataGrid Source, object[] Rows, CreationContext? Creation);

/// <summary>AI hỗ trợ: dữ liệu nhãn/giá trị cho mẫu trường XAML, không chứa đối tượng giao diện.</summary>
/// <param name="Label">Nhãn lấy từ đầu cột nguồn.</param>
/// <param name="Value">Chuỗi đã định dạng để hiển thị, gồm thông báo khi thiếu thông tin.</param>
public sealed record DetailField(string Label, string Value);

/// <summary>AI hỗ trợ: mô tả cột được phép đọc; loại bỏ trường mật khẩu và giá trị xác thực trước khi tạo mẫu.</summary>
/// <param name="Label">Nhãn hiển thị gốc của cột dữ liệu.</param>
/// <param name="Key">Tên trường được UiRecord đọc trên bản ghi gốc.</param>
public sealed record DetailColumn(string Label, string Key);

/// <summary>AI hỗ trợ: dữ liệu nhóm liên quan; Rows giữ nguyên bản ghi để nhấp đúp đi qua OpenDetails hiện có.</summary>
public sealed class RelatedDetail
{
    /// <summary>AI hỗ trợ: tiêu đề nhóm liên quan từ cấu hình quan hệ của workspace.</summary>
    public string Title { get; }
    /// <summary>AI hỗ trợ: nhãn nút thêm dòng con phù hợp tên nhóm hiện tại.</summary>
    public string AddLabel => "Thêm " + Title.ToLower(CultureInfo.CurrentCulture);
    /// <summary>AI hỗ trợ: Tag điều hướng được routed handler của workspace đọc khi bấm nút thêm.</summary>
    public string AddTag { get; }
    /// <summary>AI hỗ trợ: Tag bảng gốc để workspace nhận diện bảng khi nhấp đúp dòng liên quan.</summary>
    public object SourceTag { get; }
    /// <summary>AI hỗ trợ: khóa cha được truyền qua CommandParameter cho luồng thêm mới hiện có.</summary>
    public CreationContext? Creation { get; }
    /// <summary>AI hỗ trợ: bản ghi nguyên gốc, không bọc thành model làm thay đổi khóa và điều hướng.</summary>
    public object[] Rows { get; }
    /// <summary>AI hỗ trợ: danh sách cột an toàn dùng cho mẫu trường trong một cột DataGrid khai báo ở XAML.</summary>
    public DetailColumn[] Columns { get; }

    /// <summary>AI hỗ trợ: sao chép thông tin quan hệ và danh sách cột; không tạo control hoặc cột giao diện mới.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: AI hỗ trợ: sao chép thông tin quan hệ và danh sách cột; không tạo control hoặc cột giao diện mới.</remarks>
    public RelatedDetail(RelatedRows relation)
    {
        Title = relation.Title;
        AddTag = "MoCuaSo:" + relation.Module;
        SourceTag = relation.Source.Tag;
        Creation = relation.Creation;
        Rows = relation.Rows;
        Columns = RecordDetails.ReadColumns(relation.Source);
    }
}

/// <summary>AI hỗ trợ: chi tiết bản ghi dùng toàn bộ bố cục XAML; C# chỉ chuẩn bị trường và quan hệ để binding.</summary>
public sealed partial class RecordDetails : UserControl
{
    /// <summary>AI hỗ trợ: các trường của bản ghi cha, đã loại dữ liệu mật khẩu/hash trước khi binding.</summary>
    public DetailField[] Fields { get; } = Array.Empty<DetailField>();
    /// <summary>AI hỗ trợ: các nhóm liên quan giữ bản ghi nguồn, bảng và ngữ cảnh thêm mới.</summary>
    public RelatedDetail[] Relations { get; } = Array.Empty<RelatedDetail>();

    /// <summary>Nạp bố cục trống trong XAML Designer; constructor nhận dữ liệu được workspace dùng khi người dùng mở một bản ghi thật.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: Nạp bố cục trống trong XAML Designer; constructor nhận dữ liệu được workspace dùng khi người dùng mở một bản ghi thật.</remarks>
    public RecordDetails() { InitializeComponent(); DataContext = this; }

    /// <summary>AI hỗ trợ: nạp XAML và dữ liệu đã chọn; bố cục, trường, bảng con và footer nằm trong RecordDetails.xaml.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: AI hỗ trợ: nạp XAML và dữ liệu đã chọn; bố cục, trường, bảng con và footer nằm trong RecordDetails.xaml.</remarks>
    public RecordDetails(DataGrid source, object record, IEnumerable<RelatedRows> related)
    {
        Fields = ReadFields(record, ReadColumns(source));
        Relations = related.Select(relation => new RelatedDetail(relation)).ToArray();
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>AI hỗ trợ: lấy cột văn bản gốc và bỏ trường xác thực để chi tiết cha/con không lộ mật khẩu, hash hay salt.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: AI hỗ trợ: lấy cột văn bản gốc và bỏ trường xác thực để chi tiết cha/con không lộ mật khẩu, hash hay salt.</remarks>
    internal static DetailColumn[] ReadColumns(DataGrid source) => source.Columns.OfType<DataGridTextColumn>()
        .Where(column => !string.IsNullOrWhiteSpace(column.SortMemberPath)
            && !new[] { "MatKhau", "Password", "Hash", "Salt" }.Any(secret => column.SortMemberPath.Contains(secret, StringComparison.OrdinalIgnoreCase)))
        .Select(column => new DetailColumn(Convert.ToString(column.Header) ?? "", column.SortMemberPath)).ToArray();

    /// <summary>AI hỗ trợ: đọc các trường được phép và định dạng ngày như màn hình chi tiết gốc; dữ liệu rỗng có nhãn rõ ràng.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: AI hỗ trợ: đọc các trường được phép và định dạng ngày như màn hình chi tiết gốc; dữ liệu rỗng có nhãn rõ ràng.</remarks>
    internal static DetailField[] ReadFields(object record, DetailColumn[] columns) => columns.Select(column =>
    {
        var value = UiRecord.Get(record, column.Key);
        var display = value is DateTime date ? date.ToString("dd/MM/yyyy HH:mm") : Convert.ToString(value);
        return new DetailField(column.Label, string.IsNullOrEmpty(display) ? "Chưa có thông tin" : display);
    }).ToArray();
}

/// <summary>AI hỗ trợ: ghép bản ghi con nguyên gốc với mô tả cột bằng MultiBinding, không sinh DataGridColumn trong C#.</summary>
public sealed class RelatedFieldsConverter : IMultiValueConverter
{
    /// <summary>AI hỗ trợ: trả dữ liệu trường cho ItemsControl trong ô XAML; lúc binding chưa sẵn sàng trả danh sách trống.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: AI hỗ trợ: trả dữ liệu trường cho ItemsControl trong ô XAML; lúc binding chưa sẵn sàng trả danh sách trống.</remarks>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length == 2 && values[0] != DependencyProperty.UnsetValue && values[1] is DetailColumn[] columns
            ? RecordDetails.ReadFields(values[0], columns) : Array.Empty<DetailField>();

    /// <summary>AI hỗ trợ: màn hình chỉ đọc nên không ghi ngược giá trị đã định dạng vào bản ghi nguồn.</summary>
    /// <remarks>AI đã tạo/chỉnh sửa phần xử lý giao diện này: AI hỗ trợ: màn hình chỉ đọc nên không ghi ngược giá trị đã định dạng vào bản ghi nguồn.</remarks>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
