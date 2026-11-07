# Thiết kế hóa đơn và đơn thuốc

Mục tiêu: làm mới phiếu hóa đơn, chi tiết đơn thuốc trong chức năng Đơn thuốc và phiếu in đơn thuốc; rõ thông tin, dễ đọc và thống nhất phong cách bệnh viện.

## Hướng thiết kế

- Đề xuất triển khai: nền trắng, xanh đậm tiết chế, đường phân cách mảnh, Segoe UI; thông tin người bệnh và chứng từ chia nhóm. Bản in vẫn đọc rõ khi in đen trắng.
- Phương án cân nhắc: mẫu hành chính đen trắng có nhiều ô kẻ. Ít khác biệt so với giao diện hiện có, kém thoáng khi hướng dẫn dài.
- Đây là lựa chọn triển khai của người thực hiện theo yêu cầu cải thiện thẩm mỹ; chưa phải lựa chọn được người dùng xác nhận riêng.

## Bố cục

- Hóa đơn: đầu bệnh viện, tiêu đề, mã/trạng thái; khối thông tin; bảng nội dung/SL/đơn giá/thành tiền; khối tổng kết với phải thu và còn phải trả nổi bật; người lập và vùng chữ ký. Giữ toàn bộ các số tiền và thông tin đối chiếu có sẵn.
- Đơn thuốc: đầu bệnh viện, mã đơn/ngày kê; thông tin bệnh nhân/lần khám/bác sĩ; chẩn đoán; danh sách thuốc đánh số, tên thuốc nổi bật, lượng và số ngày rõ ràng; liều, đường dùng và hướng dẫn tách dòng; ghi chú và chữ ký bác sĩ.
- Chi tiết đơn thuốc: tiêu đề/trạng thái rõ, thông tin người bệnh theo nhóm, nội dung thuốc có khoảng cách và đường phân cách; phần bàn giao có vùng riêng. Nguồn đơn cũ thiếu snapshot vẫn hiện mã thuốc và dữ liệu đã lưu.
- Cửa sổ xem trước: thanh công cụ thống nhất, trang giấy trắng trên nền xám, nút in/xuất PDF rõ ràng. Dùng cùng tài liệu đơn thuốc cho xem và in.

## Ràng buộc và kiểm chứng

Giữ WPF .NET 8 và API hiện có, không thêm package. Giữ kiểm tra quyền, trạng thái đã xác nhận, mã chứng từ cố định và đọc lại nguồn trước khi in; giữ tiền và nội dung chỉ định. Không thay SQL/BUS/DAL. Giữ các thay đổi khác đang có trong workspace, làm trực tiếp trên nhánh hiện tại, không commit/push.

Kiểm chứng bằng build, render PNG/XPS tài liệu A4, nội dung dài/nhiều dòng, đầy đủ thuốc và tổng tiền, bản xem trước/chi tiết ở chiều rộng hẹp. Không gửi tới máy in thật trong kiểm thử.
