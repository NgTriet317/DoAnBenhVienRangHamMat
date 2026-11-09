using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Dapper;
using ET;

namespace DAL;

/// <summary>
/// Tầng truy cập dữ liệu thanh toán trọn gói, thực hiện truy vấn và kiểm tra trong phạm vi kết nối/giao dịch của dbconnect.
/// </summary>
public sealed partial class DAL_THANHTOANTRONGOI
{
    // Chỉ BUS đầu mối sở hữu commit. Các DAL con không mở kết nối hay commit riêng.
    public KetQuaThanhToan ThanhToan(YeuCauThanhToan input, NguCanhNghiepVu user)
    {
        if (paymentTransaction is null || paymentTransaction.IsolationLevel != IsolationLevel.Serializable)
            throw Error("ThanhToan", "Cần giao dịch Serializable của đầu mối thanh toán.");
        return ThucHien((db, tx) =>
        {
            var actor = DAL_QUYENNGHIEPVU.XacThuc(db, tx, user);
            if (!actor.CoQuyen(QuyenLamViec.ThuTien)) throw DAL_QUYENNGHIEPVU.TuChoi();
            var r = Normalize(input);
            var requestHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {
                r.MaLS, r.MaKhoCapThuoc, r.DauVanTayBangTinh, r.PhuongThuc,
                SoTienKhachDua = r.SoTienKhachDua.ToString("0.00", CultureInfo.InvariantCulture), r.MaThamChieu })));
            PaymentLock(db, tx, "RHM:ThanhToan:YeuCau:" + r.MaYeuCau.ToString("D"));
            // Các truy vấn legacy và cấp mã dùng vùng khóa rộng. Đồng bộ writer trước khi đọc
            // phạm vi hóa đơn/tồn để hai lượt không giữ S range rồi chờ nâng khóa lẫn nhau.
            PaymentLock(db, tx, "RHM:ThanhToan:Write");
            PaymentLock(db, tx, "RHM:Kham:LanKham:" + r.MaLS);
            // U khóa lần khám cũng đồng bộ với API tài chính/kho legacy, ngoài application lock mới.
            if (db.ExecuteScalar<string?>("SELECT MaLS FROM dbo.LICH_SU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@MaLS", r, tx) is null)
                throw DAL_QUYENNGHIEPVU.TuChoi();
            var existing = db.QuerySingleOrDefault<ET_HOADON>("SELECT * FROM dbo.HOA_DON WITH(HOLDLOCK) WHERE MaYeuCau=@requestId", new { requestId = r.MaYeuCau.ToString("D") }, tx);
            if (existing is not null)
            {
                if (existing.MaLS != r.MaLS || !string.Equals(existing.DauVanTayYeuCau, requestHash, StringComparison.Ordinal))
                    throw Error("MaYeuCau", "Mã yêu cầu đã dùng với đầu vào khác. Giữ nguyên yêu cầu cũ để đối chiếu.");
                return ReadPaymentResult(db, tx, existing.MaHD, true);
            }
            // Yêu cầu mới trả chứng từ cũ cũng phải có kết quả cố định. Journal dùng bảng
            // nhật ký sẵn có; request lock bảo vệ ánh xạ append-only, không giữ range lock nhật ký.
            var alias = db.QuerySingleOrDefault<RequestAlias>("""
                SELECT JSON_VALUE(DuLieuSau,N'$.MaLS') MaLS,JSON_VALUE(DuLieuSau,N'$.MaHD') MaHD,
                    JSON_VALUE(DuLieuSau,N'$.DauVanTayYeuCau') DauVanTayYeuCau
                FROM dbo.NHAT_KY_THAO_TAC WITH(READCOMMITTEDLOCK)
                WHERE LoaiDoiTuong=N'YEU_CAU_THANH_TOAN' AND HanhDong=N'Đối chiếu yêu cầu thanh toán' AND MaDoiTuong=@requestId
                """, new { requestId=r.MaYeuCau.ToString("D") },tx);
            if(alias is not null)
            {
                if(alias.MaLS!=r.MaLS || alias.DauVanTayYeuCau!=requestHash) throw Error("MaYeuCau","Mã yêu cầu đã dùng với đầu vào khác. Giữ nguyên yêu cầu cũ để đối chiếu.");
                return ReadPaymentResult(db,tx,alias.MaHD,true);
            }
            existing = db.QuerySingleOrDefault<ET_HOADON>("SELECT * FROM dbo.HOA_DON WITH(HOLDLOCK) WHERE MaLS=@MaLS AND PhienBanQuyTrinh=2", r, tx);
            if (existing is not null)
            {
                var aliasResult=ReadPaymentResult(db,tx,existing.MaHD,true);
                var aliasAudit=TaoMa(db,tx,"NHAT_KY_THAO_TAC","MaNK");
                db.Execute("""
                    INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,DuLieuSau,KetQua)
                    VALUES(@audit,@MaTK,SYSDATETIME(),N'Đối chiếu yêu cầu thanh toán',N'YEU_CAU_THANH_TOAN',@requestId,@data,N'Thành công')
                    """,new{audit=aliasAudit,actor.MaTK,requestId=r.MaYeuCau.ToString("D"),data=JsonSerializer.Serialize(new{r.MaLS,existing.MaHD,DauVanTayYeuCau=requestHash})},tx);
                return aliasResult;
            }

            // Giữ tất cả mặt hàng theo thứ tự ổn định trước khi đọc tồn/giá, kể cả nhiều đơn.
            db.Query<string>("""
                SELECT h.MaHH FROM dbo.HANG_HOA h WITH(UPDLOCK,HOLDLOCK)
                WHERE h.MaHH IN(SELECT t.MaHH FROM dbo.DON_THUOC d WITH(HOLDLOCK)
                    JOIN dbo.CHI_TIET_DON_THUOC c WITH(HOLDLOCK) ON c.MaDT=d.MaDT
                    JOIN dbo.THUOC t WITH(HOLDLOCK) ON t.MaThuoc=c.MaThuoc WHERE d.MaLS=@MaLS AND d.TrangThai=N'Đã xác nhận')
                ORDER BY h.MaHH
                """, r, tx).ToArray();
            var fee = new DAL_THANHTOANTRONGOI(tx).LapBangTinh(r.MaLS, r.MaKhoCapThuoc, actor);
            if (!fee.DuocThanhToan || fee.DauVanTay is null) throw Error("MaLS", fee.ThongBao);
            if (!string.Equals(fee.DauVanTay, r.DauVanTayBangTinh, StringComparison.Ordinal))
                throw new XungDotDuLieuKhamException("Giá, tồn hoặc nguồn phí đã đổi. Xem lại bảng tính trước khi xác nhận thu.");
            if (fee.TongTien > MaxMoney) throw Error("ThanhToan", "Tổng vượt giới hạn số tiền SQL.");
            if ((r.PhuongThuc == "Tiền mặt" && r.SoTienKhachDua < fee.TongTien) ||
                (r.PhuongThuc == "Chuyển khoản" && r.SoTienKhachDua != fee.TongTien))
                throw Error("SoTienKhachDua", "Tiền mặt phải đủ; chuyển khoản phải đúng số phải thu.");
            if (fee.TongTien == 0 && r.SoTienKhachDua != 0) throw Error("SoTienKhachDua", "Hóa đơn 0 không nhận tiền khách đưa.");

            var exports = new DAL_PHIEUXUAT(tx);
            var pending = db.QuerySingleOrDefault<ET_HOADON>("SELECT * FROM dbo.HOA_DON WITH(UPDLOCK,HOLDLOCK) WHERE MaLS=@MaLS AND TaoKhiHoanTat=1 AND PhienBanQuyTrinh=1 AND TrangThai=N'Nháp'", r, tx);
            var orders = fee.Thuoc.Select(d => d.MaDT).Distinct().ToArray(); // Thứ tự SQL MaDT/MaCTDT của preview.
            var deliveries = orders.Select(id => new DonThuocDaThu(id, exports.DispenseCoordinatedPrescription(id, fee.MaKhoCapThuoc!, actor.MaNV))).ToArray();
            var invoices = new DAL_HOADON(tx);
            var invoiceId = pending?.MaHD ?? invoices.CreateInvoice(r.MaLS, actor.MaNV, 0);
            if (pending is not null) AttachPrescriptionExports(db, tx, invoiceId, deliveries);
            db.Execute("UPDATE dbo.HOA_DON SET PhienBanQuyTrinh=2,MaYeuCau=@requestId,DauVanTayYeuCau=@requestHash WHERE MaHD=@invoiceId AND TrangThai=N'Nháp'", new { invoiceId, requestId = r.MaYeuCau.ToString("D"), requestHash }, tx);
            var total = db.ExecuteScalar<decimal>("SELECT SoTienPhaiThu FROM dbo.HOA_DON WHERE MaHD=@invoiceId", new { invoiceId }, tx);
            if (total != fee.TongTien) throw Error("ThanhToan", "Tổng SQL không khớp bảng tính; giao dịch được hủy để đối chiếu.");
            VerifyChargeSources(db, tx, invoiceId, fee);
            invoices.ConfirmCoordinatedInvoice(invoiceId);
            if (total > 0) new DAL_THANHTOAN(tx).RecordCoordinatedPayment(invoiceId, actor.MaNV, total, r.PhuongThuc, r.SoTienKhachDua, r.MaThamChieu);
            foreach (var d in deliveries)
                db.Execute("INSERT dbo.BAN_GIAO_THUOC(MaDT,MaHD,MaPX,TrangThai) VALUES(@MaDT,@invoiceId,@MaPX,N'Chờ giao')", new { d.MaDT, invoiceId, d.MaPX }, tx);
            db.Execute("UPDATE p SET TrangThai=@status FROM dbo.PHIEU_KHAM_BENH p JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaPhieu=p.MaPhieu WHERE l.MaLS=@MaLS", new { r.MaLS, status = deliveries.Length == 0 ? "Hoàn tất" : "Chờ nhận thuốc" }, tx);
            var result = ReadPaymentResult(db, tx, invoiceId, false);
            if (!result.CacDon.SequenceEqual(deliveries)) throw Error("ThanhToan", "Nguồn bàn giao chưa khớp đủ đơn đã thu.");
            var audit = TaoMa(db, tx, "NHAT_KY_THAO_TAC", "MaNK");
            db.Execute("""
                INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,DuLieuSau,KetQua)
                VALUES(@audit,@MaTK,SYSDATETIME(),N'Thanh toán trọn gói',N'HOA_DON',@invoiceId,@data,N'Thành công')
                """, new { audit, actor.MaTK, invoiceId, data = JsonSerializer.Serialize(new { r.MaYeuCau, requestHash, result.MaTT, result.CacDon, result.SoTienThu }) }, tx);
            return result;
        });
    }

    /// <summary>
    /// Giới hạn số tiền được chấp nhận trong kiểm tra nghiệp vụ được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private const decimal MaxMoney = 9999999999999999m;
    /// <summary>
    /// Chuẩn hóa đầu vào và kiểm tra các điều kiện của yêu cầu trước khi xử lý tiếp.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>Error</c>, <c>.Trim</c>, <c>r.MaKhoCapThuoc.Trim</c>, <c>r.MaThamChieu.Trim</c>.</remarks>
    /// <param name="r">Gói dữ liệu đầu vào cho thao tác; các trường được kiểm tra trong quá trình xử lý.</param>
    /// <returns>Kết quả kiểu YeuCauThanhToan dùng cho bước xử lý tiếp theo.</returns>
    private static YeuCauThanhToan Normalize(YeuCauThanhToan r)
    {
        if (r is null) throw Error("ThanhToan", "Thiếu yêu cầu thanh toán.");
        var visit = r.MaLS?.Trim(); var warehouse = string.IsNullOrWhiteSpace(r.MaKhoCapThuoc) ? null : r.MaKhoCapThuoc.Trim();
        var hash = r.DauVanTayBangTinh?.Trim().ToUpperInvariant(); var method = r.PhuongThuc?.Trim();
        var reference = string.IsNullOrWhiteSpace(r.MaThamChieu) ? null : r.MaThamChieu.Trim();
        if (string.IsNullOrEmpty(visit) || visit.Length > 50 || warehouse?.Length > 50 || r.MaYeuCau == Guid.Empty || hash?.Length != 64 || !hash.All(Uri.IsHexDigit))
            throw Error("ThanhToan", "Mã lượt, kho, yêu cầu hoặc bảng tính không hợp lệ.");
        if (method is not ("Tiền mặt" or "Chuyển khoản") || reference?.Length > 100)
            throw Error("PhuongThuc", "Chọn tiền mặt hoặc chuyển khoản; tham chiếu tối đa 100 ký tự.");
        if (r.SoTienKhachDua < 0 || r.SoTienKhachDua > MaxMoney || r.SoTienKhachDua != decimal.Truncate(r.SoTienKhachDua))
            throw Error("SoTienKhachDua", "Nhập tiền VND nguyên đồng trong giới hạn cho phép.");
        if (method == "Chuyển khoản" && (reference is null || !r.DaDoiChieuChuyenKhoan))
            throw Error("MaThamChieu", "Chuyển khoản cần mã tham chiếu và xác nhận đã đối chiếu thủ công.");
        return r with { MaLS = visit, MaKhoCapThuoc = warehouse, DauVanTayBangTinh = hash, PhuongThuc = method, MaThamChieu = reference };
    }
    /// <summary>
    /// Lấy khóa ứng dụng cho yêu cầu hoặc nguồn thanh toán trong giao dịch hiện tại. Dùng application lock sở hữu bởi giao dịch; từ chối khi không lấy được khóa trong thời gian chờ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>db.ExecuteScalar&lt;int&gt;</c>, <c>Error</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="key">Giá trị khóa dùng khi thực hiện thao tác; kiểu string.</param>
    private static void PaymentLock(IDbConnection db, IDbTransaction? tx, string key)
    { if (db.ExecuteScalar<int>("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; SELECT @r", new { key }, tx) < 0) throw Error("ThanhToan", "Lượt đang xử lý; thử lại cùng yêu cầu."); }

    /// <summary>
    /// Đọc và đối chiếu dữ liệu hóa đơn, khoản thu và bàn giao để tạo kết quả thanh toán. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>db.QuerySingle&lt;ET_HOADON&gt;</c>, <c>db.Query&lt;ET_THANHTOAN&gt;</c>, <c>Error</c>, <c>db.Query&lt;DeliveryState&gt;</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="existing">Giá trị đã tồn tại dùng khi thực hiện thao tác; kiểu bool.</param>
    /// <returns>Kết quả kiểu KetQuaThanhToan dùng cho bước xử lý tiếp theo.</returns>
    private static KetQuaThanhToan ReadPaymentResult(IDbConnection db, IDbTransaction? tx, string id, bool existing)
    {
        var h = db.QuerySingle<ET_HOADON>("SELECT * FROM dbo.HOA_DON WITH(HOLDLOCK) WHERE MaHD=@id", new { id }, tx);
        var payments = db.Query<ET_THANHTOAN>("SELECT * FROM dbo.THANH_TOAN WITH(HOLDLOCK) WHERE MaHD=@id", new { id }, tx).ToArray();
        if (h.PhienBanQuyTrinh != 2 || h.TrangThai != "Đã xác nhận" ||
            (h.SoTienPhaiThu == 0 ? payments.Length != 0 : payments.Length != 1 || payments[0].TrangThai != "Đã xác nhận" || payments[0].LoaiGiaoDich != "Thu" || payments[0].SoTien != h.SoTienPhaiThu || payments[0].SoTienKhachDua is null || payments[0].TienThua is null))
            throw Error("MaHD", "Chứng từ chưa đủ điều kiện thu trọn gói; cần đối chiếu, không tự thu thêm.");
        var deliveries = db.Query<DeliveryState>("SELECT MaDT,MaPX,TrangThai TrangThaiGiao,MaNVGiao,ThoiGianGiao FROM dbo.BAN_GIAO_THUOC WITH(HOLDLOCK) WHERE MaHD=@id ORDER BY MaDT", new { id }, tx)
            .Select(d=>new DonThuocDaThu(d.MaDT,d.MaPX){TrangThaiGiao=d.TrangThaiGiao,MaNVGiao=d.MaNVGiao,ThoiGianGiao=d.ThoiGianGiao}).ToArray();
        return new(h.MaLS, h.MaHD, payments.SingleOrDefault()?.MaTT, deliveries, h.SoTienPhaiThu,
            payments.SingleOrDefault()?.SoTienKhachDua ?? 0, payments.SingleOrDefault()?.TienThua ?? 0, existing);
    }
    /// <summary>
    /// Đối chiếu các nguồn dịch vụ và từng phần lô thuốc trên hóa đơn với bảng tính đã xem.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>db.Query&lt;string&gt;</c>, <c>services.SequenceEqual</c>, <c>fee.DichVu.Select</c>, <c>Error</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="invoiceId">Giá trị mã hóa đơn dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="fee">Giá trị phí dùng khi thực hiện thao tác; kiểu BangTinhPhi.</param>
    private static void VerifyChargeSources(IDbConnection db, IDbTransaction? tx, string invoiceId, BangTinhPhi fee)
    {
        var services = db.Query<string>("SELECT MaDVThucHien FROM dbo.CHI_TIET_HOA_DON WHERE MaHD=@invoiceId AND MaDVThucHien IS NOT NULL ORDER BY MaDVThucHien", new { invoiceId }, tx).ToArray();
        if (!services.SequenceEqual(fee.DichVu.Select(d => d.MaDVThucHien))) throw Error("ThanhToan", "Nguồn dịch vụ không khớp.");
        var actual = db.Query<PhanThuDaChot>("""
            SELECT x.MaCTDT,x.MaLo,x.SoLuong,c.DonGia,c.ThanhTien FROM dbo.CHI_TIET_HOA_DON c
            JOIN dbo.CHI_TIET_PHIEU_XUAT x ON x.MaCTPX=c.MaCTPX WHERE c.MaHD=@invoiceId ORDER BY x.MaCTDT,x.MaLo
            """, new { invoiceId }, tx).ToArray();
        var expected = fee.Thuoc.SelectMany(d => d.CacLo.Select(l => new PhanThuDaChot(d.MaCTDT, l.MaLo, l.SoLuong, l.DonGia, l.ThanhTien)))
            .OrderBy(l => l.MaCTDT, StringComparer.Ordinal).ThenBy(l => l.MaLo, StringComparer.Ordinal).ToArray();
        if (!actual.OrderBy(l=>l.MaCTDT,StringComparer.Ordinal).ThenBy(l=>l.MaLo,StringComparer.Ordinal).SequenceEqual(expected))
            throw Error("ThanhToan", "Nguồn thuốc hoặc từng phần lô không khớp bảng tính.");
    }
    /// <summary>
    /// Liên kết các dòng thuốc đã chốt với từng phần xuất lô; chỉ thay nguồn sau khi số lượng và tiền khớp. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>db.Query&lt;ET_CHITIETHOADON&gt;</c>, <c>deliveries.Select</c>, <c>exported.Sum</c>, <c>Error</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="invoiceId">Giá trị mã hóa đơn dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="deliveries">Giá trị các đơn bàn giao dùng khi thực hiện thao tác; kiểu DonThuocDaThu[].</param>
    private static void AttachPrescriptionExports(IDbConnection db, IDbTransaction? tx, string invoiceId, DonThuocDaThu[] deliveries)
    {
        var pending = db.Query<ET_CHITIETHOADON>("SELECT * FROM dbo.CHI_TIET_HOA_DON WITH(UPDLOCK,HOLDLOCK) WHERE MaHD=@invoiceId AND MaCTDT IS NOT NULL ORDER BY MaCTDT", new { invoiceId }, tx).ToArray();
        foreach (var source in pending)
        {
            var exported = db.Query<ET_CHITIETHOADON>("""
                SELECT x.MaCTPX,@content NoiDung,@price DonGia,
                    CAST(ROUND(x.SoLuong*@price,2) AS decimal(18,2)) ThanhTien
                FROM dbo.CHI_TIET_PHIEU_XUAT x JOIN dbo.PHIEU_XUAT p ON p.MaPX=x.MaPX
                WHERE x.MaCTDT=@MaCTDT AND p.TrangThai=N'Đã ghi sổ' AND x.MaPX IN @exportIds ORDER BY x.MaCTPX
                """, new { source.MaCTDT, content = source.NoiDung, price = source.DonGia, exportIds = deliveries.Select(d => d.MaPX).ToArray() }, tx).ToArray();
            if (exported.Length == 0 || exported.Sum(c => c.ThanhTien) != source.ThanhTien)
                throw Error("MaHD", "Thuốc xuất không khớp khoản đã chốt; giao dịch được hủy.");
            // Giữ nguồn đơn đến khi đủ mọi phần lô; trigger chỉ cho thay thế đúng nguồn, lượng và giá.
            foreach (var part in exported) DAL_CHITIETHOADON.ThemChiTiet(db, tx, invoiceId, part);
            db.Execute("DELETE dbo.CHI_TIET_HOA_DON WHERE MaCTHD=@MaCTHD AND MaHD=@invoiceId", new { source.MaCTHD, invoiceId }, tx);
        }
    }
    /// <summary>
    /// Gói dữ liệu phân thu đã chốt truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
    /// </summary>
    /// <param name="MaCTDT">Mã chi tiết đơn thuốc xác định đối tượng cần xử lý.</param>
    /// <param name="MaLo">Mã lô xác định đối tượng cần xử lý.</param>
    /// <param name="SoLuong">Giá trị số lượng dùng khi thực hiện thao tác; kiểu decimal.</param>
    /// <param name="DonGia">Giá trị đơn giá dùng khi thực hiện thao tác; kiểu decimal.</param>
    /// <param name="ThanhTien">Giá trị thành tiền dùng khi thực hiện thao tác; kiểu decimal.</param>
    private sealed record PhanThuDaChot(string MaCTDT, string MaLo, decimal SoLuong, decimal DonGia, decimal ThanhTien);
    /// <summary>
    /// Gói dữ liệu yêu cầu bí danh yêu cầu truyền giữa các bước xử lý; các thành phần được khai báo ngay trong kiểu.
    /// </summary>
    /// <param name="MaLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="MaHD">Mã hóa đơn xác định đối tượng cần xử lý.</param>
    /// <param name="DauVanTayYeuCau">Giá trị dùng đối chiếu phiên bản dữ liệu tại bước xử lý tương ứng.</param>
    private sealed record RequestAlias(string MaLS,string MaHD,string DauVanTayYeuCau);
    /// <summary>
    /// Mô hình và thao tác hỗ trợ bàn giao trạng thái dùng bởi các thành phần liên quan.
    /// </summary>
    private sealed class DeliveryState
    {
        /// <summary>
        /// Mã đơn thuốc.
        /// </summary>
        public string MaDT{get;set;}="";
        /// <summary>
        /// Mã phiếu xuất.
        /// </summary>
        public string MaPX{get;set;}="";
        /// <summary>
        /// Trạng thái bàn giao thuốc cho bệnh nhân.
        /// </summary>
        public string TrangThaiGiao{get;set;}="";
        /// <summary>
        /// Mã nhân viên xác nhận bàn giao thuốc. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MaNVGiao{get;set;}
        /// <summary>
        /// Thời điểm xác nhận bàn giao thuốc. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? ThoiGianGiao{get;set;}
    }
}
