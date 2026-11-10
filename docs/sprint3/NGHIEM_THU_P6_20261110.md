# Nghiệm thu nội bộ P6 — 10/11/2026

Lần chạy `20261110_114040_eae3f295` đạt **32/32 bước nội bộ**; ma trận từ
`KeHoachTrienKhai.docx` có **20/21 tiêu chí PASS**, **XR04 chờ hạ tầng**.
`LocalStatus=PASS`, `ReadyForDeployment=false`. Chưa đủ điều kiện kết luận
nghiệm thu phát hành: cần kho UNC qua hai máy thật, phục hồi trên máy khác
và xác nhận cấu hình/quyền vận hành.

Bằng chứng đã chọn nằm trong [thư mục P6](evidence/20261110-p6/README.md), gồm
[kết quả JSON](evidence/20261110-p6/acceptance.json),
[ma trận JSON](evidence/20261110-p6/matrix.json),
[32 bước](evidence/20261110-p6/steps.csv), log và17 ảnh UI chứa dữ liệu giả.
Log đầy đủ, script cài thử, `.bak` và kho ảnh nằm trong
`obj/P6Evidence/run-20261110_114040_eae3f295` và `obj/XrayEvidence`, được bỏ qua
bởi Git. Không đưa snapshot hay ảnh người bệnh thật vào bộ evidence.

## Bản mã và phạm vi

Nhánh `Sprint3_tan`, HEAD `1cafa7d2096a3e954d381e473520cba8bab4b9dd`.
Các thay đổi P0–P6 còn trong working tree, chưa commit/push/publish. Giữ nguyên
việc bỏ `fo-dicom.Desktop` của người dùng khỏi bốn project. Hai manifest nguồn
[trước](evidence/20261110-p6/source-before.json) và
[sau](evidence/20261110-p6/source-after.json) giống nhau (481 tệp); không sửa nguồn
biên dịch/công cụ trong lúc chạy. Tài liệu Markdown không thuộc snapshot này.

SHA-256 SQL chính đã kiểm:
`C32BC5A606FE7F615DB862259D6637B07C5C26F1F70568DC027360DAAD2CD1BC`
(1.112.290 byte). Baseline được lấy trực tiếp từ blob git của HEAD trên, giữ
nguyên byte, hash
`37938EC64813917BDDA239A0F5E0B99A108F0934884C24A418DFFD63BB244699`.
Hash khi chuẩn hóa CRLF khớp mốc P0
`1EEB1EF8232485C86B18700BB12A81149268D9B7E495E64F6E6E06541BA56BCB`;
khác hash thô do newline, không phải khác nội dung SQL.

Mọi ghi thử dùng tên mới `RHM_Hospital_Test_P6_20261110_114040_eae3f295_*`
và `RHM_Hospital_Test_P5Demo_P6_20261110_114040_eae3f295`. Database nghiệp vụ
`QLBenhVienRangHamMat` chưa được nâng cấp hay ghi bởi bộ nghiệm thu. Bộ kiểm
không tự sửa bệnh án thiếu mã, tạo share, thay ACL hoặc cấu hình phát hành.
SELECT tổng hợp sau nghiệm thu ngày10/11 vẫn ghi nhận62 bảng,11 người bệnh,
11 lượt khám và11 hóa đơn trên DB này; không đọc hoặc xuất chi tiết bệnh án.

## Ma trận theo Word

PASS dưới đây có nghĩa đạt ca đã thực thi trong môi trường nội bộ. Các ca
đồng thời dùng các kết nối độc lập trên cùng máy; chỉ XR04 có điều kiện hạ
tầng máy khác trong yêu cầu. Không cộng số assertion của các suite thành số
tiêu chí độc lập.

| Mã | Nội dung đã đối chiếu | Kết quả | Bằng chứng |
|---|---|---|---|
| TN01 | Thường chọn dịch vụ trước, không phụ thu; điều phối ngoài lịch theo quyền | PASS | [service](evidence/20261110-p6/service.log), [UI thường](evidence/20261110-p6/ui/ordinary-reception.png) |
| TN02 | Dịch vụ khóa trước khi chọn bác sĩ; ca/hiện diện/phòng hợp lệ; backend lưu loại/phí/audit | PASS | [service](evidence/20261110-p6/service.log), [UI dịch vụ](evidence/20261110-p6/ui/service-reception.png) |
| TN03 | Chặn thiếu ca, ca nháp/hết giờ, vắng/nghỉ phép, thiếu phòng, tài khoản khóa và bác sĩ nghỉ việc | PASS | [service](evidence/20261110-p6/service.log), gồm ca qua ngày và đúng giờ kết thúc |
| TN04 | Đổi NV002→NV003 khác chuyên khoa xóa dịch vụ, đổi phòng, lọc MaCK; chặn dịch vụ ngừng/chưa áp dụng | PASS | [service](evidence/20261110-p6/service.log), [UI đổi bác sĩ](evidence/20261110-p6/ui/service-doctor-switch.png) |
| TN05 | Hủy ca sau khi chọn rồi lưu: BUS/DAL và UI từ chối, không sinh phiếu/hồ sơ mồ côi | PASS | [service](evidence/20261110-p6/service.log), [UI lỗi ca](evidence/20261110-p6/ui/service-missing-shift.png) |
| TN06 | Ba yêu cầu đồng thời: cùng bệnh nhân cùng phiếu, khác bệnh nhân khác số; loại/phí đúng | PASS | [concurrent](evidence/20261110-p6/concurrent.log) |
| TN07 | Lịch hẹn giữ Thông thường/0đ; retry giữ phiếu, không âm thầm đổi loại khám | PASS | [service](evidence/20261110-p6/service.log) |
| HD01 | Một dòng phụ thu30.000đ, tổng130.000đ; retry/in không nhân đôi | PASS | [service](evidence/20261110-p6/service.log), [hóa đơn](evidence/20261110-p6/ui/service-invoice.png) |
| HD02 | Thường và phiếu lịch sử giữ0đ, SQL chặn phụ thu cho phiếu thường | PASS | [foundation-new](evidence/20261110-p6/foundation-new.log), [service](evidence/20261110-p6/service.log) |
| HD03 | Nhánh hóa đơn cũ và chốt hoàn tất dùng cùng nguồn phụ thu/giá | PASS | [service](evidence/20261110-p6/service.log) |
| HD04 | Sửa TongTien sai bị51007; dịch vụ giả30.000đ không thay nguồn phí (53404); chặn giảm giá ăn vào phí | PASS | [service](evidence/20261110-p6/service.log), [foundation-new](evidence/20261110-p6/foundation-new.log) |
| HD05 | Thu từng phần/đủ, hoàn hợp lệ, thuốc hai lô, giá chốt và bản in khớp | PASS | [service](evidence/20261110-p6/service.log), [P5](evidence/20261110-p6/p5.log) |
| XR01 | JPG/PNG thật, đúng lần khám, ảnh lịch sử và phân công theo quyền | PASS | [xray](evidence/20261110-p6/xray.log), [UI](evidence/20261110-p6/ui/xray-editor.png) |
| XR02 | Tệp hỏng/quá dung lượng/giả MIME/tên đường dẫn/mất tệp có lỗi, không ghi metadata mồ côi | PASS | [xray](evidence/20261110-p6/xray.log) |
| XR03 | Bác sĩ ngoài phạm vi, tài khoản khóa/không quyền và nhân viên không được giao bị chặn | PASS | [xray](evidence/20261110-p6/xray.log) |
| XR04 | Ẩn giữ metadata/audit; backup+restore SQL/15 ảnh và viewer đã kiểm cùng máy; thiếu máy khác | **CHỜ HẠ TẦNG** | [xray-restore](evidence/20261110-p6/xray-restore.log), [UNC một máy](evidence/20261110-p6/smb-loopback.json) |
| DB01 | Cài mới64, nâng cấp62→64; fingerprint cột cũ giữ nguyên; chạy lại hai lần và sau lỗi mô phỏng | PASS | [preserve-legacy](evidence/20261110-p6/preserve-legacy.log), [rerun1](evidence/20261110-p6/preserve-rerun-1.log), [rerun2](evidence/20261110-p6/preserve-rerun-2.log), [ngắt](evidence/20261110-p6/interrupt-sealed.log) |
| DB02 | FK/CHECK tin cậy, trigger hoạt động; DBCC cài mới/nâng cấp/rerun; tiền/kho/ca đối soát | PASS | [constraints](evidence/20261110-p6/constraints.log), [foundation-upgrade](evidence/20261110-p6/foundation-upgrade.log), [sql-sync](evidence/20261110-p6/sql-sync.log), [P5](evidence/20261110-p6/p5.log) |
| BC01 | Ba tháng, lượt xác nhận và MaDMTT hợp lệ; không nhân đôi bệnh/lượt, tách người/lượt | PASS | [P5](evidence/20261110-p6/p5.log), [conditions.csv](evidence/20261110-p6/conditions.csv), [reports](evidence/20261110-p6/reports.log) |
| BC02 | Hóa đơn khác dòng tiền theo kỳ; phụ thu đúng kỳ và không cộng hai lần | PASS | [monthly-finance.csv](evidence/20261110-p6/monthly-finance.csv), [lineage.csv](evidence/20261110-p6/lineage.csv), [P5](evidence/20261110-p6/p5.log) |
| RG01 | Luồng khám/đơn/thu/kho, ca/công/lương/quyền/nhật ký; Admin mở27 phân hệ | PASS | [clinical](evidence/20261110-p6/clinical.log), [sql-sync](evidence/20261110-p6/sql-sync.log), [smoke27](evidence/20261110-p6/smoke27.log), [build](evidence/20261110-p6/build-schema.log) |

HD04 kiểm xóa dòng phụ thu khỏi hóa đơn nháp rồi xác nhận: hóa đơn thiếu phí
bắt buộc bị chặn. Hóa đơn nháp được phép chưa hoàn chỉnh trong lúc dựng chi
tiết; không thể chốt/xác nhận để bỏ qua phụ thu.

## Kết quả bộ kiểm

| Bộ kiểm | Kết quả |
|---|---|
| Điều phối/guard/proof logic | 16/16 |
| Nền tảng trên cài mới và nâng cấp | 35/35 mỗi bộ |
| Giữ cột cũ qua nâng cấp; giữ dữ liệu qua hai lần chạy lại | 62/62;64/64;64/64 bảng |
| Chứng từ có phụ thu/ảnh qua rerun, lỗi mô phỏng và sửa tiếp | Đạt từng marker xác minh |
| Đồng bộ ET/DAL, lịch ca/công/lương | 32/32 |
| Tiếp nhận/thu phí/UI; đồng thời | 31/31;1/1 |
| Luồng khám/đơn thuốc/kho/thanh toán | 47/47 |
| Admin đăng nhập/mở27 phân hệ | 2/2 |
| Ảnh X-quang | 32/32 |
| Phục hồi và BUS/viewer mở lại | 15/15 tệp;2/2 ca sau restore |
| P5 đối soát/báo cáo; guard công cụ | 21/21;7/7 |
| Báo cáo SQL/render | 41 dòng PASS |
| DBCC/FK/CHECK | Không vi phạm/không tắt/không mất tin cậy trên ba DB |
| Build Debug/Release và thủ tục kiểm schema | Thành công,0 lỗi |

DEMO có18 người/26 lượt trong08–10/2026, lần lượt8/12/6 lượt. Hóa đơn
1.000.000/1.500.000/750.000đ; thu ròng900.000/1.500.000/750.000đ.
Chênh100.000đ là khoản thu tháng11, không phải sai lệch. Có13 dòng phụ thu,
26 đơn vị thuốc xuất từ300 còn274. Dữ liệu và CSV có nhãn DEMO; bảng truy
nguồn nối lượt→hóa đơn→thanh toán→xuất thuốc trong `lineage.csv`.

## Sửa lỗi và kết quả rà soát

Nghiệm thu phát hiện lỗi trường bác sĩ còn hiện khi chuyển Khám dịch vụ sang
Thông thường sau lần lưu thất bại. `ConfigureMode` nay gọi
`ClinicalFormInput.ClearErrors(this)`. Ca UI đã RED27/1→GREEN28/0 trước khi
mở rộng thành31 ca; ảnh UI thường cuối đã được kiểm bằng mắt.

Rà soát độc lập chỉ ra thiếu ca thực thi TN04/HD04 và nguy cơ helper chỉ đọc
summary cuối. Đã bổ sung đổi bác sĩ khác MaCK, dịch vụ ngừng hoạt động,
sửa TongTien trực tiếp và dịch vụ giả phụ thu trên SQL/BUS/WPF thật. Các guard
nghiệp vụ có sẵn đã chặn đúng, không tắt trigger. Helper đã RED15/1→GREEN16/0
với hai summary liên tiếp lỗi→xanh, rồi chạy lại toàn bộ32 bước. Không còn
finding Critical/Important đã xác nhận chưa xử lý trong phạm vi rà soát này.

## Phần chờ và giới hạn

UNC loopback `\\localhost\D$` trên `DESKTOP-C9VCE8A` đã xác minh hash15 ảnh,
mở13 ảnh còn hoạt động qua BUS và giữ2 ảnh ẩn. Đây vẫn là một máy.
Các JSON giả lập hai hostname chỉ kiểm logic so sánh, không tính là bằng
chứng hạ tầng. Chưa nhận cấu hình SQL/UNC hoặc máy thứ hai để thực thi.

Phần còn thiếu là hai proof từ hai máy thật cùng SQL/database/UNC, phục hồi
SQL+ảnh sang máy khác và mở được qua ứng dụng, cùng xác minh tài khoản/quyền
thực tế. Hướng dẫn và lệnh đã chuẩn bị tại
[triển khai/rollback](HUONG_DAN_TRIEN_KHAI_VA_ROLLBACK.md). Không tự nâng cấp DB
nghiệp vụ, chỉnh ACL hoặc phục hồi ghi đè để hoàn tất hình thức.

Đồng thời ba quầy được kiểm bằng ba tác vụ/kết nối trong một máy, chưa thử
đứt mạng đúng thời điểm commit hoặc mọi biến thể liên kết trên SMB. Build đạt
0 lỗi; không coi đây là kết luận đã loại hết cảnh báo có sẵn trong mã.

## Chạy lại

Phần chuẩn bị tiếp theo đã có [gói XR04 chạy bằng runtime](BO_NGHIEM_THU_XR04.md)
và [kết quả kiểm gói](KET_QUA_BO_NGHIEM_THU_XR04_20261110.md). Bốn script
bàn giao thêm sau mốc P6 không thay C#/XAML/SQL cũ; builder đối chiếu đủ481
mục nguồn đã kiểm. Đây là phụ lục chuẩn bị, không đổi trạng thái XR04 trong
JSON nghiệm thu lịch sử.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraNghiemThu.ps1 -MayChu .
```

Runner tạo mã lần chạy mới, từ chối database/thư mục đã tồn tại, khóa các bộ
kiểm dùng scratch chung và dừng khi exit khác0, có FAIL, thiếu marker/summary
hoặc số kiểm thấp. Mã thoát0 chỉ xác nhận phạm vi nội bộ; xem `matrix.json`
và `ReadyForDeployment` trước khi kết luận bàn giao vận hành.
