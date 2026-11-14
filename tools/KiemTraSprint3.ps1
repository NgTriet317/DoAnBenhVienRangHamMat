<#
.SYNOPSIS
Kiểm hợp đồng ET của Sprint 3 và dựng solution ở Debug/Release.
.DESCRIPTION
Sinh project tạm trong obj/Sprint3Checks để kiểm nhãn nhật ký, danh mục cho phép
và giới hạn bộ lọc mà không kết nối SQL; sau đó build hai cấu hình nếu không bỏ qua.
.PARAMETER SkipBuild
Bỏ build solution Debug/Release; project kiểm hợp đồng vẫn được dựng và chạy.
#>
param([switch]$SkipBuild)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root 'obj/Sprint3Checks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../ET/ET.csproj" /></ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $scratch 'Sprint3Checks.csproj'),$project)
Copy-Item (Join-Path $PSScriptRoot 'Sprint3Checks.cs.txt') (Join-Path $scratch 'Program.cs') -Force
dotnet run --project (Join-Path $scratch 'Sprint3Checks.csproj')
if($LASTEXITCODE -ne 0){throw 'Sprint 3 checks failed.'}
if(!$SkipBuild){
 foreach($configuration in @('Debug','Release')){
  dotnet build (Join-Path $root 'BenhVienRangHamMat.sln') -c $configuration --no-restore
  if($LASTEXITCODE -ne 0){throw "Build $configuration failed."}
 }
}
