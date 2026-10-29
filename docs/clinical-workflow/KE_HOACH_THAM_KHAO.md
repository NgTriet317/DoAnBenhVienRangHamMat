# Tình trạng và định hướng kế hoạch

Yêu cầu ngày 28/10: bỏ vị trí răng, tự hiện định hướng kế hoạch sau nhập tình trạng. Dữ liệu danh mục dựa trên NHS; không tạo danh tính bác sĩ/bệnh nhân thật hoặc coi giá seed là giá thực tế. Đang chờ người dùng xác định nguồn hồ sơ/bảng giá thật.

Thiết kế: DM_TINH_TRANG_BENH lưu tên bệnh thường gặp và định hướng tham khảo kèm nguồn. Form chọn nhanh hoặc nhập tên chính xác (không phân biệt dấu/hoa thường), tự hiện đề xuất ngay trong tab tình trạng. Tên ngoài danh mục hiển thị yêu cầu bác sĩ tự lập; không suy chẩn đoán từ văn bản tự do. Nút Lập kế hoạch nháp từ đề xuất mở tab Kế hoạch, điền định hướng và bước đánh giá; bác sĩ phải chọn dịch vụ/giá phù hợp trước khi lưu. Không tự chốt kế hoạch, chỉ định thuốc, thực hiện dịch vụ, thu tiền hoặc thay kế hoạch đã duyệt. Chưa ghép dịch vụ điều trị chuyên biệt khi danh mục/bảng giá thực tế chưa có.

- Bỏ ViTriRang trong form/ET/DTO/CRUD/metadata; migration08 backup trước khi bỏ cột, giữ cột khác.
- Seed danh mục tham khảo insert-only và nạp qua API khám đã kiểm tra quyền, giữ bốn project, không thêm màn cấu hình.
- Kiểm tra build, schema, ghép tên có/không dấu, tên không biết và không ghi đè xử trí đang nhập; cập nhật publish.

Nguồn: https://www.nhs.uk/conditions/tooth-decay/ ; https://www.nhs.uk/live-well/healthy-teeth-and-gums/dental-treatments/ ; https://www.nhs.uk/conditions/gum-disease/

Kết quả: 19/19 trên fixture cài mới, giao diện database chính 1/1; build Debug/Release và verify SQL đạt. Migration08 đã chạy sau backup COPY_ONLY CHECKSUM/VERIFYONLY; hash mọi dòng cũ của52 bảng khớp, bỏ duy nhất cột ViTriRang. Danh mục mới có6 dòng insert-only, không sửa bệnh nhân/bác sĩ/giá seed cũ. Nhật ký nạp danh mục gắn tài khoản quản trị admin001; đây là thao tác nâng cấp dữ liệu, không phải phê duyệt chuyên môn. Mốc lỗi: lần cài thử đầu bị trigger audit từ chối và rollback08; đã thêm context quản trị và kiểm lại cài mới. So khớp không dấu ban đầu lỗi với CompareInfo, đã đổi chuẩn hóa Unicode và kiểm lại.
