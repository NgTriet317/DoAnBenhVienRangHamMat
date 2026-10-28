-- Danh mục tình trạng thực tế và định hướng tham khảo, không phải hồ sơ người thật.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() IN(N'master',N'model',N'msdb',N'tempdb') THROW 52146,N'Chọn database bệnh viện.',1;
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'RHM.SimpleConditions.Version' AND TRY_CONVERT(int,value)=1) THROW 52146,N'Chạy migration06 trước.',1;
DECLARE @previousActor nvarchar(128)=TRY_CONVERT(nvarchar(128),SESSION_CONTEXT(N'RHM.MaTK'));
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @lock int;
 EXEC @lock=sys.sp_getapplock @Resource=N'RHM:Schema:SimpleConditions',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
 IF @lock<0 THROW 52146,N'Không lấy được khóa nâng cấp.',1;
 IF COL_LENGTH(N'dbo.TINH_TRANG_BENH',N'ViTriRang') IS NOT NULL
     ALTER TABLE dbo.TINH_TRANG_BENH DROP COLUMN ViTriRang;
 DECLARE @ref TABLE(Id nvarchar(50),Name nvarchar(255),PlanText nvarchar(max));
 INSERT @ref VALUES
 (N'REF28_CARIES',N'Sâu răng',N'Khám đánh giá tổn thương và khả năng bảo tồn. Tùy kết quả, bác sĩ cân nhắc fluor cho tổn thương sớm, trám khi có lỗ sâu hoặc đánh giá nội nha khi liên quan tủy. Chưa đủ thông tin để chọn thủ thuật chỉ từ tên bệnh. Nguồn: https://www.nhs.uk/conditions/tooth-decay/'),
 (N'REF28_CAVITY',N'Sâu răng có lỗ',N'Đánh giá độ sâu, tủy và khả năng phục hồi; cân nhắc phục hồi bằng trám nếu phù hợp. Bác sĩ xác định vật liệu và chỉ định sau khám. Nguồn: https://www.nhs.uk/conditions/tooth-decay/'),
 (N'REF28_PULP',N'Viêm tủy răng',N'Khám xác định tình trạng tủy và khả năng giữ răng; đánh giá chỉ định nội nha và phục hồi sau nội nha nếu phù hợp. Bác sĩ quyết định sau khám, không tự chỉ định nhổ răng hoặc thuốc. Nguồn: https://www.nhs.uk/tests-and-treatments/root-canal-treatment/'),
 (N'REF28_GINGIVITIS',N'Viêm nướu',N'Khám mô nha chu, đánh giá mảng bám và cao răng; hướng dẫn vệ sinh, cân nhắc làm sạch chuyên môn và đánh giá lại theo bác sĩ. Nguồn: https://www.nhs.uk/conditions/gum-disease/'),
 (N'REF28_TARTAR',N'Cao răng',N'Đánh giá nướu và nha chu; cân nhắc lấy cao răng, làm sạch mảng bám và hướng dẫn vệ sinh theo kết quả khám. Nguồn: https://www.nhs.uk/live-well/healthy-teeth-and-gums/dental-treatments/'),
 (N'REF28_ORTHO',N'Răng lệch lạc',N'Khám đánh giá khớp cắn và nhu cầu chỉnh nha, trao đổi lựa chọn phù hợp. Chưa xác định khí cụ, số buổi hoặc thời gian điều trị từ tên tình trạng. Nguồn: https://www.nhs.uk/live-well/healthy-teeth-and-gums/dental-treatments/');
 IF EXISTS(SELECT 1 FROM @ref r JOIN dbo.DM_TINH_TRANG_BENH d ON d.MaDMTT=r.Id WHERE d.TenTinhTrang<>r.Name) THROW 52146,N'Mã tham khảo trùng dữ liệu hiện có.',1;
 DECLARE @admin nvarchar(50)=(SELECT TOP(1) t.MaTK FROM dbo.TAI_KHOAN t JOIN dbo.NHAN_VIEN n ON n.MaNV=t.MaNV WHERE t.TenDangNhap=N'admin001' AND t.TrangThai=N'Đang hoạt động' AND n.TrangThai=N'Đang làm việc');
 IF @admin IS NULL THROW 52146,N'Cần tài khoản admin001 hoạt động để ghi nhật ký nạp danh mục.',1;
 EXEC sys.sp_set_session_context @key=N'RHM.MaTK',@value=@admin;
 INSERT dbo.DM_TINH_TRANG_BENH(MaDMTT,TenTinhTrang,MoTa,TrangThai)
 SELECT Id,Name,PlanText,N'Đang áp dụng' FROM @ref r WHERE NOT EXISTS(SELECT 1 FROM dbo.DM_TINH_TRANG_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaDMTT=r.Id);
 EXEC sys.sp_set_session_context @key=N'RHM.MaTK',@value=@previousActor;
 COMMIT;
 PRINT N'Đã bỏ vị trí răng; nạp 6 tình trạng và định hướng có nguồn. Không tạo người thật, bảng giá hoặc chứng từ.';
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 EXEC sys.sp_set_session_context @key=N'RHM.MaTK',@value=@previousActor;
 THROW;
END CATCH;
