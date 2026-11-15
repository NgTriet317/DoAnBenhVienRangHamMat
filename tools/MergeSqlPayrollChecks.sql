-- Executed after the existing SqlSyncChecks rollback fixture with explicit DonGiaCa=100000.
UPDATE dbo.DON_XIN_NGHI SET LoaiNghi=N'Nghỉ phép năm' WHERE MaDonNghi=N'SYNC_DN';
INSERT dbo.CA_LAM(MaCa,GioBatDau,GioKetThuc,TrangThai) VALUES(N'SYNC_CA2','13:00','17:00',N'Đang hoạt động');
INSERT dbo.LICH_LAM_VIEC(MaLLV,MaNV,MaCa,NgayLam,TrangThai,TrangThaiHienDien)
VALUES(N'SYNC_LEAVE1',N'SYNC_NV',N'SYNC_CA','20000105',N'Đã duyệt',N'Nghỉ phép'),
      (N'SYNC_LEAVE2',N'SYNC_NV',N'SYNC_CA2','20000105',N'Đã duyệt',N'Nghỉ phép');
EXEC dbo.RHM_GhiNhanQuyPhepKhiDuyet @MaDonNghi=N'SYNC_DN';
EXEC dbo.RHM_GhiNhanQuyPhepKhiDuyet @MaDonNghi=N'SYNC_DN';
IF(SELECT COUNT(*) FROM dbo.RHM_NGAY_PHEP_NAM WHERE MaNV=N'SYNC_NV')<>1
 THROW 55996,N'Annual leave must consume one ledger day for two shifts and remain idempotent.',1;
EXEC dbo.RHM_TinhLuongKy @TuNgay='20000101',@DenNgay='20000131',@MaNVTinh=@admin,@MaBL=N'SYNC_BL';
EXEC dbo.RHM_TinhLuongKy @TuNgay='20000101',@DenNgay='20000131',@MaNVTinh=@admin,@MaBL=N'SYNC_BL';
IF NOT EXISTS(SELECT 1 FROM dbo.BANG_LUONG WHERE MaBL=N'SYNC_BL' AND DonGiaCa=100000
 AND CongNghiPhep=0.5 AND CongThucTe=1.25 AND LuongTheoCa=250000 AND TienHoaHong=10000 AND TongLuong=87600)
 THROW 55996,N'Explicit shift policy must use shift rate, half-day annual leave and paid-service commission.',1;
IF(SELECT COUNT(*) FROM dbo.CHI_TIET_HOA_HONG_LUONG WHERE MaBL=N'SYNC_BL')<>1
 THROW 55996,N'Recalculating fixed-shift payroll duplicated commission.',1;
EXEC dbo.RHM_ChotLuongNhanVien @MaBL=N'SYNC_BL',@MaNVChot=@admin,@CongDaXem=1.25,@TongDaXem=87600,@HoaHongDaXem=10000;
IF NOT EXISTS(SELECT 1 FROM dbo.BANG_LUONG WHERE MaBL=N'SYNC_BL' AND TrangThai=N'Đã chốt')
 THROW 55996,N'Fixed-shift salary cannot be closed after reconciliation.',1;
PRINT N'PASS: fixed-shift salary, one annual-leave day for two shifts, commission retry and closing.';
