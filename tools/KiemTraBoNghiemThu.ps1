<#
.SYNOPSIS
Kiểm logic đọc kết quả, lấy baseline và đối chiếu bằng chứng của bộ nghiệm thu P6.
.DESCRIPTION
Dùng log/JSON và tiến trình con có kiểm soát trong obj/P6Evidence để kiểm mã thoát,
marker, hash baseline và các guard CLI. Proof giả lập chỉ kiểm logic đối chiếu hai máy.
#>
param()
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$work=Join-Path $root ('obj/P6Evidence/runner-tests-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work -Force | Out-Null
$helper=Join-Path $PSScriptRoot 'NghiemThuSupport.ps1'
if(Test-Path -LiteralPath $helper){. $helper}
$passed=0;$failed=0
function Check([string]$Name,[scriptblock]$Body){
 try {& $Body;$script:passed++;Write-Output ('PASS: '+$Name)}
 catch {$script:failed++;Write-Output ('FAIL: '+$Name+' | '+$_.Exception.Message)}
}
function Outcome([string]$Body,[int]$Code=0,[string]$Mode='Summary',[int]$Minimum=2,[string[]]$Required=@()){
 $path=Join-Path $work ([Guid]::NewGuid().ToString('N')+'.log')
 [IO.File]::WriteAllText($path,$Body,[Text.UTF8Encoding]::new($false))
 Get-P6Outcome -Log $path -ExitCode $Code -Mode $Mode -MinimumPassed $Minimum -RequiredPatterns $Required
}
Check 'summary comma và semicolon đều xác nhận đủ kiểm' {
 foreach($body in @('RESULT: 2 passed, 0 failed','RESULT: 2 passed; 0 failed')){if((Outcome $body).Status -ne 'PASS'){throw 'Không đọc đúng summary'}}
}
Check 'exit khác0 không được PASS dù summary xanh' {if((Outcome 'RESULT: 2 passed, 0 failed' 9).Status -ne 'FAIL'){throw 'Bỏ qua exit code'}}
Check 'dòng FAIL không bị summary xanh che' {if((Outcome "FAIL: must fail`nRESULT: 2 passed, 0 failed").Status -ne 'FAIL'){throw 'Bỏ qua FAIL'}}
Check 'summary lỗi trước đó không bị summary xanh cuối che' {if((Outcome "RESULT: 2 passed, 1 failed`nRESULT: 3 passed, 0 failed").Status -ne 'FAIL'){throw 'Bỏ qua summary lỗi trước đó'}}
Check 'thiếu summary hoặc số kiểm thấp không được PASS' {
 foreach($body in @('Build succeeded.','RESULT: 1 passed, 0 failed','RESULT: 2 passed, 1 failed')){if((Outcome $body).Status -ne 'FAIL'){throw 'Chạy thiếu/lỗi vẫn PASS'}}
}
Check 'báo cáo không summary phải đủ PASS và marker bắt buộc' {
 if((Outcome "PASS A`nPASS B" 0 'Lines' 2 @('^PASS B$')).Status -ne 'PASS'){throw 'Không đếm PASS báo cáo'}
 if((Outcome 'PASS A' 0 'Lines' 2).Status -ne 'FAIL'){throw 'Báo cáo thiếu vẫn PASS'}
 if((Outcome "PASS A`nPASS B" 0 'Lines' 2 @('^PASS C$')).Status -ne 'FAIL'){throw 'Thiếu bằng chứng vẫn PASS'}
}
Check 'operation không được qua nếu không có marker' {
 if((Outcome 'done' 0 'Marker' 0).Status -ne 'FAIL'){throw 'Operation không bằng chứng vẫn PASS'}
 if((Outcome 'P6_COMPLETE' 0 'Marker' 0 @('^P6_COMPLETE$')).Status -ne 'PASS'){throw 'Operation hợp lệ bị từ chối'}
}
Check 'gọi child thật phải giữ nguyên exit khác0' {
 $child=Join-Path $work 'child.ps1';[IO.File]::WriteAllText($child,"Write-Output 'RESULT: 2 passed, 0 failed'; exit 7",[Text.UTF8Encoding]::new($false))
 $out=Invoke-P6Tool -Name 'child-error' -Tool $child -ToolArgs @() -Directory $work -MinimumPassed 2
 if($out.Status -ne 'FAIL' -or $out.ExitCode -ne 7){throw 'Không giữ exit của child'}
}
Check 'SQL gốc lấy đúng byte, khớp bản CRLF đã khóa nguồn' {
 $baseline=Export-P6Baseline -Root $root -Destination (Join-Path $work 'baseline.sql')
 if($baseline.CrlfSha256 -ne '1EEB1EF8232485C86B18700BB12A81149268D9B7E495E64F6E6E06541BA56BCB'){throw 'SQL gốc khác nguồn đã khóa'}
 if((Get-Item -LiteralPath $baseline.Path).Length -ne 1080881){throw 'Blob git bị đổi byte'}
}
function Proof([string]$Machine,[hashtable]$Change=@{}){
 $value=[ordered]@{Protocol='RHM.Xray.SharedStorageProbe.v1';Machine=$Machine;SqlServer='SQL-TEST';Database='RHM_Hospital_Test_P6_Proof';StorageRoot='\\SQL-TEST\RHM\fixture';MetadataSha256=('A'*64);Images=15;HiddenImages=2;OpenedImages=13;AllHashesVerified=$true;UsedApplicationPermissions=$true}
 foreach($key in $Change.Keys){$value[$key]=$Change[$key]}
 $path=Join-Path $work ('proof-'+[Guid]::NewGuid().ToString('N')+'.json');[IO.File]::WriteAllText($path,($value|ConvertTo-Json),[Text.UTF8Encoding]::new($false));$path
}
Check 'proof cùng hostname không chứng minh hai máy' {
 $a=Proof 'TEST-A';$b=Proof 'TEST-A';if((Compare-P6StorageProof $a $b).Status -ne 'FAIL'){throw 'Cùng máy bị coi là hai máy'}
}
Check 'proof khác SQL/kho/hash hoặc cờ không phải boolean bị từ chối' {
 $a=Proof 'TEST-A'
 foreach($change in @(@{SqlServer='OTHER'},@{StorageRoot='\\OTHER\share'},@{MetadataSha256=('B'*64)},@{AllHashesVerified='false'})){
  $b=Proof 'TEST-B' $change;if((Compare-P6StorageProof $a $b).Status -ne 'FAIL'){throw 'Proof không tương đương vẫn được chấp nhận'}
 }
}
Check 'logic đối chiếu proof đủ trường từ hai hostname khác chấp nhận' {
 $a=Proof 'UNIT-HOST-A';$b=Proof 'UNIT-HOST-B';if((Compare-P6StorageProof $a $b).Status -ne 'PASS_SHARED_STORAGE'){throw 'Logic so proof hợp lệ bị từ chối'}
 # Đây là test logic bằng JSON kiểm soát, không phải bằng chứng đã chạy trên hai máy.
}
Check 'proof path local hoặc thiếu ảnh ẩn không đủ điều kiện' {
 $a=Proof 'TEST-A';foreach($change in @(@{StorageRoot='D:\local'},@{HiddenImages=0;OpenedImages=15})){
  $b=Proof 'TEST-B' $change;if((Compare-P6StorageProof $a $b).Status -ne 'FAIL'){throw 'Thiếu điều kiện kho dùng chung vẫn PASS'}
 }
}
Check 'probe UNC từ chối database production trước khi kết nối' {
 $log=Join-Path $work 'probe-production.log';$oldPreference=$ErrorActionPreference
 try {$ErrorActionPreference='Continue';& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'KiemTraKhoAnhHaiMay.ps1') -Database QLBenhVienRangHamMat -ThuMucKho '\\NO-SERVER\fixture' *> $log;$probeExit=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference}
 if($probeExit -eq 0 -or [IO.File]::ReadAllText($log) -notmatch 'Chỉ nhận database fixture'){throw 'Probe không từ chối đúng giới hạn fixture'}
}
Check 'runner từ chối mã không hợp lệ trước kết nối SQL' {
 $log=Join-Path $work 'invalid-run.log';$oldPreference=$ErrorActionPreference
 try {$ErrorActionPreference='Continue';& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'KiemTraNghiemThu.ps1') -MaLanChay '../invalid;run' -MayChu 'P6_DO_NOT_CONTACT' *> $log;$guardExit=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference}
 if($guardExit -eq 0 -or [IO.File]::ReadAllText($log) -notmatch 'Mã lần chạy chỉ nhận') {throw 'Mã sai không bị chặn đúng bước'}
}
Check 'runner không ghi đè lần nghiệm thu đã có' {
 $id='guard_'+[Guid]::NewGuid().ToString('N');$existing=Join-Path $root ('obj/P6Evidence/run-'+$id);New-Item -ItemType Directory -Path $existing|Out-Null
 $log=Join-Path $work 'existing-run.log';$oldPreference=$ErrorActionPreference
 try {$ErrorActionPreference='Continue';& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'KiemTraNghiemThu.ps1') -MaLanChay $id -MayChu 'P6_DO_NOT_CONTACT' *> $log;$guardExit=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference}
 if($guardExit -eq 0 -or [IO.File]::ReadAllText($log) -notmatch 'Lần nghiệm thu đã tồn tại' -or @(Get-ChildItem -LiteralPath $existing).Count){throw 'Đã ghi đè/không từ chối trước SQL'}
}
Write-Output "RESULT: $passed passed, $failed failed"
if($failed){exit 1}
