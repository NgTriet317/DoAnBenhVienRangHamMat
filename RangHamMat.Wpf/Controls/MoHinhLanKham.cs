// PHẦN AI ĐÃ THỰC HIỆN: AI đã bổ sung chú thích tiếng Việt cho MoHinhLanKham.cs: Tổng hợp một lần khám gồm nháp thông tin khám, tiếp nhận, tình trạng, các kế hoạch cùng bệnh nhân, dịch vụ và đơn thuốc để binding màn hình chi tiết. Giải thích được đặt cạnh khai báo/hàm tương ứng.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
namespace RangHamMat.Controls;

// Các mô hình hiển thị cung cấp trường dữ liệu cần cho màn hình chi tiết lần khám.
/// <summary>
/// Tổng hợp một lần khám gồm nháp thông tin khám, tiếp nhận, tình trạng, các kế hoạch cùng bệnh nhân, dịch vụ và đơn thuốc để binding màn hình chi tiết.
/// </summary>
public sealed class TongHopLanKham
{
    /// <summary>
    /// Bản ghi nguồn đã đọc để mô hình tra các trường/liên kết; không phải một bản SQL mới được tạo tự động.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Lần khám nguồn của dữ liệu đang xem; dùng để mở lại lần khám hoặc tổng hợp thông tin điều trị.
    /// </summary>
    public TreatmentVisit Visit { get; }
    /// <summary>
    /// Mã khóa của bản ghi dùng để hiển thị và xác định chính xác dòng đang thao tác.
    /// </summary>
    public string Code => Visit.Code;
    /// <summary>
    /// Ngày/giờ của dòng nghiệp vụ được định dạng để hiển thị; nội dung mặc định cho biết chưa có thời gian.
    /// </summary>
    public string When => Visit.When;
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string PatientId => DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", Record);
    /// <summary>
    /// Thông tin hoặc nhãn bệnh nhân liên kết với bản ghi/hồ sơ đang xem.
    /// </summary>
    public string Patient => Visit.Patient;
    /// <summary>
    /// Trạng thái hiển thị của bản ghi; với bước kế hoạch có thể được suy ra thêm từ lượng thực hiện đã xác nhận.
    /// </summary>
    public string Status => Visit.Status;
    /// <summary>
    /// Nhãn bác sĩ hoặc nhân viên thực hiện được tra từ mã liên kết trong bản ghi nguồn.
    /// </summary>
    public string Doctor => DuLieuGiaoDien.Lookup("MaBS", UiRecord.Get(Record, "MaBS"));
    /// <summary>
    /// Nhãn phòng khám được tra từ danh mục theo mã phòng của bản ghi.
    /// </summary>
    public string Room => DuLieuGiaoDien.Lookup("MaPhong", UiRecord.Get(Record, "MaPhong"));
    /// <summary>
    /// Mã hoặc nhãn lượt tiếp nhận liên kết với lần khám.
    /// </summary>
    public string Receipt => UiRecord.Text(Record, "MaPhieu");
    /// <summary>
    /// Thời gian tiếp nhận của lần khám, định dạng để kiểm tra trình tự đến khám.
    /// </summary>
    public string ReceiptTime { get; }
    /// <summary>
    /// Lý do nghiệp vụ đọc từ tiếp nhận, lịch hẹn hoặc đơn thuốc nguồn.
    /// </summary>
    public string Reason { get; }
    /// <summary>
    /// Ngày/giờ xác nhận hoặc thông báo Chưa xác nhận, dùng cho thông tin trạng thái trên màn hình.
    /// </summary>
    public string Confirmation => UiRecord.Date(Record, "NgayXacNhan")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa xác nhận";
    /// <summary>
    /// Các dòng tình trạng bệnh gắn với lần khám hiện tại.
    /// </summary>
    public DongChiTietLanKham[] Conditions { get; }
    /// <summary>
    /// Các kế hoạch điều trị liên quan được mô hình tổng hợp để binding giao diện.
    /// </summary>
    public KeHoachLanKham[] Plans { get; }
    /// <summary>
    /// Các dịch vụ thực hiện của lần khám hiện tại.
    /// </summary>
    public DongChiTietLanKham[] Services { get; }
    /// <summary>
    /// Các dịch vụ thực hiện không có mã bước kế hoạch, hiển thị riêng như khoản phát sinh ngoài kế hoạch.
    /// </summary>
    public DongChiTietLanKham[] OutsidePlan => Services.Where(row => string.IsNullOrEmpty(row.Step)).ToArray();
    /// <summary>
    /// Các đơn thuốc gắn với lần khám hiện tại để mở đúng chi tiết kê đơn.
    /// </summary>
    public PrescriptionCard[] Orders { get; }
    /// <summary>
    /// Thông báo trạng thái trống khi chưa ghi nhận tình trạng bệnh.
    /// </summary>
    public string ConditionsNote => Conditions.Length == 0 ? "Chưa ghi nhận tình trạng bệnh." : "";
    /// <summary>
    /// Thông báo trạng thái trống khi chưa có kế hoạch điều trị.
    /// </summary>
    public string PlansNote => Plans.Length == 0 ? "Chưa lập kế hoạch điều trị." : "";
    /// <summary>
    /// Thông báo trạng thái trống khi chưa có dịch vụ thực hiện.
    /// </summary>
    public string ServicesNote => Services.Length == 0 ? "Chưa ghi nhận dịch vụ tại lần khám này." : "";
    /// <summary>
    /// Thông báo khi không có dịch vụ ngoài kế hoạch ở lần khám hiện tại.
    /// </summary>
    public string OutsideNote => OutsidePlan.Length == 0 ? "Không có dịch vụ phát sinh ngoài kế hoạch tại lần khám này." : "";
    /// <summary>
    /// Thông báo khi lần khám chưa có đơn thuốc.
    /// </summary>
    public string OrdersNote => Orders.Length == 0 ? "Chưa có đơn thuốc tại lần khám này." : "";
    /// <summary>
    /// Tạo đối tượng TongHopLanKham. Tổng hợp một lần khám gồm nháp thông tin khám, tiếp nhận, tình trạng, các kế hoạch cùng bệnh nhân, dịch vụ và đơn thuốc để binding màn hình chi tiết.
    /// </summary>
    public TongHopLanKham(object record, TreatmentVisit? draft = null)
    {
        Record = record; Visit = new(record); if (draft is not null) Visit.CopyDraftFrom(draft);
        var receipt = DuLieuGiaoDien.Find("PHIEU_KHAM_BENH", UiRecord.Get(record, "MaPhieu"));
        ReceiptTime = receipt is null ? "Chưa có thông tin" : UiRecord.Date(receipt, "ThoiGianTiepNhan")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa có thời gian";
        Reason = receipt is null ? "" : UiRecord.Text(receipt, "LyDoKham");
        Conditions = DuLieuGiaoDien.Children("TINH_TRANG_BENH", "MaLS", Code).Select(row => new DongChiTietLanKham("TINH_TRANG_BENH", row)).ToArray();
        Plans = DuLieuGiaoDien.Rows("KE_HOACH_DIEU_TRI").Where(row => PatientId.Length > 0 && DuLieuGiaoDien.PatientId("KE_HOACH_DIEU_TRI", row) == PatientId)
            .OrderByDescending(row => UiRecord.Text(row, "MaLS") == Code).ThenByDescending(row => UiRecord.Date(row, "NgayLap")).Select(row => new KeHoachLanKham(row)).ToArray();
        Services = DuLieuGiaoDien.Children("DICH_VU_THUC_HIEN", "MaLS", Code).OrderBy(row => UiRecord.Date(row, "ThoiGianThucHien")).Select(row => new DongChiTietLanKham("DICH_VU_THUC_HIEN", row)).ToArray();
        Orders = DuLieuGiaoDien.Children("DON_THUOC", "MaLS", Code).OrderByDescending(row => UiRecord.Date(row, "NgayKe")).Select(row => new PrescriptionCard(row)).ToArray();
    }
}
/// <summary>
/// Mô hình kế hoạch gồm bước và các lần thực hiện ở bất kỳ lần khám nào của cùng bệnh nhân; tính tổng và khả năng sửa/xác nhận.
/// </summary>
public sealed class KeHoachLanKham
{
    /// <summary>
    /// Bản ghi nguồn đã đọc để mô hình tra các trường/liên kết; không phải một bản SQL mới được tạo tự động.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Mã khóa của bản ghi dùng để hiển thị và xác định chính xác dòng đang thao tác.
    /// </summary>
    public string Code => UiRecord.Text(Record, "MaKHDT");
    /// <summary>
    /// Trạng thái hiển thị của bản ghi; với bước kế hoạch có thể được suy ra thêm từ lượng thực hiện đã xác nhận.
    /// </summary>
    public string Status => UiRecord.Text(Record, "TrangThai");
    /// <summary>
    /// Nhãn bác sĩ hoặc nhân viên thực hiện được tra từ mã liên kết trong bản ghi nguồn.
    /// </summary>
    public string Doctor => DuLieuGiaoDien.Lookup("MaBS", UiRecord.Get(Record, "MaBS"));
    /// <summary>
    /// Ngày lập kế hoạch được định dạng cho vùng chi tiết.
    /// </summary>
    public string Created => UiRecord.Date(Record, "NgayLap")?.ToString("dd/MM/yyyy") ?? "Chưa có ngày lập";
    /// <summary>
    /// Nhãn mã lần khám gốc nơi kế hoạch được lập.
    /// </summary>
    public string OriginVisit => "Lần khám " + UiRecord.Text(Record, "MaLS");
    /// <summary>
    /// Thời điểm bắt đầu lịch hẹn hoặc ngày dự kiến bắt đầu kế hoạch, tùy mô hình hiển thị.
    /// </summary>
    public string Start => UiRecord.Date(Record, "NgayBatDau")?.ToString("dd/MM/yyyy") ?? "Chưa dự kiến";
    /// <summary>
    /// Thời điểm kết thúc lịch hẹn hoặc ngày dự kiến kết thúc kế hoạch.
    /// </summary>
    public string End => UiRecord.Date(Record, "NgayDuKienKetThuc")?.ToString("dd/MM/yyyy") ?? "Chưa dự kiến";
    /// <summary>
    /// Ghi chú của bản ghi/dòng nháp để hiển thị hoặc nhập bổ sung trước khi lưu.
    /// </summary>
    public string Note => UiRecord.Text(Record, "GhiChu");
    /// <summary>
    /// Cờ cho phép sửa dựa trên trạng thái, bản cha và các thực hiện đã chốt; dùng bật/tắt nút/ô nhập.
    /// </summary>
    public bool CanEdit => DuLieuGiaoDien.CanEdit("KE_HOACH_DIEU_TRI", Record) && Steps.All(row => row.CanEdit);
    /// <summary>
    /// Cờ cho phép xác nhận khi bản nháp còn được sửa và có đủ bước cần thiết.
    /// </summary>
    public bool CanConfirm => DuLieuGiaoDien.CanEdit("KE_HOACH_DIEU_TRI", Record) && Steps.Length > 0;
    /// <summary>
    /// Các bước chi tiết thuộc kế hoạch hiện tại, dùng để chọn và theo dõi tiến độ.
    /// </summary>
    public DongChiTietLanKham[] Steps { get; }
    /// <summary>
    /// Các dịch vụ đã thực hiện liên kết với các bước của kế hoạch và cùng bệnh nhân.
    /// </summary>
    public DongChiTietLanKham[] Executions { get; }
    /// <summary>
    /// Tổng tiền dạng chuỗi đã định dạng để hiển thị; các dòng tính tiền dùng decimal trước khi chuyển thành chuỗi.
    /// </summary>
    public string Total => TreatmentMoney.Format(Steps.Where(row => row.Status != "Hủy").Sum(row => row.Amount));
    /// <summary>
    /// Thông báo khi kế hoạch chưa có bước điều trị, hướng dẫn thêm bước trước khi xác nhận.
    /// </summary>
    public string StepsNote => Steps.Length == 0 ? "Chưa có bước điều trị. Thêm bước trước khi xác nhận kế hoạch." : "";
    /// <summary>
    /// Thông báo khi kế hoạch chưa có dịch vụ thực hiện liên kết.
    /// </summary>
    public string ExecutionsNote => Executions.Length == 0 ? "Chưa ghi nhận thực hiện cho kế hoạch này." : "";
    /// <summary>
    /// Tạo đối tượng KeHoachLanKham. Mô hình kế hoạch gồm bước và các lần thực hiện ở bất kỳ lần khám nào của cùng bệnh nhân; tính tổng và khả năng sửa/xác nhận.
    /// </summary>
    public KeHoachLanKham(object record)
    {
        Record = record;
        Steps = DuLieuGiaoDien.Children("CHI_TIET_KE_HOACH", "MaKHDT", Code).OrderBy(row => TreatmentMoney.Number(row, "LanThu")).Select(row => new DongChiTietLanKham("CHI_TIET_KE_HOACH", row)).ToArray();
        var stepIds = Steps.Select(row => row.Code).ToHashSet(StringComparer.Ordinal);
        var patientId = DuLieuGiaoDien.PatientId("KE_HOACH_DIEU_TRI", record);
        // Kế hoạch có thể thực hiện ở lần khám khác; chỉ lấy các thực hiện thuộc cùng bệnh nhân.
        Executions = DuLieuGiaoDien.Rows("DICH_VU_THUC_HIEN").Where(row => stepIds.Contains(UiRecord.Text(row, "MaCTKH")) && patientId.Length > 0 && DuLieuGiaoDien.PatientId("DICH_VU_THUC_HIEN", row) == patientId).OrderBy(row => UiRecord.Date(row, "ThoiGianThucHien")).Select(row => new DongChiTietLanKham("DICH_VU_THUC_HIEN", row)).ToArray();
    }
}
/// <summary>
/// Mô hình dòng tình trạng/bước/dịch vụ cho chi tiết lần khám: lấy nhãn, tính lượng đã làm/còn lại, trạng thái tiến độ và thành tiền.
/// </summary>
public sealed class DongChiTietLanKham
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
    /// Mã khóa của bản ghi dùng để hiển thị và xác định chính xác dòng đang thao tác.
    /// </summary>
    public string Code => UiRecord.Text(Record, DuLieuGiaoDien.Key(Table));
    /// <summary>
    /// Mã lần khám chứa dòng dịch vụ/chi tiết để điều hướng đến nơi thực hiện thực tế.
    /// </summary>
    public string VisitId => UiRecord.Text(Record, "MaLS");
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string PatientId => DuLieuGiaoDien.PatientId(Table, Record);
    /// <summary>
    /// Thông tin hoặc nhãn bệnh nhân liên kết với bản ghi/hồ sơ đang xem.
    /// </summary>
    public string Patient => DuLieuGiaoDien.Lookup("MaBN",PatientId);
    /// <summary>
    /// Mã bước kế hoạch liên kết với dịch vụ thực hiện hoặc dòng đang xem.
    /// </summary>
    public string Step => UiRecord.Text(Record, "MaCTKH");
    /// <summary>
    /// Loại tình trạng bệnh đọc từ LoaiTinhTrang của dòng nguồn.
    /// </summary>
    public string Type => UiRecord.Text(Record, "LoaiTinhTrang");
    /// <summary>
    /// Mô tả tình trạng hoặc dịch vụ để hiển thị và tìm kiếm.
    /// </summary>
    public string Description => UiRecord.Text(Record, "MoTaTinhTrang");
    /// <summary>
    /// Ghi chú của bản ghi/dòng nháp để hiển thị hoặc nhập bổ sung trước khi lưu.
    /// </summary>
    public string Note => UiRecord.Text(Record, "GhiChu");
    /// <summary>
    /// Nhãn dịch vụ liên kết của dòng hoặc lịch hẹn.
    /// </summary>
    public string Service => DuLieuGiaoDien.Lookup("MaDV", UiRecord.Get(Record, "MaDV"));
    /// <summary>
    /// Nhãn bác sĩ hoặc nhân viên thực hiện được tra từ mã liên kết trong bản ghi nguồn.
    /// </summary>
    public string Doctor => DuLieuGiaoDien.Lookup("MaNVThucHien", UiRecord.Get(Record, "MaNVThucHien"));
    /// <summary>
    /// Ngày/giờ của dòng nghiệp vụ được định dạng để hiển thị; nội dung mặc định cho biết chưa có thời gian.
    /// </summary>
    public string When => UiRecord.Date(Record, "ThoiGianThucHien")?.ToString("dd/MM/yyyy HH:mm") ?? "";
    /// <summary>
    /// Kết quả thực hiện dịch vụ đã ghi để đọc trong lần khám.
    /// </summary>
    public string Result => UiRecord.Text(Record, "KetQua");
    /// <summary>
    /// Số lần/bước điều trị trong kế hoạch.
    /// </summary>
    public string Sequence => UiRecord.Text(Record, "LanThu");
    /// <summary>
    /// Tổng lượng dịch vụ đã xác nhận cho bước kế hoạch của đúng bệnh nhân; không cộng các dòng nháp/hủy.
    /// </summary>
    public decimal CompletedQuantity => Table == "CHI_TIET_KE_HOACH" ? DuLieuGiaoDien.Children("DICH_VU_THUC_HIEN", "MaCTKH", Code)
        .Where(row => UiRecord.Text(row, "TrangThai") == "Đã xác nhận" && DuLieuGiaoDien.PatientId("DICH_VU_THUC_HIEN", row) == PatientId)
        .Sum(row => TreatmentMoney.Number(row, "SoLuong")) : 0;
    /// <summary>
    /// Lượng dự kiến còn lại sau khi trừ lượng đã thực hiện, chặn giá trị âm bằng 0.
    /// </summary>
    public decimal RemainingQuantity => Math.Max(0, Quantity - CompletedQuantity);
    /// <summary>
    /// Trạng thái hiển thị của bản ghi; với bước kế hoạch có thể được suy ra thêm từ lượng thực hiện đã xác nhận.
    /// </summary>
    public string Status => Table == "CHI_TIET_KE_HOACH" ? QuyTacHienThi.PlanStepStatus(UiRecord.Text(Record, "TrangThai"), Quantity, CompletedQuantity) : UiRecord.Text(Record, "TrangThai");
    /// <summary>
    /// Số lượng của dòng nháp/chi tiết; thay đổi ở TreatmentDraft sẽ phát thông báo để tính lại tổng.
    /// </summary>
    public decimal Quantity => TreatmentMoney.Number(Record, Table == "CHI_TIET_KE_HOACH" ? "SoLuongDuKien" : "SoLuong");
    /// <summary>
    /// Đơn giá của dòng hoặc giá dịch vụ hiển thị; dữ liệu nháp tính tiền bằng decimal.
    /// </summary>
    public decimal Price => TreatmentMoney.Number(Record, Table == "CHI_TIET_KE_HOACH" ? "DonGiaDuKien" : "DonGia");
    /// <summary>
    /// Thành tiền tính bằng decimal; dòng dịch vụ làm tròn số lượng nhân giá đến 2 chữ số, tổng kế hoạch loại dòng hủy.
    /// </summary>
    public decimal Amount => decimal.Round(Quantity * Price,2,MidpointRounding.AwayFromZero);
    /// <summary>
    /// Đơn giá dạng chuỗi định dạng vi-VN để hiển thị.
    /// </summary>
    public string PriceText => TreatmentMoney.Format(Price);
    /// <summary>
    /// Thành tiền dạng chuỗi định dạng vi-VN để hiển thị.
    /// </summary>
    public string AmountText => TreatmentMoney.Format(Amount);
    /// <summary>
    /// Cờ cho phép sửa dựa trên trạng thái, bản cha và các thực hiện đã chốt; dùng bật/tắt nút/ô nhập.
    /// </summary>
    public bool CanEdit => DuLieuGiaoDien.CanEdit(Table, Record);
    /// <summary>
    /// Cờ cho phép tạo thực hiện từ bước kế hoạch khi bước chưa hủy/hoàn thành và các điều kiện nguồn phù hợp.
    /// </summary>
    public bool CanExecute => Table == "CHI_TIET_KE_HOACH" && (Status is not "Hủy" and not "Hoàn thành") &&
        DuLieuGiaoDien.Find("KE_HOACH_DIEU_TRI", UiRecord.Get(Record, "MaKHDT")) is object plan && (UiRecord.Text(plan, "TrangThai") is not "Hủy" and not "Hoàn thành");
    /// <summary>
    /// Tạo đối tượng DongChiTietLanKham. Mô hình dòng tình trạng/bước/dịch vụ cho chi tiết lần khám: lấy nhãn, tính lượng đã làm/còn lại, trạng thái tiến độ và thành tiền.
    /// </summary>
    public DongChiTietLanKham(string table, object record) { Table = table; Record = record; }
}
/// <summary>
/// Mô hình gọn cho danh sách lần khám gồm mã, bệnh nhân, giờ khám, bác sĩ, chẩn đoán và trạng thái; giữ bản ghi nguồn để mở chi tiết.
/// </summary>
public sealed class MucDanhSachLanKham
{
    /// <summary>
    /// Bản ghi nguồn đã đọc để mô hình tra các trường/liên kết; không phải một bản SQL mới được tạo tự động.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Mã khóa của bản ghi dùng để hiển thị và xác định chính xác dòng đang thao tác.
    /// </summary>
    public string Code => UiRecord.Text(Record, "MaLS");
    /// <summary>
    /// Mã bệnh nhân nguồn để giới hạn dữ liệu và giữ đúng hồ sơ khi mở form hoặc chuyển phân hệ.
    /// </summary>
    public string PatientId => UiRecord.Text(Record,"MaBN") is {Length:>0} id?id:DuLieuGiaoDien.PatientId("LICH_SU_KHAM_BENH", Record);
    /// <summary>
    /// Thông tin hoặc nhãn bệnh nhân liên kết với bản ghi/hồ sơ đang xem.
    /// </summary>
    public string Patient => UiRecord.Text(Record,"HoTenBN") is {Length:>0} name?$"{name} ({PatientId})":DuLieuGiaoDien.Lookup("MaBN", PatientId);
    /// <summary>
    /// Ngày/giờ của dòng nghiệp vụ được định dạng để hiển thị; nội dung mặc định cho biết chưa có thời gian.
    /// </summary>
    public string When => UiRecord.Date(Record, "ThoiGianKham")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa có thời gian";
    /// <summary>
    /// Trạng thái hiển thị của bản ghi; với bước kế hoạch có thể được suy ra thêm từ lượng thực hiện đã xác nhận.
    /// </summary>
    public string Status => UiRecord.Text(Record, "TrangThai");
    /// <summary>
    /// Chẩn đoán của lần khám nguồn để đọc cùng hồ sơ hoặc đơn thuốc.
    /// </summary>
    public string Diagnosis => UiRecord.Text(Record, "ChanDoan");
    /// <summary>
    /// Nhãn bác sĩ hoặc nhân viên thực hiện được tra từ mã liên kết trong bản ghi nguồn.
    /// </summary>
    public string Doctor => DuLieuGiaoDien.Lookup("MaBS", UiRecord.Get(Record, "MaBS"));
    /// <summary>
    /// Tạo đối tượng MucDanhSachLanKham. Mô hình gọn cho danh sách lần khám gồm mã, bệnh nhân, giờ khám, bác sĩ, chẩn đoán và trạng thái; giữ bản ghi nguồn để mở chi tiết.
    /// </summary>
    public MucDanhSachLanKham(object record) { Record = record; }
}
