-- Nâng cấp SQL V9 để duyệt chấm công. Dùng trên DB đang chạy, KHÔNG xóa dữ liệu.
-- Chạy một lần trong SSMS trên database TEST đã kiểm tra; sửa tên database ở SSMS trước khi chạy.
-- Không thực hiện tính lại hoặc chốt BANG_LUONG.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID('dbo.CHAM_CONG','U') IS NULL OR OBJECT_ID('dbo.BANG_LUONG','U') IS NULL
       OR OBJECT_ID('dbo.LICH_LAM_VIEC','U') IS NULL OR OBJECT_ID('dbo.CA_LAM','U') IS NULL
        THROW 51110,N'Database thiếu bảng SQL V9. Không thể cài nâng cấp duyệt công.',1;

    IF OBJECT_ID('dbo.NHAT_KY_DUYET_CHAM_CONG','U') IS NULL
    BEGIN
        CREATE TABLE dbo.NHAT_KY_DUYET_CHAM_CONG
        (
            MaNhatKy BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            MaLLV NVARCHAR(50) NOT NULL REFERENCES dbo.LICH_LAM_VIEC(MaLLV),
            MaNV NVARCHAR(50) NOT NULL REFERENCES dbo.NHAN_VIEN(MaNV),
            NguoiThucHien NVARCHAR(50) NOT NULL REFERENCES dbo.NHAN_VIEN(MaNV),
            NgayThucHien DATETIME2(0) NOT NULL,
            TrangThaiCu NVARCHAR(50) NOT NULL,
            TrangThaiMoi NVARCHAR(50) NOT NULL,
            GioVaoCu DATETIME2(0) NULL,
            GioRaCu DATETIME2(0) NULL,
            GioVaoMoi DATETIME2(0) NULL,
            GioRaMoi DATETIME2(0) NULL,
            CongCu DECIMAL(4,2) NOT NULL,
            CongMoi DECIMAL(4,2) NOT NULL,
            LyDo NVARCHAR(500) NOT NULL
        );
        CREATE INDEX IX_NKCC_Lich_Ngay ON dbo.NHAT_KY_DUYET_CHAM_CONG(MaLLV,NgayThucHien DESC);
    END;
    EXEC sys.sp_executesql N'
    CREATE OR ALTER TRIGGER dbo.TR_NKCC_KhongSua ON dbo.NHAT_KY_DUYET_CHAM_CONG
    AFTER UPDATE, DELETE AS
    BEGIN
      SET NOCOUNT ON;
      THROW 51111,N''Nhật ký đối soát chỉ được thêm, không được sửa hay xóa.'',1;
    END';
    EXEC sys.sp_executesql N'
    CREATE OR ALTER TRIGGER dbo.TR_CC_KhoaKyDaChot ON dbo.CHAM_CONG
    AFTER INSERT,UPDATE,DELETE AS
    BEGIN
      SET NOCOUNT ON;
      IF EXISTS (
        SELECT 1 FROM (SELECT MaLLV FROM inserted UNION SELECT MaLLV FROM deleted) x
        JOIN dbo.LICH_LAM_VIEC l ON l.MaLLV=x.MaLLV
        JOIN dbo.BANG_LUONG b ON b.MaNV=l.MaNV AND l.NgayLam BETWEEN b.TuNgay AND b.DenNgay
        WHERE b.TrangThai=N''Đã chốt'')
        THROW 51112,N''Không sửa chấm công của kỳ lương đã chốt.'',1;
    END';
    EXEC sys.sp_executesql N'
    CREATE OR ALTER TRIGGER dbo.TR_LLV_KhoaKyDaChot ON dbo.LICH_LAM_VIEC
    AFTER INSERT,UPDATE,DELETE AS
    BEGIN
      SET NOCOUNT ON;
      IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN dbo.BANG_LUONG b ON b.MaNV=i.MaNV AND i.NgayLam BETWEEN b.TuNgay AND b.DenNgay
        LEFT JOIN deleted d ON d.MaLLV=i.MaLLV
        WHERE b.TrangThai=N''Đã chốt'' AND d.MaLLV IS NULL)
        THROW 51113,N''Không thêm lịch vào kỳ lương đã chốt.'',1;
      IF EXISTS(
        SELECT 1 FROM deleted d
        JOIN dbo.BANG_LUONG b ON b.MaNV=d.MaNV AND d.NgayLam BETWEEN b.TuNgay AND b.DenNgay
        LEFT JOIN inserted i ON i.MaLLV=d.MaLLV
        WHERE b.TrangThai=N''Đã chốt'' AND (
          i.MaLLV IS NULL OR
          ISNULL(i.MaNV,N'''')<>ISNULL(d.MaNV,N'''') OR
          i.NgayLam<>d.NgayLam OR i.MaCa<>d.MaCa OR
          ISNULL(i.TrangThai,N'''')<>ISNULL(d.TrangThai,N'''') OR
          ISNULL(i.TrangThaiHienDien,N'''')<>ISNULL(d.TrangThaiHienDien,N'''') OR
          ISNULL(i.GioVao,CONVERT(DATETIME2(0),''19000101''))<>ISNULL(d.GioVao,CONVERT(DATETIME2(0),''19000101'')) OR
          ISNULL(i.GioRa,CONVERT(DATETIME2(0),''19000101''))<>ISNULL(d.GioRa,CONVERT(DATETIME2(0),''19000101''))
        )) THROW 51113,N''Lịch chấm công thuộc kỳ lương đã chốt, không thể sửa.'',1;
    END';
    COMMIT TRANSACTION;
    PRINT N'Đã tạo nhật ký và cơ chế khóa công kỳ lương đã chốt.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
