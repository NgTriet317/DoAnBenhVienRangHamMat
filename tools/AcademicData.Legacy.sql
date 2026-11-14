-- Chi dung trong cong cu do an: caller luu ban truoc/sau va khoi phuc trigger trong cung transaction.
-- Chuẩn hóa các hồ sơ/chứng từ mẫu cũ có mã cố định sau khi đã bổ sung danh mục DL26_.
-- Caller phải đặt RHM.AcademicRepair và quản lý giao dịch; các kiểm tra đầu vào dừng khi sai bộ mẫu.
IF @@TRANCOUNT=0 OR COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'RHM.AcademicRepair')),0)<>1
 THROW 59962,N'Legacy academic repair requires authorized caller transaction.',1;
IF (SELECT COUNT(*) FROM dbo.HOA_DON WHERE MaHD IN(N'HD001',N'HD002',N'HD003',N'HD004',N'HD005',N'HD006',N'HD007',N'HD008',N'HD009',N'HD010'))<>10
 THROW 59963,N'Legacy sample anchors do not match; stop instead of guessing.',1;
-- Ánh xạ đúng tình trạng nguồn cho từng lần khám mẫu và ghi rõ lượt không kê thuốc.
DECLARE @Map TABLE(VisitId nvarchar(50),ConditionId nvarchar(50));
INSERT @Map VALUES
 (N'LS001',N'DL26_TT_ORAL_SOFT'),(N'LS002',N'RHM_TT_TMD'),(N'LS003',N'DL26_TT_LIP_CONTACT'),
 (N'LS004',N'RHM_TT_ORAL_ULCER'),(N'LS005',N'RHM_TT_FACIAL_WOUND'),(N'LS006',N'DL26_TT_SOFT_TRAUMA'),
 (N'LS007',N'DL26_TT_SOFT_TRAUMA'),(N'LS008',N'DL26_TT_SKIN_CONTACT'),(N'LS009',N'RHM_TT_FACIAL_WOUND'),
 (N'LS010',N'DL26_TT_JAW_REHAB'),(N'LSKB011',N'RHM_TT_FACIAL_SCAR');
UPDATE t SET MaDMTT=m.ConditionId,LoaiTinhTrang=c.TenTinhTrang,
 GhiChu=CONCAT(t.GhiChu,N' [ĐỒ ÁN 2026] Chuẩn hóa tình huống mẫu, không phải dữ liệu người bệnh thật.')
FROM dbo.TINH_TRANG_BENH t JOIN @Map m ON m.VisitId=t.MaLS JOIN dbo.DM_TINH_TRANG_BENH c ON c.MaDMTT=m.ConditionId;
UPDATE l SET ChanDoan=N'Sẹo vùng hàm mặt sau chấn thương; tái khám phục hồi vận động hàm.',
 TrieuChung=N'Căng tức vùng sẹo hàm mặt, cần đánh giá phục hồi vận động trong tình huống mẫu.'
FROM dbo.LICH_SU_KHAM_BENH l WHERE l.MaLS=N'LSKB011';
UPDATE k SET MaTTBNguon=t.MaTTB FROM dbo.KE_HOACH_DIEU_TRI k JOIN dbo.TINH_TRANG_BENH t ON t.MaLS=k.MaLS JOIN @Map m ON m.VisitId=k.MaLS;
UPDATE l SET KhongKeThuoc=1,LyDoKhongKeThuoc=N'Tình huống đồ án: chỉ theo dõi/chăm sóc, không có chỉ định thuốc trong lượt này.'
FROM dbo.LICH_SU_KHAM_BENH l WHERE l.MaLS IN(N'LS009',N'LS010');

-- Sua moc thoi gian mau nhap nham sang thang 12; ca thuc te cua tinh huong la 10/11.
UPDATE c SET NgayDoiSoat=DATEADD(minute,15,v.GioRa)
FROM dbo.CHAM_CONG c JOIN dbo.LICH_LAM_VIEC v ON v.MaLLV=c.MaLLV
WHERE c.MaLLV=N'LLV011' AND c.NgayDoiSoat>SYSDATETIME() AND v.GioRa IS NOT NULL;
UPDATE n SET NgayThucHien=c.NgayDoiSoat
FROM dbo.NHAT_KY_DUYET_CHAM_CONG n JOIN dbo.CHAM_CONG c ON c.MaLLV=n.MaLLV
WHERE n.MaLLV=N'LLV011' AND n.NgayThucHien>SYSDATETIME();
UPDATE dbo.NHAT_KY_THAO_TAC SET ThoiGian=DATEADD(month,-1,ThoiGian),ThoiGianUtc=DATEADD(month,-1,ThoiGianUtc)
WHERE MaNK IN(N'NKTT010',N'NKTT011') AND ThoiGian>SYSDATETIME();
UPDATE dbo.PHIEN_DANG_NHAP SET DangNhapUtc=DATEADD(month,-1,DangNhapUtc),DangXuatUtc=DATEADD(month,-1,DangXuatUtc),HoatDongCuoiUtc=DATEADD(month,-1,HoatDongCuoiUtc)
WHERE MaPhien='1f1610d0-7a74-44b4-bc78-9a7d3c7f32fd' AND TrangThaiPhien<>N'Đang mở' AND DangNhapUtc>SYSUTCDATETIME();

-- Bổ sung snapshot tên/hàm lượng/đơn vị khi ký; tách đường dùng khỏi hàm lượng ở đơn mẫu.
UPDATE c SET TenThuocChot=h.TenHH,HamLuongChot=t.HamLuong,DonViTinhChot=h.DonViTinh,
 DuongDung=CASE c.MaThuoc WHEN N'TH003' THEN N'Súc miệng' WHEN N'TH006' THEN N'Bôi niêm mạc miệng'
 WHEN N'TH008' THEN N'Rửa ngoài' WHEN N'TH009' THEN N'Bôi ngoài da' ELSE N'Uống' END,
 LieuDung=CASE WHEN c.MaThuoc IN(N'TH003',N'TH006',N'TH008',N'TH009') THEN N'Chỉ định mô phỏng: dùng tại chỗ theo hướng dẫn trong hồ sơ mẫu.'
 ELSE N'Chỉ định mô phỏng: dùng theo liều ghi trong hồ sơ thực hành.' END,
 HuongDan=N'[ĐỒ ÁN] Đơn dùng thực hành phần mềm; không dùng điều trị. Đường dùng và đơn vị được tách riêng với hàm lượng.'
FROM dbo.CHI_TIET_DON_THUOC c JOIN dbo.DON_THUOC d ON d.MaDT=c.MaDT JOIN dbo.THUOC t ON t.MaThuoc=c.MaThuoc JOIN dbo.HANG_HOA h ON h.MaHH=t.MaHH
WHERE d.MaDT IN(N'DT001',N'DT002',N'DT003',N'DT004',N'DT005',N'DT006',N'DT007',N'DT008',N'DT011');

-- Giữ mốc thu từ lịch sử cũ, rồi bổ sung các dòng viện phí còn thiếu từ dịch vụ và thuốc đã xuất.
DECLARE @Invoices TABLE(Id nvarchar(50),Moment datetime2(0));
INSERT @Invoices SELECT h.MaHD,COALESCE((SELECT MAX(t.NgayThanhToan) FROM dbo.THANH_TOAN t WHERE t.MaHD=h.MaHD AND t.TrangThai=N'Đã xác nhận'),DATEADD(minute,5,COALESCE(h.NgayXacNhan,h.NgayLap)))
FROM dbo.HOA_DON h WHERE h.MaHD IN(N'HD001',N'HD002',N'HD003',N'HD004',N'HD005',N'HD006',N'HD007',N'HD008',N'HD009',N'HD010');
INSERT dbo.CHI_TIET_HOA_DON(MaCTHD,MaHD,MaDVThucHien,NoiDung,ThanhTien)
SELECT N'DL26_FIX_'+s.MaDVThucHien,h.MaHD,s.MaDVThucHien,d.TenDV,s.ThanhTien
FROM dbo.HOA_DON h JOIN @Invoices i ON i.Id=h.MaHD JOIN dbo.DICH_VU_THUC_HIEN s ON s.MaLS=h.MaLS AND s.TrangThai=N'Đã xác nhận'
JOIN dbo.DICH_VU d ON d.MaDV=s.MaDV WHERE NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON c WHERE c.MaDVThucHien=s.MaDVThucHien);
INSERT dbo.CHI_TIET_HOA_DON(MaCTHD,MaHD,MaCTPX,NoiDung,DonGia,ThanhTien)
SELECT N'DL26_FIX_'+c.MaCTPX,h.MaHD,c.MaCTPX,hh.TenHH,t.DonGiaBan,ROUND(c.SoLuong*t.DonGiaBan,2)
FROM dbo.HOA_DON h JOIN @Invoices i ON i.Id=h.MaHD JOIN dbo.PHIEU_XUAT x ON x.MaLS=h.MaLS AND x.TrangThai=N'Đã ghi sổ'
JOIN dbo.CHI_TIET_PHIEU_XUAT c ON c.MaPX=x.MaPX JOIN dbo.CHI_TIET_DON_THUOC r ON r.MaCTDT=c.MaCTDT
JOIN dbo.THUOC t ON t.MaThuoc=r.MaThuoc JOIN dbo.HANG_HOA hh ON hh.MaHH=t.MaHH
WHERE NOT EXISTS(SELECT 1 FROM dbo.CHI_TIET_HOA_DON ct WHERE ct.MaCTPX=c.MaCTPX);

-- Thay lich su thu/hoan MO PHONG thanh mot Thu du cua quy trinh hien tai; ban goc duoc luu trong backup va legacy-before.json.
DELETE t FROM dbo.THANH_TOAN t JOIN @Invoices i ON i.Id=t.MaHD;
UPDATE h SET TongTien=(SELECT COALESCE(SUM(c.ThanhTien),0) FROM dbo.CHI_TIET_HOA_DON c WHERE c.MaHD=h.MaHD),
 TrangThai=N'Đã xác nhận',NgayXacNhan=COALESCE(h.NgayXacNhan,DATEADD(minute,1,h.NgayLap)),PhienBanQuyTrinh=2,
 MaYeuCau=CONVERT(nvarchar(36),NEWID()),DauVanTayYeuCau=CONVERT(varchar(64),HASHBYTES('SHA2_256',N'DL26_LEGACY_'+h.MaHD),2)
FROM dbo.HOA_DON h JOIN @Invoices i ON i.Id=h.MaHD;
INSERT dbo.THANH_TOAN(MaTT,MaHD,MaNV,NgayThanhToan,SoTien,LoaiGiaoDich,PhuongThuc,TrangThai,SoTienKhachDua,TienThua,LyDo)
SELECT N'DL26_FIX_TT_'+h.MaHD,h.MaHD,N'NV008',CASE WHEN i.Moment<h.NgayXacNhan THEN DATEADD(minute,5,h.NgayXacNhan) ELSE i.Moment END,
 h.SoTienPhaiThu,N'Thu',N'Tiền mặt',N'Đã xác nhận',h.SoTienPhaiThu,0,N'[ĐỒ ÁN] Thu đủ mô phỏng theo nguồn dịch vụ và thuốc đã có; lịch sử mẫu cũ lưu trong bằng chứng.'
FROM dbo.HOA_DON h JOIN @Invoices i ON i.Id=h.MaHD WHERE h.SoTienPhaiThu>0;

-- Chỉ lập bàn giao còn thiếu khi đơn liên kết đúng một phiếu xuất; không tạo lượt xuất kho mới.
INSERT dbo.BAN_GIAO_THUOC(MaDT,MaHD,MaPX,TrangThai,MaNVGiao,ThoiGianGiao,GhiChu)
SELECT d.MaDT,h.MaHD,MIN(x.MaPX),N'Đã giao',N'NV006',DATEADD(minute,10,MAX(t.NgayThanhToan)),N'[ĐỒ ÁN] Bàn giao lịch sử giả lập; dùng lại phiếu xuất đã ghi sổ, không xuất thêm.'
FROM dbo.DON_THUOC d JOIN dbo.HOA_DON h ON h.MaLS=d.MaLS JOIN @Invoices i ON i.Id=h.MaHD
JOIN dbo.CHI_TIET_DON_THUOC c ON c.MaDT=d.MaDT JOIN dbo.CHI_TIET_PHIEU_XUAT z ON z.MaCTDT=c.MaCTDT
JOIN dbo.PHIEU_XUAT x ON x.MaPX=z.MaPX AND x.MaLS=d.MaLS AND x.TrangThai=N'Đã ghi sổ'
JOIN dbo.THANH_TOAN t ON t.MaHD=h.MaHD
WHERE d.TrangThai=N'Đã xác nhận' AND NOT EXISTS(SELECT 1 FROM dbo.BAN_GIAO_THUOC b WHERE b.MaDT=d.MaDT)
GROUP BY d.MaDT,h.MaHD HAVING MIN(x.MaPX)=MAX(x.MaPX);
UPDATE p SET TrangThai=N'Hoàn tất',GhiChu=CONCAT(p.GhiChu,N' [ĐỒ ÁN] Hoàn tất lịch sử mẫu theo quy trình thống nhất.')
FROM dbo.PHIEU_KHAM_BENH p JOIN dbo.LICH_SU_KHAM_BENH l ON l.MaPhieu=p.MaPhieu JOIN @Map m ON m.VisitId=l.MaLS;
