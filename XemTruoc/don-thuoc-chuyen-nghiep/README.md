# Mẫu đơn thuốc và chi tiết — thiết kế chuyên khoa

Ảnh dựng từ dữ liệu mẫu để kiểm tra bố cục, không phải hồ sơ bệnh nhân thực tế.

- [Phiếu đơn thuốc A4](phieu-don-thuoc-a4.png): đầu nhận diện, thông tin chia ô, thuốc căn cột, hướng dẫn riêng, ghi chú và vùng ký.
- [Toàn bộ nội dung chi tiết](chi-tiet-don-thuoc.png): đầu navy, thông tin người bệnh, chẩn đoán, từng thẻ thuốc và phần bàn giao.
- [Chi tiết ở cửa sổ hẹp](chi-tiet-cua-so-hep.png): số lượng, liều, đường dùng và hướng dẫn tự xuống dòng.
- [Bản xem trước khi in](xem-truoc-in.png): hiển thị từng trang A4 với mã đơn và số trang.

Kiểm chứng bằng `powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraChungTu.ps1`. Chứng từ dài, dữ liệu thiếu snapshot, lỗi nguồn/lỗi in và bố cục hẹp đều nằm trong bộ kiểm. Chưa kiểm thử máy in vật lý.
