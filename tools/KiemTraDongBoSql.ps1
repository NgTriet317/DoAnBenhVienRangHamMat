<#
.SYNOPSIS
Kiểm tra ánh xạ ET/DAL, schema và các nghiệp vụ chấm công/lương trên database kiểm thử.
.DESCRIPTION
Chỉ nhận database có tiền tố RHM_Hospital_Test_. ChiSchema giới hạn phạm vi theo chế độ --schema-only của chương trình kiểm tra.
.NOTES
Các đường dẫn làm việc được tính từ vị trí script; lỗi được dừng và báo cho nơi gọi.
#>
param(
    [string]$Database = 'RHM_Hospital_Test_20261029',
    [string]$MayChu = '.',
    [switch]$ChiSchema
)
$ErrorActionPreference = 'Stop'
if ($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$') {
    throw 'Chỉ kiểm tra database cách ly có tiền tố RHM_Hospital_Test_.'
}
$root = Split-Path $PSScriptRoot -Parent
# Thư mục tạm chứa project kiểm tra, mã sinh và bằng chứng; không phải project sản phẩm.
$scratch = Join-Path $root 'obj/SqlSyncChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
# Tạo đường dẫn project console tạm để chạy đúng mã kiểm chứng đi kèm.
$project = Join-Path $scratch 'SqlSyncChecks.csproj'
$xml = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../BUS/BUS.csproj" /></ItemGroup></Project>'
[IO.File]::WriteAllText($project, $xml, [Text.UTF8Encoding]::new($false))
# Chép mã kiểm chứng được quản lý trong tools vào project tạm trước khi biên dịch.
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'SqlSyncChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
# Khôi phục dependency cho project kiểm; kiểm tra mã thoát trước khi chạy các ca kiểm.
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Không khôi phục được gói kiểm tra SQL.' }
$runArgs = @($MayChu, $Database)
if ($ChiSchema) { $runArgs += '--schema-only' }
# Biên dịch/chạy công cụ và chuyển các đối số đã chuẩn bị; mã thoát khác 0 làm bước bàn giao thất bại.
& dotnet run --project $project --no-restore -p:NuGetAudit=false -v quiet -- @runArgs
if ($LASTEXITCODE -ne 0) { throw 'Kiểm tra đồng bộ SQL thất bại. Xem các dòng FAIL phía trên.' }
