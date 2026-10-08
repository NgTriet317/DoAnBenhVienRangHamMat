using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Text.Json;

var root = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
var wpfRoot = Path.Combine(root, "RangHamMat.Wpf");
var errors = new List<string>();
var sql = File.ReadAllText(Path.Combine(root, "Database", "QLBenhVienRangHamMat.sql"));
var schema = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
foreach (Match table in Regex.Matches(sql, @"CREATE TABLE \[dbo\]\.\[(\w+)\] \((.*?)\r?\n\);", RegexOptions.Singleline))
    schema[table.Groups[1].Value] = Regex.Matches(table.Groups[2].Value, @"^\s*\[(\w+)\] (.+?)(?:,)?\r?$", RegexOptions.Multiline)
        .Cast<Match>().ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value);
var required = schema.SelectMany(table => table.Value.Keys.Select(column => table.Key + "." + column)).ToHashSet();
var fields = new HashSet<string>();
var columns = new HashSet<string>();
var keys = new HashSet<string>();
var resources = new HashSet<string>();
XNamespace w = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
var files = Directory.EnumerateFiles(wpfRoot, "*.xaml", SearchOption.AllDirectories)
    .Where(path => !Path.GetRelativePath(wpfRoot, path).Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj")).ToArray();
var documents = new Dictionary<string, XDocument>();
foreach (var file in files)
{
    var text = File.ReadAllText(file);
    XDocument doc;
    try { doc = XDocument.Parse(text); }
    catch (Exception ex) { errors.Add($"Invalid XML: {file}: {ex.Message}"); continue; }
    documents[Path.GetFileName(file)] = doc;
    foreach (var element in doc.Descendants())
    {
        if ((string?)element.Attribute(x + "Key") is string key) keys.Add(key);
        var tag = (string?)element.Attribute("Tag") ?? "";
        if (tag.StartsWith("DB:", StringComparison.Ordinal))
        {
            var field = tag[3..];
            if (!required.Contains(field)) { errors.Add($"Unknown SQL field: {field} in {file}"); continue; }
            fields.Add(field);
            var parts = field.Split('.');
            var definition = schema[parts[0]][parts[1]];
            if (definition.Contains("IDENTITY", StringComparison.Ordinal) && (string?)element.Attribute("IsReadOnly") != "True")
                errors.Add($"Identity field is editable: {field}");
            if (parts[1] == "MatKhau" && element.Name != w + "PasswordBox") errors.Add("Password field must use PasswordBox");
            if (definition.StartsWith("DATE", StringComparison.Ordinal) && element.Name != w + "DatePicker")
                errors.Add($"Date field needs DatePicker: {field}");
        }
        if (element.Name == w + "DataGrid" && tag.StartsWith("Table:", StringComparison.Ordinal))
        {
            var table = tag[6..];
            if (!schema.ContainsKey(table)) { errors.Add($"Unknown SQL table: {table}"); continue; }
            foreach (var col in element.Descendants(w + "DataGridTextColumn"))
            {
                var column = (string?)col.Attribute("SortMemberPath") ?? "";
                if (!schema[table].ContainsKey(column)) errors.Add($"Unknown list column: {table}.{column}");
                if (column == "MatKhau") errors.Add("Password must not be shown in a list");
                columns.Add(table + "." + column);
            }
        }
        if (tag.StartsWith("MoTrang:", StringComparison.Ordinal) || tag.StartsWith("MoCuaSo:", StringComparison.Ordinal))
        {
            var target = tag.StartsWith("MoTrang:", StringComparison.Ordinal) ? tag["MoTrang:".Length..] : "CuaSo" + tag["MoCuaSo:".Length..];
            if (!File.Exists(Path.Combine(wpfRoot, "Views", target + ".xaml"))) errors.Add($"Broken navigation: {tag}");
        }
    }
    foreach (Match resource in Regex.Matches(text, @"\{StaticResource\s+([^}]+)\}")) resources.Add(resource.Groups[1].Value);
    foreach (var purple in new[] { "#7553CE", "#5E43B4", "#9C83E4", "#B5A4E8", "#EEE8FA", "#F8F7FC" })
        if (text.Contains(purple, StringComparison.OrdinalIgnoreCase)) errors.Add($"Purple remains in {file}");
    if (Path.GetDirectoryName(file) == Path.Combine(wpfRoot, "Views") && !file.EndsWith("DangNhap.xaml", StringComparison.Ordinal) && !file.EndsWith("ChonRang.xaml", StringComparison.Ordinal))
    {
        if (!File.Exists(file + ".cs")) errors.Add($"Missing code-behind: {file}");
        if (doc.Root?.Name != w + "UserControl") errors.Add($"View must be embedded: {file}");
    }
}
foreach (var missing in required.Except(fields)) errors.Add($"Missing input field: {missing}");
foreach (var missing in required.Except(columns).Where(field => field != "TAI_KHOAN.MatKhau")) errors.Add($"Missing list column: {missing}");
foreach (var missing in resources.Except(keys)) errors.Add($"Missing StaticResource: {missing}");
var map = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(Path.Combine(root, "Database", "ui-table-map.json")))!;
foreach (var (module, tables) in map)
{
    var view = documents[module + ".xaml"];
    var editor = documents["CuaSo" + module + ".xaml"];
    var tags = view.Descendants().Concat(editor.Descendants()).Select(e => (string?)e.Attribute("Tag") ?? "").ToHashSet();
    foreach (var table in tables)
        foreach (var column in schema[table].Keys)
            if (!tags.Contains("DB:" + table + "." + column)) errors.Add($"Missing field in assigned module: {module}/{table}.{column}");
}
var home = documents["MainWindow.xaml"];
foreach (var (attribute, value) in new[] { ("WindowStyle", "None"), ("ResizeMode", "NoResize"), ("WindowState", "Maximized") })
    if ((string?)home.Root?.Attribute(attribute) != value) errors.Add($"Fullscreen setting missing: {attribute}");
var launchers = home.Descendants(w + "Button").Where(e => (string?)e.Attribute("Click") == "App_Click").ToArray();
if (launchers.Length != 25 || launchers.Select(e => (string?)e.Attribute("Tag")).Distinct().Count() != 25) errors.Add("Expected 25 unique Home apps");
var mainCode = File.ReadAllText(Path.Combine(wpfRoot, "MainWindow.xaml.cs"));
foreach (var launcher in launchers)
{
    var tag = (string?)launcher.Attribute("Tag");
    if (!mainCode.Contains($"\"{tag}\" => new {tag}()", StringComparison.Ordinal)) errors.Add($"Missing app route: {tag}");
}
if (mainCode.Contains(".Show();", StringComparison.Ordinal) || mainCode.Contains(".ShowDialog()", StringComparison.Ordinal)) errors.Add("Navigation opens a separate window");
if (!mainCode.Contains("Key.Escape", StringComparison.Ordinal)) errors.Add("Main window must handle Escape");
if (!home.Descendants(w + "Button").Any(e => (string?)e.Attribute("Click") == "Exit_Click")) errors.Add("Fullscreen Home needs Exit");
foreach (var form in documents.Where(pair => pair.Key.StartsWith("CuaSo", StringComparison.Ordinal)))
{
    foreach (var tag in new[] { "VeTrangChu", "DongCuaSo" })
        if (!form.Value.Descendants(w + "Button").Any(e => (string?)e.Attribute("Tag") == tag)) errors.Add($"Missing {tag}: {form.Key}");
}
if (!documents["AppWorkspace.xaml"].Descendants(w + "Button").Any(e => (string?)e.Attribute("Tag") == "VeTrangChu")) errors.Add("Workspace needs Home");
if (schema.Count != 34 || required.Count != 245) errors.Add("Unexpected database schema size");
if (errors.Count != 0)
{
    foreach (var error in errors) Console.Error.WriteLine("FAIL " + error);
    Environment.ExitCode = 1;
}
else Console.WriteLine($"SUCCESS: {schema.Count} tables, {fields.Count} fields, {columns.Count} list columns; {files.Length} XAML files; fullscreen navigation source checks passed.");
