-- Chay tren DUNG database ma WPF va API ket noi.
USE [QLBenhVienRangHamMat];
GO
SELECT DB_NAME() AS DatabaseDangKiemTra,
       OBJECT_ID(N'dbo.AI_PHIEN_TIEP_NHAN',N'U') AS BangPhienAI,
       OBJECT_ID(N'dbo.AI_TIEP_NHAN_NHAP',N'U') AS BangBanKhai;
GO
IF OBJECT_ID(N'dbo.AI_PHIEN_TIEP_NHAN',N'U') IS NOT NULL
BEGIN
  SELECT TrangThai,COUNT(*) AS SoPhien FROM dbo.AI_PHIEN_TIEP_NHAN GROUP BY TrangThai ORDER BY TrangThai;
  SELECT TOP(15) p.MaPhien, p.TrangThai, p.NgayTao, n.HoTen, n.SoDienThoai, n.LyDoKham
  FROM dbo.AI_PHIEN_TIEP_NHAN p
  LEFT JOIN dbo.AI_TIEP_NHAN_NHAP n ON n.MaPhien=p.MaPhien
  ORDER BY p.NgayTao DESC;
END
GO
