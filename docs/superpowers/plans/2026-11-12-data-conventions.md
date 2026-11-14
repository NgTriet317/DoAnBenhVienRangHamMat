# Data presentation and code conventions

User yêu cầu bỏ nhãn đồ án trong dữ liệu, giữ lịch sử khám, không thêm hàng chờ; phân phòng lịch làm việc và dùng mã nghiệp vụ theo dữ liệu cũ. Quyền chuẩn hóa database đồ án đã được cấp, không hỏi lại lựa chọn kịch bản.

- [x] Sao lưu và dựng fixture từ SQL hiện tại; ghi nhận lỗi trước sửa: nhãn/prefix DL26,13 ca đang chờ/đang khám,624 lịch chưa phân phòng.
- [x] Kiểm tra thất bại trước sửa quy tắc cấp mã; thống nhất BN/NV/TK/PH/KHO/CA/TH/HH/LS/HS/LO… và chỉ tính hậu tố số đúng tiền tố.
- [x] Giữ các lượt có nội dung bằng chốt khám qua BUS/DAL; chỉ xóa phiếu chờ chưa có lịch sử. Giữ chứng từ thu/giao hiện có.
- [x] Đổi mã mới cùng mọi FK, tham chiếu văn bản/JSON; làm sạch nhãn hiển thị; phân phòng hợp chức vụ, không trùng ca.
- [x] Kiểm FK/CHECK/trigger, in đơn, tính phí, lịch sử và thử rollback; rà soát độc lập trước áp dụng chính.
- [x] Áp dụng vào database chính có backup; xuất SQL; kiểm cài mới/chạy lại và SHA256; cập nhật hướng dẫn/mã mới, dọn fixture riêng.

Giữ nguồn gốc dữ liệu trong tài liệu và backup; không biến mô tả nguồn giá thành xác nhận giá thị trường. Không commit/push, không sửa các thay đổi giao diện có sẵn.

Kết quả: main trước ghi 3.224 dòng (thêm 3 nhật ký/phiên hợp lệ so với snapshot cũ), sau 3.285 dòng/64 bảng. Giữ36 lượt khám, xóa6 phiếu chờ rỗng, chốt7 lượt nháp qua BUS, phân phòng624 ca. Kiểm21 đơn in/18 bàn giao/13 hóa đơn; thử lỗi CHECK hoàn nguyên SHA64/ràng buộc/trigger/marker. Cài mới/chạy lại SQL khớp SHA64; tiếp nhậnPKB037/bắt đầuLS037 trên fixture và chạy lại giữ nguyên dữ liệu phát sinh. Build Debug/Release0 lỗi (118/122 cảnh báo có sẵn). Ba fixture riêng đã xóa. Rà soát độc lập đã xử lý giới hạn kiểm theo cohort, giữ lý do thanh lý và tiền tố DMTT; không còn lỗi quan trọng.
