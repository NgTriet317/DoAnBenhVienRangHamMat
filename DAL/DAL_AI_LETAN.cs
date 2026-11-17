using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using Dapper;
using ET;

namespace DAL;

/// <summary>
/// Chuẩn bị phiên tiếp nhận AI trên SQL thật. Việc tạo PHIEU_KHAM_BENH luôn được
/// thực hiện bởi DAL_AI_TAOPHIEU.taoPhieu / dbo.AI_TaoPhieuTuBanKhai.
/// </summary>
public sealed class DAL_AI_LETAN : dbconnect
{
    private static NguCanhNghiepVu XacNhanNguoiTiepNhan(IDbConnection db, IDbTransaction? tx, string maNV)
    {
        var principal = Thread.CurrentPrincipal as ClaimsPrincipal;
        var identities = principal?.Identities.Where(i => i.IsAuthenticated &&
            i.AuthenticationType == DinhDanhNghiepVu.KieuXacThuc).ToArray();
        if (identities?.Length != 1) throw DAL_QUYENNGHIEPVU.TuChoi();
        var claims = identities[0].FindAll(DinhDanhNghiepVu.MaNhanVienClaim).ToArray();
        var accounts = identities[0].FindAll(ClaimTypes.NameIdentifier).ToArray();
        if (claims.Length != 1 || accounts.Length != 1 || claims[0].Value != maNV)
            throw DAL_QUYENNGHIEPVU.TuChoi();
        var actor = DAL_QUYENNGHIEPVU.XacThuc(db, tx,
            new NguCanhNghiepVu { MaNV = maNV, MaTK = accounts[0].Value });
        if (!actor.CoQuyen(QuyenLamViec.TiepNhan)) throw DAL_QUYENNGHIEPVU.TuChoi();
        return actor;
    }

    public List<ET_AI_PHONG_LETAN> LayPhongDangCho(string maKhoa) => ThucHien((db, tx) =>
        db.Query<ET_AI_PHONG_LETAN>("""
            SELECT p.MaPhong,p.TenPhong,p.MaKhoa,
                   CONVERT(int,COUNT(pk.MaPhieu)) AS SoDangCho
            FROM dbo.PHONG p
            LEFT JOIN dbo.PHIEU_KHAM_BENH pk ON pk.MaPhong=p.MaPhong
              AND pk.TrangThai=N'Chờ khám'
              AND pk.ThoiGianTiepNhan>=CONVERT(date,SYSDATETIME())
              AND pk.ThoiGianTiepNhan<DATEADD(day,1,CONVERT(date,SYSDATETIME()))
            WHERE p.MaKhoa=@maKhoa AND p.TrangThai=N'Đang hoạt động'
              AND p.LoaiPhong LIKE N'Phòng khám%'
            GROUP BY p.MaPhong,p.TenPhong,p.MaKhoa
            ORDER BY COUNT(pk.MaPhieu),p.MaPhong
            """, new { maKhoa }, tx).ToList());

    public string TraCuu(string queryType) => ThucHien((db, tx) =>
    {
        if (queryType == "BANG_GIA")
        {
            var data = db.Query<ET_AI_BANGGIA_LETAN>("""
                SELECT TOP (3) TenDV,DonGia FROM dbo.DICH_VU
                WHERE TrangThai=N'Đang hoạt động' AND NgayApDung<=CONVERT(date,SYSDATETIME())
                ORDER BY TenDV,MaDV
                """, transaction: tx).ToList();
            return data.Count == 0 ? "Chưa có bảng giá; hỏi quầy." :
                string.Join("; ", data.Select(x => $"{x.TenDV}:{x.DonGia:N0}đ"));
        }
        if (queryType == "THOI_GIAN_CHO")
        {
            var q = db.Query<ET_AI_CHO_LETAN>("""
                SELECT TOP (3) p.TenPhong,COUNT(k.MaPhieu) SoCho
                FROM dbo.PHONG p LEFT JOIN dbo.PHIEU_KHAM_BENH k ON k.MaPhong=p.MaPhong
                  AND k.TrangThai=N'Chờ khám' AND k.ThoiGianTiepNhan>=CONVERT(date,SYSDATETIME())
                WHERE p.TrangThai=N'Đang hoạt động' AND p.LoaiPhong LIKE N'Phòng khám%'
                GROUP BY p.MaPhong,p.TenPhong ORDER BY COUNT(k.MaPhieu),p.TenPhong
                """, transaction: tx).ToList();
            return string.Join("; ", q.Select(x => $"{x.TenPhong}:{x.SoCho} chờ"));
        }
        if (queryType == "VI_TRI_PHONG")
        {
            var q = db.Query<string>("""
                SELECT TOP (3) TenPhong FROM dbo.PHONG
                WHERE TrangThai=N'Đang hoạt động' AND LoaiPhong LIKE N'Phòng khám%'
                ORDER BY TenPhong
                """, transaction: tx).ToList();
            return string.Join("; ", q);
        }
        return "BHYT: cần nhân viên tiếp nhận kiểm tra giấy tờ và quyền lợi; không tự cam kết chi trả.";
    });

    /// <summary>
    /// Lưu dữ liệu người bệnh đã xác nhận vào AI_PHIEN_TIEP_NHAN / AI_TIEP_NHAN_NHAP.
    /// Một MaPhien duy nhất được cấp cho một lần xác nhận; retry cùng nội dung là idempotent.
    /// </summary>
    public void DamBaoPhienChoTaoPhieu(ET_AI_TAOPHIEU phieu)
    {
        if (phieu.MaPhien == Guid.Empty || string.IsNullOrWhiteSpace(phieu.HoTen) ||
            phieu.NgaySinh.Year < 1900 || phieu.NgaySinh.Date > DateTime.Today ||
            string.IsNullOrWhiteSpace(phieu.SDT) || string.IsNullOrWhiteSpace(phieu.LyDoKham))
            throw new ArgumentException("Thiếu thông tin xác nhận bệnh nhân.");
        ThucHien((db, tx) =>
        {
            var actor = XacNhanNguoiTiepNhan(db, tx, phieu.MaNV);
            var existing = db.QuerySingleOrDefault<ET_AI_PHIEN_LETAN>("""
                SELECT p.TrangThai,n.HoTen,n.NgaySinh,n.SoDienThoai,n.LyDoKham
                FROM dbo.AI_PHIEN_TIEP_NHAN p WITH (UPDLOCK,HOLDLOCK)
                JOIN dbo.AI_TIEP_NHAN_NHAP n WITH (UPDLOCK,HOLDLOCK) ON n.MaPhien=p.MaPhien
                WHERE p.MaPhien=@MaPhien
                """, new { phieu.MaPhien }, tx);
            if (existing is not null)
            {
                if (existing.TrangThai is not ("ChoNhanVien" or "DaDuyet") ||
                    existing.HoTen != phieu.HoTen || existing.NgaySinh?.Date != phieu.NgaySinh.Date ||
                    existing.SoDienThoai != phieu.SDT || existing.LyDoKham != phieu.LyDoKham)
                    throw new InvalidOperationException("Phiên AI đã thay đổi thông tin; không tự tạo phiếu khác.");
                return true;
            }
            byte[] hash = SHA256.HashData(RandomNumberGenerator.GetBytes(32));
            db.Execute("""
                INSERT dbo.AI_PHIEN_TIEP_NHAN
                  (MaPhien,MaKiosk,TokenHash,Buoc,TrangThai,DongY,NgayTao,NgayCapNhat,HetHan)
                VALUES(@MaPhien,N'WPF-LETAN',@hash,N'XacNhan',N'ChoNhanVien',1,
                       SYSUTCDATETIME(),SYSUTCDATETIME(),DATEADD(minute,30,SYSUTCDATETIME()));
                INSERT dbo.AI_TIEP_NHAN_NHAP
                  (MaPhien,HoTen,NgaySinh,SoDienThoai,LyDoKham,TrieuChung,CanHoTroGap)
                VALUES(@MaPhien,@HoTen,@NgaySinh,@SDT,@LyDoKham,@LyDoKham,0);
                INSERT dbo.AI_SU_KIEN(MaPhien,LoaiSuKien,MaNV)
                VALUES(@MaPhien,N'WpfTuDongXacNhan',@MaNV);
                """, new { phieu.MaPhien, hash, phieu.HoTen, phieu.NgaySinh,
                          phieu.SDT, phieu.LyDoKham, MaNV = actor.MaNV }, tx);
            return true;
        });
    }

    public ET_AI_PHIEU_LETAN? LayPhieuDaTao(ET_AI_TAOPHIEU phieu) => ThucHien((db, tx) =>
    {
        XacNhanNguoiTiepNhan(db, tx, phieu.MaNV);
        var saved = db.QuerySingleOrDefault<ET_AI_PHIEU_LETAN>("""
            SELECT k.MaPhieu,p.SoThuTu,p.MaPhong,ph.TenPhong,
                   k.HoTenXacNhan AS HoTen,k.NgaySinhXacNhan AS NgaySinh,
                   k.SDTXacNhan AS SoDienThoai,k.LyDoKhamXacNhan AS LyDoKham,
                   p.ThoiGianTiepNhan AS NgayTao,
                   (SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH q
                    WHERE q.MaPhong=p.MaPhong AND q.TrangThai=N'Chờ khám'
                      AND CONVERT(date,q.ThoiGianTiepNhan)=CONVERT(date,p.ThoiGianTiepNhan)
                      AND q.SoThuTu<p.SoThuTu) AS SoDangTruoc
            FROM dbo.AI_KET_QUA_TAO_PHIEU k
            JOIN dbo.AI_PHIEN_TIEP_NHAN s ON s.MaPhien=k.MaPhien AND s.TrangThai=N'DaDuyet'
            JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=k.MaPhieu
            JOIN dbo.PHONG ph ON ph.MaPhong=p.MaPhong
            WHERE k.MaPhien=@MaPhien
            """, new { phieu.MaPhien }, tx);
        if (saved is not null && (saved.HoTen != phieu.HoTen || saved.NgaySinh.Date != phieu.NgaySinh.Date ||
            saved.SoDienThoai != phieu.SDT || saved.LyDoKham != phieu.LyDoKham))
            throw new InvalidOperationException("Phiên AI đã thay đổi thông tin; không tự tạo phiếu khác.");
        return saved;
    });

    public void GhiNguonGocTuDong(string maPhieu, string maNV)
    {
        ThucHien((db, tx) =>
        {
            XacNhanNguoiTiepNhan(db, tx, maNV);
            db.Execute("""
                UPDATE dbo.PHIEU_KHAM_BENH
                SET GhiChu=N'Tiếp nhận qua trợ lý AI WPF; bệnh nhân đã xác nhận trên màn hình. Cần bác sĩ kiểm tra lâm sàng.'
                WHERE MaPhieu=@maPhieu AND MaNVTiepNhan=@maNV
                  AND GhiChu=N'Tiếp nhận từ bản khai AI; đã đối chiếu bởi nhân viên.'
                """, new { maPhieu, maNV }, tx);
            return true;
        });
    }
}
