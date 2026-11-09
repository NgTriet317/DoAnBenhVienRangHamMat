// AI bổ sung kiểm tra nhập liệu: hiển thị thông báo vượt giới hạn khi phép nhân tiền nháp tràn số.
// PHẦN AI ĐÃ THỰC HIỆN: AI đã bổ sung chú thích tiếng Việt cho TreatmentModels.cs: Thông tin lần khám kèm nháp đang sửa và snapshot gốc; gom tình trạng, kế hoạch, dịch vụ theo ngày để binding và giữ nháp khi làm mới. Giải thích được đặt cạnh khai báo/hàm tương ứng.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
namespace RangHamMat.Controls;

// Các lớp chỉ giữ dữ liệu hiển thị/nháp; phần tử giao diện điều trị khai báo trong XAML.
/// <summary>
/// Thông tin lần khám kèm nháp đang sửa và snapshot gốc; gom tình trạng, kế hoạch, dịch vụ theo ngày để binding và giữ nháp khi làm mới.
/// </summary>
public sealed class TreatmentVisit
{
    /// <summary>
    /// Bản ghi nguồn đã đọc để mô hình tra các trường/liên kết; không phải một bản SQL mới được tạo tự động.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Snapshot các giá trị gốc của lần khám để phát hiện bị sửa ở cửa sổ khác trước khi lưu nháp.
    /// </summary>
    public Dictionary<string,object?> OriginalValues {get;private set;}
    /// <summary>
    /// Trạng thái mở rộng thông tin lần khám ở giao diện.
    /// </summary>
    public bool Expanded { get; set; } = true;
    /// <summary>
    /// Cờ cho phép sửa dựa trên trạng thái, bản cha và các thực hiện đã chốt; dùng bật/tắt nút/ô nhập.
    /// </summary>
    public bool CanEdit => DuLieuGiaoDien.CanEdit("LICH_SU_KHAM_BENH", Record);
    /// <summary>
    /// Mã khóa của bản ghi dùng để hiển thị và xác định chính xác dòng đang thao tác.
    /// </summary>
    public string Code => UiRecord.Text(Record, "MaLS");
    /// <summary>
    /// Trạng thái hiển thị của bản ghi; với bước kế hoạch có thể được suy ra thêm từ lượng thực hiện đã xác nhận.
    /// </summary>
    public string Status => UiRecord.Text(Record, "TrangThai");
    /// <summary>
    /// Ngày/giờ của dòng nghiệp vụ được định dạng để hiển thị; nội dung mặc định cho biết chưa có thời gian.
    /// </summary>
    public string When => UiRecord.Date(Record, "ThoiGianKham")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa ghi thời gian";
    /// <summary>
    /// Thông tin hoặc nhãn bệnh nhân liên kết với bản ghi/hồ sơ đang xem.
    /// </summary>
    public string Patient => DuLieuGiaoDien.Lookup("MaBN", DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", Record));
    /// <summary>
    /// Mã hoặc nhãn lượt tiếp nhận liên kết với lần khám.
    /// </summary>
    public string Receipt => DuLieuGiaoDien.Lookup("MaPhieu", UiRecord.Get(Record, "MaPhieu"));
    /// <summary>
    /// Giá trị lưu nội bộ của thuộc tính MaBS, dùng bởi getter/setter tương ứng.
    /// </summary>
    private string? doctor,room;
    /// <summary>
    /// Giá trị lưu nội bộ của thuộc tính TrieuChung, dùng bởi getter/setter tương ứng.
    /// </summary>
    private string symptom="",diagnosis="",note="",handling="";
    /// <summary>
    /// Đánh dấu thông tin khám đang nhập đã khác với bản đã lưu; dùng giữ nháp khi làm mới dữ liệu con.
    /// </summary>
    public bool IsDirty { get; private set; }
    /// <summary>
    /// Mã bác sĩ, liên kết danh mục nhân viên. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string? MaBS { get=>doctor;set {if(doctor!=value){doctor=value;IsDirty=true;}} }
    /// <summary>
    /// Mã phòng. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string? MaPhong { get=>room;set {if(room!=value){room=value;IsDirty=true;}} }
    /// <summary>
    /// Triệu chứng được ghi khi khám. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string TrieuChung { get=>symptom;set {if(symptom!=value){symptom=value;IsDirty=true;}} }
    /// <summary>
    /// Chẩn đoán của lần khám. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string ChanDoan { get=>diagnosis;set {if(diagnosis!=value){diagnosis=value;IsDirty=true;}} }
    /// <summary>
    /// Ghi chú bổ sung. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string GhiChu { get=>note;set {if(note!=value){note=value;IsDirty=true;}} }
    /// <summary>
    /// Hướng xử lý sau khi khám. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string KeHoachXuLy { get=>handling;set {if(handling!=value){handling=value;IsDirty=true;}} }
    /// <summary>
    /// Đánh dấu nháp đã lưu và chụp lại giá trị gốc để lần lưu tiếp theo phát hiện việc bị sửa đồng thời.
    /// </summary>
    public void AcceptSaved(){IsDirty=false;OriginalValues=DuLieuGiaoDien.Values(Record);}
    /// <summary>
    /// Giữ các trường khám người dùng đang nhập khi làm mới chi tiết con; chỉ sao chép nếu nháp có thay đổi và lần khám vẫn được sửa.
    /// </summary>
    public void CopyDraftFrom(TreatmentVisit prior)
    {
        if(!prior.IsDirty||!CanEdit)return;
        MaBS=prior.MaBS;MaPhong=prior.MaPhong;TrieuChung=prior.TrieuChung;ChanDoan=prior.ChanDoan;GhiChu=prior.GhiChu;KeHoachXuLy=prior.KeHoachXuLy;IsDirty=true;
        OriginalValues=new(prior.OriginalValues);
    }
    /// <summary>
    /// Danh sách mã/nhãn nhân viên để chọn bác sĩ trong form lần khám.
    /// </summary>
    public IEnumerable<LuaChonDanhMuc> Doctors => DuLieuGiaoDien.Options("NHAN_VIEN");
    /// <summary>
    /// Danh sách mã/nhãn phòng để chọn phòng cho lần khám.
    /// </summary>
    public IEnumerable<LuaChonDanhMuc> Rooms => DuLieuGiaoDien.Options("PHONG");
    /// <summary>
    /// Các dòng tình trạng bệnh gắn với lần khám hiện tại.
    /// </summary>
    public TreatmentRow[] Conditions { get; }
    /// <summary>
    /// Các kế hoạch điều trị liên quan được mô hình tổng hợp để binding giao diện.
    /// </summary>
    public TreatmentPlan[] Plans { get; }
    /// <summary>
    /// Các ngày điều trị hoặc các ô ngày trong lịch được gom để binding.
    /// </summary>
    public TreatmentDay[] Days { get; }
    /// <summary>
    /// Thông báo trạng thái trống khi chưa ghi nhận tình trạng bệnh.
    /// </summary>
    public string ConditionsNote => Conditions.Length == 0 ? "Chưa ghi nhận tình trạng bệnh. Chọn Thêm tình trạng để ghi nhận." : "";
    /// <summary>
    /// Thông báo trạng thái trống khi chưa có kế hoạch điều trị.
    /// </summary>
    public string PlansNote => Plans.Length == 0 ? "Chưa có kế hoạch điều trị. Chọn Thêm kế hoạch để chọn dịch vụ dự kiến." : "";
    /// <summary>
    /// Thông báo khi chưa có ngày/dòng dịch vụ thực hiện trong lần khám.
    /// </summary>
    public string ProgressNote => Days.Length == 0 ? "Chưa có dịch vụ thực hiện. Chọn Thêm ngày điều trị để ghi nhận." : "";
    /// <summary>
    /// Tổng chi phí dự kiến của các kế hoạch thuộc lần khám, định dạng tiền để hiển thị.
    /// </summary>
    public string EstimatedTotal => TreatmentMoney.Format(Plans.Sum(plan => plan.Amount));
    /// <summary>
    /// Tổng chi phí thực hiện của các dòng chưa hủy thuộc lần khám.
    /// </summary>
    public string ActualTotal => TreatmentMoney.Format(Days.SelectMany(day => day.Rows).Where(row => row.Status != "Hủy").Sum(row => row.Amount));
    /// <summary>
    /// Tổng chi phí các dịch vụ đã xác nhận thuộc lần khám.
    /// </summary>
    public string CompletedTotal => TreatmentMoney.Format(Days.SelectMany(day => day.Rows).Where(row => row.Status == "Đã xác nhận").Sum(row => row.Amount));
    /// <summary>
    /// Tạo đối tượng TreatmentVisit. Thông tin lần khám kèm nháp đang sửa và snapshot gốc; gom tình trạng, kế hoạch, dịch vụ theo ngày để binding và giữ nháp khi làm mới.
    /// </summary>
    public TreatmentVisit(object record)
    {
        Record = record; OriginalValues=DuLieuGiaoDien.Values(record);doctor = UiRecord.Text(record,"MaBS"); room = UiRecord.Text(record,"MaPhong");
        symptom = UiRecord.Text(record,"TrieuChung"); diagnosis = UiRecord.Text(record,"ChanDoan"); note = UiRecord.Text(record,"GhiChu"); handling = UiRecord.Text(record,"KeHoachXuLy");
        Conditions = DuLieuGiaoDien.Children("TINH_TRANG_BENH","MaLS",Code).Select(row => new TreatmentRow("TINH_TRANG_BENH",row)).ToArray();
        Plans = DuLieuGiaoDien.Children("KE_HOACH_DIEU_TRI","MaLS",Code).Select(row => new TreatmentPlan(row)).ToArray();
        Days = DuLieuGiaoDien.Children("DICH_VU_THUC_HIEN","MaLS",Code).OrderBy(row=>UiRecord.Date(row,"ThoiGianThucHien")).GroupBy(row=>UiRecord.Date(row,"ThoiGianThucHien")?.Date).Select(group => new TreatmentDay(group.Key, group)).ToArray();
    }
}
/// <summary>
/// Kế hoạch với các bước dự kiến, trạng thái/quyền sửa và tổng tiền các bước chưa hủy để hiển thị.
/// </summary>
public sealed class TreatmentPlan
{
    /// <summary>
    /// Bản ghi nguồn đã đọc để mô hình tra các trường/liên kết; không phải một bản SQL mới được tạo tự động.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Mã khóa của bản ghi dùng để hiển thị và xác định chính xác dòng đang thao tác.
    /// </summary>
    public string Code => UiRecord.Text(Record,"MaKHDT");
    /// <summary>
    /// Trạng thái hiển thị của bản ghi; với bước kế hoạch có thể được suy ra thêm từ lượng thực hiện đã xác nhận.
    /// </summary>
    public string Status => UiRecord.Text(Record,"TrangThai");
    /// <summary>
    /// Nhãn bác sĩ hoặc nhân viên thực hiện được tra từ mã liên kết trong bản ghi nguồn.
    /// </summary>
    public string Doctor => DuLieuGiaoDien.Lookup("MaBS",UiRecord.Get(Record,"MaBS"));
    /// <summary>
    /// Ghi chú của bản ghi/dòng nháp để hiển thị hoặc nhập bổ sung trước khi lưu.
    /// </summary>
    public string Note => UiRecord.Text(Record,"GhiChu");
    /// <summary>
    /// Cờ cho phép sửa dựa trên trạng thái, bản cha và các thực hiện đã chốt; dùng bật/tắt nút/ô nhập.
    /// </summary>
    public bool CanEdit => DuLieuGiaoDien.CanEdit("KE_HOACH_DIEU_TRI",Record) && Rows.All(row=>row.CanEdit);
    /// <summary>
    /// Các dòng chi tiết trong một kế hoạch hoặc ngày điều trị.
    /// </summary>
    public TreatmentRow[] Rows { get; }
    /// <summary>
    /// Thành tiền tính bằng decimal; dòng dịch vụ làm tròn số lượng nhân giá đến 2 chữ số, tổng kế hoạch loại dòng hủy.
    /// </summary>
    public decimal Amount => Status=="Hủy"?0:Rows.Where(row=>row.Status!="Hủy").Sum(row=>row.Amount);
    /// <summary>
    /// Tổng tiền dạng chuỗi đã định dạng để hiển thị; các dòng tính tiền dùng decimal trước khi chuyển thành chuỗi.
    /// </summary>
    public string Total => TreatmentMoney.Format(Amount);
    /// <summary>
    /// Tạo đối tượng TreatmentPlan. Kế hoạch với các bước dự kiến, trạng thái/quyền sửa và tổng tiền các bước chưa hủy để hiển thị.
    /// </summary>
    public TreatmentPlan(object record) { Record=record; Rows=DuLieuGiaoDien.Children("CHI_TIET_KE_HOACH","MaKHDT",Code).Select(row=>new TreatmentRow("CHI_TIET_KE_HOACH",row)).ToArray(); }
}
/// <summary>
/// Nhóm các dịch vụ thực tế theo một ngày khám/điều trị, cùng tổng tiền những dòng chưa hủy.
/// </summary>
public sealed class TreatmentDay
{
    /// <summary>
    /// Ngày/giờ của dòng nghiệp vụ được định dạng để hiển thị; nội dung mặc định cho biết chưa có thời gian.
    /// </summary>
    public string When { get; }
    /// <summary>
    /// Các dòng chi tiết trong một kế hoạch hoặc ngày điều trị.
    /// </summary>
    public TreatmentRow[] Rows { get; }
    /// <summary>
    /// Tổng tiền dạng chuỗi đã định dạng để hiển thị; các dòng tính tiền dùng decimal trước khi chuyển thành chuỗi.
    /// </summary>
    public string Total => TreatmentMoney.Format(Rows.Where(row=>row.Status!="Hủy").Sum(row=>row.Amount));
    /// <summary>
    /// Tạo đối tượng TreatmentDay. Nhóm các dịch vụ thực tế theo một ngày khám/điều trị, cùng tổng tiền những dòng chưa hủy.
    /// </summary>
    public TreatmentDay(DateTime? date,IEnumerable<object> rows) { When=date?.ToString("dd/MM/yyyy")??"Chưa ghi ngày";Rows=rows.Select(row=>new TreatmentRow("DICH_VU_THUC_HIEN",row)).ToArray(); }
}
/// <summary>
/// Mô hình trình bày tình trạng, bước hoặc thực hiện: tra danh mục, tính tiền/lượng và khóa sửa bước đã có thực hiện xác nhận.
/// </summary>
public sealed class TreatmentRow
{
    /// <summary>
    /// Bản ghi nguồn đã đọc để mô hình tra các trường/liên kết; không phải một bản SQL mới được tạo tự động.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Tên bảng/loại nghiệp vụ nguồn để đọc trường và mở biểu mẫu đúng loại.
    /// </summary>
    public string Table { get; }
    /// <summary>
    /// Cờ cho phép sửa dựa trên trạng thái, bản cha và các thực hiện đã chốt; dùng bật/tắt nút/ô nhập.
    /// </summary>
    public bool CanEdit => DuLieuGiaoDien.CanEdit(Table,Record) && !HasCompletedExecution;
    /// <summary>
    /// Cho biết bước đã có ít nhất một dịch vụ thực hiện xác nhận, dùng chặn sửa nội dung bước nguồn.
    /// </summary>
    public bool HasCompletedExecution => Table=="CHI_TIET_KE_HOACH" && DuLieuGiaoDien.Children("DICH_VU_THUC_HIEN","MaCTKH",UiRecord.Get(Record,"MaCTKH")).Any(row=>UiRecord.Text(row,"TrangThai")=="Đã xác nhận");
    /// <summary>
    /// Cờ cho phép tạo thực hiện từ bước kế hoạch khi bước chưa hủy/hoàn thành và các điều kiện nguồn phù hợp.
    /// </summary>
    public bool CanExecute => Table=="CHI_TIET_KE_HOACH" && Status is not "Hoàn thành" and not "Hủy";
    /// <summary>
    /// Mã khóa của bản ghi dùng để hiển thị và xác định chính xác dòng đang thao tác.
    /// </summary>
    public string Code => UiRecord.Text(Record,DuLieuGiaoDien.Key(Table));
    /// <summary>
    /// Nhãn dịch vụ liên kết của dòng hoặc lịch hẹn.
    /// </summary>
    public string Service => DuLieuGiaoDien.Lookup("MaDV",UiRecord.Get(Record,"MaDV"));
    /// <summary>
    /// Nhãn bác sĩ hoặc nhân viên thực hiện được tra từ mã liên kết trong bản ghi nguồn.
    /// </summary>
    public string Doctor => DuLieuGiaoDien.Lookup("MaNVThucHien",UiRecord.Get(Record,"MaNVThucHien"));
    /// <summary>
    /// Trạng thái hiển thị của bản ghi; với bước kế hoạch có thể được suy ra thêm từ lượng thực hiện đã xác nhận.
    /// </summary>
    public string Status
    {
        get
        {
            var state=UiRecord.Text(Record,"TrangThai");
            if(Table!="CHI_TIET_KE_HOACH"||state=="Hủy")return state;
            var completed=DuLieuGiaoDien.Children("DICH_VU_THUC_HIEN","MaCTKH",UiRecord.Get(Record,"MaCTKH"))
                .Where(row=>UiRecord.Text(row,"TrangThai")=="Đã xác nhận").Sum(row=>TreatmentMoney.Number(row,"SoLuong"));
            return QuyTacHienThi.PlanStepStatus(state,Quantity,completed);
        }
    }
    /// <summary>
    /// Loại tình trạng bệnh đọc từ LoaiTinhTrang của dòng nguồn.
    /// </summary>
    public string Type => UiRecord.Text(Record,"LoaiTinhTrang");
    /// <summary>
    /// Mô tả tình trạng hoặc dịch vụ để hiển thị và tìm kiếm.
    /// </summary>
    public string Description => UiRecord.Text(Record,"MoTaTinhTrang");
    /// <summary>
    /// Ghi chú của bản ghi/dòng nháp để hiển thị hoặc nhập bổ sung trước khi lưu.
    /// </summary>
    public string Note => UiRecord.Text(Record,"GhiChu");
    /// <summary>
    /// Kết quả thực hiện dịch vụ đã ghi để đọc trong lần khám.
    /// </summary>
    public string Result => UiRecord.Text(Record,"KetQua");
    /// <summary>
    /// Số lần/bước điều trị trong kế hoạch.
    /// </summary>
    public string Sequence => UiRecord.Text(Record,"LanThu");
    /// <summary>
    /// Mã bước kế hoạch liên kết với dịch vụ thực hiện hoặc dòng đang xem.
    /// </summary>
    public string Step => UiRecord.Text(Record,"MaCTKH");
    /// <summary>
    /// Giờ thực hiện hoặc khoảng giờ lịch hẹn được định dạng cho giao diện.
    /// </summary>
    public string Time => UiRecord.Date(Record,"ThoiGianThucHien")?.ToString("HH:mm")??"";
    /// <summary>
    /// Số lượng của dòng nháp/chi tiết; thay đổi ở TreatmentDraft sẽ phát thông báo để tính lại tổng.
    /// </summary>
    public decimal Quantity => TreatmentMoney.Number(Record,Table=="CHI_TIET_KE_HOACH"?"SoLuongDuKien":"SoLuong");
    /// <summary>
    /// Đơn giá của dòng hoặc giá dịch vụ hiển thị; dữ liệu nháp tính tiền bằng decimal.
    /// </summary>
    public decimal Price => TreatmentMoney.Number(Record,Table=="CHI_TIET_KE_HOACH"?"DonGiaDuKien":"DonGia");
    /// <summary>
    /// Thành tiền tính bằng decimal; dòng dịch vụ làm tròn số lượng nhân giá đến 2 chữ số, tổng kế hoạch loại dòng hủy.
    /// </summary>
    public decimal Amount => decimal.Round(Quantity*Price,2,MidpointRounding.AwayFromZero);
    /// <summary>
    /// Đơn giá dạng chuỗi định dạng vi-VN để hiển thị.
    /// </summary>
    public string PriceText => TreatmentMoney.Format(Price);
    /// <summary>
    /// Thành tiền dạng chuỗi định dạng vi-VN để hiển thị.
    /// </summary>
    public string AmountText => TreatmentMoney.Format(Amount);
    /// <summary>
    /// Tạo đối tượng TreatmentRow. Mô hình trình bày tình trạng, bước hoặc thực hiện: tra danh mục, tính tiền/lượng và khóa sửa bước đã có thực hiện xác nhận.
    /// </summary>
    public TreatmentRow(string table,object record){Table=table;Record=record;}
}
/// <summary>
/// Chuyển giá trị số trong bản ghi và định dạng tiền vi-VN cho giao diện; tiền vẫn dùng decimal khi tính.
/// </summary>
public static class TreatmentMoney
{
    /// <summary>
    /// Đọc trường số theo culture hiện tại để tính tổng hiển thị; giá trị không phân tích được được xem là 0 tại tầng trình bày.
    /// </summary>
    public static decimal Number(object row,string field)=>decimal.TryParse(UiRecord.Text(row,field),NumberStyles.Number,CultureInfo.CurrentCulture,out var value)?value:0;
    /// <summary>
    /// Định dạng tiền theo vi-VN, bỏ phần lẻ khi là số nguyên hoặc hiện 2 chữ số thập phân, thêm ký hiệu đ.
    /// </summary>
    public static string Format(decimal value)=>value.ToString(value==decimal.Truncate(value)?"N0":"N2",CultureInfo.GetCultureInfo("vi-VN"))+" đ";
}
/// <summary>
/// Dòng dịch vụ nháp có INotifyPropertyChanged để cập nhật thành tiền sau sửa số lượng/giá; giữ thông tin liên kết và người thực hiện trước khi lưu.
/// </summary>
public sealed class TreatmentDraft : INotifyPropertyChanged
{
    /// <summary>
    /// Thông báo thay đổi thuộc tính cho các thành phần đã đăng ký theo dõi.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>
    /// Tham chiếu bản ghi cũ của dòng nháp; null nghĩa là thêm mới, có giá trị nghĩa là sửa dòng đang có.
    /// </summary>
    public object? Existing { get; init; }
    /// <summary>
    /// Mã dịch vụ. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string MaDV { get; init; }="";
    /// <summary>
    /// Nhãn dễ đọc cho lựa chọn/dòng dữ liệu; với ô lịch là mô tả hành động tạo lịch.
    /// </summary>
    public string Label { get; init; }="";
    /// <summary>
    /// Mã bước chi tiết kế hoạch. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string? MaCTKH { get; set; }
    /// <summary>
    /// Mã nhân viên thực hiện dịch vụ. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public string? MaNVThucHien { get; set; }
    /// <summary>
    /// Số thứ tự lần/bước điều trị. Giá trị phục vụ binding/nhập liệu; được ghi SQL khi người dùng lưu form.
    /// </summary>
    public int LanThu { get; set; }=1;
    /// <summary>
    /// Ghi chú của bản ghi/dòng nháp để hiển thị hoặc nhập bổ sung trước khi lưu.
    /// </summary>
    public string Note { get; set; }="";
    /// <summary>
    /// Trạng thái hiển thị của bản ghi; với bước kế hoạch có thể được suy ra thêm từ lượng thực hiện đã xác nhận.
    /// </summary>
    public string Status { get; set; }="Nháp";
    /// <summary>
    /// Giá trị lưu nội bộ của thuộc tính Quantity, dùng bởi getter/setter tương ứng.
    /// </summary>
    private decimal quantity=1,price;
    /// <summary>
    /// Số lượng của dòng nháp/chi tiết; thay đổi ở TreatmentDraft sẽ phát thông báo để tính lại tổng.
    /// </summary>
    public decimal Quantity { get=>quantity;set{quantity=value;Changed();Changed(nameof(Total));} }
    /// <summary>
    /// Đơn giá của dòng hoặc giá dịch vụ hiển thị; dữ liệu nháp tính tiền bằng decimal.
    /// </summary>
    public decimal Price { get=>price;set{price=value;Changed();Changed(nameof(Total));} }
    /// <summary>
    /// Thành tiền tính bằng decimal; dòng dịch vụ làm tròn số lượng nhân giá đến 2 chữ số, tổng kế hoạch loại dòng hủy.
    /// </summary>
    public decimal Amount=>decimal.Round(Quantity*Price,2,MidpointRounding.AwayFromZero);
    /// <summary>
    /// Tổng tiền dạng chuỗi đã định dạng để hiển thị; các dòng tính tiền dùng decimal trước khi chuyển thành chuỗi.
    /// </summary>
    // AI bổ sung: nhập số rất lớn không làm getter binding phát lỗi tràn số ra màn hình.
    public string Total {get {try{return TreatmentMoney.Format(Amount);}catch(OverflowException){return "Vượt giới hạn số tiền";}}}
    /// <summary>
    /// Lựa chọn nhân viên thực hiện từ danh mục nhân viên trong cache.
    /// </summary>
    public IEnumerable<LuaChonDanhMuc> Staff=>DuLieuGiaoDien.Options("NHAN_VIEN");
    /// <summary>
    /// Các trạng thái có thể chọn trên dòng dịch vụ nháp.
    /// </summary>
    public string[] States => new[]{"Nháp","Đã xác nhận","Hủy","Ngừng áp dụng"};
    /// <summary>
    /// Phát PropertyChanged để WPF cập nhật các binding của dòng nháp và thành tiền khi số lượng/đơn giá thay đổi.
    /// </summary>
    private void Changed([CallerMemberName]string? name=null)=>PropertyChanged?.Invoke(this,new(name));
}
/// <summary>
/// Một dịch vụ trong lớp chọn danh mục, gồm mã/tên/nhóm/mô tả/giá và cờ đánh dấu Selected để thêm nhiều dịch vụ.
/// </summary>
public sealed class TreatmentServiceChoice
{
    /// <summary>
    /// Bản ghi nguồn đã đọc để mô hình tra các trường/liên kết; không phải một bản SQL mới được tạo tự động.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Dịch vụ/dòng đang chọn hoặc cờ đánh dấu dịch vụ trong danh mục trước khi thêm vào nháp.
    /// </summary>
    public bool Selected { get; set; }
    /// <summary>
    /// Mã khóa của bản ghi dùng để hiển thị và xác định chính xác dòng đang thao tác.
    /// </summary>
    public string Code => UiRecord.Text(Record,"MaDV");
    /// <summary>
    /// Nhãn dễ đọc cho lựa chọn/dòng dữ liệu; với ô lịch là mô tả hành động tạo lịch.
    /// </summary>
    public string Label => UiRecord.Text(Record,"TenDV");
    /// <summary>
    /// Nhóm dịch vụ dùng cho bộ lọc trong lớp chọn dịch vụ.
    /// </summary>
    public string Group => UiRecord.Text(Record,"NhomDV");
    /// <summary>
    /// Mô tả tình trạng hoặc dịch vụ để hiển thị và tìm kiếm.
    /// </summary>
    public string Description => UiRecord.Text(Record,"MoTa");
    /// <summary>
    /// Đơn giá của dòng hoặc giá dịch vụ hiển thị; dữ liệu nháp tính tiền bằng decimal.
    /// </summary>
    public string Price => TreatmentMoney.Format(TreatmentMoney.Number(Record,"DonGia"));
    /// <summary>
    /// Tạo đối tượng TreatmentServiceChoice. Một dịch vụ trong lớp chọn danh mục, gồm mã/tên/nhóm/mô tả/giá và cờ đánh dấu Selected để thêm nhiều dịch vụ.
    /// </summary>
    public TreatmentServiceChoice(object record){Record=record;}
}
