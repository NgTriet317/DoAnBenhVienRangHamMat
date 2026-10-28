# Đơn giản hóa tình trạng bệnh và dữ liệu chuyên khoa — 28/10/2026

Yêu cầu: bác sĩ nhập được tình trạng bệnh dễ dàng; bỏ Mức độ trong TINH_TRANG_BENH, bỏ Cấu hình hỗ trợ của Khám điều trị; bổ sung dữ liệu mẫu và bác sĩ phù hợp dịch vụ/chuyên khoa.

Thiết kế trong phạm vi người dùng giao thực hiện: thay bảng nhập trực tiếp/danh mục/mức độ bằng form rõ ràng **Tình trạng**, **Mô tả**, **Vị trí răng/vùng**, **Ghi chú** và nút Thêm/Cập nhật/Xóa. Chỉ Tình trạng bắt buộc; mô tả để trống dùng tên tình trạng. Danh sách phía dưới để chọn sửa, không phải bảng nhập liệu. Bỏ danh mục nguồn, gợi ý mẫu và trang Cấu hình hỗ trợ. Kế hoạch bác sĩ lập trực tiếp từ dịch vụ hiện có. Lượt đã xác nhận hoặc lịch sử vẫn khóa đúng nghiệp vụ, lượt nháp đang khám nhập được.

Phương án giữ DataGrid rồi sửa combo danh mục không giải quyết độ phức tạp; phương án tự sinh chỉ định từ tình trạng không phù hợp vì bác sĩ phải tự quyết định. Form trực tiếp được chọn trong phạm vi yêu cầu giản lược.

SQL: migration06 bỏ cột MucDo thật sự, sau backup đã kiểm tra. Các liên kết/tables mẫu cũ giữ cho tính toàn vẹn dữ liệu lịch sử, không còn API/màn hình cấu hình mới. Cột MaDMTT cũ không xuất hiện trong form; giá trị đã có được giữ khi sửa bản nháp, bản mới để NULL. Schema vẫn52 bảng. Marker riêng xác nhận schema tình trạng đơn giản; bộ cài chính tích hợp06 và07.

Dữ liệu: migration07 bổ sung bác sĩ/tài khoản/ca/phòng và dịch vụ còn thiếu ở các chuyên khoa chuyên môn; không đổi chuyên khoa/giá của dịch vụ cũ đang được chứng từ sử dụng. Dữ liệu mới đánh dấu mô phỏng, có phiếu chờ và lượt nháp để thực hành. Không ký sẵn điều trị, không tạo khoản thu hoặc thay tồn. Không ghi đè lịch làm việc/dữ liệu người dùng đã có. Lặp migration không reset kết quả đã xử lý.

## Kế hoạch và kiểm tra

- [x] Tái hiện trạng thái nhập của grid hiện tại trên DLL cũ; ghi rõ khác biệt bản nháp/lịch sử.
- [x] Test RED schema/model không MucDo, form thêm/sửa/xóa không danh mục, lưu/readback không MucDo, khóa lịch sử/khác bác sĩ, dữ liệu bắt buộc/xung đột và nháp chưa thêm.
- [x] Sửa ET/BUS/DAL/metadata/XAML, gỡ API/giao diện mẫu điều trị; giữ kế hoạch thủ công, lưu nháp và hoàn tất.
- [x] Kiểm tra06/07 trên fixture sạch và nâng cấp, chạy lặp, kiểm tra độ phủ bác sĩ và liên kết; kiểm thử GUI, Admin/hàng chờ hồi quy.
- [x] Review độc lập và sửa phát hiện; backup và áp dụng vào database mặc định; đối chiếu dữ liệu cũ ngoài cột bị bỏ, build/publish và bàn giao tài khoản mẫu.

Phân công độc lập: tác nhân dữ liệu chỉ viết migration07 và tài liệu dữ liệu; tác nhân chẩn đoán chỉ kiểm tra DLL cũ trong obj/ConditionProbe. Phần UI/schema06 và tích hợp do tác nhân chính thực hiện. Không sửa chung file hoặc database thật đồng thời.

## Kết quả và dữ liệu đã áp dụng

Probe trên DLL cũ xác nhận grid của bản nháp được phép nhập và commit nội dung trong bộ nhớ; lịch sử bị khóa đúng chủ đích. Giao diện cũ cần tìm dòng trống mới, chọn danh mục và nhập mức độ nên khó sử dụng. Probe không gọi lưu SQL. Form mới có ô nhập và nút rõ ràng, giữ nội dung khi thiếu tên, đổi lựa chọn hoặc gặp xung đột. Cả Lưu nháp và Hoàn tất nhận nội dung form đang nhập trước khi xử lý.

Database `QLBenhVienRangHamMat` trên server `.` đã được cập nhật ngày 28/10/2026, sau backup `COPY_ONLY CHECKSUM` và `RESTORE VERIFYONLY WITH CHECKSUM` thành công. Backup:

```text
C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\QLBenhVienRangHamMat_before_simple_conditions_20261028_195234_d65bca37a4f24f60a0cbb159a58d9a78.bak
```

Đối chiếu theo bộ khóa gốc của từng bảng: số dòng và SHA256 của tất cả cột cũ, trừ MucDo được yêu cầu bỏ, giống nhau ở cả 52 bảng trước/sau nâng cấp. Việc so sánh không tính các dòng demo mới. Bản nháp do người dùng tạo trước đợt này vẫn giữ nguyên. Cột MucDo đã được xóa thật khỏi TINH_TRANG_BENH; ET/DTO/SQL CRUD và các màn chi tiết không còn trường này. Ba bảng danh mục/mẫu cũ và khóa nguồn được giữ để bảo toàn lịch sử; không còn màn hình/API cấu hình, gợi ý hay áp mẫu điều trị. Cấu hình hướng dẫn thuốc thuộc phân hệ kê đơn vẫn giữ.

Đã thêm 12 bác sĩ/tài khoản chuyên khoa, 7 phòng, 4 dịch vụ còn thiếu, 224 lịch làm việc, 14 bệnh nhân/hồ sơ/phiếu, 7 lượt khám nháp và 7 tình trạng mẫu. Tổng 14 bác sĩ hoạt động, bao phủ 7 chuyên khoa có dịch vụ. Không thay nhân thân, mật khẩu hay chuyên khoa của bác sĩ cũ. Xem [tài khoản, lịch và dịch vụ](DU_LIEU_MAU_CHUYEN_KHOA.md); dùng `demo_ck002_dem` / `123456` để mở phiếu mẫu đã phân lúc nạp dữ liệu trên máy hiện tại.

## Xác minh

| Kiểm tra | Kết quả |
|---|---|
| RED trên bản cũ | 1 đạt, 9 lỗi đúng các yêu cầu mới chưa có |
| Bản nâng cấp: installer cũ →06→07 | 18/18 |
| Bản cài mới đầy đủ, dùng DLL publish Release | 18/18 |
| Hồi quy toàn quyền Admin, DLL publish | 12/12 |
| Hồi quy hàng chờ bác sĩ, DLL publish | 16/16 |
| Database sử dụng: đăng nhập bác sĩ chuyên khoa mới, form tình trạng sửa được | 1/1, chỉ đọc SQL |
| Database sử dụng: đăng nhập Admin, mở 26 module và bàn giao thuốc | 3/3 |
| KiemTraBanGiao: bốn project, Views, build Debug/Release, schema/quyền/marker SQL | đạt, 0 lỗi build |
| Hash ET/DAL/BUS/WPF trong PhatHanh so với Release | khớp cả bốn DLL |
| Chạy lại seed cùng ngày | 0 dòng thêm; nội dung đã sửa và lượt đã ký giữ nguyên |

Review độc lập không phát hiện lỗi chức năng, quyền hoặc an toàn dữ liệu mới. Một dòng whitespace thừa ở CuaSoKhamDieuTri.xaml đã sửa. Một lần kiểm tra chuẩn bị snapshot JSON lỗi do ExecuteScalar đọc phân đoạn JSON; đã sửa cách đọc và chạy lại, lỗi xảy ra trước backup/migration nên chưa đổi database. Build đầy đủ trước đó còn các cảnh báo nullable sẵn có; build cuối tăng dần báo 0 cảnh báo/0 lỗi. Chưa mở rộng nghiệm thu thiết bị/DPI ngoài phạm vi thay đổi này.

Bộ kiểm mới: `tools/KiemTraTinhTrangDonGian.ps1`, source ở `tools/SimpleConditionChecks.cs.txt`; project kiểm tạm sinh trong obj, solution sản phẩm vẫn chỉ bốn project. Chế độ mặc định chỉ chấp nhận hai database fixture được nêu trong source; có ca commit để chứng minh seed không reset nên phải dựng lại fixture trước khi chạy lại toàn bộ. Hướng dẫn dựng fixture và bằng chứng trong [evidence/SimpleConditions](evidence/SimpleConditions/README.md).

```powershell
# Chỉ kiểm tra GUI đã có dữ liệu trên database sử dụng, không ghi khám:
.\tools\KiemTraTinhTrangDonGian.ps1 -BanPhatHanh -ChiGiaoDien -Database QLBenhVienRangHamMat
# Kiểm tra cấu trúc/build và SQL chỉ đọc:
.\KiemTra.bat -KiemTraSql -MayChu . -Database QLBenhVienRangHamMat
```

Bản chạy trong PhatHanh đã cập nhật DLL, SQL và hướng dẫn tài khoản, giữ appsettings của máy. Đóng phiên ứng dụng cũ rồi mở lại exe để dùng form mới. Các database/project kiểm thử tạm của đợt này được dọn sau khi lưu bằng chứng; bản backup database sử dụng được giữ.
