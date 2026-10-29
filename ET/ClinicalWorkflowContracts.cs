namespace ET;

/// <summary>Phân biệt từ chối quyền với lỗi SQL/kết nối để không coi lỗi hệ thống là ca quyền đạt.</summary>
public sealed class TuChoiQuyenNghiepVuException : InvalidOperationException
{
    /// <summary>
    /// Khởi tạo lỗi từ chối quyền nghiệp vụ.
    /// </summary>
    public TuChoiQuyenNghiepVuException()
        : base("Không tìm thấy dữ liệu trong phạm vi được phép hoặc tài khoản không còn quyền thao tác.") { }
}

/// <summary>
/// Mô hình và thao tác hỗ trợ định danh nghiệp vụ dùng bởi các thành phần liên quan.
/// </summary>
public static class DinhDanhNghiepVu
{
    /// <summary>
    /// Kiểu xác thực được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    public const string KieuXacThuc = "RHM.Desktop";
    /// <summary>
    /// Mã nhân viên lấy từ thông tin xác thực của phiên được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    public const string MaNhanVienClaim = "RHM.MaNV";
}

/// <summary>Quyền nghiệp vụ cho sáu miền lâm sàng; độc lập với vai trò hệ thống Admin/User.</summary>
[Flags]
public enum QuyenLamViec
{
    /// <summary>
    /// Lựa chọn không trong ClinicalWorkflowContracts.
    /// </summary>
    Khong = 0,
    /// <summary>
    /// Lựa chọn xem hồ sơ trong ClinicalWorkflowContracts.
    /// </summary>
    XemHoSo = 1,
    /// <summary>
    /// Lựa chọn tiếp nhận trong ClinicalWorkflowContracts.
    /// </summary>
    TiepNhan = 2,
    /// <summary>
    /// Lựa chọn khám trong ClinicalWorkflowContracts.
    /// </summary>
    Kham = 4,
    /// <summary>
    /// Lựa chọn cập nhật kết quả được giao trong ClinicalWorkflowContracts.
    /// </summary>
    CapNhatKetQuaDuocGiao = 8,
    /// <summary>
    /// Lựa chọn thu tiền trong ClinicalWorkflowContracts.
    /// </summary>
    ThuTien = 16,
    /// <summary>
    /// Lựa chọn giao thuốc trong ClinicalWorkflowContracts.
    /// </summary>
    GiaoThuoc = 32,
    /// <summary>
    /// Lựa chọn cấu hình hỗ trợ trong ClinicalWorkflowContracts.
    /// </summary>
    CauHinhHoTro = 64,
    /// <summary>
    /// Lựa chọn duyệt hướng dẫn thuốc trong ClinicalWorkflowContracts.
    /// </summary>
    DuyetHuongDanThuoc = 256,
    /// <summary>
    /// Lựa chọn điều phối ngoài lịch trong ClinicalWorkflowContracts.
    /// </summary>
    DieuPhoiNgoaiLich = 512
}

/// <summary>Snapshot phục vụ UI. Endpoint phải xác minh lại bằng MaNV/MaTK trong SQL.</summary>
public sealed record NguCanhNghiepVu
{
    /// <summary>
    /// Mã nhân viên thực hiện hoặc được gán cho bản ghi.
    /// </summary>
    public string MaNV { get; init; } = "";
    /// <summary>
    /// Mã tài khoản đăng nhập.
    /// </summary>
    public string MaTK { get; init; } = "";
    /// <summary>
    /// Mã chức vụ.
    /// </summary>
    public string MaChucVu { get; init; } = "";
    /// <summary>
    /// Mã chuyên khoa. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaCK { get; init; }
    /// <summary>
    /// Dấu hiệu người dùng có vai trò quản trị viên.
    /// </summary>
    public bool LaAdmin { get; init; }
    /// <summary>
    /// Tập quyền nghiệp vụ của ngữ cảnh hiện tại.
    /// </summary>
    public QuyenLamViec Quyen { get; init; }
    /// <summary>
    /// Thực hiện có quyền trong ngữ cảnh nghiệp vụ.
    /// </summary>
    /// <param name="quyen">Giá trị quyền dùng khi thực hiện thao tác; kiểu QuyenLamViec.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool CoQuyen(QuyenLamViec quyen) => (Quyen & quyen) == quyen;
}

/// <summary>
/// Gói dữ liệu bộ lọc hàng đợi truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
public sealed record BoLocHangDoi
{
    /// <summary>
    /// Mã bệnh nhân. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaBN { get; init; }
    /// <summary>
    /// Mã bác sĩ. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaBS { get; init; }
    /// <summary>
    /// Mã phiếu tiếp nhận khám. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaPhieu { get; init; }
    /// <summary>
    /// Ngày bắt đầu khoảng áp dụng/lọc. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public DateTime? TuNgay { get; init; }
    /// <summary>
    /// Ngày kết thúc khoảng áp dụng/lọc. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public DateTime? DenNgay { get; init; }
    /// <summary>
    /// Trạng thái. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? TrangThai { get; init; }
    /// <summary>
    /// Từ khóa tìm kiếm áp dụng cho danh sách. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? TuKhoa { get; init; }
    /// <summary>
    /// Số trang đang được yêu cầu.
    /// </summary>
    public int Trang { get; init; } = 1;
    /// <summary>
    /// Số bản ghi tối đa của mỗi trang kết quả.
    /// </summary>
    public int KichThuocTrang { get; init; } = 50;
}

/// <summary>
/// Mô hình và thao tác hỗ trợ hàng đợi khám mục dùng bởi các thành phần liên quan.
/// </summary>
public sealed class HangDoiKhamItem
{
    /// <summary>
    /// Mã phiếu tiếp nhận khám.
    /// </summary>
    public string MaPhieu { get; set; } = "";
    /// <summary>
    /// Mã lần khám liên kết các chứng từ cùng lượt. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaLS { get; set; }
    /// <summary>
    /// Mã hồ sơ bệnh án của bệnh nhân.
    /// </summary>
    public string MaHS { get; set; } = "";
    /// <summary>
    /// Mã bệnh nhân.
    /// </summary>
    public string MaBN { get; set; } = "";
    /// <summary>
    /// Họ tên bệnh nhân.
    /// </summary>
    public string HoTenBN { get; set; } = "";
    /// <summary>
    /// Ngày sinh. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public DateTime? NgaySinh { get; set; }
    /// <summary>
    /// Mã bác sĩ.
    /// </summary>
    public string MaBS { get; set; } = "";
    /// <summary>
    /// Mã phòng.
    /// </summary>
    public string MaPhong { get; set; } = "";
    /// <summary>
    /// Mã dịch vụ. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaDV { get; set; }
    /// <summary>
    /// Số thứ tự của mục trong danh sách.
    /// </summary>
    public int SoThuTu { get; set; }
    /// <summary>
    /// Thời điểm tiếp nhận dùng sắp thứ tự hàng chờ khám.
    /// </summary>
    public DateTime ThoiGianTiepNhan { get; set; }
    /// <summary>
    /// Trạng thái.
    /// </summary>
    public string TrangThai { get; set; } = "";
    /// <summary>
    /// Tên bác sĩ.
    /// </summary>
    public string TenBS { get; set; } = "";
    /// <summary>
    /// Tên phòng.
    /// </summary>
    public string TenPhong { get; set; } = "";
    /// <summary>
    /// Tên dịch vụ.
    /// </summary>
    public string TenDV { get; set; } = "";
    /// <summary>
    /// Lý do khám.
    /// </summary>
    public string LyDoKham { get; set; } = "";
    /// <summary>
    /// Giá trị Thao tác; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string ThaoTac => MaLS is null ? "Bắt đầu khám" : "Khám tiếp";
}

/// <summary>Chi tiết đã qua kiểm tra phạm vi, không phải entity bảng SQL.</summary>
public sealed record LanKhamChiTiet(ET_LICHSUKHAMBENH LanKham, string MaBN, string MaHS, bool DuocSua);

/// <summary>
/// Gói dữ liệu đơn thuốc chi tiết truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="DonThuoc">Giá trị đơn thuốc dùng khi thực hiện thao tác; kiểu ET_DONTHUOC.</param>
/// <param name="CacDong">Giá trị các dòng dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;ET_CHITIETDONTHUOC&gt;.</param>
/// <param name="MaBN">Mã bệnh nhân xác định đối tượng cần xử lý.</param>
/// <param name="DuocSua">Giá trị được sửa dùng khi thực hiện thao tác; kiểu bool.</param>
public sealed record DonThuocChiTiet(ET_DONTHUOC DonThuoc, IReadOnlyList<ET_CHITIETDONTHUOC> CacDong, string MaBN, bool DuocSua);

/// <summary>
/// Gói dữ liệu dữ liệu hồ sơ theo quyền truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="NguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
/// <param name="CacBang">Giá trị các Bang dùng khi thực hiện thao tác; kiểu IReadOnlyDictionary&lt;string, object[]&gt;.</param>
public sealed record DuLieuHoSoTheoQuyen(NguCanhNghiepVu NguoiDung, IReadOnlyDictionary<string, object[]> CacBang);

/// <summary>
/// Ngoại lệ lỗi tiếp nhận để nơi gọi nhận diện lỗi và phản hồi phù hợp cho người dùng.
/// </summary>
public sealed class LoiTiepNhanException(string truong,string thongBao) : InvalidOperationException(thongBao)
{
    /// <summary>
    /// Tên trường dữ liệu liên quan đến lỗi để giao diện phản hồi đúng ô nhập.
    /// </summary>
    public string Truong { get; } = truong;
}

/// <summary>
/// Gói dữ liệu yêu cầu tiếp nhận truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
public sealed record YeuCauTiepNhan
{
    /// <summary>
    /// Mã bệnh nhân.
    /// </summary>
    public string MaBN { get; init; } = "";
    /// <summary>
    /// Mã dịch vụ.
    /// </summary>
    public string MaDV { get; init; } = "";
    /// <summary>
    /// Mã bác sĩ.
    /// </summary>
    public string MaBS { get; init; } = "";
    /// <summary>
    /// Mã phòng.
    /// </summary>
    public string MaPhong { get; init; } = "";
    /// <summary>
    /// Mã lịch. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaLich { get; init; }
    /// <summary>
    /// Lý do khám.
    /// </summary>
    public string LyDoKham { get; init; } = "";
    /// <summary>
    /// Nội dung ghi chú bổ sung của bản ghi. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? GhiChu { get; init; }
    /// <summary>
    /// Lý do điều phối ngoài lịch. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? LyDoDieuPhoiNgoaiLich { get; init; }
}

/// <summary>
/// Mô hình và thao tác hỗ trợ bác sĩ được phân công khi tiếp nhận dùng bởi các thành phần liên quan.
/// </summary>
public sealed class BacSiTiepNhan
{
    /// <summary>
    /// Mã nhân viên thực hiện hoặc được gán cho bản ghi.
    /// </summary>
    public string MaNV { get; set; } = "";
    /// <summary>
    /// Họ tên.
    /// </summary>
    public string HoTen { get; set; } = "";
    /// <summary>
    /// Mã chuyên khoa.
    /// </summary>
    public string MaCK { get; set; } = "";
    /// <summary>
    /// Mã khóa.
    /// </summary>
    public string MaKhoa { get; set; } = "";
    /// <summary>
    /// Mã phòng. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaPhong { get; set; }
    /// <summary>
    /// Tên phòng. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? TenPhong { get; set; }
    /// <summary>
    /// Mã ca làm việc. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public string? MaCa { get; set; }
    /// <summary>
    /// Số đang chờ.
    /// </summary>
    public int SoDangCho { get; set; }
    /// <summary>
    /// Thiếu lịch.
    /// </summary>
    public bool ThieuLich { get; set; }
    /// <summary>
    /// Giá trị Tình trạng lịch; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string TinhTrangLich => ThieuLich ? "Chưa có lịch làm việc để đối chiếu" : "Có lịch làm việc phù hợp";
    /// <summary>
    /// Giá trị Nhãn; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string Label => $"{HoTen} ({MaNV}) · {TenPhong ?? "chọn phòng"} · {SoDangCho} đang chờ · {TinhTrangLich}";
}

/// <summary>
/// Gói dữ liệu kết quả tiếp nhận truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
/// </summary>
/// <param name="MaBN">Mã bệnh nhân xác định đối tượng cần xử lý.</param>
/// <param name="MaHS">Mã hồ sơ xác định đối tượng cần xử lý.</param>
/// <param name="MaPhieu">Mã phiếu xác định đối tượng cần xử lý.</param>
/// <param name="MaBS">Mã bác sĩ xác định đối tượng cần xử lý.</param>
/// <param name="SoThuTu">Giá trị số thứ tự dùng khi thực hiện thao tác; kiểu int.</param>
/// <param name="TrangThai">Giá trị trạng thái dùng khi thực hiện thao tác; kiểu string.</param>
public sealed record KetQuaTiepNhan(string MaBN,string MaHS,string MaPhieu,string MaBS,int SoThuTu,string TrangThai);
