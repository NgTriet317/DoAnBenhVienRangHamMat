using System.Data;
using Dapper;
using ET;

namespace DAL;

public sealed class DAL_THANHTOAN : dbconnect
{
    public string GhiNhanThanhToan(string maHD, string maNV, decimal soTien, string loaiGiaoDich, string phuongThuc, string? lyDo = null)
        => RecordPayment(maHD, maNV, soTien, loaiGiaoDich, phuongThuc, lyDo);
    private readonly DAL_GIAODICH? giaoDich;
    public DAL_THANHTOAN(DAL_GIAODICH? giaoDich = null) { this.giaoDich = giaoDich; }
    public ET_THANHTOAN[] LayDanhSach() => layToanBo().ToArray();
    public IEnumerable<ET_THANHTOAN> layToanBo() => Doc((db, tx) =>
        db.Query<ET_THANHTOAN>("SELECT * FROM dbo.THANH_TOAN", transaction: tx).ToArray());
    public ET_THANHTOAN? layTheoMa(string ma) => Doc((db, tx) =>
        db.QuerySingleOrDefault<ET_THANHTOAN>("SELECT * FROM dbo.THANH_TOAN WHERE MaTT=@ma", new { ma }, tx));

    private T Doc<T>(Func<IDbConnection, IDbTransaction?, T> operation)
    {
        if (giaoDich is not null) return giaoDich.ThucHien(operation);
        using var db = CreateConnection();
        return operation(db, null);
    }

    private T Transaction<T>(Func<IDbConnection, IDbTransaction?, T> operation)
    {
        if (giaoDich is not null) return giaoDich.ThucHien(operation);
        using var transaction = new DAL_GIAODICH();
        return transaction.ThucHien(operation);
    }

    private DataTable Query(string sql, object? parameters = null) => Doc((connection, tx) =>
    {
        using var reader = connection.ExecuteReader(sql, parameters, tx);
        var table = new DataTable();
        table.Load(reader);
        return table;
    });

    private static void Employee(IDbConnection db, IDbTransaction? tx, string id)
    {
        if (!db.ExecuteScalar<bool>("SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.NHAN_VIEN WHERE MaNV=@id AND TrangThai=N'Đang làm việc') THEN 1 ELSE 0 END AS bit)", new { id }, tx))
            throw new InvalidOperationException("Chọn nhân viên đang làm việc để thực hiện giao dịch.");
    }

    public DataTable GetPayments(string invoiceId) => Query("""
        SELECT t.*,n.HoTen NguoiThucHien FROM dbo.THANH_TOAN t
        JOIN dbo.NHAN_VIEN n ON n.MaNV=t.MaNV WHERE MaHD=@invoiceId ORDER BY NgayThanhToan DESC,MaTT DESC
        """, new { invoiceId });

    public string RecordPayment(string invoiceId, string employeeId, decimal amount, string type, string method, string? reason) => Transaction((db, tx) =>
    {
        Employee(db, tx, employeeId);
        var invoice = DAL_HOADON.LockInvoice(db, tx, invoiceId);
        if (invoice.TrangThai != "Đã xác nhận") throw new InvalidOperationException("Xác nhận hóa đơn trước khi thu hoặc hoàn tiền.");
        if (amount <= 0 || decimal.Round(amount, 2) != amount) throw new InvalidOperationException("Số tiền phải lớn hơn 0 và có tối đa 2 chữ số thập phân.");
        if (type is not "Thu" and not "Hoàn") throw new InvalidOperationException("Loại giao dịch phải là Thu hoặc Hoàn.");
        if (string.IsNullOrWhiteSpace(method) || method.Length > 50) throw new InvalidOperationException("Chọn phương thức thanh toán hợp lệ.");
        if (type == "Hoàn" && string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Nhập lý do hoàn tiền.");
        var net = db.ExecuteScalar<decimal>("SELECT COALESCE(SUM(CASE WHEN LoaiGiaoDich=N'Thu' THEN SoTien ELSE -SoTien END),0) FROM dbo.THANH_TOAN WHERE MaHD=@invoiceId AND TrangThai=N'Đã xác nhận'", new { invoiceId }, tx);
        if (type == "Thu" && amount > invoice.SoTienPhaiThu - net) throw new InvalidOperationException("Số thu vượt số còn phải trả của hóa đơn.");
        if (type == "Hoàn" && amount > net) throw new InvalidOperationException("Số hoàn vượt tiền đã thu ròng của hóa đơn.");
        var paymentId = DAL_GIAODICH.TaoMa(db, tx, "THANH_TOAN", "MaTT");
        db.Execute("""
            INSERT dbo.THANH_TOAN(MaTT,MaHD,MaNV,NgayThanhToan,SoTien,LoaiGiaoDich,PhuongThuc,LyDo,TrangThai)
            VALUES(@paymentId,@invoiceId,@employeeId,SYSDATETIME(),@amount,@type,@method,@reason,N'Đã xác nhận')
            """, new { paymentId, invoiceId, employeeId, amount, type, method = method.Trim(), reason = reason?.Trim() }, tx);
        return paymentId;
    });

}
