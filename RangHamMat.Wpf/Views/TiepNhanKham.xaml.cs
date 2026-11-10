using BUS;
using ET;
using RangHamMat.Controls;
using RangHamMat.Controls.ClinicalWorkflow;
using System.Windows;
using System.Windows.Controls;
using System.Data;

namespace RangHamMat.Views;

/// <summary>Form tiếp nhận chỉ gửi lựa chọn; định danh người lưu, giờ và số thứ tự được DAL xác định lại.</summary>
public partial class TiepNhanKham : UserControl
{
    /// <summary>
    /// Cờ Sẵn sàng; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
    /// </summary>
    private bool ready;
    /// <summary>
    /// Mã bệnh nhân được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private string? patientId;
    /// <summary>
    /// Ngữ cảnh định danh và quyền của người dùng đang thao tác.
    /// </summary>
    private NguCanhNghiepVu? user;
    private readonly BUS_TIEPNHAN receptionBus;
    private readonly IDbTransaction? transaction;
    private bool updatingChoices, saving;
    /// <summary>
    /// Đã lưu kết quả. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public KetQuaTiepNhan? SavedResult { get; private set; }
    /// <summary>
    /// Khởi tạo tiếp nhận khám. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
    /// </summary>
    public TiepNhanKham() : this(null,null) { }
    public TiepNhanKham(NguCanhNghiepVu? nguoiDung,IDbTransaction? transaction)
    {
        InitializeComponent();user=nguoiDung;this.transaction=transaction;receptionBus=new(transaction);
        LoaiKhamPicker.ItemsSource=new[]{new LuaChonDanhMuc("Thông thường","Khám thông thường"),new LuaChonDanhMuc("Dịch vụ","Khám dịch vụ — chọn bác sĩ")};
        LoaiKhamPicker.SelectedValue="Thông thường";
    }
    /// <summary>
    /// Giá trị Lịch hẹn; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    private object? Appointment => AppointmentPicker.SelectedValue is string id && id.Length>0?DuLieuGiaoDien.Find("LICH_KHAM",id):null;
    private bool DirectArrival=>string.IsNullOrEmpty(Convert.ToString(AppointmentPicker.SelectedValue));
    private bool ServiceVisit=>DirectArrival && LoaiKhamPicker.SelectedValue as string=="Dịch vụ";

    /// <summary>
    /// Nạp dữ liệu khởi đầu khi biểu mẫu được tải lên giao diện.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>user.CoQuyen</c>, <c>BieuMauHoSo.GetRecord</c>, <c>BieuMauHoSo.GetPatientScope</c>, <c>DuLieuGiaoDien.PatientId</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Form_Loaded(object sender,RoutedEventArgs e)
    {
        if(ready)return;
        try
        {
            user??=new BUS_QUYENNGHIEPVU(transaction).LayNguCanh(PhienDangNhap.MaNV);
            if(!user.CoQuyen(QuyenLamViec.TiepNhan))throw new TuChoiQuyenNghiepVuException();
            var seed=BieuMauHoSo.GetRecord(this);
            patientId=BieuMauHoSo.GetPatientScope(this);
            if(string.IsNullOrEmpty(patientId) && seed is not null) patientId=DuLieuGiaoDien.PatientId("PHIEU_KHAM_BENH",seed);
            if(DuLieuGiaoDien.PhamViLamSang && !string.IsNullOrEmpty(patientId) && DuLieuGiaoDien.Find("BENH_NHAN",patientId) is null)
                DuLieuGiaoDien.BaoDamNguCanh(new(){MaBN=patientId});
            var patient=DuLieuGiaoDien.Find("BENH_NHAN",patientId) ?? throw new InvalidOperationException("Chọn bệnh nhân từ hồ sơ trước khi tiếp nhận.");
            PatientIdentity.Text=$"{UiRecord.Text(patient,"HoTenBN")} · {patientId} · {UiRecord.Date(patient,"NgaySinh"):dd/MM/yyyy} · {UiRecord.Text(patient,"SDT")}";
            var history=DuLieuGiaoDien.Children("HO_SO_BENH_AN","MaBN",patientId).SingleOrDefault();
            MedicalIdentity.Text=history is null ? "Chưa có hồ sơ bệnh án; hệ thống sẽ lập khi tiếp nhận thành công." : $"Hồ sơ: {UiRecord.Text(history,"MaHS")}";
            SafetyInfo.Text=$"Dị ứng: {Known(UiRecord.Text(patient,"DiUng"))}\nTiền sử: {Known(UiRecord.Text(patient,"TienSuBenh"))}";
            SystemInfo.Text=$"Nhân viên: {DuLieuGiaoDien.Lookup("MaNV",user.MaNV)} ({user.MaNV}). Giờ tiếp nhận và số thứ tự được tự cấp khi lưu.";
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
    /// <summary>
    /// Kiểm tra hoặc lấy thông tin đã xác định của dữ liệu hiện tại.
    /// </summary>
    /// <param name="value">Giá trị string được truyền vào để đọc, kiểm tra hoặc cập nhật trong thao tác này.</param>
    /// <returns>Chuỗi kết quả của thao tác đã biết.</returns>
    private static string Known(string value)=>string.IsNullOrWhiteSpace(value)?"Chưa ghi nhận, cần đối chiếu người bệnh":value;
    /// <summary>
    /// Tiếp nhận thay đổi lịch hẹn và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>UiRecord.Text</c>, <c>UiRecord.Get</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Appointment_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!ready||updatingChoices)return;
        updatingChoices=true;
        try{LoaiKhamPicker.SelectedValue="Thông thường";LoaiKhamBlock.Visibility=DirectArrival?Visibility.Visible:Visibility.Collapsed;}
        finally{updatingChoices=false;}
        ConfigureMode();
        if(Appointment is object appointment)
        {ArrivalReason.Text=UiRecord.Text(appointment,"LyDoKham");ServicePicker.SelectedValue=UiRecord.Get(appointment,"MaDV");}
        else ArrivalReason.Clear();
    }
    private void LoaiKham_Changed(object sender,SelectionChangedEventArgs e)
    {if(ready&&!updatingChoices)ConfigureMode();}

    private void ConfigureMode()
    {
        ClinicalFormInput.ClearErrors(this);
        updatingChoices=true;
        try
        {
            ServicePicker.ItemsSource=null;ServicePicker.SelectedIndex=-1;ServicePicker.Text="";
            DoctorPicker.ItemsSource=null;DoctorPicker.SelectedIndex=-1;DoctorPicker.Text="";
            ClearRoom();FormError.Text="";DestinationIdentity.Text="Chưa chọn bác sĩ phụ trách";
            Grid.SetRow(DoctorField,ServiceVisit?0:1);Grid.SetRow(ServiceField,ServiceVisit?1:0);
            FeeHint.Text=ServiceVisit?"Phụ thu khám dịch vụ (chọn bác sĩ): 30.000 đ/lượt. Khoản này được ghi riêng trên hóa đơn.":"Khám thông thường không có phụ thu chọn bác sĩ.";
            DoctorPicker.IsEnabled=DirectArrival;ServicePicker.IsEnabled=!ServiceVisit && (Appointment is null||string.IsNullOrEmpty(UiRecord.Text(Appointment,"MaDV")));
            if(ServiceVisit)
            {
                var doctors=receptionBus.LayBacSiDichVu(user!);DoctorPicker.ItemsSource=doctors;
                ScheduleHint.Text=doctors.Count==0?"Chưa có bác sĩ có ca, hiện diện và phòng hợp lệ. Chưa thể tiếp nhận khám dịch vụ.":"Chọn bác sĩ đang trong ca, sau đó chọn dịch vụ thuộc chuyên khoa của bác sĩ.";
            }
            else
            {
                ServicePicker.ItemsSource=new BUS_DICHVU(transaction).layToanBoDichVu().Where(v=>v.TrangThai=="Đang hoạt động"&&!string.IsNullOrEmpty(v.MaCK))
                    .Select(v=>new LuaChonDanhMuc(v.MaDV,$"{v.TenDV} ({v.MaDV})")).ToArray();
                ScheduleHint.Text="Chọn dịch vụ trước, sau đó chọn bác sĩ phù hợp.";
            }
        }
        catch(InvalidOperationException ex){FormError.Text=ClinicalFormInput.Message(ex);}
        finally{updatingChoices=false;}
    }
    private void ClearRoom()
    {RoomPicker.ItemsSource=null;RoomPicker.SelectedIndex=-1;RoomPicker.IsEnabled=false;DispatchReason.Clear();DispatchBlock.Visibility=Visibility.Collapsed;}
    /// <summary>
    /// Tiếp nhận thay đổi dịch vụ và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DispatchReason.Clear</c>, <c>UiRecord.Get</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Service_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!ready||updatingChoices)return;
        if(ServiceVisit)
        {
            if(DoctorPicker.SelectedItem is BacSiTiepNhan chosen)DestinationIdentity.Text=$"Bác sĩ: {chosen.HoTen} ({chosen.MaNV}) · dịch vụ {ServicePicker.SelectedValue}";
            return;
        }
        DoctorPicker.ItemsSource=null;DoctorPicker.SelectedIndex=-1;RoomPicker.ItemsSource=null;RoomPicker.SelectedIndex=-1;DispatchReason.Clear();DispatchBlock.Visibility=Visibility.Collapsed;
        DestinationIdentity.Text="Chưa chọn bác sĩ phụ trách";
        ScheduleHint.Text="Chọn dịch vụ trước, sau đó chọn bác sĩ phù hợp.";
        if(ServicePicker.SelectedValue is not string service || string.IsNullOrEmpty(service))return;
        try
        {
            var doctors=receptionBus.LayBacSi(service,user!);DoctorPicker.ItemsSource=doctors;
            ScheduleHint.Text=doctors.Count==0?"Không có bác sĩ phù hợp đang làm việc. Chưa thể lưu tiếp nhận.":"Chọn bác sĩ; lịch làm việc và số đang chờ được đối chiếu từ hệ thống.";
            if(Appointment is object appointment)DoctorPicker.SelectedValue=UiRecord.Get(appointment,"MaBS");
        }
        catch(InvalidOperationException ex){FormError.Text=ex.Message;}
    }
    /// <summary>
    /// Tiếp nhận thay đổi bác sĩ và cập nhật trạng thái/dữ liệu phụ thuộc.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DispatchReason.Clear</c>, <c>DuLieuGiaoDien.Rows</c>, <c>UiRecord.Text</c>, <c>UiRecord.Get</c>.</remarks>
    /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
    /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
    private void Doctor_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!ready||updatingChoices)return;
        ClearRoom();
        if(ServiceVisit)
        {
            updatingChoices=true;
            try
            {
                ServicePicker.ItemsSource=null;ServicePicker.SelectedIndex=-1;ServicePicker.Text="";ServicePicker.IsEnabled=false;
                DestinationIdentity.Text="Chưa chọn bác sĩ phụ trách";
                if(DoctorPicker.SelectedItem is not BacSiTiepNhan chosen)return;
                var services=receptionBus.LayDichVuTheoBacSi(chosen.MaNV,user!);
                ServicePicker.ItemsSource=services.Select(d=>new LuaChonDanhMuc(d.MaDV,$"{d.TenDV} ({d.MaDV})")).ToArray();ServicePicker.IsEnabled=services.Count>0;
                RoomPicker.ItemsSource=new[]{new LuaChonDanhMuc(chosen.MaPhong!,$"{chosen.TenPhong} ({chosen.MaPhong})")};RoomPicker.SelectedValue=chosen.MaPhong;
                DestinationIdentity.Text=$"Bác sĩ: {chosen.HoTen} ({chosen.MaNV})";
                ScheduleHint.Text=services.Count==0?"Bác sĩ chưa có dịch vụ đang áp dụng trong chuyên khoa.":$"{chosen.TinhTrangLich} · {chosen.SoDangCho} bệnh nhân đang chờ. Phòng theo ca được khóa khi tiếp nhận.";
            }
            catch(InvalidOperationException ex){FormError.Text=ClinicalFormInput.Message(ex);}
            finally{updatingChoices=false;}
            return;
        }
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
        if(saving){error="Đang lưu tiếp nhận, vui lòng chờ.";return false;}
        saving=true;SaveReceptionButton.IsEnabled=false;
        try
        {
            if(user is null || string.IsNullOrEmpty(patientId))throw new InvalidOperationException("Chọn hồ sơ bệnh nhân hợp lệ.");
            var request=new YeuCauTiepNhan {LoaiKham=ServiceVisit?"Dịch vụ":"Thông thường",MaBN=patientId,MaDV=Convert.ToString(ServicePicker.SelectedValue)??"",MaBS=(DoctorPicker.SelectedItem as BacSiTiepNhan)?.MaNV??"",MaPhong=Convert.ToString(RoomPicker.SelectedValue)??"",MaLich=Convert.ToString(AppointmentPicker.SelectedValue),LyDoKham=ArrivalReason.Text,GhiChu=ReceptionNote.Text,LyDoDieuPhoiNgoaiLich=DispatchReason.Text};
            SavedResult=receptionBus.TiepNhan(request,user);
            SaveReceptionButton.IsEnabled=false;SaveReceptionButton.Content="Đã tiếp nhận";InputPanel.IsEnabled=false;QueueNumber.Text=SavedResult.SoThuTu.ToString();
            SavedStatus.Text=$"Đã chuyển vào hàng đợi của bác sĩ {DuLieuGiaoDien.Lookup("MaBS",SavedResult.MaBS)} · số {SavedResult.SoThuTu} · {SavedResult.MaPhieu}.";
            if(SavedResult.PhiPhuThuDichVu>0)SavedStatus.Text+=" Khám dịch vụ · phụ thu 30.000 đ đã lưu.";
            // Tải lại có thể lỗi sau commit; vẫn giữ biên nhận, không báo sai là chưa lưu.
            try{DuLieuGiaoDien.RefreshFromDatabase();}catch(InvalidOperationException){SavedStatus.Text+=" Đã lưu; tải lại hồ sơ khi kết nối ổn định.";}
            return true;
        }
        catch(InvalidOperationException ex)
        {
            error=FormError.Text=ClinicalFormInput.Message(ex);ClinicalFormInput.ShowError(this,ex);
            if(ex is LoiTiepNhanException invalid)
                (invalid.Truong switch {"LoaiKham"=>LoaiKhamPicker,"MaDV"=>ServicePicker,"MaBS"=>DoctorPicker,"MaPhong"=>RoomPicker,"LyDoKham"=>ArrivalReason,"LyDoDieuPhoiNgoaiLich"=>DispatchReason,_=>(Control)AppointmentPicker}).Focus();
            return false;
        }
        finally{saving=false;if(SavedResult is null)SaveReceptionButton.IsEnabled=true;}
    }
}
