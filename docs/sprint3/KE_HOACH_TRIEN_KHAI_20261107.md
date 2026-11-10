# Triển khai kế hoạch ngày 07/11/2026

Nguồn yêu cầu: `KeHoachTrienKhai.docx`, nhánh `Sprint3_tan`, SHA đầu
`1cafa7d2096a3e954d381e473520cba8bab4b9dd`. Người dùng cho phép điều chỉnh và bắt đầu triển khai.

## Thiết kế và thứ tự

Giữ kiến trúc WPF .NET 8 → BUS → DAL → ET/SQL Server và một file SQL triển khai.
Luồng hiện tại: tiếp nhận → lần khám → xác nhận thực hiện → ký đơn/hoàn tất →
`DAL_HOADON.TaoTuKhamHoanTat` → thu trọn gói → in/báo cáo. Nhánh hóa đơn cũ
đọc `EligibleChargesSql`; nhánh hoàn tất đọc `CompletionChargesSql`.

Đã triển khai **P0/P1 (SQL/ET)**, **P2/P3 (tiếp nhận, thu phí, in)** và
**P4 (ảnh X-quang)** và **P5 (đối soát/dữ liệu báo cáo)**. Lựa chọn khám dịch vụ
đã được nối đủ luồng tiếp nhận đến hóa đơn; ảnh có tải/xem/ẩn và sao lưu/phục hồi.
P5 có audit chỉ đọc, bộ DEMO riêng26 lượt/18 người/ba kỳ, đối chiếu tiền/kho/bệnh
lý và nhãn dữ liệu giả trên báo cáo. P6 nội bộ đã đạt ngày10/11/2026;
nghiệm thu đầy đủ còn chờ hạ tầng hai máy và xác nhận vận hành.

Các cách đã cân nhắc:

- Thêm phí như dịch vụ thực hiện: ít sửa hơn nhưng làm sai thống kê dịch vụ; loại bỏ.
- Lưu phí chỉ trong giao diện/DAL: không bảo vệ ghi trực tiếp SQL; loại bỏ.
- Nguồn thu riêng liên kết phiếu tiếp nhận, ràng buộc SQL và snapshot phí: chọn
  theo tài liệu; thêm bảo vệ bất biến loại khám/phí và chống chuyển dòng phụ thu sang lượt khác.

Điều chỉnh kỹ thuật do đối chiếu mã:

- `TR_CTHD_TinhTien`, `TR_HD_ChotHoanTat`, view chi tiết có định nghĩa cuối tại
  pha HospitalWorkflow3, sau các phiên bản đầu file. `TR_HD_XacNhan` nằm ở pha
  hợp đồng tài chính trước đó. Cập nhật định nghĩa cuối, không tắt trigger.
- Pha HospitalWorkflow3 luôn dựng lại CHECK ba nguồn. Phải tránh dựng lại CHECK
  cũ khi cột nguồn phụ thu đã tồn tại, nếu không chạy SQL lần hai sẽ thất bại.
- Giá phụ thu chỉ nằm trên phiếu; yêu cầu tiếp nhận tương lai chỉ nhận loại khám,
  không nhận số tiền do UI cung cấp. Không thêm trường yêu cầu trước khi DAL xử lý.
- Metadata ảnh được kiểm tra đường dẫn tương đối, MIME/đuôi, hash, trạng thái ẩn
  ngay trong SQL; BUS xác minh và giữ byte ảnh gốc. P4 đã có UI tải/xem/ẩn và audit.
- P4 thêm bảng `PHAN_CONG_ANH_XQUANG` vì dịch vụ ở bước Thực hiện đã chốt người/giá;
  đổi nhân viên tải ảnh không được sửa chứng từ. Schema hiện64 bảng. Có lịch sử
  phân công thì phạm vi cũ không tự khôi phục sau hủy/đổi người; tài khoản bị khóa
  vẫn được thay thế bằng người đang hoạt động. Lịch sử đã hủy được giữ nguyên.
- Không hồi tố loại khám, không nạp hồ sơ giả vào DB đang sử dụng; thử trên DB riêng.
- Làm việc trực tiếp trên nhánh yêu cầu đã có, không tạo nhánh/worktree khác.

## Gói đầu: SQL/ET

- [x] P0: ghi SHA/hash, sao chép SQL gốc vào `obj/Sprint3Foundation`, build nền.
- [x] P1.1: thêm kiểm thử SQL thực, chạy RED trên schema cũ.
- [x] P1.2: thêm loại khám/phí, nguồn thu thứ tư, ràng buộc, trigger, view và bảng ảnh.
- [x] P1.3: đồng bộ ET, công cụ kiểm cấu trúc/schema và kiểm chạy lại có phụ thu tồn tại.
- [x] P1.4: cài mới, nâng cấp từ SQL gốc, chạy lại; so sánh dữ liệu cũ, build/hồi quy.

## Các gói tiếp theo

- [x] P2: UI đến trực tiếp, bác sĩ trước, dịch vụ theo chuyên khoa, kiểm ca/phòng tại DAL.
- [x] P3: hai đường tạo hóa đơn, bảng tính phí, in, đối soát và thu/hoàn tiền có phụ thu.
- [x] P4: kho ảnh dùng chung, upload/xem/ẩn theo quyền, audit, đối soát tệp và phục hồi.
- [x] P5: audit chỉ đọc, đối soát bất thường; dữ liệu nhiều kỳ chỉ trong DB trình diễn riêng.
- [ ] P6: đủ ma trận TN/HD/XR/DB/BC/RG, ảnh UI và hướng dẫn vận hành/rollback.

P6 ngày10/11: đã hoàn tất kiểm nội bộ32/32 bước, ma trận20 PASS và XR04 chờ
hạ tầng; có17 ảnh UI dữ liệu giả, source manifest ổn định và hướng dẫn backup/
nâng cấp/xác minh/rollback. Bộ tiếp nhận mở rộng31/31, helper16/16; đã sửa lỗi
UI giữ thông báo trường sau khi đổi loại khám. UNC loopback đọc15 ảnh/mở13,
giữ2 ảnh ẩn trên một máy; chưa thay cho kiểm hai máy hay phục hồi từ xa.
Xem [biên bản P6](NGHIEM_THU_P6_20261110.md) và
[hướng dẫn vận hành](HUONG_DAN_TRIEN_KHAI_VA_ROLLBACK.md). Giữ ô P6 chưa hoàn
tất và `ReadyForDeployment=false` cho đến khi có bằng chứng phần còn thiếu.

Tiếp ngày10/11: hoàn tất gói công cụ chạy XR04 bằng runtime, không cần source/
SDK trên máy chạy. Gói đã giải nén kiểm17/17 guard và12/12 bước thực tế, gồm
backup/restore15 ảnh và viewer. Không thay mã nghiệp vụ đã qua P6; so đủ481
mục nguồn cũ, thêm bốn script bàn giao. Xem
[kết quả gói](KET_QUA_BO_NGHIEM_THU_XR04_20261110.md) và
[hướng dẫn mang sang máy thứ hai](BO_NGHIEM_THU_XR04.md). Chưa nhận cấu hình
máy/SQL/UNC thứ hai, nên ô P6 vẫn giữ chưa hoàn tất.

SHA-256 SQL gốc: `1EEB1EF8232485C86B18700BB12A81149268D9B7E495E64F6E6E06541BA56BCB`.
Không commit/push khi toàn bộ gói chưa đạt nghiệm thu; trạng thái và bằng chứng xem
`KET_QUA_NEN_TANG_20261107.md`, `KET_QUA_KHAM_DICH_VU_20261107.md` và
[kết quả P4](KET_QUA_ANH_XQUANG_20261107.md). P4 kiểm phục hồi sang database/thư
mục khác trên cùng máy; kiểm UNC thực giữa hai máy chờ môi trường triển khai.

P5 ngày08/11: [kết quả đối soát/trình diễn](KET_QUA_DOI_SOAT_P5_20261108.md).
DB đang dùng vẫn62 bảng, chỉ đọc:0 sai xác định trong kiểm đã chạy;11 lượt thiếu
mã cần bệnh án đối chiếu và3 mục schema thiếu. Không tự sửa. DB DEMO riêng
`RHM_Hospital_Test_P5Demo_20261108_125355` có schema64 và26 chuỗi chứng từ giả.

SQL ngày10/11 tiếp tục được đồng bộ theo yêu cầu người dùng: checker tổng
bao phủ ảnh/phụ thu/audit, đối chiếu21 thuộc tính ảnh/phân công và đúng FK/
index, guard chuỗi pha không báo READY sau lỗi. Đạt44/44 nền tảng và32/32
bước nghiệm thu mới `20261110_124022_78d39f69`; snapshot/dữ liệu cũ giữ nguyên.
Xem [kết quả SQL và cách áp dụng](DONG_BO_DATABASE_20261110.md). Chưa chạy
migration vào DB nghiệp vụ, chưa thay trạng thái XR04 chờ máy khác.
