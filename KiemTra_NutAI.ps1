param([Parameter(Mandatory=$true)][string]$ProjectRoot)
$ProjectRoot=[IO.Path]::GetFullPath($ProjectRoot)
if((Split-Path $ProjectRoot -Leaf) -eq 'RangHamMat.Wpf'){$ProjectRoot=Split-Path $ProjectRoot -Parent}
$x=Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml'
$c=Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml.cs'
Write-Host "Dang kiem tra: $ProjectRoot"
if(-not (Test-Path $x) -or -not(Test-Path $c)){throw 'Khong tim thay XAML hoac code-behind tren duong dan da chon.'}
$tests=@(
  @('Nut hien thi', $x, 'Click\s*=\s*"MoTiepNhanAI_Click"'),
  @('Ham xu ly', $c, 'void\s+MoTiepNhanAI_Click\s*\('),
  @('Cua so TiepNhanAI.xaml', (Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\TiepNhanAI.xaml'), ''),
  @('Cua so TiepNhanAI.xaml.cs', (Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\TiepNhanAI.xaml.cs'), ''),
  @('BUS AI', (Join-Path $ProjectRoot 'BUS\BUS_AITIEPNHAN.cs'), ''),
  @('DAL AI', (Join-Path $ProjectRoot 'DAL\DAL_AITIEPNHAN.cs'), ''),
  @('ET AI', (Join-Path $ProjectRoot 'ET\ET_AITIEPNHAN.cs'), '')
)
$bad=0
foreach($t in $tests){
  $ok=Test-Path $t[1]
  if($ok -and $t[2]){$ok=([IO.File]::ReadAllText($t[1]) -match $t[2])}
  if(-not $ok){$bad++}
  Write-Host ($t[0] + ': ' + $(if($ok){'OK'}else{'THIEU'})) -ForegroundColor $(if($ok){'Green'}else{'Red'})
}
if($bad -gt 0){exit 1}
Write-Host 'Tat ca file va handler da co. Can Rebuild WPF va chay dung startup project.' -ForegroundColor Green
