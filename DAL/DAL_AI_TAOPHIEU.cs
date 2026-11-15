using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using Dapper;
using ET;

namespace DAL;

// Tất cả câu SQL có tham số. Không ghi dữ liệu bệnh án từ nội dung AI mà chưa được duyệt.
public class DAL_AI_TAOPHIEU : dbconnect
{
    public DAL_AI_TAOPHIEU(IDbTransaction? transaction = null) : base(transaction) { }

    public List<ET_AI_DANHMUC> layPhong() => ThucHien((db, tx) => db.Query<ET_AI_DANHMUC>("""
        SELECT MaPhong AS Ma,TenPhong AS Ten
        FROM dbo.PHONG WHERE TrangThai=N'Đang hoạt động' AND LoaiPhong LIKE N'Phòng khám%'
        ORDER BY TenPhong
        """, transaction: tx).ToList());

    public List<ET_AI_DANHMUC> layBacSi(string maPhong, bool khamDichVu = false) => ThucHien((db, tx) =>
        BacSiTaiThoiDiem(db, tx, maPhong, db.ExecuteScalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())", transaction: tx), khamDichVu)
            .Select(n => new ET_AI_DANHMUC { Ma = n.MaNV, Ten = n.HoTen }).ToList());

    // Cùng điều kiện ca/phòng/chuyên khoa/hiện diện với tiếp nhận thủ công và thủ tục duyệt AI.
    private static List<BacSiTiepNhan> BacSiTaiThoiDiem(IDbConnection db, IDbTransaction? tx, string maPhong, DateTime now, bool khamDichVu)
        => db.Query<BacSiTiepNhan>("""
        WITH shifts AS
        (
            SELECT l.MaNV,l.MaPhong,l.TrangThai,l.TrangThaiHienDien,c.TrangThai AS CaStatus,
                   DATEADD(second,DATEDIFF(second,CAST('00:00:00' AS time),c.GioBatDau),CAST(l.NgayLam AS datetime2(0))) AS Begins,
                   DATEADD(day,CASE WHEN c.GioKetThuc<=c.GioBatDau THEN 1 ELSE 0 END,
                       DATEADD(second,DATEDIFF(second,CAST('00:00:00' AS time),c.GioKetThuc),CAST(l.NgayLam AS datetime2(0)))) AS Ends
            FROM dbo.LICH_LAM_VIEC l JOIN dbo.CA_LAM c ON c.MaCa=l.MaCa
            WHERE l.NgayLam BETWEEN DATEADD(day,-1,CAST(@now AS date)) AND CAST(@now AS date)
        )
        SELECT DISTINCT n.MaNV,n.HoTen,n.MaCK,p.MaPhong,p.TenPhong
        FROM shifts s JOIN dbo.NHAN_VIEN n ON n.MaNV=s.MaNV
        JOIN dbo.CHUYEN_KHOA ck ON ck.MaCK=n.MaCK
        JOIN dbo.PHONG p ON p.MaPhong=s.MaPhong AND p.MaKhoa=ck.MaKhoa
        WHERE p.MaPhong=@MaPhong AND p.TrangThai=N'Đang hoạt động' AND p.LoaiPhong LIKE N'Phòng khám%'
          AND s.TrangThai=N'Đã xác nhận' AND s.CaStatus=N'Đang hoạt động'
          AND @now>=s.Begins AND @now<s.Ends
          AND ((@khamDichVu=1 AND s.TrangThaiHienDien=N'Có mặt')
               OR (@khamDichVu=0 AND s.TrangThaiHienDien NOT IN(N'Vắng mặt',N'Nghỉ phép')))
          AND n.MaChucVu=N'CV002' AND n.TrangThai=N'Đang làm việc'
          AND EXISTS(SELECT 1 FROM dbo.TAI_KHOAN t WHERE t.MaNV=n.MaNV AND t.TrangThai=N'Đang hoạt động')
          AND NOT EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI d WHERE d.MaNV=n.MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
                         AND CAST(@now AS date) BETWEEN d.TuNgay AND d.DenNgay)
        ORDER BY n.HoTen,n.MaNV
        """, new { MaPhong = maPhong, now, khamDichVu }, tx).ToList();

    public List<ET_AI_DANHMUC> layDichVu(string maPhong, string? maBS) => ThucHien((db, tx) =>
    {
        var now = db.ExecuteScalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())", transaction: tx);
        var doctor = BacSiTaiThoiDiem(db, tx, maPhong, now, true).SingleOrDefault(n => n.MaNV == maBS);
        if (doctor is null) return new List<ET_AI_DANHMUC>();
        return db.Query<ET_AI_DANHMUC>("""
        SELECT dv.MaDV AS Ma,dv.TenDV AS Ten
        FROM dbo.DICH_VU dv
        WHERE dv.MaCK=@MaCK AND dv.TrangThai=N'Đang hoạt động' AND dv.NgayApDung<=CAST(@now AS date)
        ORDER BY dv.TenDV,dv.MaDV
        """, new { doctor.MaCK, now }, tx).ToList();
    });

    public List<ET_AI_BENHNHANTIM> timBenhNhan(string tim) => ThucHien((db, tx) => db.Query<ET_AI_BENHNHANTIM>("""
        SELECT TOP (25) MaBN,HoTenBN,NgaySinh,SDT
        FROM dbo.BENH_NHAN
        WHERE HoTenBN LIKE N'%' + @Tim + N'%' OR SDT LIKE N'%' + @Tim + N'%'
              OR MaBN=@Tim
        ORDER BY HoTenBN, MaBN
        """, new { Tim = tim }, tx).ToList());

    // Tự đối chiếu: không dùng câu trả lời "đã từng khám" làm điều kiện tạo hồ sơ.
    // Có dữ liệu nghi trùng thì không tự chọn nhầm bệnh nhân.
    public List<ET_AI_BENHNHANTIM> doiChieuBenhNhan(string hoTen, DateTime ngaySinh, string sdt)
        => ThucHien((db, tx) => db.Query<ET_AI_BENHNHANTIM>("""
            DECLARE @SDTChuan nvarchar(50) = REPLACE(REPLACE(REPLACE(REPLACE(
                ISNULL(@SDT,N''),N' ',N''),N'-',N''),N'.',N''),N'(',N'');
            SET @SDTChuan=REPLACE(@SDTChuan,N')',N'');
            SELECT TOP (30) b.MaBN,b.HoTenBN,b.NgaySinh,b.SDT,
                   CASE WHEN LTRIM(RTRIM(b.HoTenBN))=@HoTen AND b.NgaySinh=@NgaySinh
                         AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(b.SDT,N''),
                             N' ',N''),N'-',N''),N'.',N''),N'(',N''),N')',N'')=@SDTChuan
                        THEN 2 ELSE 1 END AS MucDoKhop
            FROM dbo.BENH_NHAN AS b
            WHERE (LTRIM(RTRIM(b.HoTenBN))=@HoTen AND b.NgaySinh=@NgaySinh)
               OR (@SDTChuan<>N'' AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(b.SDT,N''),
                   N' ',N''),N'-',N''),N'.',N''),N'(',N''),N')',N'')=@SDTChuan)
            ORDER BY MucDoKhop DESC,b.MaBN;
            """,new { HoTen=hoTen.Trim(), NgaySinh=ngaySinh.Date, SDT=sdt.Trim() },tx).ToList());

    public ET_AI_KETQUATAO taoPhieu(ET_AI_TAOPHIEU m)
    {
        // MaNV trong biểu mẫu không phải bằng chứng xác thực của người duyệt.
        var identities = (System.Threading.Thread.CurrentPrincipal as ClaimsPrincipal)?.Identities
            .Where(i => i.IsAuthenticated && i.AuthenticationType == DinhDanhNghiepVu.KieuXacThuc).ToArray();
        if (identities?.Length != 1) throw DAL_QUYENNGHIEPVU.TuChoi();
        var accountClaims = identities[0].FindAll(ClaimTypes.NameIdentifier).ToArray();
        var employeeClaims = identities[0].FindAll(DinhDanhNghiepVu.MaNhanVienClaim).ToArray();
        var sessionClaims = identities[0].FindAll("RHM.MaPhien").ToArray();
        if (accountClaims.Length != 1 || string.IsNullOrWhiteSpace(accountClaims[0].Value)
            || employeeClaims.Length != 1 || employeeClaims[0].Value != m.MaNV
            || sessionClaims.Length != 1 || !Guid.TryParse(sessionClaims[0].Value, out var session)
            || session == Guid.Empty)
            throw DAL_QUYENNGHIEPVU.TuChoi();

        // Procedure tự quản lý transaction (không lồng transaction của ThucHien).
        // Duyệt lại cùng mã AI sẽ trả cùng MaPhieu thay vì tạo thêm lượt khám.
        using var db = MoKetNoi();
        var actor = DAL_QUYENNGHIEPVU.XacThuc(db, null, new NguCanhNghiepVu
        {
            MaNV = employeeClaims[0].Value,
            MaTK = accountClaims[0].Value
        });
        if (!actor.CoQuyen(QuyenLamViec.TiepNhan)) throw DAL_QUYENNGHIEPVU.TuChoi();
        var args = new DynamicParameters();
        args.Add("MaPhien", m.MaPhien);
        args.Add("MaNV", actor.MaNV);
        args.Add("MaBNHienCo", m.MaBNHienCo);
        args.Add("HoTen", m.HoTen.Trim());
        args.Add("NgaySinh", m.NgaySinh.Date, DbType.Date);
        args.Add("SDT", m.SDT.Trim());
        args.Add("LyDoKham", m.LyDoKham.Trim());
        args.Add("LoaiKham", m.LoaiKham);
        args.Add("MaPhong", m.MaPhong);
        args.Add("MaBS", m.MaBS);
        args.Add("MaDV", m.MaDV);
        args.Add("MaBNOut", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
        args.Add("MaHSOut", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
        args.Add("MaPhieuOut", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
        args.Add("SoThuTuOut", dbType: DbType.Int32, direction: ParameterDirection.Output);
        db.Execute("dbo.AI_TaoPhieuTuBanKhai",args,
            commandType: CommandType.StoredProcedure, commandTimeout: 30);
        return new ET_AI_KETQUATAO
        {
            MaBN = args.Get<string>("MaBNOut") ?? "",
            MaHS = args.Get<string>("MaHSOut") ?? "",
            MaPhieu = args.Get<string>("MaPhieuOut") ?? "",
            SoThuTu = args.Get<int>("SoThuTuOut")
        };
    }
}
