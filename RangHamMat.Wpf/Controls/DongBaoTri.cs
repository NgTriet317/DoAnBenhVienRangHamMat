namespace RangHamMat.Controls;

/// <summary>Nhãn và vị trí của một thiết bị vật lý; mã và serial luôn đi cùng nhau.</summary>
public sealed class DongThietBiBaoTri
{
    /// <summary>
    /// Bản ghi.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Giá trị Mã thiết bị; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string MaTB => UiRecord.Text(Record, "MaTB");
    /// <summary>
    /// Giá trị Số serial nhận diện từng thiết bị vật lý; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string SoSerial => UiRecord.Text(Record, "SoSerial");
    /// <summary>
    /// Tên thiết bị.
    /// </summary>
    public string TenThietBi { get; }
    /// <summary>
    /// Tên phòng.
    /// </summary>
    public string TenPhong { get; }
    /// <summary>
    /// Giá trị Tình trạng; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string TinhTrang => UiRecord.Text(Record, "TinhTrang");
    /// <summary>
    /// Giá trị Nhận; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string Nhan => $"{TenThietBi} · {MaTB} · Serial: {SoSerial}";
    /// <summary>
    /// Giá trị Từ khóa tìm kiếm áp dụng cho danh sách; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string TuKhoa => string.Join(" ", MaTB, SoSerial, TenThietBi, TenPhong);
    /// <summary>
    /// Khởi tạo dòng thiết bị bảo trì.
    /// </summary>
    /// <param name="record">Giá trị bản ghi dùng khi thực hiện thao tác; kiểu object.</param>
    public DongThietBiBaoTri(object record)
    {
        Record = record;
        var hangHoa = DuLieuGiaoDien.Find("HANG_HOA", UiRecord.Get(record, "MaHH"));
        TenThietBi = hangHoa is null ? MaTB : UiRecord.Text(hangHoa, "TenHH");
        var phong = DuLieuGiaoDien.Find("PHONG", UiRecord.Get(record, "MaPhong"));
        TenPhong = phong is null ? "Chưa bố trí phòng" : UiRecord.Text(phong, "TenPhong");
    }
}

/// <summary>Dữ liệu trình bày lịch sử; giữ bản ghi nguồn để kiểm tra sửa đồng thời.</summary>
public sealed class DongBaoTri
{
    /// <summary>
    /// Bản ghi.
    /// </summary>
    public object Record { get; }
    /// <summary>
    /// Giá trị Mã bảo trì; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string MaBT => UiRecord.Text(Record, "MaBT");
    /// <summary>
    /// Giá trị Mã thiết bị; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string MaTB => UiRecord.Text(Record, "MaTB");
    /// <summary>
    /// Giá trị Ngày bắt đầu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public DateTime NgayBatDau => UiRecord.Date(Record, "NgayBatDau") ?? DateTime.MinValue;
    /// <summary>
    /// Giá trị Ngày tiếp theo; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public DateTime? NgayTiepTheo => UiRecord.Date(Record, "NgayBaoTriTiepTheo");
    /// <summary>
    /// Giá trị Chỉ phí; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public decimal ChiPhi => Convert.ToDecimal(UiRecord.Get(Record, "ChiPhi"));
    /// <summary>
    /// Giá trị Trạng thái; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string Status => UiRecord.Text(Record, "TrangThai");
    /// <summary>
    /// Giá trị Nội dung được lưu hoặc hiển thị cho mục này; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string NoiDung => UiRecord.Text(Record, "NoiDung");
    /// <summary>
    /// Giá trị Nội dung ghi chú bổ sung của bản ghi; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string GhiChu => string.IsNullOrWhiteSpace(UiRecord.Text(Record, "GhiChu")) ? "Chưa có ghi chú" : UiRecord.Text(Record, "GhiChu");
    /// <summary>
    /// Tên thiết bị.
    /// </summary>
    public string TenThietBi { get; }
    /// <summary>
    /// Số serial nhận diện từng thiết bị vật lý.
    /// </summary>
    public string SoSerial { get; }
    /// <summary>
    /// Tên phòng.
    /// </summary>
    public string TenPhong { get; }
    /// <summary>
    /// Giá trị Thiết bị đầy đủ; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string ThietBiDayDu => $"{TenThietBi} · {MaTB}";
    /// <summary>
    /// Nhân viên.
    /// </summary>
    public string NhanVien { get; }
    /// <summary>
    /// Giá trị Thời gian; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string ThoiGian => NgayBatDau.ToString("dd/MM/yyyy") + " → " + (UiRecord.Date(Record, "NgayKetThuc")?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa kết thúc");
    /// <summary>
    /// Giá trị Thời điểm xác nhận; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string ThoiDiemXacNhan => UiRecord.Date(Record, "NgayXacNhan")?.ToString("dd/MM/yyyy HH:mm") ?? "Không có dữ liệu thời điểm";
    /// <summary>
    /// Giá trị Thời điểm thực hiện; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string ThoiDiemThucHien => UiRecord.Date(Record, "NgayThucHien")?.ToString("dd/MM/yyyy HH:mm") ?? "Không có dữ liệu thời điểm";
    /// <summary>
    /// Đến hạn.
    /// </summary>
    public bool DenHan { get; set; }
    /// <summary>
    /// Giá trị Lịch tiếp theo; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string LichTiepTheo => NgayTiepTheo is null ? "Chưa lên lịch" : NgayTiepTheo.Value.ToString("dd/MM/yyyy") + (DenHan ? " · Đến / quá hạn" : "");
    /// <summary>
    /// Giá trị Quyền hoặc điều kiện cho phép sửa mục hiện tại; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public bool ChoSua => Status == "Chưa xác nhận" && DuLieuGiaoDien.CanEdit("BAO_TRI_THIET_BI", Record);
    /// <summary>
    /// Giá trị Chờ xác nhận; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public bool ChoXacNhan => ChoSua;
    /// <summary>
    /// Giá trị Quyền hoặc điều kiện cho phép hủy mục hiện tại; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public bool ChoHuy => ChoSua;
    /// <summary>
    /// Giá trị Chờ bắt đầu; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public bool ChoBatDau => Status == "Đã xác nhận";
    /// <summary>
    /// Giá trị Chờ hoàn thành; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public bool ChoHoanThanh => Status == "Đang thực hiện";
    /// <summary>
    /// Giá trị Hướng dẫn; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string HuongDan => Status switch
    {
        "Chưa xác nhận" => "Có thể sửa nội dung, xác nhận hoặc hủy phiếu.",
        "Đã xác nhận" => "Nội dung đã khóa. Bắt đầu sẽ chuyển đúng thiết bị này sang Đang bảo trì.",
        "Đang thực hiện" => "Hoàn thành sẽ ghi giờ kết thúc và chuyển thiết bị sang Đang hoạt động.",
        _ => "Phiếu đã kết thúc, chỉ xem chi tiết."
    };
    /// <summary>
    /// Giá trị Từ khóa tìm kiếm áp dụng cho danh sách; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string TuKhoa => string.Join(" ", MaBT, MaTB, SoSerial, ThietBiDayDu, NhanVien, TenPhong, NoiDung);
    /// <summary>
    /// Khởi tạo dòng bảo trì.
    /// </summary>
    /// <param name="record">Giá trị bản ghi dùng khi thực hiện thao tác; kiểu object.</param>
    public DongBaoTri(object record)
    {
        Record = record;
        var thietBi = DuLieuGiaoDien.Find("THIET_BI", MaTB);
        if (thietBi is not null)
        {
            var nhan = new DongThietBiBaoTri(thietBi);
            TenThietBi = nhan.TenThietBi; TenPhong = nhan.TenPhong; SoSerial = nhan.SoSerial;
        }
        else { TenThietBi = MaTB; TenPhong = "Chưa bố trí phòng"; SoSerial = "Chưa có serial"; }
        NhanVien = DuLieuGiaoDien.Lookup("MaNV", UiRecord.Get(record, "MaNV"));
    }
}
