# Đồng bộ SQL với dự án — 10/11/2026

Đã cập nhật `Database/QLBenhVienRangHamMat.sql` cho dự án ET/DAL/BUS/WPF hiện
tại, schema64 bảng. Cấu hình mặc định của ứng dụng và đích CREATE/USE trong
SQL cùng dùng tên `QLBenhVienRangHamMat`. Không thay cấu hình kết nối hoặc
tự chạy migration lên database nghiệp vụ.

## Thay đổi trong file SQL

`usp_RHM_KiemTraSchema` được định nghĩa cuối file, sau mọi pha nâng cấp.
Một lệnh kiểm tổng nay kiểm cả schema nền, kê thuốc, công/lương V9, bảo trì,
khám dịch vụ/phụ thu và ảnh X-quang/phân công. Chuỗi tổng→kho ảnh→nền tảng
Sprint3 không gọi vòng lại thủ tục tổng.

Checker ảnh đối chiếu đủ21 thuộc tính của hai lớp ET ảnh/phân công: tên cột,
kiểu SQL, độ dài, scale và nullability. Khóa ngoại phân công phải trỏ đúng
bảng/cột dịch vụ hoặc nhân viên; trigger phải đúng bảng và đang hoạt động;
index chống trùng phải đúng cột dịch vụ và filter phân công đang áp dụng.
Checker tổng cũng kiểm index chống trùng `MaSuKien` của nhật ký.

Các pha bảo trì, phân quyền, schema nhật ký, trigger audit, module lương,
khám dịch vụ và ảnh có cờ hoàn tất của lần chạy hiện tại. Cờ được xóa trước
khi chạy; lỗi pha trước ngăn xác nhận cuối, kể cả khi SSMS tiếp tục batch.
Module cập nhật công/lương có transaction/TRY-CATCH và giải phóng cursor khi
lỗi. Chỉ khi các pha hoàn tất và kiểm tổng đạt mới in `PROJECT_SCHEMA_READY`.

Snapshot giữa hai marker quản lý giữ nguyên. Chạy lại giữ dữ liệu hiện có;
không hồi tố30.000đ vào phiếu cũ và không tự xóa nhật ký UTC trùng sự kiện
để ép index tạo thành công. SQL chỉ chứa metadata ảnh; kho tệp UNC vẫn được
cấu hình ở ứng dụng.

SHA-256 SQL mới:
`0167478AC3BCDDFFEEE011659B3F450566E3DA5BA29012C0A109D6C2CFF24200`
(1.121.553 byte). Hash trước tác vụ:
`C32BC5A606FE7F615DB862259D6637B07C5C26F1F70568DC027360DAAD2CD1BC`.
Xem [đối chiếu snapshot/hash](evidence/20261110-database-sync/sql-change.json).

## Kiểm chứng

Lần nghiệm thu cuối `20261110_124022_78d39f69` đạt **32/32 bước nội bộ**,
source-before/after ổn định485 mục. Đã cài mới64, nâng cấp từ SQL gốc62→64,
so fingerprint62 bảng cũ và64 bảng qua hai lần chạy lại, ngắt/sửa migration
có chứng từ, DBCC/FK/CHECK,47 lớp ET/392 thuộc tính ánh xạ,32 ca đồng bộ,
31 tiếp nhận,47 clinical,32 ảnh,2 ca mở ảnh phục hồi,21 P5/7 guard,
41 báo cáo và build Debug/Release0 lỗi. Xem
[kết quả máy đọc](evidence/20261110-database-sync/acceptance.json),
[bảng bước](evidence/20261110-database-sync/steps.csv) và
[log](evidence/20261110-database-sync/run.log).

Ma trận tổng có20 PASS,XR04 chờ hạ tầng máy thứ hai; trạng thái phát hành
giữfalse. Kiểm SELECT tổng hợp trên DB nghiệp vụ vẫn62 bảng/11 người/11
lượt/11 hóa đơn. Đây là trạng thái DB đang chạy, tách với SQL nguồn64 bảng
đã được kiểm trên các DB thử.

Bộ nền tảng đạt44/44 ca, gồm9 ca bổ sung cho hợp đồng tổng, liên kết ảnh
và nhật ký. Đã theo dõi RED→GREEN:35/7 thiếu hợp đồng ban đầu,42/1 sai cột
index và43/1 thiếu index audit; kết quả cuối44/0.

Rà soát độc lập xác nhận snapshot giữ nguyên và phát hiện một lỗi báo sẵn
sàng sau pha audit thất bại. Đã tái hiện bằng hai dòng UTC giả trùng
`MaSuKien` trong DB thử: trước sửa vẫn in READY; sau sửa báo lỗi và giữ đủ
hai dòng, không in READY. Ca lỗi P1/P4 cũng kiểm cờ cũ, số lượng chứng từ và
khả năng chạy lại sửa tiếp. Các fixture audit được dọn đúng PK trong DB thử
sau ca; không dọn dữ liệu nghiệp vụ.

Evidence và script tái hiện được lưu tại
[bộ chứng cứ SQL](evidence/20261110-database-sync/README.md). Các biên bản
P6/gói XR04 cũ giữ nguyên như lịch sử theo hash cũ; đối chiếu đúng bản SQL
và mã lần chạy khi sử dụng kết quả.

## Áp dụng vào SQL Server của ứng dụng

File `.sql` là nguồn cài/nâng cấp; ứng dụng mở kết nối đến SQL Server qua
`ConnectionStrings:RangHamMat` trong `appsettings.json` cạnh exe. Biến môi
trường `RHM_CONNECTION_STRING` nếu được đặt có mức ưu tiên cao hơn. Sửa file
SQL trong repository không tự thay đổi database đang chạy.

Sau khi backup/phục hồi thử và thống nhất cửa sổ dừng ghi, chạy **toàn bộ**
file trên SQL Server tương ứng với cấu hình, bằng SSMS hoặc:

```powershell
sqlcmd -S '<SQL_SERVER_CUA_UNG_DUNG>' -d master -E -C -b -I -f65001 `
  -i .\Database\QLBenhVienRangHamMat.sql
sqlcmd -S '<SQL_SERVER_CUA_UNG_DUNG>' -d QLBenhVienRangHamMat -E -C -b -I -f65001 `
  -Q 'EXEC dbo.usp_RHM_KiemTraSchema;'
```

Mã thoát phải bằng0 và lần cài phải có `PROJECT_SCHEMA_READY`. Nếu có lỗi,
giữ log, xử lý nguyên nhân rồi chạy lại cả file; không bỏ qua batch lỗi
hoặc tắt bảo vệ. Xem
[backup/triển khai/rollback](HUONG_DAN_TRIEN_KHAI_VA_ROLLBACK.md).

Để kiểm trên các DB thử mới:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraNghiemThu.ps1 -MayChu .
```

Mọi ghi thử giới hạn ở `RHM_Hospital_Test_...`. Chưa áp dụng migration lên
database đang dùng; chưa commit/push. Kiểm kho/phục hồi qua máy thứ hai vẫn
là điều kiện hạ tầng riêng của XR04.
