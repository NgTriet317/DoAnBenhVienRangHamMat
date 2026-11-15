USE [QLBenhVienRangHamMat];
GO
SELECT OBJECT_ID(N'dbo.AI_TaoPhieuTuBanKhai',N'P') AS CoThuTuc,
       CASE WHEN EXISTS(SELECT 1 FROM sys.sql_modules
           WHERE object_id=OBJECT_ID(N'dbo.AI_TaoPhieuTuBanKhai',N'P')
             AND definition LIKE N'%TuDongTaoMoi_KhaiTungKham%') THEN 1 ELSE 0 END AS DaCapNhatV22;
GO
-- Xem thử danh sách bệnh nhân để test trùng; không tự thay đổi dữ liệu.
SELECT TOP (20) MaBN,HoTenBN,NgaySinh,SDT FROM dbo.BENH_NHAN ORDER BY MaBN;
GO
