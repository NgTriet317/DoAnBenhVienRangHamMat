using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DAL
{
    // Nghiệp vụ kỳ lương V9: truy vấn + tính + đối soát + chốt qua SQL có giao dịch.
    public class DAL_BANGLUONG : dbconnect
    {
        // Cho phép mở các phiếu cũ để tra cứu; tuyệt đối không dùng công thức cũ để chi trả.
        public bool suDungSchemaV9()
        {
            using var ketNoi = CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = "SELECT CASE WHEN COL_LENGTH('dbo.BANG_LUONG','CongThucTe') IS NOT NULL " +
                "AND OBJECT_ID('dbo.CHAM_CONG','U') IS NOT NULL THEN 1 ELSE 0 END";
            ketNoi.Open();
            return Convert.ToInt32(lenh.ExecuteScalar()) == 1;
        }

        /// <summary>
        /// Đọc phiếu lương theo tháng/năm để hiển thị và đối soát.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>suDungSchemaV9</c>, <c>CreateConnection</c>, <c>ketNoi.CreateCommand</c>, <c>lenh.Parameters.Add</c>.</remarks>
        /// <param name="tuNgay">Giá trị từ ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="denNgay">Giá trị đến ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <returns>Kết quả kiểu DataTable dùng cho bước xử lý tiếp theo.</returns>
        public DataTable layBangLuongTheoKy(DateTime tuNgay, DateTime denNgay)
        {
            bool laV9 = suDungSchemaV9();
            using var ketNoi = CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = laV9 ? @"SELECT b.MaBL,b.MaNV,n.HoTen,
                    ISNULL(cv.TenChucVu,N'Chưa phân loại') AS TenChucVu,
                    b.MaCSL,b.TuNgay,b.DenNgay,b.CongChuan,b.CongNghiPhep,
                    b.CongTruDiTre,b.CongThucTe,b.SoCaTinh,b.LuongCoBan,
                    b.LuongTheoCa,b.TienHoaHong,b.TienPhuCap,b.TienTangCa,
                    b.TienBaoHiem,b.TienKhauTruKhac,b.TongLuong,b.TrangThai,
                    b.NgayChot,b.GhiChu,b.PhienBanTinhLuong
                  FROM dbo.BANG_LUONG b
                  JOIN dbo.NHAN_VIEN n ON n.MaNV=b.MaNV
                  LEFT JOIN dbo.CHUC_VU cv ON cv.MaChucVu=n.MaChucVu
                  WHERE (b.TuNgay=@TuNgay AND b.DenNgay=@DenNgay)
                     OR (b.PhienBanTinhLuong=8 AND b.TuNgay<=@DenNgay AND b.DenNgay>=@TuNgay)
                  ORDER BY n.HoTen,b.MaNV" : @"SELECT b.MaBL,b.MaNV,n.HoTen,
                    ISNULL(cv.TenChucVu,N'Chưa phân loại') AS TenChucVu,
                    b.MaCSL,b.TuNgay,b.DenNgay,CAST(b.SoCaDinhMuc AS DECIMAL(7,2)) AS CongChuan,
                    CAST(0 AS DECIMAL(7,2)) AS CongNghiPhep,CAST(0 AS DECIMAL(7,2)) AS CongTruDiTre,
                    CAST(b.SoCaTinh * 0.5 AS DECIMAL(7,2)) AS CongThucTe,
                    b.SoCaTinh,b.LuongCoBan,b.LuongTheoCa,b.TienHoaHong,
                    CAST(0 AS DECIMAL(18,2)) AS TienPhuCap,
                    CAST(0 AS DECIMAL(18,2)) AS TienTangCa,
                    CAST(0 AS DECIMAL(18,2)) AS TienBaoHiem,
                    CAST(0 AS DECIMAL(18,2)) AS TienKhauTruKhac,
                    b.TongLuong,b.TrangThai,b.NgayChot,b.GhiChu,CAST(8 AS INT) AS PhienBanTinhLuong
                  FROM dbo.BANG_LUONG b JOIN dbo.NHAN_VIEN n ON n.MaNV=b.MaNV
                  LEFT JOIN dbo.CHUC_VU cv ON cv.MaChucVu=n.MaChucVu
                  WHERE b.TuNgay<=@DenNgay AND b.DenNgay>=@TuNgay
                  ORDER BY n.HoTen,b.MaNV";
            lenh.Parameters.Add(new SqlParameter("@TuNgay",SqlDbType.Date){Value=tuNgay.Date});
            lenh.Parameters.Add(new SqlParameter("@DenNgay",SqlDbType.Date){Value=denNgay.Date});
            ketNoi.Open();
            using var boDoc = lenh.ExecuteReader();
            var ketQua = new DataTable();
            ketQua.Load(boDoc);
            return ketQua;
        }

        /// <summary>
        /// Đếm các ca công còn chờ xử lý trong kỳ phục vụ đối soát lương.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>suDungSchemaV9</c>, <c>CreateConnection</c>, <c>ketNoi.CreateCommand</c>, <c>lenh.Parameters.Add</c>.</remarks>
        /// <param name="tuNgay">Giá trị từ ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="denNgay">Giá trị đến ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <returns>Giá trị số được đọc, đếm hoặc tính toán trong hàm.</returns>
        public int demCongChoDoiSoat(DateTime tuNgay, DateTime denNgay)
        {
            if (!suDungSchemaV9()) return -1; // Schema cũ chưa có CHAM_CONG.
            using var ketNoi = CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = @"SELECT COUNT(*) FROM dbo.LICH_LAM_VIEC llv
                                 LEFT JOIN dbo.CHAM_CONG cc ON llv.MaLLV=cc.MaLLV
                                 WHERE llv.NgayLam>=@TuNgay AND llv.NgayLam<=@DenNgay
                                   AND llv.TrangThai NOT IN(N'Hủy',N'Từ chối',N'Nghỉ phép',N'Nghỉ việc')
                                   AND NOT EXISTS (SELECT 1 FROM dbo.DON_XIN_NGHI d
                                       WHERE d.MaNV=llv.MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
                                       AND llv.NgayLam BETWEEN d.TuNgay AND d.DenNgay)
                                   AND (cc.MaCC IS NULL OR cc.TrangThai<>N'Đã xác nhận')";
            lenh.Parameters.Add(new SqlParameter("@TuNgay",SqlDbType.Date){Value=tuNgay.Date});
            lenh.Parameters.Add(new SqlParameter("@DenNgay",SqlDbType.Date){Value=denNgay.Date});
            ketNoi.Open();
            return Convert.ToInt32(lenh.ExecuteScalar());
        }

        // Chỉ bật nút tính/chốt khi SQL đã được cài đặt 2 thủ tục V9.
        public bool daCaiTinhLuong()
        {
            using var ketNoi = CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = @"SELECT CASE WHEN OBJECT_ID('dbo.RHM_TinhLuongKy','P') IS NOT NULL
                      AND OBJECT_ID('dbo.RHM_ChotLuongNhanVien','P') IS NOT NULL THEN 1 ELSE 0 END";
            ketNoi.Open();
            return Convert.ToInt32(lenh.ExecuteScalar()) == 1;
        }

        // Dùng cùng ngày của SQL Server với thủ tục chốt lương để không lệch ngày giữa hai máy.
        public DateTime layNgayMayChu()
        {
            using var ketNoi = CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = "SELECT CONVERT(date, SYSDATETIME())";
            ketNoi.Open();
            return Convert.ToDateTime(lenh.ExecuteScalar()).Date;
        }

        // Đếm riêng các lỗi đối soát của TỪNG nhân viên trên đúng kỳ đang xem.
        public DataTable layDoiSoatTheoKy(DateTime tuNgay, DateTime denNgay)
        {
            using var ketNoi = CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = @"
                SELECT b.MaBL,
                    (SELECT COUNT(*) FROM dbo.LICH_LAM_VIEC ll
                     LEFT JOIN dbo.CHAM_CONG cc ON cc.MaLLV=ll.MaLLV
                     WHERE ll.MaNV=b.MaNV AND ll.NgayLam BETWEEN @TuNgay AND @DenNgay
                       AND ll.TrangThai NOT IN(N'Hủy',N'Từ chối',N'Nghỉ phép',N'Nghỉ việc')
                       AND NOT EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI d
                           WHERE d.MaNV=ll.MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
                             AND ll.NgayLam BETWEEN d.TuNgay AND d.DenNgay)
                       AND (cc.MaCC IS NULL OR cc.TrangThai<>N'Đã xác nhận')) AS CongChuaDuyet,
                    (SELECT COUNT(*) FROM dbo.DON_XIN_NGHI d
                     WHERE d.MaNV=b.MaNV AND d.TrangThaiDuyet=N'Chờ duyệt'
                       AND d.TuNgay<=@DenNgay AND d.DenNgay>=@TuNgay) AS DonNghiChoDuyet,
                    (SELECT COUNT(*) FROM dbo.DON_XIN_NGHI d
                     JOIN dbo.LICH_LAM_VIEC ll ON ll.MaNV=d.MaNV AND ll.NgayLam BETWEEN d.TuNgay AND d.DenNgay
                     JOIN dbo.CHAM_CONG cc ON cc.MaLLV=ll.MaLLV
                     WHERE d.MaNV=b.MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
                       AND ll.NgayLam BETWEEN @TuNgay AND @DenNgay
                       AND cc.TrangThai=N'Đã xác nhận' AND cc.CongDuocTinh>0) AS CongTrungNgayPhep,
                    (SELECT COUNT(*) FROM dbo.CHI_TIET_LUONG ct
                     WHERE ct.MaBL=b.MaBL AND ct.TinhChat=N'Cộng'
                         AND ct.LoaiKhoan NOT IN(N'Phụ cấp',N'Làm thêm giờ')) AS KhoanCongChuaPhanLoai,
                    (SELECT COUNT(*) FROM dbo.CHI_TIET_LUONG ct
                     WHERE ct.MaBL=b.MaBL AND ct.MaNVDuyet IS NULL) AS ChungTuChuaDuyet,
                    (SELECT COUNT(*) FROM dbo.CHI_TIET_HOA_HONG_LUONG hh
                     JOIN dbo.CHI_TIET_HOA_DON ct ON ct.MaCTHD=hh.MaCTHD
                     JOIN dbo.HOA_DON hd ON hd.MaHD=ct.MaHD
                     LEFT JOIN dbo.DICH_VU_THUC_HIEN dv ON dv.MaDVThucHien=ct.MaDVThucHien
                     WHERE hh.MaBL=b.MaBL AND (hd.TrangThai<>N'Đã xác nhận'
                         OR dv.MaDVThucHien IS NULL OR dv.TrangThai<>N'Đã xác nhận'
                         OR COALESCE((SELECT SUM(CASE WHEN tt.LoaiGiaoDich=N'Thu' THEN tt.SoTien
                                         ELSE -tt.SoTien END) FROM dbo.THANH_TOAN tt
                             WHERE tt.MaHD=hd.MaHD AND tt.TrangThai=N'Đã xác nhận'),0)<>hd.SoTienPhaiThu)
                    ) AS HoaHongChuaHopLe,
                    CAST(CASE WHEN cs.TrangThai=N'Đã duyệt'
                        AND cs.MaChucVu=nv.MaChucVu AND cs.NgayHieuLuc<=@TuNgay
                        AND (cs.NgayHetHieuLuc IS NULL OR cs.NgayHetHieuLuc>=@DenNgay)
                        THEN 1 ELSE 0 END AS BIT) AS ChinhSachHopLe
                FROM dbo.BANG_LUONG b
                JOIN dbo.NHAN_VIEN nv ON nv.MaNV=b.MaNV
                JOIN dbo.CHINH_SACH_LUONG cs ON cs.MaCSL=b.MaCSL
                WHERE b.TuNgay=@TuNgay AND b.DenNgay=@DenNgay";
            lenh.Parameters.Add(new SqlParameter("@TuNgay",SqlDbType.Date){Value=tuNgay.Date});
            lenh.Parameters.Add(new SqlParameter("@DenNgay",SqlDbType.Date){Value=denNgay.Date});
            ketNoi.Open();
            using var doc = lenh.ExecuteReader();
            var ketQua = new DataTable();
            ketQua.Load(doc);
            return ketQua;
        }

        /// <summary>
        /// Gọi nghiệp vụ tính lương cho kỳ được chỉ định; giữ phiếu đã chốt theo quy tắc SQL và đối soát đầu vào.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>CreateConnection</c>, <c>ketNoi.CreateCommand</c>, <c>lenh.Parameters.Add</c>, <c>ketNoi.Open</c>.</remarks>
        /// <param name="tuNgay">Giá trị từ ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="denNgay">Giá trị đến ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="maNVQuanLy">Mã nhân viên quản lý xác định đối tượng cần xử lý.</param>
        public void tinhLuongTheoKy(DateTime tuNgay, DateTime denNgay, string maNVQuanLy)
        {
            using var ketNoi = CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandType = CommandType.StoredProcedure;
            lenh.CommandText = "dbo.RHM_TinhLuongKy";
            lenh.CommandTimeout = 90;
            lenh.Parameters.Add(new SqlParameter("@TuNgay",SqlDbType.Date){Value=tuNgay.Date});
            lenh.Parameters.Add(new SqlParameter("@DenNgay",SqlDbType.Date){Value=denNgay.Date});
            lenh.Parameters.Add(new SqlParameter("@MaNVTinh",SqlDbType.NVarChar,50){Value=maNVQuanLy});
            ketNoi.Open();
            lenh.ExecuteNonQuery();
        }

        /// <summary>
        /// Chốt một phiếu lương sau khi đối chiếu số công, tổng tiền và hoa hồng mà người dùng đã xem.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>CreateConnection</c>, <c>ketNoi.CreateCommand</c>, <c>lenh.Parameters.Add</c>, <c>ketNoi.Open</c>.</remarks>
        /// <param name="maBL">Mã bảng lương xác định đối tượng cần xử lý.</param>
        /// <param name="maNVQuanLy">Mã nhân viên quản lý xác định đối tượng cần xử lý.</param>
        /// <param name="congDaXem">Giá trị công đã xem dùng khi thực hiện thao tác; kiểu decimal.</param>
        /// <param name="tongDaXem">Giá trị tổng đã xem dùng khi thực hiện thao tác; kiểu decimal.</param>
        /// <param name="hoaHongDaXem">Giá trị hóa hồng đã xem dùng khi thực hiện thao tác; kiểu decimal.</param>
        public void chotLuongNhanVien(string maBL, string maNVQuanLy, decimal congDaXem,
            decimal tongDaXem, decimal hoaHongDaXem)
        {
            using var ketNoi = CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandType = CommandType.StoredProcedure;
            lenh.CommandText = "dbo.RHM_ChotLuongNhanVien";
            lenh.CommandTimeout = 90;
            lenh.Parameters.Add(new SqlParameter("@MaBL",SqlDbType.NVarChar,50){Value=maBL});
            lenh.Parameters.Add(new SqlParameter("@MaNVChot",SqlDbType.NVarChar,50){Value=maNVQuanLy});
            lenh.Parameters.Add(new SqlParameter("@CongDaXem",SqlDbType.Decimal){Precision=7,Scale=2,Value=congDaXem});
            lenh.Parameters.Add(new SqlParameter("@TongDaXem",SqlDbType.Decimal){Precision=18,Scale=2,Value=tongDaXem});
            lenh.Parameters.Add(new SqlParameter("@HoaHongDaXem",SqlDbType.Decimal){Precision=18,Scale=2,Value=hoaHongDaXem});
            ketNoi.Open();
            lenh.ExecuteNonQuery();
        }
    }
}
