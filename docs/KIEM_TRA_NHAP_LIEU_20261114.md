# Kiểm tra nhập liệu các chức năng được phân công

Các trường sai hiển thị viền đỏ, thông báo và gợi ý sửa ngay dưới ô nhập. Kiểm tra khi rời ô và trước khi lưu/tải báo cáo; ô đã báo lỗi được kiểm tra lại khi sửa. Nút lưu không bỏ qua trường sai thuộc tab chưa mở: giao diện mở tab, cuộn và đưa focus tới trường cần sửa. Các ô tùy chọn vẫn được bỏ trống theo quy tắc từng biểu mẫu.

| Chức năng | Phạm vi kiểm tra |
| --- | --- |
| Hồ sơ bệnh nhân, bệnh án | Trường bắt buộc, họ tên, ngày sinh không sau hôm nay, điện thoại, nhóm máu, ký tự mã BHYT, giới hạn độ dài, lựa chọn và nội dung hồ sơ. Mã BHYT kiểm tra chữ/số và độ dài lưu trữ; không tự xác nhận quyền bảo hiểm hay tính hợp lệ của thẻ. |
| Khám điều trị | Triệu chứng, chẩn đoán, hướng xử lý, tình trạng, ngày kế hoạch, dịch vụ, số lượng dương, buổi là số nguyên dương, nội dung và bước xác nhận thực hiện. Mô tả tình trạng vẫn tùy chọn như luồng lưu hiện có. |
| Kê thuốc | Kiểm tra toàn bộ dòng, kể cả thuốc chưa chọn: thuốc, số lượng, số ngày, liều, đường dùng, hướng dẫn, lý do thay/bổ sung đơn. Không tự áp giới hạn liều y khoa. |
| Dịch vụ thực hiện | Lịch sử hiện tại chỉ tra cứu dữ liệu đã chốt; giữ cơ chế này. Dữ liệu nhập và xác nhận thực hiện được kiểm tại luồng khám điều trị. |
| Lịch sử bảo trì, thanh lý | Nhân viên, ngày bắt đầu, chi phí, nội dung, thứ tự ngày bảo trì tiếp theo; lý do và giá trị thu hồi. Các khoảng ngày lọc sai hiển thị lỗi dưới ô ngày. |
| Nhà cung cấp | Tên, người liên hệ, email bắt buộc và đúng định dạng; điện thoại/địa chỉ tùy chọn; giới hạn độ dài và trạng thái. |
| Hóa đơn và thanh toán | Bệnh nhân/lượt/nhân viên, giảm giá, số tiền, thu không vượt dư nợ, hoàn không vượt tiền đã thu, lý do hoàn; tiền khách đưa, tham chiếu và đối chiếu chuyển khoản theo nghiệp vụ sẵn có. Đổi loại giao dịch/phương thức cập nhật các lỗi phụ thuộc. |
| Báo cáo và thống kê | Ngày thực nhập, khoảng ngày, chế độ kỳ/so sánh, năm đối chiếu và lựa chọn đang có hiệu lực. Bộ lọc sai chặn tải/xuất, không dùng SelectedDate cũ hoặc âm thầm dùng năm mặc định. |
| Danh mục hệ thống | Trường động theo định nghĩa từng danh mục: bắt buộc, độ dài, số, ngày, danh mục cha và lựa chọn. Kiểm tra trường khóa được nhập/chọn thủ công, không yêu cầu người dùng nhập mã tự sinh. |

Số được đọc không phụ thuộc thiết lập vùng Windows: cho phép dấu phẩy hoặc dấu chấm cho phần lẻ, không dùng dấu phân cách hàng nghìn. Kiểm tra số chữ số lẻ và giới hạn lưu trữ trước khi chuyển kiểu. Trường tiền VND nguyên đồng trong bước thu trọn gói giữ quy tắc nghiệp vụ hiện có. Dữ liệu quá dài được giữ để sửa, thay vì âm thầm cắt bằng MaxLength ở các biểu mẫu đang chỉnh.

Ngày gõ sai không được thay bằng SelectedDate cũ. Người dùng sửa chữ hoặc chọn ngày trên lịch, kể cả chọn lại chính ngày trước đó, để xác nhận giá trị hợp lệ. Đóng lịch khi chưa chọn không tự xóa lỗi.

Ví dụ: số ngày dùng `1,5` báo cần số nguyên dương; số lượng `0` báo cần lớn hơn 0; số tiền `1.000.000` báo bỏ dấu phân cách; email `abc@` báo định dạng và ví dụ `lienhe@example.com`; ngày kết thúc trước ngày bắt đầu báo chọn lại ngày kết thúc.

## Kiểm chứng

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraNhapLieu.ps1 -All -BuildApplication
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBaoCao.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBanGiao.ps1
```

Kết quả tại workspace: **101/101** ca trong bốn bộ kiểm nhập liệu (21 chung/hồ sơ, 19 lâm sàng/kê thuốc, 5 báo cáo/danh mục, 56 tài chính/bảo trì); **17/17** ca hồi quy báo cáo hiện có; kiểm cấu trúc và build **Debug/Release thành công**. Bộ kiểm nhập liệu gồm kiểm hành vi WPF STA, đọc/chuyển kiểu và kiểm cấu trúc các trường XAML; ô chọn dịch vụ được chạy từ template thực tế để kiểm hiển thị lỗi và cập nhật dòng kế hoạch.

Các kiểm tra trên không ghi dữ liệu nghiệp vụ hoặc chạy script khởi tạo database. Chưa thực hiện kiểm thử toàn bộ luồng ghi/đồng thời với SQL Server; các kiểm tra BUS/DAL và giao dịch hiện có vẫn được giữ nguyên.

Rà soát độc lập phát hiện và đã xử lý: giữ lỗi khi sửa dữ liệu vẫn sai; mô tả tùy chọn bị báo bắt buộc; lỗi phụ thuộc phương thức thanh toán; xác nhận lại ngày cũ sau chuỗi ngày sai. Bộ kiểm WPF bao gồm ca chọn ngày và cập nhật lỗi của giao dịch hoàn/thu.
