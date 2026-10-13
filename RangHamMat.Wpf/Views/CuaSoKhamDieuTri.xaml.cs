// AI bổ sung kiểm tra nhập liệu: kiểm tra binding, precision từng dòng/tổng, chặn tràn số và giữ form khi thất bại.
// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ form lần khám với nhiều dòng dịch vụ, tính tổng, kiểm tra bản nháp và lưu các dòng trong một luồng; đã chú thích trạng thái tab và đầu vào bắt buộc.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
/// <summary>
/// Màn hình biểu mẫu lần khám/tình trạng/kế hoạch/bước/dịch vụ thực hiện: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class CuaSoKhamDieuTri : UserControl, IBieuMauLuu
{
    private readonly ObservableCollection<TreatmentDraft> planRows=new(), progressRows=new();
    private TreatmentServiceChoice[] services=Array.Empty<TreatmentServiceChoice>();
    private string initializedTable="";
    /// <summary>
    /// Khởi tạo biểu mẫu lần khám/tình trạng/kế hoạch/bước/dịch vụ thực hiện từ XAML.
    /// </summary>
    public CuaSoKhamDieuTri()
    {
        InitializeComponent(); PlanDrafts.ItemsSource=planRows;ProgressDrafts.ItemsSource=progressRows;
        planRows.CollectionChanged+=(_,_)=>UpdateTotals();progressRows.CollectionChanged+=(_,_)=>UpdateTotals();
    }
    /// <summary>
    /// Tên bảng/loại nghiệp vụ nguồn để đọc trường và mở biểu mẫu đúng loại.
    /// </summary>
    private string Table=>(EditorTabs.SelectedItem as TabItem)?.Tag?.ToString()?.Replace("Table:","")??"LICH_SU_KHAM_BENH";
    /// <summary>
    /// Collection dòng nháp của tab đang chọn: bước kế hoạch hoặc dịch vụ thực hiện.
    /// </summary>
    private ObservableCollection<TreatmentDraft> Drafts=>Table=="KE_HOACH_DIEU_TRI"?planRows:progressRows;
    /// <summary>
    /// Nạp danh mục cho các control theo Tag rồi khởi tạo các dòng nháp của tab nghiệp vụ hiện tại.
    /// </summary>
    private void Form_Loaded(object sender,RoutedEventArgs e) { BieuMauHoSo.Prepare(this);InitializeDrafts(); }
    /// <summary>
    /// Khi form đã tải và người dùng đổi đúng EditorTabs, khởi tạo phần nhập của bảng mới, tránh xử lý sự kiện từ tab con.
    /// </summary>
    private void EditorTabs_SelectionChanged(object sender,SelectionChangedEventArgs e) { if(IsLoaded&&e.Source==EditorTabs)InitializeDrafts(); }
    /// <summary>
    /// Đặt tiêu đề theo bảng và nạp dòng dịch vụ đang sửa hoặc bước kế hoạch nguồn; chỉ khởi tạo mỗi bảng một lần để giữ dữ liệu đang nhập.
    /// </summary>
    private void InitializeDrafts()
    {
        EditorTitle.Text=Table switch {"LICH_SU_KHAM_BENH"=>"Thông tin lần khám","TINH_TRANG_BENH"=>"Ghi nhận tình trạng bệnh","KE_HOACH_DIEU_TRI"=>"Kế hoạch điều trị","CHI_TIET_KE_HOACH"=>"Dịch vụ dự kiến",_=>"Dịch vụ thực hiện"};
        if(initializedTable==Table)return;initializedTable=Table;
        if(Table!="DICH_VU_THUC_HIEN")return;
        var existing=BieuMauHoSo.GetRecord(this);
        if(existing is not null)
        {
            AddDraft(new(){Existing=existing,MaDV=UiRecord.Text(existing,"MaDV"),Label=DuLieuGiaoDien.Lookup("MaDV",UiRecord.Get(existing,"MaDV")),MaCTKH=UiRecord.Text(existing,"MaCTKH"),MaNVThucHien=UiRecord.Text(existing,"MaNVThucHien"),Quantity=TreatmentMoney.Number(existing,"SoLuong"),Price=TreatmentMoney.Number(existing,"DonGia"),Note=UiRecord.Text(existing,"KetQua"),Status=UiRecord.Text(existing,"TrangThai")});
            return;
        }
        var step=DuLieuGiaoDien.Find("CHI_TIET_KE_HOACH",ReadField("DICH_VU_THUC_HIEN","MaCTKH"));
        if(step is null)return;
        var plan=DuLieuGiaoDien.Find("KE_HOACH_DIEU_TRI",UiRecord.Get(step,"MaKHDT"));
        if(plan is not null)
        {
            if (ReadField("DICH_VU_THUC_HIEN","MaLS") is null || string.IsNullOrWhiteSpace(Convert.ToString(ReadField("DICH_VU_THUC_HIEN","MaLS")))) WriteField("DICH_VU_THUC_HIEN","MaLS",UiRecord.Get(plan,"MaLS"));
        }
        AddDraft(new(){MaDV=UiRecord.Text(step,"MaDV"),Label=DuLieuGiaoDien.Lookup("MaDV",UiRecord.Get(step,"MaDV")),MaCTKH=UiRecord.Text(step,"MaCTKH"),MaNVThucHien=plan is null?null:UiRecord.Text(plan,"MaBS"),Quantity=TreatmentMoney.Number(step,"SoLuongDuKien"),Price=TreatmentMoney.Number(step,"DonGiaDuKien")});
    }
    /// <summary>
    /// Gắn thông báo thay đổi cho dòng dịch vụ, thêm vào collection của tab đang chọn và tính lại tổng tiền hiển thị.
    /// </summary>
    private void AddDraft(TreatmentDraft draft) { draft.PropertyChanged+=DraftChanged;Drafts.Add(draft);UpdateTotals(); }
    /// <summary>
    /// Tính lại tổng dự kiến/thực hiện khi dòng nháp báo số lượng hoặc đơn giá thay đổi.
    /// </summary>
    private void DraftChanged(object? sender,PropertyChangedEventArgs e)=>UpdateTotals();
    /// <summary>
    /// Cộng thành tiền đã làm tròn của các dòng nháp và cập nhật hai nhãn tổng tiền; chưa ghi số tổng này xuống SQL.
    /// </summary>
    private void UpdateTotals()
    {
        if(PlanDraftTotal is null)return;
        try
        {
            PlanDraftTotal.Text="Tổng chi phí dự kiến: "+TreatmentMoney.Format(planRows.Sum(row=>row.Amount));
            ProgressDraftTotal.Text="Tổng chi phí thực hiện: "+TreatmentMoney.Format(progressRows.Sum(row=>row.Amount));
        }
        catch(OverflowException)
        {
            PlanDraftTotal.Text=ProgressDraftTotal.Text="Số lượng/đơn giá quá lớn để tính tổng.";
            FormError.Text="Giảm số lượng hoặc đơn giá; giá trị đang nhập vượt giới hạn tính tiền.";
        }
    }
    /// <summary>
    /// Hẹn cập nhật tổng qua Dispatcher để binding nhận giá trị vừa nhập xong trong ô DataGrid trước khi tính.
    /// </summary>
    private void DraftCellEditEnding(object sender,DataGridCellEditEndingEventArgs e)=>Dispatcher.BeginInvoke(new Action(UpdateTotals));
    /// <summary>
    /// Gỡ sự kiện của dòng nháp và bỏ dòng khỏi collection đang nhập; chưa xóa chứng từ SQL ở bước này.
    /// </summary>
    private void RemoveDraft_Click(object sender,RoutedEventArgs e) { if((sender as FrameworkElement)?.DataContext is TreatmentDraft draft){draft.PropertyChanged-=DraftChanged;Drafts.Remove(draft);} }
    /// <summary>
    /// Đọc danh mục dịch vụ còn sử dụng, tạo nhóm lọc và mở lớp chọn dịch vụ để thêm một hoặc nhiều dòng nháp.
    /// </summary>
    private void OpenServices_Click(object sender,RoutedEventArgs e)
    {
        services=DuLieuGiaoDien.Rows("DICH_VU").Where(row=>UiRecord.Text(row,"TrangThai") is not "Hủy" and not "Ngừng hoạt động").Select(row=>new TreatmentServiceChoice(row)).ToArray();
        ServiceGroups.ItemsSource=new[]{"Tất cả"}.Concat(services.Select(row=>row.Group).Where(group=>!string.IsNullOrWhiteSpace(group)).Distinct()).ToArray();ServiceGroups.SelectedIndex=0;ServiceSearch.Clear();FilterServices();ServiceOverlay.Visibility=Visibility.Visible;
    }
    /// <summary>
    /// Ẩn lớp chọn dịch vụ, quay về biểu mẫu điều trị và giữ các dòng nháp hiện có.
    /// </summary>
    private void CloseServices_Click(object sender,RoutedEventArgs e)=>ServiceOverlay.Visibility=Visibility.Collapsed;
    /// <summary>
    /// Lọc lại danh mục chọn dịch vụ theo chuỗi tìm kiếm mới.
    /// </summary>
    private void ServiceSearch_Changed(object sender,TextChangedEventArgs e)=>FilterServices();
    /// <summary>
    /// Lọc lại danh mục dịch vụ theo nhóm đang chọn, kết hợp với chuỗi tìm kiếm.
    /// </summary>
    private void ServiceGroup_Changed(object sender,SelectionChangedEventArgs e)=>FilterServices();
    /// <summary>
    /// Lọc danh mục theo nhóm và mã/tên/mô tả, cập nhật ItemsSource và thông báo danh mục trống hoặc hướng dẫn chọn nhiều dịch vụ.
    /// </summary>
    private void FilterServices()
    {
        if(ServiceCatalog is null)return;
        var query=ServiceSearch.Text.Trim();var group=ServiceGroups.SelectedItem as string??"Tất cả";
        ServiceCatalog.ItemsSource=services.Where(row=>(group=="Tất cả"||row.Group==group)&&(query.Length==0||(row.Code+" "+row.Label+" "+row.Description).Contains(query,StringComparison.CurrentCultureIgnoreCase))).ToArray();
        CatalogMessage.Text=services.Length==0?"Chưa có dịch vụ trong danh mục.":"Đánh dấu dịch vụ cần thêm. Có thể chọn nhiều dịch vụ.";
    }
    /// <summary>
    /// Chốt ô chọn trong bảng, kiểm tra có dịch vụ được đánh dấu; điền một bước kế hoạch hoặc thêm các dòng nháp với giá hiện hành và bác sĩ của lần khám.
    /// </summary>
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
            if(BieuMauHoSo.GetRecord(this)is not null&&Table=="DICH_VU_THUC_HIEN") { CatalogMessage.Text="Bản ghi thực hiện đang chỉnh sửa giữ nguyên dịch vụ. Thêm bản ghi mới để chọn dịch vụ khác.";return; }
            foreach(var service in selected)
            {
                var visit=DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH",ReadField(Table,"MaLS"));
                AddDraft(new(){MaDV=service.Code,Label=service.Label,Price=TreatmentMoney.Number(service.Record,"DonGia"),LanThu=planRows.Count+1,MaNVThucHien=visit is null?null:UiRecord.Text(visit,"MaBS")});
            }
        }
        ServiceOverlay.Visibility=Visibility.Collapsed;
    }
    /// <summary>
    /// Mở hoặc dùng lại giao dịch chung, gọi CommitCore kiểm tra/lưu form; trả true khi lưu được hoặc false kèm lỗi và rollback khi không thành công.
    /// </summary>
    public bool Commit(out string error)
    {
        if (!KiemTraNhapLieu.PrepareSave(this, out error)) return false;
        string failure="";
        try {var result=DuLieuGiaoDien.Transaction(()=>CommitCore(out failure));error=failure;return result;}
        catch(LoiNhapLieu ex) {KiemTraNhapLieu.MarkError(this,ex);error=ex.Message;return false;}
        catch(Exception ex) when(ex is InvalidOperationException or FormatException or OverflowException or ArgumentOutOfRangeException) {error=ex.Message;return false;}
    }
    /// <summary>
    /// Kiểm tra snapshot, quyền sửa, lỗi binding và từng tab; lưu lần khám/tình trạng/bước hoặc lưu kế hoạch và nhiều dòng dịch vụ cùng transaction để không lưu dở dang.
    /// </summary>
    private bool CommitCore(out string error)
    {
        BieuMauHoSo.VerifyUnchanged(this);
        error="";
        var existing=BieuMauHoSo.GetRecord(this);
        if(existing is not null && (!DuLieuGiaoDien.CanEdit(Table,existing) || Table=="CHI_TIET_KE_HOACH"&&!new TreatmentRow(Table,existing).CanEdit || Table=="KE_HOACH_DIEU_TRI"&&!new TreatmentPlan(existing).CanEdit)) {error="Thông tin đã xác nhận hoặc đã có điều trị hoàn thành được giữ nguyên.";return false;}
        var activeGrid=Table=="KE_HOACH_DIEU_TRI"?PlanDrafts:Table=="DICH_VU_THUC_HIEN"?ProgressDrafts:null;
        if(activeGrid is not null&&(!activeGrid.CommitEdit(DataGridEditingUnit.Cell,true)||!activeGrid.CommitEdit(DataGridEditingUnit.Row,true))) {error="Kiểm tra số lượng, lần thứ và đơn giá đã nhập.";return false;}
        if(Elements(this).Any(element=>Validation.GetHasError(element))) {error="Kiểm tra số lượng, lần thứ và đơn giá đã nhập.";return false;}
        if(Table=="KE_HOACH_DIEU_TRI")
        {
            if(planRows.Count==0&&BieuMauHoSo.GetRecord(this)is null){error="Chọn ít nhất một dịch vụ dự kiến.";return false;}
            if(!ValidateDrafts(planRows,false,out error))return false;
            if(ReadField(Table,"NgayBatDau")is DateTime start&&ReadField(Table,"NgayDuKienKetThuc")is DateTime finish&&finish<start){error="Ngày dự kiến kết thúc phải từ ngày bắt đầu trở đi.";return false;}
            if(!BieuMauHoSo.TrySave(this,out var plan,out error)||plan is null)return false;
            foreach(var row in planRows) DuLieuGiaoDien.Upsert("CHI_TIET_KE_HOACH",new Dictionary<string,object?>{{"MaKHDT",UiRecord.Get(plan,"MaKHDT")},{"MaDV",row.MaDV},{"LanThu",row.LanThu},{"SoLuongDuKien",row.Quantity},{"DonGiaDuKien",row.Price},{"TrangThai","Nháp"},{"GhiChu",row.Note}});
            return true;
        }
        if(Table=="DICH_VU_THUC_HIEN")
        {
            if(DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH",ReadField(Table,"MaLS"))is null||ReadField(Table,"ThoiGianThucHien")is not DateTime) {error="Chọn lần khám và ngày điều trị.";return false;}
            if(progressRows.Count==0){error="Chọn ít nhất một dịch vụ thực hiện.";return false;}
            if(!ValidateDrafts(progressRows,true,out error))return false;
            var first=progressRows[0];WriteProgress(first);
            if(!BieuMauHoSo.TrySave(this,out var saved,out error)||saved is null)return false;
            foreach(var row in progressRows.Skip(1))
            {
                var values=DuLieuGiaoDien.Values(saved);values.Remove("MaDVThucHien");values["MaDV"]=row.MaDV;values["MaCTKH"]=row.MaCTKH;values["MaNVThucHien"]=row.MaNVThucHien;values["SoLuong"]=row.Quantity;values["DonGia"]=row.Price;values["KetQua"]=row.Note;values["TrangThai"]=row.Status;
                DuLieuGiaoDien.Upsert("DICH_VU_THUC_HIEN",values);
            }
            return true;
        }
        if(Table=="TINH_TRANG_BENH" && (DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH",ReadField(Table,"MaLS"))is null || string.IsNullOrWhiteSpace(Convert.ToString(ReadField(Table,"LoaiTinhTrang"))) || string.IsNullOrWhiteSpace(Convert.ToString(ReadField(Table,"MoTaTinhTrang"))))) {error="Chọn lần khám và nhập loại, mô tả tình trạng bệnh.";return false;}
        if(Table=="CHI_TIET_KE_HOACH")
        {
            var values=BieuMauHoSo.Read(this);
            if(DuLieuGiaoDien.Find("KE_HOACH_DIEU_TRI",values.GetValueOrDefault("MaKHDT"))is null||DuLieuGiaoDien.Find("DICH_VU",values.GetValueOrDefault("MaDV"))is null){error="Chọn kế hoạch và dịch vụ dự kiến.";return false;}
            if(!int.TryParse(Convert.ToString(values.GetValueOrDefault("LanThu")),out var sequence)||sequence<=0||!QuyTacHienThi.TryNonNegativeNumber(Convert.ToString(values.GetValueOrDefault("SoLuongDuKien"))??"",out var quantity)||quantity<=0||!QuyTacHienThi.TryNonNegativeNumber(Convert.ToString(values.GetValueOrDefault("DonGiaDuKien"))??"",out var price)){error="Lần thứ và số lượng phải lớn hơn 0; đơn giá không được âm. Không nhập phân cách hàng nghìn.";return false;}
            WriteField(Table,"SoLuongDuKien",quantity);WriteField(Table,"DonGiaDuKien",price);WriteField(Table,"LanThu",sequence);
        }
        return BieuMauHoSo.TrySave(this,out _,out error);
    }
    /// <summary>
    /// Kiểm tra số lượng/lần thứ dương, giá không âm và dịch vụ tồn tại; với dịch vụ thực hiện còn bắt buộc có nhân viên thực hiện hợp lệ.
    /// </summary>
    private static bool ValidateDrafts(IEnumerable<TreatmentDraft> rows,bool actual,out string error)
    {
        error="";
        decimal total=0;
        foreach(var row in rows)
        {
            if(row.Quantity<=0||row.Price<0||row.LanThu<=0){error="Số lượng và lần thứ phải lớn hơn 0; đơn giá không được âm.";return false;}
            if(DuLieuGiaoDien.Find("DICH_VU",row.MaDV)is null){error="Dịch vụ không còn trong danh mục.";return false;}
            if(actual&&DuLieuGiaoDien.Find("NHAN_VIEN",row.MaNVThucHien)is null){error="Chọn người thực hiện cho từng dịch vụ.";return false;}
            try
            {
                KiemTraNhapLieu.ValidateValues(actual?"DICH_VU_THUC_HIEN":"CHI_TIET_KE_HOACH",
                    new Dictionary<string,object?> { [actual?"SoLuong":"SoLuongDuKien"]=row.Quantity, [actual?"DonGia":"DonGiaDuKien"]=row.Price, ["LanThu"]=row.LanThu });
                KiemTraNhapLieu.ValidateValues("CHI_TIET_HOA_DON",new Dictionary<string,object?> {["ThanhTien"]=row.Amount});
                total+=row.Amount;
                KiemTraNhapLieu.ValidateValues("KE_HOACH_DIEU_TRI",new Dictionary<string,object?> {["TongTienDuKien"]=total});
            }
            catch(LoiNhapLieu ex) {error=ex.Message;return false;}
            catch(OverflowException) {error="Số lượng/đơn giá quá lớn để tính thành tiền.";return false;}
        }
        return true;
    }
    /// <summary>
    /// Chuyển dữ liệu một dòng thực hiện vào các control ẩn có Tag DB để dùng chung cơ chế Read/TrySave của form.
    /// </summary>
    private void WriteProgress(TreatmentDraft row)
    {
        WriteField(Table,"MaDV",row.MaDV);WriteField(Table,"MaCTKH",row.MaCTKH);WriteField(Table,"MaNVThucHien",row.MaNVThucHien);WriteField(Table,"SoLuong",row.Quantity);WriteField(Table,"DonGia",row.Price);WriteField(Table,"KetQua",row.Note);WriteField(Table,"TrangThai",row.Status);
    }
    /// <summary>
    /// Tìm control có Tag DB:BANG.COT trong cây logical của form để đọc hoặc điền một trường cụ thể.
    /// </summary>
    private FrameworkElement? Field(string table,string field)=>Elements(this).OfType<FrameworkElement>().FirstOrDefault(element=>Equals(element.Tag,$"DB:{table}.{field}"));
    /// <summary>
    /// Đọc mã chọn, chuỗi nhập hoặc ngày từ control tìm theo Tag; trả null nếu không có loại control hỗ trợ.
    /// </summary>
    private object? ReadField(string table,string field)
    {
        var element=Field(table,field);
        return element switch {ComboBox box=>box.SelectedValue,TextBox text=>text.Text,DatePicker date=>date.SelectedDate,_=>null};
    }
    /// <summary>
    /// Điền SelectedValue/Text/SelectedDate cho control theo Tag; giữ mã khóa ngoại ở ComboBox thay vì nhãn hiển thị.
    /// </summary>
    private void WriteField(string table,string field,object? value)
    {
        var element=Field(table,field);
        switch(element){case ComboBox box:box.SelectedValue=Convert.ToString(value);break;case TextBox text:text.Text=Convert.ToString(value)??"";break;case DatePicker date:date.SelectedDate=value as DateTime?;break;}
    }
    /// <summary>
    /// Duyệt đệ quy cây logical của control để tìm các phần tử có Tag dữ liệu hoặc các ô nhập cần xử lý.
    /// </summary>
    private static IEnumerable<DependencyObject> Elements(DependencyObject parent)
    {
        yield return parent;
        foreach(var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())foreach(var element in Elements(child))yield return element;
    }
}
