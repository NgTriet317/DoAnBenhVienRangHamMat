<#
.SYNOPSIS
Kiểm kho, sao lưu SQL + ảnh, hoặc phục hồi thử vào database mới.
.DESCRIPTION
Không xóa orphan, không ghi đè bản sao/database. KhoiPhuc chỉ nhận database mới
có tiền tố RHM_Hospital_Test_. Mặc định kho phải UNC; KhoCucBo dùng cho kiểm thử.
#>
param(
 [ValidateSet('KiemTra','SaoLuu','KhoiPhuc')][string]$CheDo='KiemTra',
 [string]$MayChu='.',[string]$Database='QLBenhVienRangHamMat',
 [Parameter(Mandatory=$true)][string]$ThuMucKho,
 [string]$ThuMucBanSao,[string]$BanSao,[string]$DatabaseMoi,[string]$ThuMucSqlServer,
 [switch]$ChoPhepKhoCucBo
)
$ErrorActionPreference='Stop'
if($Database -notmatch '^[A-Za-z0-9_]+$'){throw 'Tên database không hợp lệ.'}
if($CheDo -eq 'KhoiPhuc' -and $DatabaseMoi -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$'){throw 'Chỉ phục hồi thử vào database mới RHM_Hospital_Test_.'}
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root 'obj/XrayStorageOperations'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$project=Join-Path $scratch 'XrayStorageOperations.csproj'
[IO.File]::WriteAllText($project,'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../BUS/BUS.csproj" /></ItemGroup></Project>',[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'XrayStorageOperations.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false -v quiet
if($LASTEXITCODE -ne 0){throw 'Restore công cụ kho ảnh thất bại.'}
$runArgs=@('--server',$MayChu,'--db',$Database,'--mode',$CheDo,'--root',$ThuMucKho)
foreach($pair in @(@('output',$ThuMucBanSao),@('archive',$BanSao),@('new-db',$DatabaseMoi),@('sql-directory',$ThuMucSqlServer))){if(![string]::IsNullOrWhiteSpace($pair[1])){$runArgs+=@(('--'+$pair[0]),$pair[1])}}
if($ChoPhepKhoCucBo){$runArgs+=@('--local','true')}
$operationOutput=@(& dotnet run --project $project --no-restore -p:NuGetAudit=false -v quiet -- @runArgs)
$operationExit=$LASTEXITCODE
$operationOutput|Write-Output
if($operationExit -ne 0){throw ('Thao tác kho ảnh thất bại: '+($operationOutput|Select-Object -Last 1))}
