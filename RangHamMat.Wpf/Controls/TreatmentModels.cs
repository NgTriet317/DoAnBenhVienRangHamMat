using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
namespace RangHamMat.Controls;

// Display/draft models only; every visual is declared in the treatment XAML.
public sealed class TreatmentVisit
{
    public object Record { get; }
    public Dictionary<string,object?> OriginalValues {get;private set;}
    public bool Expanded { get; set; } = true;
    public bool CanEdit => ClinicalData.CanEdit("LICH_SU_KHAM_BENH", Record);
    public string Code => UiRecord.Text(Record, "MaLS");
    public string Status => UiRecord.Text(Record, "TrangThai");
    public string When => UiRecord.Date(Record, "ThoiGianKham")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa ghi thời gian";
    public string Patient => ClinicalData.Lookup("MaBN", ClinicalData.PatientId("LICH_SU_KHAM_BENH", Record));
    public string Receipt => ClinicalData.Lookup("MaPhieu", UiRecord.Get(Record, "MaPhieu"));
    private string? doctor,room;
    private string symptom="",diagnosis="",note="",handling="";
    public bool IsDirty { get; private set; }
    public string? MaBS { get=>doctor;set {if(doctor!=value){doctor=value;IsDirty=true;}} }
    public string? MaPhong { get=>room;set {if(room!=value){room=value;IsDirty=true;}} }
    public string TrieuChung { get=>symptom;set {if(symptom!=value){symptom=value;IsDirty=true;}} }
    public string ChanDoan { get=>diagnosis;set {if(diagnosis!=value){diagnosis=value;IsDirty=true;}} }
    public string GhiChu { get=>note;set {if(note!=value){note=value;IsDirty=true;}} }
    public string KeHoachXuLy { get=>handling;set {if(handling!=value){handling=value;IsDirty=true;}} }
    public void AcceptSaved(){IsDirty=false;OriginalValues=ClinicalData.Values(Record);}
    public void CopyDraftFrom(TreatmentVisit prior)
    {
        if(!prior.IsDirty||!CanEdit)return;
        MaBS=prior.MaBS;MaPhong=prior.MaPhong;TrieuChung=prior.TrieuChung;ChanDoan=prior.ChanDoan;GhiChu=prior.GhiChu;KeHoachXuLy=prior.KeHoachXuLy;IsDirty=true;
        OriginalValues=new(prior.OriginalValues);
    }
    public IEnumerable<ClinicalOption> Doctors => ClinicalData.Options("NHAN_VIEN");
    public IEnumerable<ClinicalOption> Rooms => ClinicalData.Options("PHONG");
    public TreatmentRow[] Conditions { get; }
    public TreatmentPlan[] Plans { get; }
    public TreatmentDay[] Days { get; }
    public string ConditionsNote => Conditions.Length == 0 ? "Chưa ghi nhận tình trạng bệnh. Chọn Thêm tình trạng để ghi nhận." : "";
    public string PlansNote => Plans.Length == 0 ? "Chưa có kế hoạch điều trị. Chọn Thêm kế hoạch để chọn dịch vụ dự kiến." : "";
    public string ProgressNote => Days.Length == 0 ? "Chưa có dịch vụ thực hiện. Chọn Thêm ngày điều trị để ghi nhận." : "";
    public string EstimatedTotal => TreatmentMoney.Format(Plans.Sum(plan => plan.Amount));
    public string ActualTotal => TreatmentMoney.Format(Days.SelectMany(day => day.Rows).Where(row => row.Status != "Hủy").Sum(row => row.Amount));
    public string CompletedTotal => TreatmentMoney.Format(Days.SelectMany(day => day.Rows).Where(row => row.Status == "Đã xác nhận").Sum(row => row.Amount));
    public TreatmentVisit(object record)
    {
        Record = record; OriginalValues=ClinicalData.Values(record);doctor = UiRecord.Text(record,"MaBS"); room = UiRecord.Text(record,"MaPhong");
        symptom = UiRecord.Text(record,"TrieuChung"); diagnosis = UiRecord.Text(record,"ChanDoan"); note = UiRecord.Text(record,"GhiChu"); handling = UiRecord.Text(record,"KeHoachXuLy");
        Conditions = ClinicalData.Children("TINH_TRANG_BENH","MaLS",Code).Select(row => new TreatmentRow("TINH_TRANG_BENH",row)).ToArray();
        Plans = ClinicalData.Children("KE_HOACH_DIEU_TRI","MaLS",Code).Select(row => new TreatmentPlan(row)).ToArray();
        Days = ClinicalData.Children("DICH_VU_THUC_HIEN","MaLS",Code).OrderBy(row=>UiRecord.Date(row,"ThoiGianThucHien")).GroupBy(row=>UiRecord.Date(row,"ThoiGianThucHien")?.Date).Select(group => new TreatmentDay(group.Key, group)).ToArray();
    }
}
public sealed class TreatmentPlan
{
    public object Record { get; }
    public string Code => UiRecord.Text(Record,"MaKHDT");
    public string Status => UiRecord.Text(Record,"TrangThai");
    public string Doctor => ClinicalData.Lookup("MaBS",UiRecord.Get(Record,"MaBS"));
    public string Note => UiRecord.Text(Record,"GhiChu");
    public bool CanEdit => ClinicalData.CanEdit("KE_HOACH_DIEU_TRI",Record) && Rows.All(row=>row.CanEdit);
    public TreatmentRow[] Rows { get; }
    public decimal Amount => Status=="Hủy"?0:Rows.Where(row=>row.Status!="Hủy").Sum(row=>row.Amount);
    public string Total => TreatmentMoney.Format(Amount);
    public TreatmentPlan(object record) { Record=record; Rows=ClinicalData.Children("CHI_TIET_KE_HOACH","MaKHDT",Code).Select(row=>new TreatmentRow("CHI_TIET_KE_HOACH",row)).ToArray(); }
}
public sealed class TreatmentDay
{
    public string When { get; }
    public TreatmentRow[] Rows { get; }
    public string Total => TreatmentMoney.Format(Rows.Where(row=>row.Status!="Hủy").Sum(row=>row.Amount));
    public TreatmentDay(DateTime? date,IEnumerable<object> rows) { When=date?.ToString("dd/MM/yyyy")??"Chưa ghi ngày";Rows=rows.Select(row=>new TreatmentRow("DICH_VU_THUC_HIEN",row)).ToArray(); }
}
public sealed class TreatmentRow
{
    public object Record { get; }
    public string Table { get; }
    public bool CanEdit => ClinicalData.CanEdit(Table,Record) && !HasCompletedExecution;
    public bool HasCompletedExecution => Table=="CHI_TIET_KE_HOACH" && ClinicalData.Children("DICH_VU_THUC_HIEN","MaCTKH",UiRecord.Get(Record,"MaCTKH")).Any(row=>UiRecord.Text(row,"TrangThai")=="Đã xác nhận");
    public bool CanExecute => Table=="CHI_TIET_KE_HOACH" && Status is not "Hoàn thành" and not "Hủy";
    public string Code => UiRecord.Text(Record,ClinicalData.Key(Table));
    public string Service => ClinicalData.Lookup("MaDV",UiRecord.Get(Record,"MaDV"));
    public string Doctor => ClinicalData.Lookup("MaNVThucHien",UiRecord.Get(Record,"MaNVThucHien"));
    public string Status
    {
        get
        {
            var state=UiRecord.Text(Record,"TrangThai");
            if(Table!="CHI_TIET_KE_HOACH"||state=="Hủy")return state;
            var completed=ClinicalData.Children("DICH_VU_THUC_HIEN","MaCTKH",UiRecord.Get(Record,"MaCTKH"))
                .Where(row=>UiRecord.Text(row,"TrangThai")=="Đã xác nhận").Sum(row=>TreatmentMoney.Number(row,"SoLuong"));
            return ClinicalRules.PlanStepStatus(state,Quantity,completed);
        }
    }
    public string Type => UiRecord.Text(Record,"LoaiTinhTrang");
    public string Description => UiRecord.Text(Record,"MoTaTinhTrang");
    public string Severity => UiRecord.Text(Record,"MucDo");
    public string Note => UiRecord.Text(Record,"GhiChu");
    public string Result => UiRecord.Text(Record,"KetQua");
    public string Sequence => UiRecord.Text(Record,"LanThu");
    public string Step => UiRecord.Text(Record,"MaCTKH");
    public string Time => UiRecord.Date(Record,"ThoiGianThucHien")?.ToString("HH:mm")??"";
    public decimal Quantity => TreatmentMoney.Number(Record,Table=="CHI_TIET_KE_HOACH"?"SoLuongDuKien":"SoLuong");
    public decimal Price => TreatmentMoney.Number(Record,Table=="CHI_TIET_KE_HOACH"?"DonGiaDuKien":"DonGia");
    public decimal Amount => decimal.Round(Quantity*Price,2,MidpointRounding.AwayFromZero);
    public string PriceText => TreatmentMoney.Format(Price);
    public string AmountText => TreatmentMoney.Format(Amount);
    public TreatmentRow(string table,object record){Table=table;Record=record;}
}
public static class TreatmentMoney
{
    public static decimal Number(object row,string field)=>decimal.TryParse(UiRecord.Text(row,field),NumberStyles.Number,CultureInfo.CurrentCulture,out var value)?value:0;
    public static string Format(decimal value)=>value.ToString(value==decimal.Truncate(value)?"N0":"N2",CultureInfo.GetCultureInfo("vi-VN"))+" đ";
}
public sealed class TreatmentDraft : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public object? Existing { get; init; }
    public string MaDV { get; init; }="";
    public string Label { get; init; }="";
    public string? MaCTKH { get; set; }
    public string? MaNVThucHien { get; set; }
    public int LanThu { get; set; }=1;
    public string Note { get; set; }="";
    public string Status { get; set; }="Nháp";
    private decimal quantity=1,price;
    public decimal Quantity { get=>quantity;set{quantity=value;Changed();Changed(nameof(Total));} }
    public decimal Price { get=>price;set{price=value;Changed();Changed(nameof(Total));} }
    public decimal Amount=>decimal.Round(Quantity*Price,2,MidpointRounding.AwayFromZero);
    public string Total=>TreatmentMoney.Format(Amount);
    public IEnumerable<ClinicalOption> Staff=>ClinicalData.Options("NHAN_VIEN");
    public string[] States => new[]{"Nháp","Đã xác nhận","Hủy","Ngừng áp dụng"};
    private void Changed([CallerMemberName]string? name=null)=>PropertyChanged?.Invoke(this,new(name));
}
public sealed class TreatmentServiceChoice
{
    public object Record { get; }
    public bool Selected { get; set; }
    public string Code => UiRecord.Text(Record,"MaDV");
    public string Label => UiRecord.Text(Record,"TenDV");
    public string Group => UiRecord.Text(Record,"NhomDV");
    public string Description => UiRecord.Text(Record,"MoTa");
    public string Price => TreatmentMoney.Format(TreatmentMoney.Number(Record,"DonGia"));
    public TreatmentServiceChoice(object record){Record=record;}
}
