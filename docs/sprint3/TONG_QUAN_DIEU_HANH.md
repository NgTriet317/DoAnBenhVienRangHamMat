# Tổng quan điều hành — 15/11/2026

Mở **Báo cáo và thống kê → Tổng quan → Tổng quan điều hành**. Chọn các tháng cần xem, cách đối chiếu rồi bấm **Tải báo cáo**. Các nút chọn nhanh hỗ trợ tháng trước, 12 tháng hoàn chỉnh, năm trước và tháng này lũy kế.

## Bốn chỉ số chính

| Chỉ số | Ý nghĩa |
|---|---|
| Số bệnh nhân đến khám | Mỗi người được tính một lần trên toàn kỳ trong các lượt khám đã xác nhận. Không cộng số người của các tháng để ra tổng. |
| Tổng lượt khám | Mỗi lần khám đã xác nhận tính một lượt, bao gồm tái khám. |
| Tiền thực thu | Tiền đã thu trừ tiền đã hoàn của giao dịch đã xác nhận, theo ngày thanh toán. Có thể âm khi tiền hoàn lớn hơn tiền thu trong kỳ. |
| Tổng lịch hẹn | Tất cả lịch có ngày giờ hẹn trong kỳ, bao gồm các trạng thái hiện tại. |

Mỗi ô ghi số kỳ đối chiếu và tăng/giảm. Kỳ gốc bằng 0 vẫn có chênh lệch tuyệt đối, nhưng không tính phần trăm. Không chọn đối chiếu thì ghi rõ, không giả định kỳ gốc bằng 0.

## Các phần bên dưới

- **Hoạt động trong kỳ:** số phiếu tiếp nhận, lượt khám còn nháp và lượt khám chưa phân loại tình trạng; có nút mở bệnh nhân/bệnh lý, thu–hoàn, lịch hẹn và dịch vụ thực hiện.
- **Tình hình lịch hẹn:** số lịch theo từng trạng thái và tỷ lệ đã khám trong chính tập lịch được chọn. Trạng thái phản ánh lúc tải, không phải ảnh chụp trạng thái trong quá khứ.
- **Tiền thu và hoàn:** tiền đã thu, tiền đã hoàn, tiền thực thu và số giao dịch xác nhận. Giá trị hóa đơn, dự kiến điều trị, xuất kho không được cộng vào tiền thực thu.
- **Xu hướng theo tháng:** chọn một trong bốn chỉ tiêu để vẽ lại ngay từ dữ liệu đã tải. Hai chuỗi kỳ đang xem/kỳ đối chiếu dùng cùng chỉ tiêu và đơn vị.
- **5 dịch vụ thực hiện nhiều:** xếp theo số lượt khám khác nhau có dịch vụ đã xác nhận, chọn theo thời gian thực hiện. Một lượt có thể sử dụng nhiều dịch vụ; ghi cùng một dịch vụ nhiều dòng trong một lượt không làm tăng số lượt. Một dịch vụ có thể thực hiện trong kỳ sau ngày khám.
- **So với kỳ đối chiếu:** bảng ngắn gồm kỳ đang xem, kỳ gốc, thay đổi cho từng chỉ tiêu. Khi xem nhiều tháng, có thêm bảng hoạt động từng tháng.

Các nguồn tổng hợp độc lập, đọc trên một kết nối/giao dịch cho mọi kỳ và bảng phụ. Tổng quan không chứa danh tính bệnh nhân/nhân viên; chi tiết lâm sàng ở chuyên đề bệnh lý với quyền hiện có. Đổi bộ lọc, chuyên đề hoặc phiên xóa gói cũ và khóa xuất. Chưa tải/lỗi dùng dấu —; chỉ dùng 0 sau khi truy vấn thành công. Kỳ chỉ có dịch vụ thực hiện vẫn được ghi nhận có hoạt động.

## Xuất và kiểm tra

CSV gồm bốn chỉ tiêu, từng tháng, đối chiếu, bảng thu/hoàn, trạng thái lịch, dịch vụ và thông tin hồ sơ cần rà soát. Dùng đúng gói đã tải; số tiền âm giữ kiểu số, văn bản được chống công thức bảng tính.

Lệnh đã chạy thành công, **58/58 kiểm tra đạt**, build WPF Release thành công:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraBaoCao.ps1 -Sql -Render
```

Bao gồm fixture SQL bệnh nhân tái khám qua nhiều tháng, nhiều tình trạng/dịch vụ, thu/hoàn xác nhận và nháp, tiền thực thu âm, kỳ rỗng, dịch vụ thực hiện cho lần khám kỳ trước; kiểm dữ liệu thật và quyền Admin/nhân viên; giao diện 1024/1366/1920, đổi biểu đồ/bộ lọc, điều hướng và CSV. Rà soát độc lập đã hoàn tất; các điểm về kỳ chỉ có dịch vụ, đổi chỉ tiêu biểu đồ và tiền âm trong CSV đã có kiểm hồi quy. Các phiên kiểm thử rollback, không sửa hồ sơ khám/tiền. Build vẫn có cảnh báo nullable sẵn có ở phân hệ khác. Không cần thay schema.

Ảnh và kết quả: [XemTruoc/tong-quan-dieu-hanh](../../XemTruoc/tong-quan-dieu-hanh/).
