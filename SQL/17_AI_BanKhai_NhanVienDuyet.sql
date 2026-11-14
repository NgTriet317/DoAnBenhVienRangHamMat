/*
  Restore AI draft → staff creates official receipt.
  Non-destructive: no DROP, no updates to existing clinical records.
  Prerequisite: SQL/07_KioskAI_TiepNhan.sql; for existing kiosk 14 is recommended.
*/
USE [QLBenhVienRangHamMat];
GO
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.AI_PHIEN_TIEP_NHAN',N'U') IS NULL
    OR OBJECT_ID(N'dbo.AI_TIEP_NHAN_NHAP',N'U') IS NULL
    THROW 51801, N'Chưa cài SQL/07_KioskAI_TiepNhan.sql.', 1;
GO
IF COL_LENGTH(N'dbo.AI_TIEP_NHAN_NHAP',N'LoaiKham') IS NULL
BEGIN
    ALTER TABLE dbo.AI_TIEP_NHAN_NHAP ADD LoaiKham nvarchar(20) NULL;
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_AI_NHAP_LoaiKham'
               AND parent_object_id = OBJECT_ID(N'dbo.AI_TIEP_NHAN_NHAP'))
BEGIN
    ALTER TABLE dbo.AI_TIEP_NHAN_NHAP WITH CHECK
    ADD CONSTRAINT CK_AI_NHAP_LoaiKham
        CHECK (LoaiKham IS NULL OR LoaiKham IN(N'ThongThuong', N'DichVu'));
END;
GO
SELECT N'AI_TIEP_NHAN_NHAP.LoaiKham' AS TenCot,
       CASE WHEN COL_LENGTH(N'dbo.AI_TIEP_NHAN_NHAP',N'LoaiKham') IS NOT NULL
            THEN N'OK' ELSE N'THIEU' END AS TrangThai;
GO
