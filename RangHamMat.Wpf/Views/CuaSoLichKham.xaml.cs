// AI bổ sung kiểm tra nhập liệu: chốt ngày/binding trước khi lưu và chặn thời điểm làm phép cộng thời lượng vượt DateTime.
// PHẦN AI ĐÃ THỰC HIỆN: AI hỗ trợ form hẹn khám: đọc thời gian/thời lượng, kiểm tra trùng, lý do và đổi lịch; đã giải thích cách giữ dữ liệu cũ khi lưu thay đổi.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
/// <summary>
/// Màn hình biểu mẫu đặt/hẹn lại lịch khám: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
/// </summary>
public partial class CuaSoLichKham : UserControl,IBieuMauLuu
{
    private bool ready;
    private bool reschedule;
    /// <summary>
    /// Khởi tạo biểu mẫu đặt/hẹn lại lịch khám từ XAML.
    /// </summary>
    public CuaSoLichKham(){InitializeComponent();}
    /// <summary>
    /// Chuẩn bị danh mục, xác định sửa lịch nháp hay hẹn lại lịch đã chốt, đặt tiêu đề/nút lưu phù hợp và tính lại cảnh báo trùng giờ.
    /// </summary>
    private void Form_Loaded(object sender,RoutedEventArgs e)
    {
        BieuMauHoSo.Prepare(this);
        var record=BieuMauHoSo.GetRecord(this);
        var actual=record is null?null:DuLieuGiaoDien.Find("LICH_KHAM",UiRecord.Get(record,"MaLich"));
        reschedule=actual is not null&&!DuLieuGiaoDien.CanEdit("LICH_KHAM",actual);
        EditorTitle.Text=reschedule?"Hẹn lại lịch khám":actual is null?"Đặt lịch khám qua điện thoại":"Cập nhật lịch hẹn";
        SaveButton.Content=reschedule?"Lưu lịch mới":"Lưu lịch hẹn";
        if(reschedule) ChangeReason.Clear();
        ready=true;UpdateSchedule();
    }
    /// <summary>
    /// Ghép ngày với giờ HH:mm, kiểm tra thời lượng từ 5 đến 480 phút và trả thời điểm bắt đầu/thời lượng đã phân tích.
    /// </summary>
    private bool TryTime(out DateTime date,out int minutes)
    {
        date=default;minutes=0;
        if(AppointmentDate.SelectedDate is not DateTime day || !TimeSpan.TryParseExact(StartTime.Text.Trim(),new[]{@"h\:mm",@"hh\:mm"},CultureInfo.InvariantCulture,out var time)||time.TotalHours>=24||!int.TryParse(Duration.Text,out minutes)||minutes<5||minutes>480)return false;
        date=day.Date+time;
        return date<=DateTime.MaxValue.AddMinutes(-minutes);
    }
    /// <summary>
    /// Tìm lịch còn hiệu lực có khoảng thời gian giao nhau và trùng bệnh nhân, bác sĩ hoặc phòng; bỏ qua lịch đang sửa và lịch đã kết thúc.
    /// </summary>
    private object[] Conflicts(DateTime start,int minutes)
    {
        var existing=BieuMauHoSo.GetRecord(this);var id=existing is null?"":UiRecord.Text(existing,"MaLich");
        var doctor=HoTroGiaoDien.Selected(DoctorPicker);var patient=HoTroGiaoDien.Selected(PatientPicker);var room=HoTroGiaoDien.Selected(RoomPicker);
        return DuLieuGiaoDien.Rows("LICH_KHAM").Where(row=>
        {
            if(UiRecord.Text(row,"MaLich")==id || UiRecord.Text(row,"TrangThai") is "Hủy" or "Đã đổi" or "Hoàn thành")return false;
            var time=UiRecord.Date(row,"NgayGioKham");var duration=int.TryParse(UiRecord.Text(row,"ThoiLuongDuKien"),out var n)?n:30;
            return time.HasValue&&time.Value<start.AddMinutes(minutes)&&time.Value.AddMinutes(duration)>start && (doctor.Length>0&&UiRecord.Text(row,"MaBS")==doctor || patient.Length>0&&UiRecord.Text(row,"MaBN")==patient || room.Length>0&&UiRecord.Text(row,"MaPhong")==room);
        }).ToArray();
    }
    /// <summary>
    /// Hiện SĐT bệnh nhân và cảnh báo các lịch chồng giờ dựa trên ngày, giờ, thời lượng và danh mục đang chọn.
    /// </summary>
    private void UpdateSchedule()
    {
        if(!ready)return;
        var patient=DuLieuGiaoDien.Find("BENH_NHAN",HoTroGiaoDien.Selected(PatientPicker));
        PatientPhone.Text=patient is null?"":"Số điện thoại liên hệ: "+UiRecord.Text(patient,"SDT");
        var conflicts=TryTime(out var date,out var minutes)?Conflicts(date,minutes):Array.Empty<object>();
        ConflictPanel.Visibility=conflicts.Length>0?Visibility.Visible:Visibility.Collapsed;
        ConflictText.Text=$"Có {conflicts.Length} lịch hẹn trùng thời gian với bệnh nhân, bác sĩ hoặc phòng. Chọn giờ khác hoặc ghi rõ lý do xếp lịch chồng giờ.";
    }
    /// <summary>
    /// Hủy xác nhận chồng lịch cũ và xóa lý do khi người dùng đổi lịch, rồi tính lại xung đột theo thông tin mới.
    /// </summary>
    private void Schedule_Changed(object sender,RoutedEventArgs e){if(!ready)return;OverlapAccepted.IsChecked=false;OverlapReason.Clear();UpdateSchedule();}
    /// <summary>
    /// Mở hoặc dùng lại giao dịch chung, gọi CommitCore kiểm tra/lưu form; trả true khi lưu được hoặc false kèm lỗi và rollback khi không thành công.
    /// </summary>
    public bool Commit(out string error)
    {
        if (!KiemTraNhapLieu.PrepareSave(this, out error)) return false;
        string failure="";
        try {var result=DuLieuGiaoDien.Transaction(()=>CommitCore(out failure));error=failure;return result;}
        catch(InvalidOperationException ex) {error=ex.Message;return false;}
    }
    /// <summary>
    /// Kiểm tra snapshot, giờ/ưu tiên, lý do hẹn lại và chấp nhận chồng lịch; khi hẹn lại thì tạo lịch nháp mới và đánh dấu lịch cũ Đã đổi trong cùng giao dịch.
    /// </summary>
    private bool CommitCore(out string error)
    {
        BieuMauHoSo.VerifyUnchanged(this);
        error="";
        try
        {
            if(!TryTime(out var date,out var minutes)){error="Chọn ngày, giờ HH:mm và thời lượng từ 5–480 phút.";return false;}
            if(!int.TryParse(Priority.Text,out var priority)||priority<0){error="Ưu tiên phải là số nguyên không âm.";return false;}
            var existing=BieuMauHoSo.GetRecord(this);var actual=existing is null?null:DuLieuGiaoDien.Find("LICH_KHAM",UiRecord.Get(existing,"MaLich"));
            if(reschedule && (actual is null||UiRecord.Text(actual,"TrangThai") is not ("Đã xác nhận" or "Chờ khám"))){error="Chỉ hẹn lại lịch đã xác nhận hoặc đang chờ khám.";return false;}
            if(reschedule&&(ChangeReason.Text.Trim().Length<5||UiRecord.Date(actual!,"NgayGioKham")==date)){error="Chọn giờ mới và ghi rõ lý do hẹn lại (ít nhất 5 ký tự).";return false;}
            var conflicts=Conflicts(date,minutes);
            if(conflicts.Length>0&&(OverlapAccepted.IsChecked!=true||OverlapReason.Text.Trim().Length<5)){error="Kiểm tra lịch trùng và ghi rõ lý do trước khi lưu.";return false;}
            var values=BieuMauHoSo.Read(this);values["NgayGioKham"]=date;values["ThoiLuongDuKien"]=minutes;values["UuTien"]=priority;values["NgayCapNhat"]=DateTime.Now;
            values["NgayTao"]=actual is null||reschedule?DateTime.Now:UiRecord.Get(actual,"NgayTao");
            if(conflicts.Length>0)values["GhiChu"]=(Notes.Text+"\nXếp lịch chồng giờ: "+OverlapReason.Text.Trim()).Trim();
            if(reschedule)
            {
                values["MaLich"]=null;values["TrangThai"]="Nháp";
                DuLieuGiaoDien.Upsert("LICH_KHAM",values);
                DuLieuGiaoDien.SetValue("LICH_KHAM",actual!,"LyDoThayDoi",ChangeReason.Text.Trim());DuLieuGiaoDien.SetValue("LICH_KHAM",actual!,"TrangThai","Đã đổi");
            }
            else DuLieuGiaoDien.Upsert("LICH_KHAM",values,actual);
            return true;
        }
        catch(InvalidOperationException ex){error=ex.Message;return false;}
    }
}
