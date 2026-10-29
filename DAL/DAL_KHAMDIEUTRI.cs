using System.Data;
using Dapper;
using ET;

namespace DAL;

/// <summary>Nguồn đọc có phạm vi SQL cho quy trình khám; không lấy cache toàn viện rồi lọc quyền trong UI.</summary>
public sealed partial class DAL_KHAMDIEUTRI : dbconnect
{
    /// <summary>
    /// Khởi tạo khám điều trị. Chuyển giao dịch tùy chọn cho lớp cơ sở; nơi cấp giao dịch chịu trách nhiệm kết thúc giao dịch đó.
    /// </summary>
    /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    public DAL_KHAMDIEUTRI(IDbTransaction? transaction = null) : base(transaction) { }

    /// <summary>
    /// Đọc danh sách hàng đợi theo phạm vi dữ liệu và quyền của người dùng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>LayTrangHangDoi</c>.</remarks>
    /// <param name="boLoc">Giá trị bộ lọc dùng khi thực hiện thao tác; kiểu BoLocHangDoi.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Tập dữ liệu kiểu IReadOnlyList&lt;HangDoiKhamItem&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
    public IReadOnlyList<HangDoiKhamItem> LayHangDoi(BoLocHangDoi boLoc, NguCanhNghiepVu nguoiDung)
        => LayTrangHangDoi(boLoc,nguoiDung).CacDong;

    /// <summary>
    /// Đọc dữ liệu chi tiết của mục được chọn để hiển thị hoặc tiếp tục xử lý.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>DAL_QUYENNGHIEPVU.KiemTraMa</c>, <c>DAL_QUYENNGHIEPVU.XacThuc</c>, <c>user.CoQuyen</c>.</remarks>
    /// <param name="maLS">Mã lần khám xác định đối tượng cần xử lý.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu LanKhamChiTiet dùng cho bước xử lý tiếp theo.</returns>
    public LanKhamChiTiet LayChiTiet(string maLS, NguCanhNghiepVu nguoiDung)
        => ThucHien((db, tx) =>
        {
            DAL_QUYENNGHIEPVU.KiemTraMa(maLS);
            var user = DAL_QUYENNGHIEPVU.XacThuc(db, tx, nguoiDung);
            var doctor = user.CoQuyen(QuyenLamViec.Kham);
            var reception = user.CoQuyen(QuyenLamViec.XemHoSo) && user.CoQuyen(QuyenLamViec.TiepNhan);
            var row = db.QuerySingleOrDefault<VisitRead>("""
                SELECT l.*,hs.MaBN,p.MaHS,p.MaBS AS BacSiTiepNhan,p.TrangThai AS TrangThaiPhieu
                FROM dbo.LICH_SU_KHAM_BENH l JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu
                JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS
                WHERE l.MaLS=@maLS AND (@LaAdmin=1 OR
                    (@doctor=1 AND l.MaBS=@MaNV AND p.MaBS=@MaNV)
                    OR (l.TrangThai=N'Đã xác nhận' AND (
                        @reception=1 OR @LaAdmin=1 OR (@doctor=1 AND EXISTS(
                            SELECT 1 FROM dbo.PHIEU_KHAM_BENH own
                            WHERE own.MaHS=p.MaHS AND own.MaBS=@MaNV AND own.TrangThai<>N'Hủy')))))
                """, new { maLS, user.MaNV, doctor, reception, user.LaAdmin }, tx);
            if (row is null) throw DAL_QUYENNGHIEPVU.TuChoi();
            DAL_QUYENNGHIEPVU.GhiTraCuuAdmin(db, tx, user, "LICH_SU_KHAM_BENH", maLS);
            var canEdit = doctor && (user.LaAdmin || row.MaBS == user.MaNV && row.BacSiTiepNhan == user.MaNV) &&
                row.TrangThai == "Nháp" && row.TrangThaiPhieu is "Chờ khám" or "Đang khám";
            return new LanKhamChiTiet(row, row.MaBN, row.MaHS, canEdit);
        });

    /// <summary>
    /// Đọc các đơn thuốc liên quan đến nguồn hồ sơ/lần khám theo phạm vi nghiệp vụ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>DAL_QUYENNGHIEPVU.KiemTraMa</c>, <c>DAL_QUYENNGHIEPVU.XacThuc</c>, <c>user.CoQuyen</c>.</remarks>
    /// <param name="maDT">Mã đơn thuốc xác định đối tượng cần xử lý.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả kiểu DonThuocChiTiet dùng cho bước xử lý tiếp theo.</returns>
    public DonThuocChiTiet LayDonThuoc(string maDT, NguCanhNghiepVu nguoiDung)
        => ThucHien((db, tx) =>
        {
            DAL_QUYENNGHIEPVU.KiemTraMa(maDT);
            var user = DAL_QUYENNGHIEPVU.XacThuc(db, tx, nguoiDung);
            var doctor = user.CoQuyen(QuyenLamViec.Kham);
            var pharmacy = user.CoQuyen(QuyenLamViec.GiaoThuoc);
            var reception = user.CoQuyen(QuyenLamViec.XemHoSo) && user.CoQuyen(QuyenLamViec.TiepNhan);
            var row = db.QuerySingleOrDefault<PrescriptionRead>("""
                SELECT d.*,hs.MaBN,l.TrangThai AS TrangThaiLanKham,p.TrangThai AS TrangThaiPhieu,
                    CONVERT(bit,CASE WHEN d.MaBS=@MaNV AND l.MaBS=@MaNV AND p.MaBS=@MaNV THEN 1 ELSE 0 END) AS ChinhBacSi
                FROM dbo.DON_THUOC d JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=d.MaLS
                JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=l.MaPhieu
                JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS
                WHERE d.MaDT=@maDT AND (@LaAdmin=1 OR
                    (@doctor=1 AND d.MaBS=@MaNV AND l.MaBS=@MaNV AND p.MaBS=@MaNV)
                    OR (l.TrangThai=N'Đã xác nhận' AND d.TrangThai IN(N'Đã xác nhận',N'Ngừng áp dụng') AND (
                        @pharmacy=1 OR @reception=1 OR @LaAdmin=1 OR (@doctor=1 AND EXISTS(
                            SELECT 1 FROM dbo.PHIEU_KHAM_BENH own
                            WHERE own.MaHS=p.MaHS AND own.MaBS=@MaNV AND own.TrangThai<>N'Hủy')))))
                """, new { maDT, user.MaNV, doctor, pharmacy, reception, user.LaAdmin }, tx);
            if (row is null) throw DAL_QUYENNGHIEPVU.TuChoi();
            var lines = db.Query<ET_CHITIETDONTHUOC>("SELECT * FROM dbo.CHI_TIET_DON_THUOC WHERE MaDT=@maDT ORDER BY MaCTDT", new { maDT }, tx).ToArray();
            DAL_QUYENNGHIEPVU.GhiTraCuuAdmin(db, tx, user, "DON_THUOC", maDT);
            var canEdit = doctor && (user.LaAdmin || row.ChinhBacSi) && row.TrangThai == "Nháp" && row.TrangThaiLanKham == "Nháp"
                && row.TrangThaiPhieu is "Chờ khám" or "Đang khám";
            return new DonThuocChiTiet(row, lines, row.MaBN, canEdit);
        });

    /// <summary>
    /// Mô hình và thao tác hỗ trợ lần khám đọc dùng bởi các thành phần liên quan.
    /// </summary>
    private sealed class VisitRead : ET_LICHSUKHAMBENH
    {
        /// <summary>
        /// Mã bệnh nhân.
        /// </summary>
        public string MaBN { get; set; } = "";
        /// <summary>
        /// Mã hồ sơ bệnh án của bệnh nhân.
        /// </summary>
        public string MaHS { get; set; } = "";
        /// <summary>
        /// Bác sĩ được phân công khi tiếp nhận. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? BacSiTiepNhan { get; set; }
        /// <summary>
        /// Trạng thái hiện tại của phiếu tiếp nhận.
        /// </summary>
        public string TrangThaiPhieu { get; set; } = "";
    }

    /// <summary>
    /// Mô hình và thao tác hỗ trợ đơn thuốc đọc dùng bởi các thành phần liên quan.
    /// </summary>
    private sealed class PrescriptionRead : ET_DONTHUOC
    {
        /// <summary>
        /// Mã bệnh nhân.
        /// </summary>
        public string MaBN { get; set; } = "";
        /// <summary>
        /// Trạng thái lần khám.
        /// </summary>
        public string TrangThaiLanKham { get; set; } = "";
        /// <summary>
        /// Trạng thái hiện tại của phiếu tiếp nhận.
        /// </summary>
        public string TrangThaiPhieu { get; set; } = "";
        /// <summary>
        /// Chính bác sĩ phụ trách.
        /// </summary>
        public bool ChinhBacSi { get; set; }
    }
}
