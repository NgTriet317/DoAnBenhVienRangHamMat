# Persistent login and logout implementation plan

> **For agentic workers:** Implement inline with superpowers:test-driven-development and superpowers:verification-before-completion.

**Goal:** Đăng xuất trở lại màn hình đăng nhập; đóng ứng dụng giữ tài khoản để mở lại vào màn hình chính.

**Architecture:** Lưu định danh tài khoản và dấu vân tay mật khẩu bằng Windows DPAPI CurrentUser trong LocalAppData. Khi mở lại, đọc tài khoản/vai trò mới nhất từ BUS và bắt đầu một phiên nhật ký mới. App điều phối các cửa sổ bằng OnExplicitShutdown; đóng cửa sổ chính bình thường kết thúc ứng dụng, đăng xuất xóa phiên lưu rồi mở đăng nhập.

**Tech Stack:** WPF, .NET 8, System.Security.Cryptography.ProtectedData, các BUS hiện có.

**Spec:** Yêu cầu trực tiếp của người dùng trong phiên làm việc ngày 2026-11-10.

## Global constraints

- Giữ các thay đổi đang có trong workspace; không tạo commit hoặc sửa dữ liệu nghiệp vụ.
- Không lưu mật khẩu rõ hoặc lấy quyền Admin từ tệp lưu.
- Kiểm tra công việc đang nhập trước khi đăng xuất, tôn trọng hủy thao tác.
- Tài khoản bị khóa/xóa, đổi mật khẩu hoặc định danh nhân viên phải đăng nhập lại.

## Review focus

- Phiên thiếu hoặc hỏng trở về đăng nhập.
- Lỗi kết nối khi khôi phục không cấp quyền từ dữ liệu cũ.
- Lỗi ghi kết thúc nhật ký không giữ heartbeat/định danh tài khoản cũ.
- Đóng đăng nhập sau đăng xuất phải kết thúc ứng dụng.
- Đăng nhập tài khoản khác phải có cửa sổ và cache mới.

### Task 1: Persist and validate login credentials

Files: `RangHamMat.Wpf/KhoPhienDangNhap.cs`, `RangHamMat.Wpf/DichVuDangNhap.cs`, `tools/KiemTraPhienDangNhap.ps1`, `tools/LoginSessionChecks.cs.txt`.

- [x] Write and run checks proving logout is currently unavailable and persistence is missing.
- [x] Implement encrypted atomic persistence, account/password-fingerprint validation, and shared login/restore service.
- [x] Verify real file roundtrip, encryption, malformed content, changed account/password/status, and deletion.

### Task 2: Connect application lifecycle and logout UI

Files: `RangHamMat.Wpf/App.xaml.cs`, `RangHamMat.Wpf/MainWindow.xaml`, `RangHamMat.Wpf/MainWindow.xaml.cs`, `RangHamMat.Wpf/Views/DangNhap.xaml.cs`, `RangHamMat.Wpf/PhienDangNhap.cs`, `RangHamMat.Wpf/PhienNhatKy.cs`.

- [x] Add logout button and preserve draft/closing checks.
- [x] Restore login on startup and reopen a fresh login dialog after logout.
- [x] Clear principal/cache and stop heartbeat even when closing audit encounters an error.
- [x] Run session checks and existing handover/build checks; report environment limitations accurately.

## Execution notes

- Work in the supplied checkout because its uncommitted changes are part of the current application.
- Initial SQLCMD connection to local SQL Server failed with an ODBC encryption/credentials error. Storage/principal/UI checks can run without SQL; attempt SQL integration separately if available.
- Windows DPAPI and SQL integrated authentication work outside the restricted sandbox; no production workaround or weakened encryption was added.
- Verified 14 storage/principal/UI assertions and 37 integration assertions across seven fresh processes on `RHM_Hospital_Test_Login_20261110_143021`. Integration covers normal exit/reopen, fresh audit session, logout, closing owned windows, switching Admin to User, restoring that employee, exit from login, and password change in SQL.
- `tools/KiemTraBanGiao.ps1` passed structure checks and Debug/Release builds with zero errors; existing compiler warnings remain.
- Independent read-only review found no blocking issues. Existing in-flight heartbeat tasks may still display a late error after changing accounts; the timer is stopped and permission checks continue to apply.
