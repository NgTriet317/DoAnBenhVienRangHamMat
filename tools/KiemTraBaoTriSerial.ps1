<#
.SYNOPSIS
Dựng và chạy các ca kiểm tra bảo trì thiết bị theo serial trên database kiểm thử.
.DESCRIPTION
Database phải có tiền tố RHM_Hospital_Test_; LogPath tùy chọn lưu kết quả. Project kiểm tra được sinh trong obj, không thêm vào solution sản phẩm.
.NOTES
Các đường dẫn làm việc được tính từ vị trí script; lỗi được dừng và báo cho nơi gọi.
#>
param(
    [string]$Database = 'RHM_Hospital_Test_MaintSerial_20261029',
    [string]$MayChu = '.',
    [string]$LogPath = ''
)
$ErrorActionPreference = 'Stop'
if ($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$') {
    throw 'Chỉ chạy kiểm thử trên database cách ly có tiền tố RHM_Hospital_Test_.'
}
$root = Split-Path $PSScriptRoot -Parent
# Thư mục tạm chứa project kiểm tra, mã sinh và bằng chứng; không phải project sản phẩm.
$scratch = Join-Path $root 'obj/MaintenanceSerialChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
# Tạo đường dẫn project console tạm để chạy đúng mã kiểm chứng đi kèm.
$project = Join-Path $scratch 'MaintenanceSerialChecks.csproj'
$xml = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../BUS/BUS.csproj" /></ItemGroup></Project>'
[IO.File]::WriteAllText($project, $xml, [Text.UTF8Encoding]::new($false))
# Chép mã kiểm chứng được quản lý trong tools vào project tạm trước khi biên dịch.
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'MaintenanceSerialChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
# Khôi phục dependency cho project kiểm; kiểm tra mã thoát trước khi chạy các ca kiểm.
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Không khôi phục được gói kiểm thử bảo trì serial.' }
if ($LogPath) {
    & dotnet run --project $project --no-restore -p:NuGetAudit=false -v quiet -- $MayChu $Database 2>&1 | Tee-Object -FilePath $LogPath
} else {
    & dotnet run --project $project --no-restore -p:NuGetAudit=false -v quiet -- $MayChu $Database
}
if ($LASTEXITCODE -ne 0) { throw 'Kiểm thử bảo trì serial thất bại. Xem các dòng FAIL phía trên.' }
