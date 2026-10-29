param([string]$Database='RHM_Hospital_Test_20261029',[string]$MayChu='.',[switch]$BanPhatHanh,[switch]$ChiGiaoDien,[switch]$TaoDatabaseKiemThu)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root 'obj/HospitalWorkflowChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
if($TaoDatabaseKiemThu){
 if($ChiGiaoDien -or $Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$'){throw 'Chỉ tạo database kiểm thử có tiền tố RHM_Hospital_Test_.'}
 $sql=(Get-Content -LiteralPath (Join-Path $root 'Database/QLBenhVienRangHamMat.sql') -Raw).Replace('QLBenhVienRangHamMat',$Database)
 $install=Join-Path $scratch 'install-fixture.sql'
 [IO.File]::WriteAllText($install,$sql,[Text.UTF8Encoding]::new($false))
 & sqlcmd -S $MayChu -d master -E -C -b -f65001 -i $install
 if($LASTEXITCODE -ne 0){throw 'Không tạo/đồng bộ được database kiểm thử.'}
}
$refs='<ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" />'
if($BanPhatHanh){$refs=(@('ET','DAL','BUS','RangHamMat.Wpf')|ForEach-Object{"<Reference Include=`"$_`"><HintPath>../../PhatHanh/$_.dll</HintPath></Reference>"}) -join '';$refs+='<PackageReference Include="Microsoft.Data.SqlClient" Version="7.1.1"/><PackageReference Include="Dapper" Version="2.1.89"/><PackageReference Include="Dapper.Contrib" Version="2.0.78"/>'}
[IO.File]::WriteAllText((Join-Path $scratch 'HospitalWorkflowChecks.csproj'),"<Project Sdk=`"Microsoft.NET.Sdk`"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup>$refs</ItemGroup></Project>")
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'HospitalWorkflowChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
$runArgs=@($MayChu,$Database)
if($ChiGiaoDien){$runArgs+='--ui-only'}
& dotnet run --project (Join-Path $scratch 'HospitalWorkflowChecks.csproj') -- @runArgs
if($LASTEXITCODE -ne 0){throw 'Kiểm tra quy trình bệnh viện thất bại.'}
