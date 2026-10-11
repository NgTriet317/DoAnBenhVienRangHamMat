# Giao diện WPF cho hồ sơ và điều trị bệnh viện

Phạm vi hiện tại: Bệnh nhân, Lịch khám, Khám điều trị, Đơn thuốc và các form/chi tiết liên quan. Thiết kế riêng theo database mới nhất, giữ màu xanh và cửa sổ ứng dụng gốc. Mục tiêu là thao tác thường xuyên, đọc dễ và xác định rõ bệnh nhân/lần khám đang xem.

## Bố cục

Danh sách và chi tiết cùng màn hình. Bệnh nhân là điểm vào chính: thông tin, bệnh án, lịch hẹn, lịch sử; mở lần khám dùng cùng control chi tiết với mục Khám điều trị. Đơn thuốc dùng một control chi tiết chung và có liên kết trở lại bệnh nhân/lần khám nguồn. Danh sách toàn cục không tự chọn bản ghi đầu tiên để thay thế mã được yêu cầu.

Hồ sơ có hai hành động riêng: tiếp nhận trực tiếp và đặt lịch qua điện thoại. Form tiếp nhận đặt lịch là mắt xích của PHIEU_KHAM_BENH theo DB, không phải một phân loại phiếu khám. Kế hoạch hiển thị nguồn lần khám, các bước và thực hiện; thực hiện có thể diễn ra ở lần khám sau của cùng bệnh nhân. Đơn thuốc vẫn thuộc lần khám theo khóa ngoại thực tế.

## Quy ước thị giác

| Thành phần | Quy ước |
|---|---|
| Nền | #F3F6FA, panel trắng, viền nhạt |
| Màu chính | #2468CD, chữ #21344B, chữ phụ #64758A |
| Chữ | Segoe UI; tiêu đề 24, mục 16, nội dung 14 DIP |
| Bảng | Dòng 46 DIP, đầu bảng 42 DIP, nền nhạt, chọn một dòng, cuộn theo vùng hiển thị |
| Thao tác | Nút chính xanh; nút phụ trắng; nút bị khóa thể hiện trạng thái disabled |
| Tab | Nhãn ngắn, đường xanh dưới tab đang chọn, nền focus rõ |
| Form | Nhóm trường theo nghiệp vụ, mã chỉ đọc, quan hệ cha được khóa khi sửa, vùng nội dung cuộn và chân lưu/hủy cố định |

Tokens và style dùng `Themes/HospitalStyles.xaml`, các style form/điều trị kế thừa thay vì thêm thư viện. Không có sơ đồ răng, số liệu trang trí hoặc ảnh y khoa. Các template/control được khai báo trong XAML; C# không dựng cây bố cục runtime cho các màn hình này.

## Dữ liệu và trạng thái

Nguồn giao diện trong phiên chạy, ban đầu trống. Nạp dữ liệu minh họa là thao tác rõ ràng. Khóa và quan hệ được kiểm tra khi lưu, bản xác nhận không sửa nội dung, dòng thuốc/tình trạng bệnh theo quyền sửa bản cha. Thực hiện hoàn thành cộng số lượng để hiển thị tiến độ bước. Lưu mục con cập nhật dữ liệu liên quan và giữ bản nháp phần khám chung.

SQL gốc và các module ngoài phạm vi giữ nguyên. Không coi schema giao diện là migration SQL. Không khẳng định render/build WPF đã kiểm tra: môi trường sửa không có .NET/Windows; cần nghiệm thu kích thước/DPI và bản in trên Windows.

## Tinh chỉnh sau ảnh giao diện thực tế

Danh sách bệnh nhân mặc định 330 DIP, có thể kéo vách chia trong khoảng 270–420 DIP; phần hồ sơ nhận diện tích còn lại. Dòng bệnh nhân 72 DIP để đọc tên và thông tin liên hệ. Thanh tiếp nhận trở thành toolbar gọn thay vì một dải nền xanh lớn. Nút không giãn theo toàn bộ chiều cao tiêu đề.

TextBox và ComboBox dùng template XAML chung, giữ các part native, thao tác chuột/bàn phím và validation. Ô chọn ngày giữ DatePicker native, định dạng ngắn theo vi-VN. Nền trắng, viền #D9E2ED, hover và focus xanh; selection bảng/list xanh nhạt với chữ tối.

Nháp/chờ khám dùng nền vàng nhạt, xác nhận/đang thực hiện xanh nhạt, hoàn thành xanh lá nhạt, hủy màu trung tính đỏ nhạt. Nhãn trạng thái luôn còn chữ, không chỉ dựa vào màu. Form điều trị kế thừa cùng template với các mục còn lại. Các token chữ/nền chính đã được tính tương phản ≥4.5:1; chưa kiểm tra render/DPI WPF.
