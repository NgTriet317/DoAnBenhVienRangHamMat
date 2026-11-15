# Minh chứng ảnh X-quang lịch sử — 14/11/2026

- `snapshot-manifest.json`: 64 bảng, 3.296 dòng, bốn bản ghi ảnh. Cài mới `RHM_Hospital_Test_XrayHistory_20261114_B` từ SQL chính và dùng `tools/XuatDuLieuSql.ps1 -ChiKiemTra` xác nhận 64/64 bảng khớp số dòng và SHA-256 **trước** các phép xem ảnh sinh thêm nhật ký.
- `history-checks.txt`: kiểm cấu hình kho, hash/giải mã ảnh thật, nhãn nguồn trong cửa sổ lớn và xuyên SQL/BUS/WPF. Đọc tệp trong `PhatHanh/DuLieu/AnhXquang`, không dùng kho riêng của phép thử. Kiểm admin, bác sĩ NV003, tiếp nhận NV005, kỹ thuật viên NV004 và từ chối thu ngân NV008.
- `LS017-history.png`, `LS017-fullview.png`: ảnh chụp control lịch sử và cửa sổ xem lớn bằng renderer WPF, thể hiện bệnh nhân/lượt, dịch vụ liên kết, ảnh và nguồn/nhãn tham khảo.

Kiểm bổ sung: X-quang hiện có 34/34, DICOM 27/27; build Debug/Release và schema SQL đạt. Nhập lại thêm 0 ảnh/0 nhật ký nhập. Tệp hash sai và database thiếu liên kết bị từ chối, tệp/số ảnh đã có giữ nguyên. File SQL đóng gói trong `PhatHanh/Database` có cùng SHA-256 với SQL chính; bốn JPG đóng gói khớp manifest. Các log chi tiết, database thử và bản sao lưu trước nhập nằm trong `obj/XrayHistoryDataChecks` và SQL Server cục bộ.

Ảnh trong minh chứng là tham khảo công khai của hồ sơ giả lập, không phải phim chụp của bệnh nhân trong ứng dụng. [Nguồn/giấy phép từng ảnh](../../../../Database/AnhXquang/README.md).
