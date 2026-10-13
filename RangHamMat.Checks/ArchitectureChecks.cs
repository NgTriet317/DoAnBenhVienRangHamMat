// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>
/// Bộ kiểm tra tĩnh kiến trúc nguồn để phát hiện thiếu/sai bộ ET-DAL-BUS và luồng gọi không đúng tầng.
/// </summary>
internal static class ArchitectureChecks
{
    /// <summary>
    /// Rà kiến trúc nguồn: sự tương ứng ET/DAL/BUS và việc điều phối đúng tầng, báo lỗi nếu xuất hiện cấu trúc không đáp ứng quy ước của dự án.
    /// </summary>
    public static void Run(string root, List<string> errors)
    {
        KiemTraBoCucXaml(root, errors);
        KiemTraQuyTacNhapLieu(root,errors);
        var entities = Directory.GetFiles(Path.Combine(root, "ET"), "ET_*.cs");
        foreach (var entity in entities)
        {
            var suffix = Path.GetFileNameWithoutExtension(entity)[3..];
            foreach (var layer in new[] { "DAL", "BUS" })
            {
                var path = Path.Combine(root, layer, layer + "_" + suffix + ".cs");
                if (!File.Exists(path)) errors.Add($"Missing {layer} for ET_{suffix}");
                else if (!File.ReadAllText(path).Contains("ET_" + suffix)) errors.Add($"{layer}_{suffix} does not use its ET");
            }
        }
        foreach (var layer in new[] { "DAL", "BUS" })
            foreach (var path in Directory.GetFiles(Path.Combine(root, layer), layer + "_*.cs"))
                if (!File.Exists(Path.Combine(root, "ET", "ET_" + Path.GetFileNameWithoutExtension(path)[4..] + ".cs")))
                    errors.Add("Layer without corresponding ET: " + Path.GetFileName(path));
        foreach (var folder in new[] { "DAL", "BUS", "RangHamMat.Wpf" })
            foreach (var path in Directory.GetFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj")))
                if (Regex.IsMatch(File.ReadAllText(path), @"\b(BUS_LAMSANG|DAL_GIAODICH)\b"))
                    errors.Add("Shared business coordinator remains: " + Path.GetRelativePath(root, path));
        var connection = File.ReadAllText(Path.Combine(root, "DAL", "dbconnect.cs"));
        if (Regex.IsMatch(connection, @"\b(BENH_NHAN|DON_THUOC|LICH_KHAM|DICH_VU_THUC_HIEN|TongTienDuKien|statusOnly)\b"))
            errors.Add("dbconnect contains entity-specific business rules");
        foreach (var path in Directory.GetFiles(Path.Combine(root, "RangHamMat.Wpf"), "*", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj")))
            if (Regex.IsMatch(Path.GetFileName(path), "Clinical|LamSang")) errors.Add("Nonfunctional UI filename: " + Path.GetFileName(path));
        Console.WriteLine($"Architecture: checked {entities.Length} ET/DAL/BUS triplets.");
    }

    /// <summary>AI tạo: đối chiếu metadata nhập liệu với SQL để thay đổi schema không làm giao diện dùng giới hạn cũ.</summary>
    private static void KiemTraQuyTacNhapLieu(string root,List<string> errors)
    {
        var path=Path.Combine(root,"RangHamMat.Wpf","Controls","QuyTacNhapLieu.xml");
        if(!File.Exists(path)){errors.Add("Thiếu quy tắc nhập liệu giao diện.");return;}
        var actual=XDocument.Load(path).Root!.Elements("Bang")
            .SelectMany(table=>table.Elements("Cot").Select(column=>(Key:(string)table.Attribute("Ten")!+"."+(string)column.Attribute("Ten")!,Column:column)))
            .ToDictionary(item=>item.Key,item=>item.Column);
        var expected=new HashSet<string>();
        var sql=File.ReadAllText(Path.Combine(root,"Database","QLBenhVienRangHamMat.sql"));
        foreach(Match table in Regex.Matches(sql,@"CREATE TABLE \[dbo\]\.\[(\w+)\] \(([\s\S]*?)\r?\n\);"))
        foreach(Match column in Regex.Matches(table.Groups[2].Value,@"(?m)^\s+\[(\w+)\]\s+(\w+)(?:\(([^)]*)\))?\s+(NOT NULL|NULL)"))
        {
            var key=table.Groups[1].Value+"."+column.Groups[1].Value;expected.Add(key);
            var primary=Regex.IsMatch(table.Groups[2].Value,@"\["+Regex.Escape(column.Groups[1].Value)+@"\][^\r\n]*PRIMARY KEY");
            if(!actual.TryGetValue(key,out var rule) || (string?)rule.Attribute("Kieu")!=column.Groups[2].Value.ToLowerInvariant() ||
                (string?)rule.Attribute("QuyCach")!=column.Groups[3].Value || (bool?)rule.Attribute("ChoTrong")!=(column.Groups[4].Value=="NULL") ||
                (bool?)rule.Attribute("Khoa")!=primary)
                errors.Add("Quy tắc nhập liệu khác schema SQL: "+key);
        }
        foreach(var extra in actual.Keys.Except(expected))errors.Add("Quy tắc nhập liệu không còn trong SQL: "+extra);
        Console.WriteLine("Input schema: checked "+expected.Count+" writable columns.");
    }

    /// <summary>
    /// Rà toàn bộ C# WPF để chặn việc new control/bố cục hoặc thêm phần tử vào cây giao diện bằng code;
    /// các control dùng chung phải có XAML khai báo sẵn. Việc nạp một màn hình có XAML qua constructor vẫn được phép.
    /// </summary>
    private static void KiemTraBoCucXaml(string root, List<string> errors)
    {
        var folder = Path.Combine(root, "RangHamMat.Wpf");
        const string controls = "Grid|Border|TextBlock|TextBox|Button|DataGrid|StackPanel|TabItem|TabControl|Canvas|Rectangle|Ellipse|Polyline|Line|ScrollViewer|WrapPanel|DockPanel|DatePicker|TableRow|TableCell|Paragraph|Run|RowDefinition|ColumnDefinition";
        foreach (var file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj")))
        {
            // Bỏ chú thích trước khi rà: giải thích tên control không phải thao tác tạo control.
            var code = Regex.Replace(File.ReadAllText(file), @"(?m)^\s*//[^\r\n]*|/\*[\s\S]*?\*/", "");
            if (Regex.IsMatch(code, @"\bnew\s+(?:" + controls + @")\s*\(|\bnew\s+(?:" + controls + @")\s*\{"))
                errors.Add("C# constructs UI instead of XAML: " + Path.GetRelativePath(root, file));
            if (Regex.IsMatch(code, @"\b(?:" + controls + @")\s+\w+\s*=\s*new\s*\("))
                errors.Add("Target-typed C# UI construction: " + Path.GetRelativePath(root, file));
            if (Regex.IsMatch(code, @"\.(Children|ColumnDefinitions|RowDefinitions|Cells|RowGroups)\.(Add|Clear)\s*\(|\b(XamlReader|FrameworkElementFactory)\b"))
                errors.Add("Runtime UI tree mutation: " + Path.GetRelativePath(root, file));
        }
        foreach (var control in new[] { "ChartView", "ScheduleView", "RecordDetails" })
            if (!File.Exists(Path.Combine(folder, "Controls", control + ".xaml")))
                errors.Add("Reusable control requires editable XAML: " + control);
    }
}
