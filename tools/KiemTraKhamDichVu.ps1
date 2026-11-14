<#
.SYNOPSIS
Dựng bộ kiểm tiếp nhận khám dịch vụ, phụ thu và tiếp nhận đồng thời.
.DESCRIPTION
Chỉ nhận database RHM_Hospital_Test_. Có thể cài schema đã bỏ snapshot trước khi chạy.
Chế độ thường dùng fixture rollback; DongThoi ghi fixture riêng để kiểm nhiều quầy cùng lúc.
.PARAMETER Database
Database thử; phải có đuôi _Concurrent khi bật DongThoi.
.PARAMETER MayChu
SQL Server chứa database thử.
.PARAMETER TaoDatabaseKiemThu
Cài bản sao SQL chính đã đổi tên database và bỏ snapshot dữ liệu.
.PARAMETER DongThoi
Chạy ca tiếp nhận đồng thời trên database riêng có đuôi _Concurrent.
#>
param([string]$Database='RHM_Hospital_Test_ServiceReception',[string]$MayChu='.',[switch]$TaoDatabaseKiemThu,[switch]$DongThoi)
$ErrorActionPreference='Stop'
if($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$'){throw 'Chỉ kiểm trên database cách ly RHM_Hospital_Test_.'}
if($DongThoi -and !$Database.EndsWith('_Concurrent')){throw 'Kiểm đồng thời giữ fixture riêng, chỉ nhận database có đuôi _Concurrent.'}
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root 'obj/ServiceReceptionChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
if($TaoDatabaseKiemThu){
 $sql=[IO.File]::ReadAllText((Join-Path $root 'Database/QLBenhVienRangHamMat.sql'))
 $sql=[regex]::Replace($sql,'(?s)-- BEGIN RHM DATA SNAPSHOT:.*?-- END RHM DATA SNAPSHOT','-- Isolated fixtures, no restored patient records.')
 $install=Join-Path $scratch 'install.sql'
 [IO.File]::WriteAllText($install,$sql.Replace('QLBenhVienRangHamMat',$Database),[Text.UTF8Encoding]::new($false))
 & sqlcmd -S $MayChu -d master -E -C -b -f65001 -i $install
 if($LASTEXITCODE -ne 0){throw 'Cài database kiểm thử thất bại.'}
}
$project=Join-Path $scratch 'ServiceReceptionChecks.csproj'
[IO.File]::WriteAllText($project,'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><StartupObject>ServiceCheckEntry</StartupObject></PropertyGroup><ItemGroup><ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" /></ItemGroup></Project>',[Text.UTF8Encoding]::new($false))
# Tái sử dụng fixture/tiện ích của bộ kiểm cũ; không sửa mã sản phẩm để phục vụ kiểm thử.
$shared=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'HospitalWorkflowChecks.cs.txt')).Replace('internal static class Program','internal static partial class Program')
[IO.File]::WriteAllText((Join-Path $scratch 'SharedChecks.cs'),$shared,[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ServiceReceptionChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false -v quiet
if($LASTEXITCODE -ne 0){throw 'Restore kiểm khám dịch vụ thất bại.'}
$checkArgs=@($MayChu,$Database)
if($DongThoi){$checkArgs+='--concurrency'}
& dotnet run --project $project --no-restore -p:NuGetAudit=false -v quiet -- @checkArgs
if($LASTEXITCODE -ne 0){throw 'Kiểm khám dịch vụ thất bại; xem FAIL.'}
