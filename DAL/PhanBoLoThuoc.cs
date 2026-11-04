using System.Data;
using Dapper;

namespace DAL;

/// <summary>Cùng truy vấn tồn và thuật toán cho bảng tính lẫn xuất kho. Giữ chính sách FIFO hiện có.</summary>
internal static class PhanBoLoThuoc
{
    /// <summary>
    /// Câu lệnh SQL tồn kho SQL dùng trong các thao tác của lớp; giá trị đầu vào được truyền theo cách nơi gọi quy định.
    /// </summary>
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

    /// <summary>
    /// Đọc các lô thuốc phù hợp của kho để phục vụ phân bổ số lượng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>db.Query&lt;Lo&gt;</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="warehouseId">Giá trị kho mã dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="prescriptionIds">Giá trị đơn thuốc các mã dùng khi thực hiện thao tác; kiểu string[].</param>
    /// <returns>Tập dữ liệu kiểu Lo[] theo điều kiện và phạm vi xử lý của hàm.</returns>
    internal static Lo[] LayLo(IDbConnection db, IDbTransaction? tx, string warehouseId, string[] prescriptionIds)
        => db.Query<Lo>(StockSql, new { warehouseId, prescriptionIds }, tx).ToArray();

    /// <summary>
    /// Phân bổ số lượng thuốc yêu cầu vào các lô đủ điều kiện và tạo các phần lô cho bước xử lý tiếp theo.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>lots.ToDictionary</c>, <c>result.Add</c>.</remarks>
    /// <param name="lines">Giá trị các dòng dùng khi thực hiện thao tác; kiểu IEnumerable&lt;Dong&gt;.</param>
    /// <param name="lots">Giá trị các lô dùng khi thực hiện thao tác; kiểu IReadOnlyList&lt;Lo&gt;.</param>
    /// <returns>Tập dữ liệu kiểu IReadOnlyList&lt;PhanCap&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
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

    /// <summary>
    /// Mô hình và thao tác hỗ trợ dòng dùng bởi các thành phần liên quan.
    /// </summary>
    internal sealed class Dong
    {
        /// <summary>
        /// Mã dòng chi tiết đơn thuốc.
        /// </summary>
        public string MaCTDT { get; set; } = "";
        /// <summary>
        /// Mã hàng hóa.
        /// </summary>
        public string MaHH { get; set; } = "";
        /// <summary>
        /// Số lượng của dòng dữ liệu.
        /// </summary>
        public decimal SoLuong { get; set; }
    }
    /// <summary>
    /// Mô hình và thao tác hỗ trợ lô dùng bởi các thành phần liên quan.
    /// </summary>
    internal sealed class Lo
    {
        /// <summary>
        /// Mã lô hàng dùng quản lý hạn dùng và tồn kho.
        /// </summary>
        public string MaLo { get; set; } = "";
        /// <summary>
        /// Mã hàng hóa.
        /// </summary>
        public string MaHH { get; set; } = "";
        /// <summary>
        /// Số lô do nghiệp vụ kho ghi nhận.
        /// </summary>
        public string SoLo { get; set; } = "";
        /// <summary>
        /// Ngày nhập.
        /// </summary>
        public DateTime NgayNhap { get; set; }
        /// <summary>
        /// Hạn sử dụng của lô hàng. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? HanSuDung { get; set; }
        /// <summary>
        /// Tồn kho.
        /// </summary>
        public decimal TonKho { get; set; }
    }
    /// <summary>
    /// Gói dữ liệu phân cấp truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
    /// </summary>
    /// <param name="MaCTDT">Mã chi tiết đơn thuốc xác định đối tượng cần xử lý.</param>
    /// <param name="MaHH">Mã hàng hóa xác định đối tượng cần xử lý.</param>
    /// <param name="MaLo">Mã lô xác định đối tượng cần xử lý.</param>
    /// <param name="SoLuong">Giá trị số lượng dùng khi thực hiện thao tác; kiểu decimal.</param>
    internal sealed record PhanCap(string MaCTDT, string MaHH, string MaLo, decimal SoLuong);
}
