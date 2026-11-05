# Sprint 3 – Thanh Tân

**Nguồn đặc tả:** `Sprint3_ThanhTan.docx` trong thư mục gốc.
**Mục tiêu:** danh mục P0/P1, nhật ký SQL thật và quản lý phiên; giữ kiến trúc WPF/BUS/DAL/ET.

## Quyết định sau khảo sát

- Giữ route/class `DichVu`; form chỉnh sửa nằm trong màn hình.
- Chỉ mở whitelist 10 bảng P0/P1; P2 giữ màn hình chuyên biệt có duyệt/version.
- Giữ trạng thái SQL của danh mục lâm sàng, ánh xạ nhãn hiển thị.
- Theo yêu cầu tiếp theo: hợp nhất mọi nâng cấp vào QLBenhVienRangHamMat.sql, giữ phần sửa sẵn của người dùng; không có migration SQL riêng.
- Không commit/push trước kiểm thử và xác nhận như mục PROMPT 5 của đặc tả.
- Làm tại checkout hiện hữu để giữ các thay đổi người dùng; không chuyển nhánh/reset.

## Công việc

- [x] A. Đọc tài liệu, truy vết kiến trúc, xuất ma trận schema/owner thực tế (SQL nếu kết nối được).
- [x] B. Migration idempotent: trạng thái còn thiếu, phiên, cột audit UTC/phiên/event và chỉ mục.
- [x] C. ET/BUS/DAL danh mục: whitelist, validate, FK, concurrency, mềm hóa trạng thái, audit atomic.
- [x] D. WPF danh mục: chọn nhóm, lọc, danh sách, form chung; giữ route.
- [x] E. ET/BUS/DAL nhật ký và phiên: quyền SQL, lọc/phân trang, login/logout, heartbeat.
- [x] F. WPF nhật ký: bộ lọc, hai tab, chi tiết chỉ đọc.
- [x] G. Rà điểm ghi còn lại, selector nghiệp vụ mới và API xóa vật lý.
- [x] H. Build Debug/Release, kiểm thử tự động, SQL và UI; ghi rõ phần chưa chạy.

## Kiểm thử trọng tâm

Tên bảng ngoài whitelist; payload thêm cột; số âm; FK ngừng dùng; khóa ghép; hai người sửa cùng bản ghi; rollback không có log thành công; user giả admin; tài khoản bị khóa; phiên hết hạn; Closing bị hủy; thời gian cũ chưa biết múi giờ; phân trang ổn định.

## Nhật ký triển khai

- Khảo sát: checkout `tan`; có sửa đổi sẵn ở `Database/QLBenhVienRangHamMat.sql` và file kế hoạch chưa tracked.
- SQL ban đầu: `sqlcmd -S . -E -C` và `lpc:.` lỗi SSL/credentials; chưa xác nhận schema live.
- Khám/tiếp nhận/thanh toán đã có audit riêng; cần giữ tính tương thích và không ghi trùng.

- Kết quả: build Debug/Release 0 lỗi; 9 kiểm tra hợp đồng, 84 kiểm tra tích hợp và 47 kiểm tra hồi quy đạt. Chi tiết và phần chưa nghiệm thu: README.md.
- Ruling: không áp migration vào DB chính hoặc bàn giao Git trong đợt triển khai lõi; giữ điều kiện nghiệm thu/xác nhận tại PROMPT 5. Không có reviewer độc lập trong harness.

- Đợt hợp nhất: 93 kiểm tra tích hợp/installer, 47 hồi quy; DB chính đã backup/verify và nâng cấp bằng file SQL duy nhất, 60 bảng cũ giữ nguyên số hàng. Các giới hạn nghiệm thu được cập nhật ở README.md.

- Chốt triển khai: 198 nghiệm thu SQL/WPF/process, 47 khám, 32 công/lương, 58 bảo trì, 9 hợp đồng ET đạt. Backup/RESTORE/CHECKDB fixture đạt; DB chính bản cuối giữ số hàng cả 62 bảng. Biên bản: acceptance-report.md. Chỉ bước bàn giao Git chờ xác nhận PROMPT 5.

- Bàn giao đã được người dùng cho phép: tạo và push nhánh mới chính xác Sprint3_tan. Kiểm tra trước push: Debug/Release và hợp đồng ET đạt; nghiệm thu hiện tại 224 kiểm tra đạt.
