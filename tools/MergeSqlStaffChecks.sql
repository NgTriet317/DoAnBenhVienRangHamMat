SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
IF DB_NAME() NOT LIKE N'RHM[_]Hospital[_]Test[_]%'
    THROW 55990,N'Only isolated hospital test databases are allowed.',1;
EXEC sys.sp_set_session_context @key=N'RHM.MaPhien',@value=NULL;
BEGIN TRY
 BEGIN TRANSACTION;
 -- A dedicated doctor and shift avoid altering the packaged clinical fixtures.
 INSERT dbo.NHAN_VIEN(MaNV,HoTen,MaChucVu,MaCK,NgayVaoLam,TrangThai)
 VALUES(N'MERGE_AI_DOCTOR',N'FIXTURE AI doctor',N'CV002',N'CK001','20200101',N'Đang làm việc');
 INSERT dbo.TAI_KHOAN(MaTK,TenDangNhap,MatKhauHash,MaVaiTro,MaNV,TrangThai)
 VALUES(N'MERGE_AI_TK',N'merge_ai_fixture',N'fixture',N'VTRO002',N'MERGE_AI_DOCTOR',N'Đang hoạt động');
 -- Staff AI approval depends on its own menu and reception duty, not patient-screen visibility.
 INSERT dbo.VAI_TRO(MaVaiTro,TenVaiTro,MoTa)
 VALUES(N'MERGE_AI_RECEPTION_ROLE',N'FIXTURE reception menu',N'Rollback-only explicit menu denial');
 INSERT dbo.PHAN_QUYEN_VAI_TRO(MaVaiTro,MaPhanHe,DuocTruyCap)
 VALUES(N'MERGE_AI_RECEPTION_ROLE',N'BenhNhan',0),(N'MERGE_AI_RECEPTION_ROLE',N'PhieuHuongDan',1);
 UPDATE dbo.TAI_KHOAN SET MaVaiTro=N'MERGE_AI_RECEPTION_ROLE' WHERE MaTK=N'TK005';
 DECLARE @begin datetime2(0)=DATEADD(hour,-1,SYSDATETIME()),@end datetime2(0)=DATEADD(hour,1,SYSDATETIME());
 INSERT dbo.CA_LAM(MaCa,GioBatDau,GioKetThuc,TrangThai)
 VALUES(N'MERGE_AI_CA',CONVERT(time(0),@begin),CONVERT(time(0),@end),N'Đang hoạt động');
 INSERT dbo.LICH_LAM_VIEC(MaLLV,MaNV,MaCa,NgayLam,MaPhong,TrangThai,TrangThaiHienDien)
 VALUES(N'MERGE_AI_SHIFT',N'MERGE_AI_DOCTOR',N'MERGE_AI_CA',CONVERT(date,@begin),N'PH001',N'Đã xác nhận',N'Có mặt');
 DECLARE @login uniqueidentifier=NEWID(),@normal uniqueidentifier=NEWID(),@service uniqueidentifier=NEWID();
 INSERT dbo.PHIEN_DANG_NHAP(MaPhien,MaTK,DangNhapUtc,HoatDongCuoiUtc,TrangThaiPhien)
 VALUES(@login,N'TK005',SYSUTCDATETIME(),SYSUTCDATETIME(),N'Đang mở');
 EXEC sys.sp_set_session_context @key=N'RHM.MaPhien',@value=@login;
 INSERT dbo.AI_PHIEN_TIEP_NHAN(MaPhien,TokenHash,TrangThai,DongY,HetHan)
 VALUES(@normal,HASHBYTES('SHA2_256',CONVERT(nvarchar(36),@normal)),N'ChoNhanVien',1,DATEADD(day,1,SYSUTCDATETIME())),
       (@service,HASHBYTES('SHA2_256',CONVERT(nvarchar(36),@service)),N'ChoNhanVien',1,DATEADD(day,1,SYSUTCDATETIME()));
 INSERT dbo.AI_TIEP_NHAN_NHAP(MaPhien,DaTungKham,HoTen,NgaySinh,SoDienThoai,LyDoKham,LoaiKham)
 VALUES(@normal,0,N'FIXTURE AI normal','19900101',N'0998800101',N'FIXTURE normal',N'ThongThuong'),
       (@service,0,N'FIXTURE AI service','19900102',N'0998800102',N'FIXTURE service',N'DichVu');
 DECLARE @bn nvarchar(50),@hs nvarchar(50),@receipt nvarchar(50),@queue int,@first nvarchar(50);
 EXEC dbo.AI_TaoPhieuTuBanKhai @MaPhien=@normal,@MaNV=N'NV005',@HoTen=N'FIXTURE AI normal',@NgaySinh='19900101',
  @SDT=N'0998800101',@LyDoKham=N'FIXTURE normal',@LoaiKham=N'ThongThuong',@MaPhong=N'PH001',@MaBS=N'MERGE_AI_DOCTOR',
  @MaBNOut=@bn OUTPUT,@MaHSOut=@hs OUTPUT,@MaPhieuOut=@receipt OUTPUT,@SoThuTuOut=@queue OUTPUT;
 IF NOT EXISTS(SELECT 1 FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@receipt AND MaNVTiepNhan=N'NV005' AND LoaiKham=N'Thông thường' AND PhiPhuThuDichVu=0)
  THROW 55995,N'Normal draft did not produce a canonical staff receipt with zero surcharge.',1;
 SET @first=@receipt;
 EXEC dbo.AI_TaoPhieuTuBanKhai @MaPhien=@normal,@MaNV=N'NV005',@HoTen=N'FIXTURE AI normal',@NgaySinh='19900101',
  @SDT=N'0998800101',@LyDoKham=N'FIXTURE normal',@LoaiKham=N'ThongThuong',@MaPhong=N'PH001',@MaBS=N'MERGE_AI_DOCTOR',
  @MaBNOut=@bn OUTPUT,@MaHSOut=@hs OUTPUT,@MaPhieuOut=@receipt OUTPUT,@SoThuTuOut=@queue OUTPUT;
 IF @first<>@receipt THROW 55995,N'Retry created a second receipt.',1;
 EXEC dbo.AI_TaoPhieuTuBanKhai @MaPhien=@service,@MaNV=N'NV005',@HoTen=N'FIXTURE AI service',@NgaySinh='19900102',
  @SDT=N'0998800102',@LyDoKham=N'FIXTURE service',@LoaiKham=N'DichVu',@MaPhong=N'PH001',@MaBS=N'MERGE_AI_DOCTOR',@MaDV=N'DV001',
  @MaBNOut=@bn OUTPUT,@MaHSOut=@hs OUTPUT,@MaPhieuOut=@receipt OUTPUT,@SoThuTuOut=@queue OUTPUT;
 IF NOT EXISTS(SELECT 1 FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@receipt AND MaNVTiepNhan=N'NV005' AND LoaiKham=N'Dịch vụ' AND PhiPhuThuDichVu=30000)
  THROW 55995,N'Service draft did not produce a canonical staff receipt with one 30000 surcharge.',1;
 IF(SELECT COUNT(*) FROM dbo.AI_KET_QUA_TAO_PHIEU WHERE MaPhien IN(@normal,@service))<>2
  OR(SELECT COUNT(*) FROM dbo.AI_PHIEN_TIEP_NHAN WHERE MaPhien IN(@normal,@service) AND TrangThai=N'DaDuyet')<>2
  THROW 55995,N'Staff approval and official receipt were not linked atomically.',1;
 DECLARE @manual uniqueidentifier=NEWID();
 INSERT dbo.BENH_NHAN(MaBN,HoTenBN,NgaySinh,SDT,TrangThai)
 VALUES(N'MERGE_AI_MANUAL_BN',N'FIXTURE AI manual','19900103',N'0998800103',N'Đang theo dõi');
 INSERT dbo.HO_SO_BENH_AN(MaHS,MaBN,NgayLap,LyDoKhamBanDau,TrangThai)
 VALUES(N'MERGE_AI_MANUAL_HS',N'MERGE_AI_MANUAL_BN',SYSDATETIME(),N'FIXTURE manual',N'Đang theo dõi');
 INSERT dbo.PHIEU_KHAM_BENH(MaPhieu,MaHS,MaPhong,MaBS,MaNVTiepNhan,SoThuTu,ThoiGianTiepNhan,LyDoKham,TrangThai)
 VALUES(N'MERGE_AI_MANUAL',N'MERGE_AI_MANUAL_HS',N'PH001',N'MERGE_AI_DOCTOR',N'NV005',@queue+1,SYSDATETIME(),N'FIXTURE manual',N'Chờ khám');
 INSERT dbo.AI_PHIEN_TIEP_NHAN(MaPhien,TokenHash,TrangThai,DongY,HetHan)
 VALUES(@manual,HASHBYTES('SHA2_256',CONVERT(nvarchar(36),@manual)),N'ChoNhanVien',1,DATEADD(day,1,SYSUTCDATETIME()));
 INSERT dbo.AI_TIEP_NHAN_NHAP(MaPhien,HoTen,NgaySinh,SoDienThoai,LyDoKham,LoaiKham)
 VALUES(@manual,N'FIXTURE AI manual','19900103',N'0998800103',N'FIXTURE manual',N'ThongThuong');
 EXEC dbo.AI_XacNhanPhien @MaPhien=@manual,@MaPhieu=N'MERGE_AI_MANUAL',@MaNV=N'NV005';
 IF NOT EXISTS(SELECT 1 FROM dbo.AI_PHIEN_TIEP_NHAN WHERE MaPhien=@manual AND MaPhieu=N'MERGE_AI_MANUAL' AND TrangThai=N'DaDuyet')
  THROW 55995,N'Reception staff with only the AI-review menu could not link an existing receipt.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.PHAN_QUYEN_VAI_TRO WHERE MaVaiTro=N'MERGE_AI_RECEPTION_ROLE' AND MaPhanHe=N'BenhNhan' AND DuocTruyCap=0)
  THROW 55995,N'AI approval unexpectedly broadened patient-screen permissions.',1;
 PRINT N'PASS: BenhNhan=0/PhieuHuongDan=1 permits reception staff issuance and existing-receipt linking; patient menu stays denied.';
 ROLLBACK;
 EXEC sys.sp_set_session_context @key=N'RHM.MaPhien',@value=NULL;
 PRINT N'PASS: normal/service staff issuance, canonical fee, idempotency and atomic links (rolled back).';
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 EXEC sys.sp_set_session_context @key=N'RHM.MaPhien',@value=NULL;
 THROW;
END CATCH;
GO
-- A caller without a current staff session cannot use a forged MaNV.
DECLARE @session uniqueidentifier=NEWID(),@bn nvarchar(50),@hs nvarchar(50),@receipt nvarchar(50),@queue int,@denied bit=0;
BEGIN TRY
 EXEC dbo.AI_TaoPhieuTuBanKhai @MaPhien=@session,@MaNV=N'NV005',@HoTen=N'FIXTURE forged caller',@NgaySinh='19900101',
  @SDT=N'0998800199',@LyDoKham=N'FIXTURE',@LoaiKham=N'ThongThuong',@MaPhong=N'PH001',@MaBS=N'NV002',
  @MaBNOut=@bn OUTPUT,@MaHSOut=@hs OUTPUT,@MaPhieuOut=@receipt OUTPUT,@SoThuTuOut=@queue OUTPUT;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()=55020 SET @denied=1; ELSE THROW;
END CATCH;
IF @denied=0 THROW 55995,N'Forged staff actor was accepted.',1;
PRINT N'PASS: missing staff session is rejected.';
GO
DECLARE @draft uniqueidentifier=NEWID(),@denied bit=0;
BEGIN TRY
 EXEC dbo.AI_XacNhanPhien @MaPhien=@draft,@MaPhieu=N'FIXTURE_FORGED',@MaNV=N'NV005';
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()=55020 SET @denied=1; ELSE THROW;
END CATCH;
IF @denied=0 THROW 55995,N'Linking an existing receipt accepted a forged staff actor.',1;
PRINT N'PASS: linking a receipt also requires a current staff session.';
GO
-- A doctor lacks business TiepNhan even when explicitly granted the AI-review menu.
DECLARE @action int=0;
WHILE @action<2
BEGIN
 DECLARE @login uniqueidentifier=NEWID(),@draft uniqueidentifier=NEWID(),@bn nvarchar(50),@hs nvarchar(50),@receipt nvarchar(50),@queue int,@denied bit=0;
 BEGIN TRY
  EXEC sys.sp_set_session_context @key=N'RHM.MaPhien',@value=NULL;
  BEGIN TRANSACTION;
  INSERT dbo.VAI_TRO(MaVaiTro,TenVaiTro,MoTa)
  VALUES(N'MERGE_AI_NOINTAKE_ROLE',N'FIXTURE no intake duty',N'Rollback-only explicit menu grant');
  INSERT dbo.PHAN_QUYEN_VAI_TRO(MaVaiTro,MaPhanHe,DuocTruyCap)
  VALUES(N'MERGE_AI_NOINTAKE_ROLE',N'BenhNhan',0),(N'MERGE_AI_NOINTAKE_ROLE',N'PhieuHuongDan',1);
  UPDATE dbo.TAI_KHOAN SET MaVaiTro=N'MERGE_AI_NOINTAKE_ROLE' WHERE MaTK=N'TK002';
  INSERT dbo.PHIEN_DANG_NHAP(MaPhien,MaTK,DangNhapUtc,HoatDongCuoiUtc,TrangThaiPhien)
  VALUES(@login,N'TK002',SYSUTCDATETIME(),SYSUTCDATETIME(),N'Đang mở');
  EXEC sys.sp_set_session_context @key=N'RHM.MaPhien',@value=@login;
  IF @action=0
   EXEC dbo.AI_TaoPhieuTuBanKhai @MaPhien=@draft,@MaNV=N'NV002',@HoTen=N'FIXTURE doctor without intake',@NgaySinh='19900101',
    @SDT=N'0998800198',@LyDoKham=N'FIXTURE',@LoaiKham=N'ThongThuong',@MaPhong=N'PH001',@MaBS=N'NV002',
    @MaBNOut=@bn OUTPUT,@MaHSOut=@hs OUTPUT,@MaPhieuOut=@receipt OUTPUT,@SoThuTuOut=@queue OUTPUT;
  ELSE
   EXEC dbo.AI_XacNhanPhien @MaPhien=@draft,@MaPhieu=N'FIXTURE_FORGED',@MaNV=N'NV002';
  IF @@TRANCOUNT>0 ROLLBACK;
 END TRY
 BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK;
  IF ERROR_NUMBER()=55020 SET @denied=1; ELSE THROW;
 END CATCH;
 EXEC sys.sp_set_session_context @key=N'RHM.MaPhien',@value=NULL;
 IF @denied=0 THROW 55995,N'An AI-review menu grant bypassed the missing reception duty.',1;
 SET @action=@action+1;
END;
PRINT N'PASS: both staff AI procedures reject a current doctor session without business TiepNhan despite PhieuHuongDan=1.';
