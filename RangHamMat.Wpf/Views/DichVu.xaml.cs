using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Automation;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Globalization;
using BUS;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>
/// Màn hình quản lý danh mục theo metadata, tạo biểu mẫu và kiểm tra từng trường trước khi ghi dữ liệu.
/// </summary>
public partial class DichVu : UserControl
{
    /// <summary>
    /// Đầu mối đọc và lưu danh mục, có kiểm tra quyền và ghi nhật ký ở tầng nghiệp vụ.
    /// </summary>
    private readonly BUS_DANHMUC bus=new();
    /// <summary>
    /// Ánh xạ tên cột sang điều khiển nhập liệu được tạo cho danh mục đang chọn.
    /// </summary>
    private readonly Dictionary<string,FrameworkElement> inputs=new();
    /// <summary>
    /// Bản dữ liệu gốc của dòng đang sửa để đối chiếu khi lưu và khôi phục khi hủy.
    /// </summary>
    private Dictionary<string,object?>? original;
    /// <summary>
    /// Định nghĩa của danh mục đang chọn trong danh sách bên trái.
    /// </summary>
    private DinhNghiaDanhMuc? Current=>CatalogList.SelectedItem as DinhNghiaDanhMuc;
    /// <summary>
    /// Đọc lại ngữ cảnh nghiệp vụ từ nhân viên đăng nhập trước khi yêu cầu thao tác danh mục.
    /// </summary>
    /// <returns>Ngữ cảnh đã được BUS_QUYENNGHIEPVU xác thực.</returns>
    private NguCanhNghiepVu Actor()=>new BUS_QUYENNGHIEPVU().LayNguCanh(PhienDangNhap.MaNV);
    /// <summary>
    /// Nạp bố cục quản lý danh mục và đăng ký các sự kiện đã khai báo trong XAML.
    /// </summary>
    public DichVu()=>InitializeComponent();
    /// <summary>
    /// Nạp danh sách định nghĩa danh mục một lần và chọn danh mục đầu tiên.
    /// </summary>
    private void Page_Loaded(object sender,RoutedEventArgs e)
    {
        if(CatalogList.ItemsSource is not null)return;
        CatalogList.ItemsSource=DanhMucHeThong.TatCa;CatalogList.SelectedIndex=0;
    }
    /// <summary>
    /// Chạy thao tác giao diện với vùng thông báo lỗi chung.
    /// </summary>
    /// <param name="action">Thao tác cần chạy; ngoại lệ được chuyển thành nội dung Message.</param>
    private void Run(Action action){try{Message.Text="";action();}catch(Exception ex){Message.Text=ex.Message;}}
    /// <summary>
    /// Đọc các dòng của danh mục mới chọn và mở biểu mẫu thêm mới theo metadata tương ứng.
    /// </summary>
    private void Catalog_Changed(object sender,SelectionChangedEventArgs e)=>Run(()=>{LoadRows();ShowForm(null);});
    /// <summary>
    /// Tải lại các dòng theo danh mục, từ khóa và bộ lọc trạng thái hiện tại.
    /// </summary>
    private void Reload_Click(object sender,RoutedEventArgs e)=>Run(LoadRows);
    /// <summary>
    /// Đọc bản ghi có quyền, tạo bảng theo khóa và trường metadata rồi cập nhật danh sách cùng số kết quả.
    /// </summary>
    private void LoadRows()
    {
        if(Current is not {} d)return;
        if(StatusFilter.SelectedIndex is <0 or >2)
        {
            KiemTraNhapLieu.SetError(StatusFilter,"Trạng thái danh mục: chọn Đang sử dụng, Không sử dụng hoặc Tất cả.");
            StatusFilter.Focus();return;
        }
        KiemTraNhapLieu.SetError(StatusFilter,"");
        var status=StatusFilter.SelectedIndex switch{0=>d.DangDung,1=>d.NgungDung,_=>""};
        var rows=bus.Lay(d.Bang,SearchBox.Text.Trim(),status,Actor());
        var table=new DataTable();
        foreach(var key in d.Khoa.Concat(d.Truong.Select(f=>f.Cot)).Append("TrangThai").Append("DauVanTay").Distinct())table.Columns.Add(key,typeof(object));
        foreach(var row in rows){var dr=table.NewRow();foreach(var p in row)dr[p.Key]=p.Value??DBNull.Value;table.Rows.Add(dr);}
        CatalogGrid.ItemsSource=table.DefaultView;ResultCount.Text=$"{rows.Count} bản ghi";
    }
    /// <summary>
    /// Ẩn dấu vân tay khỏi bảng, đổi nhãn cột theo metadata và ánh xạ trạng thái sang nhãn hiển thị.
    /// </summary>
    private void Grid_AutoGeneratingColumn(object sender,DataGridAutoGeneratingColumnEventArgs e)
    {
        if(e.PropertyName=="DauVanTay"){e.Cancel=true;return;}
        e.Column.Header=Current?.Truong.FirstOrDefault(f=>f.Cot==e.PropertyName)?.Nhan??(e.PropertyName=="TrangThai"?"Trạng thái":e.PropertyName);
        if(e.PropertyName=="TrangThai"&&e.Column is DataGridTextColumn column&&Current is {} definition)
            column.Binding=new Binding("TrangThai"){Converter=new StatusLabel(definition)};
    }
    /// <summary>
    /// Bộ chuyển đổi trạng thái riêng của từng danh mục sang nhãn đang sử dụng hoặc không sử dụng.
    /// </summary>
    /// <param name="definition">Định nghĩa cung cấp giá trị trạng thái đang dùng và ngừng dùng.</param>
    private sealed class StatusLabel(DinhNghiaDanhMuc definition):IValueConverter
    {
        /// <summary>
        /// Đổi hai giá trị trạng thái của danh mục sang nhãn chung, giữ nguyên giá trị khác nếu có.
        /// </summary>
        /// <returns>Nhãn trạng thái hoặc giá trị đầu vào khi không khớp hai trạng thái đã định nghĩa.</returns>
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
            =>Equals(value,definition.DangDung)?"Đang sử dụng":Equals(value,definition.NgungDung)?"Không sử dụng":value;
        /// <summary>
        /// Giữ binding trạng thái theo chiều hiển thị, không ghi nhãn đã dịch ngược vào dữ liệu.
        /// </summary>
        /// <returns>Binding.DoNothing để WPF bỏ qua cập nhật nguồn.</returns>
        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
    }
    /// <summary>
    /// Chuyển dòng đang chọn thành từ điển giá trị và mở biểu mẫu chỉnh sửa của dòng đó.
    /// </summary>
    private void Row_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(CatalogGrid.SelectedItem is not DataRowView row)return;
        Run(()=>ShowForm(row.Row.Table.Columns.Cast<DataColumn>().ToDictionary(c=>c.ColumnName,c=>row[c.ColumnName]==DBNull.Value?null:(object?)row[c.ColumnName])));
    }
    /// <summary>
    /// Lựa chọn danh mục cha giữ riêng mã lưu và tên hiển thị cho ComboBox.
    /// </summary>
    private sealed record Choice(string Id,string Name){
        /// <summary>
        /// Trả tên lựa chọn để ComboBox hiển thị trong khi giá trị lưu vẫn là mã.
        /// </summary>
        /// <returns>Tên hiển thị của lựa chọn danh mục cha.</returns>
        public override string ToString()=>Name;}
    /// <summary>
    /// Dựng biểu mẫu theo kiểu trường và danh mục cha, giữ lựa chọn cũ đã ngừng dùng và khóa các trường khóa khi sửa.
    /// </summary>
    /// <param name="row">Bản ghi cần sửa; null để tạo biểu mẫu thêm mới.</param>
    private void ShowForm(Dictionary<string,object?>? row)
    {
        if(Current is not {} d)return;
        original=row;inputs.Clear();EditorPanel.Children.Clear();
        FormTitle.Text=row is null?"Thêm mới · "+d.Ten:"Chỉnh sửa · "+string.Join(" / ",d.Khoa.Select(k=>row[k]));
        foreach(var f in d.Truong)
        {
            EditorPanel.Children.Add(new TextBlock{Text=f.Nhan+(f.BatBuoc?" *":""),Margin=new Thickness(0,6,0,4)});
            object? value=row?.GetValueOrDefault(f.Cot);
            FrameworkElement control;
            if(f.BangCha is not null)
            {
                var parent=DanhMucHeThong.Lay(f.BangCha);
                var options=bus.Lay(parent.Bang,"",parent.DangDung,Actor()).Select(r=>new Choice(Convert.ToString(r[parent.Khoa[0]])??"",Convert.ToString(r[parent.Truong.First(x=>x.Cot.StartsWith("Ten")).Cot])??"")).ToList();
                string id=Convert.ToString(value)??"";
                if(id.Length>0&&!options.Any(o=>o.Id==id))options.Add(new(id,id+" · đã ngừng dùng"));
                control=new ComboBox{ItemsSource=options,SelectedValuePath="Id",SelectedValue=id,MinHeight=30};
            }
            else if(f.LuaChon is not null)control=new ComboBox{ItemsSource=f.LuaChon,SelectedItem=value,MinHeight=30};
            else if(f.Kieu=="date")control=new DatePicker{SelectedDate=value is DateTime date?date:DateTime.Today};
            else if(f.Kieu=="bool")control=new CheckBox{IsChecked=value is bool flag?flag:true,Content="Có"};
            else control=new TextBox{Text=Convert.ToString(value)??"",MinHeight=30,TextWrapping=TextWrapping.Wrap};
            if(row is not null&&d.Khoa.Contains(f.Cot))control.IsEnabled=false;
            control.Tag=$"DB:{d.Bang}.{f.Cot}";
            AutomationProperties.SetName(control,f.Nhan);
            control.LostKeyboardFocus+=(_,_)=>{if(!control.IsKeyboardFocusWithin&&control.IsEnabled)KiemTraTruong(f,control);};
            // Chỉ kiểm tra lại ô đang báo lỗi trong lúc gõ; không làm mất lỗi ở các ô khác.
            void Recheck(object? sender,RoutedEventArgs e){if(KiemTraNhapLieu.GetHasError(control))KiemTraTruong(f,control);}
            if(control is TextBox text)text.TextChanged+=Recheck;
            if(control is ComboBox combo)combo.SelectionChanged+=Recheck;
            if(control is DatePicker picker)
            {
                picker.AddHandler(TextBoxBase.TextChangedEvent,new TextChangedEventHandler(Recheck));
                picker.SelectedDateChanged+=Recheck;
                picker.DateValidationError+=(_,e)=>
                {
                    e.ThrowException=false;
                    KiemTraNhapLieu.SetError(picker,f.Nhan+": ngày không hợp lệ. Nhập dd/MM/yyyy hoặc chọn lại trên lịch, ví dụ 25/10/2026.");
                };
            }
            inputs[f.Cot]=control;
            EditorPanel.Children.Add(new ContentControl{Content=control,Style=(Style)FindResource("TruongNhapLieu")});
        }
        StateButton.IsEnabled=row is not null;
        StateButton.Content=row is null||Equals(row.GetValueOrDefault("TrangThai"),d.DangDung)?"Ngừng sử dụng":"Sử dụng lại";
    }
    /// <summary>
    /// Đọc các điều khiển động thành từ điển cột/giá trị để chuẩn hóa và lưu.
    /// </summary>
    /// <returns>Giá trị văn bản, mã lựa chọn, ngày hoặc cờ tương ứng với từng cột metadata.</returns>
    private Dictionary<string,object?> Values()=>inputs.ToDictionary(p=>p.Key,p=>p.Value switch
    {
        TextBox t=>(object?)t.Text,ComboBox c=>c.SelectedValue??c.SelectedItem,DatePicker d=>KiemTraNhapLieu.ReadDate(d),
        CheckBox c=>c.IsChecked??false,_=>null
    });
    /// <summary>Kiểm tra một ô động theo cả schema và metadata, gồm khóa ghép nhập tay và lựa chọn danh mục cha.</summary>
    private bool KiemTraTruong(TruongDanhMuc field,FrameworkElement control)
    {
        if(Current is not {} definition||!control.IsEnabled)return true;
        try
        {
            object? value=control switch
            {
                TextBox text=>text.Text,
                ComboBox combo=>combo.SelectedValue??combo.SelectedItem,
                DatePicker date=>KiemTraNhapLieu.ReadDate(date),
                CheckBox check=>check.IsChecked??false,
                _=>null
            };
            if(control is ComboBox choice&&(choice.SelectedIndex<0||!choice.Items.Contains(choice.SelectedItem)))
                throw new ArgumentException(field.Nhan+": vui lòng chọn một giá trị trong danh sách.");
            var values=new Dictionary<string,object?>{{field.Cot,value}};
            // ChuanHoa kiểm tra cả khóa mà ValidateValues bỏ qua vì các khóa tự sinh không được nhập trên form.
            (definition with{Truong=[field]}).ChuanHoa(values);
            KiemTraNhapLieu.ValidateValues(definition.Bang,values);
            KiemTraNhapLieu.SetError(control,"");
            return true;
        }
        catch(Exception ex) when(ex is ArgumentException or LoiNhapLieu)
        {
            KiemTraNhapLieu.SetError(control,ex.Message);
            return false;
        }
    }
    /// <summary>Đánh dấu tất cả trường sai trước khi lưu, cuộn tới ô lỗi đầu tiên và giữ nguyên chữ người dùng đã nhập.</summary>
    private bool KiemTraBieuMau()
    {
        if(Current is not {} definition)return false;
        FrameworkElement? first=null;
        foreach(var field in definition.Truong)
            if(!KiemTraTruong(field,inputs[field.Cot]))first??=inputs[field.Cot];
        if(first is null)return true;
        first.Focus();(first.Parent as FrameworkElement)?.BringIntoView();
        Message.Text="Kiểm tra các ô được đánh dấu và sửa theo hướng dẫn bên dưới trước khi lưu.";
        return false;
    }
    /// <summary>
    /// Bỏ lựa chọn dòng hiện tại và mở biểu mẫu thêm mới của danh mục đang chọn.
    /// </summary>
    private void New_Click(object sender,RoutedEventArgs e)=>Run(()=>{CatalogGrid.SelectedItem=null;ShowForm(null);});
    /// <summary>
    /// Khôi phục biểu mẫu từ bản dữ liệu gốc, bỏ nội dung đang nhập chưa lưu.
    /// </summary>
    private void Cancel_Click(object sender,RoutedEventArgs e)=>Run(()=>ShowForm(original));
    /// <summary>
    /// Lưu giá trị biểu mẫu mà không yêu cầu thay đổi trạng thái riêng.
    /// </summary>
    private void Save_Click(object sender,RoutedEventArgs e)=>Run(()=>Save(null));
    /// <summary>
    /// Xin xác nhận ngừng sử dụng hoặc sử dụng lại bản ghi hiện tại rồi lưu với trạng thái được yêu cầu.
    /// </summary>
    private void State_Click(object sender,RoutedEventArgs e)=>Run(()=>
    {
        if(Current is null||original is null)return;
        bool active=!Equals(original.GetValueOrDefault("TrangThai"),Current.DangDung);
        if(MessageBox.Show(active?"Sử dụng lại danh mục này?":"Ngừng sử dụng danh mục này? Dữ liệu lịch sử vẫn được giữ.","Xác nhận",MessageBoxButton.YesNo)==MessageBoxResult.Yes)Save(active);
    });
    /// <summary>
    /// Kiểm tra từng trường, đánh dấu lỗi đầu tiên rồi lưu qua nghiệp vụ cùng bản gốc và mã phiên nhật ký.
    /// </summary>
    /// <remarks>Sau khi lưu thành công, danh sách được tải lại và biểu mẫu nhận bản ghi vừa lưu làm dữ liệu gốc mới.</remarks>
    /// <param name="active">Trạng thái cần đổi; null để lưu nội dung biểu mẫu, có giá trị để dùng dữ liệu gốc và chỉ yêu cầu đổi trạng thái.</param>
    private void Save(bool? active)
    {
        if(Current is null)return;
        if(!active.HasValue&&!KiemTraBieuMau())return;
        var values=active.HasValue&&original is not null
            ?Current.Truong.ToDictionary(f=>f.Cot,f=>original.GetValueOrDefault(f.Cot)):Values();
        Dictionary<string,object?> saved;
        try{saved=bus.Luu(Current.Bang,values,original,Actor(),PhienDangNhap.MaPhien,active);}
        catch(ArgumentException ex)
        {
            var field=Current.Truong.FirstOrDefault(f=>ex.Message.StartsWith(f.Nhan+":",StringComparison.Ordinal))
                ??(ex.Message.StartsWith("Tên danh mục",StringComparison.Ordinal)?Current.Truong.FirstOrDefault(f=>f.Cot.StartsWith("Ten",StringComparison.Ordinal)):null);
            if(field is not null)
            {
                var control=inputs[field.Cot];KiemTraNhapLieu.SetError(control,ex.Message);
                control.Focus();(control.Parent as FrameworkElement)?.BringIntoView();
            }
            throw;
        }
        LoadRows();ShowForm(saved);Message.Text="Đã lưu dữ liệu và nhật ký thao tác.";
    }
}
