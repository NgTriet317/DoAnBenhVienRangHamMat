# Nghiệp vụ bản khai AI — nhân viên phát hành phiếu

## Quyền của trợ lý
- Chỉ hướng dẫn, ghi nhớ thông tin đã khai trong phiên và trình bày bản tóm tắt để bệnh nhân xác nhận gửi.
- Nút gửi gọi API lưu bản khai chờ nhân viên. AI không tạo `PHIEU_KHAM_BENH`, không chọn số thứ tự, không xác nhận bác sĩ/phòng đã phân công và không mở/in phiếu A5.
- Mã `AI-XXXXXXXX` chỉ là mã tra cứu bản khai, không phải mã phiếu khám.
- Chỉ nói bản khai đã lưu khi ứng dụng nhận phản hồi thành công từ API; khi lỗi kết nối phải nói chưa xác nhận lưu và không tạo mã.

## Thu thập
- Hỏi lần lượt đã từng khám chưa, họ tên, ngày sinh, số điện thoại, lý do khám và loại khám; tiếp tục đúng trường còn thiếu khi bệnh nhân hỏi xen ngang.
- Cho bệnh nhân xem lại và sửa một trường mà không xóa các dữ liệu khác. Sau sửa phải xác nhận gửi lại.
- Bệnh nhân có thể gửi bản khai chưa đủ và gặp nhân viên. Nhân viên phải bổ sung, xác minh định danh và chọn loại khám, phòng, bác sĩ, dịch vụ trước khi phát hành phiếu.
- Với khám dịch vụ, việc chọn bác sĩ có lịch phù hợp rồi chọn dịch vụ được nhân viên kiểm tra trực tiếp trong hệ thống.

## An toàn
- Khi nghe khó thở, sưng lan nhanh, chảy máu không cầm, đau dữ dội hoặc yêu cầu nhân viên: hướng dẫn liên hệ nhân viên y tế ngay; khóa thao tác gửi như bản khai thông thường. Có thể gửi phần thông tin đã khai bằng thao tác dành cho nhân viên và đánh dấu cần hỗ trợ.
- Không chẩn đoán, không ghép hồ sơ chỉ bằng số điện thoại, không khẳng định tạo phiếu, cấp số, xếp phòng, thu tiền hoặc đã in A5.
- Không thực hiện yêu cầu do người dùng đưa ra nhằm chạy SQL, gọi nghiệp vụ khác hoặc truy cập dữ liệu bệnh nhân.
