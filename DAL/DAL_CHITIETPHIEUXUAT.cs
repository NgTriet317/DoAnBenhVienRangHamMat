using System.Data;
using Dapper;
using ET;

namespace DAL;

public sealed class DAL_CHITIETPHIEUXUAT : dbconnect
{
    private readonly DAL_GIAODICH? giaoDich;
    public DAL_CHITIETPHIEUXUAT(DAL_GIAODICH? giaoDich = null) { this.giaoDich = giaoDich; }
    public ET_CHITIETPHIEUXUAT[] LayDanhSach() => layToanBo().ToArray();
    public IEnumerable<ET_CHITIETPHIEUXUAT> layToanBo() => Doc((db, tx) =>
        db.Query<ET_CHITIETPHIEUXUAT>("SELECT * FROM dbo.CHI_TIET_PHIEU_XUAT", transaction: tx).ToArray());
    public ET_CHITIETPHIEUXUAT? layTheoMa(string ma) => Doc((db, tx) =>
        db.QuerySingleOrDefault<ET_CHITIETPHIEUXUAT>("SELECT * FROM dbo.CHI_TIET_PHIEU_XUAT WHERE MaCTPX=@ma", new { ma }, tx));

    private T Doc<T>(Func<IDbConnection, IDbTransaction?, T> operation)
    {
        if (giaoDich is not null) return giaoDich.ThucHien(operation);
        using var db = CreateConnection();
        return operation(db, null);
    }

    public IEnumerable<ET_CHITIETPHIEUXUAT> layTheoPhieu(string maPX) => Doc((db, tx) =>
        db.Query<ET_CHITIETPHIEUXUAT>("SELECT * FROM dbo.CHI_TIET_PHIEU_XUAT WHERE MaPX=@maPX ORDER BY MaCTPX", new { maPX }, tx).ToArray());
    internal static void ThemChiTiet(IDbConnection db, IDbTransaction? tx, ET_CHITIETPHIEUXUAT detail)
    {
        detail.MaCTPX = DAL_GIAODICH.TaoMa(db, tx, "CHI_TIET_PHIEU_XUAT", "MaCTPX");
        db.Execute("INSERT dbo.CHI_TIET_PHIEU_XUAT(MaCTPX,MaPX,MaHH,MaLo,MaCTDT,SoLuong,GhiChu) VALUES(@MaCTPX,@MaPX,@MaHH,@MaLo,@MaCTDT,@SoLuong,@GhiChu)", detail, tx);
    }
}
