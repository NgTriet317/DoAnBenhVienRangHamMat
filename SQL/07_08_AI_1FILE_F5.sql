/*
  KIOSK AI / MVP LAN DAU - SQL SERVER 2019+
  Chay SAU SQL F5 cua du an. KHONG drop table nghiep vu.
  Chay lai migration an toan (idempotent) - khong doi schema da tao.
*/
USE [QLBenhVienRangHamMat];
GO
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.PHIEU_KHAM_BENH',N'U') IS NULL
    THROW 51000, N'Can cai dat database benh vien truoc khi chay migration Kiosk AI.', 1;
GO
IF OBJECT_ID(N'dbo.AI_PHIEN_TIEP_NHAN', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_PHIEN_TIEP_NHAN
    (
        MaPhien uniqueidentifier NOT NULL CONSTRAINT PK_AI_PHIEN_TIEP_NHAN PRIMARY KEY,
        MaKiosk nvarchar(40) NOT NULL CONSTRAINT DF_AI_PH_MaKiosk DEFAULT N'LAPTOP-01',
        TokenHash binary(32) NOT NULL,
        Buoc nvarchar(32) NOT NULL CONSTRAINT DF_AI_PH_Buoc DEFAULT N'TungKham',
        TrangThai nvarchar(32) NOT NULL CONSTRAINT DF_AI_PH_TrangThai DEFAULT N'DangKhaiBao',
        DongY bit NOT NULL,
        PhienBanThongBao nvarchar(20) NOT NULL CONSTRAINT DF_AI_PH_ThongBao DEFAULT N'v1-demo',
        NgayTao datetime2(3) NOT NULL CONSTRAINT DF_AI_PH_NgayTao DEFAULT SYSUTCDATETIME(),
        NgayCapNhat datetime2(3) NOT NULL CONSTRAINT DF_AI_PH_NgayCapNhat DEFAULT SYSUTCDATETIME(),
        HetHan datetime2(3) NOT NULL,
        MaPhieu nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
        MaNVXacNhan nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
        NgayXacNhan datetime2(3) NULL,
        PhienBan rowversion NOT NULL,
        CONSTRAINT CK_AI_PH_TrangThai CHECK (TrangThai IN (
             N'DangKhaiBao',N'ChoNhanVien',N'CanHoTro',N'DaDuyet',N'DaHuy',N'HetHan')),
        CONSTRAINT CK_AI_PH_DongY CHECK (DongY=1),
        CONSTRAINT CK_AI_PH_ThoiHan CHECK (HetHan > NgayTao),
        CONSTRAINT FK_AI_PH_PhieuKham FOREIGN KEY (MaPhieu) REFERENCES dbo.PHIEU_KHAM_BENH(MaPhieu),
        CONSTRAINT FK_AI_PH_NhanVien FOREIGN KEY (MaNVXacNhan) REFERENCES dbo.NHAN_VIEN(MaNV)
    );
END;
GO
IF OBJECT_ID(N'dbo.AI_TIEP_NHAN_NHAP', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_TIEP_NHAN_NHAP
    (
        MaPhien uniqueidentifier NOT NULL CONSTRAINT PK_AI_TIEP_NHAN_NHAP PRIMARY KEY,
        DaTungKham bit NULL,
        HoTen nvarchar(255) NULL,
        NgaySinh date NULL,
        SoDienThoai nvarchar(25) NULL,
        LyDoKham nvarchar(1000) NULL,
        TrieuChung nvarchar(1000) NULL,
        CanHoTroGap bit NOT NULL CONSTRAINT DF_AI_NHAP_CanHoTro DEFAULT 0,
        NgayCapNhat datetime2(3) NOT NULL CONSTRAINT DF_AI_NHAP_NgayCapNhat DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_AI_NHAP_Phien FOREIGN KEY(MaPhien) REFERENCES dbo.AI_PHIEN_TIEP_NHAN(MaPhien)
    );
END;
GO
IF OBJECT_ID(N'dbo.AI_TIN_NHAN', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_TIN_NHAN
    (
        MaTin bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AI_TIN_NHAN PRIMARY KEY,
        MaPhien uniqueidentifier NOT NULL,
        VaiTro nvarchar(12) NOT NULL,
        NoiDung nvarchar(1200) NOT NULL,
        NgayTao datetime2(3) NOT NULL CONSTRAINT DF_AI_TN_NgayTao DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_AI_TN_VaiTro CHECK (VaiTro IN (N'BenhNhan',N'TroLy')),
        CONSTRAINT FK_AI_TN_Phien FOREIGN KEY(MaPhien) REFERENCES dbo.AI_PHIEN_TIEP_NHAN(MaPhien)
    );
END;
GO
IF OBJECT_ID(N'dbo.AI_SU_KIEN', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_SU_KIEN
    (
        MaSuKien bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AI_SU_KIEN PRIMARY KEY,
        MaPhien uniqueidentifier NOT NULL,
        LoaiSuKien nvarchar(40) NOT NULL,
        MaNV nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
        NgayTao datetime2(3) NOT NULL CONSTRAINT DF_AI_SK_NgayTao DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_AI_SK_Phien FOREIGN KEY(MaPhien) REFERENCES dbo.AI_PHIEN_TIEP_NHAN(MaPhien)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AI_PHIEN_TIEP_NHAN') AND name=N'UX_AI_PH_TokenHash')
    CREATE UNIQUE INDEX UX_AI_PH_TokenHash ON dbo.AI_PHIEN_TIEP_NHAN(TokenHash);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AI_PHIEN_TIEP_NHAN') AND name=N'UX_AI_PH_MaPhieu')
    CREATE UNIQUE INDEX UX_AI_PH_MaPhieu ON dbo.AI_PHIEN_TIEP_NHAN(MaPhieu) WHERE MaPhieu IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AI_PHIEN_TIEP_NHAN') AND name=N'IX_AI_PH_TrangThai_NgayTao')
    CREATE INDEX IX_AI_PH_TrangThai_NgayTao ON dbo.AI_PHIEN_TIEP_NHAN(TrangThai,NgayTao DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AI_TIN_NHAN') AND name=N'IX_AI_TN_MaPhien')
    CREATE INDEX IX_AI_TN_MaPhien ON dbo.AI_TIN_NHAN(MaPhien,MaTin);
GO
/* Chi nhan vien co phieu kham thuc te moi duoc gan MaPhieu.
   Ngan 2 nhan vien cung duyet 1 phien (UPDLOCK, HOLDLOCK + transaction).
   Chu y: KHONG tao BENH_NHAN / HO_SO_BENH_AN / PHIEU_KHAM_BENH o day.
*/
CREATE OR ALTER PROCEDURE dbo.AI_XacNhanPhien
    @MaPhien uniqueidentifier,
    @MaPhieu nvarchar(50),
    @MaNV nvarchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @TrangThai nvarchar(32), @PhieuCu nvarchar(50);
        SELECT @TrangThai=TrangThai, @PhieuCu=MaPhieu
        FROM dbo.AI_PHIEN_TIEP_NHAN WITH(UPDLOCK,HOLDLOCK)
        WHERE MaPhien=@MaPhien;
        IF @TrangThai IS NULL THROW 51001, N'Khong tim thay phien AI.',1;
        IF @TrangThai=N'DaDuyet' AND @PhieuCu=@MaPhieu
        BEGIN
            COMMIT;
            RETURN; -- idempotent
        END;
        IF @TrangThai<>N'ChoNhanVien' THROW 51002, N'Phien chua san sang duyet hoac da xu ly.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.NHAN_VIEN WHERE MaNV=@MaNV AND TrangThai=N'Đang làm việc')
           AND NOT EXISTS(SELECT 1 FROM dbo.NHAN_VIEN WHERE MaNV=@MaNV AND TrangThai=N'Đang hoạt động')
           THROW 51003,N'Nhan vien khong hoat dong hoac khong ton tai.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@MaPhieu)
           THROW 51004,N'Can tao phieu tiep nhan chinh thuc truoc.',1;
        IF EXISTS(SELECT 1 FROM dbo.AI_PHIEN_TIEP_NHAN WITH(UPDLOCK,HOLDLOCK)
                  WHERE MaPhieu=@MaPhieu AND MaPhien<>@MaPhien)
           THROW 51005,N'Phieu nay da lien ket voi phien AI khac.',1;
        IF EXISTS(
            SELECT 1
            FROM dbo.AI_TIEP_NHAN_NHAP n
            JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=@MaPhieu
            JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS
            JOIN dbo.BENH_NHAN b ON b.MaBN=hs.MaBN
            WHERE n.MaPhien=@MaPhien
              AND (
                 (n.HoTen IS NOT NULL AND LTRIM(RTRIM(n.HoTen))<>LTRIM(RTRIM(b.HoTenBN)))
                 OR (n.NgaySinh IS NOT NULL AND (b.NgaySinh IS NULL OR n.NgaySinh<>b.NgaySinh))
                 OR (NULLIF(n.SoDienThoai,N'') IS NOT NULL AND
                       (b.SDT IS NULL OR REPLACE(b.SDT,N' ',N'')<>REPLACE(n.SoDienThoai,N' ',N'')))
              )
        ) THROW 51006,N'Thong tin phieu kham khong khop ban khai AI (ho ten/ngay sinh/SDT). Hay doi chieu truoc.',1;
        UPDATE dbo.AI_PHIEN_TIEP_NHAN
        SET TrangThai=N'DaDuyet', MaPhieu=@MaPhieu,
            MaNVXacNhan=@MaNV, NgayXacNhan=SYSUTCDATETIME(), NgayCapNhat=SYSUTCDATETIME()
        WHERE MaPhien=@MaPhien AND TrangThai=N'ChoNhanVien';
        IF @@ROWCOUNT<>1 THROW 51007,N'Phien da bi thay doi boi mot nhan vien khac.',1;
        INSERT dbo.AI_SU_KIEN(MaPhien,LoaiSuKien,MaNV) VALUES(@MaPhien,N'DuyetPhieu',@MaNV);
        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
/* Don dep theo lich CHI voi phien chua duyet; khong xoa ho so benh an */
CREATE OR ALTER PROCEDURE dbo.AI_DonDepPhien
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRAN;
    UPDATE dbo.AI_PHIEN_TIEP_NHAN
      SET TrangThai=N'HetHan', NgayCapNhat=SYSUTCDATETIME()
    WHERE TrangThai=N'DangKhaiBao' AND HetHan<SYSUTCDATETIME();
    DELETE tn FROM dbo.AI_TIN_NHAN tn
      JOIN dbo.AI_PHIEN_TIEP_NHAN p ON p.MaPhien=tn.MaPhien
    WHERE p.TrangThai IN (N'HetHan',N'DaHuy') AND p.NgayCapNhat<DATEADD(day,-1,SYSUTCDATETIME());
    DELETE n FROM dbo.AI_TIEP_NHAN_NHAP n
      JOIN dbo.AI_PHIEN_TIEP_NHAN p ON p.MaPhien=n.MaPhien
    WHERE p.TrangThai IN (N'HetHan',N'DaHuy') AND p.NgayCapNhat<DATEADD(day,-1,SYSUTCDATETIME());
    COMMIT;
END;
GO
PRINT N'Kiosk AI migration installed. Khong can thay doi bang PHIEU_KHAM_BENH.';
SELECT N'AI_PHIEN_TIEP_NHAN' TenBang, COUNT(*) SoBanGhi FROM dbo.AI_PHIEN_TIEP_NHAN;
GO


/* ======= V2 ======= */
/*
  KIOSK AI V2: NHAN VIEN DUYET -> TU DONG TAO BENH NHAN (NEU MOI),
  HO SO BENH AN (NEU CHUA CO), PHIEU KHAM BENH VA LIEN KET BAN KHAI.
  Chay SAU SQL F5 benh vien va 07_KioskAI_TiepNhan.sql.
  Khong chay script reset/recreate DB. Khong xoa du lieu.
  Khong tu tao hoa don/phu thu 30.000: phu thu du kien duoc ghi nhat ky,
  quy trinh hoa don phai su dung chinh sach gia cua du an de xac nhan thu tien.
*/
USE [QLBenhVienRangHamMat];
GO
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.AI_PHIEN_TIEP_NHAN',N'U') IS NULL OR OBJECT_ID(N'dbo.AI_TIEP_NHAN_NHAP',N'U') IS NULL
    THROW 55000,N'Can chay 07_KioskAI_TiepNhan.sql truoc khi cai V2.',1;
IF OBJECT_ID(N'dbo.PHIEU_KHAM_BENH',N'U') IS NULL
    THROW 55000,N'Thieu database benh vien.',1;
GO
IF OBJECT_ID(N'dbo.AI_KET_QUA_TAO_PHIEU',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_KET_QUA_TAO_PHIEU
    (
        MaPhien uniqueidentifier NOT NULL CONSTRAINT PK_AI_KET_QUA_TAO_PHIEU PRIMARY KEY,
        MaBN nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        MaHS nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        MaPhieu nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        MaNVDuyet nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        LoaiKham nvarchar(20) NOT NULL,
        PhuThuDuKien decimal(18,2) NOT NULL CONSTRAINT DF_AI_KQ_PhuThu DEFAULT(0),
        HoTenXacNhan nvarchar(255) NOT NULL,
        NgaySinhXacNhan date NOT NULL,
        SDTXacNhan nvarchar(25) NOT NULL,
        LyDoKhamXacNhan nvarchar(1000) NOT NULL,
        NgayDuyet datetime2(0) NOT NULL CONSTRAINT DF_AI_KQ_Ngay DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_AI_KQ_Phien FOREIGN KEY(MaPhien) REFERENCES dbo.AI_PHIEN_TIEP_NHAN(MaPhien),
        CONSTRAINT FK_AI_KQ_BN FOREIGN KEY(MaBN) REFERENCES dbo.BENH_NHAN(MaBN),
        CONSTRAINT FK_AI_KQ_HS FOREIGN KEY(MaHS) REFERENCES dbo.HO_SO_BENH_AN(MaHS),
        CONSTRAINT FK_AI_KQ_PKB FOREIGN KEY(MaPhieu) REFERENCES dbo.PHIEU_KHAM_BENH(MaPhieu),
        CONSTRAINT FK_AI_KQ_NV FOREIGN KEY(MaNVDuyet) REFERENCES dbo.NHAN_VIEN(MaNV),
        CONSTRAINT UQ_AI_KQ_Phieu UNIQUE(MaPhieu),
        CONSTRAINT CK_AI_KQ_LoaiKham CHECK(LoaiKham IN(N'ThongThuong',N'DichVu')),
        CONSTRAINT CK_AI_KQ_PhuThu CHECK(PhuThuDuKien>=0)
    );
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

    IF @HoTen IS NULL OR LEN(@HoTen)<2 OR @NgaySinh IS NULL OR @NgaySinh>=CONVERT(date,SYSDATETIME())
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
                  AND b.NgaySinh=@NgaySinh AND REPLACE(ISNULL(b.SDT,N''),N' ',N'')=REPLACE(@SDT,N' ',N''))
                THROW 55014,N'Ho so cu khong khop ten/ngay sinh/SDT. Hay doi chieu hoac sua trong Ho so benh nhan.',1;
            SET @MaBNOut=@MaBNHienCo;
        END
        ELSE
        BEGIN
            IF @TungKham=1 THROW 55015,N'Benh nhan tu khai da tung kham, phai tra cuu ho so cu truoc khi tao moi.',1;
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
PRINT N'Da cai Kiosk AI V2: AI_TaoPhieuTuBanKhai va AI_KET_QUA_TAO_PHIEU.';
SELECT OBJECT_ID(N'dbo.AI_TaoPhieuTuBanKhai',N'P') AS ThuTuc,
       OBJECT_ID(N'dbo.AI_KET_QUA_TAO_PHIEU',N'U') AS BangKetQua;
GO
