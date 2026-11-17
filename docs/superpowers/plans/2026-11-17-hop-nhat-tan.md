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

- [ ] Ghi nhận diff và hash file F5.
- [ ] Bổ sung ca kiểm chốt lương khi ca chưa ghi nhận/chưa xác nhận; chứng kiến thất bại trên SQL cũ.
- [ ] Gộp thay đổi chốt lương, giữ phần phiên đăng nhập bản gốc.
- [ ] Cài SQL mới vào database riêng và chạy ca kiểm nghiệp vụ.

### Task 2: Hợp nhất nhánh

**Files:** Các file Git báo xung đột, `RangHamMat.Wpf/Presentation/DongBoNhanSu.cs`, giao diện AI/chấm công.

- [ ] Kiểm build nền và lấy nhánh remote mới nhất.
- [ ] Chuyển `tan`, merge nhánh hiện tại và `origin/chamcongmoinhat` theo lịch sử.
- [ ] Xử lý xung đột; giữ thay đổi chức năng của cả hai nhánh.
- [ ] Build và sửa lỗi tích hợp có tái hiện bằng kiểm tra.

### Task 3: Xác minh và bàn giao

**Files:** Bộ kiểm hiện có trong `tools/`, `Test/`, tài liệu kết quả gộp.

- [ ] Build Debug/Release, kiểm nguồn, SQL schema/chấm công/lương, quy trình khám và phiên đăng nhập/đồng bộ phù hợp.
- [ ] Rà soát diff cuối, không còn conflict marker, kiểm ancestor hai nhánh nguồn trong `tan`.
- [ ] Commit kết quả, ghi số kiểm tra và các giới hạn kiểm chứng trong tài liệu bàn giao.

## Nhật ký thực hiện

- F5 SHA256: `E3EB8E3E0715B54C91B4A6D3A54B5FD0E3F677D03A74813B9FDB2E693ED68FDD`.
- Diff F5: thay điều kiện bắt buộc xác nhận toàn bộ ca khi chốt lương; thiếu khối SINGLE ACCOUNT SESSION của bản gốc, không tiếp nhận phần thiếu này.
- Nguồn: `dong_bo_giua_cac_may` tại `4832f86`; `origin/chamcongmoinhat` tại `1f7a406`; `tan` tại `dcf4837`.
- SQL Server kết nối được ngoài sandbox; kiểm SQL dùng quyền nâng để chạy database thử riêng.
