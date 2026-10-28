param([string]$Database='RHM_HangChoKham_Test_20261028',[string]$MayChu='.',[switch]$BanPhatHanh,[switch]$ChiGiaoDien)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root 'obj/HangChoKhamChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$references='<ProjectReference Include="../../RangHamMat.Wpf/RangHamMat.Wpf.csproj" />'
if($BanPhatHanh){
 $references=(@('ET','DAL','BUS','RangHamMat.Wpf')|ForEach-Object{"<Reference Include=`"$_`"><HintPath>../../PhatHanh/$_.dll</HintPath></Reference>"}) -join ''
 $references+='<PackageReference Include="Microsoft.Data.SqlClient" Version="7.1.1"/><PackageReference Include="Dapper" Version="2.1.89"/><PackageReference Include="Dapper.Contrib" Version="2.0.78"/>'
}
$project="<Project Sdk=`"Microsoft.NET.Sdk`"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup>$references</ItemGroup></Project>"
[IO.File]::WriteAllText((Join-Path $scratch 'HangChoKhamChecks.csproj'),$project)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'HangChoKhamChecks.cs.txt') -Destination (Join-Path $scratch 'Program.cs') -Force
$runArgs=@($MayChu,$Database)
if($ChiGiaoDien){$runArgs+='--ui-only'}
& dotnet run --project (Join-Path $scratch 'HangChoKhamChecks.csproj') -- @runArgs
if($LASTEXITCODE -ne 0){throw 'Kiểm tra hàng chờ khám thất bại.'}
