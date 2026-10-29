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
        SELECT c.MaCTHD,c.MaHD,c.MaDVThucHien,c.MaCTPX,COALESCE(c.MaCTDT,x.MaCTDT) MaCTDT,c.NoiDung,c.DonGia,c.ThanhTien,
            COALESCE(s.SoLuong,x.SoLuong,d.SoLuong) SoLuong,
            COALESCE(s.DonGia,c.DonGia) DonGiaChot,
            CASE WHEN c.MaDVThucHien IS NOT NULL THEN N'Dịch vụ' WHEN c.MaCTDT IS NOT NULL THEN N'Thuốc theo đơn' ELSE N'Thuốc đã cấp' END Loai,
            d.MaDT,l.SoLo,COALESCE(d.DonViTinhChot,hh.DonViTinh) DonViTinh
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
    /// SQL hợp nhất dịch vụ đã xác nhận và thuốc đã xuất của cùng lần khám; NOT EXISTS loại nguồn đã có chi tiết hóa đơn. HOLDLOCK giữ ổn định giá thuốc khi đọc; dòng thuốc tính thành tiền bằng lượng xuất nhân giá bán và làm tròn hai chữ số.
    /// </summary>
    internal const string CompletionChargesSql = """
        SELECT s.MaDVThucHien,CAST(NULL AS nvarchar(50)) MaCTPX,CAST(NULL AS nvarchar(50)) MaCTDT,
            d.TenDV NoiDung,CAST(NULL AS decimal(18,2)) DonGia,s.ThanhTien
        FROM dbo.DICH_VU_THUC_HIEN s WITH(HOLDLOCK) JOIN dbo.DICH_VU d ON d.MaDV=s.MaDV
        WHERE s.MaLS=@visitId AND s.TrangThai=N'Đã xác nhận'
          AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON c WITH(HOLDLOCK) WHERE c.MaDVThucHien=s.MaDVThucHien)
        UNION ALL
        SELECT NULL,NULL,c.MaCTDT,c.TenThuocChot+N' '+c.HamLuongChot,t.DonGiaBan,
            CAST(ROUND(c.SoLuong*t.DonGiaBan,2) AS decimal(18,2))
        FROM dbo.DON_THUOC r WITH(HOLDLOCK) JOIN dbo.CHI_TIET_DON_THUOC c WITH(HOLDLOCK) ON c.MaDT=r.MaDT
        JOIN dbo.THUOC t WITH(HOLDLOCK) ON t.MaThuoc=c.MaThuoc
        WHERE r.MaLS=@visitId AND r.TrangThai=N'Đã xác nhận'
          AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON h WITH(HOLDLOCK) WHERE h.MaCTDT=c.MaCTDT)
        """;

    internal const string EligibleChargesSql = """
        SELECT s.MaDVThucHien,CAST(NULL AS nvarchar(50)) MaCTPX,d.TenDV NoiDung,s.SoLuong,
            s.DonGia DonGiaChot,CAST(NULL AS decimal(18,2)) DonGia,s.ThanhTien,N'Dịch vụ' Loai
        FROM dbo.DICH_VU_THUC_HIEN s JOIN dbo.DICH_VU d ON d.MaDV=s.MaDV
        WHERE s.MaLS=@visitId AND s.TrangThai=N'Đã xác nhận'
            AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON c WHERE c.MaDVThucHien=s.MaDVThucHien)
        UNION ALL
        SELECT NULL,x.MaCTPX,COALESCE(NULLIF(ct.TenThuocChot,N''),hh.TenHH)+N' '+COALESCE(NULLIF(ct.HamLuongChot,N''),t.HamLuong),x.SoLuong,t.DonGiaBan,t.DonGiaBan,
            CAST(ROUND(x.SoLuong*t.DonGiaBan,2) AS decimal(18,2)),N'Thuốc đã cấp'
        FROM dbo.CHI_TIET_PHIEU_XUAT x JOIN dbo.PHIEU_XUAT p ON p.MaPX=x.MaPX
        JOIN dbo.CHI_TIET_DON_THUOC ct ON ct.MaCTDT=x.MaCTDT
        JOIN dbo.DON_THUOC dt ON dt.MaDT=ct.MaDT JOIN dbo.THUOC t WITH(HOLDLOCK) ON t.MaThuoc=ct.MaThuoc
        JOIN dbo.HANG_HOA hh ON hh.MaHH=x.MaHH
        WHERE p.MaLS=@visitId AND dt.MaLS=@visitId AND p.TrangThai=N'Đã ghi sổ'
            AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON c WHERE c.MaCTPX=x.MaCTPX)
        """;
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
        db.Execute("INSERT dbo.CHI_TIET_HOA_DON(MaCTHD,MaHD,MaDVThucHien,MaCTPX,MaCTDT,NoiDung,DonGia) VALUES(@detailId,@invoiceId,@MaDVThucHien,@MaCTPX,@MaCTDT,@NoiDung,@DonGia)", new { detailId, invoiceId, charge.MaDVThucHien, charge.MaCTPX, charge.MaCTDT, charge.NoiDung, charge.DonGia }, tx);
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
