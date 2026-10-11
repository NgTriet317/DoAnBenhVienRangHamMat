using System.Data;
using Dapper;
using ET;

namespace DAL;

public sealed class DAL_HOADON : dbconnect
{
    public string LapHoaDon(string maLS, string maNV, decimal giamGia = 0) => CreateInvoice(maLS, maNV, giamGia);
    public void XacNhanHoaDon(string maHD) => ConfirmInvoice(maHD);
    public void HuyHoaDonNhap(string maHD) => CancelInvoice(maHD);
    private readonly DAL_GIAODICH? giaoDich;
    public DAL_HOADON(DAL_GIAODICH? giaoDich = null) { this.giaoDich = giaoDich; }
    public ET_HOADON[] LayDanhSach() => layToanBo().ToArray();
    public IEnumerable<ET_HOADON> layToanBo() => Doc((db, tx) =>
        db.Query<ET_HOADON>("SELECT * FROM dbo.HOA_DON", transaction: tx).ToArray());
    public ET_HOADON? layTheoMa(string ma) => Doc((db, tx) =>
        db.QuerySingleOrDefault<ET_HOADON>("SELECT * FROM dbo.HOA_DON WHERE MaHD=@ma", new { ma }, tx));

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

    public string CreateInvoice(string visitId, string employeeId, decimal discount) => Transaction((db, tx) =>
    {
        Employee(db, tx, employeeId);
        var visit = db.QuerySingleOrDefault<Visit>("SELECT MaLS,TrangThai,ThoiGianKham FROM dbo.LICH_SU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@visitId", new { visitId }, tx)
            ?? throw new InvalidOperationException("Không tìm thấy lần khám.");
        if (visit.TrangThai != "Đã xác nhận") throw new InvalidOperationException("Xác nhận lần khám trước khi lập hóa đơn.");
        if (visit.ThoiGianKham > DateTime.Now) throw new InvalidOperationException("Thời điểm lần khám chưa đến; chưa thể lập hóa đơn.");
        var charges = db.Query<ET_CHITIETHOADON>(DAL_CHITIETHOADON.EligibleChargesSql, new { visitId }, tx).ToArray();
        if (charges.Length == 0) throw new InvalidOperationException("Lần khám chưa có khoản thu mới: cần dịch vụ đã xác nhận hoặc thuốc đã cấp đủ, chưa nằm trên hóa đơn khác.");
        if (discount < 0 || discount > charges.Sum(c => c.ThanhTien)) throw new InvalidOperationException("Giảm giá phải từ 0 đến tổng khoản thu.");
        var invoiceId = DAL_GIAODICH.TaoMa(db, tx, "HOA_DON", "MaHD");
        db.Execute("""
            INSERT dbo.HOA_DON(MaHD,MaLS,MaNV,NgayLap,TongTien,GiamGia,TrangThai)
            VALUES(@invoiceId,@visitId,@employeeId,SYSDATETIME(),0,0,N'Nháp')
            """, new { invoiceId, visitId, employeeId }, tx);
        foreach (var charge in charges) DAL_CHITIETHOADON.ThemChiTiet(db, tx, invoiceId, charge);
        db.Execute("UPDATE dbo.HOA_DON SET GiamGia=@discount WHERE MaHD=@invoiceId", new { invoiceId, discount }, tx);
        return invoiceId;
    });

    internal static ET_HOADON LockInvoice(IDbConnection db, IDbTransaction? tx, string invoiceId) =>
        db.QuerySingleOrDefault<ET_HOADON>("SELECT MaHD,TrangThai,SoTienPhaiThu FROM dbo.HOA_DON WITH(UPDLOCK,HOLDLOCK) WHERE MaHD=@invoiceId", new { invoiceId }, tx)
        ?? throw new InvalidOperationException("Không tìm thấy hóa đơn.");

    public void ConfirmInvoice(string invoiceId) => Transaction((db, tx) =>
    {
        if (LockInvoice(db, tx, invoiceId).TrangThai != "Nháp") throw new InvalidOperationException("Chỉ xác nhận hóa đơn nháp.");
        db.Execute("UPDATE dbo.HOA_DON SET TrangThai=N'Đã xác nhận',NgayXacNhan=SYSDATETIME() WHERE MaHD=@invoiceId", new { invoiceId }, tx);
        return true;
    });

    public void CancelInvoice(string invoiceId) => Transaction((db, tx) =>
    {
        if (LockInvoice(db, tx, invoiceId).TrangThai != "Nháp") throw new InvalidOperationException("Chỉ hủy hóa đơn nháp. Hóa đơn xác nhận được giữ nguyên.");
        db.Execute("UPDATE dbo.HOA_DON SET GiamGia=0 WHERE MaHD=@invoiceId", new { invoiceId }, tx);
        DAL_CHITIETHOADON.GoChiTietNhap(db, tx, invoiceId);
        db.Execute("UPDATE dbo.HOA_DON SET TrangThai=N'Hủy',NgayXacNhan=NULL WHERE MaHD=@invoiceId", new { invoiceId }, tx);
        return true;
    });

    private sealed class Visit { public string TrangThai { get; set; } = ""; public DateTime ThoiGianKham { get; set; } }
}
