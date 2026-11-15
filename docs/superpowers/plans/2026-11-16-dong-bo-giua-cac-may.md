# Đồng bộ giữa các máy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Đồng bộ các phân hệ WPF qua SQL mỗi khoảng 2 giây, giữ dữ liệu chưa lưu.

**Architecture:** SQL Change Tracking trả về phiên bản commit và tên bảng thay đổi. Bộ điều phối mỗi phiên gom bảng chờ; các adapter tường minh tải và áp dụng lại dữ liệu khi an toàn. Giữ nguyên các giao dịch ghi BUS/DAL.

**Tech Stack:** .NET 8, WPF, SQL Server 2016 SP1+, SqlClient/Dapper hiện có.

**Spec:** `docs/superpowers/specs/2026-11-16-dong-bo-giua-cac-may-design.md` (đã được duyệt trong hội thoại).

## Global Constraints

- Nhánh `dong_bo_giua_cac_may`; không sửa con trỏ `tan`.
- Chu kỳ mặc định 2 giây, tối đa một lượt; retry mất mạng tối đa 30 giây.
- Không tự ALTER database khi mở ứng dụng. SQL chính có phần cài theo dõi chạy lại an toàn, giữ dấu 7 ngày khi cài mới.
- Không tự lưu/hủy/chốt chứng từ khi đồng bộ; không tải lại form đang sửa hay yêu cầu thu/giao đang chờ.
- Chỉ dữ liệu commit, theo quyền hiện tại; không chia sẻ hội thoại AI giữa máy.

## Review Focus

- Commit xảy ra trong lúc đọc mốc: snapshot + kiểm thử hai kết nối SQL.
- Hoãn cập nhật rồi có thay đổi tiếp: giữ hợp các bảng pending, chỉ xóa phần đã áp dụng.
- Đổi phiên/ngữ cảnh trong khi đang đọc: generation guard và cancellation.
- Form đang nhập nhưng không có interface dirty: chặn bảo thủ ở modal/form và theo dõi thao tác nhập của màn hình riêng.
- Lỗi tải lại: giữ pending, hiển thị lỗi gọn; không lặp MessageBox.

### Task 1: SQL và hợp đồng theo dõi

Files: `ET/DongBoDuLieu.cs`, `DAL/DAL_DONGBO.cs`, `BUS/BUS_DONGBO.cs`, file SQL chính, `Test/SyncChecks`.

Interface: `BUS_DONGBO.DocThayDoiAsync(long? version, CancellationToken)` trả `DotThayDoiDuLieu` gồm Version, DatabaseIdentity, FullRefresh, Tables. Constructor nhận connection string rõ ràng; SELECT đọc trong snapshot, không dùng principal UI trên luồng nền. Cài SQL đủ các bảng có khóa chính; fail rõ nếu thiếu bảng theo dõi.

- [x] Viết và chạy kiểm SQL/contract khi chưa có triển khai.
- [x] Triển khai, chạy kiểm commit/rollback, delete, hết hạn mốc, cấu hình thiếu trên database `RHM_Hospital_Test_...` riêng.
- [x] Kiểm tra SQL chạy lại không thay dữ liệu nghiệp vụ; báo bằng chứng.

### Task 2: Adapter phân hệ tải riêng

Files: `RangHamMat.Wpf/Presentation/DongBo*.cs`, các view tải riêng khi cần.

Interface dùng chung do root tạo: `Services.DongBoManHinh.Register(UserControl, string[] tables, Action refresh, Func<bool>? busy = null)`. Chỉ gọi action khi root đã xác nhận không có modal/dirty/operation. Đăng ký/hủy theo Loaded/Unloaded. Có thể bổ sung interface async chuẩn bị dữ liệu nền. Không reflection, không tạo lại trang.

- [x] Lập ma trận từng view; kiểm chứng hàm đọc không thực hiện ghi nghiệp vụ.
- [x] Kết nối nhân sự, lịch ca, chứng chỉ, chấm công, lương, chính sách, kho, danh mục, thiết bị, nhập xuất, phiếu hướng dẫn, tài khoản, nhật ký, nghỉ việc, tiếp nhận và ảnh.
- [x] Bảo vệ form nháp, giữ bộ lọc/lựa chọn; chạy build và kiểm tra adapter có đủ phạm vi.

### Task 3: Bộ điều phối và WPF

Files: `BUS/TrangThaiDongBo.cs`, `RangHamMat.Wpf/Services/DongBo*.cs`, `MainWindow.DongBo.cs`, `AppWorkspace.xaml.cs`, `Controls/DuLieuGiaoDien.cs`, cấu hình.

- [x] Test pending union, defer, failed reload retry, version reset, no-overlap trước khi triển khai.
- [x] Triển khai vòng theo dõi, trạng thái, generation/cancellation và cấu hình.
- [x] Tách đọc snapshot nền và áp dụng cache Dispatcher; bảo vệ toàn workspace khỏi refresh trong lúc nhập.
- [x] Kết nối adapter tải riêng và các observer lâm sàng; nút tải lại gọi cùng bộ điều phối.
- [x] Kiểm WPF dirty guard với form bệnh nhân, kê đơn, nhân sự, phiếu kho và yêu cầu thanh toán.

### Task 4: Tích hợp, rà soát và bàn giao

- [x] Build solution, chạy SyncChecks và AIIntakeStateChecks, các hồi quy phù hợp.
- [x] Rà soát độc lập toàn diff, sửa phát hiện quan trọng và chạy lại kiểm liên quan.
- [x] Ghi hướng dẫn bật SQL, cấu hình các máy, ma trận phủ và giới hạn bằng chứng hai máy vật lý.
- [x] Commit trên nhánh mới; xác nhận `tan` giữ commit `dcf4837`.

## Ledger

- Setup: branch đã có, baseline build 0 lỗi/133 cảnh báo; AI state checks pass.
- Ruling: triển khai tiếp ngay theo phê duyệt của người dùng, không hỏi lại cách thực thi; các tác vụ độc lập chia theo quyền sở hữu tệp, root tích hợp và kiểm toàn nhánh.

- Task 1 complete: SqlSyncChecks 21/21 trên database thử; schema đầy đủ 74 bảng, fingerprint không đổi khi cài lại tracking.
- Task 2 complete: adapter legacy prepared + dirty guards; ma trận phạm vi do agent ghi riêng. UI smoke 2/2 mở các phân hệ.
- Task 3 complete: SyncChecks 6/6, WpfSyncChecks 25/25, RuntimeSyncChecks 5/5; pending/dirty/tab ẩn/modal và cache snapshot đã kiểm.
- Task 4 verification: hồi quy lâm sàng 47/47; AI state pass; review độc lập có sửa các finding important. Chưa kiểm hai máy vật lý, không merge/push vào tan theo yêu cầu.
- Ruling: một số trình tra cứu lâm sàng giữ hàm đọc chi tiết hiện có sau snapshot nền để hạn chế thay đổi luồng nghiệp vụ; giới hạn đo trễ được ghi trong bằng chứng, không khẳng định mọi truy vấn cũ đã bất đồng bộ.
