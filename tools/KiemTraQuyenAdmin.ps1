param([string]$Database='RHM_AdminQuyen_Test_20261028',[string]$MayChu='.',[switch]$ChiGiaoDien,[switch]$BanPhatHanh)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root 'obj/AdminQuyenChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$project=@'
<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>
 <ItemGroup><ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" /></ItemGroup>
</Project>
'@
if($BanPhatHanh){
 $references=foreach($assembly in @('ET','DAL','BUS','RangHamMat.Wpf')){"<Reference Include=`"$assembly`"><HintPath>../../PhatHanh/$assembly.dll</HintPath></Reference>"}
 $project=$project.Replace('<ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" />',($references -join '')+'<PackageReference Include="Microsoft.Data.SqlClient" Version="7.1.1"/><PackageReference Include="Dapper" Version="2.1.89"/><PackageReference Include="Dapper.Contrib" Version="2.0.78"/>')
}
[IO.File]::WriteAllText((Join-Path $scratch 'AdminQuyenChecks.csproj'),$project)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'AdminQuyenChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
$runArgs=@($MayChu,$Database)
if($ChiGiaoDien){$runArgs+='--ui-only'}
& dotnet run --project (Join-Path $scratch 'AdminQuyenChecks.csproj') -- @runArgs
if($LASTEXITCODE -ne 0){throw 'Kiểm tra quyền Admin thất bại.'}
