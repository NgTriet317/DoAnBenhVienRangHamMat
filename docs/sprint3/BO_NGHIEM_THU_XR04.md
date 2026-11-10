# Bộ chạy nghiệm thu XR04

Gói này chứa ba công cụ đã biên dịch: Probe đọc ảnh qua UNC bằng BUS, Storage
sao lưu/phục hồi SQL kèm ảnh và Verify mở ảnh/viewer sau phục hồi. Máy chạy
cần Windows x64, PowerShell và .NET8 runtime; bước viewer cần Windows Desktop
Runtime8 và phiên desktop. Các lệnh chỉ gọi `dotnet <tool.dll>`, không build,
restore NuGet hay truy cập source. Máy tạo gói cần SDK. Không phải bộ cài ứng
dụng nghiệp vụ; trạng thái phát hành luôn `false`.

## Kiểm gói trước khi chạy

Giữ ZIP và tệp `.zip.sha256` do máy tạo gói xuất. So SHA-256 của ZIP bằng
`Get-FileHash`, giải nén vào thư mục mới rồi mở PowerShell tại thư mục đó:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\ChayNghiemThuXR04.ps1 -CheDo KiemTraGoi
```

Wrapper từ chối tệp thiếu/đổi hash/thừa ngoài manifest. Hash giúp phát hiện
gói thay đổi hoặc chép thiếu; đây không phải chữ ký xác thực nhà phát hành.
Không chỉnh binary, script hoặc `package.json` sau khi tạo gói. Kết quả mới
được lưu trong `Results`, tách khỏi payload đã kiểm hash. Mỗi lệnh tạo thư
mục kết quả mới và giữ log/kết quả JSON khi công cụ thất bại; không ghi đè.

## Chuẩn bị fixture và quyền

Chỉ dùng DB thử `RHM_Hospital_Test_...` từ bộ kiểm Xray có dấu `FIXTURE` và
kho chứa cả ảnh đang dùng lẫn ảnh ẩn. Không dùng bản sao bệnh án thật. Lấy
database/kho từ kết quả P6 hoặc tạo fixture bằng bộ kiểm trong repository.
Người vận hành chép nguyên kho vào UNC thử đã chuẩn bị, giữ tên/hash tệp.
Mọi máy trỏ cùng UNC và SQL/database này ở bước Probe; không tự tạo share.

Tài khoản Windows chạy công cụ cần quyền SQL và quyền share/NTFS tương ứng.
Sao lưu/phục hồi cần quyền SQL quản trị phù hợp; thư mục SQL staging phải
được dịch vụ SQL Server và tài khoản chạy công cụ cùng đọc/ghi. Gói không
cấp quyền, thay firewall hay ACL. Không dùng biến môi trường trỏ DB thật
thay cho các tham số thử.

## Probe trên hai máy

Thay placeholder bằng cấu hình thật rồi chạy cùng lệnh trên máy A và B:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\ChayNghiemThuXR04.ps1 `
  -CheDo Probe -MayChu '<CUNG_SQL_SERVER>' -Database '<CUNG_DB_FIXTURE>' `
  -ThuMucKho '\\SERVER\RHM_XQuang_Test'
```

Đọc `XR04_RESULT`, lấy `proof.json` trong thư mục tương ứng. Probe xác minh
hash toàn bộ ảnh, mở ảnh đang dùng theo quyền BUS và ghi audit xem trong
DB thử; không upload/ẩn ảnh. Gom hai proof thật rồi so:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\ChayNghiemThuXR04.ps1 `
  -CheDo Compare -DoiChieuVoi '<PROOF_MAY_A>.json' -TepBangChung '<PROOF_MAY_B>.json'
```

Hai proof phải khác hostname, cùng SQL/database/UNC/hash và số ảnh. Đổi tên
hostname trong JSON không phải kiểm chứng. `PASS_SHARED_STORAGE` chỉ xác
nhận đọc kho chung, chưa xác nhận phục hồi sang SQL Server khác. UNC loopback
trên cùng máy bị từ chối khi so hai proof.

## Kiểm kho, sao lưu và phục hồi thử

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\ChayNghiemThuXR04.ps1 `
  -CheDo KiemTraKho -MayChu '<SQL_NGUON>' -Database '<DB_FIXTURE_NGUON>' `
  -ThuMucKho '\\SERVER\RHM_XQuang_Test'
powershell -NoProfile -ExecutionPolicy Bypass -File .\ChayNghiemThuXR04.ps1 `
  -CheDo SaoLuu -MayChu '<SQL_NGUON>' -Database '<DB_FIXTURE_NGUON>' `
  -ThuMucKho '\\SERVER\RHM_XQuang_Test' -ThuMucBanSao '<THU_MUC_BAN_SAO>' `
  -ThuMucSqlServer '\\SERVER\SQL_Staging'
```

Chỉ dùng bản sao có `BACKUP_COMPLETE`, đủ manifest/SQL/ảnh và không có
`INCOMPLETE.txt`. Phục hồi bản đó sang SQL khác và kho UNC mới:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\ChayNghiemThuXR04.ps1 `
  -CheDo KhoiPhuc -MayChu '<SQL_THU_HAI>' -Database '<DB_FIXTURE_NGUON>' `
  -DatabaseMoi 'RHM_Hospital_Test_Restore_<MOC_MOI>' `
  -ThuMucKho '\\SERVER\RHM_Restore_Test_Moi' -BanSao '<BACKUP_COMPLETE>' `
  -ThuMucSqlServer '\\SERVER\SQL_Staging'
powershell -NoProfile -ExecutionPolicy Bypass -File .\ChayNghiemThuXR04.ps1 `
  -CheDo XacMinhPhucHoi -MayChu '<SQL_THU_HAI>' `
  -Database 'RHM_Hospital_Test_Restore_<MOC_MOI>' `
  -ThuMucKho '\\SERVER\RHM_Restore_Test_Moi'
```

Công cụ không ghi đè DB đích đã tồn tại. Nếu lỗi sau khi bắt đầu, giữ DB/kho
và marker `INCOMPLETE_RESTORE.txt` để chẩn đoán. Khi xác minh thành công,
`tool.log` phải có2 ca PASS, viewer nạp/phóng ảnh được và đối soát kho khớp.
Ảnh viewer fixture được lưu dưới `obj/RhmFinal` trong thư mục kết quả.

Có thể dùng `-ThuMucKetQua '<THU_MUC_MOI>'` cho từng lệnh để chọn nơi lưu
log; thư mục phải chưa tồn tại. `-TepBangChung` của Probe cũng phải là tệp mới.
Không dùng các công cụ này để rollback ghi đè database đang vận hành.

## Bằng chứng để chốt XR04

Giữ `package.json`, SHA-256 ZIP, hai proof khác máy, kết quả Compare, log
sao lưu/phục hồi, `RESTORE_COMPLETE`, kết quả viewer từ máy khác, hostname/
SQL/kho thực tế và xác nhận quyền vận hành. Một lần chạy PASS chỉ xác nhận
thao tác tương ứng; wrapper không tự sửa kết quả P6 hoặc đánh dấu phát hành.
Phải đối chiếu đủ điều kiện XR04 rồi lập phụ lục nghiệm thu mới.
