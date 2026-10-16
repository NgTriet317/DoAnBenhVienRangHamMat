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
    /// <summary>
    /// Trả tồn khả dụng theo kho và thuốc trong đơn: tổng nhập Đã ghi sổ trừ tổng xuất Đã ghi sổ; chỉ lấy lô Còn sử dụng, đã đến ngày nhập, chưa hết hạn và còn lượng dương.
    /// </summary>
    /// <param name = "warehouseId">Mã kho lấy thuốc và tính tồn; khi cấp đơn kho phải Đang hoạt động.</param>
    /// <param name = "prescriptionId">Mã đơn thuốc để lấy dòng thuốc, kiểm tra trạng thái và truy vết việc cấp đủ đơn.</param>
    /// <returns>DataTable đã nạp đủ các dòng kết quả; có thể rỗng nếu chưa có dữ liệu phù hợp.</returns>
    public DataTable GetStock(string warehouseId, string prescriptionId)
    {
        return Query(StockSql, new { warehouseId, prescriptionId });
    }

    /// <summary>
    /// Kiểm tra nhân viên và kho đang hoạt động; khóa lần khám, hàng hóa rồi đầu đơn, yêu cầu đơn Đã xác nhận và chưa cấp. Phân bổ đủ lượng theo thứ tự lô, báo lỗi trước khi ghi nếu thiếu; tạo phiếu Nháp, chi tiết rồi ghi sổ cùng giao dịch, trả mã phiếu xuất.
    /// </summary>
    /// <param name = "prescriptionId">Mã đơn thuốc để lấy dòng thuốc, kiểm tra trạng thái và truy vết việc cấp đủ đơn.</param>
    /// <param name = "warehouseId">Mã kho lấy thuốc và tính tồn; khi cấp đơn kho phải Đang hoạt động.</param>
    /// <param name = "employeeId">Mã nhân viên thực hiện; phải là nhân viên Đang làm việc.</param>
    /// <returns>Mã phiếu xuất đã ghi sổ sau khi phân bổ đủ đơn.</returns>
    public string DispensePrescription(string prescriptionId, string warehouseId, string employeeId)
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

            if (prescription.NgayKe > DateTime.Now)
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

            var lines = db.Query<PrescriptionLine>("SELECT c.MaCTDT,c.SoLuong,t.MaHH FROM dbo.CHI_TIET_DON_THUOC c JOIN dbo.THUOC t ON t.MaThuoc=c.MaThuoc WHERE c.MaDT=@prescriptionId ORDER BY c.MaCTDT", new { prescriptionId }, tx).ToArray();
            if (lines.Length == 0)
            {
                throw new InvalidOperationException("Đơn thuốc không có thuốc để cấp.");
            }

            var lots = db.Query<Lot>(StockSql, new { warehouseId, prescriptionId }, tx).ToArray();
            // Chỉ lập phương án cấp trong bộ nhớ: nếu thiếu bất kỳ thuốc nào thì dừng trước khi INSERT phiếu/chi tiết.
            var phuongAnCap = LapPhuongAnCapThuoc(lines, lots);

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

    /// <summary>
    /// Tính lô và lượng cần lấy cho từng dòng thuốc bằng vòng lặp.
    /// Hàm chỉ lập phương án trong bộ nhớ; nếu thiếu thuốc thì báo lỗi trước khi ghi phiếu xuất.
    /// Thứ tự lô đã được SQL sắp xếp để lấy lô nhập trước, còn hạn sử dụng.
    /// </summary>
    private static List<PhanBoThuoc> LapPhuongAnCapThuoc(PrescriptionLine[] cacThuoc, Lot[] cacLo)
    {
        var phuongAn = new List<PhanBoThuoc>();

        foreach (var thuoc in cacThuoc)
        {
            decimal conThieu = thuoc.SoLuong;

            foreach (var lo in cacLo)
            {
                if (lo.MaHH != thuoc.MaHH || lo.TonKho <= 0)
                {
                    continue;
                }

                decimal soLuongLay = Math.Min(conThieu, lo.TonKho);
                phuongAn.Add(new PhanBoThuoc
                {
                    MaCTDT = thuoc.MaCTDT,
                    MaHH = thuoc.MaHH,
                    MaLo = lo.MaLo,
                    SoLuong = soLuongLay
                });

                // Giảm tồn tạm để hai dòng cùng thuốc không dùng lại cùng một lượng tồn.
                lo.TonKho -= soLuongLay;
                conThieu -= soLuongLay;
                if (conThieu == 0)
                {
                    break;
                }
            }

            if (conThieu > 0)
            {
                throw new InvalidOperationException($"Kho không đủ thuốc mã hàng {thuoc.MaHH} (thiếu {conThieu:N3}). Chưa cấp bất kỳ thuốc nào; chọn kho đủ toàn bộ đơn hoặc bổ sung tồn hợp lệ.");
            }
        }

        return phuongAn;
    }

    /// <summary>Một phần thuốc lấy từ một lô; thay tuple bằng lớp có tên trường giống ET để dễ đọc.</summary>
    private sealed class PhanBoThuoc
    {
        /// <summary>Dòng đơn thuốc nhận phần cấp phát này.</summary>
        public string MaCTDT { get; set; } = "";
        /// <summary>Mã hàng hóa của thuốc cần lấy.</summary>
        public string MaHH { get; set; } = "";
        /// <summary>Lô được chọn theo thứ tự nhập trước.</summary>
        public string MaLo { get; set; } = "";
        /// <summary>Lượng lấy từ lô để cấp cho dòng thuốc.</summary>
        public decimal SoLuong { get; set; }
    }

    /// <summary>
    /// Kết quả truy vấn đầu đơn, giữ lần khám, trạng thái và ngày kê để kiểm tra trước khi cấp thuốc.
    /// </summary>
    private sealed class Prescription
    {
        /// <summary>
        /// Mã lần khám gắn với đầu đơn thuốc trong kết quả truy vấn.
        /// </summary>
        public string MaLS { get; set; }
        /// <summary>
        /// Trạng thái SQL của bản được đọc, dùng kiểm tra đã xác nhận trước khi lập hóa đơn hoặc cấp thuốc.
        /// </summary>
        public string TrangThai { get; set; } = "";
        /// <summary>
        /// Ngày kê đơn để chặn cấp thuốc trước thời điểm kê.
        /// </summary>
        public DateTime NgayKe { get; set; }
    }

    /// <summary>
    /// Dòng đơn thuốc sau khi ghép danh mục thuốc: mã dòng, mã hàng và số lượng cần phân bổ từ các lô.
    /// </summary>
    private sealed class PrescriptionLine
    {
        /// <summary>
        /// Mã dòng đơn thuốc để liên kết chi tiết phiếu xuất với lượng đã kê.
        /// </summary>
        public string MaCTDT { get; set; }
        /// <summary>
        /// Mã hàng hóa dùng ghép dòng thuốc với các lô tồn cùng mặt hàng.
        /// </summary>
        public string MaHH { get; set; }
        /// <summary>
        /// Số lượng của dòng đơn cần cấp đầy đủ từ một hoặc nhiều lô.
        /// </summary>
        public decimal SoLuong { get; set; }
    }

    /// <summary>
    /// Kết quả tồn khả dụng của lô; TonKho được giảm trong bộ nhớ sau mỗi phân bổ để các dòng đơn không dùng trùng lượng tồn.
    /// </summary>
    private sealed class Lot
    {
        /// <summary>
        /// Mã lô hàng được chọn làm nguồn cấp thuốc.
        /// </summary>
        public string MaLo { get; set; }
        /// <summary>
        /// Mã hàng hóa dùng ghép dòng thuốc với các lô tồn cùng mặt hàng.
        /// </summary>
        public string MaHH { get; set; }
        /// <summary>
        /// Lượng tồn khả dụng của lô; giảm tạm trong bộ nhớ khi phân bổ cho từng dòng đơn.
        /// </summary>
        public decimal TonKho { get; set; }
    }
}
