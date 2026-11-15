<# Kiểm cấu hình kho và bộ ảnh tham khảo, không ghi database. #>
param([switch]$ChiCauHinh,[string]$Database,[string]$MayChu='.',[string]$ThuMucKho)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root 'obj/XrayHistoryDataChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$project=Join-Path $scratch 'XrayHistoryDataChecks.csproj'
[IO.File]::WriteAllText($project,'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" /></ItemGroup></Project>',[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'XrayHistoryDataChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
$runArgs=@()
if(!$ChiCauHinh){$runArgs+=Join-Path $root 'Database/AnhXquang'}
if($Database){
    if($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$' -or $ChiCauHinh){throw 'Kiểm SQL/UI cần database thử và bộ ảnh.'}
    $runArgs+=@($MayChu,$Database,$scratch)
    if($ThuMucKho){$runArgs+=[IO.Path]::GetFullPath($ThuMucKho)}
}
& dotnet run --project $project -p:NuGetAudit=false -v quiet -- @runArgs
if($LASTEXITCODE -ne 0){throw 'Kiểm dữ liệu ảnh X-quang thất bại; xem FAIL.'}
