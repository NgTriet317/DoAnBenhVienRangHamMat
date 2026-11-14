namespace ET;

/// <summary>
/// Lỗi nghiệp vụ ảnh kèm mã ổn định để giao diện xử lý cấu hình, quyền thao tác hoặc sự cố tệp.
/// </summary>
public sealed class LoiAnhXquangException(string maLoi,string message,Exception? inner=null)
    : InvalidOperationException(message,inner)
{
    public string MaLoi { get; } = maLoi;
}

/// <summary>
/// Dịch vụ thực hiện thuộc lần khám dùng làm nguồn liên kết ảnh và xác định người phụ trách.
/// </summary>
public sealed record DichVuAnhXquang(string MaDVThucHien,string TenDV,string MaNVThucHien);
/// <summary>
/// Thông tin lượt khám trong danh sách công việc chụp ảnh được giao cho kỹ thuật viên.
/// </summary>
public sealed record LuotAnhDuocGiao(string MaLS,string MaBN,string HoTenBN,DateTime ThoiGianKham);
/// <summary>
/// Metadata ảnh kèm tên người tải và quyền ẩn đã được DAL tính theo ngữ cảnh hiện tại.
/// </summary>
public sealed record AnhXquangHienThi(ET_ANHXQUANGLANKHAM Anh,string NguoiTai,bool DuocAn);
/// <summary>
/// Dữ liệu màn hình ảnh của một lượt, gồm dịch vụ nguồn, các ảnh và quyền tải hoặc phân công.
/// </summary>
public sealed record DanhSachAnhXquang(string MaLS,string MaBN,string HoTenBN,bool DuocTaiLen,
    IReadOnlyList<DichVuAnhXquang> DichVu,IReadOnlyList<AnhXquangHienThi> CacAnh,bool DuocPhanCong=false);
/// <summary>
/// Mã và tên kỹ thuật viên dùng khi lựa chọn người nhận dịch vụ chụp ảnh.
/// </summary>
public sealed record NhanVienChupAnh(string MaNV,string HoTen);
/// <summary>
/// Metadata cùng các byte ảnh đã đọc từ kho để chuyển tới bộ xem.
/// </summary>
public sealed record DuLieuAnhXquang(ET_ANHXQUANGLANKHAM Anh,byte[] NoiDung);
/// <summary>
/// Ảnh đã xác nhận định dạng và giới hạn, kèm SHA-256 cùng kích thước để lưu và kiểm tra toàn vẹn.
/// </summary>
public sealed record AnhDaKiemTra(byte[] NoiDung,string MimeType,string PhanMoRong,string Sha256,int ChieuRong,int ChieuCao);
/// <summary>
/// Một lỗi đối chiếu kho theo loại, tên tệp và mã ảnh; mã ảnh có thể thiếu với tệp chưa đăng ký.
/// </summary>
public sealed record SuCoKhoAnh(string Loai,string TenTep,string? MaAnh);
