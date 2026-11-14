param(
    [Parameter(Mandatory=$true)][string]$ProjectRoot
)
$ErrorActionPreference = 'Stop'
$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$targetRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$files = @(
    'RangHamMat.Wpf\Views\TiepNhanAI.xaml',
    'RangHamMat.Wpf\Views\TiepNhanAI.xaml.cs',
    'ET\ET_AI_TAOPHIEU.cs',
    'DAL\DAL_AI_TAOPHIEU.cs',
    'BUS\BUS_AI_TAOPHIEU.cs'
)
# Tham dinh duong dan va source V2 truoc khi copy bat ky file nao
foreach ($rel in $files) {
    $src = Join-Path $source $rel
    $dst = Join-Path $targetRoot $rel
    if (-not (Test-Path -LiteralPath $src -PathType Leaf)) { throw "Thieu file ban va: $src" }
    if (-not (Test-Path -LiteralPath $dst -PathType Leaf)) { throw "Du an chua co Kiosk AI V2: $dst" }
}
$xaml = Get-Content -LiteralPath (Join-Path $targetRoot 'RangHamMat.Wpf\Views\TiepNhanAI.xaml') -Raw
$code = Get-Content -LiteralPath (Join-Path $targetRoot 'RangHamMat.Wpf\Views\TiepNhanAI.xaml.cs') -Raw
if ($xaml -notmatch 'cboBenhNhanCu' -or $code -notmatch 'TaoPhieu_Click') {
    throw 'Source TiepNhanAI khong dung V2. Dung chep de; vui long so sanh file truoc.'
}
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backup = Join-Path $targetRoot "Backup_KioskAI_V21_$stamp"
foreach ($rel in $files) {
    $src = Join-Path $source $rel
    $dst = Join-Path $targetRoot $rel
    $old = Join-Path $backup $rel
    $parent = Split-Path $old -Parent
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    Copy-Item -LiteralPath $dst -Destination $old -Force
    Write-Host "Sao luu $rel"
}
foreach ($rel in $files) {
    Copy-Item -LiteralPath (Join-Path $source $rel) -Destination (Join-Path $targetRoot $rel) -Force
    Write-Host "Da cap nhat $rel" -ForegroundColor Green
}
Write-Host "\nBackup: $backup" -ForegroundColor Yellow
Write-Host 'Quan trong: Can chay SQL/10_Sua_KhaiNhan_TaoPhieu.sql tren database da cai 07 va 08.'
Write-Host 'Trong Visual Studio: Clean Solution -> Rebuild Solution.'
