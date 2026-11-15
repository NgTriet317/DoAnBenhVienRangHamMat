<# Bộ kiểm hồi quy thanh toán sau khi hợp nhất Sprint3_tan và AI; chỉ dùng database thử riêng. #>
param([string]$Database='RHM_Hospital_Test_MergePayment_20261115',[string]$MayChu='.',[switch]$TaoDatabaseKiemThu)
$ErrorActionPreference='Stop'
if($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$'){throw 'Chỉ kiểm trên database cách ly RHM_Hospital_Test_.'}
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root 'obj/MergePaymentChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
if($TaoDatabaseKiemThu){
 $sql=[IO.File]::ReadAllText((Join-Path $root 'Database/QLBenhVienRangHamMat.sql'))
 $sql=[regex]::Replace($sql,'(?s)-- BEGIN RHM DATA SNAPSHOT:.*?-- END RHM DATA SNAPSHOT','-- Isolated fixtures, no restored patient records.')
 $install=Join-Path $scratch 'install.sql'
 [IO.File]::WriteAllText($install,$sql.Replace('QLBenhVienRangHamMat',$Database),[Text.UTF8Encoding]::new($false))
 & sqlcmd -S $MayChu -d master -E -C -b -f65001 -i $install
 if($LASTEXITCODE -ne 0){throw 'Cài database kiểm thử thất bại.'}
}
$project=Join-Path $scratch 'MergePaymentChecks.csproj'
[IO.File]::WriteAllText($project,'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><StartupObject>MergePaymentCheckEntry</StartupObject></PropertyGroup><ItemGroup><ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" /></ItemGroup></Project>',[Text.UTF8Encoding]::new($false))
$shared=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'HospitalWorkflowChecks.cs.txt')).Replace('internal static class Program','internal static partial class Program')
[IO.File]::WriteAllText((Join-Path $scratch 'SharedChecks.cs'),$shared,[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'MergePaymentChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false -v quiet
if($LASTEXITCODE -ne 0){throw 'Restore kiểm thanh toán hợp nhất thất bại.'}
& dotnet run --project $project --no-restore -p:NuGetAudit=false -v quiet -- $MayChu $Database
if($LASTEXITCODE -ne 0){throw 'Kiểm thanh toán hợp nhất thất bại; xem FAIL.'}
