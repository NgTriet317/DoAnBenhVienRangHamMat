<#
.SYNOPSIS
Đọc fixture X-quang qua UNC bằng BUS, ghi proof hostname; đối chiếu hai proof khác máy.
.NOTES
Không tạo share/sửa ACL, không tải hoặc ẩn ảnh; ghi audit xem ảnh trong DB thử. Kết quả chỉ chứng minh shared-storage, không tự nghiệm thu restore SQL từ xa.
#>
param([string]$MayChu='.',[string]$Database,[string]$ThuMucKho,[string]$TepBangChung,[string]$DoiChieuVoi)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'NghiemThuSupport.ps1')
if($DoiChieuVoi){
 if(!$TepBangChung){throw 'Cần hai proof được tạo trên hai máy.'}
 $comparison=Compare-P6StorageProof $DoiChieuVoi $TepBangChung
 $comparison|ConvertTo-Json -Depth 4|Write-Output
 if($comparison.Status -ne 'PASS_SHARED_STORAGE'){throw ('Chưa đạt kho hai máy: '+$comparison.Reason)}
 Write-Output 'SHARED_STORAGE_TWO_HOSTS=PASS';exit 0
}
if($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$'){throw 'Chỉ nhận database fixture RHM_Hospital_Test_.'}
if(!$ThuMucKho -or !$ThuMucKho.StartsWith('\\') -or $ThuMucKho.StartsWith('\\?\') -or $ThuMucKho.StartsWith('\\.\')){throw 'Probe yêu cầu kho UNC; không nhận thư mục máy trạm.'}
$scratch=Join-Path $root ('obj/SharedStorageProbe-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch | Out-Null
if(!$TepBangChung){$TepBangChung=Join-Path $root ('obj/P6Evidence/shared-'+[Environment]::MachineName+'-'+[Guid]::NewGuid().ToString('N')+'.json')}
if(Test-Path -LiteralPath $TepBangChung){throw 'Không ghi đè proof đã tồn tại.'}
$project=Join-Path $scratch 'SharedStorageProbe.csproj'
[IO.File]::WriteAllText($project,'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../../BUS/BUS.csproj" /></ItemGroup></Project>',[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'SharedStorageProbe.cs.txt') -Destination (Join-Path $scratch 'Program.cs')
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false -v quiet
if($LASTEXITCODE -ne 0){throw 'Restore probe thất bại.'}
& dotnet run --project $project --no-restore -p:NuGetAudit=false -v quiet -- $MayChu $Database $ThuMucKho $TepBangChung
if($LASTEXITCODE -ne 0){throw 'Probe UNC thất bại; không có bằng chứng nghiệm thu.'}
