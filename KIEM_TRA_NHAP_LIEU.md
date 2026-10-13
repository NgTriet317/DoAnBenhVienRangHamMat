# Rà soát và bổ sung kiểm tra nhập liệu

Ngày thực hiện: 13/10/2026. AI đã rà XAML/code-behind, điểm lưu từ giao diện, kiểm tra BUS/DAL và ràng buộc SQL. Phần kiểm tra mới có chú thích tiếng Việt ngay trong code.

Trước khi sửa, ca thử trên form bệnh nhân thật xác nhận số điện thoại `abc123` vẫn lưu được. Nhiều luồng đã kiểm tra bắt buộc/liên kết/nghiệp vụ, nhưng chưa thống nhất kiểm tra định dạng, độ dài, thông báo và lỗi binding.

## Các trường hợp đã xử lý

| Trường hợp | Cách xử lý |
|---|---|
| Trường bắt buộc trống hoặc chỉ có khoảng trắng | Chặn lưu, gọi tên trường tiếng Việt và đưa focus về ô sai. |
| Thông tin nullable chưa biết | Cho bỏ trống; không tự gán ngày sinh, giới tính hoặc điện thoại. |
| Điện thoại có chữ, quá ngắn/dài | Yêu cầu 8–15 chữ số; cho phép + ở đầu, khoảng trắng, ngoặc, chấm/gạch nối. Giữ số 0 đầu. |
| Chuỗi quá dài | Đối chiếu giới hạn SQL, ví dụ tên 255 ký tự và mã 50 ký tự. |
| Ngày sai/không tồn tại | Báo lỗi khi rời ô; chặn lưu kể cả khi WPF phục hồi ngày cũ. Cần sửa hoặc chọn lại ngày trên lịch. |
| Ngày sinh tương lai; ngày kết thúc trước bắt đầu | Chặn và chỉ rõ trường cần sửa. |
| Chữ trong ô số, NaN, Infinity, tràn int/decimal | Chặn trước khi lưu; không tự đổi thành 0 hoặc lưu giá trị cũ. |
| Số nguyên/số lượng/giá không hợp lệ | Kiểm tra số nguyên và phạm vi; lượng dương, đơn giá không âm ở luồng nghiệp vụ. |
| Quá nhiều chữ số lẻ | Tiền tối đa 2 số lẻ, lượng tối đa 3 số lẻ, theo precision/scale cột; không tự làm tròn dữ liệu sai. |
| Dấu phẩy/chấm | Đọc như dấu thập phân thống nhất giữa form và DataGrid; không dùng phân cách hàng nghìn. |
| Ô DataGrid đang sửa sai kiểu | Giữ chữ đang nhập, đánh dấu lỗi và chặn commit/lưu; không ghi giá trị cũ trong model. |
| Số lượng × giá/tổng vượt giới hạn | Báo lỗi, không để tràn số làm sập màn hình; kiểm tra trước khi lưu dòng. |
| Giảm giá sai hoặc vượt tổng | Hiện lỗi tại form hóa đơn và chặn tạo hóa đơn. |
| Thu/hoàn bằng 0, sai tiền hoặc hoàn thiếu lý do | Chặn tại giao diện; tầng lưu tiếp tục kiểm tra số dư/chứng từ trong transaction. |
| Nhân sự/chức vụ thiếu thông tin hoặc sai ngày/điện thoại | Chặn trước khi gọi BUS, thông báo trong XAML và giữ nguyên form; mã chức vụ hiển thị tự sinh. |
| Lưu thành công nhưng tải lại lỗi | Báo rõ dữ liệu đã lưu, tránh thông báo nhầm thao tác thêm thất bại. |
| Đăng nhập thiếu tên/mật khẩu | Chặn bước tiếp tục trên giao diện mẫu; chưa phải xác thực SQL. |

Lỗi hiển thị qua vùng thông báo, viền đỏ/tooltip và focus phù hợp. Ô có Tag DB hợp lệ được kiểm tra khi rời ô. Form nhiều dòng kiểm tra các dòng trước khi lưu và giữ cơ chế rollback. Nhập sai không xóa form hoặc đóng modal.

## Phạm vi màn hình

| Màn hình/luồng | Kiểm tra |
|---|---|
| Bệnh nhân, bệnh án, tiếp nhận | Kiểu/độ dài, bắt buộc, ngày, điện thoại và liên kết trước khi lưu. |
| Lịch hẹn | Kiểm tra chung và kiểm tra HH:mm, thời lượng 5–480 phút, ưu tiên nguyên không âm, trùng lịch/lý do hẹn lại hiện có. |
| Khám, tình trạng, kế hoạch, bước và dịch vụ thực hiện | Form/ô bảng, từng dòng lượng/giá/lần thứ, ngày và tổng tiền; giữ khóa trạng thái đã xác nhận. |
| Đơn thuốc/dòng thuốc | Thuốc/liều/hướng dẫn, lượng tối đa 3 số lẻ, ngày dùng nguyên dương và độ dài liều trước khi lưu đầu đơn. |
| Hóa đơn, thu/hoàn, cấp thuốc | Kiểm tra đầu vào giao diện và giữ kiểm tra nghiệp vụ/giao dịch SQL. |
| Nhân sự/chức vụ | Kiểm tra trước khi thêm, thông báo trong XAML, giới hạn ngày sinh. |
| Đăng nhập | Chặn thiếu thông tin; vẫn ghi rõ chưa xác thực SQL. |
| Form mẫu bảo trì, ca làm, dịch vụ/danh mục, nhà cung cấp, nhập xuất vật tư, phiếu hướng dẫn, tài khoản, thiết bị, vật tư | Nút lưu vẫn tắt vì chưa có luồng lưu. Tag phù hợp schema được kiểm tra khi rời ô; Tag cũ có kiểm tra trường *, định dạng số/ngày và độ dài tên cột chung. Điều này không ánh xạ Tag cũ thành SQL hoặc bổ sung CRUD. Biến động số lượng lô trên form cũ được phép âm. |
| Form bảng lương | Vùng hiển thị mẫu, chưa có ô nhập/lưu riêng. |

Giới hạn hiện có: chưa xác thực tài khoản SQL và chưa nối lưu các form mẫu nói trên. Kiểm tra định dạng không bảo đảm điện thoại/email thực sự tồn tại. Kiểm tra liên kết, bản ghi cũ, khóa chứng từ và rollback vẫn thuộc luồng lưu.

## Code và cách hoạt động

- [KiemTraNhapLieu.cs](RangHamMat.Wpf/Controls/KiemTraNhapLieu.cs): đọc quy tắc cột, kiểm tra giá trị/binding/ngày, xử lý khi rời ô, thông báo và focus.
- [QuyTacNhapLieu.xml](RangHamMat.Wpf/Controls/QuyTacNhapLieu.xml): metadata từ v8, 44 bảng/349 cột có thể ghi; không chứa dữ liệu bệnh nhân.
- [NhapSoConverter.cs](RangHamMat.Wpf/Controls/NhapSoConverter.cs): converter và ValidationRule cho lượng/giá. Converter trả UnsetValue khi sai để tránh ngoại lệ runtime, theo [tài liệu WPF của Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.windows.data.ivalueconverter.convertback).
- [BieuMauHoSo.cs](RangHamMat.Wpf/Controls/BieuMauHoSo.cs), [DuLieuGiaoDien.cs](RangHamMat.Wpf/Controls/DuLieuGiaoDien.cs), [AppWorkspace.xaml.cs](RangHamMat.Wpf/AppWorkspace.xaml.cs): kiểm tra tại điểm đọc/lưu, giữ form khi lỗi.
- Các form khám/đơn thuốc/hóa đơn/nhân sự/đăng nhập cùng trigger Theme/HospitalStyles/NhanSu dùng control hiện hữu trong XAML.

Khi đổi schema SQL, cập nhật metadata XML tương ứng. RangHamMat.Checks đối chiếu tên cột, kiểu, nullable, khóa, precision/scale và độ dài với SQL; metadata lệch làm kiểm tra thất bại.

## Xác minh

- Nhập liệu: **531 kiểm tra đạt** trên WPF thật, gồm biên schema, thông báo/viền đỏ, sửa lỗi, ngày gõ sai rồi rời ô, form mẫu và binding.
- Checks: 39 bộ ET/DAL/BUS, 61 XAML, 349 quy tắc cột đạt.
- WPF đầy đủ: hồ sơ, lưu/sửa đơn nhiều dòng, kế hoạch/dịch vụ, hóa đơn/phân trang in và rollback đạt.
- Persistence: 16 kiểm tra đạt. Finance: 54 kiểm tra đạt.
- Mọi bản ghi thử được rollback.

Chạy riêng kiểm tra nhập sai:

```powershell
dotnet run --project RangHamMat.WpfSmoke -c Release --no-restore -v quiet -p:WarningLevel=0 -- --validation
```

Lệnh WpfSmoke mặc định cũng chạy kiểm tra nhập liệu sau các luồng nghiệp vụ. Cần Windows và SQL Server của dự án.
