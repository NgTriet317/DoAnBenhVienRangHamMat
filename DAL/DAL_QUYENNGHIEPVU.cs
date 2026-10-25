using System.Data;
using System.Text.Json;
using System.Security.Claims;
using Dapper;
using ET;

namespace DAL;

/// <summary>Đọc lại định danh/quyền từ SQL trong transaction của thao tác, không tin cờ quyền từ UI.</summary>
public sealed class DAL_QUYENNGHIEPVU : dbconnect
{
    private readonly IDbTransaction? callerTransaction;
    private static readonly Lazy<IReadOnlyDictionary<string, QuyenLamViec>> Mapping = new(ReadMapping);

    public DAL_QUYENNGHIEPVU(IDbTransaction? transaction = null) : base(transaction)
        => callerTransaction = transaction;

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
                WHERE l.MaLS=@maLS AND l.MaBS=@MaNV AND p.MaBS=@MaNV AND l.TrangThai=N'Nháp'
                  AND p.TrangThai IN(N'Chờ khám',N'Đang khám')
                """, new { maLS, user.MaNV }, tx);
            if (allowed != 1) throw TuChoi();
            return true;
        });
    }

    internal static NguCanhNghiepVu XacThuc(IDbConnection db, IDbTransaction? tx, NguCanhNghiepVu nguoiDung)
    {
        if (nguoiDung is null) throw TuChoi();
        var current = ReadIdentity(db, tx, nguoiDung.MaNV);
        if (!string.Equals(current.MaTK, nguoiDung.MaTK, StringComparison.Ordinal)) throw TuChoi();
        return current;
    }

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
                CONVERT(bit,CASE WHEN vt.TenVaiTro=N'Admin' THEN 1 ELSE 0 END) AS LaAdmin
            FROM dbo.TAI_KHOAN tk WITH(HOLDLOCK)
            JOIN dbo.NHAN_VIEN nv WITH(HOLDLOCK) ON nv.MaNV=tk.MaNV
            JOIN dbo.CHUC_VU cv ON cv.MaChucVu=nv.MaChucVu
            JOIN dbo.VAI_TRO vt ON vt.MaVaiTro=tk.MaVaiTro
            WHERE nv.MaNV=@maNV AND tk.TrangThai=N'Đang hoạt động' AND nv.TrangThai=N'Đang làm việc'
            """, new { maNV }, tx);
        if (user is null || user.MaTK != accountClaims[0].Value) throw TuChoi();
        return user with { Quyen = Mapping.Value.GetValueOrDefault(user.MaChucVu, QuyenLamViec.Khong) };
    }

    internal static void GhiTraCuuAdmin(IDbConnection db, IDbTransaction? tx, NguCanhNghiepVu user, string table, string id)
    {
        if (!user.LaAdmin) return;
        var maNK = TaoMa(db, tx, "NHAT_KY_THAO_TAC", "MaNK");
        db.Execute("""
            INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua)
            VALUES(@maNK,@MaTK,SYSDATETIME(),N'Tra cứu quy trình khám',@table,@id,N'Thành công')
            """, new { maNK, user.MaTK, table, id }, tx);
    }

    internal static void KiemTraMa(string? ma)
    {
        if (string.IsNullOrWhiteSpace(ma) || ma.Length > 50) throw TuChoi();
    }

    internal static TuChoiQuyenNghiepVuException TuChoi() => new();

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
