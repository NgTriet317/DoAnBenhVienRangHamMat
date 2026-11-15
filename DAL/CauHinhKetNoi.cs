using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DAL;

/// <summary>
/// Nơi duy nhất quản lý chuỗi kết nối SQL Server của ứng dụng.
/// Đọc cấu hình (môi trường/AppData/appsettings), dò SQL Server, lấy database,
/// kiểm tra cấu trúc dữ liệu và bảo vệ cấu hình riêng trên từng máy.
/// </summary>
public static class CauHinhKetNoi
{
    public const string TenDatabaseMacDinh = "";

    private const string ChuoiKetNoiMacDinh = "";


    private static string DuongDanCauHinh => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RangHamMat", "ketnoi-sql.dat");

    /// <summary>
    /// Điểm gọi duy nhất của dbconnect.CreateConnection().
    /// Ưu tiên: biến môi trường, cấu hình AppData đã mã hóa, appsettings.json, mặc định.
    /// Nếu môi trường hay cấu hình đã lưu không kết nối được, BUS sẽ kiểm tra và
    /// kích hoạt phương án khác TRƯỚC khi đăng nhập.
    /// </summary>
    public static string LayChuoiKetNoi()
    {
        string? tuMoiTruong = Environment.GetEnvironmentVariable("RHM_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(tuMoiTruong)) return tuMoiTruong;

        string? daLuu = DocCauHinhDaLuu();
        if (!string.IsNullOrWhiteSpace(daLuu)) return daLuu;

        return DocChuoiKetNoiAppSettings() ?? ChuoiKetNoiMacDinh;
    }

    /// <summary>
    /// Danh sách ứng viên để kiểm tra lúc khởi động, theo thứ tự ưu tiên.
    /// appsettings.json bị lỗi thì bỏ qua để vẫn có thể mở View chọn server.
    /// Không kết luận một chuỗi kết nối hợp lệ nếu chưa OpenAsync thành công.
    /// </summary>
    public static string[] LayCacChuoiKetNoiUngVien()
    {
        var danhSach = new List<string>();

        void Them(string? chuoi)
        {
            if (!string.IsNullOrWhiteSpace(chuoi) &&
                !danhSach.Contains(chuoi, StringComparer.Ordinal))
                danhSach.Add(chuoi);
        }

        Them(Environment.GetEnvironmentVariable("RHM_CONNECTION_STRING"));
        Them(DocCauHinhDaLuu());
        try { Them(DocChuoiKetNoiAppSettings()); }
        catch (InvalidOperationException)
        {
            // File bị lỗi: View vẫn cho phép sửa cấu hình trên máy mới.
        }
        Them(ChuoiKetNoiMacDinh);
        return danhSach.ToArray();
    }

    /// <summary>
    /// Đọc cấu hình mặc định được phát hành cùng ứng dụng; không sửa các mục AI/XQuang.
    /// </summary>
    private static string? DocChuoiKetNoiAppSettings()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path)) return null;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            string? value = document.RootElement
                .GetProperty("ConnectionStrings")
                .GetProperty("RangHamMat")
                .GetString();
            if (string.IsNullOrWhiteSpace(value)) throw new JsonException();
            return value;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException
                                   or InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Kiểm tra ConnectionStrings:RangHamMat trong appsettings.json cạnh ứng dụng.", ex);
        }
    }

    /// <summary>
    /// Chỉ ghi đè chuỗi kết nối trong tiến trình hiện tại. Các DAL hiện có tự lấy
    /// giá trị mới bằng LayChuoiKetNoi mà không cần thay đổi dbconnect.
    /// </summary>
    public static void SuDungChuoiKetNoi(string chuoiKetNoi)
    {
        _ = new SqlConnectionStringBuilder(chuoiKetNoi);
        Environment.SetEnvironmentVariable("RHM_CONNECTION_STRING", chuoiKetNoi,
            EnvironmentVariableTarget.Process);
    }

    /// <summary>
    /// Dò các SQL Server instance có đăng ký tại máy Windows này. Đây không phải
    /// dịch vụ dò tất cả máy chủ trên LAN; người dùng có thể nhập tên máy/IP thủ công.
    /// </summary>
    public static string[] TimMayChu()
    {
        var danhSach = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".", @".\SQLEXPRESS"
        };

        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var may = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var instances = may.OpenSubKey(
                    @"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");
                if (instances is null) continue;
                foreach (string ten in instances.GetValueNames())
                {
                    if (ten.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase))
                        danhSach.Add(".");
                    else if (!string.IsNullOrWhiteSpace(ten))
                        danhSach.Add(@".\" + ten);
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException
                                       or UnauthorizedAccessException
                                       or IOException)
            {
                // Hạn chế đọc registry: ComboBox vẫn cho nhập server thủ công.
            }
        }

        return danhSach.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static string TaoChuoiKetNoi(string mayChu, string tenDatabase,
        bool xacThucWindows, string taiKhoan, string matKhau)
    {
        if (string.IsNullOrWhiteSpace(mayChu))
            throw new ArgumentException("Vui lòng chọn hoặc nhập tên SQL Server.");
        if (string.IsNullOrWhiteSpace(tenDatabase))
            throw new ArgumentException("Vui lòng chọn tên cơ sở dữ liệu.");

        // Mật khẩu SQL đã mã hóa không hiện lại trên PasswordBox. Chỉ dùng lại
        // nếu người dùng không đổi server, kiểu xác thực hoặc tài khoản SQL.
        if (!xacThucWindows && string.IsNullOrEmpty(matKhau))
        {
            string? daLuu = DocCauHinhDaLuu();
            if (!string.IsNullOrWhiteSpace(daLuu))
            {
                var cu = TachChuoiKetNoi(daLuu);
                if (!cu.XacThucWindows &&
                    string.Equals(cu.MayChu, mayChu.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(cu.TaiKhoan, taiKhoan.Trim(), StringComparison.OrdinalIgnoreCase))
                    matKhau = cu.MatKhau;
            }
        }

        if (!xacThucWindows &&
            (string.IsNullOrWhiteSpace(taiKhoan) || string.IsNullOrEmpty(matKhau)))
            throw new ArgumentException("Vui lòng nhập tài khoản và mật khẩu SQL Server.");

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = mayChu.Trim(),
            InitialCatalog = tenDatabase.Trim(),
            IntegratedSecurity = xacThucWindows,
            ConnectTimeout = 4,
            TrustServerCertificate = true,
            PersistSecurityInfo = false,
            ApplicationName = "RangHamMat.Wpf"
        };
        if (!xacThucWindows)
        {
            builder.UserID = taiKhoan.Trim();
            builder.Password = matKhau;
        }
        return builder.ConnectionString;
    }

    public static (string MayChu, string Database, bool XacThucWindows,
        string TaiKhoan, string MatKhau) TachChuoiKetNoi(string chuoiKetNoi)
    {
        var builder = new SqlConnectionStringBuilder(chuoiKetNoi);
        return (builder.DataSource, builder.InitialCatalog, builder.IntegratedSecurity,
            builder.UserID, builder.Password);
    }

    /// <summary>
    /// Mở kết nối SQL và kiểm tra bảng nghiệp vụ. Không tạo/xóa/ghi dữ liệu.
    /// </summary>
    public static async Task KiemTraKetNoiAsync(string chuoiKetNoi,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(chuoiKetNoi);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = new SqlCommand("""
            SELECT CASE WHEN OBJECT_ID(N'dbo.TAI_KHOAN', N'U') IS NOT NULL
                AND OBJECT_ID(N'dbo.NHAN_VIEN', N'U') IS NOT NULL
                AND OBJECT_ID(N'dbo.HO_SO_BENH_AN', N'U') IS NOT NULL
                THEN 1 ELSE 0 END
            """, connection) { CommandTimeout = 5 };
        object? result = await command.ExecuteScalarAsync(cancellationToken)
            .ConfigureAwait(false);
        if (Convert.ToInt32(result) != 1)
            throw new InvalidOperationException(
                "Kết nối SQL thành công nhưng database chưa có đủ bảng nghiệp vụ " +
                "(TAI_KHOAN, NHAN_VIEN, HO_SO_BENH_AN). Hãy chạy SQL chính " +
                "trong SSMS hoặc chọn database đúng.");
    }

    /// <summary>
    /// Tải database từ chính SQL Server đang kết nối (sys.databases).
    /// SSMS chỉ là công cụ quản lý SQL Server, không phải nơi lưu database.
    /// </summary>
    public static async Task<string[]> LayDanhSachDatabaseAsync(string chuoiKetNoi,
        CancellationToken cancellationToken = default)
    {
        var builder = new SqlConnectionStringBuilder(chuoiKetNoi) { InitialCatalog = "master" };
        using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = new SqlCommand("""
            SELECT name FROM sys.databases
            WHERE state = 0 AND HAS_DBACCESS(name) = 1
            ORDER BY CASE WHEN name = N'QLBenhVienRangHamMat' THEN 0 ELSE 1 END, name
            """, connection) { CommandTimeout = 5 };
        using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        var danhSach = new List<string>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            danhSach.Add(reader.GetString(0));
        return danhSach.ToArray();
    }

    /// <summary>
    /// Giải mã profile SQL của tài khoản Windows hiện tại. Giữ nguyên định dạng
    /// ketnoi-sql.dat của bản DAL_CAUHINHCSDL cũ, không cần chuyển đổi dữ liệu.
    /// </summary>
    public static string? DocCauHinhDaLuu()
    {
        try
        {
            if (!File.Exists(DuongDanCauHinh)) return null;
            byte[] maHoa = File.ReadAllBytes(DuongDanCauHinh);
            byte[] noiDung = ProtectedData.Unprotect(maHoa, null, DataProtectionScope.CurrentUser);
            string chuoi = Encoding.UTF8.GetString(noiDung);
            _ = new SqlConnectionStringBuilder(chuoi);
            return chuoi;
        }
        catch (Exception ex) when (ex is CryptographicException or IOException
                                   or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Chỉ gọi sau KiemTraKetNoiAsync thành công. Lưu bằng Windows DPAPI,
    /// không sửa appsettings.json và không lưu mật khẩu thuần văn bản.
    /// </summary>
    public static void LuuCauHinh(string chuoiKetNoi)
    {
        _ = new SqlConnectionStringBuilder(chuoiKetNoi);
        string folder = Path.GetDirectoryName(DuongDanCauHinh)!;
        Directory.CreateDirectory(folder);
        byte[] noiDung = Encoding.UTF8.GetBytes(chuoiKetNoi);
        byte[] maHoa = ProtectedData.Protect(noiDung, null, DataProtectionScope.CurrentUser);
        string tam = DuongDanCauHinh + ".tmp";
        File.WriteAllBytes(tam, maHoa);
        File.Move(tam, DuongDanCauHinh, overwrite: true);
    }
}
