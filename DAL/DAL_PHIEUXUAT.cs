// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL;
/// <summary>
/// Tầng truy cập SQL cho phiếu xuất kho trong bảng PHIEU_XUAT. Cấp đủ một đơn đã xác nhận từ các lô còn hạn và có tồn, khóa dữ liệu liên quan rồi ghi phiếu xuất cùng chi tiết trong một giao dịch. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
/// </summary>
public sealed class DAL_PHIEUXUAT : dbconnect
{
    /// <summary>
    /// Tên gọi tiếng Việt của DispensePrescription: cấp đủ đơn từ một kho hợp lệ và trả mã phiếu xuất đã ghi sổ.
    /// </summary>
    /// <param name = "maDT">Mã đơn thuốc để lấy dòng thuốc, kiểm tra trạng thái và truy vết việc cấp đủ đơn.</param>
    /// <param name = "maKho">Mã kho lấy thuốc và tính tồn; khi cấp đơn kho phải Đang hoạt động.</param>
    /// <param name = "maNV">Mã nhân viên thực hiện; phải là nhân viên Đang làm việc.</param>
    /// <returns>Mã phiếu xuất đã ghi sổ sau khi phân bổ đủ đơn.</returns>
    public string CapDonThuoc(string maDT, string maKho, string maNV)
    {
        return DispensePrescription(maDT, maKho, maNV);
    }

    /// <summary>
    /// Tạo DAL quản lý phiếu xuất kho, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
    /// </summary>
    /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
    public DAL_PHIEUXUAT(IDbTransaction? transaction = null) : base(transaction)
    {
    }

    /// <summary>
    /// Trả danh sách phiếu xuất kho dưới dạng mảng bằng luồng layToanBo, phù hợp nơi gọi cần truy cập và đếm kết quả ngay.
    /// </summary>
    /// <returns>Mảng ET; không có bản ghi thì mảng rỗng.</returns>
    public ET_PHIEUXUAT[] LayDanhSach()
    {
        return layToanBo().ToArray();
    }

    /// <summary>
    /// Đọc toàn bộ phiếu xuất kho từ dbo.PHIEU_XUAT, Dapper ánh xạ từng dòng sang ET; SQL không quy định thứ tự; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
    /// </summary>
    /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
    public IEnumerable<ET_PHIEUXUAT> layToanBo()
    {
        return ThucHien((db, tx) =>
        {
            return db.Query<ET_PHIEUXUAT>("SELECT * FROM dbo.PHIEU_XUAT", transaction: tx).ToArray();
        });
    }

    /// <summary>
    /// Tìm phiếu xuất kho theo khóa trong dbo.PHIEU_XUAT; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
    /// </summary>
    /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
    /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
    public ET_PHIEUXUAT? layTheoMa(string ma)
    {
        return ThucHien((db, tx) =>
        {
            return db.QuerySingleOrDefault<ET_PHIEUXUAT>("SELECT * FROM dbo.PHIEU_XUAT WHERE MaPX=@ma", new { ma }, tx);
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
    /// SQL tính tồn theo từng lô từ chứng từ nhập/xuất đã ghi sổ, lọc đúng kho và hàng trong đơn, loại lô chưa nhập/hết hạn/ngừng sử dụng/hết tồn. Ưu tiên NgayNhap trước, rồi lô có hạn dùng và HanSuDung sớm, cuối cùng MaLo; không phải chỉ sắp theo hạn dùng.
    /// </summary>
    private const string StockSql = PhanBoLoThuoc.StockSql;
    /// <summary>
    /// Trả tồn khả dụng theo kho và thuốc trong đơn: tổng nhập Đã ghi sổ trừ tổng xuất Đã ghi sổ; chỉ lấy lô Còn sử dụng, đã đến ngày nhập, chưa hết hạn và còn lượng dương.
    /// </summary>
    /// <param name = "warehouseId">Mã kho lấy thuốc và tính tồn; khi cấp đơn kho phải Đang hoạt động.</param>
    /// <param name = "prescriptionId">Mã đơn thuốc để lấy dòng thuốc, kiểm tra trạng thái và truy vết việc cấp đủ đơn.</param>
    /// <returns>DataTable đã nạp đủ các dòng kết quả; có thể rỗng nếu chưa có dữ liệu phù hợp.</returns>
    public DataTable GetStock(string warehouseId, string prescriptionId)
    {
        return Query(StockSql, new { warehouseId, prescriptionIds = new[] { prescriptionId } });
    }

    /// <summary>
    /// Kiểm tra nhân viên và kho đang hoạt động; khóa lần khám, hàng hóa rồi đầu đơn, yêu cầu đơn Đã xác nhận và chưa cấp. Phân bổ đủ lượng theo thứ tự lô, báo lỗi trước khi ghi nếu thiếu; tạo phiếu Nháp, chi tiết rồi ghi sổ cùng giao dịch, trả mã phiếu xuất.
    /// </summary>
    /// <param name = "prescriptionId">Mã đơn thuốc để lấy dòng thuốc, kiểm tra trạng thái và truy vết việc cấp đủ đơn.</param>
    /// <param name = "warehouseId">Mã kho lấy thuốc và tính tồn; khi cấp đơn kho phải Đang hoạt động.</param>
    /// <param name = "employeeId">Mã nhân viên thực hiện; phải là nhân viên Đang làm việc.</param>
    /// <returns>Mã phiếu xuất đã ghi sổ sau khi phân bổ đủ đơn.</returns>
    public string DispensePrescription(string prescriptionId, string warehouseId, string employeeId)
        => DispenseCore(prescriptionId, warehouseId, employeeId, false);

    internal string DispenseCoordinatedPrescription(string prescriptionId, string warehouseId, string employeeId)
        => DispenseCore(prescriptionId, warehouseId, employeeId, true);

    private string DispenseCore(string prescriptionId, string warehouseId, string employeeId, bool coordinated)
    {
        return ThucHien((db, tx) =>
        {
            KiemTraNhanVien(db, tx, employeeId);
            if (!db.ExecuteScalar<bool>("SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.KHO WHERE MaKho=@warehouseId AND TrangThai=N'Đang hoạt động') THEN 1 ELSE 0 END AS bit)", new { warehouseId }, tx))
            {
                throw new InvalidOperationException("Chọn kho đang hoạt động.");
            }

            var visitId = db.ExecuteScalar<string?>("SELECT MaLS FROM dbo.DON_THUOC WHERE MaDT=@prescriptionId", new { prescriptionId }, tx);
            if (visitId is null)
            {
                throw new InvalidOperationException("Không tìm thấy đơn thuốc.");
            }

            db.ExecuteScalar<string>("SELECT MaLS FROM dbo.LICH_SU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@visitId", new { visitId }, tx);
            DAL_HOADON.KiemTraLuotLegacy(db, tx, visitId);
            // Khóa hàng hóa trước đầu đơn theo cùng thứ tự mà trigger tồn kho sử dụng để hạn chế tranh chấp khóa.
            db.Query<string>("""
            SELECT h.MaHH FROM dbo.HANG_HOA h WITH(UPDLOCK,HOLDLOCK)
            WHERE h.MaHH IN(SELECT t.MaHH FROM dbo.CHI_TIET_DON_THUOC c JOIN dbo.THUOC t ON t.MaThuoc=c.MaThuoc WHERE c.MaDT=@prescriptionId)
            ORDER BY h.MaHH
            """, new { prescriptionId }, tx).ToArray();
            var prescription = db.QuerySingle<Prescription>("SELECT MaLS,TrangThai,NgayKe FROM dbo.DON_THUOC WITH(UPDLOCK,HOLDLOCK) WHERE MaDT=@prescriptionId", new { prescriptionId }, tx);
            if (prescription.TrangThai != "Đã xác nhận")
            {
                throw new InvalidOperationException("Chỉ cấp đơn thuốc đã xác nhận và còn áp dụng.");
            }

            var now = coordinated ? db.ExecuteScalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())", transaction: tx) : DateTime.Now;
            if (prescription.NgayKe > now)
            {
                throw new InvalidOperationException("Ngày kê đơn chưa đến.");
            }

            if (db.ExecuteScalar<int>("""
            SELECT COUNT(*) FROM dbo.CHI_TIET_PHIEU_XUAT x JOIN dbo.PHIEU_XUAT p ON p.MaPX=x.MaPX
            JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaCTDT=x.MaCTDT WHERE c.MaDT=@prescriptionId AND p.TrangThai=N'Đã ghi sổ'
            """, new { prescriptionId }, tx) > 0)
            {
                throw new InvalidOperationException("Đơn thuốc đã được cấp; không thể cấp lại.");
            }

            var lines = db.Query<PhanBoLoThuoc.Dong>("SELECT c.MaCTDT,c.SoLuong,t.MaHH FROM dbo.CHI_TIET_DON_THUOC c JOIN dbo.THUOC t ON t.MaThuoc=c.MaThuoc WHERE c.MaDT=@prescriptionId ORDER BY c.MaCTDT", new { prescriptionId }, tx).ToArray();
            if (lines.Length == 0)
            {
                throw new InvalidOperationException("Đơn thuốc không có thuốc để cấp.");
            }

            var lots = PhanBoLoThuoc.LayLo(db, tx, warehouseId, new[] { prescriptionId });
            // Chỉ lập phương án cấp trong bộ nhớ: nếu thiếu bất kỳ thuốc nào thì dừng trước khi INSERT phiếu/chi tiết.
            var phuongAnCap = PhanBoLoThuoc.Lap(lines, lots);

            // Khi đã đủ toàn bộ đơn mới ghi đầu phiếu, từng dòng phân bổ và chuyển sang ghi sổ trong cùng giao dịch.
            var exportId = TaoMa(db, tx, "PHIEU_XUAT", "MaPX");
            db.Execute("""
            INSERT dbo.PHIEU_XUAT(MaPX,MaKho,MaNV,MaLS,NgayXuat,LyDo,TrangThai,GhiChu)
            VALUES(@exportId,@warehouseId,@employeeId,@visitId,SYSDATETIME(),N'Cấp đủ đơn thuốc',N'Nháp',@note)
            """, new { exportId, warehouseId, employeeId, visitId, note = $"Đơn thuốc {prescriptionId}" }, tx);
            foreach (var phanBo in phuongAnCap)
            {
                DAL_CHITIETPHIEUXUAT.ThemChiTiet(db, tx, new ET_CHITIETPHIEUXUAT
                {
                    MaPX = exportId,
                    MaHH = phanBo.MaHH,
                    MaLo = phanBo.MaLo,
                    MaCTDT = phanBo.MaCTDT,
                    SoLuong = phanBo.SoLuong,
                    GhiChu = "Cấp theo đơn thuốc"
                });
            }

            db.Execute("UPDATE dbo.PHIEU_XUAT SET TrangThai=N'Đã ghi sổ' WHERE MaPX=@exportId", new { exportId }, tx);
            return exportId;
        });
    }

    private sealed class Prescription
    {
        public string MaLS { get; set; } = "";
        public string TrangThai { get; set; } = "";
        public DateTime NgayKe { get; set; }
    }
}
