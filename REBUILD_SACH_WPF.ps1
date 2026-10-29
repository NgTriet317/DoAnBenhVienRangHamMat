# Chay tai thu muc chua file BenhVienRangHamMat.sln. Khong sua database va ma nguon.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root 'BenhVienRangHamMat.sln'
if (-not (Test-Path $solution)) { throw 'Khong tim thay BenhVienRangHamMat.sln. Hay chay trong thu muc ma nguon.' }
Write-Host 'Dung WPF/Visual Studio truoc khi chay script nay.' -ForegroundColor Yellow
$proj = @('RangHamMat.Wpf','BUS','DAL','ET','RangHamMat.WpfSmoke','RangHamMat.Checks','RangHamMat.FinanceChecks','RangHamMat.PersistenceChecks','RangHamMat.SchemaChecks')
foreach ($p in $proj) {
 foreach ($d in @('bin','obj')) {
  $path = Join-Path (Join-Path $root $p) $d
  if (Test-Path $path) { Remove-Item -LiteralPath $path -Recurse -Force; Write-Host "Xoa bo nho build: $p/$d" }
 }
}
$vs = Join-Path $root '.vs'
if (Test-Path $vs) { Remove-Item -LiteralPath $vs -Recurse -Force; Write-Host 'Xoa bo nho Visual Studio: .vs' }
Write-Host 'Da don cache. Mo Visual Studio 2022 va chon Rebuild Solution.' -ForegroundColor Green
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
 $answer = Read-Host 'Build ngay bang dotnet? (Y/N)'
 if ($answer -match '^[yY]$') {
  & dotnet restore $solution
  if ($LASTEXITCODE -ne 0) { throw 'Restore that bai. Xem thong bao phia tren.' }
  & dotnet build $solution -c Debug --no-restore -t:Rebuild
  if ($LASTEXITCODE -ne 0) { throw 'Build that bai. Khong duoc chay exe cu.' }
  Write-Host 'Build thanh cong.' -ForegroundColor Green
 }
}
