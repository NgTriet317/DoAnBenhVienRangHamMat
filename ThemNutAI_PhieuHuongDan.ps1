# Bo sung nut AI ngay tren Phieu huong dan, khong sua MainWindow/PhienDangNhap/SQL.
param([Parameter(Mandatory=$true)][string]$ProjectRoot)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest

$ProjectRoot=[IO.Path]::GetFullPath($ProjectRoot)
$source=Split-Path -Parent $MyInvocation.MyCommand.Path

# Chap nhan thu muc solution, hoac chinh thu muc RangHamMat.Wpf.
if ((Split-Path $ProjectRoot -Leaf) -eq 'RangHamMat.Wpf' -and
    (Test-Path (Join-Path $ProjectRoot 'Views\PhieuHuongDan.xaml'))) {
    $ProjectRoot=Split-Path $ProjectRoot -Parent
}
$xamlPath=Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml'
$codePath=Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml.cs'
Write-Host "ProjectRoot: $ProjectRoot" -ForegroundColor Cyan
Write-Host "XAML: $xamlPath"
Write-Host "Code: $codePath"
foreach($p in @($xamlPath,$codePath)) {
    if(-not (Test-Path -LiteralPath $p)) {
       throw "Khong tim thay $p. Hay dung duong dan THU MUC goc solution (co RangHamMat.Wpf), khong phai thu muc bin/Debug."
    }
}

$xaml=[IO.File]::ReadAllText($xamlPath)
$code=[IO.File]::ReadAllText($codePath)
if($xaml -notmatch 'x:Class\s*=\s*"RangHamMat\.Views\.PhieuHuongDan"') {
    throw 'File XAML khong dung class PhieuHuongDan. Dung lai, chua thay doi file.'
}
if($code -notmatch '\bpartial\s+class\s+PhieuHuongDan\b') {
    throw 'File code-behind khong phai class PhieuHuongDan. Dung lai.'
}

$hasButton=($xaml -match 'Click\s*=\s*"MoTiepNhanAI_Click"')
$hasMethod=($code -match '\bvoid\s+MoTiepNhanAI_Click\s*\(')
if(-not $hasButton) {
    # Doi chinh nut Lam moi, giu nguyen thuoc tinh nut cu; nen khong phu thuoc thu tu XAML.
    $pattern='(?s)<Button\b(?=[^>]*\bContent\s*=\s*"Làm mới danh sách")(?=[^>]*\bClick\s*=\s*"LamMoi_Click")[^>]*/>'
    $match=[regex]::Match($xaml,$pattern)
    if(-not $match.Success) {
      throw 'Khong thay nut "Làm mới danh sách" (LamMoi_Click) trong XAML. Chua thay doi file; hay gui hai file PhieuHuongDan.xaml va .xaml.cs.'
    }
    $oldButton=[regex]::Replace($match.Value,'\s+Grid\.Column\s*=\s*"1"','')
    $replacement=@'
<StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center" HorizontalAlignment="Right">
                <Button x:Name="btnNhanBanKhaiAI" Content="Nhận bản khai AI" Style="{StaticResource NutChinh}"
                        Click="MoTiepNhanAI_Click" Margin="0,0,9,0" Padding="15,9"
                        AutomationProperties.Name="Mở danh sách bản khai AI từ bệnh nhân"/>
                __OLD_BUTTON__
            </StackPanel>
'@
    $replacement=$replacement.Replace('__OLD_BUTTON__',$oldButton)
    $xaml=$xaml.Substring(0,$match.Index)+$replacement+$xaml.Substring($match.Index+$match.Length)
}

if(-not $hasMethod) {
    $anchor=[regex]::Match($code,'(?m)^\s*private\s+void\s+LamMoi_Click\s*\(')
    if(-not $anchor.Success){throw 'Khong tim thay Ham LamMoi_Click trong code-behind. Chua ghi file.'}
    $method=@'
    // Mo cua so xem nhung phien benh nhan da khai bao tren giao dien AI.
    private void MoTiepNhanAI_Click(object sender, RoutedEventArgs e)
    {
        var cuaSoCha = Window.GetWindow(this);
        var cuaSo = new TiepNhanAI();
        if (cuaSoCha != null) cuaSo.Owner = cuaSoCha;
        cuaSo.ShowDialog();
        if (!string.IsNullOrWhiteSpace(cuaSo.MaPhieuDuocDuyet))
        {
            hienThiDuLieu();
            ChonPhieu(cuaSo.MaPhieuDuocDuyet);
        }
        else if (cuaSo.MoBenhNhan && cuaSoCha is RangHamMat.MainWindow mainWindow)
        {
            mainWindow.OpenApp("BenhNhan");
        }
    }

'@
    $code=$code.Insert($anchor.Index,$method)
}

# Kiem tra truoc khi ghi.
try { $xml=[xml]$xaml }
catch { throw "XAML khong hop le: $($_.Exception.Message). Chua ghi file." }
if($xaml -notmatch 'Click="MoTiepNhanAI_Click"' -or $code -notmatch 'void\s+MoTiepNhanAI_Click\s*\(') {
    throw 'Them nut/handler khong thanh cong. Chua ghi file.'
}
$need=@('RangHamMat.Wpf\Views\TiepNhanAI.xaml','RangHamMat.Wpf\Views\TiepNhanAI.xaml.cs',
        'ET\ET_AITIEPNHAN.cs','BUS\BUS_AITIEPNHAN.cs','DAL\DAL_AITIEPNHAN.cs')
foreach($rel in $need) {
    $sourceFile=Join-Path $source $rel
    if(-not (Test-Path $sourceFile)){throw "File ZIP thieu: $rel; chua ghi file."}
}

# Sao luu va ghi file.
$backup=Join-Path $ProjectRoot ('Backup_PhieuHuongDan_AI_'+(Get-Date -Format 'yyyyMMdd_HHmmss'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
Copy-Item -LiteralPath $xamlPath -Destination (Join-Path $backup 'PhieuHuongDan.xaml')
Copy-Item -LiteralPath $codePath -Destination (Join-Path $backup 'PhieuHuongDan.xaml.cs')
$utf8=New-Object System.Text.UTF8Encoding($true)
try {
    [IO.File]::WriteAllText($xamlPath,$xaml,$utf8)
    [IO.File]::WriteAllText($codePath,$code,$utf8)
    foreach($rel in $need) {
      $target=Join-Path $ProjectRoot $rel
      if(Test-Path $target) {Write-Host "Da co, giu nguyen: $rel" -ForegroundColor Yellow; continue}
      New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
      Copy-Item (Join-Path $source $rel) $target
      Write-Host "Da them: $rel" -ForegroundColor Green
    }
}
catch {
    Copy-Item (Join-Path $backup 'PhieuHuongDan.xaml') $xamlPath -Force
    Copy-Item (Join-Path $backup 'PhieuHuongDan.xaml.cs') $codePath -Force
    throw
}

$verifyX=[IO.File]::ReadAllText($xamlPath)
$verifyC=[IO.File]::ReadAllText($codePath)
if($verifyX -notmatch 'Click="MoTiepNhanAI_Click"' -or $verifyC -notmatch 'void\s+MoTiepNhanAI_Click\s*\(') {
    throw "DA GHI FILE NHUNG KHONG QUA KIEM TRA. Backup: $backup"
}
Write-Host ''
Write-Host 'OK: NUT NHAN BAN KHAI AI DA CO TRONG PHIEU HUONG DAN.' -ForegroundColor Green
Write-Host "Da sua dung: $xamlPath"
Write-Host "Da sao luu tai: $backup"
Write-Host 'Tiep theo: dong app dang chay -> Visual Studio Clean -> Rebuild -> F5.'
Write-Host 'Neu khong thay: Ctrl+Shift+F tim MoTiepNhanAI_Click, kiem tra dung project va EXE dang chay.'
