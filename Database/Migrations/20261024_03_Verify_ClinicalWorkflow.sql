-- Chỉ đọc, kiểm chứng hợp đồng sau nâng cấp.
SET NOCOUNT ON;
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
IF EXISTS(SELECT 1 FROM @expected e LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+e.T) AND c.name=e.C LEFT JOIN sys.types t ON t.user_type_id=c.user_type_id WHERE c.column_id IS NULL OR (c.column_id IS NOT NULL AND (t.name<>e.TypeName OR c.max_length<>e.MaxLength OR c.is_nullable<>e.Nullable OR (e.TypeName=N'decimal' AND(c.precision<>e.PrecisionValue OR c.scale<>e.ScaleValue)) OR (e.TypeName=N'datetime2' AND c.scale<>e.ScaleValue)))) THROW 52101,N'Cấu trúc cột không khớp hợp đồng quy trình khám; không tự sửa kiểu dữ liệu.',1;
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'RHM.ClinicalWorkflow.Version' AND TRY_CONVERT(int,value)=2) THROW 52102,N'Chưa ghi phiên bản quy trình khám 2.',1;
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
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BAN_GIAO_THUOC') AND name=N'IX_CW_BanGiao_HD' AND is_disabled=0) THROW 52102,N'Thiếu index IX_CW_BanGiao_HD.',1;IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name LIKE N'FK[_]CW[_]%' AND(is_disabled=1 OR is_not_trusted=1)) THROW 52102,N'FK mới chưa được kiểm chứng.',1;
SELECT N'ClinicalWorkflow v2' AS SchemaVersion,COUNT(*) AS TableCount FROM sys.tables WHERE is_ms_shipped=0;
SELECT MaHD,MaLS,SoTienPhaiThu,DaThuRong,ConPhaiTra FROM dbo.VW_HOA_DON_THANH_TOAN WHERE ConPhaiTra<>0;
