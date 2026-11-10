# Triển khai, vận hành và phục hồi Sprint3

Ngày cập nhật: 10/11/2026. Đây là hướng dẫn để người vận hành thực hiện khi có
môi trường và lịch chuyển đổi đã thống nhất. Chưa chuyển database đang dùng;
kết quả kiểm nội bộ và điều kiện còn thiếu nằm trong
[biên bản P6](NGHIEM_THU_P6_20261110.md).

## Chuẩn bị trước chuyển đổi

Giữ cùng một phiên bản bốn tầng ET/DAL/BUS/WPF và file
`Database/QLBenhVienRangHamMat.sql`. Không dùng SQL trích từ thư mục `obj` để
triển khai. Ghi SHA-256 SQL, hash bộ nguồn/bản dựng, tên SQL Server/database,
kho ảnh, tài khoản Windows chạy ứng dụng và người phụ trách phục hồi.

Chuẩn bị .NET Desktop Runtime 8 cho máy chạy WPF; máy dựng cần .NET SDK 8,
SQL Server truy cập được bằng Windows Integrated Security và `sqlcmd`.
Kiểm phiên bản/cấu hình tại môi trường đích trước lịch triển khai. Database
đích phải có quyền SQL theo cơ chế tài khoản của hệ thống; tài khoản thực hiện
migration/backup cần quyền quản trị tương ứng, không dùng tài khoản này làm
tài khoản vận hành hằng ngày.

Kho ảnh dùng UNC, ví dụ `\\MAYCHU\RHM_XQuang`. Người quản trị chuẩn bị share và
quyền NTFS cho tài khoản Windows chạy ứng dụng, gồm đọc/ghi/tạo tệp theo nghiệp
vụ; kiểm truy cập từ cả hai máy. Kho sao lưu và thư mục trung chuyển `.bak`
phải truy cập được bởi cả tài khoản chạy công cụ và dịch vụ SQL Server. Quyền
SQL không tự cấp quyền Windows cho thư mục. Công cụ không tạo share hay sửa ACL.

Giữ bản cấu hình cũ và bản ứng dụng cũ. Ngừng các client trước backup cuối cùng,
migration và chuyển cấu hình; duy trì cửa sổ dừng ghi cho đến khi xác minh xong.
Không nâng cấp khi chưa có bản sao đã phục hồi thử được.

## Sao lưu trước nâng cấp

Database trước Sprint3 còn 62 bảng, chưa có metadata X-quang mới. Dùng backup
SQL gốc cho mốc này; công cụ `QuanLyKhoAnhXquang.ps1` yêu cầu schema ảnh nên
không thay thế bước backup trước nâng cấp62.

Ví dụ T-SQL sau chạy trên SQL Server đã chọn, đường dẫn phải mới và do người
vận hành điền; không dùng tên một bản sao đã có:

```sql
BACKUP DATABASE [QLBenhVienRangHamMat]
 TO DISK=N'<thu_muc_SQL_Server_truy_cap_duoc>\RHM_truoc_Sprint3_<moc_moi>.bak'
 WITH COPY_ONLY, CHECKSUM;
RESTORE VERIFYONLY
 FROM DISK=N'<thu_muc_SQL_Server_truy_cap_duoc>\RHM_truoc_Sprint3_<moc_moi>.bak'
 WITH CHECKSUM;
RESTORE FILELISTONLY
 FROM DISK=N'<thu_muc_SQL_Server_truy_cap_duoc>\RHM_truoc_Sprint3_<moc_moi>.bak';
```

`VERIFYONLY` không thay việc phục hồi thật. Dùng SSMS hoặc `RESTORE DATABASE`
với `MOVE` theo các logical name trả về, vào **database thử mới**
`RHM_Hospital_Test_PreUpgrade_<moc>` và các MDF/LDF mới. Đối chiếu số bảng, số
dòng, chứng từ và đăng nhập trên bản phục hồi; giữ nguyên bản gốc. Nếu hệ thống
có tệp ngoài SQL ở mốc cũ, sao lưu riêng cùng thời điểm và ghi rõ trong biên bản.

Với database đã có schema64 và kho ảnh, dùng bản sao SQL kèm ảnh, gồm ảnh ẩn:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\QuanLyKhoAnhXquang.ps1 `
  -CheDo KiemTra -MayChu '<SQL_SERVER>' -Database '<DATABASE>' `
  -ThuMucKho '\\MAYCHU\RHM_XQuang'
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\QuanLyKhoAnhXquang.ps1 `
  -CheDo SaoLuu -MayChu '<SQL_SERVER>' -Database '<DATABASE>' `
  -ThuMucKho '\\MAYCHU\RHM_XQuang' -ThuMucBanSao '<NOI_LUU_BAN_SAO>' `
  -ThuMucSqlServer '\\MAYCHU\SQL_Staging'
```

Chỉ công nhận bản sao khi có `BACKUP_COMPLETE`, `manifest.json`, `database.bak`,
thư mục `images` và không còn `INCOMPLETE.txt`. Bản sao có hash SQL và từng ảnh;
không tự xóa tệp không đăng ký. Bảo quản bản sao với quyền phù hợp vì có dữ liệu
người bệnh. Không đưa `.bak`, kho ảnh hoặc snapshot thật vào Git.

## Nâng cấp và cấu hình ứng dụng

Thử toàn bộ trên bản phục hồi trước khi chuyển database đang dùng. File SQL
chính có tên `QLBenhVienRangHamMat` trong nội dung; lệnh `-d` không đổi được
những `USE` đó. Bộ kiểm tự đổi tên trong bản sao SQL riêng và giới hạn tiền tố
database thử. Khi triển khai thật, chỉ chạy file chính nếu tên database đích
đúng như file; nếu dùng tên khác, phải chuẩn bị bản triển khai đổi tên và rà
soát lại toàn bộ trước khi chạy.

```powershell
# Chỉ thực hiện trong cửa sổ triển khai đã thống nhất, sau backup/phục hồi thử.
sqlcmd -S '<SQL_SERVER>' -d master -E -C -b -I -f65001 `
  -i .\Database\QLBenhVienRangHamMat.sql
# Mã thoát phải bằng0; lưu toàn bộ log và kiểm cấu trúc sau đó.
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraBanGiao.ps1 `
  -KiemTraSql -MayChu '<SQL_SERVER>' -Database QLBenhVienRangHamMat
```

SQL được kiểm cài mới, nâng cấp62→64, chạy lại và sửa tiếp sau lỗi mô phỏng.
Nếu lỗi, giữ log và trạng thái database, tiếp tục dừng ghi; xác định pha lỗi
trước khi chạy lại cùng file/hash. Không hạ schema bằng cách xóa cột/bảng hay
tắt trigger. Lệnh kiểm không tự chạy migration.

Cấu hình `appsettings.json` cạnh ứng dụng:

```json
{
  "ConnectionStrings": {
    "RangHamMat": "Data Source=<SQL_SERVER>;Initial Catalog=QLBenhVienRangHamMat;Integrated Security=True;TrustServerCertificate=True;"
  },
  "XQuang": {
    "ThuMucDungChung": "\\\\MAYCHU\\RHM_XQuang",
    "DungLuongToiDaMB": 20,
    "DiemAnhToiDa": 40000000,
    "ChoPhepDuongDanCucBo": false
  }
}
```

Kiểm biến môi trường `RHM_CONNECTION_STRING` và `RHM_XRAY_ROOT`: chúng có thể
ưu tiên hơn cấu hình file. Tất cả client phải trỏ cùng SQL/database/kho UNC;
không bật cho phép kho cục bộ trong cấu hình dùng chung.

## Xác minh trước mở lại

Chạy các thủ tục kiểm schema từ `KiemTraBanGiao.ps1`, kiểm đủ64 bảng và
`DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS,NO_INFOMSGS`. Đối chiếu số dòng và
các cột cũ với bản trước nâng cấp. Phiếu lịch sử cũ phải giữ Thông thường/0đ;
không tự gán dịch vụ hoặc mã bệnh cho dữ liệu thiếu.

Dùng `DoiSoatDuLieu.ps1 -MayChu '<SQL_SERVER>' -Database '<DATABASE>'` để đọc
các bất thường tiền/kho/ca/mã bệnh; kiểm đường dẫn xuất từ output và lưu biên
bản xử lý. Công cụ chỉ báo cáo, không tự sửa. Thiếu `MaDMTT` cần người có trách
nhiệm đối chiếu bệnh án; báo cáo bệnh lý chỉ có ý nghĩa với lượt đã phân loại.

Trên DB thử riêng, chạy lại tiếp nhận thường/dịch vụ, phí30.000đ, in hóa đơn,
thu/hoàn, ảnh theo quyền và báo cáo ba kỳ. Không chạy bộ tạo fixture vào
database đang dùng. Kiểm tài khoản thực theo từng vai trò, quyền share/NTFS,
kết nối sau khởi động lại và mở cùng ảnh từ hai máy trước khi cho ghi mới.

## Hoàn tất phần nghiệm thu hai máy

Có thể dùng [gói XR04 đã biên dịch](BO_NGHIEM_THU_XR04.md) để máy kiểm chỉ
cần runtime thay vì repository/SDK. Gói đã được
[kiểm từ ZIP giải nén](KET_QUA_BO_NGHIEM_THU_XR04_20261110.md) trên môi trường
hiện tại. Các lệnh repository dưới đây vẫn là cách chạy khi có source/SDK.

Bộ nghiệm thu tổng tạo fixture ảnh có dấu `FIXTURE`, gồm ảnh đang dùng và ảnh
ẩn. Người vận hành chép nguyên kho fixture vào UNC thử đã chuẩn bị, giữ tên
tệp/hash, cho cả hai máy dùng **cùng** database thử, SQL và UNC. Không dùng
probe này với bản sao bệnh án thật. Trên từng máy chạy:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraKhoAnhHaiMay.ps1 `
  -MayChu '<CUNG_SQL_SERVER>' -Database '<CUNG_DB_FIXTURE_XRAY>' `
  -ThuMucKho '\\MAYCHU\RHM_XQuang_Test' -TepBangChung '<DUONG_DAN_PROOF_MOI>.json'
# Gom hai proof thật từ hai máy rồi so sánh:
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraKhoAnhHaiMay.ps1 `
  -TepBangChung '<PROOF_MAY_B>.json' -DoiChieuVoi '<PROOF_MAY_A>.json'
```

Hai proof phải khác hostname, khớp SQL/database/UNC/hash/số ảnh và chứng minh
mở bằng BUS theo quyền ứng dụng. Probe ghi audit xem ảnh trong DB fixture;
không upload/ẩn hay thay dữ liệu ảnh. `PASS_SHARED_STORAGE` chỉ xác nhận kho
chung; không xác nhận phục hồi SQL từ xa. UNC `\\localhost\D$` kiểm trên một
máy cũng không đáp ứng mục hai máy.

Phục hồi bản sao SQL+ảnh sang SQL Server khác và kho đích mới, sau đó mở lại
từ máy khác:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\QuanLyKhoAnhXquang.ps1 `
  -CheDo KhoiPhuc -MayChu '<SQL_SERVER_THU_HAI>' -Database '<DB_NGUON>' `
  -DatabaseMoi 'RHM_Hospital_Test_Restore_<moc_moi>' -BanSao '<BACKUP_COMPLETE>' `
  -ThuMucKho '\\MAYCHU\RHM_Restore_Test_Moi' -ThuMucSqlServer '\\MAYCHU\SQL_Staging'
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraAnhXquang.ps1 `
  -MayChu '<SQL_SERVER_THU_HAI>' -Database 'RHM_Hospital_Test_Restore_<moc_moi>' `
  -KiemTraPhucHoi -ThuMucKho '\\MAYCHU\RHM_Restore_Test_Moi'
```

Đây là kiểm phục hồi fixture; `-KiemTraPhucHoi` yêu cầu bộ fixture tương ứng.
Database thật phục hồi cần quy trình đối soát và tài khoản nghiệp vụ thực.
Kiểm `RESTORE_COMPLETE`, hết `INCOMPLETE_RESTORE.txt`, đúng metadata/hash,
ảnh ẩn còn giữ và viewer mở được ảnh. Ghi hostname/SQL đích/kho đích/log/UI
vào biên bản. Công cụ từ chối DB đích đã có, không dùng `WITH REPLACE`.

## Rollback khi chuyển đổi có lỗi

1. Ngừng ghi từ tất cả client; ghi thời điểm, lỗi, phiên bản và người xử lý.
   Giữ database/kho sau lỗi để chẩn đoán, không xóa hay ghi đè.
2. Nếu chưa phát sinh nghiệp vụ sau backup cuối, phục hồi bản trước nâng cấp
   vào DB mới, đối chiếu và kiểm bằng phiên bản ứng dụng/cấu hình cũ. Mốc62
   dùng backup SQL gốc; mốc64 phục hồi cả SQL và kho ảnh cùng bản sao.
3. Nếu đã có nghiệp vụ mới, giữ bản sao trạng thái mới và đối chiếu chênh lệch
   trước khi quyết định chuyển lại. Phục hồi bản cũ sẽ bỏ các giao dịch phát
   sinh sau backup; không tự động chọn cách này.
4. Người vận hành xác nhận điểm phục hồi, đường kết nối, quyền và kết quả kiểm
   trước khi đổi client sang bộ ứng dụng/database/kho đã xác minh. Chuyển
   database đang dùng là bước vận hành riêng, ngoài công cụ restore thử.
5. Mở lại theo nhóm nhỏ, kiểm giao dịch/ảnh/báo cáo rồi mới mở toàn bộ; giữ log,
   bản sao và hiện vật lỗi theo quy định lưu trữ của đơn vị.

## Hồ sơ bàn giao

Lưu ma trận21 tiêu chí, mã lần chạy, source manifest, log build, migration,
đối soát, backup/restore, hai proof khác máy, cấu hình đích và xác nhận của
người vận hành. `ReadyForDeployment=false` còn giữ nguyên cho đến khi đủ
bằng chứng hạ tầng và nghiệm thu vận hành. Không sửa JSON cũ để biến kết quả
chưa kiểm thành PASS; tạo phụ lục hoặc lần kiểm mới gắn đúng nguồn đã chạy.
