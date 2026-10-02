# Kiểm tra bản cập nhật lịch, thống kê và chi tiết — 29/09/2026

## Đã chạy

- Checker C#: `SUCCESS: 34 tables, 245 fields, 244 list columns; 41 XAML files; fullscreen navigation source checks passed.`
- Kiểm tra XML tên control không trùng trong mỗi view; cột bảng có Binding; hai màn lịch có công tắc chế độ; hai vùng ChartView; tab Thuốc; nguồn bệnh viện tách Thu kho/Chi kho.
- Biên dịch toàn bộ C# bằng Roslyn .NET 8 với bộ tham chiếu WindowsDesktop và khai báo tạm lớp/trường từ XAML: không lỗi/cảnh báo. Tệp tạm không đưa vào gói.
- Rà soát nguồn độc lập phát hiện hai lỗi: thiếu đường vào form tạo mục con, và hóa đơn chi kho không được gom khi xem giao dịch kho. Đã thêm tạo mục con có mã cha cố định và gom cả nguồn thu/chi trước khi lọc.
- SQL giữ nguyên so với gói Blue Care trước; không thêm bảng thuốc, bảng nhật ký hoặc trường thu/chi.

## Giới hạn

Chưa build WPF hoàn chỉnh hoặc chạy ứng dụng trên Windows. Biên dịch C# với khai báo tạm không thay thế XAML compiler, kiểm tra tương tác và bố cục/DPI thực tế. Lịch/đồ thị bắt đầu trống vì chưa có kết nối dữ liệu; không nạp bản ghi giả. Không khẳng định số liệu nghiệp vụ đã được tính đúng vì phần nghiệp vụ chưa triển khai.

## Kiểm tra trực tiếp bằng Visual Studio 2022

1. Chạy KiemTra.bat, F5, vào Home. Xác nhận fullscreen không controlbox.
2. Ca làm và Lịch khám: chuyển Bảng dữ liệu/Lịch theo tuần, tuần trước/sau, hôm nay và chọn ngày. Cuộn giờ: tiêu đề ngày phải còn nhìn thấy. Thử DPI 100%/125% trên 1366×768 và 1920×1080.
3. Khi nạp nguồn kiểm thử: ca qua đêm phải xuất hiện ở ngày kế; nhiều lịch trùng giờ chia cột nhỏ. Lịch khám chỉ đánh dấu giờ bắt đầu. Dòng thiếu giờ không bị tự đặt vào một giờ giả.
4. Thống kê: chuyển Bảng dữ liệu/Đồ thị. Dữ liệu trống phải hiện thông báo; khi nạp ChartPoint, bệnh viện là đồ thị đường, kho có hai dãy cột thu và chi.
5. Với bảng đã nạp dữ liệu: nhấp một lần/nhấp đúp tiêu đề/vùng trắng không mở chi tiết. Double-click một dòng mở đúng bản ghi; mục con chỉ thuộc khóa cha đó. Mật khẩu không hiện trong chi tiết.
6. Double-click tiếp bảng con rồi Quay lại/Esc: trở về đúng cấp. Từ chi tiết cha, bấm Thêm mục con: mở form tạo mới với mã liên kết được điền và khóa. Home trở về màn chính.
7. Vật tư: SetMaterials với loại Thuốc và loại khác; hai tab không lẫn dữ liệu, không ảnh hưởng bộ lọc của nhau. Thêm thuốc chọn sẵn loại Thuốc; lô thuốc theo MaVT.
8. Thanh toán/Báo cáo: nạp chứng từ thu và chi riêng, kiểm tra hai tab không trộn. Khi xem giao dịch kho, hóa đơn liên quan được tìm trong cả nguồn thu và chi. Không cộng kho vào bệnh viện.
9. Nhật ký: chỉ bốn nhóm hoạt động được phép xuất hiện; bộ lọc hoạt động chính xác. Không có tự ghi nhật ký hoặc xác thực đăng nhập.

Xem HUONG_DAN_NGUON_DU_LIEU.md để biết điểm nối nguồn dùng cho các kiểm tra có dữ liệu. Mọi kiểm tra ở mục này cần chạy trên Windows; không được coi là kết quả runtime đã thực hiện.
