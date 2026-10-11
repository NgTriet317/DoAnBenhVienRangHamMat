using System.Data;
using Dapper;
using ET;

namespace DAL;

public sealed class DAL_PHIEUXUAT : dbconnect
{
    public string CapDonThuoc(string maDT, string maKho, string maNV) => DispensePrescription(maDT, maKho, maNV);
    private readonly DAL_GIAODICH? giaoDich;
    public DAL_PHIEUXUAT(DAL_GIAODICH? giaoDich = null) { this.giaoDich = giaoDich; }
    public ET_PHIEUXUAT[] LayDanhSach() => layToanBo().ToArray();
    public IEnumerable<ET_PHIEUXUAT> layToanBo() => Doc((db, tx) =>
        db.Query<ET_PHIEUXUAT>("SELECT * FROM dbo.PHIEU_XUAT", transaction: tx).ToArray());
    public ET_PHIEUXUAT? layTheoMa(string ma) => Doc((db, tx) =>
        db.QuerySingleOrDefault<ET_PHIEUXUAT>("SELECT * FROM dbo.PHIEU_XUAT WHERE MaPX=@ma", new { ma }, tx));

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

    private const string StockSql = """
        SELECT l.MaLo,l.MaHH,l.SoLo,l.NgayNhap,l.HanSuDung,
          COALESCE(n.SoLuong,0)-COALESCE(x.SoLuong,0) TonKho
        FROM dbo.LO_HANG_HOA l
        OUTER APPLY(SELECT SUM(c.SoLuong) SoLuong FROM dbo.CHI_TIET_PHIEU_NHAP c JOIN dbo.PHIEU_NHAP p ON p.MaPN=c.MaPN
          WHERE c.MaLo=l.MaLo AND p.MaKho=@warehouseId AND p.TrangThai=N'Đã ghi sổ') n
        OUTER APPLY(SELECT SUM(c.SoLuong) SoLuong FROM dbo.CHI_TIET_PHIEU_XUAT c JOIN dbo.PHIEU_XUAT p ON p.MaPX=c.MaPX
          WHERE c.MaLo=l.MaLo AND p.MaKho=@warehouseId AND p.TrangThai=N'Đã ghi sổ') x
        WHERE l.MaKho=@warehouseId AND l.MaHH IN(SELECT t.MaHH FROM dbo.CHI_TIET_DON_THUOC c JOIN dbo.THUOC t ON t.MaThuoc=c.MaThuoc WHERE c.MaDT=@prescriptionId)
          AND l.TrangThai=N'Còn sử dụng' AND l.NgayNhap<=SYSDATETIME()
          AND(l.HanSuDung IS NULL OR l.HanSuDung>=CAST(SYSDATETIME() AS date))
          AND COALESCE(n.SoLuong,0)>COALESCE(x.SoLuong,0)
        ORDER BY l.NgayNhap,CASE WHEN l.HanSuDung IS NULL THEN 1 ELSE 0 END,l.HanSuDung,l.MaLo
        """;

    public DataTable GetStock(string warehouseId, string prescriptionId) => Query(StockSql, new { warehouseId, prescriptionId });

    public string DispensePrescription(string prescriptionId, string warehouseId, string employeeId) => Transaction((db, tx) =>
    {
        Employee(db, tx, employeeId);
        if (!db.ExecuteScalar<bool>("SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.KHO WHERE MaKho=@warehouseId AND TrangThai=N'Đang hoạt động') THEN 1 ELSE 0 END AS bit)", new { warehouseId }, tx))
            throw new InvalidOperationException("Chọn kho đang hoạt động.");
        var visitId = db.ExecuteScalar<string?>("SELECT MaLS FROM dbo.DON_THUOC WHERE MaDT=@prescriptionId", new { prescriptionId }, tx)
            ?? throw new InvalidOperationException("Không tìm thấy đơn thuốc.");
        db.ExecuteScalar<string>("SELECT MaLS FROM dbo.LICH_SU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@visitId", new { visitId }, tx);
        // The installed stock triggers lock the same goods before prescription headers.
        db.Query<string>("""
            SELECT h.MaHH FROM dbo.HANG_HOA h WITH(UPDLOCK,HOLDLOCK)
            WHERE h.MaHH IN(SELECT t.MaHH FROM dbo.CHI_TIET_DON_THUOC c JOIN dbo.THUOC t ON t.MaThuoc=c.MaThuoc WHERE c.MaDT=@prescriptionId)
            ORDER BY h.MaHH
            """, new { prescriptionId }, tx).ToArray();
        var prescription = db.QuerySingle<Prescription>("SELECT MaLS,TrangThai,NgayKe FROM dbo.DON_THUOC WITH(UPDLOCK,HOLDLOCK) WHERE MaDT=@prescriptionId", new { prescriptionId }, tx);
        if (prescription.TrangThai != "Đã xác nhận") throw new InvalidOperationException("Chỉ cấp đơn thuốc đã xác nhận và còn áp dụng.");
        if (prescription.NgayKe > DateTime.Now) throw new InvalidOperationException("Ngày kê đơn chưa đến.");
        if (db.ExecuteScalar<int>("""
            SELECT COUNT(*) FROM dbo.CHI_TIET_PHIEU_XUAT x JOIN dbo.PHIEU_XUAT p ON p.MaPX=x.MaPX
            JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaCTDT=x.MaCTDT WHERE c.MaDT=@prescriptionId AND p.TrangThai=N'Đã ghi sổ'
            """, new { prescriptionId }, tx) > 0) throw new InvalidOperationException("Đơn thuốc đã được cấp; không thể cấp lại.");
        var lines = db.Query<PrescriptionLine>("SELECT c.MaCTDT,c.SoLuong,t.MaHH FROM dbo.CHI_TIET_DON_THUOC c JOIN dbo.THUOC t ON t.MaThuoc=c.MaThuoc WHERE c.MaDT=@prescriptionId ORDER BY c.MaCTDT", new { prescriptionId }, tx).ToArray();
        if (lines.Length == 0) throw new InvalidOperationException("Đơn thuốc không có thuốc để cấp.");
        var lots = db.Query<Lot>(StockSql, new { warehouseId, prescriptionId }, tx).ToArray();
        var allocations = new List<(string Line, string Goods, string Lot, decimal Quantity)>();
        foreach (var line in lines)
        {
            var remaining = line.SoLuong;
            foreach (var lot in lots.Where(l => l.MaHH == line.MaHH && l.TonKho > 0))
            {
                var quantity = Math.Min(remaining, lot.TonKho);
                allocations.Add((line.MaCTDT, line.MaHH, lot.MaLo, quantity));
                lot.TonKho -= quantity; remaining -= quantity;
                if (remaining == 0) break;
            }
            if (remaining > 0) throw new InvalidOperationException($"Kho không đủ thuốc mã hàng {line.MaHH} (thiếu {remaining:N3}). Chưa cấp bất kỳ thuốc nào; chọn kho đủ toàn bộ đơn hoặc bổ sung tồn hợp lệ.");
        }
        var exportId = DAL_GIAODICH.TaoMa(db, tx, "PHIEU_XUAT", "MaPX");
        db.Execute("""
            INSERT dbo.PHIEU_XUAT(MaPX,MaKho,MaNV,MaLS,NgayXuat,LyDo,TrangThai,GhiChu)
            VALUES(@exportId,@warehouseId,@employeeId,@visitId,SYSDATETIME(),N'Cấp đủ đơn thuốc',N'Nháp',@note)
            """, new { exportId, warehouseId, employeeId, visitId, note = $"Đơn thuốc {prescriptionId}" }, tx);
        foreach (var allocation in allocations)
            DAL_CHITIETPHIEUXUAT.ThemChiTiet(db, tx, new ET_CHITIETPHIEUXUAT
            { MaPX = exportId, MaHH = allocation.Goods, MaLo = allocation.Lot, MaCTDT = allocation.Line, SoLuong = allocation.Quantity, GhiChu = "Cấp theo đơn thuốc" });
        db.Execute("UPDATE dbo.PHIEU_XUAT SET TrangThai=N'Đã ghi sổ' WHERE MaPX=@exportId", new { exportId }, tx);
        return exportId;
    });

    private sealed class Prescription { public string MaLS { get; set; } public string TrangThai { get; set; } = ""; public DateTime NgayKe { get; set; } }
    private sealed class PrescriptionLine { public string MaCTDT { get; set; } public string MaHH { get; set; } public decimal SoLuong { get; set; } }
    private sealed class Lot { public string MaLo { get; set; } public string MaHH { get; set; } public decimal TonKho { get; set; } }
}
