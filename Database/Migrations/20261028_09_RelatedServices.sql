-- Nâng cấp sau08. Insert-only: không đặt lại giá/dịch vụ/liên kết đã có.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() IN(N'master',N'model',N'msdb',N'tempdb') THROW 52147,N'Chọn database bệnh viện.',1;
IF COL_LENGTH(N'dbo.TINH_TRANG_BENH',N'ViTriRang') IS NOT NULL OR OBJECT_ID(N'dbo.DM_TINH_TRANG_BENH') IS NULL THROW 52147,N'Chạy migration08 trước.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @lock int;
 EXEC @lock=sys.sp_getapplock @Resource=N'RHM:Schema:RelatedServices',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
 IF @lock<0 THROW 52147,N'Không lấy được khóa nâng cấp.',1;
 IF OBJECT_ID(N'dbo.DICH_VU_TINH_TRANG',N'U') IS NULL
 CREATE TABLE [dbo].[DICH_VU_TINH_TRANG](
  MaDMTT nvarchar(50) NOT NULL,MaDV nvarchar(50) NOT NULL,
  NoiDungDeXuat nvarchar(500) NOT NULL,ThuTu int NOT NULL,
  CONSTRAINT PK_DichVuTinhTrang PRIMARY KEY(MaDMTT,MaDV),
  CONSTRAINT FK_DVTT_TinhTrang FOREIGN KEY(MaDMTT) REFERENCES dbo.DM_TINH_TRANG_BENH(MaDMTT),
  CONSTRAINT FK_DVTT_DichVu FOREIGN KEY(MaDV) REFERENCES dbo.DICH_VU(MaDV),
  CONSTRAINT CK_DVTT_NoiDung CHECK(ThuTu>0 AND LEN(LTRIM(RTRIM(NoiDungDeXuat)))>0)
 );
 DECLARE @source nvarchar(max)=N'Giá dịch vụ tham khảo BVĐK Bình Định, hiệu lực 15/6/2026; số thứ tự trong tên ghi chú. Nguồn: https://benhvienbinhdinh.com.vn/wp-content/uploads/2026/06/BANG-GIA-BENH-VIEN-hieu-luc-ngay-15.6.2026_W.pdf';
 DECLARE @svc TABLE(Id nvarchar(50),Name nvarchar(255),CK nvarchar(50),Price decimal(18,2),Minutes int,STT int);
 INSERT @svc VALUES
 (N'REF_DV_COMPOSITE',N'Điều trị sâu ngà răng phục hồi bằng Composite',N'CK002',560000,30,1730),
 (N'REF_DV_GIC',N'Điều trị sâu ngà răng phục hồi bằng GlassIonomer Cement',N'CK002',360000,30,1731),
 (N'REF_DV_SCALING1',N'Lấy cao răng (1 hàm)',N'CK001',220000,30,1727),
 (N'REF_DV_SCALING2',N'Lấy cao răng (2 hàm)',N'CK001',440000,45,1726),
 (N'REF_DV_ENDO123',N'Điều trị tủy, hàn kín bằng Gutta percha nóng chảy (răng 1, 2, 3)',N'CK002',2110000,60,3984),
 (N'REF_DV_ENDO45',N'Điều trị tủy, hàn kín bằng Gutta percha nóng chảy (răng 4, 5)',N'CK002',2280000,60,3980),
 (N'REF_DV_ENDO67D',N'Điều trị tủy, hàn kín bằng Gutta percha nóng chảy (răng 6, 7 hàm dưới)',N'CK002',2540000,90,3982),
 (N'REF_DV_ENDO67T',N'Điều trị tủy, hàn kín bằng Gutta percha nóng chảy (răng 6, 7 hàm trên)',N'CK002',2690000,90,3986);
 IF EXISTS(SELECT 1 FROM @svc s JOIN dbo.DICH_VU d ON d.MaDV=s.Id WHERE d.TenDV<>s.Name OR d.MaCK<>s.CK) THROW 52147,N'Mã dịch vụ tham khảo trùng dữ liệu khác; cần đối chiếu.',1;
 INSERT dbo.DICH_VU(MaDV,TenDV,NhomDV,MaCK,DonGia,ThoiGianDuKien,MoTa,NgayApDung,TrangThai)
 SELECT s.Id,s.Name,N'Điều trị răng miệng',s.CK,s.Price,s.Minutes,@source+N'; STT '+CONVERT(nvarchar(10),s.STT),CONVERT(date,'20260615'),N'Đang hoạt động'
 FROM @svc s WHERE NOT EXISTS(SELECT 1 FROM dbo.DICH_VU WITH(UPDLOCK,HOLDLOCK) WHERE MaDV=s.Id);
 DECLARE @map TABLE(C nvarchar(50),D nvarchar(50),Note nvarchar(500),Seq int);
 INSERT @map VALUES
 (N'REF28_CARIES',N'REF_DV_COMPOSITE',N'Lựa chọn phục hồi bằng Composite khi bác sĩ xác định có chỉ định trám.',1),
 (N'REF28_CARIES',N'REF_DV_GIC',N'Lựa chọn phục hồi bằng GIC khi bác sĩ xác định phù hợp.',2),
 (N'REF28_CAVITY',N'REF_DV_COMPOSITE',N'Phục hồi lỗ sâu bằng Composite sau đánh giá tủy và khả năng bảo tồn.',1),
 (N'REF28_CAVITY',N'REF_DV_GIC',N'Lựa chọn thay thế bằng GIC theo đánh giá của bác sĩ.',2),
 (N'REF28_PULP',N'REF_DV_ENDO123',N'Lựa chọn nội nha cho răng 1, 2, 3 nếu có chỉ định và giữ được răng.',1),
 (N'REF28_PULP',N'REF_DV_ENDO45',N'Lựa chọn nội nha cho răng 4, 5 nếu có chỉ định.',2),
 (N'REF28_PULP',N'REF_DV_ENDO67D',N'Lựa chọn nội nha cho răng 6, 7 hàm dưới nếu có chỉ định.',3),
 (N'REF28_PULP',N'REF_DV_ENDO67T',N'Lựa chọn nội nha cho răng 6, 7 hàm trên nếu có chỉ định.',4),
 (N'REF28_GINGIVITIS',N'REF_DV_SCALING1',N'Làm sạch cao răng 1 hàm nếu phù hợp sau khám nha chu.',1),
 (N'REF28_GINGIVITIS',N'REF_DV_SCALING2',N'Lựa chọn làm sạch cao răng 2 hàm khi có chỉ định.',2),
 (N'REF28_TARTAR',N'REF_DV_SCALING1',N'Lấy cao răng 1 hàm theo phạm vi cần điều trị.',1),
 (N'REF28_TARTAR',N'REF_DV_SCALING2',N'Lựa chọn lấy cao răng 2 hàm theo phạm vi cần điều trị.',2),
 (N'REF28_ORTHO',N'DV001',N'Khám đánh giá khớp cắn và nhu cầu chỉnh nha trước khi lập liệu trình.',1),
 (N'REF28_ORTHO',N'DV006',N'Chụp hình ảnh hàm mặt chỉ khi bác sĩ thấy cần thiết để đánh giá.',2);
 IF (SELECT COUNT(*) FROM dbo.DM_TINH_TRANG_BENH WHERE MaDMTT IN(SELECT C FROM @map))<>6 THROW 52147,N'Thiếu tình trạng tham khảo từ08.',1;
 INSERT dbo.DICH_VU_TINH_TRANG(MaDMTT,MaDV,NoiDungDeXuat,ThuTu)
 SELECT C,D,Note,Seq FROM @map m WHERE NOT EXISTS(SELECT 1 FROM dbo.DICH_VU_TINH_TRANG WITH(UPDLOCK,HOLDLOCK) WHERE MaDMTT=m.C AND MaDV=m.D);
 IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'RHM.RelatedServices.Version') EXEC sys.sp_addextendedproperty @name=N'RHM.RelatedServices.Version',@value=1;
 COMMIT;
 PRINT N'Đã đồng bộ 8 dịch vụ tham khảo và 14 liên kết; giữ nguyên dữ liệu đã có.';
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
