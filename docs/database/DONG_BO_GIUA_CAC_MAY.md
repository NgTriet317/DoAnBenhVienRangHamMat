# Đồng bộ dữ liệu giữa các máy

Nhánh `dong_bo_giua_cac_may` bổ sung theo dõi SQL Server Change Tracking mỗi khoảng 2 giây. Mỗi máy đọc lại dữ liệu liên quan từ database chung bằng quyền nghiệp vụ hiện có. Độ trễ gồm chu kỳ kiểm tra và thời gian truy vấn; phần đang nhập được hoãn cập nhật.

## Triển khai

1. Sao lưu database. Trên server, dùng tài khoản quản trị chạy file SQL chính `Database/QLBenhVienRangHamMat.sql` từ nhánh này theo README. Nếu schema hiện tại đã đúng, có thể chọn riêng đoạn giữa `BEGIN RHM CROSS-MACHINE CHANGE TRACKING` và `END RHM CROSS-MACHINE CHANGE TRACKING` ở cuối file, sau khi chọn đúng database trong SSMS.
2. Phần cài bật snapshot isolation và Change Tracking trên các bảng có khóa chính. Cài mới dùng retention 7 ngày, tự dọn; chạy lại giữ cấu hình tracking đã có và không ghi dữ liệu nghiệp vụ. Phần mềm không tự chạy ALTER khi mở.
3. Tài khoản kết nối SQL cần SELECT, VIEW CHANGE TRACKING trên các bảng, VIEW DEFINITION trên database và quyền thấy định danh database trong sys.database_recovery_status. Phần cài không tự cấp quyền SQL. Quyền ứng dụng vẫn được kiểm ở BUS/DAL.
4. Build/publish toàn bộ mã nguồn nhánh mới; cập nhật đủ thư mục ứng dụng trên các máy. Cấu hình cùng server/database qua appsettings.json hoặc RHM_CONNECTION_STRING. Không cần thêm cổng hay dịch vụ mạng.
5. Mặc định bật đồng bộ với cấu hình sau trong appsettings.json cạnh exe:

```json
"DongBo": {
  "Bat": true,
  "ChuKyGiay": 2
}
```

Khoảng kiểm tra hợp lệ 1–60 giây. Ảnh X-quang vẫn dùng kho UNC chung theo README; đồng bộ metadata không sao chép tệp ảnh.

## Sử dụng

- Dòng trạng thái cuối cửa sổ cho biết đang theo dõi, đã cập nhật, đang hoãn hoặc lỗi cấu hình/kết nối.
- Biểu mẫu, nội dung khám/đơn thuốc chưa lưu, DataGrid đang sửa, yêu cầu thu/giao đang xử lý được giữ nguyên. Một số form được bảo vệ bảo thủ cả khi chưa gõ; kết thúc thao tác hoặc đóng form để cập nhật.
- Nút Tải lại dữ liệu dùng cùng bộ điều phối và bao phủ các adapter tải riêng trên phiên hiện tại; cần kết thúc nhập liệu trước khi tải lại.
- Bộ điều phối không tự lưu/hủy/chốt chứng từ, thu tiền, xuất kho hoặc tính lương. Các bước ghi vẫn kiểm quyền, trạng thái và xung đột như trước.
- Mất mạng: giữ phần đang hiển thị, thử lại với thời gian lùi tối đa 30 giây. Mốc hết hạn hoặc database thay thế yêu cầu đọc lại dữ liệu.
- Hội thoại và bản khai AI đang nhập thuộc máy hiện tại; không thay bằng hội thoại của máy khác.
- Nhật ký/heartbeat không kích hoạt tải lại toàn bộ cache nghiệp vụ.

## Kiểm tra

```powershell
dotnet build BenhVienRangHamMat.sln --no-restore
dotnet run --project Test/SyncChecks
dotnet run --project Test/WpfSyncChecks
dotnet run --project Test/SqlSyncChecks -- 'lpc:.' --full-schema
dotnet run --project Test/AIIntakeStateChecks
```

Kiểm vòng WPF thực tế trên database fixture đã được tạo bởi công cụ hồi quy:

```powershell
dotnet run --project Test/RuntimeSyncChecks -- 'lpc:.' RHM_Hospital_Test_DongBo_20261116_Runtime
```

Bộ SQL tạo database RHM_Hospital_Test_ChangeTracking_... riêng, thử hai kết nối và tự dọn database fixture. Không dùng database nghiệp vụ làm fixture. Nếu sandbox Windows không có SSPI hợp lệ, chạy trong terminal có quyền Windows phù hợp.

Nghiệm thu mạng: mở hai máy dùng cùng database, tiếp nhận A → hàng chờ B, chốt khám A → hóa đơn B, thu/giao A → trạng thái B; thử tương tự kho/nhân sự/lịch/lương. Khi B đang nhập, A cập nhật phải không làm mất nội dung B. Ngắt mạng B, nối lại và kiểm nhận đủ thay đổi. Hai kết nối trên một máy không thay cho nghiệm thu hai máy vật lý.

Xem [ma trận phạm vi và bằng chứng](DONG_BO_PHAM_VI.md).
Kết quả chạy và các giới hạn đã kiểm: [kiểm chứng ngày 16/11](DONG_BO_KIEM_CHUNG_20261116.md).

## Tắt theo dõi

Đặt DongBo:Bat=false rồi mở lại ứng dụng. Có thể dùng lại binary trước đó và tải lại thủ công; dữ liệu nghiệp vụ không đổi định dạng. Không tự tắt Change Tracking trên server nếu máy hoặc hệ thống khác vẫn sử dụng.
