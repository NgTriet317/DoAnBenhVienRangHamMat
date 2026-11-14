<# Bộ kiểm WPF STA chỉ kiểm nhập liệu, không ghi hoặc khởi tạo database. #>
param(
    [string[]]$Source=@('tools/InputValidationChecks.cs.txt'),
    [string]$Entry='InputValidationChecks',
    [switch]$BuildApplication,
    [switch]$All
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if($Entry -notmatch '^[A-Za-z_][A-Za-z0-9_.]*$'){throw 'Tên entry không hợp lệ.'}
$scratch=Join-Path $root ('obj/InputValidation/'+$Entry)
$binary=Join-Path $root 'RangHamMat.Wpf/bin/Debug/net8.0-windows'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
if($All){
    $entries=@('InputValidationChecks','ClinicalInputValidationChecks','ReportCatalogInputChecks','FinanceMaintenanceInputChecks')
    foreach($suite in $entries){
        & $PSCommandPath -Source @('tools/'+$suite+'.cs.txt') -Entry $suite -BuildApplication:($BuildApplication -and $suite -eq $entries[0])
    }
    return
}
if($BuildApplication){
    & dotnet build (Join-Path $root 'BenhVienRangHamMat.sln') --no-restore -p:NuGetAudit=false -v quiet
    if($LASTEXITCODE -ne 0){throw 'Build ứng dụng thất bại.'}
}
$references=foreach($name in @('RangHamMat.Wpf','BUS','DAL','ET')){
    $dll=Join-Path $binary ($name+'.dll')
    if(!(Test-Path -LiteralPath $dll)){throw 'Cần build ứng dụng trước hoặc dùng -BuildApplication.'}
    '<Reference Include="'+$name+'"><HintPath>'+[Security.SecurityElement]::Escape($dll)+'</HintPath></Reference>'
}
$project=Join-Path $scratch 'InputValidation.csproj'
$xml='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems><StartupObject>'+$Entry+'</StartupObject></PropertyGroup><ItemGroup>'+($references -join '')
$index=0
foreach($file in $Source){
    $sourcePath=if([IO.Path]::IsPathRooted($file)){$file}else{Join-Path $root $file}
    $target=Join-Path $scratch ('Checks'+$index+'.cs')
    Copy-Item -LiteralPath $sourcePath -Destination $target -Force
    $xml+='<Compile Include="Checks'+$index+'.cs" />'
    $index++
}
$xml+='</ItemGroup></Project>'
[IO.File]::WriteAllText($project,$xml,[Text.UTF8Encoding]::new($false))
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false -v quiet
if($LASTEXITCODE -ne 0){throw 'Restore bộ kiểm nhập liệu thất bại.'}
& dotnet build $project --no-restore -p:NuGetAudit=false -v quiet
if($LASTEXITCODE -ne 0){throw 'Build bộ kiểm nhập liệu thất bại.'}
$output=Join-Path $scratch 'bin/Debug/net8.0-windows'
# Các DLL phụ trợ WPF/SQL được nạp từ output đã build; không dùng kết nối hay dữ liệu thật.
Get-ChildItem -LiteralPath $binary -Filter '*.dll' -File | Copy-Item -Destination $output -Force
Push-Location -LiteralPath $root
try{
    & dotnet (Join-Path $output 'InputValidation.dll')
    if($LASTEXITCODE -ne 0){throw 'Kiểm nhập liệu thất bại; xem các dòng FAIL.'}
}finally{Pop-Location}
