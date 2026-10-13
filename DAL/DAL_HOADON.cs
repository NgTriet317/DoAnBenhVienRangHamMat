// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL;

/// <summary>
/// Tầng truy cập SQL cho hóa đơn trong bảng HOA_DON. Tập hợp khoản thu mới của lần khám đã xác nhận, lập hóa đơn nháp có giảm giá rồi xác nhận hoặc hủy nháp trong giao dịch. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
/// </summary>
public sealed class DAL_HOADON : dbconnect
{
    /// <summary>
    /// Tên gọi tiếng Việt của CreateInvoice: lập hóa đơn Nháp cho khoản thu mới của lần khám đã xác nhận và trả mã hóa đơn.
    /// </summary>
    /// <param name="maLS">Mã lần khám liên kết; dùng để xác định bệnh nhân hoặc nguồn thu hợp lệ.</param>
    /// <param name="maNV">Mã nhân viên thực hiện; phải là nhân viên Đang làm việc.</param>
    /// <param name="giamGia">Số tiền giảm giá, từ 0 đến tổng các khoản thu được đưa vào hóa đơn.</param>
    /// <returns>Mã hóa đơn Nháp vừa lập; hóa đơn được xác nhận bằng thao tác riêng.</returns>
    public string LapHoaDon(string maLS, string maNV, decimal giamGia = 0) => CreateInvoice(maLS, maNV, giamGia);
    /// <summary>
    /// Chuyển yêu cầu xác nhận hóa đơn sang ConfirmInvoice; chỉ hóa đơn Nháp được xác nhận và ghi ngày xác nhận.
    /// </summary>
    /// <param name="maHD">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    public void XacNhanHoaDon(string maHD) => ConfirmInvoice(maHD);
    /// <summary>
    /// Chuyển yêu cầu hủy hóa đơn ở trạng thái Nháp sang CancelInvoice; các dòng chi tiết được gỡ để có thể lập hóa đơn khác từ nguồn thu đó.
    /// </summary>
    /// <param name="maHD">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    public void HuyHoaDonNhap(string maHD) => CancelInvoice(maHD);
    /// <summary>
    /// Tạo DAL quản lý hóa đơn, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
    /// </summary>
    /// <param name="transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
    public DAL_HOADON(IDbTransaction? transaction = null) : base(transaction) { }
    /// <summary>
    /// Trả danh sách hóa đơn dưới dạng mảng bằng luồng layToanBo, phù hợp nơi gọi cần truy cập và đếm kết quả ngay.
    /// </summary>
    /// <returns>Mảng ET; không có bản ghi thì mảng rỗng.</returns>
    public ET_HOADON[] LayDanhSach() => layToanBo().ToArray();
    /// <summary>
    /// Đọc toàn bộ hóa đơn từ dbo.HOA_DON, Dapper ánh xạ từng dòng sang ET; SQL không quy định thứ tự; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
    /// </summary>
    /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
    public IEnumerable<ET_HOADON> layToanBo() => Doc((db, tx) =>
        db.Query<ET_HOADON>("SELECT * FROM dbo.HOA_DON", transaction: tx).ToArray());
    /// <summary>
    /// Tìm hóa đơn theo khóa trong dbo.HOA_DON; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
    /// </summary>
    /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
    /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
    public ET_HOADON? layTheoMa(string ma) => Doc((db, tx) =>
        db.QuerySingleOrDefault<ET_HOADON>("SELECT * FROM dbo.HOA_DON WHERE MaHD=@ma", new { ma }, tx));

    /// <summary>
    /// Chuyển hàm đọc cho ThucHien để dùng kết nối và giao dịch chung; khi không có giao dịch ngoài, hạ tầng tự mở giao dịch Serializable.
    /// </summary>
    /// <param name="operation">Hàm nhận kết nối và giao dịch đang dùng, chạy truy vấn/thao tác và trả kết quả cho hạ tầng.</param>
    /// <returns>Kết quả do hàm operation tạo ra; ngoại lệ được chuyển về nơi gọi.</returns>
    private T Doc<T>(Func<IDbConnection, IDbTransaction?, T> operation) => ThucHien(operation);

    /// <summary>
    /// Chuyển toàn bộ thao tác cho ThucHien: dùng giao dịch ngoài nếu có, còn giao dịch cục bộ được commit khi thành công và rollback khi lỗi.
    /// </summary>
    /// <param name="operation">Hàm nhận kết nối và giao dịch đang dùng, chạy truy vấn/thao tác và trả kết quả cho hạ tầng.</param>
    /// <returns>Kết quả do hàm operation tạo ra; ngoại lệ được chuyển về nơi gọi.</returns>
    private T Transaction<T>(Func<IDbConnection, IDbTransaction?, T> operation) => ThucHien(operation);

    /// <summary>
    /// Chạy SQL có tham số bằng Dapper, nạp toàn bộ IDataReader vào DataTable rồi giải phóng reader; bảng trả về dùng cho hiển thị hoặc truy vấn tổng hợp.
    /// </summary>
    /// <param name="sql">Câu lệnh SQL cần thực thi bằng reader.</param>
    /// <param name="parameters">Đối tượng ánh xạ tên tham số SQL sang giá trị; null nếu câu lệnh không có tham số.</param>
    /// <returns>DataTable đã nạp đủ các dòng kết quả; có thể rỗng nếu chưa có dữ liệu phù hợp.</returns>
    private DataTable Query(string sql, object? parameters = null) => Doc((connection, tx) =>
    {
        using var reader = connection.ExecuteReader(sql, parameters, tx);
        var table = new DataTable();
        table.Load(reader);
        return table;
    });

    /// <summary>
    /// Kiểm tra tồn tại nhân viên có trạng thái Đang làm việc; ném lỗi nếu người thực hiện giao dịch không hợp lệ.
    /// </summary>
    /// <param name="db">Kết nối SQL đang mở của giao dịch do nơi gọi quản lý.</param>
    /// <param name="tx">Giao dịch đang dùng để đọc/ghi và giữ khóa đồng bộ; được truyền tiếp cho các DAL liên quan nếu có.</param>
    /// <param name="id">Mã nhân viên cần kiểm tra đang làm việc.</param>
    private static void Employee(IDbConnection db, IDbTransaction? tx, string id)
    {
        if (!db.ExecuteScalar<bool>("SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.NHAN_VIEN WHERE MaNV=@id AND TrangThai=N'Đang làm việc') THEN 1 ELSE 0 END AS bit)", new { id }, tx))
            throw new InvalidOperationException("Chọn nhân viên đang làm việc để thực hiện giao dịch.");
    }

    /// <summary>
    /// Đọc hóa đơn cùng bệnh nhân và người lập; cộng tiền Thu, trừ tiền Hoàn đã xác nhận để tính DaThu và ConPhaiThu. Có thể lọc một lần khám; sắp ngày và mã mới nhất trước.
    /// </summary>
    /// <param name="visitId">Mã lần khám để lọc hóa đơn; null để lấy tất cả.</param>
    /// <returns>DataTable đã nạp đủ các dòng kết quả; có thể rỗng nếu chưa có dữ liệu phù hợp.</returns>
    public DataTable GetInvoices(string? visitId = null) => Query("""
        SELECT h.*, bn.MaBN,bn.HoTenBN,nv.HoTen NguoiLap,
            COALESCE(t.ThuRong,0) DaThu,h.SoTienPhaiThu-COALESCE(t.ThuRong,0) ConPhaiThu
        FROM dbo.HOA_DON h
        JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=h.MaLS
        JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu
        JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS
        JOIN dbo.BENH_NHAN bn ON bn.MaBN=hs.MaBN
        JOIN dbo.NHAN_VIEN nv ON nv.MaNV=h.MaNV
        OUTER APPLY(SELECT SUM(CASE WHEN LoaiGiaoDich=N'Thu' THEN SoTien ELSE -SoTien END) ThuRong
            FROM dbo.THANH_TOAN WHERE MaHD=h.MaHD AND TrangThai=N'Đã xác nhận') t
        WHERE @visitId IS NULL OR h.MaLS=@visitId ORDER BY h.NgayLap DESC,h.MaHD DESC
        """, new { visitId });

    /// <summary>
    /// Khóa lần khám bằng UPDLOCK/HOLDLOCK, yêu cầu đã xác nhận và thời điểm đã đến; lấy nguồn thu chưa tính tiền, kiểm tra giảm giá rồi cấp mã, ghi đầu hóa đơn Nháp, chi tiết và giảm giá trong cùng giao dịch.
    /// </summary>
    /// <param name="visitId">Mã lần khám liên kết; dùng để xác định bệnh nhân hoặc nguồn thu hợp lệ.</param>
    /// <param name="employeeId">Mã nhân viên thực hiện; phải là nhân viên Đang làm việc.</param>
    /// <param name="discount">Số tiền giảm giá, từ 0 đến tổng các khoản thu được đưa vào hóa đơn.</param>
    /// <returns>Mã hóa đơn Nháp vừa lập; hóa đơn được xác nhận bằng thao tác riêng.</returns>
    public string CreateInvoice(string visitId, string employeeId, decimal discount) => Transaction((db, tx) =>
    {
        Employee(db, tx, employeeId);
        var visit = db.QuerySingleOrDefault<Visit>("SELECT MaLS,TrangThai,ThoiGianKham FROM dbo.LICH_SU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@visitId", new { visitId }, tx)
            ?? throw new InvalidOperationException("Không tìm thấy lần khám.");
        if (visit.TrangThai != "Đã xác nhận") throw new InvalidOperationException("Xác nhận lần khám trước khi lập hóa đơn.");
        if (visit.ThoiGianKham > DateTime.Now) throw new InvalidOperationException("Thời điểm lần khám chưa đến; chưa thể lập hóa đơn.");
        // Loại nguồn thu đã gắn chi tiết hóa đơn; khóa lần khám giữ ổn định phạm vi lập hóa đơn đồng thời.
        var charges = db.Query<ET_CHITIETHOADON>(DAL_CHITIETHOADON.EligibleChargesSql, new { visitId }, tx).ToArray();
        if (charges.Length == 0) throw new InvalidOperationException("Lần khám chưa có khoản thu mới: cần dịch vụ đã xác nhận hoặc thuốc đã cấp đủ, chưa nằm trên hóa đơn khác.");
        if (discount < 0 || discount > charges.Sum(c => c.ThanhTien)) throw new InvalidOperationException("Giảm giá phải từ 0 đến tổng khoản thu.");
        var invoiceId = TaoMa(db, tx, "HOA_DON", "MaHD");
        db.Execute("""
            INSERT dbo.HOA_DON(MaHD,MaLS,MaNV,NgayLap,TongTien,GiamGia,TrangThai)
            VALUES(@invoiceId,@visitId,@employeeId,SYSDATETIME(),0,0,N'Nháp')
            """, new { invoiceId, visitId, employeeId }, tx);
        // Đầu hóa đơn bắt đầu với giảm giá 0; ghi đủ chi tiết trước rồi mới áp dụng giảm giá đã kiểm tra.
        foreach (var charge in charges) DAL_CHITIETHOADON.ThemChiTiet(db, tx, invoiceId, charge);
        db.Execute("UPDATE dbo.HOA_DON SET GiamGia=@discount WHERE MaHD=@invoiceId", new { invoiceId, discount }, tx);
        return invoiceId;
    });

    /// <summary>
    /// Đọc mã, trạng thái và số tiền phải thu với UPDLOCK/HOLDLOCK, giữ khóa theo giao dịch để xác nhận, hủy hoặc thanh toán không thao tác đồng thời trên cùng hóa đơn; báo lỗi nếu không tìm thấy.
    /// </summary>
    /// <param name="db">Kết nối SQL đang mở của giao dịch do nơi gọi quản lý.</param>
    /// <param name="tx">Giao dịch đang dùng để đọc/ghi và giữ khóa đồng bộ; được truyền tiếp cho các DAL liên quan nếu có.</param>
    /// <param name="invoiceId">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    /// <returns>ET rút gọn chứa MaHD, TrangThai và SoTienPhaiThu; không có hóa đơn thì ném lỗi.</returns>
    internal static ET_HOADON LockInvoice(IDbConnection db, IDbTransaction? tx, string invoiceId) =>
        db.QuerySingleOrDefault<ET_HOADON>("SELECT MaHD,TrangThai,SoTienPhaiThu FROM dbo.HOA_DON WITH(UPDLOCK,HOLDLOCK) WHERE MaHD=@invoiceId", new { invoiceId }, tx)
        ?? throw new InvalidOperationException("Không tìm thấy hóa đơn.");

    /// <summary>
    /// Khóa hóa đơn, yêu cầu trạng thái Nháp rồi đổi sang Đã xác nhận và đặt NgayXacNhan theo thời gian SQL trong cùng giao dịch.
    /// </summary>
    /// <param name="invoiceId">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    public void ConfirmInvoice(string invoiceId) => Transaction((db, tx) =>
    {
        if (LockInvoice(db, tx, invoiceId).TrangThai != "Nháp") throw new InvalidOperationException("Chỉ xác nhận hóa đơn nháp.");
        db.Execute("UPDATE dbo.HOA_DON SET TrangThai=N'Đã xác nhận',NgayXacNhan=SYSDATETIME() WHERE MaHD=@invoiceId", new { invoiceId }, tx);
        return true;
    });

    /// <summary>
    /// Khóa và yêu cầu hóa đơn còn Nháp; đặt giảm giá về 0, xóa các dòng chi tiết để giải phóng nguồn thu rồi đánh dấu Hủy và xóa ngày xác nhận trong cùng giao dịch.
    /// </summary>
    /// <param name="invoiceId">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    public void CancelInvoice(string invoiceId) => Transaction((db, tx) =>
    {
        if (LockInvoice(db, tx, invoiceId).TrangThai != "Nháp") throw new InvalidOperationException("Chỉ hủy hóa đơn nháp. Hóa đơn xác nhận được giữ nguyên.");
        db.Execute("UPDATE dbo.HOA_DON SET GiamGia=0 WHERE MaHD=@invoiceId", new { invoiceId }, tx);
        // Gỡ chi tiết nháp để truy vấn NOT EXISTS có thể đưa các nguồn này vào hóa đơn khác về sau.
        DAL_CHITIETHOADON.GoChiTietNhap(db, tx, invoiceId);
        db.Execute("UPDATE dbo.HOA_DON SET TrangThai=N'Hủy',NgayXacNhan=NULL WHERE MaHD=@invoiceId", new { invoiceId }, tx);
        return true;
    });

    /// <summary>
    /// Kết quả truy vấn rút gọn của lần khám, chỉ giữ trạng thái và thời điểm để kiểm tra đủ điều kiện lập hóa đơn.
    /// </summary>
    private sealed class Visit
    {
        /// <summary>
        /// Trạng thái SQL của bản được đọc, dùng kiểm tra đã xác nhận trước khi lập hóa đơn hoặc cấp thuốc.
        /// </summary>
        public string TrangThai { get; set; } = "";
        /// <summary>
        /// Thời điểm lần khám để chặn lập hóa đơn cho lần khám chưa đến thời gian.
        /// </summary>
        public DateTime ThoiGianKham { get; set; }
    }
}
