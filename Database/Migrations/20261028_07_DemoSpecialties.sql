/* DỮ LIỆU DEMO HƯ CẤU. Chạy sau 20261028_06; không phải hướng dẫn chuyên môn.
   Không USE/CREATE DATABASE: người chạy phải chọn đúng database demo đã sao lưu.
   Không cập nhật/xóa dữ liệu đã có. Mã DEMO28_* dành riêng cho bộ dữ liệu này.
   Chạy lại chỉ thêm lịch còn thiếu trong cửa sổ mới, giữ trạng thái đã xử lý.
   SQL Server 2016+. Chạy toàn bộ batch; lỗi bất kỳ rollback toàn bộ lần chạy. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF COL_LENGTH(N'dbo.TINH_TRANG_BENH',N'MucDo') IS NOT NULL
    THROW 52140,N'Chạy migration 20261028_06 trước dữ liệu demo chuyên khoa.',1;
IF OBJECT_ID(N'dbo.TINH_TRANG_BENH',N'U') IS NULL
    THROW 52140,N'Thiếu schema ClinicalWorkflow; không thể nạp dữ liệu demo.',1;

DECLARE @Now datetime2(0)=SYSDATETIME();
DECLARE @Today date=CONVERT(date,@Now);
DECLARE @Marker nvarchar(200)=N'DEMO28: dữ liệu hư cấu để thử giao diện, không dùng điều trị.';
DECLARE @Lock int, @NewDoctors int=0,@NewRooms int=0,@NewServices int=0,@NewSchedules int=0,
    @NewPatients int=0,@NewReceipts int=0,@NewDrafts int=0,@NewConditions int=0;
BEGIN TRY
 BEGIN TRANSACTION;
 EXEC @Lock=sys.sp_getapplock @Resource=N'RHM:DemoSpecialties:20261028',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
 IF @Lock<0 THROW 52140,N'Bộ dữ liệu demo đang được nạp bởi phiên khác.',1;
 -- Cùng cổng ghi với DAL_TIEPNHAN: giữ số thứ tự theo ngày/phòng không đụng quầy tiếp nhận.
 EXEC @Lock=sys.sp_getapplock @Resource=N'RHM:TiepNhan:Write',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
 IF @Lock<0 THROW 52140,N'Quầy tiếp nhận đang bận; thử nạp demo lại sau.',1;

 DECLARE @CK TABLE(MaCK nvarchar(50) PRIMARY KEY,MaKhoa nvarchar(50),Ten nvarchar(255),MaDV nvarchar(50),TenDV nvarchar(255),TinhTrang nvarchar(255),MoTa nvarchar(1000),ViTri nvarchar(100));
 INSERT @CK VALUES
 (N'CK001',N'KHOA001',N'Tổng quát',N'DV001',N'Khám răng hàm mặt tổng quát',N'Khó chịu vùng răng miệng',N'Bệnh nhân hư cấu mô tả khó chịu khi nhai, cần ghi nhận khi khám tổng quát.',N'16'),
 (N'CK002',N'KHOA002',N'Bảo tồn',N'DEMO28_DV_CK002',N'Khám tư vấn điều trị bảo tồn (demo)',N'Ê buốt răng',N'Bệnh nhân hư cấu mô tả ê buốt răng khi ăn uống; chưa kết luận chẩn đoán.',N'36'),
 (N'CK003',N'KHOA003',N'Hàm mặt',N'DEMO28_DV_CK003',N'Khám tư vấn phẫu thuật hàm mặt (demo)',N'Khó chịu vùng hàm',N'Bệnh nhân hư cấu mô tả khó chịu vùng hàm; đang chờ bác sĩ đánh giá.',NULL),
 (N'CK004',N'KHOA004',N'Hình ảnh',N'DV006',N'Chụp X-quang hàm mặt',N'Khảo sát vùng hàm mặt',N'Hồ sơ mô phỏng chờ đối chiếu thông tin khảo sát; chưa có kết quả hình ảnh.',NULL),
 (N'CK005',N'KHOA005',N'Xét nghiệm',N'DEMO28_DV_CK005',N'Khám tư vấn xét nghiệm (demo)',N'Trao đổi thông tin xét nghiệm',N'Hồ sơ hư cấu chờ bác sĩ trao đổi thông tin xét nghiệm; chưa lấy mẫu, chưa có kết quả.',NULL),
 (N'CK006',N'KHOA006',N'Gây mê',N'DEMO28_DV_CK006',N'Khám đánh giá trước gây mê (demo)',N'Thu thập tiền sử trước gây mê',N'Hồ sơ hư cấu dùng thử phần ghi nhận tiền sử; chưa đánh giá nguy cơ, chưa có chỉ định.',NULL),
 (N'CK007',N'KHOA007',N'Phục hồi',N'DV008',N'Chăm sóc vùng da bị tổn thương',N'Theo dõi vùng da và vận động hàm',N'Bệnh nhân hư cấu đến trao đổi tình trạng vùng da và vận động hàm; chưa thực hiện dịch vụ.',NULL);
 IF EXISTS(SELECT 1 FROM @CK x LEFT JOIN dbo.CHUYEN_KHOA c WITH(UPDLOCK,HOLDLOCK) ON c.MaCK=x.MaCK LEFT JOIN dbo.KHOA k ON k.MaKhoa=x.MaKhoa
   WHERE c.MaCK IS NULL OR c.MaKhoa<>x.MaKhoa OR c.TrangThai<>N'Đang hoạt động' OR k.TrangThai<>N'Đang hoạt động')
   THROW 52140,N'Cần các chuyên khoa CK001..CK007 đang hoạt động, đúng khoa gốc; không tự sửa dữ liệu hiện có.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.CHUC_VU WHERE MaChucVu=N'CV002') OR NOT EXISTS(SELECT 1 FROM dbo.VAI_TRO WHERE MaVaiTro=N'VTRO002')
   THROW 52140,N'Thiếu chức vụ bác sĩ CV002 hoặc vai trò User VTRO002.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.NHAN_VIEN n JOIN dbo.TAI_KHOAN t ON t.MaNV=n.MaNV
   WHERE n.MaNV=N'NV005' AND n.MaChucVu=N'CV004' AND n.TrangThai=N'Đang làm việc' AND t.TrangThai=N'Đang hoạt động')
   THROW 52140,N'Cần nhân viên tiếp nhận NV005 đang hoạt động; không tự đổi nhân sự hiện có.',1;

 DECLARE @Doctors TABLE(MaCK nvarchar(50),Buoi nvarchar(10),MatKhau nvarchar(512),PRIMARY KEY(MaCK,Buoi));
 INSERT @Doctors VALUES
 (N'CK002',N'ngay',N'pbkdf2_sha256$210000$bxiYXsBICN+JKz4/Zv7/4Q==$AHbwkMFstwwMUmyTLCpaNNBwGZFvADoDDSQJkf4F5/c='),
 (N'CK002',N'dem',N'pbkdf2_sha256$210000$FVdm/PvdQ9ob1QAf/qy4nw==$LrXDpekeif/utP9wMTjopty7uOYWXoTr3cCcUjIY/Jc='),
 (N'CK003',N'ngay',N'pbkdf2_sha256$210000$juB531vLj+5TTs1O6Hdsbg==$lS59OFVJKeu0UaCNAFPesRZ+4nFFldis+rqPW3l7RzU='),
 (N'CK003',N'dem',N'pbkdf2_sha256$210000$qvcGZXSOJYSulhAUFItZIQ==$9Q/wv2v5SczgV0zqJDdcO7JQdSy+p3YCsnqWxKpBN1o='),
 (N'CK004',N'ngay',N'pbkdf2_sha256$210000$oVQM3tk4x5hheuVeO25EKg==$89+Ro3TNg8NUwEoYseQDWPO7dxXTlVDQ23u99uJ3l3k='),
 (N'CK004',N'dem',N'pbkdf2_sha256$210000$5ipFHGE3E7MEea/v+vCYFA==$kn9124wqypmq5wAJVpfao+OL8KmOQp6nZwMrEHtggHU='),
 (N'CK005',N'ngay',N'pbkdf2_sha256$210000$TkpPf1aBIgIo/0ibXdVggw==$ixj/qXkCXcEpH1Q8mcD1klgxaG27JlhcxXV2i2l+hMY='),
 (N'CK005',N'dem',N'pbkdf2_sha256$210000$iBW4GRPDPBgvOlNzG1tTCA==$4IhBlz4fXapGAx2mKyLfGhQ8AbHw5M0fnoq3r9VEQyU='),
 (N'CK006',N'ngay',N'pbkdf2_sha256$210000$hm55xyiW2GX005lCGOCuKg==$pK3Kb2lEP5rLRBh3I+HHjkJFDn1oQIUOhGC0QfnfRAE='),
 (N'CK006',N'dem',N'pbkdf2_sha256$210000$vR2o9J2V8UlDGfXG5tR8aA==$kho9vsgTwkeHt4soUKETPCDfBXOqEPfxJtTmJuUBVdc='),
 (N'CK007',N'ngay',N'pbkdf2_sha256$210000$kUEEKd+LjWw2YdJr7WO1Rg==$Xl7yD/PwxzdGtXoX1r69fR1ShfvkumgMqNULPAwzeYw='),
 (N'CK007',N'dem',N'pbkdf2_sha256$210000$qrOSy9GxQH92lUbxayvtjw==$Jklc6gG2SpHqkgAmvfdIhIYmXl3reZilL08JtnF7Opc=');
 -- Kiểm tra mã dành riêng trước INSERT; tuyệt đối không MERGE/UPDATE các dòng hiện có.
 IF EXISTS(SELECT 1 FROM @Doctors d JOIN dbo.NHAN_VIEN n WITH(UPDLOCK,HOLDLOCK) ON n.MaNV=N'DEMO28_NV_'+d.MaCK+N'_'+d.Buoi
   WHERE n.MaCK<>d.MaCK OR n.MaChucVu<>N'CV002' OR n.DiaChi<>@Marker OR n.DiaChi IS NULL)
   THROW 52141,N'Xung đột mã nhân viên DEMO28; dừng để đối chiếu, không ghi đè.',1;
 INSERT dbo.NHAN_VIEN(MaNV,HoTen,GioiTinh,NgaySinh,SDT,DiaChi,MaChucVu,MaCK,BangCap,NgayVaoLam,TrangThai)
 SELECT N'DEMO28_NV_'+d.MaCK+N'_'+d.Buoi,N'BS Hư Cấu '+c.Ten+CASE d.Buoi WHEN N'ngay' THEN N' Ngày' ELSE N' Đêm' END,NULL,'1985-01-01',NULL,@Marker,N'CV002',d.MaCK,N'Bác sĩ (hư cấu để demo)','2026-01-01',N'Đang làm việc'
 FROM @Doctors d JOIN @CK c ON c.MaCK=d.MaCK WHERE NOT EXISTS(SELECT 1 FROM dbo.NHAN_VIEN n WITH(UPDLOCK,HOLDLOCK) WHERE n.MaNV=N'DEMO28_NV_'+d.MaCK+N'_'+d.Buoi);
 SET @NewDoctors=@@ROWCOUNT;
 IF EXISTS(SELECT 1 FROM @Doctors d JOIN dbo.TAI_KHOAN t WITH(UPDLOCK,HOLDLOCK)
   ON t.MaTK=N'DEMO28_TK_'+d.MaCK+N'_'+d.Buoi OR t.TenDangNhap=N'demo_'+LOWER(d.MaCK)+N'_'+d.Buoi OR t.MaNV=N'DEMO28_NV_'+d.MaCK+N'_'+d.Buoi
   WHERE t.MaTK<>N'DEMO28_TK_'+d.MaCK+N'_'+d.Buoi OR t.MaNV<>N'DEMO28_NV_'+d.MaCK+N'_'+d.Buoi OR t.TenDangNhap<>N'demo_'+LOWER(d.MaCK)+N'_'+d.Buoi)
   THROW 52141,N'Xung đột mã/tên đăng nhập/liên kết tài khoản DEMO28; không ghi đè.',1;
 INSERT dbo.TAI_KHOAN(MaTK,TenDangNhap,MatKhauHash,MaVaiTro,MaNV,MaKhoa,MaPhong,TrangThai)
 SELECT N'DEMO28_TK_'+MaCK+N'_'+Buoi,N'demo_'+LOWER(MaCK)+N'_'+Buoi,MatKhau,N'VTRO002',N'DEMO28_NV_'+MaCK+N'_'+Buoi,NULL,NULL,N'Đang hoạt động'
 FROM @Doctors d WHERE NOT EXISTS(SELECT 1 FROM dbo.TAI_KHOAN t WITH(UPDLOCK,HOLDLOCK) WHERE t.MaTK=N'DEMO28_TK_'+d.MaCK+N'_'+d.Buoi);

 IF EXISTS(SELECT 1 FROM @CK c JOIN dbo.PHONG p WITH(UPDLOCK,HOLDLOCK) ON p.MaPhong=N'DEMO28_PH_'+c.MaCK WHERE p.MaKhoa<>c.MaKhoa OR p.TenPhong<>N'Phòng khám demo - '+c.Ten)
   THROW 52141,N'Xung đột mã phòng DEMO28; không ghi đè.',1;
 INSERT dbo.PHONG(MaPhong,TenPhong,MaKhoa,LoaiPhong,TinhTrang,TrangThai)
 SELECT N'DEMO28_PH_'+c.MaCK,N'Phòng khám demo - '+c.Ten,c.MaKhoa,N'Phòng khám chuyên khoa',N'Sẵn sàng',N'Đang hoạt động'
 FROM @CK c WHERE NOT EXISTS(SELECT 1 FROM dbo.PHONG p WITH(UPDLOCK,HOLDLOCK) WHERE p.MaPhong=N'DEMO28_PH_'+c.MaCK);
 SET @NewRooms=@@ROWCOUNT;
 IF EXISTS(SELECT 1 FROM @CK c JOIN dbo.DICH_VU d WITH(UPDLOCK,HOLDLOCK) ON d.MaDV=c.MaDV WHERE d.MaCK<>c.MaCK OR (c.MaDV LIKE N'DEMO28[_]%' AND (d.MoTa<>@Marker OR d.MoTa IS NULL)))
   THROW 52141,N'Dịch vụ không khớp chuyên khoa hoặc xung đột mã dịch vụ DEMO28; không thay chuyên khoa lịch sử.',1;
 INSERT dbo.DICH_VU(MaDV,TenDV,NhomDV,MaCK,DonGia,ThoiGianDuKien,MoTa,NgayApDung,TrangThai)
 SELECT MaDV,TenDV,N'Khám và tư vấn',MaCK,100000,30,@Marker,'2026-01-01',N'Đang hoạt động'
 FROM @CK c WHERE c.MaDV LIKE N'DEMO28[_]%' AND NOT EXISTS(SELECT 1 FROM dbo.DICH_VU d WITH(UPDLOCK,HOLDLOCK) WHERE d.MaDV=c.MaDV);
 SET @NewServices=@@ROWCOUNT;
 IF EXISTS(SELECT 1 FROM @CK c LEFT JOIN dbo.DICH_VU d ON d.MaDV=c.MaDV WHERE d.MaDV IS NULL OR d.TrangThai<>N'Đang hoạt động')
   THROW 52140,N'Dịch vụ demo hoặc dịch vụ gốc đang ngừng hoạt động; không tự bật lại.',1;

 DECLARE @Shifts TABLE(MaCa nvarchar(50) PRIMARY KEY,Buoi nvarchar(10),BatDau time(0),KetThuc time(0));
 INSERT @Shifts VALUES(N'DEMO28_CA_NGAY',N'ngay','07:00','19:00'),(N'DEMO28_CA_DEM',N'dem','19:00','07:00');
 IF EXISTS(SELECT 1 FROM @Shifts s JOIN dbo.CA_LAM c WITH(UPDLOCK,HOLDLOCK) ON c.MaCa=s.MaCa WHERE c.GioBatDau<>s.BatDau OR c.GioKetThuc<>s.KetThuc OR c.GhiChu<>@Marker OR c.GhiChu IS NULL)
   THROW 52141,N'Xung đột mã ca DEMO28; không ghi đè.',1;
 INSERT dbo.CA_LAM(MaCa,GioBatDau,GioKetThuc,TrangThai,GhiChu)
 SELECT s.MaCa,s.BatDau,s.KetThuc,N'Đang hoạt động',@Marker FROM @Shifts s WHERE NOT EXISTS(SELECT 1 FROM dbo.CA_LAM c WITH(UPDLOCK,HOLDLOCK) WHERE c.MaCa=s.MaCa);
 DECLARE @Roster TABLE(MaNV nvarchar(50),MaCK nvarchar(50),Buoi nvarchar(10),PRIMARY KEY(MaNV));
 INSERT @Roster SELECT N'DEMO28_NV_'+MaCK+N'_'+Buoi,MaCK,Buoi FROM @Doctors;
 -- Chỉ thêm lịch cho bác sĩ gốc nếu đúng chuyên khoa/chức vụ và tài khoản còn hoạt động.
 INSERT @Roster SELECT n.MaNV,N'CK001',CASE n.MaNV WHEN N'NV002' THEN N'ngay' ELSE N'dem' END
 FROM dbo.NHAN_VIEN n WHERE n.MaNV IN(N'NV002',N'NV003') AND n.MaCK=N'CK001' AND n.MaChucVu=N'CV002';
 DECLARE @Schedule TABLE(MaLLV nvarchar(50) PRIMARY KEY,MaNV nvarchar(50),MaCa nvarchar(50),Ngay date,MaPhong nvarchar(50),BatDau datetime2(0),KetThuc datetime2(0));
 ;WITH Days AS(SELECT -1 n UNION ALL SELECT n+1 FROM Days WHERE n<14)
 INSERT @Schedule
 SELECT N'DEMO28_L_'+CONVERT(nvarchar(8),DATEADD(day,d.n,@Today),112)+N'_'+r.MaNV,r.MaNV,s.MaCa,DATEADD(day,d.n,@Today),N'DEMO28_PH_'+r.MaCK,
   DATEADD(hour,CASE r.Buoi WHEN N'ngay' THEN 7 ELSE 19 END,CONVERT(datetime2(0),DATEADD(day,d.n,@Today))),
   DATEADD(hour,CASE r.Buoi WHEN N'ngay' THEN 19 ELSE 31 END,CONVERT(datetime2(0),DATEADD(day,d.n,@Today)))
 FROM Days d CROSS JOIN @Roster r JOIN @Shifts s ON s.Buoi=r.Buoi;
 IF EXISTS(SELECT 1 FROM @Schedule s JOIN dbo.LICH_LAM_VIEC l WITH(UPDLOCK,HOLDLOCK) ON l.MaLLV=s.MaLLV WHERE l.MaNV<>s.MaNV OR l.NgayLam<>s.Ngay OR l.GhiChu<>@Marker OR l.GhiChu IS NULL)
   THROW 52141,N'Xung đột mã lịch DEMO28; không ghi đè.',1;
 -- Không đè lịch (kể cả nháp/hủy), phòng đã có lịch hoặc đơn nghỉ đã duyệt.
 -- So sánh khoảng nửa mở [bắt đầu,kết thúc), bao gồm ca qua nửa đêm.
 INSERT dbo.LICH_LAM_VIEC(MaLLV,MaNV,MaCa,NgayLam,MaPhong,TrangThai,TrangThaiHienDien,GhiChu)
 SELECT s.MaLLV,s.MaNV,s.MaCa,s.Ngay,s.MaPhong,N'Đã xác nhận',N'Có mặt',@Marker
 FROM @Schedule s JOIN dbo.NHAN_VIEN n ON n.MaNV=s.MaNV AND n.TrangThai=N'Đang làm việc'
 JOIN dbo.CA_LAM ca ON ca.MaCa=s.MaCa AND ca.TrangThai=N'Đang hoạt động'
 JOIN dbo.PHONG p ON p.MaPhong=s.MaPhong AND p.TrangThai=N'Đang hoạt động' AND p.LoaiPhong LIKE N'Phòng khám%'
 WHERE EXISTS(SELECT 1 FROM dbo.TAI_KHOAN t WHERE t.MaNV=s.MaNV AND t.TrangThai=N'Đang hoạt động')
 AND NOT EXISTS(SELECT 1 FROM dbo.LICH_LAM_VIEC l WITH(UPDLOCK,HOLDLOCK) WHERE l.MaLLV=s.MaLLV)
 AND NOT EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI d WITH(UPDLOCK,HOLDLOCK) WHERE d.MaNV=s.MaNV AND d.TrangThaiDuyet=N'Đã duyệt'
   AND d.TuNgay<=CONVERT(date,DATEADD(second,-1,s.KetThuc)) AND d.DenNgay>=s.Ngay)
 AND NOT EXISTS(SELECT 1 FROM dbo.LICH_LAM_VIEC l WITH(UPDLOCK,HOLDLOCK) JOIN dbo.CA_LAM c ON c.MaCa=l.MaCa
   CROSS APPLY(SELECT DATEADD(second,DATEDIFF(second,CAST('00:00' AS time),c.GioBatDau),CONVERT(datetime2(0),l.NgayLam)) B,
     DATEADD(day,CASE WHEN c.GioKetThuc<=c.GioBatDau THEN 1 ELSE 0 END,DATEADD(second,DATEDIFF(second,CAST('00:00' AS time),c.GioKetThuc),CONVERT(datetime2(0),l.NgayLam))) E) t
   WHERE (l.MaNV=s.MaNV OR l.MaPhong=s.MaPhong) AND t.B<s.KetThuc AND t.E>s.BatDau);
 SET @NewSchedules=@@ROWCOUNT;
 INSERT dbo.CA_THEO_NGAY(NgayLam,MaCa)
 SELECT DISTINCT l.NgayLam,l.MaCa FROM dbo.LICH_LAM_VIEC l JOIN @Schedule s ON s.MaLLV=l.MaLLV
 WHERE NOT EXISTS(SELECT 1 FROM dbo.CA_THEO_NGAY c WITH(UPDLOCK,HOLDLOCK) WHERE c.NgayLam=l.NgayLam AND c.MaCa=l.MaCa);

 DECLARE @Cases TABLE(MaCK nvarchar(50),Loai nvarchar(10),MaBN nvarchar(50) PRIMARY KEY,MaHS nvarchar(50),MaPhieu nvarchar(50),MaLS nvarchar(50),MaTTB nvarchar(50),MaBS nvarchar(50),MaPhong nvarchar(50),MaDV nvarchar(50));
 INSERT @Cases(MaCK,Loai,MaBN,MaHS,MaPhieu,MaLS,MaTTB,MaDV)
 SELECT c.MaCK,v.Loai,N'DEMO28_BN_'+c.MaCK+N'_'+v.Loai,N'DEMO28_HS_'+c.MaCK+N'_'+v.Loai,N'DEMO28_PK_'+c.MaCK+N'_'+v.Loai,
   CASE WHEN v.Loai=N'NHAP' THEN N'DEMO28_LS_'+c.MaCK END,CASE WHEN v.Loai=N'NHAP' THEN N'DEMO28_TTB_'+c.MaCK END,c.MaDV
 FROM @CK c CROSS JOIN(VALUES(N'CHO'),(N'NHAP'))v(Loai);
 -- Các neo định danh nguồn bất biến được kiểm tra cả khi lượt đã ký/đã thanh toán.
 IF EXISTS(SELECT 1 FROM @Cases x JOIN dbo.BENH_NHAN b WITH(UPDLOCK,HOLDLOCK) ON b.MaBN=x.MaBN WHERE b.DiaChi<>@Marker OR b.DiaChi IS NULL)
 OR EXISTS(SELECT 1 FROM @Cases x JOIN dbo.HO_SO_BENH_AN h WITH(UPDLOCK,HOLDLOCK) ON h.MaHS=x.MaHS OR h.MaBN=x.MaBN WHERE h.MaHS<>x.MaHS OR h.MaBN<>x.MaBN)
 OR EXISTS(SELECT 1 FROM @Cases x JOIN dbo.PHIEU_KHAM_BENH p WITH(UPDLOCK,HOLDLOCK) ON p.MaPhieu=x.MaPhieu WHERE p.MaHS<>x.MaHS)
 OR EXISTS(SELECT 1 FROM @Cases x JOIN dbo.LICH_SU_KHAM_BENH l WITH(UPDLOCK,HOLDLOCK) ON l.MaLS=x.MaLS OR (x.MaLS IS NOT NULL AND l.MaPhieu=x.MaPhieu) WHERE l.MaLS<>x.MaLS OR l.MaPhieu<>x.MaPhieu)
 OR EXISTS(SELECT 1 FROM @Cases x JOIN dbo.TINH_TRANG_BENH t WITH(UPDLOCK,HOLDLOCK) ON t.MaTTB=x.MaTTB WHERE t.MaLS<>x.MaLS)
   THROW 52141,N'Xung đột mã/nguồn bệnh nhân, hồ sơ, phiếu hoặc tình trạng DEMO28; không ghi đè.',1;
 -- Phục hồi tự động sau khi ai đó xóa một phần hồ sơ có thể tạo trạng thái sai: dừng để đối chiếu.
 IF EXISTS(SELECT 1 FROM @Cases x JOIN dbo.BENH_NHAN b ON b.MaBN=x.MaBN LEFT JOIN dbo.HO_SO_BENH_AN h ON h.MaHS=x.MaHS LEFT JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=x.MaPhieu
   WHERE h.MaHS IS NULL OR p.MaPhieu IS NULL)
   THROW 52141,N'Bộ hồ sơ demo đã bị xóa một phần; dừng để đối chiếu, không tự tái tạo.',1;
 -- Dùng lịch thật bao phủ thời điểm tiếp nhận, cùng tiêu chí DAL_TIEPNHAN.Candidates.
 UPDATE x SET MaBS=a.MaNV,MaPhong=a.MaPhong FROM @Cases x OUTER APPLY(
   SELECT TOP(1) n.MaNV,l.MaPhong FROM dbo.NHAN_VIEN n JOIN dbo.CHUYEN_KHOA ck ON ck.MaCK=n.MaCK
   CROSS APPLY(SELECT TOP(1) s.MaPhong,s.MaLLV FROM dbo.LICH_LAM_VIEC s JOIN dbo.CA_LAM c ON c.MaCa=s.MaCa
     CROSS APPLY(SELECT DATEADD(second,DATEDIFF(second,CAST('00:00' AS time),c.GioBatDau),CONVERT(datetime2(0),s.NgayLam)) B,
       DATEADD(day,CASE WHEN c.GioKetThuc<=c.GioBatDau THEN 1 ELSE 0 END,DATEADD(second,DATEDIFF(second,CAST('00:00' AS time),c.GioKetThuc),CONVERT(datetime2(0),s.NgayLam))) E) t
     WHERE s.MaNV=n.MaNV AND s.TrangThai=N'Đã xác nhận' AND s.TrangThaiHienDien NOT IN(N'Vắng mặt',N'Nghỉ phép') AND c.TrangThai=N'Đang hoạt động'
       AND @Now>=t.B AND @Now<t.E AND s.NgayLam BETWEEN DATEADD(day,-1,@Today) AND @Today
     ORDER BY s.MaLLV) l
   JOIN dbo.PHONG p ON p.MaPhong=l.MaPhong AND p.MaKhoa=ck.MaKhoa AND p.TrangThai=N'Đang hoạt động' AND p.LoaiPhong LIKE N'Phòng khám%'
   WHERE n.MaCK=x.MaCK AND n.MaChucVu=N'CV002' AND n.TrangThai=N'Đang làm việc'
     AND EXISTS(SELECT 1 FROM dbo.TAI_KHOAN tk WHERE tk.MaNV=n.MaNV AND tk.TrangThai=N'Đang hoạt động')
     AND NOT EXISTS(SELECT 1 FROM dbo.DON_XIN_NGHI d WHERE d.MaNV=n.MaNV AND d.TrangThaiDuyet=N'Đã duyệt' AND @Today BETWEEN d.TuNgay AND d.DenNgay)
   ORDER BY CASE WHEN n.MaNV LIKE N'DEMO28[_]%' THEN 0 ELSE 1 END,n.MaNV,l.MaLLV) a
 WHERE NOT EXISTS(SELECT 1 FROM dbo.PHIEU_KHAM_BENH p WHERE p.MaPhieu=x.MaPhieu);
 IF EXISTS(SELECT 1 FROM @Cases x WHERE x.MaBS IS NULL AND NOT EXISTS(SELECT 1 FROM dbo.PHIEU_KHAM_BENH p WHERE p.MaPhieu=x.MaPhieu))
   THROW 52142,N'Có chuyên khoa chưa có bác sĩ/phòng/ca hợp lệ lúc này. Lịch hoặc nghỉ phép hiện có được giữ nguyên; đối chiếu trước khi thử lại.',1;

 INSERT dbo.BENH_NHAN(MaBN,HoTenBN,GioiTinh,NgaySinh,DiaChi,TienSuBenh,DiUng,TrangThai)
 SELECT x.MaBN,N'BN Hư Cấu '+c.Ten+CASE x.Loai WHEN N'CHO' THEN N' Chờ' ELSE N' Nháp' END,NULL,'1990-01-01',@Marker,N'Dữ liệu demo; chưa xác minh tiền sử.',N'Dữ liệu demo; chưa xác minh dị ứng.',N'Đang theo dõi'
 FROM @Cases x JOIN @CK c ON c.MaCK=x.MaCK WHERE NOT EXISTS(SELECT 1 FROM dbo.BENH_NHAN b WHERE b.MaBN=x.MaBN);
 SET @NewPatients=@@ROWCOUNT;
 INSERT dbo.HO_SO_BENH_AN(MaHS,MaBN,NgayLap,LyDoKhamBanDau,GhiChu,TrangThai)
 SELECT x.MaHS,x.MaBN,@Now,c.MoTa,@Marker,N'Đang theo dõi' FROM @Cases x JOIN @CK c ON c.MaCK=x.MaCK WHERE NOT EXISTS(SELECT 1 FROM dbo.HO_SO_BENH_AN h WHERE h.MaHS=x.MaHS);
 DECLARE @NewCases TABLE(MaPhieu nvarchar(50) PRIMARY KEY);
 ;WITH Missing AS(SELECT x.*,ROW_NUMBER() OVER(PARTITION BY x.MaPhong ORDER BY x.MaCK,x.Loai) rn FROM @Cases x WHERE NOT EXISTS(SELECT 1 FROM dbo.PHIEU_KHAM_BENH p WHERE p.MaPhieu=x.MaPhieu))
 INSERT dbo.PHIEU_KHAM_BENH(MaPhieu,MaHS,MaLich,MaPhong,MaBS,MaNVTiepNhan,MaDV,SoThuTu,ThoiGianTiepNhan,LyDoKham,TrangThai,GhiChu)
 OUTPUT inserted.MaPhieu INTO @NewCases
 SELECT x.MaPhieu,x.MaHS,NULL,x.MaPhong,x.MaBS,N'NV005',x.MaDV,CONVERT(int,q.MaxOrder+x.rn),@Now,c.MoTa,CASE x.Loai WHEN N'CHO' THEN N'Chờ khám' ELSE N'Đang khám' END,@Marker
 FROM Missing x JOIN @CK c ON c.MaCK=x.MaCK CROSS APPLY(SELECT COALESCE(MAX(p.SoThuTu),0) MaxOrder FROM dbo.PHIEU_KHAM_BENH p WITH(UPDLOCK,HOLDLOCK) WHERE p.MaPhong=x.MaPhong AND p.ThoiGianTiepNhan>=@Today AND p.ThoiGianTiepNhan<DATEADD(day,1,@Today)) q;
 SET @NewReceipts=@@ROWCOUNT;
 -- Chỉ tạo lần khám/tình trạng cho phiếu vừa tạo; không điền thêm vào lượt đã được người dùng xử lý.
 INSERT dbo.LICH_SU_KHAM_BENH(MaLS,MaPhieu,MaBS,MaPhong,ThoiGianKham,TrieuChung,ChanDoan,KeHoachXuLy,TrangThai,NgayXacNhan,GhiChu,KhongKeThuoc)
 SELECT x.MaLS,x.MaPhieu,x.MaBS,x.MaPhong,@Now,c.MoTa,N'DEMO: chưa kết luận chẩn đoán.',N'DEMO: đang nhập hồ sơ, chưa chỉ định điều trị.',N'Nháp',NULL,@Marker,0
 FROM @Cases x JOIN @NewCases n ON n.MaPhieu=x.MaPhieu JOIN @CK c ON c.MaCK=x.MaCK WHERE x.Loai=N'NHAP';
 SET @NewDrafts=@@ROWCOUNT;
 INSERT dbo.TINH_TRANG_BENH(MaTTB,MaLS,LoaiTinhTrang,MoTaTinhTrang,GhiChu,MaDMTT)
 SELECT x.MaTTB,x.MaLS,c.TinhTrang,c.MoTa,@Marker,NULL
 FROM @Cases x JOIN @NewCases n ON n.MaPhieu=x.MaPhieu JOIN @CK c ON c.MaCK=x.MaCK WHERE x.Loai=N'NHAP';
 SET @NewConditions=@@ROWCOUNT;
 -- Bất biến nguồn trên toàn bộ bộ demo; không ép trạng thái vì các lượt cũ có thể đã được xử lý.
 IF (SELECT COUNT(*) FROM @Cases x JOIN dbo.BENH_NHAN b ON b.MaBN=x.MaBN JOIN dbo.HO_SO_BENH_AN h ON h.MaHS=x.MaHS AND h.MaBN=x.MaBN JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=x.MaPhieu AND p.MaHS=x.MaHS)<>14
   THROW 52143,N'Kiểm tra đủ 14 bệnh nhân/hồ sơ/phiếu demo thất bại.',1;
 IF EXISTS(SELECT 1 FROM @NewCases n JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=n.MaPhieu JOIN dbo.DICH_VU d ON d.MaDV=p.MaDV JOIN dbo.NHAN_VIEN bs ON bs.MaNV=p.MaBS WHERE bs.MaCK<>d.MaCK)
 OR EXISTS(SELECT 1 FROM @NewCases n JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaPhieu=n.MaPhieu WHERE l.TrangThai<>N'Nháp' OR l.NgayXacNhan IS NOT NULL)
   THROW 52143,N'Kiểm tra bác sĩ/chuyên khoa hoặc lần khám nháp thất bại.',1;
 COMMIT TRANSACTION;
 SELECT @NewDoctors BacSiThem,@NewRooms PhongThem,@NewServices DichVuThem,@NewSchedules LichThem,
   (SELECT COUNT(*) FROM @Schedule)-@NewSchedules LichDaCoHoacBoQua,@NewPatients BenhNhanThem,@NewReceipts PhieuThem,@NewDrafts LanKhamNhapThem,@NewConditions TinhTrangThem;
 SELECT x.MaCK,x.Loai,x.MaBN,p.MaPhieu,p.TrangThai,p.MaBS,t.TenDangNhap,p.MaPhong,p.MaDV,p.ThoiGianTiepNhan,l.MaLS,l.TrangThai TrangThaiLanKham
 FROM @Cases x JOIN dbo.PHIEU_KHAM_BENH p ON p.MaPhieu=x.MaPhieu LEFT JOIN dbo.TAI_KHOAN t ON t.MaNV=p.MaBS LEFT JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaPhieu=p.MaPhieu ORDER BY x.MaCK,x.Loai;
 PRINT N'Đã nạp demo hư cấu; giữ nguyên dữ liệu cũ. Không tạo đơn thuốc, thu tiền, hóa đơn hoặc xuất kho.';
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
