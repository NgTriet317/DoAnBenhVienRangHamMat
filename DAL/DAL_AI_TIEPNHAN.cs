using Dapper;
using ET;
using System.Data;

namespace DAL;

public sealed class DAL_AI_TIEPNHAN : dbconnect
{
    public bool XacThucThietBi(string thumbprint) => ThucHien((db, tx) =>
        db.ExecuteScalar<int>("""
            SELECT COUNT(*) FROM dbo.AI_SERVICE_IDENTITY
             WHERE CertificateThumbprint=@thumbprint AND Quyen=N'AI_TAO_PHIEU' AND TrangThai=1
            """, new { thumbprint }, tx) == 1);

    public KetQuaTaoPhieuAI? LayTheoIdempotency(string thumbprint, Guid key) => ThucHien((db, tx) =>
        db.QuerySingleOrDefault<KetQuaTaoPhieuAI>("""
            SELECT N'created' AS Status,p.MaPhieu,p.SoThuTu,p.MaPhong,p.MaBS,
                   ph.TenPhong AS Phong,nv.HoTen AS BacSi,
                   TODATETIMEOFFSET(p.ThoiGianTiepNhan,DATEPART(TZOFFSET,SYSDATETIMEOFFSET())) AS ThoiGianTiepNhan,p.TrangThai,
                   CONVERT(bit,0) AS RequiresStaff
              FROM dbo.AI_YEU_CAU_TAO_PHIEU r
              JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=r.MaPhieu
              JOIN dbo.PHONG ph ON ph.MaPhong=p.MaPhong
              LEFT JOIN dbo.NHAN_VIEN nv ON nv.MaNV=p.MaBS
              JOIN dbo.AI_SERVICE_IDENTITY s ON s.MaAIService=r.MaAIService
             WHERE r.IdempotencyKey=@key AND r.TrangThai=N'Created'
               AND s.CertificateThumbprint=@thumbprint AND s.Quyen=N'AI_TAO_PHIEU' AND s.TrangThai=1
            """, new { thumbprint, key }, tx));

    public IReadOnlyList<LuaChonTiepNhanAI> LayBacSi(DateOnly ngay) => ThucHien((db, tx) =>
        db.Query<LuaChonTiepNhanAI>("""
            DECLARE @now datetime2(0)=SYSDATETIME();
            WITH shifts AS
            (
                SELECT lv.MaNV,lv.MaPhong,lv.NgayLam,c.GioBatDau,c.GioKetThuc,c.TrangThai,
                       DATEADD(day,CASE WHEN c.GioKetThuc<=c.GioBatDau THEN 1 ELSE 0 END,
                         DATEADD(second,DATEDIFF(second,CAST('00:00:00' AS time),c.GioKetThuc),CAST(lv.NgayLam AS datetime2(0)))) EndsAt
                  FROM dbo.LICH_LAM_VIEC lv JOIN dbo.CA_LAM c ON c.MaCa=lv.MaCa
                 WHERE lv.NgayLam BETWEEN DATEADD(day,-1,@ngay) AND @ngay
                   AND lv.TrangThai=N'Đã xác nhận' AND lv.TrangThaiHienDien NOT IN(N'Vắng mặt',N'Nghỉ phép')
            )
            SELECT DISTINCT n.MaNV AS Id,n.HoTen AS Name,ph.MaPhong AS RoomId,ph.TenPhong AS RoomName
              FROM shifts s JOIN dbo.NHAN_VIEN n ON n.MaNV=s.MaNV
              JOIN dbo.PHONG ph ON ph.MaPhong=s.MaPhong AND ph.MaKhoa=n.MaCK AND ph.TrangThai=N'Đang hoạt động'
             WHERE CONVERT(date,@now)=@ngay AND s.TrangThai=N'Đang hoạt động'
               AND @now>=DATEADD(second,DATEDIFF(second,CAST('00:00:00' AS time),s.GioBatDau),CAST(s.NgayLam AS datetime2(0)))
               AND @now<s.EndsAt AND ph.LoaiPhong LIKE N'Phòng khám%'
               AND n.MaChucVu=N'CV002' AND n.TrangThai=N'Đang làm việc'
               AND EXISTS(SELECT 1 FROM dbo.TAI_KHOAN tk WHERE tk.MaNV=n.MaNV AND tk.TrangThai=N'Đang hoạt động')
               AND NOT EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI d WHERE d.MaNV=n.MaNV AND d.TrangThaiDuyet=N'Đã duyệt' AND @ngay BETWEEN d.TuNgay AND d.DenNgay)
             ORDER BY n.HoTen,n.MaNV;
            """, new { ngay = ngay.ToDateTime(TimeOnly.MinValue) }, tx).ToArray());

    public IReadOnlyList<LuaChonTiepNhanAI> LayDichVu(string maBS) => ThucHien((db, tx) =>
        db.Query<LuaChonTiepNhanAI>("""
            DECLARE @now datetime2(0)=SYSDATETIME();
            SELECT d.MaDV AS Id,d.TenDV AS Name,CAST(NULL AS nvarchar(50)) AS RoomId,
                   CAST(NULL AS nvarchar(255)) AS RoomName
              FROM dbo.DICH_VU d JOIN dbo.NHAN_VIEN n ON n.MaCK=d.MaCK
             WHERE n.MaNV=@maBS AND n.MaChucVu=N'CV002' AND n.TrangThai=N'Đang làm việc'
               AND EXISTS(SELECT 1 FROM dbo.TAI_KHOAN tk WHERE tk.MaNV=n.MaNV AND tk.TrangThai=N'Đang hoạt động')
               AND d.TrangThai=N'Đang hoạt động' AND d.NgayApDung<=CONVERT(date,@now)
               AND EXISTS(SELECT 1 FROM dbo.LICH_LAM_VIEC lv JOIN dbo.CA_LAM c ON c.MaCa=lv.MaCa
                 JOIN dbo.PHONG ph ON ph.MaPhong=lv.MaPhong AND ph.MaKhoa=n.MaCK AND ph.TrangThai=N'Đang hoạt động'
                 WHERE lv.MaNV=n.MaNV AND lv.NgayLam BETWEEN DATEADD(day,-1,CONVERT(date,@now)) AND CONVERT(date,@now)
                   AND lv.TrangThai=N'Đã xác nhận' AND lv.TrangThaiHienDien NOT IN(N'Vắng mặt',N'Nghỉ phép')
                   AND c.TrangThai=N'Đang hoạt động'
                   AND @now>=DATEADD(second,DATEDIFF(second,CAST('00:00:00' AS time),c.GioBatDau),CAST(lv.NgayLam AS datetime2(0)))
                   AND @now<DATEADD(day,CASE WHEN c.GioKetThuc<=c.GioBatDau THEN 1 ELSE 0 END,
                     DATEADD(second,DATEDIFF(second,CAST('00:00:00' AS time),c.GioKetThuc),CAST(lv.NgayLam AS datetime2(0))))
                   AND ph.LoaiPhong LIKE N'Phòng khám%'
                   AND NOT EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI dn WHERE dn.MaNV=n.MaNV AND dn.TrangThaiDuyet=N'Đã duyệt' AND CONVERT(date,@now) BETWEEN dn.TuNgay AND dn.DenNgay))
             ORDER BY d.TenDV,d.MaDV;
            """, new { maBS }, tx).ToArray());
}
