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
