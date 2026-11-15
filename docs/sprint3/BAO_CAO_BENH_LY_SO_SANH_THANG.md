# Bệnh lý thường gặp và so sánh tháng — 15/11/2026

Mở **Báo cáo và thống kê → Khám & bệnh lý → Bệnh/tình trạng thường gặp**.

1. Chọn **Tháng cần xem** và **Năm**.
2. Ở **So với**, chọn **Tháng / năm tùy chọn**, rồi chọn tháng/năm đối chiếu. Có thể chọn hai tháng trong cùng năm hoặc khác năm; vẫn có chế độ tháng trước, cùng kỳ năm trước và không so sánh.
3. Có thể giới hạn chuyên khoa của bác sĩ và dịch vụ tiếp nhận. Bấm **Tải báo cáo**.

Khi xem tháng hiện tại, **Lũy kế tháng hiện tại (MTD)** mặc định bật. Ví dụ ngày 15/11: đối chiếu 01–15/11 với 01–15 của tháng gốc, thay vì lấy trọn tháng gốc. Tháng gốc ngắn hơn được chặn ở cuối tháng. Kỳ tương lai bị từ chối.

## Đọc kết quả

- Bốn ô tổng quan là **Số bệnh nhân đến khám**, **Tổng lượt khám**, **Số loại bệnh / tình trạng**, **Tình trạng gặp nhiều nhất**. Số người tách khỏi số lượt tái khám; loại bệnh đếm theo mã đã ghi nhận; tình trạng đứng đầu tính theo số bệnh nhân.
- Bảng **Chi tiết bệnh nhân đến khám** đặt ngay dưới tổng quan, mỗi dòng là một lần khám. Có mã/tên bệnh nhân, ngày giờ khám, dịch vụ, chẩn đoán, các tình trạng ghi nhận và bác sĩ. Nhập từ khóa để tìm tên không dấu, mã, chẩn đoán hoặc tình trạng. Một lượt có nhiều tình trạng vẫn chỉ một dòng; các lần tái khám là các dòng riêng.
- Khung **Phổ biến nhất trong kỳ** chỉ rõ bệnh/tình trạng đứng đầu của mỗi kỳ, kể cả đồng hạng. Có thể chuyển tiêu chí giữa số bệnh nhân và số lượt khám.
- Bảng có toàn bộ mã xuất hiện ở một trong hai kỳ: bệnh nhân, số lượt, tỷ lệ bệnh nhân và biến động. Cột tên bệnh được giữ khi cuộn ngang. Biểu đồ so sánh tối đa 10 nhóm có đủ dữ liệu được công bố ở hai kỳ.
- **Xuất báo cáo này** xuất toàn bộ nhóm, tổng quan, hai kỳ, số người/lượt, tỷ lệ, biến động, tiêu chí xếp hạng và chất lượng dữ liệu, kèm chi tiết bệnh nhân nếu đã tải và có quyền. Tìm nhanh chỉ lọc bảng trên màn hình; CSV vẫn có đầy đủ kỳ đã chọn. Đổi bộ lọc xóa cả chi tiết cũ và khóa xuất đến khi tải lại.

Bảng định danh bệnh nhân yêu cầu quyền **Xem dữ liệu lâm sàng** hiện có (Admin, bác sĩ/điều dưỡng được cấp quyền). Tài khoản chỉ xem tổng hợp vẫn xem thống kê nhưng không nhận dữ liệu định danh; DAL kiểm lại quyền và phiên trước khi đọc/xuất. Tổng hợp, các kỳ so sánh và danh sách bệnh nhân được đọc trên cùng kết nối/giao dịch, giữ số liệu nhất quán khi có lượt khám được xác nhận đồng thời.

Số bệnh nhân là DISTINCT MaBN của các lượt **Đã xác nhận** theo ThoiGianKham, qua phiếu khám/hồ sơ bệnh án. Một người tái khám nhiều lần chỉ tính một người trong kỳ; một người có thể có nhiều tình trạng nên tổng tỷ lệ có thể vượt 100%. Không suy mã từ chữ chẩn đoán tự do.

Giữ quy tắc hiện có: nhóm dưới 5 bệnh nhân được ẩn số, tỷ lệ và chênh lệch. Dấu **—** cũng dùng khi không chọn đối chiếu hoặc chưa đủ dữ liệu phân loại. Nhóm vắng ở kỳ được phân loại đầy đủ có giá trị 0; thiếu mã không được coi là không có bệnh. Khi xếp theo lượt mà còn nhóm bị ẩn, nhận xét không kết luận tuyệt đối nhóm nhiều lượt nhất vì các bệnh nhân trong nhóm ẩn có thể tái khám nhiều lần.

## Xác minh

Lệnh đã chạy thành công:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBaoCao.ps1 -Sql -Render
```

**54/54 kiểm tra đạt**; build Release WPF thành công. Bao gồm tháng bất kỳ cùng/khác năm, tháng nhuận, MTD, DISTINCT bệnh nhân, bệnh chỉ có ở kỳ gốc, nhóm nhỏ, dữ liệu thiếu mã, đồng hạng, CSV hơn 10 nhóm, đổi xếp hạng/bộ lọc, chuyển sang báo cáo 12 tháng, quyền Admin/User và ảnh ở 1024/1366/1920. Các ca mới đối chiếu bảng bệnh nhân với tổng lượt/số người thực tế, kiểm tình trạng chưa mã, nhiều tình trạng không nhân lượt, tìm không dấu, chống công thức CSV, quyền lâm sàng và gói dữ liệu nhất quán. Nhánh SQL dùng bảng tạm và phiên kiểm thử rollback; không chỉnh dữ liệu khám. Build vẫn có các cảnh báo nullable sẵn có ở các phân hệ khác.

Rà soát độc lập đã hoàn tất; trường hợp nhóm ẩn có nhiều lượt tái khám đã có kiểm thử hồi quy. Không cần thay schema hoặc chạy SQL nâng cấp.

Ảnh và kết quả kiểm: [XemTruoc/benh-ly-so-sanh](../../XemTruoc/benh-ly-so-sanh/).
