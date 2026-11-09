<#
.SYNOPSIS
Cập nhật vùng dữ liệu snapshot trong file SQL chính từ database nguồn.
.DESCRIPTION
Script ghi Database/QLBenhVienRangHamMat.sql khi được chạy: sao lưu before.sql trong obj/SqlSnapshot/<thời gian>, xuất snapshot, rồi chỉ thay vùng giữa hai marker quản lý. Chỉ dùng khi cần đóng gói lại dữ liệu nguồn.
.NOTES
Các đường dẫn làm việc được tính từ vị trí script; lỗi được dừng và báo cho nơi gọi.
#>
param([string]$MayChu='.',[string]$Database='QLBenhVienRangHamMat')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$target=Join-Path $root 'Database/QLBenhVienRangHamMat.sql'
$contents=[IO.File]::ReadAllText($target)
# Xác định vùng snapshot quản lý; thiếu hoặc sai thứ tự marker phải dừng trước khi ghi file SQL.
$start=$contents.IndexOf('-- BEGIN RHM DATA SNAPSHOT:')
$endMarker='-- END RHM DATA SNAPSHOT'
$end=$contents.IndexOf($endMarker)
if($start -lt 0 -or $end -le $start){throw 'File SQL chưa có vùng snapshot được quản lý.'}
$stamp=Get-Date -Format 'yyyyMMdd_HHmmss'
# Thư mục tạm chứa project kiểm tra, mã sinh và bằng chứng; không phải project sản phẩm.
$scratch=Join-Path $root ('obj/SqlSnapshot/'+$stamp)
New-Item -ItemType Directory -Path $scratch -Force|Out-Null
Copy-Item -LiteralPath $target -Destination (Join-Path $scratch 'before.sql')
& (Join-Path $PSScriptRoot 'XuatDuLieuSql.ps1') -MayChu $MayChu -Database $Database -Output ('obj/SqlSnapshot/'+$stamp+'/bootstrap.sql') -Manifest ('obj/SqlSnapshot/'+$stamp+'/manifest.json')
$snapshot=[IO.File]::ReadAllText((Join-Path $scratch 'bootstrap.sql'))
# Giữ phần schema/nâng cấp ngoài vùng snapshot, chỉ ghép dữ liệu vừa xuất vào giữa hai marker.
$updated=$contents.Substring(0,$start)+$snapshot+$contents.Substring($end+$endMarker.Length)
[IO.File]::WriteAllText($target,$updated,[Text.UTF8Encoding]::new($true))
Write-Output 'Đã cập nhật dữ liệu hiện tại vào file SQL; database nguồn chỉ được đọc, không bị thay đổi.'
