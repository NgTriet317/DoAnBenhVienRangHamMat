SET NOCOUNT ON;
IF DB_NAME() NOT LIKE N'RHM[_]Hospital[_]Test[_]%'
    THROW 55990,N'Only isolated hospital test databases are allowed.',1;
IF EXISTS(SELECT T FROM (VALUES
 (N'AI_PHIEN_TIEP_NHAN'),(N'AI_TIEP_NHAN_NHAP'),(N'AI_TIN_NHAN'),(N'AI_SU_KIEN'),
 (N'AI_KET_QUA_TAO_PHIEU'),(N'AI_SERVICE_IDENTITY'),(N'AI_SERVICE_INTAKE_SESSION'),(N'AI_YEU_CAU_TAO_PHIEU'),
 (N'RHM_CAU_HINH_QUY_PHEP'),(N'RHM_NGAY_PHEP_NAM')) required(T)
 WHERE OBJECT_ID(N'dbo.'+T,N'U') IS NULL)
    THROW 55991,N'Missing merged AI intake or annual leave schema.',1;
IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)<>74
    THROW 55992,N'Expected 64 Sprint3 tables plus 10 AI/leave extension tables.',1;
IF OBJECT_ID(N'dbo.AI_XacNhanPhien',N'P') IS NULL
 OR OBJECT_ID(N'dbo.AI_TraCuuLichBacSi',N'P') IS NULL
 OR OBJECT_ID(N'dbo.AI_TaoPhieuTuKiosk',N'P') IS NULL
 OR OBJECT_ID(N'dbo.AI_TaoPhieuTuBanKhai',N'P') IS NULL
 OR OBJECT_ID(N'dbo.RHM_GhiNhanQuyPhepKhiDuyet',N'P') IS NULL
 OR COL_LENGTH(N'dbo.AI_TIEP_NHAN_NHAP',N'LoaiKham') IS NULL
 OR COL_LENGTH(N'dbo.CHI_TIET_HOA_DON',N'MaPhieuPhuThu') IS NULL
 OR COL_LENGTH(N'dbo.CHI_TIET_HOA_DON',N'MaPhienAI') IS NULL
 OR COL_LENGTH(N'dbo.CHINH_SACH_LUONG',N'DonGiaCa') IS NULL
 OR COL_LENGTH(N'dbo.BANG_LUONG',N'DonGiaCa') IS NULL
    THROW 55993,N'Missing integrated workflow contracts.',1;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1)
 OR EXISTS(SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1)
 OR EXISTS(SELECT 1 FROM sys.triggers WHERE parent_class=1 AND is_disabled=1)
    THROW 55994,N'Disabled or untrusted workflow protection.',1;
EXEC dbo.usp_RHM_KiemTraSchema;
PRINT N'PASS: merged schema and runtime schema validation.';
