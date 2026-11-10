using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;

public partial class AnhXquangDuocGiao : UserControl
{
    private readonly NguCanhNghiepVu actor;
    private readonly IDbTransaction? transaction;
    private readonly CauHinhKhoAnh? config;
    public AnhXquangDuocGiao(NguCanhNghiepVu user,IDbTransaction? transaction=null,CauHinhKhoAnh? cauHinh=null)
    {actor=user;this.transaction=transaction;config=cauHinh;InitializeComponent();Reload();}
    private void Reload()
    {
        ImageHost.Content=null;
        try
        {
            var rows=new BUS_ANHXQUANG(transaction,config).LayLuotDuocGiao(actor);
            VisitPicker.ItemsSource=rows.Select(x=>new LuaChonDanhMuc(x.MaLS,$"{x.ThoiGianKham:dd/MM HH:mm} · {x.HoTenBN} ({x.MaBN}) · {x.MaLS}")).ToArray();
            StatusText.Text=rows.Count==0?"Chưa có lượt chụp đang mở được giao trong phạm vi của bạn.":$"{rows.Count} lượt chụp đang mở. Quyền được kiểm tra lại khi tải hoặc xem ảnh.";
        }
        catch(InvalidOperationException ex){VisitPicker.ItemsSource=null;StatusText.Text=ex.Message;}
    }
    private void Visit_Changed(object sender,SelectionChangedEventArgs e)
    {ImageHost.Content=VisitPicker.SelectedValue is string id?new AnhXquangLanKham(id,actor,transaction,config):null;}
    private void Reload_Click(object sender,RoutedEventArgs e)=>Reload();
}
