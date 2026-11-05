# Biên bản nghiệm thu Sprint 3 – Thanh Tân

## Điều chỉnh phạm vi nhật ký

Yêu cầu mới thay thế phần nhật ký xem/tra cứu và sự kiện phiên kỹ thuật: chỉ ghi các thao tác sửa đổi dữ liệu và đăng nhập thành công/thất bại/đăng xuất. Đã bỏ lời gọi audit tại các đường đọc hồ sơ, trang bệnh nhân, hàng đợi, dịch vụ, thuốc, tính phí và in hóa đơn; không phát SESSION_ABNORMAL_END. Lọc lịch sử kỹ thuật/tra cứu ngay tại SQL và KPI, giữ dữ liệu cũ. Không còn tab phiên trên UI nhật ký. Bộ kiểm chứng hiện tại: **210** kiểm tra đạt tại evidence/operation-scope-final.log.

## Kết quả

Phần triển khai chức năng và cài đặt database trong kế hoạch đã hoàn tất tại workspace hiện tại. Không còn ca FAIL mở trong các bộ kiểm thử đã chạy. Người dùng đã xác nhận bàn giao bằng yêu cầu đẩy toàn dự án lên nhánh mới Sprint3_tan; kết quả bàn giao được ghi trong README.md.

Database sản phẩm chỉ có **Database/QLBenhVienRangHamMat.sql**. Database chính đã được backup COPY_ONLY/CHECKSUM, VERIFYONLY rồi cập nhật bằng chính file này; đối chiếu bản cuối giữ nguyên số hàng cả 62 bảng. Không chèn fixture vào database chính.

## Ma trận yêu cầu

| ID | Kết quả thực thi |
|---|---|
| CAT-01 | PASS: P0/P1 đúng bảng/FK, PHONG, SQL reload và bộ chọn |
| CAT-02 | PASS: thêm cả 10 danh mục qua BUS/DAL và nút Lưu WPF, đọc lại SQL |
| CAT-03 | PASS: dữ liệu trùng cả 10 danh mục, thiếu trường, số âm, FK cha ngừng dùng; không có log thành công; lỗi hiển thị tại ô nhập |
| CAT-04 | PASS: sửa qua form cả 10 danh mục; hai tiến trình đồng thời cho đúng một COMMIT, một CONFLICT |
| CAT-05 | PASS: chặn API xóa vật lý cũ; ngừng dùng bằng nút WPF cả 10 danh mục, hàng SQL còn nguyên |
| CAT-06 | PASS: khôi phục bằng nút WPF cả 10 danh mục; từ chối khôi phục khi cha ngừng dùng và ngừng cha còn con hoạt động |
| LOG-01 | PASS: login WPF thực, logout đúng một lần, Closing bị hủy; LOGIN_FAILED tài khoản thật có giới hạn một lần/phút, không ghi mật khẩu |
| LOG-02 | PASS: MaTK/MaPhien/UTC/event, danh mục audit atomic, 30 trigger tích hợp; ghi tên trường đổi, không ghi giá trị cá nhân/bí mật |
| LOG-03 | PASS: rollback hủy cả mutation và audit; dữ liệu sai không sinh log thành công |
| LOG-04 | PASS: hai tiến trình độc lập, một tiến trình chết không đóng phiên còn lại; tiến trình sống logout bình thường |
| LOG-05 | PASS: kill tiến trình fixture thật; đối soát kết thúc bất thường, DangXuatUtc NULL. Timeout được tăng tốc bằng chỉnh heartbeat trong DB test |
| LOG-06 | PASS: user giả admin bị từ chối; tài khoản bị khóa bị từ chối; User gọi thẳng SQL sửa Vai trò bị rollback với lỗi 53023 |
| LOG-07 | PASS: SQL lọc và phân trang ổn định; UI danh sách sự kiện, bộ lọc ngày/giờ, chi tiết chỉ đọc |
| SQL-01 | PASS: cài mới bằng file SQL chính; chạy lại không đổi số hàng |
| SQL-02 | PASS: nâng cấp DB chính có dữ liệu, giữ nguyên số hàng; chạy lặp; backup fixture, RESTORE DB cách ly, CHECKDB và đối chiếu số hàng |
| REG-01 | PASS: DAL cũ từ chối FK cha ngừng dùng; bộ chọn giữ liên kết cũ, lọc cho tạo mới; hồi quy khám, tiếp nhận, thuốc, kho, công/lương, bảo trì |
| REL-01 | Build và kiểm Git diff PASS. Người dùng đã chỉ định bàn giao Git lên Sprint3_tan; xem README.md cho nhánh bàn giao |

## Bằng chứng

- `evidence/acceptance-final.log`: **198** kiểm tra installer/SQL/WPF/nhiều tiến trình đạt.
- `evidence/unified-regression.log`: **47** hồi quy khám/đơn thuốc đạt.
- `evidence/payroll-regression-final.log`: **32** hồi quy SQL công/lương đạt.
- `evidence/maintenance-regression-final.log`: **58** hồi quy bảo trì/kho/serial đạt.
- `evidence/build-and-contracts.log`: **9** kiểm tra hợp đồng ET đạt, build Debug và Release không lỗi. Còn cảnh báo hiện hữu/NuGet vulnerability feed không truy cập được.
- `evidence/restore-final.log`: backup CHECKSUM, RESTORE DB cách ly, CHECKDB và so sánh số hàng đạt.
- `evidence/main-final-backup.log`, `main-final-preservation.txt`: backup trước cập nhật, số hàng 62 bảng DB chính giữ nguyên.
- `evidence/catalog-*.png`, `audit-*.png`: màn hình SQL thật trên fixture 1280×850 và 1024×768.

## Phạm vi kết luận

Hai tiến trình cùng máy kiểm chứng cạnh tranh SQL và độc lập phiên; chưa chạy trên hai máy vật lý hoặc cắt điện thật. SQL chạy trên instance cục bộ, chưa có instance SQL Server 2016 SP1 riêng để chạy lại. Có tự rà mã và test hồi quy; môi trường không cung cấp reviewer độc lập. Không coi các giới hạn môi trường này là đã được kiểm thử.

Trigger 30 bảng được kiểm tra đã cài và thực thi tiêu biểu Nhân sự/Tài khoản/Kho, cùng các hồi quy chuyên biệt; không tuyên bố đã bấm mọi màn hình và mọi tổ hợp thao tác của cả 30 bảng.

P2 tiếp tục dùng cơ chế duyệt/phiên bản ở màn hình chuyên biệt như đặc tả; không chuyển thành CRUD tùy ý. Đăng nhập sai không xác định tài khoản không tạo log có MaTK giả; giữ phản hồi lỗi chung để không lộ tài khoản.

## Chạy lại

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraSprint3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraSprint3Sql.ps1 -CaiDat
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraKhoiPhucSprint3.ps1
```

Các script kiểm thử giới hạn database có tiền tố fixture; backup/data/log của SQL Server nằm ngoài dự án. Không có file database SQL sản phẩm thứ hai.
