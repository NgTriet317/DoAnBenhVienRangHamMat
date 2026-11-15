# Kế hoạch tổng quan bệnh lý và so sánh tháng

**Goal:** Thống kê bệnh nhân và bệnh phổ biến, so sánh hai tháng bất kỳ trong tab bệnh lý.

**Architecture:** Giữ truy vấn DISTINCT và kiểm quyền DAL. Bổ sung ghép kỳ trong BUS; WPF dùng chung kết quả cho bảng, biểu đồ, nhận xét và CSV.

**Tech Stack:** C# / WPF / .NET 8 / SQL Server.

**Spec:** `docs/superpowers/specs/2026-11-15-benh-ly-so-sanh-design.md`

## Ràng buộc

Giữ quy tắc ẩn nhóm dưới 5 người, không đoán mã từ chữ, không thêm thư viện/schema. Thực hiện trong workspace được người dùng chỉ định, không commit hoặc thay nhánh.

## Kiểm tra trọng tâm

Kỳ khác năm/tháng nhuận; MTD cùng số ngày; bệnh chỉ có ở kỳ gốc; nhóm bị ẩn; kỳ chưa đủ mã; đổi bộ lọc và xuất CSV không giữ số cũ.

## Công việc

1. [x] Bổ sung kiểm thử vào `tools/ReportChecks.cs.txt`: chọn tháng tùy ý, ghép hợp hai tập mã, tỷ lệ theo số người, đồng hạng, nhóm bị ẩn và kỳ thiếu mã. Chạy `tools/KiemTraBaoCao.ps1` để thấy thất bại trước khi triển khai.
2. [x] Mở rộng `ET/ET_THONGKE.cs`, `BUS/SoSanhThongKe.cs`, thêm `BUS/ThongKeBenhLy.cs`; cập nhật `Controls/DuLieuSoSanhBaoCao.cs` để giữ cả hai kỳ và xuất toàn bộ bệnh. Chạy lại kiểm thử.
3. [x] Chỉnh `Views/BaoCao.xaml`, `Views/BaoCao.xaml.cs`, `Controls/BaoCaoSoSanh.cs`, thêm phần điều phối riêng cho bệnh lý: chọn tháng/năm, tổng quan, bảng hai kỳ và biểu đồ bệnh phổ biến. Kiểm tra build, bộ kiểm báo cáo, SQL đọc dữ liệu nếu có và ảnh WPF ở 1024/1366/1920.
4. [x] Rà soát diff, quyền, số đếm DISTINCT và CSV, ghi kết quả kiểm tra.

Hoàn tất: bộ kiểm SQL/WPF 47/47 đạt; diff không có lỗi whitespace. Rà soát độc lập đã xử lý giới hạn nhóm ẩn khi xếp theo lượt, đồng nhất đồng hạng và hồi quy chọn nhanh 12 tháng.
