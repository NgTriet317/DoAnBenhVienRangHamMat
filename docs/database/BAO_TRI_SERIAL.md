# Bảo trì và thanh lý theo serial thiết bị

Mỗi dòng THIET_BI là một thiết bị vật lý có SoSerial duy nhất. Lịch sử bảo trì và hồ sơ thanh lý gắn với đúng MaTB/serial đó; nhập kho nhiều thiết bị vẫn tạo từng serial riêng.

## Sử dụng

1. Trong Trang thiết bị, đánh dấu đúng serial **Hư hại**. Mở Lịch sử bảo trì, tab **Thiết bị hư hại** hiển thị các serial chưa có phiếu bảo trì mở và chưa ngưng sử dụng.
2. Chọn serial rồi chọn **Lập phiếu bảo trì** hoặc **Thanh lý thiết bị**.
3. Với bảo trì: chọn nhân viên, ngày dự kiến, nội dung, chi phí và lịch tiếp theo nếu có. **Lưu chưa xác nhận** giữ phiếu để sửa, xác nhận hoặc hủy. **Lưu và xác nhận**, hoặc **Xác nhận** trên phiếu nháp, lập tức chuyển phiếu sang **Đang thực hiện**, thiết bị sang **Đang bảo trì**; không còn bước/nút bắt đầu riêng. Giờ xác nhận và giờ thực hiện được ghi cùng lúc theo SQL Server. Ngày dự kiến tương lai chỉ lưu nháp, chưa xác nhận sớm.
4. **Hoàn thành** ghi giờ kết thúc, chuyển phiếu sang **Đã hoàn thành**, thiết bị về **Đang hoạt động**. Chỉ phiếu **Chưa xác nhận** được sửa/hủy. Không xóa lịch sử.
5. Với thanh lý: chọn nhân viên đang làm việc, nhập lý do và giá trị thu hồi (không âm; tối đa hai chữ số thập phân). Ví dụ nhập `125000,50`, không dùng dấu phân cách hàng nghìn. Bấm **Xác nhận thanh lý** để lưu hồ sơ, ngưng sử dụng thiết bị và thu hồi khỏi phòng cùng giao dịch.
6. Sau lưu thanh lý, hệ thống mở tab **Thanh lý** và chọn đúng hồ sơ vừa lưu. Có thể tìm theo serial, mã/tên thiết bị, nhân viên, phòng hoặc lý do, lọc ngày, xem thông tin đầy đủ và tổng giá trị thu hồi theo bộ lọc.

Thiết bị có phiếu bảo trì mở không được thanh lý. Muốn đổi hướng xử lý của phiếu nháp, hãy hủy phiếu rồi thanh lý từ hàng chờ; phiếu đã thực hiện cần hoàn tất bảo trì trước. Nếu thiết bị tiếp tục hư hại sau sửa, đánh dấu Hư hại rồi thực hiện thanh lý.

## Đồng bộ và bảo toàn dữ liệu

- Một serial chỉ có một phiếu bảo trì mở; SQL có unique index và trigger bảo vệ ngoài kiểm tra giao diện.
- Hồ sơ thanh lý lưu một lần ngay trên THIET_BI: NgayThanhLy, MaNVThanhLy, LyDoThanhLy, GiaTriThanhLy, TenThietBiThanhLy, TenPhongThanhLy, TenNhanVienThanhLy. Ngày do SQL ghi, tên thiết bị/phòng/nhân viên được chụp tại thời điểm thanh lý, không đổi theo danh mục sau này. Khóa ngoại nhân viên được giữ.
- Khi thanh lý, TinhTrang và TrangThai chuyển **Ngưng sử dụng**, MaPhong được xóa để thu hồi thiết bị khỏi phòng. Lịch sử bảo trì, serial, ngày mua, nhà cung cấp và nguồn nhập được giữ nguyên. Không xóa hoặc khôi phục thiết bị đã thanh lý; trigger giữ nguyên hồ sơ kể cả khi ghi trực tiếp từ phân hệ khác.
- Thiết bị đã thanh lý không còn trong hàng chờ bảo trì, không được phân phòng/lập phiếu mới và không xuất hiện trong lọc đến hạn bảo trì. Trang thiết bị đọc trạng thái Ngưng sử dụng; số thiết bị theo phòng không còn bao gồm tài sản này.
- Thiết bị ngưng sử dụng trước đây chưa có hồ sơ thanh lý không được tự coi là đã thanh lý: không suy đoán ngày, nhân viên hoặc giá trị.
- Giá trị thu hồi được lưu để tra cứu tài sản; không tự tạo hóa đơn bệnh nhân, giao dịch thu tiền khám hoặc phiếu xuất kho thuốc/vật tư.
- Lưu/xác nhận bảo trì và thanh lý cập nhật SQL lẫn cache trong một giao dịch. Chỉ báo thay đổi sau commit; rollback khôi phục cả hai. Đối chiếu bản đã xem để chặn ghi đè từ phiên khác. **Tải lại** đọc thay đổi của phiên làm việc khác.

## Cài đặt và nâng cấp

Chạy toàn bộ `Database/QLBenhVienRangHamMat.sql` trước khi dùng bản ứng dụng mới; không cần file nâng cấp riêng. Pha 6 bổ sung trường/ràng buộc/trigger thanh lý trên THIET_BI và thay đổi vòng đời BAO_TRI_THIET_BI, không thêm bảng nghiệp vụ mới.

Phiếu cũ **Đã xác nhận** chuyển sang **Đang thực hiện** khi đồng bộ: giữ giờ xác nhận, nội dung và chi phí cũ, ghi giờ bắt đầu lúc đồng bộ và cập nhật đúng thiết bị. Nếu có phiếu đã xác nhận nhưng ngày dự kiến còn ở tương lai, pha nâng cấp dừng và rollback để đối chiếu lịch trước khi áp dụng; không tự sửa ngày đã khóa. Các phiếu hoàn thành/hủy và ngày thực tế của lịch sử được giữ nguyên. Dữ liệu bảo trì cũ chưa từng ghi giờ xác nhận/thực hiện tiếp tục hiển thị **Không có dữ liệu thời điểm**.

## Kiểm chứng ngày 05/11/2026

```powershell
# Sau khi cài file SQL chính vào database thử có tiền tố RHM_Hospital_Test_:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBaoTriSerial.ps1 -Database RHM_Hospital_Test_AssetDisposal_20261105
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraThanhLyGiaoDien.ps1 -Database RHM_Hospital_Test_AssetDisposal_20261105
```

Bộ kiểm SQL/BUS/DAL rollback dữ liệu fixture: **78 ca đạt**, gồm xác nhận bắt đầu ngay, khóa nội dung, đồng thời lập phiếu, thanh lý trùng, phiếu mở, dữ liệu cũ, tên danh mục dài, lịch sử bất biến và bảo toàn nguồn nhập. Bộ kiểm WPF thật **15 ca đạt**, gồm lưu xác nhận, đồng bộ cache, biểu mẫu thanh lý, tự chọn tab/hồ sơ, tìm kiếm và rollback. Bộ kiểm WPF tạo/commit thiết bị riêng trên database thử và giữ lịch sử để kiểm tra chạy lại SQL; không chạy lên database sử dụng.

Kiểm hồi quy liên quan: **32 ca SQL V9**, **47 ca khám/thu tiền**; build Debug/Release thành công. Nâng cấp từ SQL cũ giữ giờ xác nhận, nội dung, chi phí và fingerprint lịch sử hoàn thành; chạy lại SQL trên database đã có hồ sơ thanh lý thành công. Bằng chứng trong `obj/thanh-ly-*.log` và ảnh form/tab tại `obj/ThanhLyUiChecks/`.
