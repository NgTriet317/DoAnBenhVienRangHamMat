// PHẦN AI ĐÃ THỰC HIỆN: tạo kiểm tra hồi quy nhập sai trên control WPF thật; các thao tác lưu thử được rollback.
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
using RangHamMat.Views;
using System.Windows.Data;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Input;

/// <summary>Kiểm tra các form từ chối dữ liệu nhập sai và giữ nội dung để sửa.</summary>
internal static class NhapLieuChecks
{
    private static int checks;
    /// <summary>Mở form bệnh nhân và thử lưu điện thoại sai trong giao dịch rollback.</summary>
    public static void Run(Window host)
    {
        CheckSchema();
        CheckNumbers();
        CheckLogin();
        var form = new CuaSoBenhNhan();
        BieuMauHoSo.Configure(form, "BENH_NHAN");
        host.Content = form; host.UpdateLayout(); Drain(host);
        var name = Field(form, "HoTenBN");
        var phone = Field(form, "SDT");
        name.Text = "Bệnh nhân kiểm tra nhập liệu";
        phone.Text = "abc123";
        DuLieuGiaoDien.Transaction(() =>
        {
            if (BieuMauHoSo.TrySave(form, out _, out var error))
                throw new Exception("FAIL: form đang chấp nhận số điện thoại abc123.");
            if (!error.Contains("điện thoại", StringComparison.OrdinalIgnoreCase))
                throw new Exception("FAIL: lỗi phải chỉ rõ số điện thoại.");
            if (name.Text != "Bệnh nhân kiểm tra nhập liệu" || phone.Text != "abc123")
                throw new Exception("FAIL: nhập sai phải giữ nguyên nội dung để sửa.");
            return false;
        });
        Console.WriteLine("PASS: chặn điện thoại sai, thông báo rõ trường và giữ dữ liệu nhập.");
        host.UpdateLayout();
        Require(KiemTraNhapLieu.GetHasError(phone),"Ô điện thoại sai cần được đánh dấu.");
        Require(((Border)phone.Template.FindName("InputBorder",phone)).BorderBrush is SolidColorBrush brush && brush.Color==Color.FromRgb(180,35,24),"Viền đỏ phải hiện trên ô điện thoại sai.");
        phone.Text="0900123456";
        name.Text="   ";
        RejectPatient(form,"Họ tên");
        name.Text=new string('a',256);
        RejectPatient(form,"255");
        name.Text="Nguyễn Thị Ánh";
        phone.Text="";
        var birth=Elements(form).OfType<DatePicker>().Single(box=>Equals(box.Tag,"DB:BENH_NHAN.NgaySinh"));
        birth.SelectedDate=new DateTime(2000,1,1);
        birth.ApplyTemplate();
        var dateInput=(TextBox)birth.Template.FindName("PART_TextBox",birth);
        dateInput.Focus();
        dateInput.Text="31/02/2026";
        name.Focus();Drain(host);
        Require(!KiemTraNhapLieu.PrepareSave(form,out var badDate)&&badDate.Contains("ngày",StringComparison.OrdinalIgnoreCase),"Ngày gõ sai không được đọc thành ngày cũ.");
        birth.SelectedDate=new DateTime(2000,1,1);
        dateInput.Text=birth.SelectedDate.Value.ToString("d",CultureInfo.GetCultureInfo(birth.Language.IetfLanguageTag));
        KiemTraNhapLieu.PrepareSave(form,out _);
        birth.SelectedDate=null;
        DuLieuGiaoDien.Transaction(() =>
        {
            Require(BieuMauHoSo.TrySave(form,out _,out var error),"Bệnh nhân hợp lệ với điện thoại/ngày sinh trống phải lưu được: "+error);
            return false;
        });
        CheckEmployee(host);
        CheckGridBinding(host);
        CheckPrototype(host);
        Console.WriteLine("PASS: "+checks+" kiểm tra nhập liệu/schema/binding trên WPF; mọi bản ghi thử rollback.");
    }

    /// <summary>AI tạo: kiểm tra nullable, bắt buộc và độ dài cho toàn bộ metadata cột ghi của schema, không truy vấn SQL.</summary>
    private static void CheckSchema()
    {
        using var stream=typeof(KiemTraNhapLieu).Assembly.GetManifestResourceStream("RangHamMat.Controls.QuyTacNhapLieu.xml")!;
        var document=XDocument.Load(stream);
        foreach(var table in document.Root!.Elements("Bang"))
        foreach(var column in table.Elements("Cot").Where(c=>!(bool)c.Attribute("Khoa")!))
        {
            var tableName=(string)table.Attribute("Ten")!;
            var field=(string)column.Attribute("Ten")!;
            var values=new Dictionary<string,object?> {[field]=null};
            if((bool)column.Attribute("ChoTrong")!) { KiemTraNhapLieu.ValidateValues(tableName,values); checks++; }
            else Reject(tableName,values,field);
            if((string)column.Attribute("Kieu")! is "nvarchar" or "varchar" && int.TryParse((string)column.Attribute("QuyCach")!,out var limit))
                Reject(tableName,new(){[field]=new string('a',limit+1)},field);
        }
        Reject("BENH_NHAN",new(){["NgaySinh"]=DateTime.Today.AddDays(1)},"NgaySinh");
        Reject("BENH_NHAN",new(){["SDT"]="123"},"SDT");
        Reject("BENH_NHAN",new(){["SDT"]="0900abc123"},"SDT");
        Reject("BENH_NHAN",new(){["SDT"]="1234567890123456"},"SDT");
        KiemTraNhapLieu.ValidateValues("BENH_NHAN",new Dictionary<string,object?> {["SDT"]="+84 (90) 123-4567",["HoTenBN"]="Nguyễn Thị Ánh"}); checks++;
        Reject("NHA_CUNG_CAP",new(){["Email"]="ten.example.com"},"Email");
        Reject("NHAN_VIEN",new(){["NgaySinh"]=new DateTime(2000,1,1),["NgayVaoLam"]=new DateTime(1999,1,1)},"NgayVaoLam");
        Reject("KE_HOACH_DIEU_TRI",new(){["NgayBatDau"]=DateTime.Today,["NgayDuKienKetThuc"]=DateTime.Today.AddDays(-1)},"NgayDuKienKetThuc");
    }

    /// <summary>AI tạo: thử số âm, chữ, NaN, tràn số, phân cách hàng nghìn và precision/scale; giữ ranh giới hợp lệ.</summary>
    private static void CheckNumbers()
    {
        foreach(var text in new[]{"abc","NaN","Infinity","1.000,00","1,000,000","-1","0","0.0001","1000000000000000"})
            Reject("CHI_TIET_DON_THUOC",new(){["SoLuong"]=text},"SoLuong");
        foreach(var text in new[]{"abc","1.5","0","-1","2147483648"})
            Reject("CHI_TIET_DON_THUOC",new(){["SoNgayDung"]=text},"SoNgayDung");
        foreach(var text in new[]{"abc","-1","0.001","10000000000000000"})
            Reject("HOA_DON",new(){["GiamGia"]=text},"GiamGia");
        KiemTraNhapLieu.ValidateValues("CHI_TIET_DON_THUOC",new Dictionary<string,object?> {["SoLuong"]="0,001",["SoNgayDung"]=1});checks++;
        KiemTraNhapLieu.ValidateValues("HOA_DON",new Dictionary<string,object?> {["GiamGia"]="9999999999999999.99"});checks++;
        var converter=new NhapSoConverter();
        Require(Equals(converter.ConvertBack("1,5",typeof(decimal),"Quantity",CultureInfo.GetCultureInfo("en-US")),1.5m),"Converter phải đọc dấu phẩy là phần lẻ.");
        Require(ReferenceEquals(converter.ConvertBack("abc",typeof(decimal),"Quantity",CultureInfo.InvariantCulture),DependencyProperty.UnsetValue),"Converter phải trả UnsetValue khi sai, không ném lỗi runtime.");
        Require(!new SoNhapValidationRule {Field="Price"}.Validate("0.001",CultureInfo.InvariantCulture).IsValid,"Đơn giá 3 số lẻ bị chặn tại binding.");
        var draft=new TreatmentDraft {Quantity=decimal.MaxValue,Price=decimal.MaxValue};
        Require(draft.Total.Contains("Vượt"),"Hiển thị tiền nháp không được sập khi nhập số cực lớn.");
    }

    /// <summary>AI tạo: kiểm tra thiếu tên/mật khẩu trên cửa sổ đăng nhập mẫu; không tuyên bố đã xác thực SQL.</summary>
    private static void CheckLogin()
    {
        var login=new DangNhap();
        Require(!login.TryValidateInputs(out var error)&&error.Contains("tên đăng nhập"),"Đăng nhập không chấp nhận tên trống.");
        ((TextBox)login.FindName("UsernameBox")).Text="test";
        Require(!login.TryValidateInputs(out error)&&error.Contains("mật khẩu"),"Đăng nhập không chấp nhận mật khẩu trống.");
        ((PasswordBox)login.FindName("PasswordBox")).Password="test";
        Require(login.TryValidateInputs(out _),"Form mẫu nhận đủ thông tin; bước này không xác thực SQL.");
        login.Close();
    }

    /// <summary>AI tạo: thử chức vụ và nhân viên trên form thật, kiểm tra thông báo cụ thể và nội dung được giữ.</summary>
    private static void CheckEmployee(Window host)
    {
        var form=new NhanSu();host.Content=form;host.UpdateLayout();Drain(host);
        // Form hiện hành yêu cầu điện thoại/ngày sinh; gọi validator thật, không giả định API cũ đã bị bỏ.
        var validate=typeof(NhanSu).GetMethod("KiemTraDuLieuThemNhanVien",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        bool Read(out string error){object?[] input=[null];var result=(bool)validate.Invoke(form,input)!;error=Convert.ToString(input[0])!;return result;}
        Require(!Read(out _),"Nhân viên trống phải bị chặn.");
        ((TextBox)form.FindName("txtHoTen")).Text="Nhân viên thử";
        ((TextBox)form.FindName("txtSDT")).Text="sai";
        Require(!Read(out var error)&&error.Contains("điện thoại"),"Nhân viên sai điện thoại phải bị chặn.");
        ((TextBox)form.FindName("txtSDT")).Text="0900123456";
        ((ComboBox)form.FindName("cboChucVu")).SelectedIndex=0;
        ((DatePicker)form.FindName("dpNgaySinh")).SelectedDate=DateTime.Today.AddYears(-25);
        ((DatePicker)form.FindName("dpNgayVaoLam")).SelectedDate=DateTime.Today;
        Require(Read(out error),"Nhân viên hợp lệ phải qua validator của nút Lưu: "+error);
    }

    /// <summary>AI tạo: thử binding thực với rule/converter của cột XAML; ô sai không được dùng giá trị model cũ để lưu.</summary>
    private static void CheckGridBinding(Window host)
    {
        var form=new CuaSoKhamDieuTri();
        var grid=(DataGrid)form.FindName("PlanDrafts");
        var column=(DataGridTextColumn)grid.Columns.OfType<DataGridTextColumn>().Single(c=>c.Binding is Binding b&&b.Path.Path=="Quantity");
        var draft=new TreatmentDraft {Quantity=2,Price=10};
        var box=new TextBox {DataContext=draft};
        box.SetBinding(TextBox.TextProperty,column.Binding);
        host.Content=box;host.UpdateLayout();Drain(host);
        box.Text="abc";Drain(host);
        Require(Validation.GetHasError(box)&&draft.Quantity==2,"Binding sai phải đánh dấu lỗi và giữ model cũ.");
        Require(!KiemTraNhapLieu.PrepareSave(box,out _),"Không được lưu model cũ khi ô vẫn đang nhập sai.");
        box.Text="0,125";Drain(host);
        Require(!Validation.GetHasError(box)&&draft.Quantity==0.125m,"Sửa số hợp lệ phải cập nhật model và hết lỗi.");
    }
    /// <summary>AI tạo: form mẫu chưa lưu cũng báo lỗi định dạng; biến động lô vẫn được phép âm để biểu diễn xuất kho.</summary>
    private static void CheckPrototype(Window host)
    {
        var form=new CuaSoNhapXuatVatTu();host.Content=form;Drain(host);
        var box=Elements(form).OfType<TextBox>().Single(x=>Equals(x.Tag,"DB:GIAO_DICH_KHO.SoLuongLo"));
        void Blur()=>box.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice,0,box,null){RoutedEvent=Keyboard.LostKeyboardFocusEvent});
        box.Text="abc";Blur();Require(KiemTraNhapLieu.GetHasError(box),"Trường số trên form mẫu phải báo sai định dạng.");
        box.Text="-1,5";Blur();Require(!KiemTraNhapLieu.GetHasError(box),"Biến động lô hợp lệ được phép âm.");
        var account=new CuaSoTaiKhoan();host.Content=account;Drain(host);
        var password=Elements(account).OfType<PasswordBox>().Single();
        password.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice,0,password,null){RoutedEvent=Keyboard.LostKeyboardFocusEvent});
        Require(KiemTraNhapLieu.GetHasError(password),"Mật khẩu mới đánh dấu bắt buộc trên form mẫu không được trống.");
    }

    /// <summary>AI tạo: yêu cầu lỗi có tên trường cụ thể để chỉ đúng ô nhập.</summary>
    private static void Reject(string table,Dictionary<string,object?> values,string field)
    {
        try {KiemTraNhapLieu.ValidateValues(table,values);throw new Exception("Chấp nhận dữ liệu sai: "+table+"."+field);}
        catch(LoiNhapLieu ex){Require(ex.Field==field,"Lỗi cần chỉ đúng trường "+field);}
    }
    /// <summary>AI tạo: thử dữ liệu bệnh nhân sai trong giao dịch rollback và đối chiếu thông báo.</summary>
    private static void RejectPatient(CuaSoBenhNhan form,string part)
    {
        DuLieuGiaoDien.Transaction(()=>
        {
            Require(!BieuMauHoSo.TrySave(form,out _,out var error)&&error.Contains(part,StringComparison.OrdinalIgnoreCase),"Bệnh nhân sai phải bị chặn: "+part);
            return false;
        });
    }
    /// <summary>AI tạo: xử lý các binding đang chờ trên Dispatcher của control thật.</summary>
    private static void Drain(Window host)=>host.Dispatcher.Invoke(()=>host.UpdateLayout(),DispatcherPriority.ApplicationIdle);
    /// <summary>AI tạo: đếm kiểm tra và phát lỗi để chương trình trả mã thất bại.</summary>
    private static void Require(bool condition,string message){checks++;if(!condition)throw new Exception(message);}

    /// <summary>Tìm ô nhập bệnh nhân theo Tag DB đã khai báo trong XAML.</summary>
    private static TextBox Field(DependencyObject parent, string field)
        => Elements(parent).OfType<TextBox>().Single(box => Equals(box.Tag, "DB:BENH_NHAN." + field));

    /// <summary>Duyệt control hiện hữu, không tạo giao diện thử thay thế.</summary>
    private static IEnumerable<DependencyObject> Elements(DependencyObject parent)
    {
        yield return parent;
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
            foreach (var element in Elements(child)) yield return element;
    }
}
