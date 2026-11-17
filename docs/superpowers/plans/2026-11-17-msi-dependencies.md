# Sửa bộ cài thiếu DLL — Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans inline. Không tự commit/push trong yêu cầu sửa này.

**Goal:** MSI tạo ở RHM/Release, đủ DLL đúng thư mục và chạy được mà không cần .NET hệ thống.

**Architecture:** Thêm backend sau SDK giải quyết trùng; dùng publish profile folder win-x64 có runtime; kiểm chứng đường dẫn MSI và executable giải nén.

**Tech Stack:** .NET 8/WPF, ASP.NET Core, Installer Projects, PowerShell.

**Spec:** docs/superpowers/specs/2026-11-17-msi-dependencies-design.md

## Constraints

- Giữ nguyên LoiCauHinh.jpg do người dùng cung cấp, database và profile người dùng.
- Không gỡ/cài sản phẩm thật hoặc dừng Visual Studio của người dùng.
- Không thêm bí mật AI vào bộ cài; giữ hành vi cấu hình lần đầu.

## Tasks

- [x] Thêm kiểm tra MSI theo đường dẫn, chạy và thấy thiếu DAL ở gốc.
- [x] Sửa thứ tự target publish; xác nhận cả gốc và KioskServer đều có DLL phụ thuộc.
- [x] Profile MSI tự chứa runtime, apphost backend, output Release và metadata phiên bản; cập nhật hướng dẫn.
- [x] Build/publish, so manifest với MSI, giải nén và chạy WPF/backend không dùng dotnet hệ thống.
- [x] Hồi quy cấu hình/phiên/đồng bộ, review độc lập và báo giới hạn.

## Ledger

- Nguyên nhân có bằng chứng: output group chỉ có KioskServer/DAL.dll và KioskServer/System.Configuration.ConfigurationManager.dll; tên DLL đơn lẻ trong bảng MSI che lỗi đường dẫn.
- Ruling: API là build dependency, chạy bằng tiến trình riêng; bỏ đàm phán framework cho reference này và xóa các global property của ứng dụng cha, rồi publish API bằng runtime/self-contained tường minh. Khai báo RuntimeIdentifiers ở cả hai app để restore do IDE không làm mất target win-x64.
- Ruling: WebView2 Content thêm cùng DLL native ở runtimes và RID publish thêm ở gốc; VSD chỉ giữ một placement khi source path trùng. Tắt Content copy riêng trong profile Installer, giữ native asset win-x64 tại gốc; publish thông thường không đổi.
- Ruling: Chạy helper bằng dotnet có thể làm tiến trình con tìm thấy dotnet tại thư mục executable của cha. Đã đổi sang helper apphost self-contained, xác nhận source cũ thất bại dotnet-not-found rồi source mới đạt; không nới điều kiện kiểm tra.
- Ruling: Một lượt build VS gặp cache markup cũ; CLI build đã tái tạo thành công, sau đó bắt buộc VS Rebuild sạch. Bằng chứng cuối là 6 succeeded/0 failed, không dùng lần MSI được tạo khi project WPF build lỗi.
- Complete: full manifest 923 tệp đạt; WPF/API FileVersion1.0.1.0; WPF khởi động/thoát và backend health đều đạt từ payload MSI; 11 cấu hình +12 phiên +6 đồng bộ đạt. Review độc lập cuối không còn phát hiện trong phạm vi.
