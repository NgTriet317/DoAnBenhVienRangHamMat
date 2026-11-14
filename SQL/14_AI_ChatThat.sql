/* MIGRATION 14 - AI chat thuc su, khong can sua WPF/SQL phieu kham V2.2.
   Cot nay chi danh dau benh nhan da duoc hoi ve dau hieu can ho tro.
   Co the chay lai an toan. */
USE [QLBenhVienRangHamMat];
GO
IF OBJECT_ID(N'dbo.AI_TIEP_NHAN_NHAP', N'U') IS NULL
    THROW 51400, N'Chua cai dat Kiosk AI migration 07. Hay cai V1-V2.2 truoc.',1;
GO
IF COL_LENGTH(N'dbo.AI_TIEP_NHAN_NHAP',N'DaXacNhanCanhBao') IS NULL
    EXEC(N'ALTER TABLE dbo.AI_TIEP_NHAN_NHAP ADD DaXacNhanCanhBao bit NOT NULL CONSTRAINT DF_AI_NHAP_XacNhanCanhBao DEFAULT(0)');
GO
PRINT N'Da cai migration 14: AI chat va kiem tra canh bao.';
SELECT COL_LENGTH(N'dbo.AI_TIEP_NHAN_NHAP',N'DaXacNhanCanhBao') AS CotCanhBao;
