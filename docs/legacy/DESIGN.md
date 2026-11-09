# Giao diện WPF cho hồ sơ và điều trị bệnh viện

## Báo cáo và thống kê

Màn hình báo cáo dùng danh mục 248 DIP, bốn nhóm nghiệp vụ thu gọn/mở rộng và tìm không dấu; đã bỏ Tổng hợp và Hệ thống theo yêu cầu. Chuyên đề đang chọn tự mở nhóm tương ứng. Nội dung gồm tiêu đề/hành động xuất cố định, bộ lọc, trạng thái tải, dải bốn KPI trắng, biểu đồ theo kỳ, cơ cấu trạng thái và bảng đối chiếu. Màu nền và xanh chính kế thừa HospitalStyles; xanh ngọc/tím/cam dùng cho chỉ số, không tô nền toàn bộ thẻ. Style riêng ở `Themes/BaoCaoStyles.xaml`. Dưới 820 DIP nội dung, KPI chuyển hai cột, hai biểu đồ xếp dọc; danh mục và nội dung cuộn riêng.

18 chuyên đề đều đọc database thật, gồm các nguồn bổ sung qua `BUS_BAOCAO` / `DAL_BAOCAO`: lương đã chốt, dòng hàng nhập/xuất, hạn dùng từng lô và đơn nghỉ/thay thế. Mỗi điểm biểu đồ khớp đúng một ngày/tuần/tháng trong bảng; giữ cả kỳ không phát sinh, khoảng dài dùng cuộn ngang và không tự gộp kỳ. Lương, vật tư, hạn dùng và đơn nghỉ có bảng chi tiết; số lượng vật tư chỉ cộng cùng mã hàng/đơn vị. CSV chứa bộ lọc và chi tiết tương ứng, bao gồm ngày chốt/ngưỡng hạn dùng. Kết quả cũ bị xóa khi đổi chuyên đề/bộ lọc hoặc làm mới dữ liệu. Xem `HUONG_DAN_BAO_CAO_THONG_KE.md` và ảnh WPF thật ở `XemTruoc/bao-cao-thong-ke`.

## Báo lỗi tại trường nhập

Các ô nhập dùng validation chung được bọc bằng style `TruongNhapLieu` trong `Themes/KieuTruongNhapLieu.xaml`: dòng lỗi đỏ nằm ngay dưới ô, xuống dòng theo chiều rộng và chiếm chỗ trong bố cục. Không lặp lỗi của từng trường ở cuối form; vùng thông báo chung giữ cho lỗi hệ thống hoặc lỗi không xác định được trường. Khi lưu sai, focus và cuộn đưa cả ô cùng dòng lỗi vào vùng nhìn thấy; sửa đúng và rời ô thì thông báo biến mất. Áp dụng cho TextBox, ComboBox, DatePicker và PasswordBox, gồm cả ô ngày/giờ nằm cạnh nhau.

## Nhà cung cấp

Danh bạ nhà cung cấp dùng nền sáng và màu xanh chung; trạng thái hoạt động xanh lá, ngừng hoạt động đỏ nhạt, luôn có chữ. Danh sách hiển thị tên/mã, điện thoại/email và trạng thái; vùng chi tiết có người liên hệ, địa chỉ và số liên kết thiết bị/lô hàng/hóa đơn nhập. Thêm/sửa dùng form 850 DIP, báo lỗi cạnh trường và chân lưu/hủy cố định. Xóa bị khóa khi đã có dữ liệu liên kết. Tìm kiếm, lọc trạng thái, thống kê và dữ liệu liên kết đều lấy từ SQL. Ô nhập và ô bảng tái sử dụng `Themes/DanhMucStyles.xaml` cùng phân hệ bảo trì.

## Lịch sử bảo trì thiết bị

Phân hệ Bảo trì dùng nền, chữ và màu xanh hiện có; thống kê lấy từ SQL, bảng đi cùng panel chi tiết và bộ lọc ngày/trạng thái/từ khóa. Nháp màu vàng, đang thực hiện/xác nhận xanh dương, hoàn thành xanh lá, hủy đỏ nhạt; luôn có nhãn chữ. Cửa sổ 1024 vẫn giữ bảng và chi tiết cạnh nhau; dưới 900 DIP nội dung, chi tiết chuyển xuống dưới. Form dùng các trường đúng schema, cuộn nội dung, chân lưu/hủy cố định và thông báo lỗi tiếng Việt. Style riêng của bề mặt này nằm ở `Themes/BaoTriStyles.xaml`, kế thừa HospitalStyles.

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

Nguồn giao diện đọc từ SQL Server qua ET/DAL/BUS và lưu trong cache trình bày; không còn nút nạp dữ liệu minh họa. Khóa và quan hệ được kiểm tra khi lưu, bản xác nhận không sửa nội dung, dòng thuốc/tình trạng bệnh theo quyền sửa bản cha. Thực hiện hoàn thành cộng số lượng để hiển thị tiến độ bước. Lưu mục con cập nhật dữ liệu liên quan và giữ bản nháp phần khám chung.

Schema hiện tại là v8. Build và kiểm tra render WPF đã chạy trên Windows trong workspace; dữ liệu thử SQL được rollback. Bố cục ứng dụng, biểu đồ, lịch tuần, chi tiết chung và bản in đều có XAML hiện hữu; C# chỉ chuẩn bị dữ liệu, geometry và xử lý sự kiện. Kết quả mới nằm trong KET_QUA_TOAN_BO_CODE_XAML.md.

## Tinh chỉnh sau ảnh giao diện thực tế

Danh sách bệnh nhân mặc định 330 DIP, có thể kéo vách chia trong khoảng 270–420 DIP; phần hồ sơ nhận diện tích còn lại. Dòng bệnh nhân 72 DIP để đọc tên và thông tin liên hệ. Thanh tiếp nhận trở thành toolbar gọn thay vì một dải nền xanh lớn. Nút không giãn theo toàn bộ chiều cao tiêu đề.

TextBox và ComboBox dùng template XAML chung, giữ các part native, thao tác chuột/bàn phím và validation. Ô chọn ngày giữ DatePicker native, định dạng ngắn theo vi-VN. Nền trắng, viền #D9E2ED, hover và focus xanh; selection bảng/list xanh nhạt với chữ tối.

Nháp/chờ khám dùng nền vàng nhạt, xác nhận/đang thực hiện xanh nhạt, hoàn thành xanh lá nhạt, hủy màu trung tính đỏ nhạt. Nhãn trạng thái luôn còn chữ, không chỉ dựa vào màu. Form điều trị kế thừa cùng template với các mục còn lại. Các token chữ/nền chính đã được tính tương phản ≥4.5:1; chưa kiểm tra render/DPI WPF.
