using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BUS;

namespace RangHamMat.Controls;

// Display cache. All changes must succeed in BUS/DAL before entering these collections.
public static class ClinicalData
{
    private static readonly Dictionary<string, ObservableCollection<object>> tables = new();
    private static readonly BUS_LAMSANG business = new();
    private static bool loaded;
    private static bool batching;
    private static readonly Dictionary<string, string> keys = new()
    {
        ["BENH_NHAN"] = "MaBN", ["HO_SO_BENH_AN"] = "MaHS", ["PHIEU_KHAM_BENH"] = "MaPhieu",
        ["LICH_KHAM"] = "MaLich", ["LICH_SU_KHAM_BENH"] = "MaLS", ["TINH_TRANG_BENH"] = "MaTTB",
        ["KE_HOACH_DIEU_TRI"] = "MaKHDT", ["CHI_TIET_KE_HOACH"] = "MaCTKH",
        ["DICH_VU_THUC_HIEN"] = "MaDVThucHien", ["DON_THUOC"] = "MaDT", ["CHI_TIET_DON_THUOC"] = "MaCTDT",
        ["NHAN_VIEN"] = "MaNV", ["PHONG"] = "MaPhong", ["CA_LAM"] = "MaCa",
        ["DICH_VU"] = "MaDV", ["THUOC"] = "MaThuoc", ["HANG_HOA"] = "MaHH",
        ["HOA_DON"] = "MaHD", ["CHI_TIET_HOA_DON"] = "MaCTHD", ["THANH_TOAN"] = "MaTT",
        ["PHIEU_XUAT"] = "MaPX", ["CHI_TIET_PHIEU_XUAT"] = "MaCTPX", ["KHO"] = "MaKho", ["LO_HANG_HOA"] = "MaLo"
    };
    public static readonly Dictionary<string, string> ForeignTables = new()
    {
        ["MaBN"] = "BENH_NHAN", ["MaHS"] = "HO_SO_BENH_AN", ["MaPhieu"] = "PHIEU_KHAM_BENH",
        ["MaLich"] = "LICH_KHAM", ["MaLS"] = "LICH_SU_KHAM_BENH", ["MaBS"] = "NHAN_VIEN",
        ["MaPhong"] = "PHONG", ["MaCa"] = "CA_LAM", ["MaDV"] = "DICH_VU",
        ["MaKHDT"] = "KE_HOACH_DIEU_TRI", ["MaCTKH"] = "CHI_TIET_KE_HOACH",
        ["MaNVTiepNhan"] = "NHAN_VIEN", ["MaHH"] = "HANG_HOA", ["MaNVThucHien"] = "NHAN_VIEN", ["MaDT"] = "DON_THUOC", ["MaThuoc"] = "THUOC",
        ["MaHD"] = "HOA_DON", ["MaDVThucHien"] = "DICH_VU_THUC_HIEN", ["MaCTPX"] = "CHI_TIET_PHIEU_XUAT",
        ["MaPX"] = "PHIEU_XUAT", ["MaCTDT"] = "CHI_TIET_DON_THUOC", ["MaKho"] = "KHO", ["MaLo"] = "LO_HANG_HOA", ["MaNV"] = "NHAN_VIEN"
    };

    public static event EventHandler? Changed;

    private static void Notify() { if (!batching) Changed?.Invoke(null, EventArgs.Empty); }
    public static void RefreshFromDatabase()
    {
        if (batching) throw new InvalidOperationException("Không tải lại khi đang lưu chứng từ.");
        var snapshot=business.Load(); // Load first: a connection error never clears the current display.
        batching=true; loaded=true;
        try { foreach(var table in snapshot) Replace(table.Key,table.Value); }
        finally { batching=false; }
        Notify();
    }
    public static void EnsureLoaded() { if (!loaded) RefreshFromDatabase(); }
    public static bool Transaction(Func<bool> action)
    {
        EnsureLoaded();
        if (batching) return action();
        var snapshot=tables.ToDictionary(t=>t.Key,t=>t.Value.Select(row=>(Row:row,Values:Values(row))).ToArray());
        business.Begin(); batching=true;
        var committed=false;
        try
        {
            if (!action()) return false;
            business.Commit(); committed=true; return true;
        }
        finally
        {
            if (!committed)
            {
                try { business.Rollback(); }
                finally
                {
                    foreach(var table in snapshot)
                    {
                        var rows=Rows(table.Key); rows.Clear();
                        foreach(var entry in table.Value)
                        {
                            var values=(IDictionary<string,object?>)entry.Row; values.Clear();
                            foreach(var pair in entry.Values) values[pair.Key]=pair.Value;
                            rows.Add(entry.Row);
                        }
                    }
                }
            }
            batching=false; Notify();
        }
    }

    public static ObservableCollection<object> Rows(string table)
    {
        EnsureLoaded();
        if (!tables.TryGetValue(table, out var rows)) tables[table] = rows = new();
        return rows;
    }

    // Run on the WPF dispatcher. Normalization supports POCO/ET, dictionaries and DataRowView.
    public static void Replace(string table, IEnumerable<object> rows)
    {
        var normalized = rows.Select(row =>
        {
            IDictionary<string, object?> result = new ExpandoObject();
            if (row is IDictionary<string, object?> dictionary)
                foreach (var pair in dictionary) result[pair.Key.Equals("sdt", StringComparison.OrdinalIgnoreCase) ? "SDT" : pair.Key] = pair.Value;
            else if (row is IDictionary values)
                foreach (DictionaryEntry pair in values) result[Convert.ToString(pair.Key)!] = pair.Value;
            else
                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(row))
                    result[property.Name.Equals("sdt", StringComparison.OrdinalIgnoreCase) ? "SDT" : property.Name] = property.GetValue(row);
            return (object)result;
        }).ToArray();
        var target = Rows(table);
        target.Clear();
        foreach (var row in normalized) target.Add(row);
        Notify();
    }

    public static string Key(string table) => keys.GetValueOrDefault(table, "");
    public static Dictionary<string, object?> Values(object row)
    {
        if (row is IDictionary<string, object?> values) return new(values);
        return TypeDescriptor.GetProperties(row).Cast<PropertyDescriptor>().ToDictionary(p => p.Name, p => p.GetValue(row));
    }
    public static void AssertUnchanged(string table,string id,IDictionary<string,object?> original)
    {
        var current=Find(table,id);
        if(current is null || original.Any(pair=>!Equals(pair.Value,UiRecord.Get(current,pair.Key))))
            throw new InvalidOperationException("Dữ liệu đã thay đổi sau khi mở form. Đóng form và mở lại để giữ nội dung mới nhất.");
    }
    public static bool CanEdit(string table, object row)
    {
        if (table is "BENH_NHAN" or "HO_SO_BENH_AN") return true;
        if (table == "CHI_TIET_KE_HOACH" && Children("DICH_VU_THUC_HIEN","MaCTKH",UiRecord.Get(row,"MaCTKH")).Any(r=>UiRecord.Text(r,"TrangThai")=="Đã xác nhận")) return false;
        var parentLink = table switch { "CHI_TIET_DON_THUOC" => ("DON_THUOC", "MaDT"), "CHI_TIET_KE_HOACH" => ("KE_HOACH_DIEU_TRI", "MaKHDT"), "TINH_TRANG_BENH" => ("LICH_SU_KHAM_BENH", "MaLS"), _ => ("", "") };
        if (parentLink.Item1.Length > 0)
        {
            var parent = Find(parentLink.Item1, UiRecord.Get(row, parentLink.Item2));
            if (parent is null || !CanEdit(parentLink.Item1, parent)) return false;
        }
        var state = UiRecord.Text(row, "TrangThai");
        return state is "" or "Nháp" or "Chưa xác nhận" or "Chờ xác nhận" or "Đang lập" or "Dự kiến" or "Chờ thực hiện";
    }
    public static object Upsert(string table, IDictionary<string, object?> values, object? existing = null,IDictionary<string,object?>? expected=null)
    {
        var key = Key(table);
        var id = (values.TryGetValue(key, out var suppliedId) ? suppliedId : null)?.ToString() ?? "";
        var target = existing is null ? Find(table, id) : Find(table, UiRecord.Get(existing, key));
        if (target is not null && !CanEdit(table, target)) throw new InvalidOperationException("Bản ghi đã xác nhận: hãy lập bản mới để thay đổi nội dung.");
        if (target is not null) id = UiRecord.Text(target, key);
        if(target is not null && expected is not null) AssertUnchanged(table,id,expected);
        IDictionary<string, object?> record = target as IDictionary<string, object?> ?? new ExpandoObject();
        if (target is not null && target is not IDictionary<string, object?>) throw new InvalidOperationException("Hãy chuẩn hóa nguồn qua ClinicalData.Replace.");
        var merged=target is null ? new Dictionary<string,object?>() : Values(target);
        foreach(var pair in values) merged[pair.Key]=pair.Value;
        ClinicalIntegrity.Validate(table,merged,target is null ? "" : UiRecord.Text(target,key));
        var parentField = table switch { "CHI_TIET_DON_THUOC" => "MaDT", "CHI_TIET_KE_HOACH" => "MaKHDT", "TINH_TRANG_BENH" => "MaLS", _ => "" };
        if (parentField.Length > 0)
        {
            var parentId = values.TryGetValue(parentField,out var suppliedParent) ? suppliedParent : target is null ? null : UiRecord.Get(target,parentField);
            var parentTable = ForeignTables[parentField]; var parent = Find(parentTable,parentId);
            if (parent is null || !CanEdit(parentTable,parent)) throw new InvalidOperationException("Chỉ thêm hoặc sửa nội dung trong bản nháp có liên kết hợp lệ.");
        }
        var persisted=business.Save(table,merged,target is null?null:id,target is null?null:expected??Values(target));
        record.Clear();
        foreach (var pair in persisted) record[pair.Key] = pair.Value is DBNull ? null : pair.Value;
        if (target is null) Rows(table).Add(record);
        UpdatePlanTotal(table,record);
        Notify();
        return record;
    }
    public static void SetValue(string table, object row, string field, object? value)
    {
        if (!ClinicalSchema.Tables.TryGetValue(table,out var schema) || !schema.ContainsKey(field)) throw new InvalidOperationException("Trường cập nhật không có trong database.");
        if (table == "LICH_KHAM" && field == "TrangThai" && (UiRecord.Text(row,"TrangThai") is "Hủy" or "Đã đổi" or "Hoàn thành") && Convert.ToString(value) != UiRecord.Text(row,"TrangThai")) throw new InvalidOperationException("Lịch đã kết thúc được giữ nguyên. Hãy tạo một lịch mới.");
        if (!CanEdit(table, row) && field is not "TrangThai" and not "LyDoThayDoi" and not "NgayXacNhan" and not "NgayCapNhat") throw new InvalidOperationException("Không sửa nội dung đã xác nhận.");
        if (row is not IDictionary<string, object?> values) throw new InvalidOperationException("Nguồn dữ liệu cần được chuẩn hóa.");
        var changes=new Dictionary<string,object?> { [field]=value };
        if (table=="LICH_SU_KHAM_BENH" && field=="TrangThai" && Convert.ToString(value)=="Đã xác nhận") changes["NgayXacNhan"]=DateTime.Now;
        var changesToSave=Values(row);foreach(var change in changes) changesToSave[change.Key]=change.Value;
        var persisted=business.Save(table,changesToSave,UiRecord.Text(row,Key(table)),Values(row),statusOnly:field is "TrangThai" or "LyDoThayDoi" or "NgayCapNhat");
        values.Clear(); foreach(var pair in persisted) values[pair.Key]=pair.Value is DBNull?null:pair.Value;
        UpdatePlanTotal(table,row); Notify();
    }
    public static void Delete(string table, object row)
    {
        if (!CanEdit(table, row)) throw new InvalidOperationException("Chỉ xóa bản nháp chưa xác nhận.");
        var key = Key(table); var id = UiRecord.Text(row, key);
        foreach (var foreign in ForeignTables.Where(p => p.Value == table))
            if (tables.Any(t => t.Key != table && t.Value.Any(r => UiRecord.Text(r, foreign.Key) == id)))
                throw new InvalidOperationException("Bản ghi đang có dữ liệu liên kết. Không thể xóa.");
        business.Delete(table,id,Values(row));
        Rows(table).Remove(row); UpdatePlanTotal(table,row); Notify();
    }

    private static void UpdatePlanTotal(string table,object row)
    {
        if (table!="CHI_TIET_KE_HOACH") return;
        var plan=Find("KE_HOACH_DIEU_TRI",UiRecord.Get(row,"MaKHDT"));
        if (plan is IDictionary<string,object?> values)
            values["TongTienDuKien"]=Children(table,"MaKHDT",values["MaKHDT"]).Where(r=>UiRecord.Text(r,"TrangThai")!="Hủy")
                .Sum(r=>Convert.ToDecimal(UiRecord.Get(r,"ThanhTienDuKien")??0));
    }

    public static object? Find(string table, object? id)
    {
        var text = Convert.ToString(id, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text) || !keys.TryGetValue(table, out var key)) return null;
        return Rows(table).FirstOrDefault(row => UiRecord.Text(row, key) == text);
    }

    public static object[] Children(string table, string field, object? id)
    {
        var text = Convert.ToString(id, CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(text) ? Array.Empty<object>() : Rows(table).Where(row => UiRecord.Text(row, field) == text).ToArray();
    }

    public static string PatientId(string table, object row)
    {
        if (table is "BENH_NHAN" or "HO_SO_BENH_AN" or "LICH_KHAM") return UiRecord.Text(row, "MaBN");
        var parent = table switch
        {
            "PHIEU_KHAM_BENH" => ("HO_SO_BENH_AN", "MaHS"),
            "LICH_SU_KHAM_BENH" => ("PHIEU_KHAM_BENH", "MaPhieu"),
            "TINH_TRANG_BENH" or "KE_HOACH_DIEU_TRI" or "DICH_VU_THUC_HIEN" or "DON_THUOC" => ("LICH_SU_KHAM_BENH", "MaLS"),
            "CHI_TIET_KE_HOACH" => ("KE_HOACH_DIEU_TRI", "MaKHDT"),
            "CHI_TIET_DON_THUOC" => ("DON_THUOC", "MaDT"),
            "HOA_DON" or "PHIEU_XUAT" => ("LICH_SU_KHAM_BENH", "MaLS"),
            "CHI_TIET_HOA_DON" or "THANH_TOAN" => ("HOA_DON", "MaHD"),
            "CHI_TIET_PHIEU_XUAT" => ("PHIEU_XUAT", "MaPX"),
            _ => ("", "")
        };
        var linked = Find(parent.Item1, UiRecord.Get(row, parent.Item2));
        return linked is null ? "" : PatientId(parent.Item1, linked);
    }

    public static string Label(string table, object row)
    {
        var name = table switch
        {
            "BENH_NHAN" => "HoTenBN", "NHAN_VIEN" => "HoTen", "PHONG" => "TenPhong",
            "DICH_VU" => "TenDV", "HANG_HOA" => "TenHH", "KHO" => "TenKho", _ => ""
        };
        var key = keys.GetValueOrDefault(table, "");
        var label = UiRecord.Text(row, name);
        if (table == "THUOC")
        {
            var goods = Find("HANG_HOA", UiRecord.Get(row, "MaHH"));
            label = (goods is null ? "" : UiRecord.Text(goods, "TenHH")) + " " + UiRecord.Text(row, "HamLuong");
        }
        if (table == "CA_LAM") label = UiRecord.Text(row, "GioBatDau") + " – " + UiRecord.Text(row, "GioKetThuc");
        if (name.Length == 0 && table is not "THUOC" and not "CA_LAM")
        {
            var patient = Find("BENH_NHAN", PatientId(table, row));
            label = patient is null ? "" : UiRecord.Text(patient, "HoTenBN");
        }
        return string.Join(" · ", new[] { UiRecord.Text(row, key), label.Trim() }.Where(value => value.Length > 0));
    }

    public static IEnumerable<ClinicalOption> Options(string table, bool all = false, string? patient = null)
    {
        if (all) yield return new("", "Tất cả");
        foreach (var row in Rows(table).Where(row => string.IsNullOrEmpty(patient) || PatientId(table, row) == patient))
            yield return new(UiRecord.Text(row, keys.GetValueOrDefault(table, "")), Label(table, row));
    }

    public static string Lookup(string field, object? id)
    {
        if (id is null) return "Chưa có thông tin";
        var text = Convert.ToString(id, CultureInfo.CurrentCulture) ?? "";
        if (!ForeignTables.TryGetValue(field, out var table)) return text;
        var row = Find(table, id);
        return row is null ? text : Label(table, row);
    }
}

public sealed record ClinicalOption(string Value, string Label);
public interface IClinicalEditor { bool Commit(out string error); }
public interface IPatientScoped { string? PatientId { get; } }

public sealed class RecordValueConverter : IValueConverter
{
    public object Convert(object value, Type type, object parameter, CultureInfo culture)
    {
        if (value is null || value == DependencyProperty.UnsetValue) return "Chưa có thông tin";
        var field = System.Convert.ToString(parameter) ?? "";
        var data = UiRecord.Get(value, field);
        if (data is DateTime date) return date.ToString(field == "NgaySinh" ? "dd/MM/yyyy" : "dd/MM/yyyy HH:mm", culture);
        var text = ClinicalData.Lookup(field, data);
        return string.IsNullOrWhiteSpace(text) ? "Chưa có thông tin" : text;
    }
    public object ConvertBack(object value, Type type, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public static class ClinicalUi
{
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
    // Listen only while mounted; static notifications must not retain closed pages.
    public static void Observe(UserControl control, Action refresh)
    {
        var listening = false;
        EventHandler changed = (_, _) => refresh();
        control.Loaded += (_, _) => { if (!listening) { ClinicalData.Changed += changed; listening = true; } refresh(); };
        control.Unloaded += (_, _) => { if (listening) { ClinicalData.Changed -= changed; listening = false; } };
    }
    public static string Choice(ComboBox? box) => (box?.SelectedItem as ComboBoxItem)?.Content as string ?? "Tất cả";
    public static string Selected(ComboBox? box) => box?.SelectedValue as string ?? "";
    public static ICollectionView Bind(DataGrid grid, string table, Predicate<object> filter)
    {
        var view = new ListCollectionView(ClinicalData.Rows(table)) { Filter = filter };
        grid.ItemsSource = view;
        return view;
    }
    public static bool Matches(string table, object row, string? query, string status, string? patient = null)
    {
        if (!string.IsNullOrEmpty(patient) && ClinicalData.PatientId(table, row) != patient) return false;
        var state = UiRecord.Text(row, "TrangThai");
        if (state.Length == 0 && table == "CHI_TIET_DON_THUOC")
        {
            var parent = ClinicalData.Find("DON_THUOC", UiRecord.Get(row, "MaDT"));
            state = parent is null ? "" : UiRecord.Text(parent, "TrangThai");
        }
        if (status != "Tất cả" && state.Length > 0 && !state.Equals(status, StringComparison.CurrentCultureIgnoreCase)) return false;
        var text = query?.Trim() ?? "";
        if (text.Length == 0) return true;
        var values = row is IDictionary<string, object?> dictionary ? dictionary.Values.Select(value => System.Convert.ToString(value) ?? "") : TypeDescriptor.GetProperties(row).Cast<PropertyDescriptor>().Select(property => System.Convert.ToString(property.GetValue(row)) ?? "");
        var patientRow = ClinicalData.Find("BENH_NHAN", ClinicalData.PatientId(table, row));
        var combined = string.Join(" ", values) + " " + (patientRow is null ? "" : ClinicalData.Label("BENH_NHAN", patientRow) + " " + UiRecord.Text(patientRow, "SDT")) + " " + ClinicalData.Lookup("MaBS", UiRecord.Get(row, "MaBS"));
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(term => combined.Contains(term, StringComparison.CurrentCultureIgnoreCase));
    }
}

public static class ClinicalForm
{
    public static readonly DependencyProperty SavedProperty = DependencyProperty.RegisterAttached("Saved",typeof(object),typeof(ClinicalForm));
    public static object? GetSaved(UserControl form) => form.GetValue(SavedProperty);
    private sealed record OriginalRow(string Table,string Id,Dictionary<string,object?> Values);
    private sealed record FormState(string Table, object? Record, CreationContext? Context,OriginalRow[] Originals,string ChildTable,string ChildField);
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<UserControl, FormState> states = new();
    public static object? GetRecord(UserControl form) => states.TryGetValue(form, out var state) ? state.Record : null;
    public static string GetTable(UserControl form) => states.TryGetValue(form, out var state) ? state.Table : "";
    public static void VerifyUnchanged(UserControl form)
    {
        if(!states.TryGetValue(form,out var state)) return;
        foreach(var original in state.Originals) ClinicalData.AssertUnchanged(original.Table,original.Id,original.Values);
        if(state.ChildTable.Length>0 && state.Originals.FirstOrDefault(row=>row.Table==state.Table) is OriginalRow header)
        {
            var prior=state.Originals.Where(row=>row.Table==state.ChildTable).Select(row=>row.Id).ToHashSet();
            var current=ClinicalData.Children(state.ChildTable,state.ChildField,header.Id).Select(row=>UiRecord.Text(row,ClinicalData.Key(state.ChildTable))).ToHashSet();
            if(!prior.SetEquals(current)) throw new InvalidOperationException("Các dòng chi tiết đã thay đổi. Đóng form và mở lại trước khi lưu.");
        }
    }
    public static void Configure(UserControl form, string table, object? existing = null, CreationContext? context = null, string? patient = null)
    {
        var originals=new List<OriginalRow>();
        var actual=existing is null?null:ClinicalData.Find(table,UiRecord.Get(existing,ClinicalData.Key(table)));
        var child=table switch {"DON_THUOC"=>("CHI_TIET_DON_THUOC","MaDT"),"KE_HOACH_DIEU_TRI"=>("CHI_TIET_KE_HOACH","MaKHDT"),_ => ("","")};
        if(actual is not null)
        {
            var id=UiRecord.Text(actual,ClinicalData.Key(table));
            originals.Add(new(table,id,ClinicalData.Values(actual)));
            if(child.Item1.Length>0)
                originals.AddRange(ClinicalData.Children(child.Item1,child.Item2,id).Select(row=>new OriginalRow(child.Item1,UiRecord.Text(row,ClinicalData.Key(child.Item1)),ClinicalData.Values(row))));
        }
        states.Remove(form); states.Add(form, new(table, existing, context,originals.ToArray(),child.Item1,child.Item2)); SetPatientScope(form, patient); Prepare(form);
        if (form.FindName("EditorTabs") is TabControl tabs)
        {
            foreach (var item in tabs.Items.OfType<TabItem>())
            {
                var match = (string?)item.Tag == "Table:" + table;
                item.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
                if (match) tabs.SelectedItem = item;
            }
        }
        foreach (var element in Elements(form).OfType<FrameworkElement>())
        {
            if (element.Tag is not string tag || !(tag.StartsWith("DB:" + table + ".") || tag.StartsWith("Time:" + table + "."))) continue;
            var field = tag.Split('.')[1]; object? value = existing is null ? null : UiRecord.Get(existing, field);
            if (context is not null && context.ParentField == field) value = context.ParentValue;
            if (value is not null)
            {
                if (element is TextBox text) text.Text = tag.StartsWith("Time:") && value is DateTime date ? date.ToString("HH:mm") : Convert.ToString(value, CultureInfo.CurrentCulture) ?? "";
                if (element is DatePicker picker && DateTime.TryParse(Convert.ToString(value), out var parsed)) picker.SelectedDate = parsed.Date;
                if (element is ComboBox box)
                {
                    if (box.ItemsSource is not null) box.SelectedValue = Convert.ToString(value);
                    else box.SelectedItem = box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => Convert.ToString(i.Content) == Convert.ToString(value));
                }
            }
            if (context is not null && context.ParentField == field) element.IsEnabled = false;
            if (existing is not null && ClinicalData.Find(table, UiRecord.Get(existing, ClinicalData.Key(table))) is not null)
            {
                if (field == ClinicalIntegrity.OwnerField(table)) element.IsEnabled = false;
                if (table == "CHI_TIET_KE_HOACH" && field == "MaDV" && ClinicalData.Children("DICH_VU_THUC_HIEN", "MaCTKH", UiRecord.Get(existing, "MaCTKH")).Length > 0) element.IsEnabled = false;
            }
        }
    }
    public static Dictionary<string, object?> Read(UserControl form)
    {
        var table = GetTable(form); var values = new Dictionary<string, object?>();
        foreach (var element in Elements(form).OfType<FrameworkElement>())
        {
            if (element.Tag is not string tag || !tag.StartsWith("DB:" + table + ".")) continue;
            var field = tag.Split('.')[1];
            values[field] = element switch { TextBox t => t.Text.Trim(), DatePicker d => d.SelectedDate, ComboBox c => c.ItemsSource is not null ? c.SelectedValue : (c.SelectedItem as ComboBoxItem)?.Content, _ => null };
        }
        foreach (var time in Elements(form).OfType<TextBox>())
            if (time.Tag is string tag && tag.StartsWith("Time:" + table + "."))
            {
                var field = tag.Split('.')[1];
                if (!TimeSpan.TryParseExact(time.Text.Trim(), new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out var clock) || clock >= TimeSpan.FromDays(1)) throw new InvalidOperationException("Giờ phải có dạng HH:mm, từ 00:00 đến 23:59.");
                if (values.GetValueOrDefault(field) is DateTime day) values[field] = day.Date + clock;
            }
        return values;
    }
    public static bool TrySave(UserControl form, out object? saved, out string error)
    {
        saved = null; error = "";
        try
        {
            VerifyUnchanged(form);
            var table = GetTable(form); var values = Read(form);
            var required = table switch {
                "BENH_NHAN" => new[] { "HoTenBN", "TrangThai" }, "HO_SO_BENH_AN" => new[] { "MaBN", "NgayLap" },
                "PHIEU_KHAM_BENH" => new[] { "MaHS", "MaPhong", "MaNVTiepNhan", "LyDoKham" },
                "LICH_KHAM" => new[] { "MaBN", "MaBS", "NgayGioKham" }, "LICH_SU_KHAM_BENH" => new[] { "MaPhieu", "MaBS", "MaPhong", "ThoiGianKham" },
                "KE_HOACH_DIEU_TRI" => new[] { "MaLS", "MaBS", "NgayLap" }, "DON_THUOC" => new[] { "MaLS", "MaBS", "NgayKe" }, _ => Array.Empty<string>() };
            foreach (var field in required) if (!values.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(Convert.ToString(value))) throw new InvalidOperationException("Vui lòng nhập/chọn " + ClinicalSchema.Label(table,field) + ".");
            foreach (var pair in values)
                if (ClinicalData.ForeignTables.TryGetValue(pair.Key, out var target) && pair.Key != ClinicalData.Key(table) && !string.IsNullOrWhiteSpace(Convert.ToString(pair.Value)) && ClinicalData.Find(target, pair.Value) is null) throw new InvalidOperationException("Liên kết " + pair.Key + " không tồn tại.");
            if (table == "LICH_SU_KHAM_BENH" && ClinicalData.Children(table,"MaPhieu",values.GetValueOrDefault("MaPhieu")).Any(r => UiRecord.Text(r,"MaLS") != UiRecord.Text(GetRecord(form)!,"MaLS"))) throw new InvalidOperationException("Phiếu tiếp nhận này đã có lần khám. Hãy dùng một phiếu tiếp nhận mới.");
            if (table == "DICH_VU_THUC_HIEN" && !string.IsNullOrWhiteSpace(Convert.ToString(values.GetValueOrDefault("MaCTKH"))))
            {
                var step=ClinicalData.Find("CHI_TIET_KE_HOACH",values.GetValueOrDefault("MaCTKH"));
                var plan=step is null ? null : ClinicalData.Find("KE_HOACH_DIEU_TRI",UiRecord.Get(step,"MaKHDT"));
                var actualVisit=ClinicalData.Find("LICH_SU_KHAM_BENH",values.GetValueOrDefault("MaLS"));
                if (plan is null || actualVisit is null || ClinicalData.PatientId("KE_HOACH_DIEU_TRI",plan)!=ClinicalData.PatientId("LICH_SU_KHAM_BENH",actualVisit)) throw new InvalidOperationException("Bước kế hoạch phải thuộc cùng bệnh nhân với lần khám thực hiện.");
                if (UiRecord.Text(step!,"MaDV")!=Convert.ToString(values.GetValueOrDefault("MaDV"))) throw new InvalidOperationException("Dịch vụ thực hiện phải khớp dịch vụ của bước kế hoạch.");
            }
            if (table == "PHIEU_KHAM_BENH" && (!int.TryParse(Convert.ToString(values.GetValueOrDefault("SoThuTu")),out var order) || order <= 0)) throw new InvalidOperationException("Số thứ tự phải là số nguyên lớn hơn 0.");
            if (table == "BENH_NHAN" && values.GetValueOrDefault("NgaySinh") is DateTime birthday && birthday > DateTime.Today) throw new InvalidOperationException("Ngày sinh không thể sau hôm nay.");
            if (table == "HO_SO_BENH_AN" && ClinicalData.Children(table, "MaBN", values.GetValueOrDefault("MaBN")).Any(r => UiRecord.Text(r, "MaHS") != UiRecord.Text(GetRecord(form)!, "MaHS"))) throw new InvalidOperationException("Bệnh nhân đã có hồ sơ bệnh án.");
            if (GetRecord(form) is null || ClinicalData.Find(table, UiRecord.Get(GetRecord(form)!, ClinicalData.Key(table))) is null)
            {
                if (table != "TINH_TRANG_BENH" && table != "CHI_TIET_DON_THUOC" && !values.ContainsKey("TrangThai")) values["TrangThai"] = table is "BENH_NHAN" or "HO_SO_BENH_AN" ? "Đang theo dõi" : "Nháp";
                if (table == "LICH_KHAM") values["NgayTao"] = DateTime.Now;
            }
            saved = ClinicalData.Upsert(table, values, GetRecord(form)); form.SetValue(SavedProperty,saved); return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException) { error = ex.Message; return false; }
    }
    public static readonly DependencyProperty PatientScopeProperty = DependencyProperty.RegisterAttached("PatientScope", typeof(string), typeof(ClinicalForm));
    public static void SetPatientScope(DependencyObject target, string? value) => target.SetValue(PatientScopeProperty, value);
    public static string? GetPatientScope(DependencyObject target) => target.GetValue(PatientScopeProperty) as string;
    public static void Prepare(UserControl form)
    {
        var patient = GetPatientScope(form);
        foreach (var element in Elements(form).OfType<FrameworkElement>())
        {
            if (element.Tag is not string tag) continue;
            if (tag.StartsWith("DB:") && element is ComboBox box && box.IsEnabled)
            {
                var field = tag.Split('.')[1];
                if (!ClinicalData.ForeignTables.TryGetValue(field, out var target)) continue;
                var scoped = target is "BENH_NHAN" or "HO_SO_BENH_AN" or "PHIEU_KHAM_BENH" or "LICH_KHAM" or "LICH_SU_KHAM_BENH" or "KE_HOACH_DIEU_TRI" or "CHI_TIET_KE_HOACH" or "DON_THUOC";
                var selected = box.SelectedValue;
                box.ItemsSource = ClinicalData.Options(target, patient: scoped ? patient : null)
                    .Where(option => target != "PHIEU_KHAM_BENH" || GetTable(form) != "LICH_SU_KHAM_BENH" || !ClinicalData.Children("LICH_SU_KHAM_BENH","MaPhieu",option.Value).Any(r => UiRecord.Text(r,"MaLS") != UiRecord.Text(GetRecord(form)!,"MaLS")))
                    .Where(option => target != "DON_THUOC" || ClinicalData.Find(target, option.Value) is object parent && UiRecord.Text(parent, "TrangThai").Equals("Nháp", StringComparison.CurrentCultureIgnoreCase)).ToArray();
                if (selected is not null) box.SelectedValue = selected;
                else if (box.Items.Count == 1 && field is not "MaLich" and not "MaCTKH" && !(field == "MaDV" && GetTable(form) is "PHIEU_KHAM_BENH" or "LICH_KHAM") && !(field == "MaBS" && GetTable(form)=="PHIEU_KHAM_BENH")) box.SelectedIndex = 0;
                if (patient is not null && field == "MaBN") { box.SelectedValue = patient; box.IsEnabled = false; }
            }
            if (element is DatePicker picker && picker.IsEnabled && picker.SelectedDate is null && tag.StartsWith("DB:") && tag.Split('.')[1] is "NgayLap" or "ThoiGianTiepNhan" or "NgayGioKham" or "ThoiGianKham" or "ThoiGianThucHien" or "NgayKe") picker.SelectedDate = DateTime.Today;
            if (element is TextBox time && tag.StartsWith("Time:") && time.Text.Length == 0) time.Text = DateTime.Now.ToString("HH:mm");
        }
    }
    private static IEnumerable<DependencyObject> Elements(DependencyObject parent)
    {
        yield return parent;
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
            foreach (var descendant in Elements(child)) yield return descendant;
    }
}
