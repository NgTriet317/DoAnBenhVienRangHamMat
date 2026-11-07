# Báo cáo tập trung vào quyết định và bệnh thường khám

> Thực hiện nối tiếp kế hoạch DOCX đã được giao; dùng executing-plans, kiểm thử trước và giữ nhánh Sprint3_tan.

**Mục tiêu:** đưa lượt khám, bệnh nhân duy nhất, thu ròng và độ bao phủ mã lên tổng quan; bệnh/tình trạng thường gặp phải dễ tìm, có số bệnh nhân và số lượt riêng. Giữ toàn bộ báo cáo cũ trong mục mở rộng.

**Căn cứ:** phản hồi người dùng ngày 06/11/2026 và các định nghĩa bệnh lý trong KeHoach_CaiTao_BaoCao_ThongKe_Sprint3_tan.docx. Đã xác minh SQL: 11 lượt xác nhận, 0 lượt có MaDMTT. Chọn nhanh tình trạng hiện không truyền/lưu mã; phải sửa nguồn dữ liệu này, không suy luận hay backfill hồ sơ cũ.

**Thiết kế:** giữ WPF/ET/BUS/DAL. Tổng quan dùng số DISTINCT trên toàn kỳ và phần trăm bao phủ tính lại từ mẫu số toàn kỳ, không cộng số bệnh nhân/phần trăm từng tháng. Bảng bệnh lý đặt ngay sau KPI, mặc định xếp theo số bệnh nhân, có lựa chọn xếp theo lượt. Khi chưa mã hóa, thông báo rõ số lượt chưa phân loại và lý do chưa thể kết luận bệnh nào phổ biến. Nhóm nhỏ vẫn che số như chính sách cũ. Không gán mã từ tên viết tay; chỉ lưu mã khi bác sĩ chọn danh mục. Không thay đổi schema hoặc dữ liệu vận hành đã xác nhận.

- [x] Task 1: thêm ca RED cho KPI tổng quan, bệnh lý dễ tìm/không gán 0 giả, và việc giữ mã được chọn; kiểm nguồn/đếm DISTINCT.
- [x] Task 2: sửa truyền mã tình trạng (ET, WPF nhập liệu, DAL xác thực danh mục); kiểm giữ mã, nhập tự do không mã, đổi tên và danh mục ngừng áp dụng. Handler giao diện chọn trùng tên đã tái hiện RED→GREEN; không ghi hồ sơ thật.
- [x] Task 3: ưu tiên KPI và bảng bệnh lý; mục mở rộng báo cáo cũ, hướng dẫn đọc số liệu, cảnh báo dữ liệu thiếu; giữ CSV nhất quán.
- [x] Task 4: ReportChecks SQL + render ba độ phân giải: 41 PASS/0 FAIL; build Debug/Release và schema thành công; rà soát độc lập, xử lý các phát hiện và lưu bằng chứng trong docs/sprint3/BAO_CAO_TRONG_TAM.md.

Không commit/push/merge thay người dùng. Các mã thiếu ở dữ liệu cũ cần người có chuyên môn rà soát qua nghiệp vụ; báo cáo không sửa hồ sơ để tạo thứ hạng.
