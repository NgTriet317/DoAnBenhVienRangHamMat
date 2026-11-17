# Sửa đồng bộ và phiên đăng nhập

Yêu cầu đã xác định trong hội thoại: nhật ký phải nhận login/logout của máy khác; rà phạm vi đồng bộ các chức năng; lần đăng nhập mới thu hồi phiên trước và hiển thị đúng “tài khoản đã được đăng nhập vào máy khác”. Tiếp tục trên nhánh dong_bo_giua_cac_may, giữ nguyên tan.

- [x] Tái hiện focus ô lọc làm hoãn đồng bộ; phân biệt bộ lọc với dữ liệu nhập chưa lưu, giữ bảo vệ nháp thật.
- [x] Rà Nhật ký: lifecycle, bộ lọc đã áp dụng, dữ liệu ngày/phiên, retry, giữ selection; tái hiện login/logout qua hai kết nối trên database thử.
- [x] Đối chiếu dependency SQL của từng main route/child với adapter, bổ sung bảng thiếu và kiểm thử phạm vi.
- [x] Last-login-wins tại SQL trong giao dịch khóa theo tài khoản; audit thu hồi/login; guard phiên cũ ở các thao tác và heartbeat.
- [x] Bổ sung PHIEN_DANG_NHAP.PhienKeTiep làm dấu phiên đã bị thay thế; index unique phiên đang mở. SQL chính cài lại an toàn, không tự migration khi mở app.
- [x] Watchdog WPF riêng khoảng 2 giây, không bị dirty/CT-disable chặn; forced logout không tự lưu nháp hoặc tự chạy lại giao dịch; quay về đăng nhập với thông báo đúng yêu cầu.
- [x] Ghi nhớ phiên có token; restore atomic chỉ khi token vẫn là phiên mới nhất, không tự cướp phiên sau khi bị đẩy ra.
- [x] SQL tests: tuần tự/đồng thời, tài khoản khác, heartbeat cũ, logout cũ, restore token cũ, audit và dữ liệu không bị ghi bởi sync.
- [x] WPF/runtime và hồi quy; review độc lập, ghi giới hạn nghiệm thu hai máy vật lý; commit trên nhánh hiện tại.

Quyền sở hữu: agent backend giữ DAL/BUS/ET session; agent UI giữ WPF login/lifecycle/remember; agent coverage giữ adapters và nhật ký; root giữ registry/coordinator/SQL chính, tích hợp và kiểm thử toàn bộ. Các phần dùng hợp đồng rõ ràng để không sửa chồng tệp.

- Ruling: SQL chỉ áp dụng lên fixture ở lượt này. Database thật được xác minh chỉ đọc CT=0/snapshot=0; yêu cầu duyệt triển khai đã gửi và chưa được trả lời. Đây là bước deploy còn chờ, không phải mã đã tự cài lên server.
- Ruling: v1 remembered token bị bỏ để yêu cầu nhập mật khẩu1lần; v2 chỉ restore atomic newest-token, không tự reclaim.
- Review: đã xử lý5đườngghi payroll/attendance/policy dùng CreateConnection bỏ qua guard. Các API nay dùng ThucHien và khóa phiên tới commit; SQL tests thực chứng minh blocked stale + fresh success + audit context.
- Verification: build0errors; SingleSession12nhómSQL, SessionWpf12, WpfSync26, Coverage15, RuntimeLogin4, SQLschema21, clinical47; baseline remembered7phaSQL pass.
