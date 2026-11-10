<#
.SYNOPSIS
Kiểm SQL/ET nền tảng khám dịch vụ và metadata X-quang trên database cách ly.
.DESCRIPTION
TaoDatabaseKiemThu chạy bản sao file SQL chính, có snapshot cài mới. Các fixture
trong chương trình kiểm đều rollback; không nhận tên database ứng dụng.
#>
param(
    [string]$Database = 'RHM_Hospital_Test_S3Foundation',
    [string]$MayChu = '.',
    [switch]$TaoDatabaseKiemThu,
    [ValidateSet('DayDu','GhiDuLieuChayLai','KiemTraChayLai','LuuDoiChieu','DoiChieu')]
    [string]$CheDo = 'DayDu',
    [switch]$MoPhongLoiTruocMigrationCuoi
)
$ErrorActionPreference = 'Stop'
if ($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$') {
    throw 'Chỉ chạy trên database cách ly có tiền tố RHM_Hospital_Test_.'
}
if ($MoPhongLoiTruocMigrationCuoi -and (!$TaoDatabaseKiemThu -or $CheDo -ne 'KiemTraChayLai')) {
    throw 'Mô phỏng lỗi chỉ dùng cùng TaoDatabaseKiemThu và CheDo KiemTraChayLai.'
}
$root = Split-Path $PSScriptRoot -Parent
$scratch = Join-Path $root 'obj/Sprint3FoundationChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
if ($TaoDatabaseKiemThu) {
    $sql = [IO.File]::ReadAllText((Join-Path $root 'Database/QLBenhVienRangHamMat.sql'))
    if ($MoPhongLoiTruocMigrationCuoi) {
        $marker = '-- BEGIN SPRINT 3 FOUNDATION 20261107:'
        if (!$sql.Contains($marker)) { throw 'Thiếu marker để mô phỏng lỗi kiểm thử.' }
        $sql = $sql.Replace($marker, "THROW 53999,N'Intentional isolated migration interruption',1;`r`nGO`r`n" + $marker)
    }
    $install = Join-Path $scratch 'install.sql'
    [IO.File]::WriteAllText($install, $sql.Replace('QLBenhVienRangHamMat', $Database), [Text.UTF8Encoding]::new($false))
    $installOutput = @(& sqlcmd -S $MayChu -d master -E -C -b -f65001 -i $install)
    $installExit = $LASTEXITCODE
    $installOutput | Write-Output
    if ($MoPhongLoiTruocMigrationCuoi) {
        if ($installExit -eq 0 -or ($installOutput -join ' ') -notmatch 'Msg 53999,') {
            throw 'Mô phỏng phải tới đúng lỗi 53999 trước migration cuối; lỗi khác là thất bại.'
        }
    }
    elseif ($installExit -ne 0) { throw 'Cài đặt/nâng cấp database kiểm thử thất bại.' }
}
$project = Join-Path $scratch 'Sprint3FoundationChecks.csproj'
[IO.File]::WriteAllText($project, '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../BUS/BUS.csproj" /></ItemGroup></Project>', [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Sprint3FoundationChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Restore project kiểm thất bại.' }
$checkArgs = @($MayChu, $Database)
if ($CheDo -eq 'GhiDuLieuChayLai') { $checkArgs += '--seed-rerun' }
elseif ($CheDo -eq 'KiemTraChayLai') { $checkArgs += '--verify-rerun' }
elseif ($CheDo -eq 'LuuDoiChieu') { $checkArgs += @('--save-baseline', (Join-Path $scratch ('baseline-' + $Database + '.json'))) }
elseif ($CheDo -eq 'DoiChieu') { $checkArgs += @('--compare-baseline', (Join-Path $scratch ('baseline-' + $Database + '.json'))) }
& dotnet run --project $project --no-restore -p:NuGetAudit=false -v quiet -- @checkArgs
if ($LASTEXITCODE -ne 0) { throw 'Kiểm nền tảng Sprint 3 thất bại; xem các dòng FAIL.' }
