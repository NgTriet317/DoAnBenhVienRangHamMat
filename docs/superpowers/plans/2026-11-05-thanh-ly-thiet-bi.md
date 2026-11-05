# Thanh lý thiết bị và xác nhận bảo trì

**Mục tiêu:** Trong Lịch sử bảo trì, thiết bị hư hại có thể lập phiếu sửa hoặc thanh lý; tab Thanh lý lưu danh sách và chi tiết đã thanh lý. Xác nhận bảo trì chuyển ngay sang Đang thực hiện.

**Thiết kế:** Giữ kiến trúc WPF → BUS → DAL → SQL Server. Một thiết bị chỉ thanh lý một lần, lưu hồ sơ thanh lý ngay trên THIET_BI, gồm ngày SQL, nhân viên, lý do, giá trị thu hồi và bản tên thiết bị/phòng/nhân viên tại lúc thanh lý. Không cần bảng và mã chứng từ mới cho một quan hệ một-một. SQL bảo vệ lịch sử, chặn thiết bị còn phiếu mở, thu hồi phòng và ngưng sử dụng cùng giao dịch. Không biến việc ngưng sử dụng cũ thành thanh lý khi chưa có chứng từ.

**Quyết định triển khai:** Bổ sung theo cấu trúc hiện tại, làm trực tiếp trong workspace để giữ các thay đổi người dùng đang có; không tạo commit hoặc đổi nhánh. Thanh lý chỉ cho thiết bị Hư hại chưa ngưng và không có phiếu bảo trì mở; phiếu nháp cần hủy trước. Giá trị thu hồi không tự ghi thành hóa đơn khám, phiếu xuất kho hoặc giao dịch thu tiền bệnh nhân.

- [x] Viết ca hồi quy xác nhận chạy ngay và thanh lý, quan sát thất bại trên mã hiện tại.
- [x] Đồng bộ file SQL chính: trường thanh lý, ràng buộc và trigger giữ lịch sử; vòng đời bảo trì và nâng cấp phiếu đã xác nhận hiện có. Giữ nguyên dữ liệu lịch sử kết thúc.
- [x] Bổ sung ET/BUS/DAL và cập nhật cache trong cùng giao dịch; đối chiếu bản gốc trước thanh lý.
- [x] Thêm cửa sổ thanh lý, nút chọn sửa/thanh lý và tab tra cứu có tìm kiếm; bỏ nút bắt đầu, đổi xác nhận thành thực hiện ngay.
- [x] Chạy build, kiểm thử SQL/BUS/DAL trên database cách ly; kiểm tra nâng cấp chạy lại, giao diện và cập nhật hướng dẫn.

**Kết quả:** 78 ca bảo trì/thanh lý, 15 ca form thật, 32 ca SQL V9 và 47 ca khám/thu tiền đạt. Đã tái hiện rồi sửa lỗi cache sau lưu xác nhận và lỗi lưu tên danh mục dài phát hiện qua rà soát độc lập. Nâng cấp từ SQL cũ bảo toàn fingerprint lịch sử đã hoàn thành; rerun sau thanh lý đạt. Không thay đổi các file lâm sàng người dùng đang đổi tên.

**Trọng tâm kiểm chứng:** Thanh lý trùng; phiếu mở; thay đổi đồng thời; lịch sử bất biến khi đổi tên danh mục; rollback không để trạng thái hoặc cache dở dang. Ngày dự kiến tương lai chỉ lưu nháp; xác nhận bắt đầu yêu cầu đã đến ngày.
