<#
.SYNOPSIS
Kiểm tệp phiên đăng nhập, xóa định danh và tùy chọn luồng đăng nhập WPF qua SQL.
.DESCRIPTION
Mặc định dùng tệp phiên riêng trong obj. KiemTraSql tạo database thử mới từ SQL chính,
rồi chạy từng pha ở tiến trình riêng để kiểm khôi phục, đổi tài khoản và thu hồi phiên.
.PARAMETER KiemTraSql
Bổ sung các pha có ghi dữ liệu vào database thử; giữ database và log để đối chiếu.
.PARAMETER MayChu
SQL Server dùng tạo database thử khi bật KiemTraSql.
#>
param([switch]$KiemTraSql, [string]$MayChu = '.')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$scratch = Join-Path $root 'obj/LoginSessionChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" /></ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $scratch 'LoginSessionChecks.csproj'), $project, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $scratch 'Program.cs'), [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'LoginSessionChecks.cs.txt')), [Text.UTF8Encoding]::new($false))
& dotnet run --project (Join-Path $scratch 'LoginSessionChecks.csproj') -p:NuGetAudit=false -- $scratch
if ($LASTEXITCODE -ne 0) { throw 'Kiểm tra phiên đăng nhập thất bại.' }
if ($KiemTraSql) {
    $database = 'RHM_Hospital_Test_Login_' + (Get-Date -Format 'yyyyMMdd_HHmmss')
    $sql = [IO.File]::ReadAllText((Join-Path $root 'Database/QLBenhVienRangHamMat.sql')).Replace('QLBenhVienRangHamMat', $database)
    $install = Join-Path $scratch 'install-login-fixture.sql'
    [IO.File]::WriteAllText($install, $sql, [Text.UTF8Encoding]::new($false))
    & sqlcmd -S $MayChu -d master -E -C -b -f65001 -i $install -o (Join-Path $scratch 'install-login-fixture.log')
    if ($LASTEXITCODE -ne 0) { throw 'Không tạo được database thử đăng nhập.' }
    Write-Output "Database kiểm thử: $database"
    [IO.File]::WriteAllText((Join-Path $scratch 'Program.cs'), [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'LoginSessionSqlChecks.cs.txt')), [Text.UTF8Encoding]::new($false))
    & dotnet build (Join-Path $scratch 'LoginSessionChecks.csproj') -p:NuGetAudit=false -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Không build được kiểm tra luồng đăng nhập.' }
    foreach ($phase in @('login', 'restore', 'logout-switch', 'restore-staff', 'logout-exit', 'login', 'invalidate')) {
        & dotnet (Join-Path $scratch 'bin/Debug/net8.0-windows/LoginSessionChecks.dll') $scratch $MayChu $database $phase
        if ($LASTEXITCODE -ne 0) { throw "Kiểm tra luồng $phase thất bại." }
    }
}
