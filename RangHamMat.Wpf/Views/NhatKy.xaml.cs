using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;

namespace RangHamMat.Views;

public partial class NhatKy:UserControl
{
    private int page=1,total;
    private bool ready;
    private BoLocNhatKy? applied;
    public NhatKy()=>InitializeComponent();
    private void Page_Loaded(object sender,RoutedEventArgs e)
    {
        if(ready)return;ready=true;
        ActionFilter.ItemsSource=HanhDongNhatKy.LuaChon;
        ActionFilter.SelectedIndex=0;
        Load(true);
    }
    private void Load(bool newFilter=false)
    {
        if(!ready)return;
        try
        {
            Message.Text="";
            if(newFilter||applied is null)
            {
                page=1;
                if(!TimeSpan.TryParseExact(FromTime.Text.Trim(),@"hh\:mm",null,out var from)||!TimeSpan.TryParseExact(ToTime.Text.Trim(),@"hh\:mm",null,out var to)||from.TotalHours>=24||to.TotalHours>=24)
                    throw new ArgumentException("Nhập giờ theo định dạng HH:mm (00:00–23:59).");
                applied=new BoLocNhatKy{TuUtc=FromDate.SelectedDate?.Date.Add(from).AddHours(-7),DenUtc=ToDate.SelectedDate?.Date.Add(to).AddMinutes(1).AddHours(-7),TaiKhoan=AccountFilter.Text.Trim(),PhanHe=ModuleFilter.Text.Trim(),HanhDong=ActionFilter.SelectedValue as string??"",KetQua=ResultFilter.Text.Trim(),MaDoiTuong=ObjectFilter.Text.Trim()};
            }
            applied.Trang=page;applied.KichThuocTrang=int.Parse(Convert.ToString((PageSize.SelectedItem as ComboBoxItem)?.Content)??"50");
            var actor=new BUS_QUYENNGHIEPVU().LayNguCanh(PhienDangNhap.MaNV);
            var stats=new BUS_NHATKY().ThongKe(actor);
            Statistics.Text=$"Hôm nay: {stats.DangNhap} đăng nhập thành công · {stats.DangNhapThatBai} đăng nhập thất bại · {stats.ThayDoi} thao tác thay đổi dữ liệu";
            var result=new BUS_NHATKY().LayTrang(applied,actor);total=result.Tong;
            var table=new DataTable();
            foreach(var row in result.Dong)
            {
                foreach(var key in row.Keys)if(!table.Columns.Contains(key))table.Columns.Add(key,typeof(object));
                if(!table.Columns.Contains("ThoiGianHienThi"))table.Columns.Add("ThoiGianHienThi",typeof(object));
                var dr=table.NewRow();foreach(var p in row)
                {
                    object? value=p.Value;
                    if(p.Key=="HanhDong")value=HanhDongNhatKy.Nhan(Convert.ToString(value));
                    if(p.Key.EndsWith("Utc")&&value is DateTime date)value=date.AddHours(7).ToString("dd/MM/yyyy HH:mm:ss")+" UTC+7";
                    dr[p.Key]=value??DBNull.Value;
                }
                dr["ThoiGianHienThi"]=row.TryGetValue("ThoiGianUtc",out var utc)&&utc is DateTime time
                        ?time.AddHours(7).ToString("dd/MM/yyyy HH:mm:ss")+" UTC+7"
                        :Convert.ToString(row["ThoiGian"])+" (cũ, chưa rõ múi giờ)";
                table.Rows.Add(dr);
            }
            AuditGrid.ItemsSource=table.DefaultView;ShowDetail(null);
            PageLabel.Text=$"Trang {page}/{Math.Max(1,(int)Math.Ceiling((double)total/applied.KichThuocTrang))} · {total} bản ghi";
        }
        catch(Exception ex){AuditGrid.ItemsSource=null;ShowDetail(null);Message.Text=ex.Message;}
    }
    private void Grid_AutoGeneratingColumn(object sender,DataGridAutoGeneratingColumnEventArgs e)
    {
        if(e.PropertyName is "DuLieuTruoc" or "DuLieuSau" or "MaSuKien" or "MaPhien" or "MaTK" or "ThoiGian" or "ThoiGianUtc"){e.Cancel=true;return;}
        e.Column.Header=e.PropertyName switch{"ThoiGianHienThi"=>"Thời gian","DangNhapUtc"=>"Đăng nhập UTC+7","DangXuatUtc"=>"Đăng xuất UTC+7","HoatDongCuoiUtc"=>"Hoạt động cuối UTC+7","TrangThaiPhien"=>"Trạng thái phiên","HoTen"=>"Nhân viên","TenDangNhap"=>"Tài khoản","HanhDong"=>"Hành động","PhanHe"=>"Phân hệ","LoaiDoiTuong"=>"Đối tượng","MaDoiTuong"=>"Mã đối tượng","KetQua"=>"Kết quả",_=>e.PropertyName};
    }
    private void Grid_ColumnsGenerated(object? sender,EventArgs e)
    {
        string[] order=["ThoiGianHienThi","HoTen","TenDangNhap","HanhDong","PhanHe","LoaiDoiTuong","MaDoiTuong","KetQua","MaNK"];
        int index=0;
        foreach(var field in order)
        {
            var column=AuditGrid.Columns.FirstOrDefault(c=>c.SortMemberPath==field);
            if(column is not null)column.DisplayIndex=index++;
        }
    }
    private void Row_Changed(object sender,SelectionChangedEventArgs e)
    {
        ShowDetail(AuditGrid.SelectedItem as DataRowView);
    }
    private void ClearSelection_Click(object sender,RoutedEventArgs e)=>AuditGrid.SelectedItem=null;
    private void Reload_Click(object sender,RoutedEventArgs e)=>Load(true);
    private void Clear_Click(object sender,RoutedEventArgs e)
    {FromDate.SelectedDate=null;ToDate.SelectedDate=null;FromTime.Text="00:00";ToTime.Text="23:59";AccountFilter.Clear();ModuleFilter.Clear();ActionFilter.SelectedIndex=0;ResultFilter.Clear();ObjectFilter.Clear();Load(true);}
    private void Size_Changed(object sender,SelectionChangedEventArgs e)=>Load(true);
    private void Previous_Click(object sender,RoutedEventArgs e){if(page>1){page--;Load();}}
    private void Next_Click(object sender,RoutedEventArgs e){if(applied is not null&&page*applied.KichThuocTrang<total){page++;Load();}}
}
