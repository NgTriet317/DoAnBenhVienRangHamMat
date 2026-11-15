<# .SYNOPSIS
Kiểm quyết định mở phân hệ WPF theo snapshot quyền SQL đã tải, không ghi database.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$scratch = Join-Path $root 'obj/MergeNavigationChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" /></ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $scratch 'MergeNavigationChecks.csproj'), $project, [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'MergeNavigationChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
& dotnet run --project (Join-Path $scratch 'MergeNavigationChecks.csproj') -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw 'Kiểm tra điều hướng hợp nhất thất bại.' }
