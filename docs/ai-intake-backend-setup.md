# Cài đặt luồng bản khai AI chờ nhân viên

AI chỉ lưu dữ liệu vào `AI_PHIEN_TIEP_NHAN` và `AI_TIEP_NHAN_NHAP`. `POST /api/ai/intake/receipts` trả HTTP 410; chỉ nhân viên xác minh trong `Phiếu hướng dẫn > Bản khai AI` mới gọi `BUS_AI_TAOPHIEU.taoPhieu` để tạo phiếu chính thức và A5.

## Cài SQL

Chạy toàn bộ `Database/QLBenhVienRangHamMat.sql` trên database cần đồng bộ, sau khi sao lưu dữ liệu hiện hữu. Installer hợp nhất đã gồm các bảng bản khai, thủ tục nhân viên xác minh, loại khám/phụ thu, phân quyền, quỹ phép và lương. Không chạy các bản cài AI rời sau file chính vì chúng thuộc phiên bản trước khi hợp nhất.

Dữ liệu/phiếu AI cũ vẫn đọc được. Phiếu mới do nhân viên lập dùng cùng loại khám và phụ thu với tiếp nhận thông thường; các nguồn phí AI cũ được giữ tương thích và không cộng lần thứ hai.

## Chạy backend

Chạy `RangHamMat.Kiosk.Api` dưới service account của backend. Cấu hình chuỗi kết nối chỉ trong secret store hoặc environment của service (`RHM_CONNECTION_STRING`); không đặt trong WPF, JSON phát hành hay log. Nếu API được expose ngoài loopback, dùng HTTPS với chứng thư máy chủ hợp lệ, firewall và xác thực thiết bị theo chính sách triển khai. Endpoint gửi bản khai hiện chỉ nhận kết nối loopback, áp dụng rate limit và chỉ nhận schema bản khai.

Endpoint bản khai là `POST /api/ai/intake/drafts`. Cùng một `SessionId` được dùng lại khi retry để không sinh phiên trùng. Mã `AI-XXXXXXXX` là mã rút gọn tra cứu bản khai, không phải `MaPhieu`.

## WPF

Màn hình Trợ lý AI của WPF lưu bản khai qua BUS/DAL vào database đã chọn khi khởi động. Client HTTP tương thích dùng `AI/Config/IntakeDraft.json` và endpoint `/api/ai/intake/drafts`; backend nhận `RHM_CONNECTION_STRING` để dùng đúng database. `PhieuCreation.json` được giữ tương thích ngược nhưng WPF không đọc file này. Groq tiếp tục lấy khóa từ cấu hình môi trường hiện tại.

Build/publish `RangHamMat.Wpf` tự dựng và đóng gói Kiosk API từ cùng mã nguồn. Xác nhận tạo phiếu chỉ có ở màn hình nhân viên; AI không cấp số thứ tự, không tự chọn phòng/bác sĩ/dịch vụ và không mở phiếu A5.
