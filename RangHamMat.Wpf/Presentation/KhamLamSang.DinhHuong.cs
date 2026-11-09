using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
namespace RangHamMat.Views;
/// <summary>
/// Phần xử lý lựa chọn dịch vụ điều trị của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public sealed class LuaChonDichVuDieuTri
{
 /// <summary>
 /// Dịch vụ.
 /// </summary>
 public DichVuTinhTrang DichVu {get;init;}=new();
 /// <summary>
 /// Chọn.
 /// </summary>
 public bool Chon {get;set;}
 /// <summary>
 /// Giá trị Giá hiển thị; được xác định bởi phần getter/setter của thuộc tính.
 /// </summary>
 public string GiaHienThi=>$"{DichVu.DonGia:N0} đ";
}
/// <summary>
/// Phần xử lý khám lâm sàng của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
/// </summary>
public partial class KhamLamSang
{
 /// <summary>
 /// Tham khảo các tình trạng được giữ trong phạm vi lớp để các thao tác dùng lại.
 /// </summary>
 private IReadOnlyList<ET_DMTINHTRANGBENH> referenceConditions=Array.Empty<ET_DMTINHTRANGBENH>();
 /// <summary>
 /// Liên quan các dịch vụ được giữ trong phạm vi lớp để các thao tác dùng lại.
 /// </summary>
 private IReadOnlyList<DichVuTinhTrang> relatedServices=Array.Empty<DichVuTinhTrang>();
 /// <summary>
 /// Dịch vụ danh mục được giữ trong phạm vi lớp để các thao tác dùng lại.
 /// </summary>
 private IReadOnlyList<ET_DICHVU> serviceCatalog=Array.Empty<ET_DICHVU>();
 /// <summary>
 /// Đề xuất được giữ trong phạm vi lớp để các thao tác dùng lại.
 /// </summary>
 private ET_DMTINHTRANGBENH? recommendation;
 /// <summary>
 /// Thiết lập nguồn chọn tình trạng và các thao tác nhập tình trạng trong lần khám.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>referenceConditions.ToArray</c>, <c>view.GroupDescriptions.Add</c>, <c>view.SortDescriptions.Add</c>.</remarks>
 private void ConfigureConditionPicker()
 {
  var view=new System.Windows.Data.ListCollectionView(referenceConditions.ToArray());
  view.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription(nameof(ET_DMTINHTRANGBENH.Nhom)));
  view.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(ET_DMTINHTRANGBENH.Nhom),System.ComponentModel.ListSortDirection.Ascending));
  view.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(ET_DMTINHTRANGBENH.TenTinhTrang),System.ComponentModel.ListSortDirection.Ascending));
  ConditionReferencePicker.ItemsSource=view;
 }
 /// <summary>
 /// Đang hiển thị tham khảo kế hoạch nguồn tình trạng được giữ trong phạm vi lớp để các thao tác dùng lại.
 /// </summary>
 private string? shownReference,planSourceCondition;
 /// <summary>
 /// Cập nhật gợi ý dịch vụ theo tình trạng đang chọn từ danh mục liên quan.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>ConditionNameEditor.Text.Trim</c>, <c>conditions.LastOrDefault</c>, <c>DinhHuongDieuTri.Tim</c>, <c>RelatedServicesList.Items.OfType&lt;LuaChonDichVuDieuTri&gt;</c>.</remarks>
 private void RefreshRecommendation()
 {
  if(ConditionRecommendation is null)return;
  var name=ConditionNameEditor.Text.Trim();if(name.Length==0)name=conditions.LastOrDefault()?.LoaiTinhTrang??"";
  recommendation=DinhHuongDieuTri.Tim(name,referenceConditions);
  var selected=shownReference==recommendation?.MaDMTT?RelatedServicesList.Items.OfType<LuaChonDichVuDieuTri>().Where(x=>x.Chon).Select(x=>x.DichVu.MaDV).ToHashSet():new HashSet<string>();
  shownReference=recommendation?.MaDMTT;
  RelatedServicesList.ItemsSource=relatedServices.Where(x=>x.MaDMTT==shownReference).Select(x=>new LuaChonDichVuDieuTri{DichVu=x,Chon=selected.Contains(x.MaDV)}).ToArray();
  ConditionRecommendation.Text=recommendation is null?"Chọn tình trạng để xem dịch vụ liên quan.":$"Dịch vụ cho {recommendation.TenTinhTrang.ToLowerInvariant()}";
  ConditionReferenceText.Text=recommendation?.MoTa??"Tình trạng ngoài danh mục: bác sĩ có thể lập kế hoạch trực tiếp từ danh mục dịch vụ.";
  RelatedServicesList.IsEnabled=snapshot is not null&&Editable;
  UseRecommendationButton.IsEnabled=snapshot is not null&&Editable&&RelatedServicesList.Items.Count>0;
  UpdateServiceSelection();
 }
 /// <summary>
 /// Cập nhật lựa chọn dịch vụ và các nút khi người dùng đổi các mục trong danh sách đề xuất.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>RelatedServicesList.Items.OfType&lt;LuaChonDichVuDieuTri&gt;</c>, <c>rows.Sum</c>.</remarks>
 private void UpdateServiceSelection()
 {
  var rows=RelatedServicesList.Items.OfType<LuaChonDichVuDieuTri>().Where(x=>x.Chon).ToArray();
  SelectedServicesSummary.Text=rows.Length==0?"Chưa chọn dịch vụ":$"{rows.Length} dịch vụ · {rows.Sum(x=>x.DichVu.DonGia):N0} đ dự kiến";
 }
 /// <summary>
 /// Xử lý thao tác dịch vụ lựa chọn khi người dùng bấm nút tương ứng trên màn hình.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>UpdateServiceSelection</c>.</remarks>
 /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
 /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
 private void ServiceChoice_Click(object sender,RoutedEventArgs e)=>UpdateServiceSelection();
 /// <summary>
 /// Phản ánh lựa chọn tham khảo vào dữ liệu và phần hiển thị liên quan.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>DinhHuongDieuTri.Tim</c>.</remarks>
 /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
 /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
 private void Reference_Selected(object sender,SelectionChangedEventArgs e)
 {
  if(loading||ConditionReferencePicker.SelectedItem is not ET_DMTINHTRANGBENH r)return;
  if(conditionInputDirty){loading=true;try{ConditionReferencePicker.SelectedItem=DinhHuongDieuTri.Tim(ConditionNameEditor.Text,referenceConditions);}finally{loading=false;}ClinicalError.Text="Lưu hoặc Bỏ nhập trước khi chọn tình trạng khác.";return;}
  ConditionNameEditor.Text=r.TenTinhTrang;
 }
 /// <summary>
 /// Xử lý thao tác sử dụng đề xuất khi người dùng bấm nút tương ứng trên màn hình.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>RequireEditable</c>, <c>RelatedServicesList.Items.OfType&lt;LuaChonDichVuDieuTri&gt;</c>, <c>service.LayKhongGianKham</c>.</remarks>
 /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
 /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
 private void UseRecommendation_Click(object sender,RoutedEventArgs e)=>Run(()=>
 {
  RequireEditable();if(planDirty)throw new LoiKhamException("KeHoach","Lưu kế hoạch đang nhập hoặc Tải lại trước khi lập kế hoạch khác.");
  var source=recommendation??throw new LoiKhamException("TinhTrang","Chọn tình trạng cần lập kế hoạch.");
  var chosen=RelatedServicesList.Items.OfType<LuaChonDichVuDieuTri>().Where(x=>x.Chon).Select(x=>x.DichVu).ToArray();
  if(chosen.Length==0)throw new LoiKhamException("KeHoach","Chọn ít nhất một dịch vụ liên quan.");
  var fresh=service.LayKhongGianKham(visitId,actor);
  foreach(var row in chosen)if(!fresh.DichVuTheoTinhTrang.Any(x=>x.MaDMTT==source.MaDMTT&&x.MaDV==row.MaDV&&x.DonGia==row.DonGia))
  {ApplyCatalog(fresh);RefreshRecommendation();throw new LoiKhamException("KeHoach","Danh mục hoặc giá đã thay đổi. Kiểm tra danh sách dịch vụ vừa cập nhật.");}
  var sourceId=(selectedCondition??(string.IsNullOrWhiteSpace(ConditionNameEditor.Text)?conditions.LastOrDefault():null))?.MaTTB;
  if(dirty||conditionInputDirty){if(!TrySave(out var error))throw new LoiKhamException("TinhTrang",error);}
  var matches=snapshot.TinhTrang.Where(x=>DinhHuongDieuTri.Tim(x.LoaiTinhTrang,referenceConditions)?.MaDMTT==source.MaDMTT).ToArray();
  var condition=!string.IsNullOrEmpty(sourceId)?matches.SingleOrDefault(x=>x.MaTTB==sourceId):matches.Length==1?matches[0]:null;
  if(condition is null)throw new LoiKhamException("TinhTrang","Chọn đúng dòng tình trạng đã lưu trong danh sách trước khi lập kế hoạch (có thể có nhiều dòng cùng tên).");
  var before=loading;loading=true;
  try{ClearPlan();planSourceCondition=condition.MaTTB;PlanNote.Text="Điều trị theo tình trạng: "+condition.LoaiTinhTrang;foreach(var row in chosen)steps.Add(new(){MaDV=row.MaDV,TenDV=row.TenDV,DonGiaDuKien=row.DonGia,SoLuongDuKien=1,LanThu=1,NoiDungBuoc=row.NoiDungDeXuat});}
  finally{loading=before;}
  planDirty=true;ClinicalTabs.SelectedIndex=2;UpdatePlanTotal();ClinicalStatus.Text="Đã lập kế hoạch từ dịch vụ đã chọn. Kiểm tra số lượng và buổi rồi lưu kế hoạch.";
 },out _);
 /// <summary>
 /// Cộng lại các dòng kế hoạch và cập nhật tổng tiền dự kiến trên màn hình.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>steps.Sum</c>.</remarks>
 private void UpdatePlanTotal()=>PlanTotal.Text=$"{steps.Count} dịch vụ · Tổng dự kiến {steps.Sum(x=>x.SoLuongDuKien*x.DonGiaDuKien):N0} đ";
 /// <summary>
 /// Điều chỉnh phần hiển thị theo kích thước thực tế của kế hoạch các bước.
 /// </summary>
 /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
 /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
 private void PlanSteps_SizeChanged(object sender,SizeChangedEventArgs e)
 {
  if(PlanSteps.Columns.Count<5)return;
  var wide=PlanSteps.ActualWidth>=720;
  PlanSteps.Columns[4].Visibility=wide?Visibility.Visible:Visibility.Collapsed;
  PlanSteps.Columns[4].Width=new DataGridLength(240);
  PlanSteps.Columns[0].Width=new DataGridLength(Math.Max(120,PlanSteps.ActualWidth-20-52-52-105-(wide?240:0)));
 }
 /// <summary>
 /// Tiếp nhận thay đổi kế hoạch dòng ghi chú và cập nhật trạng thái/dữ liệu phụ thuộc.
 /// </summary>
 /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
 /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
 private void PlanRowNote_Changed(object sender,TextChangedEventArgs e)
 {if(!loading&&Editable&&sender is TextBox box&&box.IsKeyboardFocusWithin)planDirty=true;}
 /// <summary>
 /// Áp dữ liệu dịch vụ đang chọn từ danh mục vào các dòng kế hoạch đang soạn.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>serviceCatalog.Select</c>.</remarks>
 /// <param name="data">Giá trị dữ liệu dùng khi thực hiện thao tác; kiểu DuLieuKhongGianKham.</param>
 private void ApplyCatalog(DuLieuKhongGianKham data)
 {
  serviceCatalog=data.DichVu;relatedServices=data.DichVuTheoTinhTrang;referenceConditions=data.DinhHuong;
  PlanServiceColumn.ItemsSource=serviceCatalog.Select(x=>new RangHamMat.Controls.LuaChonDanhMuc(x.MaDV,x.TenDV+(x.TrangThai=="Đang hoạt động"?"":" · ngừng hoạt động"))).ToArray();
 }
 /// <summary>
 /// Xử lý thao tác làm mới kế hoạch các mức giá khi người dùng bấm nút tương ứng trên màn hình.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>RequireEditable</c>, <c>RangHamMat.Controls.ClinicalWorkflow.ClinicalFormInput.Commit</c>, <c>ApplyCatalog</c>.</remarks>
 /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
 /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
 private void RefreshPlanPrices_Click(object sender,RoutedEventArgs e)=>Run(()=>
 {
  RequireEditable();if(plan is not null&&plan.KeHoach.TrangThai!="Nháp")throw new LoiKhamException("KeHoach","Chỉ cập nhật giá kế hoạch nháp.");
  RangHamMat.Controls.ClinicalWorkflow.ClinicalFormInput.Commit(PlanSteps,"CacBuoc");ApplyCatalog(service.LayKhongGianKham(visitId,actor));
  foreach(var row in steps){var current=serviceCatalog.FirstOrDefault(x=>x.MaDV==row.MaDV&&x.TrangThai=="Đang hoạt động");if(current is null)throw new LoiKhamException("CacBuoc",$"Dịch vụ {row.MaDV} không còn hoạt động. Chọn dịch vụ khác hoặc xóa dòng.");}
  foreach(var row in steps)row.DonGiaDuKien=serviceCatalog.Single(x=>x.MaDV==row.MaDV).DonGia;
  planDirty=true;UpdatePlanTotal();RefreshRecommendation();ClinicalStatus.Text="Đã cập nhật giá danh mục, giữ nguyên nội dung và số lượng đang soạn. Lưu kế hoạch để ghi nhận.";
 },out _);
 /// <summary>
 /// Phản ánh lựa chọn kế hoạch dịch vụ vào dữ liệu và phần hiển thị liên quan.
 /// </summary>
 /// <remarks>Các bước được phối hợp qua <c>Run</c>, <c>RequireEditable</c>, <c>service.LayKhongGianKham</c>, <c>serviceCatalog.FirstOrDefault</c>.</remarks>
 /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
 /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
 private void PlanService_Selected(object sender,SelectionChangedEventArgs e)=>Run(()=>
 {
  if(loading||sender is not ComboBox box||!box.IsKeyboardFocusWithin||box.DataContext is not BuocKeHoachNhap row||box.SelectedValue is not string id)return;
  RequireEditable();serviceCatalog=service.LayKhongGianKham(visitId,actor).DichVu;
  var item=serviceCatalog.FirstOrDefault(x=>x.MaDV==id&&x.TrangThai=="Đang hoạt động")??throw new LoiKhamException("CacBuoc","Dịch vụ không còn hoạt động. Chọn lại từ danh mục mới.");
  row.MaDV=id;row.TenDV=item.TenDV;row.DonGiaDuKien=item.DonGia;planDirty=true;if(string.IsNullOrWhiteSpace(row.NoiDungBuoc))row.NoiDungBuoc=item.TenDV;
  UpdatePlanTotal();
 },out _);
}
