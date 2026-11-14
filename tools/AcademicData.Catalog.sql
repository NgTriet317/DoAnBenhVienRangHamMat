-- Du lieu mo phong do an. Caller so huu giao dich va actor; khong chay rieng tren du lieu van hanh.
-- Bổ sung danh mục nguồn cho AcademicData.Apply: nhà cung cấp/kho, thuốc, vật tư,
-- thiết bị, tình trạng bệnh/dịch vụ, phòng, nhân viên và ca làm mang mã DL26_.
-- Giá, hàm lượng và liên kết nguồn dưới đây phục vụ dữ liệu đồ án; script không tự commit.
IF @@TRANCOUNT=0 THROW 59961,N'Academic data requires a caller-owned transaction.',1;
DECLARE @Note nvarchar(300)=N'[ĐỒ ÁN 2026] Dữ liệu giả lập; giá/lô/serial/chứng từ không phải giao dịch thực tế.';
INSERT dbo.NHA_CUNG_CAP(MaNCC,TenNCC,Email,NguoiLienHe,DiaChi,TrangThai) VALUES
 (N'DL26_NCC_DUOC',N'Công ty Dược phẩm An Tâm (mô phỏng)',N'duoc@example.invalid',N'Bộ phận dược mô phỏng',N'Dữ liệu đồ án',N'Đang hoạt động'),
 (N'DL26_NCC_NHAKHOA',N'Công ty Vật tư Nha khoa Minh An (mô phỏng)',N'nhakhoa@example.invalid',N'Bộ phận vật tư mô phỏng',N'Dữ liệu đồ án',N'Đang hoạt động');
INSERT dbo.LOAI_HANG_HOA(MaLoaiHH,TenLoaiHH,MoTa,TrangThai)
 VALUES(N'DL26_LHH_NHAKHOA',N'Vật liệu nha khoa',@Note,N'Đang hoạt động');
INSERT dbo.KHO(MaKho,TenKho,MaNV,LoaiKho,TrangThai)
 VALUES(N'DL26_KHO_NHAKHOA',N'Kho vật tư nha khoa',N'NV007',N'Vật tư',N'Đang hoạt động');

-- Tạo hàng hóa trước thuốc để giữ liên kết MaHH; chỉ điền giá mẫu cũ ở nơi giá đang bằng 0.
DECLARE @Drugs TABLE(Id nvarchar(50),Name nvarchar(255),Strength nvarchar(100),Price decimal(18,2),GroupId nvarchar(50),Url nvarchar(400));
INSERT @Drugs VALUES
 (N'01',N'Uprofen 400',N'Ibuprofen 400 mg',3000,N'RHM_NSAID',N'https://stellapharm.com/products/uprofen-400/'),
 (N'02',N'Azicine 500',N'Azithromycin 500 mg',18000,N'RHM_KS_MACROLID',N'https://stellapharm.com/products/azicine-500/'),
 (N'03',N'Clarithromycin STELLA 250 mg',N'Clarithromycin 250 mg',12000,N'RHM_KS_MACROLID',N'https://stellapharm.com/products/clarithromycin-stella-250-mg/'),
 (N'04',N'Clindamycin STELLA 150 mg',N'Clindamycin 150 mg',3500,N'RHM_KS_LINCOSAMID',N'https://stellapharm.com/product-category/antibacterials-for-systemic-use/'),
 (N'05',N'Spirastad 3 M.I.U',N'Spiramycin 3 triệu IU',15000,N'RHM_KS_MACROLID',N'https://stellapharm.com/product-category/antibacterials-for-systemic-use/'),
 (N'06',N'Cotrimoxazole 800/160',N'Sulfamethoxazol 800 mg + trimethoprim 160 mg',2500,N'RHM_KS_SULFONAMID',N'https://stellapharm.com/products/cotrimoxazole-800-160/');
INSERT dbo.HANG_HOA(MaHH,TenHH,MaLoaiHH,DonViTinh,MoTa,TrangThai)
 SELECT N'DL26_HH_TH_'+Id,Name,N'LHH_RHM_THUOC',N'Viên',@Note+N' Tên/hàm lượng đối chiếu 12/11/2026: '+Url+N' Giá bán là giá mô phỏng theo viên; không phải giá công bố của nhà sản xuất.',N'Đang hoạt động' FROM @Drugs;
INSERT dbo.THUOC(MaThuoc,MaHH,HamLuong,MucTonToiThieu,DonGiaBan,MaNhomThuoc)
 SELECT N'DL26_TH_'+Id,N'DL26_HH_TH_'+Id,Strength,30,Price,GroupId FROM @Drugs;

DECLARE @Prices TABLE(Id nvarchar(50),Price decimal(18,2));
INSERT @Prices VALUES(N'THRHM041',1200),(N'THRHM042',1500),(N'THRHM043',2500),(N'THRHM044',2000),(N'THRHM045',3000),(N'THRHM046',10000),
 (N'THRHM047',10000),(N'THRHM048',18000),(N'THRHM049',5000),(N'THRHM050',1000),(N'THRHM051',12000),(N'THRHM052',20000);
UPDATE t SET DonGiaBan=p.Price FROM dbo.THUOC t JOIN @Prices p ON p.Id=t.MaThuoc WHERE t.DonGiaBan=0;
UPDATE h SET MoTa=@Note+N' Đơn giá bán hiện tại là giá giả lập phục vụ thực hành, thay trạng thái chưa thiết lập giá trước đây. '+COALESCE(h.MoTa,N'')
 FROM dbo.HANG_HOA h JOIN dbo.THUOC t ON t.MaHH=h.MaHH JOIN @Prices p ON p.Id=t.MaThuoc;

-- Dùng cùng đơn vị cơ sở cho danh mục vật tư và các chứng từ nhập/xuất được tạo sau đó.
DECLARE @Materials TABLE(Id nvarchar(10),Name nvarchar(255),Unit nvarchar(50));
INSERT @Materials VALUES
 (N'01',N'Cuộn bông nha khoa',N'Cuộn'),(N'02',N'Ống hút nước bọt dùng một lần',N'Cái'),
 (N'03',N'Ống hút phẫu thuật vô khuẩn',N'Cái'),(N'04',N'Yếm nha khoa dùng một lần',N'Cái'),
 (N'05',N'Ly súc miệng dùng một lần',N'Cái'),(N'06',N'Kim gây tê nha khoa 30G',N'Cái'),
 (N'07',N'Chỉ khâu nylon 3/0 vô khuẩn',N'Sợi'),(N'08',N'Mũi khoan nha khoa tiệt khuẩn',N'Cái'),
 (N'09',N'Côn giấy nội nha ISO 25',N'Côn'),(N'10',N'Côn gutta-percha ISO 25',N'Côn'),
 (N'11',N'Trâm nội nha K-file ISO 15',N'Cái'),(N'12',N'Trâm nội nha K-file ISO 20',N'Cái'),
 (N'13',N'Xi măng phục hồi GIC GC Fuji IX GP',N'Bộ'),(N'14',N'Vật liệu trám composite quang trùng hợp',N'Tuýp');
INSERT dbo.HANG_HOA(MaHH,TenHH,MaLoaiHH,DonViTinh,MoTa,TrangThai)
 SELECT N'DL26_HH_VT_'+Id,Name,N'DL26_LHH_NHAKHOA',Unit,@Note+N' Đơn vị cơ sở dùng thống nhất ở nhập/xuất. Nguồn nhận diện GIC: https://www.gc.dental/america/products/operatory/glass-ionomer-restoratives ; vật tư khác mô tả theo chủng loại phổ biến.',N'Đang hoạt động' FROM @Materials;
INSERT dbo.VAT_TU(MaVT,MaHH,MucTonToiThieu) SELECT N'DL26_VT_'+Id,N'DL26_HH_VT_'+Id,20 FROM @Materials;

DECLARE @Assets TABLE(Id nvarchar(10),Name nvarchar(255));
INSERT @Assets VALUES
 (N'01',N'Ghế máy nha khoa tích hợp'),(N'02',N'Máy lấy cao răng siêu âm'),(N'03',N'Đèn quang trùng hợp LED'),
 (N'04',N'Máy định vị chóp nội nha'),(N'05',N'Máy nội nha điều khiển mô-men'),(N'06',N'Camera trong miệng'),
 (N'07',N'Máy nén khí nha khoa không dầu'),(N'08',N'Bể rửa dụng cụ siêu âm');
INSERT dbo.HANG_HOA(MaHH,TenHH,MaLoaiHH,DonViTinh,MoTa,TrangThai)
 SELECT N'DL26_HH_TB_'+Id,Name,N'LHH010',N'Cái',@Note+N' Chủng loại thiết bị phổ biến, không gán chứng nhận/model có thật cho serial giả. Nguồn loại máy nội nha/định vị chóp: https://products.coltene.com/EN/US/products/endodontics/endo-motors/endo-motors',N'Đang hoạt động' FROM @Assets;

-- Liên kết tình trạng với dịch vụ gợi ý; lựa chọn thực hiện vẫn đi qua quy trình khám.
DECLARE @Conditions TABLE(Id nvarchar(50),Name nvarchar(255),ServiceId nvarchar(50));
INSERT @Conditions VALUES
 (N'DL26_TT_ORAL_SOFT',N'Viêm mô mềm khoang miệng',N'DV001'),
 (N'DL26_TT_LIP_CONTACT',N'Kích ứng môi do tiếp xúc',N'DV003'),
 (N'DL26_TT_SOFT_TRAUMA',N'Chấn thương phần mềm vùng hàm mặt',N'DV006'),
 (N'DL26_TT_SKIN_CONTACT',N'Kích ứng da vùng mặt do tiếp xúc',N'DV008'),
 (N'DL26_TT_JAW_REHAB',N'Theo dõi phục hồi vận động hàm',N'DV010');
INSERT dbo.DM_TINH_TRANG_BENH(MaDMTT,TenTinhTrang,MoTa,TrangThai)
 SELECT Id,Name,@Note+N' Phân loại tình huống lâm sàng mẫu; không phải mã ICD.',N'Đang áp dụng' FROM @Conditions;
INSERT dbo.DICH_VU_TINH_TRANG(MaDMTT,MaDV,NoiDungDeXuat,ThuTu)
 SELECT Id,ServiceId,N'Lựa chọn dịch vụ cho tình huống mẫu, bác sĩ xác nhận khi thực hành.',1 FROM @Conditions;

DECLARE @Services TABLE(Id nvarchar(50),Name nvarchar(255),CK nvarchar(50),Price decimal(18,2));
INSERT @Services VALUES
 (N'DL26_DV_KHAM_CK002',N'Khám chuyên khoa điều trị bảo tồn',N'CK002',100000),
 (N'DL26_DV_KHAM_CK003',N'Khám chuyên khoa phẫu thuật hàm mặt',N'CK003',150000),
 (N'DL26_DV_KHAM_CK004',N'Khám tư vấn chẩn đoán hình ảnh răng hàm mặt',N'CK004',100000),
 (N'DL26_DV_KHAM_CK007',N'Khám theo dõi phục hồi vận động hàm',N'CK007',100000),
 (N'DL26_DV_FLUOR',N'Bôi fluor dự phòng sâu răng',N'CK002',150000),
 (N'DL26_DV_SEALANT',N'Trám bít hố rãnh dự phòng sâu răng',N'CK002',250000),
 (N'DL26_DV_POLISH',N'Đánh bóng răng sau lấy cao răng',N'CK001',100000),
 (N'DL26_DV_SPLINT',N'Làm máng nhai hỗ trợ rối loạn khớp thái dương hàm',N'CK007',1500000);
INSERT dbo.DICH_VU(MaDV,TenDV,NhomDV,MaCK,DonGia,ThoiGianDuKien,MoTa,NgayApDung,TrangThai)
 SELECT Id,Name,N'Dịch vụ răng hàm mặt',CK,Price,30,@Note+N' Giá và thời lượng là cấu hình mô phỏng; không phải bảng giá của cơ sở y tế thật.',CONVERT(date,'20260101'),N'Đang hoạt động' FROM @Services;
INSERT dbo.DICH_VU_TINH_TRANG(MaDMTT,MaDV,NoiDungDeXuat,ThuTu) VALUES
 (N'REF28_CARIES',N'DL26_DV_FLUOR',N'Bác sĩ lựa chọn theo tình huống dự phòng phù hợp.',80),
 (N'REF28_CARIES',N'DL26_DV_SEALANT',N'Bác sĩ lựa chọn khi có chỉ định dự phòng.',81),
 (N'REF28_TARTAR',N'DL26_DV_POLISH',N'Thực hành bước đánh bóng sau lấy cao răng.',80),
 (N'RHM_TT_TMD',N'DL26_DV_SPLINT',N'Bác sĩ lựa chọn theo đánh giá khớp thái dương hàm.',80);

-- Chuẩn bị phòng và nhân sự để phía C# lập lịch, tạo tài khoản và tiếp nhận theo chuyên khoa.
INSERT dbo.PHONG(MaPhong,MaKhoa,TenPhong,LoaiPhong,TinhTrang,TrangThai) VALUES
 (N'DL26_PH_CK001B',N'KHOA001',N'Phòng khám tổng quát B',N'Phòng khám chuyên khoa',N'Sẵn sàng',N'Đang hoạt động'),
 (N'DL26_PH_CK002',N'KHOA002',N'Phòng khám bảo tồn B',N'Phòng khám chuyên khoa',N'Sẵn sàng',N'Đang hoạt động'),
 (N'DL26_PH_CK003',N'KHOA003',N'Phòng khám phẫu thuật hàm mặt B',N'Phòng khám chuyên khoa',N'Sẵn sàng',N'Đang hoạt động'),
 (N'DL26_PH_CK004',N'KHOA004',N'Phòng khám tư vấn hình ảnh B',N'Phòng khám chuyên khoa',N'Sẵn sàng',N'Đang hoạt động'),
 (N'DL26_PH_CK007',N'KHOA007',N'Phòng khám phục hồi hàm mặt B',N'Phòng khám chuyên khoa',N'Sẵn sàng',N'Đang hoạt động');
INSERT dbo.NHAN_VIEN(MaNV,HoTen,GioiTinh,NgaySinh,MaChucVu,MaCK,BangCap,NgayVaoLam,TrangThai,DiaChi) VALUES
 (N'DL26_NV_CK002',N'Nguyễn Anh Duy',N'Nam','19870415',N'CV002',N'CK002',N'Bác sĩ (nhân sự mô phỏng)','20260101',N'Đang làm việc',N'Nhân sự giả lập đồ án'),
 (N'DL26_NV_CK003',N'Phạm Minh Anh',N'Nữ','19890820',N'CV002',N'CK003',N'Bác sĩ (nhân sự mô phỏng)','20260101',N'Đang làm việc',N'Nhân sự giả lập đồ án'),
 (N'DL26_NV_CK004',N'Võ Thanh Tùng',N'Nam','19850510',N'CV002',N'CK004',N'Bác sĩ (nhân sự mô phỏng)','20260101',N'Đang làm việc',N'Nhân sự giả lập đồ án'),
 (N'DL26_NV_CK007',N'Trần Bảo Châu',N'Nữ','19900322',N'CV002',N'CK007',N'Bác sĩ (nhân sự mô phỏng)','20260101',N'Đang làm việc',N'Nhân sự giả lập đồ án');
INSERT dbo.CA_LAM(MaCa,GioBatDau,GioKetThuc,TrangThai,GhiChu)
 VALUES(N'DL26_CA_SANG','08:00','12:00',N'Đang hoạt động',@Note+N' Ca sáng 0,5 công.'),
 (N'DL26_CA_CHIEU','13:00','17:00',N'Đang hoạt động',@Note+N' Ca chiều 0,5 công.');
