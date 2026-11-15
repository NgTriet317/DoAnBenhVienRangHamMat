# Tổng quan bệnh lý và so sánh hai tháng

Yêu cầu: làm lại chuyên đề bệnh lý thường gặp để biết số bệnh nhân đến khám, các bệnh/tình trạng ghi nhận, nhóm phổ biến nhất và đối chiếu hai tháng bất kỳ trong cùng năm hoặc khác năm.

Giữ WPF .NET 8, BUS/DAL và nguồn lượt khám đã xác nhận hiện có. Đếm DISTINCT bệnh nhân trên toàn kỳ và theo từng mã bệnh; không cộng lượt tái khám thành số người. Không suy mã bệnh từ chữ chẩn đoán tự do. Giữ quy tắc ẩn nhóm dưới 5 bệnh nhân hiện có.

## Phương án

- Mở rộng so sánh tổng lượt hiện tại: ít thay đổi nhưng không trả lời từng bệnh tăng/giảm ra sao.
- Tạo màn hình riêng: trùng lặp bộ lọc, quyền và xuất báo cáo.
- Lựa chọn triển khai: mở rộng chuyên đề hiện có với chọn tháng/năm, bảng từng bệnh và biểu đồ hai kỳ; dùng lại kiểm quyền và CSV.

## Hành vi

- Chuyên đề 20 chọn tháng/năm cần xem; chế độ đối chiếu hỗ trợ tháng/năm bất kỳ. Mặc định xem tháng hiện tại với tháng trước, lũy kế cùng số ngày khi tháng đang diễn ra.
- Hiển thị tổng bệnh nhân, lượt khám, lượt đã phân loại và chưa phân loại. Nêu bệnh phổ biến nhất mỗi kỳ theo số người, thể hiện đồng hạng.
- Bảng ghép hợp các mã ở hai kỳ, có số người, số lượt, tỷ lệ trên tổng bệnh nhân của kỳ và thay đổi số người. Đổi xếp hạng theo người/lượt cập nhật bảng, biểu đồ và CSV nhất quán.
- Bệnh vắng ở kỳ có dữ liệu phân loại đầy đủ được tính 0; kỳ chưa phân loại đầy đủ để trống thay vì suy diễn 0. Nhóm bị ẩn không được lộ số hoặc chênh lệch.
- Không có lượt, chưa có mã, tất cả nhóm bị ẩn đều có lời giải thích. Đổi bộ lọc xóa kết quả và khóa xuất. CSV gồm toàn bộ nhóm và số liệu đối chiếu đã tải.

Không thay schema, dữ liệu khám, phân quyền hay các báo cáo khác. Các lựa chọn bố cục và mặc định trên do người triển khai quyết định từ yêu cầu.

## Bổ sung theo yêu cầu tiếp theo của người dùng

Thay bốn ô trên chuyên đề20 bằng Số bệnh nhân đến khám, Tổng lượt khám, Số loại bệnh/tình trạng, Tình trạng gặp nhiều nhất. Thêm bảng từng bệnh nhân và lần khám, gồm danh tính, ngày giờ, dịch vụ, chẩn đoán, tình trạng, bác sĩ; giữ so sánh tháng. Dùng quyền dữ liệu lâm sàng đã có cho nhánh chi tiết; tổng hợp không mở thêm quyền danh tính. Toàn bộ gói thống kê và chi tiết dùng một giao dịch để tránh lệch tổng khi khám được xác nhận đồng thời. Kế hoạch bổ sung: `docs/superpowers/plans/2026-11-15-chi-tiet-benh-nhan-bao-cao.md`.
