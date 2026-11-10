<#
.SYNOPSIS
Nghiệm thu nội bộ P0–P6 bằng DB thử mới, ma trận21 tiêu chí và log có kiểm chứng.
.NOTES
Exit0 chỉ có nghĩa các bước nội bộ đạt. ReadyForDeployment luôn false nếu chưa đủ hạ tầng hai máy/nghiệm thu vận hành.
#>
param([string]$MayChu='.',[string]$MaLanChay=((Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[Guid]::NewGuid().ToString('N').Substring(0,8)))
$ErrorActionPreference='Stop'
if($MaLanChay -notmatch '^[A-Za-z0-9_]{1,40}$'){throw 'Mã lần chạy chỉ nhận1–40 chữ/số/gạch dưới.'}
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'NghiemThuSupport.ps1')
$work=Join-Path $root ('obj/P6Evidence/run-'+$MaLanChay)
if(Test-Path -LiteralPath $work){throw 'Lần nghiệm thu đã tồn tại; không ghi đè. Chọn mã lần chạy mới.'}
$mutex=[Threading.Mutex]::new($false,'Local\RHM_P6_Acceptance');$owned=$false
$steps=[Collections.Generic.List[object]]::new();$complete=$false;$failure=$null;$started=[DateTime]::UtcNow
$dbPrefix='RHM_Hospital_Test_P6_'+$MaLanChay
$databases=[ordered]@{New=$dbPrefix+'_New';Upgrade=$dbPrefix+'_Upgrade';Rerun=$dbPrefix+'_Rerun';Clinical=$dbPrefix+'_Clinical';Service=$dbPrefix+'_Service';Concurrent=$dbPrefix+'_Concurrent';Xray=$dbPrefix+'_Xray';Demo='RHM_Hospital_Test_P5Demo_P6_'+$MaLanChay}
function Step([string]$Name,[string]$Tool,[string[]]$ToolArgs,[int]$Minimum=1,[string]$Mode='Summary',[string[]]$Required=@()){
 $toolPath=if([IO.Path]::IsPathRooted($Tool)){$Tool}else{Join-Path $PSScriptRoot $Tool}
 $result=Invoke-P6Tool -Name $Name -Tool $toolPath -ToolArgs $ToolArgs -Directory $work -Mode $Mode -MinimumPassed $Minimum -RequiredPatterns $Required
 $steps.Add($result);Write-Host ("STEP {0}: {1} ({2}s)" -f $Name,$result.Status,$result.DurationSeconds)
 if($result.Status -ne 'PASS'){throw ($Name+': '+$result.Reason)}
 return $result
}
function Marker([object]$StepResult,[string]$Key){
 $p6MarkerLines=@(Get-Content -LiteralPath $StepResult.Log|Where-Object {$_ -like ($Key+'=*')})
 if(!$p6MarkerLines.Count){throw ('Thiếu artifact marker '+$Key)}
 $p6MarkerLines[-1].Substring($Key.Length+1)
}
function Install([string]$Name,[string]$Source,[string]$Database,[bool]$Fresh,[int]$Tables){
 if($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$'){throw 'Không cài SQL ngoài DB thử.'}
 $content=[IO.File]::ReadAllText($Source).Replace('QLBenhVienRangHamMat',$Database)
 $guard=if($Fresh){"USE [master];`r`nGO`r`nDECLARE @p6Lock int; EXEC @p6Lock=sys.sp_getapplock @Resource=N'RHM:P6:Create:$Database',@LockMode='Exclusive',@LockOwner='Session',@LockTimeout=15000; IF @p6Lock<0 THROW 53520,N'P6 creation busy.',1; IF DB_ID(N'$Database') IS NOT NULL THROW 53521,N'P6 DB already exists; no overwrite.',1;`r`nGO`r`n"}else{''}
 $trailer="`r`nGO`r`nIF DB_NAME()<>N'$Database' OR (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)<>$Tables THROW 53522,N'P6 schema/table count mismatch.',1; PRINT N'P6_SQL_COMPLETE:$Database`:$Tables';`r`nGO`r`n"
 $file=Join-Path $work ($Name+'.sql');[IO.File]::WriteAllText($file,$guard+$content+$trailer,[Text.UTF8Encoding]::new($false))
 Step $Name $script:sqlTool @('-MayChu',$MayChu,'-SqlFile',$file) 0 'Marker' @('^P6_SQL_COMPLETE:'+([regex]::Escape($Database))+':'+$Tables+'\s*$')
}
function Copy-Ui([string[]]$Names){
 foreach($name in $Names){$source=Join-Path $root ('obj/RhmFinal/'+$name);if(!(Test-Path -LiteralPath $source)){throw ('Thiếu ảnh minh chứng '+$name)};Copy-Item -LiteralPath $source -Destination (Join-Path $work ('ui/'+$name)) -Force}
}
try {
 try {$owned=$mutex.WaitOne(0)}catch [Threading.AbandonedMutexException]{$owned=$true}
 if(!$owned){throw 'Đang có lượt nghiệm thu khác; không chạy chồng bộ kiểm dùng scratch chung.'}
 New-Item -ItemType Directory -Path $work | Out-Null
 New-Item -ItemType Directory -Path (Join-Path $work 'ui') | Out-Null
 Push-Location -LiteralPath $root
 try {
  $sourceBefore=Get-P6SourceManifest $root;Write-P6Json (Join-Path $work 'source-before.json') $sourceBefore
  $baseline=Export-P6Baseline $root (Join-Path $work 'baseline-git.sql');Write-P6Json (Join-Path $work 'baseline.json') $baseline
  $script:sqlTool=Join-Path $work 'invoke-sql.ps1'
  [IO.File]::WriteAllText($script:sqlTool,@'
param([string]$MayChu,[string]$SqlFile)
$ErrorActionPreference='Stop'
& sqlcmd -S $MayChu -d master -E -C -b -I -f65001 -i $SqlFile
exit $LASTEXITCODE
'@,[Text.UTF8Encoding]::new($false))
  $checks=($databases.Values|ForEach-Object {"IF DB_ID(N'$_') IS NOT NULL THROW 53521,N'P6 requires all new test databases.',1;"}) -join "`r`n"
  $preflight=Join-Path $work 'preflight.sql';[IO.File]::WriteAllText($preflight,$checks+"`r`nPRINT N'P6_DATABASES_NEW';",[Text.UTF8Encoding]::new($false))
  $null=Step 'preflight' $script:sqlTool @('-MayChu',$MayChu,'-SqlFile',$preflight) 0 'Marker' @('^P6_DATABASES_NEW\s*$')
  $null=Step 'runner-checks' 'KiemTraBoNghiemThu.ps1' @() 16
  $current=Join-Path $root 'Database/QLBenhVienRangHamMat.sql'
  $null=Install 'install-new' $current $databases.New $true 64
  $null=Step 'foundation-new' 'KiemTraNenTangSprint3.ps1' @('-MayChu',$MayChu,'-Database',$databases.New) 35
  $null=Install 'install-baseline' $baseline.Path $databases.Upgrade $true 62
  $null=Step 'save-legacy' 'KiemTraNenTangSprint3.ps1' @('-MayChu',$MayChu,'-Database',$databases.Upgrade,'-CheDo','LuuDoiChieu') 0 'Marker' @('^Saved 62 table fingerprints')
  $null=Install 'install-upgrade' $current $databases.Upgrade $false 64
  $null=Step 'preserve-legacy' 'KiemTraNenTangSprint3.ps1' @('-MayChu',$MayChu,'-Database',$databases.Upgrade,'-CheDo','DoiChieu') 62
  $null=Step 'foundation-upgrade' 'KiemTraNenTangSprint3.ps1' @('-MayChu',$MayChu,'-Database',$databases.Upgrade) 35
  $null=Step 'save-upgraded' 'KiemTraNenTangSprint3.ps1' @('-MayChu',$MayChu,'-Database',$databases.Upgrade,'-CheDo','LuuDoiChieu') 0 'Marker' @('^Saved 64 table fingerprints')
  foreach($iteration in 1,2){
   $null=Install ('rerun-'+$iteration) $current $databases.Upgrade $false 64
   $null=Step ('preserve-rerun-'+$iteration) 'KiemTraNenTangSprint3.ps1' @('-MayChu',$MayChu,'-Database',$databases.Upgrade,'-CheDo','DoiChieu') 64
  }
  $null=Step 'seed-sealed' 'KiemTraNenTangSprint3.ps1' @('-MayChu',$MayChu,'-Database',$databases.Rerun,'-TaoDatabaseKiemThu','-CheDo','GhiDuLieuChayLai') 0 'Marker' @('^PASS: --seed-rerun\s*$')
  $null=Step 'rerun-sealed' 'KiemTraNenTangSprint3.ps1' @('-MayChu',$MayChu,'-Database',$databases.Rerun,'-TaoDatabaseKiemThu','-CheDo','KiemTraChayLai') 0 'Marker' @('^PASS: --verify-rerun\s*$')
  $null=Step 'interrupt-sealed' 'KiemTraNenTangSprint3.ps1' @('-MayChu',$MayChu,'-Database',$databases.Rerun,'-TaoDatabaseKiemThu','-CheDo','KiemTraChayLai','-MoPhongLoiTruocMigrationCuoi') 0 'Marker' @('Msg 53999','^PASS: --verify-rerun\s*$')
  $null=Step 'repair-sealed' 'KiemTraNenTangSprint3.ps1' @('-MayChu',$MayChu,'-Database',$databases.Rerun,'-TaoDatabaseKiemThu','-CheDo','KiemTraChayLai') 0 'Marker' @('^PASS: --verify-rerun\s*$')
  $null=Step 'sql-sync' 'KiemTraDongBoSql.ps1' @('-MayChu',$MayChu,'-Database',$databases.Upgrade) 32
  $null=Step 'service' 'KiemTraKhamDichVu.ps1' @('-MayChu',$MayChu,'-Database',$databases.Service,'-TaoDatabaseKiemThu') 31
  Copy-Ui @('service-reception.png','service-doctor-switch.png','ordinary-reception.png','service-missing-shift.png','service-fee-preview.png','service-invoice.png','service-clinical-summary.png')
  $null=Step 'concurrent' 'KiemTraKhamDichVu.ps1' @('-MayChu',$MayChu,'-Database',$databases.Concurrent,'-TaoDatabaseKiemThu','-DongThoi') 1
  $null=Step 'clinical' 'KiemTraQuyTrinhBenhVien.ps1' @('-MayChu',$MayChu,'-Database',$databases.Clinical,'-TaoDatabaseKiemThu') 47
  Copy-Ui @('prescription-dialog.png','visit-summary.png','service-history.png')
  $null=Step 'smoke27' 'KiemTraQuyTrinhBenhVien.ps1' @('-MayChu',$MayChu,'-Database',$databases.New,'-ChiGiaoDien') 2 'Summary' @('27 phân hệ')
  $xray=Step 'xray' 'KiemTraAnhXquang.ps1' @('-MayChu',$MayChu,'-Database',$databases.Xray,'-TaoDatabaseKiemThu') 32
  $xrayRoot=Marker $xray 'XRAY_ROOT'
  $restore=Step 'xray-restore' 'KiemTraSaoLuuAnhXquang.ps1' @('-MayChu',$MayChu,'-Database',$databases.Xray,'-ThuMucKho',$xrayRoot) 2 'Summary' @('^RESTORE_COMPLETE=','^PASS: restore từ chối database','hidden assets retained')
  Copy-Ui @('xray-editor.png','xray-history.png','xray-unconfigured.png','xray-assigned.png','xray-fullview.png')
  if($xrayRoot -match '^[A-Za-z]:\\'){
   $loopback='\\localhost\'+$xrayRoot.Substring(0,1)+'$'+$xrayRoot.Substring(2)
   if(Test-Path -LiteralPath $loopback){
    $null=Step 'smb-loopback' 'KiemTraKhoAnhHaiMay.ps1' @('-MayChu',$MayChu,'-Database',$databases.Xray,'-ThuMucKho',$loopback,'-TepBangChung',(Join-Path $work 'smb-loopback.json')) 0 'Marker' @('^PROBE_COMPLETE=','^PASS: UNC image probe;')
   } else {Write-Host 'SMB_LOOPBACK=NOT_AVAILABLE (không tạo share/đổi ACL)'}
  }
  $demo=Step 'create-demo' 'TaoDuLieuTrinhDien.ps1' @('-MayChu',$MayChu,'-Database',$databases.Demo) 0 'Marker' @(('^DEMO_DATABASE='+([regex]::Escape($databases.Demo))+'\s*$'),'^DEMO_COMPLETE=','^PASS: 18 synthetic patients; 26 visits')
  $p5=Step 'p5' 'KiemTraDoiSoatP5.ps1' @('-MayChu',$MayChu,'-Database',$databases.Demo,'-KiemTraTrinhDien') 21
  $null=Step 'p5-guards' 'KiemTraCongCuP5.ps1' @('-MayChu',$MayChu,'-Database',$databases.Demo) 7
  foreach($name in @('p5-overview.png','p5-conditions.png')){Copy-Item -LiteralPath (Join-Path $root ('obj/P5Evidence/'+$name)) -Destination (Join-Path $work ('ui/'+$name)) -Force}
  $report=Marker $p5 'P5_REPORT_EVIDENCE';$demoPath=Marker $demo 'DEMO_COMPLETE'
  foreach($name in @('conditions.csv','monthly-finance.csv')){Copy-Item -LiteralPath (Join-Path $report $name) -Destination (Join-Path $work $name)}
  foreach($name in @('manifest.json','lineage.csv')){Copy-Item -LiteralPath (Join-Path $demoPath $name) -Destination (Join-Path $work $name)}
  $null=Step 'reports' 'KiemTraBaoCao.ps1' @('-MayChu',$MayChu,'-Database',$databases.Upgrade,'-Sql','-Render') 41 'Lines'
  $constraintSql=Join-Path $work 'constraints.sql'
  $constraintBody=@(foreach($db in @($databases.New,$databases.Upgrade,$databases.Rerun)){
@"
USE [$db];
GO
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1)
 OR EXISTS(SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1)
 THROW 53523,N'P6 constraints disabled or untrusted.',1;
CREATE TABLE #P6Violations([Table] nvarchar(max),[Constraint] nvarchar(max),[Where] nvarchar(max));
INSERT #P6Violations EXEC(N'DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS,NO_INFOMSGS');
IF EXISTS(SELECT 1 FROM #P6Violations) THROW 53524,N'P6 DBCC found constraint violations.',1;
DROP TABLE #P6Violations;
PRINT N'P6_CONSTRAINTS_OK:$db';
GO
"@
  }) -join "`r`n"
  [IO.File]::WriteAllText($constraintSql,$constraintBody,[Text.UTF8Encoding]::new($false))
  $constraintMarkers=@($databases.New,$databases.Upgrade,$databases.Rerun)|ForEach-Object {'^P6_CONSTRAINTS_OK:'+([regex]::Escape($_))+'\s*$'}
  $null=Step 'constraints' $script:sqlTool @('-MayChu',$MayChu,'-SqlFile',$constraintSql) 0 'Marker' $constraintMarkers
  $null=Step 'build-schema' 'KiemTraBanGiao.ps1' @('-MayChu',$MayChu,'-Database',$databases.Upgrade,'-KiemTraSql') 0 'Marker' @('Build succeeded\.','0 Error\(s\)','^PASS: HospitalWorkflow3')
  $sourceAfter=Get-P6SourceManifest $root;Write-P6Json (Join-Path $work 'source-after.json') $sourceAfter
  if(($sourceBefore|ConvertTo-Json -Depth 4 -Compress) -cne ($sourceAfter|ConvertTo-Json -Depth 4 -Compress)){throw 'Nguồn đã thay đổi trong khi nghiệm thu; cần chạy lại với bản mã ổn định.'}
  $complete=$true
 } finally {Pop-Location}
} catch {$failure=$_.Exception.Message;Write-Host ('FAIL: '+$failure)}
finally {
 if(Test-Path -LiteralPath $work){
  $dependencies=[ordered]@{TN01=@('service');TN02=@('service');TN03=@('service');TN04=@('service');TN05=@('service');TN06=@('concurrent');TN07=@('service');HD01=@('service');HD02=@('foundation-new','service');HD03=@('service');HD04=@('foundation-new','service');HD05=@('service');XR01=@('xray');XR02=@('xray');XR03=@('xray');XR04=@('xray-restore');DB01=@('install-new','preserve-legacy','preserve-rerun-1','preserve-rerun-2','interrupt-sealed');DB02=@('foundation-upgrade','sql-sync','constraints','p5');BC01=@('p5','reports');BC02=@('p5','reports');RG01=@('sql-sync','clinical','smoke27','reports','build-schema')}
  $matrix=@(foreach($id in $dependencies.Keys){
   $required=$dependencies[$id];$found=@($steps|Where-Object {$_.Name -in $required});$status=if(@($found|Where-Object Status -eq 'FAIL').Count){'FAIL'}elseif($found.Count -ne $required.Count){'NOT_RUN'}else{'PASS'}
   $note=if($id -eq 'XR04'){'SQL/file restore trên cùng máy đã kiểm nếu bước xanh; còn kho UNC/hai máy và restore trên máy khác.'}else{''}
   if($id -eq 'XR04' -and $status -eq 'PASS'){$status='PENDING_ENVIRONMENT'}
   [pscustomobject]@{Id=$id;Status=$status;Evidence=$found.Log;Note=$note}
  })
  Write-P6Json (Join-Path $work 'matrix.json') $matrix
  $summary=[pscustomobject]@{Run=$MaLanChay;Machine=[Environment]::MachineName;SqlServer=$MayChu;StartedUtc=$started;FinishedUtc=[DateTime]::UtcNow;LocalStatus=if($failure){'FAIL'}elseif($complete){'PASS'}else{'NOT_FINISHED'};ReadyForDeployment=$false;Failure=$failure;Databases=$databases;Steps=$steps.ToArray();Matrix=$matrix;Pending=@('SMB/hai máy thực và phục hồi trên máy khác','Cấu hình/quyền và nghiệm thu vận hành trước triển khai');SourceManifest='source-after.json';GitCommit=(& git -C $root rev-parse HEAD)}
  Write-P6Json (Join-Path $work 'acceptance.json') $summary
  Write-Output ('P6_EVIDENCE='+$work);Write-Output ('P6_LOCAL_STATUS='+$summary.LocalStatus);Write-Output 'P6_READY_FOR_DEPLOYMENT=False'
 }
 if($owned){$mutex.ReleaseMutex()};$mutex.Dispose()
}
if(!$complete -or $failure){exit 1}
