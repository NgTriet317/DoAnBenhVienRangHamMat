/*
 NÂNG CẤP TRÊN DATABASE SQL V9 ĐÃ CÓ: không xóa bảng, không nạp lại dữ liệu mẫu.
 Chạy 1 lần hoặc chạy lại an toàn trong SSMS (F5).
 Đổi tên database bên dưới nếu kết nối WPF sử dụng database V9 khác.
 Chỉ chốt từ ngày +5 sau hết tháng, quy tắc do TR_BL_ChotLuong quyết định.
*/
USE [QLBenhVienRangHamMat_TestLuong];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.BANG_LUONG',N'U') IS NULL
   OR COL_LENGTH(N'dbo.BANG_LUONG',N'CongThucTe') IS NULL
   OR OBJECT_ID(N'dbo.CHAM_CONG',N'U') IS NULL
   OR OBJECT_ID(N'dbo.TR_BL_ChotLuong',N'TR') IS NULL
   OR OBJECT_ID(N'dbo.NHAT_KY_DUYET_CHAM_CONG',N'U') IS NULL
    THROW 52000,N'Chưa có đủ schema SQL V9 và nâng cấp duyệt chấm công. Không cài đặt.',1;
GO

-- Nguồn: ca đã xác nhận (0,5 công/ca), nghỉ được duyệt, khấu trừ đi trễ,
-- hoa hồng đã hạch toán, chi tiết lương và tỷ lệ bảo hiểm của chính sách đã duyệt.
-- Ca ngắn KHÔNG bị loại vì thời lượng không đủ 8 giờ.
CREATE OR ALTER PROCEDURE dbo.RHM_TinhLuongKy
    @TuNgay DATE,
    @DenNgay DATE,
    @MaNVTinh NVARCHAR(50),
    @MaBL NVARCHAR(50)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @TuNgay IS NULL OR @DenNgay<>EOMONTH(@TuNgay) OR DAY(@TuNgay)<>1
        THROW 52001,N'Chỉ được tính lương cho một tháng đầy đủ.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.TAI_KHOAN tk JOIN dbo.VAI_TRO vt ON tk.MaVaiTro=vt.MaVaiTro
                  WHERE tk.MaNV=@MaNVTinh AND vt.TenVaiTro=N'Admin' AND tk.TrangThai=N'Đang hoạt động')
        THROW 52002,N'Chỉ tài khoản quản lý đang hoạt động được tính lương.',1;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @Khoa INT;
        EXEC @Khoa=sys.sp_getapplock @Resource=N'RHM:TinhChotLuong',
             @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
        IF @Khoa<0 THROW 52003,N'Kỳ lương đang được thao tác ở cửa sổ khác.',1;

        IF @MaBL IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.BANG_LUONG WITH(UPDLOCK,HOLDLOCK)
            WHERE MaBL=@MaBL AND TuNgay=@TuNgay AND DenNgay=@DenNgay AND TrangThai=N'Nháp')
            THROW 52004,N'Phiếu đã chốt, không còn hoặc không thuộc kỳ lương được chọn.',1;

        -- Khi mở kỳ mới: lập phiếu nháp cho nhân viên đang làm việc và đã có
        -- CHÍNH XÁC MỘT chính sách được duyệt áp dụng trọn tháng.
        IF @MaBL IS NULL
        BEGIN
            IF EXISTS(
                SELECT 1 FROM dbo.NHAN_VIEN nv
                WHERE nv.TrangThai=N'Đang làm việc' AND nv.NgayVaoLam<=@DenNgay
                  AND NOT EXISTS(SELECT 1 FROM dbo.BANG_LUONG bl WITH(UPDLOCK,HOLDLOCK)
                                 WHERE bl.MaNV=nv.MaNV AND bl.TuNgay=@TuNgay AND bl.DenNgay=@DenNgay)
                  AND (SELECT COUNT(*) FROM dbo.CHINH_SACH_LUONG cs
                       WHERE cs.MaChucVu=nv.MaChucVu AND cs.TrangThai=N'Đã duyệt'
                         AND cs.NgayHieuLuc<=@TuNgay AND (cs.NgayHetHieuLuc IS NULL OR cs.NgayHetHieuLuc>=@DenNgay))<>1
            ) THROW 52005,N'Có nhân viên chưa có hoặc có nhiều chính sách lương hợp lệ. Hãy kiểm tra chính sách trước khi tạo kỳ.',1;

            INSERT dbo.BANG_LUONG (MaBL,MaNV,MaCSL,TuNgay,DenNgay,SoCaTinh,LuongCoBan,
                SoCaDinhMuc,CongChuan,CongNghiPhep,CongTruDiTre,CongThucTe,TienHoaHong,
                TienPhuCap,TienTangCa,TienBaoHiem,TienKhauTruKhac,TrangThai,NgayChot,MaNVChot,GhiChu)
            SELECT N'BL'+CONVERT(CHAR(6),@TuNgay,112)+N'-'+REPLACE(CONVERT(NVARCHAR(36),NEWID()),N'-',N''),
                nv.MaNV,cs.MaCSL,@TuNgay,@DenNgay,0,cs.LuongCoBan,cs.SoCaDinhMuc,
                1,0,0,0,0,0,0,0,0,N'Nháp',NULL,NULL,N'Phiếu được tạo từ chính sách lương đã duyệt.'
            FROM dbo.NHAN_VIEN nv
            CROSS APPLY (
                SELECT cs.MaCSL,cs.LuongCoBan,cs.SoCaDinhMuc
                FROM dbo.CHINH_SACH_LUONG cs WHERE cs.MaChucVu=nv.MaChucVu
                    AND cs.TrangThai=N'Đã duyệt' AND cs.NgayHieuLuc<=@TuNgay
                    AND (cs.NgayHetHieuLuc IS NULL OR cs.NgayHetHieuLuc>=@DenNgay)
            ) cs
            WHERE nv.TrangThai=N'Đang làm việc' AND nv.NgayVaoLam<=@DenNgay
                AND NOT EXISTS(SELECT 1 FROM dbo.BANG_LUONG bl WITH(UPDLOCK,HOLDLOCK)
                    WHERE bl.MaNV=nv.MaNV AND bl.TuNgay=@TuNgay AND bl.DenNgay=@DenNgay);
        END;

        -- Số công chuẩn tính thứ Hai-Thứ Bảy, loại ngày nghỉ lễ có hưởng lương
        -- (theo cùng quy ước của trigger TR_BL_ChotLuong trong SQL V9).
        DECLARE @CongChuan INT;
        ;WITH N AS (
            SELECT n.k FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),
                (10),(11),(12),(13),(14),(15),(16),(17),(18),(19),
                (20),(21),(22),(23),(24),(25),(26),(27),(28),(29),(30)) n(k)
        )
        SELECT @CongChuan=COUNT(*) FROM N
        WHERE DATEADD(DAY,N.k,@TuNgay)<=@DenNgay
            AND DATEDIFF(DAY,CONVERT(DATE,'19000107'),DATEADD(DAY,N.k,@TuNgay))%7<>0
            AND NOT EXISTS(SELECT 1 FROM dbo.NGAY_NGHI_LE h
                WHERE h.NgayNghi=DATEADD(DAY,N.k,@TuNgay) AND h.CoHuongLuong=1);
        IF @CongChuan IS NULL OR @CongChuan<=0 THROW 52006,N'Không xác định được công chuẩn trong kỳ.',1;

        IF EXISTS(SELECT 1 FROM dbo.BANG_LUONG b
            JOIN dbo.NHAN_VIEN nv ON nv.MaNV=b.MaNV
            JOIN dbo.CHINH_SACH_LUONG cs ON cs.MaCSL=b.MaCSL
            WHERE b.TuNgay=@TuNgay AND b.DenNgay=@DenNgay AND b.TrangThai=N'Nháp'
            AND (@MaBL IS NULL OR b.MaBL=@MaBL)
            AND (cs.TrangThai<>N'Đã duyệt' OR cs.MaChucVu<>nv.MaChucVu
                OR cs.NgayHieuLuc>@TuNgay OR (cs.NgayHetHieuLuc IS NOT NULL AND cs.NgayHetHieuLuc<@DenNgay)))
            THROW 52007,N'Chính sách lương của phiếu chưa duyệt, sai chức vụ hoặc không bao phủ cả kỳ.',1;

        ;WITH SoCong AS (
            SELECT b.MaBL,
                COALESCE((SELECT SUM(cc.CongDuocTinh) FROM dbo.CHAM_CONG cc WITH(HOLDLOCK)
                    JOIN dbo.LICH_LAM_VIEC ll WITH(HOLDLOCK) ON ll.MaLLV=cc.MaLLV
                    WHERE ll.MaNV=b.MaNV AND ll.NgayLam BETWEEN @TuNgay AND @DenNgay
                      AND cc.TrangThai=N'Đã xác nhận'),0) AS CongCa,
                (SELECT COUNT(*) FROM dbo.CHAM_CONG cc WITH(HOLDLOCK)
                    JOIN dbo.LICH_LAM_VIEC ll WITH(HOLDLOCK) ON ll.MaLLV=cc.MaLLV
                    WHERE ll.MaNV=b.MaNV AND ll.NgayLam BETWEEN @TuNgay AND @DenNgay
                      AND cc.TrangThai=N'Đã xác nhận' AND cc.CongDuocTinh=0.5) AS SoCa,
                (SELECT COUNT(*) FROM dbo.CHAM_CONG cc WITH(HOLDLOCK)
                    JOIN dbo.LICH_LAM_VIEC ll WITH(HOLDLOCK) ON ll.MaLLV=cc.MaLLV
                    WHERE ll.MaNV=b.MaNV AND ll.NgayLam BETWEEN @TuNgay AND @DenNgay
                      AND cc.TrangThai=N'Đã xác nhận' AND cc.CongDuocTinh=0.5
                      AND cc.SoPhutDiTre BETWEEN 16 AND 60) * CAST(0.25 AS DECIMAL(7,2)) AS TruTre,
                (SELECT COUNT(*) FROM (VALUES(0),(1),(2),(3),(4),(5),(6),(7),(8),(9),
                    (10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20),
                    (21),(22),(23),(24),(25),(26),(27),(28),(29),(30)) n(k)
                    WHERE DATEADD(DAY,n.k,@TuNgay)<=@DenNgay
                        AND DATEDIFF(DAY,CONVERT(DATE,'19000107'),DATEADD(DAY,n.k,@TuNgay))%7<>0
                        AND NOT EXISTS(SELECT 1 FROM dbo.NGAY_NGHI_LE h
                            WHERE h.NgayNghi=DATEADD(DAY,n.k,@TuNgay) AND h.CoHuongLuong=1)
                        AND EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI d WITH(HOLDLOCK)
                            WHERE d.MaNV=b.MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
                              AND DATEADD(DAY,n.k,@TuNgay) BETWEEN d.TuNgay AND d.DenNgay)) AS CongPhep,
                COALESCE((SELECT SUM(hh.TienHoaHong) FROM dbo.CHI_TIET_HOA_HONG_LUONG hh WITH(HOLDLOCK)
                    WHERE hh.MaBL=b.MaBL),0) AS HoaHong,
                COALESCE((SELECT SUM(ct.SoTien) FROM dbo.CHI_TIET_LUONG ct WITH(HOLDLOCK)
                    WHERE ct.MaBL=b.MaBL AND ct.LoaiKhoan=N'Phụ cấp' AND ct.TinhChat=N'Cộng'),0) AS PhuCapThem,
                COALESCE((SELECT SUM(ct.SoTien) FROM dbo.CHI_TIET_LUONG ct WITH(HOLDLOCK)
                    WHERE ct.MaBL=b.MaBL AND ct.LoaiKhoan=N'Làm thêm giờ' AND ct.TinhChat=N'Cộng'),0) AS TangCa,
                COALESCE((SELECT SUM(ct.SoTien) FROM dbo.CHI_TIET_LUONG ct WITH(HOLDLOCK)
                    WHERE ct.MaBL=b.MaBL AND ct.TinhChat=N'Trừ'),0) AS KhauTru
            FROM dbo.BANG_LUONG b WITH(UPDLOCK,HOLDLOCK)
            WHERE b.TuNgay=@TuNgay AND b.DenNgay=@DenNgay AND b.TrangThai=N'Nháp'
                AND (@MaBL IS NULL OR b.MaBL=@MaBL)
        )
        UPDATE b SET
            b.SoCaTinh=c.SoCa,
            b.SoCaDinhMuc=cs.SoCaDinhMuc,
            b.LuongCoBan=cs.LuongCoBan,
            b.CongChuan=@CongChuan,
            b.CongNghiPhep=c.CongPhep,
            b.CongTruDiTre=c.TruTre,
            b.CongThucTe=c.CongCa+c.CongPhep-c.TruTre,
            b.TienHoaHong=c.HoaHong,
            b.TienPhuCap=cs.PhuCapCoDinh+c.PhuCapThem,
            b.TienTangCa=c.TangCa,
            b.TienBaoHiem=CAST(ROUND(cs.LuongCoBan*(cs.TyLeBHXH+cs.TyLeBHYT+cs.TyLeBHTN),2) AS DECIMAL(18,2)),
            b.TienKhauTruKhac=c.KhauTru,
            b.GhiChu=N'Tính tự động từ công đã duyệt, nghỉ phép, hoa hồng và chi tiết lương. Chỉ chi trả sau khi chốt.'
        FROM dbo.BANG_LUONG b
        JOIN SoCong c ON c.MaBL=b.MaBL
        JOIN dbo.CHINH_SACH_LUONG cs ON cs.MaCSL=b.MaCSL;

        IF EXISTS(SELECT 1 FROM dbo.BANG_LUONG b WHERE b.TuNgay=@TuNgay AND b.DenNgay=@DenNgay
            AND b.TrangThai=N'Nháp' AND (@MaBL IS NULL OR b.MaBL=@MaBL)
            AND (b.CongThucTe<0 OR b.CongThucTe>b.CongChuan))
            THROW 52008,N'Công thực tế vượt công chuẩn (hoặc âm); cần đối soát lại ca/nghỉ phép trước khi tính.',1;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- Một phiếu = một nhân viên. Không bao giờ chốt tự động tất cả.
-- Gọi lại tính từ nguồn TRƯỚC khi chốt, bắt người dùng xem lại nếu giá trị đổi.
CREATE OR ALTER PROCEDURE dbo.RHM_ChotLuongNhanVien
    @MaBL NVARCHAR(50),
    @MaNVChot NVARCHAR(50),
    @CongDaXem DECIMAL(7,2),
    @TongDaXem DECIMAL(18,2),
    @HoaHongDaXem DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.TAI_KHOAN tk JOIN dbo.VAI_TRO vt ON tk.MaVaiTro=vt.MaVaiTro
                  WHERE tk.MaNV=@MaNVChot AND vt.TenVaiTro=N'Admin' AND tk.TrangThai=N'Đang hoạt động')
        THROW 52010,N'Không có quyền chốt lương.',1;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @Khoa INT;
        EXEC @Khoa=sys.sp_getapplock @Resource=N'RHM:TinhChotLuong',
             @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
        IF @Khoa<0 THROW 52011,N'Có người đang thao tác kỳ lương.',1;
        DECLARE @MaNV NVARCHAR(50),@TuNgay DATE,@DenNgay DATE;
        SELECT @MaNV=MaNV,@TuNgay=TuNgay,@DenNgay=DenNgay
        FROM dbo.BANG_LUONG WITH(UPDLOCK,HOLDLOCK)
        WHERE MaBL=@MaBL AND TrangThai=N'Nháp';
        IF @MaNV IS NULL THROW 52012,N'Phiếu không tồn tại hoặc đã chốt.',1;
        IF @TuNgay<>DATEFROMPARTS(YEAR(@TuNgay),MONTH(@TuNgay),1) OR @DenNgay<>EOMONTH(@TuNgay)
            THROW 52013,N'Phiếu lương phải thuộc một tháng đầy đủ.',1;
        IF CONVERT(DATE,SYSDATETIME()) < DATEADD(DAY,5,@DenNgay)
            THROW 52014,N'Chưa tới ngày được chốt: phải sau khi hết tháng ít nhất 5 ngày.',1;
        IF EXISTS(
            SELECT 1 FROM dbo.LICH_LAM_VIEC ll WITH(HOLDLOCK)
            LEFT JOIN dbo.CHAM_CONG cc WITH(HOLDLOCK) ON cc.MaLLV=ll.MaLLV
            WHERE ll.MaNV=@MaNV AND ll.NgayLam BETWEEN @TuNgay AND @DenNgay
              AND ll.TrangThai NOT IN(N'Hủy',N'Từ chối',N'Nghỉ phép',N'Nghỉ việc')
              AND NOT EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI d WITH(HOLDLOCK)
                  WHERE d.MaNV=ll.MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
                    AND ll.NgayLam BETWEEN d.TuNgay AND d.DenNgay)
              AND (cc.MaCC IS NULL OR cc.TrangThai<>N'Đã xác nhận')
        ) THROW 52015,N'Nhân viên còn ca chưa xác nhận công. Hãy đối soát ở mục Chấm công.',1;
        IF EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI d WITH(HOLDLOCK)
            WHERE d.MaNV=@MaNV AND d.TrangThaiDuyet=N'Chờ duyệt'
                AND d.TuNgay<=@DenNgay AND d.DenNgay>=@TuNgay)
            THROW 52016,N'Có đơn nghỉ trong kỳ chưa duyệt/từ chối.',1;
        IF EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI d WITH(HOLDLOCK)
            JOIN dbo.LICH_LAM_VIEC ll WITH(HOLDLOCK) ON ll.MaNV=d.MaNV AND ll.NgayLam BETWEEN d.TuNgay AND d.DenNgay
            JOIN dbo.CHAM_CONG cc WITH(HOLDLOCK) ON cc.MaLLV=ll.MaLLV
            WHERE d.MaNV=@MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
                AND ll.NgayLam BETWEEN @TuNgay AND @DenNgay
                AND cc.TrangThai=N'Đã xác nhận' AND cc.CongDuocTinh>0)
            THROW 52017,N'Ca có công bị trùng ngày nghỉ đã duyệt. Cần xử lý trước khi chốt.',1;
        -- Tránh bỏ sót các điều chỉnh CỘNG chưa được quy đổi về nhóm phụ cấp/tăng ca.
        IF EXISTS(SELECT 1 FROM dbo.CHI_TIET_LUONG ct WITH(HOLDLOCK)
            WHERE ct.MaBL=@MaBL AND ct.TinhChat=N'Cộng'
                AND ct.LoaiKhoan NOT IN(N'Phụ cấp',N'Làm thêm giờ'))
            THROW 52022,N'Có khoản cộng chưa có cột hạch toán (điều chỉnh). Hãy đối soát và phân loại lại.',1;
        IF EXISTS(SELECT 1 FROM dbo.CHI_TIET_LUONG ct WITH(HOLDLOCK)
            WHERE ct.MaBL=@MaBL AND ct.MaNVDuyet IS NULL)
            THROW 52023,N'Có chi tiết lương chưa ghi người duyệt. Hãy duyệt chứng từ trước khi chốt.',1;
        -- Hoa hồng chỉ được chốt khi nguồn dịch vụ và hóa đơn đã xác nhận, thu đủ.
        IF EXISTS(
            SELECT 1 FROM dbo.CHI_TIET_HOA_HONG_LUONG hh WITH(HOLDLOCK)
            JOIN dbo.CHI_TIET_HOA_DON ct ON ct.MaCTHD=hh.MaCTHD
            JOIN dbo.HOA_DON hd ON hd.MaHD=ct.MaHD
            LEFT JOIN dbo.DICH_VU_THUC_HIEN dv ON dv.MaDVThucHien=ct.MaDVThucHien
            WHERE hh.MaBL=@MaBL AND (
                hd.TrangThai<>N'Đã xác nhận' OR dv.MaDVThucHien IS NULL
                OR dv.TrangThai<>N'Đã xác nhận'
                OR COALESCE((SELECT SUM(CASE WHEN tt.LoaiGiaoDich=N'Thu' THEN tt.SoTien
                                           ELSE -tt.SoTien END)
                             FROM dbo.THANH_TOAN tt WITH(HOLDLOCK)
                             WHERE tt.MaHD=hd.MaHD AND tt.TrangThai=N'Đã xác nhận'),0)<>hd.SoTienPhaiThu
            )
        ) THROW 52021,N'Có hoa hồng liên kết hóa đơn/dịch vụ chưa xác nhận hoặc chưa thu đủ.',1;
        -- Kiểm tra trước đó đã xem phiếu nào; calc chỉ cập nhật đúng phiếu này.
        EXEC dbo.RHM_TinhLuongKy @TuNgay=@TuNgay,@DenNgay=@DenNgay,@MaNVTinh=@MaNVChot,@MaBL=@MaBL;
        IF NOT EXISTS(SELECT 1 FROM dbo.BANG_LUONG WITH(UPDLOCK,HOLDLOCK)
            WHERE MaBL=@MaBL AND TrangThai=N'Nháp'
              AND CongThucTe=@CongDaXem AND TongLuong=@TongDaXem AND TienHoaHong=@HoaHongDaXem)
            THROW 52018,N'Số liệu đã thay đổi sau lần tính/xem. Hãy tính lại, kiểm tra rồi chốt.',1;
        IF EXISTS(SELECT 1 FROM dbo.BANG_LUONG WHERE MaBL=@MaBL AND TongLuong<0)
            THROW 52019,N'Thực nhận âm, cần điều chỉnh các khoản trừ.',1;
        UPDATE dbo.BANG_LUONG
        SET TrangThai=N'Đã chốt',NgayChot=SYSDATETIME(),MaNVChot=@MaNVChot
        WHERE MaBL=@MaBL AND TrangThai=N'Nháp';
        IF @@ROWCOUNT<>1 THROW 52020,N'Phiếu đã thay đổi trạng thái, vui lòng tải lại.',1;
        -- SQL V9 kiểm tra chính sách, công, tiền qua TR_BL_ChotLuong.
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
PRINT N'Đã cài đặt: dbo.RHM_TinhLuongKy + dbo.RHM_ChotLuongNhanVien. Dữ liệu cũ được giữ nguyên.';
GO
