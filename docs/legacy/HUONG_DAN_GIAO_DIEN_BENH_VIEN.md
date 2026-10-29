# Hướng dẫn thao tác bệnh viện — database v8

Bản 11/10/2026. Các phân hệ yêu cầu trong KeHoach.txt đã dùng SQL thật. Không cần nạp dữ liệu mẫu. Chuẩn bị SQL Server theo README và chọn vai trò có quyền; Quản trị dùng được toàn bộ luồng. Nhân viên, phòng, dịch vụ, thuốc và kho phải có trong danh mục thực tế.

Mã được tạo khi lưu, theo chữ đầu của tên bảng và số thứ tự tối thiểu hai chữ số: BN01, HSBA01, LSKB01, DT01, HD01… Database có dữ liệu sẵn thì tiếp tục từ số lớn nhất, không bắt đầu lại ở 01; các mã cũ vẫn dùng được. Không cần nhập mã mới thủ công.

## Bệnh nhân đến trực tiếp

1. **Bệnh nhân → Thêm bệnh nhân**: nhập họ tên, giới tính, số điện thoại, BHYT và sức khỏe. Ngày sinh không biết có thể để trống. Số điện thoại giữ số 0 đầu.
2. Giữ **Lập hồ sơ bệnh án sau khi lưu** nếu cần; lưu bệnh án cho đúng bệnh nhân. Một bệnh nhân có một bệnh án.
3. Trong hồ sơ chọn **Tiếp nhận trực tiếp**. Nhập phòng, nhân viên, thời gian, lý do khám; lịch hẹn để trống.
4. Chọn tạo lần khám sau tiếp nhận. Bổ sung bác sĩ, phòng, triệu chứng, chẩn đoán, kế hoạch xử lý rồi lưu. Một phiếu tiếp nhận chỉ có một lần khám.
5. Tab **Lịch sử khám** mở lần khám vừa lưu; các tab tình trạng, kế hoạch, dịch vụ, đơn thuốc dùng cùng lần khám nguồn. Đóng/mở lại ứng dụng hoặc Tải lại vẫn thấy dữ liệu đã lưu.

## Đặt hẹn qua điện thoại

1. Từ hồ sơ chọn **Đặt lịch qua điện thoại**, hoặc mở mục **Lịch khám**.
2. Chọn bệnh nhân, bác sĩ, phòng, ca, ngày giờ và thời lượng; nhập lý do. Lưu nháp rồi xác nhận.
3. Kiểm tra cảnh báo lịch chồng giờ. Database kiểm tra lại ràng buộc khi lưu; lỗi được báo và giao dịch được rollback.
4. Khi đến viện chọn lịch → **Tiếp nhận khi đến viện**. Nếu thiếu bệnh án, lập bệnh án trước. Không tiếp nhận trùng lịch đã dùng, hủy hoặc đổi.
5. Hẹn lại lịch đã xác nhận cần ngày giờ mới và lý do. Lưu đồng thời lịch mới và trạng thái **Đã đổi** của lịch cũ; dữ liệu cũ được giữ để tra cứu.

## Khám, kế hoạch và dịch vụ thực hiện

1. Mở lần khám từ hồ sơ hoặc **Khám điều trị**. **Lưu khám chung** trước khi rời phần đang nhập.
2. Thêm tình trạng bệnh với loại, mô tả, mức độ; không có trường vị trí/số răng trong schema.
3. Tạo kế hoạch và chọn nhiều dịch vụ dự kiến. Nhập lần thứ, số lượng, đơn giá; database tính thành tiền từng dòng và tổng kế hoạch.
4. Chọn bước → **Thực hiện**, hoặc thêm dịch vụ ngoài kế hoạch và để trống bước. Chọn người thực hiện, thời gian, số lượng, giá, kết quả.
5. Lưu nháp rồi **Xác nhận** dịch vụ. Trạng thái lưu trong SQL là **Đã xác nhận**; số lượng đã xác nhận được cộng để hiển thị mức hoàn thành bước kế hoạch. Thực hiện một phần vẫn có thể tiếp tục.
6. Kế hoạch có thể tiếp tục ở lần khám khác của cùng bệnh nhân. Dịch vụ phải khớp bước và đúng bệnh nhân. Bản xác nhận giữ nguyên nội dung.
7. **Dịch vụ thực hiện** có tìm kiếm, lọc bệnh nhân/lần khám/trạng thái; mở được hồ sơ, lần khám, đơn thuốc và hóa đơn của đúng nguồn.
8. Xác nhận lần khám trước khi lập hóa đơn; ngày và trạng thái xác nhận được lưu cùng giao dịch.

## Đơn thuốc

1. Từ lần khám chọn **Lập đơn thuốc**. Đơn gắn MaLS, không gắn trực tiếp với kế hoạch.
2. Thêm nhiều dòng thuốc; chọn thuốc, liều, số lượng, số ngày nguyên dương và hướng dẫn.
3. Lưu nháp; có thể sửa hoặc xóa dòng. Toàn bộ đơn lưu cùng giao dịch, không để lại một phần nếu có lỗi.
4. Xác nhận khi thông tin và các dòng hợp lệ. Đơn xác nhận được giữ nguyên; bổ sung bằng một đơn mới.
5. Nút hồ sơ, lần khám, dịch vụ, hóa đơn giữ đúng nguồn. Bản in đơn thuốc dùng thông tin đã lưu.

Số lượng cho phép một dấu phẩy hoặc chấm thập phân, ví dụ 2,5; không nhập dấu phân cách hàng nghìn. SQL chặn số âm, lượng bằng 0, vượt độ chính xác cột hoặc liên kết không hợp lệ.

## Cấp thuốc, hóa đơn và thanh toán

1. Từ đơn hoặc lần khám mở **Hóa đơn / thanh toán**; chọn nhân viên thực hiện.
2. Tab **Cấp thuốc** chọn đơn đã xác nhận và một kho đủ thuốc. Kiểm tra tồn/lô; **Cấp thuốc và ghi sổ** tự phân lô FIFO còn hạn và ghi phiếu xuất. Nếu thiếu bất kỳ thuốc nào, toàn bộ lần cấp được hủy.
3. Tab **Hóa đơn / thu tiền** → **Lập hóa đơn**. Chọn lần khám đã xác nhận; kiểm tra các dịch vụ đã xác nhận và thuốc đã cấp chưa được lập hóa đơn. Nhập giảm giá hợp lệ rồi lưu.
4. Kiểm tra các dòng và số phải thu; **Xác nhận** hóa đơn để thu tiền. Hóa đơn nháp có thể hủy; bản đã xác nhận được giữ nguyên.
5. Chọn **Thu**, số tiền và phương thức → **Ghi nhận giao dịch**. Có thể thu nhiều đợt, mỗi đợt không vượt số còn phải trả.
6. **Hoàn** cần lý do; không vượt tiền đã thu ròng. Danh sách giao dịch và số còn phải trả cập nhật sau lưu.
7. **Xem trước / In… → In / Xuất PDF…** chọn máy in hoặc Microsoft Print to PDF. Hóa đơn dài tự chia trang.

Nguồn dịch vụ/thuốc không thể tính trùng. Giá thuốc chốt khi lập hóa đơn. Dịch vụ/thuốc/hóa đơn khóa lại trong giao dịch để hai quầy không thu hoặc cấp đồng thời vượt giới hạn.

## Khi gặp lỗi

- Không kết nối được: kiểm tra SQL Server, tên database, quyền Windows và RHM_CONNECTION_STRING.
- Form đã cũ: đóng form, Tải lại và mở lại trước khi chỉnh sửa.
- Ràng buộc/liên kết/trạng thái không hợp lệ: sửa thông tin hoặc mở đúng bản nháp; không đổi khóa cha của bản đã lưu.
- Danh mục trống: bổ sung danh mục thật qua công cụ/phân hệ hiện có trước khi tạo hồ sơ liên quan.

WPF đã được kiểm tra trên Windows ở 1366×768, gồm lưu/sửa đơn nhiều dòng, kế hoạch, dịch vụ, phạm vi hóa đơn và phân trang. Máy in vật lý, DPI 125%/150% chưa được kiểm tra; xem THUC_HIEN_KE_HOACH.md.
