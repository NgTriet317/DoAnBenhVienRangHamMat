<#
.SYNOPSIS
Dựng và chạy bộ kiểm tra quy trình bệnh viện hoặc kiểm tra giao diện.
.DESCRIPTION
TaoDatabaseKiemThu chỉ tạo database có tiền tố RHM_Hospital_Test_ từ bản sao SQL; ChiGiaoDien chỉ mở các phân hệ. BanPhatHanh kiểm DLL đã đóng gói thay cho project nguồn.
.NOTES
Các đường dẫn làm việc được tính từ vị trí script; lỗi được dừng và báo cho nơi gọi.
#>
param([string]$Database='RHM_Hospital_Test_20261029',[string]$MayChu='.',[switch]$BanPhatHanh,[switch]$ChiGiaoDien,[switch]$TaoDatabaseKiemThu)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
# Thư mục tạm chứa project kiểm tra, mã sinh và bằng chứng; không phải project sản phẩm.
$scratch=Join-Path $root 'obj/HospitalWorkflowChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
# Chỉ nhánh được yêu cầu mới cài database cách ly; SQL sản phẩm được đọc vào bản sao, không ghi đè file gốc.
if($TaoDatabaseKiemThu){
 if($ChiGiaoDien -or $Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$'){throw 'Chỉ tạo database kiểm thử có tiền tố RHM_Hospital_Test_.'}
 $sql=Get-Content -LiteralPath (Join-Path $root 'Database/QLBenhVienRangHamMat.sql') -Raw
 # Bộ kiểm khám có fixture riêng và kiểm nền trống; không chép hồ sơ phục hồi vào fixture.
 # Chỉ bỏ snapshot trong bản sao kiểm thử, giữ nguyên SQL sản phẩm và toàn bộ pha đồng bộ.
 $sql=[regex]::Replace($sql,'(?s)-- BEGIN RHM DATA SNAPSHOT:.*?-- END RHM DATA SNAPSHOT','-- Snapshot omitted only for isolated clinical fixtures.')
 $sql=$sql.Replace('QLBenhVienRangHamMat',$Database)
 $install=Join-Path $scratch 'install-fixture.sql'
 [IO.File]::WriteAllText($install,$sql,[Text.UTF8Encoding]::new($false))
 & sqlcmd -S $MayChu -d master -E -C -b -f65001 -i $install
 if($LASTEXITCODE -ne 0){throw 'Không tạo/đồng bộ được database kiểm thử.'}
}
$refs='<ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" />'
if($BanPhatHanh){$refs=(@('ET','DAL','BUS','RangHamMat.Wpf')|ForEach-Object{"<Reference Include=`"$_`"><HintPath>../../PhatHanh/$_.dll</HintPath></Reference>"}) -join '';$refs+='<PackageReference Include="Microsoft.Data.SqlClient" Version="7.1.1"/><PackageReference Include="Dapper" Version="2.1.89"/><PackageReference Include="Dapper.Contrib" Version="2.0.78"/>'}
[IO.File]::WriteAllText((Join-Path $scratch 'HospitalWorkflowChecks.csproj'),"<Project Sdk=`"Microsoft.NET.Sdk`"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup>$refs</ItemGroup></Project>")
# Chép mã kiểm chứng được quản lý trong tools vào project tạm trước khi biên dịch.
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'HospitalWorkflowChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
$runArgs=@($MayChu,$Database)
if($ChiGiaoDien){$runArgs+='--ui-only'}
# Biên dịch/chạy công cụ và chuyển các đối số đã chuẩn bị; mã thoát khác 0 làm bước bàn giao thất bại.
& dotnet run --project (Join-Path $scratch 'HospitalWorkflowChecks.csproj') -- @runArgs
if($LASTEXITCODE -ne 0){throw 'Kiểm tra quy trình bệnh viện thất bại.'}
