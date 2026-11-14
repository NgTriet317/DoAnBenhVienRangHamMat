/* Independent read-only workflow audit. No persistent writes, EXEC, or mutations.
   Run against isolated clone only. Histories and unfinished work are classified separately. */
SET NOCOUNT ON;
DECLARE @Today date=CONVERT(date,SYSDATETIME());
DECLARE @Findings TABLE([Rule] varchar(60),Class varchar(30),RecordId nvarchar(250),Evidence nvarchar(2000));

INSERT @Findings
SELECT 'APPOINTMENT_SPECIALTY','Mismatch',l.MaLich,CONCAT(l.MaBS,N':',n.MaCK,N' vs ',l.MaDV,N':',d.MaCK)
FROM dbo.LICH_KHAM l JOIN dbo.NHAN_VIEN n ON n.MaNV=l.MaBS JOIN dbo.DICH_VU d ON d.MaDV=l.MaDV
WHERE l.TrangThai NOT IN(N'Hủy',N'Đã hủy',N'Đã đổi') AND (n.MaChucVu<>N'CV002' OR n.MaCK IS NULL OR n.MaCK<>d.MaCK);

INSERT @Findings
SELECT 'APPOINTMENT_ROOM_SPECIALTY','Mismatch',l.MaLich,CONCAT(l.MaBS,N':',n.MaCK,N'/',k.MaKhoa,N' vs ',l.MaPhong,N':',p.MaKhoa)
FROM dbo.LICH_KHAM l JOIN dbo.NHAN_VIEN n ON n.MaNV=l.MaBS JOIN dbo.CHUYEN_KHOA k ON k.MaCK=n.MaCK JOIN dbo.PHONG p ON p.MaPhong=l.MaPhong
WHERE l.TrangThai NOT IN(N'Hủy',N'Đã hủy',N'Đã đổi') AND k.MaKhoa<>p.MaKhoa;

INSERT @Findings
SELECT 'APPOINTMENT_SHIFT_TIME','Mismatch',l.MaLich,CONCAT(l.NgayGioKham,N' + ',l.ThoiLuongDuKien,N' minutes vs ',l.MaCa,N' ',c.GioBatDau,N'-',c.GioKetThuc)
FROM dbo.LICH_KHAM l JOIN dbo.CA_LAM c ON c.MaCa=l.MaCa
CROSS APPLY(SELECT DATEADD(second,DATEDIFF(second,CONVERT(time,'00:00'),c.GioBatDau),CONVERT(datetime2,CONVERT(date,l.NgayGioKham))) Starts) st
CROSS APPLY(SELECT DATEADD(day,CASE WHEN c.GioKetThuc<=c.GioBatDau THEN 1 ELSE 0 END,DATEADD(second,DATEDIFF(second,CONVERT(time,'00:00'),c.GioKetThuc),CONVERT(datetime2,CONVERT(date,l.NgayGioKham)))) Ends) en
WHERE l.TrangThai NOT IN(N'Hủy',N'Đã hủy',N'Đã đổi') AND(l.NgayGioKham<st.Starts OR DATEADD(minute,l.ThoiLuongDuKien,l.NgayGioKham)>en.Ends);

INSERT @Findings
SELECT 'APPOINTMENT_RESOURCE_OVERLAP','Mismatch',CONCAT(a.MaLich,N' / ',b.MaLich),CONCAT(a.NgayGioKham,N'; ',a.MaBS,N'/',a.MaPhong,N'/',a.MaBN,N' vs ',b.MaBS,N'/',b.MaPhong,N'/',b.MaBN)
FROM dbo.LICH_KHAM a JOIN dbo.LICH_KHAM b ON a.MaLich<b.MaLich
AND a.NgayGioKham<DATEADD(minute,b.ThoiLuongDuKien,b.NgayGioKham) AND b.NgayGioKham<DATEADD(minute,a.ThoiLuongDuKien,a.NgayGioKham)
WHERE a.TrangThai NOT IN(N'Hủy',N'Đã hủy',N'Đã đổi',N'Hoàn thành',N'Đã khám') AND b.TrangThai NOT IN(N'Hủy',N'Đã hủy',N'Đã đổi',N'Hoàn thành',N'Đã khám')
AND(a.MaBS=b.MaBS OR a.MaPhong=b.MaPhong OR a.MaBN=b.MaBN OR a.MaBS=b.MaBSDuBi OR a.MaBSDuBi=b.MaBS OR a.MaBSDuBi=b.MaBSDuBi);

INSERT @Findings
SELECT 'APPOINTMENT_ACTIVE_ROSTER','Mismatch',l.MaLich,CONCAT(l.MaBS,N'/',l.MaCa,N'/',CONVERT(date,l.NgayGioKham),N'/',l.MaPhong,N' missing confirmed matching shift')
FROM dbo.LICH_KHAM l
WHERE l.TrangThai NOT IN(N'Hủy',N'Đã hủy',N'Đã đổi',N'Hoàn thành',N'Đã khám') AND CONVERT(date,l.NgayGioKham)>=@Today
AND NOT EXISTS(SELECT 1 FROM dbo.LICH_LAM_VIEC s WHERE s.MaNV=l.MaBS AND s.MaCa=l.MaCa AND s.NgayLam=CONVERT(date,l.NgayGioKham) AND s.MaPhong=l.MaPhong AND s.TrangThai=N'Đã xác nhận');

INSERT @Findings
SELECT 'APPOINTMENT_APPROVED_LEAVE','Mismatch',l.MaLich,CONCAT(l.MaBS,N' or ',l.MaBSDuBi,N'; ',d.MaDonNghi)
FROM dbo.LICH_KHAM l JOIN dbo.DON_XIN_NGHI d ON(d.MaNV=l.MaBS OR d.MaNV=l.MaBSDuBi) AND CONVERT(date,l.NgayGioKham)>=d.TuNgay AND(CONVERT(date,l.NgayGioKham)<=d.DenNgay OR d.GhiChu LIKE N'%Loại đơn: Nghỉ việc%')
WHERE d.TrangThaiDuyet=N'Đã duyệt' AND l.TrangThai NOT IN(N'Hủy',N'Đã hủy',N'Đã đổi',N'Hoàn thành',N'Đã khám');

INSERT @Findings
SELECT 'APPOINTMENT_BACKUP_INVALID','Mismatch',l.MaLich,CONCAT(l.MaBS,N'/',l.MaBSDuBi,N'/',l.MucDoNghiemTrong)
FROM dbo.LICH_KHAM l JOIN dbo.NHAN_VIEN n ON n.MaNV=l.MaBS LEFT JOIN dbo.NHAN_VIEN b ON b.MaNV=l.MaBSDuBi
WHERE l.TrangThai NOT IN(N'Hủy',N'Đã hủy',N'Đã đổi',N'Hoàn thành',N'Đã khám') AND
((l.MucDoNghiemTrong=N'Cao' AND l.MaBSDuBi IS NULL) OR(l.MaBSDuBi IS NOT NULL AND(l.MaBS=l.MaBSDuBi OR b.MaNV IS NULL OR b.MaCK<>n.MaCK OR b.MaChucVu<>n.MaChucVu OR b.TrangThai<>N'Đang làm việc')));

INSERT @Findings
SELECT 'ROSTER_EMPLOYEE_OVERLAP','Mismatch',CONCAT(a.MaLLV,N' / ',b.MaLLV),CONCAT(a.MaNV,N' ',a.NgayLam,N' ',a.MaCa,N' vs ',b.MaCa)
FROM dbo.LICH_LAM_VIEC a JOIN dbo.LICH_LAM_VIEC b ON a.MaLLV<b.MaLLV AND a.MaNV=b.MaNV AND a.NgayLam=b.NgayLam
JOIN dbo.CA_LAM ca ON ca.MaCa=a.MaCa JOIN dbo.CA_LAM cb ON cb.MaCa=b.MaCa
WHERE a.TrangThai IN(N'Đã xác nhận',N'Chờ xác nhận',N'Dự kiến thay thế') AND b.TrangThai IN(N'Đã xác nhận',N'Chờ xác nhận',N'Dự kiến thay thế')
AND ca.GioKetThuc>ca.GioBatDau AND cb.GioKetThuc>cb.GioBatDau AND ca.GioBatDau<cb.GioKetThuc AND cb.GioBatDau<ca.GioKetThuc;

INSERT @Findings
SELECT 'ROSTER_DOCTOR_ROOM_OVERLAP','Mismatch',CONCAT(a.MaLLV,N' / ',b.MaLLV),CONCAT(a.MaPhong,N' ',a.NgayLam,N' ',a.MaNV,N' vs ',b.MaNV)
FROM dbo.LICH_LAM_VIEC a JOIN dbo.LICH_LAM_VIEC b ON a.MaLLV<b.MaLLV AND a.MaNV<>b.MaNV AND a.MaPhong=b.MaPhong AND a.NgayLam=b.NgayLam
JOIN dbo.NHAN_VIEN na ON na.MaNV=a.MaNV JOIN dbo.NHAN_VIEN nb ON nb.MaNV=b.MaNV JOIN dbo.CA_LAM ca ON ca.MaCa=a.MaCa JOIN dbo.CA_LAM cb ON cb.MaCa=b.MaCa
WHERE a.TrangThai=N'Đã xác nhận' AND b.TrangThai=N'Đã xác nhận' AND na.MaChucVu=N'CV002' AND nb.MaChucVu=N'CV002'
AND ca.GioKetThuc>ca.GioBatDau AND cb.GioKetThuc>cb.GioBatDau AND ca.GioBatDau<cb.GioKetThuc AND cb.GioBatDau<ca.GioKetThuc;

INSERT @Findings
SELECT 'ROSTER_SPECIALTY_ROOM','Mismatch',l.MaLLV,CONCAT(l.MaNV,N':',k.MaKhoa,N' vs ',l.MaPhong,N':',p.MaKhoa)
FROM dbo.LICH_LAM_VIEC l JOIN dbo.NHAN_VIEN n ON n.MaNV=l.MaNV JOIN dbo.CHUYEN_KHOA k ON k.MaCK=n.MaCK JOIN dbo.PHONG p ON p.MaPhong=l.MaPhong
WHERE l.TrangThai IN(N'Đã xác nhận',N'Chờ xác nhận',N'Dự kiến thay thế') AND n.MaChucVu=N'CV002' AND k.MaKhoa<>p.MaKhoa;

INSERT @Findings
SELECT 'ROSTER_MISSING_DAY_SHIFT','Mismatch',l.MaLLV,CONCAT(l.NgayLam,N'/',l.MaCa)
FROM dbo.LICH_LAM_VIEC l WHERE l.TrangThai NOT IN(N'Hủy',N'Từ chối',N'Nghỉ phép',N'Nghỉ việc')
AND NOT EXISTS(SELECT 1 FROM dbo.CA_THEO_NGAY d WHERE d.NgayLam=l.NgayLam AND d.MaCa=l.MaCa);

INSERT @Findings
SELECT 'ROSTER_APPROVED_LEAVE','Mismatch',l.MaLLV,CONCAT(l.MaNV,N' ',l.NgayLam,N'; ',d.MaDonNghi)
FROM dbo.LICH_LAM_VIEC l JOIN dbo.DON_XIN_NGHI d ON d.MaNV=l.MaNV AND l.NgayLam>=d.TuNgay AND(l.NgayLam<=d.DenNgay OR d.GhiChu LIKE N'%Loại đơn: Nghỉ việc%')
WHERE d.TrangThaiDuyet=N'Đã duyệt' AND l.TrangThai IN(N'Đã xác nhận',N'Chờ xác nhận',N'Dự kiến thay thế');

INSERT @Findings
SELECT 'ROSTER_ATTENDANCE_DATES','Mismatch',l.MaLLV,CONCAT(l.NgayLam,N'; ',l.GioVao,N'/',l.GioRa)
FROM dbo.LICH_LAM_VIEC l WHERE(l.GioVao IS NOT NULL AND CONVERT(date,l.GioVao)<>l.NgayLam) OR(l.GioRa IS NOT NULL AND(l.GioVao IS NULL OR l.GioRa<l.GioVao)) OR l.GioVao>SYSDATETIME() OR l.GioRa>SYSDATETIME();

INSERT @Findings
SELECT 'ROSTER_BEFORE_EMPLOYMENT','Mismatch',l.MaLLV,CONCAT(l.MaNV,N' ',l.NgayLam,N' before ',n.NgayVaoLam)
FROM dbo.LICH_LAM_VIEC l JOIN dbo.NHAN_VIEN n ON n.MaNV=l.MaNV WHERE l.TrangThai NOT IN(N'Hủy',N'Từ chối') AND l.NgayLam<n.NgayVaoLam;

INSERT @Findings
SELECT 'ATTENDANCE_APPROVAL_INCONSISTENT','Mismatch',c.MaCC,CONCAT(l.MaLLV,N' ',c.CongDuocTinh,N'; ',l.GioVao,N'/',l.GioRa,N'; ',l.TrangThai,N'/',l.TrangThaiHienDien)
FROM dbo.CHAM_CONG c JOIN dbo.LICH_LAM_VIEC l ON l.MaLLV=c.MaLLV JOIN dbo.CA_LAM ca ON ca.MaCa=l.MaCa
WHERE c.TrangThai=N'Đã xác nhận' AND(l.NgayLam>@Today OR c.NgayDoiSoat<l.GioRa OR c.NgayDoiSoat>SYSDATETIME()
OR l.TrangThai IN(N'Hủy',N'Từ chối',N'Nghỉ phép',N'Nghỉ việc')
OR(c.CongDuocTinh=0.5 AND(l.GioVao IS NULL OR l.GioRa IS NULL OR l.TrangThaiHienDien<>N'Có mặt'))
OR(c.CongDuocTinh=0 AND(l.GioVao IS NOT NULL OR l.GioRa IS NOT NULL OR l.TrangThaiHienDien<>N'Vắng'))
OR c.SoPhutDiTre<>CASE WHEN DATEDIFF(minute,DATEADD(second,DATEDIFF(second,CONVERT(time,'00:00'),ca.GioBatDau),CONVERT(datetime2,l.NgayLam)),l.GioVao)>0 THEN DATEDIFF(minute,DATEADD(second,DATEDIFF(second,CONVERT(time,'00:00'),ca.GioBatDau),CONVERT(datetime2,l.NgayLam)),l.GioVao) ELSE 0 END);

INSERT @Findings
SELECT 'ATTENDANCE_APPROVED_LEAVE','Mismatch',c.MaCC,CONCAT(l.MaLLV,N' / ',d.MaDonNghi)
FROM dbo.CHAM_CONG c JOIN dbo.LICH_LAM_VIEC l ON l.MaLLV=c.MaLLV JOIN dbo.DON_XIN_NGHI d ON d.MaNV=l.MaNV AND l.NgayLam BETWEEN d.TuNgay AND d.DenNgay
WHERE c.TrangThai=N'Đã xác nhận' AND c.CongDuocTinh>0 AND d.TrangThaiDuyet=N'Đã duyệt';

INSERT @Findings
SELECT 'REPLACEMENT_CONFLICT','Mismatch',p.MaPCTT,CONCAT(p.MaDonNghi,N'/',p.MaLLVThayThe,N'; absent ',d.MaNV,N'; substitute ',l.MaNV,N'; ',l.NgayLam)
FROM dbo.PHAN_CONG_THAY_THE p JOIN dbo.DON_XIN_NGHI d ON d.MaDonNghi=p.MaDonNghi JOIN dbo.LICH_LAM_VIEC l ON l.MaLLV=p.MaLLVThayThe
JOIN dbo.NHAN_VIEN absent ON absent.MaNV=d.MaNV JOIN dbo.NHAN_VIEN sub ON sub.MaNV=l.MaNV
WHERE p.TrangThai=N'Đã xác nhận' AND(d.TrangThaiDuyet<>N'Đã duyệt' OR l.MaNV=d.MaNV OR l.NgayLam<d.TuNgay OR(l.NgayLam>d.DenNgay AND d.GhiChu NOT LIKE N'%Loại đơn: Nghỉ việc%')
OR l.TrangThai NOT IN(N'Đã xác nhận',N'Dự kiến thay thế') OR sub.MaChucVu<>absent.MaChucVu OR ISNULL(sub.MaCK,N'')<>ISNULL(absent.MaCK,N'') OR p.NgayXuLy<d.NgayDuyet);

INSERT @Findings
SELECT 'PAYROLL_V9_APPROVED_SOURCES','Mismatch',b.MaBL,CONCAT(N'Expected attendance ',a.Cong,N', shifts ',a.Shifts,N', commission ',hh.Commission,N'; recorded ',b.CongThucTe,N'/',b.SoCaTinh,N'/',b.TienHoaHong)
FROM dbo.BANG_LUONG b
CROSS APPLY(SELECT COALESCE(SUM(c.CongDuocTinh),0) Cong,COUNT(CASE WHEN c.CongDuocTinh=0.5 THEN 1 END) Shifts,COUNT(CASE WHEN c.CongDuocTinh=0.5 AND c.SoPhutDiTre BETWEEN 16 AND 60 THEN 1 END)*0.25 Late FROM dbo.CHAM_CONG c JOIN dbo.LICH_LAM_VIEC l ON l.MaLLV=c.MaLLV WHERE c.TrangThai=N'Đã xác nhận' AND l.MaNV=b.MaNV AND l.NgayLam BETWEEN b.TuNgay AND b.DenNgay) a
CROSS APPLY(SELECT COALESCE(SUM(h.TienHoaHong),0) Commission FROM dbo.CHI_TIET_HOA_HONG_LUONG h WHERE h.MaBL=b.MaBL) hh
WHERE b.PhienBanTinhLuong=9 AND b.TrangThai=N'Đã chốt' AND(b.SoCaTinh<>a.Shifts OR b.CongThucTe<>a.Cong+b.CongNghiPhep-a.Late OR b.CongTruDiTre<>a.Late OR b.TienHoaHong<>hh.Commission);

INSERT @Findings
SELECT 'PAYROLL_V8_PARTIAL_MONTH','HistoricalBlocker',b.MaBL,CONCAT(b.MaNV,N' ',b.TuNgay,N' to ',b.DenNgay,N'; ',b.TrangThai,N'; V8 overlap blocks October V9 creation by THROW 52333. Preserve historical amounts.')
FROM dbo.BANG_LUONG b WHERE b.PhienBanTinhLuong=8 AND(b.TuNgay<>DATEFROMPARTS(YEAR(b.TuNgay),MONTH(b.TuNgay),1) OR b.DenNgay<>EOMONTH(b.TuNgay));

INSERT @Findings
SELECT 'ASSET_MAINTENANCE_STATE','Mismatch',t.MaTB,CONCAT(t.TinhTrang,N'/',t.TrangThai,N'; ',b.MaBT,N'/',b.TrangThai)
FROM dbo.THIET_BI t LEFT JOIN dbo.BAO_TRI_THIET_BI b ON b.MaTB=t.MaTB AND b.TrangThai IN(N'Chưa xác nhận',N'Đã xác nhận',N'Đang thực hiện')
WHERE(t.TinhTrang=N'Đang bảo trì' AND(b.MaBT IS NULL OR b.TrangThai<>N'Đang thực hiện'))
OR(b.TrangThai=N'Đang thực hiện' AND t.TinhTrang<>N'Đang bảo trì') OR(b.TrangThai IN(N'Chưa xác nhận',N'Đã xác nhận') AND t.TinhTrang<>N'Hư hại')
OR(b.MaBT IS NOT NULL AND(t.NgayThanhLy IS NOT NULL OR t.TrangThai IN(N'Ngưng sử dụng',N'Ngừng sử dụng')));

INSERT @Findings
SELECT 'MAINTENANCE_DATE_CONFLICT','Mismatch',b.MaBT,CONCAT(b.MaTB,N'; purchase ',t.NgayMua,N'; start ',b.NgayBatDau,N'; confirm ',b.NgayXacNhan,N'; execute ',b.NgayThucHien,N'; complete ',b.NgayKetThuc,N'; dispose ',t.NgayThanhLy)
FROM dbo.BAO_TRI_THIET_BI b JOIN dbo.THIET_BI t ON t.MaTB=b.MaTB
WHERE b.NgayBatDau<t.NgayMua OR b.NgayXacNhan<CONVERT(datetime2,b.NgayBatDau) OR b.NgayThucHien<b.NgayXacNhan OR b.NgayKetThuc<b.NgayThucHien OR b.NgayKetThuc<CONVERT(datetime2,b.NgayBatDau)
OR(b.TrangThai=N'Đang thực hiện' AND(b.NgayXacNhan IS NULL OR b.NgayThucHien IS NULL OR b.NgayKetThuc IS NOT NULL))
OR(b.TrangThai=N'Đã hoàn thành' AND b.NgayKetThuc IS NULL)
OR(t.NgayThanhLy IS NOT NULL AND(b.NgayKetThuc>t.NgayThanhLy OR b.NgayBatDau>CONVERT(date,t.NgayThanhLy)))
OR b.NgayThucHien>SYSDATETIME() OR b.NgayKetThuc>SYSDATETIME();

INSERT @Findings
SELECT 'ASSET_DISPOSAL_STATE','Mismatch',t.MaTB,CONCAT(t.NgayThanhLy,N'; ',t.TinhTrang,N'/',t.TrangThai,N'/',t.MaPhong)
FROM dbo.THIET_BI t WHERE t.NgayThanhLy IS NOT NULL AND(t.TinhTrang<>N'Ngưng sử dụng' OR t.TrangThai<>N'Ngưng sử dụng' OR t.MaPhong IS NOT NULL OR t.NgayThanhLy<CONVERT(datetime2,t.NgayMua) OR t.NgayThanhLy>SYSDATETIME() OR NULLIF(LTRIM(RTRIM(t.TenThietBiThanhLy)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(t.TenNhanVienThanhLy)),N'') IS NULL OR t.MaNVThanhLy IS NULL);

INSERT @Findings
SELECT 'ASSET_ROOM_STATUS','Mismatch',t.MaTB,CONCAT(t.TrangThai,N'; room ',t.MaPhong)
FROM dbo.THIET_BI t WHERE(t.TrangThai=N'Đang sử dụng' AND t.MaPhong IS NULL) OR(t.TrangThai=N'Chờ bàn giao' AND t.MaPhong IS NOT NULL);

INSERT @Findings
SELECT 'ASSET_SUBTYPE_DUPLICATE','Mismatch',t.MaTB,CONCAT(t.MaHH,N' is also VAT_TU')
FROM dbo.THIET_BI t WHERE EXISTS(SELECT 1 FROM dbo.VAT_TU v WHERE v.MaHH=t.MaHH);

INSERT @Findings
SELECT 'ROSTER_ACTIVE_REFERENCES','Mismatch',l.MaLLV,CONCAT(l.MaNV,N'/',n.TrangThai,N'; ',l.MaCa,N'/',c.TrangThai,N'; ',l.MaPhong,N'/',p.TrangThai)
FROM dbo.LICH_LAM_VIEC l JOIN dbo.NHAN_VIEN n ON n.MaNV=l.MaNV JOIN dbo.CA_LAM c ON c.MaCa=l.MaCa LEFT JOIN dbo.PHONG p ON p.MaPhong=l.MaPhong
WHERE l.NgayLam>=@Today AND l.TrangThai IN(N'Đã xác nhận',N'Chờ xác nhận',N'Dự kiến thay thế') AND(n.TrangThai<>N'Đang làm việc' OR c.TrangThai<>N'Đang hoạt động' OR p.TrangThai<>N'Đang hoạt động');

/* Missing declared snapshot FKs: existence is reported separately; zero rows is not corruption. */
INSERT @Findings
SELECT 'CHANGE_CONFIRMATION_UNDECLARED_REFERENCE','Mismatch',CONCAT(x.MaDonNghi,N'/',x.MaLich),N'At least one non-NULL snapshot reference cannot resolve'
FROM dbo.XAC_NHAN_DOI_LICH_KHAM x
WHERE(x.MaBSCu IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.NHAN_VIEN n WHERE n.MaNV=x.MaBSCu))
OR(x.MaBSMoi IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.NHAN_VIEN n WHERE n.MaNV=x.MaBSMoi))
OR(x.MaPhongCu IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.PHONG p WHERE p.MaPhong=x.MaPhongCu))
OR(x.MaPhongMoi IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.PHONG p WHERE p.MaPhong=x.MaPhongMoi))
OR(x.MaCa IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.CA_LAM c WHERE c.MaCa=x.MaCa))
OR(x.MaLLVThayThe IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.LICH_LAM_VIEC l WHERE l.MaLLV=x.MaLLVThayThe));

SELECT DB_NAME() AuditDB,@Today AuditDay,SYSDATETIME() ServerNow;
SELECT Class,[Rule],COUNT(*) FindingCount FROM @Findings GROUP BY Class,[Rule] ORDER BY Class,[Rule];
SELECT * FROM @Findings ORDER BY Class,[Rule],RecordId;
SELECT N'MismatchCount' Result,COUNT(*) FindingCount FROM @Findings WHERE Class='Mismatch';
SELECT N'PendingRecordedHours' Result,COUNT(*) ShiftCount FROM dbo.LICH_LAM_VIEC l LEFT JOIN dbo.CHAM_CONG c ON c.MaLLV=l.MaLLV WHERE l.GioVao IS NOT NULL AND l.GioRa IS NOT NULL AND(c.MaCC IS NULL OR c.TrangThai<>N'Đã xác nhận');
SELECT N'IncompleteClockOut' Result,l.MaLLV,l.MaNV,l.NgayLam FROM dbo.LICH_LAM_VIEC l WHERE l.GioVao IS NOT NULL AND l.GioRa IS NULL;
SELECT N'UnconstrainedSnapshotReference' Result,c.name ColumnName,(SELECT COUNT(*) FROM dbo.XAC_NHAN_DOI_LICH_KHAM) SourceRows FROM sys.columns c WHERE c.object_id=OBJECT_ID(N'dbo.XAC_NHAN_DOI_LICH_KHAM') AND c.name IN(N'MaBSCu',N'MaPhongCu',N'MaBSMoi',N'MaPhongMoi',N'MaLLVThayThe',N'MaCa') AND NOT EXISTS(SELECT 1 FROM sys.foreign_key_columns f WHERE f.parent_object_id=c.object_id AND f.parent_column_id=c.column_id);
SELECT N'AssetSourceMatchingDateAndSupplier' Result,t.MaTB,t.MaHH,t.NgayMua,t.MaNCC,p.MaPN,ct.MaCTPN,h.MaHDN
FROM dbo.THIET_BI t JOIN dbo.CHI_TIET_PHIEU_NHAP ct ON ct.MaHH=t.MaHH JOIN dbo.PHIEU_NHAP p ON p.MaPN=ct.MaPN JOIN dbo.HOA_DON_NHAP h ON h.MaHDN=p.MaHDN
WHERE CONVERT(date,p.NgayNhap)=t.NgayMua AND h.MaNCC=t.MaNCC;
SELECT N'InstalledV8PayrollGuard' Result,CASE WHEN definition LIKE N'%old.PhienBanTinhLuong=8%' AND definition LIKE N'%THROW 52333%' THEN 1 ELSE 0 END GuardPresent
FROM sys.sql_modules WHERE object_id=OBJECT_ID(N'dbo.RHM_TinhLuongKy');
