# Prescription handover implementation plan

**Goal:** Hiển thị Đã giao thuốc sau bàn giao thành công, phân biệt đơn đã xác nhận chưa giao.

**Design approved:** Dùng BAN_GIAO_THUOC làm nguồn trạng thái giao; giữ DON_THUOC.TrangThai là trạng thái bản ký. Chỉ giao khi hóa đơn đủ điều kiện qua nghiệp vụ hiện có. Không giao lại đơn đã giao.

**Approaches:** Đổi trạng thái bản ký sẽ ảnh hưởng kiểm tra hóa đơn, kho và in đơn. Phương án đã được người dùng chọn là suy trạng thái hiển thị từ bản ghi bàn giao hiện có.

**Implementation:**
- [x] Bổ sung dữ liệu bàn giao và trạng thái hiển thị vào DongLichSuDon; kiểm tra đơn chưa giao, đã giao, ngừng áp dụng.
- [x] Truy vấn lịch sử nối bàn giao và lọc theo trạng thái hiển thị trước phân trang.
- [x] Cập nhật thẻ danh sách, bộ lọc, tiêu đề chi tiết và làm mới sau commit; giữ thông tin người giao và thời gian.
- [x] Build solution, chạy kiểm tra trạng thái và rà soát luồng chặn giao lại/thanh toán.

**Execution:** Thực hiện tại workspace hiện tại theo yêu cầu triển khai; không tạo commit hay phát hành.

**Verification:** Kiểm tra 5 trường hợp trạng thái đã chạy thất bại trước khi bổ sung thuộc tính và thành công sau triển khai. Build solution: 0 lỗi, còn cảnh báo ở mã hiện có. `git diff --check` thành công.

**Review:** Sửa tình huống đơn cũ bị mất lựa chọn sau giao khi danh sách đã giao có nhiều trang: chuyển bộ lọc sang Đã giao thuốc đồng thời tìm theo mã đơn vừa giao.

**Limit:** Đã thử kiểm tra SQL chỉ đọc qua sqlcmd và Microsoft.Data.SqlClient; môi trường báo không hỗ trợ mã hóa kết nối SQL Server, nên chưa nghiệm thu SQL/giao diện với dữ liệu thực. Có thể chạy lại `powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraTrangThaiDonThuoc.ps1 -KiemTraSql` trên máy kết nối được SQL Server.
