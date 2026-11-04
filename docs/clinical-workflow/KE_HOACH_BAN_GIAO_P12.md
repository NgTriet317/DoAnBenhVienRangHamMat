# P12 và bản bàn giao bốn project

Yêu cầu: hoàn tất kế hoạch, giữ RangHamMat.Wpf + ET/DAL/BUS, Views có XAML và code-behind đi kèm, SQL chính cài được schema hiện hành. Thực hiện trực tiếp theo yêu cầu người dùng ngày27/10/2026; đây là mở rộng được cho phép của phạm vi dọn/đồng bộ toàn dự án.

Kiến trúc: giữ .NET8/WPF và nghiệp vụ đã kiểm chứng P01–P11. Trước dọn dùng các project kiểm tra hiện có; sau dọn chỉ bốn csproj. Giữ bằng chứng và hướng dẫn trong docs/clinical-workflow, source kiểm thử cũ truy được ở checkpoint Git. Không thay/xóa database đang dùng hoặc Word gốc. Không bỏ kiểm tra quyền, snapshot, FIFO, idempotency hay trigger để làm xanh.

## Công việc

- [x] 1. Chuẩn hóa bàn giao: QLBenhVienRangHamMat.sql là cài mới52 bảng/version2; giữ migration cho database cũ. Hợp nhất bản SQL cài mới trùng, đối chiếu seed và schema. Cấu hình kết nối: env ưu tiên, connection string trong file cạnh executable, mặc định SQL local; không gắn tên máy. Thử cài mới, chạy lại phải dừng an toàn, nâng cấp hai lần/rollback trước dữ liệu và chặn rollback sau dữ liệu.
- [x] 2. Nghiệm thu: chạy Clinical SQL/WPF, thu/giao/concurrency/ma trận DPI, schema/persistence/tài chính/supplier/maintenance/report/module smoke trên DB mới và nâng cấp. Thêm ca liên tục commit từ khám/đơn tới thu/giao/in đúng mã, không thu/xuất lần hai. Đối chiếu AT01–AT34 bằng bằng chứng thực. Phân biệt kiểm tra nguồn legacy đã lỗi thời với lỗi ứng dụng; sửa lỗi chạy thực trong phạm vi đồng bộ được người dùng mở rộng.
- [x] 3. Backup/recovery: backup COPY_ONLY CHECKSUM bản thử, VERIFYONLY, restore tên mới riêng, DBCC CHECKDB và đối chiếu hash/tổng; dọn đúng DB thuộc lượt này sau lưu log. Không can thiệp DB gốc/bản thử chính.
- [x] 4. Dọn cấu trúc: chuyển9 file .cs phụ trong Views sang Presentation, giữ namespace và các cặp .xaml/.xaml.cs. Solution đúng4 project. Sau kiểm chứng bỏ10 project Checks/Smoke, scripts kiểm tra gọi chúng, artifacts build/preview phát sinh và SQL cài mới trùng; giữ tài liệu gốc có giá trị, gom tài liệu lịch sử vào docs/legacy nếu phù hợp. Không tự xóa .vs đang dùng hoặc dữ liệu cá nhân.
- [x] 5. Kiểm tra bản bàn giao: Debug/Release/publish, đăng nhập SQL và mở module thật, script kiểm tra nhẹ không tạo csproj, README cấu hình/cài mới/nâng cấp/chạy theo chức vụ; cập nhật P12, danh sách file/giới hạn thiết bị thực. Một review toàn thay đổi, một lượt sửa nếu có phát hiện quan trọng; commit cục bộ, không merge/push.

## Chứng minh và thứ tự

Trước thay cấu hình/cài mới có kiểm tra thất bại xác nhận tên máy cũ/schema46/csproj dư/file Views.cs dư. Sau sửa đòi schema52+version2, seeded login hợp lệ, mở25 module, tồn/số tiền khớp; build bốn project và không file phụ Views. Review tập trung cấu hình lộ bí mật, cài lại phá dữ liệu, thiếu source sau move/delete, mất quyền/phiên do cleanup, hướng dẫn trỏ đường dẫn đã xóa. Giới hạn không có máy in vật lý được ghi rõ; không báo đã nghiệm thu thiết bị thật khi chỉ dùng paginator/native-DPI.