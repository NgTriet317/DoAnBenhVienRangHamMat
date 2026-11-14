<#
.SYNOPSIS
Dựng bộ kiểm đối soát dữ liệu và tùy chọn kiểm chuỗi chứng từ DEMO P5.
.DESCRIPTION
Chỉ nhận database RHM_Hospital_Test_. Các ca gây lỗi dữ liệu chạy trong giao dịch
rollback; KiemTraTrinhDien bổ sung đối chiếu báo cáo và giao diện trên DEMO đã chuẩn bị.
.PARAMETER Database
Database thử chứa schema hiện hành và dữ liệu phù hợp với chế độ kiểm.
.PARAMETER MayChu
SQL Server dùng chạy các ca kiểm đối soát.
.PARAMETER KiemTraTrinhDien
Truyền --demo để kiểm số liệu, chứng từ và ảnh báo cáo của bộ dữ liệu P5.
#>
param([Parameter(Mandatory=$true)][string]$Database,[string]$MayChu='.',[switch]$KiemTraTrinhDien)
$ErrorActionPreference='Stop'
if($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$'){throw 'Chỉ kiểm trên database thử RHM_Hospital_Test_.'}
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root 'obj/P5Checks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$project=Join-Path $scratch 'P5Checks.csproj'
[IO.File]::WriteAllText($project,'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" /></ItemGroup></Project>',[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'P5Checks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'P5Artifacts.cs.txt') -Destination (Join-Path $scratch 'P5Artifacts.cs') -Force
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false -v quiet
if($LASTEXITCODE -ne 0){throw 'Restore bộ kiểm P5 thất bại.'}
$checkArgs=@($MayChu,$Database)
if($KiemTraTrinhDien){$checkArgs+='--demo'}
& dotnet run --project $project --no-restore -p:NuGetAudit=false -v quiet -- @checkArgs
if($LASTEXITCODE -ne 0){throw 'Kiểm P5 thất bại; xem FAIL.'}
