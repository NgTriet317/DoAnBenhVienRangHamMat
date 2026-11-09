# Hợp nhất SQL V9 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Một file Database/QLBenhVienRangHamMat.sql cài mới và đồng bộ được đầy đủ schema mà WPF/BUS/DAL đang sử dụng.

**Architecture:** Giữ nền ClinicalWorkflow/HospitalWorkflow/PrescriptionFlow hiện hành; thêm pha V9 có giao dịch và cờ thành công. Đối chiếu V9 theo đối tượng, không nối phần dữ liệu demo hoặc ghi đè các quy tắc lâm sàng mới bằng phiên bản cũ.

**Tech Stack:** SQL Server 2016 SP1+, WPF .NET 8, Microsoft.Data.SqlClient, Dapper, PowerShell/sqlcmd.

**Spec:** Yêu cầu người dùng ngày 29/10/2026: so sánh SQL_V9_CAI_MOI_TEST_F5.sql, bổ sung các phần thiếu vào QLBenhVienRangHamMat.sql và kiểm tra liên kết toàn dự án. Người dùng đã yêu cầu thực hiện sau khi đối chiếu.

## Global Constraints

- Chỉ một file SQL chính cần chạy trên máy mới; không phụ thuộc script rời hoặc dữ liệu demo.
- Giữ dữ liệu, mật khẩu, giá và quy trình khám/thu tiền hiện hành.
- Không thay đổi ngầm tiền lương lịch sử hoặc tự tạo danh tính duyệt/chốt.
- Cài mới và chạy lại phải kiểm chứng trên database thử riêng; không ghi database đang dùng trong lúc phát triển.
- Giữ nguyên file V9 người dùng cung cấp để đối chiếu.

## Review Focus

- Chạy lại khi đã có lương V9 phải giữ số liệu và khôi phục đúng trigger V9.
- Database cũ có lương đã lưu không được tự đổi công thức làm lệch tiền.
- Lỗi ở batch trước không được mở pha ghi V9 phía sau khi chạy F5 trong SSMS.
- Các ràng buộc/index/trigger phải active/trusted; kiểm bàn giao phải bắt đối tượng bị thiếu.
- Tính/chốt lương phải giữ khóa kỳ, nguồn hoa hồng, ngày +5 và kiểm bản số liệu đã xem.

### Task 1: Đối chiếu và kiểm tra hồi quy

**Files:** tools/KiemTraDongBoSql.ps1, tools/SqlSyncChecks.cs.txt; tài liệu đối chiếu.

**Interfaces:** nhận tên SQL Server/database thử; trả exit code khác 0 khi còn lệch schema hoặc nghiệp vụ.

- [x] Liệt kê các bảng/cột/view/trigger/procedure V9 còn thiếu và quy tắc xung đột.
- [x] Viết và chạy kiểm tra trên database cũ: ET, DAL lịch khám, chấm công, lương phải tái hiện lỗi.
- [x] Hoàn thiện fixture giao dịch rollback cho ca ngắn, tính/chốt lương và khóa chứng từ.

### Task 2: Hợp nhất file SQL chính

**Files:** Database/QLBenhVienRangHamMat.sql.

**Interfaces:** giữ các thủ tục kiểm cũ; bổ sung kiểm tra đồng bộ V9. Kết quả cài mới là 60 bảng.

- [x] Thêm hai cột lịch khám, bảy bảng và các cột/chỉ số/khóa/ràng buộc lương.
- [x] Tích hợp view, trigger và hai thủ tục V9, giữ quy tắc hóa đơn/đơn thuốc hiện hành.
- [x] Thêm kiểm tra bảo toàn tiền lương cũ trước khi đổi computed columns.
- [x] Cài mới, chạy lại, nâng cấp bản 53 bảng và xác minh lỗi không ghi tiếp pha sau.

### Task 3: Bàn giao và kiểm chứng

**Files:** tools/KiemTraBanGiao.ps1, tools/HospitalWorkflowChecks.cs.txt, README.md, tài liệu đối chiếu.

**Interfaces:** KiemTra.bat kiểm đủ schema hợp nhất và báo lỗi nếu thiếu V9; hướng dẫn chỉ trỏ file SQL chính.

- [x] Cập nhật số bảng và kiểm bàn giao; bổ sung kiểm chức năng chấm công phù hợp.
- [x] Chạy kiểm đồng bộ SQL, bộ kiểm quy trình khám/thu tiền, build Debug/Release.
- [x] Kiểm tra độc lập thay đổi, xử lý phát hiện và ghi giới hạn cài trên máy mới.

## Execution ledger

- Làm trên nhánh hiện tại `tan` theo yêu cầu sửa trực tiếp file của người dùng; không tạo worktree/commit không được yêu cầu.
- Skill dispatching-parallel-agents: hai phần độc lập là đối chiếu chỉ đọc và xây dựng bộ kiểm được giao riêng; SQL chính do tác giả chính thực hiện.
- RED đã xác minh: 0/5 phép kiểm schema đạt trên database RHM_Hospital_Test_Audit_20261029; lỗi đúng các thành phần thiếu.
- Ruling: bảo toàn tiền lương bằng PhienBanTinhLuong=8 và hai bản tiền trước đồng bộ — database hiện tại có phiếu đã chốt nên không được tự áp công thức mới hoặc bịa người chốt. Chỉ Tính lương chủ động chuyển nháp sang9; phiếu cũ chồng một phần tháng cần đối chiếu để tránh chi trùng.
- Ruling: giữ V9 gốc, không nhập dữ liệu demo/nhật ký phê duyệt/tồn kho/thu tiền mẫu; không áp lịch nghỉ mẫu cho database sử dụng.
- Final review:3 Important đã sửa, RED27 đạt/5 lỗi tái hiện; GREEN32/32. Minor về kỳ in/chi tiết và thông báo đã xử lý bằng ngày thực tế và thông báo đối chiếu kỳ cũ.
- Task1/2/3 complete: bản cài mới cuối32/32 SQL,47/47 quy trình,2/2 kiểm giao diện (27 phân hệ), build Debug/Release; cài lại/nâng cấp/guard lỗi đều đạt. Database sử dụng chỉ được đọc đối chiếu; không áp schema thật trong tác vụ hợp nhất file.
