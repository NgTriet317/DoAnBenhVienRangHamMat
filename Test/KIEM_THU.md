# Kịch bản kiểm thử AI thật — dữ liệu giả

1. Không đặt API key: `/health/ai` trả `aiConfigured=false`; nút Bắt đầu bị khóa, không tự sử dụng các câu trả lời cố định.
2. Đặt API key Groq hợp lệ và khởi động script: `/health/ai` trả `provider=Groq Cloud`, `aiConfigured=true`.
3. Khai bệnh nhân mới nhưng chọn "Đã khám rồi"; SQL V2.2 sẽ đối chiếu lại hồ sơ ở bước nhân viên — không tin mù quáng lời khai.
4. Khi AI hỏi tên, nhập "Tôi là Nguyễn Văn An, sinh 18/10/2001, số 0912345678" — AI phải trích xuất nhiều trường, tránh hỏi lại trường đã có.
5. Đang hỏi triệu chứng, nhập "Tôi đau răng phía dưới khi nhai, kéo dài hai ngày" — bản tóm tắt phải giữ được lý do và triệu chứng tự khai, không thêm chẩn đoán.
6. Trả lời câu ngoài lề "Làm sao in phiếu?" — AI giải thích ngắn về quy trình sau đó quay lại trường thông tin còn thiếu.
7. Tại tóm tắt nói "Sửa tên thành ..." — giữ lại những trường khác, chỉ đổi tên đã sửa.
8. Tại tóm tắt nói "Xác nhận" — chuyển `ChoNhanVien` và nhận mã AI; WPF V2.2 xử lý phiếu như cũ.
9. Bệnh nhân nói "Tôi khó thở" — chuyển `CanHoTro`, yêu cầu gặp nhân viên, không phát hành phiếu tự động.
10. Thử "Tôi không khó thở, không khó nuốt" — không được hiểu thành đang khó thở.
11. Rút mạng hoặc thay key sai trước khi gửi: 503, không ghi câu người bệnh hoặc thay đổi bản khai; có thể thử lại.
12. Hai lượt gửi cùng trạng thái: phải chặn lượt cũ bằng rowversion, không được ghi trùng.
13. Refresh trình duyệt: `/api/trang-thai` hiển thị phản hồi AI gần nhất.
14. Đóng trình duyệt rồi mở lại dùng cùng phiên trong 30 phút; xử lý session theo cookie; người dùng bấm Kết thúc phải xóa màn hình.
15. Trong toàn bộ luồng, tuyệt đối không nhập bệnh nhân thật khi dùng nhà cung cấp cloud chưa qua đánh giá dữ liệu.
