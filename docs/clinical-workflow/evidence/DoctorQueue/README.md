# Bằng chứng hàng chờ bác sĩ — 28/10/2026

- `red.log`: bản cũ không có màn hàng chờ mặc định, thiếu API phân trang và lọc bác sĩ; còn trả phiếu hủy/lượt đã xong hoặc bỏ phiếu qua ngày. Test GUI thiếu control mới cũng thất bại. Đây là RED trước triển khai.
- `edge-red.log`: bản đầu đã chặn bắt đầu phiếu vừa hủy nhưng chưa làm mới nút/danh sách; đã sửa.
- `review-red.log`:15 đạt/1 lỗi phiên đang khám bị đóng khi ra khỏi bộ lọc Chờ khám, do review độc lập phát hiện.
- `identity-layout-red2.log`:15 đạt/1 lỗi tên người bệnh bị khuất ở800×600.
- `final-checks.log`:16/16 source sau các sửa cuối.
- `published-tests.log`:16/16 trên DLL Release được đóng gói; ảnh `queue-*.png` lấy từ lượt này bằng cửa sổ WPF thật. Dữ liệu trong ảnh là mô phỏng từ bộ cài/fixture.
- `admin-regression.log`:12/12 kiểm tra Admin không bị hồi quy.
- `local-doctor-ui.log`, `local-admin-ui.log`:1/1 và3/3 trên database mặc định; không ghi phiếu khám/bệnh nhân giả vào database đó.
- `delivery-check.log`, `publish.log`: đúng bốn project, XAML/code-behind, SQL52 bảng, Debug/Release0 lỗi và publish thành công. Cảnh báo cũ được giữ trong log.
- `fixture-cleanup.log`, `results.json`: trạng thái dọn hai database kiểm thử và hash bản Release.

Các log đã chuẩn hóa UTF8 và bỏ khoảng trắng cuối dòng. Test phát triển có một lần lỗi biên dịch do đưa bản soạn view vào project harness, một lần thiếu namespace IO và một lỗi ghép đoạn SQL ở bản đầu; đã sửa rồi chạy lại toàn bộ. Không dùng những lỗi harness làm bằng chứng lỗi sản phẩm.

Mã kiểm thử/runner ở `tools/HangChoKhamChecks.cs.txt` và `tools/KiemTraHangChoKham.ps1`. Các ca SQL riêng lẻ rollback; các ca GUI commit vào database fixture để mô phỏng kết nối khác nhau. Xem HANG_CHO_BAC_SI.md để tạo lại fixture sạch và chạy kiểm tra.
