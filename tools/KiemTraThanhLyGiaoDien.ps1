<#
.SYNOPSIS
Chạy form WPF thật để kiểm bảo trì, thanh lý thiết bị và đồng bộ dữ liệu hiển thị.
.DESCRIPTION
Dựng project tạm trong obj/ThanhLyUiChecks. Bộ kiểm có commit hồ sơ thiết bị/thanh lý
trên database thử, giữ dữ liệu để kiểm chạy lại SQL và xuất ảnh làm bằng chứng.
.PARAMETER Database
Database thử đã có schema và dữ liệu nền, với tiền tố RHM_Hospital_Test_.
.PARAMETER MayChu
SQL Server dùng cho các thao tác form trong bộ kiểm.
#>
param([string]$Database='RHM_Hospital_Test_AssetDisposal_20261105',[string]$MayChu='.')
$ErrorActionPreference='Stop'
if($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$'){throw 'Chỉ chạy trên database kiểm thử cách ly.'}
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root 'obj/ThanhLyUiChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$xml='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" /></ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $scratch 'ThanhLyUiChecks.csproj'),$xml,[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ThanhLyUiChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
& dotnet run --project (Join-Path $scratch 'ThanhLyUiChecks.csproj') -p:NuGetAudit=false -- $MayChu $Database
if($LASTEXITCODE -ne 0){throw 'Kiểm tra giao diện thanh lý thất bại.'}
