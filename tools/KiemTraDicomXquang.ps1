<# Kiểm chuyển đổi DICOM và bộ xem WPF với dữ liệu tổng hợp, không kết nối SQL. #>
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root 'obj/DicomXrayChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$project=Join-Path $scratch 'DicomXrayChecks.csproj'
[IO.File]::WriteAllText($project,'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" /></ItemGroup></Project>',[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'DicomXrayChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false -v quiet
if($LASTEXITCODE -ne 0){throw 'Restore bộ kiểm DICOM thất bại.'}
Push-Location -LiteralPath $root
try{
    & dotnet run --project $project --no-restore -p:NuGetAudit=false -v quiet
    if($LASTEXITCODE -ne 0){throw 'Kiểm DICOM thất bại; xem các dòng FAIL.'}
}finally{Pop-Location}
