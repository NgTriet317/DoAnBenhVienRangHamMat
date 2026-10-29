// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL;
/// <summary>
/// Tầng truy cập SQL cho hóa đơn trong bảng HOA_DON. Tập hợp khoản thu mới của lần khám đã xác nhận, lập hóa đơn nháp có giảm giá rồi xác nhận hoặc hủy nháp trong giao dịch. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
/// </summary>
public sealed class DAL_HOADON : dbconnect
{
    private readonly IDbTransaction? callerTransaction;
    /// <summary>
    /// Tên gọi tiếng Việt của CreateInvoice: lập hóa đơn Nháp cho khoản thu mới của lần khám đã xác nhận và trả mã hóa đơn.
    /// </summary>
    /// <param name = "maLS">Mã lần khám liên kết; dùng để xác định bệnh nhân hoặc nguồn thu hợp lệ.</param>
    /// <param name = "maNV">Mã nhân viên thực hiện; phải là nhân viên Đang làm việc.</param>
    /// <param name = "giamGia">Số tiền giảm giá, từ 0 đến tổng các khoản thu được đưa vào hóa đơn.</param>
    /// <returns>Mã hóa đơn Nháp vừa lập; hóa đơn được xác nhận bằng thao tác riêng.</returns>
    public string LapHoaDon(string maLS, string maNV, decimal giamGia = 0)
    {
        return CreateInvoice(maLS, maNV, giamGia);
    }

    /// <summary>
    /// Chuyển yêu cầu xác nhận hóa đơn sang ConfirmInvoice; chỉ hóa đơn Nháp được xác nhận và ghi ngày xác nhận.
    /// </summary>
    /// <param name = "maHD">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    public void XacNhanHoaDon(string maHD)
    {
        ConfirmInvoice(maHD);
    }

    /// <summary>
    /// Chuyển yêu cầu hủy hóa đơn ở trạng thái Nháp sang CancelInvoice; các dòng chi tiết được gỡ để có thể lập hóa đơn khác từ nguồn thu đó.
    /// </summary>
    /// <param name = "maHD">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    public void HuyHoaDonNhap(string maHD)
    {
        CancelInvoice(maHD);
    }

    /// <summary>
    /// Tạo DAL quản lý hóa đơn, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
    /// </summary>
    /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
    public DAL_HOADON(IDbTransaction? transaction = null) : base(transaction)
    {
        callerTransaction = transaction;
    }

    /// <summary>
    /// Trả danh sách hóa đơn dưới dạng mảng bằng luồng layToanBo, phù hợp nơi gọi cần truy cập và đếm kết quả ngay.
    /// </summary>
    /// <returns>Mảng ET; không có bản ghi thì mảng rỗng.</returns>
    public ET_HOADON[] LayDanhSach()
    {
        return layToanBo().ToArray();
    }

    /// <summary>
    /// Đọc toàn bộ hóa đơn từ dbo.HOA_DON, Dapper ánh xạ từng dòng sang ET; SQL không quy định thứ tự; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
    /// </summary>
    /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
    public IEnumerable<ET_HOADON> layToanBo()
    {
        return ThucHien((db, tx) =>
        {
            return db.Query<ET_HOADON>("SELECT * FROM dbo.HOA_DON", transaction: tx).ToArray();
        });
    }

    /// <summary>
    /// Tìm hóa đơn theo khóa trong dbo.HOA_DON; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
    /// </summary>
    /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
    /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
    public ET_HOADON? layTheoMa(string ma)
    {
        return ThucHien((db, tx) =>
        {
            return db.QuerySingleOrDefault<ET_HOADON>("SELECT * FROM dbo.HOA_DON WHERE MaHD=@ma", new { ma }, tx);
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
    /// Đọc hóa đơn cùng bệnh nhân và người lập; cộng tiền Thu, trừ tiền Hoàn đã xác nhận để tính DaThu và ConPhaiThu. Có thể lọc một lần khám; sắp ngày và mã mới nhất trước.
    /// </summary>
    /// <param name = "visitId">Mã lần khám để lọc hóa đơn; null để lấy tất cả.</param>
    /// <returns>DataTable đã nạp đủ các dòng kết quả; có thể rỗng nếu chưa có dữ liệu phù hợp.</returns>
    public DataTable GetInvoices(string? visitId = null)
    {
        return Query("""
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
    }

    /// <summary>
    /// Khóa lần khám bằng UPDLOCK/HOLDLOCK, yêu cầu đã xác nhận và thời điểm đã đến; lấy nguồn thu chưa tính tiền, kiểm tra giảm giá rồi cấp mã, ghi đầu hóa đơn Nháp, chi tiết và giảm giá trong cùng giao dịch.
    /// </summary>
    /// <param name = "visitId">Mã lần khám liên kết; dùng để xác định bệnh nhân hoặc nguồn thu hợp lệ.</param>
    /// <param name = "employeeId">Mã nhân viên thực hiện; phải là nhân viên Đang làm việc.</param>
    /// <param name = "discount">Số tiền giảm giá, từ 0 đến tổng các khoản thu được đưa vào hóa đơn.</param>
    /// <returns>Mã hóa đơn Nháp vừa lập; hóa đơn được xác nhận bằng thao tác riêng.</returns>
    public string CreateInvoice(string visitId, string employeeId, decimal discount)
    {
        return ThucHien((db, tx) =>
        {
            KiemTraNhanVien(db, tx, employeeId);
            var visit = db.QuerySingleOrDefault<Visit>("SELECT MaLS,TrangThai,ThoiGianKham FROM dbo.LICH_SU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@visitId", new { visitId }, tx);
            if (visit is null)
            {
                throw new InvalidOperationException("Không tìm thấy lần khám.");
            }
            KiemTraLuotLegacy(db, tx, visitId);

            if (visit.TrangThai != "Đã xác nhận")
            {
                throw new InvalidOperationException("Xác nhận lần khám trước khi lập hóa đơn.");
            }

            if (visit.ThoiGianKham > DateTime.Now)
            {
                throw new InvalidOperationException("Thời điểm lần khám chưa đến; chưa thể lập hóa đơn.");
            }

            // Loại nguồn thu đã gắn chi tiết hóa đơn; khóa lần khám giữ ổn định phạm vi lập hóa đơn đồng thời.
            var charges = db.Query<ET_CHITIETHOADON>(DAL_CHITIETHOADON.EligibleChargesSql, new { visitId }, tx).ToArray();
            if (charges.Length == 0)
            {
                throw new InvalidOperationException("Lần khám chưa có khoản thu mới: cần dịch vụ đã xác nhận hoặc thuốc đã cấp đủ, chưa nằm trên hóa đơn khác.");
            }

            if (discount < 0 || discount > charges.Sum(c => c.ThanhTien))
            {
                throw new InvalidOperationException("Giảm giá phải từ 0 đến tổng khoản thu.");
            }

            var invoiceId = TaoMa(db, tx, "HOA_DON", "MaHD");
            db.Execute("""
            INSERT dbo.HOA_DON(MaHD,MaLS,MaNV,NgayLap,TongTien,GiamGia,TrangThai)
            VALUES(@invoiceId,@visitId,@employeeId,SYSDATETIME(),0,0,N'Nháp')
            """, new { invoiceId, visitId, employeeId }, tx);
            // Đầu hóa đơn bắt đầu với giảm giá 0; ghi đủ chi tiết trước rồi mới áp dụng giảm giá đã kiểm tra.
            foreach (var charge in charges)
            {
                DAL_CHITIETHOADON.ThemChiTiet(db, tx, invoiceId, charge);
            }

            db.Execute("UPDATE dbo.HOA_DON SET GiamGia=@discount WHERE MaHD=@invoiceId", new { invoiceId, discount }, tx);
            return invoiceId;
        });
    }

    /// <summary>
    /// Đọc mã, trạng thái và số tiền phải thu với UPDLOCK/HOLDLOCK, giữ khóa theo giao dịch để xác nhận, hủy hoặc thanh toán không thao tác đồng thời trên cùng hóa đơn; báo lỗi nếu không tìm thấy.
    /// </summary>
    /// <param name = "db">Kết nối SQL đang mở của giao dịch do nơi gọi quản lý.</param>
    /// <param name = "tx">Giao dịch đang dùng để đọc/ghi và giữ khóa đồng bộ; được truyền tiếp cho các DAL liên quan nếu có.</param>
    /// <param name = "invoiceId">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    /// <returns>ET rút gọn chứa MaHD, TrangThai và SoTienPhaiThu; không có hóa đơn thì ném lỗi.</returns>
    internal static ET_HOADON LockInvoice(IDbConnection db, IDbTransaction? tx, string invoiceId)
    {
        return db.QuerySingleOrDefault<ET_HOADON>("SELECT MaHD,MaLS,TrangThai,SoTienPhaiThu,PhienBanQuyTrinh,TaoKhiHoanTat FROM dbo.HOA_DON WITH(UPDLOCK,HOLDLOCK) WHERE MaHD=@invoiceId", new { invoiceId }, tx) ?? throw new InvalidOperationException("Không tìm thấy hóa đơn.");
    }

    /// <summary>
    /// Khóa hóa đơn, yêu cầu trạng thái Nháp rồi đổi sang Đã xác nhận và đặt NgayXacNhan theo thời gian SQL trong cùng giao dịch.
    /// </summary>
    /// <param name = "invoiceId">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    public void ConfirmInvoice(string invoiceId)
        => ConfirmInvoiceCore(invoiceId, false);

    internal void ConfirmCoordinatedInvoice(string invoiceId) => ConfirmInvoiceCore(invoiceId, true);
    private void ConfirmInvoiceCore(string invoiceId, bool coordinated)
    {
        ThucHien((db, tx) =>
        {
            var invoice = LockInvoice(db, tx, invoiceId);
            if (!coordinated) KiemTraHoaDonLegacy(invoice);
            else if (invoice.PhienBanQuyTrinh != 2) throw new InvalidOperationException("Cần hóa đơn quy trình 2.");
            if (invoice.TrangThai != "Nháp")
            {
                throw new InvalidOperationException("Chỉ xác nhận hóa đơn nháp.");
            }

            db.Execute("UPDATE dbo.HOA_DON SET TrangThai=N'Đã xác nhận',NgayXacNhan=SYSDATETIME() WHERE MaHD=@invoiceId", new { invoiceId }, tx);
            return true;
        });
    }

    /// <summary>
    /// Khóa và yêu cầu hóa đơn còn Nháp; đặt giảm giá về 0, xóa các dòng chi tiết để giải phóng nguồn thu rồi đánh dấu Hủy và xóa ngày xác nhận trong cùng giao dịch.
    /// </summary>
    /// <param name = "invoiceId">Mã hóa đơn cần đọc, khóa, xác nhận, hủy hoặc ghi nhận thanh toán.</param>
    public void CancelInvoice(string invoiceId)
    {
        ThucHien((db, tx) =>
        {
            var invoice = LockInvoice(db, tx, invoiceId);
            KiemTraHoaDonLegacy(invoice);
            if (invoice.TrangThai != "Nháp")
            {
                throw new InvalidOperationException("Chỉ hủy hóa đơn nháp. Hóa đơn xác nhận được giữ nguyên.");
            }

            db.Execute("UPDATE dbo.HOA_DON SET GiamGia=0 WHERE MaHD=@invoiceId", new { invoiceId }, tx);
            // Gỡ chi tiết nháp để truy vấn NOT EXISTS có thể đưa các nguồn này vào hóa đơn khác về sau.
            DAL_CHITIETHOADON.GoChiTietNhap(db, tx, invoiceId);
            db.Execute("UPDATE dbo.HOA_DON SET TrangThai=N'Hủy',NgayXacNhan=NULL WHERE MaHD=@invoiceId", new { invoiceId }, tx);
            return true;
        });
    }

    /// <summary>Chốt toàn bộ khoản thu trong chính giao dịch hoàn tất khám; chưa xuất kho hay thu tiền.</summary>
    internal string TaoTuKhamHoanTat(string maLS, string maNV)
    {
        if (callerTransaction is null)
            throw new InvalidOperationException("Lập hóa đơn hoàn tất cần giao dịch của đầu mối khám.");
        return ThucHien((db, tx) =>
        {
            KiemTraNhanVien(db, tx, maNV);
            var visit = db.QuerySingleOrDefault<Visit>("SELECT TrangThai,ThoiGianKham FROM dbo.LICH_SU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@maLS", new { maLS }, tx);
            if (visit is null || visit.TrangThai != "Đã xác nhận" || visit.ThoiGianKham > db.ExecuteScalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())", transaction: tx))
                throw new LoiKhamException("MaLS", "Hoàn tất lần khám trước khi chốt hóa đơn.");
            if (db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.HOA_DON WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@maLS", new { maLS }, tx) != 0 ||
                db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.PHIEU_XUAT p WITH(HOLDLOCK) WHERE p.MaLS=@maLS OR EXISTS(SELECT 1 FROM dbo.CHI_TIET_PHIEU_XUAT x JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaCTDT=x.MaCTDT JOIN dbo.DON_THUOC d ON d.MaDT=c.MaDT WHERE x.MaPX=p.MaPX AND d.MaLS=@maLS)", new { maLS }, tx) != 0)
                throw new LoiKhamException("MaLS", "Lượt đã có chứng từ tài chính/kho; cần đối chiếu trước hoàn tất.");
            if (db.ExecuteScalar<int>("SELECT (SELECT COUNT(*) FROM dbo.DON_THUOC WHERE MaLS=@maLS AND TrangThai=N'Nháp')+(SELECT COUNT(*) FROM dbo.DICH_VU_THUC_HIEN WHERE MaLS=@maLS AND TrangThai=N'Nháp')", new { maLS }, tx) != 0)
                throw new LoiKhamException("MaLS", "Xử lý hết đơn thuốc và dịch vụ nháp trước hoàn tất.");
            var charges = db.Query<ET_CHITIETHOADON>(DAL_CHITIETHOADON.CompletionChargesSql, new { visitId = maLS }, tx).ToArray();
            if (db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DON_THUOC d JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaDT=d.MaDT JOIN dbo.THUOC t ON t.MaThuoc=c.MaThuoc WHERE d.MaLS=@maLS AND d.TrangThai=N'Đã xác nhận' AND t.DonGiaBan<=0", new { maLS }, tx)>0)
                throw new LoiKhamException("DonGiaBan", "Có thuốc chưa thiết lập giá. Cập nhật giá thuốc trước khi hoàn tất và lập hóa đơn.");
            if (charges.Length == 0)
                throw new LoiKhamException("MaLS", "Lần khám cần có dịch vụ hoặc thuốc đã ký để lập hóa đơn.");
            if (charges.Any(c => c.ThanhTien != decimal.Truncate(c.ThanhTien)))
                throw new LoiKhamException("MaLS", "Khoản thu có số lẻ đồng; điều chỉnh giá trước hoàn tất và chốt hóa đơn.");
            var invoiceId = TaoMa(db, tx, "HOA_DON", "MaHD");
            db.Execute("INSERT dbo.HOA_DON(MaHD,MaLS,MaNV,NgayLap,TongTien,GiamGia,TrangThai) VALUES(@invoiceId,@maLS,@maNV,SYSDATETIME(),0,0,N'Nháp')", new { invoiceId, maLS, maNV }, tx);
            foreach (var charge in charges) DAL_CHITIETHOADON.ThemChiTiet(db, tx, invoiceId, charge);
            db.Execute("UPDATE dbo.HOA_DON SET TaoKhiHoanTat=1 WHERE MaHD=@invoiceId", new { invoiceId }, tx);
            return invoiceId;
        });
    }

    internal static void KiemTraHoaDonLegacy(ET_HOADON invoice)
    {
        if (invoice.PhienBanQuyTrinh == 2 || invoice.TaoKhiHoanTat)
            throw new LoiKhamException("MaHD", "Hóa đơn quy trình mới chỉ được ghi trong đầu mối thu trọn gói; chứng từ đã thu chỉ được xem/in lại.");
    }
    internal static void KiemTraLuotLegacy(IDbConnection db, IDbTransaction? tx, string visitId)
    {
        if (db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.HOA_DON WITH(HOLDLOCK) WHERE MaLS=@visitId AND (PhienBanQuyTrinh=2 OR TaoKhiHoanTat=1)", new { visitId }, tx) > 0)
            throw new LoiKhamException("MaLS", "Lượt đã chốt hóa đơn; chỉ thu/xuất thuốc qua đầu mối thanh toán, không lập thêm chứng từ.");
    }

    /// <summary>
    /// Kết quả truy vấn rút gọn của lần khám, chỉ giữ trạng thái và thời điểm để kiểm tra đủ điều kiện lập hóa đơn.
    /// </summary>
    private sealed class Visit
    {
        /// <summary>
        /// Trạng thái SQL của bản được đọc, dùng kiểm tra đã xác nhận trước khi lập hóa đơn hoặc cấp thuốc.
        /// </summary>
        public string TrangThai { get; set; } = "";
        /// <summary>
        /// Thời điểm lần khám để chặn lập hóa đơn cho lần khám chưa đến thời gian.
        /// </summary>
        public DateTime ThoiGianKham { get; set; }
    }
}
