# Đơn giản hóa dbconnect và DAL để trình bày đồ án

Mục tiêu: các hàm thể hiện tuần tự từng bước, tên biến rõ nghĩa, ít LINQ/reflection lồng nhau và ít hàm bọc chuyển tiếp. Giữ nguyên API đang được BUS/giao diện dùng và kết quả xử lý.

- [x] Chụp bản trước khi sửa để đối chiếu SQL, API và cách chuẩn hóa dữ liệu.
- [x] Viết rõ luồng kết nối/giao dịch, cấp mã và kiểm tra dữ liệu trong dbconnect.
- [x] Gom đoạn kiểm tra chỉ đổi trạng thái bị lặp trong DAL; bỏ hàm bọc không thêm xử lý; trình bày SQL và nhánh nghiệp vụ dễ đọc.
- [x] Chạy các kiểm tra persistence, tài chính, kiến trúc và WPF/nhập liệu.
- [x] Viết hướng dẫn trình bày bằng tiếng Việt, chỉ rõ vì sao cần transaction, khóa dữ liệu và kiểm tra bản ghi cũ.

Không đổi schema hoặc dữ liệu SQL, không bỏ kiểm tra đầu vào, chống ghi đè, rollback hay khóa chứng từ. Không thêm thư viện/generic repository/kiến trúc mới.
