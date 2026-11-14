# Kiểm tra nhập liệu các chức năng được yêu cầu

Yêu cầu: rào dữ liệu sai trên hồ sơ bệnh nhân, khám điều trị, đơn thuốc, dịch vụ thực hiện, lịch sử bảo trì, nhà cung cấp, hóa đơn/thanh toán, báo cáo/thống kê và danh mục hệ thống. Lỗi và hướng dẫn sửa phải nằm ngay dưới trường tương ứng. Chỉ kiểm tra các trường người dùng có thể nhập; giữ quyền bỏ trống trường tùy chọn, quyền thao tác và trạng thái chứng từ hiện có.

Thiết kế: mở rộng `KiemTraNhapLieu` và style `TruongNhapLieu` đang có, nối các biểu mẫu thực tế vào cùng cơ chế. Kiểm tra khi rời ô và trước khi lưu/tải báo cáo, kiểm tra lại các ô đang lỗi khi sửa. Đối chiếu schema cho kiểu, độ dài, precision/scale và các quan hệ ngày; kiểm tra các giá trị lựa chọn và điều kiện nghiệp vụ trước khi gọi BUS. Không tự sửa, cắt hoặc âm thầm chuyển giá trị sai thành dữ liệu hợp lệ.

Phương án: dùng cơ chế hiện có giúp đồng nhất với dự án và giảm lặp; tự viết validator riêng trên mỗi trang dễ lệch quy tắc, thay toàn bộ bằng MVVM vượt phạm vi. Chọn mở rộng cơ chế hiện có theo đánh giá triển khai.

Không tự suy diễn giới hạn y khoa, liều thuốc hoặc quy định BHYT mới. Không thay đổi dữ liệu SQL hiện hữu. Giữ các thay đổi chưa commit đang có; thực hiện tại workspace vì chúng là nền tảng thực tế của yêu cầu.

Kiểm chứng bằng bộ chạy WPF STA không ghi dữ liệu nghiệp vụ, kiểm tra cả thông báo hiển thị và dữ liệu hợp lệ, build solution và bộ kiểm cấu trúc hiện có.
