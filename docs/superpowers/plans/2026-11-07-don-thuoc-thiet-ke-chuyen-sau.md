# Đơn thuốc — thiết kế chuyên sâu

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for inline implementation.

**Goal:** Thay thiết kế còn đơn giản bằng mẫu chuyên khoa có nhận diện rõ và thẻ thuốc có cấu trúc.
**Architecture:** Dữ liệu snapshot trình bày dùng chung; chi tiết là XAML thẻ thuốc, phiếu in là FlowDocument có cột căn chỉnh, preview hiển thị các trang thực qua DocumentPageView. Giữ các điều kiện quyền, bàn giao và đọc lại nguồn trước khi in.
**Tech Stack:** WPF .NET 8, XAML, FlowDocument, DocumentPaginator. Không thêm package.
**Spec:** Yêu cầu bổ sung của người dùng: đơn thuốc in và giao diện chi tiết phải đẹp, chuyên nghiệp hơn bản hiện tại còn đơn giản. Người thực hiện chọn navy/xanh ngọc, đầu phiếu chuyên khoa, phân nhóm thông tin và thẻ thuốc để tạo khác biệt rõ ràng.

## Constraints

Làm trực tiếp workspace hiện tại, giữ các thay đổi sẵn có. Không đổi dữ liệu SQL, giá tiền, liều hoặc hướng dẫn; không đoán lịch uống từ văn bản. Tên/hàm lượng/đơn vị giữ snapshot, thiếu thì dùng mã. Không commit/push.

## Review Focus

Hướng dẫn rất dài không bị cắt; dữ liệu thiếu snapshot; bảng/tên thuốc dài; chiều rộng 360px; đổi nguồn/lỗi máy in; xem trước đủ tất cả trang.

- [x] Dựng DonThuocTrinhBay để dùng chung dữ liệu đã kiểm tra quyền giữa XAML và bản in.
- [x] Làm lại chi tiết: đầu navy, thông tin bệnh nhân, khối chẩn đoán, thẻ số lượng/liều/thời gian/đường dùng/hướng dẫn, ghi chú, bàn giao.
- [x] Làm lại phiếu in: đầu nhận diện, thông tin chia ô, thuốc căn cột, ghi chú, vùng ký và đầu/chân trang.
- [x] Xem trước từng trang A4 bằng DocumentPageView, cùng bộ phân trang dùng khi in.
- [x] Build Debug/Release; chạy kiểm chứng bảo toàn nội dung, phân trang và ảnh bố cục rộng/hẹp; review cuối.

## Kết quả

- Build solution Debug/Release thành công; kiểm cấu trúc bàn giao đạt.
- Bộ kiểm chứng 17/17 đạt: A4 ngắn/dài, đủ từng trang preview, mã đơn/số trang, snapshot lịch sử, nguồn đổi mã, đơn nháp, lỗi máy in, cửa sổ 360px và chuỗi thuốc/hàm lượng/đường dùng dài.
- Review độc lập xác nhận một lỗi: bảng tên thuốc có thể bị tách khỏi chỉ định khi qua trang. Đã tái hiện bằng 30 thuốc có marker tên/liều/hướng dẫn riêng, rồi giữ đầu thuốc XAML trong Paragraph.KeepWithNext để đi cùng phần đầu chỉ định. Hướng dẫn dài vẫn phân trang được.
- Phiếu sử dụng pixel widths tính theo vùng in cho các Table của FlowDocument; tên thuốc dùng Grid XAML để vừa căn cột vừa giữ điểm ngắt trang.
- Header thuốc in điền dữ liệu snapshot đồng bộ trước phân trang, tránh phụ thuộc lượt cập nhật binding của Dispatcher.
- Ảnh mẫu sạch tại XemTruoc/don-thuoc-chuyen-nghiep. Ảnh chi tiết là bố cục XAML thật với sự kiện tháo ra trong công cụ kiểm để không truy cập SQL.
- Chưa kiểm thử máy in vật lý hoặc tích hợp sự kiện với SQL thật. Nguồn và điều kiện quyền/bàn giao đã được giữ theo mã hiện có.
