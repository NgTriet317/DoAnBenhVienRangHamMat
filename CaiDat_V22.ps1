param([Parameter(Mandatory=$true)][string]$ProjectRoot)
$ErrorActionPreference = 'Stop'
$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path -LiteralPath $ProjectRoot).Path
$files = @(
 'RangHamMat.Wpf\Views\TiepNhanAI.xaml',
 'RangHamMat.Wpf\Views\TiepNhanAI.xaml.cs',
 'ET\ET_AI_TAOPHIEU.cs',
 'DAL\DAL_AI_TAOPHIEU.cs',
 'BUS\BUS_AI_TAOPHIEU.cs'
)
foreach ($rel in $files) {
 $src = Join-Path $source $rel
 $dst = Join-Path $root $rel
 if (-not (Test-Path -LiteralPath $src -PathType Leaf)) { throw "Ban va thieu file $src" }
 if (-not (Test-Path -LiteralPath $dst -PathType Leaf)) { throw "Du an thieu file $dst. Can cai AI V2.1 truoc." }
}
$code = Get-Content -LiteralPath (Join-Path $root 'RangHamMat.Wpf\Views\TiepNhanAI.xaml.cs') -Raw
$xaml = Get-Content -LiteralPath (Join-Path $root 'RangHamMat.Wpf\Views\TiepNhanAI.xaml') -Raw
if ($code -notmatch 'doiChieu|chkXacNhanChuaTungKham|TaoPhieu_Click' -or $xaml -notmatch 'cboBenhNhanCu') {
 throw 'Source TiepNhanAI khong phu hop V2.1. Khong ghi de; can doi chieu thu cong.'
}
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backup = Join-Path $root "Backup_KioskAI_V22_$stamp"
foreach ($rel in $files) {
 $dst = Join-Path $root $rel
 $b = Join-Path $backup $rel
 New-Item -ItemType Directory -Force -Path (Split-Path $b -Parent) | Out-Null
 Copy-Item -LiteralPath $dst -Destination $b -Force
}
foreach ($rel in $files) {
 Copy-Item -LiteralPath (Join-Path $source $rel) -Destination (Join-Path $root $rel) -Force
 Write-Host "DA CAP NHAT: $rel" -ForegroundColor Green
}
Write-Host "Backup: $backup" -ForegroundColor Yellow
Write-Host 'CHAY SQL: SQL\12_AI_TuDoiChieuBenhNhan.sql (chay 1 lan, sau V2.1).'
Write-Host 'Visual Studio 2022: Clean Solution -> Rebuild Solution -> F5.'
