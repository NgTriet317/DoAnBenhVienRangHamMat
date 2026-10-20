# Lịch sử bảo trì thiết bị

Mở phân hệ **Bảo trì** từ màn hình chính hoặc mục Thiết bị. Quyền mở phân hệ giữ theo tài khoản hiện có của dự án.

## Sử dụng

- Tìm bằng mã lần bảo trì, mã/tên thiết bị, serial, phòng, nhân viên hoặc nội dung.
- Lọc theo ngày bắt đầu, trạng thái; bỏ trống ngày để xem toàn bộ lịch sử.
- Chọn một dòng để xem nội dung đầy đủ, nhân viên phụ trách, chi phí, ngày kết thúc và lịch tiếp theo ở vùng chi tiết.
- Chọn **Thêm lần bảo trì**, điền trường bắt buộc và lưu. Mã được cấp tự động khi lưu thành công.
- **Cập nhật** áp dụng cho Nháp và Đang thực hiện. Chỉ **Xóa bản nháp**; lần đang thực hiện có thể chuyển Hủy để giữ lịch sử.
- Hoàn thành, Đã xác nhận và Hủy được giữ nguyên. Khi hoàn thành/xác nhận phải nhập ngày kết thúc.
- Chi phí không âm, tối đa hai chữ số thập phân; ngày kết thúc không trước ngày bắt đầu. Lịch tiếp theo phải sau ngày kết thúc, hoặc sau ngày bắt đầu khi chưa kết thúc.
- **Tải lại** đọc lại SQL. Lưu thất bại giữ form và hiển thị lỗi; bản đã thay đổi ở nơi khác được chặn ghi đè.

Thống kê phía trên tính trên toàn bộ lịch sử; số dòng dưới bảng theo bộ lọc. Thiết bị đến/quá hạn chỉ lấy ngày hẹn của lần mới nhất chưa hủy trên từng thiết bị, tránh đếm lại các hẹn cũ.

## Database và ba lớp

Đối chiếu bảng `BAO_TRI_THIET_BI` trong [file SQL](Database/QLBenhVienRangHamMat.sql). Database cấu hình trên máy đã có bảng và 10 dòng, được giao diện đọc trực tiếp; không cần chạy lại script tạo database. Chức năng này không yêu cầu thêm cột hoặc sửa dữ liệu mẫu.

Luồng lưu: `BaoTri / CuaSoBaoTri → BieuMauHoSo / DuLieuGiaoDien → BUS_BAOTRITHIETBI → DAL_BAOTRITHIETBI → SQL Server`. `ET_BAOTRITHIETBI` tiếp tục ánh xạ đúng 10 cột SQL. Tái sử dụng Dapper, SQL có tham số, giao dịch, cấp mã và kiểm tra sửa đồng thời của dự án. Theo phong cách các DAL hiện có, kiểm tra trước khi ghi được thực hiện tại DAL để tất cả nơi gọi BUS cùng áp dụng.

Tên thiết bị lấy qua `THIET_BI.MaHH → HANG_HOA.TenHH`; phòng qua `THIET_BI.MaPhong → PHONG`; người phụ trách qua `MaNV → NHAN_VIEN`. Giao diện không dùng các cột cũ không có trong SQL như `LoaiBaoTri`, `NgayBaoTri`, `NguoiThucHien`.

Kết nối tiếp tục dùng `DAL/dbconnect.cs` và biến môi trường `RHM_CONNECTION_STRING` nếu có.

## Kiểm tra

```powershell
dotnet build BenhVienRangHamMat.sln -c Release
dotnet run --project RangHamMat.MaintenanceChecks -c Release
```

`MaintenanceChecks` chạy control WPF thật và SQL thật: đọc dữ liệu, tra tên, tìm kiếm/lọc, chi tiết, thêm/lưu từ nút thật, cập nhật/xóa nháp, số tiền lẻ, nhập sai, sửa đồng thời, giữ lịch sử và tính hạn theo thiết bị. Mọi thao tác ghi thử nằm trong giao dịch được rollback.

Ảnh kiểm tra nằm tại [XemTruoc/bao-tri](XemTruoc/bao-tri), gồm màn hình 1366, 1024 và form thêm bảo trì.

Bộ `RangHamMat.WpfSmoke` cũ hiện không biên dịch do `NhapLieuChecks` gọi `NhanSu.TryReadPosition/TryReadEmployee` không tồn tại. Bộ kiểm tra bảo trì riêng chạy độc lập với lỗi có sẵn này.
