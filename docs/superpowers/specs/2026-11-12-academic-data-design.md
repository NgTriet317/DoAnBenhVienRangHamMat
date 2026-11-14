# Chuẩn hóa và mở rộng dữ liệu đồ án

Người dùng xác nhận 12/11/2026: database hoàn toàn cho đồ án, được chuẩn hóa hồ sơ cũ và tự quyết định bộ dữ liệu giả lập thực tế liên thông. Cho phép áp dụng vào database hiện tại và cập nhật file SQL; không cần hỏi lại lựa chọn kịch bản. Đây là dữ liệu mô phỏng, không phải lịch sử điều trị/mua bán thật.

Giữ schema 64 bảng, bốn project, mã/tài khoản cũ và các chỉnh sửa ứng dụng đang có. Giữ giá trị đã chốt khi dữ liệu cũ hợp lý; hoàn chỉnh nguồn phí còn thiếu, chuẩn hóa lịch sử thu mẫu thành đúng một Thu đủ cho hóa đơn v2, bảo toàn bản gốc trong backup và bằng chứng trước/sau. Các lượt cũ đã kết thúc được mô phỏng Hoàn tất; không xuất thuốc lần hai để nối nguồn. Sửa tên/hàm lượng/đơn vị chốt, đường dùng, mã tình trạng và cờ không kê thuốc. Hồ sơ nháp hiện có vẫn dùng để thực hành.

Chọn công cụ chủ động chạy trong một giao dịch Serializable, không tự chạy sửa dữ liệu khi mở ứng dụng. SQL sửa legacy giới hạn mã nguồn cũ, khôi phục mọi trigger sau sửa; dữ liệu mới chạy qua BUS/DAL cho các bước khám, kế hoạch, thực hiện, kê đơn, hoàn tất, thanh toán và bàn giao. Không thêm schema hoặc làm yếu các guard sản phẩm. Marker extended property giúp chạy lại không sinh trùng hoặc phục hồi trạng thái người dùng đã thao tác.

So với tạo lại toàn database, cách này giữ mã và tài khoản quen thuộc. So với chỉ thêm mẫu và bỏ qua hồ sơ cũ, cách này sửa được các điểm chặn in/thu/giao đã phát hiện. Đây là lựa chọn triển khai của agent theo quyền tự quyết người dùng đã cấp.

Bổ sung danh mục có nguồn và giá mô phỏng; nhập thuốc/vật tư có chứng từ, lô, đơn vị, hạn dùng, tồn; mua và phân bổ thiết bị có serial, bảo trì và tài sản chờ xử lý. Bổ sung bác sĩ/tài khoản/phòng/ca theo chuyên khoa đang thiếu. Bổ sung ít nhất 30 người bệnh giả lập với lịch hẹn, tiếp nhận, khám nháp, chờ thu, chờ giao và đã hoàn tất, có cả lượt không kê thuốc, khám thường và khám dịch vụ có phụ thu. Giữ nhãn mô phỏng trong ghi chú/metadata để đọc tên sản phẩm tự nhiên trên giao diện.

Kiểm chứng trên database thử từ snapshot hiện tại: kiểm tra ban đầu phải tìm thấy lỗi; sau chuẩn hóa không còn lỗi nguồn/tiền/tồn/bản ký đã xác định. Đọc/in và tính phí/bàn giao qua API thực của dự án; các thử thu/giao tiếp diễn rollback. Chạy lại công cụ phải giữ nguyên dữ liệu. Sau áp dụng database chính, xuất snapshot và kiểm cài mới/chạy lặp khớp SHA256 của cả 64 bảng. Sao lưu checksum trước ghi, giữ nguyên database khác, không commit/push/phát hành.
