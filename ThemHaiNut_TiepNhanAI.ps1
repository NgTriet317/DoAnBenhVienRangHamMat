# Sua cac loi mo Tiep nhan AI: nut trang chu va nut trong Phieu huong dan.
# Chi bo sung ma va backup file cu; KHONG ghi de he thong dang nhap hoac SQL.
param([Parameter(Mandatory=$true)][string]$ProjectRoot)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ProjectRoot=[System.IO.Path]::GetFullPath($ProjectRoot)
if(-not (Test-Path (Join-Path $ProjectRoot 'RangHamMat.Wpf\MainWindow.xaml'))) {
    $guess = Split-Path $ProjectRoot -Parent
    if(Test-Path (Join-Path $guess 'RangHamMat.Wpf\MainWindow.xaml')) { $ProjectRoot=$guess }
}
$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$paths = @{
    mainX = Join-Path $ProjectRoot 'RangHamMat.Wpf\MainWindow.xaml'
    mainC = Join-Path $ProjectRoot 'RangHamMat.Wpf\MainWindow.xaml.cs'
    phieuX = Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml'
    phieuC = Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml.cs'
}
foreach($key in $paths.Keys){
    if(-not (Test-Path $paths[$key])) { throw "Khong tim thay file '$($paths[$key])'. ProjectRoot phai la thu muc cha cua RangHamMat.Wpf." }
}
$need=@('ET\ET_AITIEPNHAN.cs','BUS\BUS_AITIEPNHAN.cs','DAL\DAL_AITIEPNHAN.cs',
        'RangHamMat.Wpf\Views\TiepNhanAI.xaml','RangHamMat.Wpf\Views\TiepNhanAI.xaml.cs')
foreach($rel in $need){
    if(-not (Test-Path (Join-Path $source $rel))) { throw "ZIP thieu $rel" }
}
$utf8=New-Object System.Text.UTF8Encoding($true)
$mainX=[IO.File]::ReadAllText($paths.mainX)
$mainC=[IO.File]::ReadAllText($paths.mainC)
$phieuX=[IO.File]::ReadAllText($paths.phieuX)
$phieuC=[IO.File]::ReadAllText($paths.phieuC)

# Xu ly MainWindow: nut moi nam canh 'Xin nghi viec' tren trang chu.
if($mainX -notmatch 'Click\s*=\s*"MoTiepNhanAI_TrangChu_Click"') {
    $pattern='(?s)<Button\b(?=[^>]*\bContent\s*=\s*"Xin nghỉ việc")(?=[^>]*\bClick\s*=\s*"XinNghiViec_Click")[^>]*/>'
    $m=[regex]::Match($mainX,$pattern)
    if(-not $m.Success){throw 'Khong tim thay nut Xin nghi viec trong MainWindow.xaml. Chua sua bat ky file nao.'}
    $old=$m.Value -replace '\sGrid.Column\s*=\s*"2"',''
    $block=@'
<StackPanel Grid.Column="2" Orientation="Horizontal" HorizontalAlignment="Right" VerticalAlignment="Center">
            <Button Content="Tiếp nhận AI" Style="{StaticResource Primary}" Click="MoTiepNhanAI_TrangChu_Click"
                    Padding="16,10" Margin="8,0,0,0" AutomationProperties.Name="Mở bản khai tiếp nhận AI" />
            __OLD_BUTTON__
          </StackPanel>
'@
    $block=$block.Replace('__OLD_BUTTON__', $old)
    $mainX=$mainX.Substring(0,$m.Index) + $block + $mainX.Substring($m.Index+$m.Length)
}
if($mainC -notmatch '\bvoid\s+MoTiepNhanAI_TrangChu_Click\s*\(') {
    $anchor=[regex]::Match($mainC,'(?m)^\s*private\s+void\s+XinNghiViec_Click\s*\(')
    if(-not $anchor.Success){throw 'Khong tim thay handler XinNghiViec_Click trong MainWindow.xaml.cs. Chua sua file.'}
    $method=@'
    // Mo danh sach phien do benh nhan khai qua chat AI tai laptop.
    private void MoTiepNhanAI_TrangChu_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PhienDangNhap.MaNV) || !CanOpen("BenhNhan"))
        {
            MessageBox.Show("Tài khoản này không có quyền tiếp nhận bệnh nhân.", "Phân quyền");
            return;
        }
        var cuaSo = new RangHamMat.Views.TiepNhanAI { Owner = this };
        cuaSo.ShowDialog();
        if (!string.IsNullOrWhiteSpace(cuaSo.MaPhieuDuocDuyet))
            OpenApp("PhieuHuongDan");
        else if (cuaSo.MoBenhNhan)
            OpenApp("BenhNhan");
    }

'@
    $mainC=$mainC.Insert($anchor.Index,$method)
}

# Xu ly PhieuHuongDan: nut nam canh 'Lam moi danh sach'.
if($phieuX -notmatch 'Click\s*=\s*"MoTiepNhanAI_Click"') {
    $pattern='(?s)<Button\b(?=[^>]*\bContent\s*=\s*"Làm mới danh sách")(?=[^>]*\bClick\s*=\s*"LamMoi_Click")[^>]*/>'
    $m=[regex]::Match($phieuX,$pattern)
    if(-not $m.Success){throw 'Khong tim thay nut Lam moi danh sach trong PhieuHuongDan.xaml. Chua sua bat ky file nao.'}
    $old=$m.Value -replace '\sGrid.Column\s*=\s*"1"',''
    $block=@'
<StackPanel Grid.Column="1" Orientation="Horizontal" HorizontalAlignment="Right" VerticalAlignment="Center">
                <Button Content="Nhận bản khai AI" Style="{StaticResource NutChinh}"
                        Click="MoTiepNhanAI_Click" Margin="0,0,9,0" Padding="15,9"/>
                __OLD_BUTTON__
            </StackPanel>
'@
    $block=$block.Replace('__OLD_BUTTON__', $old)
    $phieuX=$phieuX.Substring(0,$m.Index) + $block + $phieuX.Substring($m.Index+$m.Length)
}
if($phieuC -notmatch '\bvoid\s+MoTiepNhanAI_Click\s*\(') {
    $anchor=[regex]::Match($phieuC,'(?m)^\s*private\s+void\s+LamMoi_Click\s*\(')
    if(-not $anchor.Success){throw 'Khong tim thay handler LamMoi_Click trong PhieuHuongDan.xaml.cs. Chua sua file.'}
    $method=@'
    // Nhan ban khai AI. Phieu A5 van duoc tao tu phieu kham da xac nhan.
    private void MoTiepNhanAI_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = Window.GetWindow(this) as RangHamMat.MainWindow;
        var cuaSo = new TiepNhanAI { Owner = mainWindow };
        cuaSo.ShowDialog();
        if (!string.IsNullOrWhiteSpace(cuaSo.MaPhieuDuocDuyet))
        {
            hienThiDuLieu();
            ChonPhieu(cuaSo.MaPhieuDuocDuyet);
        }
        else if (cuaSo.MoBenhNhan)
            mainWindow?.OpenApp("BenhNhan");
    }

'@
    $phieuC=$phieuC.Insert($anchor.Index,$method)
}

# Preflight: sua noi dung trong RAM truoc, kiem tra XML va handler truoc khi ghi.
try{$xmlMain = [xml]$mainX; $xmlPhieu = [xml]$phieuX}
catch{throw "XAML khong hop le, CHUA ghi file: $($_.Exception.Message)"}
if(($mainX -notmatch 'MoTiepNhanAI_TrangChu_Click') -or
   ($mainC -notmatch 'void\s+MoTiepNhanAI_TrangChu_Click\s*\(') -or
   ($phieuX -notmatch 'MoTiepNhanAI_Click') -or
   ($phieuC -notmatch 'void\s+MoTiepNhanAI_Click\s*\(')){
   throw 'Thieu nut hoac handler sau ghep. CHUA ghi file.'
}
$backup=Join-Path $ProjectRoot ('Backup_NutAI_'+(Get-Date -Format 'yyyyMMdd_HHmmss'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
foreach($key in @('mainX','mainC','phieuX','phieuC')){
   $relative=$paths[$key].Substring($ProjectRoot.Length).TrimStart('\')
   $to=Join-Path $backup $relative
   New-Item -ItemType Directory -Path (Split-Path -Parent $to) -Force | Out-Null
   Copy-Item $paths[$key] $to -Force
}
[IO.File]::WriteAllText($paths.mainX,$mainX,$utf8)
[IO.File]::WriteAllText($paths.mainC,$mainC,$utf8)
[IO.File]::WriteAllText($paths.phieuX,$phieuX,$utf8)
[IO.File]::WriteAllText($paths.phieuC,$phieuC,$utf8)
foreach($rel in $need){
   $target=Join-Path $ProjectRoot $rel
   if(Test-Path $target){Write-Host "Giu nguyen (da ton tai): $rel" -ForegroundColor Yellow;continue}
   New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
   Copy-Item (Join-Path $source $rel) $target
   Write-Host "Da them: $rel" -ForegroundColor Green
}
Write-Host ''
Write-Host '=== HOAN THANH: DA THEM HAI NUT ===' -ForegroundColor Green
Write-Host "Du an: $ProjectRoot"
Write-Host "Backup: $backup"
Write-Host 'MainWindow.xaml : Tiếp nhận AI (goc tren trang chu)'
Write-Host 'Views\PhieuHuongDan.xaml : Nhận bản khai AI (canh Lam moi danh sach)'
Write-Host 'VS2022: dong ung dung dang chay; Clean Solution; Rebuild Solution; F5.'
Write-Host 'Neu khong thay nut, tim Ctrl+Shift+F: MoTiepNhanAI_TrangChu_Click; kiem tra Startup Project.'
