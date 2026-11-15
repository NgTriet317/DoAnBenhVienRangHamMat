# Làm lại Tổng quan điều hành

**Yêu cầu:** Hiển thị thông tin hợp lý, dễ hiểu, phù hợp dự án bệnh viện răng hàm mặt; tiếp tục giữ tab bệnh lý đã hoàn thiện.

**Thiết kế lựa chọn:** Bốn chỉ số Số bệnh nhân đến khám, Tổng lượt khám, Tiền thực thu, Tổng lịch hẹn. Bên dưới: tình hình lịch hẹn, tiền thu/hoàn, phiếu tiếp nhận và hồ sơ khám cần rà soát, xu hướng tháng, dịch vụ thực hiện nhiều, bảng tháng và đối chiếu bốn chỉ số. Bệnh lý và danh tính bệnh nhân xem tại chuyên đề riêng. Có nút mở các chuyên đề liên quan.

**Phương án:** Mở rộng tổng quan hiện có thay vì tạo màn hình mới. Các số liệu chọn kỳ theo ngày nghiệp vụ; trạng thái lịch/hồ sơ phản ánh khi tải, không diễn giải thành trạng thái tại thời điểm lịch sử. Tiền thực thu = Thu − Hoàn xác nhận; không gọi là lợi nhuận hoặc lấy từ hóa đơn. Bệnh nhân DISTINCT trên toàn khoảng, không cộng người giữa các tháng. Dịch vụ xếp theo số lượt khám khác nhau có dịch vụ xác nhận, theo thời gian thực hiện. Không cộng các đơn vị tiền/người/lượt vào cùng thang đo.

**Dữ liệu và quyền:** Gói tổng hợp nhiều kỳ và các bảng toàn khoảng đọc trên một kết nối/giao dịch. Giữ quyền tổng hợp hiện có; không trả định danh bệnh nhân hoặc nhân viên. CSV dùng chính dữ liệu đã tải, kể cả bộ lọc, thu/hoàn, lịch hẹn và dịch vụ. Đổi kỳ/chuyên đề/phiên phải xóa cả gói cũ; chưa tải/lỗi dùng — và khóa xuất.

**Công việc:**

1. [x] Viết kiểm tra thất bại cho bốn chỉ số và nội dung tổng quan mới.
2. [x] Thêm ET/DAL/BUS gói Tổng quan: số đếm độc lập, tổng toàn kỳ DISTINCT, thu/hoàn và trạng thái/dịch vụ; kiểm SQL bằng fixture nhiều lần khám, nhiều dịch vụ, hoàn tiền, kỳ rỗng.
3. [x] Nối gói vào so sánh và CSV, thêm bố cục riêng trong XAML/Controls; giữ tab bệnh lý và các chuyên đề khác. Kiểm trạng thái trống, đổi bộ lọc, điều hướng và cỡ 1024/1366/1920.
4. [x] Chạy kiểm SQL/WPF, rà soát độc lập, lưu ảnh, tài liệu và kết quả.

Hoàn tất: 58/58 kiểm SQL/WPF đạt; build Release thành công; diff không lỗi whitespace. Rà soát độc lập xác nhận số đếm/tiền/ngày/quyền/giao dịch; các điểm kỳ chỉ có dịch vụ, đổi chỉ tiêu biểu đồ và số tiền âm trong CSV đã sửa và kiểm hồi quy. Ảnh và log tại XemTruoc/tong-quan-dieu-hanh.
