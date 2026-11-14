<#
.SYNOPSIS
Kiểm quy tắc xác minh gói XR04 và giới hạn đầu vào bằng các tệp giả lập trong obj.
.DESCRIPTION
Tạo gói có kiểm soát để thử hash, tệp thiếu/thừa, manifest và dấu INCOMPLETE.
Các lời gọi CLI dùng đầu vào phải bị từ chối trước kết nối SQL; cuối cùng tổng hợp PASS/FAIL.
#>
param()
$ErrorActionPreference='Stop'
$work=Join-Path (Split-Path $PSScriptRoot -Parent) ('obj/XR04Handover/tests-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work -Force | Out-Null
$helper=Join-Path $PSScriptRoot 'XR04BanGiaoSupport.ps1';if(Test-Path -LiteralPath $helper){. $helper}
$passed=0;$failed=0
function Check([string]$Name,[scriptblock]$Body){try{& $Body;$script:passed++;Write-Output ('PASS: '+$Name)}catch{$script:failed++;Write-Output ('FAIL: '+$Name+' | '+$_.Exception.Message)}}
function Reject([scriptblock]$Body){$rejected=$false;try{& $Body | Out-Null}catch [Management.Automation.CommandNotFoundException]{throw}catch{$rejected=$true};if(!$rejected){throw 'Expected rejection'}}
function Fixture {
 $path=Join-Path $work ([Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $path | Out-Null
 [IO.File]::WriteAllText((Join-Path $path 'tool.dll'),'controlled artifact')
 [IO.File]::WriteAllText((Join-Path $path 'guide.md'),'controlled guide')
 $items=Get-XR04FileManifest $path
 $value=[pscustomobject]@{Protocol='RHM.XR04.Kit.v1';ReadyForDeployment=$false;Files=@($items)}
 [IO.File]::WriteAllText((Join-Path $path 'package.json'),($value|ConvertTo-Json -Depth 6))
 return $path
}
Check 'gói đúng hash và Results riêng vẫn kiểm được' {$p=Fixture;New-Item -ItemType Directory -Path (Join-Path $p 'Results') | Out-Null;[IO.File]::WriteAllText((Join-Path $p 'Results/proof.json'),'result');Assert-XR04Package $p}
Check 'gói thiếu tệp bị từ chối' {$p=Fixture;Remove-Item -LiteralPath (Join-Path $p 'tool.dll');Reject {Assert-XR04Package $p}}
Check 'gói sửa tệp bị từ chối' {$p=Fixture;[IO.File]::WriteAllText((Join-Path $p 'tool.dll'),'changed');Reject {Assert-XR04Package $p}}
Check 'gói thêm payload bị từ chối' {$p=Fixture;[IO.File]::WriteAllText((Join-Path $p 'extra.dll'),'extra');Reject {Assert-XR04Package $p}}
Check 'payload tệp ẩn ngoài manifest cũng bị từ chối' {$p=Fixture;$file=Join-Path $p 'hidden.dll';[IO.File]::WriteAllText($file,'hidden payload');[IO.File]::SetAttributes($file,[IO.FileAttributes]::Hidden);Reject {Assert-XR04Package $p}}
Check 'payload trong thư mục ẩn ngoài manifest bị từ chối' {$p=Fixture;$folder=Join-Path $p 'HiddenPayload';New-Item -ItemType Directory -Path $folder | Out-Null;[IO.File]::SetAttributes($folder,[IO.FileAttributes]::Hidden);[IO.File]::WriteAllText((Join-Path $folder 'extra.dll'),'hidden payload');Reject {Assert-XR04Package $p}}
Check 'manifest không được thoát thư mục gói' {$p=Fixture;$v=Get-Content (Join-Path $p 'package.json') -Raw|ConvertFrom-Json;$v.Files[0].Path='../outside.dll';$v|ConvertTo-Json -Depth 6|Set-Content (Join-Path $p 'package.json');Reject {Assert-XR04Package $p}}
Check 'gói INCOMPLETE và trạng thái phát hành sai bị từ chối' {$p=Fixture;[IO.File]::WriteAllText((Join-Path $p 'INCOMPLETE.txt'),'unfinished');Reject {Assert-XR04Package $p};$q=Fixture;$v=Get-Content (Join-Path $q 'package.json') -Raw|ConvertFrom-Json;$v.ReadyForDeployment=$true;$v|ConvertTo-Json -Depth 6|Set-Content (Join-Path $q 'package.json');Reject {Assert-XR04Package $q}}
Check 'guard DB nghiệp vụ trước khi kết nối' {Reject {Assert-XR04Input 'Probe' 'QLBenhVienRangHamMat' '\\server\share' ''};Assert-XR04Input 'Probe' 'RHM_Hospital_Test_Kit' '\\server\share' ''}
Check 'guard kho local hoặc device path' {foreach($p in @('D:\images','\\?\D:\images','\\.\pipe\x','\\server')){Reject {Assert-XR04Input 'Probe' 'RHM_Hospital_Test_Kit' $p ''}}}
Check 'restore phải có tên DB thử mới' {Reject {Assert-XR04Input 'KhoiPhuc' 'RHM_Hospital_Test_Kit' '\\server\share' 'QLBenhVienRangHamMat'};Assert-XR04Input 'KhoiPhuc' 'RHM_Hospital_Test_Kit' '\\server\share' 'RHM_Hospital_Test_Restored'}
Check 'Compare và KiemTraGoi không cần kết nối' {Assert-XR04Input 'Compare' '' '' '';Assert-XR04Input 'KiemTraGoi' '' '' ''}
Check 'nguồn P6 đổi hoặc thiếu bị từ chối' {
 $old=@([pscustomobject]@{Path='BUS/Test.cs';Bytes=1;Sha256=('A'*64)})
 $same=@([pscustomobject]@{Path='BUS/Test.cs';Bytes=1;Sha256=('A'*64)})
 Assert-XR04Source $old $same
 Reject {Assert-XR04Source $old @([pscustomobject]@{Path='BUS/Test.cs';Bytes=1;Sha256=('B'*64)})}
 Reject {Assert-XR04Source $old @()}
}
Check 'nguồn mới ngoài bốn script bàn giao phải kiểm lại P6' {
 $old=@([pscustomobject]@{Path='BUS/Test.cs';Bytes=1;Sha256=('A'*64)})
 $new=@($old)+@([pscustomobject]@{Path='BUS/New.cs';Bytes=1;Sha256=('B'*64)})
 Reject {Assert-XR04Source $old $new}
 Assert-XR04Source $old (@($old)+@([pscustomobject]@{Path='tools/TaoBoNghiemThuXR04.ps1';Bytes=1;Sha256=('B'*64)}))
}
function RejectCli([string]$File,[string[]]$Arguments,[string]$Pattern){
 $path=Join-Path $PSScriptRoot $File;if(!(Test-Path -LiteralPath $path)){throw ('Missing CLI: '+$File)}
 $log=Join-Path $work ([Guid]::NewGuid().ToString('N')+'.log');$old=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& powershell -NoProfile -ExecutionPolicy Bypass -File $path @Arguments *> $log;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$old}
 if($code -eq 0 -or [IO.File]::ReadAllText($log) -notmatch $Pattern){throw 'CLI không từ chối đúng guard trước thao tác'}
}
Check 'wrapper process từ chối DB thật trước runtime/SQL' {RejectCli 'ChayNghiemThuXR04.ps1' @('-CheDo','Probe','-Database','QLBenhVienRangHamMat','-MayChu','XR04_DO_NOT_CONTACT','-ThuMucKho','\\server\share') 'Chỉ nhận database thử'}
Check 'builder process từ chối mã gói trước tạo thư mục' {RejectCli 'TaoBoNghiemThuXR04.ps1' @('-ThuMucP6','XR04_DO_NOT_READ','-MaGoi','../bad') 'Mã gói'}
Check 'marker INCOMPLETE ở thư mục cha cũng khóa gói' {
 $p=Fixture;$marker=Join-Path (Split-Path $p -Parent) 'INCOMPLETE.txt'
 try{[IO.File]::WriteAllText($marker,'unfinished packaging');Reject {Assert-XR04Package $p}}finally{Remove-Item -LiteralPath $marker}
}
Write-Output "RESULT: $passed passed, $failed failed"
if($failed){exit 1}
