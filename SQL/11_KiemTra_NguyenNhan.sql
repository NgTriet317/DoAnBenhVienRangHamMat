/* KIEM TRA KIOSK AI V2.1 - chi doc, khong sua du lieu */
USE [QLBenhVienRangHamMat];
GO
SELECT OBJECT_ID(N'dbo.AI_TaoPhieuTuBanKhai', N'P') AS MaProcedure,
       OBJECT_ID(N'dbo.AI_KET_QUA_TAO_PHIEU',N'U') AS BangKetQua;
SELECT TOP (30) p.MaPhien,p.TrangThai,n.DaTungKham,n.CanHoTroGap,
    p.MaPhieu, CASE WHEN k.MaPhien IS NULL THEN 0 ELSE 1 END DaTaoPhieu
FROM dbo.AI_PHIEN_TIEP_NHAN p
LEFT JOIN dbo.AI_TIEP_NHAN_NHAP n ON n.MaPhien=p.MaPhien
LEFT JOIN dbo.AI_KET_QUA_TAO_PHIEU k ON k.MaPhien=p.MaPhien
ORDER BY p.NgayTao DESC;
SELECT TOP (50) nv.MaNV,nv.TrangThai AS TrangThaiNhanVien,tk.TrangThai AS TrangThaiTaiKhoan
FROM dbo.NHAN_VIEN nv LEFT JOIN dbo.TAI_KHOAN tk ON tk.MaNV=nv.MaNV
ORDER BY nv.MaNV;
SELECT TOP (30) MaPhong,TenPhong,TrangThai FROM dbo.PHONG ORDER BY TenPhong;
-- Tra cuu ho so nghi trung bang SDT / ho ten / ngay sinh (thay du lieu gia):
DECLARE @SDT nvarchar(25) = N'0000000000', @HoTen nvarchar(255) = N'BENH NHAN THU', @NgaySinh date='1990-01-01';
SELECT MaBN,HoTenBN,NgaySinh,SDT FROM dbo.BENH_NHAN
WHERE REPLACE(ISNULL(SDT,N''),N' ',N'')=REPLACE(@SDT,N' ',N'')
   OR (HoTenBN=@HoTen AND NgaySinh=@NgaySinh);
GO
