-- Đơn giản hóa tình trạng bệnh: bỏ cột MucDo theo yêu cầu.
-- Sao lưu database trước khi chạy. Không thay nội dung khám, chứng từ hoặc tồn kho.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() IN(N'master',N'model',N'msdb',N'tempdb')
    THROW 52140,N'Chọn database bệnh viện trước khi nâng cấp tình trạng.',1;
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'RHM.ClinicalWorkflow.Version' AND TRY_CONVERT(int,value)=2)
    THROW 52140,N'Cần ClinicalWorkflow v2 trước khi chạy migration06.',1;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'RHM:Schema:SimpleConditions',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
IF @lock<0 THROW 52140,N'Không lấy được khóa nâng cấp tình trạng bệnh.',1;
IF COL_LENGTH(N'dbo.TINH_TRANG_BENH',N'MucDo') IS NOT NULL
BEGIN
    IF EXISTS(SELECT 1 FROM sys.index_columns WHERE object_id=OBJECT_ID(N'dbo.TINH_TRANG_BENH') AND column_id=COLUMNPROPERTY(OBJECT_ID(N'dbo.TINH_TRANG_BENH'),N'MucDo','ColumnId'))
        THROW 52140,N'Cột MucDo đang được index tùy chỉnh sử dụng; đối chiếu trước khi bỏ cột.',1;
    DECLARE @default sysname=(SELECT name FROM sys.default_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.TINH_TRANG_BENH') AND parent_column_id=COLUMNPROPERTY(OBJECT_ID(N'dbo.TINH_TRANG_BENH'),N'MucDo','ColumnId'));
    IF @default IS NOT NULL
    BEGIN
        DECLARE @sql nvarchar(max)=N'ALTER TABLE dbo.TINH_TRANG_BENH DROP CONSTRAINT '+QUOTENAME(@default);
        EXEC sys.sp_executesql @sql;
    END;
    EXEC sys.sp_executesql N'ALTER TABLE dbo.TINH_TRANG_BENH DROP COLUMN MucDo;';
END;
IF EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'RHM.SimpleConditions.Version')
    EXEC sys.sp_updateextendedproperty @name=N'RHM.SimpleConditions.Version',@value=1;
ELSE
    EXEC sys.sp_addextendedproperty @name=N'RHM.SimpleConditions.Version',@value=1;
COMMIT;
PRINT N'Đã bỏ MucDo; SimpleConditions v1. Dữ liệu các cột còn lại được giữ nguyên.';
