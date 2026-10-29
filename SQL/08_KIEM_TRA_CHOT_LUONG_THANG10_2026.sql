/*
 KIỂM TRA CHỈ ĐỌC – KHÔNG UPDATE/INSERT/DELETE.
 Mở SSMS, CHỌN ĐÚNG DATABASE ứng dụng đang kết nối, rồi F5.
 Thay @MaNV nếu muốn kiểm tra nhân viên khác. Tháng 10/2026 mẫu trong ảnh là NV005.
 LƯU Ý: SQL Server chốt theo SYSDATETIME() của chính SQL Server,
 không theo đồng hồ trên máy chạy WPF khi dùng SQL server khác máy.
*/
DECLARE @TuNgay DATE = '20261001';
DECLARE @DenNgay DATE = EOMONTH(@TuNgay);
DECLARE @MaNV NVARCHAR(50) = N'NV005';

SELECT DB_NAME() AS DatabaseDangChay, @@SERVERNAME AS SqlServer,
       SYSDATETIME() AS ThoiGianSqlServer,
       DATEADD(DAY, 5, @DenNgay) AS NgayChoPhepChot,
       CASE WHEN CAST(SYSDATETIME() AS DATE) >= DATEADD(DAY,5,@DenNgay)
            THEN N'Đã đến ngày được chốt' ELSE N'Chưa đến ngày được chốt trên SQL Server' END AS KetQuaNgay,
       OBJECT_ID('dbo.RHM_TinhLuongKy','P') AS ThuTucTinhLuong,
       OBJECT_ID('dbo.RHM_ChotLuongNhanVien','P') AS ThuTucChotLuong;

-- Có đúng 1 phiếu cho NV005 và có phải phiếu nguyên tháng hay không?
SELECT b.MaBL,b.MaNV,b.TuNgay,b.DenNgay,b.TrangThai,
       b.CongThucTe,b.CongChuan,b.TongLuong,b.TienHoaHong,b.NgayChot,b.MaCSL,
       nv.MaChucVu, nv.TrangThai AS TrangThaiNhanVien,
       cs.TrangThai AS TrangThaiChinhSach,cs.NgayHieuLuc,cs.NgayHetHieuLuc,
       CASE WHEN b.TuNgay=@TuNgay AND b.DenNgay=@DenNgay
          THEN N'Đúng kỳ tháng 10' ELSE N'Phiếu lệch kỳ (cần kiểm tra dữ liệu cũ)' END AS LoaiKy
FROM dbo.BANG_LUONG b
JOIN dbo.NHAN_VIEN nv ON nv.MaNV=b.MaNV
LEFT JOIN dbo.CHINH_SACH_LUONG cs ON cs.MaCSL=b.MaCSL
WHERE b.MaNV=@MaNV AND b.TuNgay<=@DenNgay AND b.DenNgay>=@TuNgay
ORDER BY b.TuNgay,b.MaBL;

-- Chi tiết ca chưa đối soát của nhân viên đang hỏi, theo cùng logic chặn chốt trong DAL/SQL.
SELECT ll.MaLLV,ll.MaNV,ll.NgayLam,ll.MaCa,ll.TrangThai AS TrangThaiLich,
       cc.MaCC,cc.TrangThai AS TrangThaiChamCong,cc.CongDuocTinh,cc.NgayDoiSoat
FROM dbo.LICH_LAM_VIEC ll
LEFT JOIN dbo.CHAM_CONG cc ON ll.MaLLV=cc.MaLLV
WHERE ll.MaNV=@MaNV AND ll.NgayLam BETWEEN @TuNgay AND @DenNgay
  AND ll.TrangThai NOT IN(N'Hủy',N'Từ chối',N'Nghỉ phép',N'Nghỉ việc')
  AND NOT EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI d
     WHERE d.MaNV=ll.MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
       AND ll.NgayLam BETWEEN d.TuNgay AND d.DenNgay)
  AND (cc.MaCC IS NULL OR cc.TrangThai<>N'Đã xác nhận')
ORDER BY ll.NgayLam,ll.MaCa;

-- Đối chiếu chênh lệch: số 10 hiển thị ở ô thống kê là của CẢ THÁNG, không riêng NV005.
SELECT ll.MaNV,COUNT(*) AS CaChuaDoiSoat
FROM dbo.LICH_LAM_VIEC ll
LEFT JOIN dbo.CHAM_CONG cc ON cc.MaLLV=ll.MaLLV
WHERE ll.NgayLam BETWEEN @TuNgay AND @DenNgay
  AND ll.TrangThai NOT IN(N'Hủy',N'Từ chối',N'Nghỉ phép',N'Nghỉ việc')
  AND NOT EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI d
     WHERE d.MaNV=ll.MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
       AND ll.NgayLam BETWEEN d.TuNgay AND d.DenNgay)
  AND (cc.MaCC IS NULL OR cc.TrangThai<>N'Đã xác nhận')
GROUP BY ll.MaNV ORDER BY ll.MaNV;

-- Những trở ngại khác ngay cả khi tất cả ca của NV005 đã duyệt.
SELECT
 (SELECT COUNT(*) FROM dbo.DON_XIN_NGHI d WHERE d.MaNV=@MaNV
    AND d.TrangThaiDuyet=N'Chờ duyệt' AND d.TuNgay<=@DenNgay AND d.DenNgay>=@TuNgay)
    AS DonNghiChuaDuyet,
 (SELECT COUNT(*) FROM dbo.DON_XIN_NGHI d
    JOIN dbo.LICH_LAM_VIEC ll ON ll.MaNV=d.MaNV AND ll.NgayLam BETWEEN d.TuNgay AND d.DenNgay
    JOIN dbo.CHAM_CONG cc ON cc.MaLLV=ll.MaLLV
    WHERE d.MaNV=@MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
      AND ll.NgayLam BETWEEN @TuNgay AND @DenNgay
      AND cc.TrangThai=N'Đã xác nhận' AND cc.CongDuocTinh>0) AS CongTrungNgayPhep,
 (SELECT COUNT(*) FROM dbo.BANG_LUONG b
    JOIN dbo.CHI_TIET_LUONG ct ON ct.MaBL=b.MaBL
    WHERE b.MaNV=@MaNV AND b.TuNgay=@TuNgay AND b.DenNgay=@DenNgay
      AND ct.TinhChat=N'Cộng' AND ct.LoaiKhoan NOT IN(N'Phụ cấp',N'Làm thêm giờ')) AS KhoanCongChuaPhanLoai,
 (SELECT COUNT(*) FROM dbo.BANG_LUONG b
    JOIN dbo.CHI_TIET_LUONG ct ON ct.MaBL=b.MaBL
    WHERE b.MaNV=@MaNV AND b.TuNgay=@TuNgay AND b.DenNgay=@DenNgay
      AND ct.MaNVDuyet IS NULL) AS ChungTuChuaDuyet,
 (SELECT COUNT(*) FROM dbo.BANG_LUONG b
    JOIN dbo.CHI_TIET_HOA_HONG_LUONG hh ON hh.MaBL=b.MaBL
    JOIN dbo.CHI_TIET_HOA_DON ct ON ct.MaCTHD=hh.MaCTHD
    JOIN dbo.HOA_DON hd ON hd.MaHD=ct.MaHD
    LEFT JOIN dbo.DICH_VU_THUC_HIEN dv ON dv.MaDVThucHien=ct.MaDVThucHien
    WHERE b.MaNV=@MaNV AND b.TuNgay=@TuNgay AND b.DenNgay=@DenNgay
     AND (hd.TrangThai<>N'Đã xác nhận' OR dv.MaDVThucHien IS NULL
      OR dv.TrangThai<>N'Đã xác nhận'
      OR COALESCE((SELECT SUM(CASE WHEN tt.LoaiGiaoDich=N'Thu' THEN tt.SoTien ELSE -tt.SoTien END)
           FROM dbo.THANH_TOAN tt WHERE tt.MaHD=hd.MaHD AND tt.TrangThai=N'Đã xác nhận'),0)<>hd.SoTienPhaiThu)
  ) AS HoaHongChuaHopLe;
