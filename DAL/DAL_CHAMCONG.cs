using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DAL
{
    // SQL Server là nguồn thời gian và kiểm tra cuối cùng, không tin dữ liệu giờ/người dùng từ giao diện.
    public class DAL_CHAMCONG : dbconnect
    {
        public DateTime layThoiGianMayChu()
        {
            using var ketNoi = (SqlConnection)CreateConnection();
            using var lenh = new SqlCommand("SELECT SYSDATETIME()", ketNoi);
            ketNoi.Open();
            return Convert.ToDateTime(lenh.ExecuteScalar());
        }

        public DataTable layChamCongTheoThang(DateTime dauThang, string maTK)
        {
            using var ketNoi = (SqlConnection)CreateConnection();
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = @"
                DECLARE @MaNV NVARCHAR(50), @LaBQL BIT = 0;
                SELECT @MaNV=tk.MaNV,
                    @LaBQL=CASE WHEN vt.TenVaiTro=N'Admin' THEN 1 ELSE 0 END
                FROM dbo.TAI_KHOAN tk JOIN dbo.VAI_TRO vt ON vt.MaVaiTro=tk.MaVaiTro
                WHERE tk.MaTK=@MaTK AND tk.TrangThai=N'Đang hoạt động';
                IF @MaNV IS NULL
                    THROW 51200,N'Tài khoản không hoạt động hoặc chưa liên kết nhân viên.',1;

                SELECT l.MaLLV,l.MaNV,n.HoTen,ISNULL(cv.TenChucVu,N'Chưa phân loại') AS TenChucVu,
                    l.MaCa,l.NgayLam,c.GioBatDau,c.GioKetThuc,l.GioVao,l.GioRa,
                    l.TrangThai AS TrangThaiLich,l.TrangThaiHienDien,
                    CASE WHEN cc.TrangThai=N'Đã xác nhận' AND cc.CongDuocTinh>0
                         THEN N'Đã chấm công'
                         WHEN cc.TrangThai=N'Đã xác nhận' AND l.GioVao IS NOT NULL
                         THEN N'Đã chấm công (0 công)'
                         WHEN cc.TrangThai=N'Đã xác nhận' THEN N'Đã ghi nhận vắng'
                         WHEN cc.TrangThai=N'Không hợp lệ' THEN N'Không hợp lệ'
                         ELSE N'Chưa chấm công' END AS TrangThai,
                    ISNULL(cc.CongDuocTinh,0) AS CongDuocTinh,
                    ISNULL(cc.SoPhutDiTre,0) AS SoPhutDiTre,
                    -- Trừ 0,25 công qua CongTruDiTre ở SQL tính lương, KHÔNG trừ 2 lần.
                    CAST(CASE WHEN cc.TrangThai=N'Đã xác nhận'
                                   AND cc.CongDuocTinh=0.50 AND cc.SoPhutDiTre BETWEEN 1 AND 60
                              THEN 0.25
                              ELSE ISNULL(cc.CongDuocTinh,0)
                         END AS DECIMAL(4,2)) AS CongThucNhan,
                    cc.MaNVDoiSoat,cc.NgayDoiSoat,cc.GhiChu,
                    CAST(CASE WHEN l.MaNV=@MaNV THEN 1 ELSE 0 END AS BIT) AS LaCaCuaToi,
                    CAST(CASE WHEN l.GioVao IS NOT NULL OR
                      (cc.MaCC IS NOT NULL AND cc.TrangThai<>N'Chờ đối soát')
                      THEN 1 ELSE 0 END AS BIT) AS DaCoGhiNhan,
                    truoc.GioBatDau AS GioBatDauCaTruoc,
                    truoc.GioKetThuc AS GioKetThucCaTruoc,
                    CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI dn
                      WHERE dn.MaNV=l.MaNV AND dn.TrangThaiDuyet=N'Đã duyệt'
                        AND l.NgayLam BETWEEN dn.TuNgay AND dn.DenNgay) THEN 1 ELSE 0 END AS BIT) AS CoDonNghi,
                    CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.BANG_LUONG bl
                      WHERE bl.MaNV=l.MaNV AND bl.TrangThai=N'Đã chốt'
                        AND l.NgayLam BETWEEN bl.TuNgay AND bl.DenNgay) THEN 1 ELSE 0 END AS BIT) AS DaChot
                FROM dbo.LICH_LAM_VIEC l
                JOIN dbo.CA_LAM c ON c.MaCa=l.MaCa
                JOIN dbo.NHAN_VIEN n ON n.MaNV=l.MaNV
                LEFT JOIN dbo.CHUC_VU cv ON cv.MaChucVu=n.MaChucVu
                LEFT JOIN dbo.CHAM_CONG cc ON cc.MaLLV=l.MaLLV
                OUTER APPLY (
                    SELECT TOP(1) c2.GioBatDau,c2.GioKetThuc
                    FROM dbo.LICH_LAM_VIEC l2 JOIN dbo.CA_LAM c2 ON c2.MaCa=l2.MaCa
                    WHERE l2.MaNV=l.MaNV AND l2.NgayLam=l.NgayLam
                      AND l2.TrangThai=N'Đã xác nhận'
                      AND (c2.GioBatDau<c.GioBatDau OR
                           (c2.GioBatDau=c.GioBatDau AND l2.MaLLV<l.MaLLV))
                    ORDER BY c2.GioBatDau DESC,l2.MaLLV DESC
                ) truoc
                WHERE l.NgayLam>=@TuNgay AND l.NgayLam<@DenNgay
                    AND (@LaBQL=1 OR l.MaNV=@MaNV)
                ORDER BY l.NgayLam,c.GioBatDau,n.HoTen";
            lenh.Parameters.Add(new SqlParameter("@MaTK",SqlDbType.NVarChar,50){Value=maTK});
            lenh.Parameters.Add(new SqlParameter("@TuNgay",SqlDbType.Date){Value=dauThang.Date});
            lenh.Parameters.Add(new SqlParameter("@DenNgay",SqlDbType.Date){Value=dauThang.AddMonths(1).Date});
            ketNoi.Open();
            using var doc = lenh.ExecuteReader();
            var duLieu = new DataTable();
            duLieu.Load(doc);
            return duLieu;
        }

        public void chamCong(string maLLV, string maTK)
            => ThucHien((connection, transaction) =>
            {
                // Dùng chung guard phiên, ngữ cảnh audit và giao dịch của tầng DAL.
                var ketNoi = (SqlConnection)connection;
                var giaoDich = (SqlTransaction?)transaction;
                // Chung khóa với thủ tục tính/chốt lương để không ghi công vào lúc đang chốt.
                using (var khoa = new SqlCommand(@"
                    DECLARE @KetQua INT;
                    EXEC @KetQua=sys.sp_getapplock @Resource=N'RHM:TinhChotLuong',
                        @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
                    IF @KetQua<0 THROW 51201,N'Đang xử lý bảng lương, vui lòng thử lại.',1;",ketNoi,giaoDich))
                    khoa.ExecuteNonQuery();

                using var lenh = new SqlCommand(@"
                    SELECT l.MaLLV,l.MaNV,l.NgayLam,l.TrangThai,l.GioVao,l.GioRa,
                           c.GioBatDau,c.GioKetThuc,
                           cc.MaCC,cc.TrangThai AS TrangThaiCong,cc.CongDuocTinh,
                           tk.MaTK AS TaiKhoanHopLe,
                           (SELECT TOP(1) c2.GioBatDau
                            FROM dbo.LICH_LAM_VIEC l2 JOIN dbo.CA_LAM c2 ON c2.MaCa=l2.MaCa
                            WHERE l2.MaNV=l.MaNV AND l2.NgayLam=l.NgayLam
                              AND l2.TrangThai=N'Đã xác nhận'
                              AND (c2.GioBatDau<c.GioBatDau OR
                                   (c2.GioBatDau=c.GioBatDau AND l2.MaLLV<l.MaLLV))
                            ORDER BY c2.GioBatDau DESC,l2.MaLLV DESC) AS GioBatDauCaTruoc,
                           (SELECT TOP(1) c2.GioKetThuc
                            FROM dbo.LICH_LAM_VIEC l2 JOIN dbo.CA_LAM c2 ON c2.MaCa=l2.MaCa
                            WHERE l2.MaNV=l.MaNV AND l2.NgayLam=l.NgayLam
                              AND l2.TrangThai=N'Đã xác nhận'
                              AND (c2.GioBatDau<c.GioBatDau OR
                                   (c2.GioBatDau=c.GioBatDau AND l2.MaLLV<l.MaLLV))
                            ORDER BY c2.GioBatDau DESC,l2.MaLLV DESC) AS GioKetThucCaTruoc
                    FROM dbo.LICH_LAM_VIEC l WITH(UPDLOCK,HOLDLOCK)
                    JOIN dbo.CA_LAM c ON c.MaCa=l.MaCa
                    LEFT JOIN dbo.CHAM_CONG cc WITH(UPDLOCK,HOLDLOCK) ON cc.MaLLV=l.MaLLV
                    LEFT JOIN dbo.TAI_KHOAN tk ON tk.MaNV=l.MaNV AND tk.MaTK=@MaTK
                      AND tk.TrangThai=N'Đang hoạt động'
                    WHERE l.MaLLV=@MaLLV;",ketNoi,giaoDich);
                lenh.Parameters.Add(new SqlParameter("@MaLLV",SqlDbType.NVarChar,50){Value=maLLV});
                lenh.Parameters.Add(new SqlParameter("@MaTK",SqlDbType.NVarChar,50){Value=maTK});
                var duLieu = new DataTable();
                using (var doc = lenh.ExecuteReader()) duLieu.Load(doc);
                if (duLieu.Rows.Count!=1)
                    throw new InvalidOperationException("Không tìm thấy ca được phân công.");
                DataRow ca = duLieu.Rows[0];
                if (ca.IsNull("TaiKhoanHopLe"))
                    throw new UnauthorizedAccessException("Chỉ nhân viên sở hữu ca và có tài khoản đang hoạt động mới được chấm công.");
                if (ca.Field<string>("TrangThai")!="Đã xác nhận")
                    throw new InvalidOperationException("Ca chưa được xác nhận phân công hoặc đã bị hủy/nghỉ.");
                if (!ca.IsNull("GioVao") || !ca.IsNull("GioRa") ||
                    (!ca.IsNull("MaCC") && ca.Field<string>("TrangThaiCong")!="Chờ đối soát"))
                    throw new InvalidOperationException("Ca đã có dữ liệu chấm công, không thể chấm trùng hoặc ghi đè dữ liệu cũ.");

                // Lấy thời gian tại SQL Server ngay trong giao dịch, tuyệt đối không dùng DateTime.Now của máy WPF.
                DateTime hienTai;
                using (var gio = new SqlCommand("SELECT SYSDATETIME()",ketNoi,giaoDich))
                    hienTai = Convert.ToDateTime(gio.ExecuteScalar());
                DateTime ngay = ca.Field<DateTime>("NgayLam").Date;
                if (ngay != hienTai.Date)
                    throw new InvalidOperationException("Chỉ được chấm công trong đúng ngày làm việc (không chấm trước hoặc bù ngày cũ).");
                // Phương án A: hết giờ kết thúc ca là khóa ngay, kể cả vẫn trong cùng ngày.
                // Ca xuyên đêm tính mốc kết thúc sang ngày hôm sau; quy tắc chỉ chấm
                // trong đúng ngày NgayLam ở trên vẫn được giữ để tương thích dữ liệu cũ.
                TimeSpan batDauCa = ca.Field<TimeSpan>("GioBatDau");
                TimeSpan ketThucCa = ca.Field<TimeSpan>("GioKetThuc");
                DateTime mocKetThuc = ngay.Add(ketThucCa);
                if (ketThucCa <= batDauCa) mocKetThuc = mocKetThuc.AddDays(1);
                if (hienTai >= mocKetThuc)
                    throw new InvalidOperationException("Ca đã kết thúc lúc " +
                        mocKetThuc.ToString("HH:mm") + ". Không được chấm công bù sau giờ kết thúc ca.");

                // Chỉ thời gian của SQL Server quyết định số phút trễ.
                // Trễ từ giây đầu tiên làm tròn lên 1 phút; đúng mốc 60 phút = 0,25 công.
                DateTime mocBatDau = ngay.Add(batDauCa);
                int soPhutDiTre = hienTai <= mocBatDau ? 0 :
                    (int)Math.Ceiling((hienTai - mocBatDau).TotalMinutes);
                // CHECK CHAM_CONG chỉ cho CongDuocTinh = 0 hoặc 0,50.
                // Với 1..60 phút, lưu 0,50 công gốc; SQL trừ 0,25 trong bảng lương.
                // Trên 60 phút: lưu 0 công, không còn trừ thêm ở bảng lương.
                decimal congGoc = soPhutDiTre > 60 ? 0m : 0.5m;
                decimal congThucNhan = soPhutDiTre == 0 ? 0.5m :
                    (soPhutDiTre <= 60 ? 0.25m : 0m);

                if (!ca.IsNull("GioKetThucCaTruoc"))
                {
                    TimeSpan batDauTruoc = ca.Field<TimeSpan>("GioBatDauCaTruoc");
                    TimeSpan ketThucTruoc = ca.Field<TimeSpan>("GioKetThucCaTruoc");
                    DateTime moc = ngay.Add(ketThucTruoc);
                    if (ketThucTruoc<=batDauTruoc) moc = moc.AddDays(1);
                    if (hienTai<moc)
                        throw new InvalidOperationException("Ca trước chưa kết thúc ("+moc.ToString("HH:mm")+"). Vui lòng chấm công ca sau khi hết ca trước.");
                }

                using (var kiemTra = new SqlCommand(@"
                    IF EXISTS(SELECT 1 FROM dbo.BANG_LUONG WITH(UPDLOCK,HOLDLOCK)
                      WHERE MaNV=@MaNV AND TrangThai=N'Đã chốt' AND @Ngay BETWEEN TuNgay AND DenNgay)
                      THROW 51202,N'Kỳ lương đã chốt, không thể ghi công.',1;
                    IF EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI WITH(UPDLOCK,HOLDLOCK)
                      WHERE MaNV=@MaNV AND TrangThaiDuyet=N'Đã duyệt' AND @Ngay BETWEEN TuNgay AND DenNgay)
                      THROW 51203,N'Ngày này đã có đơn nghỉ phép được duyệt.',1;
                    IF OBJECT_ID('dbo.NHAT_KY_DUYET_CHAM_CONG','U') IS NULL
                      THROW 51204,N'Chưa có bảng nhật ký chấm công từ phiên bản V9.',1;",ketNoi,giaoDich))
                {
                    kiemTra.Parameters.Add(new SqlParameter("@MaNV",SqlDbType.NVarChar,50){Value=ca.Field<string>("MaNV")!});
                    kiemTra.Parameters.Add(new SqlParameter("@Ngay",SqlDbType.Date){Value=ngay});
                    kiemTra.ExecuteNonQuery();
                }

                string maNV = ca.Field<string>("MaNV")!;
                string? maCC = ca.Field<string>("MaCC");
                if (string.IsNullOrWhiteSpace(maCC)) maCC="CC"+Guid.NewGuid().ToString("N");
                using var luu = new SqlCommand(@"
                    UPDATE dbo.LICH_LAM_VIEC SET GioVao=@ThoiDiem,GioRa=NULL,
                        TrangThaiHienDien=N'Có mặt' WHERE MaLLV=@MaLLV;
                    IF EXISTS(SELECT 1 FROM dbo.CHAM_CONG WHERE MaLLV=@MaLLV)
                        UPDATE dbo.CHAM_CONG SET SoPhutDiTre=@SoPhutDiTre,CongDuocTinh=@CongGoc,
                            TrangThai=N'Đã xác nhận',MaNVDoiSoat=@MaNV,
                            NgayDoiSoat=@ThoiDiem,GhiChu=@GhiChu
                        WHERE MaLLV=@MaLLV;
                    ELSE
                        INSERT dbo.CHAM_CONG(MaCC,MaLLV,SoPhutDiTre,CongDuocTinh,
                            TrangThai,MaNVDoiSoat,NgayDoiSoat,GhiChu)
                        VALUES(@MaCC,@MaLLV,@SoPhutDiTre,@CongGoc,N'Đã xác nhận',@MaNV,@ThoiDiem,
                            @GhiChu);
                    INSERT dbo.NHAT_KY_DUYET_CHAM_CONG(
                        MaLLV,MaNV,NguoiThucHien,NgayThucHien,TrangThaiCu,TrangThaiMoi,
                        GioVaoCu,GioRaCu,GioVaoMoi,GioRaMoi,CongCu,CongMoi,LyDo)
                    VALUES(@MaLLV,@MaNV,@MaNV,@ThoiDiem,@TrangThaiCu,
                        N'Đã xác nhận',NULL,NULL,@ThoiDiem,NULL,@CongCu,@CongGoc,@GhiChu);",ketNoi,giaoDich);
                luu.Parameters.Add(new SqlParameter("@MaLLV",SqlDbType.NVarChar,50){Value=maLLV});
                luu.Parameters.Add(new SqlParameter("@MaCC",SqlDbType.NVarChar,50){Value=maCC});
                luu.Parameters.Add(new SqlParameter("@MaNV",SqlDbType.NVarChar,50){Value=maNV});
                luu.Parameters.Add(new SqlParameter("@ThoiDiem",SqlDbType.DateTime2){Value=hienTai});
                luu.Parameters.Add(new SqlParameter("@SoPhutDiTre",SqlDbType.Int){Value=soPhutDiTre});
                luu.Parameters.Add(new SqlParameter("@CongGoc",SqlDbType.Decimal){Precision=4,Scale=2,Value=congGoc});
                luu.Parameters.Add(new SqlParameter("@GhiChu",SqlDbType.NVarChar,500){
                    Value=$"Nhân viên tự chấm công; trễ {soPhutDiTre} phút; công thực nhận {congThucNhan:0.##}"});
                luu.Parameters.Add(new SqlParameter("@TrangThaiCu",SqlDbType.NVarChar,50){Value=ca.Field<string>("TrangThaiCong") ?? "Chưa chấm công"});
                luu.Parameters.Add(new SqlParameter("@CongCu",SqlDbType.Decimal){Precision=4,Scale=2,Value=ca.Field<decimal?>("CongDuocTinh") ?? 0m});
                luu.ExecuteNonQuery();
                return 0;
            });
    }
}
