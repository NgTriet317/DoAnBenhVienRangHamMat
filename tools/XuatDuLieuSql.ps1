<#
.SYNOPSIS
Xuất hoặc đối chiếu snapshot dữ liệu từ SQL Server bằng công cụ SqlSnapshotExporter.
.DESCRIPTION
Output và Manifest là đường dẫn tính từ gốc dự án. ChiKiemTra truyền --verify để đối chiếu; script này không tự thay file SQL chính.
.NOTES
Các đường dẫn làm việc được tính từ vị trí script; lỗi được dừng và báo cho nơi gọi.
#>
param([string]$Database='QLBenhVienRangHamMat',[string]$MayChu='.',[string]$Output='obj/SqlSnapshot/bootstrap.sql',[string]$Manifest='obj/SqlSnapshot/manifest.json',[switch]$ChiKiemTra)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
# Thư mục tạm chứa project kiểm tra, mã sinh và bằng chứng; không phải project sản phẩm.
$scratch=Join-Path $root 'obj/SqlSnapshot'
New-Item -ItemType Directory -Path $scratch -Force|Out-Null
# Tạo đường dẫn project console tạm để chạy đúng mã kiểm chứng đi kèm.
$project=Join-Path $scratch 'SqlSnapshot.csproj'
[IO.File]::WriteAllText($project,'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../DAL/DAL.csproj" /></ItemGroup></Project>')
# Chép mã kiểm chứng được quản lý trong tools vào project tạm trước khi biên dịch.
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'SqlSnapshotExporter.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
# Chuyển đường dẫn đầu ra tuyệt đối cho công cụ xuất/đối chiếu, tránh phụ thuộc thư mục shell hiện tại.
$snapshotArgs=@($MayChu,$Database,(Join-Path $root $Output),(Join-Path $root $Manifest))
if($ChiKiemTra){$snapshotArgs+='--verify'}
# Biên dịch/chạy công cụ và chuyển các đối số đã chuẩn bị; mã thoát khác 0 làm bước bàn giao thất bại.
& dotnet run --project $project -p:NuGetAudit=false -v quiet -- @snapshotArgs
if($LASTEXITCODE -ne 0){throw 'Xuất/đối chiếu snapshot SQL thất bại.'}
