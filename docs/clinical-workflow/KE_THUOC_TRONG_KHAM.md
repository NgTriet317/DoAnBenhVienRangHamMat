# Kê thuốc trong khám và tra cứu/bàn giao đơn

> Mốc nền tảng. Cách thao tác hiện tại đã đổi sang cửa sổ xác nhận đơn nháp rồi chốt ký/hóa đơn; xem [CUA_SO_KE_THUOC.md](CUA_SO_KE_THUOC.md).

Yêu cầu29/10/2026: Đơn thuốc chỉ xem đơn bác sĩ đã kê và bàn giao. Soạn/ký đơn nằm trong khám, sau thực hiện kế hoạch, trước tổng kết dịch vụ/thuốc và hóa đơn. Bỏ mẫu/phiên bản/cấu hình hướng dẫn và nút tải lại bỏ nháp trùng chức năng. Thuốc có nhóm độc lập với mẫu và danh mục thực tế có nguồn.

Chọn cách tái sử dụng bộ soạn đơn đã có ngay trong màn khám, giữ validation bác sĩ/thuốc/chỉ định và giao dịch tài chính. Thực hiện kế hoạch có xác nhận riêng trước kê thuốc; tổng kết đọc dịch vụ thực tế và đơn hiệu lực đã ký từ SQL. Hoàn tất chỉ khóa lượt/lập hóa đơn, không tự thực hiện phần kế hoạch còn thiếu. Không kê thuốc thì lưu lý do ở bước kê thuốc. Đơn thuốc bên ngoài dùng danh sách/chi tiết chỉ đọc và bàn giao theo quyền sau thu đủ.

Nhóm thuốc dùng FK trực tiếp THUOC→NHOM_THUOC_KE_DON; không dùng cấu hình liều/mẫu để lọc nhóm. Giữ phiên bản chỉ định trong chứng từ cũ khi đọc, đơn mới bác sĩ nhập chỉ định trực tiếp. Giá công khai có nguồn chỉ là tham khảo khởi tạo; thuốc chưa tìm được giá xác thực hiển thị rõ và phải thiết lập giá trước khi lập hóa đơn. Không tự tạo liều dùng, tồn kho hoặc lịch sử người bệnh.

## Kế hoạch triển khai

- [x] Kiểm hồi quy thất bại: kê thuốc chưa nằm trong khám, còn Mẫu v/tải lại bỏ nháp, nhóm còn lệ thuộc mẫu.
- [x] Xác nhận thực hiện trong khám, chống lặp, kế hoạch nháp/chưa xong không được kê thuốc hoặc lập hóa đơn; tổng kết đọc nguồn thật.
- [x] Nhúng soạn/ký đơn vào bước kê thuốc; bỏ mẫu/cấu hình/tải lại bỏ nháp, giữ một thao tác hủy nháp, bảo vệ thay đổi đang nhập.
- [x] Đơn thuốc chỉ tra cứu/in/bàn giao theo phạm vi và quyền, không còn đường mở form kê/ghi cũ.
- [x] Bổ sung danh mục thuốc có nguồn, nhóm và liên kết SQL tự chứa; bảo toàn giá/dữ liệu đang có.
- [x] Kiểm cài sạch/chạy lặp, nhập→thực hiện→kê/ký→tổng kết→hóa đơn→thu→bàn giao, quyền, rollback và GUI.
- [x] Sao lưu/đồng bộ database, publish và cập nhật hướng dẫn.
- [x] Lưu bằng chứng, dọn hai database kiểm thử và thư mục tạm do tác vụ tạo.

## Kết quả

Bản nguồn và bản phát hành đều đạt41 nhóm kiểm tra. Bộ kiểm sử dụng WPF thật, chọn thuốc có nguồn, nhập chỉ định có nhãn FIXTURE, lưu/ký ngay trong khám, đọc Tổng kết từ SQL và kiểm đúng số tiền hóa đơn. Kiểm riêng đơn chưa thu không giao được; sau thu đủ thì nạp đúng nguồn bàn giao; giao lại không xuất thêm kho hay tạo bản ghi trùng. Kiểm kế hoạch nháp/token cũ/bác sĩ khác, không kê thuốc, giá thiếu, giữ giá đã chốt và tất cả hồi quy trước đều đạt. Mọi nghiệp vụ fixture rollback trên database thử riêng. Rà soát độc lập không tìm thấy lỗi cần chặn bàn giao.

Admin mở đủ26 phân hệ trên database sạch và database đang dùng; build Debug/Release/publish đạt. SQL chính cài trống và chạy lặp đạt. Database thật được backup COPY_ONLY/CHECKSUM, RESTORE VERIFYONLY; mọi dòng/cột cũ của53 bảng được so sánh hash và giữ nguyên. Chỉ thêm52 thuốc/20 nhóm và điền nhóm cho10 thuốc cũ. Hiện có62 thuốc,0 thuốc chưa phân nhóm; không thêm hồ sơ, đơn thuốc, chứng từ hoặc tồn kho thử vào database đang dùng.

Giá và thuốc mới có nguồn tại [DANH_MUC_THUOC_RHM](DANH_MUC_THUOC_RHM.md).12 thuốc chưa có nguồn giá để0 kèm nhãn, chặn hóa đơn đến khi thiết lập giá; thuốc mới không có lô/tồn kho giả. Mẫu liều/phiên bản không dùng cho đơn mới; dữ liệu lịch sử đã ký được giữ nguyên. Bằng chứng và backup: [evidence/PrescriptionFlow1](evidence/PrescriptionFlow1/README.md).
