# Ảnh và kiểm tra báo cáo bệnh lý

- `benh-ly-du-lieu-sql-1920.png`: giao diện dùng dữ liệu SQL hiện có, tháng11/2026 lũy kế đến15/11 đối chiếu tháng10/2026 cùng số ngày.
- `chi-tiet-benh-nhan-sql-1920.png`: bốn ô tổng quan mới và bảng bệnh nhân/ngày giờ/dịch vụ/chẩn đoán/tình trạng, dùng dữ liệu SQL hiện có.
- `chi-tiet-benh-nhan-minh-hoa-1920.png`: bảng chi tiết từ fixture minh họa, có các lần tái khám của cùng một bệnh nhân.
- `so-sanh-minh-hoa-1024.png`, `so-sanh-minh-hoa-1366.png`, `so-sanh-minh-hoa-1920.png`: dữ liệu fixture trong bộ nhớ để kiểm hai tháng khác năm, bệnh đứng đầu khác nhau và bệnh chỉ có ở kỳ gốc; không phải số liệu SQL của bệnh viện.
- `KET_QUA_KIEM_TRA.txt`: 54 ca PASS từ lần chạy cuối `tools/KiemTraBaoCao.ps1 -Sql -Render`.

Xem [hướng dẫn và định nghĩa thống kê](../../docs/sprint3/BAO_CAO_BENH_LY_SO_SANH_THANG.md).
