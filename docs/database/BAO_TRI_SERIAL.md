# Bảo trì theo serial thiết bị

Mỗi dòng THIET_BI là một tài sản vật lý có SoSerial duy nhất. Nhập kho số lượng lớn tiếp tục tạo từng thiết bị với serial riêng. Mỗi phiếu BAO_TRI_THIET_BI liên kết qua MaTB tới đúng tài sản đó; serial được hiển thị trực tiếp ở hàng chờ, lịch sử và biểu mẫu.

## Sử dụng

1. Trong Trang thiết bị, đánh dấu đúng serial **Hư hại**. Khi mở Lịch sử bảo trì, tab **Thiết bị hư hại** đọc lại SQL và hiển thị các serial chưa có phiếu mở.
2. Chọn serial rồi **Lập phiếu bảo trì**. Thiết bị/serial được khóa trên phiếu; chọn nhân viên phụ trách, ngày dự kiến, nhập nội dung, chi phí và lịch tiếp theo nếu có.
3. **Lưu chưa xác nhận** cho phép mở lại sửa, xác nhận hoặc hủy. **Lưu và xác nhận** lưu nội dung và xác nhận trong cùng giao dịch. Xác nhận khóa nội dung, chưa bắt đầu bảo trì thực tế.
4. Chọn phiếu **Đã xác nhận**, bấm **Tiến hành bảo trì**: phiếu chuyển **Đang thực hiện**, đúng serial đó chuyển **Đang bảo trì** tại Trang thiết bị.
5. Bấm **Hoàn thành** ở phiếu đang thực hiện: hệ thống ghi giờ kết thúc SQL, chuyển phiếu **Đã hoàn thành** và serial trở lại **Đang hoạt động**.
6. Chỉ phiếu **Chưa xác nhận** được **Hủy**. Phiếu hủy giữ nguyên lịch sử và thiết bị vẫn hư hại; có thể lập phiếu mới sau đó. Phiếu đã xác nhận không sửa nội dung hoặc hủy. Phiếu hoàn thành/hủy chỉ được xem.

Tìm theo serial, mã/tên thiết bị, phòng, mã phiếu hoặc nhân viên. Hai cột mã và serial được giữ cố định khi cuộn ngang. Chọn **Tải lại** để đọc thay đổi từ phiên làm việc khác; màn Trang thiết bị đọc lại dữ liệu khi được mở lại.

## Quy tắc dữ liệu

- Một serial chỉ có một phiếu đang mở (Chưa xác nhận/Đã xác nhận/Đang thực hiện). Bảo vệ bằng unique index ở SQL, không chỉ kiểm giao diện.
- MaTB của phiếu không đổi sau khi lưu. Serial và nguồn hàng hóa của tài sản có lịch sử bảo trì không được sửa để tránh đổi danh tính lịch sử.
- Không xóa vật lý phiếu bảo trì, kể cả nháp. Không cho bỏ qua bước xác nhận/thực hiện hoặc khôi phục phiếu đã kết thúc.
- NgayBatDau là ngày dự kiến. NgayXacNhan, NgayThucHien và NgayKetThuc do hệ thống ghi theo giờ SQL; không nhập/sửa tay. Không bắt đầu trước ngày dự kiến.
- Chi phí không âm; lịch tiếp theo khi lập/sửa phiếu phải sau ngày dự kiến bắt đầu. Lịch hẹn có thể trở thành quá hạn khi một lần bảo trì kéo dài; lịch sử vẫn được giữ.
- TinhTrang là tình trạng vật lý; TrangThai quản lý tài sản/phân phòng được giữ nguyên. Không tự ngưng hoặc đổi tình trạng của thiết bị đang có phiếu mở. Khi đang bảo trì, Trang thiết bị hiển thị đúng **Đang bảo trì**.
- Chuyển trạng thái, giờ hệ thống và tình trạng thiết bị cùng commit/rollback; kiểm tra bản gốc để chặn ghi đè dữ liệu từ phiên cũ.

## File SQL và dữ liệu cũ

Chỉ chạy toàn bộ `Database/QLBenhVienRangHamMat.sql`. Pha6 bổ sung cho **THIET_BI** và **BAO_TRI_THIET_BI**, không tạo bảng mới; tổng vẫn60 bảng. Phần SQL tồn tại trước tác vụ được giữ nguyên. Hai cột mới của bảo trì là NgayXacNhan/NgayThucHien nullable, kèm CHECK, unique index, trigger bảo vệ/đồng bộ và thủ tục kiểm tra. Các khóa ngoại thiết bị/nhân viên và unique serial hiện có tiếp tục được dùng.

Khi nâng cấp lần đầu:

- Nháp → Chưa xác nhận; Hoàn thành → Đã hoàn thành. Phiếu cũ Đã xác nhận đã có ngày kết thúc được coi là lịch sử Đã hoàn thành.
- Tốt/Bình thường/Hoạt động tốt → Đang hoạt động; Hỏng/Cần bảo trì → Hư hại; Ngừng sử dụng → Ngưng sử dụng. Phiếu đang thực hiện đồng bộ đúng tài sản thành Đang bảo trì.
- Giữ serial, mã liên kết, nội dung, chi phí và ngày kết thúc thực tế của lịch sử. Không suy đoán giờ xác nhận/thực hiện chưa từng được lưu; giao diện hiển thị **Không có dữ liệu thời điểm**.
- Ngày kết thúc của nháp/lần đang làm cũ được giữ trong ghi chú như ngày dự kiến cũ, rồi trường NgayKetThuc dành cho lúc hoàn thành thực tế.
- Nếu thiếu serial thật, nhiều phiếu mở cho cùng thiết bị, tình trạng không xác định hoặc thiết bị đã ngưng còn phiếu mở, pha nâng cấp dừng để đối chiếu; không tự xóa/hủy phiếu hoặc tạo serial thay thế.

Sao lưu database trước khi áp dụng. Các lần kiểm bảo trì dùng database riêng. Ngày30/10, database chính đã được phục hồi và đồng bộ, gồm10 lịch sử bảo trì cũ đã hoàn thành; xem [phục hồi dữ liệu](PHUC_HOI_VA_DU_LIEU_SQL.md).

## Kiểm chứng

```powershell
# Cài database thử và kiểm các nghiệp vụ khám/thu tiền hiện có:
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraQuyTrinhBenhVien.ps1 -TaoDatabaseKiemThu -Database RHM_Hospital_Test_MaintSerial_20261029
# Kiểm vòng đời và SQL/DAL/BUS bảo trì:
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraBaoTriSerial.ps1 -Database RHM_Hospital_Test_MaintSerial_20261029
# Kiểm schema đầy đủ trên database cần bàn giao:
.\KiemTra.bat -KiemTraSql -Database QLBenhVienRangHamMat
```

Bộ kiểm bảo trì từ chối database ngoài tiền tố RHM_Hospital_Test_. Các ca nghiệp vụ/bulk nhập rollback. Ca đồng thời dùng một thiết bị đủ điều kiện đang có; nếu database thử trống, bộ kiểm tự tạo fixture riêng để hai kết nối cùng thấy rồi dọn đúng các dòng do nó tạo. Kiểm chứng trên database trống kết thúc với0 thiết bị,0 phiếu,0 danh mục fixture còn lại. Không chạy fixture lên database sử dụng. Bộ kiểm form thật lưu một số phiếu vào database thử riêng để kiểm cả commit và cache, không tạo vào database sử dụng.

Kết quả:58/58 kiểm tra bảo trì trên bản cài mới và bản hiện tại;32/32 kiểm SQL V9;47/47 khám/thu tiền; đăng nhập/mở27 phân hệ; build Debug/Release đạt. Form WPF thật đã kiểm lưu nháp, sửa, lưu và xác nhận, các bước trạng thái, khóa form cuối và rollback cả cache lẫn SQL. Kiểm nâng cấp/rerun giữ nội dung, tiền, serial, ngày lịch sử; schema và fingerprint dữ liệu của58 bảng khác không đổi. Rà soát độc lập không còn lỗi Critical/Important; thông báo thời điểm lịch sử thiếu đã được sửa để không hiển thị sai là chưa xác nhận.

Nhật ký trong phiên kiểm chứng nằm tại obj/MaintenanceSerial_20261029, obj/MaintenanceSerialChecks và obj/MaintenanceUi_20261029.
