# Thống kê phục vụ điều hành bệnh viện

Điều chỉnh theo phản hồi ngày 06/11/2026: ưu tiên số liệu giúp hiểu nhu cầu khám và hoạt động bệnh viện, làm rõ bệnh/tình trạng nào được nhiều bệnh nhân đến khám.

## Màn hình chính

Mặc định chỉ hiện 5 chuyên đề: **Tổng quan điều hành**, **Bệnh/tình trạng thường gặp**, **Thu–hoàn**, **Dòng hàng vật tư**, **Hạn dùng lô hàng**. Bật **Hiện báo cáo khác** hoặc tìm kiếm để mở các chuyên đề cũ; không xóa chức năng nào.

| Chỉ số tổng quan | Cách đọc |
| --- | --- |
| Lượt khám xác nhận | Số lần khám trong kỳ; một người khám 3 lần tạo 3 lượt |
| Bệnh nhân duy nhất | DISTINCT MaBN trên toàn kỳ; không cộng số người từng tháng, không gọi là bệnh nhân mới |
| Thu ròng | Thu trừ hoàn đã xác nhận theo ngày thanh toán; không cộng hóa đơn hay giá trị kho |
| Lượt khám đã phân loại (%) | Lượt có mã tình trạng / tổng lượt xác nhận; kỳ không có lượt để trống, không coi là 0% |

Bảng bệnh/tình trạng nằm ngay sau KPI, trước các biểu đồ và bảng đối chiếu. Mặc định xếp theo **số bệnh nhân duy nhất**; có thể đổi sang **số lượt khám**. Hai cách xếp trả lời hai câu hỏi khác nhau: nhiều người đến khám, hay có nhiều lần khám/tái khám. Thứ tự bảng, biểu đồ và CSV dùng cùng lựa chọn. Không sắp theo chuỗi số hiển thị ở tiêu đề cột.

Mỗi mã có số lượt, số bệnh nhân, tỷ trọng trên các lượt có mã và biến động so kỳ gốc. Một lượt có nhiều tình trạng được tính ở mỗi mã; không cộng tổng bệnh để suy ra tổng lượt. Nhóm dưới 5 bệnh nhân giữ chính sách che số và chênh lệch. Danh mục hiện chứa cả bệnh, triệu chứng và theo dõi, nên tiêu đề dùng **bệnh/tình trạng**, không gọi đây là tỷ lệ mắc trong cộng đồng.

## Vì sao dữ liệu hiện tại chưa có thứ hạng bệnh?

Kiểm tra chỉ đọc trên SQL Server cục bộ ngày 06/11/2026: **11 lượt khám đã xác nhận, 0 lượt có MaDMTT**; 11 dòng tình trạng đều chưa gắn mã. Tổng quan kỳ 10/2026 có 11 bệnh nhân duy nhất. Giao diện giải thích **chưa thể xác định bệnh nào được khám nhiều nhất**, thay vì hiển thị như không có bệnh.

Không tự gán mã từ chẩn đoán viết tay, không cập nhật hồ sơ cũ và không tạo dữ liệu để có biểu đồ. Hồ sơ đã xác nhận cần được nhân viên chuyên môn rà soát qua nghiệp vụ phù hợp trước khi dùng cho thống kê bệnh theo mã.

## Sửa nguồn dữ liệu cho lượt khám mới

Đã xác định luồng **Chọn nhanh** trong Khám lâm sàng chỉ giữ tên và làm mất mã khi tạo/lưu dòng tình trạng. Đã bổ sung mã vào DTO, giữ mã qua các bước chọn–cập nhật–đọc lại, kiểm mã với danh mục trong DAL trước DML. Chọn danh mục có cùng tên với dòng nhập tự do cũng phải đánh dấu thay đổi dù TextChanged không phát sinh.

- Bác sĩ chọn từ danh mục: mã được truyền tới bước lưu; DAL kiểm mã tồn tại, trạng thái và tên tương ứng.
- Nhập tự do: không suy ra mã từ tên; vẫn thuộc dữ liệu chưa phân loại.
- Đổi tên khác danh mục: không giữ mã cũ một cách âm thầm.
- Mã đã ngừng áp dụng: được giữ khi sửa ghi chú của dòng lịch sử cùng mã/tên, không được gắn mới.
- Chỉ lượt **Đã xác nhận** mới đi vào thống kê. Lưu nháp chưa làm tăng thứ hạng.

Không sửa schema hoặc file SQL chính. Kiểm giao diện chọn/cập nhật mã thực hiện trên trạng thái nháp trong bộ nhớ, đọc SQL trong giao dịch dùng chung rồi rollback, không gọi lưu vào hồ sơ thật. Kiểm validation tên/mã/trạng thái là kiểm trực tiếp quy tắc được DAL dùng; chưa chạy một lượt khám mới hoàn chỉnh có ghi nghiệp vụ trên database kiểm thử riêng.

## Bằng chứng

Kết quả ngày 06/11/2026: **41 PASS, 0 FAIL**. Build Debug/Release thành công, 0 lỗi; Debug còn 118 cảnh báo của dự án, Release tăng dần báo 0 cảnh báo. Kiểm schema 62 bảng chỉ đọc thành công. Bộ kiểm báo cáo có các ca phân biệt số người/lượt, thiếu mã khác không phát sinh, mã được chọn khác nhập tự do, xếp hạng số, CSV, phân quyền và trạng thái tải.

- [Log kiểm báo cáo](evidence/bao-cao-trong-tam/ReportChecks.log).
- [Log build và schema đã lược bỏ định danh chứng từ](evidence/bao-cao-trong-tam/Build-Schema.log).
- [Tái hiện trước sửa trường hợp chọn danh mục cùng tên](evidence/bao-cao-trong-tam/Review-RED.log).
- Giao diện dùng số liệu SQL thật: [1024×768](evidence/bao-cao-trong-tam/admin-1024.png), [1366×768](evidence/bao-cao-trong-tam/admin-1366.png), [Full HD](evidence/bao-cao-trong-tam/admin-1920.png), [nhân viên](evidence/bao-cao-trong-tam/user-1366.png).

Lượt rà soát độc lập đã bắt trường hợp chọn danh mục trùng tên (Important). Đã tái hiện bằng handler giao diện, sửa và chạy GREEN cả bộ kiểm. Điểm về sắp xếp chuỗi số ở tiêu đề được xử lý bằng cách chỉ cho dùng lựa chọn xếp hạng số có đồng bộ CSV; không còn mục nhỏ hoãn lại. Các giới hạn về ý nghĩa lâm sàng của nhãn danh mục và dữ liệu lịch sử giữ nguyên, được giải thích trong phần trên. Không có thay đổi SQL hoặc hồ sơ đã xác nhận; chưa commit/push.
