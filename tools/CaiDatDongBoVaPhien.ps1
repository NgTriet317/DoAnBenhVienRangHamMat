<#
.SYNOPSIS
Xuất hoặc áp dụng riêng phần cài đồng bộ/phiên ở cuối SQL chính, không nạp snapshot dữ liệu.
#>
param([string]$MayChu='.',[string]$Database='QLBenhVienRangHamMat',[switch]$ApDung)
$ErrorActionPreference='Stop'
if($Database -notmatch '^[A-Za-z0-9_]+$' -or $Database -in @('master','model','msdb','tempdb')){throw 'Chỉ nhận tên database ứng dụng hợp lệ; không chạy trên database hệ thống.'}
$root=Split-Path $PSScriptRoot -Parent
$source=[IO.File]::ReadAllText((Join-Path $root 'Database/QLBenhVienRangHamMat.sql'))
$sections=@()
foreach($name in @('RHM SINGLE ACCOUNT SESSION','RHM CROSS-MACHINE CHANGE TRACKING')){
 $begin='-- BEGIN '+$name
 $end='-- END '+$name
 $first=$source.IndexOf($begin,[StringComparison]::Ordinal)
 $last=$source.IndexOf($end,[StringComparison]::Ordinal)
 if($first -lt 0 -or $last -le $first){throw "Không tìm đủ marker $name trong SQL chính."}
 $sections+=$source.Substring($first,$last+$end.Length-$first)
}
$scratch=Join-Path $root 'obj/SessionSyncInstall'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$output=Join-Path $scratch 'install.sql'
[IO.File]::WriteAllText($output,("USE [$Database];`r`nGO`r`n"+($sections -join "`r`n")),[Text.UTF8Encoding]::new($false))
Write-Output "SQL đã xuất: $output"
if(!$ApDung){Write-Output 'Chưa áp dụng vào SQL Server. Thêm -ApDung khi đã duyệt phần cài đặt.';return}
& sqlcmd -S $MayChu -d $Database -E -C -b -f65001 -t60 -i $output
if($LASTEXITCODE -ne 0){throw 'Cài đặt chưa hoàn tất; xem lỗi SQL phía trên. Script có thể chạy lại sau khi xử lý lỗi.'}
