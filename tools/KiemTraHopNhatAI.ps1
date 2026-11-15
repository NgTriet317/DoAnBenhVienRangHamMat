<# Kiểm tra installer hợp nhất trên database cách ly: cài mới, giữ dữ liệu khi chạy lại. #>
param(
    [string]$Database = 'RHM_Hospital_Test_MergeSql_20261115',
    [string]$MayChu = 'lpc:localhost',
    [string]$SqlFile = '',
    [switch]$ChiKiemTra,
    [switch]$KhoiPhucDangDo
)
$ErrorActionPreference = 'Stop'
if ($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$') { throw 'Chỉ dùng database kiểm thử RHM_Hospital_Test_.' }
$repo = Split-Path $PSScriptRoot -Parent
if (!$SqlFile) { $SqlFile = Join-Path $repo 'Database/QLBenhVienRangHamMat.sql' }
$sqlcmd = (Get-Command sqlcmd -ErrorAction SilentlyContinue).Source
if (!$sqlcmd) { $sqlcmd = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE' }
$scratch = Join-Path $repo 'obj/MergeSqlChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$installer = Join-Path $scratch ($Database + '.sql')
$sql = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $SqlFile))
$sql = $sql.Replace('QLBenhVienRangHamMat', $Database)
[IO.File]::WriteAllText($installer, $sql, [Text.UTF8Encoding]::new($false))
function Invoke-TestSql([string[]]$Arguments) {
    & $sqlcmd -S $MayChu -E -C -b -f 65001 -w 240 @Arguments
    if ($LASTEXITCODE -ne 0) { throw "SQL verification failed ($LASTEXITCODE)." }
}
function Invoke-ScheduleChecks {
    $project = Join-Path $scratch 'AiScheduleChecks.csproj'
    [IO.File]::WriteAllText($project, '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><Compile Include="../../tools/AiScheduleChecks.cs.txt" Link="Program.cs" /><ProjectReference Include="../../DAL/DAL.csproj" /></ItemGroup></Project>', [Text.UTF8Encoding]::new($false))
    & dotnet run --project $project -p:NuGetAudit=false -- $MayChu $Database
    if ($LASTEXITCODE -ne 0) { throw 'Doctor schedule SQL contract checks failed.' }
}
function Assert-PartialData {
    if (!$KhoiPhucDangDo) { return }
    Invoke-TestSql @('-d', $Database, '-Q', @'
IF NOT EXISTS(SELECT 1 FROM dbo.RHM_CAU_HINH_QUY_PHEP WHERE Nam=2026 AND SoNgayPhepNam=18 AND SoThangTieuChuan=10 AND NgayCapNhat='2026-01-02T03:04:05')
 OR NOT EXISTS(SELECT 1 FROM dbo.AI_SERVICE_IDENTITY WHERE MaAIService=N'PACKAGE_FIXTURE' AND CertificateThumbprint=REPLICATE('A',40) AND TrangThai=0 AND NgayTao='2026-01-02T03:04:05')
 THROW 55997,N'Partial recovery replaced existing configuration or AI identity data.',1;
IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.TAI_KHOAN') AND name=N'MaTK' AND is_nullable=1)
 THROW 55997,N'TAI_KHOAN.MaTK must remain NOT NULL.',1;
PRINT N'PASS: partial-install configuration and AI identity preserved; account PK is NOT NULL.';
'@)
    $currentIds = & $sqlcmd -S $MayChu -E -C -b -d $Database -h -1 -W -Q $partialIdQuery
    if ($LASTEXITCODE -ne 0 -or (Compare-Object $partialIds $currentIds)) { throw 'Recovery recreated a pre-existing extension table.' }
}
$checks = Join-Path $PSScriptRoot 'MergeSqlChecks.sql'
function Invoke-FixedShiftChecks {
    $source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'SqlSyncChecks.cs.txt'))
    $fixture = [regex]::Match($source, '(?s)const string FixtureSql\s*=\s*"""\r?\n(.*?)\r?\n\s*""";')
    if (!$fixture.Success) { throw 'Cannot locate the existing payroll rollback fixture.' }
    $sqlFixture = $fixture.Groups[1].Value.Replace('PhuCapCoDinh,TrangThai,MaNVDuyet', 'PhuCapCoDinh,DonGiaCa,TrangThai,MaNVDuyet')
    $sqlFixture = $sqlFixture.Replace("0.1,100000,N'Đã duyệt'", "0.1,100000,100000,N'Đã duyệt'")
    $prefix = "SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF;`nEXEC sys.sp_set_session_context @key=N'RHM.MaPhien',@value=NULL;`nBEGIN TRY BEGIN TRANSACTION; DECLARE @admin nvarchar(50)=N'NV001';`n"
    $suffix = "`nROLLBACK; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;"
    $payrollFile = Join-Path $scratch ($Database + '-payroll.sql')
    $sqlAssertions = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'MergeSqlPayrollChecks.sql'))
    [IO.File]::WriteAllText($payrollFile, $prefix + $sqlFixture + "`n" + $sqlAssertions + $suffix, [Text.UTF8Encoding]::new($false))
    Invoke-TestSql @('-d', $Database, '-i', $payrollFile)
}
if ($ChiKiemTra) {
    Invoke-TestSql @('-d', $Database, '-i', $checks)
    Invoke-TestSql @('-d', $Database, '-i', (Join-Path $PSScriptRoot 'MergeSqlStaffChecks.sql'))
    Invoke-FixedShiftChecks
    Invoke-ScheduleChecks
    return
}
Invoke-TestSql @('-d', 'master', '-Q', "IF DB_ID(N'$Database') IS NOT NULL THROW 55990,N'Test requires a NEW database.',1;")
if ($KhoiPhucDangDo) {
    Invoke-TestSql @('-d', 'master', '-Q', "CREATE DATABASE [$Database];")
    # Real remnants of a failed package installation; custom values must survive.
    Invoke-TestSql @('-d', $Database, '-Q', @'
CREATE TABLE dbo.RHM_CAU_HINH_QUY_PHEP (
 Nam int NOT NULL CONSTRAINT PK_RHM_CAU_HINH_QUY_PHEP PRIMARY KEY,
 SoNgayPhepNam decimal(6,2) NOT NULL, SoThangTieuChuan tinyint NOT NULL,
 NgayCapNhat datetime2(0) NOT NULL CONSTRAINT DF_RHM_CAU_HINH_PHEP_Ngay DEFAULT SYSDATETIME(),
 CONSTRAINT CK_RHM_CAU_HINH_PHEP CHECK(Nam BETWEEN 2000 AND 2100 AND SoNgayPhepNam BETWEEN 1 AND 60 AND SoThangTieuChuan BETWEEN 1 AND 12));
INSERT dbo.RHM_CAU_HINH_QUY_PHEP VALUES(2026,18,10,'2026-01-02T03:04:05');
CREATE TABLE dbo.AI_SERVICE_IDENTITY (
 MaAIService nvarchar(100) NOT NULL CONSTRAINT PK_AI_SERVICE_IDENTITY PRIMARY KEY,
 CertificateThumbprint char(40) NOT NULL,
 Quyen nvarchar(50) NOT NULL CONSTRAINT DF_AI_SERVICE_IDENTITY_Quyen DEFAULT N'AI_TAO_PHIEU',
 TrangThai bit NOT NULL CONSTRAINT DF_AI_SERVICE_IDENTITY_TrangThai DEFAULT 1,
 NgayTao datetime2(0) NOT NULL CONSTRAINT DF_AI_SERVICE_IDENTITY_NgayTao DEFAULT SYSDATETIME(),
 CONSTRAINT UQ_AI_SERVICE_IDENTITY_Thumbprint UNIQUE(CertificateThumbprint),
 CONSTRAINT CK_AI_SERVICE_IDENTITY_Quyen CHECK(Quyen=N'AI_TAO_PHIEU'));
INSERT dbo.AI_SERVICE_IDENTITY VALUES(N'PACKAGE_FIXTURE',REPLICATE('A',40),N'AI_TAO_PHIEU',0,'2026-01-02T03:04:05');
'@)
    $partialIdQuery = "SET NOCOUNT ON; SELECT name,object_id FROM sys.tables WHERE name IN(N'RHM_CAU_HINH_QUY_PHEP',N'AI_SERVICE_IDENTITY') ORDER BY name;"
    $partialIds = & $sqlcmd -S $MayChu -E -C -b -d $Database -h -1 -W -Q $partialIdQuery
    if ($LASTEXITCODE -ne 0) { throw 'Cannot snapshot partial-install tables.' }
}
Invoke-TestSql @('-d', 'master', '-i', $installer)
Assert-PartialData
Invoke-TestSql @('-d', $Database, '-i', $checks)
Invoke-TestSql @('-d', $Database, '-i', (Join-Path $PSScriptRoot 'MergeSqlStaffChecks.sql'))
Invoke-FixedShiftChecks
$snapshotQuery = 'SET NOCOUNT ON; SELECT t.name,SUM(p.rows) AS [RowCount] FROM sys.tables t JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN(0,1) WHERE t.is_ms_shipped=0 GROUP BY t.name ORDER BY t.name;'
$before = & $sqlcmd -S $MayChu -E -C -b -d $Database -Q $snapshotQuery -h -1 -W
if ($LASTEXITCODE -ne 0) { throw 'Cannot snapshot test rows.' }
Invoke-TestSql @('-d', 'master', '-i', $installer)
Assert-PartialData
Invoke-TestSql @('-d', $Database, '-i', $checks)
$after = & $sqlcmd -S $MayChu -E -C -b -d $Database -Q $snapshotQuery -h -1 -W
if ($LASTEXITCODE -ne 0) { throw 'Cannot verify test rows.' }
if (Compare-Object $before $after) { throw 'Rerunning installer changed table row counts.' }
Invoke-ScheduleChecks
Write-Output 'PASS: merged schema, trusted constraints, install/rerun and preserved row counts.'
