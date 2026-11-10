# Helper thuần cho gói nghiệm thu; không kết nối SQL hoặc chạy child process.
function Get-XR04FileManifest {
 param([string]$Root)
 $base=(Get-Item -LiteralPath $Root).FullName.TrimEnd('\','/')
 @(Get-ChildItem -LiteralPath $base -File -Recurse -Force | Where-Object {
  $relative=$_.FullName.Substring($base.Length+1).Replace('\','/')
  $relative -ne 'package.json' -and $relative -notmatch '^Results/'
 } | Sort-Object FullName | ForEach-Object {
  if($_.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Gói không được chứa reparse point.'}
  [pscustomobject]@{Path=$_.FullName.Substring($base.Length+1).Replace('\','/');Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
 })
}
function Assert-XR04Package {
 param([string]$Root,[switch]$DangDongGoi)
 $base=(Get-Item -LiteralPath $Root).FullName
 if((Test-Path -LiteralPath (Join-Path $base 'INCOMPLETE.txt')) -or (!$DangDongGoi -and (Test-Path -LiteralPath (Join-Path (Split-Path $base -Parent) 'INCOMPLETE.txt')))){throw 'Gói INCOMPLETE, chưa được sử dụng.'}
 $manifest=Get-Content -LiteralPath (Join-Path $base 'package.json') -Raw | ConvertFrom-Json
 if($manifest.Protocol -ne 'RHM.XR04.Kit.v1' -or $manifest.ReadyForDeployment -isnot [bool] -or $manifest.ReadyForDeployment){throw 'Sai protocol/trạng thái gói XR04.'}
 if(!$manifest.Files.Count){throw 'Gói không có danh mục tệp.'}
 $actual=@{};foreach($entry in (Get-XR04FileManifest $base)){$actual[$entry.Path]=$entry}
 $seen=@{}
 foreach($entry in $manifest.Files){
  if(!$entry.Path -or $entry.Path -match '(^[\\/]|:|\\|(^|/)\.\.?(/|$))' -or $entry.Path -in @('package.json') -or $entry.Path -match '^Results/' -or $seen.ContainsKey($entry.Path)){throw 'Đường dẫn manifest không hợp lệ/trùng.'}
  $seen[$entry.Path]=$true
  if(!$actual.ContainsKey($entry.Path) -or $entry.Sha256 -notmatch '^[A-Fa-f0-9]{64}$' -or $actual[$entry.Path].Sha256 -cne $entry.Sha256 -or $actual[$entry.Path].Bytes -ne $entry.Bytes){throw ('Tệp thiếu/khác hash: '+$entry.Path)}
 }
 if($actual.Count -ne $seen.Count){throw 'Gói có tệp ngoài manifest.'}
 foreach($folder in Get-ChildItem -LiteralPath $base -Directory -Recurse -Force){
  if($folder.FullName.Substring($base.Length+1) -notmatch '^Results([\\/]|$)' -and ($folder.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Gói không được chứa thư mục liên kết.'}
 }
}
function Assert-XR04Input {
 param([string]$Mode,[string]$Database,[string]$StorageRoot,[string]$NewDatabase)
 if($Mode -in @('Compare','KiemTraGoi')){return}
 if($Mode -notin @('Probe','KiemTraKho','SaoLuu','KhoiPhuc','XacMinhPhucHoi')){throw 'Chế độ XR04 không hợp lệ.'}
 if($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$'){throw 'Chỉ nhận database thử RHM_Hospital_Test_.'}
 if($StorageRoot -notmatch '^\\\\[^\\]+\\[^\\]+' -or $StorageRoot.StartsWith('\\?\') -or $StorageRoot.StartsWith('\\.\')){throw 'Kho ảnh phải là UNC thông thường.'}
 if($Mode -eq 'KhoiPhuc' -and ($NewDatabase -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$' -or $NewDatabase -eq $Database)){throw 'Phục hồi cần tên database thử mới khác nguồn.'}
}
function Assert-XR04Source {
 param([object[]]$Accepted,[object[]]$Current)
 if(!$Accepted.Count){throw 'Thiếu manifest nguồn P6.'}
 $actual=@{};foreach($entry in $Current){$actual[$entry.Path]=$entry}
 $seen=@{}
 foreach($entry in $Accepted){
  if($seen.ContainsKey($entry.Path) -or !$actual.ContainsKey($entry.Path) -or $entry.Sha256 -notmatch '^[A-Fa-f0-9]{64}$' -or $actual[$entry.Path].Sha256 -cne $entry.Sha256 -or $actual[$entry.Path].Bytes -ne $entry.Bytes){throw ('Nguồn khác P6: '+$entry.Path)}
  $seen[$entry.Path]=$true
 }
 foreach($entry in $Current){
  if(!$seen.ContainsKey($entry.Path) -and $entry.Path -notmatch '^tools/(TaoBoNghiemThuXR04|KiemTraBoBanGiaoXR04|XR04BanGiaoSupport|ChayNghiemThuXR04)\.ps1$'){throw ('Nguồn phát sinh cần kiểm lại P6: '+$entry.Path)}
 }
}
