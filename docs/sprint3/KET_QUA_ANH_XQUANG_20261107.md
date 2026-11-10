# Kết quả P4 — ảnh X-quang theo lần khám

Đã triển khai P4 từ `KeHoachTrienKhai.docx`, tiếp nối P0–P3 trên nhánh
`Sprint3_tan`. Giữ bốn project WPF/BUS/DAL/ET, dùng duy nhất
`Database/QLBenhVienRangHamMat.sql` để triển khai schema. Giữ thay đổi bỏ
`fo-dicom.Desktop` của người dùng; chức năng này nhận ảnh tĩnh JPG/JPEG/PNG.

## Hành vi và phạm vi quyền

Bác sĩ mở tab **Ảnh X-quang** trong lần khám đã lưu: chọn tệp, liên kết dịch vụ
chụp nếu có, ghi chú, tải lên, chọn ảnh xem trước, **Xem lớn / phóng thu** hoặc
**Ẩn ảnh**. Có thể xem metadata đã ẩn; ảnh đã ẩn không mở lại qua ứng dụng.
Trang cuộn theo chiều dọc, giữ vùng ảnh/nút thao tác khi cửa sổ thấp.

| Người dùng | Xem ảnh | Tải và ẩn ảnh |
|---|---|---|
| Admin | Mọi lượt hợp lệ | Lượt Nháp còn mở; vẫn tuân thủ trạng thái |
| Bác sĩ phụ trách | Lượt của mình; lịch sử hoàn tất cùng hồ sơ từng phụ trách | Lượt Nháp có bác sĩ phiếu/lượt cùng là mình |
| Kỹ thuật viên/điều dưỡng có quyền được giao | Chỉ ảnh liên kết dịch vụ chụp CK004 được giao | Chỉ lượt còn mở; ẩn ảnh do mình tải trong phần được giao |
| Tiếp nhận có XemHoSo | Lượt đã xác nhận trong phạm vi tra cứu hiện có | Không |
| Thu ngân/dược sĩ thiếu XemHoSo, bác sĩ ngoài phạm vi | Bị từ chối | Bị từ chối |

Quyền/tài khoản được đọc lại từ SQL cho từng lần tải, xem hoặc ẩn. Metadata và
ảnh xem trước đã nạp không cấp quyền mở lại sau khi tài khoản bị khóa hoặc đổi
người phụ trách/phân công. Lượt đã xác nhận chỉ xem, kể cả Admin.

Sau khi ghi nhận dịch vụ chụp tại **4. Thực hiện**, bác sĩ chọn dịch vụ và nhân
viên trong vùng **Giao tải ảnh cho kỹ thuật viên**. Kỹ thuật viên mở **Dịch vụ
thực hiện → Ảnh X-quang được giao**, chọn lượt rồi tải ảnh gắn đúng dịch vụ.

Điều chỉnh so với nền tảng: thêm `PHAN_CONG_ANH_XQUANG`, nâng tổng schema lên
**64 bảng** và hợp đồng `RHM.XrayStorage.Version=1`. Phân công lưu riêng để giữ
nguyên người thực hiện, giá và trạng thái của chứng từ dịch vụ đã chốt. Đổi người
hủy phân công cũ rồi tạo dòng mới trong một giao dịch; giữ lịch sử. Người bị khóa
hoặc nghỉ việc vẫn có thể được thay bằng người đang hoạt động. Fallback theo
`MaNVThucHien` chỉ dùng cho dịch vụ cũ chưa từng có lịch sử phân công ảnh.

## Lưu trữ và nhất quán

- Giữ nguyên byte ảnh gốc; không chuyển đổi/nén lại. Kiểm tên, đuôi, MIME thực,
  decode một khung, tối đa20 MiB,40 triệu điểm ảnh và12000 điểm ảnh mỗi chiều.
  Cấu hình có thể giảm giới hạn. DICOM/GIF và nội dung giả định dạng bị chặn.
- Metadata theo `MaLS`, nhân viên tải, thời gian, ghi chú, kích thước, MIME,
  SHA-256. Tên lưu là GUID32, đường dẫn tương đối chỉ bằng khóa tệp; không nhận
  đường dẫn máy trạm hoặc đường dẫn đi lùi. Chặn reparse point ở gốc/các thư mục
  cha, tệp và vùng tạm.
- Tải ảnh sở hữu giao dịch riêng: khóa kho chia sẻ, kiểm quyền, staging `.pending`,
  flush, di chuyển cùng kho, ghi metadata/audit, commit. Không tải trong giao dịch
  khám chưa lưu. Lỗi SQL trước commit dọn tệp mới; commit chưa rõ kết quả giữ tệp
  để đối soát. Tệp orphan/tệp tạm không tự bị xóa.
- Retry cùng nội dung/lượt/người tải/dịch vụ dùng lại ảnh đang sử dụng sau khi
  kiểm tệp hiện hữu. Tệp mất hoặc sai hash trả lỗi và lưu audit, giữ metadata.
- Ẩn là cập nhật trạng thái; giữ tệp, metadata và lịch sử. Audit UTC có
  `XR_UPLOAD`, `XR_UPLOAD_DUPLICATE`, `XR_VIEW`, `XR_HIDE`, `XR_ASSIGN`,
  `XR_FILE_MISSING`, `XR_FILE_INVALID`; gắn phiên khi đăng nhập có phiên.
  Audit không sao chép ảnh, ghi chú bệnh án hoặc đường dẫn kho.

## Cấu hình kho chung

Tạo thư mục UNC thật, ví dụ `\\MAYCHU\RHM_XQuang`, rồi sửa `appsettings.json`
cạnh exe trên các máy:

```json
"XQuang": {
  "ThuMucDungChung": "\\\\MAYCHU\\RHM_XQuang",
  "DungLuongToiDaMB": 20,
  "DiemAnhToiDa": 40000000,
  "ChoPhepDuongDanCucBo": false
}
```

`RHM_XRAY_ROOT` ưu tiên hơn đường dẫn JSON. Dùng cùng kho với cùng database;
không đổi kho của một client trong khi các client khác vẫn dùng kho cũ. Bản giao
để đường dẫn trống: trang hiển thị thông báo nếu chưa cấu hình hoặc mất kết nối.

Tài khoản Windows chạy ứng dụng cần quyền share/NTFS đọc, tạo, đổi tên và dọn tệp
tạm/tệp mới chưa liên kết; bảo vệ kho khỏi sửa trực tiếp. Tài khoản SQL dịch vụ
không cần truy cập kho ảnh thông thường. Tài khoản vận hành cần đọc toàn bộ kho,
ghi nơi sao lưu và quyền SQL backup/restore tương ứng. Cờ đường dẫn cục bộ chỉ
dùng có chủ đích trong môi trường kiểm thử; mặc định sản phẩm yêu cầu UNC.

## Vận hành kho và phục hồi

Công cụ dùng Windows Integrated Security của người vận hành, không giả danh
quyền ứng dụng. Các ví dụ cần thay tên máy/share bằng môi trường thật.

```powershell
# Đối soát chỉ đọc; gồm ảnh ẩn, missing/hash/orphan/tệp tạm, không dọn tệp:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/QuanLyKhoAnhXquang.ps1 -CheDo KiemTra -MayChu . -Database QLBenhVienRangHamMat -ThuMucKho '\\MAYCHU\RHM_XQuang'

# Thư mục SQL staging phải được cả SQL Server service và công cụ truy cập:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/QuanLyKhoAnhXquang.ps1 -CheDo SaoLuu -MayChu . -Database QLBenhVienRangHamMat -ThuMucKho '\\MAYCHU\RHM_XQuang' -ThuMucBanSao '\\MAYCHU\RHM_Backup' -ThuMucSqlServer '\\MAYCHU\RHM_SqlStaging'

# Chỉ phục hồi thử vào DB mới RHM_Hospital_Test_... và kho mới/rỗng:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/QuanLyKhoAnhXquang.ps1 -CheDo KhoiPhuc -MayChu . -DatabaseMoi RHM_Hospital_Test_Xray_Restore -ThuMucKho '\\MAYCHU\RHM_XQuang_Restore' -BanSao '\\MAYCHU\RHM_Backup\XrayBackup_<mốc_GUID>' -ThuMucSqlServer '\\MAYCHU\RHM_SqlStaging'
```

Kiểm kho/sao lưu giữ khóa độc quyền `RHM:Xray:Storage`; thao tác ảnh bình thường
dùng khóa chia sẻ trong giao dịch. Lên lịch lúc ít thao tác; ứng dụng báo kho đang
sao lưu nếu chờ quá hạn. Khóa được giải phóng khi giao dịch/kết nối đóng.

Sao lưu tạo thư mục mới có `database.bak` (**COPY_ONLY/CHECKSUM/VERIFYONLY**),
`images` và `manifest.json` với hash SQL/metadata. Tất cả ảnh tham chiếu, gồm ảnh
ẩn, phải toàn vẹn; orphan/tệp tạm được báo và giữ nguyên ngoài bộ sao lưu. Chỉ
marker `BACKUP_COMPLETE` xác nhận hoàn tất; thư mục còn `INCOMPLETE.txt` không dùng.
SQL staging giữ tệp SQL trung gian; người vận hành quản lý thời hạn lưu sau khi
xác nhận bản sao. Không sửa ACL hay dùng quyền admin tự động khi không đọc được
thư mục backup riêng của SQL Server; chỉ định staging đã được cấp quyền phù hợp.

Restore từ chối database hiện hữu trước khi tạo kho, không dùng `WITH REPLACE`.
Kiểm manifest/hash ảnh/hash SQL trước khi chép; tạo MDF/LDF riêng, kiểm SQL backup,
phục hồi, đối chiếu toàn bộ metadata/tệp và CHECK/FK. Chỉ dùng khi có
`RESTORE_COMPLETE` và không còn `INCOMPLETE_RESTORE.txt`. Nếu thất bại sau khi bắt
đầu, giữ nguyên hiện vật để chẩn đoán; không tự xóa database hoặc kho.

Khi rollback triển khai: dừng các client, giữ nguyên SQL cùng kho ảnh, bảo quản
bản sao đầy đủ trước nâng cấp. Thử restore cả SQL và ảnh vào môi trường riêng,
đối soát và mở ảnh trước khi điều phối chuyển lại database/kho. Công cụ chủ động
giới hạn restore vào DB thử mới; chuyển production cần thao tác vận hành riêng.
Không phục hồi SQL riêng lẻ rồi trỏ vào kho ảnh khác thời điểm.

## Bằng chứng kiểm thực tế

| Nhóm | Kết quả |
|---|---|
| P4 SQL/BUS/DAL/WPF | **32/32 PASS** |
| Sao lưu/phục hồi SQL và kho khác | **15/15 tệp** toàn vẹn, gồm ảnh ẩn; từ chối DB đã tồn tại |
| BUS mở lại ảnh phục hồi và viewer WPF/phóng thu thật | **2/2 PASS** |
| Chạy lại SQL với ảnh/lịch sử phân công đã tồn tại | **64/64** bảng giữ nguyên số dòng và fingerprint các cột dữ liệu |
| Nền tảng P0/P1 | **35/35 PASS** |
| Đồng bộ ET/DAL, công/lương | **32/32 PASS**;47 bảng ET/392 thuộc tính ánh xạ |
| Tiếp nhận/thu phí P2/P3 | **28/28 PASS** |
| Luồng khám, đơn thuốc, kho, hóa đơn, WPF hiện có | **47/47 PASS** |
| Báo cáo SQL/render | **41/41 PASS** |
| Build Debug/Release và cấu trúc/SQL chỉ đọc | Thành công,0 lỗi; Release còn118 cảnh báo trong mã hiện có |
| Review độc lập | Đã xử lý các finding; không còn Critical/Important được xác nhận trong phạm vi P4 |

Ca mới kiểm cả file JPEG/PNG thật tạo từ fixture, mất/hỏng/hash, tên/định dạng/dung
lượng/kích thước sai, SQL lỗi sau ghi tệp, retry, quyền theo bệnh nhân/dịch vụ,
khóa tài khoản, lịch sử chỉ xem, UI lịch sử mất quyền, phân công từ luồng khám
thật và thay người bị khóa. Có RED/GREEN cho lỗi duplicate thiếu tệp, sự kiện
Loaded mất quyền, thiếu luồng phân công, thay người bị khóa và vùng xem trước bị
che. Chưa mô phỏng mất mạng chính xác tại thời điểm commit hoặc tấn công thay
liên kết đồng thời; kiểm reparse là bảo vệ trong mã, không tuyên bố đã thử mọi
biến thể liên kết trên SMB.

Fixture cuối: `RHM_Hospital_Test_Xray_20261107_180558`; phục hồi:
`RHM_Hospital_Test_Xray_Restore_20261107_180626`. Toàn bộ phép ghi thử dùng database
`RHM_Hospital_Test_...` và tệp trong `obj`. Đối chiếu SQL chạy lại dùng fixture
`RHM_Hospital_Test_Xray_20261107_175912` có ảnh/phân công/lịch sử đã hủy.

Bằng chứng gọn và ảnh UI chứa dữ liệu giả tại
[evidence/20261107-xray](evidence/20261107-xray/). Log đầy đủ, SQL backup và kho
fixture nằm trong `obj/XrayEvidence`, được gitignore; không đóng gói ảnh người bệnh
hoặc `.bak` vào mã nguồn.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraAnhXquang.ps1 -TaoDatabaseKiemThu
# Lấy XRAY_DATABASE/XRAY_ROOT từ kết quả trên:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraSaoLuuAnhXquang.ps1 -Database RHM_Hospital_Test_Xray_<mốc> -ThuMucKho '<XRAY_ROOT>'
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBanGiao.ps1 -KiemTraSql -Database RHM_Hospital_Test_Xray_<mốc>
```

## Trạng thái bàn giao

P4 hoàn tất phần mã và kiểm thử trên môi trường hiện tại. Đã phục hồi sang DB/kho
khác trên cùng SQL Server, dùng cấu hình local thử rõ ràng. **Chưa kiểm UNC thực
giữa hai máy**, quyền share/NTFS hoặc sao lưu SQL Server ở máy khác; cần nghiệm
thu các mục đó với hạ tầng triển khai.

Chưa nâng cấp `QLBenhVienRangHamMat` đang dùng, chưa cấu hình UNC sản xuất, chưa
commit/push/publish. P5 audit/dữ liệu báo cáo nhiều kỳ và P6 nghiệm thu tổng thể
vẫn chưa hoàn tất; kiểm báo cáo ở đây là hồi quy hiện có.
