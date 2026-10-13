// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL;

/// <summary>
/// Tầng truy cập SQL cho dòng xuất kho trong bảng CHI_TIET_PHIEU_XUAT. Liên kết hàng hóa, lô và dòng đơn thuốc để truy vết số lượng đã cấp trong phiếu xuất. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
/// </summary>
public sealed class DAL_CHITIETPHIEUXUAT : dbconnect
{
    /// <summary>
    /// Tạo DAL quản lý dòng xuất kho, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
    /// </summary>
    /// <param name="transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
    public DAL_CHITIETPHIEUXUAT(IDbTransaction? transaction = null) : base(transaction) { }
    /// <summary>
    /// Trả danh sách dòng xuất kho dưới dạng mảng bằng luồng layToanBo, phù hợp nơi gọi cần truy cập và đếm kết quả ngay.
    /// </summary>
    /// <returns>Mảng ET; không có bản ghi thì mảng rỗng.</returns>
    public ET_CHITIETPHIEUXUAT[] LayDanhSach() => layToanBo().ToArray();
    /// <summary>
    /// Đọc toàn bộ dòng xuất kho từ dbo.CHI_TIET_PHIEU_XUAT, Dapper ánh xạ từng dòng sang ET; SQL không quy định thứ tự; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
    /// </summary>
    /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
    public IEnumerable<ET_CHITIETPHIEUXUAT> layToanBo() => Doc((db, tx) =>
        db.Query<ET_CHITIETPHIEUXUAT>("SELECT * FROM dbo.CHI_TIET_PHIEU_XUAT", transaction: tx).ToArray());
    /// <summary>
    /// Tìm dòng xuất kho theo khóa trong dbo.CHI_TIET_PHIEU_XUAT; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
    /// </summary>
    /// <param name="ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
    /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
    public ET_CHITIETPHIEUXUAT? layTheoMa(string ma) => Doc((db, tx) =>
        db.QuerySingleOrDefault<ET_CHITIETPHIEUXUAT>("SELECT * FROM dbo.CHI_TIET_PHIEU_XUAT WHERE MaCTPX=@ma", new { ma }, tx));

    /// <summary>
    /// Chuyển hàm đọc cho ThucHien để dùng kết nối và giao dịch chung; khi không có giao dịch ngoài, hạ tầng tự mở giao dịch Serializable.
    /// </summary>
    /// <param name="operation">Hàm nhận kết nối và giao dịch đang dùng, chạy truy vấn/thao tác và trả kết quả cho hạ tầng.</param>
    /// <returns>Kết quả do hàm operation tạo ra; ngoại lệ được chuyển về nơi gọi.</returns>
    private T Doc<T>(Func<IDbConnection, IDbTransaction?, T> operation) => ThucHien(operation);

    /// <summary>
    /// Đọc mọi dòng xuất thuộc mã phiếu, truyền mã qua tham số SQL và sắp theo MaCTPX; vật hóa kết quả thành mảng trước khi kết thúc thao tác.
    /// </summary>
    /// <param name="maPX">Mã phiếu xuất cha để lọc các dòng xuất.</param>
    /// <returns>Danh sách ET dòng xuất đã nạp đủ; rỗng nếu phiếu chưa có chi tiết.</returns>
    public IEnumerable<ET_CHITIETPHIEUXUAT> layTheoPhieu(string maPX) => Doc((db, tx) =>
        db.Query<ET_CHITIETPHIEUXUAT>("SELECT * FROM dbo.CHI_TIET_PHIEU_XUAT WHERE MaPX=@maPX ORDER BY MaCTPX", new { maPX }, tx).ToArray());
    /// <summary>
    /// Cấp MaCTPX trực tiếp vào ET rồi chèn hàng, lô, dòng đơn thuốc và số lượng; dùng đúng kết nối/giao dịch cấp thuốc do nơi gọi cung cấp, không tự commit.
    /// </summary>
    /// <param name="db">Kết nối SQL đang mở của giao dịch do nơi gọi quản lý.</param>
    /// <param name="tx">Giao dịch đang dùng để đọc/ghi và giữ khóa đồng bộ; được truyền tiếp cho các DAL liên quan nếu có.</param>
    /// <param name="detail">ET dòng xuất chứa hàng, lô, dòng đơn và lượng phân bổ; MaCTPX sẽ được gán trước INSERT.</param>
    internal static void ThemChiTiet(IDbConnection db, IDbTransaction? tx, ET_CHITIETPHIEUXUAT detail)
    {
        detail.MaCTPX = TaoMa(db, tx, "CHI_TIET_PHIEU_XUAT", "MaCTPX");
        db.Execute("INSERT dbo.CHI_TIET_PHIEU_XUAT(MaCTPX,MaPX,MaHH,MaLo,MaCTDT,SoLuong,GhiChu) VALUES(@MaCTPX,@MaPX,@MaHH,@MaLo,@MaCTDT,@SoLuong,@GhiChu)", detail, tx);
    }
}
