# Chú thích toàn bộ code và chuyển bố cục C# sang XAML

**Yêu cầu mới:** giải thích toàn bộ source bao gồm BUS/DAL; mọi giao diện bổ sung phải có XAML hiện hữu để chỉnh sửa và xem bố cục, C# chỉ giữ dữ liệu/tính toán/sự kiện.

**Phạm vi:** giữ mọi thay đổi sẵn có trong workspace; snapshot đầu lượt ở `obj/XamlAudit/baseline`. Bốn phần còn tạo phần tử giao diện trực tiếp là ChartView, ScheduleView, RecordDetails và các dòng bản in XemTruocHoaDon. Constructor mở một Window/UserControl đã có XAML vẫn là nạp màn hình khai báo sẵn, không phải dựng bố cục bằng C#.

- [x] Chú thích 39 lớp DAL theo từng hàm và đoạn xử lý SQL quan trọng; giữ token thực thi.
- [x] Chú thích 39 lớp BUS theo từng hàm và mục đích kiểm tra/điều phối; giữ token thực thi.
- [x] Thêm XAML cho ChartView, ScheduleView và RecordDetails; chuyển nội dung hóa đơn in sang template XAML giữ đầy đủ dòng và phân trang.
- [x] C# chỉ tính dữ liệu, tọa độ/geometry, điều khiển trạng thái và xử lý sự kiện; không new layout/control hay dùng XamlReader để tạo bố cục thay thế.
- [x] Bổ sung kiểm tra kiến trúc trên toàn WPF; kiểm tra control thật cho các trường hợp trống/có dữ liệu, lịch ca qua đêm và chồng giờ, chi tiết bản ghi và hóa đơn dài.
- [x] Cập nhật tài liệu giải thích, phạm vi AI và kết quả kiểm chứng; build và chạy các kiểm tra hiện có.

**Cách phối hợp:** sử dụng skill dispatching-parallel-agents cho ba phạm vi file độc lập DAL, BUS, RecordDetails/bản in. Root xử lý ChartView/ScheduleView, tích hợp và kiểm chứng. Không commit/push.

**Rủi ro cần kiểm tra:** binding model theo template không vô tình ghi ngược property chỉ đọc; thông tin nhạy cảm không hiện ở chi tiết chung; lịch qua đêm không bị cắt; đổi collection/resize vẫn cập nhật; hóa đơn nhiều dòng đủ trang và đủ nội dung khi in.
