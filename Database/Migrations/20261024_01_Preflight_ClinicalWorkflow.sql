-- Chỉ đọc. Chạy bằng sqlcmd -d <database đã xác nhận> -b -i <file này>.
-- Không chứa USE, không sửa dữ liệu, không xuất thông tin bệnh án hoặc tài khoản.
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() IN (N'master',N'model',N'msdb',N'tempdb')
    THROW 51000, N'Phải chọn database bệnh viện cần tiền kiểm.', 1;

DECLARE @Required TABLE (Name sysname NOT NULL);
INSERT @Required VALUES
 (N'BENH_NHAN'),(N'HO_SO_BENH_AN'),(N'PHIEU_KHAM_BENH'),(N'LICH_SU_KHAM_BENH'),
 (N'TINH_TRANG_BENH'),(N'KE_HOACH_DIEU_TRI'),(N'CHI_TIET_KE_HOACH'),
 (N'DICH_VU_THUC_HIEN'),(N'DON_THUOC'),(N'CHI_TIET_DON_THUOC'),
 (N'HOA_DON'),(N'CHI_TIET_HOA_DON'),(N'THANH_TOAN'),(N'PHIEU_XUAT'),
 (N'CHI_TIET_PHIEU_XUAT'),(N'TAI_KHOAN'),(N'NHAN_VIEN'),(N'CHUC_VU'),(N'NHAT_KY_THAO_TAC');
SELECT Name AS MissingTable FROM @Required WHERE OBJECT_ID(N'dbo.'+Name,N'U') IS NULL;
IF EXISTS (SELECT 1 FROM @Required WHERE OBJECT_ID(N'dbo.'+Name,N'U') IS NULL)
    THROW 51001, N'Thiếu bảng baseline; dừng để đối chiếu schema.', 1;

SELECT DB_NAME() AS DatabaseName, COUNT(*) AS TableCount FROM sys.tables;
SELECT cv.MaChucVu,cv.TenChucVu,COUNT(nv.MaNV) AS EmployeeCount
FROM dbo.CHUC_VU cv LEFT JOIN dbo.NHAN_VIEN nv ON nv.MaChucVu=cv.MaChucVu
GROUP BY cv.MaChucVu,cv.TenChucVu ORDER BY cv.MaChucVu;

SELECT Name AS MissingObject FROM (VALUES
 (N'TR_PHIEU_LienKet'),(N'TR_LS_LienKet'),(N'TR_DT_XacNhan'),(N'TR_CTDT_KhoaDon'),
 (N'TR_CTHD_TinhTien'),(N'TR_HD_XacNhan'),(N'TR_TT_KiemTraSoDu'),
 (N'VW_TON_KHO'),(N'VW_HOA_DON_THANH_TOAN'),(N'VW_CHI_TIET_HOA_DON'),
 (N'VW_TIEN_THUOC_THEO_DON'),(N'VW_LICH_SU_KHAM_BENH_NHAN')) expected(Name)
WHERE OBJECT_ID(N'dbo.'+Name) IS NULL;
SELECT name AS DisabledForeignKey FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1;
SELECT name AS DisabledTrigger FROM sys.triggers WHERE is_disabled=1 AND parent_class=1;

-- Các mã cần đối chiếu: không tự xóa hoặc chuẩn hóa lịch sử.
SELECT MaBN,COUNT(*) AS ProfileCount FROM dbo.HO_SO_BENH_AN GROUP BY MaBN HAVING COUNT(*)>1;
SELECT MaPhieu,COUNT(*) AS VisitCount FROM dbo.LICH_SU_KHAM_BENH GROUP BY MaPhieu HAVING COUNT(*)>1;
SELECT ls.MaLS,ls.MaPhieu FROM dbo.LICH_SU_KHAM_BENH ls
LEFT JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=ls.MaPhieu
WHERE p.MaPhieu IS NULL OR p.MaBS IS NULL OR ls.MaBS<>p.MaBS OR ls.MaPhong<>p.MaPhong;
SELECT p.MaPhieu FROM dbo.PHIEU_KHAM_BENH p
LEFT JOIN dbo.HO_SO_BENH_AN hs ON hs.MaHS=p.MaHS WHERE hs.MaHS IS NULL;
SELECT MaLich,COUNT(*) AS ActiveReceiptCount FROM dbo.PHIEU_KHAM_BENH
WHERE MaLich IS NOT NULL AND TrangThai<>N'Hủy' GROUP BY MaLich HAVING COUNT(*)>1;
SELECT MaDVThucHien,COUNT(*) AS InvoiceSourceCount FROM dbo.CHI_TIET_HOA_DON
WHERE MaDVThucHien IS NOT NULL GROUP BY MaDVThucHien HAVING COUNT(*)>1;
SELECT MaCTPX,COUNT(*) AS InvoiceSourceCount FROM dbo.CHI_TIET_HOA_DON
WHERE MaCTPX IS NOT NULL GROUP BY MaCTPX HAVING COUNT(*)>1;

SELECT MaHD,MaLS,TrangThai,TongTien,SoTienPhaiThu,DaThuRong,ConPhaiTra
FROM dbo.VW_HOA_DON_THANH_TOAN WHERE TrangThai=N'Nháp' OR ConPhaiTra<>0 ORDER BY MaHD;
SELECT DISTINCT px.MaPX,dt.MaDT,px.MaLS,px.TrangThai
FROM dbo.PHIEU_XUAT px JOIN dbo.CHI_TIET_PHIEU_XUAT ct ON ct.MaPX=px.MaPX
JOIN dbo.CHI_TIET_DON_THUOC dt ON dt.MaCTDT=ct.MaCTDT ORDER BY px.MaPX,dt.MaDT;

SELECT COUNT(*) AS InvoiceCount,COALESCE(SUM(TongTien),0) AS InvoiceTotal,
 COALESCE(SUM(SoTienPhaiThu),0) AS DueTotal FROM dbo.HOA_DON;
SELECT LoaiGiaoDich,TrangThai,COUNT(*) AS PaymentCount,SUM(SoTien) AS Amount
FROM dbo.THANH_TOAN GROUP BY LoaiGiaoDich,TrangThai;
SELECT COUNT(*) AS StockPositionCount,COALESCE(SUM(SoLuongTon),0) AS StockQuantity FROM dbo.VW_TON_KHO;
