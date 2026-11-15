# Kiểm chứng đồng bộ ngày 16/11/2026

Phạm vi: nhánh `dong_bo_giua_cac_may`, tách từ `tan` tại `dcf4837`. Chỉ database thử được ghi trong các bộ kiểm. Chưa nghiệm thu trên hai máy vật lý hoặc SQL Server từ xa.

| Bộ kiểm | Kết quả | Bằng chứng hành vi |
| --- | --- | --- |
| SyncChecks | 6/6 | Gom bảng, hoãn không mất pending, xác nhận cũ không xóa cập nhật mới, reset phiên |
| SqlSyncChecks --full-schema | 21/21 | Hai kết nối SQL, commit/rollback, insert/update/delete, commit giữa checkpoint/enumeration, quyền thiếu, mốc hết hạn, identity khi thay database, batch truy vấn, cài đủ 74 bảng |
| WpfSyncChecks | 25/25 | Dispatcher/STA thật: modal, focus nhập, DataGrid edit, dirty form khám/lịch/nhân sự, lỗi load/restore/retry, hủy/đổi ngữ cảnh, tab ẩn và unload, chứng từ NCC biến mất không chuyển nhầm nguồn |
| RuntimeSyncChecks | 5/5 | Vòng poll thật với kết nối SQL khác: chức vụ cập nhật ở màn nhân sự đang mở, giữ nháp, nhận pending sau reset, snapshot nền cập nhật cache và danh sách NCC, nhận xóa NCC |
| KiemTraQuyTrinhBenhVien | 47/47 | Hồi quy khám/kê thuốc/chốt hóa đơn, rollback, chống xung đột và các giao diện lâm sàng |
| KiemTraQuyTrinhBenhVien -ChiGiaoDien | 2/2 | Đăng nhập admin, mở các phân hệ hiện có và Trợ lý AI, lịch sử dịch vụ giữ chỉ đọc |
| AIIntakeStateChecks | Thành công | Máy trạng thái bản khai AI, xác nhận và retry |

Các lệnh đã chạy (SQL local `lpc:.` trong terminal có quyền Windows phù hợp):

```powershell
dotnet build BenhVienRangHamMat.sln --no-restore -v quiet
dotnet run --project Test/SyncChecks --no-restore
dotnet run --project Test/WpfSyncChecks --no-restore
dotnet run --project Test/SqlSyncChecks --no-restore -- 'lpc:.' --full-schema
dotnet run --project Test/AIIntakeStateChecks --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraQuyTrinhBenhVien.ps1 -MayChu 'lpc:.' -Database RHM_Hospital_Test_DongBo_20261116_Runtime -TaoDatabaseKiemThu
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraQuyTrinhBenhVien.ps1 -MayChu 'lpc:.' -Database RHM_Hospital_Test_DongBo_20261116_Runtime -ChiGiaoDien
dotnet run --project Test/RuntimeSyncChecks --no-restore -- 'lpc:.' RHM_Hospital_Test_DongBo_20261116_Runtime
```

Build solution thành công, 0 lỗi. Các cảnh báo nullable, tên kiểu và project bộ cài vdproj có từ nền; bản dựng sau thay đổi vẫn có cảnh báo. Không coi build thành công là bằng chứng mọi nghiệp vụ đã được thử trên hai máy.

Bộ SQL tính SHA256 toàn bộ giá trị các bảng trước/sau chạy lại phần cài tracking trên schema đầy đủ. Hai giá trị bằng nhau:

`6A0EC6D9F7D08BCDE60BE7014B1BCE42FBD1412E6A4900BEE53854D4AF470744`

Log chạy cục bộ ở `obj/sync-*.log`, được Git bỏ qua. Các test được quản lý trong Test để chạy lại độc lập. RuntimeSyncChecks chỉ nhận database có tiền tố `RHM_Hospital_Test_`, phục hồi tên chức vụ và xóa NCC fixture khi kết thúc; database fixture của công cụ hồi quy được giữ để chạy lại.

## Các lỗi phát hiện và sửa trong rà soát

- Nhận diện ShowDialog bằng trạng thái cửa sổ native; thuộc tính WPF IsEnabled không phản ánh đủ việc owner bị disable.
- Bỏ kết quả tải nền của màn hình đã unload/đổi thế hệ, giữ pending để thử lại.
- Ghi nhớ revision theo bảng cho tab ẩn, yêu cầu cập nhật khi hiển thị lại.
- Phân biệt form lịch/nhân sự/tài khoản sạch với form có nội dung chưa lưu.
- Không tự chuyển sang hóa đơn NCC đầu tiên khi chứng từ đang xem rời danh sách.
- Không để cờ dangTai kẹt sau lỗi làm mới; không nuốt lỗi đọc để xác nhận pending sai.
- Báo cáo bị đổi bộ lọc/chuyên đề trong lúc tải không khôi phục lại nguồn thuộc yêu cầu cũ.
- Tách đọc nhân viên thuần SELECT cho nhịp tự động; giữ việc đồng bộ nghỉ ở các thao tác chủ động cũ.

## Giới hạn cần nghiệm thu tại nơi triển khai

Chưa thử gián đoạn mạng vật lý, tải nhiều máy đồng thời, quyền SQL thực tế tại server, kho ảnh UNC từ máy khác hoặc toàn bộ tình huống nghiệp vụ trong từng adapter. Các giao dịch ghi hiện có vẫn là nơi xác minh quyền/trạng thái/xung đột. Một số trình tra cứu lâm sàng giữ hàm đọc chi tiết hiện tại sau bước snapshot nền; cần đo độ trễ trên mạng triển khai nếu dữ liệu lớn. Không bổ sung làm việc offline hoặc sao chép ảnh giữa máy.
