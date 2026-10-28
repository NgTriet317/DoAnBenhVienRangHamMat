using System.Data;
using Dapper;
using ET;

namespace DAL;

/// <summary>Nguồn đọc có phạm vi SQL cho quy trình khám; không lấy cache toàn viện rồi lọc quyền trong UI.</summary>
public sealed partial class DAL_KHAMDIEUTRI : dbconnect
{
    public DAL_KHAMDIEUTRI(IDbTransaction? transaction = null) : base(transaction) { }

    public IReadOnlyList<HangDoiKhamItem> LayHangDoi(BoLocHangDoi boLoc, NguCanhNghiepVu nguoiDung)
        => LayTrangHangDoi(boLoc,nguoiDung).CacDong;

    public LanKhamChiTiet LayChiTiet(string maLS, NguCanhNghiepVu nguoiDung)
        => ThucHien((db, tx) =>
        {
            DAL_QUYENNGHIEPVU.KiemTraMa(maLS);
            var user = DAL_QUYENNGHIEPVU.XacThuc(db, tx, nguoiDung);
            var doctor = user.CoQuyen(QuyenLamViec.Kham);
            var reception = user.CoQuyen(QuyenLamViec.XemHoSo) && user.CoQuyen(QuyenLamViec.TiepNhan);
            var row = db.QuerySingleOrDefault<VisitRead>("""
                SELECT l.*,hs.MaBN,p.MaHS,p.MaBS AS BacSiTiepNhan,p.TrangThai AS TrangThaiPhieu
                FROM dbo.LICH_SU_KHAM_BENH l JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu
                JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS
                WHERE l.MaLS=@maLS AND (@LaAdmin=1 OR
                    (@doctor=1 AND l.MaBS=@MaNV AND p.MaBS=@MaNV)
                    OR (l.TrangThai=N'Đã xác nhận' AND (
                        @reception=1 OR @LaAdmin=1 OR (@doctor=1 AND EXISTS(
                            SELECT 1 FROM dbo.PHIEU_KHAM_BENH own
                            WHERE own.MaHS=p.MaHS AND own.MaBS=@MaNV AND own.TrangThai<>N'Hủy')))))
                """, new { maLS, user.MaNV, doctor, reception, user.LaAdmin }, tx);
            if (row is null) throw DAL_QUYENNGHIEPVU.TuChoi();
            DAL_QUYENNGHIEPVU.GhiTraCuuAdmin(db, tx, user, "LICH_SU_KHAM_BENH", maLS);
            var canEdit = doctor && (user.LaAdmin || row.MaBS == user.MaNV && row.BacSiTiepNhan == user.MaNV) &&
                row.TrangThai == "Nháp" && row.TrangThaiPhieu is "Chờ khám" or "Đang khám";
            return new LanKhamChiTiet(row, row.MaBN, row.MaHS, canEdit);
        });

    public DonThuocChiTiet LayDonThuoc(string maDT, NguCanhNghiepVu nguoiDung)
        => ThucHien((db, tx) =>
        {
            DAL_QUYENNGHIEPVU.KiemTraMa(maDT);
            var user = DAL_QUYENNGHIEPVU.XacThuc(db, tx, nguoiDung);
            var doctor = user.CoQuyen(QuyenLamViec.Kham);
            var pharmacy = user.CoQuyen(QuyenLamViec.GiaoThuoc);
            var reception = user.CoQuyen(QuyenLamViec.XemHoSo) && user.CoQuyen(QuyenLamViec.TiepNhan);
            var row = db.QuerySingleOrDefault<PrescriptionRead>("""
                SELECT d.*,hs.MaBN,l.TrangThai AS TrangThaiLanKham,p.TrangThai AS TrangThaiPhieu,
                    CONVERT(bit,CASE WHEN d.MaBS=@MaNV AND l.MaBS=@MaNV AND p.MaBS=@MaNV THEN 1 ELSE 0 END) AS ChinhBacSi
                FROM dbo.DON_THUOC d JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=d.MaLS
                JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu
                JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS
                WHERE d.MaDT=@maDT AND (@LaAdmin=1 OR
                    (@doctor=1 AND d.MaBS=@MaNV AND l.MaBS=@MaNV AND p.MaBS=@MaNV)
                    OR (l.TrangThai=N'Đã xác nhận' AND d.TrangThai IN(N'Đã xác nhận',N'Ngừng áp dụng') AND (
                        @pharmacy=1 OR @reception=1 OR @LaAdmin=1 OR (@doctor=1 AND EXISTS(
                            SELECT 1 FROM dbo.PHIEU_KHAM_BENH own
                            WHERE own.MaHS=p.MaHS AND own.MaBS=@MaNV AND own.TrangThai<>N'Hủy')))))
                """, new { maDT, user.MaNV, doctor, pharmacy, reception, user.LaAdmin }, tx);
            if (row is null) throw DAL_QUYENNGHIEPVU.TuChoi();
            var lines = db.Query<ET_CHITIETDONTHUOC>("SELECT * FROM dbo.CHI_TIET_DON_THUOC WHERE MaDT=@maDT ORDER BY MaCTDT", new { maDT }, tx).ToArray();
            DAL_QUYENNGHIEPVU.GhiTraCuuAdmin(db, tx, user, "DON_THUOC", maDT);
            var canEdit = doctor && (user.LaAdmin || row.ChinhBacSi) && row.TrangThai == "Nháp" && row.TrangThaiLanKham == "Nháp"
                && row.TrangThaiPhieu is "Chờ khám" or "Đang khám";
            return new DonThuocChiTiet(row, lines, row.MaBN, canEdit);
        });

    private sealed class VisitRead : ET_LICHSUKHAMBENH
    {
        public string MaBN { get; set; } = "";
        public string MaHS { get; set; } = "";
        public string? BacSiTiepNhan { get; set; }
        public string TrangThaiPhieu { get; set; } = "";
    }

    private sealed class PrescriptionRead : ET_DONTHUOC
    {
        public string MaBN { get; set; } = "";
        public string TrangThaiLanKham { get; set; } = "";
        public string TrangThaiPhieu { get; set; } = "";
        public bool ChinhBacSi { get; set; }
    }
}
