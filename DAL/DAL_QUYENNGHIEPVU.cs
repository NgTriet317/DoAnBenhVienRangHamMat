using System.Data;
using System.Text.Json;
using System.Security.Claims;
using Dapper;
using ET;

namespace DAL;

/// <summary>Đọc lại định danh/quyền từ SQL trong transaction của thao tác, không tin cờ quyền từ UI.</summary>
public sealed partial class DAL_QUYENNGHIEPVU : dbconnect
{
    /// <summary>
    /// Giao dịch được giữ để các thao tác liên quan dùng cùng một phạm vi commit/rollback.
    /// </summary>
    private readonly IDbTransaction? callerTransaction;
    /// <summary>
    /// Ánh xạ mã chức vụ sang quyền nghiệp vụ, được nạp khi dùng lần đầu được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static readonly Lazy<IReadOnlyDictionary<string, QuyenLamViec>> Mapping = new(ReadMapping);

    /// <summary>
    /// Khởi tạo quyền nghiệp vụ. Chuyển giao dịch tùy chọn cho lớp cơ sở; nơi cấp giao dịch chịu trách nhiệm kết thúc giao dịch đó.
    /// </summary>
    /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    public DAL_QUYENNGHIEPVU(IDbTransaction? transaction = null) : base(transaction)
        => callerTransaction = transaction;

    /// <summary>
    /// Đọc ngữ cảnh nghiệp vụ từ định danh đăng nhập để áp dụng quyền và phạm vi dữ liệu.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>ReadIdentity</c>.</remarks>
    /// <param name="maNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
    /// <returns>Kết quả kiểu NguCanhNghiepVu dùng cho bước xử lý tiếp theo.</returns>
    public NguCanhNghiepVu LayNguCanh(string maNV)
        => ThucHien((db, tx) => ReadIdentity(db, tx, maNV));

    /// <summary>Khóa cùng giao dịch với lệnh ghi tiếp theo; không cho dùng như kiểm tra độc lập rồi mở giao dịch khác.</summary>
    public void KiemTraQuyenGhiLanKham(string maLS, NguCanhNghiepVu nguoiDung)
    {
        if (callerTransaction is null && System.Transactions.Transaction.Current is null)
            throw new InvalidOperationException("Kiểm tra quyền ghi phải dùng chung giao dịch với thao tác lưu.");
        ThucHien((db, tx) =>
        {
            var user = XacThuc(db, tx, nguoiDung);
            if (!user.CoQuyen(QuyenLamViec.Kham)) throw TuChoi();
            KiemTraMa(maLS);
            var allowed = db.ExecuteScalar<int>("""
                SELECT COUNT(*) FROM dbo.LICH_SU_KHAM_BENH l WITH(UPDLOCK,HOLDLOCK)
                JOIN dbo.PHIEU_KHAM_BENH p WITH(UPDLOCK,HOLDLOCK) ON p.MaPhieu=l.MaPhieu
                WHERE l.MaLS=@maLS AND (@LaAdmin=1 OR (l.MaBS=@MaNV AND p.MaBS=@MaNV)) AND l.TrangThai=N'Nháp'
                  AND p.TrangThai IN(N'Chờ khám',N'Đang khám')
                """, new { maLS, user.MaNV, user.LaAdmin }, tx);
            if (allowed != 1) throw TuChoi();
            return true;
        });
    }

    /// <summary>
    /// Xác thực lại người dùng theo dữ liệu hiện tại trước khi cho phép thao tác nghiệp vụ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TuChoi</c>, <c>ReadIdentity</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu NguCanhNghiepVu dùng cho bước xử lý tiếp theo.</returns>
    internal static NguCanhNghiepVu XacThuc(IDbConnection db, IDbTransaction? tx, NguCanhNghiepVu nguoiDung)
    {
        if (nguoiDung is null) throw TuChoi();
        var current = ReadIdentity(db, tx, nguoiDung.MaNV);
        if (!string.Equals(current.MaTK, nguoiDung.MaTK, StringComparison.Ordinal)) throw TuChoi();
        return current;
    }

    /// <summary>
    /// Đọc thông tin định danh cần đối chiếu với tài khoản và nhân viên trong SQL. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>KiemTraMa</c>, <c>.Identities.SingleOrDefault</c>, <c>.FindAll</c>, <c>TuChoi</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="maNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
    /// <returns>Kết quả kiểu NguCanhNghiepVu dùng cho bước xử lý tiếp theo.</returns>
    private static NguCanhNghiepVu ReadIdentity(IDbConnection db, IDbTransaction? tx, string maNV)
    {
        KiemTraMa(maNV);
        // Principal được gắn tại điểm đăng nhập thành công, độc lập với DTO do màn hình gửi.
        // Claims không thay thế việc đọc lại trạng thái/quyền từ SQL ở bên dưới.
        var identity = (Thread.CurrentPrincipal as ClaimsPrincipal)?.Identities
            .SingleOrDefault(i => i.IsAuthenticated && i.AuthenticationType == DinhDanhNghiepVu.KieuXacThuc);
        var employeeClaims = identity?.FindAll(DinhDanhNghiepVu.MaNhanVienClaim).ToArray();
        var accountClaims = identity?.FindAll(ClaimTypes.NameIdentifier).ToArray();
        if (employeeClaims?.Length != 1 || accountClaims?.Length != 1 || employeeClaims[0].Value != maNV)
            throw TuChoi();
        var user = db.QuerySingleOrDefault<NguCanhNghiepVu>("""
            SELECT nv.MaNV,tk.MaTK,nv.MaChucVu,nv.MaCK,
                CONVERT(bit,CASE WHEN UPPER(LTRIM(RTRIM(vt.TenVaiTro))) IN(N'ADMIN',N'QUẢN TRỊ') THEN 1 ELSE 0 END) AS LaAdmin
            FROM dbo.TAI_KHOAN tk WITH(HOLDLOCK)
            JOIN dbo.NHAN_VIEN nv WITH(HOLDLOCK) ON nv.MaNV=tk.MaNV
            JOIN dbo.CHUC_VU cv ON cv.MaChucVu=nv.MaChucVu
            JOIN dbo.VAI_TRO vt ON vt.MaVaiTro=tk.MaVaiTro
            WHERE nv.MaNV=@maNV AND tk.TrangThai=N'Đang hoạt động' AND nv.TrangThai=N'Đang làm việc'
            """, new { maNV }, tx);
        if (user is null || user.MaTK != accountClaims[0].Value) throw TuChoi();
        // Chỉ vai trò vừa xác thực lại trong SQL được cấp toàn quyền; không dùng cờ từ DTO/UI.
        return user with { Quyen = user.LaAdmin
            ? Enum.GetValues<QuyenLamViec>().Aggregate(QuyenLamViec.Khong, (all, right) => all | right)
            : Mapping.Value.GetValueOrDefault(user.MaChucVu, QuyenLamViec.Khong) };
    }

    /// <summary>
    /// Ghi nhật ký tra cứu cho quản trị viên để theo dõi việc truy cập dữ liệu quy trình khám.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TaoMa</c>, <c>db.Execute</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <param name="table">Giá trị bảng dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    internal static void GhiTraCuuAdmin(IDbConnection db, IDbTransaction? tx, NguCanhNghiepVu user, string table, string id)
    {
        if (!user.LaAdmin) return;
        var maNK = TaoMa(db, tx, "NHAT_KY_THAO_TAC", "MaNK");
        db.Execute("""
            INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua)
            VALUES(@maNK,@MaTK,SYSDATETIME(),N'Tra cứu quy trình khám',@table,@id,N'Thành công')
            """, new { maNK, user.MaTK, table, id }, tx);
    }

    /// <summary>
    /// Kiểm tra mã định danh hợp lệ trước khi tra dữ liệu nghiệp vụ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>TuChoi</c>.</remarks>
    /// <param name="ma">Mã định danh của bản ghi cần xử lý.</param>
    internal static void KiemTraMa(string? ma)
    {
        if (string.IsNullOrWhiteSpace(ma) || ma.Length > 50) throw TuChoi();
    }

    /// <summary>
    /// Tạo ngoại lệ từ chối quyền nghiệp vụ cho thao tác không thuộc phạm vi cho phép.
    /// </summary>
    /// <returns>Kết quả kiểu TuChoiQuyenNghiepVuException dùng cho bước xử lý tiếp theo.</returns>
    internal static TuChoiQuyenNghiepVuException TuChoi() => new();

    /// <summary>
    /// Đọc ánh xạ quyền hoặc phạm vi phục vụ xác thực ngữ cảnh.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>JsonSerializer.Deserialize&lt;Dictionary&lt;string,string[]&gt;&gt;</c>, <c>Enum.TryParse&lt;QuyenLamViec&gt;</c>, <c>Enum.IsDefined</c>, <c>mapping.Add</c>.</remarks>
    /// <returns>Kết quả kiểu IReadOnlyDictionary&lt;string, QuyenLamViec&gt; dùng cho bước xử lý tiếp theo.</returns>
    private static IReadOnlyDictionary<string, QuyenLamViec> ReadMapping()
    {
        using var stream = typeof(DAL_QUYENNGHIEPVU).Assembly.GetManifestResourceStream("DAL.ClinicalWorkflowPermissions.json")
            ?? throw new InvalidOperationException("Thiếu cấu hình quyền quy trình khám.");
        var source = JsonSerializer.Deserialize<Dictionary<string, string[]>>(stream)
            ?? throw new InvalidOperationException("Cấu hình quyền quy trình khám không hợp lệ.");
        var mapping = new Dictionary<string, QuyenLamViec>(StringComparer.Ordinal);
        foreach (var (job, names) in source)
        {
            QuyenLamViec permissions = QuyenLamViec.Khong;
            foreach (var name in names)
            {
                if (!Enum.TryParse<QuyenLamViec>(name, out var permission) || !Enum.IsDefined(permission))
                    throw new InvalidOperationException("Cấu hình có tên quyền không hợp lệ.");
                permissions |= permission;
            }
            mapping.Add(job, permissions);
        }
        return mapping;
    }
}
