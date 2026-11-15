# Kiểm thử Kiosk AI – dự kiến (chưa đánh dấu Pass)

Dùng database **thử nghiệm**, không sử dụng dữ liệu bệnh nhân thật. Mỗi testcase phải chụp lại kết quả WPF/SQL trước khi ghi Pass.

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AI-01 | Truy cập `http://127.0.0.1:5188` | Trang bắt đầu hiển thị, không yêu cầu tài khoản nhân viên |
| AI-02 | Chưa tích đồng ý, bấm Bắt đầu | Không cho tạo phiên |
| AI-03 | Tích đồng ý, trả lời đủ 8 bước | Hiện tóm tắt và mã `AI-XXXXXXXX`; SQL có phiên `ChoNhanVien` |
| AI-04 | Nhập ngày sinh 31/02/2020 | Từ chối, hỏi lại ngày sinh; không chuyển bước |
| AI-05 | Nhập điện thoại 123 | Từ chối, không ghi số điện thoại sai vào bản nháp |
| AI-06 | Bệnh nhân báo "khó thở" | Phiên `CanHoTro`, hiển thị yêu cầu đến nhân viên y tế ngay |
| AI-07 | Nhập "không khó thở" ở lý do khám | Không kết luận khẩn chỉ vì chuỗi "khó thở" bị phủ định; cần kiểm tra thêm |
| AI-08 | Bấm "Sửa lại" ở màn xác nhận | Quay về bước đầu, dữ liệu nháp trước được xóa để nhập lại |
| AI-09 | Gửi lại đúng bước cũ sau khi đã chuyển bước | API trả 409, không tạo bản khai trùng |
| AI-10 | Làm mới trình duyệt giữa phiên | Khôi phục câu hỏi hiện tại nhờ cookie HttpOnly, không cấp lại tài khoản |
| AI-11 | Nhấn Kết thúc phiên | Trở về chào; cookie bị xóa, phiên chưa gửi chuyển `DaHuy` |
| AI-12 | API chưa chạy hoặc SQL mất kết nối | Trang báo lỗi; không giả vờ tiếp nhận thành công |
| AI-13 | Chạy 07_KioskAI_TiepNhan.sql 2 lần trên DB test | Không tạo trùng bảng/index/procedure |
| AI-14 | WPF `Phiếu hướng dẫn > Nhận bản khai AI` | Chỉ người có quyền tiếp nhận mới đọc hàng chờ |
| AI-15 | Duyệt phiên chưa gửi | Không cho duyệt, DB không ghi `MaPhieu` |
| AI-16 | Tạo phiếu khám khác tên/ngày sinh/SDT | Procedure từ chối liên kết, tránh nhầm bệnh nhân |
| AI-17 | Duyệt hai nhân viên đồng thời cùng phiên | Chỉ một kết quả phê duyệt được ghi |
| AI-18 | Gán một MaPhieu cho 2 phiên khác nhau | Unique filtered index / stored procedure từ chối |
| AI-19 | Hoàn tất phiếu khám thật và liên kết thành công | Mở đúng phiếu trong chức năng A5 hiện có |
| AI-20 | Chạy `EXEC dbo.AI_DonDepPhien` trên DB test | Phiên hết hạn chuyển `HetHan`, dọn dữ liệu chat của phiên cũ theo điều kiện |

## Kiểm tra nhanh SQL sau khi chạy migration

```sql
SELECT name FROM sys.tables WHERE name IN
(N'AI_PHIEN_TIEP_NHAN',N'AI_TIEP_NHAN_NHAP',N'AI_TIN_NHAN',N'AI_SU_KIEN');
SELECT OBJECT_ID(N'dbo.AI_XacNhanPhien',N'P') AS XacNhanProcedure;
SELECT TrangThai,COUNT(*) AS SoPhien FROM dbo.AI_PHIEN_TIEP_NHAN GROUP BY TrangThai;
```
