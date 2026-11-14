param([Parameter(Mandatory=$true)][string]$ProjectRoot)
$ProjectRoot=[IO.Path]::GetFullPath($ProjectRoot)
$view=Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml'
$code=Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml.cs'
$needed=@('ET\ET_AITIEPNHAN.cs','BUS\BUS_AITIEPNHAN.cs','DAL\DAL_AITIEPNHAN.cs','RangHamMat.Wpf\Views\TiepNhanAI.xaml','RangHamMat.Wpf\Views\TiepNhanAI.xaml.cs')
Write-Host '=== KIEM TRA NUT NHAN BAN KHAI AI ==='
if(-not (Test-Path $view) -or -not (Test-Path $code)){ Write-Host 'SAI DUONG DAN DU AN' -ForegroundColor Red; exit 1 }
$x=[IO.File]::ReadAllText($view);$c=[IO.File]::ReadAllText($code)
Write-Host "XAML nut AI: $($x.Contains('MoTiepNhanAI_Click'))"
Write-Host "Code-behind xu ly nut AI: $($c.Contains('private void MoTiepNhanAI_Click'))"
foreach($f in $needed){ Write-Host "$f : $(Test-Path (Join-Path $ProjectRoot $f))" }
Write-Host 'Neu ket qua False: chay SuaNut_BanKhaiAI.ps1 va Rebuild Solution.'
Write-Host 'Neu deu True nhung khong thay nut: kiem tra file phieu huong dan cua project dang build va khoi dong lai app.'
