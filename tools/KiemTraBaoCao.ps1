<#
.SYNOPSIS
Dựng bộ kiểm công thức, phân quyền và hiển thị báo cáo WPF trong obj/ReportChecks.
.DESCRIPTION
Mặc định kiểm bằng dữ liệu trong bộ nhớ. Sql thêm đối chiếu SQL trong giao dịch rollback;
Render bổ sung ảnh giao diện khi chạy nhánh SQL. Dừng nếu restore hoặc bộ kiểm thất bại.
.PARAMETER Sql
Kết nối database để kiểm số liệu và quyền truy cập các chuyên đề.
.PARAMETER Render
Bổ sung kiểm giao diện và xuất bằng chứng khi dùng cùng Sql.
.PARAMETER MayChu
SQL Server dùng cho nhánh kiểm SQL.
.PARAMETER Database
Database dùng đối chiếu khi bật Sql.
#>
param([switch]$Sql, [switch]$Render, [string]$MayChu='.', [string]$Database='QLBenhVienRangHamMat')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$scratch = Join-Path $root 'obj/ReportChecks'
New-Item -ItemType Directory -Force $scratch | Out-Null
$project = Join-Path $scratch 'RangHamMat.ReportChecks.csproj'
$xml = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" /></ItemGroup></Project>'
[IO.File]::WriteAllText($project, $xml, [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ReportChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Restore ReportChecks thất bại.' }
$options = @($MayChu, $Database)
if ($Sql) { $options += '--sql' }
if ($Render) { $options += '--render' }
& dotnet run --project $project -c Release --no-restore -p:NuGetAudit=false -v quiet -- @options
if ($LASTEXITCODE -ne 0) { throw 'ReportChecks thất bại; xem các dòng FAIL.' }
