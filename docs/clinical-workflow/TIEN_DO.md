# Tiến độ triển khai

Ngày 25/10/2026. Kế hoạch chính: [KeHoach_CaiTo_QuyTrinhKham_6ChucNang.md](../../KeHoach_CaiTo_QuyTrinhKham_6ChucNang.md).

| Tác vụ | Trạng thái | Bằng chứng / phần còn lại |
| --- | --- | --- |
| P01 | Đã hoàn tất khảo sát baseline | Backup/verify/restore bản thử, build và toàn bộ suite cũ đã chạy; lỗi sẵn và chứng từ cần đối chiếu tại BASELINE.md |
| P02 | Đã hoàn tất quyền và tích hợp UI | Nguồn SQL có phạm vi, guard ghi chung transaction, cache theo phiên, quyền mở/sửa riêng, điều hướng và dữ liệu quầy thu giới hạn. 35 ca SQL + 8 ca WPF đạt; đã rà soát và sửa các phát hiện |
| P03–P12 | Chưa triển khai | Chưa có migration nâng/hạ cấp, UI/luồng khám mới, mẫu điều trị, đơn snapshot, bảng tính, thu đủ/chống lặp hoặc bàn giao thuốc |

## Quyết định khi thực hiện

1. Làm trên nhánh riêng trong checkout đang mở theo P01. Không tạo worktree phụ. Chi phí nếu cần đổi hướng: chuyển nhánh hoặc tạo worktree ở lần tiếp theo; chỉnh sửa đang ở nhánh riêng.
2. Ghi nhận bốn lỗi suite cũ, tiếp tục phần nền độc lập thay vì sửa nhân sự/lịch ngoài phạm vi. Chi phí: chưa có cổng nghiệm thu toàn dự án xanh; phải xử lý rõ các lỗi đó trước P12.
3. Đợt nền chưa mở màn hình cho vai trò mới; sau tích hợp P02 đã mở đúng năm module theo quyền nghiệp vụ, với nguồn đọc và guard ghi tương ứng. Bộ lọc/danh sách giao diện tiếp tục được cải tổ trong P04–P11.
4. Giữ kế hoạch gốc ở thư mục đồ án và đặt bằng chứng trong `docs/clinical-workflow/`, tránh hai bản Markdown tiến độ lệch nhau. Không sửa Word.
5. Thêm đúng một lệnh khởi tạo principal sau đăng nhập thành công để ràng buộc API mới với tài khoản phiên; đây là tích hợp tối thiểu cần thiết sau rà soát. Không đổi thuật toán xác thực; cần giữ điểm tích hợp khi sửa đăng nhập về sau.
6. Hóa đơn lịch sử trong phạm vi lâm sàng chỉ đối chiếu/in lại; khóa thao tác lập/thu/cấp cũ trước khi có P09. Chi phí: chưa có thao tác thu đủ/giao thuốc mới để sử dụng cho đến khi triển khai P09/P10; không thu lại chứng từ lịch sử.
7. Sửa tối thiểu `LichKham.xaml/.cs` ở chế độ nhúng trong hồ sơ để dùng nguồn SQL đã giới hạn và mở chi tiết chỉ đọc. Chi phí nếu sai: có thể ảnh hưởng lịch độc lập; đã thêm ca WPF xác minh lịch độc lập giữ nguồn và nút cũ. Thuật toán đặt lịch không đổi.
8. Thêm `RangHamMat.ClinicalUiChecks` vì WpfSmoke đang không biên dịch do API nhân sự cũ. Chi phí: thêm một project kiểm tra; không thay thế nghiệm thu WpfSmoke/AT30 ở P12.

## Rà soát độc lập

Đã rà soát phần nền. Phát hiện quan trọng: chỉ kiểm tra MaNV/MaTK trong DTO còn cho phép thay toàn bộ định danh. Đã tái hiện bằng bốn ca FAIL và sửa bằng principal tại điểm đăng nhập; cả bốn ca chuyển PASS. Assertion từ chối quyền dùng exception riêng, không chấp nhận lỗi SQL bọc trong InvalidOperationException làm PASS.

Lượt rà soát tích hợp P02 phát hiện lỗi so sánh số dạng chuỗi khi điều dưỡng lưu, nội dung khám nháp đi kèm dòng được giao, ghi chú chuyên môn trong dữ liệu thu và lịch nhúng đọc nguồn toàn viện. Đã tái hiện từng lỗi trước khi sửa: SQL 32 đạt/3 lỗi → 35/35; WPF 6 đạt/1 lỗi → 7/7; bổ sung kiểm tra lịch độc lập thành 8/8. Các view không mở lượt nháp của người khác từ ngữ cảnh tối thiểu; lỗi quyền khi mở chi tiết được hiển thị thay vì thoát ứng dụng.

Không còn phát hiện quan trọng chưa xử lý trong P02. P03 chưa thay đổi schema: bước tiếp theo là migration, hợp đồng ET/DAL và kiểm tra nâng cấp/rollback trên bản sao. P04–P12, phân trang SQL quy mô lớn, giao diện mới và giao dịch thu đủ chưa hoàn tất.
