# Kết quả triển khai P0/P1 — 07/11/2026

Đã đọc `KeHoachTrienKhai.docx`, đối chiếu đúng nhánh `Sprint3_tan` tại
`1cafa7d2096a3e954d381e473520cba8bab4b9dd` và triển khai **gói nền tảng SQL/ET**.
Đây là mốc đầu của kế hoạch, chưa phải nghiệm thu toàn bộ tính năng khám dịch vụ/X-quang.

Cập nhật sau mốc này: **P2/P3 đã hoàn thành**, xem
[kết quả tiếp nhận và thu phí khám dịch vụ](KET_QUA_KHAM_DICH_VU_20261107.md).
Các mục “chưa làm” bên dưới ghi trạng thái tại thời điểm bàn giao P1.

## Đã thay đổi

| Thành phần | Kết quả |
|---|---|
| `Database/QLBenhVienRangHamMat.sql` | Migration cuối file trong transaction, khóa nâng cấp, chạy lại được; schema hiện tại 63 bảng. |
| Phiếu khám | `LoaiKham`, `PhiPhuThuDichVu`: mặc định Thông thường/0; Dịch vụ/30000 chỉ áp dụng phiếu trực tiếp. Khóa mã phiếu, loại khám và phí sau tiếp nhận. |
| Chi tiết hóa đơn | `MaPhieuPhuThu`, FK và unique filtered index; CHECK đúng một trong bốn nguồn, giá phụ thu phải có và đúng 30000. |
| Trigger tài chính/view | Tính thành tiền từ phiếu; đối chiếu đúng lần khám; giới hạn giảm giá; chặn chốt/xác nhận thiếu phí; view có loại phụ thu và số lượng 1. Giữ các khóa thuốc, dịch vụ, hóa đơn, xuất kho hiện có. |
| Chạy lại bị ngắt | Các pha cũ không ghi đè trigger/view hỗ trợ phụ thu trước khi tới migration cuối. Đã thử lỗi có chủ đích trước migration cuối. |
| Ảnh | Bảng `ANH_XQUANG_LAN_KHAM`, FK/index và ET: metadata, hash, người tải, ghi chú, trạng thái ẩn. Khóa liên kết từ cả phía ảnh, dịch vụ và lần khám. Cấm xóa cứng metadata. |
| Kiểm tra | Thêm `usp_RHM_KiemTraNenTangSprint3` chỉ đọc; cập nhật kiểm schema cũ để nhận bảng ảnh; cập nhật công cụ bàn giao và thêm bộ kiểm tích hợp. |

SQL chỉ lưu metadata ảnh. Quy ước khóa tệp là GUID 32 ký tự hex cộng `.jpg`,
`.jpeg` hoặc `.png`, một cấp trong kho chung; đường dẫn metadata bằng khóa tệp.
Giới hạn SQL 20 MiB/tệp, kiểm MIME/đuôi/hash/trạng thái. Đây **không thay thế**
kiểm nội dung/chữ ký ảnh hoặc phân quyền ở P4.

ET được cập nhật tại `ET_PHIEUKHAMBENH.cs`, `ET_CHITIETHOADON.cs`,
`ET_ANHXQUANGLANKHAM.cs`. Constructor cũ vẫn dùng được. Chưa thêm loại khám
vào yêu cầu tiếp nhận trước khi DAL/UI xử lý đầy đủ ở P2.

## Kết quả thực thi

| Kiểm tra | Kết quả thực tế |
|---|---|
| Build nền trước sửa | Thành công, 0 lỗi; có sẵn cảnh báo package/nullable. |
| Kiểm thử RED trên SQL cũ | Thất bại do thiếu schema mới, đúng kỳ vọng. |
| Cài mới SQL cuối, có snapshot | **35/35 PASS**, DB `RHM_Hospital_Test_S3Foundation_Final`. |
| Nâng cấp DB cài SQL gốc | **35/35 PASS**, DB `RHM_Hospital_Test_S3Foundation_Control`. |
| Bảo toàn dữ liệu cũ | **62/62 PASS**: số dòng và SHA-256 của toàn bộ cột cũ theo thứ tự PK không đổi trước/sau trên cùng DB Control. |
| Chạy lại có hóa đơn đã chốt/phụ thu/ảnh | **PASS**, không mất/nhân đôi nguồn; ghi thử phụ thu mới vẫn đúng 30000. |
| Ngắt trước migration cuối | Lỗi 53999 có chủ đích; **PASS** kiểm sau ngắt: view/trigger đang dùng vẫn đúng, hóa đơn/metadata giữ nguyên. |
| Hồi quy ET/DAL, chấm công/lương | **32/32 PASS** trên DB Control. |
| Hồi quy khám, đơn thuốc, hóa đơn, kho, WPF | **47/47 PASS**, DB `RHM_Hospital_Test_S3Foundation_Clinical`, cài không snapshot theo fixture của bộ kiểm cũ. |
| Báo cáo SQL và render WPF | **41/41 PASS** trên DB Control; không thêm hồ sơ giả vào DB ứng dụng. |
| FK/CHECK và `DBCC CHECKCONSTRAINTS` | Không có vi phạm; không có FK/CHECK bị tắt hoặc chưa trusted; 63 bảng. |
| Kiểm bàn giao | PASS: bốn project, cấu trúc Views, SQL và các hợp đồng schema. |
| Build Debug/Release cuối | Thành công, mỗi cấu hình 0 lỗi và 4 cảnh báo NU1701 đã có về `fo-dicom.Desktop 4.0.8`. |
| Review độc lập | Các finding Important đã có ca RED và sửa tới GREEN; không còn finding Critical/Important trong phạm vi P0/P1. |

Bằng chứng rút gọn, không chứa bệnh án/tệp ảnh: [evidence/20261107-foundation](evidence/20261107-foundation).
Log đầy đủ của phiên nằm trong `obj/Sprint3Foundation/` và có thể bị dọn khi clean.

Kiểm đối chiếu phải lưu fingerprint **trước khi nâng cấp trên chính DB đó**.
Không so hai lần cài mới độc lập vì timestamp tạo quyền mặc định có thể khác nhau.

Các fixture nghiệp vụ thông thường đều rollback. Chế độ `GhiDuLieuChayLai`
cố ý giữ một fixture để thử migration, chỉ chấp nhận DB cách ly có đuôi `_Rerun`.
Ảnh trong các kiểm này chỉ là metadata; chưa ghi tệp vào kho ảnh.

## Chạy lại các kiểm tra

```powershell
# Cài mới hoặc đồng bộ CHỈ database kiểm thử có tiền tố bắt buộc:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraNenTangSprint3.ps1 -Database RHM_Hospital_Test_S3Foundation_Final -TaoDatabaseKiemThu

# Đối chiếu nâng cấp: DB này phải đã cài SQL của commit gốc trước lệnh LuuDoiChieu.
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraNenTangSprint3.ps1 -Database RHM_Hospital_Test_S3Foundation_Control -CheDo LuuDoiChieu
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraNenTangSprint3.ps1 -Database RHM_Hospital_Test_S3Foundation_Control -TaoDatabaseKiemThu
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraNenTangSprint3.ps1 -Database RHM_Hospital_Test_S3Foundation_Control -CheDo DoiChieu

# Ghi fixture một lần trên DB _Rerun mới, sau đó thử chạy lại và ngắt giữa chừng:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraNenTangSprint3.ps1 -Database RHM_Hospital_Test_S3Foundation_Rerun -TaoDatabaseKiemThu -CheDo GhiDuLieuChayLai
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraNenTangSprint3.ps1 -Database RHM_Hospital_Test_S3Foundation_Rerun -TaoDatabaseKiemThu -CheDo KiemTraChayLai
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraNenTangSprint3.ps1 -Database RHM_Hospital_Test_S3Foundation_Rerun -TaoDatabaseKiemThu -CheDo KiemTraChayLai -MoPhongLoiTruocMigrationCuoi

# Hồi quy và build:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraDongBoSql.ps1 -Database RHM_Hospital_Test_S3Foundation_Control
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraQuyTrinhBenhVien.ps1 -Database RHM_Hospital_Test_S3Foundation_Clinical -TaoDatabaseKiemThu
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBaoCao.ps1 -Database RHM_Hospital_Test_S3Foundation_Control -Sql -Render
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBanGiao.ps1 -ChiCauTruc -KiemTraSql -Database RHM_Hospital_Test_S3Foundation_Control
dotnet build BenhVienRangHamMat.sln -c Debug --no-restore
dotnet build BenhVienRangHamMat.sln -c Release --no-restore
```

Lệnh `GhiDuLieuChayLai` sẽ từ chối khi fixture đã tồn tại; các lần sau dùng
`KiemTraChayLai`. Fingerprint nằm trong `obj/Sprint3FoundationChecks/`.

## Giới hạn và phần tiếp theo

- **P2 chưa làm:** hai chế độ UI, bác sĩ trước/dịch vụ sau, ca qua ngày, hiện diện,
  nghỉ phép, recheck trong giao dịch, retry có so loại khám. Không mở loại khám
  dịch vụ ở UI cho đến khi cả P2 và P3 hoàn tất.
- **P3 chưa làm:** đưa phụ thu vào cả hai truy vấn nguồn DAL, tổng kết, bảng tính
  phí, in/preview và đối soát/thu trọn gói. Các ca hóa đơn ở trên kiểm lớp SQL,
  không phải nghiệm thu quy trình có phụ thu từ đầu đến cuối.
- **P4 chưa làm:** upload/xem/ẩn từ UI, quyền người dùng/audit, xác minh chữ ký ảnh,
  kho UNC/cấu hình/kiểm tệp thất lạc, backup và phục hồi trên máy khác.
- **P5 chưa làm:** audit dữ liệu vận hành và dữ liệu báo cáo nhiều kỳ. Chưa sửa
  dữ liệu người thật hoặc thêm dữ liệu trình diễn vào DB ứng dụng.
- **P6 chưa nghiệm thu toàn bộ:** chưa có ảnh chụp UI mới hoặc ma trận TN/HD/XR
  đầu cuối; các kiểm hồi quy đã chạy chỉ chứng minh phạm vi nền tảng hiện tại.

Không chạy migration lên `QLBenhVienRangHamMat` đang sử dụng. Chỉ đọc danh sách
database để chọn môi trường kiểm, mọi phép ghi ở DB `RHM_Hospital_Test_...`.
Do đó chưa tạo `.bak` vận hành; bản SQL gốc lưu ở
`obj/Sprint3Foundation/QLBenhVienRangHamMat.before.sql`, SHA-256
`1EEB1EF8232485C86B18700BB12A81149268D9B7E495E64F6E6E06541BA56BCB`.

Chưa commit/push. File Word gốc được giữ nguyên. Trước phát hành sau P2–P6,
cần sao lưu DB và kho ảnh cùng thời điểm, dừng ghi khi nâng cấp và thử phục hồi;
không hạ schema bằng cách xóa cột/bảng đã có chứng từ mới.
