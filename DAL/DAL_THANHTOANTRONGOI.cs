using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Dapper;
using ET;
namespace DAL;

/// <summary>
/// Tầng truy cập dữ liệu thanh toán trọn gói, thực hiện truy vấn và kiểm tra trong phạm vi kết nối/giao dịch của dbconnect.
/// </summary>
public sealed partial class DAL_THANHTOANTRONGOI : dbconnect
{
    /// <summary>
    /// Giao dịch được giữ để các thao tác liên quan dùng cùng một phạm vi commit/rollback.
    /// </summary>
    private readonly IDbTransaction? paymentTransaction;
    /// <summary>
    /// Khởi tạo thanh toán trọn gói. Chuyển giao dịch tùy chọn cho lớp cơ sở; nơi cấp giao dịch chịu trách nhiệm kết thúc giao dịch đó.
    /// </summary>
    /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    public DAL_THANHTOANTRONGOI(IDbTransaction? transaction = null) : base(transaction) => paymentTransaction = transaction;
    /// <summary>
    /// Đọc nguồn phí dịch vụ, thuốc và tồn lô để lập bảng tính trước khi thu; trả các điều kiện cho phép thanh toán. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch. Dùng application lock sở hữu bởi giao dịch; từ chối khi không lấy được khóa trong thời gian chờ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>DAL_QUYENNGHIEPVU.KiemTraMa</c>, <c>db.ExecuteScalar&lt;int&gt;</c>, <c>DAL_QUYENNGHIEPVU.XacThuc</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="maKhoCapThuoc">Mã kho cấp thuốc xác định đối tượng cần xử lý.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu BangTinhPhi dùng cho bước xử lý tiếp theo.</returns>
    public BangTinhPhi LapBangTinh(string maLS, string? maKhoCapThuoc, NguCanhNghiepVu nguoiDung)
        => ThucHien((db, tx) =>
        {
            DAL_QUYENNGHIEPVU.KiemTraMa(maLS);
            if (db.ExecuteScalar<int>("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; SELECT @r", new { key = "RHM:Kham:LanKham:" + maLS }, tx) < 0)
                throw new InvalidOperationException("Lượt đang được xử lý, vui lòng tải lại bảng tính.");
            var actor = DAL_QUYENNGHIEPVU.XacThuc(db, tx, nguoiDung);
            var visit = db.QuerySingleOrDefault<LuotPhi>("""
                SELECT l.MaBS,p.MaBS BacSiPhieu,l.TrangThai,p.TrangThai TrangThaiPhieu,b.MaBN,b.HoTenBN,l.ThoiGianKham,l.NgayXacNhan
                FROM dbo.LICH_SU_KHAM_BENH l WITH(HOLDLOCK)
                JOIN dbo.PHIEU_KHAM_BENH p WITH(HOLDLOCK) ON p.MaPhieu=l.MaPhieu
                JOIN dbo.HO_SO_BENH_AN h ON h.MaHS=p.MaHS JOIN dbo.BENH_NHAN b ON b.MaBN=h.MaBN
                WHERE l.MaLS=@maLS
                """, new { maLS }, tx);
            if (visit is null || (!actor.LaAdmin && !actor.CoQuyen(QuyenLamViec.ThuTien) &&
                !(actor.CoQuyen(QuyenLamViec.Kham) && visit.MaBS == actor.MaNV && visit.BacSiPhieu == actor.MaNV)))
                throw DAL_QUYENNGHIEPVU.TuChoi();

            var old = db.Query<HoaDonDoiChieu>("""
                SELECT h.MaHD,h.TrangThai,h.PhienBanQuyTrinh,h.TongTien,h.GiamGia,h.SoTienPhaiThu PhaiThu,
                    COALESCE(t.DaThu,0) DaThu,h.SoTienPhaiThu-COALESCE(t.DaThu,0) ConPhaiThu
                FROM dbo.HOA_DON h WITH(HOLDLOCK)
                OUTER APPLY(SELECT SUM(CASE WHEN LoaiGiaoDich=N'Thu' THEN SoTien ELSE -SoTien END) DaThu
                    FROM dbo.THANH_TOAN WITH(HOLDLOCK) WHERE MaHD=h.MaHD AND TrangThai=N'Đã xác nhận') t
                WHERE h.MaLS=@maLS ORDER BY h.MaHD
                """, new { maLS }, tx).ToArray();
            // Có chứng từ cũ kể cả nháp/hủy: đối chiếu trước, không coi nguồn còn lại là một lần thu mới.
            var hasExports = db.ExecuteScalar<int>("""
                SELECT COUNT(*) FROM dbo.PHIEU_XUAT p WITH(HOLDLOCK)
                WHERE p.MaLS=@maLS OR EXISTS(SELECT 1 FROM dbo.CHI_TIET_PHIEU_XUAT x WITH(HOLDLOCK)
                    JOIN dbo.CHI_TIET_DON_THUOC c WITH(HOLDLOCK) ON c.MaCTDT=x.MaCTDT
                    JOIN dbo.DON_THUOC d WITH(HOLDLOCK) ON d.MaDT=c.MaDT WHERE x.MaPX=p.MaPX AND d.MaLS=@maLS)
                """, new { maLS }, tx) > 0;
            var pending = db.QuerySingleOrDefault<ET_HOADON>("SELECT * FROM dbo.HOA_DON WITH(HOLDLOCK) WHERE MaLS=@maLS AND TaoKhiHoanTat=1 AND PhienBanQuyTrinh=1 AND TrangThai=N'Nháp'", new { maLS }, tx);
            if (pending is not null && db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.THANH_TOAN WITH(HOLDLOCK) WHERE MaHD=@MaHD", pending, tx) != 0)
                throw Error("MaHD", "Hóa đơn chờ thu đã có giao dịch; cần đối chiếu lịch sử.");
            if ((old.Length > 0 && !(old.Length == 1 && pending is not null)) || hasExports)
            {
                DAL_QUYENNGHIEPVU.GhiTraCuuAdmin(db, tx, actor, "LICH_SU_KHAM_BENH", maLS);
                var paid = old.Length == 1 && old[0].PhienBanQuyTrinh == 2 && old[0].TrangThai == "Đã xác nhận" && old[0].ConPhaiThu == 0;
                return new BangTinhPhi(maLS, visit.MaBN, visit.HoTenBN, null, [], [], old, null, false,
                    paid ? "Đã thanh toán. Xem hóa đơn và trạng thái giao thuốc trong lịch sử." : "Lượt đã có chứng từ tài chính hoặc xuất thuốc. Đối chiếu lịch sử và số còn phải thu; không lập bảng thu mới.");
            }

            new DAL_DONTHUOC(tx).KiemTraDonChoThu(maLS, actor);
            if (visit.TrangThaiPhieu != "Chờ thanh toán") throw Error("MaLS", "Phiếu chưa ở trạng thái Chờ thanh toán.");
            var services = db.Query<DongPhiDichVu>("""
                SELECT s.MaDVThucHien,s.MaDV,d.TenDV NoiDung,s.SoLuong,s.DonGia,s.ThanhTien
                FROM dbo.DICH_VU_THUC_HIEN s WITH(HOLDLOCK) JOIN dbo.DICH_VU d WITH(HOLDLOCK) ON d.MaDV=s.MaDV
                WHERE s.MaLS=@maLS AND s.TrangThai=N'Đã xác nhận'
                    AND (NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON c WITH(HOLDLOCK) WHERE c.MaDVThucHien=s.MaDVThucHien)
                        OR EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON c WITH(HOLDLOCK) WHERE c.MaDVThucHien=s.MaDVThucHien AND c.MaHD=@pendingId))
                ORDER BY s.MaDVThucHien
                """, new { maLS, pendingId = pending?.MaHD }, tx).ToArray();
            var orders = db.Query<DonPhi>("SELECT MaDT,NgayKe,MaDTThayThe FROM dbo.DON_THUOC WITH(HOLDLOCK) WHERE MaLS=@maLS AND TrangThai=N'Đã xác nhận' ORDER BY MaDT", new { maLS }, tx).ToArray();
            var lines = db.Query<ThuocPhi>("""
                SELECT c.MaDT,c.MaCTDT,c.MaThuoc,t.MaHH,c.SoLuong,COALESCE(bill.DonGia,t.DonGiaBan) DonGia,
                    c.TenThuocChot,c.HamLuongChot,c.DonViTinhChot,h.TrangThai TrangThaiHang
                FROM dbo.DON_THUOC d WITH(HOLDLOCK) JOIN dbo.CHI_TIET_DON_THUOC c WITH(HOLDLOCK) ON c.MaDT=d.MaDT
                JOIN dbo.THUOC t WITH(HOLDLOCK) ON t.MaThuoc=c.MaThuoc JOIN dbo.HANG_HOA h WITH(HOLDLOCK) ON h.MaHH=t.MaHH
                LEFT JOIN dbo.CHI_TIET_HOA_DON bill WITH(HOLDLOCK) ON bill.MaCTDT=c.MaCTDT AND bill.MaHD=@pendingId
                WHERE d.MaLS=@maLS AND d.TrangThai=N'Đã xác nhận' ORDER BY d.MaDT,c.MaCTDT
                """, new { maLS, pendingId = pending?.MaHD }, tx).ToArray();
            if (pending is not null)
            {
                var sources = db.Query<ET_CHITIETHOADON>("SELECT * FROM dbo.CHI_TIET_HOA_DON WITH(HOLDLOCK) WHERE MaHD=@MaHD", pending, tx).ToArray();
                if (sources.Length != services.Length + lines.Length || sources.Any(c => c.MaCTPX is not null) ||
                    !sources.Where(c => c.MaDVThucHien is not null).Select(c => c.MaDVThucHien).Order().SequenceEqual(services.Select(c => c.MaDVThucHien).Order()) ||
                    !sources.Where(c => c.MaCTDT is not null).Select(c => c.MaCTDT).Order().SequenceEqual(lines.Select(c => c.MaCTDT).Order()))
                    throw Error("MaHD", "Nguồn phí khác hóa đơn đã chốt khi hoàn tất; không tự thêm khoản thu.");
            }
            if (lines.Any(l => string.IsNullOrWhiteSpace(l.TenThuocChot) || string.IsNullOrWhiteSpace(l.HamLuongChot) || string.IsNullOrWhiteSpace(l.DonViTinhChot)))
                return new BangTinhPhi(maLS, visit.MaBN, visit.HoTenBN, null, [], [], [], null, false,
                    "Đơn thuốc lịch sử chưa có thông tin chốt. Cần đối chiếu trước khi xử lý tài chính.");
            if (orders.Any(o => !lines.Any(l => l.MaDT == o.MaDT))) throw Error("MaLS", "Đơn hiệu lực không có dòng thuốc.");
            if (lines.Any(l => l.TrangThaiHang != "Đang hoạt động")) throw Error("MaLS", "Có thuốc ngừng sử dụng; cần bác sĩ đối chiếu đơn.");
            // Cùng độ chính xác với NgayKe do P07 tạo; datetime2(0) làm tròn giây khi ghi.
            var now = db.ExecuteScalar<DateTime>("SELECT CONVERT(datetime2(0),SYSDATETIME())", transaction: tx);
            if (visit.ThoiGianKham > now || orders.Any(o => o.NgayKe > now)) throw Error("MaLS", "Ngày khám hoặc kê đơn chưa đến.");
            if (services.Length == 0 && lines.Length == 0) throw Error("MaLS", "Không có dịch vụ hoặc thuốc hợp lệ để tính phí.");

            PhanBoLoThuoc.Lo[] lots = [];
            var drugs = new List<DongPhiThuoc>();
            if (lines.Length > 0)
            {
                if (string.IsNullOrWhiteSpace(maKhoCapThuoc) || !db.ExecuteScalar<bool>("SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.KHO WITH(HOLDLOCK) WHERE MaKho=@id AND TrangThai=N'Đang hoạt động') THEN 1 ELSE 0 END AS bit)", new { id = maKhoCapThuoc }, tx))
                    throw Error("MaKhoCapThuoc", "Chọn kho cấp thuốc đang hoạt động, đủ toàn bộ đơn.");
                lots = PhanBoLoThuoc.LayLo(db, tx, maKhoCapThuoc, orders.Select(o => o.MaDT).ToArray());
                IReadOnlyList<PhanBoLoThuoc.PhanCap> allocation;
                try { allocation = PhanBoLoThuoc.Lap(lines.Select(l => new PhanBoLoThuoc.Dong { MaCTDT = l.MaCTDT, MaHH = l.MaHH, SoLuong = l.SoLuong }), lots); }
                catch (InvalidOperationException ex) { throw Error("MaKhoCapThuoc", ex.Message); }
                foreach (var line in lines)
                {
                    var parts = allocation.Where(p => p.MaCTDT == line.MaCTDT).Select(p =>
                        new PhanLoTinhPhi(p.MaLo, lots.Single(l => l.MaLo == p.MaLo).SoLo, p.SoLuong, line.DonGia,
                            decimal.Round(p.SoLuong * line.DonGia, 2, MidpointRounding.AwayFromZero))).ToArray();
                    drugs.Add(new(line.MaDT, line.MaCTDT, line.MaThuoc, line.TenThuocChot + " " + line.HamLuongChot,
                        line.DonViTinhChot!, line.SoLuong, line.DonGia, parts));
                }
            }
            else maKhoCapThuoc = null;
            var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { maLS, maKhoCapThuoc,
                visit.TrangThai, visit.TrangThaiPhieu, visit.NgayXacNhan, services, orders, lines, lots, drugs })));
            var fractional = services.Any(s => s.ThanhTien != decimal.Truncate(s.ThanhTien)) || drugs.Any(d => d.ThanhTien != decimal.Truncate(d.ThanhTien));
            if (pending is not null && services.Sum(s => s.ThanhTien) + drugs.Sum(d => d.ThanhTien) != pending.SoTienPhaiThu)
                throw Error("MaHD", "Tổng phân bổ thuốc khác hóa đơn đã chốt; cần đối chiếu trước thu.");
            return new BangTinhPhi(maLS, visit.MaBN, visit.HoTenBN, maKhoCapThuoc, services, drugs, [], hash, !fractional,
                fractional ? "Nguồn phí có số lẻ đồng. Cần điều chỉnh giá phù hợp trước thu; bảng tính giữ đúng tổng theo từng lô." :
                pending is null ? "Giá và tồn sẽ được kiểm tra lại khi thanh toán." : "Hóa đơn đã lập khi hoàn tất khám. Thu đúng khoản đã chốt; tồn thuốc được kiểm tra khi thanh toán.", pending?.MaHD);
        });

    /// <summary>
    /// Tạo ngoại lệ nghiệp vụ với thông tin trường và thông báo được truyền vào.
    /// </summary>
    /// <param name="field">Giá trị trường dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="text">Giá trị nội dung dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả kiểu LoiKhamException dùng cho bước xử lý tiếp theo.</returns>
    private static LoiKhamException Error(string field, string text) => new(field, text);
    /// <summary>
    /// Mô hình và thao tác hỗ trợ lượt phí dùng bởi các thành phần liên quan.
    /// </summary>
    private sealed class LuotPhi
    {
        /// <summary>
        /// Mã bác sĩ.
        /// </summary>
        public string MaBS { get; set; } = "";
        /// <summary>
        /// Bác sĩ trên phiếu tiếp nhận.
        /// </summary>
        public string BacSiPhieu { get; set; } = "";
        /// <summary>
        /// Trạng thái.
        /// </summary>
        public string TrangThai { get; set; } = "";
        /// <summary>
        /// Trạng thái hiện tại của phiếu tiếp nhận.
        /// </summary>
        public string TrangThaiPhieu { get; set; } = "";
        /// <summary>
        /// Mã bệnh nhân.
        /// </summary>
        public string MaBN { get; set; } = "";
        /// <summary>
        /// Họ tên bệnh nhân.
        /// </summary>
        public string HoTenBN { get; set; } = "";
        /// <summary>
        /// Thời gian khám.
        /// </summary>
        public DateTime ThoiGianKham { get; set; }
        /// <summary>
        /// Thời điểm xác nhận; dữ liệu nháp hoặc chưa xác nhận có thể chưa có giá trị. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? NgayXacNhan { get; set; }
    }
    /// <summary>
    /// Mô hình và thao tác hỗ trợ đơn phí dùng bởi các thành phần liên quan.
    /// </summary>
    private sealed class DonPhi
    {
        /// <summary>
        /// Mã đơn thuốc.
        /// </summary>
        public string MaDT { get; set; } = "";
        /// <summary>
        /// Ngày kê.
        /// </summary>
        public DateTime NgayKe { get; set; }
        /// <summary>
        /// Mã đơn thuốc được thay thế bởi đơn hiện tại. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MaDTThayThe { get; set; }
    }
    /// <summary>
    /// Mô hình và thao tác hỗ trợ thuốc phí dùng bởi các thành phần liên quan.
    /// </summary>
    private sealed class ThuocPhi
    {
        /// <summary>
        /// Mã đơn thuốc.
        /// </summary>
        public string MaDT { get; set; } = "";
        /// <summary>
        /// Mã dòng chi tiết đơn thuốc.
        /// </summary>
        public string MaCTDT { get; set; } = "";
        /// <summary>
        /// Mã thuốc.
        /// </summary>
        public string MaThuoc { get; set; } = "";
        /// <summary>
        /// Mã hàng hóa.
        /// </summary>
        public string MaHH { get; set; } = "";
        /// <summary>
        /// Số lượng của dòng dữ liệu.
        /// </summary>
        public decimal SoLuong { get; set; }
        /// <summary>
        /// Đơn giá áp dụng cho dòng dữ liệu.
        /// </summary>
        public decimal DonGia { get; set; }
        /// <summary>
        /// Tên thuốc chốt. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? TenThuocChot { get; set; }
        /// <summary>
        /// Hàm lượng chốt. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? HamLuongChot { get; set; }
        /// <summary>
        /// Đơn vị tính đã chốt. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? DonViTinhChot { get; set; }
        /// <summary>
        /// Trạng thái hàng.
        /// </summary>
        public string TrangThaiHang { get; set; } = "";
    }
}
