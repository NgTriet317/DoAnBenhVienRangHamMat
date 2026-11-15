// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL;
/// <summary>
/// Tầng truy cập SQL cho chi tiết hóa đơn trong bảng CHI_TIET_HOA_DON. Đọc các khoản thu dịch vụ và thuốc đã xuất; tìm nguồn chưa tính tiền và ghi chi tiết trong giao dịch lập hóa đơn. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
/// </summary>
public sealed class DAL_CHITIETHOADON : dbconnect
{
    /// <summary>
    /// Tạo DAL quản lý chi tiết hóa đơn, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
    /// </summary>
    /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
    public DAL_CHITIETHOADON(IDbTransaction? transaction = null) : base(transaction)
    {
    }

    /// <summary>
    /// Trả danh sách chi tiết hóa đơn dưới dạng mảng bằng luồng layToanBo, phù hợp nơi gọi cần truy cập và đếm kết quả ngay.
    /// </summary>
    /// <returns>Mảng ET; không có bản ghi thì mảng rỗng.</returns>
    public ET_CHITIETHOADON[] LayDanhSach()
    {
        return layToanBo().ToArray();
    }

    /// <summary>
    /// Đọc toàn bộ chi tiết hóa đơn từ dbo.CHI_TIET_HOA_DON, Dapper ánh xạ từng dòng sang ET; SQL không quy định thứ tự; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
    /// </summary>
    /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
    public IEnumerable<ET_CHITIETHOADON> layToanBo()
    {
        return ThucHien((db, tx) =>
        {
            return db.Query<ET_CHITIETHOADON>("SELECT * FROM dbo.CHI_TIET_HOA_DON", transaction: tx).ToArray();
        });
    }

    /// <summary>
    /// Tìm chi tiết hóa đơn theo khóa trong dbo.CHI_TIET_HOA_DON; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
    /// </summary>
    /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
    /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
    public ET_CHITIETHOADON? layTheoMa(string ma)
    {
        return ThucHien((db, tx) =>
        {
            return db.QuerySingleOrDefault<ET_CHITIETHOADON>("SELECT * FROM dbo.CHI_TIET_HOA_DON WHERE MaCTHD=@ma", new { ma }, tx);
        });
    }

    /// <summary>
    /// Chạy SQL có tham số bằng Dapper, nạp toàn bộ IDataReader vào DataTable rồi giải phóng reader; bảng trả về dùng cho hiển thị hoặc truy vấn tổng hợp.
    /// </summary>
    /// <param name = "sql">Câu lệnh SQL cần thực thi bằng reader.</param>
    /// <param name = "parameters">Đối tượng ánh xạ tên tham số SQL sang giá trị; null nếu câu lệnh không có tham số.</param>
    /// <returns>DataTable đã nạp đủ các dòng kết quả; có thể rỗng nếu chưa có dữ liệu phù hợp.</returns>
    private DataTable Query(string sql, object? parameters = null)
    {
        return ThucHien((connection, tx) =>
        {
            using var reader = connection.ExecuteReader(sql, parameters, tx);
            var table = new DataTable();
            table.Load(reader);
            return table;
        });
    }

    /// <summary>
    /// Đọc chi tiết của một hóa đơn, ghép dịch vụ hoặc dòng xuất thuốc để lấy lượng, giá chốt, loại khoản thu, đơn thuốc, số lô và đơn vị tính; sắp theo mã chi tiết.
    /// </summary>
    /// <param name = "invoiceId">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    /// <returns>DataTable đã nạp đủ các dòng kết quả; có thể rỗng nếu chưa có dữ liệu phù hợp.</returns>
    public DataTable GetInvoiceDetails(string invoiceId)
    {
        return Query("""
        SELECT c.MaCTHD,c.MaHD,c.MaDVThucHien,c.MaCTPX,COALESCE(c.MaCTDT,x.MaCTDT) MaCTDT,c.MaPhieuPhuThu,c.MaPhienAI,c.NoiDung,c.DonGia,c.ThanhTien,
            CASE WHEN c.MaPhieuPhuThu IS NOT NULL OR c.MaPhienAI IS NOT NULL THEN CAST(1 AS decimal(18,3)) ELSE COALESCE(s.SoLuong,x.SoLuong,d.SoLuong) END SoLuong,
            COALESCE(s.DonGia,c.DonGia) DonGiaChot,
            CASE WHEN c.MaPhieuPhuThu IS NOT NULL OR c.MaPhienAI IS NOT NULL THEN N'Phụ thu khám dịch vụ' WHEN c.MaDVThucHien IS NOT NULL THEN N'Dịch vụ' WHEN c.MaCTDT IS NOT NULL THEN N'Thuốc theo đơn' ELSE N'Thuốc đã cấp' END Loai,
            d.MaDT,l.SoLo,CASE WHEN c.MaPhieuPhuThu IS NOT NULL OR c.MaPhienAI IS NOT NULL THEN N'Lượt' ELSE COALESCE(d.DonViTinhChot,hh.DonViTinh) END DonViTinh
        FROM dbo.CHI_TIET_HOA_DON c
        LEFT JOIN dbo.DICH_VU_THUC_HIEN s ON s.MaDVThucHien=c.MaDVThucHien
        LEFT JOIN dbo.CHI_TIET_PHIEU_XUAT x ON x.MaCTPX=c.MaCTPX
        LEFT JOIN dbo.CHI_TIET_DON_THUOC d ON d.MaCTDT=COALESCE(c.MaCTDT,x.MaCTDT)
        LEFT JOIN dbo.HANG_HOA hh ON hh.MaHH=x.MaHH
        LEFT JOIN dbo.LO_HANG_HOA l ON l.MaLo=x.MaLo
        WHERE c.MaHD=@invoiceId ORDER BY c.MaCTHD
        """, new { invoiceId });
    }

    /// <summary>
    /// Chọn duy nhất một phụ thu cho phiếu: ưu tiên phí đã chốt tại tiếp nhận; phiên AI chỉ bổ sung cho phiếu cũ chưa lưu phí.
    /// Dùng cả trước khi ký khám để tổng kết và dấu vân tay khớp nguồn hóa đơn sau khi ký.
    /// </summary>
    internal const string SurchargeSourcesSql = """
        SELECT l.MaLS,p.MaPhieu,
            CASE WHEN p.PhiPhuThuDichVu>0 THEN p.MaPhieu END MaPhieuPhuThu,
            CASE WHEN p.PhiPhuThuDichVu=0 THEN ai.MaPhien END MaPhienAI,
            CASE WHEN p.PhiPhuThuDichVu>0 THEN p.PhiPhuThuDichVu ELSE CAST(30000 AS decimal(18,2)) END DonGia,
            CASE WHEN p.PhiPhuThuDichVu>0 THEN N'Phụ thu khám dịch vụ (chọn bác sĩ)' ELSE N'Phụ thu tiếp nhận khám dịch vụ' END NoiDung
        FROM dbo.LICH_SU_KHAM_BENH l WITH(HOLDLOCK)
        JOIN dbo.PHIEU_KHAM_BENH p WITH(HOLDLOCK) ON p.MaPhieu=l.MaPhieu
        OUTER APPLY(
            SELECT TOP(1) source.MaPhien FROM(
                SELECT k.MaPhien FROM dbo.AI_KET_QUA_TAO_PHIEU k WITH(HOLDLOCK)
                WHERE k.MaPhieu=p.MaPhieu AND k.LoaiKham=N'DichVu' AND k.PhuThuDuKien=30000
                UNION ALL
                SELECT r.MaPhien FROM dbo.AI_YEU_CAU_TAO_PHIEU r WITH(HOLDLOCK)
                WHERE r.MaPhieu=p.MaPhieu AND r.LoaiKham=N'service'
                    AND NOT EXISTS(SELECT 1 FROM dbo.AI_KET_QUA_TAO_PHIEU k WITH(HOLDLOCK) WHERE k.MaPhien=r.MaPhien)
            ) source ORDER BY source.MaPhien
        ) ai
        WHERE l.MaLS=@visitId AND ((p.LoaiKham=N'Dịch vụ' AND p.PhiPhuThuDichVu>0)
            OR (p.PhiPhuThuDichVu=0 AND ai.MaPhien IS NOT NULL))
        """;

    /// <summary>Loại cả hai kiểu nguồn phụ thu đã thu của cùng lượt; không cấp lại phụ thu bằng kiểu nguồn còn lại.</summary>
    private const string UnbilledSurchargesSql = "SELECT f.* FROM (" + SurchargeSourcesSql + """
        ) f WHERE NOT EXISTS(
            SELECT 1 FROM dbo.CHI_TIET_HOA_DON c WITH(HOLDLOCK)
            JOIN dbo.HOA_DON h WITH(HOLDLOCK) ON h.MaHD=c.MaHD
            WHERE h.MaLS=f.MaLS AND (c.MaPhieuPhuThu IS NOT NULL OR c.MaPhienAI IS NOT NULL))
        """;

    /// <summary>
    /// SQL hợp nhất dịch vụ, thuốc theo đơn đã xác nhận và một phụ thu chưa tính tiền của cùng lần khám.
    /// </summary>
    internal const string CompletionChargesSql = """
        SELECT s.MaDVThucHien,CAST(NULL AS nvarchar(50)) MaCTPX,CAST(NULL AS nvarchar(50)) MaCTDT,
            d.TenDV NoiDung,CAST(NULL AS decimal(18,2)) DonGia,s.ThanhTien,
            CAST(NULL AS nvarchar(50)) MaPhieuPhuThu,CAST(NULL AS uniqueidentifier) MaPhienAI
        FROM dbo.DICH_VU_THUC_HIEN s WITH(HOLDLOCK) JOIN dbo.DICH_VU d ON d.MaDV=s.MaDV
        WHERE s.MaLS=@visitId AND s.TrangThai=N'Đã xác nhận'
          AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON c WITH(HOLDLOCK) WHERE c.MaDVThucHien=s.MaDVThucHien)
        UNION ALL
        SELECT NULL,NULL,c.MaCTDT,c.TenThuocChot+N' '+c.HamLuongChot,t.DonGiaBan,
            CAST(ROUND(c.SoLuong*t.DonGiaBan,2) AS decimal(18,2)),NULL,CAST(NULL AS uniqueidentifier)
        FROM dbo.DON_THUOC r WITH(HOLDLOCK) JOIN dbo.CHI_TIET_DON_THUOC c WITH(HOLDLOCK) ON c.MaDT=r.MaDT
        JOIN dbo.THUOC t WITH(HOLDLOCK) ON t.MaThuoc=c.MaThuoc
        WHERE r.MaLS=@visitId AND r.TrangThai=N'Đã xác nhận'
          AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON h WITH(HOLDLOCK) WHERE h.MaCTDT=c.MaCTDT)
        UNION ALL
        SELECT NULL,NULL,NULL,f.NoiDung,f.DonGia,f.DonGia,f.MaPhieuPhuThu,f.MaPhienAI
        FROM (
        """ + UnbilledSurchargesSql + ") f";

    /// <summary>
    /// Câu lệnh SQL đủ điều kiện các khoản phí SQL dùng trong các thao tác của lớp; giá trị đầu vào được truyền theo cách nơi gọi quy định.
    /// </summary>
    internal const string EligibleChargesSql = """
        SELECT s.MaDVThucHien,CAST(NULL AS nvarchar(50)) MaCTPX,d.TenDV NoiDung,s.SoLuong,
            s.DonGia DonGiaChot,CAST(NULL AS decimal(18,2)) DonGia,s.ThanhTien,N'Dịch vụ' Loai,
            CAST(NULL AS nvarchar(50)) MaPhieuPhuThu,CAST(NULL AS uniqueidentifier) MaPhienAI
        FROM dbo.DICH_VU_THUC_HIEN s JOIN dbo.DICH_VU d ON d.MaDV=s.MaDV
        WHERE s.MaLS=@visitId AND s.TrangThai=N'Đã xác nhận'
            AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON c WHERE c.MaDVThucHien=s.MaDVThucHien)
        UNION ALL
        SELECT NULL,x.MaCTPX,COALESCE(NULLIF(ct.TenThuocChot,N''),hh.TenHH)+N' '+COALESCE(NULLIF(ct.HamLuongChot,N''),t.HamLuong),x.SoLuong,t.DonGiaBan,t.DonGiaBan,
            CAST(ROUND(x.SoLuong*t.DonGiaBan,2) AS decimal(18,2)),N'Thuốc đã cấp',NULL,CAST(NULL AS uniqueidentifier)
        FROM dbo.CHI_TIET_PHIEU_XUAT x JOIN dbo.PHIEU_XUAT p ON p.MaPX=x.MaPX
        JOIN dbo.CHI_TIET_DON_THUOC ct ON ct.MaCTDT=x.MaCTDT
        JOIN dbo.DON_THUOC dt ON dt.MaDT=ct.MaDT JOIN dbo.THUOC t WITH(HOLDLOCK) ON t.MaThuoc=ct.MaThuoc
        JOIN dbo.HANG_HOA hh ON hh.MaHH=x.MaHH
        WHERE p.MaLS=@visitId AND dt.MaLS=@visitId AND p.TrangThai=N'Đã ghi sổ'
            AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON c WHERE c.MaCTPX=x.MaCTPX)
        UNION ALL
        SELECT NULL,NULL,f.NoiDung,CAST(1 AS decimal(18,3)),f.DonGia,f.DonGia,f.DonGia,
            N'Phụ thu khám dịch vụ',f.MaPhieuPhuThu,f.MaPhienAI
        FROM (
        """ + UnbilledSurchargesSql + ") f";
    /// <summary>
    /// Trả bảng khoản thu chưa nằm trên hóa đơn nào của lần khám: dịch vụ Đã xác nhận và thuốc từ phiếu xuất Đã ghi sổ; giá thuốc lấy từ danh mục hiện hành.
    /// </summary>
    /// <param name = "visitId">Mã lần khám liên kết; dùng để xác định bệnh nhân hoặc nguồn thu hợp lệ.</param>
    /// <returns>DataTable đã nạp đủ các dòng kết quả; có thể rỗng nếu chưa có dữ liệu phù hợp.</returns>
    public DataTable GetEligibleCharges(string visitId)
    {
        return Query(EligibleChargesSql, new { visitId });
    }

    /// <summary>
    /// Cấp mã chi tiết trong giao dịch lập hóa đơn, chèn liên kết dịch vụ hoặc dòng xuất cùng nội dung và giá thuốc đã chọn; số lượng/thành tiền không được INSERT trực tiếp ở hàm này.
    /// </summary>
    /// <param name = "db">Kết nối SQL đang mở của giao dịch do nơi gọi quản lý.</param>
    /// <param name = "tx">Giao dịch đang dùng để đọc/ghi và giữ khóa đồng bộ; được truyền tiếp cho các DAL liên quan nếu có.</param>
    /// <param name = "invoiceId">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    /// <param name = "charge">Khoản thu đã chọn gồm nguồn dịch vụ/dòng xuất, nội dung và đơn giá; điều kiện đủ để tính tiền do truy vấn nguồn và nơi gọi kiểm tra.</param>
    internal static void ThemChiTiet(IDbConnection db, IDbTransaction? tx, string invoiceId, ET_CHITIETHOADON charge)
    {
        var detailId = TaoMa(db, tx, "CHI_TIET_HOA_DON", "MaCTHD");
        db.Execute("INSERT dbo.CHI_TIET_HOA_DON(MaCTHD,MaHD,MaDVThucHien,MaCTPX,MaCTDT,MaPhieuPhuThu,MaPhienAI,NoiDung,DonGia) VALUES(@detailId,@invoiceId,@MaDVThucHien,@MaCTPX,@MaCTDT,@MaPhieuPhuThu,@MaPhienAI,@NoiDung,@DonGia)", new { detailId, invoiceId, charge.MaDVThucHien, charge.MaCTPX, charge.MaCTDT, charge.MaPhieuPhuThu, charge.MaPhienAI, charge.NoiDung, charge.DonGia }, tx);
    }

    /// <summary>
    /// Xóa mọi dòng chi tiết của hóa đơn trong giao dịch được truyền vào để giải phóng các nguồn thu khi hủy; kiểm tra hóa đơn còn Nháp do nơi gọi thực hiện.
    /// </summary>
    /// <param name = "db">Kết nối SQL đang mở của giao dịch do nơi gọi quản lý.</param>
    /// <param name = "tx">Giao dịch đang dùng để đọc/ghi và giữ khóa đồng bộ; được truyền tiếp cho các DAL liên quan nếu có.</param>
    /// <param name = "invoiceId">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    internal static void GoChiTietNhap(IDbConnection db, IDbTransaction? tx, string invoiceId)
    {
        db.Execute("DELETE dbo.CHI_TIET_HOA_DON WHERE MaHD=@invoiceId", new { invoiceId }, tx);
    }
}
