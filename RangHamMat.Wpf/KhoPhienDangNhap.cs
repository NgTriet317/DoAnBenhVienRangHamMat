using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ET;

namespace RangHamMat;

/// <summary>Phiên ghi nhớ được mã hóa cho người dùng Windows hiện tại; không lưu mật khẩu.</summary>
internal sealed class KhoPhienDangNhap
{
    /// <summary>
    /// Dữ liệu bổ sung cố định dùng nhất quán khi DPAPI mã hóa và giải mã phiên ghi nhớ.
    /// </summary>
    private static readonly byte[] entropy = Encoding.UTF8.GetBytes("RangHamMat.Login.v1");
    /// <summary>
    /// Đường dẫn tệp chứa phiên đã mã hóa của người dùng Windows hiện tại.
    /// </summary>
    private readonly string path;

    /// <summary>
    /// Chọn đường dẫn lưu phiên; mặc định đặt dưới LocalApplicationData của người dùng Windows.
    /// </summary>
    /// <param name="path">Đường dẫn tùy chọn; null để dùng tệp phien-dang-nhap.dat của ứng dụng.</param>
    internal KhoPhienDangNhap(string? path = null)
    {
        this.path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RangHamMat", "phien-dang-nhap.dat");
    }

    /// <summary>
    /// Mã hóa định danh và dấu vân tay mật khẩu bằng DPAPI, rồi thay tệp đích qua tệp tạm để tránh bản lưu dở.
    /// </summary>
    /// <param name="taiKhoan">Tài khoản đã xác thực cần ghi nhớ; chỉ lưu mã và dấu vân tay của hash mật khẩu.</param>
    internal void Luu(ET_TAIKHOAN taiKhoan)
    {
        var saved = new PhienDaLuu(1, taiKhoan.MaTK, taiKhoan.MaNV, PhienDaLuu.DauVanTay(taiKhoan.MatKhauHash));
        var encrypted = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(saved), entropy, DataProtectionScope.CurrentUser);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, encrypted);
            File.Move(temporary, path, true);
        }
        finally { File.Delete(temporary); }
    }

    /// <summary>
    /// Giải mã và kiểm tra phiên bản, mã định danh và độ dài dấu vân tay của phiên ghi nhớ.
    /// </summary>
    /// <remarks>Việc đối chiếu tài khoản hiện tại thuộc DichVuDangNhap.ThuKhoiPhuc; đọc được tệp chưa đồng nghĩa đăng nhập thành công.</remarks>
    /// <returns>Phiên hợp lệ về định dạng; null nếu tệp thiếu, không giải mã được hoặc nội dung không hợp lệ.</returns>
    internal PhienDaLuu? Doc()
    {
        try
        {
            var encrypted = File.ReadAllBytes(path);
            var bytes = ProtectedData.Unprotect(encrypted, entropy, DataProtectionScope.CurrentUser);
            var saved = JsonSerializer.Deserialize<PhienDaLuu>(bytes);
            return saved is { PhienBan: 1 } && !string.IsNullOrWhiteSpace(saved.MaTK)
                && !string.IsNullOrWhiteSpace(saved.MaNV) && saved.MaTK.Length <= 50 && saved.MaNV.Length <= 50
                && saved.DauVanTayMatKhau?.Length == 64 ? saved : null;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or CryptographicException or JsonException)
        {
            // Không có hoặc không giải mã được phiên: quay về đăng nhập bằng mật khẩu.
            return null;
        }
    }

    /// <summary>
    /// Xóa tệp ghi nhớ; lỗi truy cập được để nơi gọi xử lý.
    /// </summary>
    internal void Xoa() => File.Delete(path);
}

/// <summary>
/// Nội dung phiên ghi nhớ để đối chiếu với tài khoản hiện tại khi mở lại ứng dụng.
/// </summary>
/// <param name="PhienBan">Phiên bản định dạng dữ liệu lưu; hiện chấp nhận giá trị 1.</param>
/// <param name="MaTK">Mã tài khoản đã được xác thực.</param>
/// <param name="MaNV">Mã nhân viên gắn với tài khoản đã ghi nhớ.</param>
/// <param name="DauVanTayMatKhau">SHA-256 của hash mật khẩu để phát hiện mật khẩu đã thay đổi.</param>
internal sealed record PhienDaLuu(int PhienBan, string MaTK, string MaNV, string DauVanTayMatKhau)
{
    /// <summary>
    /// Băm hash mật khẩu thành chuỗi SHA-256 dùng trong bản ghi nhớ.
    /// </summary>
    /// <param name="hash">Hash mật khẩu hiện tại của tài khoản.</param>
    /// <returns>Chuỗi hex của dấu vân tay để đối chiếu khi khôi phục phiên.</returns>
    internal static string DauVanTay(string hash) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));

    /// <summary>
    /// Đối chiếu mã tài khoản, nhân viên, trạng thái và dấu vân tay mật khẩu với bản ghi nhớ.
    /// </summary>
    /// <param name="taiKhoan">Tài khoản vừa đọc lại từ nguồn dữ liệu hiện tại.</param>
    /// <returns>True nếu tài khoản vẫn đang hoạt động và khớp toàn bộ thông tin đã ghi nhớ.</returns>
    internal bool KhopTaiKhoan(ET_TAIKHOAN taiKhoan) => PhienBan == 1
        && MaTK == taiKhoan.MaTK && MaNV == taiKhoan.MaNV
        && taiKhoan.TrangThai.Equals("Đang hoạt động", StringComparison.OrdinalIgnoreCase)
        && DauVanTayMatKhau == DauVanTay(taiKhoan.MatKhauHash);
}
