// AI bổ sung kiểm tra nhập liệu: đọc ngày đang gõ, kiểm tra kiểu/độ dài và báo đúng tên trường trước khi lưu.
// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ đọc/điền trường theo Tag, chuyển kiểu dữ liệu nhập, xác định phạm vi hồ sơ, lưu snapshot để phát hiện dữ liệu cũ, kiểm tra trước khi gọi luồng lưu; đã ghi chú cách hoạt động từng hàm.
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
/// Cơ chế đọc/lưu biểu mẫu theo Tag DB và Time, quản lý snapshot chống ghi đè và ngữ cảnh bệnh nhân/liên kết cha.
/// </summary>
public static class BieuMauHoSo
{
    public static readonly DependencyProperty SavedProperty = DependencyProperty.RegisterAttached("Saved", typeof(object), typeof(BieuMauHoSo));
    /// <summary>
    /// Đọc attached property Saved để màn hình cha biết bản ghi vừa lưu thành công từ biểu mẫu.
    /// </summary>
    public static object? GetSaved(UserControl form) => form.GetValue(SavedProperty);
    /// <summary>
    /// Snapshot gồm tên bảng, mã khóa và các giá trị lúc mở form để phát hiện dữ liệu đã thay đổi trước khi lưu.
    /// </summary>
    private sealed record OriginalRow(string Table, string Id, Dictionary<string, object?> Values);
    /// <summary>
    /// Trạng thái một form gồm bảng, bản ghi nguồn, liên kết cha, snapshot đầu/chi tiết và quan hệ các dòng con.
    /// </summary>
    private sealed record FormState(string Table, object? Record, CreationContext? Context, OriginalRow[] Originals, string ChildTable, string ChildField);
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<UserControl, FormState> states = new();
    // ConditionalWeakTable giữ trạng thái theo form; form đóng và không còn tham chiếu có thể được thu hồi bộ nhớ.
    /// <summary>
    /// Trả bản ghi nguồn đang chỉnh sửa từ trạng thái form; trả null nếu chưa cấu hình hoặc đang tạo mới.
    /// </summary>
    public static object? GetRecord(UserControl form) => states.TryGetValue(form, out var state) ? state.Record : null;
    /// <summary>
    /// Lấy tên bảng mà form đang xử lý để đọc đúng Tag DB và gọi luồng lưu tương ứng.
    /// </summary>
    public static string GetTable(UserControl form) => states.TryGetValue(form, out var state) ? state.Table : "";
    /// <summary>
    /// So sánh dữ liệu gốc và tập khóa các dòng con với cache hiện tại; chặn lưu form cũ khi bản ghi hoặc chi tiết đã thay đổi.
    /// </summary>
    public static void VerifyUnchanged(UserControl form)
    {
        if (!states.TryGetValue(form, out var state)) return;
        foreach (var original in state.Originals) DuLieuGiaoDien.AssertUnchanged(original.Table, original.Id, original.Values);
        if (state.ChildTable.Length > 0 && state.Originals.FirstOrDefault(row => row.Table == state.Table) is OriginalRow header)
        {
            var prior = state.Originals.Where(row => row.Table == state.ChildTable).Select(row => row.Id).ToHashSet();
            var current = DuLieuGiaoDien.Children(state.ChildTable, state.ChildField, header.Id).Select(row => UiRecord.Text(row, DuLieuGiaoDien.Key(state.ChildTable))).ToHashSet();
            if (!prior.SetEquals(current)) throw new InvalidOperationException("Các dòng chi tiết đã thay đổi. Đóng form và mở lại trước khi lưu.");
        }
    }
    /// <summary>
    /// Lưu ảnh chụp dữ liệu gốc, chọn tab đúng bảng, đổ giá trị vào control theo Tag DB/Time và khóa liên kết cha khi sửa hoặc tạo từ ngữ cảnh.
    /// </summary>
    public static void Configure(UserControl form, string table, object? existing = null, CreationContext? context = null, string? patient = null)
    {
        var originals = new List<OriginalRow>();
        var actual = existing is null ? null : DuLieuGiaoDien.Find(table, UiRecord.Get(existing, DuLieuGiaoDien.Key(table)));
        var child = table switch { "DON_THUOC" => ("CHI_TIET_DON_THUOC", "MaDT"), "KE_HOACH_DIEU_TRI" => ("CHI_TIET_KE_HOACH", "MaKHDT"), _ => ("", "") };
        if (actual is not null)
        {
            var id = UiRecord.Text(actual, DuLieuGiaoDien.Key(table));
            originals.Add(new(table, id, DuLieuGiaoDien.Values(actual)));
            if (child.Item1.Length > 0)
                originals.AddRange(DuLieuGiaoDien.Children(child.Item1, child.Item2, id).Select(row => new OriginalRow(child.Item1, UiRecord.Text(row, DuLieuGiaoDien.Key(child.Item1)), DuLieuGiaoDien.Values(row))));
        }
        states.Remove(form); states.Add(form, new(table, existing, context, originals.ToArray(), child.Item1, child.Item2)); SetPatientScope(form, patient); Prepare(form);
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
            if (existing is not null && DuLieuGiaoDien.Find(table, UiRecord.Get(existing, DuLieuGiaoDien.Key(table))) is not null)
            {
                if (field == TruongDuLieu.OwnerField(table)) element.IsEnabled = false;
                if (table == "CHI_TIET_KE_HOACH" && field == "MaDV" && DuLieuGiaoDien.Children("DICH_VU_THUC_HIEN", "MaCTKH", UiRecord.Get(existing, "MaCTKH")).Length > 0) element.IsEnabled = false;
            }
        }
    }
    /// <summary>
    /// Duyệt cây logical của form, đọc các control có Tag DB:BANG.COT và ghép ngày với giờ HH:mm từ Tag Time; trả dictionary tên cột/giá trị để lưu.
    /// </summary>
    public static Dictionary<string, object?> Read(UserControl form)
    {
        var table = GetTable(form); var values = new Dictionary<string, object?>();
        foreach (var element in Elements(form).OfType<FrameworkElement>())
        {
            if (element.Tag is not string tag || !tag.StartsWith("DB:" + table + ".")) continue;
            var field = tag.Split('.')[1];
            values[field] = element switch { TextBox t => t.Text.Trim(), DatePicker d => KiemTraNhapLieu.ReadDate(d), ComboBox c => c.ItemsSource is not null ? c.SelectedValue : (c.SelectedItem as ComboBoxItem)?.Content, _ => null };
        }
        foreach (var time in Elements(form).OfType<TextBox>())
            // Control ngày và ô giờ cùng cột được ghép lại; ô giờ không phải một cột SQL riêng.
            if (time.Tag is string tag && tag.StartsWith("Time:" + table + "."))
            {
                var field = tag.Split('.')[1];
                if (!TimeSpan.TryParseExact(time.Text.Trim(), new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out var clock) || clock >= TimeSpan.FromDays(1)) throw new InvalidOperationException("Giờ phải có dạng HH:mm, từ 00:00 đến 23:59.");
                if (values.GetValueOrDefault(field) is DateTime day) values[field] = day.Date + clock;
            }
        // AI bổ sung: kiểm tra nội dung nhập ngay khi đọc, trước khi mở luồng ghi dữ liệu.
        KiemTraNhapLieu.ValidateValues(table, values);
        return values;
    }
    /// <summary>
    /// Kiểm tra form còn mới, trường bắt buộc và liên kết danh mục, thêm trạng thái mặc định khi tạo mới rồi gọi Upsert; lưu kết quả vào SavedProperty, trả lỗi cho giao diện.
    /// </summary>
    public static bool TrySave(UserControl form, out object? saved, out string error)
    {
        saved = null; error = "";
        try
        {
            if (!KiemTraNhapLieu.PrepareSave(form, out error)) return false;
            VerifyUnchanged(form);
            var table = GetTable(form); var values = Read(form);
            var required = table switch
            {
                "BENH_NHAN" => new[] { "HoTenBN", "TrangThai" },
                "HO_SO_BENH_AN" => new[] { "MaBN", "NgayLap" },
                "PHIEU_KHAM_BENH" => new[] { "MaHS", "MaPhong", "MaNVTiepNhan", "LyDoKham" },
                "LICH_KHAM" => new[] { "MaBN", "MaBS", "NgayGioKham" },
                "LICH_SU_KHAM_BENH" => new[] { "MaPhieu", "MaBS", "MaPhong", "ThoiGianKham" },
                "KE_HOACH_DIEU_TRI" => new[] { "MaLS", "MaBS", "NgayLap" },
                "DON_THUOC" => new[] { "MaLS", "MaBS", "NgayKe" },
                _ => Array.Empty<string>()
            };
            foreach (var field in required) if (!values.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(Convert.ToString(value))) throw new InvalidOperationException("Vui lòng nhập/chọn " + TruongDuLieu.Label(table, field) + ".");
            foreach (var pair in values)
                if (DuLieuGiaoDien.ForeignTables.TryGetValue(pair.Key, out var target) && pair.Key != DuLieuGiaoDien.Key(table) && !string.IsNullOrWhiteSpace(Convert.ToString(pair.Value)) && DuLieuGiaoDien.Find(target, pair.Value) is null) throw new LoiNhapLieu(pair.Key,TruongDuLieu.Label(table,pair.Key)+" không còn trong danh mục. Hãy chọn lại.");
            if (GetRecord(form) is null || DuLieuGiaoDien.Find(table, UiRecord.Get(GetRecord(form)!, DuLieuGiaoDien.Key(table))) is null)
            {
                if (table != "TINH_TRANG_BENH" && table != "CHI_TIET_DON_THUOC" && !values.ContainsKey("TrangThai")) values["TrangThai"] = table is "BENH_NHAN" or "HO_SO_BENH_AN" ? "Đang theo dõi" : "Nháp";
                if (table == "LICH_KHAM") values["NgayTao"] = DateTime.Now;
            }
            saved = DuLieuGiaoDien.Upsert(table, values, GetRecord(form)); form.SetValue(SavedProperty, saved); return true;
        }
        catch (LoiNhapLieu ex) { KiemTraNhapLieu.MarkError(form, ex); error = ex.Message; return false; }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or OverflowException or ArgumentOutOfRangeException) { error = ex.Message; return false; }
    }
    public static readonly DependencyProperty PatientScopeProperty = DependencyProperty.RegisterAttached("PatientScope", typeof(string), typeof(BieuMauHoSo));
    /// <summary>
    /// Gắn mã bệnh nhân vào attached property của form để các danh mục liên kết chỉ hiện dữ liệu thuộc hồ sơ đó.
    /// </summary>
    public static void SetPatientScope(DependencyObject target, string? value) => target.SetValue(PatientScopeProperty, value);
    /// <summary>
    /// Đọc mã bệnh nhân đã gắn vào form; null nghĩa là chưa giới hạn theo một bệnh nhân cụ thể.
    /// </summary>
    public static string? GetPatientScope(DependencyObject target) => target.GetValue(PatientScopeProperty) as string;
    /// <summary>
    /// Nạp lựa chọn khóa ngoại theo bệnh nhân, loại các liên kết không phù hợp và đặt ngày/giờ cho trường nghiệp vụ; không tự đặt ngày sinh bệnh nhân.
    /// </summary>
    public static void Prepare(UserControl form)
    {
        var patient = GetPatientScope(form);
        foreach (var element in Elements(form).OfType<FrameworkElement>())
        {
            if (element.Tag is not string tag) continue;
            if (tag.StartsWith("DB:") && element is ComboBox box && box.IsEnabled)
            {
                var field = tag.Split('.')[1];
                if (!DuLieuGiaoDien.ForeignTables.TryGetValue(field, out var target)) continue;
                var scoped = target is "BENH_NHAN" or "HO_SO_BENH_AN" or "PHIEU_KHAM_BENH" or "LICH_KHAM" or "LICH_SU_KHAM_BENH" or "KE_HOACH_DIEU_TRI" or "CHI_TIET_KE_HOACH" or "DON_THUOC";
                var selected = box.SelectedValue;
                box.ItemsSource = DuLieuGiaoDien.Options(target, patient: scoped ? patient : null)
                    .Where(option => target != "PHIEU_KHAM_BENH" || GetTable(form) != "LICH_SU_KHAM_BENH" || !DuLieuGiaoDien.Children("LICH_SU_KHAM_BENH", "MaPhieu", option.Value).Any(r => UiRecord.Text(r, "MaLS") != UiRecord.Text(GetRecord(form)!, "MaLS")))
                    .Where(option => target != "DON_THUOC" || DuLieuGiaoDien.Find(target, option.Value) is object parent && UiRecord.Text(parent, "TrangThai").Equals("Nháp", StringComparison.CurrentCultureIgnoreCase)).ToArray();
                if (selected is not null) box.SelectedValue = selected;
                else if (box.Items.Count == 1 && field is not "MaLich" and not "MaCTKH" && !(field == "MaDV" && GetTable(form) is "PHIEU_KHAM_BENH" or "LICH_KHAM") && !(field == "MaBS" && GetTable(form) == "PHIEU_KHAM_BENH")) box.SelectedIndex = 0;
                if (patient is not null && field == "MaBN") { box.SelectedValue = patient; box.IsEnabled = false; }
            }
            if (element is DatePicker picker && picker.IsEnabled && picker.SelectedDate is null && tag.StartsWith("DB:") && tag.Split('.')[1] is "NgayLap" or "ThoiGianTiepNhan" or "NgayGioKham" or "ThoiGianKham" or "ThoiGianThucHien" or "NgayKe") picker.SelectedDate = DateTime.Today;
            if (element is TextBox time && tag.StartsWith("Time:") && time.Text.Length == 0) time.Text = DateTime.Now.ToString("HH:mm");
        }
    }
    /// <summary>
    /// Duyệt đệ quy cây logical của control để tìm các phần tử có Tag dữ liệu hoặc các ô nhập cần xử lý.
    /// </summary>
    private static IEnumerable<DependencyObject> Elements(DependencyObject parent)
    {
        yield return parent;
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
            foreach (var descendant in Elements(child)) yield return descendant;
    }
}
