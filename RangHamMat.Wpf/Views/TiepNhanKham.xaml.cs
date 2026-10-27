using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
using System.Windows;
using System.Windows.Controls;

namespace RangHamMat.Views;

/// <summary>Form tiếp nhận chỉ gửi lựa chọn; định danh người lưu, giờ và số thứ tự được DAL xác định lại.</summary>
public partial class TiepNhanKham : UserControl
{
    private bool ready;
    private string? patientId;
    private NguCanhNghiepVu? user;
    public KetQuaTiepNhan? SavedResult { get; private set; }
    public TiepNhanKham() => InitializeComponent();
    private object? Appointment => DuLieuGiaoDien.Find("LICH_KHAM",AppointmentPicker.SelectedValue);

    private void Form_Loaded(object sender,RoutedEventArgs e)
    {
        if(ready)return;
        try
        {
            user=new BUS_QUYENNGHIEPVU().LayNguCanh(PhienDangNhap.MaNV);
            if(!user.CoQuyen(QuyenLamViec.TiepNhan))throw new TuChoiQuyenNghiepVuException();
            var seed=BieuMauHoSo.GetRecord(this);
            patientId=BieuMauHoSo.GetPatientScope(this);
            if(string.IsNullOrEmpty(patientId) && seed is not null) patientId=DuLieuGiaoDien.PatientId("PHIEU_KHAM_BENH",seed);
            var patient=DuLieuGiaoDien.Find("BENH_NHAN",patientId) ?? throw new InvalidOperationException("Chọn bệnh nhân từ hồ sơ trước khi tiếp nhận.");
            PatientIdentity.Text=$"{UiRecord.Text(patient,"HoTenBN")} · {patientId} · {UiRecord.Date(patient,"NgaySinh"):dd/MM/yyyy} · {UiRecord.Text(patient,"SDT")}";
            var history=DuLieuGiaoDien.Children("HO_SO_BENH_AN","MaBN",patientId).SingleOrDefault();
            MedicalIdentity.Text=history is null ? "Chưa có hồ sơ bệnh án; hệ thống sẽ lập khi tiếp nhận thành công." : $"Hồ sơ: {UiRecord.Text(history,"MaHS")}";
            SafetyInfo.Text=$"Dị ứng: {Known(UiRecord.Text(patient,"DiUng"))}\nTiền sử: {Known(UiRecord.Text(patient,"TienSuBenh"))}";
            SystemInfo.Text=$"Nhân viên: {DuLieuGiaoDien.Lookup("MaNV",user.MaNV)} ({user.MaNV}). Giờ tiếp nhận và số thứ tự được tự cấp khi lưu.";
            ServicePicker.ItemsSource=DuLieuGiaoDien.Rows("DICH_VU").Where(v=>UiRecord.Text(v,"TrangThai")=="Đang hoạt động" && !string.IsNullOrEmpty(UiRecord.Text(v,"MaCK")))
                .Select(v=>new LuaChonDanhMuc(UiRecord.Text(v,"MaDV"),$"{UiRecord.Text(v,"TenDV")} ({UiRecord.Text(v,"MaDV")})")).ToArray();
            var arrivals=new List<LuaChonDanhMuc> {new("","Đến trực tiếp")};
            arrivals.AddRange(DuLieuGiaoDien.Rows("LICH_KHAM").Where(v=>UiRecord.Text(v,"MaBN")==patientId && UiRecord.Date(v,"NgayGioKham")?.Date==DateTime.Today && UiRecord.Text(v,"TrangThai") is "Nháp" or "Đã xác nhận" or "Chờ khám")
                .Select(v=>new LuaChonDanhMuc(UiRecord.Text(v,"MaLich"),$"{UiRecord.Text(v,"MaLich")} · {UiRecord.Date(v,"NgayGioKham"):dd/MM HH:mm} · {DuLieuGiaoDien.Lookup("MaBS",UiRecord.Get(v,"MaBS"))}")));
            AppointmentPicker.ItemsSource=arrivals;
            ready=true;
            var source=seed is null?"":UiRecord.Text(seed,"MaLich");
            AppointmentPicker.SelectedValue=source;
            if(AppointmentPicker.SelectedItem is null)throw new InvalidOperationException("Lịch nguồn không còn hiệu lực trong ngày. Đối chiếu lịch trước khi tiếp nhận.");
            if(seed is not null && DuLieuGiaoDien.Find("PHIEU_KHAM_BENH",UiRecord.Get(seed,"MaPhieu")) is not null)
            {InputPanel.IsEnabled=false;SaveReceptionButton.IsEnabled=false;FormError.Text="Phiếu đã lưu. Mở chi tiết phiếu để xem trạng thái; không tạo lại lượt tiếp nhận.";}
        }
        catch(InvalidOperationException ex){FormError.Text=ex.Message;SaveReceptionButton.IsEnabled=false;}
    }
    private static string Known(string value)=>string.IsNullOrWhiteSpace(value)?"Chưa ghi nhận, cần đối chiếu người bệnh":value;
    private void Appointment_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!ready)return;
        ServicePicker.SelectedIndex=-1;
        ServicePicker.IsEnabled=Appointment is null || string.IsNullOrEmpty(UiRecord.Text(Appointment,"MaDV"));
        DoctorPicker.IsEnabled=Appointment is null;
        if(Appointment is object appointment)
        {ArrivalReason.Text=UiRecord.Text(appointment,"LyDoKham");ServicePicker.SelectedValue=UiRecord.Get(appointment,"MaDV");}
    }
    private void Service_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!ready)return;
        DoctorPicker.ItemsSource=null;DoctorPicker.SelectedIndex=-1;RoomPicker.ItemsSource=null;RoomPicker.SelectedIndex=-1;DispatchReason.Clear();DispatchBlock.Visibility=Visibility.Collapsed;
        DestinationIdentity.Text="Chưa chọn bác sĩ phụ trách";
        ScheduleHint.Text="Chọn dịch vụ trước, sau đó chọn bác sĩ phù hợp.";
        if(ServicePicker.SelectedValue is not string service || string.IsNullOrEmpty(service))return;
        try
        {
            var doctors=new BUS_TIEPNHAN().LayBacSi(service,user!);DoctorPicker.ItemsSource=doctors;
            ScheduleHint.Text=doctors.Count==0?"Không có bác sĩ phù hợp đang làm việc. Chưa thể lưu tiếp nhận.":"Chọn bác sĩ; lịch làm việc và số đang chờ được đối chiếu từ hệ thống.";
            if(Appointment is object appointment)DoctorPicker.SelectedValue=UiRecord.Get(appointment,"MaBS");
        }
        catch(InvalidOperationException ex){FormError.Text=ex.Message;}
    }
    private void Doctor_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!ready)return;
        RoomPicker.ItemsSource=null;RoomPicker.SelectedIndex=-1;DispatchReason.Clear();DispatchBlock.Visibility=Visibility.Collapsed;
        if(DoctorPicker.SelectedItem is not BacSiTiepNhan doctor)return;
        DestinationIdentity.Text=$"Bác sĩ: {doctor.HoTen} ({doctor.MaNV}) · dịch vụ {ServicePicker.SelectedValue}";
        ScheduleHint.Text=$"{doctor.TinhTrangLich} · {doctor.SoDangCho} bệnh nhân đang chờ.";
        RoomPicker.ItemsSource=DuLieuGiaoDien.Rows("PHONG").Where(r=>UiRecord.Text(r,"TrangThai")=="Đang hoạt động" && UiRecord.Text(r,"MaKhoa")==doctor.MaKhoa && UiRecord.Text(r,"LoaiPhong").StartsWith("Phòng khám") && (doctor.ThieuLich || UiRecord.Text(r,"MaPhong")==doctor.MaPhong))
            .Select(r=>new LuaChonDanhMuc(UiRecord.Text(r,"MaPhong"),$"{UiRecord.Text(r,"TenPhong")} ({UiRecord.Text(r,"MaPhong")})")).ToArray();
        RoomPicker.IsEnabled=doctor.ThieuLich && Appointment is null;
        if(!doctor.ThieuLich)RoomPicker.SelectedValue=doctor.MaPhong;
        if(Appointment is object appointment)RoomPicker.SelectedValue=UiRecord.Get(appointment,"MaPhong");
        if(doctor.ThieuLich)
        {
            DispatchBlock.Visibility=Visibility.Visible;DispatchReason.IsEnabled=user!.CoQuyen(QuyenLamViec.DieuPhoiNgoaiLich);
            if(!DispatchReason.IsEnabled)ScheduleHint.Text+=" Phiên này chưa có quyền điều phối ngoài lịch; cần quầy có quyền đối chiếu.";
        }
    }

    /// <summary>Được workspace gọi ngoài giao dịch cache; chỉ tải cache sau khi BUS đã commit.</summary>
    public bool TryReceive(out string error)
    {
        error="";FormError.Text="";ClinicalFormInput.ClearErrors(this);
        if(SavedResult is not null)return true;
        try
        {
            if(user is null || string.IsNullOrEmpty(patientId))throw new InvalidOperationException("Chọn hồ sơ bệnh nhân hợp lệ.");
            var request=new YeuCauTiepNhan {MaBN=patientId,MaDV=Convert.ToString(ServicePicker.SelectedValue)??"",MaBS=(DoctorPicker.SelectedItem as BacSiTiepNhan)?.MaNV??"",MaPhong=Convert.ToString(RoomPicker.SelectedValue)??"",MaLich=Convert.ToString(AppointmentPicker.SelectedValue),LyDoKham=ArrivalReason.Text,GhiChu=ReceptionNote.Text,LyDoDieuPhoiNgoaiLich=DispatchReason.Text};
            SavedResult=new BUS_TIEPNHAN().TiepNhan(request,user);
            SaveReceptionButton.IsEnabled=false;SaveReceptionButton.Content="Đã tiếp nhận";InputPanel.IsEnabled=false;QueueNumber.Text=SavedResult.SoThuTu.ToString();
            SavedStatus.Text=$"Đã chuyển vào hàng đợi của bác sĩ {DuLieuGiaoDien.Lookup("MaBS",SavedResult.MaBS)} · số {SavedResult.SoThuTu} · {SavedResult.MaPhieu}.";
            // Tải lại có thể lỗi sau commit; vẫn giữ biên nhận, không báo sai là chưa lưu.
            try{DuLieuGiaoDien.RefreshFromDatabase();}catch(InvalidOperationException){SavedStatus.Text+=" Đã lưu; tải lại hồ sơ khi kết nối ổn định.";}
            return true;
        }
        catch(InvalidOperationException ex)
        {
            error=FormError.Text=ClinicalFormInput.Message(ex);ClinicalFormInput.ShowError(this,ex);
            if(ex is LoiTiepNhanException invalid)
                (invalid.Truong switch {"MaDV"=>ServicePicker,"MaBS"=>DoctorPicker,"MaPhong"=>RoomPicker,"LyDoKham"=>ArrivalReason,"LyDoDieuPhoiNgoaiLich"=>DispatchReason,_=>(Control)AppointmentPicker}).Focus();
            return false;
        }
    }
}
