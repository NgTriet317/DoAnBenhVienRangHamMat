# Compact Installer Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans for inline execution and superpowers:verification-before-completion for the final checks.

**Goal:** Thu gọn payload cài đặt nhưng giữ đủ runtime, SQL, ảnh và Kiosk.

**Architecture:** WPF và API có bundle self-contained riêng, nội dung đọc theo đường dẫn giữ bên ngoài. MSI lấy publish output mới; host hỗ trợ cả executable bundle và DLL phát triển.

**Tech Stack:** .NET 8, WPF, MSBuild, Visual Studio Installer Projects, PowerShell.

**Spec:** `docs/superpowers/specs/2026-11-17-compact-installer-design.md`

## Global Constraints

- Windows x64, .NET 8 self-contained, PublishTrimmed=false.
- Giữ Database/QLBenhVienRangHamMat.sql và mọi tài nguyên runtime ở đường dẫn hiện hành.
- Không sửa SQL/cấu hình người dùng, không phát hành AiSecrets.local.json.
- Chỉ tan chứa thay đổi mới; master giữ bản 8fe4016 trước khi triển khai.

## Review Focus

- Kiosk có exe nhưng không có DLL rời vẫn khởi động; bản phát triển chỉ có DLL vẫn có fallback.
- Backend không bị bundle vào WPF hoặc lẫn runtime cũ từ thư mục trung gian.
- SQL, AI, ảnh và wwwroot giữ nguyên nội dung, native DICOM vẫn hoạt động.
- Visual Studio PublishItems/MSI lấy bundle cuối cùng, không lấy DLL trước bước bundling.
- Nâng cấp MSI giữ UpgradeCode và cờ xác nhận SQL; không cần .NET cài sẵn.

### Task 1: Kiểm payload và hỗ trợ host

**Files:** `Test/InstallerRuntimeChecks/Program.cs`, `RangHamMat.Wpf/Services/KioskApiHost.cs`, `tools/KiemTraBanPhatHanhGon.ps1`.

- [x] Publish API bundle độc lập làm fixture, chạy helper hiện tại và thấy thiếu DLL.
- [x] Sửa host chọn exe trước, chỉ kiểm DLL ở fallback; bổ sung kiểm trang web vào helper.
- [x] Kiểm helper bằng fixture bundle khi PATH không có dotnet.

### Task 2: Profile và MSI

**Files:** `RangHamMat.Wpf/RangHamMat.Wpf.csproj`, `RangHamMat.Wpf/Properties/PublishProfiles/{Installer,FolderProfile}.pubxml`, `RangHamMat.Kiosk.Api/RangHamMat.Kiosk.Api.csproj`, `RHM/RHM.vdproj`, `tools/KiemTraCauHinhSauCaiDat.ps1`.

Quyết định khi nghiệm thu: thêm `Directory.Build.targets` để giữ ngoài bundle đúng DLL mixed-mode `Dicom.Native.dll`; native DICOM RED 4 lỗi/28 ca là bằng chứng cần ngoại lệ này. Thêm tùy chọn `-BanGon` cho bộ kiểm DICOM hiện có để có thể chạy lại hồi quy single-file. Mặc định Configuration của Installer là Release khi Visual Studio chưa gán giá trị để đường dẫn publish nhất quán.

- [x] Kiểm cấu trúc bản cũ thất bại đúng vì còn DLL/runtime rời.
- [x] Bật single-file/native extraction, giữ tài nguyên ngoài và backend riêng, loại PDB rời.
- [x] Tăng version 1.0.2 và IDs nâng cấp, điều chỉnh bộ kiểm manifest cho bundle.
- [x] Publish và Rebuild MSI; đối chiếu đầy đủ manifest/nguồn tài nguyên và giải nén vào obj.

### Task 3: Nghiệm thu và bàn giao

**Files:** `README.md`, `docs/database/BO_CAI_GON_20261117.md`.

- [x] Chạy WPF từ payload MSI, backend health/wwwroot, kiểm native DICOM bằng helper single-file.
- [x] Chạy KiemTraBanGiao.ps1 và DatabaseSetupChecks, ghi số file/dung lượng và giới hạn kiểm thử.
- [x] Review thay đổi cuối, sửa các vấn đề cần thiết, commit và đẩy tan; xác nhận master vẫn là bản trước triển khai.

## Bằng chứng và review

Task 1: RED helper báo thiếu RangHamMat.Kiosk.Api.dll với exe bundle; GREEN health/trang Kiosk không cần dotnet. Task 2: RED checker phát hiện DLL rời và SQL bị bundle; GREEN MSI cuối 87 kiểm tra/32 file, SHA256 khớp payload. Task 3: RED DICOM bundle lỗi 4 codec, GREEN giữ DLL mixed-mode đạt 28/28; script hồi quy single-file 27/27. Build Debug/Release và 11 cấu hình/12 phiên/6 trạng thái đồng bộ đạt.

Review độc lập đã kiểm target chung và ngoại lệ mixed-mode; sửa checker để bắt DLL ở thư mục lồng nhau và chuẩn hóa dấu phân cách cuối đường dẫn. Các quyết định triển khai và giới hạn đã ghi tại spec và `docs/database/BO_CAI_GON_20261117.md`. Không còn phát hiện chặn triển khai.
