using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
public partial class CuaSoKhamDieuTri : UserControl, IClinicalEditor
{
    private readonly ObservableCollection<TreatmentDraft> planRows=new(), progressRows=new();
    private TreatmentServiceChoice[] services=Array.Empty<TreatmentServiceChoice>();
    private string initializedTable="";
    public CuaSoKhamDieuTri()
    {
        InitializeComponent(); PlanDrafts.ItemsSource=planRows;ProgressDrafts.ItemsSource=progressRows;
        planRows.CollectionChanged+=(_,_)=>UpdateTotals();progressRows.CollectionChanged+=(_,_)=>UpdateTotals();
    }
    private string Table=>(EditorTabs.SelectedItem as TabItem)?.Tag?.ToString()?.Replace("Table:","")??"LICH_SU_KHAM_BENH";
    private ObservableCollection<TreatmentDraft> Drafts=>Table=="KE_HOACH_DIEU_TRI"?planRows:progressRows;
    private void Form_Loaded(object sender,RoutedEventArgs e) { ClinicalForm.Prepare(this);InitializeDrafts(); }
    private void EditorTabs_SelectionChanged(object sender,SelectionChangedEventArgs e) { if(IsLoaded&&e.Source==EditorTabs)InitializeDrafts(); }
    private void InitializeDrafts()
    {
        EditorTitle.Text=Table switch {"LICH_SU_KHAM_BENH"=>"Thông tin lần khám","TINH_TRANG_BENH"=>"Ghi nhận tình trạng bệnh","KE_HOACH_DIEU_TRI"=>"Kế hoạch điều trị","CHI_TIET_KE_HOACH"=>"Dịch vụ dự kiến",_=>"Dịch vụ thực hiện"};
        if(initializedTable==Table)return;initializedTable=Table;
        if(Table!="DICH_VU_THUC_HIEN")return;
        var existing=ClinicalForm.GetRecord(this);
        if(existing is not null)
        {
            AddDraft(new(){Existing=existing,MaDV=UiRecord.Text(existing,"MaDV"),Label=ClinicalData.Lookup("MaDV",UiRecord.Get(existing,"MaDV")),MaCTKH=UiRecord.Text(existing,"MaCTKH"),MaNVThucHien=UiRecord.Text(existing,"MaNVThucHien"),Quantity=TreatmentMoney.Number(existing,"SoLuong"),Price=TreatmentMoney.Number(existing,"DonGia"),Note=UiRecord.Text(existing,"KetQua"),Status=UiRecord.Text(existing,"TrangThai")});
            return;
        }
        var step=ClinicalData.Find("CHI_TIET_KE_HOACH",ReadField("DICH_VU_THUC_HIEN","MaCTKH"));
        if(step is null)return;
        var plan=ClinicalData.Find("KE_HOACH_DIEU_TRI",UiRecord.Get(step,"MaKHDT"));
        if(plan is not null)
        {
            if (ReadField("DICH_VU_THUC_HIEN","MaLS") is null || string.IsNullOrWhiteSpace(Convert.ToString(ReadField("DICH_VU_THUC_HIEN","MaLS")))) WriteField("DICH_VU_THUC_HIEN","MaLS",UiRecord.Get(plan,"MaLS"));
        }
        AddDraft(new(){MaDV=UiRecord.Text(step,"MaDV"),Label=ClinicalData.Lookup("MaDV",UiRecord.Get(step,"MaDV")),MaCTKH=UiRecord.Text(step,"MaCTKH"),MaNVThucHien=plan is null?null:UiRecord.Text(plan,"MaBS"),Quantity=TreatmentMoney.Number(step,"SoLuongDuKien"),Price=TreatmentMoney.Number(step,"DonGiaDuKien")});
    }
    private void AddDraft(TreatmentDraft draft) { draft.PropertyChanged+=DraftChanged;Drafts.Add(draft);UpdateTotals(); }
    private void DraftChanged(object? sender,PropertyChangedEventArgs e)=>UpdateTotals();
    private void UpdateTotals()
    {
        if(PlanDraftTotal is null)return;
        PlanDraftTotal.Text="Tổng chi phí dự kiến: "+TreatmentMoney.Format(planRows.Sum(row=>row.Amount));
        ProgressDraftTotal.Text="Tổng chi phí thực hiện: "+TreatmentMoney.Format(progressRows.Sum(row=>row.Amount));
    }
    private void DraftCellEditEnding(object sender,DataGridCellEditEndingEventArgs e)=>Dispatcher.BeginInvoke(new Action(UpdateTotals));
    private void RemoveDraft_Click(object sender,RoutedEventArgs e) { if((sender as FrameworkElement)?.DataContext is TreatmentDraft draft){draft.PropertyChanged-=DraftChanged;Drafts.Remove(draft);} }
    private void OpenServices_Click(object sender,RoutedEventArgs e)
    {
        services=ClinicalData.Rows("DICH_VU").Where(row=>UiRecord.Text(row,"TrangThai") is not "Hủy" and not "Ngừng hoạt động").Select(row=>new TreatmentServiceChoice(row)).ToArray();
        ServiceGroups.ItemsSource=new[]{"Tất cả"}.Concat(services.Select(row=>row.Group).Where(group=>!string.IsNullOrWhiteSpace(group)).Distinct()).ToArray();ServiceGroups.SelectedIndex=0;ServiceSearch.Clear();FilterServices();ServiceOverlay.Visibility=Visibility.Visible;
    }
    private void CloseServices_Click(object sender,RoutedEventArgs e)=>ServiceOverlay.Visibility=Visibility.Collapsed;
    private void ServiceSearch_Changed(object sender,TextChangedEventArgs e)=>FilterServices();
    private void ServiceGroup_Changed(object sender,SelectionChangedEventArgs e)=>FilterServices();
    private void FilterServices()
    {
        if(ServiceCatalog is null)return;
        var query=ServiceSearch.Text.Trim();var group=ServiceGroups.SelectedItem as string??"Tất cả";
        ServiceCatalog.ItemsSource=services.Where(row=>(group=="Tất cả"||row.Group==group)&&(query.Length==0||(row.Code+" "+row.Label+" "+row.Description).Contains(query,StringComparison.CurrentCultureIgnoreCase))).ToArray();
        CatalogMessage.Text=services.Length==0?"Chưa có dịch vụ trong danh mục.":"Đánh dấu dịch vụ cần thêm. Có thể chọn nhiều dịch vụ.";
    }
    private void ChooseServices_Click(object sender,RoutedEventArgs e)
    {
        ServiceCatalog.CommitEdit(DataGridEditingUnit.Cell,true);ServiceCatalog.CommitEdit(DataGridEditingUnit.Row,true);
        var selected=services.Where(row=>row.Selected).ToArray();
        if(selected.Length==0){CatalogMessage.Text="Chọn ít nhất một dịch vụ.";return;}
        if(Table=="CHI_TIET_KE_HOACH")
        {
            if(selected.Length!=1){CatalogMessage.Text="Chọn một dịch vụ cho bước kế hoạch này.";return;}
            WriteField(Table,"MaDV",selected[0].Code);WriteField(Table,"SoLuongDuKien",1);WriteField(Table,"LanThu",1);WriteField(Table,"DonGiaDuKien",TreatmentMoney.Number(selected[0].Record,"DonGia"));
        }
        else
        {
            if(ClinicalForm.GetRecord(this)is not null&&Table=="DICH_VU_THUC_HIEN") { CatalogMessage.Text="Bản ghi thực hiện đang chỉnh sửa giữ nguyên dịch vụ. Thêm bản ghi mới để chọn dịch vụ khác.";return; }
            foreach(var service in selected)
            {
                var visit=ClinicalData.Find("LICH_SU_KHAM_BENH",ReadField(Table,"MaLS"));
                AddDraft(new(){MaDV=service.Code,Label=service.Label,Price=TreatmentMoney.Number(service.Record,"DonGia"),LanThu=planRows.Count+1,MaNVThucHien=visit is null?null:UiRecord.Text(visit,"MaBS")});
            }
        }
        ServiceOverlay.Visibility=Visibility.Collapsed;
    }
    public bool Commit(out string error)
    {
        string failure="";
        try {var result=ClinicalData.Transaction(()=>CommitCore(out failure));error=failure;return result;}
        catch(InvalidOperationException ex) {error=ex.Message;return false;}
    }
    private bool CommitCore(out string error)
    {
        ClinicalForm.VerifyUnchanged(this);
        error="";
        var existing=ClinicalForm.GetRecord(this);
        if(existing is not null && (!ClinicalData.CanEdit(Table,existing) || Table=="CHI_TIET_KE_HOACH"&&!new TreatmentRow(Table,existing).CanEdit || Table=="KE_HOACH_DIEU_TRI"&&!new TreatmentPlan(existing).CanEdit)) {error="Thông tin đã xác nhận hoặc đã có điều trị hoàn thành được giữ nguyên.";return false;}
        var activeGrid=Table=="KE_HOACH_DIEU_TRI"?PlanDrafts:Table=="DICH_VU_THUC_HIEN"?ProgressDrafts:null;
        if(activeGrid is not null&&(!activeGrid.CommitEdit(DataGridEditingUnit.Cell,true)||!activeGrid.CommitEdit(DataGridEditingUnit.Row,true))) {error="Kiểm tra số lượng, lần thứ và đơn giá đã nhập.";return false;}
        if(Elements(this).Any(element=>Validation.GetHasError(element))) {error="Kiểm tra số lượng, lần thứ và đơn giá đã nhập.";return false;}
        if(Table=="KE_HOACH_DIEU_TRI")
        {
            if(planRows.Count==0&&ClinicalForm.GetRecord(this)is null){error="Chọn ít nhất một dịch vụ dự kiến.";return false;}
            if(!ValidateDrafts(planRows,false,out error))return false;
            if(ReadField(Table,"NgayBatDau")is DateTime start&&ReadField(Table,"NgayDuKienKetThuc")is DateTime finish&&finish<start){error="Ngày dự kiến kết thúc phải từ ngày bắt đầu trở đi.";return false;}
            if(!ClinicalForm.TrySave(this,out var plan,out error)||plan is null)return false;
            foreach(var row in planRows) ClinicalData.Upsert("CHI_TIET_KE_HOACH",new Dictionary<string,object?>{{"MaKHDT",UiRecord.Get(plan,"MaKHDT")},{"MaDV",row.MaDV},{"LanThu",row.LanThu},{"SoLuongDuKien",row.Quantity},{"DonGiaDuKien",row.Price},{"TrangThai","Nháp"},{"GhiChu",row.Note}});
            return true;
        }
        if(Table=="DICH_VU_THUC_HIEN")
        {
            if(ClinicalData.Find("LICH_SU_KHAM_BENH",ReadField(Table,"MaLS"))is null||ReadField(Table,"ThoiGianThucHien")is not DateTime) {error="Chọn lần khám và ngày điều trị.";return false;}
            if(progressRows.Count==0){error="Chọn ít nhất một dịch vụ thực hiện.";return false;}
            if(!ValidateDrafts(progressRows,true,out error))return false;
            var first=progressRows[0];WriteProgress(first);
            if(!ClinicalForm.TrySave(this,out var saved,out error)||saved is null)return false;
            foreach(var row in progressRows.Skip(1))
            {
                var values=ClinicalData.Values(saved);values.Remove("MaDVThucHien");values["MaDV"]=row.MaDV;values["MaCTKH"]=row.MaCTKH;values["MaNVThucHien"]=row.MaNVThucHien;values["SoLuong"]=row.Quantity;values["DonGia"]=row.Price;values["KetQua"]=row.Note;values["TrangThai"]=row.Status;
                ClinicalData.Upsert("DICH_VU_THUC_HIEN",values);
            }
            return true;
        }
        if(Table=="TINH_TRANG_BENH" && (ClinicalData.Find("LICH_SU_KHAM_BENH",ReadField(Table,"MaLS"))is null || string.IsNullOrWhiteSpace(Convert.ToString(ReadField(Table,"LoaiTinhTrang"))) || string.IsNullOrWhiteSpace(Convert.ToString(ReadField(Table,"MoTaTinhTrang"))))) {error="Chọn lần khám và nhập loại, mô tả tình trạng bệnh.";return false;}
        if(Table=="CHI_TIET_KE_HOACH")
        {
            var values=ClinicalForm.Read(this);
            if(ClinicalData.Find("KE_HOACH_DIEU_TRI",values.GetValueOrDefault("MaKHDT"))is null||ClinicalData.Find("DICH_VU",values.GetValueOrDefault("MaDV"))is null){error="Chọn kế hoạch và dịch vụ dự kiến.";return false;}
            if(!int.TryParse(Convert.ToString(values.GetValueOrDefault("LanThu")),out var sequence)||sequence<=0||!ClinicalRules.TryNonNegativeNumber(Convert.ToString(values.GetValueOrDefault("SoLuongDuKien"))??"",out var quantity)||quantity<=0||!ClinicalRules.TryNonNegativeNumber(Convert.ToString(values.GetValueOrDefault("DonGiaDuKien"))??"",out var price)){error="Lần thứ và số lượng phải lớn hơn 0; đơn giá không được âm. Không nhập phân cách hàng nghìn.";return false;}
            WriteField(Table,"SoLuongDuKien",quantity);WriteField(Table,"DonGiaDuKien",price);WriteField(Table,"LanThu",sequence);
        }
        return ClinicalForm.TrySave(this,out _,out error);
    }
    private static bool ValidateDrafts(IEnumerable<TreatmentDraft> rows,bool actual,out string error)
    {
        error="";
        foreach(var row in rows)
        {
            if(row.Quantity<=0||row.Price<0||row.LanThu<=0){error="Số lượng và lần thứ phải lớn hơn 0; đơn giá không được âm.";return false;}
            if(ClinicalData.Find("DICH_VU",row.MaDV)is null){error="Dịch vụ không còn trong danh mục.";return false;}
            if(actual&&ClinicalData.Find("NHAN_VIEN",row.MaNVThucHien)is null){error="Chọn người thực hiện cho từng dịch vụ.";return false;}
        }
        return true;
    }
    private void WriteProgress(TreatmentDraft row)
    {
        WriteField(Table,"MaDV",row.MaDV);WriteField(Table,"MaCTKH",row.MaCTKH);WriteField(Table,"MaNVThucHien",row.MaNVThucHien);WriteField(Table,"SoLuong",row.Quantity);WriteField(Table,"DonGia",row.Price);WriteField(Table,"KetQua",row.Note);WriteField(Table,"TrangThai",row.Status);
    }
    private FrameworkElement? Field(string table,string field)=>Elements(this).OfType<FrameworkElement>().FirstOrDefault(element=>Equals(element.Tag,$"DB:{table}.{field}"));
    private object? ReadField(string table,string field)
    {
        var element=Field(table,field);
        return element switch {ComboBox box=>box.SelectedValue,TextBox text=>text.Text,DatePicker date=>date.SelectedDate,_=>null};
    }
    private void WriteField(string table,string field,object? value)
    {
        var element=Field(table,field);
        switch(element){case ComboBox box:box.SelectedValue=Convert.ToString(value);break;case TextBox text:text.Text=Convert.ToString(value)??"";break;case DatePicker date:date.SelectedDate=value as DateTime?;break;}
    }
    private static IEnumerable<DependencyObject> Elements(DependencyObject parent)
    {
        yield return parent;
        foreach(var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())foreach(var element in Elements(child))yield return element;
    }
}
