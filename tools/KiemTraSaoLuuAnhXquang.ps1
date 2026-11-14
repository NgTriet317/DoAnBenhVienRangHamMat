<#
.SYNOPSIS
Kiểm toàn bộ chuỗi sao lưu SQL và ảnh, phục hồi riêng, rồi mở ảnh qua ứng dụng.
.DESCRIPTION
Chỉ nhận fixture RHM_Hospital_Test_. Kiểm kho trước sao lưu, phục hồi sang database
và thư mục khác, thử từ chối database đích đã có rồi chạy kiểm BUS/giao diện sau phục hồi.
.PARAMETER Database
Database fixture nguồn đã có metadata ảnh và kho tương ứng.
.PARAMETER ThuMucKho
Kho ảnh nguồn; bộ kiểm cho phép kho cục bộ để chạy fixture.
.PARAMETER MayChu
SQL Server thực hiện sao lưu và phục hồi database thử.
#>
param([Parameter(Mandatory=$true)][string]$Database,[Parameter(Mandatory=$true)][string]$ThuMucKho,[string]$MayChu='.')
$ErrorActionPreference='Stop'
if($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$'){throw 'Chỉ sao lưu/phục hồi fixture cách ly.'}
$root=Split-Path $PSScriptRoot -Parent
$evidence=Join-Path $root 'obj/XrayEvidence'
$run=Join-Path $evidence ('operations-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$sqlShared=Join-Path $run 'sql-shared'
New-Item -ItemType Directory -Path $sqlShared -Force | Out-Null
$ops=Join-Path $PSScriptRoot 'QuanLyKhoAnhXquang.ps1'
& $ops -CheDo KiemTra -MayChu $MayChu -Database $Database -ThuMucKho $ThuMucKho -ChoPhepKhoCucBo
if($LASTEXITCODE -ne 0){throw 'Kho fixture chưa toàn vẹn trước backup.'}
$backupOutput=@(& $ops -CheDo SaoLuu -MayChu $MayChu -Database $Database -ThuMucKho $ThuMucKho -ThuMucBanSao $run -ThuMucSqlServer $sqlShared -ChoPhepKhoCucBo)
$backupOutput|Write-Output
if($LASTEXITCODE -ne 0){throw 'Backup fixture thất bại.'}
$backupLine=$backupOutput|Where-Object {$_ -like 'BACKUP_COMPLETE=*'}|Select-Object -Last 1
if(!$backupLine){throw 'Thiếu marker backup hoàn tất.'}
$backup=$backupLine.Substring('BACKUP_COMPLETE='.Length)
$dbRestored='RHM_Hospital_Test_Xray_Restore_'+(Get-Date -Format 'yyyyMMdd_HHmmss')
$destination=Join-Path $run 'restored-store'
& $ops -CheDo KhoiPhuc -MayChu $MayChu -Database $Database -DatabaseMoi $dbRestored -ThuMucKho $destination -BanSao $backup -ThuMucSqlServer $sqlShared -ChoPhepKhoCucBo
if($LASTEXITCODE -ne 0){throw 'Phục hồi fixture thất bại.'}
Write-Output 'PASS: SQL backup CHECKSUM + ảnh/manifest được phục hồi vào DB và thư mục khác.'
$rejectLog=Join-Path $run 'existing-database-rejected.log'
$unexpectedDestination=Join-Path $run 'another-store'
try {
 $ErrorActionPreference='Continue'
 & powershell -NoProfile -ExecutionPolicy Bypass -File $ops -CheDo KhoiPhuc -MayChu $MayChu -Database $Database -DatabaseMoi $dbRestored -ThuMucKho $unexpectedDestination -BanSao $backup -ThuMucSqlServer $sqlShared -ChoPhepKhoCucBo *> $rejectLog
 $rejectExit=$LASTEXITCODE
} finally {$ErrorActionPreference='Stop'}
if($rejectExit -eq 0 -or (Test-Path -LiteralPath $unexpectedDestination) -or (Get-Content -LiteralPath $rejectLog -Raw) -notmatch 'Database đích đã tồn tại'){throw 'Restore phải từ chối đúng lỗi database đã tồn tại trước khi tạo kho đích.'}
Write-Output 'PASS: restore từ chối database đích đã tồn tại.'
& (Join-Path $PSScriptRoot 'KiemTraAnhXquang.ps1') -Database $dbRestored -MayChu $MayChu -KiemTraPhucHoi -ThuMucKho $destination
if($LASTEXITCODE -ne 0){throw 'Ứng dụng không mở lại được ảnh sau restore.'}
Write-Output "RESTORED_DATABASE=$dbRestored"
Write-Output "RESTORED_ROOT=$destination"
