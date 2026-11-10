# Bằng chứng P4 ngày07/11/2026

Ảnh minh chứng và tệp kiểm chỉ dùng fixture được tạo cho database
`RHM_Hospital_Test_...`. Các log ở đây lọc dòng kết quả từ log đầy đủ trong
`obj/XrayEvidence`; không chứa snapshot SQL, `.bak` hoặc ảnh người bệnh.

| Tệp | Nội dung |
|---|---|
| `final-xray.log` |32/32 SQL/BUS/DAL/WPF; nguồn fixture cuối |
| `final-operations.log` |15 ảnh/metadata; backup SQL CHECKSUM; restore kho/DB khác; từ chối DB hiện hữu |
| `final-restored-ui.log` |2/2 mở ảnh qua BUS và cửa sổ WPF/phóng thu sau restore |
| `final-rerun.log` |64/64 bảng giữ fingerprint dữ liệu sau chạy lại SQL |
| `final-foundation.log`, `final-sql-sync.log` |35/35 nền tảng;32/32 ánh xạ và công/lương |
| `final-service.log`, `final-clinical.log`, `final-reports.log` |28/28 tiếp nhận;47/47 luồng khám;41 ca báo cáo PASS |
| `final-build-handover.log` |Debug/Release0 lỗi, cấu trúc4 project/SQL64; Release118 cảnh báo hiện có |
| `review-red.log`, `reassign-red.log`, `layout-red.log` |Ca tái hiện lỗi trước khi sửa; xem GREEN trong `final-xray.log` |

- [Bác sĩ: tải, danh sách và xem trước](xray-editor.png)
- [Kỹ thuật viên: phần được giao sau khi cuộn](xray-assigned.png)
- [Lịch sử chỉ xem](xray-history.png)
- [Thông báo kho chưa cấu hình](xray-unconfigured.png)
- [Viewer mở ảnh phục hồi, slider phóng2 lần](xray-fullview.png)

Review độc lập đã xác nhận đóng các lỗi: Loaded lịch sử mất quyền, duplicate
không kiểm tệp hiện hữu, thiếu luồng phân công từ ứng dụng, không thay được người
bị khóa, thiếu phiên audit XR_ASSIGN và fallback khôi phục quyền cũ. Sau review,
kiểm hình ảnh bổ sung phát hiện vùng xem trước bị che; đã sửa cuộn trang và kiểm
bố trí trên cửa sổ WPF thật. Chưa kiểm UNC giữa hai máy; xem phạm vi đầy đủ trong
[kết quả P4](../../KET_QUA_ANH_XQUANG_20261107.md).
