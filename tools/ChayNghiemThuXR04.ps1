<#
.SYNOPSIS
Chạy công cụ XR04 đã đóng gói bằng .NET runtime; chỉ dùng database thử và kho UNC.
.NOTES
Không build/restore project, không tự nghiệm thu hai máy, không ghi đè kết quả.
#>
param(
 [ValidateSet('KiemTraGoi','Probe','Compare','KiemTraKho','SaoLuu','KhoiPhuc','XacMinhPhucHoi')][string]$CheDo='KiemTraGoi',
 [string]$MayChu='.',[string]$Database,[string]$ThuMucKho,[string]$DatabaseMoi,
 [string]$TepBangChung,[string]$DoiChieuVoi,[string]$ThuMucKetQua,
 [string]$ThuMucBanSao,[string]$BanSao,[string]$ThuMucSqlServer
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'XR04BanGiaoSupport.ps1')
Assert-XR04Input $CheDo $Database $ThuMucKho $DatabaseMoi
Assert-XR04Package $PSScriptRoot
. (Join-Path $PSScriptRoot 'NghiemThuSupport.ps1')
if($CheDo -eq 'KiemTraGoi'){Write-Output 'PASS: XR04 package files verified; ReadyForDeployment=false';exit 0}
if(!$ThuMucKetQua){$ThuMucKetQua=Join-Path $PSScriptRoot ('Results/'+$CheDo+'-'+[DateTime]::UtcNow.ToString('yyyyMMdd_HHmmss')+'-'+[Guid]::NewGuid().ToString('N'))}
$resultDirectory=[IO.Path]::GetFullPath($ThuMucKetQua)
if(Test-Path -LiteralPath $resultDirectory){throw 'Thư mục kết quả đã tồn tại; không ghi đè.'}
for($parent=[IO.DirectoryInfo]::new($resultDirectory);$null -ne $parent;$parent=$parent.Parent){if($parent.Exists -and ($parent.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Thư mục kết quả không được qua liên kết.'}}
New-Item -ItemType Directory -Path $resultDirectory | Out-Null
$record=[ordered]@{Protocol='RHM.XR04.KitResult.v1';Mode=$CheDo;Machine=[Environment]::MachineName;StartedUtc=[DateTime]::UtcNow;Status='FAIL';ReadyForDeployment=$false;Scope='SingleHostOperation';Failure=$null}
try {
 if($CheDo -eq 'Compare'){
  if(!$TepBangChung -or !$DoiChieuVoi){throw 'Cần hai proof từ hai máy.'}
  $comparison=Compare-P6StorageProof $DoiChieuVoi $TepBangChung
  Write-P6Json (Join-Path $resultDirectory 'comparison.json') $comparison
  if($comparison.Status -ne 'PASS_SHARED_STORAGE'){throw ('Chưa đạt hai máy: '+$comparison.Reason)}
  $record.Scope='SharedStorageOnly';Write-Output 'SHARED_STORAGE_TWO_HOSTS=PASS'
 } else {
  if(!(Get-Command dotnet -ErrorAction SilentlyContinue)){throw 'Cần cài .NET8 runtime; VerifyRestore cần Desktop Runtime8.'}
  $runtimes=@(& dotnet --list-runtimes)
  if($LASTEXITCODE -ne 0 -or !($runtimes -match '^Microsoft.NETCore.App 8\.')){throw 'Thiếu Microsoft.NETCore.App8.'}
  if($CheDo -eq 'XacMinhPhucHoi' -and !($runtimes -match '^Microsoft.WindowsDesktop.App 8\.')){throw 'Thiếu Windows Desktop Runtime8.'}
  $mode='Marker';$minimum=0;$patterns=@();$toolArgs=@()
  if($CheDo -eq 'Probe'){
   if(!$TepBangChung){$TepBangChung=Join-Path $resultDirectory 'proof.json'}
   if(Test-Path -LiteralPath $TepBangChung){throw 'Proof đã tồn tại; không ghi đè.'}
   $tool='Probe/Probe.dll';$toolArgs=@($MayChu,$Database,$ThuMucKho,[IO.Path]::GetFullPath($TepBangChung));$patterns=@('^PROBE_COMPLETE=','^PASS: UNC image probe;')
  } elseif($CheDo -eq 'XacMinhPhucHoi'){
   $tool='Verify/Verify.dll';$toolArgs=@($MayChu,$Database,'--restore',$ThuMucKho);$mode='Summary';$minimum=2
  } else {
   $storageMode=if($CheDo -eq 'KiemTraKho'){'KiemTra'}else{$CheDo}
   $tool='Storage/Storage.dll';$toolArgs=@('--mode',$storageMode,'--server',$MayChu,'--db',$Database,'--root',$ThuMucKho)
   if($CheDo -eq 'KiemTraKho'){$patterns=@('^RESULT: \d+ metadata, 0 storage issues; no files deleted\s*$')}
   if($CheDo -eq 'SaoLuu'){
    if(!$ThuMucBanSao){throw 'Cần ThuMucBanSao.'};$toolArgs+=@('--output',$ThuMucBanSao);$patterns=@('^BACKUP_COMPLETE=','^PASS: verified SQL COPY_ONLY/CHECKSUM')
   }
   if($CheDo -eq 'KhoiPhuc'){
    if(!$BanSao){throw 'Cần BanSao từ BACKUP_COMPLETE.'};$toolArgs+=@('--archive',$BanSao,'--new-db',$DatabaseMoi);$patterns=@('^RESTORE_COMPLETE=','^PASS: \d+ image files and SQL metadata restored; hidden assets retained\.')
   }
   if($ThuMucSqlServer){$toolArgs+=@('--sql-directory',$ThuMucSqlServer)}
  }
  $dll=Join-Path $PSScriptRoot ('Tools/'+$tool);if(!(Test-Path -LiteralPath $dll)){throw 'Gói thiếu tool đã biên dịch.'}
  $log=Join-Path $resultDirectory 'tool.log';$oldPreference=$ErrorActionPreference
  Push-Location -LiteralPath $resultDirectory
  try{$ErrorActionPreference='Continue';& dotnet $dll @toolArgs *> $log;$childExit=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference;Pop-Location}
  $outcome=Get-P6Outcome -Log $log -ExitCode $childExit -Mode $mode -MinimumPassed $minimum -RequiredPatterns $patterns
  $record.ExitCode=$childExit;$record.Log=$log;$record.Passed=$outcome.Passed
  if($outcome.Status -ne 'PASS'){throw ('Công cụ thất bại: '+$outcome.Reason+'; xem '+$log)}
  Get-Content -LiteralPath $log | Where-Object {$_ -match '^(PASS:|RESULT:|PROBE_|BACKUP_|RESTORE[D]?_)'} | Write-Output
 }
 $record.Status='PASS'
} catch {$record.Failure=$_.Exception.Message;throw}
finally {
 $record.FinishedUtc=[DateTime]::UtcNow
 Write-P6Json (Join-Path $resultDirectory 'result.json') ([pscustomobject]$record)
 Write-Output ('XR04_RESULT='+$resultDirectory);Write-Output 'XR04_READY_FOR_DEPLOYMENT=False'
}
