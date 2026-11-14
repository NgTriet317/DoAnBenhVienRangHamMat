param([Parameter(Mandatory=$true)][string]$ProjectRoot)
$ErrorActionPreference='Stop'
$ProjectRoot=[IO.Path]::GetFullPath($ProjectRoot)
if(-not (Test-Path (Join-Path $ProjectRoot 'RangHamMat.Wpf\MainWindow.xaml'))){
    $parent=Split-Path $ProjectRoot -Parent
    if(Test-Path (Join-Path $parent 'RangHamMat.Wpf\MainWindow.xaml')){$ProjectRoot=$parent}
}
$files=@{
 'Trang chu - nut AI'='RangHamMat.Wpf\MainWindow.xaml'
 'Trang chu - code'='RangHamMat.Wpf\MainWindow.xaml.cs'
 'Phieu huong dan - nut AI'='RangHamMat.Wpf\Views\PhieuHuongDan.xaml'
 'Phieu huong dan - code'='RangHamMat.Wpf\Views\PhieuHuongDan.xaml.cs'
 'Cua so AI'='RangHamMat.Wpf\Views\TiepNhanAI.xaml'
}
Write-Host "Dang kiem tra project: $ProjectRoot"
foreach($k in $files.Keys){
   $p=Join-Path $ProjectRoot $files[$k]
   $ok=Test-Path $p
   if($ok -and $k -eq 'Trang chu - nut AI'){$ok=([IO.File]::ReadAllText($p)).Contains('MoTiepNhanAI_TrangChu_Click')}
   if($ok -and $k -eq 'Trang chu - code'){$ok=([IO.File]::ReadAllText($p)).Contains('void MoTiepNhanAI_TrangChu_Click')}
   if($ok -and $k -eq 'Phieu huong dan - nut AI'){$ok=([IO.File]::ReadAllText($p)).Contains('MoTiepNhanAI_Click')}
   if($ok -and $k -eq 'Phieu huong dan - code'){$ok=([IO.File]::ReadAllText($p)).Contains('void MoTiepNhanAI_Click')}
   Write-Host "${k}: $(if($ok){'OK'}else{'THIEU'})"
}
Write-Host 'Neu tat ca OK nhung UI van khong co, dang chay EXE/nhanh/Startup Project khac.'
