/* Kiosk AI V2.1: cho phep nhan vien sua loi benh nhan khai nham da tung kham.
   CHI CHAY SAU ban Kiosk AI V1 (07) va V2 (08), tren database da sao luu.
   Khong xoa bang, khong reset du lieu, khong sua ho so benh nhan da co.
   @XacNhanChuaTungKham can duoc nhan vien doi chieu, khong duoc lay tu chatbot.
*/
USE [QLBenhVienRangHamMat];
GO
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.AI_TaoPhieuTuBanKhai',N'P') IS NULL
BEGIN
    PRINT N'CANH BAO: Chua co procedure V2; vui long cai SQL 08 truoc khi chay ban va 10.';
    -- SSMS can bao loi nhung co the van tiep tuc GO, hay dung script neu thieu V2.
    THROW 55100,N'Chua cai Kiosk AI V2 (08).',1;
END;
GO
CREATE OR ALTER PROCEDURE dbo.AI_TaoPhieuTuBanKhai
    @MaPhien uniqueidentifier,
    @MaNV nvarchar(50),
    @MaBNHienCo nvarchar(50) = NULL,
    @HoTen nvarchar(255),
    @NgaySinh date,
    @SDT nvarchar(25),
    @LyDoKham nvarchar(1000),
    @LoaiKham nvarchar(20),
    @MaPhong nvarchar(50),
    @MaBS nvarchar(50) = NULL,
    @MaDV nvarchar(50) = NULL,
    @XacNhanChuaTungKham bit = 0,
    @MaBNOut nvarchar(50) OUTPUT,
    @MaHSOut nvarchar(50) OUTPUT,
    @MaPhieuOut nvarchar(50) OUTPUT,
    @SoThuTuOut int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

    SET @HoTen=NULLIF(LTRIM(RTRIM(@HoTen)),N'');
    SET @SDT=NULLIF(LTRIM(RTRIM(@SDT)),N'');
    SET @LyDoKham=NULLIF(LTRIM(RTRIM(@LyDoKham)),N'');
    SET @MaBNHienCo=NULLIF(LTRIM(RTRIM(@MaBNHienCo)),N'');
    SET @MaBS=NULLIF(LTRIM(RTRIM(@MaBS)),N'');
    SET @MaDV=NULLIF(LTRIM(RTRIM(@MaDV)),N'');
    SET @MaBNOut=NULL; SET @MaHSOut=NULL; SET @MaPhieuOut=NULL; SET @SoThuTuOut=NULL;

    IF @HoTen IS NULL OR LEN(@HoTen)<2 OR @NgaySinh IS NULL OR @NgaySinh>CONVERT(date,SYSDATETIME())
        THROW 55001,N'Ho ten hoac ngay sinh khong hop le. Nhan vien phai kiem tra lai.',1;
    IF @SDT IS NULL OR LEN(@SDT)<9 OR LEN(@SDT)>15 OR @SDT LIKE N'%[^0-9+]%'
        THROW 55002,N'So dien thoai chua hop le.',1;
    IF @LyDoKham IS NULL THROW 55003,N'Phai xac nhan ly do kham.',1;
    IF @LoaiKham NOT IN(N'ThongThuong',N'DichVu') OR @LoaiKham IS NULL
        THROW 55004,N'Loai kham khong hop le.',1;

    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS(
           SELECT 1 FROM dbo.NHAN_VIEN nv JOIN dbo.TAI_KHOAN tk ON tk.MaNV=nv.MaNV
           WHERE nv.MaNV=@MaNV AND nv.TrangThai=N'Đang làm việc' AND tk.TrangThai=N'Đang hoạt động')
           THROW 55008,N'Nhan vien khong con tai khoan hoat dong.',1;
        IF OBJECT_ID(N'dbo.PHAN_QUYEN_VAI_TRO',N'U') IS NOT NULL
        BEGIN
            IF NOT EXISTS(
                SELECT 1 FROM dbo.TAI_KHOAN tk
                JOIN dbo.PHAN_QUYEN_VAI_TRO pq ON pq.MaVaiTro=tk.MaVaiTro
                WHERE tk.MaNV=@MaNV AND pq.MaPhanHe IN(N'BenhNhan',N'PhieuHuongDan')
                  AND pq.DuocTruyCap=1)
                THROW 55020,N'Tai khoan khong duoc cap quyen tiep nhan hoac phieu huong dan.',1;
        END;
        DECLARE @TrangThai nvarchar(32), @CanHoTro bit, @TungKham bit, @MaPhieuCu nvarchar(50);
        SELECT @TrangThai=p.TrangThai,@MaPhieuCu=p.MaPhieu,
               @CanHoTro=ISNULL(n.CanHoTroGap,0),@TungKham=n.DaTungKham
        FROM dbo.AI_PHIEN_TIEP_NHAN p WITH(UPDLOCK,HOLDLOCK)
        JOIN dbo.AI_TIEP_NHAN_NHAP n WITH(UPDLOCK,HOLDLOCK) ON n.MaPhien=p.MaPhien
        WHERE p.MaPhien=@MaPhien;
        IF @TrangThai IS NULL THROW 55005,N'Khong tim thay ban khai AI.',1;
        IF @TrangThai=N'DaDuyet'
        BEGIN
            SELECT @MaPhieuOut=p.MaPhieu,@MaHSOut=k.MaHS,@MaBNOut=k.MaBN,
                   @SoThuTuOut=pk.SoThuTu
            FROM dbo.AI_PHIEN_TIEP_NHAN p
            LEFT JOIN dbo.AI_KET_QUA_TAO_PHIEU k ON k.MaPhien=p.MaPhien
            LEFT JOIN dbo.PHIEU_KHAM_BENH pk ON pk.MaPhieu=p.MaPhieu
            WHERE p.MaPhien=@MaPhien;
            IF @MaPhieuOut IS NULL THROW 55006,N'Phien da duyet nhung thieu ma phieu, can nhan vien kiem tra.',1;
            COMMIT TRANSACTION;
            RETURN;
        END;
        IF @TrangThai<>N'ChoNhanVien' OR @CanHoTro=1
            THROW 55007,N'Chi duoc tu dong tiep nhan ban khai ChoNhanVien, khong co canh bao khan.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.PHONG WHERE MaPhong=@MaPhong AND TrangThai=N'Đang hoạt động')
            THROW 55009,N'Phong kham khong hoat dong hoac khong ton tai.',1;
        IF @LoaiKham=N'DichVu' AND (@MaBS IS NULL OR @MaDV IS NULL)
            THROW 55010,N'Kham dich vu bat buoc chon bac si va dich vu.',1;
        IF @LoaiKham=N'ThongThuong' AND @MaDV IS NOT NULL
            THROW 55022,N'Kham thong thuong khong tu dong them dich vu. Hay chuyen sang loai kham dich vu neu can.',1;

        IF @MaDV IS NOT NULL AND NOT EXISTS(
            SELECT 1 FROM dbo.DICH_VU d
            JOIN dbo.CHUYEN_KHOA ck ON ck.MaCK=d.MaCK
            JOIN dbo.PHONG ph ON ph.MaKhoa=ck.MaKhoa
            WHERE d.MaDV=@MaDV AND ph.MaPhong=@MaPhong
              AND d.TrangThai=N'Đang hoạt động' AND d.NgayApDung<=CONVERT(date,SYSDATETIME()))
            THROW 55011,N'Dich vu khong du dieu kien hoac khong thuoc khoa/phong da chon.',1;
        IF @MaBS IS NOT NULL AND NOT EXISTS(
            SELECT 1 FROM dbo.NHAN_VIEN nv
            JOIN dbo.CHUC_VU cv ON cv.MaChucVu=nv.MaChucVu
            JOIN dbo.LICH_LAM_VIEC lv ON lv.MaNV=nv.MaNV
            WHERE nv.MaNV=@MaBS AND cv.TenChucVu LIKE N'%Bác sĩ%'
              AND nv.TrangThai=N'Đang làm việc' AND lv.TrangThai=N'Đã xác nhận'
              AND lv.NgayLam=CONVERT(date,SYSDATETIME()) AND lv.MaPhong=@MaPhong)
            THROW 55012,N'Bac si khong co lich lam viec da xac nhan tai phong nay hom nay.',1;
        IF @MaBS IS NOT NULL AND @MaDV IS NOT NULL AND NOT EXISTS(
            SELECT 1 FROM dbo.NHAN_VIEN nv JOIN dbo.DICH_VU dv ON dv.MaCK=nv.MaCK
            WHERE nv.MaNV=@MaBS AND dv.MaDV=@MaDV)
            THROW 55013,N'Bac si khong thuoc chuyen khoa cua dich vu.',1;

        IF @MaBNHienCo IS NOT NULL
        BEGIN
            IF NOT EXISTS(
                SELECT 1 FROM dbo.BENH_NHAN b WITH(UPDLOCK,HOLDLOCK)
                WHERE b.MaBN=@MaBNHienCo AND LTRIM(RTRIM(b.HoTenBN))=@HoTen
                  AND b.NgaySinh=@NgaySinh)
                THROW 55014,N'Ho so cu khong khop ho ten / ngay sinh. Hay xac minh danh tinh va sua ho so truoc khi lien ket.',1;
            SET @MaBNOut=@MaBNHienCo;
            IF EXISTS (SELECT 1 FROM dbo.BENH_NHAN b
                       WHERE b.MaBN=@MaBNHienCo AND REPLACE(ISNULL(b.SDT,N''),N' ',N'')<>REPLACE(@SDT,N' ',N''))
                INSERT dbo.AI_SU_KIEN(MaPhien,LoaiSuKien,MaNV)
                VALUES(@MaPhien,N'SDT_HoSoCu_Khac',@MaNV);
        END
        ELSE
        BEGIN
            IF @TungKham=1 AND ISNULL(@XacNhanChuaTungKham,0)<>1
                THROW 55015,N'Benh nhan khai da tung kham. Hay tra cuu va xac nhan khai nham neu that su chua co ho so.',1;
            DECLARE @KhoaBN int;
            EXEC @KhoaBN=sys.sp_getapplock @Resource=N'RHM_AI_TAO_BENH_NHAN',
                   @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
            IF @KhoaBN<0 THROW 55016,N'Khong khoa duoc thao tac tao benh nhan; thu lai.',1;
            IF EXISTS(SELECT 1 FROM dbo.BENH_NHAN b WITH(UPDLOCK,HOLDLOCK)
                      WHERE REPLACE(ISNULL(b.SDT,N''),N' ',N'')=REPLACE(@SDT,N' ',N'')
                         OR (b.HoTenBN=@HoTen AND b.NgaySinh=@NgaySinh))
                THROW 55017,N'Co ho so nghi trung (SDT hoac ho ten + ngay sinh). Hay tra cuu va xac minh truoc.',1;
            SET @MaBNOut=N'BN_AI_'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
            INSERT dbo.BENH_NHAN(MaBN,HoTenBN,GioiTinh,NgaySinh,SDT,DiaChi,MaBHYT,NhomMau,TienSuBenh,DiUng,TrangThai)
            VALUES(@MaBNOut,@HoTen,NULL,@NgaySinh,@SDT,NULL,NULL,NULL,NULL,NULL,N'Đang theo dõi');
        END;

        SELECT @MaHSOut=MaHS FROM dbo.HO_SO_BENH_AN WITH(UPDLOCK,HOLDLOCK) WHERE MaBN=@MaBNOut;
        IF @MaHSOut IS NULL
        BEGIN
            SET @MaHSOut=N'HS_AI_'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
            INSERT dbo.HO_SO_BENH_AN(MaHS,MaBN,NgayLap,LyDoKhamBanDau,GhiChu,TrangThai)
            VALUES(@MaHSOut,@MaBNOut,SYSDATETIME(),@LyDoKham,N'Ban khai ban dau da duoc nhan vien kiem tra qua AI.',N'Đang theo dõi');
        END;

        -- Nếu bệnh nhân đã có phiếu còn hiệu lực cùng ngày/phòng, dừng để nhân viên đối chiếu.
        IF EXISTS(SELECT 1 FROM dbo.PHIEU_KHAM_BENH p WITH(UPDLOCK,HOLDLOCK)
                  WHERE p.MaHS=@MaHSOut AND p.MaPhong=@MaPhong
                    AND p.TrangThai<>N'Hủy'
                    AND p.ThoiGianTiepNhan>=CONVERT(date,SYSDATETIME())
                    AND p.ThoiGianTiepNhan<DATEADD(day,1,CONVERT(date,SYSDATETIME())))
            THROW 55021,N'Benh nhan da co phieu kham hom nay o phong nay. Hay kiem tra truoc khi tao tiep.',1;

        DECLARE @KhoaSTT int,@NgayHienTai date=CONVERT(date,SYSDATETIME()),
                @Resource nvarchar(255)=N'RHM_AI_STT_'+@MaPhong+N'_'+CONVERT(nvarchar(8),SYSDATETIME(),112);
        EXEC @KhoaSTT=sys.sp_getapplock @Resource=@Resource,
             @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
        IF @KhoaSTT<0 THROW 55018,N'Khong khoa duoc so thu tu, thu lai.',1;
        SELECT @SoThuTuOut=ISNULL(MAX(SoThuTu),0)+1
        FROM dbo.PHIEU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK)
        WHERE MaPhong=@MaPhong AND ThoiGianTiepNhan>=@NgayHienTai
              AND ThoiGianTiepNhan<DATEADD(day,1,@NgayHienTai);
        SET @MaPhieuOut=N'PKB_AI_'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
        INSERT dbo.PHIEU_KHAM_BENH
            (MaPhieu,MaHS,MaLich,MaPhong,MaBS,MaNVTiepNhan,MaDV,SoThuTu,
             ThoiGianTiepNhan,LyDoKham,TrangThai,GhiChu)
        VALUES(@MaPhieuOut,@MaHSOut,NULL,@MaPhong,@MaBS,@MaNV,@MaDV,@SoThuTuOut,
               SYSDATETIME(),@LyDoKham,N'Chờ khám',
               N'Tiếp nhận từ bản khai AI; đã đối chiếu bởi nhân viên.');

        UPDATE dbo.AI_PHIEN_TIEP_NHAN
        SET TrangThai=N'DaDuyet',MaPhieu=@MaPhieuOut,MaNVXacNhan=@MaNV,
            NgayXacNhan=SYSUTCDATETIME(),NgayCapNhat=SYSUTCDATETIME()
        WHERE MaPhien=@MaPhien AND TrangThai=N'ChoNhanVien';
        IF @@ROWCOUNT<>1 THROW 55019,N'Phien da bi thay doi boi nhan vien khac.',1;
        INSERT dbo.AI_KET_QUA_TAO_PHIEU
            (MaPhien,MaBN,MaHS,MaPhieu,MaNVDuyet,LoaiKham,PhuThuDuKien,
             HoTenXacNhan,NgaySinhXacNhan,SDTXacNhan,LyDoKhamXacNhan)
        VALUES(@MaPhien,@MaBNOut,@MaHSOut,@MaPhieuOut,@MaNV,@LoaiKham,
               CASE WHEN @LoaiKham=N'DichVu' THEN 30000 ELSE 0 END,
               @HoTen,@NgaySinh,@SDT,@LyDoKham);
        IF @MaBNHienCo IS NULL AND @TungKham=1 AND @XacNhanChuaTungKham=1
            INSERT dbo.AI_SU_KIEN(MaPhien,LoaiSuKien,MaNV)
            VALUES(@MaPhien,N'DoiChieuKhaiNhan',@MaNV);
        INSERT dbo.AI_SU_KIEN(MaPhien,LoaiSuKien,MaNV)
        VALUES(@MaPhien,N'TaoPhieuTuAI',@MaNV);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
PRINT NDa nang cap dbo.AI_TaoPhieuTuBanKhai V2.1.;
GO
