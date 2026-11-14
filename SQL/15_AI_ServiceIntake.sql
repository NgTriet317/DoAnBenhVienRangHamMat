/* AI intake service identity and atomic official receipt creation.
   Apply after SQL/14_AI_ChatThat.sql. Register certificate thumbprints out of band. */
USE [QLBenhVienRangHamMat];
GO
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.AI_SERVICE_IDENTITY',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_SERVICE_IDENTITY
    (
        MaAIService nvarchar(100) NOT NULL CONSTRAINT PK_AI_SERVICE_IDENTITY PRIMARY KEY,
        CertificateThumbprint char(40) NOT NULL,
        Quyen nvarchar(50) NOT NULL CONSTRAINT DF_AI_SERVICE_IDENTITY_Quyen DEFAULT(N'AI_TAO_PHIEU'),
        TrangThai bit NOT NULL CONSTRAINT DF_AI_SERVICE_IDENTITY_TrangThai DEFAULT(1),
        NgayTao datetime2(0) NOT NULL CONSTRAINT DF_AI_SERVICE_IDENTITY_NgayTao DEFAULT(SYSDATETIME()),
        CONSTRAINT UQ_AI_SERVICE_IDENTITY_Thumbprint UNIQUE(CertificateThumbprint),
        CONSTRAINT CK_AI_SERVICE_IDENTITY_Quyen CHECK(Quyen=N'AI_TAO_PHIEU')
    );
END;
GO
IF OBJECT_ID(N'dbo.AI_YEU_CAU_TAO_PHIEU',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_YEU_CAU_TAO_PHIEU
    (
        IdempotencyKey uniqueidentifier NOT NULL CONSTRAINT PK_AI_YEU_CAU_TAO_PHIEU PRIMARY KEY,
        MaPhien uniqueidentifier NOT NULL,
        PayloadHash binary(32) NOT NULL,
        MaAIService nvarchar(100) NOT NULL,
        MaBN nvarchar(50) NULL,
        MaHS nvarchar(50) NULL,
        MaPhieu nvarchar(50) NULL,
        SoThuTu int NULL,
        MaPhong nvarchar(50) NULL,
        MaBS nvarchar(50) NULL,
        LoaiKham nvarchar(20) NOT NULL,
        TrangThai nvarchar(20) NOT NULL,
        NgayTao datetime2(0) NOT NULL CONSTRAINT DF_AI_YEU_CAU_NgayTao DEFAULT(SYSDATETIME()),
        CONSTRAINT UQ_AI_YEU_CAU_TAO_PHIEU_MaPhien UNIQUE(MaPhien),
        CONSTRAINT FK_AI_YEU_CAU_Service FOREIGN KEY(MaAIService) REFERENCES dbo.AI_SERVICE_IDENTITY(MaAIService),
        CONSTRAINT FK_AI_YEU_CAU_BenhNhan FOREIGN KEY(MaBN) REFERENCES dbo.BENH_NHAN(MaBN),
        CONSTRAINT FK_AI_YEU_CAU_HoSo FOREIGN KEY(MaHS) REFERENCES dbo.HO_SO_BENH_AN(MaHS),
        CONSTRAINT FK_AI_YEU_CAU_Phieu FOREIGN KEY(MaPhieu) REFERENCES dbo.PHIEU_KHAM_BENH(MaPhieu),
        CONSTRAINT CK_AI_YEU_CAU_TrangThai CHECK(TrangThai IN(N'Created')),
        CONSTRAINT CK_AI_YEU_CAU_LoaiKham CHECK(LoaiKham IN(N'normal',N'service'))
    );
END;
GO
IF OBJECT_ID(N'dbo.AI_SERVICE_INTAKE_SESSION',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_SERVICE_INTAKE_SESSION
    (
        MaPhien uniqueidentifier NOT NULL CONSTRAINT PK_AI_SERVICE_INTAKE_SESSION PRIMARY KEY,
        MaAIService nvarchar(100) NOT NULL,
        PatientConfirmed bit NOT NULL,
        CanHoTro bit NOT NULL,
        TrangThai nvarchar(20) NOT NULL,
        NgayTao datetime2(0) NOT NULL CONSTRAINT DF_AI_SERVICE_SESSION_NgayTao DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_AI_SERVICE_SESSION_Service FOREIGN KEY(MaAIService) REFERENCES dbo.AI_SERVICE_IDENTITY(MaAIService),
        CONSTRAINT CK_AI_SERVICE_SESSION_Confirmed CHECK(PatientConfirmed=1 AND CanHoTro=0),
        CONSTRAINT CK_AI_SERVICE_SESSION_Status CHECK(TrangThai IN(N'Created'))
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AI_YEU_CAU_Session')
    ALTER TABLE dbo.AI_YEU_CAU_TAO_PHIEU WITH CHECK ADD CONSTRAINT FK_AI_YEU_CAU_Session FOREIGN KEY(MaPhien) REFERENCES dbo.AI_SERVICE_INTAKE_SESSION(MaPhien);
IF COL_LENGTH(N'dbo.PHIEU_KHAM_BENH',N'NguonTiepNhan') IS NULL
    ALTER TABLE dbo.PHIEU_KHAM_BENH ADD NguonTiepNhan nvarchar(20) NOT NULL CONSTRAINT DF_PHIEU_NguonTiepNhan DEFAULT(N'NhanVien');
IF COL_LENGTH(N'dbo.PHIEU_KHAM_BENH',N'MaPhienAI') IS NULL
    ALTER TABLE dbo.PHIEU_KHAM_BENH ADD MaPhienAI uniqueidentifier NULL;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_PHIEU_KHAM_BENH_MaNVTiepNhan')
    ALTER TABLE dbo.PHIEU_KHAM_BENH DROP CONSTRAINT FK_PHIEU_KHAM_BENH_MaNVTiepNhan;
IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.PHIEU_KHAM_BENH') AND name=N'MaNVTiepNhan' AND is_nullable=0)
    ALTER TABLE dbo.PHIEU_KHAM_BENH ALTER COLUMN MaNVTiepNhan nvarchar(50) NULL;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_PHIEU_KHAM_BENH_MaNVTiepNhan')
    ALTER TABLE dbo.PHIEU_KHAM_BENH WITH CHECK ADD CONSTRAINT FK_PHIEU_KHAM_BENH_MaNVTiepNhan FOREIGN KEY(MaNVTiepNhan) REFERENCES dbo.NHAN_VIEN(MaNV);
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_PHIEU_KHAM_BENH_AI_SESSION')
    ALTER TABLE dbo.PHIEU_KHAM_BENH ADD CONSTRAINT FK_PHIEU_KHAM_BENH_AI_SESSION FOREIGN KEY(MaPhienAI) REFERENCES dbo.AI_SERVICE_INTAKE_SESSION(MaPhien);
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_PHIEU_KHAM_BENH_NguonTiepNhan')
    ALTER TABLE dbo.PHIEU_KHAM_BENH ADD CONSTRAINT CK_PHIEU_KHAM_BENH_NguonTiepNhan CHECK
        ((NguonTiepNhan=N'NhanVien' AND MaNVTiepNhan IS NOT NULL AND MaPhienAI IS NULL)
         OR (NguonTiepNhan=N'AI' AND MaNVTiepNhan IS NULL AND MaPhienAI IS NOT NULL));
IF COL_LENGTH(N'dbo.NHAT_KY_THAO_TAC',N'MaAIService') IS NULL
    ALTER TABLE dbo.NHAT_KY_THAO_TAC ADD MaAIService nvarchar(100) NULL;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_NHAT_KY_THAO_TAC_MaTK')
    ALTER TABLE dbo.NHAT_KY_THAO_TAC DROP CONSTRAINT FK_NHAT_KY_THAO_TAC_MaTK;
IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.NHAT_KY_THAO_TAC') AND name=N'MaTK' AND is_nullable=0)
    ALTER TABLE dbo.NHAT_KY_THAO_TAC ALTER COLUMN MaTK nvarchar(50) NULL;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_NHAT_KY_THAO_TAC_MaTK')
    ALTER TABLE dbo.NHAT_KY_THAO_TAC WITH CHECK ADD CONSTRAINT FK_NHAT_KY_THAO_TAC_MaTK FOREIGN KEY(MaTK) REFERENCES dbo.TAI_KHOAN(MaTK);
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_NHAT_KY_THAO_TAC_AI_SERVICE')
    ALTER TABLE dbo.NHAT_KY_THAO_TAC ADD CONSTRAINT FK_NHAT_KY_THAO_TAC_AI_SERVICE FOREIGN KEY(MaAIService) REFERENCES dbo.AI_SERVICE_IDENTITY(MaAIService);
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_NHAT_KY_THAO_TAC_Actor')
    ALTER TABLE dbo.NHAT_KY_THAO_TAC ADD CONSTRAINT CK_NHAT_KY_THAO_TAC_Actor CHECK
        ((MaTK IS NOT NULL AND MaAIService IS NULL)
         OR (MaTK IS NULL AND MaAIService IS NOT NULL)
         OR (MaTK IS NULL AND MaAIService IS NULL AND HanhDong<>N'Tạo phiếu khám từ AI'));
IF COL_LENGTH(N'dbo.CHI_TIET_HOA_DON',N'MaPhienAI') IS NULL
    ALTER TABLE dbo.CHI_TIET_HOA_DON ADD MaPhienAI uniqueidentifier NULL;
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_CHI_TIET_HOA_DON_3')
    ALTER TABLE dbo.CHI_TIET_HOA_DON DROP CONSTRAINT CK_CHI_TIET_HOA_DON_3;
ALTER TABLE dbo.CHI_TIET_HOA_DON WITH CHECK ADD CONSTRAINT CK_CHI_TIET_HOA_DON_3 CHECK
    ((MaDVThucHien IS NOT NULL AND MaCTPX IS NULL AND MaCTDT IS NULL AND MaPhienAI IS NULL AND DonGia IS NULL)
     OR (MaDVThucHien IS NULL AND MaCTPX IS NOT NULL AND MaCTDT IS NULL AND MaPhienAI IS NULL AND DonGia IS NOT NULL AND DonGia>=0)
     OR (MaDVThucHien IS NULL AND MaCTPX IS NULL AND MaCTDT IS NOT NULL AND MaPhienAI IS NULL AND DonGia IS NOT NULL AND DonGia>=0)
     OR (MaDVThucHien IS NULL AND MaCTPX IS NULL AND MaCTDT IS NULL AND MaPhienAI IS NOT NULL AND DonGia=30000));
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_CTHD_AI_ServiceSession')
    ALTER TABLE dbo.CHI_TIET_HOA_DON WITH CHECK ADD CONSTRAINT FK_CTHD_AI_ServiceSession FOREIGN KEY(MaPhienAI) REFERENCES dbo.AI_SERVICE_INTAKE_SESSION(MaPhien);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CHI_TIET_HOA_DON') AND name=N'UX_CTHD_AI_ServiceSession')
    CREATE UNIQUE INDEX UX_CTHD_AI_ServiceSession ON dbo.CHI_TIET_HOA_DON(MaPhienAI) WHERE MaPhienAI IS NOT NULL;
GO
CREATE OR ALTER TRIGGER dbo.TR_CTHD_TinhTien ON dbo.CHI_TIET_HOA_DON
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.TR_CTHD_TinhTien'))>1 RETURN;
    DECLARE @lock nvarchar(50);
    SELECT @lock=h.MaHD FROM dbo.HOA_DON h WITH(UPDLOCK,HOLDLOCK)
     WHERE h.MaHD IN(SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted);
    IF EXISTS(SELECT 1 FROM dbo.HOA_DON h JOIN(SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted) z ON z.MaHD=h.MaHD WHERE h.TrangThai<>N'Nháp')
        THROW 51001,N'Chỉ thay đổi chi tiết hóa đơn nháp.',1;
    IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.HOA_DON h ON h.MaHD=i.MaHD
      LEFT JOIN dbo.CHI_TIET_PHIEU_XUAT x ON x.MaCTPX=i.MaCTPX
      WHERE h.TaoKhiHoanTat=1 AND (i.MaCTPX IS NULL OR NOT EXISTS(
        SELECT 1 FROM dbo.CHI_TIET_HOA_DON pending WHERE pending.MaHD=i.MaHD AND pending.MaCTDT=x.MaCTDT
          AND pending.DonGia=i.DonGia AND pending.NoiDung=i.NoiDung)))
        THROW 52231,N'Hóa đơn đã chốt nguồn; chỉ thay thuốc theo đơn bằng đúng phần xuất kho.',1;
    IF EXISTS(SELECT 1 FROM deleted d JOIN dbo.HOA_DON h ON h.MaHD=d.MaHD
      LEFT JOIN dbo.CHI_TIET_DON_THUOC rx ON rx.MaCTDT=d.MaCTDT
      OUTER APPLY(SELECT SUM(x.SoLuong) Qty,SUM(c.ThanhTien) Amount FROM dbo.CHI_TIET_HOA_DON c
        JOIN dbo.CHI_TIET_PHIEU_XUAT x ON x.MaCTPX=c.MaCTPX WHERE c.MaHD=d.MaHD AND x.MaCTDT=d.MaCTDT) a
      WHERE h.TaoKhiHoanTat=1 AND (d.MaCTDT IS NULL OR EXISTS(SELECT 1 FROM inserted i WHERE i.MaCTHD=d.MaCTHD)
        OR COALESCE(a.Qty,0)<>rx.SoLuong OR COALESCE(a.Amount,0)<>d.ThanhTien))
        THROW 52232,N'Không xóa/sửa khoản đã chốt; phần xuất phải khớp toàn bộ lượng và tiền đơn thuốc.',1;
    IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.HOA_DON h ON h.MaHD=i.MaHD
      LEFT JOIN dbo.DICH_VU_THUC_HIEN s ON s.MaDVThucHien=i.MaDVThucHien
      LEFT JOIN dbo.CHI_TIET_PHIEU_XUAT x ON x.MaCTPX=i.MaCTPX
      LEFT JOIN dbo.PHIEU_XUAT p ON p.MaPX=x.MaPX
      LEFT JOIN dbo.CHI_TIET_DON_THUOC d ON d.MaCTDT=COALESCE(i.MaCTDT,x.MaCTDT)
      LEFT JOIN dbo.DON_THUOC r ON r.MaDT=d.MaDT
      LEFT JOIN dbo.AI_YEU_CAU_TAO_PHIEU ai ON ai.MaPhien=i.MaPhienAI
      LEFT JOIN dbo.PHIEU_KHAM_BENH ap ON ap.MaPhieu=ai.MaPhieu
      LEFT JOIN dbo.LICH_SU_KHAM_BENH al ON al.MaPhieu=ap.MaPhieu
      WHERE (i.MaDVThucHien IS NOT NULL AND(s.MaLS<>h.MaLS OR s.TrangThai<>N'Đã xác nhận'))
       OR(i.MaCTPX IS NOT NULL AND(x.MaCTDT IS NULL OR p.MaLS<>h.MaLS OR r.MaLS<>h.MaLS OR p.TrangThai<>N'Đã ghi sổ'))
       OR(i.MaCTDT IS NOT NULL AND(r.MaLS<>h.MaLS OR r.TrangThai<>N'Đã xác nhận' OR r.NgayKe>h.NgayLap
         OR NULLIF(LTRIM(RTRIM(d.TenThuocChot)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(d.HamLuongChot)),N'') IS NULL
         OR NULLIF(LTRIM(RTRIM(d.DonViTinhChot)),N'') IS NULL))
       OR(i.MaPhienAI IS NOT NULL AND(ai.MaPhien IS NULL OR ai.LoaiKham<>N'service' OR al.MaLS IS NULL OR al.MaLS<>h.MaLS OR i.DonGia<>30000)))
        THROW 51002,N'Nguồn thu không hợp lệ với hóa đơn hoặc phiên AI.',1;
    IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.HOA_DON h ON h.MaHD=i.MaHD
      JOIN dbo.CHI_TIET_PHIEU_XUAT x ON x.MaCTPX=i.MaCTPX JOIN dbo.CHI_TIET_DON_THUOC rx ON rx.MaCTDT=x.MaCTDT
      WHERE h.TaoKhiHoanTat=1 AND(SELECT SUM(y.SoLuong) FROM dbo.CHI_TIET_HOA_DON c
        JOIN dbo.CHI_TIET_PHIEU_XUAT y ON y.MaCTPX=c.MaCTPX WHERE c.MaHD=i.MaHD AND y.MaCTDT=x.MaCTDT)>rx.SoLuong)
        THROW 52233,N'Phần xuất vượt lượng đã chốt.',1;
    UPDATE c SET ThanhTien=CASE WHEN c.MaPhienAI IS NOT NULL THEN 30000 WHEN c.MaDVThucHien IS NOT NULL THEN s.ThanhTien
      ELSE CAST(ROUND(COALESCE(x.SoLuong,d.SoLuong)*c.DonGia,2) AS decimal(18,2)) END
      FROM dbo.CHI_TIET_HOA_DON c JOIN inserted i ON i.MaCTHD=c.MaCTHD
      LEFT JOIN dbo.DICH_VU_THUC_HIEN s ON s.MaDVThucHien=c.MaDVThucHien
      LEFT JOIN dbo.CHI_TIET_PHIEU_XUAT x ON x.MaCTPX=c.MaCTPX
      LEFT JOIN dbo.CHI_TIET_DON_THUOC d ON d.MaCTDT=c.MaCTDT;
    IF EXISTS(SELECT 1 FROM dbo.HOA_DON h WHERE h.MaHD IN(SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted)
      AND h.GiamGia>COALESCE((SELECT SUM(c.ThanhTien) FROM dbo.CHI_TIET_HOA_DON c WHERE c.MaHD=h.MaHD),0))
        THROW 51003,N'Tổng mới thấp hơn giảm giá.',1;
    UPDATE h SET TongTien=COALESCE((SELECT SUM(c.ThanhTien) FROM dbo.CHI_TIET_HOA_DON c WHERE c.MaHD=h.MaHD),0)
      FROM dbo.HOA_DON h WHERE h.MaHD IN(SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted);
END;
GO
GO
CREATE OR ALTER PROCEDURE dbo.AI_TaoPhieuTuKiosk
    @CertificateThumbprint char(40),
    @MaPhien uniqueidentifier, @IdempotencyKey uniqueidentifier, @PayloadHash binary(32),
    @HoTen nvarchar(255), @NgaySinh date, @SDT nvarchar(25), @TungKham nvarchar(20),
    @LyDoKham nvarchar(1000), @LoaiKham nvarchar(20),
    @MaBSYeuCau nvarchar(50)=NULL, @MaDV nvarchar(50)=NULL, @MaPhongYeuCau nvarchar(50)=NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    SET @HoTen=NULLIF(LTRIM(RTRIM(@HoTen)),N''); SET @SDT=NULLIF(LTRIM(RTRIM(@SDT)),N'');
    SET @LyDoKham=NULLIF(LTRIM(RTRIM(@LyDoKham)),N'');
    IF @IdempotencyKey IS NULL OR @MaPhien IS NULL OR @PayloadHash IS NULL OR @HoTen IS NULL OR LEN(@HoTen)<2
       OR @NgaySinh IS NULL OR @NgaySinh>CONVERT(date,SYSDATETIME()) OR @SDT IS NULL OR LEN(@SDT)<9 OR LEN(@SDT)>15
       OR @SDT LIKE N'%[^0-9+]%' OR @LyDoKham IS NULL OR @TungKham IS NULL OR @TungKham NOT IN(N'yes',N'no',N'unknown')
       OR @LoaiKham NOT IN(N'normal',N'service')
        THROW 55101,N'Dữ liệu tiếp nhận chưa hợp lệ.',1;
    IF @LoaiKham=N'service' AND (@MaBSYeuCau IS NULL OR @MaDV IS NULL)
        THROW 55102,N'Khám dịch vụ cần chọn bác sĩ và dịch vụ.',1;
    IF @LoaiKham=N'normal' AND @MaDV IS NOT NULL
        THROW 55103,N'Khám thông thường không nhận mã dịch vụ.',1;

    BEGIN TRY
      BEGIN TRANSACTION;
      DECLARE @service nvarchar(100);
      SELECT @service=MaAIService FROM dbo.AI_SERVICE_IDENTITY WITH(UPDLOCK,HOLDLOCK)
       WHERE CertificateThumbprint=@CertificateThumbprint
         AND Quyen=N'AI_TAO_PHIEU' AND TrangThai=1;
      IF @service IS NULL THROW 55104,N'Thiết bị chưa được cấp quyền AI_TAO_PHIEU.',1;

      DECLARE @existing uniqueidentifier,@oldHash binary(32),@oldSession uniqueidentifier,@maPhieu nvarchar(50);
      SELECT @existing=IdempotencyKey,@oldHash=PayloadHash,@oldSession=MaPhien,@maPhieu=MaPhieu
        FROM dbo.AI_YEU_CAU_TAO_PHIEU WITH(UPDLOCK,HOLDLOCK) WHERE IdempotencyKey=@IdempotencyKey;
      IF @existing IS NULL
        SELECT @existing=IdempotencyKey,@oldHash=PayloadHash,@oldSession=MaPhien,@maPhieu=MaPhieu
          FROM dbo.AI_YEU_CAU_TAO_PHIEU WITH(UPDLOCK,HOLDLOCK) WHERE MaPhien=@MaPhien;
      IF @existing IS NOT NULL
      BEGIN
        IF @existing<>@IdempotencyKey OR @oldSession<>@MaPhien OR @oldHash<>@PayloadHash
          THROW 55105,N'Phiên đã có yêu cầu tạo phiếu khác hoặc dữ liệu đã thay đổi.',1;
        SELECT N'created' AS Status,p.MaPhieu,p.SoThuTu,p.MaPhong,p.MaBS,
               ph.TenPhong AS Phong,nv.HoTen AS BacSi,
               TODATETIMEOFFSET(p.ThoiGianTiepNhan,DATEPART(TZOFFSET,SYSDATETIMEOFFSET())) AS ThoiGianTiepNhan,
               p.TrangThai,CONVERT(bit,0) RequiresStaff
          FROM dbo.PHIEU_KHAM_BENH p JOIN dbo.PHONG ph ON ph.MaPhong=p.MaPhong
          LEFT JOIN dbo.NHAN_VIEN nv ON nv.MaNV=p.MaBS WHERE p.MaPhieu=@maPhieu;
        COMMIT; RETURN;
      END;

      DECLARE @sessionService nvarchar(100),@confirmed bit,@urgent bit,@sessionStatus nvarchar(20),@now datetime2(0)=SYSDATETIME();
      SELECT @sessionService=MaAIService,@confirmed=PatientConfirmed,@urgent=CanHoTro,@sessionStatus=TrangThai
        FROM dbo.AI_SERVICE_INTAKE_SESSION WITH(UPDLOCK,HOLDLOCK) WHERE MaPhien=@MaPhien;
      IF @sessionService IS NOT NULL AND @sessionService<>@service
        THROW 55106,N'Phiên thuộc thiết bị AI khác.',1;
      IF @sessionService IS NOT NULL AND @sessionStatus=N'Created'
      BEGIN
        SELECT N'created' AS Status,p.MaPhieu,p.SoThuTu,p.MaPhong,p.MaBS,ph.TenPhong AS Phong,
               nv.HoTen AS BacSi,TODATETIMEOFFSET(p.ThoiGianTiepNhan,DATEPART(TZOFFSET,SYSDATETIMEOFFSET())) AS ThoiGianTiepNhan,
               p.TrangThai,CONVERT(bit,0) RequiresStaff
          FROM dbo.AI_SERVICE_INTAKE_SESSION s JOIN dbo.AI_YEU_CAU_TAO_PHIEU r ON r.MaPhien=s.MaPhien
          JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=r.MaPhieu JOIN dbo.PHONG ph ON ph.MaPhong=p.MaPhong
          LEFT JOIN dbo.NHAN_VIEN nv ON nv.MaNV=p.MaBS WHERE s.MaPhien=@MaPhien AND r.IdempotencyKey=@IdempotencyKey AND r.PayloadHash=@PayloadHash;
        IF @@ROWCOUNT=0 THROW 55105,N'Phiên đã hoàn tất với yêu cầu khác.',1;
        COMMIT; RETURN;
      END;
      IF @sessionService IS NULL
        INSERT dbo.AI_SERVICE_INTAKE_SESSION(MaPhien,MaAIService,PatientConfirmed,CanHoTro,TrangThai)
        VALUES(@MaPhien,@service,1,0,N'Created');
      ELSE IF @confirmed<>1 OR @urgent<>0 OR @sessionStatus<>N'Created'
        THROW 55106,N'Phiên không được xác nhận hoặc cần nhân viên hỗ trợ.',1;

      DECLARE @doctor nvarchar(50)=@MaBSYeuCau,@room nvarchar(50)=@MaPhongYeuCau,
              @specialty nvarchar(50),@shiftRoom nvarchar(50),@begin datetime2(0),@end datetime2(0);
      IF @doctor IS NULL AND @LoaiKham=N'normal'
      BEGIN
        SELECT TOP(1) @doctor=n.MaNV,@room=lv.MaPhong,@begin=DATEADD(second,DATEDIFF(second,CAST('00:00:00' AS time),c.GioBatDau),CAST(lv.NgayLam AS datetime2(0))),
          @end=DATEADD(second,DATEDIFF(second,CAST('00:00:00' AS time),c.GioKetThuc),CAST(lv.NgayLam AS datetime2(0)))
        FROM dbo.NHAN_VIEN n JOIN dbo.LICH_LAM_VIEC lv ON lv.MaNV=n.MaNV JOIN dbo.CA_LAM c ON c.MaCa=lv.MaCa
        JOIN dbo.PHONG ph ON ph.MaPhong=lv.MaPhong AND ph.MaKhoa=n.MaCK AND ph.TrangThai=N'Đang hoạt động'
        WHERE n.MaChucVu=N'CV002' AND n.TrangThai=N'Đang làm việc' AND lv.NgayLam=CONVERT(date,@now)
          AND lv.TrangThai=N'Đã xác nhận' AND lv.TrangThaiHienDien NOT IN(N'Vắng mặt',N'Nghỉ phép')
          AND c.TrangThai=N'Đang hoạt động'
          AND @now>=DATEADD(second,DATEDIFF(second,CAST('00:00:00' AS time),c.GioBatDau),CAST(lv.NgayLam AS datetime2(0)))
          AND @now<DATEADD(day,CASE WHEN c.GioKetThuc<=c.GioBatDau THEN 1 ELSE 0 END,
            DATEADD(second,DATEDIFF(second,CAST('00:00:00' AS time),c.GioKetThuc),CAST(lv.NgayLam AS datetime2(0))))
          AND ph.LoaiPhong LIKE N'Phòng khám%'
          AND NOT EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI d WHERE d.MaNV=n.MaNV AND d.TrangThaiDuyet=N'Đã duyệt' AND CONVERT(date,@now) BETWEEN d.TuNgay AND d.DenNgay)
          AND EXISTS(SELECT 1 FROM dbo.TAI_KHOAN tk WHERE tk.MaNV=n.MaNV AND tk.TrangThai=N'Đang hoạt động')
        ORDER BY (SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH q WHERE q.MaBS=n.MaNV AND q.ThoiGianTiepNhan>=CONVERT(date,@now) AND q.ThoiGianTiepNhan<DATEADD(day,1,CONVERT(date,@now))),n.MaNV;
      END
      ELSE
      BEGIN
        SELECT @shiftRoom=lv.MaPhong,@specialty=n.MaCK
          FROM dbo.NHAN_VIEN n JOIN dbo.LICH_LAM_VIEC lv ON lv.MaNV=n.MaNV JOIN dbo.CA_LAM c ON c.MaCa=lv.MaCa
         WHERE n.MaNV=@doctor AND n.MaChucVu=N'CV002' AND n.TrangThai=N'Đang làm việc'
           AND lv.NgayLam=CONVERT(date,@now) AND lv.TrangThai=N'Đã xác nhận'
           AND lv.TrangThaiHienDien NOT IN(N'Vắng mặt',N'Nghỉ phép') AND c.TrangThai=N'Đang hoạt động'
           AND @now>=DATEADD(second,DATEDIFF(second,CAST('00:00:00' AS time),c.GioBatDau),CAST(lv.NgayLam AS datetime2(0)))
           AND @now<DATEADD(day,CASE WHEN c.GioKetThuc<=c.GioBatDau THEN 1 ELSE 0 END,
             DATEADD(second,DATEDIFF(second,CAST('00:00:00' AS time),c.GioKetThuc),CAST(lv.NgayLam AS datetime2(0))))
           AND NOT EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI d WHERE d.MaNV=n.MaNV AND d.TrangThaiDuyet=N'Đã duyệt' AND CONVERT(date,@now) BETWEEN d.TuNgay AND d.DenNgay);
        IF @doctor IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.TAI_KHOAN tk WHERE tk.MaNV=@doctor AND tk.TrangThai=N'Đang hoạt động')
            THROW 55108,N'Bác sĩ không còn tài khoản hoạt động; vui lòng chọn lại.',1;
        IF @MaBSYeuCau IS NOT NULL AND @shiftRoom IS NULL
            THROW 55108,N'Bác sĩ vừa hết ca hoặc không còn lịch làm việc; vui lòng chọn lại.',1;
        IF @shiftRoom IS NOT NULL AND @MaPhongYeuCau IS NOT NULL AND @MaPhongYeuCau<>@shiftRoom
            THROW 55110,N'Phòng làm việc vừa thay đổi; vui lòng chọn lại.',1;
        SET @room=COALESCE(@room,@shiftRoom);
      END;
      IF @doctor IS NULL OR @room IS NULL THROW 55108,N'Không có bác sĩ/phòng phù hợp đang làm việc; vui lòng gặp nhân viên.',1;
      IF @LoaiKham=N'service' AND NOT EXISTS(SELECT 1 FROM dbo.DICH_VU d WHERE d.MaDV=@MaDV AND d.MaCK=@specialty AND d.TrangThai=N'Đang hoạt động' AND d.NgayApDung<=CONVERT(date,@now))
        THROW 55109,N'Dịch vụ không còn phù hợp với bác sĩ đã chọn.',1;
      IF @MaPhongYeuCau IS NOT NULL AND @MaPhongYeuCau<>@room THROW 55110,N'Phòng làm việc vừa thay đổi; vui lòng chọn lại.',1;

      DECLARE @phoneNorm nvarchar(50)=REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@SDT,N' ',N''),N'-',N''),N'.',N''),N'(',N''),N')',N''),
              @candidates int,@exact int,@bn nvarchar(50),@hs nvarchar(50),@phieu nvarchar(50),@order int;
      DECLARE @patientLock int;
      EXEC @patientLock=sp_getapplock @Resource=N'RHM_AI_SERVICE_PATIENT',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
      IF @patientLock<0 THROW 55113,N'Không khóa được đối chiếu bệnh nhân, vui lòng thử lại.',1;
      DECLARE @matches TABLE(MaBN nvarchar(50) PRIMARY KEY,ExactMatch bit);
      INSERT @matches SELECT b.MaBN,CONVERT(bit,CASE WHEN b.HoTenBN=@HoTen AND b.NgaySinh=@NgaySinh AND
          REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(b.SDT,N''),N' ',N''),N'-',N''),N'.',N''),N'(',N''),N')',N'')=@phoneNorm THEN 1 ELSE 0 END)
        FROM dbo.BENH_NHAN b WITH(UPDLOCK,HOLDLOCK)
       WHERE b.HoTenBN=@HoTen AND b.NgaySinh=@NgaySinh OR REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(b.SDT,N''),N' ',N''),N'-',N''),N'.',N''),N'(',N''),N')',N'')=@phoneNorm;
      SELECT @candidates=COUNT(*),@exact=COALESCE(SUM(CONVERT(int,ExactMatch)),0) FROM @matches;
      IF @candidates=1 AND @exact=1
      BEGIN
        SELECT @bn=m.MaBN FROM @matches m JOIN dbo.BENH_NHAN b ON b.MaBN=m.MaBN WHERE b.TrangThai=N'Đang theo dõi';
        IF @bn IS NULL THROW 55111,N'Hồ sơ phù hợp hiện không hoạt động; cần nhân viên đối chiếu.',1;
      END
      ELSE IF @candidates=0 AND @TungKham=N'no'
      BEGIN SET @bn=N'BN_AI_'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
        INSERT dbo.BENH_NHAN(MaBN,HoTenBN,GioiTinh,NgaySinh,SDT,DiaChi,MaBHYT,NhomMau,TienSuBenh,DiUng,TrangThai)
        VALUES(@bn,@HoTen,NULL,@NgaySinh,@SDT,NULL,NULL,NULL,NULL,NULL,N'Đang theo dõi'); END
      ELSE IF @candidates=0
        THROW 55111,N'Chưa tìm được hồ sơ khớp với lời khai đã từng khám; cần nhân viên đối chiếu.',1;
      ELSE THROW 55111,N'Cần nhân viên đối chiếu hồ sơ trùng hoặc thông tin không khớp.',1;

      SELECT @hs=MaHS FROM dbo.HO_SO_BENH_AN WITH(UPDLOCK,HOLDLOCK) WHERE MaBN=@bn;
      IF @hs IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.HO_SO_BENH_AN WHERE MaHS=@hs AND TrangThai=N'Đang theo dõi')
        THROW 55111,N'Hồ sơ bệnh án hiện không hoạt động; cần nhân viên đối chiếu.',1;
      IF @hs IS NULL BEGIN SET @hs=N'HS_AI_'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
        INSERT dbo.HO_SO_BENH_AN(MaHS,MaBN,NgayLap,LyDoKhamBanDau,GhiChu,TrangThai)
        VALUES(@hs,@bn,@now,@LyDoKham,N'Tiếp nhận qua service AI.',N'Đang theo dõi'); END;
      IF EXISTS(SELECT 1 FROM dbo.PHIEU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaHS=@hs AND TrangThai<>N'Hủy' AND ThoiGianTiepNhan>=CONVERT(date,@now) AND ThoiGianTiepNhan<DATEADD(day,1,CONVERT(date,@now)))
        THROW 55112,N'Bệnh nhân đã có phiếu trong ngày; vui lòng gặp nhân viên đối chiếu.',1;

      DECLARE @queueLock int,@resource nvarchar(255)=N'RHM:TiepNhan:Queue:'+CONVERT(nvarchar(8),@now,112)+N':'+@room;
      EXEC @queueLock=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
      IF @queueLock<0 THROW 55113,N'Không cấp được số thứ tự, vui lòng thử lại.',1;
      SELECT @order=COALESCE(MAX(SoThuTu),0)+1 FROM dbo.PHIEU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaPhong=@room AND ThoiGianTiepNhan>=CONVERT(date,@now) AND ThoiGianTiepNhan<DATEADD(day,1,CONVERT(date,@now));
      SET @phieu=N'PKB_AI_'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
      INSERT dbo.PHIEU_KHAM_BENH(MaPhieu,MaHS,MaLich,MaPhong,MaBS,MaNVTiepNhan,MaDV,SoThuTu,ThoiGianTiepNhan,LyDoKham,TrangThai,GhiChu,NguonTiepNhan,MaPhienAI)
      VALUES(@phieu,@hs,NULL,@room,@doctor,NULL,@MaDV,@order,@now,@LyDoKham,N'Chờ khám',N'Tiếp nhận bởi service AI.',N'AI',@MaPhien);
      INSERT dbo.AI_YEU_CAU_TAO_PHIEU(IdempotencyKey,MaPhien,PayloadHash,MaAIService,MaBN,MaHS,MaPhieu,SoThuTu,MaPhong,MaBS,LoaiKham,TrangThai)
      VALUES(@IdempotencyKey,@MaPhien,@PayloadHash,@service,@bn,@hs,@phieu,@order,@room,@doctor,@LoaiKham,N'Created');
      INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,MaAIService,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuSau)
      VALUES(N'NK'+CONVERT(nvarchar(36),NEWID()),NULL,@service,@now,N'Tạo phiếu khám từ AI',N'PHIEU_KHAM_BENH',@phieu,N'Thành công',
          (SELECT @MaPhien AS MaPhien,@bn AS MaBN,@hs AS MaHS,@phieu AS MaPhieu,@order AS SoThuTu,@room AS MaPhong,@doctor AS MaBS,@LoaiKham AS LoaiKham FOR JSON PATH,WITHOUT_ARRAY_WRAPPER));
      COMMIT;
      SELECT N'created' AS Status,p.MaPhieu,p.SoThuTu,p.MaPhong,p.MaBS,ph.TenPhong AS Phong,nv.HoTen AS BacSi,
             TODATETIMEOFFSET(p.ThoiGianTiepNhan,DATEPART(TZOFFSET,SYSDATETIMEOFFSET())) AS ThoiGianTiepNhan,
             p.TrangThai,CONVERT(bit,0) RequiresStaff
        FROM dbo.PHIEU_KHAM_BENH p JOIN dbo.PHONG ph ON ph.MaPhong=p.MaPhong LEFT JOIN dbo.NHAN_VIEN nv ON nv.MaNV=p.MaBS WHERE p.MaPhieu=@phieu;
    END TRY
    BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH
END;
GO
PRINT N'Đã cài đặt identity service AI, chống trùng idempotency và tạo phiếu trong transaction.';
GO
