# Dịch vụ theo tình trạng và cải tiến màn khám

Yêu cầu: tình trạng bệnh phải dẫn tới các dịch vụ thực sự có trong SQL; màn khám gọn và dễ dùng. Giữ bốn project WPF/ET/BUS/DAL, không đưa lại cấu hình mẫu điều trị.

Thiết kế: thêm bảng liên kết DICH_VU_TINH_TRANG (FK tới DM_TINH_TRANG_BENH và DICH_VU, khóa ghép, nội dung đề xuất, thứ tự). Nạp dịch vụ còn thiếu theo danh mục/giá dịch vụ công khai BVĐK Bình Định hiệu lực 15/6/2026; đây là giá nguồn tham khảo được cấu hình cho đồ án, không phải xác nhận giá của một cơ sở khác. Không sửa giá/danh tính/chứng từ cũ. Schema tăng từ52 lên53 bảng, solution vẫn bốn project.

Sau nhập hoặc chọn tình trạng: hiển thị các dịch vụ đang hoạt động được liên kết, có mã/tên/giá SQL. Không chọn sẵn thủ thuật; bác sĩ chọn dịch vụ phù hợp rồi tạo kế hoạch. Lưu tình trạng trước khi tạo kế hoạch để FK MaTTBNguon trỏ đúng dòng đã lưu. Dòng kế hoạch có sẵn mã dịch vụ, tên, giá, số lượng1 và nội dung; không sinh bước trống mã. Không ghi đè kế hoạch đang soạn. DAL đối chiếu lại hoạt động/giá khi lưu bước mới, báo xung đột nếu nguồn đã đổi. Giá của bước cũ giữ snapshot lịch sử.

Giao diện: các bước Khám → Tình trạng & dịch vụ → Kế hoạch → Tổng kết; thẻ trắng trên nền dịu, nhãn rõ, nút chính nổi bật, mô tả/ghi chú phụ thu gọn. Danh sách dịch vụ chọn bằng checkbox, kế hoạch có tổng tiền; bớt hướng dẫn dài/URL trong luồng chính. Bộ lọc hàng chờ thu gọn, tìm nhanh giữ ngay đầu trang. Bố cục dùng được khi khung khám hẹp, không đặt hai cột cố định trong editor.

## Triển khai

- [x] RED: kiểm DB liên kết và UI dịch vụ chưa có.
- [x] Migration09 + installer; ET/DAL/BUS đọc liên kết, bảo vệ giá và lưu đúng tình trạng nguồn.
- [x] UI tình trạng/dịch vụ/kế hoạch, giá tự điền, tổng tiền và bộ lọc gọn.
- [x] Kiểm fixture cài mới/nâng cấp/lặp, CRUD/khác bác sĩ/đổi giá/ngừng dịch vụ, ảnh WPF ở nhiều kích thước.
- [x] Backup+đối chiếu dữ liệu cũ, áp dụng database chính, build/publish và hướng dẫn.

Nguồn danh mục/giá: https://benhvienbinhdinh.com.vn/wp-content/uploads/2026/06/BANG-GIA-BENH-VIEN-hieu-luc-ngay-15.6.2026_W.pdf (trang42,98 của PDF; số thứ tự1726,1727,1730,1731,3980,3982,3984,3986). Đối chiếu tên kỹ thuật; thời gian dự kiến do đồ án cấu hình, không lấy từ bảng giá. Liên kết bệnh–dịch vụ là nhóm lựa chọn cho bác sĩ, không phải chỉ định tự động.

## Kết quả

- RED: 19 đạt/2 lỗi đúng phần SQL liên kết và danh sách dịch vụ còn thiếu.
- Sau thực hiện: 29/29 trên bản cài mới và 29/29 trên bản nâng cấp08→09; chạy lặp09 giữ giá đã sửa và14 liên kết.
- DLL publish:29/29; hồi quy Admin12/12 và hàng chờ16/16. Kiểm tra editor tại1024×768,800×600,440×760 và hàng chờ1280×850,1024×768,800×600.
- Review độc lập phát hiện gắn nhầm dòng trùng tên và cache giá cũ; đã sửa, thêm kiểm tra chọn đúng MaTTB/cập nhật giá giữ nháp/chọn ComboBox thật. Review lại không còn phát hiện chức năng cần sửa.
- Database chính đã sao lưu COPY_ONLY CHECKSUM và VERIFYONLY; migration09 chỉ thêm8 dịch vụ/14 liên kết/bảng mới, hash toàn bộ dòng cũ của52 bảng khớp trước/sau. Tổng53 bảng,14 bác sĩ, không đổi giá và chứng từ cũ.
- Bản phát hành giữ appsettings, có SQL/migration09 mới. Hướng dẫn thao tác: chọn Tình trạng và dịch vụ → nhập Sâu răng có lỗ → chọn Composite hoặc GIC phù hợp → Lập kế hoạch → Lưu kế hoạch. Các lựa chọn thay thế không tự được chọn cộng dồn.

Bằng chứng trong [evidence/RelatedServices](evidence/RelatedServices/). Giới hạn: đây là nhóm lựa chọn cho bác sĩ; không suy chỉ định từ ghi chú tự do, không tự duyệt, thu tiền hay xuất kho. Bảng giá nguồn là tham khảo cho đồ án; ứng dụng lấy giá hiện hành trong DICH_VU, giữ snapshot giá kế hoạch cũ. Hồ sơ nhân sự/bệnh nhân có thật vẫn cần nguồn được phép sử dụng.
