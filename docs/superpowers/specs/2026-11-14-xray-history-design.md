# Ảnh X-quang cho lịch sử khám của bộ dữ liệu đồ án

Người dùng yêu cầu bổ sung ảnh có thể mở xem, liên kết nhất quán với tình trạng bệnh và dịch vụ đã thực hiện. Bốn lượt LS006/BN006, LS017/BN028, LS023/BN034, LS035/BN046 có tình trạng chấn thương hàm mặt và dịch vụ chụp nhưng bảng ảnh đang trống.

## Cách thực hiện

Dùng cơ chế lưu tệp và metadata hiện có. Đóng gói bốn ảnh panorama tham khảo công khai có giấy phép cùng nguồn/giấy phép, liên kết đúng MaLS và MaDVThucHien. Ghi rõ ảnh tham khảo trong ghi chú hiển thị; đây không phải phim chụp của các bệnh nhân giả lập. Không suy diễn kết quả chẩn đoán từ ảnh, sửa bệnh lý, sửa giá, mở lại lượt đã chốt hoặc vô hiệu hóa trigger. Ngày nhập và nhật ký phải phản ánh lần nhập bổ sung, không giả ngày chụp.

## Các phương án

Chỉ thêm metadata không đáp ứng yêu cầu xem ảnh. Dùng đường dẫn tuyệt đối máy hiện tại gây lỗi khi chuyển máy. Giữ kho cấu hình hiện có, hỗ trợ đường dẫn tương đối khi đã cho phép kho cục bộ, và đóng gói tệp qua project WPF là cách phù hợp với dự án; lựa chọn triển khai này do người thực hiện đưa ra trong phạm vi yêu cầu bổ sung đồng bộ.

## Ràng buộc và kiểm chứng

SQL Server 2016 SP1+, .NET 8/WPF. Giữ kiểm tra định dạng, SHA-256, quyền xem và trạng thái chỉ xem lịch sử. Công cụ nhập có chế độ chỉ kiểm tra, nhập trong giao dịch, chạy lại không tạo bản sao và từ chối liên kết sai. Chạy trên database hiện tại sau khi sao lưu, xuất lại snapshot SQL; cài mới từ SQL phải có cùng liên kết. Kho dùng chung hoặc RHM_XRAY_ROOT do người vận hành đặt vẫn được ưu tiên. Kiểm trên database thử, xác nhận ảnh thật được giải mã và giao diện lịch sử mở được nhưng không cho tải/ẩn.
