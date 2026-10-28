using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;
namespace RangHamMat.Views;
public sealed class LuaChonDichVuDieuTri
{
 public DichVuTinhTrang DichVu {get;init;}=new();
 public bool Chon {get;set;}
 public string GiaHienThi=>$"{DichVu.DonGia:N0} đ";
}
public partial class KhamLamSang
{
 private IReadOnlyList<ET_DMTINHTRANGBENH> referenceConditions=Array.Empty<ET_DMTINHTRANGBENH>();
 private IReadOnlyList<DichVuTinhTrang> relatedServices=Array.Empty<DichVuTinhTrang>();
 private IReadOnlyList<ET_DICHVU> serviceCatalog=Array.Empty<ET_DICHVU>();
 private ET_DMTINHTRANGBENH? recommendation;
 private string? shownReference,planSourceCondition;
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
 private void UpdateServiceSelection()
 {
  var rows=RelatedServicesList.Items.OfType<LuaChonDichVuDieuTri>().Where(x=>x.Chon).ToArray();
  SelectedServicesSummary.Text=rows.Length==0?"Chưa chọn dịch vụ":$"{rows.Length} dịch vụ · {rows.Sum(x=>x.DichVu.DonGia):N0} đ dự kiến";
 }
 private void ServiceChoice_Click(object sender,RoutedEventArgs e)=>UpdateServiceSelection();
 private void Reference_Selected(object sender,SelectionChangedEventArgs e)
 {
  if(loading||ConditionReferencePicker.SelectedItem is not ET_DMTINHTRANGBENH r)return;
  if(conditionInputDirty){loading=true;try{ConditionReferencePicker.SelectedItem=DinhHuongDieuTri.Tim(ConditionNameEditor.Text,referenceConditions);}finally{loading=false;}ClinicalError.Text="Lưu hoặc Bỏ nhập trước khi chọn tình trạng khác.";return;}
  ConditionNameEditor.Text=r.TenTinhTrang;
 }
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
 private void UpdatePlanTotal()=>PlanTotal.Text=$"{steps.Count} dịch vụ · Tổng dự kiến {steps.Sum(x=>x.SoLuongDuKien*x.DonGiaDuKien):N0} đ";
 private void PlanSteps_SizeChanged(object sender,SizeChangedEventArgs e)
 {
  if(PlanSteps.Columns.Count<5)return;
  var wide=PlanSteps.ActualWidth>=720;
  PlanSteps.Columns[4].Visibility=wide?Visibility.Visible:Visibility.Collapsed;
  PlanSteps.Columns[4].Width=new DataGridLength(240);
  PlanSteps.Columns[0].Width=new DataGridLength(Math.Max(120,PlanSteps.ActualWidth-20-52-52-105-(wide?240:0)));
 }
 private void PlanRowNote_Changed(object sender,TextChangedEventArgs e)
 {if(!loading&&Editable&&sender is TextBox box&&box.IsKeyboardFocusWithin)planDirty=true;}
 private void ApplyCatalog(DuLieuKhongGianKham data)
 {
  serviceCatalog=data.DichVu;relatedServices=data.DichVuTheoTinhTrang;referenceConditions=data.DinhHuong;
  PlanServiceColumn.ItemsSource=serviceCatalog.Select(x=>new RangHamMat.Controls.LuaChonDanhMuc(x.MaDV,x.TenDV+(x.TrangThai=="Đang hoạt động"?"":" · ngừng hoạt động"))).ToArray();
 }
 private void RefreshPlanPrices_Click(object sender,RoutedEventArgs e)=>Run(()=>
 {
  RequireEditable();if(plan is not null&&plan.KeHoach.TrangThai!="Nháp")throw new LoiKhamException("KeHoach","Chỉ cập nhật giá kế hoạch nháp.");
  RangHamMat.Controls.ClinicalWorkflow.ClinicalFormInput.Commit(PlanSteps,"CacBuoc");ApplyCatalog(service.LayKhongGianKham(visitId,actor));
  foreach(var row in steps){var current=serviceCatalog.FirstOrDefault(x=>x.MaDV==row.MaDV&&x.TrangThai=="Đang hoạt động");if(current is null)throw new LoiKhamException("CacBuoc",$"Dịch vụ {row.MaDV} không còn hoạt động. Chọn dịch vụ khác hoặc xóa dòng.");}
  foreach(var row in steps)row.DonGiaDuKien=serviceCatalog.Single(x=>x.MaDV==row.MaDV).DonGia;
  planDirty=true;UpdatePlanTotal();RefreshRecommendation();ClinicalStatus.Text="Đã cập nhật giá danh mục, giữ nguyên nội dung và số lượng đang soạn. Lưu kế hoạch để ghi nhận.";
 },out _);
 private void PlanService_Selected(object sender,SelectionChangedEventArgs e)=>Run(()=>
 {
  if(loading||sender is not ComboBox box||!box.IsKeyboardFocusWithin||box.DataContext is not BuocKeHoachNhap row||box.SelectedValue is not string id)return;
  RequireEditable();serviceCatalog=service.LayKhongGianKham(visitId,actor).DichVu;
  var item=serviceCatalog.FirstOrDefault(x=>x.MaDV==id&&x.TrangThai=="Đang hoạt động")??throw new LoiKhamException("CacBuoc","Dịch vụ không còn hoạt động. Chọn lại từ danh mục mới.");
  row.MaDV=id;row.TenDV=item.TenDV;row.DonGiaDuKien=item.DonGia;planDirty=true;if(string.IsNullOrWhiteSpace(row.NoiDungBuoc))row.NoiDungBuoc=item.TenDV;
  UpdatePlanTotal();
 },out _);
}
