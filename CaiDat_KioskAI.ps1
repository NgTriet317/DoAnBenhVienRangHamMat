param(
  [Parameter(Mandatory=$true)][string]$ProjectRoot
)
$ErrorActionPreference='Stop'
$ProjectRoot=[IO.Path]::GetFullPath($ProjectRoot)
$SourceRoot=Split-Path -Parent $MyInvocation.MyCommand.Path
$names=@(
 'ChayThu_KioskAI.ps1',
 'ET\ET_AITIEPNHAN.cs',
 'BUS\BUS_AITIEPNHAN.cs',
 'DAL\DAL_AITIEPNHAN.cs',
 'RangHamMat.Wpf\Views\TiepNhanAI.xaml',
 'RangHamMat.Wpf\Views\TiepNhanAI.xaml.cs'
)
$required=@(
 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml',
 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml.cs',
 'BenhVienRangHamMat.sln'
)
foreach($item in $required){if(-not (Test-Path (Join-Path $ProjectRoot $item))){throw "Khong tim thay file goc: $item. Hay chon thu muc chua file solution."}}
$ui=Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml'
$code=Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml.cs'
$utf8=New-Object System.Text.UTF8Encoding($true)
$xamlText=[IO.File]::ReadAllText($ui).Replace("`r`n","`n")
$codeText=[IO.File]::ReadAllText($code)
$originalButton=@'
            <Button Grid.Column="1" Content="Làm mới danh sách" Style="{StaticResource NutPhu}"
                    Click="LamMoi_Click" VerticalAlignment="Center" Margin="8,0,0,0"/>
'@
$newButtons=@'
            <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
                <Button Content="Nhận bản khai AI" Style="{StaticResource NutChinh}"
                        Click="MoTiepNhanAI_Click" Margin="0,0,9,0" Padding="15,9"/>
                <Button Content="Làm mới danh sách" Style="{StaticResource NutPhu}"
                        Click="LamMoi_Click" Margin="0,0,0,0"/>
            </StackPanel>
'@
$anchor='    private void LamMoi_Click('
$method=@'
    // Nhận dữ liệu khai báo trên laptop, xác nhận trong WPF, sau đó dùng phiếu hướng dẫn A5 cũ.
    private void MoTiepNhanAI_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = Window.GetWindow(this) as MainWindow;
        var cuaSo = new TiepNhanAI { Owner = mainWindow };
        cuaSo.ShowDialog();
        if (!string.IsNullOrWhiteSpace(cuaSo.MaPhieuDuocDuyet))
        {
            hienThiDuLieu();
            ChonPhieu(cuaSo.MaPhieuDuocDuyet);
        }
        else if (cuaSo.MoBenhNhan)
        {
            mainWindow?.OpenApp("BenhNhan");
        }
    }

'@
$already=$xamlText.Contains('Click="MoTiepNhanAI_Click"')
if(-not $already){
  if(-not $xamlText.Contains($originalButton)){throw 'PhieuHuongDan.xaml co bo cuc khac. Dung ghi de va lien he de doi chieu anchor.'}
  if(-not $codeText.Contains($anchor)){throw 'PhieuHuongDan.xaml.cs co bo cuc khac. Dung ghi de.'}
}
if($xamlText.Contains('Click="MoTiepNhanAI_Click"') -xor $codeText.Contains('private void MoTiepNhanAI_Click(')){
  throw 'Phat hien ban va chua hoan tat. Hay doi chieu thu cong truoc khi chay.'
}
foreach($name in $names){
  $s=Join-Path $SourceRoot $name
  $d=Join-Path $ProjectRoot $name
  if(-not (Test-Path $s)){throw "Bo cai thieu: $name"}
  if((Test-Path $d) -and ((Get-FileHash $s -Algorithm SHA256).Hash -ne (Get-FileHash $d -Algorithm SHA256).Hash)){
    throw "File $name da duoc sua tren may ban. Khong tu ghi de."
  }
}
$backup=Join-Path $ProjectRoot ('Backup_KioskAI_'+(Get-Date -Format 'yyyyMMdd_HHmmss'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
foreach($path in @($ui,$code)){
  Copy-Item $path (Join-Path $backup (Split-Path $path -Leaf)) -Force
}
if(-not $already){
  $xamlText=$xamlText.Replace($originalButton,$newButtons)
  $codeText=$codeText.Replace($anchor,$method+$anchor)
  [IO.File]::WriteAllText($ui,$xamlText,$utf8)
  [IO.File]::WriteAllText($code,$codeText,$utf8)
}
foreach($name in $names){
  $s=Join-Path $SourceRoot $name; $d=Join-Path $ProjectRoot $name;
  New-Item -ItemType Directory -Path (Split-Path -Parent $d) -Force | Out-Null
  if(Test-Path $d){
    $oldHash=(Get-FileHash $d -Algorithm SHA256).Hash
    $newHash=(Get-FileHash $s -Algorithm SHA256).Hash
    if($oldHash -ne $newHash){throw "File $name da duoc sua tren may ban. Khong tu ghi de. Backup: $backup"}
  } else {Copy-Item $s $d}
}
$apiSource=Join-Path $SourceRoot 'RangHamMat.Kiosk.Api'
$apiTarget=Join-Path $ProjectRoot 'RangHamMat.Kiosk.Api'
if(Test-Path $apiTarget){
  Write-Warning 'Project Kiosk API da ton tai, khong ghi de. Neu can update, doi chieu thu cong.'
} else {
  Copy-Item $apiSource $apiTarget -Recurse
}
if(Get-Command dotnet -ErrorAction SilentlyContinue){
  & dotnet sln (Join-Path $ProjectRoot 'BenhVienRangHamMat.sln') add (Join-Path $apiTarget 'RangHamMat.Kiosk.Api.csproj')
  if($LASTEXITCODE -ne 0){Write-Warning 'Khong the them project API vao solution. Co the Add Existing Project trong VS2022.'}
} else {Write-Warning 'Khong tim thay dotnet CLI. Hay Add Existing Project bang Visual Studio 2022.'}
Write-Host "Da cai dat code Kiosk AI; backup: $backup" -ForegroundColor Green
Write-Host 'Can chay SQL\07_KioskAI_TiepNhan.sql tren database dang su dung (neu chua chay).'
Write-Host 'Sau do dotnet run --project RangHamMat.Kiosk.Api\RangHamMat.Kiosk.Api.csproj'
