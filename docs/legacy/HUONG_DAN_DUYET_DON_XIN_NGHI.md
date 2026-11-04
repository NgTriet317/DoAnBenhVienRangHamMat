# Bổ sung duyệt đơn xin nghỉ trong Ca làm

## Các bước sử dụng

1. Đăng nhập bằng tài khoản **Admin** có liên kết mã nhân viên (`PhienDangNhap.MaNV`).
2. Mở phân hệ **Ca làm**. Phần trên giao diện hiển thị hộp thông báo số **đơn chờ duyệt** từ bảng `DON_XIN_NGHI`.
3. Nhấp vào hộp thông báo để mở view `DonXinNghiQuanLy` (DataGrid). Ban đầu hiển thị các đơn **Chờ duyệt**; chọn **Tất cả trạng thái** để xem lịch sử.
4. Lọc theo **tên nhân viên**, **trạng thái**, **chuyên khoa**; có thể kết hợp 3 bộ lọc. Nhấp **Tải lại** để cập nhật dữ liệu mới nhất từ SQL.
5. Nhấp đúp vào đơn để xem chi tiết. Nếu còn chờ duyệt, quản lý chọn **Duyệt đơn** hoặc **Không duyệt**. Với **Không duyệt** phải điền lý do rồi xác nhận.
6. Sau khi cập nhật, đơn được giữ lại ở DataGrid. Lý do từ chối được lưu vào `GhiChu`, kèm `MaNVDuyet` và `NgayDuyet`.
7. Nhấn **Về Ca làm** để đóng view; hộp thông báo được tải lại số đơn chờ duyệt.

## Quy tắc xử lý

- Chỉ tài khoản có quyền `IsAdmin` xem được view quản lý.
- Không thể tự duyệt đơn của chính mình; database cũng có CHECK ràng buộc mã người duyệt khác mã người xin nghỉ.
- UPDATE chỉ được phép với bản ghi còn `Chờ duyệt` và chưa có người duyệt; nếu 2 quản lý xử lý cùng lúc, chỉ thao tác đầu tiên thành công.
- Trạng thái sau xử lý là **Đã duyệt** hoặc **Không duyệt**. Bản ghi giữ lại, không bị xóa.
- **Đã bổ sung phân công người thay và đồng bộ lịch khám**: trước khi duyệt đơn, chọn người thay cho các ca bị ảnh hưởng. Duyệt sẽ cập nhật ca làm và lịch khám trong cùng giao dịch. Xem chi tiết tại `HUONG_DAN_LUONG_XIN_NGHI_PHAN_CA.md`. Không tự đổi trạng thái nhân viên nghỉ việc.
- Loại đơn **Nghỉ phép / Nghỉ việc** đang được phân biệt qua `GhiChu` do giao diện gửi đơn đã tạo trước đó; đơn mẫu cũ thiếu nhãn loại sẽ hiển thị **Chưa xác định**.
- Hộp thông báo cập nhật khi mở phân hệ hoặc quay về từ view, không phải thông báo thời gian thực. Có thể nhấn hộp để mở danh sách mới nhất.

## Các file thay đổi

- `RangHamMat.Wpf/Views/CaLamViec.xaml` + `.xaml.cs` — hộp thông báo, hiển thị view bên trong.
- `RangHamMat.Wpf/Views/DonXinNghiQuanLy.xaml` + `.xaml.cs` — mới: DataGrid, 3 bộ lọc, chi tiết, duyệt/từ chối.
- `DAL/DAL_DON_XIN_NGHI.cs` — JOIN thông tin nhân viên/chuyên khoa, đếm đơn, cập nhật duyệt.
- `BUS/BUS_DON_XIN_NGHI.cs` — đọc danh sách, quy tắc kiểm tra khi xử lý.
- `ET/ET_DON_XIN_NGHI_HIENTHI.cs` — mới: mô hình dữ liệu hiển thị.

**Không cần thay đổi bảng SQL hoặc thêm NuGet.** Các dự án dùng kiểu SDK sẽ tự nhận `.xaml` / `.cs` mới. Chạy trên Windows với Visual Studio 2022 và SQL Server đã cấu hình. Chưa xác nhận build WPF trong môi trường Linux của phiên chỉnh sửa.
