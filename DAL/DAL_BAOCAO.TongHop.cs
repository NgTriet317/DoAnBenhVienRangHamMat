using System.Data;
using System.Security.Claims;
using Dapper;
using ET;

namespace DAL;

/// <summary>
/// Phần xác thực phiên, phân quyền chuyên đề, ghi truy cập và tổng hợp báo cáo từ nguồn SQL cố định.
/// </summary>
public sealed partial class DAL_BAOCAO
{
    /// <summary>
    /// Yêu cầu đúng một định danh nghiệp vụ đã xác thực và đủ claim tài khoản, nhân viên trên luồng hiện tại.
    /// </summary>
    internal static void KiemTraPrincipal()
    {
        var identity = (Thread.CurrentPrincipal as ClaimsPrincipal)?.Identities
            .SingleOrDefault(x => x.IsAuthenticated && x.AuthenticationType == DinhDanhNghiepVu.KieuXacThuc);
        if (identity?.FindFirst(ClaimTypes.NameIdentifier) is null || identity.FindFirst(DinhDanhNghiepVu.MaNhanVienClaim) is null)
            throw new UnauthorizedAccessException("Chưa đăng nhập hoặc phiên đã hết hạn.");
    }

    /// <summary>
    /// Xác thực lại người dùng và phiên SQL trước khi cấp quyền chuyên đề theo vai trò hiện tại.
    /// </summary>
    /// <returns>Các cờ quyền báo cáo của người dùng; từ chối nếu phiên hết hạn hoặc thiếu quyền chuyên đề.</returns>
    private static QuyenBaoCao XacThucQuyen(IDbConnection db, string ma, IDbTransaction? tx = null)
    {
        KiemTraPrincipal();
        var principal = (ClaimsPrincipal)Thread.CurrentPrincipal!;
        var actor = DAL_QUYENNGHIEPVU.XacThuc(db, tx, new NguCanhNghiepVu
        {
            MaNV = principal.FindFirst(DinhDanhNghiepVu.MaNhanVienClaim)!.Value,
            MaTK = principal.FindFirst(ClaimTypes.NameIdentifier)!.Value
        });
        if (!Guid.TryParse(principal.FindFirst("RHM.MaPhien")?.Value, out var session))
            throw new UnauthorizedAccessException("Phiên đã hết hạn; vui lòng đăng nhập lại.");
        DAL_NHATKY.KiemTraPhien(db, tx, actor.MaTK, session);
        var rights = ChinhSachBaoCao.TuVaiTro(actor);
        if (!ChinhSachBaoCao.ChoPhep(ma, rights)) throw new UnauthorizedAccessException("Không có quyền xem chuyên đề này.");
        return rights;
    }

    /// <summary>
    /// Đọc bộ quyền báo cáo hiện hành sau khi xác thực principal và phiên trong SQL.
    /// </summary>
    public QuyenBaoCao LayQuyen()
    {
        KiemTraPrincipal();
        using var db = MoKetNoi();
        return XacThucQuyen(db, "00");
    }
    /// <summary>
    /// Đọc extended property RHM.Demo.P5 của database để nhận diện dữ liệu trình diễn có quyền truy cập.
    /// </summary>
    public bool LaDuLieuTrinhDien()
    {
        KiemTraPrincipal();using var db=MoKetNoi();XacThucQuyen(db,"00");
        return db.ExecuteScalar<int>("SELECT COALESCE(MAX(TRY_CONVERT(int,value)),0) FROM sys.extended_properties WHERE class=0 AND name=N'RHM.Demo.P5'")==1;
    }

    /// <summary>
    /// Lấy chuyên khoa và dịch vụ cho bộ lọc sau khi xác thực quyền chuyên đề bệnh lý.
    /// </summary>
    public (LuaChonThongKe[] ChuyenKhoa,LuaChonThongKe[] DichVu) LayDanhMucLoc()
    {
        KiemTraPrincipal();using var db=MoKetNoi();XacThucQuyen(db,"20");
        return (db.Query<LuaChonThongKe>("SELECT MaCK AS Ma,TenCK AS Ten FROM dbo.CHUYEN_KHOA ORDER BY TenCK").ToArray(),
            db.Query<LuaChonThongKe>("SELECT MaDV AS Ma,TenDV AS Ten FROM dbo.DICH_VU ORDER BY TenDV").ToArray());
    }

    /// <summary>
    /// Chỉ ghi sự kiện VIEW_REPORT hoặc EXPORT_REPORT với khoảng ngày và phiên đã xác thực.
    /// </summary>
    /// <remarks>Xuất báo cáo phải có thêm cờ quyền xuất; sự kiện được ghi trong giao dịch của ThucHien.</remarks>
    public void GhiTruyCap(string ma,string hanhDong,DateTime tu,DateTime den)
    {
        if(hanhDong is not ("VIEW_REPORT" or "EXPORT_REPORT"))throw new ArgumentException("Sự kiện báo cáo không hợp lệ.");
        KiemTraDinhDang(ma,tu,den,30);KiemTraPrincipal();
        ThucHien((db,tx)=>
        {
            var rights=XacThucQuyen(db,ma,tx);
            if(hanhDong=="EXPORT_REPORT"&&!rights.HasFlag(QuyenBaoCao.Xuat))throw new UnauthorizedAccessException();
            var principal=(ClaimsPrincipal)Thread.CurrentPrincipal!;
            DAL_NHATKY.Ghi(db,tx,principal.FindFirst(ClaimTypes.NameIdentifier)!.Value,
                Guid.Parse(principal.FindFirst("RHM.MaPhien")!.Value),hanhDong,"Báo cáo","BAO_CAO",ma,
                after:System.Text.Json.JsonSerializer.Serialize(new{TuNgay=tu.Date,DenNgay=den.Date}));
            return true;
        });
    }

    /// <summary>
    /// Xác thực lại phiên, quyền xuất và quyền chi tiết chuyên đề trước khi xuất dữ liệu đã xem.
    /// </summary>
    /// <param name="ma">Mã chuyên đề của dữ liệu cần xuất.</param>
    /// <param name="gomChiTiet">true để yêu cầu thêm quyền xem chi tiết tài chính, lương hoặc kho tương ứng.</param>
    public void KiemTraXuat(string ma,bool gomChiTiet=false)
    {
        KiemTraPrincipal();
        using var db = MoKetNoi();
        var rights=XacThucQuyen(db,ma);
        if (!rights.HasFlag(QuyenBaoCao.Xuat) || gomChiTiet&&!ChinhSachBaoCao.ChoPhepChiTiet(ma,rights))
            throw new UnauthorizedAccessException("Không còn quyền xuất phạm vi dữ liệu đã tải. Tải lại báo cáo để cập nhật quyền.");
    }

    /// <summary>
    /// Giới hạn mã chuyên đề, khoảng ngày tối đa 10 năm và ngưỡng cảnh báo 30/60/90 ngày.
    /// </summary>
    /// <remarks>Báo cáo hạn dùng chuyên đề 14 chỉ nhận một ngày chốt.</remarks>
    internal static void KiemTraDinhDang(string ma, DateTime tu, DateTime den, int canhBao)
    {
        if (ma != "00" && (!int.TryParse(ma, out var code) || code is < 2 or > 20 || ma != code.ToString("00")))
            throw new ArgumentException("Chuyên đề không tồn tại.");
        if (tu.Date > den.Date || (den.Date-tu.Date).TotalDays > 3660 || den.Year > 9998 || tu.Year < 1900)
            throw new ArgumentException("Khoảng ngày báo cáo không hợp lệ hoặc vượt 10 năm.");
        if (canhBao is not (30 or 60 or 90)) throw new ArgumentException("Ngưỡng cảnh báo phải là 30/60/90 ngày.");
        if (ma == "14" && tu.Date != den.Date) throw new ArgumentException("Hạn dùng phải dùng một ngày chốt.");
    }

    /// <summary>
    /// Xác thực quyền rồi gom các chỉ số theo ngày và nhóm từ nguồn truy vấn cố định của chuyên đề.
    /// </summary>
    private ET_DONGBAOCAO[] DocTongHop(string ma, DateTime tu, DateTime den)
    {
        using var db = MoKetNoi();
        XacThucQuyen(db, ma);
        // Chỉ tên bảng/biểu thức cố định trong mã; mọi giá trị lọc đều qua tham số.
        var source = NguonTongHop(ma);
        return db.Query<ET_DONGBAOCAO>($"""
            SELECT CONVERT(date,Ngay) AS Ngay,SUM(Mot) AS Mot,SUM(Hai) AS Hai,SUM(Ba) AS Ba,SUM(Bon) AS Bon,
                Nhom,SUM(GiaTriCoCau) AS GiaTriCoCau
            FROM ({source}) s
            GROUP BY CONVERT(date,Ngay),Nhom
            ORDER BY Ngay,Nhom
            """, new { TuNgay=tu.Date, DenExclusive=den.Date.AddDays(1) }).ToArray();
    }

    /// <summary>
    /// Ánh xạ mã chuyên đề sang bảng và biểu thức SQL đã định nghĩa trong mã.
    /// </summary>
    /// <remarks>Chỉ dùng tên bảng, cột và biểu thức nội bộ; giá trị lọc thời gian được truyền qua tham số SQL.</remarks>
    private static string NguonTongHop(string ma)
    {
        static string Co(string condition) => $"CASE WHEN {condition} THEN 1 ELSE 0 END";
        static string Table(string table, string date, string mot="1", string hai="0", string ba="0", string bon="0", string nhom="TrangThai", string extra="") =>
            $"SELECT {date} AS Ngay,{mot} AS Mot,{hai} AS Hai,{ba} AS Ba,{bon} AS Bon,ISNULL({nhom},N'Chưa ghi nhận') AS Nhom,CAST(1 AS decimal(18,2)) AS GiaTriCoCau FROM dbo.{table} WHERE {date}>=@TuNgay AND {date}<@DenExclusive {extra}";
        return ma switch
        {
            "00" => Table("LICH_SU_KHAM_BENH","ThoiGianKham",extra:"AND TrangThai=N'Đã xác nhận'")
                + " UNION ALL " + Table("THANH_TOAN","NgayThanhToan","0", "CASE WHEN LoaiGiaoDich=N'Thu' THEN SoTien ELSE -SoTien END",nhom:"N'Thu ròng'",extra:"AND TrangThai=N'Đã xác nhận'"),
            "02" => Table("HO_SO_BENH_AN","NgayLap","1","0","0",Co("NULLIF(LTRIM(RTRIM(GhiChu)),N'') IS NOT NULL"))
                + " UNION ALL " + Table("PHIEU_KHAM_BENH","ThoiGianTiepNhan","0","1")
                + " UNION ALL " + Table("LICH_SU_KHAM_BENH","ThoiGianKham","0","0","1",extra:"AND TrangThai=N'Đã xác nhận'"),
            "03" => Table("PHIEU_KHAM_BENH","ThoiGianTiepNhan","1",Co("NULLIF(MaLich,N'') IS NOT NULL"),Co("NULLIF(MaLich,N'') IS NULL"),Co("TrangThai=N'Hủy'")),
            "04" => Table("LICH_KHAM","NgayGioKham","1",Co("TrangThai=N'Đã khám'"),Co("TrangThai=N'Hủy'"),Co("NULLIF(MaDV,N'') IS NOT NULL")),
            "05" => Table("LICH_SU_KHAM_BENH","ThoiGianKham","1",Co("TrangThai=N'Đã xác nhận'"),Co("TrangThai=N'Nháp'"),Co("NULLIF(LTRIM(RTRIM(ChanDoan)),N'') IS NOT NULL")),
            "06" => Table("KE_HOACH_DIEU_TRI","NgayLap","1",Co("NgayBatDau IS NOT NULL"),"TongTienDuKien",Co("NgayDuKienKetThuc IS NOT NULL")),
            "07" => Table("DICH_VU_THUC_HIEN","ThoiGianThucHien","1",Co("TrangThai=N'Đã xác nhận'"),Co("TrangThai=N'Hủy'"),"CASE WHEN TrangThai=N'Đã xác nhận' THEN ThanhTien ELSE 0 END"),
            "08" => Table("DON_THUOC","NgayKe","1",Co("TrangThai=N'Đã xác nhận'"),Co("TrangThai=N'Nháp'"),Co("TrangThai=N'Hủy'")),
            "09" => """
                SELECT NgayThanhToan AS Ngay,CASE WHEN LoaiGiaoDich=N'Thu' THEN SoTien ELSE 0 END AS Mot,
                 CASE WHEN LoaiGiaoDich=N'Hoàn' THEN SoTien ELSE 0 END AS Hai,
                 CASE WHEN LoaiGiaoDich=N'Thu' THEN SoTien ELSE -SoTien END AS Ba,1 AS Bon,
                 LoaiGiaoDich+N' · '+PhuongThuc AS Nhom,SoTien AS GiaTriCoCau FROM dbo.THANH_TOAN
                 WHERE TrangThai=N'Đã xác nhận' AND NgayThanhToan>=@TuNgay AND NgayThanhToan<@DenExclusive
                """,
            "10" => Table("HOA_DON","NgayLap","1",Co("TrangThai=N'Đã xác nhận'"),Co("TrangThai=N'Hủy'"),"CASE WHEN TrangThai=N'Đã xác nhận' THEN SoTienPhaiThu ELSE 0 END"),
            "12" => Table("PHIEU_NHAP","NgayNhap",Co("TrangThai=N'Đã ghi sổ'"),"0",Co("TrangThai=N'Hủy'"),nhom:"N'Nhập · '+TrangThai")
                + " UNION ALL " + Table("PHIEU_XUAT","NgayXuat","0",Co("TrangThai=N'Đã ghi sổ'"),"0",Co("TrangThai=N'Hủy'"),"N'Xuất · '+TrangThai"),
            "15" => Table("HOA_DON_NHAP","NgayHoaDon","1",Co("TrangThai=N'Nháp'"),Co("TrangThai=N'Hủy'"),"CASE WHEN TrangThai=N'Đã xác nhận' THEN TongTien ELSE 0 END"),
            "16" => Table("LICH_LAM_VIEC","NgayLam","1",Co("TrangThaiHienDien=N'Có mặt'"),Co("TrangThaiHienDien=N'Vắng mặt'"),Co("ISNULL(TrangThaiHienDien,N'') NOT IN(N'Có mặt',N'Vắng mặt')"),"TrangThaiHienDien","AND TrangThai=N'Đã xác nhận'"),
            "17" => Table("LICH_LAM_VIEC","NgayLam","1",Co("TrangThai=N'Đã xác nhận'"),Co("TrangThai=N'Chờ xác nhận'"),Co("TrangThai=N'Hủy'")),
            "19" => Table("BAO_TRI_THIET_BI","NgayBatDau","1",Co("TrangThai=N'Đang thực hiện'"),Co("TrangThai=N'Đã hoàn thành'"),"CASE WHEN TrangThai IN(N'Đang thực hiện',N'Đã hoàn thành') THEN ChiPhi ELSE 0 END"),
            _ => throw new ArgumentException("Không có nguồn tổng hợp cho chuyên đề.")
        };
    }
}
