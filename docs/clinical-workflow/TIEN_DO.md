# Tiến độ triển khai

Ngày 25/10/2026. Kế hoạch chính: [KeHoach_CaiTo_QuyTrinhKham_6ChucNang.md](../../KeHoach_CaiTo_QuyTrinhKham_6ChucNang.md).

| Tác vụ | Trạng thái | Bằng chứng / phần còn lại |
| --- | --- | --- |
| P01 | Đã hoàn tất khảo sát baseline | Backup/verify/restore bản thử, build và toàn bộ suite cũ đã chạy; lỗi sẵn và chứng từ cần đối chiếu tại BASELINE.md |
| P02 | Đang thực hiện | Đã có hợp đồng, mapping chức vụ, principal phiên, SQL scoped read/guard và ClinicalChecks. Chưa đổi nguồn/cache toàn bộ UI, điều hướng hoặc lệnh ghi cũ; chưa nghiệm thu P02 |
| P03–P12 | Chưa triển khai | Chưa có migration nâng/hạ cấp, UI/luồng khám mới, mẫu điều trị, đơn snapshot, bảng tính, thu đủ/chống lặp hoặc bàn giao thuốc |

## Quyết định khi thực hiện

1. Làm trên nhánh riêng trong checkout đang mở theo P01. Không tạo worktree phụ. Chi phí nếu cần đổi hướng: chuyển nhánh hoặc tạo worktree ở lần tiếp theo; chỉnh sửa đang ở nhánh riêng.
2. Ghi nhận bốn lỗi suite cũ, tiếp tục phần nền độc lập thay vì sửa nhân sự/lịch ngoài phạm vi. Chi phí: chưa có cổng nghiệm thu toàn dự án xanh; phải xử lý rõ các lỗi đó trước P12.
3. Chưa mở các màn hình cũ cho vai trò mới trước khi đổi đủ nguồn đọc/ghi. Chi phí: quyền nghiệp vụ mới hiện chỉ được dùng qua API và kiểm thử, chưa sử dụng từ UI.
4. Giữ kế hoạch gốc ở thư mục đồ án và đặt bằng chứng trong `docs/clinical-workflow/`, tránh hai bản Markdown tiến độ lệch nhau. Không sửa Word.
5. Thêm đúng một lệnh khởi tạo principal sau đăng nhập thành công để ràng buộc API mới với tài khoản phiên; đây là tích hợp tối thiểu cần thiết sau rà soát. Không đổi thuật toán xác thực; cần giữ điểm tích hợp khi sửa đăng nhập về sau.

## Rà soát độc lập

Đã rà soát phần nền. Phát hiện quan trọng: chỉ kiểm tra MaNV/MaTK trong DTO còn cho phép thay toàn bộ định danh. Đã tái hiện bằng bốn ca FAIL và sửa bằng principal tại điểm đăng nhập; cả bốn ca chuyển PASS. Assertion từ chối quyền dùng exception riêng, không chấp nhận lỗi SQL bọc trong InvalidOperationException làm PASS.

Các việc nằm ngoài đợt nền đầu tiên được giữ trong kế hoạch: API ghi mới và UI P02, cache theo phiên, P03–P12. Đây là việc còn phải làm, không phải lỗi được đánh dấu đã giải quyết. Không có đề xuất nhỏ nào còn treo từ lượt rà soát phần nền.
