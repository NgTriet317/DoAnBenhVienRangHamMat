<# .SYNOPSIS
Kiểm vai trò tùy chỉnh và tên Quản trị viên bằng fixture riêng trên database thử.
#>
param([Parameter(Mandatory = $true)][string]$Database, [string]$MayChu = '.')
$ErrorActionPreference = 'Stop'
if ($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$') { throw 'Chỉ chạy trên database RHM_Hospital_Test_.' }
$root = Split-Path $PSScriptRoot -Parent
$scratch = Join-Path $root 'obj/MergeRoleSqlChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" /></ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $scratch 'MergeRoleSqlChecks.csproj'), $project, [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'MergeRoleSqlChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
& dotnet run --project (Join-Path $scratch 'MergeRoleSqlChecks.csproj') -p:NuGetAudit=false -- $MayChu $Database
if ($LASTEXITCODE -ne 0) { throw 'Kiểm tra vai trò hợp nhất thất bại.' }
