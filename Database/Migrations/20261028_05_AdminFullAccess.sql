-- Bổ sung toàn quyền nghiệp vụ cho Admin trên schema ClinicalWorkflow v2.
-- Chạy sau 01/02/03 trên database đã sao lưu; không thay chức vụ hoặc dữ liệu chứng từ.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'RHM.ClinicalWorkflow.Version' AND TRY_CONVERT(int,value)=2)
    THROW 52130,N'Cần nâng cấp ClinicalWorkflow v2 trước khi áp dụng quyền Admin.',1;
BEGIN TRANSACTION;
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.TR_CW_MauKH_BaoVe ON dbo.MAU_KE_HOACH_DIEU_TRI AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.MaMauKH=d.MaMauKH WHERE d.TrangThai<>N''Nháp'' AND
 (i.MaMauKH IS NULL OR i.TrangThai=N''Nháp'' OR EXISTS(SELECT i.MaDMTT,i.MucDo,i.TenMau,i.PhienBan,i.MaBSDuyet,i.NgayDuyet EXCEPT SELECT d.MaDMTT,d.MucDo,d.TenMau,d.PhienBan,d.MaBSDuyet,d.NgayDuyet)))
  THROW 52115,N''Mẫu đã duyệt giữ nguyên nội dung; tạo mã/phiên bản mới.'',1;
 IF EXISTS(SELECT 1 FROM inserted i WHERE i.TrangThai=N''Đã duyệt'' AND
 (NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_MAU_KE_HOACH c WHERE c.MaMauKH=i.MaMauKH) OR
 NOT EXISTS(SELECT 1 FROM dbo.NHAN_VIEN n JOIN dbo.TAI_KHOAN t ON t.MaNV=n.MaNV WHERE n.MaNV=i.MaBSDuyet AND (n.MaChucVu=N''CV002'' OR EXISTS(SELECT 1 FROM dbo.VAI_TRO v WHERE v.MaVaiTro=t.MaVaiTro AND UPPER(LTRIM(RTRIM(v.TenVaiTro))) IN(N''ADMIN'',N''QUẢN TRỊ''))) AND n.TrangThai=N''Đang làm việc'' AND t.TrangThai=N''Đang hoạt động'')))
  THROW 52115,N''Mẫu cần các bước và bác sĩ hoặc Admin đang hoạt động duyệt.'',1;
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
 SELECT 1 FROM dbo.NHAN_VIEN n JOIN dbo.TAI_KHOAN t ON t.MaNV=n.MaNV WHERE n.MaNV=i.MaNguoiDuyet AND (n.MaChucVu IN(N''CV002'',N''CV005'') OR EXISTS(SELECT 1 FROM dbo.VAI_TRO v WHERE v.MaVaiTro=t.MaVaiTro AND UPPER(LTRIM(RTRIM(v.TenVaiTro))) IN(N''ADMIN'',N''QUẢN TRỊ''))) AND n.TrangThai=N''Đang làm việc'' AND t.TrangThai=N''Đang hoạt động''))
  THROW 52117,N''Người duyệt hướng dẫn cần quyền chuyên môn đang hoạt động.'',1;
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
 IF EXISTS(SELECT 1 FROM inserted i WHERE i.TrangThai=N''Đã giao'' AND NOT EXISTS(SELECT 1 FROM dbo.NHAN_VIEN n JOIN dbo.TAI_KHOAN t ON t.MaNV=n.MaNV WHERE n.MaNV=i.MaNVGiao AND (n.MaChucVu=N''CV005'' OR EXISTS(SELECT 1 FROM dbo.VAI_TRO v WHERE v.MaVaiTro=t.MaVaiTro AND UPPER(LTRIM(RTRIM(v.TenVaiTro))) IN(N''ADMIN'',N''QUẢN TRỊ''))) AND n.TrangThai=N''Đang làm việc'' AND t.TrangThai=N''Đang hoạt động''))
 THROW 52121,N''Người giao phải là dược sĩ hoặc Admin đang hoạt động.'',1;
END;';

COMMIT;
PRINT N'Đã đồng bộ quyền duyệt/giao thuốc cho Admin; dữ liệu được giữ nguyên.';
