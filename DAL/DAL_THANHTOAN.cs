// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL;
/// <summary>
/// Tầng truy cập SQL cho giao dịch thanh toán trong bảng THANH_TOAN. Ghi nhận thu hoặc hoàn cho hóa đơn đã xác nhận, khóa hóa đơn và đối chiếu tiền thu ròng để ngăn thu hoặc hoàn quá mức. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
/// </summary>
public sealed class DAL_THANHTOAN : dbconnect
{
    /// <summary>
    /// Tên gọi tiếng Việt của RecordPayment: ghi nhận Thu hoặc Hoàn trên hóa đơn đã xác nhận và trả mã giao dịch thanh toán.
    /// </summary>
    /// <param name = "maHD">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    /// <param name = "maNV">Mã nhân viên thực hiện; phải là nhân viên Đang làm việc.</param>
    /// <param name = "soTien">Số tiền giao dịch dương, tối đa hai chữ số thập phân; giới hạn theo tiền còn phải thu hoặc thu ròng.</param>
    /// <param name = "loaiGiaoDich">Loại giao dịch Thu hoặc Hoàn; quyết định giới hạn và dấu khi tính thu ròng.</param>
    /// <param name = "phuongThuc">Phương thức thanh toán không rỗng, dài tối đa 50 ký tự trước khi Trim.</param>
    /// <param name = "lyDo">Lý do giao dịch; bắt buộc có nội dung khi Hoàn, được Trim trước khi ghi.</param>
    /// <returns>Mã giao dịch thu/hoàn Đã xác nhận vừa ghi.</returns>
    public string GhiNhanThanhToan(string maHD, string maNV, decimal soTien, string loaiGiaoDich, string phuongThuc, string? lyDo = null)
    {
        return RecordPayment(maHD, maNV, soTien, loaiGiaoDich, phuongThuc, lyDo);
    }

    /// <summary>
    /// Tạo DAL quản lý giao dịch thanh toán, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
    /// </summary>
    /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
    public DAL_THANHTOAN(IDbTransaction? transaction = null) : base(transaction)
    {
    }

    /// <summary>
    /// Trả danh sách giao dịch thanh toán dưới dạng mảng bằng luồng layToanBo, phù hợp nơi gọi cần truy cập và đếm kết quả ngay.
    /// </summary>
    /// <returns>Mảng ET; không có bản ghi thì mảng rỗng.</returns>
    public ET_THANHTOAN[] LayDanhSach()
    {
        return layToanBo().ToArray();
    }

    /// <summary>
    /// Đọc toàn bộ giao dịch thanh toán từ dbo.THANH_TOAN, Dapper ánh xạ từng dòng sang ET; SQL không quy định thứ tự; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
    /// </summary>
    /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
    public IEnumerable<ET_THANHTOAN> layToanBo()
    {
        return ThucHien((db, tx) =>
        {
            return db.Query<ET_THANHTOAN>("SELECT * FROM dbo.THANH_TOAN", transaction: tx).ToArray();
        });
    }

    /// <summary>
    /// Tìm giao dịch thanh toán theo khóa trong dbo.THANH_TOAN; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
    /// </summary>
    /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
    /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
    public ET_THANHTOAN? layTheoMa(string ma)
    {
        return ThucHien((db, tx) =>
        {
            return db.QuerySingleOrDefault<ET_THANHTOAN>("SELECT * FROM dbo.THANH_TOAN WHERE MaTT=@ma", new { ma }, tx);
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
    /// Kiểm tra tồn tại nhân viên có trạng thái Đang làm việc; ném lỗi nếu người thực hiện giao dịch không hợp lệ.
    /// </summary>
    /// <param name = "db">Kết nối SQL đang mở của giao dịch do nơi gọi quản lý.</param>
    /// <param name = "tx">Giao dịch đang dùng để đọc/ghi và giữ khóa đồng bộ; được truyền tiếp cho các DAL liên quan nếu có.</param>
    /// <param name = "id">Mã nhân viên cần kiểm tra đang làm việc.</param>
    private static void KiemTraNhanVien(IDbConnection db, IDbTransaction? tx, string id)
    {
        if (!db.ExecuteScalar<bool>("SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.NHAN_VIEN WHERE MaNV=@id AND TrangThai=N'Đang làm việc') THEN 1 ELSE 0 END AS bit)", new { id }, tx))
        {
            throw new InvalidOperationException("Chọn nhân viên đang làm việc để thực hiện giao dịch.");
        }
    }

    /// <summary>
    /// Đọc các lần thu/hoàn của hóa đơn cùng tên nhân viên thực hiện; sắp ngày thanh toán và mã giao dịch mới nhất trước, không lọc riêng trạng thái.
    /// </summary>
    /// <param name = "invoiceId">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    /// <returns>DataTable đã nạp đủ các dòng kết quả; có thể rỗng nếu chưa có dữ liệu phù hợp.</returns>
    public DataTable GetPayments(string invoiceId)
    {
        return Query("""
        SELECT t.*,n.HoTen NguoiThucHien FROM dbo.THANH_TOAN t
        JOIN dbo.NHAN_VIEN n ON n.MaNV=t.MaNV WHERE MaHD=@invoiceId ORDER BY NgayThanhToan DESC,MaTT DESC
        """, new { invoiceId });
    }

    /// <summary>
    /// Khóa hóa đơn đã xác nhận; kiểm tra nhân viên, số tiền dương tối đa hai chữ số lẻ, loại Thu/Hoàn, phương thức và lý do hoàn. Đối chiếu thu ròng để chặn vượt số phải thu/số đã thu, cấp mã và ghi giao dịch Đã xác nhận trong cùng giao dịch.
    /// </summary>
    /// <param name = "invoiceId">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    /// <param name = "employeeId">Mã nhân viên thực hiện; phải là nhân viên Đang làm việc.</param>
    /// <param name = "amount">Số tiền giao dịch dương, tối đa hai chữ số thập phân; giới hạn theo tiền còn phải thu hoặc thu ròng.</param>
    /// <param name = "type">Loại giao dịch Thu hoặc Hoàn; quyết định giới hạn và dấu khi tính thu ròng.</param>
    /// <param name = "method">Phương thức thanh toán không rỗng, dài tối đa 50 ký tự trước khi Trim.</param>
    /// <param name = "reason">Lý do giao dịch; bắt buộc có nội dung khi Hoàn, được Trim trước khi ghi.</param>
    /// <returns>Mã giao dịch thu/hoàn Đã xác nhận vừa ghi.</returns>
    public string RecordPayment(string invoiceId, string employeeId, decimal amount, string type, string method, string? reason)
        => RecordPaymentCore(invoiceId, employeeId, amount, type, method, reason, null, null);

    internal string RecordCoordinatedPayment(string invoiceId, string employeeId, decimal amount, string method, decimal given, string? reference)
        => RecordPaymentCore(invoiceId, employeeId, amount, "Thu", method, null, given, reference);

    private string RecordPaymentCore(string invoiceId, string employeeId, decimal amount, string type, string method, string? reason, decimal? given, string? reference)
    {
        return ThucHien((db, tx) =>
        {
            KiemTraNhanVien(db, tx, employeeId);
            // Khóa hóa đơn trước khi tính thu ròng để hai giao dịch không cùng dùng một số tiền còn có thể thu/hoàn.
            var invoice = DAL_HOADON.LockInvoice(db, tx, invoiceId);
            if (given is null) DAL_HOADON.KiemTraHoaDonLegacy(invoice);
            else if (invoice.PhienBanQuyTrinh != 2) throw new InvalidOperationException("Cần hóa đơn quy trình 2.");
            if (invoice.TrangThai != "Đã xác nhận")
            {
                throw new InvalidOperationException("Xác nhận hóa đơn trước khi thu hoặc hoàn tiền.");
            }

            if (amount <= 0 || decimal.Round(amount, 2) != amount)
            {
                throw new InvalidOperationException("Số tiền phải lớn hơn 0 và có tối đa 2 chữ số thập phân.");
            }

            if (type != "Thu" && type != "Hoàn")
            {
                throw new InvalidOperationException("Loại giao dịch phải là Thu hoặc Hoàn.");
            }

            if (string.IsNullOrWhiteSpace(method) || method.Length > 50)
            {
                throw new InvalidOperationException("Chọn phương thức thanh toán hợp lệ.");
            }

            if (type == "Hoàn" && string.IsNullOrWhiteSpace(reason))
            {
                throw new InvalidOperationException("Nhập lý do hoàn tiền.");
            }

            // Thu ròng = tổng Thu trừ tổng Hoàn đã xác nhận; cả hai loại vẫn lưu SoTien dương trong SQL.
            decimal soTienDaThuRong = db.ExecuteScalar<decimal>("""
                SELECT COALESCE(SUM(
                    CASE WHEN LoaiGiaoDich=N'Thu' THEN SoTien ELSE -SoTien END
                ),0)
                FROM dbo.THANH_TOAN
                WHERE MaHD=@invoiceId AND TrangThai=N'Đã xác nhận'
                """, new { invoiceId }, tx);

            if (type == "Thu")
            {
                decimal conPhaiTra = invoice.SoTienPhaiThu - soTienDaThuRong;
                if (amount > conPhaiTra)
                {
                    throw new InvalidOperationException("Số thu vượt số còn phải trả của hóa đơn.");
                }
            }
            else
            {
                if (amount > soTienDaThuRong)
                {
                    throw new InvalidOperationException("Số hoàn vượt tiền đã thu ròng của hóa đơn.");
                }
            }

            var paymentId = TaoMa(db, tx, "THANH_TOAN", "MaTT");
            db.Execute("""
            INSERT dbo.THANH_TOAN(MaTT,MaHD,MaNV,NgayThanhToan,SoTien,LoaiGiaoDich,PhuongThuc,LyDo,TrangThai,SoTienKhachDua,TienThua,MaThamChieu)
            VALUES(@paymentId,@invoiceId,@employeeId,SYSDATETIME(),@amount,@type,@method,@reason,N'Đã xác nhận',@given,@change,@reference)
            """, new { paymentId, invoiceId, employeeId, amount, type, method = method.Trim(), reason = reason?.Trim(), given, change = given - amount, reference }, tx);
            return paymentId;
        });
    }
}
