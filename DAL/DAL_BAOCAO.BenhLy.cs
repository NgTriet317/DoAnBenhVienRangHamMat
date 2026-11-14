using Dapper;
using ET;

namespace DAL;

/// <summary>
/// Phần thống kê bệnh lý theo mã đã ghi nhận trên lượt xác nhận, kèm chỉ số chất lượng dữ liệu.
/// </summary>
public sealed partial class DAL_BAOCAO
{
    // Một hàng/lượt, độc lập số tình trạng và dịch vụ để tránh nhân bản mẫu số.
    private const string LuotKham = """
        WITH Luot AS (
          SELECT ls.MaLS,hs.MaBN,ls.ThoiGianKham,
            CASE WHEN EXISTS(SELECT 1 FROM dbo.TINH_TRANG_BENH tt WHERE tt.MaLS=ls.MaLS AND tt.MaDMTT IS NOT NULL) THEN 1 ELSE 0 END AS CoMa
          FROM dbo.LICH_SU_KHAM_BENH ls
          JOIN dbo.PHIEU_KHAM_BENH pk ON pk.MaPhieu=ls.MaPhieu
          JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=pk.MaHS
          JOIN dbo.NHAN_VIEN bs ON bs.MaNV=ls.MaBS
          WHERE ls.TrangThai=N'Đã xác nhận' AND ls.ThoiGianKham>=@TuNgay AND ls.ThoiGianKham<@DenExclusive
            AND (@MaCK IS NULL OR bs.MaCK=@MaCK) AND (@MaDV IS NULL OR pk.MaDV=@MaDV)
        )
        """;

    private const string SqlChatLuong = LuotKham + """
            SELECT COUNT(*) AS TongLuot,ISNULL(SUM(CoMa),0) AS LuotCoMa,
              COUNT(*)-ISNULL(SUM(CoMa),0) AS LuotChuaPhanLoai,COUNT(DISTINCT MaBN) AS SoBenhNhan,
              (SELECT COUNT(*) FROM dbo.TINH_TRANG_BENH tt JOIN Luot l ON l.MaLS=tt.MaLS WHERE tt.MaDMTT IS NULL) AS DongChuaGanMa,
              (SELECT ISNULL(SUM(SoDong-1),0) FROM (SELECT COUNT(*) AS SoDong FROM dbo.TINH_TRANG_BENH tt
                JOIN Luot l ON l.MaLS=tt.MaLS WHERE tt.MaDMTT IS NOT NULL GROUP BY tt.MaLS,tt.MaDMTT HAVING COUNT(*)>1) d) AS DongTrungMa
            FROM Luot;
            """;
    private const string SqlBenh = LuotKham + """
            ,Benh AS (SELECT DISTINCT l.MaLS,l.MaBN,tt.MaDMTT FROM Luot l
              JOIN dbo.TINH_TRANG_BENH tt ON tt.MaLS=l.MaLS WHERE tt.MaDMTT IS NOT NULL)
            SELECT b.MaDMTT,dm.TenTinhTrang,
              CASE WHEN COUNT(DISTINCT b.MaBN)>=5 THEN COUNT(*) END AS SoLuotKham,
              CASE WHEN COUNT(DISTINCT b.MaBN)>=5 THEN COUNT(DISTINCT b.MaBN) END AS SoBenhNhan,
              CASE WHEN COUNT(DISTINCT b.MaBN)>=5 THEN 100.0*COUNT(*)/NULLIF((SELECT SUM(CoMa) FROM Luot),0) END AS TyTrong,
              CONVERT(bit,CASE WHEN COUNT(DISTINCT b.MaBN)<5 THEN 1 ELSE 0 END) AS DaLamMo
            FROM Benh b JOIN dbo.DM_TINH_TRANG_BENH dm ON dm.MaDMTT=b.MaDMTT
            GROUP BY b.MaDMTT,dm.TenTinhTrang ORDER BY COUNT(*) DESC,b.MaDMTT;
            """;

    /// <summary>
    /// Đọc chất lượng gắn mã và phân bố bệnh lý trong cùng giao dịch sau khi xác thực quyền báo cáo.
    /// </summary>
    /// <remarks>Khử trùng mã theo lượt trước khi đếm; nhóm dưới 5 bệnh nhân được làm mờ. Giữ mã danh mục ngừng áp dụng trong số liệu lịch sử.</remarks>
    /// <param name="ky">Kỳ gồm ngày bắt đầu và mốc cuối không bao gồm; hai mốc phải là ngày tròn.</param>
    /// <param name="maCK">Mã chuyên khoa của bác sĩ để lọc, hoặc null để lấy tất cả.</param>
    /// <param name="maDV">Mã dịch vụ trên phiếu khám để lọc, hoặc null để lấy tất cả.</param>
    /// <returns>Chất lượng dữ liệu và các nhóm bệnh có số đếm nullable khi bị làm mờ.</returns>
    public KetQuaBenhLy DocBenhLy(KySoSanh ky, string? maCK = null, string? maDV = null)
    {
        KiemTraDinhDang("20",ky.TuNgay,ky.DenExclusive.AddDays(-1),30);
        if (ky.TuNgay.TimeOfDay != TimeSpan.Zero || ky.DenExclusive.TimeOfDay != TimeSpan.Zero)
            throw new ArgumentException("Kỳ thống kê phải dùng ngày tròn.");
        if (maCK?.Length > 50 || maDV?.Length > 50) throw new ArgumentException("Mã bộ lọc quá dài.");
        KiemTraPrincipal();
        return ThucHien((db,tx) =>
        {
        XacThucQuyen(db,"20",tx);
        var args = new { ky.TuNgay, ky.DenExclusive, MaCK=string.IsNullOrWhiteSpace(maCK)?null:maCK, MaDV=string.IsNullOrWhiteSpace(maDV)?null:maDV };
        var quality = db.QuerySingle<ChatLuongMaBenh>(SqlChatLuong,args,tx);
        var conditions = db.Query<DongThongKeBenh>(SqlBenh,args,tx).ToArray();
        // Không lọc trạng thái danh mục: mã ngừng áp dụng vẫn thuộc lịch sử.

        return new KetQuaBenhLy(quality,conditions);
        });
    }

    /// <summary>
    /// Tổng hợp theo ngày các lượt xác nhận, số bệnh nhân và tình trạng gắn mã mà không nhân bản theo dịch vụ.
    /// </summary>
    public ET_DONGBAOCAO[] DocLuotBenh(KySoSanh ky)
    {
        KiemTraDinhDang("20",ky.TuNgay,ky.DenExclusive.AddDays(-1),30);
        KiemTraPrincipal();
        using var db=MoKetNoi();
        XacThucQuyen(db,"20");
        return db.Query<ET_DONGBAOCAO>(LuotKham + """
            SELECT CONVERT(date,ThoiGianKham) AS Ngay,COUNT(*) AS Mot,COUNT(DISTINCT MaBN) AS Hai,
              SUM(CoMa) AS Ba,COUNT(*)-SUM(CoMa) AS Bon,N'Lượt xác nhận' AS Nhom,COUNT(*) AS GiaTriCoCau
            FROM Luot GROUP BY CONVERT(date,ThoiGianKham)
            """,new {ky.TuNgay,ky.DenExclusive,MaCK=(string?)null,MaDV=(string?)null}).ToArray();
    }
}
