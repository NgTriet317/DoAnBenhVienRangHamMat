<#
.SYNOPSIS
Tạo gói nghiệm thu XR04 cục bộ từ đúng nguồn P6 đã kiểm; không kết nối SQL.
#>
param([Parameter(Mandatory=$true)][string]$ThuMucP6,[string]$MaGoi=((Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[Guid]::NewGuid().ToString('N').Substring(0,8)))
$ErrorActionPreference='Stop'
if($MaGoi -notmatch '^[A-Za-z0-9_]{1,40}$'){throw 'Mã gói chỉ nhận1–40 chữ/số/gạch dưới.'}
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'NghiemThuSupport.ps1')
. (Join-Path $PSScriptRoot 'XR04BanGiaoSupport.ps1')
$p6=(Get-Item -LiteralPath $ThuMucP6).FullName
$acceptance=Get-Content -LiteralPath (Join-Path $p6 'acceptance.json') -Raw | ConvertFrom-Json
if($acceptance.LocalStatus -ne 'PASS' -or !$acceptance.Steps.Count -or @($acceptance.Steps|Where-Object Status -ne 'PASS').Count){throw 'Cần kết quả P6 nội bộ đạt.'}
$accepted=Get-Content -LiteralPath (Join-Path $p6 'source-after.json') -Raw | ConvertFrom-Json
$before=Get-Content -LiteralPath (Join-Path $p6 'source-before.json') -Raw | ConvertFrom-Json
if(($before|ConvertTo-Json -Depth 5 -Compress) -cne ($accepted|ConvertTo-Json -Depth 5 -Compress)){throw 'Nguồn thay đổi trong lần P6 được cung cấp.'}
foreach($path in @('tools/SharedStorageProbe.cs.txt','tools/XrayStorageOperations.cs.txt','tools/XrayChecks.cs.txt','tools/HospitalWorkflowChecks.cs.txt','RangHamMat.Wpf/RangHamMat.Wpf.csproj','Database/QLBenhVienRangHamMat.sql')){if(!($accepted|Where-Object Path -eq $path)){throw ('Manifest P6 thiếu '+$path)}}
$source=Get-P6SourceManifest $root;Assert-XR04Source $accepted $source
$work=Join-Path $root ('obj/XR04Handover/'+$MaGoi)
if(Test-Path -LiteralPath $work){throw 'Mã gói đã tồn tại; không ghi đè.'}
New-Item -ItemType Directory -Path $work | Out-Null
$incomplete=Join-Path $work 'INCOMPLETE.txt';[IO.File]::WriteAllText($incomplete,'Packaging is incomplete; do not use.')
$package=Join-Path $work 'Package';New-Item -ItemType Directory -Path $package | Out-Null
foreach($name in @('Probe','Storage','Verify')){
 $build=Join-Path $work ('build/'+$name);New-Item -ItemType Directory -Path $build -Force | Out-Null
 $reference=if($name -eq 'Verify'){Join-Path $root 'RangHamMat.Wpf/RangHamMat.Wpf.csproj'}else{Join-Path $root 'BUS/BUS.csproj'}
 $properties=if($name -eq 'Verify'){'<TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><StartupObject>XrayCheckEntry</StartupObject>'}else{'<TargetFramework>net8.0</TargetFramework>'}
 $project=Join-Path $build ($name+'.csproj')
 $xml='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><UseAppHost>false</UseAppHost>'+$properties+'</PropertyGroup><ItemGroup><ProjectReference Include="'+[Security.SecurityElement]::Escape($reference)+'" /></ItemGroup></Project>'
 [IO.File]::WriteAllText($project,$xml,[Text.UTF8Encoding]::new($false))
 $sourceFile=switch($name){'Probe'{'SharedStorageProbe.cs.txt'}'Storage'{'XrayStorageOperations.cs.txt'}'Verify'{'XrayChecks.cs.txt'}}
 Copy-Item -LiteralPath (Join-Path $PSScriptRoot $sourceFile) -Destination (Join-Path $build 'Program.cs')
 if($name -eq 'Verify'){
  $shared=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'HospitalWorkflowChecks.cs.txt')).Replace('internal static class Program','internal static partial class Program')
  [IO.File]::WriteAllText((Join-Path $build 'SharedChecks.cs'),$shared,[Text.UTF8Encoding]::new($false))
 }
 $output=Join-Path $package ('Tools/'+$name);$log=Join-Path $work ($name+'-publish.log');$oldPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& dotnet publish $project -c Release -o $output --self-contained false --ignore-failed-sources -p:NuGetAudit=false -v quiet *> $log;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference}
 if($code -ne 0){throw ('Publish '+$name+' thất bại; xem '+$log)}
 foreach($extension in @('.dll','.deps.json','.runtimeconfig.json')){if(!(Test-Path -LiteralPath (Join-Path $output ($name+$extension)))){throw ('Thiếu runtime artifact '+$name+$extension)}}
 # Tham chiếu WPF có thể kéo cấu hình nguồn theo; gói thử không mặc định trỏ DB nghiệp vụ.
 $settings=Join-Path $output 'appsettings.json'
 if(Test-Path -LiteralPath $settings){[IO.File]::WriteAllText($settings,'{"ConnectionStrings":{"RangHamMat":"Data Source=.;Initial Catalog=RHM_Hospital_Test_ConfigureBeforeUse;Integrated Security=True;TrustServerCertificate=True;"},"XQuang":{"ThuMucDungChung":"","DungLuongToiDaMB":20,"DiemAnhToiDa":40000000,"ChoPhepDuongDanCucBo":false}}',[Text.UTF8Encoding]::new($false))}
 Write-Output ('PASS: published '+$name+' for runtime invocation')
}
foreach($file in @('ChayNghiemThuXR04.ps1','XR04BanGiaoSupport.ps1','NghiemThuSupport.ps1')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination (Join-Path $package $file)}
Copy-Item -LiteralPath (Join-Path $root 'docs/sprint3/BO_NGHIEM_THU_XR04.md') -Destination (Join-Path $package 'README.md')
$after=Get-P6SourceManifest $root;Assert-XR04Source $accepted $after
if(($source|ConvertTo-Json -Depth 5 -Compress) -cne ($after|ConvertTo-Json -Depth 5 -Compress)){throw 'Nguồn thay đổi khi đóng gói.'}
Write-P6Json (Join-Path $package 'source.json') $source
$manifest=[pscustomobject]@{Protocol='RHM.XR04.Kit.v1';Id=$MaGoi;CreatedUtc=[DateTime]::UtcNow;P6Run=$acceptance.Run;AcceptedSourceFiles=$accepted.Count;GitCommit=(& git -C $root rev-parse HEAD);ReadyForDeployment=$false;XR04Status='PENDING_ENVIRONMENT';Runtime='Windows x64, .NET8; Verify requires Windows Desktop Runtime8';Files=@(Get-XR04FileManifest $package)}
Write-P6Json (Join-Path $package 'package.json') $manifest
Assert-XR04Package $package -DangDongGoi
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=Join-Path $work ('XR04-'+$MaGoi+'.zip');[IO.Compression.ZipFile]::CreateFromDirectory($package,$zip,[IO.Compression.CompressionLevel]::Optimal,$false)
$sha=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
[IO.File]::WriteAllText(($zip+'.sha256'),($sha+'  '+[IO.Path]::GetFileName($zip)),[Text.UTF8Encoding]::new($false))
Remove-Item -LiteralPath $incomplete
try{Assert-XR04Package $package}catch{[IO.File]::WriteAllText($incomplete,'Final package validation failed.');throw}
Write-Output ('XR04_DIRECTORY='+$package);Write-Output ('XR04_PACKAGE='+$zip);Write-Output ('XR04_SHA256='+$sha);Write-Output 'XR04_READY_FOR_DEPLOYMENT=False'
