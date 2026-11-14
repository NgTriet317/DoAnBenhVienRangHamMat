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
