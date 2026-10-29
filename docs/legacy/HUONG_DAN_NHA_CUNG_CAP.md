# Nhà cung cấp

Mở **Nhà cung cấp** từ màn hình chính theo quyền tài khoản hiện có. Màn hình đọc trực tiếp SQL Server; database trên máy đang có 10 nhà cung cấp tương ứng dữ liệu trong [file SQL](../../Database/QLBenhVienRangHamMat.sql).

## Sử dụng

- Tìm theo mã, tên, người liên hệ, điện thoại, email hoặc địa chỉ; tìm không phân biệt hoa/thường.
- Lọc **Đang hoạt động**, **Ngừng hoạt động**; chọn **Bỏ bộ lọc** để xem toàn bộ.
- Chọn một dòng để xem đầy đủ thông tin và số thiết bị, lô hàng, hóa đơn nhập liên kết. **Sao chép** đưa thông tin liên hệ vào clipboard.
- **Thêm nhà cung cấp**: tên, người liên hệ, email và trạng thái là bắt buộc. Điện thoại và địa chỉ được phép bỏ trống theo SQL. Mã tự sinh khi lưu, không nhập thủ công.
- **Cập nhật** để sửa thông tin, ngừng hoạt động hoặc hoạt động trở lại. Điện thoại giữ số 0 đầu; số điện thoại nhập phải có 8–15 chữ số, có thể chứa mã quốc gia và dấu phân cách thông dụng.
- **Xóa** chỉ khả dụng khi chưa có dữ liệu liên kết. SQL/DAL kiểm tra lại cả ba bảng, kể cả khi dữ liệu thay đổi sau lần tải gần nhất. Với đối tác đã được sử dụng, chuyển **Ngừng hoạt động** để giữ lịch sử.
- **Tải lại** đọc dữ liệu mới từ SQL. Lỗi nhập/lưu giữ nguyên form; bản cũ bị chặn khi dữ liệu đã được sửa ở nơi khác.

Thống kê phía trên tính toàn bộ danh mục; số dòng dưới bảng theo bộ lọc. Bảng và vùng chi tiết cuộn riêng, hỗ trợ cửa sổ 1366 và 1024; vùng chi tiết chuyển xuống dưới khi chiều rộng nội dung dưới 900 DIP.

## Ba lớp và phong cách dự án

Luồng: `NhaCungCap / CuaSoNhaCungCap → BieuMauHoSo / DuLieuGiaoDien → BUS_NHACUNGCAP → DAL_NHACUNGCAP → SQL Server`. Tái sử dụng `ET_NHACUNGCAP` ánh xạ đúng 7 cột, Dapper, SQL có tham số, giao dịch, cấp mã và kiểm tra sửa đồng thời hiện có. Theo cách tổ chức các DAL của dự án, kiểm tra trước khi ghi được bổ sung tại DAL và áp dụng cho mọi nơi gọi BUS.

Cache bổ sung `NHA_CUNG_CAP` và `HOA_DON_NHAP`; dùng các bảng `THIET_BI`, `LO_HANG_HOA` đã tải để hiển thị số liên kết. XAML dùng Tag `DB:NHA_CUNG_CAP.Cot` và nút `HoSoSave` để dùng chung cơ chế form. Các style ô nhập và ô bảng dùng chung với bảo trì qua `Themes/DanhMucStyles.xaml`, kế thừa `HospitalStyles.xaml`.

Không cần thêm cột hoặc chạy lại script tạo database trên máy hiện tại. Kết nối tiếp tục dùng `DAL/dbconnect.cs` và `RHM_CONNECTION_STRING` nếu đã cấu hình.

## Kiểm tra

```powershell
dotnet build BenhVienRangHamMat.sln -c Release
dotnet run --project RangHamMat.SupplierChecks -c Release
dotnet run --project RangHamMat.MaintenanceChecks -c Release
```

`SupplierChecks` kiểm tra control WPF và SQL thật: đọc dữ liệu, tìm/lọc, chi tiết, thống kê/liên kết, thêm/lưu/sửa từ nút thật, lỗi nhập liệu, cấp mã, điện thoại, nullable, đổi trạng thái, xóa, bảo vệ liên kết và chống ghi đè. Dữ liệu ghi thử được rollback; đối chiếu lại toàn bộ 7 trường để xác nhận SQL ban đầu giữ nguyên.

Ảnh kiểm tra: [màn hình 1366](../../XemTruoc/nha-cung-cap/nha-cung-cap-1366.png), [màn hình 1024](../../XemTruoc/nha-cung-cap/nha-cung-cap-1024.png), [biểu mẫu](../../XemTruoc/nha-cung-cap/bieu-mau-nha-cung-cap.png), [báo lỗi nhập liệu](../../XemTruoc/nha-cung-cap/bieu-mau-nha-cung-cap-bao-loi.png).
