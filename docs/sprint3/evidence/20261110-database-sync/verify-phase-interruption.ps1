param([Parameter(Mandatory=$true)][string]$Database)
$ErrorActionPreference='Stop'
if($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$'){throw 'Only isolated test database'}
$root=$PWD.Path;$work=Join-Path $root ('obj/SqlProjectSync/guards-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
$sql=[IO.File]::ReadAllText((Join-Path $root 'Database/QLBenhVienRangHamMat.sql')).Replace('QLBenhVienRangHamMat',$Database)
$checks=[Collections.Generic.List[object]]::new()
$counts="SELECT N'BENH_NHAN' T,COUNT_BIG(*) R FROM dbo.BENH_NHAN UNION ALL SELECT N'LICH_SU_KHAM_BENH',COUNT_BIG(*) FROM dbo.LICH_SU_KHAM_BENH UNION ALL SELECT N'HOA_DON',COUNT_BIG(*) FROM dbo.HOA_DON UNION ALL SELECT N'ANH_XQUANG_LAN_KHAM',COUNT_BIG(*) FROM dbo.ANH_XQUANG_LAN_KHAM UNION ALL SELECT N'PHAN_CONG_ANH_XQUANG',COUNT_BIG(*) FROM dbo.PHAN_CONG_ANH_XQUANG"
foreach($phase in @('Foundation','Xray')){
 $key=if($phase -eq 'Foundation'){'Sprint3Foundation'}else{'XrayStorage'}
 $errorCode=if($phase -eq 'Foundation'){53997}else{53998}
 $expectedFoundation=if($phase -eq 'Foundation'){0}else{1}
 $pattern=if($phase -eq 'Foundation'){"(?m)^ COMMIT TRANSACTION;\r?\n EXEC sys\.sp_set_session_context @key=N'RHM\.Sprint3Foundation\.Completed',@value=1;"}else{"(?m)^ COMMIT;\r?\n EXEC sys\.sp_set_session_context @key=N'RHM\.XrayStorage\.Completed',@value=1;"}
 if([regex]::Matches($sql,$pattern).Count -ne 1){throw 'Injection point must be unique'}
 $broken=[regex]::Replace($sql,$pattern,(' THROW '+$errorCode+",N'Intentional isolated "+$phase+" failure',1;`r`n"+'$0'))
 $prefix="USE [$Database];`r`nGO`r`nSELECT * INTO #SyncBefore FROM ($counts) q;`r`nEXEC sys.sp_set_session_context @key=N'RHM.Sprint3Foundation.Completed',@value=1; EXEC sys.sp_set_session_context @key=N'RHM.XrayStorage.Completed',@value=1;`r`nGO`r`n"
 $trailer=@"

GO
IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'RHM.Sprint3Foundation.Completed')),-1)<>$expectedFoundation
 OR COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'RHM.XrayStorage.Completed')),-1)<>0
 THROW 53996,N'Stale completion flag after injected failure.',1;
IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)<>64
 OR EXISTS(SELECT * FROM #SyncBefore EXCEPT SELECT * FROM ($counts) q)
 OR EXISTS(SELECT * FROM ($counts) q EXCEPT SELECT * FROM #SyncBefore)
 THROW 53996,N'Business counts changed after injected failure.',1;
PRINT N'SQL_SYNC_GUARD_OK:$phase';
GO
"@
 $file=Join-Path $work ($phase+'.sql');[IO.File]::WriteAllText($file,$prefix+$broken+$trailer,[Text.UTF8Encoding]::new($false))
 $log=Join-Path $work ($phase+'.log');$oldPreference=$ErrorActionPreference
 # Cố ý bỏ -b để mô phỏng SSMS tiếp tục GO sau batch lỗi; đánh giá marker thay exit0.
 try{$ErrorActionPreference='Continue';& sqlcmd -S . -d master -E -C -f65001 -i $file *> $log;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference}
 $body=[IO.File]::ReadAllText($log)
 if($body -notmatch ('Msg '+$errorCode+',') -or $body -notmatch 'Msg 53416,' -or $body -notmatch ('SQL_SYNC_GUARD_OK:'+$phase) -or $body -match 'PROJECT_SCHEMA_READY:'){throw ($phase+': incorrect failure handling; see '+$log)}
 $checks.Add([pscustomobject]@{Phase=$phase;Status='PASS';SqlcmdExit=$code;InjectedError=$errorCode;FinalGuard=53416;ReadyPrinted=$false;BusinessCountsPreserved=$true;Log=$log})
 Write-Output ('PASS: '+$phase+' failure blocks final readiness, clears stale flags and preserves business counts')
}
$repair=Join-Path $work 'repair.sql';[IO.File]::WriteAllText($repair,$sql,[Text.UTF8Encoding]::new($false))
& sqlcmd -S . -d master -E -C -b -f65001 -i $repair *> (Join-Path $work 'repair.log')
if($LASTEXITCODE -ne 0 -or [IO.File]::ReadAllText((Join-Path $work 'repair.log')) -notmatch 'PROJECT_SCHEMA_READY:'){throw 'Repair rerun failed'}
Write-Output 'PASS: complete rerun repairs the isolated interrupted database'
$checks | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $work 'guards.json') -Encoding UTF8
Write-Output ('SQL_SYNC_GUARDS='+$work)
