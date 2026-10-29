# Giao diện xin nghỉ phép / nghỉ việc

## Vị trí

- Ở góc trên bên phải trang chủ `MainWindow` có nút **Xin nghỉ việc**.
- Khi mở các phân hệ khác, nút vẫn nằm ở bên phải thanh tiêu đề `AppWorkspace`.
- Nhấn nút để mở view `Views/XinNghiViec.xaml` bên trong `MainWindow` (không mở cửa sổ mới).
- Dùng nút **Màn hình chính** để quay lại trang chủ.

## Hai tab

1. **Đơn xin nghỉ phép**: mã nhân viên, loại nghỉ, từ ngày, đến ngày, lý do và người bàn giao.
2. **Đơn xin nghỉ việc**: mã nhân viên, ngày dự kiến nghỉ việc, lý do và kế hoạch bàn giao.

Nếu phiên đăng nhập có mã nhân viên, biểu mẫu tự điền và khóa mã đó. Với phiên không có mã nhân viên, người dùng có thể tự nhập mã để xem trước.

Các nút **Xem trước đơn** kiểm tra các trường bắt buộc và ngày hợp lệ, sau đó hiển thị nội dung bằng MessageBox. **Nhập lại** xóa phần đang nhập của tab tương ứng.

**Chưa có chức năng lưu/gửi đơn hay duyệt đơn qua database**, vì database hiện chưa có luồng quản lý loại đơn này. Các thay đổi chỉ bổ sung giao diện và chức năng xem trước, không sửa cấu trúc database hoặc các phân hệ nghiệp vụ khác.

## Các file đã sửa / tạo

- `RangHamMat.Wpf/MainWindow.xaml`
- `RangHamMat.Wpf/MainWindow.xaml.cs`
- `RangHamMat.Wpf/AppWorkspace.xaml`
- `RangHamMat.Wpf/AppWorkspace.xaml.cs`
- `RangHamMat.Wpf/Views/XinNghiViec.xaml` (mới)
- `RangHamMat.Wpf/Views/XinNghiViec.xaml.cs` (mới)

Mở `BenhVienRangHamMat.sln` bằng Visual Studio 2022, đặt `RangHamMat.Wpf` làm startup project và chạy bằng Windows. Không cần sửa `.csproj` vì dự án .NET SDK tự nhận các file `.xaml` và `.cs` mới.
