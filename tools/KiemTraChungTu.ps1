param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$scratch = Join-Path $root 'obj/DocumentChecks'
New-Item -ItemType Directory -Force $scratch | Out-Null
$project = Join-Path $scratch 'DocumentChecks.csproj'
$xml = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" /></ItemGroup></Project>'
[IO.File]::WriteAllText($project, $xml, [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'DocumentChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Không khôi phục được project kiểm chứng chứng từ.' }
& dotnet run --project $project -c Release --no-restore -v quiet -p:WarningLevel=0 -p:NuGetAudit=false -- $root
if ($LASTEXITCODE -ne 0) { throw 'Kiểm chứng chứng từ thất bại; xem các dòng FAIL.' }
