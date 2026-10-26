using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Dapper;
using ET;
namespace DAL;

public sealed partial class DAL_THANHTOANTRONGOI : dbconnect
{
    private readonly IDbTransaction? paymentTransaction;
    public DAL_THANHTOANTRONGOI(IDbTransaction? transaction = null) : base(transaction) => paymentTransaction = transaction;
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
            if (old.Length > 0 || hasExports)
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
                    AND NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON c WITH(HOLDLOCK) WHERE c.MaDVThucHien=s.MaDVThucHien)
                ORDER BY s.MaDVThucHien
                """, new { maLS }, tx).ToArray();
            var orders = db.Query<DonPhi>("SELECT MaDT,NgayKe,MaDTThayThe FROM dbo.DON_THUOC WITH(HOLDLOCK) WHERE MaLS=@maLS AND TrangThai=N'Đã xác nhận' ORDER BY MaDT", new { maLS }, tx).ToArray();
            var lines = db.Query<ThuocPhi>("""
                SELECT c.MaDT,c.MaCTDT,c.MaThuoc,t.MaHH,c.SoLuong,t.DonGiaBan DonGia,
                    c.TenThuocChot,c.HamLuongChot,c.DonViTinhChot,h.TrangThai TrangThaiHang
                FROM dbo.DON_THUOC d WITH(HOLDLOCK) JOIN dbo.CHI_TIET_DON_THUOC c WITH(HOLDLOCK) ON c.MaDT=d.MaDT
                JOIN dbo.THUOC t WITH(HOLDLOCK) ON t.MaThuoc=c.MaThuoc JOIN dbo.HANG_HOA h WITH(HOLDLOCK) ON h.MaHH=t.MaHH
                WHERE d.MaLS=@maLS AND d.TrangThai=N'Đã xác nhận' ORDER BY d.MaDT,c.MaCTDT
                """, new { maLS }, tx).ToArray();
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
            return new BangTinhPhi(maLS, visit.MaBN, visit.HoTenBN, maKhoCapThuoc, services, drugs, [], hash, !fractional,
                fractional ? "Nguồn phí có số lẻ đồng. Cần điều chỉnh giá phù hợp trước thu; bảng tính giữ đúng tổng theo từng lô." :
                "Bảng tính chưa phải hóa đơn. Giá và tồn sẽ được kiểm tra lại khi thanh toán.");
        });

    private static LoiKhamException Error(string field, string text) => new(field, text);
    private sealed class LuotPhi
    {
        public string MaBS { get; set; } = "";
        public string BacSiPhieu { get; set; } = "";
        public string TrangThai { get; set; } = "";
        public string TrangThaiPhieu { get; set; } = "";
        public string MaBN { get; set; } = "";
        public string HoTenBN { get; set; } = "";
        public DateTime ThoiGianKham { get; set; }
        public DateTime? NgayXacNhan { get; set; }
    }
    private sealed class DonPhi
    {
        public string MaDT { get; set; } = "";
        public DateTime NgayKe { get; set; }
        public string? MaDTThayThe { get; set; }
    }
    private sealed class ThuocPhi
    {
        public string MaDT { get; set; } = "";
        public string MaCTDT { get; set; } = "";
        public string MaThuoc { get; set; } = "";
        public string MaHH { get; set; } = "";
        public decimal SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string? TenThuocChot { get; set; }
        public string? HamLuongChot { get; set; }
        public string? DonViTinhChot { get; set; }
        public string TrangThaiHang { get; set; } = "";
    }
}
