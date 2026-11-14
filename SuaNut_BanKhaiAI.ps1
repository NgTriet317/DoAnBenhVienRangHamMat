param(
    [Parameter(Mandatory=$true)][string]$ProjectRoot
)
$ErrorActionPreference = 'Stop'
$ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)
$SourceRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$view = Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml'
$code = Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\PhieuHuongDan.xaml.cs'
if (-not (Test-Path $view) -or -not (Test-Path $code)) {
    throw 'Sai thu muc du an: phai chon thu muc co RangHamMat.Wpf\Views\PhieuHuongDan.xaml.'
}
$xml = [IO.File]::ReadAllText($view)
$cs = [IO.File]::ReadAllText($code)
if (-not $xml.Contains('x:Class="RangHamMat.Views.PhieuHuongDan"')) {
    throw 'Khong dung giao dien PhieuHuongDan cua du an. Khong sua tu dong.'
}
$needXml = $xml -notmatch 'Click\s*=\s*"MoTiepNhanAI_Click"'
$needCode = $cs -notmatch 'private\s+void\s+MoTiepNhanAI_Click\s*\('
$pattern = '(?s)<Button\b(?=[^>]*\bContent\s*=\s*"Làm mới danh sách")(?=[^>]*\bClick\s*=\s*"LamMoi_Click")[^>]*/>'
$match = [regex]::Match($xml,$pattern)
$anchor = [regex]::Match($cs, '(?m)^\s*private\s+void\s+LamMoi_Click\s*\(')
if ($needXml -and -not $match.Success) {
    throw 'Khong thay nut Lam moi danh sach trong PhieuHuongDan.xaml. Hay gui 2 file PhieuHuongDan.xaml va .xaml.cs de doi chieu; chua sua file.'
}
if ($needCode -and -not $anchor.Success) {
    throw 'Khong thay ham LamMoi_Click trong PhieuHuongDan.xaml.cs; chua sua file.'
}
$files = @(
 'ET\ET_AITIEPNHAN.cs',
 'BUS\BUS_AITIEPNHAN.cs',
 'DAL\DAL_AITIEPNHAN.cs',
 'RangHamMat.Wpf\Views\TiepNhanAI.xaml',
 'RangHamMat.Wpf\Views\TiepNhanAI.xaml.cs'
)
foreach($f in $files){
    if(-not (Test-Path (Join-Path $SourceRoot $f))){throw "Bo va thieu file: $f"}
}
$wpfUi = Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\TiepNhanAI.xaml'
$wpfCs = Join-Path $ProjectRoot 'RangHamMat.Wpf\Views\TiepNhanAI.xaml.cs'
if((Test-Path $wpfUi) -xor (Test-Path $wpfCs)) {
    throw 'Chi tim thay 1 trong 2 file TiepNhanAI.xaml / .xaml.cs: can doi chieu thu cong.'
}
if($needXml) {
    $new = @'
<StackPanel Grid.Column="1" Orientation="Horizontal" HorizontalAlignment="Right" VerticalAlignment="Center">
                <Button Content="Nhận bản khai AI" Style="{StaticResource NutChinh}"
                        Click="MoTiepNhanAI_Click" Margin="0,0,9,0" Padding="15,9"/>
                <Button Content="Làm mới danh sách" Style="{StaticResource NutPhu}"
                        Click="LamMoi_Click" VerticalAlignment="Center" Margin="0"/>
            </StackPanel>
'@
    $xml = $xml.Substring(0,$match.Index) + $new + $xml.Substring($match.Index+$match.Length)
}
if($needCode) {
    $method = @'
    // Mo cua so nhan phieu khai AI; nhan vien phai doi chieu va tao phieu kham that.
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
        {
            mainWindow?.OpenApp("BenhNhan");
        }
    }

'@
    $cs = $cs.Insert($anchor.Index,$method)
}
try {
    $null = [xml]$xml
} catch {throw "PhieuHuongDan.xaml khong hop le sau ghep: $($_.Exception.Message)"}
# Backup source, including any current AI-related files.
$backup = Join-Path $ProjectRoot ('Backup_SuaBanKhaiAI_' + (Get-Date -Format 'yyyyMMdd_HHmmss'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
foreach($file in @($view,$code)) { Copy-Item $file (Join-Path $backup (Split-Path -Leaf $file)) -Force }
$utf8Bom = New-Object System.Text.UTF8Encoding($true)
if($needXml) { [IO.File]::WriteAllText($view,$xml,$utf8Bom) }
if($needCode) { [IO.File]::WriteAllText($code,$cs,$utf8Bom) }
foreach($f in $files){
    $s=Join-Path $SourceRoot $f
    $d=Join-Path $ProjectRoot $f
    if(Test-Path $d){
        Write-Host "Giu nguyen file da co: $f"
    } else {
        New-Item -ItemType Directory -Path (Split-Path -Parent $d) -Force | Out-Null
        Copy-Item $s $d
        Write-Host "Da them file: $f" -ForegroundColor Green
    }
}
Write-Host "Da sua nut 'Nhan ban khai AI': $($needXml -or $needCode)" -ForegroundColor Green
Write-Host "Backup file cu: $backup"
Write-Host 'Kiem tra lai trong VS2022: Clean Solution -> Rebuild Solution; mo lai Phieu huong dan.'
Write-Host 'Neu nut hien nhung khong co phien, xem KIEM_TRA_DU_LIEU.sql va /health/db.'
