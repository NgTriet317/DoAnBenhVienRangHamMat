/* Kiem tra sau khi F5 migration 08 - khong thay doi du lieu */
USE [QLBenhVienRangHamMat];
GO
SELECT OBJECT_ID(N'dbo.AI_TaoPhieuTuBanKhai',N'P') AS ThuTucTaoPhieu,
       OBJECT_ID(N'dbo.AI_KET_QUA_TAO_PHIEU',N'U') AS BangKetQua;
SELECT p.TrangThai,COUNT(*) AS SoPhien
FROM dbo.AI_PHIEN_TIEP_NHAN p GROUP BY p.TrangThai ORDER BY p.TrangThai;
SELECT TOP (30) p.MaPhien,p.TrangThai,n.HoTen,n.SoDienThoai,
       p.MaPhieu,k.MaBN,k.MaHS,k.LoaiKham,k.PhuThuDuKien,
       pk.SoThuTu,pk.MaPhong,pk.MaNVTiepNhan
FROM dbo.AI_PHIEN_TIEP_NHAN p
LEFT JOIN dbo.AI_TIEP_NHAN_NHAP n ON n.MaPhien=p.MaPhien
LEFT JOIN dbo.AI_KET_QUA_TAO_PHIEU k ON k.MaPhien=p.MaPhien
LEFT JOIN dbo.PHIEU_KHAM_BENH pk ON pk.MaPhieu=p.MaPhieu
ORDER BY p.NgayTao DESC;
GO
