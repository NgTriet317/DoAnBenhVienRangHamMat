using System.Data;
using Dapper;
using ET;

namespace DAL;

public sealed class DAL_CHITIETHOADON : dbconnect
{
    private readonly DAL_GIAODICH? giaoDich;
    public DAL_CHITIETHOADON(DAL_GIAODICH? giaoDich = null) { this.giaoDich = giaoDich; }
    public ET_CHITIETHOADON[] LayDanhSach() => layToanBo().ToArray();
    public IEnumerable<ET_CHITIETHOADON> layToanBo() => Doc((db, tx) =>
        db.Query<ET_CHITIETHOADON>("SELECT * FROM dbo.CHI_TIET_HOA_DON", transaction: tx).ToArray());
    public ET_CHITIETHOADON? layTheoMa(string ma) => Doc((db, tx) =>
        db.QuerySingleOrDefault<ET_CHITIETHOADON>("SELECT * FROM dbo.CHI_TIET_HOA_DON WHERE MaCTHD=@ma", new { ma }, tx));

    private T Doc<T>(Func<IDbConnection, IDbTransaction?, T> operation)
    {
        if (giaoDich is not null) return giaoDich.ThucHien(operation);
        using var db = CreateConnection();
        return operation(db, null);
    }

    private DataTable Query(string sql, object? parameters = null) => Doc((connection, tx) =>
    {
        using var reader = connection.ExecuteReader(sql, parameters, tx);
        var table = new DataTable();
        table.Load(reader);
        return table;
    });

    public DataTable GetInvoiceDetails(string invoiceId) => Query("""
        SELECT c.*,COALESCE(s.SoLuong,x.SoLuong) SoLuong,
            COALESCE(s.DonGia,c.DonGia) DonGiaChot,
            CASE WHEN c.MaDVThucHien IS NOT NULL THEN N'Dịch vụ' ELSE N'Thuốc đã cấp' END Loai,
            d.MaDT,l.SoLo,hh.DonViTinh
        FROM dbo.CHI_TIET_HOA_DON c
        LEFT JOIN dbo.DICH_VU_THUC_HIEN s ON s.MaDVThucHien=c.MaDVThucHien
        LEFT JOIN dbo.CHI_TIET_PHIEU_XUAT x ON x.MaCTPX=c.MaCTPX
        LEFT JOIN dbo.CHI_TIET_DON_THUOC d ON d.MaCTDT=x.MaCTDT
        LEFT JOIN dbo.HANG_HOA hh ON hh.MaHH=x.MaHH
        LEFT JOIN dbo.LO_HANG_HOA l ON l.MaLo=x.MaLo
        WHERE c.MaHD=@invoiceId ORDER BY c.MaCTHD
        """, new { invoiceId });

    internal const string EligibleChargesSql = """
        SELECT s.MaDVThucHien,CAST(NULL AS nvarchar(50)) MaCTPX,d.TenDV NoiDung,s.SoLuong,
            s.DonGia DonGiaChot,CAST(NULL AS decimal(18,2)) DonGia,s.ThanhTien,N'Dịch vụ' Loai
        FROM dbo.DICH_VU_THUC_HIEN s JOIN dbo.DICH_VU d ON d.MaDV=s.MaDV
        WHERE s.MaLS=@visitId AND s.TrangThai=N'Đã xác nhận'
            AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON c WHERE c.MaDVThucHien=s.MaDVThucHien)
        UNION ALL
        SELECT NULL,x.MaCTPX,hh.TenHH+N' '+t.HamLuong,x.SoLuong,t.DonGiaBan,t.DonGiaBan,
            CAST(ROUND(x.SoLuong*t.DonGiaBan,2) AS decimal(18,2)),N'Thuốc đã cấp'
        FROM dbo.CHI_TIET_PHIEU_XUAT x JOIN dbo.PHIEU_XUAT p ON p.MaPX=x.MaPX
        JOIN dbo.CHI_TIET_DON_THUOC ct ON ct.MaCTDT=x.MaCTDT
        JOIN dbo.DON_THUOC dt ON dt.MaDT=ct.MaDT JOIN dbo.THUOC t WITH(HOLDLOCK) ON t.MaThuoc=ct.MaThuoc
        JOIN dbo.HANG_HOA hh ON hh.MaHH=x.MaHH
        WHERE p.MaLS=@visitId AND dt.MaLS=@visitId AND p.TrangThai=N'Đã ghi sổ'
            AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON c WHERE c.MaCTPX=x.MaCTPX)
        """;
    public DataTable GetEligibleCharges(string visitId) => Query(EligibleChargesSql, new { visitId });

    internal static void ThemChiTiet(IDbConnection db, IDbTransaction? tx, string invoiceId, ET_CHITIETHOADON charge)
    {
        var detailId = DAL_GIAODICH.TaoMa(db, tx, "CHI_TIET_HOA_DON", "MaCTHD");
        db.Execute("INSERT dbo.CHI_TIET_HOA_DON(MaCTHD,MaHD,MaDVThucHien,MaCTPX,NoiDung,DonGia) VALUES(@detailId,@invoiceId,@MaDVThucHien,@MaCTPX,@NoiDung,@DonGia)", new { detailId, invoiceId, charge.MaDVThucHien, charge.MaCTPX, charge.NoiDung, charge.DonGia }, tx);
    }
    internal static void GoChiTietNhap(IDbConnection db, IDbTransaction? tx, string invoiceId) =>
        db.Execute("DELETE dbo.CHI_TIET_HOA_DON WHERE MaHD=@invoiceId", new { invoiceId }, tx);
}
