param([string]$Database='RHM_Sprint3_Test_Unified',[string]$MayChu='.',[switch]$CaiDat)
$ErrorActionPreference='Stop'
if($Database -notmatch '^RHM_Sprint3_Test_[A-Za-z0-9_]+$'){throw 'Chỉ chạy trên database kiểm thử Sprint 3.'}
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root 'obj/Sprint3Integration'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $root 'docs/sprint3/evidence') -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $scratch 'Sprint3Integration.csproj'),'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" /></ItemGroup></Project>')
Copy-Item (Join-Path $PSScriptRoot 'Sprint3Integration.cs.txt') (Join-Path $scratch 'Program.cs') -Force
Copy-Item (Join-Path $PSScriptRoot 'Sprint3Acceptance.cs.txt') (Join-Path $scratch 'Acceptance.cs') -Force
$arguments=@($MayChu,$Database)
if($CaiDat){$arguments+='--install'}
dotnet run --project (Join-Path $scratch 'Sprint3Integration.csproj') -- @arguments
if($LASTEXITCODE -ne 0){throw 'Sprint 3 SQL/UI integration failed.'}
