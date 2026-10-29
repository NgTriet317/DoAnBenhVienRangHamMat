# Phục hồi dữ liệu và một database Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Phục hồi dữ liệu trước khi database bị xóa, đồng bộ schema hiện tại, đóng gói dữ liệu vào QLBenhVienRangHamMat.sql để cài mới bằng SSMS/F5, giữ dữ liệu khi chạy lại và cuối cùng chỉ có một database ứng dụng.

**Architecture:** Sao lưu database hiện tại; phục hồi backup15:32 ngày29/10 vào một database làm việc tạm. Đồng bộ bản đó, xuất bootstrap SQL đầy đủ schema/dữ liệu vào đầu file chính; bootstrap chỉ chạy khi chưa có bảng. Giữ các pha nâng cấp cho database đã tồn tại, bỏ dọn DEMO tự động. Kiểm cài mới/chạy lại, phục hồi database chính và dọn database tạm.

**Tech Stack:** SQL Server2016SP1+, .NET8/SqlClient, PowerShell/sqlcmd, WPF hiện tại.

**Spec:** Yêu cầu người dùng ngày30/10: F5 có dữ liệu trước đó, ứng dụng đồng bộ, không sinh ba database kiểm thử; chỉ QLBenhVienRangHamMat là database ứng dụng cuối cùng.

## Global Constraints

- Không bịa lại dữ liệu nghiệp vụ, không hứa dữ liệu sau mốc backup15:32 còn nguyên khi chưa có nguồn.
- Backup hiện trạng trước phục hồi: QLBenhVienRangHamMat_before_recovery_20261030_37118B95C1984FE38948F3CC24C20320.bak trong SQL Backup mặc định.
- Backup cứu dữ liệu: QLBenhVienRangHamMat_before_prescription_flow1_20261029_153231_9326f297db1344bf96bbdb71e64194fe.bak, đã VERIFYONLY hợp lệ.
- Source khôi phục có53 bảng:11 bệnh nhân,10 nhân viên/thiết bị/bảo trì/phiếu lương/tài khoản,11 hóa đơn.
- Mọi kiểm thử dùng duy nhất database tạm RHM_Hospital_Test_Recovery_20261030; cuối cùng xóa nó. Ba database kiểm trước đã không còn tại lúc tiếp tục.
- File SQL bàn giao chỉ tạo/tác động QLBenhVienRangHamMat, không RESTORE từ đường dẫn máy này, không cần SQLCMD mode, không DROP database, không tự xóa dữ liệu cũ.
- Xuất đầy đủ NULL/Unicode/decimal/date/time/binary, identity, computed, khóa/index/trigger/view/procedure; FK/CHECK phải trusted.
- Bootstrap dữ liệu chỉ khi database không có bảng; chạy lại không ghi đè dữ liệu chỉnh sửa hoặc dữ liệu mới.
- Không commit/push dữ liệu cá nhân lên dịch vụ ngoài.

## Review Focus

- Dữ liệu có tiền và phiếu đã chốt giữ chính xác; nhập snapshot trước trigger, kiểm FK/CHECK sau nhập.
- Không cắt Unicode/string trên4000 ký tự hoặc batch quá1000 dòng; giữ identity và computed.
- Lỗi bootstrap rollback toàn bộ và các pha sau không tiếp tục ghi.
- So sánh fingerprint từng bảng trước/sau cài mới/chạy lại và trước thay database chính.
- Dọn đúng database tạm thuộc tác vụ, giữ backup đã xác minh và các database hệ thống.

### Task1: Phục hồi nguồn và giữ dữ liệu

- [x] Backup database hiện tại; kiểm backup gốc; phục hồi vào database tạm.
- [x] Kiểm đếm dữ liệu nguồn, không in hồ sơ cá nhân ra log.
- [x] Bỏ dọn DEMO tự động, đồng bộ bản phục hồi lên schema60 bảng, ghi số liệu trước/sau.

### Task2: Đóng gói SQL có dữ liệu

**Files:** tools/XuatDuLieuSql.ps1, tools/SqlSnapshotExporter.cs.txt, Database/QLBenhVienRangHamMat.sql.

- [x] Xuất bootstrap schema/dữ liệu và manifest fingerprints từ nguồn đã đồng bộ.
- [x] Ghép vào file chính; chỉ một CREATE DATABASE đích; thêm guard cài mới/rerun.
- [x] Kiểm cài từ không có database và chạy lại không mất/ghi đè dữ liệu.

### Task3: Phục hồi chính và bàn giao

- [x] Phục hồi database chính từ nguồn đã kiểm, áp file chính, đối chiếu toàn bộ dữ liệu và schema.
- [x] Kiểm kết nối ứng dụng, build, đọc27 phân hệ và các nghiệp vụ phù hợp dữ liệu có sẵn.
- [x] Dọn database tạm, xác minh chỉ còn database ứng dụng chính; cập nhật hướng dẫn/snapshot thời điểm.

## Ledger

- Tiếp tục yêu cầu người dùng, không mở luồng xin phép lặp lại; phục hồi/đóng gói đã được yêu cầu trực tiếp.
- Agent xuất snapshot không hoạt động do giới hạn sử dụng; tác giả chính tiếp tục triển khai và tự rà soát.
- Database chính hiện tại có0 bệnh nhân/bảo trì/hóa đơn; bản backup hiện trạng được giữ để quay lại nếu cần.

- Xác minh nguồn đã đồng bộ:60 bảng/1.410 dòng. Cài mới và chạy lại giữ dòng mới/nội dung sửa; SHA256 mọi bảng khớp manifest.
- Phục hồi chính đã xong; backup hiện trạng lần cuối được VERIFYONLY trước RESTORE. Chạy SQL chính và SHA25660 bảng đều đạt.
- Kiểm bảo trì58/58; V9 lương/chấm công32/32 trên dữ liệu phục hồi; giao diện chính đăng nhập Admin và mở27 phân hệ2/2.
- Ruling: SQL Server có database bài tập khác; chỉ dọn database thử thuộc dự án để giữ nguyên dữ liệu ngoài phạm vi.
- Ruling: Bộ fixture khám yêu cầu nền trống; chỉ bỏ snapshot trong bản sao tạo fixture qua -TaoDatabaseKiemThu, không thay SQL cài mới sản phẩm.

- Final review: reviewer độc lập review_recovery_final không có Critical; Important nguồn xuất thiếu schema được tái hiện RED (nguồn thiếu validator vẫn xuất), sửa preflight schema/60 bảng và kiểm GREEN (từ chối; SQL chính byte-for-byte không đổi).
- Final: minor (deferred): chưa giữ riêng identity last_value đã tiêu hao vì rollback/xóa. Không ảnh hưởng snapshot hiện tại (identity duy nhất trống/last_value NULL); giới hạn và cách dùng .bak đã ghi trong hướng dẫn.
- Kiểm giao diện tạo8 audit rows: giữ nguyên chúng.59 bảng khác không đổi; snapshot cuối1.418 dòng. Manifest gốc vẫn giữ riêng, manifest cuối final-manifest.json.
- Final tests: fresh install60 hashes PASS; rerun giữ dòng mới/nội dung sửa và60 hashes PASS; main apply +60 hashes PASS; failure-continue rollback0 bảng PASS; clinical47/47; UI27 modules2/2; maintenance58/58; V9 32/32; Debug/Release build exit0; structural/schema check PASS.
- Dọn đúng RHM_Hospital_Test_Recovery_20261030. Kiểm sys.databases tên QLBenhVienRangHamMat hoặc RHM_* chỉ còn QLBenhVienRangHamMat ONLINE. Các DB bài tập khác giữ nguyên.
- Không commit/push. SQL tự chứa, không cần đường dẫn .bak khi chạy trên máy mới.
