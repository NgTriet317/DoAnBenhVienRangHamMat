param([string]$Database='RHM_Sprint3_Test_Unified',[string]$MayChu='.')
$ErrorActionPreference='Stop'
if($Database -notmatch '^RHM_Sprint3_Test_[A-Za-z0-9_]+$'){throw 'Chỉ backup/restore DB fixture Sprint 3.'}
$sql=@'
SET NOCOUNT ON;
DECLARE @source sysname=N'__SOURCE__',@target sysname=N'RHM_Sprint3_Test_Restore_'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'');
DECLARE @backupFolder nvarchar(4000)=CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'));
DECLARE @dataFolder nvarchar(4000)=CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath'));
DECLARE @logFolder nvarchar(4000)=CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath'));
IF @backupFolder IS NULL OR @dataFolder IS NULL OR @logFolder IS NULL THROW 53040,N'Không tìm thấy thư mục SQL Server mặc định.',1;
IF DB_ID(@target) IS NOT NULL THROW 53041,N'Tên DB restore đã tồn tại; không ghi đè.',1;
IF (SELECT COUNT(*) FROM sys.master_files WHERE database_id=DB_ID(@source))<>2 THROW 53042,N'Fixture phải có một file dữ liệu và một file log.',1;
DECLARE @dataName sysname=(SELECT name FROM sys.master_files WHERE database_id=DB_ID(@source) AND type=0);
DECLARE @logName sysname=(SELECT name FROM sys.master_files WHERE database_id=DB_ID(@source) AND type=1);
DECLARE @backup nvarchar(4000)=@backupFolder+N'\'+@target+N'.bak';
DECLARE @data nvarchar(4000)=@dataFolder+N'\'+@target+N'.mdf',@log nvarchar(4000)=@logFolder+N'\'+@target+N'.ldf';
BACKUP DATABASE @source TO DISK=@backup WITH COPY_ONLY,CHECKSUM;
RESTORE VERIFYONLY FROM DISK=@backup WITH CHECKSUM;
RESTORE DATABASE @target FROM DISK=@backup WITH MOVE @dataName TO @data,MOVE @logName TO @log,CHECKSUM,RECOVERY;
DBCC CHECKDB(@target) WITH NO_INFOMSGS;
DECLARE @verify nvarchar(max)=N'IF EXISTS(SELECT t.name,SUM(p.rows) AS n FROM '+QUOTENAME(@source)+N'.sys.tables t JOIN '+QUOTENAME(@source)+N'.sys.partitions p ON p.object_id=t.object_id AND p.index_id IN(0,1) GROUP BY t.name EXCEPT SELECT t.name,SUM(p.rows) FROM '+QUOTENAME(@target)+N'.sys.tables t JOIN '+QUOTENAME(@target)+N'.sys.partitions p ON p.object_id=t.object_id AND p.index_id IN(0,1) GROUP BY t.name) THROW 53043,N''Restore sai số hàng.'',1;';
EXEC sys.sp_executesql @verify;
SELECT N'PASS: backup CHECKSUM, restore isolated database, CHECKDB, row-count comparison' AS Result,@target AS RestoredDatabase;
'@
$sql=$sql.Replace('__SOURCE__',$Database)
& sqlcmd -S $MayChu -d master -E -C -b -f65001 -Q $sql
if($LASTEXITCODE -ne 0){throw 'Kiểm thử khôi phục Sprint 3 thất bại.'}
