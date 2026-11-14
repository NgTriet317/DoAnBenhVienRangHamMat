// PHẦN AI ĐÃ THỰC HIỆN: tạo kiểm tra đầu vào chung cho giao diện, đọc giới hạn schema v8, báo lỗi tiếng Việt và đưa focus về ô sai.
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.Xml.Linq;
using System.Net.Mail;

namespace RangHamMat.Controls;

/// <summary>Lỗi nhập liệu kèm tên trường để giao diện chỉ đúng ô cần sửa; không ghi hoặc xóa dữ liệu.</summary>
public sealed class LoiNhapLieu(string field, string message) : InvalidOperationException(message)
{
    /// <summary>Tên cột/trường nhập gây lỗi, dùng tìm control theo Tag DB hoặc tên XAML.</summary>
    public string Field { get; } = field;
}

/// <summary>Chặn dữ liệu sai trước khi gọi BUS; ràng buộc nghiệp vụ và SQL vẫn được kiểm tra lại khi lưu.</summary>
public static class KiemTraNhapLieu
{
    /// <summary>
    /// Các quy tắc được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, XElement>> rules = LoadRules();
    /// <summary>
    /// Cờ Đã đăng ký; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private static bool registered;
    /// <summary>
    /// Định danh dependency property đánh dấu ngày không hợp lệ được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static readonly DependencyProperty InvalidDateProperty=DependencyProperty.RegisterAttached("InvalidDate",typeof(string),typeof(KiemTraNhapLieu),new PropertyMetadata(""));
    private static readonly DependencyProperty CalendarChoiceProperty=DependencyProperty.RegisterAttached("CalendarChoice",typeof(bool),typeof(KiemTraNhapLieu),new PropertyMetadata(false));
    private static readonly DependencyProperty CalendarOwnerProperty=DependencyProperty.RegisterAttached("CalendarOwner",typeof(DatePicker),typeof(KiemTraNhapLieu));
    /// <summary>
    /// Định danh dependency property lưu thông báo lỗi trên điều khiển được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    public static readonly DependencyProperty ErrorProperty = DependencyProperty.RegisterAttached("Error", typeof(string), typeof(KiemTraNhapLieu), new PropertyMetadata(""));
    /// <summary>
    /// Định danh dependency property cho trạng thái có lỗi được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    public static readonly DependencyProperty HasErrorProperty = DependencyProperty.RegisterAttached("HasError", typeof(bool), typeof(KiemTraNhapLieu), new PropertyMetadata(false));
    public static readonly DependencyProperty HintProperty = DependencyProperty.RegisterAttached("Hint", typeof(string), typeof(KiemTraNhapLieu), new PropertyMetadata(""));
    private static readonly DependencyProperty FieldValidatorProperty = DependencyProperty.RegisterAttached("FieldValidator", typeof(Action), typeof(KiemTraNhapLieu));
    public static void SetFieldValidator(FrameworkElement field, Action validate) => field.SetValue(FieldValidatorProperty, validate);
    public static bool RecheckField(FrameworkElement field)
    {
        try { ValidateField(field); SetError(field, ""); return true; }
        catch (LoiNhapLieu ex) { SetError(field, ex.Message); return false; }
    }
    private static bool HasFieldValidator(FrameworkElement field) => field.GetValue(FieldValidatorProperty) is Action;
    public static string GetHint(DependencyObject target) => (string)target.GetValue(HintProperty);
    /// <summary>Cờ cho trigger XAML đổi viền khi có lỗi nhập liệu.</summary>
    public static bool GetHasError(DependencyObject target) => (bool)target.GetValue(HasErrorProperty);
    /// <summary>Gán cờ lỗi; SetError đồng bộ cờ này với nội dung thông báo.</summary>
    public static void SetHasError(DependencyObject target, bool value) => target.SetValue(HasErrorProperty, value);
    /// <summary>Đọc lỗi của ô nhập để trigger XAML hiện viền đỏ và tooltip hướng dẫn sửa.</summary>
    public static string GetError(DependencyObject target) => (string)target.GetValue(ErrorProperty);
    /// <summary>Đặt/xóa lỗi tại control hiện hữu; không tạo control bằng C#.</summary>
    public static void SetError(DependencyObject target, string value)
    {
        target.SetValue(ErrorProperty, value);
        target.SetValue(HintProperty, value.Length == 0 ? "" : CorrectionHint(target));
        SetHasError(target, value.Length > 0);
    }

    private static string CorrectionHint(DependencyObject target)
    {
        if (target is not FrameworkElement field) return "Kiểm tra và nhập lại theo thông báo trên.";
        var key = FieldName(field);
        if (field.Tag is string time && time.StartsWith("Time:")) return "Nhập giờ dạng HH:mm, ví dụ 08:30; giờ từ 00:00 đến 23:59.";
        if (field is DatePicker) return "Chọn ngày trên lịch hoặc nhập dd/MM/yyyy, ví dụ 25/10/2026; kiểm tra thứ tự các ngày liên quan.";
        if (field is ComboBox) return "Chọn một giá trị có sẵn trong danh sách; tải lại danh mục nếu lựa chọn không còn sử dụng.";
        if (key is "SDT" or "DienThoai") return "Nhập 8–15 chữ số, ví dụ 0901234567 hoặc +84 901 234 567; có thể để trống nếu chưa có.";
        if (key == "Email") return "Nhập địa chỉ email đầy đủ, ví dụ lienhe@example.com, không có khoảng trắng hoặc dấu chấm liên tiếp.";
        if (key == "HoTenBN") return "Nhập họ tên bằng chữ có dấu hoặc không dấu; có thể dùng khoảng trắng, dấu nháy, chấm hoặc gạch nối.";
        if (key == "NhomMau") return "Nhập A, B, AB hoặc O, có thể kèm + hoặc −, ví dụ AB+; để trống nếu chưa xác định.";
        if (key == "MaBHYT") return "Nhập mã chữ/số đúng trên thẻ bảo hiểm, không dùng khoảng trắng hay ký hiệu; để trống nếu chưa có thông tin.";
        var parts = (field.Tag as string ?? "").Replace("DB:", "").Split('.');
        if (parts.Length == 2 && rules.TryGetValue(parts[0], out var columns) && columns.TryGetValue(parts[1], out var column))
        {
            var type = (string?)column.Attribute("Kieu");
            var size = (string?)column.Attribute("QuyCach") ?? "";
            if (type == "int") return "Nhập số nguyên trong giới hạn cho phép, ví dụ 1; không dùng dấu phân cách hoặc phần thập phân.";
            if (type is "decimal" or "numeric") return "Nhập số không âm, tối đa " + size.Split(',').Last() + " chữ số lẻ; dùng dấu phẩy hoặc chấm cho phần lẻ, không dùng phân cách hàng nghìn.";
            if (type == "time") return "Nhập giờ dạng HH:mm, ví dụ 08:30; giờ từ 00:00 đến 23:59.";
            return "Nhập nội dung" + (int.TryParse(size, out var limit) ? " tối đa " + limit + " ký tự" : "") + "; trường bắt buộc phải có nội dung, không chỉ khoảng trắng.";
        }
        return "Nhập lại theo định dạng và giới hạn trong thông báo trên.";
    }

    /// <summary>Nạp metadata cột được trích từ SQL: kiểu, nullable, độ dài và precision/scale; không cần kết nối database.</summary>
    private static Dictionary<string, Dictionary<string, XElement>> LoadRules()
    {
        using var stream = typeof(KiemTraNhapLieu).Assembly.GetManifestResourceStream("RangHamMat.Controls.QuyTacNhapLieu.xml")
            ?? throw new InvalidOperationException("Thiếu cấu hình kiểm tra nhập liệu.");
        return XDocument.Load(stream).Root!.Elements("Bang").ToDictionary(
            table => (string)table.Attribute("Ten")!,
            table => table.Elements("Cot").ToDictionary(column => (string)column.Attribute("Ten")!, column => column));
    }

    /// <summary>Đăng ký xử lý ngày gõ sai cho toàn bộ DatePicker; chỉ đăng ký một lần ở lúc khởi động.</summary>
    public static void Register()
    {
        if (registered) return;
        registered = true;
        EventManager.RegisterClassHandler(typeof(DatePicker), FrameworkElement.LoadedEvent, new RoutedEventHandler(DateLoaded));
        // Xóa trạng thái ngày gõ sai trước khi biểu mẫu kiểm tra ngày mới và khoảng ngày liên quan.
        EventManager.RegisterClassHandler(typeof(DatePicker), DatePicker.SelectedDateChangedEvent, new EventHandler<SelectionChangedEventArgs>(DateSelected));
        // AI bổ sung: cả form đang hoạt động và form mẫu có Tag DB hợp lệ đều được kiểm tra khi rời ô nhập.
        foreach(var type in new[]{typeof(TextBox),typeof(ComboBox),typeof(DatePicker),typeof(PasswordBox),typeof(CheckBox)})
            EventManager.RegisterClassHandler(type,Keyboard.LostKeyboardFocusEvent,new KeyboardFocusChangedEventHandler(FieldLostFocus));
        EventManager.RegisterClassHandler(typeof(TextBox), TextBox.TextChangedEvent, new TextChangedEventHandler(ErroredTextChanged));
    }
    /// <summary>AI tạo: kiểm tra một ô theo Tag DB khi người dùng rời ô; lỗi xuất hiện ở viền/tooltip và vùng thông báo có sẵn.</summary>
    private static void FieldLostFocus(object sender,KeyboardFocusChangedEventArgs e)
    {
        if(sender is not FrameworkElement field || !field.IsEnabled || field.IsKeyboardFocusWithin || field is TextBox {IsReadOnly:true} ||
            !HasFieldValidator(field) && (field.Tag is not string tag || !(tag.StartsWith("DB:") || tag.StartsWith("Time:")))) return;
        try
        {
            var previous=GetError(field);
            ValidateField(field);
            SetError(field,"");
            var form=Ancestors(field).OfType<UserControl>().FirstOrDefault();
            var label=form is null?null:Elements(form).OfType<TextBlock>().FirstOrDefault(x=>x.Name is "FormError" or "Feedback" or "PageStatus");
            if(previous.Length>0 && label?.Text==previous)label.Text="";
        }
        catch(LoiNhapLieu ex)
        {
            SetError(field,ex.Message);
            var form=Ancestors(field).OfType<UserControl>().FirstOrDefault();
            if(form is not null) ShowError(form,ex.Message);
        }
    }

    private static void ErroredTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox field || !GetHasError(field) || field.IsReadOnly || !field.IsEnabled || !HasFieldValidator(field) && (field.Tag is not string tag || !tag.StartsWith("DB:"))) return;
        try { ValidateField(field); SetError(field, ""); }
        catch (LoiNhapLieu ex) { SetError(field, ex.Message); }
    }

    private static object? ReadField(FrameworkElement field) => field switch
    {
        TextBox text => text.Text.Trim(), DatePicker date => ReadDate(date),
        ComboBox box => box.ItemsSource is null ? (box.SelectedItem as ComboBoxItem)?.Content ?? box.SelectedItem : box.SelectedValue ?? box.SelectedItem,
        PasswordBox password => password.Password, _ => null
    };

    private static void ValidateField(FrameworkElement field)
    {
        ValidateStoredField(field);
        if (field.GetValue(FieldValidatorProperty) is Action validate) validate();
    }

    private static void ValidateStoredField(FrameworkElement field)
    {
        if (field.Tag is not string tag) return;
        var parts = tag[(tag.IndexOf(':') + 1)..].Split('.');
        if (parts.Length != 2) return;
        if (tag.StartsWith("Time:") && field is TextBox time)
        {
            if (!TimeSpan.TryParseExact(time.Text.Trim(), new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out var clock) || clock >= TimeSpan.FromDays(1))
                throw new LoiNhapLieu(tag, "Giờ không hợp lệ. Nhập HH:mm từ 00:00 đến 23:59, ví dụ 08:30.");
            return;
        }
        if (field is ComboBox combo && (combo.IsEditable && combo.SelectedIndex < 0 && !string.IsNullOrWhiteSpace(combo.Text) || combo.SelectedIndex >= 0 && !combo.Items.Contains(combo.SelectedItem)))
            throw new LoiNhapLieu(parts[1], InputLabel(field) + " không có trong danh sách. Hãy chọn một lựa chọn có sẵn.");
        var value = ReadField(field);
        if (rules.TryGetValue(parts[0], out var columns) && columns.ContainsKey(parts[1]))
            ValidateValues(parts[0], new Dictionary<string, object?> { [parts[1]] = value });
        else ValidatePrototypeField(field, parts[1], value);
    }

    /// <summary>AI tạo: form mẫu còn Tag cũ vẫn kiểm tra trường đánh dấu *, định dạng số/ngày và độ dài theo tên trường chung; không biến Tag cũ thành cột SQL.</summary>
    private static void ValidatePrototypeField(FrameworkElement control,string field,object? value)
    {
        var label=InputLabel(control);
        var text=Convert.ToString(value,CultureInfo.InvariantCulture)?.Trim()??"";
        var group = control.Parent is ContentControl { Tag: "InlineField" } wrapper ? wrapper.Parent : control.Parent;
        var caption=(group as Panel)?.Children.OfType<TextBlock>().FirstOrDefault()?.Text??label;
        if(text.Length==0)
        {
            if(caption.TrimEnd().EndsWith('*'))throw new LoiNhapLieu(field,"Vui lòng nhập/chọn "+label+".");
            return;
        }
        if(control is TextBox && (field.StartsWith("SoLuong") || field.StartsWith("DonGia") || field=="ThanhTien"))
        {
            var scale=field.StartsWith("SoLuong")?3:2;
            if(!decimal.TryParse(text.Replace(',','.'),NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out var number) ||
                decimal.Round(number,scale)!=number || decimal.Abs(number)>=(scale==3?1000000000000000m:10000000000000000m) ||
                field!="SoLuongLo" && number<0)
                throw new LoiNhapLieu(field,label+" phải là số hợp lệ, tối đa "+scale+" chữ số lẻ và trong phạm vi cho phép.");
            return;
        }
        // Tên cột cùng loại dùng chung giới hạn dài; nullable của bảng khác không quyết định bắt buộc cho form mẫu.
        var known=rules.FirstOrDefault(table=>table.Value.TryGetValue(field,out var rule) && (string?)rule.Attribute("Kieu")=="nvarchar");
        if(known.Value is not null && int.TryParse((string?)known.Value[field].Attribute("QuyCach"),out var limit) && text.Length>limit)
            throw new LoiNhapLieu(field,label+" tối đa "+limit+" ký tự.");
    }

    /// <summary>Gắn handler một lần mỗi lần Loaded; khi ngày bị gõ sai, chặn ngoại lệ WPF và báo rõ cho người dùng.</summary>
    private static void DateLoaded(object sender, RoutedEventArgs e)
    {
        var picker = (DatePicker)sender;
        picker.DateValidationError -= DateError;
        picker.DateValidationError += DateError;
        picker.PreviewTextInput -= EditingDate; picker.PreviewTextInput += EditingDate;
        picker.PreviewKeyDown -= DeletingDate; picker.PreviewKeyDown += DeletingDate;
        picker.CalendarOpened -= CalendarOpened; picker.CalendarOpened += CalendarOpened;
        picker.CalendarClosed -= CalendarClosed; picker.CalendarClosed += CalendarClosed;
        DataObject.RemovePastingHandler(picker,PastingDate); DataObject.AddPastingHandler(picker,PastingDate);
    }
    /// <summary>AI tạo: khi người dùng sửa nội dung, kiểm tra lại chữ mới ở lần lưu tiếp theo.</summary>
    private static void EditingDate(object sender,TextCompositionEventArgs e)=>((DatePicker)sender).SetValue(InvalidDateProperty,"");
    /// <summary>AI tạo: Backspace/Delete là thao tác sửa ngày, không coi Tab/Enter là đã sửa lỗi.</summary>
    private static void DeletingDate(object sender,KeyEventArgs e) {if(e.Key is Key.Back or Key.Delete)((DatePicker)sender).SetValue(InvalidDateProperty,"");}
    /// <summary>AI tạo: dán nội dung ngày phải được kiểm tra lại, không dùng kết quả của lần nhập trước.</summary>
    private static void PastingDate(object sender,DataObjectPastingEventArgs e)=>((DatePicker)sender).SetValue(InvalidDateProperty,"");
    /// <summary>AI tạo: chọn ngày mới bằng lịch xóa lỗi của chuỗi ngày cũ.</summary>
    private static void DateSelected(object? sender,SelectionChangedEventArgs e) {if(sender is DatePicker picker){picker.SetValue(InvalidDateProperty,"");SetError(picker,"");}}
    /// <summary>AI tạo: chọn lại ngày trên lịch xác nhận lựa chọn hợp lệ kể cả khi chọn cùng ngày đang lưu.</summary>
    private static void CalendarClosed(object? sender,RoutedEventArgs e)
    {
        if(sender is not DatePicker picker || picker.GetValue(CalendarChoiceProperty) is not true || string.IsNullOrEmpty((string)picker.GetValue(InvalidDateProperty)) || picker.SelectedDate is not DateTime selected)return;
        picker.SetValue(InvalidDateProperty,"");
        picker.Text=selected.ToString("d",new CultureInfo(picker.Language.IetfLanguageTag,true));
        // Chọn lại cùng ngày cũng là xác nhận sửa, dù giá trị SelectedDate không thay đổi.
        picker.RaiseEvent(new ConfirmedDateEventArgs(selected));
    }

    private sealed class ConfirmedDateEventArgs(DateTime selected)
        : SelectionChangedEventArgs(DatePicker.SelectedDateChangedEvent,Array.Empty<object>(),new object[]{selected})
    {
        protected override void InvokeEventHandler(Delegate handler,object target)
            => ((EventHandler<SelectionChangedEventArgs>)handler)(target,this);
    }

    private static void CalendarOpened(object? sender,RoutedEventArgs e)
    {
        if(sender is not DatePicker picker)return;
        picker.SetValue(CalendarChoiceProperty,false);
        if(picker.Template.FindName("PART_Popup",picker) is not Popup { Child: DependencyObject child })return;
        var calendar=Elements(child).OfType<System.Windows.Controls.Calendar>().FirstOrDefault();
        if(calendar is null || calendar.GetValue(CalendarOwnerProperty) is not null)return;
        calendar.SetValue(CalendarOwnerProperty,picker);
        bool IsDay(object source)=>source is DependencyObject node && new[]{node}.Concat(Ancestors(node)).OfType<CalendarDayButton>().Any(day=>day.IsEnabled&&!day.IsBlackedOut);
        calendar.AddHandler(Mouse.PreviewMouseUpEvent,new MouseButtonEventHandler((_,args)=>{if(args.ChangedButton==MouseButton.Left&&IsDay(args.OriginalSource))picker.SetValue(CalendarChoiceProperty,true);}),true);
        calendar.AddHandler(ButtonBase.ClickEvent,new RoutedEventHandler((_,args)=>{if(IsDay(args.OriginalSource))picker.SetValue(CalendarChoiceProperty,true);}),true);
        calendar.AddHandler(Keyboard.PreviewKeyDownEvent,new KeyEventHandler((_,args)=>{if(args.Key is Key.Enter or Key.Space && calendar.DisplayMode==CalendarMode.Month && calendar.SelectedDate is not null && Keyboard.FocusedElement is DependencyObject focused && IsDay(focused))picker.SetValue(CalendarChoiceProperty,true);}),true);
    }

    /// <summary>Thông báo ngày không tồn tại/ngoài phạm vi thay vì âm thầm sử dụng ngày đã chọn trước đó.</summary>
    private static void DateError(object? sender, DatePickerDateValidationErrorEventArgs e)
    {
        e.ThrowException = false;
        if (sender is not DatePicker picker) return;
        picker.SetValue(InvalidDateProperty,e.Text);
        var message = InputLabel(picker) + ": ngày không hợp lệ. Chọn lại ngày trên lịch hoặc nhập ngày/tháng/năm, ví dụ 25/10/2026.";
        SetError(picker, message);
        var parent = Ancestors(picker).OfType<UserControl>().FirstOrDefault();
        if (parent is not null) ShowError(parent, message);
    }

    /// <summary>Kiểm tra riêng các giá trị có trong dữ liệu form; bỏ khóa tự sinh/computed và giữ quyền bỏ trống cột nullable.</summary>
    public static void ValidateValues(string table, IDictionary<string, object?> values)
    {
        if (!rules.TryGetValue(table, out var columns)) throw new InvalidOperationException("Không tìm thấy quy tắc cho biểu mẫu " + table + ".");
        foreach (var (field, value) in values)
        {
            if (!columns.TryGetValue(field, out var column) || (bool)column.Attribute("Khoa")!) continue;
            var label = TruongDuLieu.Label(table, field);
            var text = value is null or DBNull ? "" : Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? "";
            if (text.Length == 0)
            {
                if (!(bool)column.Attribute("ChoTrong")!) throw new LoiNhapLieu(field, "Vui lòng nhập/chọn " + label + ".");
                continue;
            }
            if(text.All(ch=>char.IsWhiteSpace(ch)||char.GetUnicodeCategory(ch)==UnicodeCategory.Format))
                throw new LoiNhapLieu(field,label+" chỉ chứa ký tự trống hoặc vô hình. Xóa ký tự lạ và nhập nội dung rõ ràng.");
            var type = (string)column.Attribute("Kieu")!;
            var size = (string)column.Attribute("QuyCach")!;
            if (type is "nvarchar" or "varchar")
            {
                if (text.Any(ch => char.IsControl(ch) && ch is not ('\r' or '\n' or '\t')))
                    throw new LoiNhapLieu(field, label + " chứa ký tự điều khiển không hợp lệ. Xóa ký tự lạ và nhập lại nội dung.");
                if (int.TryParse(size, out var limit) && text.Length > limit)
                    throw new LoiNhapLieu(field, label + " tối đa " + limit + " ký tự.");
            }
            if (type == "int" && (!Regex.IsMatch(text, @"^-?[0-9]+$") || !int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _)))
                throw new LoiNhapLieu(field, label + " phải là số nguyên trong phạm vi cho phép.");
            if (type is "decimal" or "numeric")
            {
                var parts = size.Split(',').Select(int.Parse).ToArray();
                if (!Regex.IsMatch(text, @"^-?[0-9]+(?:[.,][0-9]+)?$") || !decimal.TryParse(text.Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number))
                    throw new LoiNhapLieu(field, label + " phải là số hợp lệ; dùng dấu phẩy hoặc chấm cho phần lẻ, không nhập phân cách hàng nghìn.");
                if (decimal.Round(number, parts[1]) != number || value is string && text.IndexOfAny(new[] { '.', ',' }) is var separator && separator >= 0 && text.Length - separator - 1 > parts[1])
                    throw new LoiNhapLieu(field, label + " tối đa " + parts[1] + " chữ số thập phân.");
                var maximum = 1m;
                for (var n = 0; n < parts[0] - parts[1]; n++) maximum *= 10;
                if (number <= -maximum || number >= maximum) throw new LoiNhapLieu(field, label + " vượt giới hạn lưu trữ.");
                if (number < 0) throw new LoiNhapLieu(field, label + " không được âm.");
                if (field is "SoLuong" or "SoLuongDuKien" && number <= 0) throw new LoiNhapLieu(field, label + " phải lớn hơn 0.");
                if (field == "TyLeHoaHong" && number > 1) throw new LoiNhapLieu(field, label + " phải từ 0 đến 1.");
            }
            if (type == "int")
            {
                var number = int.Parse(text, CultureInfo.InvariantCulture);
                var positive = field is "SoNgayDung" or "LanThu" or "PhienBan" or "SoCaDinhMuc" or "ThoiLuongDuKien" or "ThoiGianDuKien" or "SoThuTu";
                if (number < 0 || positive && number == 0)
                    throw new LoiNhapLieu(field, label + (positive ? " phải là số nguyên lớn hơn 0." : " không được âm."));
            }
            if (type is "date" or "datetime2" or "datetime")
            {
                if (value is not DateTime && !DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out _))
                    throw new LoiNhapLieu(field, label + " phải là ngày hợp lệ.");
                if (field == "NgaySinh" && Convert.ToDateTime(value, CultureInfo.CurrentCulture).Date > DateTime.Today)
                    throw new LoiNhapLieu(field, "Ngày sinh không được sau hôm nay.");
            }
            if (type == "time" && (!TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var clock) || clock < TimeSpan.Zero || clock >= TimeSpan.FromDays(1)))
                throw new LoiNhapLieu(field, label + " phải là giờ từ 00:00 đến 23:59.");
            if (field is "SDT" or "DienThoai")
            {
                var digits = text.Count(char.IsAsciiDigit);
                if (!Regex.IsMatch(text, @"^\+?[0-9 ()\-.]+$") || digits is < 8 or > 15)
                    throw new LoiNhapLieu(field, label + " cần 8–15 chữ số; chỉ dùng số, dấu + ở đầu, khoảng trắng, dấu ngoặc, chấm hoặc gạch nối.");
            }
            if (field == "Email" && (!MailAddress.TryCreate(text, out var address) || address.Address != text || !Regex.IsMatch(text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") || text.Contains("..") || text.StartsWith('.') || text.Contains(".@") || text.Contains("@.") || text.EndsWith('.')))
                throw new LoiNhapLieu(field, label + " không đúng định dạng, ví dụ ten@example.com.");
            if (table == "BENH_NHAN" && field == "HoTenBN" && (!text.Any(char.IsLetter) || !Regex.IsMatch(text, @"^[\p{L}\p{M} .'’\-]+$")))
                throw new LoiNhapLieu(field, "Họ tên bệnh nhân phải có chữ; không nhập số, biểu tượng hoặc ký tự đặc biệt. Ví dụ Nguyễn Văn An.");
            if (table == "BENH_NHAN" && field == "NhomMau" && !Regex.IsMatch(text.Replace('−', '-').Replace(" ", ""), @"^(?:AB|A|B|O)[+-]?$", RegexOptions.IgnoreCase))
                throw new LoiNhapLieu(field, "Nhóm máu không đúng định dạng. Nhập A, B, AB hoặc O, có thể kèm + hoặc −; để trống nếu chưa xác định.");
            if (table == "BENH_NHAN" && field == "GioiTinh" && text is not ("Nam" or "Nữ" or "Khác"))
                throw new LoiNhapLieu(field, "Giới tính không hợp lệ. Chọn Nam, Nữ hoặc Khác trong danh sách.");
            if(table=="BENH_NHAN"&&field=="MaBHYT"&&!Regex.IsMatch(text,@"^[A-Za-z0-9]+$"))
                throw new LoiNhapLieu(field,"Mã BHYT chỉ gồm chữ và số. Nhập đúng mã trên thẻ, hoặc để trống nếu chưa có thông tin.");
            if(table=="NHA_CUNG_CAP"&&field is "TenNCC" or "NguoiLienHe"&&!text.Any(char.IsLetter))
                throw new LoiNhapLieu(field,label+" phải có chữ để nhận diện. Nhập tên đầy đủ, không chỉ nhập số hoặc ký hiệu.");
        }
        CheckDates(table, values, "NgayBatDau", "NgayDuKienKetThuc");
        CheckDates(table, values, "NgayHieuLuc", "NgayHetHieuLuc");
        CheckDates(table, values, "TuNgay", "DenNgay");
        CheckDates(table, values, "NgayNhap", "HanSuDung");
        CheckDates(table, values, "NgaySinh", "NgayVaoLam");
        CheckDates(table, values, "NgayCap", "HanChungChi");
        CheckDates(table, values, "NgayMua", "HanBaoHanh");
        CheckDates(table, values, "NgayBatDau", "NgayKetThuc");
        CheckDates(table, values, "GioVao", "GioRa");
        if (table == "BAO_TRI_THIET_BI")
        {
            values.TryGetValue("NgayKetThuc", out var ngayKetThuc); values.TryGetValue("TrangThai", out var trangThai);
            values.TryGetValue("NgayBatDau", out var ngayBatDau); values.TryGetValue("NgayBaoTriTiepTheo", out var ngayTiepTheo);
            var ketThuc = ngayKetThuc as DateTime?;
            if ((Convert.ToString(trangThai) is "Chưa xác nhận" or "Đã xác nhận") && ketThuc is not null)
                throw new LoiNhapLieu("NgayKetThuc", "Ngày kết thúc do hệ thống ghi khi hoàn thành bảo trì.");
            var moc = ketThuc ?? ngayBatDau as DateTime?;
            if (moc is not null && ngayTiepTheo is DateTime tiepTheo && tiepTheo.Date <= moc.Value.Date)
                throw new LoiNhapLieu("NgayBaoTriTiepTheo", "Ngày bảo trì tiếp theo phải sau ngày kết thúc hoặc ngày bắt đầu.");
        }
    }

    /// <summary>Chặn ngày kết thúc trước ngày bắt đầu khi cả hai được cung cấp; ngày trống vẫn theo nullable của schema.</summary>
    private static void CheckDates(string table, IDictionary<string, object?> values, string startField, string endField)
    {
        if (values.TryGetValue(startField, out var first) && values.TryGetValue(endField, out var last) && first is DateTime start && last is DateTime end &&
            (startField=="GioVao"?end<start:end.Date<start.Date))
            throw new LoiNhapLieu(endField, TruongDuLieu.Label(table, endField) + " không được trước " + TruongDuLieu.Label(table, startField) + ".");
    }

    /// <summary>Đọc phần chữ đang hiện trong DatePicker, chặn ngày sai trước khi đọc SelectedDate cũ; không tự sửa nội dung nhập.</summary>
    public static DateTime? ReadDate(DatePicker picker)
    {
        if(picker.GetValue(InvalidDateProperty) is string failed && failed.Length>0)
            throw new LoiNhapLieu(FieldName(picker),InputLabel(picker)+": ngày vừa nhập không hợp lệ. Hãy sửa ô ngày hoặc chọn lại trên lịch.");
        picker.ApplyTemplate();
        var text = (picker.Template?.FindName("PART_TextBox", picker) as TextBox)?.Text ?? picker.Text;
        if (string.IsNullOrWhiteSpace(text)) return null;
        // WPF dùng cả định dạng vùng của Windows; CultureInfo.GetCultureInfo bỏ qua user override và có thể đọc sai ngày hợp lệ.
        if (!DateTime.TryParse(text, new CultureInfo(picker.Language.IetfLanguageTag,true), DateTimeStyles.None, out var date) ||
            picker.DisplayDateStart is DateTime first && date.Date < first.Date ||
            picker.DisplayDateEnd is DateTime last && date.Date > last.Date || picker.BlackoutDates.Contains(date.Date))
            throw new LoiNhapLieu(FieldName(picker), InputLabel(picker) + ": ngày không hợp lệ hoặc ngoài phạm vi cho phép.");
        return date.Date;
    }

    /// <summary>Chốt binding/ô DataGrid đang sửa, kiểm tra lỗi chuyển kiểu và ngày đang gõ trước khi đọc/lưu form.</summary>
    public static bool PrepareSave(DependencyObject root, out string error)
    {
        error = "";
        try
        {
            var table = root is UserControl form ? BieuMauHoSo.GetTable(form) : "";
            var elements = Elements(root).OfType<FrameworkElement>().Distinct().Where(x => x.IsEnabled &&
                (x.IsVisible || root is FrameworkElement { IsVisible: false } && IsSectionAvailable(x,root) || table.Length > 0 && x.Tag is string tag && (tag.StartsWith("DB:" + table + ".") || tag.StartsWith("Time:" + table + ".")))).ToArray();
            var failures = new List<(FrameworkElement Field, LoiNhapLieu Error)>();
            // Chỉ chốt ô đang gõ và còn chờ LostFocus; binding PropertyChanged đã tự cập nhật.
            // Không đẩy lại chữ cũ từ control vào model đã được code xử lý/cập nhật trực tiếp.
            foreach (var text in elements.OfType<TextBox>().Where(text=>text.IsKeyboardFocusWithin))
            {
                var binding=text.GetBindingExpression(TextBox.TextProperty);
                if(binding is not null && binding.ParentBinding.UpdateSourceTrigger!=UpdateSourceTrigger.PropertyChanged) binding.UpdateSource();
            }
            foreach (var grid in elements.OfType<DataGrid>().Where(grid => !grid.IsReadOnly))
                if (!grid.CommitEdit(DataGridEditingUnit.Cell, true) || !grid.CommitEdit(DataGridEditingUnit.Row, true))
                    failures.Add((grid, new LoiNhapLieu("", "Dòng đang sửa có dữ liệu sai. Nhập lần thứ là số nguyên dương; số lượng dương và đơn giá không âm, không dùng phân cách hàng nghìn.")));
            var groups = new Dictionary<string, Dictionary<string, (object? Value, FrameworkElement Control)>>();
            foreach (var element in elements)
            {
                if (element is TextBox { IsReadOnly: true }) continue;
                if (table.Length > 0 && element.Tag is string db && (db.StartsWith("DB:") || db.StartsWith("Time:")) && !db.StartsWith("DB:" + table + ".") && !db.StartsWith("Time:" + table + ".")) continue;
                if (Validation.GetHasError(element))
                {
                    var content=Validation.GetErrors(element).FirstOrDefault()?.ErrorContent as string;
                    var message = content is not null && (content.StartsWith("Nhập ")||content.StartsWith("Số ")||content.StartsWith("Đơn "))
                        ? content : InputLabel(element) + ": dữ liệu không đúng kiểu. Kiểm tra số/ngày đã nhập.";
                    SetError(element, message);
                    failures.Add((element, new LoiNhapLieu(FieldName(element), message)));
                    continue;
                }
                try
                {
                    // Kiểm tra chữ gốc trước khi đọc binding hoặc chuyển số vào mô hình có kiểu.
                    if (HasFieldValidator(element) || element.Tag is string tag && (tag.StartsWith("DB:") || tag.StartsWith("Time:"))) ValidateField(element);
                    if (element is DatePicker date) date.SelectedDate = ReadDate(date);
                    if (element.Tag is string path && path.StartsWith("DB:") && path[3..].Split('.') is [var owner, var column])
                    {
                        if (!groups.TryGetValue(owner, out var values)) groups[owner] = values = new();
                        values[column] = (ReadField(element), element);
                    }
                    // Ô không có Tag được kiểm tra theo quy tắc nghiệp vụ riêng của biểu mẫu.
                    if (HasFieldValidator(element) || element.Tag is string attached && (attached.StartsWith("DB:") || attached.StartsWith("Time:")) || element is DatePicker) SetError(element, "");
                }
                catch (LoiNhapLieu ex) { SetError(element, ex.Message); failures.Add((element, ex)); }
            }
            foreach (var (owner, inputs) in groups)
            {
                if (!rules.ContainsKey(owner)) continue;
                var values = inputs.ToDictionary(x => x.Key, x => x.Value.Value);
                while (values.Count > 0)
                {
                    try { ValidateValues(owner, values); break; }
                    catch (LoiNhapLieu ex)
                    {
                        if (!inputs.TryGetValue(ex.Field, out var input)) throw;
                        SetError(input.Control, ex.Message); failures.Add((input.Control, ex));
                        values.Remove(ex.Field);
                    }
                }
            }
            if (failures.Count > 0)
            {
                var first = failures.OrderBy(x => x.Field.IsVisible ? 0 : 1).First();
                error = first.Error.Message;
                RevealField(first.Field);
                ShowError(root, error);
                return false;
            }
            return true;
        }
        catch (LoiNhapLieu ex) { error = ex.Message; MarkError(root, ex); return false; }
    }

    /// <summary>Đánh dấu đúng ô sai và cuộn cả ô cùng dòng lỗi vào vùng nhìn thấy.</summary>
    public static void MarkError(DependencyObject root, LoiNhapLieu error)
    {
        var table=root is UserControl form?BieuMauHoSo.GetTable(form):"";
        var field = error.Field.Length==0 ? null : Elements(root).OfType<FrameworkElement>()
            .Where(x => (FieldName(x) == error.Field || Equals(x.Tag, error.Field)) && x.IsEnabled && x is TextBox or ComboBox or DatePicker or PasswordBox or CheckBox)
            .Where(x => table.Length==0 || x.Tag is not string tag || !tag.StartsWith("DB:") || tag.StartsWith("DB:"+table+"."))
            .OrderBy(x=>x.IsVisible?0:1).ThenBy(x => x.Tag is string tag && tag.StartsWith("Time:") ? 1 : 0).FirstOrDefault();
        if (field is not null)
        {
            SetError(field, error.Message);
            RevealField(field);
        }
        ShowError(root, error.Message);
    }

    private static void RevealField(FrameworkElement field)
    {
        foreach(var expander in Ancestors(field).OfType<Expander>())expander.IsExpanded=true;
        foreach (var tab in Ancestors(field).OfType<TabItem>().Reverse())
            if (tab.Visibility != Visibility.Collapsed && ItemsControl.ItemsControlFromItemContainer(tab) is TabControl tabs) tabs.SelectedItem = tab;
        field.Focus();
        var group = field.Parent is ContentControl { Tag: "InlineField" } wrapper ? wrapper : field;
        group.UpdateLayout(); group.BringIntoView();
    }

    private static bool IsSectionAvailable(FrameworkElement field, DependencyObject root)
    {
        for (DependencyObject? node=field; node is not null; node=LogicalTreeHelper.GetParent(node))
        {
            if (node is FrameworkElement { Visibility: not Visibility.Visible }) return false;
            if (ReferenceEquals(node,root)) return true;
        }
        return false;
    }

    /// <summary>Lỗi của một ô chỉ hiện dưới ô đó; vùng chung dành cho lỗi không gắn được với trường nhập.</summary>
    public static bool ShowError(DependencyObject root, string message)
    {
        bool inline = message.Length > 0 && Elements(root).OfType<FrameworkElement>().Any(field =>
            field.Parent is ContentControl { Tag: "InlineField" } && field.IsVisible && GetHasError(field) && GetError(field) == message);
        var label = Elements(root).OfType<TextBlock>().FirstOrDefault(x => x.Name is "FormError" or "Feedback" or "PageStatus");
        if (label is null) return inline;
        label.Text = inline ? "" : message;
        if (!inline && message.Length > 0) label.BringIntoView();
        return true;
    }

    /// <summary>Lấy tên cột từ Tag DB/Time, hoặc tên control đối với ô nhập không có Tag.</summary>
    private static string FieldName(FrameworkElement element)
        => element.Tag is string tag && (tag.StartsWith("DB:") || tag.StartsWith("Time:")) ? tag.Split('.').Last() : element.Name;

    /// <summary>Ưu tiên nhãn trợ năng và nhãn tiếng Việt của trường dữ liệu khi tạo thông báo.</summary>
    private static string InputLabel(FrameworkElement element)
    {
        var label = AutomationProperties.GetName(element);
        if (!string.IsNullOrWhiteSpace(label)) return label;
        if (element.Tag is string tag && tag.Contains('.')) return TruongDuLieu.Label(tag.Split('.')[0].Split(':').Last(), tag.Split('.').Last());
        return "Ô nhập " + (element.Name.Length > 0 ? element.Name : "đang chỉnh sửa");
    }

    /// <summary>Duyệt cả logical/visual để thấy ô nhập trong template và ô DataGrid đang sửa; tránh đi lặp bằng HashSet.</summary>
    private static IEnumerable<DependencyObject> Elements(DependencyObject root)
    {
        var pending = new Stack<DependencyObject>(); var visited = new HashSet<DependencyObject>(); pending.Push(root);
        while (pending.TryPop(out var current))
        {
            if (!visited.Add(current)) continue;
            yield return current;
            foreach (var child in LogicalTreeHelper.GetChildren(current).OfType<DependencyObject>()) pending.Push(child);
            if (current is Visual)
                for (var index = 0; index < VisualTreeHelper.GetChildrenCount(current); index++) pending.Push(VisualTreeHelper.GetChild(current, index));
        }
    }

    /// <summary>Tìm form chứa ô nhập để đưa lỗi ngày tới đúng vùng thông báo.</summary>
    private static IEnumerable<DependencyObject> Ancestors(DependencyObject current)
    {
        while ((current = (current is Visual ? VisualTreeHelper.GetParent(current) : null) ?? LogicalTreeHelper.GetParent(current)) is not null)
            yield return current;
    }
}
