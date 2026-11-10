using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Dapper;
using ET;

namespace DAL;

/// <summary>Audit SQL cùng giao dịch nơi gọi; không nhận payload bệnh án hoặc bí mật.</summary>
public sealed class DAL_NHATKY(IDbTransaction? transaction = null) : dbconnect(transaction)
{
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
    internal static NguCanhNghiepVu Admin(IDbConnection db, IDbTransaction? tx, NguCanhNghiepVu actor)
    {
        var user = DAL_QUYENNGHIEPVU.XacThuc(db, tx, actor);
        if (!user.LaAdmin) throw new UnauthorizedAccessException("Chỉ quản trị viên được sử dụng chức năng này.");
        return user;
    }

    internal static void KiemTraPhien(IDbConnection db, IDbTransaction? tx, string account, Guid session)
    {
        if (session == Guid.Empty || db.ExecuteScalar<int>("""
            SELECT COUNT(*) FROM dbo.PHIEN_DANG_NHAP WITH(UPDLOCK,HOLDLOCK)
            WHERE MaPhien=@session AND MaTK=@account AND TrangThaiPhien=N'Đang mở'
              AND HoatDongCuoiUtc>=DATEADD(minute,-5,SYSUTCDATETIME())
            """, new { session, account }, tx) != 1)
            throw new UnauthorizedAccessException("Phiên đã hết hạn; vui lòng đăng nhập lại.");
    }

    internal static void Ghi(IDbConnection db, IDbTransaction? tx, string account, Guid session,
        string action, string module, string table, string id, string? before = null, string? after = null)
    {
        db.Execute("""
            INSERT dbo.NHAT_KY_THAO_TAC
            (MaNK,MaTK,ThoiGian,ThoiGianUtc,MaPhien,MaSuKien,PhanHe,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuTruoc,DuLieuSau)
            VALUES(@nk,@account,SYSDATETIME(),SYSUTCDATETIME(),@session,@eventId,@module,@action,@table,@id,N'Thành công',@before,@after)
            """, new { nk = TaoMa(db, tx, "NHAT_KY_THAO_TAC", "MaNK"), account, session, eventId = Guid.NewGuid(), module, action, table, id, before, after }, tx);
    }

    public Guid BatDauPhien(NguCanhNghiepVu actor) => ThucHien((db, tx) =>
    {
        var user = DAL_QUYENNGHIEPVU.XacThuc(db, tx, actor);
        // Khóa các phiên quá hạn trước khi tạo phiên mới; không suy diễn giờ đăng xuất.
        db.Execute("""
            UPDATE dbo.PHIEN_DANG_NHAP WITH(UPDLOCK)
            SET TrangThaiPhien=N'Kết thúc bất thường',LyDoKetThuc=N'Không nhận heartbeat quá 5 phút'
            WHERE TrangThaiPhien=N'Đang mở' AND HoatDongCuoiUtc<DATEADD(minute,-5,SYSUTCDATETIME())
            """, transaction:tx);
        var id = Guid.NewGuid();
        db.Execute("""
            INSERT dbo.PHIEN_DANG_NHAP(MaPhien,MaTK,DangNhapUtc,HoatDongCuoiUtc,TrangThaiPhien)
            VALUES(@id,@MaTK,SYSUTCDATETIME(),SYSUTCDATETIME(),N'Đang mở')
            """, new { id, user.MaTK },tx);
        Ghi(db,tx,user.MaTK,id,"LOGIN_SUCCESS","Phiên đăng nhập","PHIEN_DANG_NHAP",id.ToString());
        return id;
    });

    public void Heartbeat(Guid session, NguCanhNghiepVu actor) => ThucHien((db,tx) =>
    {
        var user=DAL_QUYENNGHIEPVU.XacThuc(db,tx,actor);
        KiemTraPhien(db,tx,user.MaTK,session);
        return db.Execute("UPDATE dbo.PHIEN_DANG_NHAP SET HoatDongCuoiUtc=SYSUTCDATETIME() WHERE MaPhien=@session",new{session},tx);
    });

    public void KetThucPhien(Guid session) => ThucHien((db,tx) =>
    {
        var account=(Thread.CurrentPrincipal as ClaimsPrincipal)?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Chưa đăng nhập.");
        // Cho phép đóng đúng phiên kể cả tài khoản vừa bị khóa; không cấp quyền ghi dữ liệu nghiệp vụ.
        int count=db.Execute("""
            UPDATE dbo.PHIEN_DANG_NHAP SET DangXuatUtc=SYSUTCDATETIME(),HoatDongCuoiUtc=SYSUTCDATETIME(),
            TrangThaiPhien=N'Đã đăng xuất',LyDoKetThuc=N'Đóng ứng dụng'
            WHERE MaPhien=@session AND MaTK=@account AND TrangThaiPhien=N'Đang mở'
            """,new{session,account},tx);
        if(count==1)Ghi(db,tx,account,session,"LOGOUT","Phiên đăng nhập","PHIEN_DANG_NHAP",session.ToString());
        return count;
    });

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
