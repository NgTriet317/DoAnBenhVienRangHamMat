# Cấu hình SQL sau khi cài lại — 17/11/2026

> Bản ghi kiểm chứng ban đầu của 1.0.0. Bộ cài và đường dẫn phát hành hiện tại đã được thay bằng 1.0.1 trong [sửa lỗi bộ cài thiếu DLL](SUA_LOI_BO_CAI_20261117.md).

Lần mở đầu sau khi cài MSI luôn hiện Cấu hình kết nối SQL Server. Cấu hình cũ có thể điền sẵn nhưng không tự bỏ qua màn hình. Chỉ Lưu và tiếp tục thành công mới xác nhận; đóng/hủy hoặc lưu lỗi vẫn hỏi lại lần sau. Tự tìm kết nối chỉ điền thông tin để kiểm tra. Những lần mở tiếp theo dùng cơ chế khôi phục kết nối hiện có; kết nối lỗi sẽ mở lại màn hình.

## Bộ cài

- Chạy `RHM/Release-CauHinhDatabase/setup.exe`, giữ `RHM.msi` cạnh nó.
- Gỡ phiên bản hiện tại trước khi cài gói mới nếu đang dùng cùng ProductCode/phiên bản 1.0.0. Các bộ cài cũ không chứa thay đổi này.
- Bộ cài dành cho tài khoản Windows hiện tại. MSI đặt DWORD `HKCU\Software\RangHamMat\CaiDat\CanCauHinhDatabase=1`; sau xác nhận App ghi 0. MSI xóa nhánh này khi gỡ, nên cài lại cùng phiên bản vẫn yêu cầu cấu hình.
- Không xóa database hoặc profile DPAPI `%LOCALAPPDATA%\RangHamMat\ketnoi-sql.dat`.
- SHA256 MSI: `9A1338C90D855AE8F46D0366A174461A07E62B075E31C4EFD0B0D078885CCE6F`.

## Kiểm chứng

| Kiểm tra | Kết quả | Bằng chứng cục bộ |
|---|---|---|
| Dịch vụ quyết định hiển thị, profile registry riêng ngẫu nhiên | 11/11 | `obj/database-setup-final-checks.log` |
| MSI thực sự chứa DWORD 1, cleanup, phạm vi per-user và các thành phần WPF/Kiosk/config | 8/8 | `obj/database-setup-msi-verified.log` |
| Hồi quy phiên đăng nhập WPF/DPAPI | 12/12 | `obj/database-setup-session-verified.log` |
| Trạng thái đồng bộ | 6/6 | `obj/database-setup-sync.log` |
| Visual Studio build Release và MSI | 6 succeeded, 0 failed | `obj/database-setup-msi-complete-build.log` |
| Build bộ kiểm tra Release | 0 warning, 0 error | `obj/database-setup-final-build.log` |

Đã thấy kiểm tra hành vi thất bại trước khi thêm dịch vụ. Kiểm tra gói cũng từng thất bại khi DWORD chưa được gói và khi thiếu backend Kiosk. Đã sửa định dạng vdproj và hook publish rồi chạy đạt. Hook Kiosk chuyển sang `ComputeResolvedFilesToPublishList` vì Installer Projects lấy `PublishItemsOutputGroup` trực tiếp qua target này; gói mới có backend cùng mã nguồn.

Lệnh kiểm tra: `dotnet run --project Test/DatabaseSetupChecks/DatabaseSetupChecks.csproj -c Release -p:NuGetAudit=false`; `powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraCauHinhSauCaiDat.ps1`.

Đã review độc lập và xử lý các phát hiện về registry, tự tìm và phạm vi cài đặt. Build toàn bộ còn cảnh báo nullable/platform đã có; kiểm tra đồng bộ có cảnh báo NuGet không truy cập được nguồn audit. Lỗi quyền registry/DPAPI khi thử trong sandbox đã được kiểm lại thành công bằng môi trường Windows có quyền thông thường, không nới bảo vệ trong mã sản phẩm.

Giới hạn: vòng đời gỡ/cài được mô phỏng trên nhánh registry riêng và đối chiếu bảng MSI; chưa gỡ/cài ứng dụng thật, chưa kiểm chuyển tài khoản Windows hay thao tác SQL qua màn hình thật. Xử lý lỗi registry đã được review ở App nhưng chưa thử ACL từ chối trên khóa sản phẩm. Không thay đổi thứ tự ưu tiên biến môi trường so với profile/appsettings.
