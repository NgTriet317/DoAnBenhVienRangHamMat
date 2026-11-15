# Merge tan AI Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans; independent SQL and UI integration investigations may use superpowers:dispatching-parallel-agents.

**Goal:** Gộp Sprint3_tan và AI vào tan, sửa lỗi tích hợp và xác minh luồng nghiệp vụ.

**Architecture:** Merge lịch sử, hợp nhất các điểm giao nhau của SQL/ET/DAL/BUS/WPF/Kiosk, chạy kiểm thử trên database cách ly.

**Tech Stack:** .NET 8, WPF, ASP.NET Core, SQL Server, PowerShell.

**Spec:** docs/superpowers/specs/2026-11-15-merge-tan-ai-design.md

## Global Constraints

- Giữ chức năng cả hai nhánh và lịch sử Git; chỉ cập nhật tan.
- Không ghi kiểm thử vào database nghiệp vụ.
- Một schema SQL và cấu hình kết nối nhất quán.

## Review Focus

- Quyền menu khớp quyền BUS/SQL, không mất quyền báo cáo hoặc chức năng AI.
- Bản khai AI được nhân viên xác minh trước khi tạo phiếu, không trùng bệnh nhân/phiếu khi retry.
- Phụ thu khám dịch vụ được tính một lần và chốt cùng hóa đơn.
- SQL cài mới/chạy lại giữ schema, dữ liệu và ràng buộc của cả hai nhánh.
- Build/publish backend dùng assembly mới, đầy đủ cấu hình và dữ liệu ảnh.

## Tasks

- [x] 1. Kiểm tra trạng thái/remote/tổ tiên; chuyển tan, fast-forward Sprint3_tan rồi merge AI không tự commit.
- [x] 2. Giải quyết xung đột SQL, cấu hình, menu/phân quyền và project; ghi lại quyết định tích hợp.
- [x] 3. Build, sửa lỗi có kiểm tra tái hiện; nghiệm thu tổng 32/32 bước đạt, 20/21 tiêu chí và một tiêu chí chờ hạ tầng hai máy.
- [x] 4. Publish/khởi động backend, kiểm hash thư viện và SQL, rà soát độc lập, xử lý góp ý ca đêm và chạy lại kiểm hồi quy.
- [x] 5. Viết báo cáo kiểm chứng và chuẩn bị merge commit trên tan. Thao tác commit/push và hai tổ tiên được xác nhận bằng Git cùng mã commit trong phản hồi cuối.

**Evidence:** docs/integration/HOP_NHAT_TAN_AI_20261115.md
