using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using RangHamMat.Controls;
using RangHamMat.Views;

internal static class InlineValidationChecks
{
    public static void Run()
    {
        var form = new CuaSoNhaCungCap();
        var host = new Window
        {
            Content = new Border { Child = form, Padding = new Thickness(20), Background = Brushes.White },
            Width = 850, Height = 850, ShowInTaskbar = false
        };
        try
        {
            host.Show(); Drain(host);
            var email = (TextBox)form.FindName("txtEmail");
            var phone = (TextBox)form.FindName("txtSDT");
            var name = (TextBox)form.FindName("txtTenNCC");
            name.Text = "Nhà cung cấp kiểm tra";
            ((TextBox)form.FindName("txtNguoiLienHe")).Text = "Nguyễn Minh An";
            email.Text = "email_sai";
            phone.Text = "abc123";
            var normalHeight = ((FrameworkElement)phone.Parent).ActualHeight;
            KiemTraNhapLieu.MarkError(form, new LoiNhapLieu("Email", "Email không đúng định dạng."));
            KiemTraNhapLieu.MarkError(form, new LoiNhapLieu("SDT", "Số điện thoại cần 8–15 chữ số; chỉ dùng số, dấu + ở đầu, khoảng trắng, dấu ngoặc, chấm hoặc gạch nối."));
            Drain(host);
            CheckBelow(email, host); CheckBelow(phone, host);
            Require(Label(phone).ActualHeight > 24, "Lỗi dài phải xuống dòng.");
            Require(((FrameworkElement)phone.Parent).ActualHeight > normalHeight, "Vùng nhập phải tự giãn khi có lỗi.");
            Require(((TextBlock)form.FindName("FormError")).Text.Length == 0, "Lỗi của ô không lặp ở cuối form.");
            var address = (TextBox)form.FindName("txtDiaChi");
            Require(Label(phone).TranslatePoint(new Point(), host).Y + Label(phone).ActualHeight <= address.TranslatePoint(new Point(), host).Y, "Lỗi không được chồng lên ô phía dưới.");
            Capture(host, "loi-duoi-truong-nha-cung-cap");
            host.Width = 640; host.Height = 700; Drain(host);
            KiemTraNhapLieu.MarkError(form, new LoiNhapLieu("Email", "Email không đúng định dạng, ví dụ ten@example.com."));
            Drain(host); CheckBelow(email, host);
            var errorBottom = Label(email).TranslatePoint(new Point(), host).Y + Label(email).ActualHeight;
            Require(errorBottom < host.ActualHeight - 60, "Cuộn phải đưa cả dòng lỗi vào vùng nhìn thấy.");
            Capture(host, "loi-duoi-truong-cua-so-hep");
            phone.Focus(); phone.Text = "02812345678"; email.Focus(); Drain(host);
            Require(!KiemTraNhapLieu.GetHasError(phone) && !Label(phone).IsVisible, "Sửa đúng và rời ô phải xóa lỗi tại ô.");
            email.Text = "contact@example.com"; name.Focus(); Drain(host);
            Require(!Label(email).IsVisible && ((FrameworkElement)phone.Parent).ActualHeight == normalHeight, "Hết lỗi thì trả lại chiều cao ban đầu.");
            KiemTraNhapLieu.ShowError(form, "Không thể kết nối database.");
            Require(((TextBlock)form.FindName("FormError")).Text.Length > 0, "Lỗi hệ thống vẫn có thông báo chung.");

            var maintenance = new CuaSoBaoTri();
            host.Content = new Border { Child = maintenance, Padding = new Thickness(20), Background = Brushes.White };
            host.Width = 1000; host.Height = 850; Drain(host);
            var device = (ComboBox)maintenance.FindName("cboThietBi");
            KiemTraNhapLieu.MarkError(maintenance, new LoiNhapLieu("MaTB", "Vui lòng chọn thiết bị."));
            Drain(host); CheckBelow(device, host);
            var endDate = (DatePicker)maintenance.FindName("dpKetThuc");
            var endTime = (TextBox)maintenance.FindName("txtGioKetThuc");
            endDate.SelectedDate = DateTime.Today; endDate.ApplyTemplate();
            var dateInput = (TextBox)endDate.Template.FindName("PART_TextBox", endDate);
            dateInput.Focus(); dateInput.Text = "31/02/2026"; endTime.Focus(); Drain(host);
            CheckBelow(endDate, host);
            Require(((TextBlock)maintenance.FindName("FormError")).Text.Length == 0, "Ngày nhập sai phải báo dưới ô ngày.");
            endDate.SelectedDate = DateTime.Today.AddDays(1);
            KiemTraNhapLieu.MarkError(maintenance, new LoiNhapLieu("NgayKetThuc", "Vui lòng chọn ngày kết thúc."));
            Drain(host); CheckBelow(endDate, host);
            Require(!KiemTraNhapLieu.GetHasError(endTime), "Lỗi ngày không được gắn nhầm vào ô giờ cùng cột.");
            KiemTraNhapLieu.SetError(endDate, "");
            endTime.Text = "25:99";
            KiemTraNhapLieu.MarkError(maintenance, new LoiNhapLieu("Time:BAO_TRI_THIET_BI.NgayKetThuc", "Giờ phải có dạng HH:mm, từ 00:00 đến 23:59."));
            Drain(host); CheckBelow(endTime, host);
            Require(!KiemTraNhapLieu.GetHasError(endDate), "Lỗi giờ phải gắn đúng ô giờ.");
            Capture(host, "loi-duoi-truong-bao-tri");
            try
            {
                KiemTraNhapLieu.ValidateValues("BAO_TRI_THIET_BI", new Dictionary<string, object?> { ["TrangThai"] = "Hoàn thành", ["NgayKetThuc"] = null });
                throw new InvalidOperationException("Phải phát hiện thiếu ngày kết thúc.");
            }
            catch (LoiNhapLieu ex) { Require(ex.Field == "NgayKetThuc", "Thiếu ngày hoàn thành phải chỉ đúng trường."); }

            CheckLogin();
            CheckCoverage();
            Console.WriteLine("PASS: lỗi dưới ô TextBox/ComboBox/DatePicker/PasswordBox, giờ/ngày đúng trường, nhiều lỗi, lỗi dài, không đè bố cục, tự cuộn/xóa lỗi, lỗi hệ thống và phủ các form.");
        }
        finally { host.Close(); }
    }

    private static void CheckLogin()
    {
        var login = new DangNhap { ShowInTaskbar = false };
        try
        {
            login.Show(); Drain(login);
            Require(!login.TryValidateInputs(out _), "Đăng nhập phải chặn tên trống.");
            Drain(login); CheckBelow((TextBox)login.FindName("UsernameBox"), login);
            ((TextBox)login.FindName("UsernameBox")).Text = "ten_kiem_tra";
            Require(!login.TryValidateInputs(out _), "Đăng nhập phải chặn mật khẩu trống.");
            Drain(login); CheckBelow((PasswordBox)login.FindName("PasswordBox"), login);
            Require(((TextBlock)login.FindName("FormError")).Text.Length == 0, "Lỗi đăng nhập trống phải nằm tại trường.");
        }
        finally { login.Close(); }
    }

    private static void CheckCoverage()
    {
        XNamespace w = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var count = 0;
        foreach (var path in System.IO.Directory.GetFiles("RangHamMat.Wpf/Views", "*.xaml"))
        {
            var root = XDocument.Load(path);
            foreach (var field in root.Descendants().Where(node => new[] { "TextBox", "ComboBox", "DatePicker", "PasswordBox" }.Contains(node.Name.LocalName)
                && ((string?)node.Attribute("Tag")) is string tag && (tag.StartsWith("DB:") || tag.StartsWith("Time:"))
                && (string?)node.Attribute("IsReadOnly") != "True"))
            {
                Require(field.Parent?.Name == w + "ContentControl" && (string?)field.Parent.Attribute("Style") == "{StaticResource TruongNhapLieu}", "Ô nhập thiếu vùng báo lỗi: " + path);
                count++;
            }
        }
        Require(count >= 219, "Phải phủ các ô nhập dùng cơ chế chung.");
    }

    private static TextBlock Label(FrameworkElement field)
    {
        Require(field.Parent is ContentControl { Tag: "InlineField" }, "Ô nhập phải có vùng báo lỗi.");
        var wrapper = (ContentControl)field.Parent;
        wrapper.ApplyTemplate();
        return (TextBlock)wrapper.Template.FindName("InlineErrorText", wrapper);
    }
    private static void CheckBelow(FrameworkElement field, Window host)
    {
        var label = Label(field);
        Require(label.IsVisible && label.Text == KiemTraNhapLieu.GetError(field), "Thông báo phải hiện đúng dưới ô sai.");
        Require(label.TranslatePoint(new Point(), host).Y >= field.TranslatePoint(new Point(), host).Y + field.ActualHeight, "Thông báo phải ở dưới, không nằm trong/đè ô nhập.");
    }
    private static void Drain(Window host)
    {
        host.UpdateLayout(); host.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); host.UpdateLayout();
    }
    private static void Capture(Window host, string name)
    {
        var content = (FrameworkElement)host.Content;
        var path = System.IO.Path.Combine(Environment.CurrentDirectory, "XemTruoc", "loi-nhap-lieu");
        System.IO.Directory.CreateDirectory(path);
        var bitmap = new RenderTargetBitmap((int)content.ActualWidth, (int)content.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = System.IO.File.Create(System.IO.Path.Combine(path, name + ".png")); encoder.Save(output);
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
