# Database Setup After Install Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan inline. Steps use checkbox syntax for tracking.

**Goal:** Lần đầu sau mỗi lần cài MSI bắt buộc xác nhận SQL, những lần tiếp theo tự kết nối.

**Architecture:** MSI quản lý cờ HKCU per-user. Dịch vụ khởi động đọc cờ trước khi gọi khôi phục SQL và chỉ xác nhận sau hộp thoại lưu thành công.

**Tech Stack:** WPF/.NET 8, Microsoft.Win32, Visual Studio Installer Projects.

**Spec:** `docs/superpowers/specs/2026-11-17-database-setup-after-install-design.md`

## Global Constraints

- Không xóa database, hồ sơ hoặc mật khẩu đã mã hóa.
- Giữ nguyên các tệp người dùng hiện có trong `RHM/Release`; tạo bộ cài mới ở đường dẫn khác.
- Thực hiện trực tiếp trên nhánh hiện có; không commit/push/cài-gỡ ứng dụng thật trong phạm vi thay đổi này.
- Không đọc/ghi nhánh registry sản phẩm trong kiểm thử; dùng nhánh tên ngẫu nhiên riêng.

## Review Focus

- Cài lại cùng phiên bản phải đặt lại cờ.
- Biến môi trường/appsettings/profile hợp lệ không được bỏ qua lần xác nhận đầu.
- Hủy hoặc lỗi lưu không được xác nhận cờ.
- Kết nối mất hiệu lực sau xác nhận vẫn phải mở màn hình cấu hình.
- Lỗi đọc/ghi registry phải hiển thị thông báo, không để ứng dụng tiếp tục hoặc văng ngoài xử lý.

## Task 1: Cờ cài đặt và quyết định khởi động

**Files:** Create `RangHamMat.Wpf/DichVuKhoiDongCSDL.cs`, `Test/DatabaseSetupChecks/{DatabaseSetupChecks.csproj,Program.cs}`, `tools/KiemTraCauHinhSauCaiDat.ps1`; modify `RangHamMat.Wpf/App.xaml.cs`, `Views/CauHinhMayChu.xaml.cs`, `RangHamMat.Wpf.csproj`, `RHM/RHM.vdproj`, `README.md`.

**Interfaces:** internal `DichVuKhoiDongCSDL(string? duongDanRegistry = null)`, `Task<bool> CanHienCauHinhAsync(Func<Task<bool>> khoiPhucKetNoi)`, `void XacNhanCauHinh()`.

- [x] Tạo và chạy bộ kiểm tra: thiếu cờ, cờ 1 dù SQL hợp lệ, đã xác nhận và SQL hợp lệ/lỗi, hủy, sai kiểu cờ, cài lại cùng phiên bản, lỗi khôi phục. Expected: thất bại vì thiếu dịch vụ; đã quan sát đúng thất bại.
- [x] Triển khai dịch vụ và App, thêm registry MSI; Expected: các kiểm tra hành vi thành công; đạt 11 ca.
- [x] Build WPF và chạy kiểm tra phiên đăng nhập/đồng bộ không cần SQL. Expected: 0 lỗi mới; đạt 12 ca phiên và 6 ca đồng bộ.
- [x] Build MSI sang đường dẫn mới, kiểm tra Registry table và các file ứng dụng. Expected: cờ DWORD 1 ở HKCU được gói trong MSI; đạt 8 kiểm tra gói.
- [x] Review thay đổi và ghi rõ giới hạn kiểm chứng; cập nhật hướng dẫn đường dẫn bộ cài.

## Ledger

- Đã điều tra: App chỉ mở cấu hình khi tất cả chuỗi kết nối thất bại; appsettings đang chứa kết nối; bộ cài không có lifecycle cho xác nhận SQL.
- Ruling: Yêu cầu và hành vi đã được người dùng xác nhận ở lượt trước; các lựa chọn kỹ thuật nhỏ được thực hiện trong phạm vi đó, không mở thêm vòng xin duyệt.
- Ruling: Kiểm tra MSI phát hiện bộ cài hiện tại không lấy backend Kiosk: SDK gọi `ComputeResolvedFilesToPublishList` trực tiếp trong `PublishItemsOutputGroup`, không gọi `ComputeFilesToPublish`. Chuyển đúng một hook trong csproj để bộ cài mới giữ đủ thành phần ứng dụng; không đổi hành vi nghiệp vụ AI.
- Ruling: Review phát hiện nút tự tìm từng lưu/đóng hộp thoại và tùy chọn Everyone không tương thích cờ HKCU. Tự tìm nay chỉ điền thông tin; ẩn Everyone theo thiết lập per-user vốn có. Giữ nguyên thứ tự nguồn chuỗi kết nối đã được tài liệu hóa.
- Task 1: complete — kiểm tra hành vi 11/11, gói 8/8, hồi quy phiên 12/12 và đồng bộ 6/6; VS build 6 succeeded/0 failed. Chi tiết và giới hạn tại `docs/database/CAU_HINH_SAU_CAI_DAT_20261117.md`.
