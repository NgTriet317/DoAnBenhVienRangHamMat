# Sửa lỗi bộ cài thiếu DLL — phiên bản 1.0.1

Ảnh `LoiCauHinh.jpg` báo không tải được `DAL`. Event Log trên máy phát hành ghi thêm lỗi không tải `System.Configuration.ConfigurationManager`. Bộ cài có các DLL này dưới `KioskServer` nhưng thiếu bản ở thư mục chứa `RangHamMat.Wpf.exe`, nên chương trình vẫn lỗi dù chạy bình thường từ bin của dự án.

## Nguyên nhân và thay đổi

- Target thêm backend trước `ComputeResolvedFilesToPublishList` khiến SDK lọc DLL gốc là tệp trùng của backend. Đã chuyển sang `AfterTargets`, thêm backend sau khi danh sách gốc được giải quyết.
- Bộ kiểm tra cũ chỉ tìm tên file trong MSI, không phân biệt thư mục. Bộ kiểm tra mới giải bảng Directory/Component, yêu cầu DLL đúng đường dẫn và đối chiếu toàn bộ manifest publish.
- Thư mục `Release-CauHinhDatabase` do đường dẫn output được đổi ở lần sửa trước. Đã trả về `RHM/Release` (Release) và `RHM/Debug` (Debug).
- MSI dùng profile folder `Properties/PublishProfiles/Installer.pubxml`, win-x64, không single-file/trimming. WPF và Kiosk có runtime riêng đi kèm. Backend được mở bằng `RangHamMat.Kiosk.Api.exe`, có fallback dotnet cho bản phát triển cũ.
- Bỏ prerequisite .NET Framework 4.7.2 không phù hợp. File version của WPF/API và phiên bản MSI tăng lên 1.0.1 để MSI thay được tệp cũ khi nâng cấp. UpgradeCode giữ nguyên.
- Cờ xác nhận SQL sau cài đặt được giữ. Không xóa database, hồ sơ hay cấu hình SQL đã mã hóa. Không đưa AiSecrets.local.json vào gói.

## Đóng gói và chuyển máy

1. Mở solution bằng Visual Studio, chọn **Release**, **Rebuild** project **RHM**.
2. Kết quả: `RHM/Release/setup.exe` và `RHM/Release/RHM.msi`. Giữ hai tệp cùng thư mục và chạy setup.exe trên Windows x64.
3. Lần đầu mở ứng dụng, chọn SQL Server/database đã được chuẩn bị bằng file SQL chính. SQL Server có thể ở máy chủ khác; máy trạm cần quyền kết nối. Máy trạm không cần .NET SDK hoặc runtime .NET riêng cho WPF/Kiosk.

Kiểm tra đầy đủ file publish:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraCauHinhSauCaiDat.ps1 -PublishPath RangHamMat.Wpf/bin/Release/net8.0-windows/win-x64/publish-msi
```

## Kiểm chứng

Kiểm tra ban đầu thất bại đúng tại `DAL.dll` ở thư mục gốc. Sau sửa, output group chứa cả `DAL.dll`, `System.Configuration.ConfigurationManager.dll` ở gốc và dưới KioskServer. Đã giải nén quản trị MSI vào `obj` để kiểm tra đúng payload; không dùng bin làm bằng chứng thay cho bộ cài.

Đã mở executable từ MSI giải nén với PATH không có dotnet và DOTNET_ROOT chỉ tới thư mục rỗng: màn hình cấu hình SQL cùng nút Lưu nạp được, đóng bằng X thoát mã 0. Không bấm Lưu hay đăng nhập. `tools/KiemTraMoBoCai.ps1` thực hiện kiểm tra này khi tài khoản chưa xác nhận SQL; script không tự thay registry sản phẩm.

`Test/InstallerRuntimeChecks` build self-contained win-x64, chép riêng bốn tệp helper (exe, dll, deps.json, runtimeconfig.json) vào thư mục ứng dụng đã giải nén và chạy **helper.exe**. Không chạy helper bằng dotnet vì Windows có thể tìm dotnet tại thư mục executable của tiến trình cha, làm kiểm tra sai. Với source host cũ, kiểm tra thất bại vì không tìm thấy dotnet; với host mới, backend trả `/health=ok` bằng apphost và runtime trong MSI. Không gọi `/health/db` hoặc ghi SQL.

Lượt build cuối, kết quả đối chiếu manifest, version và hồi quy được ghi tại `obj/msi-clean-build.log`, `obj/msi-complete-package-checks.log`, `obj/msi-final-ui.log`, `obj/msi-final-runtime.log`, `obj/msi-regression.log`. Các tệp obj là bằng chứng cục bộ, không phát hành cùng nguồn.

Kết quả cuối: Rebuild Visual Studio **6 thành công/0 lỗi**; **923 tệp publish** khớp đúng đường dẫn và kích thước trong MSI; WPF/API có FileVersion **1.0.1.0**; kiểm tra mở/đóng WPF và backend health đều đạt. Hồi quy **11 cấu hình, 12 phiên đăng nhập, 6 trạng thái đồng bộ** đều đạt. Review độc lập xác nhận không còn lỗi trong phạm vi.

MSI cuối: 127.606.272 byte, SHA256 `48B77A7B0F0F00307373ADCD8FE02FEEC833459B95AF7122A1ACA321AC4443B6`.

Giới hạn: đã giải nén/chạy payload, chưa thực hiện gỡ/cài/nâng cấp sản phẩm thật hoặc thử trên máy vật lý khác. Build dự án còn các cảnh báo nullable/platform có sẵn.
