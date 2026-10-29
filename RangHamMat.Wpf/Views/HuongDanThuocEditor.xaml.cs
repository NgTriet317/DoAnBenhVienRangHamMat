using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
using RangHamMat.Controls.ClinicalWorkflow;
using RangHamMat.Controls;
namespace RangHamMat.Views;
/// <summary>
/// Phần xử lý hướng dẫn thuốc trình nhập liệu của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class HuongDanThuocEditor:UserControl
{
    /// <summary>
    /// Đối tượng nghiệp vụ đơn thuốc được màn hình dùng để đọc/ghi dữ liệu.
    /// </summary>
    private readonly BUS_DONTHUOC service;
    /// <summary>
    /// Giao dịch được giữ để các thao tác liên quan dùng cùng một phạm vi commit/rollback.
    /// </summary>
    private readonly IDbTransaction? transaction;
    /// <summary>
    /// Ngữ cảnh định danh và quyền của người dùng đang thao tác.
    /// </summary>
    private readonly NguCanhNghiepVu actor;
    /// <summary>
    /// Đã lưu được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private BanNhapHuongDanThuoc? saved;
    /// <summary>
    /// Đang chọn thuốc được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private string? selectedDrug;
    /// <summary>
    /// Cờ Cờ đang điền dữ liệu, dùng tránh xử lý lại sự kiện phát sinh do cập nhật giao diện, Cờ có nội dung thay đổi chưa lưu, Tiếp phiên bản; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool loading=true,dirty,nextVersion;
    /// <summary>
    /// Giá trị Có thay đổi chưa lưu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public bool CoThayDoiChuaLuu=>dirty||GroupName.Text.Length>0;
    /// <summary>
    /// Thông báo quay lại được yêu cầu cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    public event Action? BackRequested;
    /// <summary>
    /// Khởi tạo hướng dẫn thuốc trình nhập liệu. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    public HuongDanThuocEditor(NguCanhNghiepVu? user=null,IDbTransaction? transaction=null)
    {this.transaction=transaction;service=new(transaction);actor=new BUS_QUYENNGHIEPVU(transaction).LayNguCanh(user?.MaNV??PhienDangNhap.MaNV);if(!actor.LaAdmin&&!actor.CoQuyen(QuyenLamViec.CauHinhHoTro))throw new TuChoiQuyenNghiepVuException();InitializeComponent();Groups();SearchDrugs();loading=false;}
    /// <summary>
    /// Đọc các nhóm thuốc phục vụ lựa chọn cấu hình hướng dẫn.
    /// </summary>
    private void Groups(){GroupChoice.ItemsSource=new[]{new LuaChonDanhMuc("","Chưa xếp nhóm")}.Concat(new BUS_NHOMTHUOCKEDON(transaction).LayDanhSach(actor).Where(g=>g.TrangThai=="Đang áp dụng").Select(g=>new LuaChonDanhMuc(g.MaNhomThuoc,g.TenNhom))).ToArray();}
    /// <summary>
    /// Điền dữ liệu đã đọc vào các trường của biểu mẫu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Rights</c>.</remarks>
    /// <param name="s">Giá trị s dùng khi thực hiện thao tác; kiểu BanNhapHuongDanThuoc?.</param>
    private void Fill(BanNhapHuongDanThuoc? s)
    {var prior=loading;loading=true;try{saved=s;nextVersion=false;GroupChoice.SelectedValue=s?.CauHinh.MaNhomThuoc??"";PopulationEditor.Text=s?.CauHinh.DoiTuongApDung??"";RouteEditor.Text=s?.CauHinh.DuongDung??"";DoseEditor.Text=s?.CauHinh.LieuDungGoiY??"";InstructionEditor.Text=s?.CauHinh.HuongDanGoiY??"";ReviewText.Text=s is null?"Hướng dẫn mới v1 — chưa duyệt":$"v{s.CauHinh.PhienBan} · {s.CauHinh.TrangThai} · người duyệt {s.CauHinh.MaNguoiDuyet??"chưa có"} · {s.CauHinh.NgayDuyet:dd/MM/yyyy HH:mm}";dirty=false;Rights();}finally{loading=prior;}}
    /// <summary>
    /// Cập nhật quyền chỉnh sửa/duyệt của màn hình cấu hình theo dữ liệu đang mở.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>actor.CoQuyen</c>.</remarks>
    private void Rights(){var edit=saved is null||saved.CauHinh.TrangThai=="Nháp"||nextVersion;InputPanel.IsEnabled=SaveButton.IsEnabled=edit&&DrugChoice.SelectedValue is string;ApproveButton.IsEnabled=edit&&!nextVersion&&saved is not null&&actor.CoQuyen(QuyenLamViec.DuyetHuongDanThuoc);NextVersionButton.IsEnabled=saved is not null&&saved.CauHinh.TrangThai!="Nháp"&&!nextVersion;}
    /// <summary>
    /// Thực thi thao tác được truyền vào và xử lý phản hồi theo quy tắc của màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ClinicalFormInput.ClearErrors</c>, <c>action</c>, <c>ClinicalFormInput.Message</c>, <c>ClinicalFormInput.ShowError</c>.</remarks>
    /// <param name="action">Giá trị action dùng khi thực hiện thao tác; kiểu Action.</param>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    private bool Run(Action action,out string error){error="";ErrorText.Text="";ClinicalFormInput.ClearErrors(this);try{action();return true;}catch(InvalidOperationException ex){error=ErrorText.Text=ClinicalFormInput.Message(ex);ClinicalFormInput.ShowError(this,ex);return false;}}
    /// <summary>
    /// Kiểm tra và thử lưu nội dung hiện tại; cập nhật trạng thái giao diện theo kết quả.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>Fill</c>, <c>service.LuuHuongDan</c>, <c>Sync</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool TrySave(out string error)=>Run(()=>{if(DrugChoice.SelectedValue is not string id)throw new LoiKhamException("MaThuoc","Chọn thuốc.");var c=new ET_CAUHINHTHUOCKEDON{MaThuoc=id,MaNhomThuoc=GroupChoice.SelectedValue is string g&&g.Length>0?g:null,DoiTuongApDung=PopulationEditor.Text,DuongDung=RouteEditor.Text,LieuDungGoiY=DoseEditor.Text,HuongDanGoiY=InstructionEditor.Text,TrangThai="Nháp",PhienBan=saved is null?1:saved.CauHinh.PhienBan+(nextVersion?1:0)};Fill(service.LuuHuongDan(c,saved?.DauVanTay,actor));StatusText.Text="Đã lưu nháp; chưa được áp hướng dẫn.";Sync();},out error);
    /// <summary>
    /// Kiểm tra và thử duyệt phiên bản hướng dẫn thuốc đang hiển thị.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>Fill</c>, <c>service.DuyetHuongDan</c>, <c>Sync</c>.</remarks>
    /// <param name="error">Giá trị lỗi dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool TryApprove(out string error)=>Run(()=>{if(saved is null||dirty||nextVersion)throw new LoiKhamException("HuongDan","Lưu nháp trước khi duyệt.");Fill(service.DuyetHuongDan(saved.CauHinh.MaThuoc,saved.DauVanTay,actor));StatusText.Text="Đã duyệt bằng định danh phiên và ghi nhật ký.";Sync();},out error);
    /// <summary>
    /// Đồng bộ dữ liệu đang nhập với mô hình được màn hình theo dõi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DuLieuGiaoDien.RefreshFromDatabase</c>.</remarks>
    private void Sync(){if(transaction is null)try{DuLieuGiaoDien.RefreshFromDatabase();}catch(InvalidOperationException){StatusText.Text+=" Đã lưu dữ liệu; tải lại khi kết nối ổn định.";}}
    /// <summary>
    /// Tiếp nhận thay đổi thuốc và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>service.LayHuongDan</c>, <c>Fill</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Drug_Changed(object sender,SelectionChangedEventArgs e){if(loading)return;if(dirty){loading=true;DrugChoice.SelectedValue=selectedDrug;loading=false;ErrorText.Text="Lưu hoặc tải lại trước khi đổi thuốc.";return;}var id=DrugChoice.SelectedValue as string;if(!Run(()=>{var data=id is null?null:service.LayHuongDan(id,actor);selectedDrug=id;Fill(data);},out _)){loading=true;DrugChoice.SelectedValue=selectedDrug;loading=false;}}
    /// <summary>
    /// Tra danh mục thuốc theo nội dung tìm trên màn hình cấu hình hướng dẫn.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>service.TimThuoc</c>, <c>choices.Any</c>, <c>Fill</c>, <c>service.LayHuongDan</c>.</remarks>
    private void SearchDrugs(){var choices=service.TimThuoc(new(TuKhoa:DrugQuery.Text),actor).Select(d=>new LuaChonDanhMuc(d.MaThuoc,$"{d.TenThuoc} · {d.HamLuong} ({d.MaThuoc})")).ToArray();var keep=choices.Any(c=>c.Value==selectedDrug)?selectedDrug:null;var prior=loading;loading=true;try{DrugChoice.ItemsSource=choices;selectedDrug=keep;DrugChoice.SelectedValue=keep;Fill(keep is null?null:service.LayHuongDan(keep,actor));}finally{loading=prior;}}
    /// <summary>
    /// Xử lý thao tác tìm thuốc khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void FindDrug_Click(object sender,RoutedEventArgs e){if(dirty){ErrorText.Text="Lưu hoặc tải lại trước khi thay danh sách thuốc.";return;}Run(SearchDrugs,out _);}
    /// <summary>
    /// Tiếp nhận thay đổi trường và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Field_Changed(object sender,RoutedEventArgs e){if(!loading)dirty=true;}
    /// <summary>
    /// Tiếp nhận thay đổi đầu vào và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Input_Changed(object sender,TextChangedEventArgs e){if(!loading)dirty=true;}
    /// <summary>
    /// Xử lý thao tác lưu khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TrySave</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Save_Click(object sender,RoutedEventArgs e)=>TrySave(out _);
    /// <summary>
    /// Xử lý thao tác duyệt khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TryApprove</c>, <c>MessageBox.Show</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Approve_Click(object sender,RoutedEventArgs e){if(dirty){TryApprove(out _);return;}if(MessageBox.Show("Đã kiểm tra và duyệt nội dung hướng dẫn này?","Duyệt chuyên môn",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)TryApprove(out _);}
    /// <summary>
    /// Xử lý thao tác tiếp phiên bản khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Rights</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void NextVersion_Click(object sender,RoutedEventArgs e){if(saved is null||dirty)return;nextVersion=true;dirty=true;ReviewText.Text=$"Soạn v{saved.CauHinh.PhienBan+1} — cần lưu và duyệt lại; bản trước giữ trong nhật ký";Rights();}
    /// <summary>
    /// Xử lý thao tác tải lại khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>GroupName.Clear</c>, <c>Fill</c>, <c>service.LayHuongDan</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Reload_Click(object sender,RoutedEventArgs e)=>Run(()=>{GroupName.Clear();loading=true;try{DrugChoice.SelectedValue=selectedDrug;Fill(selectedDrug is string id?service.LayHuongDan(id,actor):null);}finally{loading=false;}},out _);
    /// <summary>
    /// Xử lý thao tác thêm nhóm khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>Groups</c>, <c>GroupName.Clear</c>, <c>Sync</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void AddGroup_Click(object sender,RoutedEventArgs e)=>Run(()=>{if(string.IsNullOrWhiteSpace(GroupName.Text))throw new LoiKhamException("TenNhom","Nhập tên nhóm.");var g=new ET_NHOMTHUOCKEDON{TenNhom=GroupName.Text,TrangThai="Đang áp dụng"};new BUS_NHOMTHUOCKEDON(transaction).LuuNhap(g,actor);var selected=GroupChoice.SelectedValue;loading=true;try{Groups();GroupChoice.SelectedValue=selected;}finally{loading=false;}if(InputPanel.IsEnabled)GroupChoice.SelectedValue=g.MaNhomThuoc;GroupName.Clear();StatusText.Text="Đã lưu nhóm kê đơn.";Sync();},out _);
    /// <summary>
    /// Xử lý thao tác quay lại khi người dùng bấm nút tương ứng trên màn hình.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>.Invoke</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Back_Click(object sender,RoutedEventArgs e){if(CoThayDoiChuaLuu){ErrorText.Text="Lưu mẫu/nhóm hoặc tải lại để bỏ thay đổi trước khi quay lại.";return;}BackRequested?.Invoke();}
}
