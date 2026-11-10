<#
.SYNOPSIS
Đối soát chỉ SELECT theo quyền SQL của người vận hành; xuất PK/nguyên nhân, không sửa dữ liệu.
.NOTES
Exit0 nghĩa là hoàn tất đối soát, không có nghĩa mọi dữ liệu đều đúng. Xem TongSaiXacDinh/TongCanDoiChieu trong audit.json.
#>
param([string]$Database='QLBenhVienRangHamMat',[string]$MayChu='.',[string]$ThuMucKetQua,[ValidateRange(1,500)][int]$GioiHanMoiQuyTac=200)
$ErrorActionPreference='Stop'
if($Database -notmatch '^[A-Za-z0-9_]+$'){throw 'Tên database không hợp lệ.'}
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root 'obj/DataReconciliation'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
if(!$ThuMucKetQua){$ThuMucKetQua=Join-Path $root ('obj/P5Evidence/audit-'+$Database+'-'+[Guid]::NewGuid().ToString('N'))}
$project=Join-Path $scratch 'DataReconciliation.csproj'
[IO.File]::WriteAllText($project,'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../BUS/BUS.csproj" /></ItemGroup></Project>',[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'DataReconciliation.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'P5Artifacts.cs.txt') -Destination (Join-Path $scratch 'P5Artifacts.cs') -Force
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false -v quiet
if($LASTEXITCODE -ne 0){throw 'Restore công cụ đối soát thất bại.'}
& dotnet run --project $project --no-restore -p:NuGetAudit=false -v quiet -- $MayChu $Database $ThuMucKetQua $GioiHanMoiQuyTac
if($LASTEXITCODE -ne 0){throw 'Đối soát thất bại; không coi là báo cáo sạch.'}
