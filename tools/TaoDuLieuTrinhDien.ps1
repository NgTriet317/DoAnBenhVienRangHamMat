<#
.SYNOPSIS
Tạo database trình diễn P5 MỚI, không chép snapshot và không sửa DB đã tồn tại.
.NOTES
Lịch sử giả lập08–10/2026, không dùng điều trị hoặc coi là dữ liệu bệnh viện thật.
#>
param([string]$Database=('RHM_Hospital_Test_P5Demo_'+(Get-Date -Format 'yyyyMMdd_HHmmss')),[string]$MayChu='.',[string]$ThuMucKetQua,[switch]$ChiXuatMinhChung)
$ErrorActionPreference='Stop'
if($Database -notmatch '^RHM_Hospital_Test_P5Demo_[A-Za-z0-9_]+$'){throw 'Chỉ tạo DB mới RHM_Hospital_Test_P5Demo_...'}
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root ('obj/P5DemoData-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
if(!$ThuMucKetQua){$suffix=if($ChiXuatMinhChung){'-export-'+[Guid]::NewGuid().ToString('N')}else{''};$ThuMucKetQua=Join-Path $root ('obj/P5Evidence/'+$Database+$suffix)}
if(Test-Path -LiteralPath $ThuMucKetQua){throw 'Thư mục kết quả đã tồn tại; không ghi đè.'}
$sql=[IO.File]::ReadAllText((Join-Path $root 'Database/QLBenhVienRangHamMat.sql'))
$sql=[regex]::Replace($sql,'(?s)-- BEGIN RHM DATA SNAPSHOT:.*?-- END RHM DATA SNAPSHOT','-- P5 isolated DEMO: no restored patient/staff/document snapshot.')
$sql=$sql.Replace('QLBenhVienRangHamMat',$Database)
# Giữ khóa trong cùng phiên sqlcmd qua GO để hai lần chạy không cùng khởi tạo DB.
$guard="USE [master];`r`nGO`r`nDECLARE @p5Lock int; EXEC @p5Lock=sys.sp_getapplock @Resource=N'RHM:P5:Create:$Database',@LockMode='Exclusive',@LockOwner='Session',@LockTimeout=15000; IF @p5Lock<0 THROW 53500,N'DEMO creation busy.',1; IF DB_ID(N'$Database') IS NOT NULL THROW 53501,N'Database DEMO đã tồn tại; không ghi đè.',1;`r`nGO`r`n"
$install=Join-Path $scratch 'install-demo.sql'
[IO.File]::WriteAllText($install,$guard+$sql,[Text.UTF8Encoding]::new($false))
$project=Join-Path $scratch 'P5DemoData.csproj'
[IO.File]::WriteAllText($project,'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../BUS/BUS.csproj" /></ItemGroup></Project>',[Text.UTF8Encoding]::new($false))
foreach($source in @('P5DemoData','P5DemoEntry','P5Artifacts')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot ($source+'.cs.txt')) -Destination (Join-Path $scratch ($source+'.cs')) -Force}
$p5BuildMutex=[Threading.Mutex]::new($false,'Local\RHM_P5Demo_Build')
$p5BuildOwned=$false
try {
 try {$p5BuildOwned=$p5BuildMutex.WaitOne([TimeSpan]::FromSeconds(60))}catch [Threading.AbandonedMutexException]{$p5BuildOwned=$true}
 if(!$p5BuildOwned){throw 'Một công cụ DEMO khác đang build; thử lại sau.'}
 & dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false -v quiet
 if($LASTEXITCODE -ne 0){throw 'Restore công cụ DEMO thất bại.'}
 & dotnet build $project --no-restore -p:NuGetAudit=false -v quiet
 if($LASTEXITCODE -ne 0){throw 'Build công cụ DEMO thất bại; chưa tạo database.'}
} finally {if($p5BuildOwned){$p5BuildMutex.ReleaseMutex()};$p5BuildMutex.Dispose()}
if($ChiXuatMinhChung){
 & dotnet run --project $project --no-build --no-restore -v quiet -- --export $MayChu $Database $ThuMucKetQua
 if($LASTEXITCODE -ne 0){throw 'Xuất minh chứng DEMO thất bại; không ghi dữ liệu.'}
 exit 0
}
& dotnet run --project $project --no-build --no-restore -v quiet -- --prepare $MayChu $Database $ThuMucKetQua
if($LASTEXITCODE -ne 0){throw 'Không chuẩn bị được DB/kết quả mới; chưa tạo database.'}
& sqlcmd -S $MayChu -d master -E -C -b -f65001 -i $install
if($LASTEXITCODE -ne 0){throw 'Không tạo được database DEMO mới; không ghi dữ liệu mẫu.'}
& dotnet run --project $project --no-build --no-restore -p:NuGetAudit=false -v quiet -- $MayChu $Database $ThuMucKetQua
if($LASTEXITCODE -ne 0){throw 'Tạo dữ liệu DEMO thất bại; giữ DB thử để chẩn đoán, không tự xóa/ghi đè.'}
