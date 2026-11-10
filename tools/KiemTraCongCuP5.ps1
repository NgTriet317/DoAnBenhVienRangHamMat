param([Parameter(Mandatory=$true)][string]$Database,[string]$MayChu='.')
$ErrorActionPreference='Stop'
if($Database -notmatch '^RHM_Hospital_Test_P5Demo_[A-Za-z0-9_]+$'){throw 'Chỉ kiểm công cụ trên DEMO cách ly.'}
$root=Split-Path $PSScriptRoot -Parent
$run=Join-Path $root ('obj/P5Evidence/guards-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$builder=Join-Path $PSScriptRoot 'TaoDuLieuTrinhDien.ps1'
$auditor=Join-Path $PSScriptRoot 'DoiSoatDuLieu.ps1'
$passed=0
function Reject-Tool([string]$Name,[string]$Tool,[string[]]$ToolArgs,[string]$Expected){
 $rejectLog=Join-Path $run ($Name+'.log')
 try {$ErrorActionPreference='Continue'; & powershell -NoProfile -ExecutionPolicy Bypass -File $Tool @ToolArgs *> $rejectLog; $rejectExit=$LASTEXITCODE}
 finally {$ErrorActionPreference='Stop'}
 if($rejectExit -eq 0 -or (Get-Content -LiteralPath $rejectLog -Raw) -notmatch $Expected){throw ('Không từ chối đúng lỗi '+$Name)}
 $script:passed++;Write-Output ('PASS: '+$Name)
}
Reject-Tool 'production-name-rejected' $builder @('-Database','QLBenhVienRangHamMat','-ThuMucKetQua',(Join-Path $run 'bad-production')) 'Chỉ tạo DB mới'
Reject-Tool 'invalid-name-rejected' $builder @('-Database',"RHM_Hospital_Test_P5Demo_x';DROP",'-ThuMucKetQua',(Join-Path $run 'bad-name')) 'Chỉ tạo DB mới'
$existingDestination=Join-Path $run 'existing-db-output'
Reject-Tool 'existing-database-rejected-before-output' $builder @('-Database',$Database,'-MayChu',$MayChu,'-ThuMucKetQua',$existingDestination) 'Database DEMO đã tồn tại'
if(Test-Path -LiteralPath $existingDestination){throw 'Đã tạo output khi DB đã tồn tại.'}
$unusedDatabase='RHM_Hospital_Test_P5Demo_Guard_'+[Guid]::NewGuid().ToString('N')
Reject-Tool 'existing-output-rejected-before-database' $builder @('-Database',$unusedDatabase,'-MayChu',$MayChu,'-ThuMucKetQua',$run) 'Thư mục kết quả đã tồn tại'
$target=Join-Path $run 'junction-target';$junction=Join-Path $run 'junction'
New-Item -ItemType Directory -Path $target | Out-Null
New-Item -ItemType Junction -Path $junction -Target $target | Out-Null
Reject-Tool 'linked-output-rejected-before-database' $builder @('-Database',$unusedDatabase,'-MayChu',$MayChu,'-ThuMucKetQua',(Join-Path $junction 'new-output')) 'Thư mục kết quả không được đi qua liên kết'
& sqlcmd -S $MayChu -d master -E -C -b -Q "IF DB_ID(N'$unusedDatabase') IS NOT NULL THROW 53509,N'FAIL: output preflight created a database.',1;"
if($LASTEXITCODE -ne 0){throw 'Đã tạo DB dù preflight thất bại.'}
$auditOut=Join-Path $run 'audit'
& $auditor -Database $Database -MayChu $MayChu -ThuMucKetQua $auditOut
if($LASTEXITCODE -ne 0){throw 'Audit không hoàn tất.'}
$auditHash=(Get-FileHash -LiteralPath (Join-Path $auditOut 'audit.json') -Algorithm SHA256).Hash
Reject-Tool 'audit-does-not-overwrite' $auditor @('-Database',$Database,'-MayChu',$MayChu,'-ThuMucKetQua',$auditOut) 'Thư mục kết quả đã tồn tại'
if((Get-FileHash -LiteralPath (Join-Path $auditOut 'audit.json') -Algorithm SHA256).Hash -ne $auditHash){throw 'Artifact bị thay đổi.'}
$recovered=Join-Path $run 'recovered-evidence'
& $builder -Database $Database -MayChu $MayChu -ChiXuatMinhChung -ThuMucKetQua $recovered
if($LASTEXITCODE -ne 0){throw 'Không xuất phục hồi được minh chứng.'}
$lineage=@(Import-Csv -LiteralPath (Join-Path $recovered 'lineage.csv'))
if($lineage.Count -ne 26 -or ($lineage|ForEach-Object {[decimal]$_.PhaiThu}|Measure-Object -Sum).Sum -ne 3250000 -or (Test-Path -LiteralPath (Join-Path $recovered 'INCOMPLETE_DEMO.txt'))){throw 'Bằng chứng phục hồi chưa đúng/hoàn tất.'}
$passed++;Write-Output 'PASS: readonly-evidence-recovery-26-visits'
Write-Output "GUARD_EVIDENCE=$run"
Write-Output "RESULT: $passed passed, 0 failed"
