# Cửa sổ chọn thuốc và chốt lần khám

Yêu cầu29/10/2026: bấm Kê thuốc trong khám để mở cửa sổ lọc nhóm/chọn thuốc. Thuốc đã chọn và các ô số lượng, số ngày, liều, đường dùng, hướng dẫn cùng nằm trong cửa sổ. Xác nhận đưa đơn về phần Kê thuốc; bác sĩ còn được sửa trước khi chốt. Chốt ký đơn và lập hóa đơn để thu/giao tại quầy.

Thiết kế: tái sử dụng bộ soạn đơn có kiểm quyền và validation, chuyển sang cửa sổ có owner. Xác nhận lưu nháp, không ký sớm; màn khám hiện danh sách để kiểm tra/mở sửa. Tổng kết đọc cả nháp chờ chốt và đơn hiệu lực, loại đơn cũ bị thay thế. Chốt kiểm phiên bản các nguồn đã xem rồi ký các nháp và lập hóa đơn trong một transaction; lỗi rollback về nháp. Đóng cửa sổ có xử lý nội dung chưa lưu. Giữ một SQL chính và bốn project; thay đổi này không thêm schema/data.

- [x] Tái hiện thiếu nút/cửa sổ và thiếu chốt nháp cùng hóa đơn.
- [x] Cửa sổ chọn/soạn thuốc, xác nhận về màn khám và mở sửa.
- [x] Tổng kết nguồn nháp, chốt ký/hóa đơn atomic, xung đột/rollback/retry.
- [x] Kiểm cửa sổ, pending edits, quyền và hồi quy thu/giao; publish và tài liệu.

Kết quả:47 nhóm đạt trên bản nguồn và DLL bản phát hành; Admin mở26 phân hệ trên database đang dùng. Build Debug/Release và kiểm schema chỉ đọc đạt. Có kiểm cửa sổ thật chọn nhóm/nhập chỉ định/xác nhận/mở sửa, giữ nhập sai khi hủy đóng, nguồn tổng kết cũ, rollback chữ ký khi hóa đơn lỗi, retry cùng hóa đơn và nháp thay thế không cộng đôi. Rà soát độc lập không tìm thấy lỗi cần chặn bàn giao. Chỉ dùng một database fixture riêng; được dọn sau lưu bằng chứng. Không chạy DML nghiệp vụ hoặc đổi schema trên database người dùng.

## Cách thao tác

1. Lưu khám và xác nhận đã thực hiện kế hoạch.
2. Trong Kê thuốc, bấm **Kê thuốc**. Cửa sổ hiện đúng bệnh nhân/lượt và thông tin dị ứng/tiền sử để đối chiếu.
3. Lọc nhóm hoặc tìm tên/hàm lượng. Chọn thuốc rồi **Thêm thuốc đã chọn**. Danh sách bên phải và các ô số lượng, ngày dùng, liều, đường dùng, hướng dẫn nằm trong cùng cửa sổ. Chọn dòng khác để sửa từng thuốc; có thể bỏ thuốc khỏi nháp.
4. **Xác nhận đơn thuốc** lưu nháp và đóng cửa sổ. Danh sách xuất hiện ở Kê thuốc trong khám. Bấm **Sửa danh sách thuốc** để mở lại; chưa có chữ ký hoặc hóa đơn ở bước này.
5. Đối chiếu Tổng kết rồi **Chốt và lập hóa đơn**. Chốt ký các nháp và lập hóa đơn chung giao dịch; nguồn khác bản đã xem bị từ chối, lỗi hóa đơn rollback cả chữ ký. Thử lại sau thành công giữ cùng hóa đơn.
6. Người bệnh ra quầy thanh toán và nhận thuốc. Đơn thuốc bên ngoài vẫn chỉ xem/in đơn đã ký và bàn giao theo trạng thái thu đủ.

Hủy nháp được giữ trong cửa sổ để có thể bỏ đơn đã lưu trước khi chuyển sang Không kê thuốc. Nút Đóng/X có lựa chọn lưu/bỏ/ở lại nếu còn nhập chưa lưu; lượng/ngày sai không tự mất khi xác nhận hoặc chọn ở lại. Đơn đã ký từ phiên cũ giữ tính bất biến, sửa bằng nguồn thay thế hợp lệ; tổng kết không cộng đôi đơn cũ và nháp thay thế.

Không cần cập nhật database cho thay đổi này: tiếp tục dùng SQL thống nhất có PrescriptionFlow1. Không thêm bảng, dữ liệu thuốc, tồn kho hoặc hồ sơ. Kiểm bằng fixture transaction rollback, không chốt khám/thu/giao trên database người dùng. Bằng chứng: [evidence/PrescriptionDialog](evidence/PrescriptionDialog/README.md).
