// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapper.Contrib.Extensions;
using ET;

// Schema SQL là hợp đồng lưu dữ liệu: tên/kiểu/nullable cột sai hoặc
// cho ghi cột tiền tính toán có thể làm sai dữ liệu dù lớp ET vẫn biên dịch được.
string[] tables =
[
    "BENH_NHAN", "HO_SO_BENH_AN", "PHIEU_KHAM_BENH", "LICH_SU_KHAM_BENH",
    "TINH_TRANG_BENH", "DICH_VU", "KE_HOACH_DIEU_TRI", "CHI_TIET_KE_HOACH",
    "DICH_VU_THUC_HIEN", "DON_THUOC", "CHI_TIET_DON_THUOC", "THUOC",
    "PHIEU_XUAT", "CHI_TIET_PHIEU_XUAT", "HOA_DON", "CHI_TIET_HOA_DON",
    "THANH_TOAN", "LICH_KHAM", "NHAN_VIEN", "PHONG", "CA_LAM",
    "HANG_HOA", "KHO", "LO_HANG_HOA"
];
string[] triggerAmounts = ["KE_HOACH_DIEU_TRI.TongTienDuKien", "HOA_DON.TongTien", "CHI_TIET_HOA_DON.ThanhTien", "PHIEU_NHAP.TongTien"];
var sql = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "QLBenhVienRangHamMat.sql"));
var errors = new List<string>();
var nullability = new NullabilityInfoContext();
var columnCount = 0;

var entityTypes = typeof(ET_BENHNHAN).Assembly.GetTypes().Where(type => type.Name.StartsWith("ET_")).ToArray();
tables = Regex.Matches(sql, @"CREATE TABLE \[dbo\]\.\[(\w+)\]")
    .Select(match => match.Groups[1].Value)
    .Where(table => entityTypes.Any(type => type.Name == "ET_" + table.Replace("_", "") || table == "VAI_TRO" && type.Name == "ET_MAVAITRO"))
    .ToArray();
if (tables.Length != entityTypes.Length) errors.Add("Each ET must map to exactly one SQL table.");
foreach (var table in tables)
{
    var definition = Regex.Match(sql, @"CREATE TABLE \[dbo\]\.\[" + table + @"\] \((.*?)\r?\n\);", RegexOptions.Singleline);
    if (!definition.Success) throw new InvalidOperationException($"SQL table {table} missing.");
    var columns = Regex.Matches(definition.Groups[1].Value, @"^\s*\[(\w+)\]\s+(INT|NVARCHAR|DATE|DATETIME2|TIME|DECIMAL|AS)\b([^\r\n]*)", RegexOptions.Multiline);
    if (columns.Count == 0) throw new InvalidOperationException($"SQL columns for {table} missing.");
    columnCount += columns.Count;
    var type = typeof(ET_BENHNHAN).Assembly.GetType("ET.ET_" + (table == "VAI_TRO" ? "MAVAITRO" : table.Replace("_", "")));
    if (type is null) { errors.Add($"ET for {table} missing."); continue; }
    if (type.GetCustomAttribute<TableAttribute>()?.Name != table) errors.Add($"{type.Name}: [Table(\"{table}\")] missing.");
    if (type.GetConstructor(Type.EmptyTypes) is null) errors.Add($"{type.Name}: parameterless constructor missing.");
    var properties = type.GetProperties();
    if (properties.Length != columns.Count) errors.Add($"{type.Name}: {properties.Length} properties, expected {columns.Count} SQL columns.");
    var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
    if (fields.Length != columns.Count) errors.Add($"{type.Name}: {fields.Length} backing fields, expected {columns.Count}.");
    var constructorTypes = new List<Type>();
    var constructorValues = new List<object>();
    var nullableColumns = new List<bool>();
    var instance = type.GetConstructor(Type.EmptyTypes)?.Invoke(null);

    foreach (Match column in columns)
    {
        var name = column.Groups[1].Value;
        var sqlType = column.Groups[2].Value;
        var tail = column.Groups[3].Value;
        var nullable = sqlType != "AS" && !tail.Contains("NOT NULL", StringComparison.Ordinal);
        var expectedType = sqlType switch
        {
            "INT" => typeof(int),
            "DECIMAL" or "AS" => typeof(decimal),
            "DATE" or "DATETIME2" => typeof(DateTime),
            "TIME" => typeof(TimeSpan),
            "NVARCHAR" => typeof(string),
            _ => throw new InvalidOperationException(sqlType)
        };
        if (nullable && expectedType.IsValueType) expectedType = typeof(Nullable<>).MakeGenericType(expectedType);
        constructorTypes.Add(expectedType);
        var valueType = Nullable.GetUnderlyingType(expectedType) ?? expectedType;
        object sample = valueType == typeof(string) ? $"Mẫu {table}.{name}" :
            valueType == typeof(int) ? 100 + constructorValues.Count :
            valueType == typeof(decimal) ? 123.45m + constructorValues.Count :
            valueType == typeof(TimeSpan) ? TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(constructorValues.Count)) :
            new DateTime(2026, 10, 11, 9, 30, 0).AddDays(constructorValues.Count);
        constructorValues.Add(sample);
        nullableColumns.Add(nullable);
        var property = type.GetProperty(name);
        if (property is null) { errors.Add($"{type.Name}.{name} missing."); continue; }
        if (property.PropertyType != expectedType) errors.Add($"{type.Name}.{name}: {property.PropertyType.Name}, expected {expectedType.Name}.");
        if (expectedType == typeof(string) && (nullability.Create(property).WriteState == NullabilityState.Nullable) != nullable)
            errors.Add($"{type.Name}.{name}: string nullability differs from SQL.");
        var identity = tail.Contains("IDENTITY", StringComparison.Ordinal);
        if ((property.GetCustomAttribute<KeyAttribute>() is not null) != identity)
            errors.Add($"{type.Name}.{name}: [Key] differs from SQL IDENTITY.");
        var explicitKey = !identity && tail.Contains("PRIMARY KEY", StringComparison.Ordinal);
        if ((property.GetCustomAttribute<ExplicitKeyAttribute>() is not null) != explicitKey)
            errors.Add($"{type.Name}.{name}: [ExplicitKey] differs from SQL assigned primary key.");
        var readOnlyAmount = sqlType == "AS" || triggerAmounts.Contains(table + "." + name);
        if (readOnlyAmount != (property.GetCustomAttribute<WriteAttribute>()?.Write == false))
            errors.Add($"{type.Name}.{name}: [Write(false)] differs from SQL derived amount contract.");
        if (!property.CanRead || !property.CanWrite || property.GetMethod?.IsPublic != true || property.SetMethod?.IsPublic != true)
            errors.Add($"{type.Name}.{name}: public get/set missing.");
        var fieldName = name == "SDT" ? "sdt" : char.ToLowerInvariant(name[0]) + name[1..];
        var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field is null || !field.IsPrivate || field.FieldType != expectedType)
            errors.Add($"{type.Name}.{name}: private {fieldName} backing field/type missing.");
        else if (expectedType == typeof(string) && (nullability.Create(field).WriteState == NullabilityState.Nullable) != nullable)
            errors.Add($"{type.Name}.{name}: backing field string nullability differs from SQL.");
        if (instance is not null && property.PropertyType == expectedType && property.CanWrite)
        {
            property.SetValue(instance, sample);
            if (!Equals(property.GetValue(instance), sample) || (field is not null && !Equals(field.GetValue(instance), sample)))
                errors.Add($"{type.Name}.{name}: setter/getter loses supplied value.");
            if (nullable)
            {
                property.SetValue(instance, null);
                if (property.GetValue(instance) is not null) errors.Add($"{type.Name}.{name}: setter loses SQL NULL.");
            }
        }
    }

    var fullConstructor = type.GetConstructor(constructorTypes.ToArray());
    if (fullConstructor is null)
        errors.Add($"{type.Name}: full constructor matching all {columns.Count} SQL columns in order missing.");
    else
    {
        var parameters = fullConstructor.GetParameters();
        for (var index = 0; index < parameters.Length; index++)
            if (constructorTypes[index] == typeof(string) && (nullability.Create(parameters[index]).WriteState == NullabilityState.Nullable) != nullableColumns[index])
                errors.Add($"{type.Name}.{columns[index].Groups[1].Value}: constructor string nullability differs from SQL.");
        var constructed = fullConstructor.Invoke(constructorValues.ToArray());
        for (var index = 0; index < columns.Count; index++)
        {
            var name = columns[index].Groups[1].Value;
            if (!Equals(type.GetProperty(name)?.GetValue(constructed), constructorValues[index]))
                errors.Add($"{type.Name}.{name}: full constructor loses supplied value.");
        }
        var withNulls = constructorValues.Select((value, index) => nullableColumns[index] ? null : value).ToArray();
        constructed = fullConstructor.Invoke(withNulls);
        for (var index = 0; index < columns.Count; index++)
            if (!Equals(type.GetProperty(columns[index].Groups[1].Value)?.GetValue(constructed), withNulls[index]))
                errors.Add($"{type.Name}.{columns[index].Groups[1].Value}: full constructor loses SQL NULL or non-null value.");
    }
}

foreach (var error in errors) Console.Error.WriteLine(error);
Console.WriteLine($"ET schema: {tables.Length} tables, {columnCount} columns, {errors.Count} failures.");
return errors.Count == 0 ? 0 : 1;
