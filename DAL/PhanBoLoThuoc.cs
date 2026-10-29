using System.Data;
using Dapper;

namespace DAL;

/// <summary>Cùng truy vấn tồn và thuật toán cho bảng tính lẫn xuất kho. Giữ chính sách FIFO hiện có.</summary>
internal static class PhanBoLoThuoc
{
    internal const string StockSql = """
        SELECT l.MaLo,l.MaHH,l.SoLo,l.NgayNhap,l.HanSuDung,
          COALESCE(n.SoLuong,0)-COALESCE(x.SoLuong,0) TonKho
        FROM dbo.LO_HANG_HOA l WITH(HOLDLOCK)
        OUTER APPLY(SELECT SUM(c.SoLuong) SoLuong FROM dbo.CHI_TIET_PHIEU_NHAP c WITH(HOLDLOCK)
          JOIN dbo.PHIEU_NHAP p WITH(HOLDLOCK) ON p.MaPN=c.MaPN
          WHERE c.MaLo=l.MaLo AND p.MaKho=@warehouseId AND p.TrangThai=N'Đã ghi sổ') n
        OUTER APPLY(SELECT SUM(c.SoLuong) SoLuong FROM dbo.CHI_TIET_PHIEU_XUAT c WITH(HOLDLOCK)
          JOIN dbo.PHIEU_XUAT p WITH(HOLDLOCK) ON p.MaPX=c.MaPX
          WHERE c.MaLo=l.MaLo AND p.MaKho=@warehouseId AND p.TrangThai=N'Đã ghi sổ') x
        WHERE l.MaKho=@warehouseId AND l.MaHH IN(SELECT t.MaHH FROM dbo.CHI_TIET_DON_THUOC c WITH(HOLDLOCK)
          JOIN dbo.THUOC t WITH(HOLDLOCK) ON t.MaThuoc=c.MaThuoc WHERE c.MaDT IN @prescriptionIds)
          AND l.TrangThai=N'Còn sử dụng' AND l.NgayNhap<=SYSDATETIME()
          AND(l.HanSuDung IS NULL OR l.HanSuDung>=CAST(SYSDATETIME() AS date))
          AND COALESCE(n.SoLuong,0)>COALESCE(x.SoLuong,0)
        ORDER BY l.NgayNhap,CASE WHEN l.HanSuDung IS NULL THEN 1 ELSE 0 END,l.HanSuDung,l.MaLo
        """;

    internal static Lo[] LayLo(IDbConnection db, IDbTransaction? tx, string warehouseId, string[] prescriptionIds)
        => db.Query<Lo>(StockSql, new { warehouseId, prescriptionIds }, tx).ToArray();

    internal static IReadOnlyList<PhanCap> Lap(IEnumerable<Dong> lines, IReadOnlyList<Lo> lots)
    {
        // Không sửa tồn đầu vào: dấu vân tay cần lượng khả dụng trước khi phân bổ.
        var remaining = lots.ToDictionary(l => l.MaLo, l => l.TonKho, StringComparer.OrdinalIgnoreCase);
        var result = new List<PhanCap>();
        foreach (var line in lines)
        {
            var need = line.SoLuong;
            foreach (var lot in lots)
            {
                if (!string.Equals(lot.MaHH, line.MaHH, StringComparison.OrdinalIgnoreCase) || remaining[lot.MaLo] <= 0) continue;
                var take = Math.Min(need, remaining[lot.MaLo]);
                result.Add(new(line.MaCTDT, line.MaHH, lot.MaLo, take));
                remaining[lot.MaLo] -= take;
                need -= take;
                if (need == 0) break;
            }
            if (need > 0)
                throw new InvalidOperationException($"Kho không đủ thuốc mã hàng {line.MaHH} (thiếu {need:N3}). Chọn kho đủ toàn bộ đơn hoặc bổ sung tồn hợp lệ.");
        }
        return result;
    }

    internal sealed class Dong
    {
        public string MaCTDT { get; set; } = "";
        public string MaHH { get; set; } = "";
        public decimal SoLuong { get; set; }
    }
    internal sealed class Lo
    {
        public string MaLo { get; set; } = "";
        public string MaHH { get; set; } = "";
        public string SoLo { get; set; } = "";
        public DateTime NgayNhap { get; set; }
        public DateTime? HanSuDung { get; set; }
        public decimal TonKho { get; set; }
    }
    internal sealed record PhanCap(string MaCTDT, string MaHH, string MaLo, decimal SoLuong);
}
