# Hợp nhất database và nhánh tan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Gộp thay đổi SQL F5, `dong_bo_giua_cac_may` và `chamcongmoinhat` vào `tan`, kiểm chứng build và nghiệp vụ trên database cách ly.

**Architecture:** Giữ SQL chính làm nguồn cài đặt duy nhất. Chỉ tiếp nhận thay đổi chốt lương của F5, bảo toàn SQL phiên đăng nhập và đồng bộ đa máy. Merge Git theo lịch sử, giải quyết các chỗ giao nhau giữa giao diện AI/chấm công và runtime đồng bộ.

**Tech Stack:** .NET 8, WPF, SQL Server, PowerShell, Git.

**Spec:** Yêu cầu trực tiếp của người dùng trong phiên ngày 17/11/2026.

## Global Constraints

- Đích hợp nhất: nhánh `tan`; giữ nguyên hai nhánh nguồn và file F5 người dùng cung cấp.
- Không ghi vào database nghiệp vụ hiện hữu; chỉ tạo database `RHM_Hospital_Test_*` để kiểm tra.
- Không push remote khi chưa có yêu cầu.
- Thực hiện tại checkout hiện tại theo yêu cầu gộp nhánh, không cần tạo worktree khác.

## Review Focus

- Ca quá hạn không chấm công: tính 0 công và vẫn chốt được lương.
- Đơn nghỉ chờ duyệt, trùng phép, chứng từ chưa duyệt: tiếp tục chặn chốt lương.
- Phiên đăng nhập duy nhất và SQL Change Tracking: không mất sau gộp SQL.
- AI và chấm công mới: giữ phân quyền, đồng bộ cửa sổ đang mở và build được.
- SQL chạy lại: không làm mất snapshot hoặc lỗi schema.

### Task 1: Đối chiếu và hợp nhất SQL

**Files:** `Database/QLBenhVienRangHamMat.sql`, `tools/SqlSyncChecks.cs.txt`, tài liệu kết quả gộp trong `docs/database/`.

- [x] Ghi nhận diff và hash file F5.
- [x] Bổ sung ca kiểm chốt lương khi ca chưa ghi nhận/chưa xác nhận; chứng kiến thất bại trên SQL cũ.
- [x] Gộp thay đổi chốt lương, giữ phần phiên đăng nhập bản gốc.
- [x] Cài SQL mới vào database riêng và chạy ca kiểm nghiệp vụ.

### Task 2: Hợp nhất nhánh

**Files:** Các file Git báo xung đột, `RangHamMat.Wpf/Presentation/DongBoNhanSu.cs`, giao diện AI/chấm công.

- [x] Kiểm build nền và lấy nhánh remote mới nhất.
- [x] Chuyển `tan`, merge nhánh hiện tại và `origin/chamcongmoinhat` theo lịch sử.
- [x] Xử lý xung đột; giữ thay đổi chức năng của cả hai nhánh.
- [x] Build và sửa lỗi tích hợp có tái hiện bằng kiểm tra.

### Task 3: Xác minh và bàn giao

**Files:** Bộ kiểm hiện có trong `tools/`, `Test/`, tài liệu kết quả gộp.

- [x] Build Debug/Release, kiểm nguồn, SQL schema/chấm công/lương, quy trình khám và phiên đăng nhập/đồng bộ phù hợp.
- [x] Rà soát diff cuối, không còn conflict marker, kiểm ancestor hai nhánh nguồn trong `tan`.
- [x] Commit kết quả, ghi số kiểm tra và các giới hạn kiểm chứng trong tài liệu bàn giao.

## Nhật ký thực hiện

- F5 SHA256: `E3EB8E3E0715B54C91B4A6D3A54B5FD0E3F677D03A74813B9FDB2E693ED68FDD`.
- Diff F5: thay điều kiện bắt buộc xác nhận toàn bộ ca khi chốt lương; thiếu khối SINGLE ACCOUNT SESSION của bản gốc, không tiếp nhận phần thiếu này.
- Nguồn: `dong_bo_giua_cac_may` tại `4832f86`; `origin/chamcongmoinhat` tại `1f7a406`; `tan` tại `dcf4837`.
- SQL Server kết nối được ngoài sandbox; kiểm SQL dùng quyền nâng để chạy database thử riêng.
- Thực hiện trực tiếp theo kế hoạch; user đã chỉ định hợp nhất cục bộ vào `tan`, không hỏi lại quyết định merge hoặc tạo worktree.
- SQL cũ: 31 đạt/2 lỗi đúng ca thiếu công. SQL F5: 33/33; sau gộp API tự chấm công: 35/35.
- Lỗi guard phiên đã được kiểm trước/sau với public API `chamCong`; bộ SingleSession đạt 12 nhóm kiểm.
- Lỗi nút chốt lương và 3 lỗi AI được kiểm trước/sau trên WPF thật; RuntimeCoverage đạt 20/20.
- Phiếu AI SQL: 5/5, gồm retry khi hết ca bác sĩ, không trùng phiếu, từ chối đổi thông tin và phiên bị thu hồi.
- Rà soát độc lập đã kiểm các bản sửa; bổ sung helper AiIntakeReview vào manifest installer theo phản hồi.
- Các bộ khám 47/47, WPF sync 28/28, cấu hình 11/11, phiên WPF 12/12, runtime sync 5/5, login sync 6/6, điều hướng/tab 29/29 đều đạt.
- Tài liệu kết quả: `docs/database/HOP_NHAT_CHAM_CONG_VA_TAN_20261117.md`; log thực thi tại `obj/MergeTan`.
- Kiểm SQL cài mới/chạy lại giữ nguyên số dòng và constraint hợp lệ; build cuối Debug/Release 0 lỗi, 119 cảnh báo mỗi cấu hình (gồm MSI không được dotnet build).
- File F5 giữ nguyên hash và trạng thái chưa theo dõi; các nhánh nguồn được giữ, không push remote.
