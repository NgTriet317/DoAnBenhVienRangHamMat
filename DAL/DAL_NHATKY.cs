using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Dapper;
using ET;
using Microsoft.Data.SqlClient;

namespace DAL;

/// <summary>Audit SQL cùng giao dịch nơi gọi; không nhận payload bệnh án hoặc bí mật.</summary>
public sealed class DAL_NHATKY(IDbTransaction? transaction = null) : dbconnect(transaction)
{
    private const string SchemaPhienCu = "Cấu trúc phiên đăng nhập chưa được cập nhật. Vui lòng chạy Database/QLBenhVienRangHamMat.sql mới nhất.";
    private const string SessionStateQuery = """
        SELECT CASE
          WHEN PhienKeTiep IS NOT NULL OR LyDoKetThuc=@replaced THEN 1
          WHEN TrangThaiPhien=N'Đã đăng xuất' THEN 3
          WHEN TrangThaiPhien=N'Đang mở' AND HoatDongCuoiUtc>=DATEADD(minute,-5,SYSUTCDATETIME()) THEN 0
          ELSE 2 END
        FROM dbo.PHIEN_DANG_NHAP
        WHERE MaPhien=@session AND MaTK=@account
        """;

    /// <summary>Đối chiếu đúng một định danh đã xác thực, tài khoản và mã phiên; không nhận token khác từ UI.</summary>
    internal static void KiemTraClaimsPhien(string account, Guid session)
    {
        var identities = (Thread.CurrentPrincipal as ClaimsPrincipal)?.Identities
            .Where(i => i.IsAuthenticated && i.AuthenticationType == DinhDanhNghiepVu.KieuXacThuc).ToArray();
        if (identities?.Length != 1) throw new UnauthorizedAccessException("Chưa đăng nhập.");
        var accounts = identities[0].FindAll(ClaimTypes.NameIdentifier).ToArray();
        var sessions = identities[0].FindAll("RHM.MaPhien").ToArray();
        if (string.IsNullOrWhiteSpace(account) || account.Length > 50 || session == Guid.Empty ||
            accounts.Length != 1 || accounts[0].Value != account || sessions.Length != 1 ||
            !Guid.TryParse(sessions[0].Value, out Guid claimed) || claimed != session)
            throw new UnauthorizedAccessException("Định danh phiên đăng nhập không hợp lệ.");
    }

    /// <summary>Đọc trạng thái trên kết nối riêng, không heartbeat, không đi qua guard của phiên đã bị thu hồi.</summary>
    public TrangThaiPhienHienTai KiemTraPhienHienTai(Guid session, string account)
    {
        KiemTraClaimsPhien(account, session);
        using var db = new SqlConnection(new SqlConnectionStringBuilder(CauHinhKetNoi.LayChuoiKetNoi()) { Enlist = false }.ConnectionString);
        db.Open();
        try
        {
            return (TrangThaiPhienHienTai)(db.QuerySingleOrDefault<int?>(SessionStateQuery,
                new { session, account, replaced = ThongBaoPhien.DangNhapMayKhac }) ?? (int)TrangThaiPhienHienTai.KhongTonTai);
        }
        catch (SqlException e) when (e.Number == 207) { throw new InvalidOperationException(SchemaPhienCu, e); }
    }

    /// <summary>Watcher hủy được, dùng chuỗi kết nối đã chụp và token tường minh để phát hiện phiên bị thay thế.</summary>
    public async Task<TrangThaiPhienHienTai> KiemTraPhienHienTaiAsync(string connectionString, Guid session, string account, CancellationToken cancellationToken = default)
    {
        KiemTraClaimsPhien(account, session);
        await using var db = new SqlConnection(new SqlConnectionStringBuilder(connectionString) { Enlist = false }.ConnectionString);
        await db.OpenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await db.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(SessionStateQuery,
                new { session, account, replaced = ThongBaoPhien.DangNhapMayKhac }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return (TrangThaiPhienHienTai)(state ?? (int)TrangThaiPhienHienTai.KhongTonTai);
        }
        catch (SqlException e) when (e.Number == 207) { throw new InvalidOperationException(SchemaPhienCu, e); }
    }

    public Task<TrangThaiPhienHienTai> KiemTraPhienHienTaiAsync(Guid session, string account, CancellationToken cancellationToken = default)
        => KiemTraPhienHienTaiAsync(CauHinhKetNoi.LayChuoiKetNoi(), session, account, cancellationToken);

    // Áp dụng tại SQL trước COUNT/OFFSET để lịch sử cũ không làm sai phân trang/KPI.
    private const string OperationScope="""
        HanhDong NOT IN(N'SESSION_ABNORMAL_END',N'VIEW_LOG',N'EXPORT_LOG',N'VIEW',N'READ',N'SEARCH',N'HEARTBEAT',N'Đối chiếu yêu cầu thanh toán')
        AND HanhDong NOT LIKE N'Tra cứu%' AND HanhDong NOT LIKE N'Xem %' AND HanhDong NOT LIKE N'Tìm kiếm%'
        AND LoaiDoiTuong<>N'YEU_CAU_THANH_TOAN'
        """;
    /// <summary>Trước xác thực: chỉ mã tài khoản thật; tối đa một sự kiện/phút, không ghi input hay mật khẩu.</summary>
    public void GhiDangNhapThatBai(string account)
    {
        if(string.IsNullOrWhiteSpace(account)||account.Length>50)return;
        ThucHien((db,tx)=>
        {
            if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.TAI_KHOAN WITH(HOLDLOCK) WHERE MaTK=@account",new{account},tx)!=1)return false;
            if(db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WITH(UPDLOCK,HOLDLOCK) WHERE MaTK=@account AND HanhDong=N'LOGIN_FAILED' AND ThoiGianUtc>=DATEADD(minute,-1,SYSUTCDATETIME())",new{account},tx)>0)return false;
            db.Execute("""
                INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,ThoiGianUtc,MaPhien,MaSuKien,PhanHe,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua)
                VALUES(@id,@account,SYSDATETIME(),SYSUTCDATETIME(),NULL,NEWID(),N'Phiên đăng nhập',N'LOGIN_FAILED',N'TAI_KHOAN',@account,N'Thất bại')
                """,new{id=TaoMa(db,tx,"NHAT_KY_THAO_TAC","MaNK"),account},tx);
            return true;
        });
    }
    /// <summary>
    /// Yêu cầu giao dịch nghiệp vụ dùng chung, xác thực phiên rồi ghi tên trường đổi trong phạm vi bảng và hành động cho phép.
    /// </summary>
    /// <remarks>Không ghi giá trị trường; loại tên mật khẩu và tên trường không hợp lệ trước khi tạo payload.</remarks>
    public void GhiThayDoi(string table,string id,string action,IEnumerable<string> fields,NguCanhNghiepVu actor,Guid session)
    {
        if(transaction is null)throw new InvalidOperationException("Audit phải dùng chung giao dịch nghiệp vụ.");
        string[] allowed=["NHA_CUNG_CAP","BAO_TRI_THIET_BI","BENH_NHAN","HO_SO_BENH_AN","PHIEU_KHAM_BENH","LICH_KHAM","LICH_SU_KHAM_BENH","TINH_TRANG_BENH","KE_HOACH_DIEU_TRI","CHI_TIET_KE_HOACH","DICH_VU_THUC_HIEN","DON_THUOC","CHI_TIET_DON_THUOC"];
        if(!allowed.Contains(table)||id.Length is <1 or >50||action is not ("CREATE" or "UPDATE" or "CANCEL"))throw new ArgumentException("Sự kiện nghiệp vụ không hợp lệ.");
        ThucHien((db,tx)=>
        {
            var user=DAL_QUYENNGHIEPVU.XacThuc(db,tx,actor);KiemTraPhien(db,tx,user.MaTK,session);
            // Chỉ tên trường, không sao chép giá trị bệnh án, thông tin cá nhân hoặc bí mật.
            var names=fields.Where(f=>f.Length<=128&&f.All(c=>char.IsAsciiLetterOrDigit(c)||c=='_')&&!f.Contains("MatKhau",StringComparison.OrdinalIgnoreCase)).Distinct().ToArray();
            Ghi(db,tx,user.MaTK,session,action,table,table,id,after:JsonSerializer.Serialize(new{TruongDaDoi=names}));
            return true;
        });
    }
    /// <summary>
    /// Lọc payload nhật ký theo các trường được phép của từng loại đối tượng trước khi trả về màn hình.
    /// </summary>
    /// <returns>JSON chỉ chứa trường được phép; nội dung cũ không hợp lệ được thay bằng thông báo đã ẩn.</returns>
    private static object? SafeDetail(string table,object? raw)
    {
        if(raw is null or DBNull)return null;
        var catalog=DanhMucHeThong.TatCa.SingleOrDefault(d=>d.Bang==table);
        try
        {
            using var doc=JsonDocument.Parse(Convert.ToString(raw)??"");
            if(doc.RootElement.ValueKind!=JsonValueKind.Object)return "[Nội dung cũ đã ẩn]";
            var allowed=catalog is null
                ?new[]{"MaNV","MaTK","MaVaiTro","MaPhanHe","MaBL","MaPN","MaPX","MaHH","MaKho","MaLo","MaTB","TrangThai","TrangThaiDuyet","TruongDaDoi"}.ToHashSet()
                :catalog.Khoa.Concat(catalog.Truong.Select(f=>f.Cot)).Append("TrangThai").ToHashSet();
            if(table=="ANH_XQUANG_LAN_KHAM")allowed=new[]{"MaAnh","MaLS","MaDVThucHien","TrangThai"}.ToHashSet();
            if(table=="PHAN_CONG_ANH_XQUANG")allowed=new[]{"MaPC","MaLS","MaDVThucHien","MaNVKyThuat","TrangThai"}.ToHashSet();
            return JsonSerializer.Serialize(doc.RootElement.EnumerateObject().Where(p=>allowed.Contains(p.Name)).ToDictionary(p=>p.Name,p=>p.Value.Clone()));
        }
        catch(JsonException){return "[Nội dung cũ đã ẩn]";}
    }

    /// <summary>
    /// Đếm sự kiện trong ngày theo UTC+7 sau kiểm quyền quản trị; áp dụng cùng phạm vi thao tác như danh sách nhật ký.
    /// </summary>
    public (int DangNhap,int DangNhapThatBai,int ThayDoi) ThongKe(NguCanhNghiepVu actor)=>ThucHien((db,tx)=>
    {
        Admin(db,tx,actor);
        return db.QuerySingle<(int,int,int)>($"""
            DECLARE @start datetime2(0)=DATEADD(hour,-7,CONVERT(datetime2(0),CONVERT(date,DATEADD(hour,7,SYSUTCDATETIME()))));
            SELECT (SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE HanhDong=N'LOGIN_SUCCESS' AND ThoiGianUtc>=@start),
             (SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE HanhDong=N'LOGIN_FAILED' AND ThoiGianUtc>=@start),
             (SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE ThoiGianUtc>=@start AND HanhDong NOT IN(N'LOGIN_SUCCESS',N'LOGIN_FAILED',N'LOGOUT') AND {OperationScope})
            """,transaction:tx);
    });
    /// <summary>
    /// Xác thực lại người dùng trong SQL và yêu cầu quyền quản trị cho các chức năng danh mục, nhật ký.
    /// </summary>
    internal static NguCanhNghiepVu Admin(IDbConnection db, IDbTransaction? tx, NguCanhNghiepVu actor)
    {
        var user = DAL_QUYENNGHIEPVU.XacThuc(db, tx, actor);
        if (!user.LaAdmin) throw new UnauthorizedAccessException("Chỉ quản trị viên được sử dụng chức năng này.");
        return user;
    }

    /// <summary>
    /// Khóa tài khoản trước phiên theo cùng thứ tự với login; yêu cầu claims khớp và heartbeat trong 5 phút gần nhất.
    /// </summary>
    internal static void KiemTraPhien(IDbConnection db, IDbTransaction? tx, string account, Guid session)
    {
        KiemTraClaimsPhien(account, session);
        int? state;
        try
        {
            // Cùng thứ tự khóa account -> session ở mọi giao dịch, kể cả thao tác sửa chính tài khoản đó.
            state = db.QuerySingleOrDefault<int?>("""
                DECLARE @lockedAccount nvarchar(50);
                SELECT @lockedAccount=MaTK FROM dbo.TAI_KHOAN WITH(UPDLOCK,HOLDLOCK) WHERE MaTK=@account;
                """ + SessionStateQuery.Replace("FROM dbo.PHIEN_DANG_NHAP", "FROM dbo.PHIEN_DANG_NHAP WITH(UPDLOCK,HOLDLOCK)"),
                new { session, account, replaced = ThongBaoPhien.DangNhapMayKhac }, tx);
        }
        catch (SqlException e) when (e.Number == 207) { throw new InvalidOperationException(SchemaPhienCu, e); }
        if (state == (int)TrangThaiPhienHienTai.DaDangNhapMayKhac)
            throw new UnauthorizedAccessException(ThongBaoPhien.DangNhapMayKhac);
        if (state != (int)TrangThaiPhienHienTai.DangHoatDong)
            throw new UnauthorizedAccessException("Phiên đã hết hạn; vui lòng đăng nhập lại.");
    }

    /// <summary>
    /// Chèn sự kiện audit bằng thời gian SQL, mã phiên và mã sự kiện mới trên kết nối, giao dịch nơi gọi.
    /// </summary>
    /// <remarks>Đây là hàm ghi nội bộ; nơi gọi chịu trách nhiệm kiểm quyền, phiên và giới hạn payload trước/sau.</remarks>
    internal static void Ghi(IDbConnection db, IDbTransaction? tx, string account, Guid session,
        string action, string module, string table, string id, string? before = null, string? after = null)
    {
        db.Execute("""
            INSERT dbo.NHAT_KY_THAO_TAC
            (MaNK,MaTK,ThoiGian,ThoiGianUtc,MaPhien,MaSuKien,PhanHe,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuTruoc,DuLieuSau)
            VALUES(@nk,@account,SYSDATETIME(),SYSUTCDATETIME(),@session,@eventId,@module,@action,@table,@id,N'Thành công',@before,@after)
            """, new { nk = TaoMa(db, tx, "NHAT_KY_THAO_TAC", "MaNK"), account, session, eventId = Guid.NewGuid(), module, action, table, id, before, after }, tx);
    }

    /// <summary>
    /// Khóa tài khoản, thu hồi phiên đang mở rồi tạo phiên mới trong cùng giao dịch; lần đăng nhập sau cùng giữ quyền.
    /// </summary>
    public Guid BatDauPhien(NguCanhNghiepVu actor) => TaoPhien(actor, null);

    /// <summary>Chỉ khôi phục token được ghi nhớ chưa có phiên kế tiếp; kiểm tra và nhận phiên mới trong cùng khóa tài khoản.</summary>
    public Guid KhoiPhucPhien(Guid rememberedSession, NguCanhNghiepVu actor)
    {
        if (rememberedSession == Guid.Empty) throw new UnauthorizedAccessException("Cần đăng nhập lại để cập nhật phiên đã ghi nhớ.");
        return TaoPhien(actor, rememberedSession);
    }

    private Guid TaoPhien(NguCanhNghiepVu actor, Guid? rememberedSession) => ThucHien((db, tx) =>
    {
        DAL_QUYENNGHIEPVU.KiemTraMa(actor?.MaTK);
        if (db.ExecuteScalar<int>("""
            SELECT CASE WHEN COL_LENGTH(N'dbo.PHIEN_DANG_NHAP',N'PhienKeTiep') IS NOT NULL
              AND EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PHIEN_DANG_NHAP') AND name=N'IX_Rhm_Phien_Account')
              THEN 1 ELSE 0 END
            """, transaction: tx) != 1)
            throw new InvalidOperationException(SchemaPhienCu);
        // UPDLOCK từ đầu tránh hai login cùng giữ S-lock rồi cùng nâng cấp khóa.
        if (db.QuerySingleOrDefault<string>("SELECT MaTK FROM dbo.TAI_KHOAN WITH(UPDLOCK,HOLDLOCK) WHERE MaTK=@MaTK", new { actor!.MaTK }, tx) is null)
            throw new UnauthorizedAccessException("Tài khoản không hợp lệ.");
        var user = DAL_QUYENNGHIEPVU.XacThuc(db, tx, actor);
        if (rememberedSession is not null && db.ExecuteScalar<int>("""
            SELECT COUNT(*) FROM dbo.PHIEN_DANG_NHAP WITH(UPDLOCK,HOLDLOCK)
            WHERE MaPhien=@rememberedSession AND MaTK=@MaTK AND PhienKeTiep IS NULL
              AND (LyDoKetThuc IS NULL OR LyDoKetThuc<>@replaced)
            """, new { rememberedSession, user.MaTK, replaced = ThongBaoPhien.DangNhapMayKhac }, tx) != 1)
            throw new UnauthorizedAccessException(ThongBaoPhien.DangNhapMayKhac);
        var id = Guid.NewGuid();
        // Account UPDLOCK là mutex của mọi thao tác ứng dụng trên tài khoản này.
        // READCOMMITTED ở các UPDATE tránh khóa range sang tài khoản kế bên trong index.
        var oldSessions = db.Query<Guid>("""
            UPDATE s
            SET TrangThaiPhien=N'Kết thúc bất thường',LyDoKetThuc=@replaced,DangXuatUtc=SYSUTCDATETIME()
            OUTPUT inserted.MaPhien
            FROM dbo.PHIEN_DANG_NHAP s WITH(READCOMMITTED,INDEX(IX_Rhm_Phien_Account))
            WHERE s.MaTK=@MaTK AND s.TrangThaiPhien=N'Đang mở'
            """, new { user.MaTK, replaced = ThongBaoPhien.DangNhapMayKhac }, tx).ToArray();
        db.Execute("""
            UPDATE s SET PhienKeTiep=@id
            FROM dbo.PHIEN_DANG_NHAP s WITH(READCOMMITTED,INDEX(IX_Rhm_Phien_Account))
            WHERE s.MaTK=@MaTK AND s.PhienKeTiep IS NULL
            """, new { id, user.MaTK }, tx);
        db.Execute("""
            INSERT dbo.PHIEN_DANG_NHAP(MaPhien,MaTK,DangNhapUtc,HoatDongCuoiUtc,TrangThaiPhien)
            VALUES(@id,@MaTK,SYSUTCDATETIME(),SYSUTCDATETIME(),N'Đang mở')
            """, new { id, user.MaTK },tx);
        foreach (var old in oldSessions)
            Ghi(db, tx, user.MaTK, old, "LOGOUT", "Phiên đăng nhập", "PHIEN_DANG_NHAP", old.ToString(),
                after: JsonSerializer.Serialize(new { TrangThai = "Kết thúc bất thường", LyDoKetThuc = ThongBaoPhien.DangNhapMayKhac }));
        Ghi(db,tx,user.MaTK,id,"LOGIN_SUCCESS","Phiên đăng nhập","PHIEN_DANG_NHAP",id.ToString());
        return id;
    });

    /// <summary>
    /// Kiểm tra người dùng và phiên còn hiệu lực trước khi cập nhật mốc hoạt động bằng thời gian UTC của SQL.
    /// </summary>
    public void Heartbeat(Guid session, NguCanhNghiepVu actor) => ThucHien((db,tx) =>
    {
        var user=DAL_QUYENNGHIEPVU.XacThuc(db,tx,actor);
        KiemTraPhien(db,tx,user.MaTK,session);
        return db.Execute("UPDATE dbo.PHIEN_DANG_NHAP SET HoatDongCuoiUtc=SYSUTCDATETIME() WHERE MaPhien=@session",new{session},tx);
    });

    /// <summary>
    /// Đóng đúng phiên đang mở của tài khoản trong principal và ghi LOGOUT khi cập nhật được một bản.
    /// </summary>
    /// <remarks>Cho phép đóng phiên kể cả tài khoản vừa bị khóa; việc này không cấp quyền ghi nghiệp vụ.</remarks>
    public void KetThucPhien(Guid session) => ThucHien((db,tx) =>
    {
        var account=(Thread.CurrentPrincipal as ClaimsPrincipal)?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Chưa đăng nhập.");
        KiemTraClaimsPhien(account, session);
        // Cho phép đóng đúng phiên kể cả tài khoản vừa bị khóa; không cấp quyền ghi dữ liệu nghiệp vụ.
        int count=db.Execute("""
            UPDATE dbo.PHIEN_DANG_NHAP SET DangXuatUtc=SYSUTCDATETIME(),HoatDongCuoiUtc=SYSUTCDATETIME(),
            TrangThaiPhien=N'Đã đăng xuất',LyDoKetThuc=N'Đóng ứng dụng'
            WHERE MaPhien=@session AND MaTK=@account AND TrangThaiPhien=N'Đang mở'
            """,new{session,account},tx);
        if(count==1)Ghi(db,tx,account,session,"LOGOUT","Phiên đăng nhập","PHIEN_DANG_NHAP",session.ToString());
        return count;
    });

    /// <summary>
    /// Kiểm quyền quản trị, đếm và đọc một trang thao tác hoặc phiên với bộ lọc được áp dụng trực tiếp tại SQL.
    /// </summary>
    /// <param name="filter">Bộ lọc đã kiểm tra; DenUtc là mốc cuối không bao gồm.</param>
    /// <param name="actor">Người dùng cần xác thực lại quyền quản trị.</param>
    /// <param name="sessions">true chọn bảng phiên; false chọn nhật ký và lọc payload trước khi trả về.</param>
    /// <returns>Các dòng của trang yêu cầu cùng tổng số dòng khớp bộ lọc.</returns>
    public TrangNhatKy LayTrang(BoLocNhatKy filter,NguCanhNghiepVu actor,bool sessions=false)
    {
        filter.KiemTra();
        return ThucHien((db,tx)=>
        {
            Admin(db,tx,actor);
            string source=sessions ? "dbo.PHIEN_DANG_NHAP n" : "dbo.NHAT_KY_THAO_TAC n";
            string time=sessions ? "n.DangNhapUtc" : "n.ThoiGianUtc";
            string where=$"""
                WHERE (@TuUtc IS NULL OR {time}>=@TuUtc) AND (@DenUtc IS NULL OR {time}<@DenUtc)
                  AND (@TaiKhoan=N'' OR tk.TenDangNhap LIKE @user OR nv.HoTen LIKE @user OR tk.MaTK=@TaiKhoan)
                """;
            where+=sessions ? " AND (@KetQua=N'' OR n.TrangThaiPhien=@KetQua)" : """
                 AND (@PhanHe=N'' OR n.PhanHe=@PhanHe OR (n.PhanHe IS NULL AND n.LoaiDoiTuong=@PhanHe))
                 AND (@HanhDong=N'' OR n.HanhDong=@HanhDong) AND (@KetQua=N'' OR n.KetQua=@KetQua)
                 AND (@MaDoiTuong=N'' OR n.MaDoiTuong LIKE @object)
                """;
            if(!sessions)where+=" AND "+OperationScope;
            string joins=" JOIN dbo.TAI_KHOAN tk ON tk.MaTK=n.MaTK LEFT JOIN dbo.NHAN_VIEN nv ON nv.MaNV=tk.MaNV ";
            var p=new DynamicParameters(filter);
            p.Add("user","%"+filter.TaiKhoan+"%");p.Add("object","%"+filter.MaDoiTuong+"%");
            p.Add("skip",(filter.Trang-1)*filter.KichThuocTrang);
            string key=sessions ? "n.MaPhien" : "n.MaNK";
            int total=db.ExecuteScalar<int>($"SELECT COUNT(*) FROM {source}{joins}{where}",p,tx);
            // Chi tiết lớn chỉ đọc cho trang đang xem, tối đa 100 hàng. Không tải toàn bộ nhật ký.
            var rows=db.Query($"SELECT n.*,tk.TenDangNhap,nv.HoTen FROM {source}{joins}{where} ORDER BY {time} DESC,{key} DESC OFFSET @skip ROWS FETCH NEXT @KichThuocTrang ROWS ONLY",p,tx)
                .Select(r=>(IDictionary<string,object>)r).ToArray();
            if(!sessions)
            foreach(var row in rows)
            foreach(var col in new[]{"DuLieuTruoc","DuLieuSau"})row[col]=SafeDetail(Convert.ToString(row["LoaiDoiTuong"])??"",row[col])!;
            return new TrangNhatKy(rows,total);
        });
    }
}
