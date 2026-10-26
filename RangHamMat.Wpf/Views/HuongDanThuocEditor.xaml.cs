using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls;
namespace RangHamMat.Views;
public partial class HuongDanThuocEditor:UserControl
{
    private readonly BUS_DONTHUOC service;private readonly IDbTransaction? transaction;private readonly NguCanhNghiepVu actor;private BanNhapHuongDanThuoc? saved;private string? selectedDrug;private bool loading=true,dirty,nextVersion;
    public bool CoThayDoiChuaLuu=>dirty||GroupName.Text.Length>0;public event Action? BackRequested;
    public HuongDanThuocEditor(NguCanhNghiepVu? user=null,IDbTransaction? transaction=null)
    {this.transaction=transaction;service=new(transaction);actor=new BUS_QUYENNGHIEPVU(transaction).LayNguCanh(user?.MaNV??PhienDangNhap.MaNV);if(!actor.LaAdmin&&!actor.CoQuyen(QuyenLamViec.CauHinhHoTro))throw new TuChoiQuyenNghiepVuException();InitializeComponent();Groups();SearchDrugs();loading=false;}
    private void Groups(){GroupChoice.ItemsSource=new[]{new LuaChonDanhMuc("","Chưa xếp nhóm")}.Concat(new BUS_NHOMTHUOCKEDON(transaction).LayDanhSach(actor).Where(g=>g.TrangThai=="Đang áp dụng").Select(g=>new LuaChonDanhMuc(g.MaNhomThuoc,g.TenNhom))).ToArray();}
    private void Fill(BanNhapHuongDanThuoc? s)
    {var prior=loading;loading=true;try{saved=s;nextVersion=false;GroupChoice.SelectedValue=s?.CauHinh.MaNhomThuoc??"";PopulationEditor.Text=s?.CauHinh.DoiTuongApDung??"";RouteEditor.Text=s?.CauHinh.DuongDung??"";DoseEditor.Text=s?.CauHinh.LieuDungGoiY??"";InstructionEditor.Text=s?.CauHinh.HuongDanGoiY??"";ReviewText.Text=s is null?"Hướng dẫn mới v1 — chưa duyệt":$"v{s.CauHinh.PhienBan} · {s.CauHinh.TrangThai} · người duyệt {s.CauHinh.MaNguoiDuyet??"chưa có"} · {s.CauHinh.NgayDuyet:dd/MM/yyyy HH:mm}";dirty=false;Rights();}finally{loading=prior;}}
    private void Rights(){var edit=saved is null||saved.CauHinh.TrangThai=="Nháp"||nextVersion;InputPanel.IsEnabled=SaveButton.IsEnabled=edit&&DrugChoice.SelectedValue is string;ApproveButton.IsEnabled=edit&&!nextVersion&&saved is not null&&actor.CoQuyen(QuyenLamViec.DuyetHuongDanThuoc);NextVersionButton.IsEnabled=saved is not null&&saved.CauHinh.TrangThai!="Nháp"&&!nextVersion;}
    private bool Run(Action action,out string error){error="";ErrorText.Text="";try{action();return true;}catch(InvalidOperationException ex){error=ErrorText.Text=ex.Message;return false;}}
    public bool TrySave(out string error)=>Run(()=>{if(DrugChoice.SelectedValue is not string id)throw new LoiKhamException("MaThuoc","Chọn thuốc.");var c=new ET_CAUHINHTHUOCKEDON{MaThuoc=id,MaNhomThuoc=GroupChoice.SelectedValue is string g&&g.Length>0?g:null,DoiTuongApDung=PopulationEditor.Text,DuongDung=RouteEditor.Text,LieuDungGoiY=DoseEditor.Text,HuongDanGoiY=InstructionEditor.Text,TrangThai="Nháp",PhienBan=saved is null?1:saved.CauHinh.PhienBan+(nextVersion?1:0)};Fill(service.LuuHuongDan(c,saved?.DauVanTay,actor));StatusText.Text="Đã lưu nháp; chưa được áp hướng dẫn.";Sync();},out error);
    public bool TryApprove(out string error)=>Run(()=>{if(saved is null||dirty||nextVersion)throw new LoiKhamException("HuongDan","Lưu nháp trước khi duyệt.");Fill(service.DuyetHuongDan(saved.CauHinh.MaThuoc,saved.DauVanTay,actor));StatusText.Text="Đã duyệt bằng định danh phiên và ghi nhật ký.";Sync();},out error);
    private void Sync(){if(transaction is null)try{DuLieuGiaoDien.RefreshFromDatabase();}catch(InvalidOperationException){StatusText.Text+=" Đã lưu dữ liệu; tải lại khi kết nối ổn định.";}}
    private void Drug_Changed(object sender,SelectionChangedEventArgs e){if(loading)return;if(dirty){loading=true;DrugChoice.SelectedValue=selectedDrug;loading=false;ErrorText.Text="Lưu hoặc tải lại trước khi đổi thuốc.";return;}var id=DrugChoice.SelectedValue as string;if(!Run(()=>{var data=id is null?null:service.LayHuongDan(id,actor);selectedDrug=id;Fill(data);},out _)){loading=true;DrugChoice.SelectedValue=selectedDrug;loading=false;}}
    private void SearchDrugs(){var choices=service.TimThuoc(new(TuKhoa:DrugQuery.Text),actor).Select(d=>new LuaChonDanhMuc(d.MaThuoc,$"{d.TenThuoc} · {d.HamLuong} ({d.MaThuoc})")).ToArray();var keep=choices.Any(c=>c.Value==selectedDrug)?selectedDrug:null;var prior=loading;loading=true;try{DrugChoice.ItemsSource=choices;selectedDrug=keep;DrugChoice.SelectedValue=keep;Fill(keep is null?null:service.LayHuongDan(keep,actor));}finally{loading=prior;}}
    private void FindDrug_Click(object sender,RoutedEventArgs e){if(dirty){ErrorText.Text="Lưu hoặc tải lại trước khi thay danh sách thuốc.";return;}Run(SearchDrugs,out _);}
    private void Field_Changed(object sender,RoutedEventArgs e){if(!loading)dirty=true;}
    private void Input_Changed(object sender,TextChangedEventArgs e){if(!loading)dirty=true;}
    private void Save_Click(object sender,RoutedEventArgs e)=>TrySave(out _);
    private void Approve_Click(object sender,RoutedEventArgs e){if(dirty){TryApprove(out _);return;}if(MessageBox.Show("Đã kiểm tra và duyệt nội dung hướng dẫn này?","Duyệt chuyên môn",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)TryApprove(out _);}
    private void NextVersion_Click(object sender,RoutedEventArgs e){if(saved is null||dirty)return;nextVersion=true;dirty=true;ReviewText.Text=$"Soạn v{saved.CauHinh.PhienBan+1} — cần lưu và duyệt lại; bản trước giữ trong nhật ký";Rights();}
    private void Reload_Click(object sender,RoutedEventArgs e)=>Run(()=>{GroupName.Clear();loading=true;try{DrugChoice.SelectedValue=selectedDrug;Fill(selectedDrug is string id?service.LayHuongDan(id,actor):null);}finally{loading=false;}},out _);
    private void AddGroup_Click(object sender,RoutedEventArgs e)=>Run(()=>{if(string.IsNullOrWhiteSpace(GroupName.Text))throw new LoiKhamException("TenNhom","Nhập tên nhóm.");var g=new ET_NHOMTHUOCKEDON{TenNhom=GroupName.Text,TrangThai="Đang áp dụng"};new BUS_NHOMTHUOCKEDON(transaction).LuuNhap(g,actor);var selected=GroupChoice.SelectedValue;loading=true;try{Groups();GroupChoice.SelectedValue=selected;}finally{loading=false;}if(InputPanel.IsEnabled)GroupChoice.SelectedValue=g.MaNhomThuoc;GroupName.Clear();StatusText.Text="Đã lưu nhóm kê đơn.";Sync();},out _);
    private void Back_Click(object sender,RoutedEventArgs e){if(CoThayDoiChuaLuu){ErrorText.Text="Lưu mẫu/nhóm hoặc tải lại để bỏ thay đổi trước khi quay lại.";return;}BackRequested?.Invoke();}
}
