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

- Triển khai tiếp nối: người dùng đã duyệt “Hãy triển khai giúp tôi”. Đã backup/verify và cài installer riêng lên database thật: 74/74 tracked, snapshot=1, cột/index phiên đủ; số bản ghi nghiệp vụ kiểm trước/sau không đổi. Bản ghi triển khai ở docs/database/SUA_DONG_BO_VA_PHIEN_20261117.md.
- Ruling: v1 remembered token bị bỏ để yêu cầu nhập mật khẩu1lần; v2 chỉ restore atomic newest-token, không tự reclaim.
- Review: đã xử lý5đườngghi payroll/attendance/policy dùng CreateConnection bỏ qua guard. Các API nay dùng ThucHien và khóa phiên tới commit; SQL tests thực chứng minh blocked stale + fresh success + audit context.
- Verification: build0errors; SingleSession12nhómSQL, SessionWpf12, WpfSync26, Coverage15, RuntimeLogin4, SQLschema21, clinical47; baseline remembered7phaSQL pass.

## Bổ sung theo yêu cầu toàn bộ phân hệ

- [x] Danh sách tiếp nhận AI cập nhật độc lập ngay khi modal mở, giữ nháp, khóa duyệt bản khai đã đổi từ xa. Hai kiểm WPF đã quan sát RED rồi GREEN.
- [x] Kiểm 26 route chính và 3 tab kho bằng điều hướng/adapter/SQL thật. Phân biệt nội dung tĩnh/cục bộ và mode tạo phiếu cần hoãn.
- [x] AllModulesSyncChecks tái hiện timeout tự chờ khóa phiên khi tải cache ngoài lâm sàng; dùng BUS_NHANVIEN(transaction) cùng giao dịch; chạy lại 29/29 GREEN.
- [x] Kiểm polling SQL thật cho modal AI: bản khai mới và trạng thái thay đổi, giữ đối chiếu, không tự tạo bệnh nhân/phiếu; RuntimeLoginSyncChecks 6/6.
- [x] Review độc lập: không còn Important/Critical. Minor: thông báo chọn lại dòng AI có thể cần chọn dòng khác trước để SelectionChanged chạy; thao tác Làm mới được hỗ trợ và đã ghi rõ trong tài liệu.
- [x] Kiểm hồi quy cuối: WpfSync28, Coverage15, RuntimeLogin6, RuntimeSync5, SessionWpf12, SingleSession12 nhóm SQL, SyncState6, lâm sàng47 đều pass. Publish Release thành công, hash WPF/BUS/DAL/ET/backend khớp, cấu hình2giây; kiểm giao diện bằng DLL PhatHanh2/2 pass. Lưu thay đổi trên nhánh dong_bo_giua_cac_may; tan giữ nguyên dcf4837.
