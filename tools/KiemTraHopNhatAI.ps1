<# Kiểm tra installer hợp nhất trên database cách ly: cài mới, giữ dữ liệu khi chạy lại. #>
param(
    [string]$Database = 'RHM_Hospital_Test_MergeSql_20261115',
    [string]$MayChu = 'lpc:localhost',
    [string]$SqlFile = '',
    [switch]$ChiKiemTra
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
    return
}
Invoke-TestSql @('-d', 'master', '-Q', "IF DB_ID(N'$Database') IS NOT NULL THROW 55990,N'Test requires a NEW database.',1;")
Invoke-TestSql @('-d', 'master', '-i', $installer)
Invoke-TestSql @('-d', $Database, '-i', $checks)
Invoke-TestSql @('-d', $Database, '-i', (Join-Path $PSScriptRoot 'MergeSqlStaffChecks.sql'))
Invoke-FixedShiftChecks
$snapshotQuery = 'SET NOCOUNT ON; SELECT t.name,SUM(p.rows) AS [RowCount] FROM sys.tables t JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN(0,1) WHERE t.is_ms_shipped=0 GROUP BY t.name ORDER BY t.name;'
$before = & $sqlcmd -S $MayChu -E -C -b -d $Database -Q $snapshotQuery -h -1 -W
if ($LASTEXITCODE -ne 0) { throw 'Cannot snapshot test rows.' }
Invoke-TestSql @('-d', 'master', '-i', $installer)
Invoke-TestSql @('-d', $Database, '-i', $checks)
$after = & $sqlcmd -S $MayChu -E -C -b -d $Database -Q $snapshotQuery -h -1 -W
if ($LASTEXITCODE -ne 0) { throw 'Cannot verify test rows.' }
if (Compare-Object $before $after) { throw 'Rerunning installer changed table row counts.' }
Write-Output 'PASS: merged schema, trusted constraints, install/rerun and preserved row counts.'
