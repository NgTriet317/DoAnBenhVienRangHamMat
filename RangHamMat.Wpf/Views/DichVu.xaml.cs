using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Globalization;
using BUS;
using ET;

namespace RangHamMat.Views;

public partial class DichVu : UserControl
{
    private readonly BUS_DANHMUC bus=new();
    private readonly Dictionary<string,FrameworkElement> inputs=new();
    private Dictionary<string,object?>? original;
    private DinhNghiaDanhMuc? Current=>CatalogList.SelectedItem as DinhNghiaDanhMuc;
    private NguCanhNghiepVu Actor()=>new BUS_QUYENNGHIEPVU().LayNguCanh(PhienDangNhap.MaNV);
    public DichVu()=>InitializeComponent();
    private void Page_Loaded(object sender,RoutedEventArgs e)
    {
        if(CatalogList.ItemsSource is not null)return;
        CatalogList.ItemsSource=DanhMucHeThong.TatCa;CatalogList.SelectedIndex=0;
    }
    private void Run(Action action){try{Message.Text="";action();}catch(Exception ex){Message.Text=ex.Message;}}
    private void Catalog_Changed(object sender,SelectionChangedEventArgs e)=>Run(()=>{LoadRows();ShowForm(null);});
    private void Reload_Click(object sender,RoutedEventArgs e)=>Run(LoadRows);
    private void LoadRows()
    {
        if(Current is not {} d)return;
        var status=StatusFilter.SelectedIndex switch{0=>d.DangDung,1=>d.NgungDung,_=>""};
        var rows=bus.Lay(d.Bang,SearchBox.Text.Trim(),status,Actor());
        var table=new DataTable();
        foreach(var key in d.Khoa.Concat(d.Truong.Select(f=>f.Cot)).Append("TrangThai").Append("DauVanTay").Distinct())table.Columns.Add(key,typeof(object));
        foreach(var row in rows){var dr=table.NewRow();foreach(var p in row)dr[p.Key]=p.Value??DBNull.Value;table.Rows.Add(dr);}
        CatalogGrid.ItemsSource=table.DefaultView;ResultCount.Text=$"{rows.Count} bản ghi";
    }
    private void Grid_AutoGeneratingColumn(object sender,DataGridAutoGeneratingColumnEventArgs e)
    {
        if(e.PropertyName=="DauVanTay"){e.Cancel=true;return;}
        e.Column.Header=Current?.Truong.FirstOrDefault(f=>f.Cot==e.PropertyName)?.Nhan??(e.PropertyName=="TrangThai"?"Trạng thái":e.PropertyName);
        if(e.PropertyName=="TrangThai"&&e.Column is DataGridTextColumn column&&Current is {} definition)
            column.Binding=new Binding("TrangThai"){Converter=new StatusLabel(definition)};
    }
    private sealed class StatusLabel(DinhNghiaDanhMuc definition):IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
            =>Equals(value,definition.DangDung)?"Đang sử dụng":Equals(value,definition.NgungDung)?"Không sử dụng":value;
        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
    }
    private void Row_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(CatalogGrid.SelectedItem is not DataRowView row)return;
        Run(()=>ShowForm(row.Row.Table.Columns.Cast<DataColumn>().ToDictionary(c=>c.ColumnName,c=>row[c.ColumnName]==DBNull.Value?null:(object?)row[c.ColumnName])));
    }
    private sealed record Choice(string Id,string Name){public override string ToString()=>Name;}
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
            else control=new TextBox{Text=Convert.ToString(value)??"",MinHeight=30,MaxLength=f.DoDai,TextWrapping=TextWrapping.Wrap};
            if(row is not null&&d.Khoa.Contains(f.Cot))control.IsEnabled=false;
            inputs[f.Cot]=control;EditorPanel.Children.Add(control);
        }
        StateButton.IsEnabled=row is not null;
        StateButton.Content=row is null||Equals(row.GetValueOrDefault("TrangThai"),d.DangDung)?"Ngừng sử dụng":"Sử dụng lại";
    }
    private Dictionary<string,object?> Values()=>inputs.ToDictionary(p=>p.Key,p=>p.Value switch
    {
        TextBox t=>(object?)t.Text,ComboBox c=>c.SelectedValue??c.SelectedItem,DatePicker d=>d.SelectedDate,
        CheckBox c=>c.IsChecked??false,_=>null
    });
    private void New_Click(object sender,RoutedEventArgs e)=>Run(()=>{CatalogGrid.SelectedItem=null;ShowForm(null);});
    private void Cancel_Click(object sender,RoutedEventArgs e)=>Run(()=>ShowForm(original));
    private void Save_Click(object sender,RoutedEventArgs e)=>Run(()=>Save(null));
    private void State_Click(object sender,RoutedEventArgs e)=>Run(()=>
    {
        if(Current is null||original is null)return;
        bool active=!Equals(original.GetValueOrDefault("TrangThai"),Current.DangDung);
        if(MessageBox.Show(active?"Sử dụng lại danh mục này?":"Ngừng sử dụng danh mục này? Dữ liệu lịch sử vẫn được giữ.","Xác nhận",MessageBoxButton.YesNo)==MessageBoxResult.Yes)Save(active);
    });
    private void Save(bool? active)
    {
        if(Current is null)return;
        var values=active.HasValue&&original is not null
            ?Current.Truong.ToDictionary(f=>f.Cot,f=>original.GetValueOrDefault(f.Cot)):Values();
        string? firstError=null;FrameworkElement? firstInvalid=null;
        foreach(var field in Current.Truong)
        {
            var control=inputs[field.Cot];RangHamMat.Controls.KiemTraNhapLieu.SetError(control,"");
            try{(Current with{Truong=[field]}).ChuanHoa(new Dictionary<string,object?>{{field.Cot,values[field.Cot]}});}
            catch(ArgumentException ex)
            {
                RangHamMat.Controls.KiemTraNhapLieu.SetError(control,ex.Message);
                firstError??=ex.Message;firstInvalid??=control;
            }
        }
        if(firstError is not null){firstInvalid?.Focus();throw new ArgumentException(firstError);}
        var saved=bus.Luu(Current.Bang,values,original,Actor(),PhienDangNhap.MaPhien,active);
        LoadRows();ShowForm(saved);Message.Text="Đã lưu dữ liệu và nhật ký thao tác.";
    }
}
