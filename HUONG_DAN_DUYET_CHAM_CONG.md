# Hướng dẫn dùng Chấm công – BQL duyệt (bản triển khai 26/10/2026)

## Áp dụng
1. Sao lưu database; chọn DB test trong SSMS (SQL V9 **đã có** `CHAM_CONG`).
2. Chạy `SQL/BO_SUNG_DUYET_CHAM_CONG_V9.sql` để tạo bảng nhật ký và trigger bảo vệ kỳ đã chốt. Không chạy script SQL V9 **cài mới** vào DB đã có dữ liệu.
3. Mở `BenhVienRangHamMat.sln`, cấu hình `RHM_CONNECTION_STRING` tới DB test, Build / Rebuild với Visual Studio 2022 + .NET 8 Windows.
4. Đăng nhập Admin có `PhienDangNhap.MaNV`, chọn tile **Duyệt chấm công** (nhóm Nhân sự), hoặc nút **Chấm công / Đối soát** trong trang Bảng lương.
5. Chọn tháng và năm; bấm **Tải lại**. Chọn ca, chọn **Hoàn tất** (nhập HH:mm giờ vào/ra), **Vắng**, hoặc **Không hợp lệ**; nhập ghi chú rồi **Lưu và duyệt ca**.
6. Có thể chọn nhiều dòng bằng Ctrl rồi bấm **Duyệt các ca đã chọn**. Chỉ áp dụng giờ vào/ra đã có trong lịch, không tự sinh giờ; thất bại một ca thì tất cả không lưu.
7. Trở lại Bảng lương, **Tải số liệu kỳ lương** để xem số ca chưa đối soát được cập nhật. Chưa có chức năng tính lại lương/chốt lương tự động trong bản này.

## Quy tắc bảo vệ
- Chỉ Admin/BQL đã đăng nhập, gắn mã nhân viên mới được duyệt.
- Không duyệt ngày tương lai. Không cho duyệt lịch bị hủy, đã nghỉ, có đơn nghỉ được duyệt, hay thuộc kỳ BANG_LUONG đã chốt.
- Không yêu cầu thời lượng `CA_LAM` bằng 8 giờ. Ca 4 giờ, 6 giờ hay 8 giờ nếu được BQL xác nhận **Hoàn tất** đều ghi nhận 0,5 công thô. Ca qua đêm nhập giờ vào/ra HH:mm và hệ thống tự cộng ngày khi giờ ra nhỏ hơn giờ vào.
- Ca hoàn tất phải có giờ vào/ra hợp lệ và giờ ra không ở tương lai; không đòi ra đúng lúc ca kết thúc. Ca hoàn tất ghi 0,5 công dù thời lượng làm thực tế ngắn. Quy tắc trừ đi trễ 16–60 phút (0,25 công khi tổng hợp bảng lương) được giữ nguyên theo SQL V9.
- Ca vắng không có giờ vào/ra, 0 công. Ca không hợp lệ không cộng công và vẫn nằm trong danh sách cần giải quyết.
- Sửa ca đã duyệt hoặc xóa giờ đã ghi nhận để chuyển sang vắng cần lý do tối thiểu 5 ký tự.
- Mỗi lượt duyệt được lưu `NHAT_KY_DUYET_CHAM_CONG` (thao tác, giờ trước/sau, công trước/sau, BQL và lý do). Nhật ký không cho sửa/xóa.
- Trigger SQL ngăn sửa lịch/chấm công của kỳ lương đã chốt.

## Các truy vấn xác minh
```sql
SELECT TOP (20) MaLLV, TrangThai, SoPhutDiTre, CongDuocTinh, MaNVDoiSoat, NgayDoiSoat
FROM dbo.CHAM_CONG ORDER BY NgayDoiSoat DESC;

SELECT TOP (20) * FROM dbo.NHAT_KY_DUYET_CHAM_CONG ORDER BY MaNhatKy DESC;

SELECT b.MaNV, b.TrangThai, b.CongThucTe, b.TongLuong FROM dbo.BANG_LUONG b
WHERE b.TuNgay='2026-10-01' AND b.DenNgay='2026-10-31';
```

## Kiểm thử đề xuất
- CA01 06:00–14:00, vào 06:00 ra 14:00: công 0,5, trễ 0 phút.
- CA01 06:00–14:00, vào 06:20 ra 14:00: công thô 0,5, trễ 20 phút; lúc tính lương trừ 0,25 công.
- CA01 vào 07:10 ra 14:00: công thô 0,5, ghi nhận trễ 70 phút (trừ đi trễ chỉ áp dụng cho 16–60 phút theo SQL V9).
- CA01 vào 06:00 ra 12:00: được duyệt hoàn tất 0,5 công dù về sớm.
- CA001 (08:00–12:00): được duyệt hoàn tất 0,5 công cho ca 4 giờ.
- Đơn nghỉ đã duyệt/trạng thái hủy, kỳ lương đã chốt, giờ vào/ra ở tương lai: từ chối.
- Duyệt nhóm chứa một ca không hợp lệ: cả giao dịch rollback.
- Hai BQL cùng duyệt: phiên sau bị từ chối do dữ liệu thay đổi.

**Giới hạn kiểm thử tại môi trường tạo file:** Không có SDK .NET và SQL Server; XML XAML, chữ ký sự kiện và ZIP được kiểm tra tĩnh, nhưng cần Build/run và thử transaction trên Windows/SQL Server thực tế trước khi dùng dữ liệu thật.
