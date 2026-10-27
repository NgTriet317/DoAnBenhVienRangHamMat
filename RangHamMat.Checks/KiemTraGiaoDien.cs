// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using RangHamMat.Controls;

/// <summary>
/// Bộ kiểm tra tĩnh cho cấu trúc giao diện, Tag và các liên kết hồ sơ; không tự render control WPF.
/// </summary>
internal static class KiemTraGiaoDien
{
    /// <summary>
    /// Đọc mã giao diện để kiểm tra các control/tag/liên kết của luồng hồ sơ theo yêu cầu; đây là kiểm tra tĩnh, không thay thế việc render WPF trên Windows.
    /// </summary>
    public static void Run(string root)
    {
        var errors = new List<string>();
        ArchitectureChecks.Run(root, errors);
        // Đánh giá điều kiện kiểm tra, ghi lỗi hoặc tăng số kiểm tra đạt tùy chương trình; không sửa nghiệp vụ ứng dụng.
        void Check(bool condition, string message) { if (!condition) errors.Add(message); }
        var wpf = Path.Combine(root, "RangHamMat.Wpf");
        var sql=File.ReadAllText(Path.Combine(root,"Database","QLBenhVienRangHamMat.sql"));
        var schema=Regex.Matches(sql,@"CREATE TABLE \[dbo\]\.\[(\w+)\] \((.*?)\r?\n[ \t]*\);",RegexOptions.Singleline)
            .Cast<Match>().ToDictionary(table=>table.Groups[1].Value,table=>Regex.Matches(table.Groups[2].Value,@"^\s*\[(\w+)\]\s+((?:INT|NVARCHAR|DATE|DATETIME2|TIME|DECIMAL|AS)\b[^\r\n]*)",RegexOptions.Multiline)
                .Cast<Match>().ToDictionary(column=>column.Groups[1].Value,column=>column.Groups[2].Value));
        var modules = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(Path.Combine(root, "Database", "UI_DBv7.modules.json")))!;
        XNamespace w = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var documents = new Dictionary<string, XDocument>();
        var resources = new HashSet<string>();
        var referenced = new HashSet<string>();
        foreach (var path in Directory.EnumerateFiles(wpf, "*.xaml", SearchOption.AllDirectories).Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin")))
        {
            var text = File.ReadAllText(path);
            try { documents[Path.GetFileName(path)] = XDocument.Parse(text); }
            catch (Exception ex) { errors.Add($"XML {path}: {ex.Message}"); continue; }
            foreach (var key in documents[Path.GetFileName(path)].Descendants().Attributes(x + "Key")) resources.Add(key.Value);
            foreach (Match match in Regex.Matches(text, @"\{StaticResource\s+([^}]+)\}")) referenced.Add(match.Groups[1].Value);
            foreach (var nameScope in documents[Path.GetFileName(path)].Descendants().Attributes(x + "Name")
                .GroupBy(attribute => attribute.Parent!.Ancestors().FirstOrDefault(element => element.Name == w + "ControlTemplate" || element.Name == w + "DataTemplate")))
                foreach (var group in nameScope.GroupBy(attribute => attribute.Value))
                    Check(group.Count() == 1, $"Duplicate name {group.Key}: {path}");
        }
        // Run.Text mặc định TwoWay; binding đọc phải đặt OneWay/OneTime khi property chỉ có getter.
        foreach (var (name, document) in documents)
            foreach (var run in document.Descendants(w + "Run"))
                if ((string?)run.Attribute("Text") is string binding && binding.StartsWith("{Binding"))
                    Check(Regex.IsMatch(binding, @"\bMode\s*=\s*(OneWay|OneTime)\b"), "Run.Text may write a read-only property: " + name + " " + binding);
        foreach (var resource in referenced.Except(resources)) errors.Add("Missing resource: " + resource);
        var scope = new List<string> { "HoSoBenhNhan.xaml", "ChiTietKhamDieuTri.xaml", "ChiTietDonThuoc.xaml", "ChiTietHoSo.xaml", "LichHenKham.xaml", "TiepNhanKham.xaml", "XemTruocDonThuoc.xaml", "AppWorkspace.xaml" };
        foreach (var (module, tables) in modules)
        {
            scope.Add(module + ".xaml"); scope.Add("CuaSo" + module + ".xaml");
            var list = documents[module + ".xaml"];
            var editor = documents["CuaSo" + module + ".xaml"];
            Check(list.Root?.Name == w + "UserControl" && editor.Root?.Name == w + "UserControl", "Views must remain embedded: " + module);
            // Thẻ gọn và dòng sửa không cần hiện mọi trường quản trị trong bảng danh sách.
            // Kiểm tra tên SQL ở nơi được dùng; vùng chi tiết chỉ đọc vẫn phủ các trường cần tra cứu.
            foreach (var table in tables)
            {
                var details = documents["KieuHienThiHoSo.xaml"].Descendants(w + "DataTemplate").FirstOrDefault(t => (string?)t.Attribute(x + "Key") == table + "Details");
                Check(details is not null, "Missing full record details: " + table);
                foreach (var column in list.Descendants(w + "DataGrid").Where(g => (string?)g.Attribute("Tag") == "Table:" + table).Descendants(w + "DataGridTextColumn"))
                {
                    var field = (string?)column.Attribute("SortMemberPath");
                    if (!string.IsNullOrEmpty(field)) Check(schema[table].ContainsKey(field), "Unknown list field: " + table + "." + field);
                }
            }
            foreach (var element in editor.Descendants().Where(element => ((string?)element.Attribute("Tag"))?.StartsWith("DB:") == true))
            {
                var parts = ((string)element.Attribute("Tag")!)[3..].Split('.');
                Check(schema.TryGetValue(parts[0], out var fields) && fields.ContainsKey(parts[1]), "Unknown input: " + string.Join('.', parts));
                if (parts[1] == schema[parts[0]].Keys.First()) Check((string?)element.Attribute("IsReadOnly") == "True", "Primary key must be read-only: " + string.Join('.', parts));
            }
        }
        foreach (var name in scope)
        {
            var file = Directory.EnumerateFiles(wpf, name, SearchOption.AllDirectories).First();
            var markup = File.ReadAllText(file);
            var code = File.ReadAllText(file + ".cs");
            Check(!Regex.IsMatch(code, @"new\s+(Grid|Border|TextBlock|TextBox|Button|DataGrid|StackPanel|TabItem|TabControl)\s*[({]"), "Runtime layout creation: " + name);
            foreach (var ev in documents[name].Descendants().Attributes().Where(attribute => attribute.Name.LocalName is "Click" or "Loaded" or "TextChanged" or "SelectionChanged" or "SelectedDateChanged" or "PreviewMouseLeftButtonUp" or "Sorting" or "CellEditEnding" or "LostFocus" or "ScrollChanged"))
                Check(Regex.IsMatch(code, @"\b" + Regex.Escape(ev.Value) + @"\s*\("), $"Missing event {ev.Value}: {name}");
            Check(!markup.Contains("ChonRang") && !markup.Contains("ViTriRang"), "Removed tooth field remains: " + name);
        }
        Check(!File.Exists(Path.Combine(wpf, "Views", "ChonRang.xaml")), "Unused tooth picker still present");
        var home = documents["MainWindow.xaml"].Root!;
        Check((string?)home.Attribute("WindowStyle") == "None" && (string?)home.Attribute("WindowState") == "Maximized", "Fullscreen configuration changed");
        // Gọi trực tiếp các helper dùng chung với WPF để kiểm tra quy tắc thật.
        var sunday = QuyTacHienThi.Period(new DateTime(2026, 10, 11, 18, 30, 0), "Week");
        Check(sunday.Start == new DateTime(2026, 10, 5) && sunday.Days == 7 && sunday.Columns == 7, "Sunday must belong to the Monday-starting week");
        var newYear = QuyTacHienThi.Period(new DateTime(2027, 1, 1), "Week");
        Check(newYear.Start == new DateTime(2026, 12, 28), "Week must cross year correctly");
        var month = QuyTacHienThi.Period(new DateTime(2026, 11, 15), "Month");
        Check(month.Start == new DateTime(2026, 10, 26) && month.Days == 42, "Month must include complete Monday-starting rows");
        var day = QuyTacHienThi.Period(new DateTime(2026, 10, 8, 23, 59, 0), "Day");
        Check(day.Start == new DateTime(2026, 10, 8) && day.Days == 1 && day.Columns == 1, "Day must discard time component");
        Check(QuyTacHienThi.TryPrescriptionNumbers("2,5", "3", out var quantity, out var days) && quantity == 2.5m && days == 3, "Vietnamese decimal quantity");
        Check(QuyTacHienThi.TryPrescriptionNumbers("2.5", "3", out quantity, out days) && quantity == 2.5m, "Dot decimal quantity");
        foreach (var bad in new[] { "0", "-1", "1,2,3", "1.000,5", "abc" })
            Check(!QuyTacHienThi.TryPrescriptionNumbers(bad, "2", out _, out _), "Invalid quantity accepted: " + bad);
        foreach (var bad in new[] { "0", "-1", "1.5", "abc" })
            Check(!QuyTacHienThi.TryPrescriptionNumbers("2", bad, out _, out _), "Invalid duration accepted: " + bad);
        Check(QuyTacHienThi.TryNonNegativeNumber("10000,50",out var price)&&price==10000.50m,"Vietnamese service price must not be read as thousands");
        Check(QuyTacHienThi.TryNonNegativeNumber("0",out price)&&price==0,"Free service price is allowed");
        Check(!QuyTacHienThi.TryNonNegativeNumber("1.000,50",out _)&&!QuyTacHienThi.TryNonNegativeNumber("-1",out _),"Ambiguous/negative service price rejected");
        Check(QuyTacHienThi.PlanStepStatus("Nháp", 2, 0) == "Nháp", "Unstarted step must retain recorded state");
        Check(QuyTacHienThi.PlanStepStatus("Nháp", 2, 1) == "Đang thực hiện", "Partial execution must remain available for continuation");
        Check(QuyTacHienThi.PlanStepStatus("Đã xác nhận", 2, 2) == "Hoàn thành", "Completed planned quantity must close the step");
        Check(QuyTacHienThi.PlanStepStatus("Đã xác nhận", 2, 3) == "Hoàn thành", "Overfulfilled step must not reopen");
        Check(QuyTacHienThi.PlanStepStatus("Hủy", 2, 1) == "Hủy", "Canceled step must remain canceled");
        if (errors.Count > 0) { foreach (var error in errors) Console.Error.WriteLine("FAIL " + error); Environment.ExitCode = 1; }
        else Console.WriteLine($"SUCCESS: {modules.Count} modules, {modules.Values.SelectMany(tables => tables).Count()} tables, {documents.Count} XAML documents; installed v8 SQL fields, event links, resources and calendar/prescription rules checked.");
    }
}
