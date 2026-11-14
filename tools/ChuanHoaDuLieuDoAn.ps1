<#
.SYNOPSIS
Chuan hoa va bo sung du lieu MO PHONG cho do an da duoc nguoi dung cho phep.
.DESCRIPTION
Mac dinh chi kiem tra. -ApDung ghi trong mot giao dich Serializable.
-HienThi chuan hoa nhan, ma, lich su kham va phan phong cua bo du lieu da bo sung.
Chi nhan QLBenhVienRangHamMat hoac database thu RHM_Hospital_Test_*.
#>
param(
    [string]$MayChu='.',
    [string]$Database='QLBenhVienRangHamMat',
    [switch]$ApDung,
    [switch]$HienThi,
    [string]$ThuMucKetQua
)
$ErrorActionPreference='Stop'
if($Database -notmatch '^(QLBenhVienRangHamMat|RHM_Hospital_Test_[A-Za-z0-9_]+)$'){throw 'Database khong thuoc pham vi do an/kiem thu.'}
$projectRoot=Split-Path $PSScriptRoot -Parent
$scratchRoot=Join-Path $projectRoot 'obj/AcademicData'
New-Item -ItemType Directory -Path $scratchRoot -Force | Out-Null
$projectFile=Join-Path $scratchRoot 'AcademicData.csproj'
[IO.File]::WriteAllText($projectFile,'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../BUS/BUS.csproj" /></ItemGroup></Project>',[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'AcademicData.cs.txt') -Destination (Join-Path $scratchRoot 'Program.cs') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'DataConventions.cs.txt') -Destination (Join-Path $scratchRoot 'DataConventions.cs') -Force
if(!$ThuMucKetQua){$ThuMucKetQua=Join-Path $scratchRoot ($Database+'-'+(Get-Date -Format 'yyyyMMdd_HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8))}
$mode=if($HienThi){if($ApDung){'--present'}else{'--present-check'}}elseif($ApDung){'--apply'}else{'--check'}
& dotnet run --project $projectFile -p:NuGetAudit=false --verbosity quiet -- $mode $MayChu $Database $projectRoot $ThuMucKetQua
if($LASTEXITCODE -ne 0){throw 'Chuan hoa/kiem tra du lieu do an that bai; xem log, khong coi la da hoan tat.'}
