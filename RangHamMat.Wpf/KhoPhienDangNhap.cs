using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ET;

namespace RangHamMat;

/// <summary>Phiên ghi nhớ được mã hóa cho người dùng Windows hiện tại; không lưu mật khẩu.</summary>
internal sealed class KhoPhienDangNhap
{
    private static readonly byte[] entropy = Encoding.UTF8.GetBytes("RangHamMat.Login.v1");
    private readonly string path;

    internal KhoPhienDangNhap(string? path = null)
    {
        this.path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RangHamMat", "phien-dang-nhap.dat");
    }

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

    internal void Xoa() => File.Delete(path);
}

internal sealed record PhienDaLuu(int PhienBan, string MaTK, string MaNV, string DauVanTayMatKhau)
{
    internal static string DauVanTay(string hash) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));

    internal bool KhopTaiKhoan(ET_TAIKHOAN taiKhoan) => PhienBan == 1
        && MaTK == taiKhoan.MaTK && MaNV == taiKhoan.MaNV
        && taiKhoan.TrangThai.Equals("Đang hoạt động", StringComparison.OrdinalIgnoreCase)
        && DauVanTayMatKhau == DauVanTay(taiKhoan.MatKhauHash);
}
