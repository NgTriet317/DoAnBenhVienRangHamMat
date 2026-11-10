param([Parameter(Mandatory=$true)][string]$Database,[switch]$ExpectBlocked)
$ErrorActionPreference='Stop'
if($Database -notmatch '^RHM_Hospital_Test_[A-Za-z0-9_]+$'){throw 'Only isolated database'}
$work=Join-Path $PWD ('obj/SqlProjectSync/audit-guard-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $work | Out-Null
$source=[IO.File]::ReadAllText((Join-Path $PWD 'Database/QLBenhVienRangHamMat.sql')).Replace('QLBenhVienRangHamMat',$Database)
$setup=@"
USE [$Database];
GO
DROP INDEX UX_S3_Event ON dbo.NHAT_KY_THAO_TAC;
DECLARE @event uniqueidentifier=NEWID();
INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,ThoiGianUtc,MaSuKien,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua)
VALUES(N'SQL_SYNC_AUDIT_DUP_A',N'TK001',SYSDATETIME(),SYSUTCDATETIME(),@event,N'CREATE',N'FIXTURE',N'FIXTURE',N'Thành công'),
      (N'SQL_SYNC_AUDIT_DUP_B',N'TK001',SYSDATETIME(),SYSUTCDATETIME(),@event,N'CREATE',N'FIXTURE',N'FIXTURE',N'Thành công');
GO
"@
$tail=@"

GO
IF(SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE MaNK IN(N'SQL_SYNC_AUDIT_DUP_A',N'SQL_SYNC_AUDIT_DUP_B') AND ThoiGianUtc IS NOT NULL)<>2
 THROW 53996,N'Audit regression removed the controlled UTC rows.',1;
PRINT N'AUDIT_DUPLICATES_PRESERVED';
GO
"@
$file=Join-Path $work 'run.sql';[IO.File]::WriteAllText($file,$setup+"`r`n"+$source+$tail,[Text.UTF8Encoding]::new($false));$log=Join-Path $work 'run.log'
try {
 $oldPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& sqlcmd -S . -d master -E -C -I -f65001 -i $file *> $log;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference}
 $body=[IO.File]::ReadAllText($log);$ready=$body -match 'PROJECT_SCHEMA_READY:'
 if($body -notmatch 'Msg 1505,' -or $body -notmatch 'AUDIT_DUPLICATES_PRESERVED'){throw 'Audit reproduction did not reach expected failure/preservation'}
 if($ExpectBlocked -and ($ready -or $body -notmatch 'Msg 53416,')){throw 'Audit failure still reports readiness'}
 if(!$ExpectBlocked -and !$ready){throw 'Expected old unsafe-ready reproduction'}
 [pscustomobject]@{ExpectedBlocked=[bool]$ExpectBlocked;ReadyPrinted=$ready;DuplicateUtcRowsPreserved=$true;SqlcmdExit=$code;Log=$log} | ConvertTo-Json | Set-Content (Join-Path $work 'audit-guard.json') -Encoding UTF8
 Write-Output ('PASS: audit duplicate failure reproduced; ReadyPrinted='+$ready+'; UTC rows retained')
} finally {
 $repair=Join-Path $work 'repair.sql';$cleanup="USE [$Database];`r`nGO`r`nDELETE dbo.NHAT_KY_THAO_TAC WHERE MaNK IN(N'SQL_SYNC_AUDIT_DUP_A',N'SQL_SYNC_AUDIT_DUP_B');`r`nGO`r`n"
 [IO.File]::WriteAllText($repair,$cleanup+$source,[Text.UTF8Encoding]::new($false))
 & sqlcmd -S . -d master -E -C -b -I -f65001 -i $repair *> (Join-Path $work 'repair.log')
 if($LASTEXITCODE -ne 0){throw 'Isolated audit fixture repair failed'}
 Write-Output ('SQL_AUDIT_GUARD='+$work)
}
