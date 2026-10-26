using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Dapper;
using ET;

namespace DAL;

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
            var orders = fee.Thuoc.Select(d => d.MaDT).Distinct().ToArray(); // Thứ tự SQL MaDT/MaCTDT của preview.
            var deliveries = orders.Select(id => new DonThuocDaThu(id, exports.DispenseCoordinatedPrescription(id, fee.MaKhoCapThuoc!, actor.MaNV))).ToArray();
            var invoices = new DAL_HOADON(tx);
            var invoiceId = invoices.CreateInvoice(r.MaLS, actor.MaNV, 0);
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

    private const decimal MaxMoney = 9999999999999999m;
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
    private static void PaymentLock(IDbConnection db, IDbTransaction? tx, string key)
    { if (db.ExecuteScalar<int>("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; SELECT @r", new { key }, tx) < 0) throw Error("ThanhToan", "Lượt đang xử lý; thử lại cùng yêu cầu."); }

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
    private sealed record PhanThuDaChot(string MaCTDT, string MaLo, decimal SoLuong, decimal DonGia, decimal ThanhTien);
    private sealed record RequestAlias(string MaLS,string MaHD,string DauVanTayYeuCau);
    private sealed class DeliveryState
    {
        public string MaDT{get;set;}=""; public string MaPX{get;set;}="";
        public string TrangThaiGiao{get;set;}="";public string? MaNVGiao{get;set;}public DateTime? ThoiGianGiao{get;set;}
    }
}
