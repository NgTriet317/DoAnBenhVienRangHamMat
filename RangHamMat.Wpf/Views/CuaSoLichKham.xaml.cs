using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
public partial class CuaSoLichKham : UserControl,IClinicalEditor
{
    private bool ready;
    private bool reschedule;
    public CuaSoLichKham(){InitializeComponent();}
    private void Form_Loaded(object sender,RoutedEventArgs e)
    {
        ClinicalForm.Prepare(this);
        var record=ClinicalForm.GetRecord(this);
        var actual=record is null?null:ClinicalData.Find("LICH_KHAM",UiRecord.Get(record,"MaLich"));
        reschedule=actual is not null&&!ClinicalData.CanEdit("LICH_KHAM",actual);
        EditorTitle.Text=reschedule?"Hẹn lại lịch khám":actual is null?"Đặt lịch khám qua điện thoại":"Cập nhật lịch hẹn";
        SaveButton.Content=reschedule?"Lưu lịch mới":"Lưu lịch hẹn";
        if(reschedule) ChangeReason.Clear();
        ready=true;UpdateSchedule();
    }
    private bool TryTime(out DateTime date,out int minutes)
    {
        date=default;minutes=0;
        if(AppointmentDate.SelectedDate is not DateTime day || !TimeSpan.TryParseExact(StartTime.Text.Trim(),new[]{@"h\:mm",@"hh\:mm"},CultureInfo.InvariantCulture,out var time)||time.TotalHours>=24||!int.TryParse(Duration.Text,out minutes)||minutes<5||minutes>480)return false;
        date=day.Date+time;return true;
    }
    private object[] Conflicts(DateTime start,int minutes)
    {
        var existing=ClinicalForm.GetRecord(this);var id=existing is null?"":UiRecord.Text(existing,"MaLich");
        var doctor=ClinicalUi.Selected(DoctorPicker);var patient=ClinicalUi.Selected(PatientPicker);var room=ClinicalUi.Selected(RoomPicker);
        return ClinicalData.Rows("LICH_KHAM").Where(row=>
        {
            if(UiRecord.Text(row,"MaLich")==id || UiRecord.Text(row,"TrangThai") is "Hủy" or "Đã đổi" or "Hoàn thành")return false;
            var time=UiRecord.Date(row,"NgayGioKham");var duration=int.TryParse(UiRecord.Text(row,"ThoiLuongDuKien"),out var n)?n:30;
            return time.HasValue&&time.Value<start.AddMinutes(minutes)&&time.Value.AddMinutes(duration)>start && (doctor.Length>0&&UiRecord.Text(row,"MaBS")==doctor || patient.Length>0&&UiRecord.Text(row,"MaBN")==patient || room.Length>0&&UiRecord.Text(row,"MaPhong")==room);
        }).ToArray();
    }
    private void UpdateSchedule()
    {
        if(!ready)return;
        var patient=ClinicalData.Find("BENH_NHAN",ClinicalUi.Selected(PatientPicker));
        PatientPhone.Text=patient is null?"":"Số điện thoại liên hệ: "+UiRecord.Text(patient,"SDT");
        var conflicts=TryTime(out var date,out var minutes)?Conflicts(date,minutes):Array.Empty<object>();
        ConflictPanel.Visibility=conflicts.Length>0?Visibility.Visible:Visibility.Collapsed;
        ConflictText.Text=$"Có {conflicts.Length} lịch hẹn trùng thời gian với bệnh nhân, bác sĩ hoặc phòng. Chọn giờ khác hoặc ghi rõ lý do xếp lịch chồng giờ.";
    }
    private void Schedule_Changed(object sender,RoutedEventArgs e){if(!ready)return;OverlapAccepted.IsChecked=false;OverlapReason.Clear();UpdateSchedule();}
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
        try
        {
            if(!TryTime(out var date,out var minutes)){error="Chọn ngày, giờ HH:mm và thời lượng từ 5–480 phút.";return false;}
            if(!int.TryParse(Priority.Text,out var priority)||priority<0){error="Ưu tiên phải là số nguyên không âm.";return false;}
            var existing=ClinicalForm.GetRecord(this);var actual=existing is null?null:ClinicalData.Find("LICH_KHAM",UiRecord.Get(existing,"MaLich"));
            if(reschedule && (actual is null||UiRecord.Text(actual,"TrangThai") is not ("Đã xác nhận" or "Chờ khám"))){error="Chỉ hẹn lại lịch đã xác nhận hoặc đang chờ khám.";return false;}
            if(reschedule&&(ChangeReason.Text.Trim().Length<5||UiRecord.Date(actual!,"NgayGioKham")==date)){error="Chọn giờ mới và ghi rõ lý do hẹn lại (ít nhất 5 ký tự).";return false;}
            var conflicts=Conflicts(date,minutes);
            if(conflicts.Length>0&&(OverlapAccepted.IsChecked!=true||OverlapReason.Text.Trim().Length<5)){error="Kiểm tra lịch trùng và ghi rõ lý do trước khi lưu.";return false;}
            var values=ClinicalForm.Read(this);values["NgayGioKham"]=date;values["ThoiLuongDuKien"]=minutes;values["UuTien"]=priority;values["NgayCapNhat"]=DateTime.Now;
            values["NgayTao"]=actual is null||reschedule?DateTime.Now:UiRecord.Get(actual,"NgayTao");
            if(conflicts.Length>0)values["GhiChu"]=(Notes.Text+"\nXếp lịch chồng giờ: "+OverlapReason.Text.Trim()).Trim();
            if(reschedule)
            {
                values["MaLich"]=null;values["TrangThai"]="Nháp";
                ClinicalData.Upsert("LICH_KHAM",values);
                ClinicalData.SetValue("LICH_KHAM",actual!,"LyDoThayDoi",ChangeReason.Text.Trim());ClinicalData.SetValue("LICH_KHAM",actual!,"TrangThai","Đã đổi");
            }
            else ClinicalData.Upsert("LICH_KHAM",values,actual);
            return true;
        }
        catch(InvalidOperationException ex){error=ex.Message;return false;}
    }
}
