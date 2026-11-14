param(
    [Parameter(Mandatory = $true)][string] $ProjectRoot,
    [switch] $CapNhatPhieuHuongDan
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $ProjectRoot).Path
$source = $PSScriptRoot
if (-not (Test-Path (Join-Path $root 'RangHamMat.Wpf'))) {
    throw "ProjectRoot phai la thu muc chua RangHamMat.Wpf: $root"
}
foreach ($d in @('ET','BUS','DAL','RangHamMat.Wpf\Views')) {
    if (-not (Test-Path (Join-Path $root $d))) { throw "Khong co thu muc $d trong $root" }
}
$files = @(
    'ET\ET_AI_TAOPHIEU.cs',
    'BUS\BUS_AI_TAOPHIEU.cs',
    'DAL\DAL_AI_TAOPHIEU.cs',
    'RangHamMat.Wpf\Views\TiepNhanAI.xaml',
    'RangHamMat.Wpf\Views\TiepNhanAI.xaml.cs'
)
if ($CapNhatPhieuHuongDan) {
    $files += @('RangHamMat.Wpf\Views\PhieuHuongDan.xaml',
                'RangHamMat.Wpf\Views\PhieuHuongDan.xaml.cs')
}
foreach ($file in $files) {
    if (-not (Test-Path (Join-Path $source $file))) { throw "Thieu file trong ZIP: $file" }
}
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backup = Join-Path $root "Backup_KioskAI_V2_$stamp"
New-Item -ItemType Directory -Path $backup -Force | Out-Null
foreach ($file in $files) {
    $dest = Join-Path $root $file
    $src = Join-Path $source $file
    if (Test-Path $dest) {
        $bak = Join-Path $backup $file
        New-Item -ItemType Directory -Path (Split-Path $bak -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $dest -Destination $bak -Force
    }
    Copy-Item -LiteralPath $src -Destination $dest -Force
    Write-Host "Da cap nhat: $file" -ForegroundColor Green
}
$phieuXaml = Join-Path $root 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml'
$phieuCs = Join-Path $root 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml.cs'
if ((Get-Content $phieuXaml -Raw) -notmatch 'MoTiepNhanAI_Click' -or
    (Get-Content $phieuCs -Raw) -notmatch 'private void MoTiepNhanAI_Click') {
    Write-Warning "PHIEU HUONG DAN CHUA CO NUT AI. Sao luu va chep 2 file PhieuHuongDan tu ZIP, hoac chay lai voi -CapNhatPhieuHuongDan."
}
Write-Host "Backup: $backup"
Write-Host 'Buoc tiep: chay SQL/08_AI_TuDongTiepNhan.sql -> Clean/Rebuild WPF -> F5.' -ForegroundColor Cyan
