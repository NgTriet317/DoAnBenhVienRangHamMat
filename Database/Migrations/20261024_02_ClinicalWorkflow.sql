-- Nâng cấp cộng thêm, chỉ chạy trên database đã xác nhận/backup. Không có USE hoặc xóa lịch sử.
SET NOCOUNT ON; SET XACT_ABORT ON; SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;
IF DB_NAME() IN(N'master',N'model',N'msdb',N'tempdb') THROW 52100,N'Chọn database bệnh viện trước nâng cấp.',1;
IF OBJECT_ID(N'dbo.LICH_SU_KHAM_BENH',N'U') IS NULL OR OBJECT_ID(N'dbo.NHAT_KY_THAO_TAC',N'U') IS NULL THROW 52100,N'Thiếu schema baseline.',1;
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @lock int; EXEC @lock=sys.sp_getapplock @Resource=N'RHM:ClinicalWorkflow:Schema',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 52100,N'Không lấy được khóa schema.',1;
DECLARE @version int=(SELECT TRY_CONVERT(int,value) FROM sys.extended_properties WHERE class=0 AND name=N'RHM.ClinicalWorkflow.Version');
IF @version IS NOT NULL AND @version<>2 THROW 52100,N'Phiên bản schema không được hỗ trợ.',1;
DECLARE @expected TABLE(T sysname,C sysname,TypeName sysname,MaxLength smallint,PrecisionValue tinyint,ScaleValue tinyint,Nullable bit);
INSERT @expected VALUES
(N'DM_TINH_TRANG_BENH',N'MaDMTT',N'nvarchar',100,0,0,0),
(N'DM_TINH_TRANG_BENH',N'TenTinhTrang',N'nvarchar',510,0,0,0),
(N'DM_TINH_TRANG_BENH',N'MoTa',N'nvarchar',-1,0,0,1),
(N'DM_TINH_TRANG_BENH',N'TrangThai',N'nvarchar',100,0,0,0),
(N'MAU_KE_HOACH_DIEU_TRI',N'MaMauKH',N'nvarchar',100,0,0,0),
(N'MAU_KE_HOACH_DIEU_TRI',N'MaDMTT',N'nvarchar',100,0,0,0),
(N'MAU_KE_HOACH_DIEU_TRI',N'MucDo',N'nvarchar',510,0,0,1),
(N'MAU_KE_HOACH_DIEU_TRI',N'TenMau',N'nvarchar',510,0,0,0),
(N'MAU_KE_HOACH_DIEU_TRI',N'PhienBan',N'int',4,0,0,0),
(N'MAU_KE_HOACH_DIEU_TRI',N'TrangThai',N'nvarchar',100,0,0,0),
(N'MAU_KE_HOACH_DIEU_TRI',N'MaBSDuyet',N'nvarchar',100,0,0,1),
(N'MAU_KE_HOACH_DIEU_TRI',N'NgayDuyet',N'datetime2',6,0,0,1),
(N'CHI_TIET_MAU_KE_HOACH',N'MaCTMau',N'nvarchar',100,0,0,0),
(N'CHI_TIET_MAU_KE_HOACH',N'MaMauKH',N'nvarchar',100,0,0,0),
(N'CHI_TIET_MAU_KE_HOACH',N'MaDV',N'nvarchar',100,0,0,0),
(N'CHI_TIET_MAU_KE_HOACH',N'ThuTu',N'int',4,0,0,0),
(N'CHI_TIET_MAU_KE_HOACH',N'LanThu',N'int',4,0,0,0),
(N'CHI_TIET_MAU_KE_HOACH',N'SoLuongDuKien',N'decimal',9,18,3,0),
(N'CHI_TIET_MAU_KE_HOACH',N'NoiDung',N'nvarchar',-1,0,0,0),
(N'NHOM_THUOC_KE_DON',N'MaNhomThuoc',N'nvarchar',100,0,0,0),
(N'NHOM_THUOC_KE_DON',N'TenNhom',N'nvarchar',510,0,0,0),
(N'NHOM_THUOC_KE_DON',N'MoTa',N'nvarchar',-1,0,0,1),
(N'NHOM_THUOC_KE_DON',N'TrangThai',N'nvarchar',100,0,0,0),
(N'CAU_HINH_THUOC_KE_DON',N'MaThuoc',N'nvarchar',100,0,0,0),
(N'CAU_HINH_THUOC_KE_DON',N'MaNhomThuoc',N'nvarchar',100,0,0,1),
(N'CAU_HINH_THUOC_KE_DON',N'DoiTuongApDung',N'nvarchar',510,0,0,1),
(N'CAU_HINH_THUOC_KE_DON',N'DuongDung',N'nvarchar',200,0,0,1),
(N'CAU_HINH_THUOC_KE_DON',N'LieuDungGoiY',N'nvarchar',510,0,0,1),
(N'CAU_HINH_THUOC_KE_DON',N'HuongDanGoiY',N'nvarchar',-1,0,0,1),
(N'CAU_HINH_THUOC_KE_DON',N'PhienBan',N'int',4,0,0,0),
(N'CAU_HINH_THUOC_KE_DON',N'TrangThai',N'nvarchar',100,0,0,0),
(N'CAU_HINH_THUOC_KE_DON',N'MaNguoiDuyet',N'nvarchar',100,0,0,1),
(N'CAU_HINH_THUOC_KE_DON',N'NgayDuyet',N'datetime2',6,0,0,1),
(N'BAN_GIAO_THUOC',N'MaDT',N'nvarchar',100,0,0,0),
(N'BAN_GIAO_THUOC',N'MaHD',N'nvarchar',100,0,0,0),
(N'BAN_GIAO_THUOC',N'MaPX',N'nvarchar',100,0,0,0),
(N'BAN_GIAO_THUOC',N'TrangThai',N'nvarchar',100,0,0,0),
(N'BAN_GIAO_THUOC',N'MaNVGiao',N'nvarchar',100,0,0,1),
(N'BAN_GIAO_THUOC',N'ThoiGianGiao',N'datetime2',6,0,0,1),
(N'BAN_GIAO_THUOC',N'GhiChu',N'nvarchar',-1,0,0,1),
(N'TINH_TRANG_BENH',N'MaDMTT',N'nvarchar',100,0,0,1),
(N'TINH_TRANG_BENH',N'ViTriRang',N'nvarchar',200,0,0,1),
(N'KE_HOACH_DIEU_TRI',N'MaTTBNguon',N'nvarchar',100,0,0,1),
(N'KE_HOACH_DIEU_TRI',N'MaMauKH',N'nvarchar',100,0,0,1),
(N'KE_HOACH_DIEU_TRI',N'PhienBanMau',N'int',4,0,0,1),
(N'CHI_TIET_KE_HOACH',N'NoiDungBuoc',N'nvarchar',-1,0,0,1),
(N'LICH_SU_KHAM_BENH',N'KhongKeThuoc',N'int',4,0,0,0),
(N'LICH_SU_KHAM_BENH',N'LyDoKhongKeThuoc',N'nvarchar',1000,0,0,1),
(N'DON_THUOC',N'MaDTThayThe',N'nvarchar',100,0,0,1),
(N'CHI_TIET_DON_THUOC',N'DuongDung',N'nvarchar',200,0,0,1),
(N'CHI_TIET_DON_THUOC',N'TenThuocChot',N'nvarchar',510,0,0,1),
(N'CHI_TIET_DON_THUOC',N'HamLuongChot',N'nvarchar',510,0,0,1),
(N'CHI_TIET_DON_THUOC',N'DonViTinhChot',N'nvarchar',510,0,0,1),
(N'CHI_TIET_DON_THUOC',N'PhienBanHuongDan',N'int',4,0,0,1),
(N'HOA_DON',N'PhienBanQuyTrinh',N'int',4,0,0,0),
(N'HOA_DON',N'MaYeuCau',N'nvarchar',100,0,0,1),
(N'HOA_DON',N'DauVanTayYeuCau',N'nvarchar',128,0,0,1),
(N'THANH_TOAN',N'SoTienKhachDua',N'decimal',9,18,2,1),
(N'THANH_TOAN',N'TienThua',N'decimal',9,18,2,1),
(N'THANH_TOAN',N'MaThamChieu',N'nvarchar',200,0,0,1);
IF EXISTS(SELECT 1 FROM @expected e LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+e.T) AND c.name=e.C LEFT JOIN sys.types t ON t.user_type_id=c.user_type_id WHERE (c.column_id IS NOT NULL AND (t.name<>e.TypeName OR c.max_length<>e.MaxLength OR c.is_nullable<>e.Nullable OR (e.TypeName=N'decimal' AND(c.precision<>e.PrecisionValue OR c.scale<>e.ScaleValue)) OR (e.TypeName=N'datetime2' AND c.scale<>e.ScaleValue)))) THROW 52101,N'Cấu trúc cột không khớp hợp đồng quy trình khám; không tự sửa kiểu dữ liệu.',1;
IF @version=2 BEGIN
IF EXISTS(SELECT 1 FROM @expected e LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+e.T) AND c.name=e.C LEFT JOIN sys.types t ON t.user_type_id=c.user_type_id WHERE c.column_id IS NULL OR (c.column_id IS NOT NULL AND (t.name<>e.TypeName OR c.max_length<>e.MaxLength OR c.is_nullable<>e.Nullable OR (e.TypeName=N'decimal' AND(c.precision<>e.PrecisionValue OR c.scale<>e.ScaleValue)) OR (e.TypeName=N'datetime2' AND c.scale<>e.ScaleValue)))) THROW 52101,N'Cấu trúc cột không khớp hợp đồng quy trình khám; không tự sửa kiểu dữ liệu.',1;
IF EXISTS(SELECT 1 FROM (VALUES (N'FK_CW_TTB_DMTT'),(N'FK_CW_KH_TTB'),(N'FK_CW_KH_Mau'),(N'FK_CW_DT_ThayThe'),(N'CK_CW_LS_NoRx'),(N'CK_CW_KH_Source'),(N'CK_CW_CTDT_Version'),(N'CK_CW_HD_Request'),(N'CK_CW_TT_Cash'),(N'PK_CW_DMTT'),(N'CK_CW_DMTT'),(N'PK_CW_MauKH'),(N'FK_CW_MauKH_DMTT'),(N'FK_CW_MauKH_BS'),(N'CK_CW_MauKH'),(N'PK_CW_CTMau'),(N'FK_CW_CTMau_MauKH'),(N'FK_CW_CTMau_DV'),(N'UQ_CW_CTMau_ThuTu'),(N'CK_CW_CTMau'),(N'PK_CW_NhomThuoc'),(N'CK_CW_NhomThuoc'),(N'PK_CW_CauHinhThuoc'),(N'FK_CW_CauHinh_Thuoc'),(N'FK_CW_CauHinh_Nhom'),(N'FK_CW_CauHinh_Duyet'),(N'CK_CW_CauHinh'),(N'PK_CW_BanGiao'),(N'FK_CW_BanGiao_DT'),(N'FK_CW_BanGiao_HD'),(N'FK_CW_BanGiao_PX'),(N'FK_CW_BanGiao_NV'),(N'CK_CW_BanGiao'),(N'TR_CW_LS_BaoVe'),(N'TR_CW_TTB_BaoVe'),(N'TR_CW_Phieu_BacSi'),(N'TR_CW_KH_BaoVe'),(N'TR_CW_CTKH_BaoVe'),(N'TR_CW_DT_ThayThe'),(N'TR_CW_MauKH_BaoVe'),(N'TR_CW_CTMau_BaoVe'),(N'TR_CW_CauHinh_BaoVe'),(N'TR_CW_HD_Request'),(N'TR_CW_TT_ThuDu'),(N'TR_CW_BanGiao_BaoVe'),(N'TR_CW_Audit_DM_TINH_TRANG_BENH'),(N'TR_CW_Audit_MAU_KE_HOACH_DIEU_TRI'),(N'TR_CW_Audit_CHI_TIET_MAU_KE_HOACH'),(N'TR_CW_Audit_NHOM_THUOC_KE_DON'),(N'TR_CW_Audit_CAU_HINH_THUOC_KE_DON')) expected(Name) WHERE OBJECT_ID(N'dbo.'+Name) IS NULL) THROW 52102,N'Thiếu FK/CHECK/trigger của migration.',1;
IF EXISTS(SELECT 1 FROM sys.triggers WHERE name LIKE N'TR[_]CW[_]%' AND is_disabled=1) OR EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name LIKE N'FK[_]CW[_]%' AND(is_disabled=1 OR is_not_trusted=1)) OR EXISTS(SELECT 1 FROM sys.check_constraints WHERE name LIKE N'CK[_]CW[_]%' AND(is_disabled=1 OR is_not_trusted=1)) THROW 52102,N'Trigger hoặc ràng buộc mới bị tắt/chưa kiểm chứng.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PHIEU_KHAM_BENH') AND name=N'IX_CW_Phieu_BacSi' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_Phieu_BacSi.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LICH_SU_KHAM_BENH') AND name=N'IX_CW_LS_BacSi' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_LS_BacSi.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HOA_DON') AND name=N'UX_CW_HD_YeuCau' AND is_disabled=0) THROW 52102,N'Thiếu index UX_CW_HD_YeuCau.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HOA_DON') AND name=N'UX_CW_HD_LuotV2' AND is_disabled=0) THROW 52102,N'Thiếu index UX_CW_HD_LuotV2.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.KE_HOACH_DIEU_TRI') AND name=N'UX_CW_KH_Source' AND is_disabled=0) THROW 52102,N'Thiếu index UX_CW_KH_Source.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TINH_TRANG_BENH') AND name=N'IX_CW_TTB_DMTT' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_TTB_DMTT.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.KE_HOACH_DIEU_TRI') AND name=N'IX_CW_KH_Mau' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_KH_Mau.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.DON_THUOC') AND name=N'IX_CW_DT_ThayThe' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_DT_ThayThe.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.MAU_KE_HOACH_DIEU_TRI') AND name=N'IX_CW_Mau_TinhTrang' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_Mau_TinhTrang.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CAU_HINH_THUOC_KE_DON') AND name=N'IX_CW_CauHinh_Nhom' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_CauHinh_Nhom.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BAN_GIAO_THUOC') AND name=N'IX_CW_BanGiao_HD' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_BanGiao_HD.',1;
COMMIT; PRINT N'ClinicalWorkflow v2 đã có; không thay đổi đối tượng.'; RETURN; END;
IF OBJECT_ID(N'dbo.DM_TINH_TRANG_BENH',N'U') IS NULL
EXEC sys.sp_executesql N'CREATE TABLE [dbo].[DM_TINH_TRANG_BENH] (
    [MaDMTT] NVARCHAR(50) NOT NULL CONSTRAINT [PK_CW_DMTT] PRIMARY KEY,
    [TenTinhTrang] NVARCHAR(255) NOT NULL,
    [MoTa] NVARCHAR(MAX) NULL,
    [TrangThai] NVARCHAR(50) NOT NULL,
    CONSTRAINT [CK_CW_DMTT] CHECK (NULLIF(LTRIM(RTRIM(TenTinhTrang)),N'''') IS NOT NULL AND TrangThai IN(N''Đang áp dụng'',N''Ngừng áp dụng''))
);';
IF OBJECT_ID(N'dbo.MAU_KE_HOACH_DIEU_TRI',N'U') IS NULL
EXEC sys.sp_executesql N'CREATE TABLE [dbo].[MAU_KE_HOACH_DIEU_TRI] (
    [MaMauKH] NVARCHAR(50) NOT NULL CONSTRAINT [PK_CW_MauKH] PRIMARY KEY,
    [MaDMTT] NVARCHAR(50) NOT NULL,
    [MucDo] NVARCHAR(255) NULL,
    [TenMau] NVARCHAR(255) NOT NULL,
    [PhienBan] INT NOT NULL,
    [TrangThai] NVARCHAR(50) NOT NULL,
    [MaBSDuyet] NVARCHAR(50) NULL,
    [NgayDuyet] DATETIME2(0) NULL,
    CONSTRAINT [FK_CW_MauKH_DMTT] FOREIGN KEY(MaDMTT) REFERENCES dbo.DM_TINH_TRANG_BENH(MaDMTT),
    CONSTRAINT [FK_CW_MauKH_BS] FOREIGN KEY(MaBSDuyet) REFERENCES dbo.NHAN_VIEN(MaNV),
    CONSTRAINT [CK_CW_MauKH] CHECK (PhienBan>0 AND NULLIF(LTRIM(RTRIM(TenMau)),N'''') IS NOT NULL AND TrangThai IN(N''Nháp'',N''Đã duyệt'',N''Ngừng áp dụng'') AND (TrangThai<>N''Đã duyệt'' OR (MaBSDuyet IS NOT NULL AND NgayDuyet IS NOT NULL)))
);';
IF OBJECT_ID(N'dbo.CHI_TIET_MAU_KE_HOACH',N'U') IS NULL
EXEC sys.sp_executesql N'CREATE TABLE [dbo].[CHI_TIET_MAU_KE_HOACH] (
    [MaCTMau] NVARCHAR(50) NOT NULL CONSTRAINT [PK_CW_CTMau] PRIMARY KEY,
    [MaMauKH] NVARCHAR(50) NOT NULL,
    [MaDV] NVARCHAR(50) NOT NULL,
    [ThuTu] INT NOT NULL,
    [LanThu] INT NOT NULL,
    [SoLuongDuKien] DECIMAL(18,3) NOT NULL,
    [NoiDung] NVARCHAR(MAX) NOT NULL,
    CONSTRAINT [FK_CW_CTMau_MauKH] FOREIGN KEY(MaMauKH) REFERENCES dbo.MAU_KE_HOACH_DIEU_TRI(MaMauKH),
    CONSTRAINT [FK_CW_CTMau_DV] FOREIGN KEY(MaDV) REFERENCES dbo.DICH_VU(MaDV),
    CONSTRAINT [UQ_CW_CTMau_ThuTu] UNIQUE(MaMauKH,ThuTu),
    CONSTRAINT [CK_CW_CTMau] CHECK (ThuTu>0 AND LanThu>0 AND SoLuongDuKien>0 AND NULLIF(LTRIM(RTRIM(NoiDung)),N'''') IS NOT NULL)
);';
IF OBJECT_ID(N'dbo.NHOM_THUOC_KE_DON',N'U') IS NULL
EXEC sys.sp_executesql N'CREATE TABLE [dbo].[NHOM_THUOC_KE_DON] (
    [MaNhomThuoc] NVARCHAR(50) NOT NULL CONSTRAINT [PK_CW_NhomThuoc] PRIMARY KEY,
    [TenNhom] NVARCHAR(255) NOT NULL,
    [MoTa] NVARCHAR(MAX) NULL,
    [TrangThai] NVARCHAR(50) NOT NULL,
    CONSTRAINT [CK_CW_NhomThuoc] CHECK (NULLIF(LTRIM(RTRIM(TenNhom)),N'''') IS NOT NULL AND TrangThai IN(N''Đang áp dụng'',N''Ngừng áp dụng''))
);';
IF OBJECT_ID(N'dbo.CAU_HINH_THUOC_KE_DON',N'U') IS NULL
EXEC sys.sp_executesql N'CREATE TABLE [dbo].[CAU_HINH_THUOC_KE_DON] (
    [MaThuoc] NVARCHAR(50) NOT NULL CONSTRAINT [PK_CW_CauHinhThuoc] PRIMARY KEY,
    [MaNhomThuoc] NVARCHAR(50) NULL,
    [DoiTuongApDung] NVARCHAR(255) NULL,
    [DuongDung] NVARCHAR(100) NULL,
    [LieuDungGoiY] NVARCHAR(255) NULL,
    [HuongDanGoiY] NVARCHAR(MAX) NULL,
    [PhienBan] INT NOT NULL,
    [TrangThai] NVARCHAR(50) NOT NULL,
    [MaNguoiDuyet] NVARCHAR(50) NULL,
    [NgayDuyet] DATETIME2(0) NULL,
    CONSTRAINT [FK_CW_CauHinh_Thuoc] FOREIGN KEY(MaThuoc) REFERENCES dbo.THUOC(MaThuoc),
    CONSTRAINT [FK_CW_CauHinh_Nhom] FOREIGN KEY(MaNhomThuoc) REFERENCES dbo.NHOM_THUOC_KE_DON(MaNhomThuoc),
    CONSTRAINT [FK_CW_CauHinh_Duyet] FOREIGN KEY(MaNguoiDuyet) REFERENCES dbo.NHAN_VIEN(MaNV),
    CONSTRAINT [CK_CW_CauHinh] CHECK (PhienBan>0 AND TrangThai IN(N''Nháp'',N''Đã duyệt'',N''Ngừng áp dụng'') AND (TrangThai<>N''Đã duyệt'' OR (MaNguoiDuyet IS NOT NULL AND NgayDuyet IS NOT NULL AND NULLIF(LTRIM(RTRIM(DoiTuongApDung)),N'''') IS NOT NULL AND NULLIF(LTRIM(RTRIM(DuongDung)),N'''') IS NOT NULL AND NULLIF(LTRIM(RTRIM(LieuDungGoiY)),N'''') IS NOT NULL AND NULLIF(LTRIM(RTRIM(HuongDanGoiY)),N'''') IS NOT NULL)))
);';
IF OBJECT_ID(N'dbo.BAN_GIAO_THUOC',N'U') IS NULL
EXEC sys.sp_executesql N'CREATE TABLE [dbo].[BAN_GIAO_THUOC] (
    [MaDT] NVARCHAR(50) NOT NULL CONSTRAINT [PK_CW_BanGiao] PRIMARY KEY,
    [MaHD] NVARCHAR(50) NOT NULL,
    [MaPX] NVARCHAR(50) NOT NULL,
    [TrangThai] NVARCHAR(50) NOT NULL,
    [MaNVGiao] NVARCHAR(50) NULL,
    [ThoiGianGiao] DATETIME2(0) NULL,
    [GhiChu] NVARCHAR(MAX) NULL,
    CONSTRAINT [FK_CW_BanGiao_DT] FOREIGN KEY(MaDT) REFERENCES dbo.DON_THUOC(MaDT),
    CONSTRAINT [FK_CW_BanGiao_HD] FOREIGN KEY(MaHD) REFERENCES dbo.HOA_DON(MaHD),
    CONSTRAINT [FK_CW_BanGiao_PX] FOREIGN KEY(MaPX) REFERENCES dbo.PHIEU_XUAT(MaPX),
    CONSTRAINT [FK_CW_BanGiao_NV] FOREIGN KEY(MaNVGiao) REFERENCES dbo.NHAN_VIEN(MaNV),
    CONSTRAINT [CK_CW_BanGiao] CHECK ((TrangThai=N''Chờ giao'' AND MaNVGiao IS NULL AND ThoiGianGiao IS NULL) OR (TrangThai=N''Đã giao'' AND MaNVGiao IS NOT NULL AND ThoiGianGiao IS NOT NULL))
);';
IF COL_LENGTH(N'dbo.TINH_TRANG_BENH',N'MaDMTT') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[TINH_TRANG_BENH] ADD [MaDMTT] NVARCHAR(50) NULL;';
IF COL_LENGTH(N'dbo.TINH_TRANG_BENH',N'ViTriRang') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[TINH_TRANG_BENH] ADD [ViTriRang] NVARCHAR(100) NULL;';
IF COL_LENGTH(N'dbo.KE_HOACH_DIEU_TRI',N'MaTTBNguon') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[KE_HOACH_DIEU_TRI] ADD [MaTTBNguon] NVARCHAR(50) NULL;';
IF COL_LENGTH(N'dbo.KE_HOACH_DIEU_TRI',N'MaMauKH') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[KE_HOACH_DIEU_TRI] ADD [MaMauKH] NVARCHAR(50) NULL;';
IF COL_LENGTH(N'dbo.KE_HOACH_DIEU_TRI',N'PhienBanMau') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[KE_HOACH_DIEU_TRI] ADD [PhienBanMau] INT NULL;';
IF COL_LENGTH(N'dbo.CHI_TIET_KE_HOACH',N'NoiDungBuoc') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[CHI_TIET_KE_HOACH] ADD [NoiDungBuoc] NVARCHAR(MAX) NULL;';
IF COL_LENGTH(N'dbo.LICH_SU_KHAM_BENH',N'KhongKeThuoc') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[LICH_SU_KHAM_BENH] ADD [KhongKeThuoc] INT NOT NULL CONSTRAINT [DF_CW_LS_KhongKeThuoc] DEFAULT (0);';
IF COL_LENGTH(N'dbo.LICH_SU_KHAM_BENH',N'LyDoKhongKeThuoc') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[LICH_SU_KHAM_BENH] ADD [LyDoKhongKeThuoc] NVARCHAR(500) NULL;';
IF COL_LENGTH(N'dbo.DON_THUOC',N'MaDTThayThe') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[DON_THUOC] ADD [MaDTThayThe] NVARCHAR(50) NULL;';
IF COL_LENGTH(N'dbo.CHI_TIET_DON_THUOC',N'DuongDung') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[CHI_TIET_DON_THUOC] ADD [DuongDung] NVARCHAR(100) NULL;';
IF COL_LENGTH(N'dbo.CHI_TIET_DON_THUOC',N'TenThuocChot') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[CHI_TIET_DON_THUOC] ADD [TenThuocChot] NVARCHAR(255) NULL;';
IF COL_LENGTH(N'dbo.CHI_TIET_DON_THUOC',N'HamLuongChot') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[CHI_TIET_DON_THUOC] ADD [HamLuongChot] NVARCHAR(255) NULL;';
IF COL_LENGTH(N'dbo.CHI_TIET_DON_THUOC',N'DonViTinhChot') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[CHI_TIET_DON_THUOC] ADD [DonViTinhChot] NVARCHAR(255) NULL;';
IF COL_LENGTH(N'dbo.CHI_TIET_DON_THUOC',N'PhienBanHuongDan') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[CHI_TIET_DON_THUOC] ADD [PhienBanHuongDan] INT NULL;';
IF COL_LENGTH(N'dbo.HOA_DON',N'PhienBanQuyTrinh') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[HOA_DON] ADD [PhienBanQuyTrinh] INT NOT NULL CONSTRAINT [DF_CW_HD_PhienBan] DEFAULT (1);';
IF COL_LENGTH(N'dbo.HOA_DON',N'MaYeuCau') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[HOA_DON] ADD [MaYeuCau] NVARCHAR(50) NULL;';
IF COL_LENGTH(N'dbo.HOA_DON',N'DauVanTayYeuCau') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[HOA_DON] ADD [DauVanTayYeuCau] NVARCHAR(64) NULL;';
IF COL_LENGTH(N'dbo.THANH_TOAN',N'SoTienKhachDua') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[THANH_TOAN] ADD [SoTienKhachDua] DECIMAL(18,2) NULL;';
IF COL_LENGTH(N'dbo.THANH_TOAN',N'TienThua') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[THANH_TOAN] ADD [TienThua] DECIMAL(18,2) NULL;';
IF COL_LENGTH(N'dbo.THANH_TOAN',N'MaThamChieu') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[THANH_TOAN] ADD [MaThamChieu] NVARCHAR(100) NULL;';
IF OBJECT_ID(N'dbo.FK_CW_TTB_DMTT') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[TINH_TRANG_BENH] WITH CHECK ADD CONSTRAINT [FK_CW_TTB_DMTT] FOREIGN KEY(MaDMTT) REFERENCES dbo.DM_TINH_TRANG_BENH(MaDMTT);';
IF OBJECT_ID(N'dbo.FK_CW_KH_TTB') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[KE_HOACH_DIEU_TRI] WITH CHECK ADD CONSTRAINT [FK_CW_KH_TTB] FOREIGN KEY(MaTTBNguon) REFERENCES dbo.TINH_TRANG_BENH(MaTTB);';
IF OBJECT_ID(N'dbo.FK_CW_KH_Mau') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[KE_HOACH_DIEU_TRI] WITH CHECK ADD CONSTRAINT [FK_CW_KH_Mau] FOREIGN KEY(MaMauKH) REFERENCES dbo.MAU_KE_HOACH_DIEU_TRI(MaMauKH);';
IF OBJECT_ID(N'dbo.FK_CW_DT_ThayThe') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[DON_THUOC] WITH CHECK ADD CONSTRAINT [FK_CW_DT_ThayThe] FOREIGN KEY(MaDTThayThe) REFERENCES dbo.DON_THUOC(MaDT);';
IF OBJECT_ID(N'dbo.CK_CW_LS_NoRx') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[LICH_SU_KHAM_BENH] WITH CHECK ADD CONSTRAINT [CK_CW_LS_NoRx] CHECK(KhongKeThuoc IN(0,1) AND (KhongKeThuoc=0 OR NULLIF(LTRIM(RTRIM(LyDoKhongKeThuoc)),N'''') IS NOT NULL));';
IF OBJECT_ID(N'dbo.CK_CW_KH_Source') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[KE_HOACH_DIEU_TRI] WITH CHECK ADD CONSTRAINT [CK_CW_KH_Source] CHECK((MaMauKH IS NULL AND PhienBanMau IS NULL) OR(MaMauKH IS NOT NULL AND MaTTBNguon IS NOT NULL AND PhienBanMau IS NOT NULL AND PhienBanMau>0));';
IF OBJECT_ID(N'dbo.CK_CW_CTDT_Version') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[CHI_TIET_DON_THUOC] WITH CHECK ADD CONSTRAINT [CK_CW_CTDT_Version] CHECK(PhienBanHuongDan IS NULL OR PhienBanHuongDan>0);';
IF OBJECT_ID(N'dbo.CK_CW_HD_Request') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[HOA_DON] WITH CHECK ADD CONSTRAINT [CK_CW_HD_Request] CHECK((PhienBanQuyTrinh=1 AND MaYeuCau IS NULL AND DauVanTayYeuCau IS NULL) OR (PhienBanQuyTrinh=2 AND MaYeuCau IS NOT NULL AND DATALENGTH(MaYeuCau)=72 AND TRY_CONVERT(uniqueidentifier,MaYeuCau) IS NOT NULL AND LOWER(MaYeuCau) COLLATE Latin1_General_100_BIN2=LOWER(CONVERT(nvarchar(36),TRY_CONVERT(uniqueidentifier,MaYeuCau))) COLLATE Latin1_General_100_BIN2 AND DauVanTayYeuCau IS NOT NULL AND DATALENGTH(DauVanTayYeuCau)=128 AND DauVanTayYeuCau COLLATE Latin1_General_100_BIN2 NOT LIKE N''%[^0-9A-Fa-f]%''));';
IF OBJECT_ID(N'dbo.CK_CW_TT_Cash') IS NULL
EXEC sys.sp_executesql N'ALTER TABLE dbo.[THANH_TOAN] WITH CHECK ADD CONSTRAINT [CK_CW_TT_Cash] CHECK((SoTienKhachDua IS NULL AND TienThua IS NULL) OR(LoaiGiaoDich=N''Thu'' AND SoTienKhachDua IS NOT NULL AND TienThua IS NOT NULL AND ((PhuongThuc=N''Tiền mặt'' AND SoTienKhachDua>=SoTien AND TienThua=SoTienKhachDua-SoTien) OR(PhuongThuc=N''Chuyển khoản'' AND SoTienKhachDua=SoTien AND TienThua=0 AND NULLIF(LTRIM(RTRIM(MaThamChieu)),N'''') IS NOT NULL))));';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PHIEU_KHAM_BENH') AND name=N'IX_CW_Phieu_BacSi')
EXEC sys.sp_executesql N'CREATE INDEX [IX_CW_Phieu_BacSi] ON dbo.[PHIEU_KHAM_BENH](MaBS,TrangThai,ThoiGianTiepNhan);';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LICH_SU_KHAM_BENH') AND name=N'IX_CW_LS_BacSi')
EXEC sys.sp_executesql N'CREATE INDEX [IX_CW_LS_BacSi] ON dbo.[LICH_SU_KHAM_BENH](MaBS,ThoiGianKham);';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HOA_DON') AND name=N'UX_CW_HD_YeuCau')
EXEC sys.sp_executesql N'CREATE UNIQUE INDEX [UX_CW_HD_YeuCau] ON dbo.[HOA_DON](MaYeuCau) WHERE MaYeuCau IS NOT NULL;';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HOA_DON') AND name=N'UX_CW_HD_LuotV2')
EXEC sys.sp_executesql N'CREATE UNIQUE INDEX [UX_CW_HD_LuotV2] ON dbo.[HOA_DON](MaLS) WHERE PhienBanQuyTrinh=2;';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.KE_HOACH_DIEU_TRI') AND name=N'UX_CW_KH_Source')
EXEC sys.sp_executesql N'CREATE UNIQUE INDEX [UX_CW_KH_Source] ON dbo.[KE_HOACH_DIEU_TRI](MaLS,MaTTBNguon,MaMauKH) WHERE MaTTBNguon IS NOT NULL AND MaMauKH IS NOT NULL;';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TINH_TRANG_BENH') AND name=N'IX_CW_TTB_DMTT')
EXEC sys.sp_executesql N'CREATE INDEX [IX_CW_TTB_DMTT] ON dbo.[TINH_TRANG_BENH](MaDMTT);';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.KE_HOACH_DIEU_TRI') AND name=N'IX_CW_KH_Mau')
EXEC sys.sp_executesql N'CREATE INDEX [IX_CW_KH_Mau] ON dbo.[KE_HOACH_DIEU_TRI](MaMauKH);';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.DON_THUOC') AND name=N'IX_CW_DT_ThayThe')
EXEC sys.sp_executesql N'CREATE INDEX [IX_CW_DT_ThayThe] ON dbo.[DON_THUOC](MaDTThayThe);';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.MAU_KE_HOACH_DIEU_TRI') AND name=N'IX_CW_Mau_TinhTrang')
EXEC sys.sp_executesql N'CREATE INDEX [IX_CW_Mau_TinhTrang] ON dbo.[MAU_KE_HOACH_DIEU_TRI](MaDMTT,TrangThai,MucDo);';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CAU_HINH_THUOC_KE_DON') AND name=N'IX_CW_CauHinh_Nhom')
EXEC sys.sp_executesql N'CREATE INDEX [IX_CW_CauHinh_Nhom] ON dbo.[CAU_HINH_THUOC_KE_DON](MaNhomThuoc,TrangThai);';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BAN_GIAO_THUOC') AND name=N'IX_CW_BanGiao_HD')
EXEC sys.sp_executesql N'CREATE INDEX [IX_CW_BanGiao_HD] ON dbo.[BAN_GIAO_THUOC](MaHD,TrangThai);';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.TR_CW_LS_BaoVe ON dbo.LICH_SU_KHAM_BENH AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.MaLS=d.MaLS WHERE d.TrangThai=N''Đã xác nhận'' AND
 (i.MaLS IS NULL OR EXISTS(SELECT i.TrieuChung,i.ChanDoan,i.KeHoachXuLy,i.GhiChu,i.KhongKeThuoc,i.LyDoKhongKeThuoc
 EXCEPT SELECT d.TrieuChung,d.ChanDoan,d.KeHoachXuLy,d.GhiChu,d.KhongKeThuoc,d.LyDoKhongKeThuoc)))
  THROW 52110,N''Nội dung lần khám đã ký được giữ nguyên; ghi bổ sung bằng nhật ký.'',1;
 IF EXISTS(SELECT 1 FROM inserted i WHERE i.KhongKeThuoc=1 AND EXISTS(SELECT 1 FROM dbo.DON_THUOC d WHERE d.MaLS=i.MaLS AND d.TrangThai IN(N''Nháp'',N''Đã xác nhận'')))
  THROW 52110,N''Đã chọn không kê thuốc thì không có đơn nháp hoặc đơn hiệu lực.'',1;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.TR_CW_TTB_BaoVe ON dbo.TINH_TRANG_BENH AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM dbo.LICH_SU_KHAM_BENH l WITH(UPDLOCK,HOLDLOCK) JOIN(SELECT MaLS FROM inserted UNION SELECT MaLS FROM deleted) x ON x.MaLS=l.MaLS WHERE l.TrangThai=N''Đã xác nhận'')
  THROW 52111,N''Không sửa hoặc bổ sung tình trạng vào lượt đã ký.'',1;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.TR_CW_Phieu_BacSi ON dbo.PHIEU_KHAM_BENH AFTER UPDATE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.MaPhieu=i.MaPhieu WHERE EXISTS(SELECT 1 FROM dbo.LICH_SU_KHAM_BENH l WHERE l.MaPhieu=i.MaPhieu)
 AND EXISTS(SELECT i.MaBS,i.MaPhong EXCEPT SELECT d.MaBS,d.MaPhong))
  THROW 52112,N''Phiếu đã bắt đầu khám không thay bác sĩ hoặc phòng.'',1;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.TR_CW_KH_BaoVe ON dbo.KE_HOACH_DIEU_TRI AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.MaKHDT=d.MaKHDT WHERE d.TrangThai<>N''Nháp'' AND
 (i.MaKHDT IS NULL OR i.TrangThai=N''Nháp'' OR EXISTS(SELECT i.MaLS,i.MaBS,i.NgayLap,i.NgayBatDau,i.NgayDuKienKetThuc,i.GhiChu,i.MaTTBNguon,i.MaMauKH,i.PhienBanMau
 EXCEPT SELECT d.MaLS,d.MaBS,d.NgayLap,d.NgayBatDau,d.NgayDuKienKetThuc,d.GhiChu,d.MaTTBNguon,d.MaMauKH,d.PhienBanMau)))
  THROW 52113,N''Kế hoạch đã duyệt chỉ cập nhật trạng thái tiến độ.'',1;
 IF EXISTS(SELECT 1 FROM inserted i LEFT JOIN dbo.TINH_TRANG_BENH t ON t.MaTTB=i.MaTTBNguon
 LEFT JOIN dbo.MAU_KE_HOACH_DIEU_TRI m ON m.MaMauKH=i.MaMauKH WHERE
 (i.MaTTBNguon IS NOT NULL AND(t.MaTTB IS NULL OR t.MaLS<>i.MaLS)) OR
 (i.MaMauKH IS NOT NULL AND(m.MaDMTT<>t.MaDMTT OR t.MaDMTT IS NULL OR m.PhienBan<>i.PhienBanMau)))
  THROW 52113,N''Nguồn tình trạng và mẫu phải khớp lượt, danh mục và phiên bản.'',1;
 IF EXISTS(SELECT 1 FROM inserted i LEFT JOIN deleted d ON d.MaKHDT=i.MaKHDT JOIN dbo.MAU_KE_HOACH_DIEU_TRI m ON m.MaMauKH=i.MaMauKH
 WHERE (d.MaKHDT IS NULL OR EXISTS(SELECT i.MaMauKH,i.PhienBanMau EXCEPT SELECT d.MaMauKH,d.PhienBanMau)) AND m.TrangThai<>N''Đã duyệt'')
  THROW 52113,N''Chỉ sao chép mẫu đã duyệt.'',1;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.TR_CW_CTKH_BaoVe ON dbo.CHI_TIET_KE_HOACH AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM dbo.KE_HOACH_DIEU_TRI k WITH(UPDLOCK,HOLDLOCK) JOIN(SELECT MaKHDT FROM inserted UNION SELECT MaKHDT FROM deleted) x ON x.MaKHDT=k.MaKHDT
 WHERE k.TrangThai<>N''Nháp'' AND EXISTS(SELECT 1 FROM inserted i FULL JOIN deleted d ON d.MaCTKH=i.MaCTKH
 WHERE (i.MaKHDT=k.MaKHDT OR d.MaKHDT=k.MaKHDT) AND(i.MaCTKH IS NULL OR d.MaCTKH IS NULL OR EXISTS(
 SELECT i.MaKHDT,i.MaDV,i.LanThu,i.SoLuongDuKien,i.DonGiaDuKien,i.GhiChu,i.NoiDungBuoc
 EXCEPT SELECT d.MaKHDT,d.MaDV,d.LanThu,d.SoLuongDuKien,d.DonGiaDuKien,d.GhiChu,d.NoiDungBuoc))))
  THROW 52114,N''Không thay nội dung/định lượng bước thuộc kế hoạch đã duyệt.'',1;
 IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.MaCTKH=d.MaCTKH WHERE EXISTS(SELECT 1 FROM dbo.DICH_VU_THUC_HIEN s WHERE s.MaCTKH=d.MaCTKH AND s.TrangThai=N''Đã xác nhận'')
 AND(i.MaCTKH IS NULL OR EXISTS(SELECT i.MaKHDT,i.MaDV,i.SoLuongDuKien,i.DonGiaDuKien,i.NoiDungBuoc EXCEPT SELECT d.MaKHDT,d.MaDV,d.SoLuongDuKien,d.DonGiaDuKien,d.NoiDungBuoc)))
  THROW 52114,N''Bước đã thực hiện không sửa nguồn.'',1;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.TR_CW_DT_ThayThe ON dbo.DON_THUOC AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted d JOIN inserted i ON i.MaDT=d.MaDT WHERE d.TrangThai<>N''Nháp'' AND EXISTS(SELECT i.MaDTThayThe EXCEPT SELECT d.MaDTThayThe))
  THROW 52116,N''Đơn đã ký giữ nguyên nguồn thay thế.'',1;
 IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaLS=i.MaLS WHERE l.KhongKeThuoc=1 AND i.TrangThai IN(N''Nháp'',N''Đã xác nhận''))
  THROW 52116,N''Lượt đã chọn không kê thuốc.'',1;
 IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.DON_THUOC old ON old.MaDT=i.MaDTThayThe WHERE i.MaDT=old.MaDT OR i.MaLS<>old.MaLS OR i.MaBS<>old.MaBS)
  THROW 52116,N''Đơn thay thế phải cùng lượt và bác sĩ, không tự trỏ chính nó.'',1;
 IF EXISTS(SELECT 1 FROM inserted i LEFT JOIN deleted d ON d.MaDT=i.MaDT WHERE i.MaDTThayThe IS NOT NULL AND (d.MaDT IS NULL OR d.TrangThai=N''Nháp'') AND
 (EXISTS(SELECT 1 FROM dbo.HOA_DON h WHERE h.MaLS=i.MaLS) OR EXISTS(SELECT 1 FROM dbo.PHIEU_XUAT p WHERE p.MaLS=i.MaLS AND p.TrangThai=N''Đã ghi sổ'')))
  THROW 52116,N''Không thay đơn của lượt đã lập hóa đơn hoặc đã xuất thuốc.'',1;
 IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.MaDT=i.MaDT WHERE i.TrangThai=N''Ngừng áp dụng'' AND d.TrangThai<>N''Ngừng áp dụng'' AND
 EXISTS(SELECT 1 FROM dbo.CHI_TIET_DON_THUOC c JOIN dbo.CHI_TIET_PHIEU_XUAT x ON x.MaCTDT=c.MaCTDT JOIN dbo.PHIEU_XUAT p ON p.MaPX=x.MaPX WHERE c.MaDT=i.MaDT AND p.TrangThai=N''Đã ghi sổ''))
  THROW 52116,N''Đơn đã xuất thuốc giữ nguyên hiệu lực lịch sử.'',1;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.TR_CW_MauKH_BaoVe ON dbo.MAU_KE_HOACH_DIEU_TRI AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.MaMauKH=d.MaMauKH WHERE d.TrangThai<>N''Nháp'' AND
 (i.MaMauKH IS NULL OR i.TrangThai=N''Nháp'' OR EXISTS(SELECT i.MaDMTT,i.MucDo,i.TenMau,i.PhienBan,i.MaBSDuyet,i.NgayDuyet EXCEPT SELECT d.MaDMTT,d.MucDo,d.TenMau,d.PhienBan,d.MaBSDuyet,d.NgayDuyet)))
  THROW 52115,N''Mẫu đã duyệt giữ nguyên nội dung; tạo mã/phiên bản mới.'',1;
 IF EXISTS(SELECT 1 FROM inserted i WHERE i.TrangThai=N''Đã duyệt'' AND
 (NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_MAU_KE_HOACH c WHERE c.MaMauKH=i.MaMauKH) OR
 NOT EXISTS(SELECT 1 FROM dbo.NHAN_VIEN n JOIN dbo.TAI_KHOAN t ON t.MaNV=n.MaNV WHERE n.MaNV=i.MaBSDuyet AND n.MaChucVu=N''CV002'' AND n.TrangThai=N''Đang làm việc'' AND t.TrangThai=N''Đang hoạt động'')))
  THROW 52115,N''Mẫu cần các bước và bác sĩ hoạt động duyệt.'',1;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.TR_CW_CTMau_BaoVe ON dbo.CHI_TIET_MAU_KE_HOACH AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM dbo.MAU_KE_HOACH_DIEU_TRI m WITH(UPDLOCK,HOLDLOCK) JOIN(SELECT MaMauKH FROM inserted UNION SELECT MaMauKH FROM deleted) x ON x.MaMauKH=m.MaMauKH WHERE m.TrangThai<>N''Nháp'')
  THROW 52115,N''Chỉ sửa bước của mẫu nháp.'',1;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.TR_CW_CauHinh_BaoVe ON dbo.CAU_HINH_THUOC_KE_DON AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.MaThuoc=d.MaThuoc WHERE d.TrangThai<>N''Nháp'' AND
 (i.MaThuoc IS NULL OR NOT((i.PhienBan=d.PhienBan AND i.TrangThai<>N''Nháp'' AND NOT EXISTS(
 SELECT i.MaNhomThuoc,i.DoiTuongApDung,i.DuongDung,i.LieuDungGoiY,i.HuongDanGoiY,i.MaNguoiDuyet,i.NgayDuyet
 EXCEPT SELECT d.MaNhomThuoc,d.DoiTuongApDung,d.DuongDung,d.LieuDungGoiY,d.HuongDanGoiY,d.MaNguoiDuyet,d.NgayDuyet))
 OR(i.PhienBan=d.PhienBan+1 AND i.TrangThai=N''Nháp'' AND i.MaNguoiDuyet IS NULL AND i.NgayDuyet IS NULL))))
  THROW 52117,N''Đổi hướng dẫn đã duyệt phải lập phiên bản nháp tiếp theo và giữ lịch sử.'',1;
 IF EXISTS(SELECT 1 FROM inserted i WHERE i.TrangThai=N''Đã duyệt'' AND NOT EXISTS(
 SELECT 1 FROM dbo.NHAN_VIEN n JOIN dbo.TAI_KHOAN t ON t.MaNV=n.MaNV WHERE n.MaNV=i.MaNguoiDuyet AND n.MaChucVu IN(N''CV002'',N''CV005'') AND n.TrangThai=N''Đang làm việc'' AND t.TrangThai=N''Đang hoạt động''))
  THROW 52117,N''Người duyệt hướng dẫn cần quyền chuyên môn đang hoạt động.'',1;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.TR_CW_HD_Request ON dbo.HOA_DON AFTER UPDATE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.MaHD=i.MaHD WHERE d.TrangThai=N''Đã xác nhận'' AND EXISTS(
 SELECT i.PhienBanQuyTrinh,i.MaYeuCau,i.DauVanTayYeuCau EXCEPT SELECT d.PhienBanQuyTrinh,d.MaYeuCau,d.DauVanTayYeuCau))
  THROW 52118,N''Hóa đơn đã xác nhận giữ nguyên khóa yêu cầu/phiên bản.'',1;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.TR_CW_TT_ThuDu ON dbo.THANH_TOAN AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @id nvarchar(50); SELECT @id=h.MaHD FROM dbo.HOA_DON h WITH(UPDLOCK,HOLDLOCK) JOIN(SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted) x ON x.MaHD=h.MaHD;
 IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.HOA_DON h ON h.MaHD=i.MaHD WHERE h.PhienBanQuyTrinh=2 AND
 (i.TrangThai<>N''Đã xác nhận'' OR i.LoaiGiaoDich<>N''Thu'' OR i.SoTien<>h.SoTienPhaiThu OR i.SoTienKhachDua IS NULL OR i.TienThua IS NULL))
  THROW 52119,N''Quy trình 2 chỉ ghi một giao dịch Thu đủ có tiền đưa và tiền thối.'',1;
 IF EXISTS(SELECT 1 FROM dbo.HOA_DON h JOIN(SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted) x ON x.MaHD=h.MaHD WHERE h.PhienBanQuyTrinh=2 AND
 (SELECT COUNT(*) FROM dbo.THANH_TOAN t WHERE t.MaHD=h.MaHD AND t.TrangThai=N''Đã xác nhận'' AND t.LoaiGiaoDich=N''Thu'')<>1)
  THROW 52119,N''Hóa đơn quy trình 2 có đúng một lần Thu đủ.'',1;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.TR_CW_BanGiao_BaoVe ON dbo.BAN_GIAO_THUOC AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.MaDT=d.MaDT WHERE i.MaDT IS NULL OR d.TrangThai=N''Đã giao'' OR EXISTS(
 SELECT i.MaHD,i.MaPX EXCEPT SELECT d.MaHD,d.MaPX)) THROW 52121,N''Không xóa/đổi nguồn bàn giao hoặc sửa bản đã giao.'',1;
 IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.DON_THUOC d ON d.MaDT=i.MaDT JOIN dbo.HOA_DON h ON h.MaHD=i.MaHD JOIN dbo.PHIEU_XUAT p ON p.MaPX=i.MaPX
 JOIN dbo.VW_HOA_DON_THANH_TOAN v ON v.MaHD=h.MaHD WHERE h.PhienBanQuyTrinh<>2 OR h.TrangThai<>N''Đã xác nhận'' OR v.ConPhaiTra<>0 OR d.MaLS<>h.MaLS OR p.MaLS<>h.MaLS OR p.TrangThai<>N''Đã ghi sổ'' OR
 NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_PHIEU_XUAT x JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaCTDT=x.MaCTDT WHERE x.MaPX=i.MaPX AND c.MaDT=i.MaDT) OR
 EXISTS(SELECT 1 FROM dbo.CHI_TIET_PHIEU_XUAT x LEFT JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaCTDT=x.MaCTDT WHERE x.MaPX=i.MaPX AND(c.MaDT IS NULL OR c.MaDT<>i.MaDT)))
 THROW 52121,N''Bàn giao cần hóa đơn đã thu đủ, xuất ghi sổ và đơn cùng lượt.'',1;
 IF EXISTS(SELECT 1 FROM inserted i WHERE i.TrangThai=N''Đã giao'' AND NOT EXISTS(SELECT 1 FROM dbo.NHAN_VIEN n JOIN dbo.TAI_KHOAN t ON t.MaNV=n.MaNV WHERE n.MaNV=i.MaNVGiao AND n.MaChucVu=N''CV005'' AND n.TrangThai=N''Đang làm việc'' AND t.TrangThai=N''Đang hoạt động''))
 THROW 52121,N''Người giao phải là dược sĩ đang hoạt động.'',1;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.[TR_CW_Audit_DM_TINH_TRANG_BENH] ON dbo.[DM_TINH_TRANG_BENH] AFTER INSERT,UPDATE,DELETE AS
BEGIN
SET NOCOUNT ON;
DECLARE @actor nvarchar(50)=TRY_CONVERT(nvarchar(50),SESSION_CONTEXT(N''RHM.MaTK''));
IF NOT EXISTS(SELECT 1 FROM dbo.TAI_KHOAN t JOIN dbo.NHAN_VIEN n ON n.MaNV=t.MaNV WHERE t.MaTK=@actor AND t.TrangThai=N''Đang hoạt động'' AND n.TrangThai=N''Đang làm việc'')
  THROW 52120,N''Cấu hình chuyên môn cần người sửa đã xác thực để lưu nhật ký.'',1;
INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuTruoc,DuLieuSau)
SELECT N''NK''+CONVERT(nvarchar(36),NEWID()),@actor,SYSDATETIME(),N''Cập nhật cấu hình hỗ trợ'',N''DM_TINH_TRANG_BENH'',COALESCE(i.[MaDMTT],d.[MaDMTT]),N''Thành công'',
  CASE WHEN d.[MaDMTT] IS NOT NULL THEN (SELECT d.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END,
  CASE WHEN i.[MaDMTT] IS NOT NULL THEN (SELECT i.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END
FROM inserted i FULL JOIN deleted d ON d.[MaDMTT]=i.[MaDMTT];
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.[TR_CW_Audit_MAU_KE_HOACH_DIEU_TRI] ON dbo.[MAU_KE_HOACH_DIEU_TRI] AFTER INSERT,UPDATE,DELETE AS
BEGIN
SET NOCOUNT ON;
DECLARE @actor nvarchar(50)=TRY_CONVERT(nvarchar(50),SESSION_CONTEXT(N''RHM.MaTK''));
IF NOT EXISTS(SELECT 1 FROM dbo.TAI_KHOAN t JOIN dbo.NHAN_VIEN n ON n.MaNV=t.MaNV WHERE t.MaTK=@actor AND t.TrangThai=N''Đang hoạt động'' AND n.TrangThai=N''Đang làm việc'')
  THROW 52120,N''Cấu hình chuyên môn cần người sửa đã xác thực để lưu nhật ký.'',1;
INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuTruoc,DuLieuSau)
SELECT N''NK''+CONVERT(nvarchar(36),NEWID()),@actor,SYSDATETIME(),N''Cập nhật cấu hình hỗ trợ'',N''MAU_KE_HOACH_DIEU_TRI'',COALESCE(i.[MaMauKH],d.[MaMauKH]),N''Thành công'',
  CASE WHEN d.[MaMauKH] IS NOT NULL THEN (SELECT d.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END,
  CASE WHEN i.[MaMauKH] IS NOT NULL THEN (SELECT i.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END
FROM inserted i FULL JOIN deleted d ON d.[MaMauKH]=i.[MaMauKH];
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.[TR_CW_Audit_CHI_TIET_MAU_KE_HOACH] ON dbo.[CHI_TIET_MAU_KE_HOACH] AFTER INSERT,UPDATE,DELETE AS
BEGIN
SET NOCOUNT ON;
DECLARE @actor nvarchar(50)=TRY_CONVERT(nvarchar(50),SESSION_CONTEXT(N''RHM.MaTK''));
IF NOT EXISTS(SELECT 1 FROM dbo.TAI_KHOAN t JOIN dbo.NHAN_VIEN n ON n.MaNV=t.MaNV WHERE t.MaTK=@actor AND t.TrangThai=N''Đang hoạt động'' AND n.TrangThai=N''Đang làm việc'')
  THROW 52120,N''Cấu hình chuyên môn cần người sửa đã xác thực để lưu nhật ký.'',1;
INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuTruoc,DuLieuSau)
SELECT N''NK''+CONVERT(nvarchar(36),NEWID()),@actor,SYSDATETIME(),N''Cập nhật cấu hình hỗ trợ'',N''CHI_TIET_MAU_KE_HOACH'',COALESCE(i.[MaCTMau],d.[MaCTMau]),N''Thành công'',
  CASE WHEN d.[MaCTMau] IS NOT NULL THEN (SELECT d.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END,
  CASE WHEN i.[MaCTMau] IS NOT NULL THEN (SELECT i.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END
FROM inserted i FULL JOIN deleted d ON d.[MaCTMau]=i.[MaCTMau];
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.[TR_CW_Audit_NHOM_THUOC_KE_DON] ON dbo.[NHOM_THUOC_KE_DON] AFTER INSERT,UPDATE,DELETE AS
BEGIN
SET NOCOUNT ON;
DECLARE @actor nvarchar(50)=TRY_CONVERT(nvarchar(50),SESSION_CONTEXT(N''RHM.MaTK''));
IF NOT EXISTS(SELECT 1 FROM dbo.TAI_KHOAN t JOIN dbo.NHAN_VIEN n ON n.MaNV=t.MaNV WHERE t.MaTK=@actor AND t.TrangThai=N''Đang hoạt động'' AND n.TrangThai=N''Đang làm việc'')
  THROW 52120,N''Cấu hình chuyên môn cần người sửa đã xác thực để lưu nhật ký.'',1;
INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuTruoc,DuLieuSau)
SELECT N''NK''+CONVERT(nvarchar(36),NEWID()),@actor,SYSDATETIME(),N''Cập nhật cấu hình hỗ trợ'',N''NHOM_THUOC_KE_DON'',COALESCE(i.[MaNhomThuoc],d.[MaNhomThuoc]),N''Thành công'',
  CASE WHEN d.[MaNhomThuoc] IS NOT NULL THEN (SELECT d.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END,
  CASE WHEN i.[MaNhomThuoc] IS NOT NULL THEN (SELECT i.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END
FROM inserted i FULL JOIN deleted d ON d.[MaNhomThuoc]=i.[MaNhomThuoc];
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.[TR_CW_Audit_CAU_HINH_THUOC_KE_DON] ON dbo.[CAU_HINH_THUOC_KE_DON] AFTER INSERT,UPDATE,DELETE AS
BEGIN
SET NOCOUNT ON;
DECLARE @actor nvarchar(50)=TRY_CONVERT(nvarchar(50),SESSION_CONTEXT(N''RHM.MaTK''));
IF NOT EXISTS(SELECT 1 FROM dbo.TAI_KHOAN t JOIN dbo.NHAN_VIEN n ON n.MaNV=t.MaNV WHERE t.MaTK=@actor AND t.TrangThai=N''Đang hoạt động'' AND n.TrangThai=N''Đang làm việc'')
  THROW 52120,N''Cấu hình chuyên môn cần người sửa đã xác thực để lưu nhật ký.'',1;
INSERT dbo.NHAT_KY_THAO_TAC(MaNK,MaTK,ThoiGian,HanhDong,LoaiDoiTuong,MaDoiTuong,KetQua,DuLieuTruoc,DuLieuSau)
SELECT N''NK''+CONVERT(nvarchar(36),NEWID()),@actor,SYSDATETIME(),N''Cập nhật cấu hình hỗ trợ'',N''CAU_HINH_THUOC_KE_DON'',COALESCE(i.[MaThuoc],d.[MaThuoc]),N''Thành công'',
  CASE WHEN d.[MaThuoc] IS NOT NULL THEN (SELECT d.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END,
  CASE WHEN i.[MaThuoc] IS NOT NULL THEN (SELECT i.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) END
FROM inserted i FULL JOIN deleted d ON d.[MaThuoc]=i.[MaThuoc];
END;';
IF EXISTS(SELECT 1 FROM @expected e LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+e.T) AND c.name=e.C LEFT JOIN sys.types t ON t.user_type_id=c.user_type_id WHERE c.column_id IS NULL OR (c.column_id IS NOT NULL AND (t.name<>e.TypeName OR c.max_length<>e.MaxLength OR c.is_nullable<>e.Nullable OR (e.TypeName=N'decimal' AND(c.precision<>e.PrecisionValue OR c.scale<>e.ScaleValue)) OR (e.TypeName=N'datetime2' AND c.scale<>e.ScaleValue)))) THROW 52101,N'Cấu trúc cột không khớp hợp đồng quy trình khám; không tự sửa kiểu dữ liệu.',1;
IF EXISTS(SELECT 1 FROM (VALUES (N'FK_CW_TTB_DMTT'),(N'FK_CW_KH_TTB'),(N'FK_CW_KH_Mau'),(N'FK_CW_DT_ThayThe'),(N'CK_CW_LS_NoRx'),(N'CK_CW_KH_Source'),(N'CK_CW_CTDT_Version'),(N'CK_CW_HD_Request'),(N'CK_CW_TT_Cash'),(N'PK_CW_DMTT'),(N'CK_CW_DMTT'),(N'PK_CW_MauKH'),(N'FK_CW_MauKH_DMTT'),(N'FK_CW_MauKH_BS'),(N'CK_CW_MauKH'),(N'PK_CW_CTMau'),(N'FK_CW_CTMau_MauKH'),(N'FK_CW_CTMau_DV'),(N'UQ_CW_CTMau_ThuTu'),(N'CK_CW_CTMau'),(N'PK_CW_NhomThuoc'),(N'CK_CW_NhomThuoc'),(N'PK_CW_CauHinhThuoc'),(N'FK_CW_CauHinh_Thuoc'),(N'FK_CW_CauHinh_Nhom'),(N'FK_CW_CauHinh_Duyet'),(N'CK_CW_CauHinh'),(N'PK_CW_BanGiao'),(N'FK_CW_BanGiao_DT'),(N'FK_CW_BanGiao_HD'),(N'FK_CW_BanGiao_PX'),(N'FK_CW_BanGiao_NV'),(N'CK_CW_BanGiao'),(N'TR_CW_LS_BaoVe'),(N'TR_CW_TTB_BaoVe'),(N'TR_CW_Phieu_BacSi'),(N'TR_CW_KH_BaoVe'),(N'TR_CW_CTKH_BaoVe'),(N'TR_CW_DT_ThayThe'),(N'TR_CW_MauKH_BaoVe'),(N'TR_CW_CTMau_BaoVe'),(N'TR_CW_CauHinh_BaoVe'),(N'TR_CW_HD_Request'),(N'TR_CW_TT_ThuDu'),(N'TR_CW_BanGiao_BaoVe'),(N'TR_CW_Audit_DM_TINH_TRANG_BENH'),(N'TR_CW_Audit_MAU_KE_HOACH_DIEU_TRI'),(N'TR_CW_Audit_CHI_TIET_MAU_KE_HOACH'),(N'TR_CW_Audit_NHOM_THUOC_KE_DON'),(N'TR_CW_Audit_CAU_HINH_THUOC_KE_DON')) expected(Name) WHERE OBJECT_ID(N'dbo.'+Name) IS NULL) THROW 52102,N'Thiếu FK/CHECK/trigger của migration.',1;
IF EXISTS(SELECT 1 FROM sys.triggers WHERE name LIKE N'TR[_]CW[_]%' AND is_disabled=1) OR EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name LIKE N'FK[_]CW[_]%' AND(is_disabled=1 OR is_not_trusted=1)) OR EXISTS(SELECT 1 FROM sys.check_constraints WHERE name LIKE N'CK[_]CW[_]%' AND(is_disabled=1 OR is_not_trusted=1)) THROW 52102,N'Trigger hoặc ràng buộc mới bị tắt/chưa kiểm chứng.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PHIEU_KHAM_BENH') AND name=N'IX_CW_Phieu_BacSi' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_Phieu_BacSi.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LICH_SU_KHAM_BENH') AND name=N'IX_CW_LS_BacSi' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_LS_BacSi.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HOA_DON') AND name=N'UX_CW_HD_YeuCau' AND is_disabled=0) THROW 52102,N'Thiếu index UX_CW_HD_YeuCau.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HOA_DON') AND name=N'UX_CW_HD_LuotV2' AND is_disabled=0) THROW 52102,N'Thiếu index UX_CW_HD_LuotV2.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.KE_HOACH_DIEU_TRI') AND name=N'UX_CW_KH_Source' AND is_disabled=0) THROW 52102,N'Thiếu index UX_CW_KH_Source.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TINH_TRANG_BENH') AND name=N'IX_CW_TTB_DMTT' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_TTB_DMTT.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.KE_HOACH_DIEU_TRI') AND name=N'IX_CW_KH_Mau' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_KH_Mau.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.DON_THUOC') AND name=N'IX_CW_DT_ThayThe' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_DT_ThayThe.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.MAU_KE_HOACH_DIEU_TRI') AND name=N'IX_CW_Mau_TinhTrang' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_Mau_TinhTrang.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CAU_HINH_THUOC_KE_DON') AND name=N'IX_CW_CauHinh_Nhom' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_CauHinh_Nhom.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BAN_GIAO_THUOC') AND name=N'IX_CW_BanGiao_HD' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_BanGiao_HD.',1;EXEC sys.sp_addextendedproperty @name=N'RHM.ClinicalWorkflow.Version',@value=2;
COMMIT;
PRINT N'ClinicalWorkflow v2: nâng cấp thành công; lịch sử được giữ nguyên.';
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK; THROW;
END CATCH;
