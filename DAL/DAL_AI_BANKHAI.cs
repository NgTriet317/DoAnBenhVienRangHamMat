using Dapper;
using ET;
using System.Data;

namespace DAL;

public sealed class DAL_AI_BANKHAI : dbconnect
{
    public KetQuaLuuBanKhaiAI Luu(YeuCauLuuBanKhaiAI m) => ThucHien((db, tx) =>
    {
            bool coLoaiKham = db.ExecuteScalar<int>(
                "SELECT CASE WHEN COL_LENGTH(N'dbo.AI_TIEP_NHAN_NHAP',N'LoaiKham') IS NULL THEN 0 ELSE 1 END",
                transaction: tx) == 1;
            if (!coLoaiKham)
                throw new InvalidOperationException("Cần cài SQL/17_AI_BanKhai_NhanVienDuyet.sql trước khi gửi bản khai.");

            byte[] tokenHash = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("RHM-AI-DRAFT:" + m.MaPhien.ToString("N")));
            var existing = db.QuerySingleOrDefault<SessionStatus>("""
                SELECT TrangThai,MaPhieu,HetHan,TokenHash,MaKiosk FROM dbo.AI_PHIEN_TIEP_NHAN WITH(UPDLOCK,HOLDLOCK)
                WHERE MaPhien=@MaPhien
                """, new { m.MaPhien }, tx);
            if (existing is not null)
            {
                if (existing.MaPhieu is not null || existing.TrangThai is "DaDuyet" or "DaHuy" or "HetHan"
                    || existing.HetHan <= DateTime.UtcNow || existing.MaKiosk != "WPF-LOCAL"
                    || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(existing.TokenHash, tokenHash))
                    throw new InvalidOperationException("Phiên này đã được nhân viên xử lý, không thể gửi lại.");
                if (db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.AI_TIEP_NHAN_NHAP WITH(UPDLOCK,HOLDLOCK) WHERE MaPhien=@MaPhien",
                    new { m.MaPhien }, tx) != 1)
                    throw new InvalidOperationException("Bản khai của phiên không đầy đủ; vui lòng gặp nhân viên.");
                db.Execute("""
                    UPDATE dbo.AI_PHIEN_TIEP_NHAN
                       SET TrangThai=@TrangThai,Buoc=N'done',DongY=1,NgayCapNhat=SYSUTCDATETIME(),
                           HetHan=DATEADD(day,30,SYSUTCDATETIME())
                     WHERE MaPhien=@MaPhien;
                    UPDATE dbo.AI_TIEP_NHAN_NHAP
                       SET HoTen=@HoTen,NgaySinh=@NgaySinh,SoDienThoai=@SoDienThoai,
                           DaTungKham=@DaTungKham,LyDoKham=@LyDoKham,TrieuChung=@TrieuChung,
                           CanHoTroGap=@CanHoTroGap,LoaiKham=@LoaiKham,NgayCapNhat=SYSUTCDATETIME()
                     WHERE MaPhien=@MaPhien;
                    """, new { m.MaPhien, m.HoTen, NgaySinh=m.NgaySinh?.ToDateTime(TimeOnly.MinValue), m.SoDienThoai,
                    m.DaTungKham, m.LyDoKham, m.TrieuChung, m.CanHoTroGap,
                    m.LoaiKham, TrangThai=m.CanHoTroGap ? "CanHoTro" : "ChoNhanVien" }, tx);
            }
            else
            {
                db.Execute("""
                    INSERT dbo.AI_PHIEN_TIEP_NHAN
                        (MaPhien,MaKiosk,TokenHash,Buoc,TrangThai,DongY,HetHan,MaPhieu,MaNVXacNhan)
                    VALUES(@MaPhien,N'WPF-LOCAL',@TokenHash,N'done',@TrangThai,1,
                           DATEADD(day,30,SYSUTCDATETIME()),NULL,NULL);
                    INSERT dbo.AI_TIEP_NHAN_NHAP
                        (MaPhien,DaTungKham,HoTen,NgaySinh,SoDienThoai,LyDoKham,TrieuChung,CanHoTroGap,LoaiKham)
                    VALUES(@MaPhien,@DaTungKham,@HoTen,@NgaySinh,@SoDienThoai,@LyDoKham,
                           @TrieuChung,@CanHoTroGap,@LoaiKham);
                    """, new { m.MaPhien, TokenHash=tokenHash, m.DaTungKham, m.HoTen,
                    NgaySinh=m.NgaySinh?.ToDateTime(TimeOnly.MinValue), m.SoDienThoai, m.LyDoKham,
                    m.TrieuChung, m.CanHoTroGap, m.LoaiKham,
                    TrangThai=m.CanHoTroGap ? "CanHoTro" : "ChoNhanVien" }, tx);
            }
            return new KetQuaLuuBanKhaiAI("saved", "AI-" + m.MaPhien.ToString("N")[..8].ToUpperInvariant(), m.MaPhien);
    });

    private sealed class SessionStatus
    {
        public string TrangThai { get; set; } = "";
        public string? MaPhieu { get; set; }
        public DateTime HetHan { get; set; }
        public byte[] TokenHash { get; set; } = [];
        public string MaKiosk { get; set; } = "";
    }
}
