# Cấu hình database sau mỗi lần cài đặt

Người dùng đã yêu cầu và xác nhận: lần mở đầu sau mỗi lần cài đặt phải hiện màn hình cấu hình SQL, kể cả khi cấu hình cũ hoặc appsettings kết nối được. Các lần mở sau tự dùng kết nối đã xác nhận. Đóng/hủy màn hình không hoàn tất cấu hình. Không xóa database, hồ sơ hoặc mật khẩu đã mã hóa.

## Cách thực hiện

Bộ cài MSI đặt DWORD `HKCU\Software\RangHamMat\CaiDat\CanCauHinhDatabase=1`. Ứng dụng kiểm tra cờ trước khi tự khôi phục SQL. Chỉ sau khi màn hình kiểm tra, lưu kết nối và trả thành công, ứng dụng ghi `0`. MSI sở hữu giá trị này nên gỡ cài đặt sẽ xóa; cài lại đặt `1` dù ProductCode hoặc phiên bản không đổi. Khi thiếu/sai kiểu giá trị, ứng dụng yêu cầu cấu hình để cả bản chạy trực tiếp cũng có hành vi lần đầu rõ ràng. Cấu hình đã lưu vẫn được điền sẵn.

Đây là lựa chọn triển khai của người thực hiện trong yêu cầu đã được xác nhận. Chỉ xóa AppData không đủ vì appsettings có sẵn kết nối; chỉ so phiên bản/ProductCode không nhận diện được cài lại cùng bộ cài. Cờ do MSI quản lý không cần custom action, không cần quyền ghi Program Files và phù hợp bộ cài per-user hiện tại.

Bộ cài ẩn tùy chọn Everyone để giao diện cài đặt nhất quán với phạm vi per-user của cờ. Nút tự tìm kết nối chỉ điền thông tin, không tự lưu/đóng hộp thoại; cần bấm Lưu và tiếp tục. Giữ thứ tự ưu tiên nguồn chuỗi kết nối hiện có: môi trường, profile mã hóa, appsettings.

## Phạm vi và kiểm chứng

Thêm dịch vụ nhỏ cho quyết định khởi động/cờ registry; nối vào App trước bước đăng nhập; thêm giá trị registry vào vdproj. Dùng kiểm tra hành vi với nhánh registry ngẫu nhiên riêng và callback thay SQL, không truy cập dữ liệu nghiệp vụ. Build WPF và MSI nếu công cụ hiện có hỗ trợ; kiểm tra bảng Registry trong MSI để xác nhận gói phát hành thực sự đặt cờ. Chưa được xem là đã thử gỡ/cài thật nếu chỉ kiểm tra gói và mô phỏng vòng đời.
