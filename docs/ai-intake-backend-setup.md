# Cài đặt luồng bản khai AI chờ nhân viên

AI chỉ lưu dữ liệu vào `AI_PHIEN_TIEP_NHAN` và `AI_TIEP_NHAN_NHAP`. `POST /api/ai/intake/receipts` trả HTTP 410; chỉ nhân viên xác minh trong `Phiếu hướng dẫn > Bản khai AI` mới gọi `BUS_AI_TAOPHIEU.taoPhieu` để tạo phiếu chính thức và A5.

## Cài SQL

1. Cài SQL/07_KioskAI_TiepNhan.sql nếu database test chưa có hai bảng bản khai.
2. Nếu dùng chat web của KioskServer, cài SQL/14_AI_ChatThat.sql theo hướng dẫn hiện hành.
3. Chạy SQL/17_AI_BanKhai_NhanVienDuyet.sql. Script chỉ thêm cột nullable `LoaiKham` khi chưa có và có thể chạy lại.
4. SQL/12_AI_TuDoiChieuBenhNhan.sql là luồng phát hành phiếu của nhân viên; kiểm tra cài đặt hiện hành trước khi cài lại.

Không xóa SQL/15_AI_ServiceIntake.sql hay dữ liệu/phiếu cũ. Phiếu cũ vẫn đọc được trong danh sách nhân viên; SQL/17 không đổi schema phiếu khám.

## Chạy backend

Chạy `RangHamMat.Kiosk.Api` dưới service account của backend. Cấu hình chuỗi kết nối chỉ trong secret store hoặc environment của service (`RHM_CONNECTION_STRING`); không đặt trong WPF, JSON phát hành hay log. Nếu API được expose ngoài loopback, dùng HTTPS với chứng thư máy chủ hợp lệ, firewall và xác thực thiết bị theo chính sách triển khai. Endpoint gửi bản khai hiện chỉ nhận kết nối loopback, áp dụng rate limit và chỉ nhận schema bản khai.

Endpoint bản khai là `POST /api/ai/intake/drafts`. Cùng một `SessionId` được dùng lại khi retry để không sinh phiên trùng. Mã `AI-XXXXXXXX` là mã rút gọn tra cứu bản khai, không phải `MaPhieu`.

## WPF

`AI/Config/IntakeDraft.json` chỉ chứa cờ tính năng, route và timeout. KioskServer được khởi chạy cục bộ bởi WPF và nhận cấu hình SQL từ environment hiện hữu của process; không thêm khóa Groq hoặc thông tin đăng nhập vào file JSON. `PhieuCreation.json` được giữ tương thích ngược nhưng WPF không đọc file này. Groq tiếp tục lấy khóa từ cấu hình môi trường hiện tại.

Build lại Kiosk API và WPF trước khi chạy. Xác nhận tạo phiếu chỉ có ở màn hình nhân viên; AI không cấp số thứ tự, không tự chọn phòng/bác sĩ/dịch vụ và không mở phiếu A5.
