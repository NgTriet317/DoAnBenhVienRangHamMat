# Rà soát liên kết dữ liệu và nghiệp vụ — 14/11/2026

Đã dựng database thử bằng **toàn bộ** `Database/QLBenhVienRangHamMat.sql`, gồm snapshot và tất cả pha đồng bộ. Đọc đủ **64 bảng, 3.285 dòng**; không phát hiện bản ghi mồ côi hoặc sai lệch liên kết chéo trong các quy tắc đã kiểm. Dữ liệu đóng gói khớp số dòng và SHA256 của cả 64 bảng với manifest ngày 12/11; chạy lại toàn bộ SQL vẫn giữ nguyên các dấu băm.

Không có bản ghi sai được xác định để sửa trong snapshot. File SQL chính và database đang sử dụng được giữ nguyên trong đợt này. Mọi phép thử ghi diễn ra trên database thử riêng; dữ liệu fixture được rollback hoặc dọn đúng mã do bộ kiểm tạo. Việc kiểm tra có thể tái lập bằng các công cụ và bằng chứng bên dưới, không phải cam kết mọi tình huống của ứng dụng đều đã được chạy qua giao diện.

## Các liên kết đã đối chiếu

| Phạm vi | Dữ liệu/điều kiện được kiểm | Kết quả |
| --- | --- | --- |
| Toàn bộ schema | Số dòng từng bảng, toàn bộ FK thực tế, CHECK và trigger đang bật/trusted, ánh xạ ET/DAL | Không phát hiện sai lệch |
| Bệnh nhân → hồ sơ → lịch hẹn → tiếp nhận → lần khám | Đúng bệnh nhân, bác sĩ, phòng, thời điểm và tình trạng có mã | 36 lần khám được giữ đầy đủ |
| Tình trạng → kế hoạch → dịch vụ thực hiện | Nguồn tình trạng/bước kế hoạch, bệnh nhân/lượt, dịch vụ, lượng và nguồn hóa đơn | Không phát hiện sai lệch; phân biệt liên kết cùng bệnh nhân qua lượt cũ với sai bệnh nhân |
| Đơn thuốc | Bản ký có chi tiết và thông tin chốt để in, bác sĩ của lượt, đơn thay thế cùng lượt | API in được 21 đơn đang hiệu lực |
| Hóa đơn → thu → xuất → bàn giao | Tổng tiền/chi tiết, nguồn dịch vụ/đơn/xuất/phụ thu cùng lượt, một lần Thu đủ của v2, lượng từng dòng và trạng thái phiếu | 18 bàn giao hợp lệ; 13 hóa đơn chờ thu tính phí được |
| Thử lại thu và giao | Gọi lại cùng yêu cầu, không tạo thêm hóa đơn/khoản thu hoặc xuất thuốc lần hai | Đạt; phép thử rollback |
| Nhập/xuất kho | Hàng–kho–lô–nhà cung cấp cùng chứng từ, tổng nhập, tồn hiện tại/lịch sử, hạn dùng và nguồn thuốc | Không phát hiện sai lệch |
| Nhân sự–ca–phòng–lịch hẹn | 1.102 lịch làm việc, 16 lịch hẹn, chuyên khoa/phòng, trùng tài nguyên, nghỉ phép và phân công thay thế | 25 quy tắc đối chiếu độc lập không phát hiện sai lệch xác định |
| Chấm công/lương | Nguồn chấm công, bảo vệ số tiền lịch sử, tính/chốt lương và chống sửa nguồn đã chốt | 32/32 ca kiểm tích hợp đạt |
| Thiết bị–mua–bảo trì–thanh lý | 18 thiết bị có nguồn mua đúng ngày/nhà cung cấp; TB016 đang bảo trì; TB015 thanh lý và thu hồi phòng | 78/78 ca kiểm tích hợp đạt |
| Tham chiếu không khai báo FK | Sáu mã bác sĩ/phòng/ca/lịch thay thế trong XAC_NHAN_DOI_LICH_KHAM | Bảng hiện trống; kiểm tường minh không có mã mồ côi |

Các bảng chi tiết lương/hoa hồng, tăng ca, mẫu hướng dẫn, ảnh X-quang hoặc xác nhận đổi lịch chưa có phát sinh không bắt buộc phải có dữ liệu. Không tạo chứng từ hay ảnh giả chỉ để mọi bảng đều có dòng. Kho ảnh dùng chung vẫn cần cấu hình tại máy triển khai như hướng dẫn hiện có.

## Những trạng thái đang chờ, không phải lỗi liên kết

- **DT012/DT013/DT014** thuộc LS019/LS020/LS022, có HD012/HD013/HD015 ở trạng thái Nháp, phiếu Chờ thanh toán và chưa xuất thuốc. Chưa có bàn giao là đúng quy trình; chỉ tạo bàn giao sau khi thu đủ. Đã sửa cảnh báo nhầm của công cụ kiểm liên thông đối với các đơn chưa thu này.
- **CK001/CK002/CK003/CK004/CK007** chưa có ca được xác nhận/Có mặt vào ngày kiểm 14/11. Cần người vận hành xác nhận lịch và hiện diện thực tế trước khi tiếp nhận khám dịch vụ. Không tự điểm danh bác sĩ hoặc hồi trạng thái trong snapshot theo ngày chạy kiểm.
- **354 ca** đã có giờ vào/ra nhưng đang chờ đối soát chấm công; **14 ca ngày 12/11** có giờ vào nhưng chưa có giờ ra. Đây là các bước nhân sự cần tiếp tục xử lý; không tự ghi giờ ra hoặc phê duyệt công.
- **BL001–BL010** là bảng lương V8 lịch sử kỳ **01–10/10/2026**; BL003 đã chốt. Theo quy tắc SQL hiện có, tạo kỳ V9 đủ tháng 10 bị chặn bởi lỗi **52333** vì trùng kỳ cũ. Đây là giới hạn chuyển đổi lịch sử, không phải chứng cứ số tiền sai. Không kéo dài kỳ, tính lại bảng đã chốt hoặc gán công/hoa hồng giả để vượt chặn. Các kỳ mới tuân thủ quy trình V9 hiện hành.
- **BT001–BT010** là lịch sử bảo trì cũ chưa ghi mốc xác nhận/bắt đầu theo schema mới. SQL chủ động giữ lịch sử này; không suy diễn thời điểm còn thiếu.

## Công cụ và chú thích đã cập nhật

`tools/KiemTraDuLieuLienThong.sql` được mở rộng để phát hiện nguồn chéo sai lượt, thiếu dòng đơn đã ký, thiếu/sai giao dịch Thu đủ v2, sai trạng thái phiếu, sai lượng bàn giao, sai nguồn lô và lô NULL của phiếu đã ghi sổ. Dữ liệu lịch sử được phân loại riêng, tránh áp quy tắc v2 lên hóa đơn cũ. Các ca giả lập chỉ SELECT dùng để kiểm khả năng phát hiện lỗi được lưu cùng bằng chứng.

Hóa đơn chờ thu được tạo lúc chốt khám có `TaoKhiHoanTat=1` nhưng `PhienBanQuyTrinh=1`; sau thu mới chuyển phiên bản 2. Vì vậy kiểm trạng thái phiếu nhận diện cả cờ tạo khi hoàn tất, không chỉ lọc phiên bản 2 rồi bỏ sót những phiếu đang chờ thanh toán.

`tools/MaintenanceSerialChecks.cs.txt` sửa điều kiện preflight đã lỗi thời: kiểm 60 bảng lõi sau khi loại cả hai bảng phiên/phân quyền và hai bảng ảnh X-quang bổ sung. Trước sửa bộ kiểm dừng nhầm trên schema 64 bảng; sau sửa đạt 78/78 ca. Không thay nghiệp vụ bảo trì của ứng dụng.

Bổ sung **2.012 dòng chú thích trong 120 tệp** theo [quy ước hiện có](../HUONG_DAN_CHU_THICH_CODE.md):

| Khu vực | Số tệp | Nội dung bổ sung |
| --- | ---: | --- |
| BUS/DAL/ET | 28 | 143 khối mô tả vai trò, hợp đồng và giao dịch |
| WPF | 49 | 253 khối XML documentation và 65 comment XAML |
| Công cụ/script | 43 | 22 tệp C#, 19 PowerShell, 2 SQL; trợ giúp, bước chạy và điều kiện đầu vào |

Sau rà soát, không còn tệp mã nguồn tự viết thiếu hoàn toàn chú thích trong các phạm vi trên. Không ghi chú file sinh tự động, JSON, ảnh, tài liệu hoặc tệp nhị phân. Các dòng chú thích cũ và thay đổi đang làm dở của người dùng được giữ.

Roslyn xác nhận token C# không đổi; cấu trúc 88 XAML, token/cú pháp PowerShell, encoding/BOM và xuống dòng được đối chiếu với baseline riêng chụp **trong đợt này**, không dùng baseline cũ ngày 30/10. Hai chỉnh sửa logic công cụ kiểm tra nêu trên được kiểm riêng, không tính là chỉnh sửa chỉ chú thích.

## Kiểm chứng và tái lập

- Cài mới toàn bộ SQL và chạy lại: thành công, 64/64 bảng khớp số dòng/SHA256 với nguồn và với trước chạy lại.
- Bộ đối soát BUS/DAL: **0 SaiXacDinh, 0 CanDoiChieu**, đọc đủ 64 bảng.
- Kiểm API trên dữ liệu đóng gói: **21 đơn in được, 18 bàn giao hợp lệ, 13 hóa đơn tính phí được**; thử thu/giao lặp rollback.
- Bộ kiểm đồng bộ SQL/chấm công/lương: **32/32**; bảo trì/serial/thanh lý: **78/78**.
- Kiểm hồi quy bộ đối soát: **6/6** phép chiếu chỉ SELECT phát hiện đúng đổi nguồn dịch vụ giữa hai hóa đơn cùng tiền, hóa đơn đã xác nhận thiếu thu, đơn ký rỗng, lô nhập NULL, lô xuất NULL và phiếu chờ thanh toán bị gán Hoàn tất. Dữ liệu gốc không bị sửa bởi các phép chiếu này.
- Build solution **Debug và Release: 0 lỗi**; còn 124 cảnh báo ở mỗi cấu hình, gồm nullable/tên kiểu và NuGet audit không kết nối được nguồn gói trong môi trường kiểm.
- `git diff --check`: đạt.

[Thư mục bằng chứng](evidence/20261114-data-workflow/) lưu manifest SHA256, số dòng từng bảng, đối soát, kết quả API, ca kiểm nhân sự/thiết bị, SQL độc lập và kiểm chú thích. Các bản SQL cài thử và baseline từng tệp giữ tại `obj/DataLinkAudit_20261114/` và `BUS/obj/CommentAudit_20261114/`.

```powershell
# Thay RHM_Hospital_Test_Audit bằng database thử đã cài đầy đủ file SQL chính.
# Các lệnh kiểm chỉ đọc hoặc dùng fixture rollback theo mô tả của từng công cụ.
powershell -NoProfile -ExecutionPolicy Bypass -File tools/DoiSoatDuLieu.ps1 -MayChu . -Database RHM_Hospital_Test_Audit
sqlcmd -S . -d RHM_Hospital_Test_Audit -E -C -b -f65001 -i tools/KiemTraDuLieuLienThong.sql
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ChuanHoaDuLieuDoAn.ps1 -HienThi -MayChu . -Database RHM_Hospital_Test_Audit
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraDongBoSql.ps1 -MayChu . -Database RHM_Hospital_Test_Audit
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBaoTriSerial.ps1 -MayChu . -Database RHM_Hospital_Test_Audit
```
