param([string]$MayChu='.')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$database='RHM_Sprint3_Test_AuditCleanup_'+[Guid]::NewGuid().ToString('N')
$sql=[IO.File]::ReadAllText((Join-Path $root 'Database/QLBenhVienRangHamMat.sql')).Replace('QLBenhVienRangHamMat',$database)
$scratch=Join-Path $root 'obj/AuditCleanupChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$testFile=Join-Path $scratch ($database+'.sql')
try {
    $fixture=@'
IF EXISTS(SELECT 1 FROM dbo.NHAT_KY_THAO_TAC WHERE ThoiGianUtc IS NULL)
 THROW 53901,N'FAIL: cài mới vẫn có nhật ký cũ chưa rõ múi giờ.',1;
-- Cả hai bản ghi đều có ngày cũ: chỉ UTC quyết định bản ghi được giữ.
INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,ThoiGianUtc,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuSau)
VALUES(N'AUDIT_TEST_LEGACY',N'TK001','2000-01-01',NULL,N'UPDATE',N'THIET_BI',N'TB001',N'Thành công',N'{"legacy":true}'),
      (N'AUDIT_TEST_CURRENT',N'TK001','2000-01-01','1999-12-31T17:00:00',N'UPDATE',N'THIET_BI',N'TB001',N'Thành công',N'{"current":true}');
SELECT * INTO #ExpectedAudit FROM dbo.NHAT_KY_THAO_TAC WHERE ThoiGianUtc IS NOT NULL;
SELECT * INTO #ExpectedSessions FROM dbo.PHIEN_DANG_NHAP;
'@
    $assertions=@'
IF EXISTS(SELECT 1 FROM dbo.NHAT_KY_THAO_TAC WHERE ThoiGianUtc IS NULL)
 THROW 53902,N'FAIL: chạy lại chưa xóa nhật ký thiếu UTC.',1;
IF EXISTS(SELECT * FROM #ExpectedAudit EXCEPT SELECT * FROM dbo.NHAT_KY_THAO_TAC)
 OR EXISTS(SELECT * FROM dbo.NHAT_KY_THAO_TAC EXCEPT SELECT * FROM #ExpectedAudit)
 THROW 53903,N'FAIL: nhật ký có UTC bị thay đổi.',1;
IF EXISTS(SELECT * FROM #ExpectedSessions EXCEPT SELECT * FROM dbo.PHIEN_DANG_NHAP)
 OR EXISTS(SELECT * FROM dbo.PHIEN_DANG_NHAP EXCEPT SELECT * FROM #ExpectedSessions)
 THROW 53904,N'FAIL: phiên đăng nhập bị thay đổi.',1;
'@
    $testSql=$sql+"`r`nGO`r`n"+$fixture+"`r`nGO`r`n"+$sql+"`r`nGO`r`n"+$assertions+"`r`nGO`r`n"+$sql+"`r`nGO`r`n"+$assertions
    [IO.File]::WriteAllText($testFile,$testSql,[Text.UTF8Encoding]::new($true))
    & sqlcmd -S $MayChu -E -C -b -f65001 -i $testFile
    if($LASTEXITCODE -ne 0){throw 'Kiểm tra dọn nhật ký cũ thất bại.'}
    Write-Output 'PASS: cài mới không có nhật ký thiếu UTC; nâng cấp xóa đúng bản ghi cũ; giữ nguyên toàn bộ nhật ký có UTC và phiên đăng nhập; chạy lại không đổi dữ liệu.'
} finally {
    & sqlcmd -S $MayChu -E -C -b -d master -Q "IF DB_ID(N'$database') IS NOT NULL DROP DATABASE [$database];"
    if($LASTEXITCODE -ne 0){throw "Không dọn được database kiểm thử $database."}
}
