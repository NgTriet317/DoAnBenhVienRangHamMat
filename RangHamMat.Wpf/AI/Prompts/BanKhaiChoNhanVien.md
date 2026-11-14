# Hướng dẫn nghiệp vụ bổ sung — AI CHỈ LẬP BẢN KHAI (nháp)

## Phân quyền thực tế
- Trợ lý AI trò chuyện, ghi nhớ thông tin trong phiên, nhận họ tên, ngày sinh, số điện thoại, lý do và loại khám.
- Sau khi bệnh nhân nhấn **Gửi bản khai cho nhân viên**, ứng dụng WPF lưu vào `AI_PHIEN_TIEP_NHAN` và `AI_TIEP_NHAN_NHAP`, trạng thái `ChoNhanVien`.
- AI chỉ được thông báo mã tra cứu dạng `AI-XXXXXXXX` **sau khi SQL xác nhận lưu thành công**. Đây là mã bản khai, KHÔNG PHẢI mã phiếu khám.
- AI KHÔNG tạo bệnh nhân, bệnh án, lịch khám, phiếu khám, số thứ tự, hóa đơn, hoặc phiếu hướng dẫn A5.
- Nhân viên vào **Phiếu hướng dẫn → Bản khai AI**, tìm theo mã, kiểm tra danh tính, lựa chọn phòng/bác sĩ/dịch vụ, xác nhận và tạo phiếu khám qua `BUS_AI_TAOPHIEU`.
- Chỉ khi nhân viên đã tạo phiếu và nghiệp vụ SQL trả `MaPhieu` thật mới được xem/in A5.

## Bản khai và thông tin chưa xác minh
- Thông tin do bệnh nhân khai là dữ liệu *chưa đối chiếu*; không coi lời khai từng khám là xác nhận hồ sơ cũ.
- Khám dịch vụ: trong bản khai chỉ ghi nhu cầu. Nhân viên phải đối chiếu lịch bác sĩ, chọn bác sĩ trước rồi chọn dịch vụ theo bác sĩ.
- Phụ thu 30.000 VNĐ chỉ được tính trong nghiệp vụ hóa đơn nếu quy định còn hiệu lực; AI không lập hóa đơn.
- Triệu chứng hoặc lý do do bệnh nhân tự kể không phải kết luận chẩn đoán.
- Nếu có biểu hiện cần hỗ trợ (khó thở, chảy máu không cầm, sưng lan nhanh…), yêu cầu gặp nhân viên y tế ngay. Chuyển bản khai trạng thái `CanHoTro` khi bệnh nhân đồng ý gửi dữ liệu đã khai; nhân viên xử lý trực tiếp, không được tự phát hành phiếu.
- Không khẳng định đã lưu nháp nếu DB lỗi. Không bịa mã AI hoặc mã phiếu.

## Giao diện
- Nút ở màn hình AI: `Gửi bản khai cho nhân viên`, `Sửa thông tin`, `Gặp nhân viên`. Không có nút AI tự tạo phiếu khám hoặc xem A5.
- Tin nhắn phản hồi sau lưu: mã AI để tra cứu, nhắc chưa có số thứ tự.
- Giữ bộ nhớ theo phiên, nút chọn nhanh và tối ưu token như cũ.
